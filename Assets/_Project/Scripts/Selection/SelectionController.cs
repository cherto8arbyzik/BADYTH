using System.Collections.Generic;
using Hollowwest.Core;
using Hollowwest.Navigation;
using Hollowwest.Presentation;
using UnityEngine;

namespace Hollowwest.Selection;

public sealed class SelectionController : MonoBehaviour
{
    private const float ClickThreshold = 7f;

    private readonly List<SelectableUnit> _selected = new();
    private readonly List<Vector3> _formationSlots = new();
    private readonly List<Vector3> _unitPositions = new();
    private readonly List<int> _slotAssignments = new();

    private Camera _camera;
    private Vector2 _dragStart;
    private bool _dragging;
    private GUIStyle _selectionBoxStyle;
    private Material _commandMarkerMaterial;

    public int SelectedCount => _selected.Count;

    public void Initialize(Camera worldCamera, Material commandMarkerMaterial)
    {
        _camera = worldCamera;
        _commandMarkerMaterial = commandMarkerMaterial;
    }

    private void Update()
    {
        if (_camera == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }

        if (Input.GetMouseButtonDown(0))
        {
            _dragStart = Input.mousePosition;
            _dragging = true;
        }

        if (_dragging && Input.GetMouseButtonUp(0))
        {
            Vector2 dragEnd = Input.mousePosition;
            bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if ((dragEnd - _dragStart).sqrMagnitude <= ClickThreshold * ClickThreshold)
            {
                SelectSingle(dragEnd, additive);
            }
            else
            {
                SelectBox(_dragStart, dragEnd, additive);
            }

            _dragging = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            IssueMoveCommand(Input.mousePosition);
        }
    }

    private void SelectSingle(Vector2 screenPosition, bool additive)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        SelectableUnit unit = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            unit = hit.collider.GetComponentInParent<SelectableUnit>();
        }

        if (!additive)
        {
            ClearSelection();
        }

        if (unit != null)
        {
            AddSelection(unit);
        }
    }

    private void SelectBox(Vector2 start, Vector2 end, bool additive)
    {
        if (!additive)
        {
            ClearSelection();
        }

        Rect rectangle = Rect.MinMaxRect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Max(start.x, end.x),
            Mathf.Max(start.y, end.y));

        foreach (SelectableUnit unit in SelectableUnit.ActiveUnits)
        {
            if (unit == null)
            {
                continue;
            }

            Vector3 screenPoint = _camera.WorldToScreenPoint(unit.transform.position);
            if (screenPoint.z > 0f && rectangle.Contains(screenPoint))
            {
                AddSelection(unit);
            }
        }
    }

    private void IssueMoveCommand(Vector2 screenPosition)
    {
        if (_selected.Count == 0)
        {
            return;
        }

        Ray ray = _camera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f);
        Vector3 destination = default;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<GroundSurface>() == null)
            {
                continue;
            }

            destination = hit.point;
            foundGround = true;
            break;
        }

        if (!foundGround)
        {
            return;
        }

        FormationPlanner.BuildCenteredGrid(destination, _selected.Count, 1.35f, _formationSlots);
        _unitPositions.Clear();

        foreach (SelectableUnit unit in _selected)
        {
            _unitPositions.Add(unit.transform.position);
        }

        FormationPlanner.AssignNearestSlots(_unitPositions, _formationSlots, _slotAssignments);

        for (int unitIndex = 0; unitIndex < _selected.Count; unitIndex++)
        {
            int slotIndex = _slotAssignments[unitIndex];
            NavigationAgent agent = _selected[unitIndex].GetComponent<NavigationAgent>();

            if (slotIndex >= 0 && agent != null)
            {
                agent.SetDestination(_formationSlots[slotIndex]);
            }
        }

        CommandMarker.Spawn(destination, _commandMarkerMaterial);
    }

    private void AddSelection(SelectableUnit unit)
    {
        if (_selected.Contains(unit))
        {
            return;
        }

        _selected.Add(unit);
        unit.SetSelected(true);
    }

    private void ClearSelection()
    {
        foreach (SelectableUnit unit in _selected)
        {
            if (unit != null)
            {
                unit.SetSelected(false);
            }
        }

        _selected.Clear();
    }

    private void OnGUI()
    {
        if (!_dragging)
        {
            return;
        }

        EnsureSelectionBoxStyle();
        Vector2 current = Input.mousePosition;
        Vector2 guiStart = new(_dragStart.x, Screen.height - _dragStart.y);
        Vector2 guiCurrent = new(current.x, Screen.height - current.y);

        Rect rectangle = Rect.MinMaxRect(
            Mathf.Min(guiStart.x, guiCurrent.x),
            Mathf.Min(guiStart.y, guiCurrent.y),
            Mathf.Max(guiStart.x, guiCurrent.x),
            Mathf.Max(guiStart.y, guiCurrent.y));

        GUI.Box(rectangle, GUIContent.none, _selectionBoxStyle);
    }

    private void EnsureSelectionBoxStyle()
    {
        if (_selectionBoxStyle != null)
        {
            return;
        }

        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, new Color(0.2f, 0.85f, 0.45f, 0.22f));
        texture.Apply();
        _selectionBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = texture }
        };
    }
}
