using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Presentation;
using Hollowwest.Selection;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class GatheringAreaController : MonoBehaviour
{
    private readonly List<ResourceNode> _orderedNodes = new();

    private Camera _worldCamera;
    private ResourceStockpile _stockpile;
    private GameObject _selectionVisual;
    private Vector3 _dragStart;
    private Vector3 _dragEnd;
    private ResourceType? _filter;
    private bool _awaitingDrag;
    private bool _dragging;
    private float _selectionVisibleUntil;

    public static bool IsAnyGatheringActive { get; private set; }
    public bool IsSelecting => _awaitingDrag || _dragging;
    public ResourceType? Filter => _filter;
    public int OrderedNodeCount => _orderedNodes.Count;

    public void Initialize(Camera worldCamera, ResourceStockpile stockpile)
    {
        _worldCamera = worldCamera;
        _stockpile = stockpile;
        CreateSelectionVisual();
    }

    public void BeginSelection(ResourceType? filter)
    {
        _filter = filter;
        _awaitingDrag = true;
        _dragging = false;
        IsAnyGatheringActive = true;
    }

    public void CancelSelection()
    {
        _awaitingDrag = false;
        _dragging = false;
        IsAnyGatheringActive = false;
        if (_selectionVisual != null)
        {
            _selectionVisual.SetActive(false);
        }
    }

    public int IssueOrder(Bounds worldArea, ResourceType? filter)
    {
        _filter = filter;
        _orderedNodes.Clear();
        Vector3 minimum = worldArea.min;
        Vector3 maximum = worldArea.max;
        IReadOnlyList<ResourceNode> availableNodes = ResourceNode.ActiveNodes;
        if (availableNodes.Count == 0)
        {
            availableNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        }

        foreach (ResourceNode node in availableNodes)
        {
            if (node == null || node.IsDepleted || (filter.HasValue && node.ResourceType != filter.Value))
            {
                continue;
            }

            Vector3 position = node.transform.position;
            if (position.x >= minimum.x && position.x <= maximum.x &&
                position.z >= minimum.z && position.z <= maximum.z)
            {
                _orderedNodes.Add(node);
            }
        }

        Vector3 center = worldArea.center;
        _orderedNodes.Sort((left, right) =>
            Vector3.SqrMagnitude(left.transform.position - center)
                .CompareTo(Vector3.SqrMagnitude(right.transform.position - center)));
        return _orderedNodes.Count;
    }

    private void Update()
    {
        RemoveInvalidOrders();
        AssignAvailableResidents();

        if (_selectionVisual != null && !_dragging && Time.time >= _selectionVisibleUntil)
        {
            _selectionVisual.SetActive(false);
        }

        if (!_awaitingDrag || _worldCamera == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelSelection();
            return;
        }

        if (PrototypeHud.BlocksWorldInput(Input.mousePosition))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && TryGetGroundPoint(Input.mousePosition, out _dragStart))
        {
            _dragEnd = _dragStart;
            _dragging = true;
            UpdateSelectionVisual();
        }

        if (_dragging && Input.GetMouseButton(0) && TryGetGroundPoint(Input.mousePosition, out Vector3 current))
        {
            _dragEnd = current;
            UpdateSelectionVisual();
        }

        if (_dragging && Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            _awaitingDrag = false;
            IsAnyGatheringActive = false;
            CreateOrdersFromSelection();
            _selectionVisibleUntil = Time.time + 2.5f;
        }
    }

    private void CreateOrdersFromSelection()
    {
        float minX = Mathf.Min(_dragStart.x, _dragEnd.x);
        float maxX = Mathf.Max(_dragStart.x, _dragEnd.x);
        float minZ = Mathf.Min(_dragStart.z, _dragEnd.z);
        float maxZ = Mathf.Max(_dragStart.z, _dragEnd.z);
        Bounds area = new(
            new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f),
            new Vector3(Mathf.Max(0.3f, maxX - minX), 4f, Mathf.Max(0.3f, maxZ - minZ)));
        IssueOrder(area, _filter);
    }

    private void AssignAvailableResidents()
    {
        if (_stockpile == null || _stockpile.UsedCapacity >= _stockpile.StorageCapacity || _orderedNodes.Count == 0)
        {
            return;
        }

        foreach (TownResident resident in TownResident.ActiveResidents)
        {
            if (resident == null || !resident.IsAvailable)
            {
                continue;
            }

            ResourceNode target = FindClosestUnassignedNode(resident.transform.position);
            if (target != null)
            {
                resident.TryAssignGathering(target, _stockpile);
            }
        }
    }

    private ResourceNode FindClosestUnassignedNode(Vector3 origin)
    {
        ResourceNode best = null;
        float bestDistance = float.MaxValue;
        foreach (ResourceNode node in _orderedNodes)
        {
            if (node == null || node.IsDepleted || IsAssigned(node))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(node.InteractionPoint - origin);
            if (distance < bestDistance)
            {
                best = node;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool IsAssigned(ResourceNode node)
    {
        foreach (TownResident resident in TownResident.ActiveResidents)
        {
            if (resident != null && resident.GatherTarget == node)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveInvalidOrders()
    {
        _orderedNodes.RemoveAll(node => node == null || node.IsDepleted);
    }

    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 groundPoint)
    {
        Ray ray = _worldCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<GroundSurface>() != null)
            {
                groundPoint = hit.point;
                return true;
            }
        }

        groundPoint = default;
        return false;
    }

    private void CreateSelectionVisual()
    {
        _selectionVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _selectionVisual.name = "Gathering Area";
        _selectionVisual.transform.SetParent(transform, false);
        Collider collider = _selectionVisual.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Material material = new(Shader.Find("Standard"))
        {
            color = new Color(0.35f, 0.78f, 0.34f, 0.24f),
            hideFlags = HideFlags.DontSave
        };
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        _selectionVisual.GetComponent<Renderer>().sharedMaterial = material;
        _selectionVisual.SetActive(false);
    }

    private void UpdateSelectionVisual()
    {
        if (_selectionVisual == null)
        {
            return;
        }

        Vector3 center = (_dragStart + _dragEnd) * 0.5f + Vector3.up * 0.08f;
        _selectionVisual.transform.position = center;
        _selectionVisual.transform.localScale = new Vector3(
            Mathf.Max(0.3f, Mathf.Abs(_dragEnd.x - _dragStart.x)),
            0.05f,
            Mathf.Max(0.3f, Mathf.Abs(_dragEnd.z - _dragStart.z)));
        _selectionVisual.SetActive(true);
    }

    private void OnDisable()
    {
        IsAnyGatheringActive = false;
    }
}
}
