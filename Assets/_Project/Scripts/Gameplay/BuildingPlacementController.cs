using Hollowwest.Economy;
using Hollowwest.Navigation;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class BuildingPlacementController : MonoBehaviour
{
    private const float RoadHalfWidth = 1.6f;
    private const float GridStep = 2f;
    private const float RotationDegreesPerPixel = 0.42f;

    private Camera _worldCamera;
    private ResourceStockpile _stockpile;
    private GridNavigationService _navigation;
    private RoadNetwork _roadNetwork;
    private PlacementGridOverlay _gridOverlay;
    private SettlementState _settlement;
    private Bounds _worldBounds;
    private Vector3[] _roadPoints;
    private Transform _buildingsRoot;
    private BuildingDefinition _activeDefinition;
    private BuildingVisualVariant _activeVisualVariant;
    private GameObject _preview;
    private Renderer[] _previewRenderers;
    private Material _previewMaterial;
    private float _yaw;
    private float _rotationDragStartMouseX;
    private float _rotationDragStartYaw;
    private bool _hasGround;
    private bool _canPlace;
    private bool _isRotationDragging;
    private bool _placementClickArmed;
    private bool _gridSnappingEnabled = true;
    private string _validationMessage = string.Empty;

    public static bool IsAnyPlacementActive { get; private set; }
    public bool IsPlacing => _activeDefinition != null;
    public bool CanPlace => _canPlace;
    public BuildingDefinition ActiveDefinition => _activeDefinition;
    public BuildingVisualVariant ActiveVisualVariant => _activeVisualVariant;
    public string ValidationMessage => _validationMessage;
    public bool GridSnappingEnabled => _gridSnappingEnabled;

    public void Initialize(
        Camera worldCamera,
        ResourceStockpile stockpile,
        GridNavigationService navigation,
        SettlementState settlement,
        Bounds worldBounds,
        Vector3[] roadPoints,
        RoadNetwork roadNetwork,
        PlacementGridOverlay gridOverlay,
        Transform buildingsRoot)
    {
        _worldCamera = worldCamera;
        _stockpile = stockpile;
        _navigation = navigation;
        _settlement = settlement;
        _worldBounds = worldBounds;
        _roadPoints = roadPoints;
        _roadNetwork = roadNetwork;
        _gridOverlay = gridOverlay;
        _buildingsRoot = buildingsRoot;
    }

    public bool BeginPlacement(BuildingDefinition definition)
    {
        return BeginPlacement(definition, null);
    }

    public bool BeginPlacement(BuildingDefinition definition, BuildingVisualVariant visualVariant)
    {
        if (definition == null || _worldCamera == null || _stockpile == null)
        {
            return false;
        }

        CancelPlacement();
        _activeDefinition = definition;
        _activeVisualVariant = visualVariant;
        _placementClickArmed = false;
        _yaw = 0f;
        _preview = TownConstructionFactory.CreatePreview(
            definition,
            transform,
            out _previewRenderers,
            visualVariant?.ModelName);
        _preview.name = definition.DisplayName + " Preview";
        _previewMaterial = CreatePreviewMaterial();

        foreach (Renderer renderer in _previewRenderers)
        {
            renderer.sharedMaterial = _previewMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        IsAnyPlacementActive = true;
        return true;
    }

    public void CancelPlacement()
    {
        if (_preview != null)
        {
            Destroy(_preview);
        }

        if (_previewMaterial != null)
        {
            Destroy(_previewMaterial);
        }

        _preview = null;
        _previewMaterial = null;
        _previewRenderers = null;
        _activeDefinition = null;
        _activeVisualVariant = null;
        _hasGround = false;
        _canPlace = false;
        _isRotationDragging = false;
        _placementClickArmed = false;
        _gridOverlay?.Hide();
        _validationMessage = string.Empty;
        IsAnyPlacementActive = false;
    }

    private void Update()
    {
        if (!IsPlacing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        HandleGridInput();
        HandleRotationInput();

        UpdatePreviewPosition();
        UpdateValidation();

        // A variant is selected with the same left mouse button that places a
        // building. Wait for that selection click to be fully released before
        // accepting a placement click, otherwise a fast UI click can also put
        // the building underneath the picker.
        if (!_placementClickArmed)
        {
            if (!Input.GetMouseButton(0))
            {
                _placementClickArmed = true;
            }

            return;
        }

        if (_hasGround &&
            _canPlace &&
            Input.GetMouseButtonDown(0) &&
            !_isRotationDragging &&
            !Input.GetKey(KeyCode.R) &&
            !PrototypeHud.BlocksWorldInput(Input.mousePosition))
        {
            PlaceActiveBuilding();
        }
    }

    private void UpdatePreviewPosition()
    {
        if (_isRotationDragging && _preview != null)
        {
            _preview.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            return;
        }

        _hasGround = TryGetGroundPoint(Input.mousePosition, out Vector3 groundPoint);
        if (!_hasGround || _preview == null)
        {
            if (_preview != null)
            {
                _preview.SetActive(false);
            }

            _gridOverlay?.Hide();

            return;
        }

        if (_gridSnappingEnabled)
        {
            groundPoint = PlacementGridOverlay.Snap(groundPoint, GridStep);
            _gridOverlay?.ShowAt(groundPoint);
        }
        else
        {
            _gridOverlay?.Hide();
        }

        groundPoint.y = 0.02f;
        _preview.SetActive(true);
        _preview.transform.position = groundPoint;
        _preview.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private void UpdateValidation()
    {
        _canPlace = false;

        if (!_hasGround || _preview == null)
        {
            _validationMessage = "Наведите на землю";
            ApplyPreviewColor(false);
            return;
        }

        Vector3 position = _preview.transform.position;
        if (!BuildingPlacementRules.IsInsideBounds(_worldBounds, position, _activeDefinition.Footprint))
        {
            _validationMessage = "Слишком близко к краю поселения";
        }
        else if (!_settlement.IsBuildingUnlocked(_activeDefinition))
        {
            _validationMessage = _settlement.GetBuildingLockReason(_activeDefinition);
        }
        else if (!_stockpile.Has(_activeDefinition.ConstructionCosts))
        {
            _validationMessage = "Не хватает: " + _stockpile.GetMissingSummary(_activeDefinition.ConstructionCosts);
        }
        else if (_activeDefinition.PlacementRequirement == BuildingPlacementRequirement.LakeShore &&
                 !LakeShoreArea.IsAnyShoreNear(position))
        {
            _validationMessage = "Рыбацкий стан нужно поставить у берега озера";
        }
        else if ((_roadNetwork != null && _roadNetwork.OverlapsArea(position, _activeDefinition.Footprint * 0.5f)) ||
                 (_roadNetwork == null && BuildingPlacementRules.OverlapsRoad(position, _activeDefinition.Footprint, _roadPoints, RoadHalfWidth)))
        {
            _validationMessage = "Нельзя перекрывать дорогу";
        }
        else if (IsOccupied(position, _activeDefinition.Footprint))
        {
            _validationMessage = "Место занято";
        }
        else
        {
            _validationMessage = _gridSnappingEnabled
                ? "ЛКМ — построить   R — поворот   G — сетка ВКЛ"
                : "ЛКМ — построить   R — поворот   G — сетка ВЫКЛ";
            _canPlace = true;
        }

        ApplyPreviewColor(_canPlace);
    }

    private void HandleRotationInput()
    {
        bool pointerOverInterface = PrototypeHud.BlocksWorldInput(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.R) && !Input.GetMouseButton(0))
        {
            _yaw = Mathf.Repeat(_yaw + 90f, 360f);
        }

        if (!_isRotationDragging &&
            _hasGround &&
            _preview != null &&
            Input.GetKey(KeyCode.R) &&
            Input.GetMouseButtonDown(0) &&
            !pointerOverInterface)
        {
            _isRotationDragging = true;
            _rotationDragStartMouseX = Input.mousePosition.x;
            _rotationDragStartYaw = _yaw;
        }

        if (!_isRotationDragging)
        {
            return;
        }

        if (!Input.GetMouseButton(0) || !Input.GetKey(KeyCode.R))
        {
            _isRotationDragging = false;
            return;
        }

        float horizontalDrag = Input.mousePosition.x - _rotationDragStartMouseX;
        _yaw = Mathf.Repeat(_rotationDragStartYaw + horizontalDrag * RotationDegreesPerPixel, 360f);
    }

    private void HandleGridInput()
    {
        if (!Input.GetKeyDown(KeyCode.G))
        {
            return;
        }

        _gridSnappingEnabled = !_gridSnappingEnabled;
        if (!_gridSnappingEnabled)
        {
            _gridOverlay?.Hide();
        }
    }

    private bool IsOccupied(Vector3 position, float footprint)
    {
        Vector3 halfExtents = new(footprint * 0.43f, 2.5f, footprint * 0.37f);
        Vector3 center = position + Vector3.up * halfExtents.y;
        Collider[] overlaps = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.Euler(0f, _yaw, 0f),
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null || !overlap.enabled)
            {
                continue;
            }

            if (overlap.GetComponentInParent<GroundSurface>() != null)
            {
                continue;
            }

            if (_activeDefinition.PlacementRequirement == BuildingPlacementRequirement.LakeShore &&
                overlap.GetComponentInParent<LakeShoreArea>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 groundPoint)
    {
        Ray ray = _worldCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 400f, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<GroundSurface>() == null)
            {
                continue;
            }

            groundPoint = hit.point;
            return true;
        }

        groundPoint = default;
        return false;
    }

    private void PlaceActiveBuilding()
    {
        if (!_settlement.IsBuildingUnlocked(_activeDefinition) ||
            !_stockpile.TrySpend(_activeDefinition.ConstructionCosts))
        {
            UpdateValidation();
            return;
        }

        Vector3 position = _preview.transform.position;
        position.y = 0f;
        TownConstructionFactory.CreateConstructionSite(
            _activeDefinition,
            _buildingsRoot,
            position,
            _yaw,
            _navigation,
            _settlement,
            _stockpile,
            _activeVisualVariant?.ModelName);
        Physics.SyncTransforms();
        UpdateValidation();
    }

    private void ApplyPreviewColor(bool valid)
    {
        if (_previewMaterial == null)
        {
            return;
        }

        _previewMaterial.color = valid
            ? new Color(0.26f, 0.92f, 0.42f, 0.48f)
            : new Color(0.95f, 0.22f, 0.16f, 0.48f);
    }

    private static Material CreatePreviewMaterial()
    {
        Material material = new(Shader.Find("Standard"))
        {
            color = new Color(0.26f, 0.92f, 0.42f, 0.48f),
            hideFlags = HideFlags.DontSave
        };
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void OnDestroy()
    {
        CancelPlacement();
    }
}
}
