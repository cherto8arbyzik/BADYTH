using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class RoadPlacementController : MonoBehaviour
{
    private const float RoadWidth = 3.2f;
    private const float MinimumSegmentLength = 2f;
    private const float MaximumSegmentLength = 50f;
    private const float GridStep = 0.5f;
    private const float ValidationSampleSpacing = 1.25f;
    private const float RoadSnapDistance = 2.4f;
    private const float JointKeyStep = 0.25f;

    private readonly RaycastHit[] _raycastHits = new RaycastHit[32];
    private readonly Collider[] _overlapHits = new Collider[32];
    private readonly Dictionary<Vector2Int, GameObject> _roadJoints = new();

    private Camera _worldCamera;
    private RoadNetwork _roadNetwork;
    private Transform _roadsRoot;
    private Material _edgeMaterial;
    private Material _surfaceMaterial;
    private Material _previewEdgeMaterial;
    private Material _previewSurfaceMaterial;
    private BuildingPlacementController _buildingPlacement;
    private ResourceStockpile _stockpile;
    private SettlementState _settlement;
    private GameObject _previewRoot;
    private GameObject _snapMarker;
    private Mesh _previewEdgeMesh;
    private Mesh _previewSurfaceMesh;
    private Vector3 _startPoint;
    private Vector3 _currentPoint;
    private bool _hasStartPoint;
    private bool _hasGroundPoint;
    private bool _isPlacing;
    private bool _canPlace;
    private bool _isSnappedToRoad;
    private int _builtSegmentCount;
    private string _validationMessage = string.Empty;
    private RoadGrade _grade;

    public static bool IsAnyPlacementActive { get; private set; }
    public bool IsPlacing => _isPlacing;
    public bool CanPlace => _canPlace;
    public string ValidationMessage => _validationMessage;
    public string GradeName => GetGradeName(_grade);

    public void Initialize(
        Camera worldCamera,
        RoadNetwork roadNetwork,
        Transform roadsRoot,
        Material edgeMaterial,
        Material surfaceMaterial,
        BuildingPlacementController buildingPlacement,
        ResourceStockpile stockpile,
        SettlementState settlement)
    {
        _worldCamera = worldCamera;
        _roadNetwork = roadNetwork;
        _roadsRoot = roadsRoot;
        _edgeMaterial = edgeMaterial;
        _surfaceMaterial = surfaceMaterial;
        _buildingPlacement = buildingPlacement;
        _stockpile = stockpile;
        _settlement = settlement;
    }

    public bool BeginPlacement()
    {
        if (_worldCamera == null || _roadNetwork == null || _roadsRoot == null)
        {
            return false;
        }

        _buildingPlacement?.CancelPlacement();
        EnsurePreview();
        _isPlacing = true;
        _hasStartPoint = false;
        _hasGroundPoint = false;
        _canPlace = false;
        _validationMessage = "ЛКМ — поставить начало дороги";
        IsAnyPlacementActive = true;
        return true;
    }

    public void CancelPlacement()
    {
        _isPlacing = false;
        _hasStartPoint = false;
        _hasGroundPoint = false;
        _canPlace = false;
        _validationMessage = string.Empty;
        IsAnyPlacementActive = false;

        if (_previewRoot != null)
        {
            _previewRoot.SetActive(false);
        }

        if (_snapMarker != null)
        {
            _snapMarker.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_isPlacing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            CycleRoadGrade();
        }

        _hasGroundPoint = TryGetGroundPoint(Input.mousePosition, out _currentPoint);
        if (_hasGroundPoint)
        {
            _currentPoint.x = Mathf.Round(_currentPoint.x / GridStep) * GridStep;
            _currentPoint.z = Mathf.Round(_currentPoint.z / GridStep) * GridStep;
            _hasGroundPoint = GroundSurface.TryProjectPoint(_currentPoint, out _currentPoint);
            if (_hasGroundPoint)
            {
                ApplyRoadSnapping();
            }
        }
        else
        {
            _isSnappedToRoad = false;
            if (_snapMarker != null)
            {
                _snapMarker.SetActive(false);
            }
        }

        UpdatePreviewAndValidation();

        if (PrototypeHud.BlocksWorldInput(Input.mousePosition) || !Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!_hasGroundPoint)
        {
            return;
        }

        if (!_hasStartPoint)
        {
            if (HasBlockingCollider(_currentPoint))
            {
                _validationMessage = "Начало дороги занято препятствием";
                return;
            }

            _startPoint = _currentPoint;
            _hasStartPoint = true;
            _validationMessage = _isSnappedToRoad
                ? "Начало привязано к готовой дороге"
                : "ЛКМ — завершить отрезок   ПКМ — закончить";
            return;
        }

        if (_canPlace)
        {
            BuildSegment(_startPoint, _currentPoint);
            _startPoint = _currentPoint;
            _canPlace = false;
            _validationMessage = "ЛКМ — продолжить дорогу   ПКМ — закончить";
        }
    }

    private void UpdatePreviewAndValidation()
    {
        if (!_hasStartPoint || !_hasGroundPoint)
        {
            _canPlace = false;
            if (_previewRoot != null)
            {
                _previewRoot.SetActive(false);
            }

            if (_hasStartPoint)
            {
                _validationMessage = "Наведите конец дороги на землю";
            }

            return;
        }

        float length = Vector3.Distance(_startPoint, _currentPoint);
        if (length < 0.05f)
        {
            _canPlace = false;
            _previewRoot.SetActive(false);
            _validationMessage = "Протяните следующий отрезок дороги";
            return;
        }

        _previewRoot.SetActive(true);
        UpdateRibbon(_previewEdgeMesh, _startPoint, _currentPoint, RoadWidth + 0.48f, 0.048f);
        UpdateRibbon(_previewSurfaceMesh, _startPoint, _currentPoint, RoadWidth, 0.064f);

        if (length < MinimumSegmentLength)
        {
            SetValidation(false, "Отрезок слишком короткий");
        }
        else if (length > MaximumSegmentLength)
        {
            SetValidation(false, "Один отрезок не длиннее 50 м");
        }
        else if (!IsSegmentClear(_startPoint, _currentPoint))
        {
            SetValidation(false, "Дорога пересекает воду, край острова или препятствие");
        }
        else if (_stockpile != null && !_stockpile.Has(GetSegmentCosts(length)))
        {
            SetValidation(false, "Не хватает: " + _stockpile.GetMissingSummary(GetSegmentCosts(length)));
        }
        else
        {
            string costSummary = BuildRoadCostSummary(GetSegmentCosts(length));
            SetValidation(
                true,
                _isSnappedToRoad
                    ? $"{GradeName} • {costSummary} • ПРИВЯЗКА • ЛКМ построить"
                    : $"{GradeName} • {costSummary} • ЛКМ построить • T тип");
        }
    }

    private void ApplyRoadSnapping()
    {
        Vector3 snappedPoint;
        if (_hasStartPoint)
        {
            _isSnappedToRoad = _roadNetwork.TryGetNearestPoint(
                _currentPoint,
                RoadSnapDistance,
                _startPoint,
                MinimumSegmentLength * 0.85f,
                out snappedPoint);
        }
        else
        {
            _isSnappedToRoad = _roadNetwork.TryGetNearestPoint(
                _currentPoint,
                RoadSnapDistance,
                out snappedPoint);
        }

        if (_isSnappedToRoad)
        {
            if (GroundSurface.TryProjectPoint(snappedPoint, out Vector3 projectedSnap))
            {
                _currentPoint = projectedSnap;
            }
            else
            {
                _isSnappedToRoad = false;
            }
        }

        if (_snapMarker != null)
        {
            _snapMarker.transform.position = _currentPoint + Vector3.up * 0.105f;
            _snapMarker.SetActive(_isSnappedToRoad);
        }
    }

    private bool IsSegmentClear(Vector3 start, Vector3 end)
    {
        Vector3 horizontalDirection = end - start;
        horizontalDirection.y = 0f;
        float horizontalLength = horizontalDirection.magnitude;
        Vector3 side = horizontalLength <= 0.0001f
            ? Vector3.right
            : Vector3.Cross(Vector3.up, horizontalDirection / horizontalLength);
        float length = Vector3.Distance(start, end);
        int sampleCount = Mathf.Max(2, Mathf.CeilToInt(length / ValidationSampleSpacing));
        for (int index = 0; index <= sampleCount; index++)
        {
            Vector3 sample = Vector3.Lerp(start, end, index / (float)sampleCount);
            if (!GroundSurface.TryProjectPoint(sample, out Vector3 projected, out Vector3 normal) ||
                Vector3.Angle(normal, Vector3.up) > 35f ||
                !HasGroundAt(projected - side * RoadWidth * 0.5f) ||
                !HasGroundAt(projected + side * RoadWidth * 0.5f) ||
                HasBlockingCollider(projected))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasGroundAt(Vector3 point)
    {
        return GroundSurface.TryProjectPoint(point, out _, out Vector3 normal) &&
               Vector3.Angle(normal, Vector3.up) <= 35f;
    }

    private bool HasBlockingCollider(Vector3 point)
    {
        int overlapCount = Physics.OverlapSphereNonAlloc(
            point + Vector3.up * 0.22f,
            RoadWidth * 0.36f,
            _overlapHits,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int index = 0; index < overlapCount; index++)
        {
            Collider overlap = _overlapHits[index];
            if (overlap == null || !overlap.enabled ||
                overlap.GetComponentInParent<GroundSurface>() != null ||
                overlap.GetComponentInParent<TownResident>() != null ||
                overlap.GetComponentInParent<SelectableUnit>() != null ||
                overlap.GetComponentInParent<EnemyUnit>() != null ||
                (_previewRoot != null && overlap.transform.IsChildOf(_previewRoot.transform)))
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
        int hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, 500f, ~0, QueryTriggerInteraction.Ignore);
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = _raycastHits[index];
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

    private void BuildSegment(Vector3 start, Vector3 end)
    {
        float length = Vector3.Distance(start, end);
        if (_stockpile != null && !_stockpile.TrySpend(GetSegmentCosts(length)))
        {
            return;
        }

        GameObject road = new($"Built Road {_builtSegmentCount + 1}");
        road.transform.SetParent(_roadsRoot, false);
        CreateRoadPart(road.transform, "Road Edge", start, end, RoadWidth + 0.48f, 0.048f, _edgeMaterial, false, out _);
        CreateRoadPart(road.transform, "Road Surface", start, end, RoadWidth, 0.064f, CreateSurfaceMaterial(_grade), false, out _);
        RoadConstructionSite constructionSite = road.AddComponent<RoadConstructionSite>();
        constructionSite.Initialize(this, start, end, Mathf.Max(4f, length * GetWorkPerMeter(_grade)), _grade);
        _builtSegmentCount++;
    }

    public void RegisterCompletedRoad(Vector3 start, Vector3 end, RoadGrade grade)
    {
        EnsureRoadJoint(start, grade);
        EnsureRoadJoint(end, grade);
        _roadNetwork.RegisterSegment(start, end, RoadWidth, GetSpeedMultiplier(grade));
    }

    private void CycleRoadGrade()
    {
        RoadGrade candidate = (RoadGrade)(((int)_grade + 1) % 3);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (CanUseGrade(candidate))
            {
                _grade = candidate;
                return;
            }

            candidate = (RoadGrade)(((int)candidate + 1) % 3);
        }
    }

    private bool CanUseGrade(RoadGrade grade)
    {
        if (grade == RoadGrade.Dirt)
        {
            return true;
        }

        if (_settlement == null)
        {
            return false;
        }

        return grade == RoadGrade.Stone
            ? _settlement.CurrentTier >= SettlementTier.Posad
            : _settlement.CurrentTier >= SettlementTier.Stronghold;
    }

    private ResourceAmount[] GetSegmentCosts(float length)
    {
        int units = Mathf.Max(1, Mathf.CeilToInt(length / 5f));
        return _grade switch
        {
            RoadGrade.Stone => new[]
            {
                new ResourceAmount(ResourceType.Stone, units)
            },
            RoadGrade.Fortified => new[]
            {
                new ResourceAmount(ResourceType.Stone, units * 2),
                new ResourceAmount(ResourceType.Brick, units)
            },
            _ => System.Array.Empty<ResourceAmount>()
        };
    }

    private Material CreateSurfaceMaterial(RoadGrade grade)
    {
        if (grade == RoadGrade.Dirt)
        {
            return _surfaceMaterial;
        }

        Material material = new(_surfaceMaterial)
        {
            color = grade == RoadGrade.Stone
                ? new Color(0.37f, 0.39f, 0.38f)
                : new Color(0.29f, 0.31f, 0.31f),
            hideFlags = HideFlags.DontSave
        };
        return material;
    }

    private static float GetWorkPerMeter(RoadGrade grade)
    {
        return grade switch
        {
            RoadGrade.Stone => 0.5f,
            RoadGrade.Fortified => 0.7f,
            _ => 0.35f
        };
    }

    private static float GetSpeedMultiplier(RoadGrade grade)
    {
        return grade switch
        {
            RoadGrade.Stone => 1.75f,
            RoadGrade.Fortified => 2f,
            _ => 1.5f
        };
    }

    private static string GetGradeName(RoadGrade grade)
    {
        return grade switch
        {
            RoadGrade.Stone => "КАМЕННАЯ ДОРОГА",
            RoadGrade.Fortified => "УКРЕПЛЁННАЯ ДОРОГА",
            _ => "ГРУНТОВАЯ ДОРОГА"
        };
    }

    private static string BuildRoadCostSummary(IReadOnlyList<ResourceAmount> costs)
    {
        if (costs == null || costs.Count == 0)
        {
            return "только работа";
        }

        System.Text.StringBuilder builder = new();
        foreach (ResourceAmount cost in costs)
        {
            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(ResourceNames.GetShort(cost.Type));
            builder.Append(" ");
            builder.Append(cost.Amount);
        }

        return builder.ToString();
    }

    private void EnsurePreview()
    {
        if (_previewRoot != null)
        {
            return;
        }

        _previewEdgeMaterial = CreatePreviewMaterial(new Color(0.18f, 0.82f, 0.92f, 0.42f));
        _previewSurfaceMaterial = CreatePreviewMaterial(new Color(0.24f, 0.92f, 0.55f, 0.52f));
        _previewRoot = new GameObject("Road Preview");
        _previewRoot.transform.SetParent(transform, false);
        CreateRoadPart(_previewRoot.transform, "Preview Edge", Vector3.zero, Vector3.forward, RoadWidth + 0.48f, 0.048f, _previewEdgeMaterial, true, out _previewEdgeMesh);
        CreateRoadPart(_previewRoot.transform, "Preview Surface", Vector3.zero, Vector3.forward, RoadWidth, 0.064f, _previewSurfaceMaterial, true, out _previewSurfaceMesh);
        _previewRoot.SetActive(false);

        _snapMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _snapMarker.name = "Road Snap Marker";
        _snapMarker.transform.SetParent(transform, false);
        _snapMarker.transform.localScale = new Vector3(0.72f, 0.018f, 0.72f);
        Collider markerCollider = _snapMarker.GetComponent<Collider>();
        markerCollider.enabled = false;
        MeshRenderer markerRenderer = _snapMarker.GetComponent<MeshRenderer>();
        markerRenderer.sharedMaterial = _previewEdgeMaterial;
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _snapMarker.SetActive(false);
    }

    private void EnsureRoadJoint(Vector3 position, RoadGrade grade)
    {
        Vector2Int key = new(
            Mathf.RoundToInt(position.x / JointKeyStep),
            Mathf.RoundToInt(position.z / JointKeyStep));
        if (_roadJoints.ContainsKey(key))
        {
            return;
        }

        GameObject joint = new($"Road Joint {key.x}:{key.y}");
        joint.transform.SetParent(_roadsRoot, false);
        CreateRoadDisc(joint.transform, "Joint Edge", position, (RoadWidth + 0.48f) * 0.5f, 0.052f, _edgeMaterial);
        CreateRoadDisc(joint.transform, "Rounded Turn", position, RoadWidth * 0.5f, 0.068f, CreateSurfaceMaterial(grade));
        _roadJoints.Add(key, joint);
    }

    private static void CreateRoadDisc(
        Transform parent,
        string name,
        Vector3 center,
        float radius,
        float height,
        Material material)
    {
        const int segmentCount = 28;
        Vector3[] vertices = new Vector3[segmentCount + 1];
        int[] triangles = new int[segmentCount * 3];
        if (GroundSurface.TryProjectPoint(center, out Vector3 projectedCenter))
        {
            center = projectedCenter;
        }

        vertices[0] = center + Vector3.up * height;

        for (int index = 0; index < segmentCount; index++)
        {
            float angle = index * Mathf.PI * 2f / segmentCount;
            Vector3 perimeter = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius);
            if (GroundSurface.TryProjectPoint(perimeter, out Vector3 projectedPerimeter))
            {
                perimeter = projectedPerimeter;
            }

            vertices[index + 1] = perimeter + Vector3.up * height;

            int triangle = index * 3;
            triangles[triangle] = 0;
            triangles[triangle + 1] = (index + 1) % segmentCount + 1;
            triangles[triangle + 2] = index + 1;
        }

        Mesh mesh = new()
        {
            name = name + " Mesh",
            hideFlags = HideFlags.DontSave,
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject disc = new(name);
        disc.transform.SetParent(parent, false);
        disc.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = disc.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    private void SetValidation(bool valid, string message)
    {
        _canPlace = valid;
        _validationMessage = message;
        if (_previewSurfaceMaterial != null)
        {
            _previewSurfaceMaterial.color = valid
                ? new Color(0.24f, 0.92f, 0.55f, 0.52f)
                : new Color(0.95f, 0.22f, 0.16f, 0.52f);
        }
    }

    private static void CreateRoadPart(
        Transform parent,
        string name,
        Vector3 start,
        Vector3 end,
        float width,
        float height,
        Material material,
        bool dynamic,
        out Mesh mesh)
    {
        GameObject part = new(name);
        part.transform.SetParent(parent, false);
        mesh = new Mesh
        {
            name = name + " Mesh",
            hideFlags = HideFlags.DontSave
        };
        if (dynamic)
        {
            mesh.MarkDynamic();
        }

        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
        UpdateRibbon(mesh, start, end, width, height);
    }

    private static void UpdateRibbon(Mesh mesh, Vector3 start, Vector3 end, float width, float height)
    {
        Vector3 horizontal = end - start;
        horizontal.y = 0f;
        int segmentCount = Mathf.Max(
            1,
            Mathf.CeilToInt(horizontal.magnitude / ValidationSampleSpacing));
        int rowCount = segmentCount + 1;
        Vector3[] centers = new Vector3[rowCount];
        Vector3[] vertices = new Vector3[rowCount * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segmentCount * 6];
        float travelled = 0f;

        for (int row = 0; row < rowCount; row++)
        {
            Vector3 center = Vector3.Lerp(start, end, row / (float)segmentCount);
            if (GroundSurface.TryProjectPoint(center, out Vector3 projected))
            {
                center = projected;
            }

            centers[row] = center;
        }

        for (int row = 0; row < rowCount; row++)
        {
            Vector3 tangent = row == 0
                ? centers[1] - centers[0]
                : row == rowCount - 1
                    ? centers[row] - centers[row - 1]
                    : centers[row + 1] - centers[row - 1];
            tangent.y = 0f;
            Vector3 side = tangent.sqrMagnitude <= 0.0001f
                ? Vector3.right
                : Vector3.Cross(Vector3.up, tangent.normalized);
            side *= width * 0.5f;

            Vector3 left = centers[row] - side;
            Vector3 right = centers[row] + side;
            if (GroundSurface.TryProjectPoint(left, out Vector3 projectedLeft))
            {
                left = projectedLeft;
            }

            if (GroundSurface.TryProjectPoint(right, out Vector3 projectedRight))
            {
                right = projectedRight;
            }

            if (row > 0)
            {
                travelled += Vector3.Distance(centers[row - 1], centers[row]);
            }

            int vertex = row * 2;
            vertices[vertex] = left + Vector3.up * height;
            vertices[vertex + 1] = right + Vector3.up * height;
            float uvY = travelled / Mathf.Max(0.1f, width);
            uvs[vertex] = new Vector2(0f, uvY);
            uvs[vertex + 1] = new Vector2(1f, uvY);

            if (row >= segmentCount)
            {
                continue;
            }

            int triangle = row * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 2;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex + 1;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private static Material CreatePreviewMaterial(Color color)
    {
        Material material = new(Shader.Find("Standard"))
        {
            color = color,
            hideFlags = HideFlags.DontSave
        };
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        return material;
    }

    private void OnDestroy()
    {
        CancelPlacement();
        if (_previewEdgeMaterial != null)
        {
            Destroy(_previewEdgeMaterial);
        }

        if (_previewSurfaceMaterial != null)
        {
            Destroy(_previewSurfaceMaterial);
        }
    }
}
}
