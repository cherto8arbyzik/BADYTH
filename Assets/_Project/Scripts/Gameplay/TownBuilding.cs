using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class TownBuilding : MonoBehaviour
{
    private readonly List<Material> _visualMaterials = new();
    private readonly List<Color> _restoredColors = new();

    private GameObject _damageVisual;
    private GameObject _restoredVisual;
    private GameObject _selectionIndicator;
    private GameObject _blueprintVisual;
    private GameObject _constructionPiecesVisual;
    private Renderer[] _constructionPieces = System.Array.Empty<Renderer>();
    private BuildingDefinition _definition;
    private SettlementState _settlement;
    private TownConstructionSite _constructionSite;
    private TownWorkplace _workplace;
    private GridNavigationService _navigation;
    private IReadOnlyList<Vector2Int> _navigationReservation;
    private float _constructionProgress = 1f;
    private bool _effectRegistered;
    private bool _constructionComplete = true;
    private bool _canDemolish;
    private bool _demolitionRequested;

    public string DisplayName { get; private set; }
    public int RestorationCost { get; private set; }
    public bool IsRuined { get; private set; }
    public bool IsSelected { get; private set; }
    public BuildingDefinition Definition => _definition;
    public bool IsUnderConstruction => !_constructionComplete;
    public bool IsOperational => !IsRuined && _constructionComplete;
    public bool CanDemolish => _canDemolish;
    public float ConstructionProgress => _constructionProgress;
    public int AssignedWorkerCount => _constructionSite == null ? 0 : _constructionSite.AssignedWorkerCount;
    public int ProductionWorkerCount => _workplace == null ? 0 : _workplace.WorkerCount;
    public int ProductionWorkerCapacity => _workplace == null ? 0 : _workplace.WorkerCapacity;
    public float ProductionProgress => _workplace == null ? 0f : _workplace.CycleProgress;
    public string ProductionStatus => _workplace == null ? string.Empty : _workplace.Status;
    public ProductionRecipe ProductionRecipe => _workplace == null ? null : _workplace.Recipe;

    public void Initialize(
        string displayName,
        int restorationCost,
        bool isRuined,
        Renderer[] renderers,
        GameObject damageVisual,
        Bounds worldBounds,
        GameObject restoredVisual = null)
    {
        DisplayName = displayName;
        RestorationCost = Mathf.Max(0, restorationCost);
        IsRuined = isRuined;
        _damageVisual = damageVisual;
        _restoredVisual = restoredVisual;

        CaptureMaterials(renderers);
        CreateSelectionIndicator(worldBounds);

        if (IsRuined)
        {
            if (_restoredVisual != null)
            {
                _restoredVisual.SetActive(false);
            }

            if (_damageVisual != null)
            {
                _damageVisual.SetActive(true);
            }

            if (_restoredVisual == null)
            {
                ApplyRuinedTint();
            }
        }
        else if (_damageVisual != null)
        {
            _damageVisual.SetActive(false);
        }
    }

    public void InitializeConstruction(
        BuildingDefinition definition,
        SettlementState settlement,
        TownConstructionSite constructionSite,
        TownWorkplace workplace,
        GridNavigationService navigation,
        IReadOnlyList<Vector2Int> navigationReservation,
        GameObject blueprintVisual,
        GameObject constructionVisual,
        GameObject constructionPiecesVisual,
        Renderer[] constructionPieces,
        Renderer[] constructionRenderers,
        Bounds worldBounds)
    {
        DisplayName = definition == null ? "Стройплощадка" : definition.DisplayName;
        RestorationCost = 0;
        IsRuined = false;
        _definition = definition;
        _settlement = settlement;
        _constructionSite = constructionSite;
        _workplace = workplace;
        _navigation = navigation;
        _navigationReservation = navigationReservation;
        _blueprintVisual = blueprintVisual;
        _restoredVisual = constructionVisual;
        _constructionPiecesVisual = constructionPiecesVisual;
        _constructionPieces = constructionPieces ?? System.Array.Empty<Renderer>();
        _constructionComplete = false;
        _canDemolish = true;
        _effectRegistered = false;

        _workplace?.SetOperational(false);

        if (_restoredVisual != null)
        {
            _restoredVisual.SetActive(false);
        }

        CaptureMaterials(constructionRenderers);
        CreateSelectionIndicator(worldBounds);
        SetConstructionProgress(0f);
    }

    public bool TryRestore(ResourceStockpile stockpile)
    {
        if (!IsRuined || stockpile == null || !stockpile.TrySpendWood(RestorationCost))
        {
            return false;
        }

        IsRuined = false;
        RestoreMaterialColors();

        if (_restoredVisual != null)
        {
            _restoredVisual.SetActive(true);
        }

        if (_damageVisual != null)
        {
            _damageVisual.SetActive(false);
        }

        return true;
    }

    public void ConfigureDefinition(BuildingDefinition definition, SettlementState settlement)
    {
        if (_effectRegistered && _definition != null && _settlement != null)
        {
            _settlement.UnregisterBuilding(_definition);
        }

        _definition = definition;
        _settlement = settlement;
        _effectRegistered = _definition != null && _settlement != null;
        if (_effectRegistered)
        {
            _settlement.RegisterBuilding(_definition);
        }
    }

    public void SetConstructionProgress(float progress)
    {
        if (_constructionComplete)
        {
            return;
        }

        _constructionProgress = Mathf.Clamp01(progress);
        if (_constructionPiecesVisual != null)
        {
            _constructionPiecesVisual.SetActive(true);
            int visiblePieces = Mathf.CeilToInt(_constructionProgress * _constructionPieces.Length);
            for (int index = 0; index < _constructionPieces.Length; index++)
            {
                if (_constructionPieces[index] != null)
                {
                    _constructionPieces[index].enabled = index < visiblePieces;
                }
            }
        }
    }

    public void CompleteConstruction()
    {
        if (_constructionComplete || _demolitionRequested)
        {
            return;
        }

        _constructionProgress = 1f;
        _constructionComplete = true;

        if (_restoredVisual != null)
        {
            _restoredVisual.SetActive(true);
        }

        if (_constructionPiecesVisual != null)
        {
            _constructionPiecesVisual.SetActive(false);
        }

        if (_blueprintVisual != null)
        {
            _blueprintVisual.SetActive(false);
        }

        if (!_effectRegistered && _definition != null && _settlement != null)
        {
            _settlement.RegisterBuilding(_definition);
            _effectRegistered = true;
        }

        _workplace?.SetOperational(true);
    }

    public bool TryAssignProductionWorker()
    {
        return IsOperational && _workplace != null && _workplace.TryAssignAvailableResident();
    }

    public bool ReleaseProductionWorker()
    {
        return _workplace != null && _workplace.ReleaseOneWorker();
    }

    public bool Demolish()
    {
        if (!_canDemolish || _demolitionRequested)
        {
            return false;
        }

        _demolitionRequested = true;
        _constructionSite?.Cancel();
        UnregisterEffect();
        ReleaseNavigationReservation();
        SetSelected(false);

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }

        return true;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (_selectionIndicator != null)
        {
            _selectionIndicator.SetActive(selected);
        }
    }

    private void CaptureMaterials(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] materials = new Material[sourceMaterials.Length];
            for (int index = 0; index < sourceMaterials.Length; index++)
            {
                Material source = sourceMaterials[index];
                if (source == null)
                {
                    continue;
                }

                Material material = new(source)
                {
                    hideFlags = HideFlags.DontSave
                };
                materials[index] = material;
                _visualMaterials.Add(material);
                _restoredColors.Add(material.color);
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void ApplyRuinedTint()
    {
        Color soot = new(0.09f, 0.08f, 0.07f);

        for (int index = 0; index < _visualMaterials.Count; index++)
        {
            _visualMaterials[index].color = Color.Lerp(_restoredColors[index], soot, 0.48f);
        }
    }

    private void RestoreMaterialColors()
    {
        for (int index = 0; index < _visualMaterials.Count; index++)
        {
            _visualMaterials[index].color = _restoredColors[index];
        }
    }

    private void CreateSelectionIndicator(Bounds worldBounds)
    {
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _selectionIndicator.name = "Building Selection";
        _selectionIndicator.transform.SetParent(transform, true);
        _selectionIndicator.transform.position = new Vector3(
            worldBounds.center.x,
            0.045f,
            worldBounds.center.z);

        float diameter = Mathf.Max(worldBounds.size.x, worldBounds.size.z) + 0.9f;
        _selectionIndicator.transform.localScale = new Vector3(diameter, 0.018f, diameter);

        Collider collider = _selectionIndicator.GetComponent<Collider>();
        collider.enabled = false;

        Material material = new(Shader.Find("Standard"))
        {
            color = new Color(1f, 0.68f, 0.16f, 0.30f),
            hideFlags = HideFlags.DontSave
        };
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        _selectionIndicator.GetComponent<Renderer>().sharedMaterial = material;
        _selectionIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        UnregisterEffect();
        ReleaseNavigationReservation();
    }

    private void UnregisterEffect()
    {
        if (!_effectRegistered || _definition == null || _settlement == null)
        {
            return;
        }

        _settlement.UnregisterBuilding(_definition);
        _effectRegistered = false;
    }

    private void ReleaseNavigationReservation()
    {
        if (_navigation == null || _navigationReservation == null)
        {
            return;
        }

        _navigation.ReleaseBlocked(_navigationReservation);
        _navigationReservation = null;
    }
}
}
