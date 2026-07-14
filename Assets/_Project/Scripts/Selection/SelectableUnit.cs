using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Selection;

public sealed class SelectableUnit : MonoBehaviour
{
    private static readonly List<SelectableUnit> ActiveUnitsInternal = new();

    private GameObject _selectionIndicator;

    public static IReadOnlyList<SelectableUnit> ActiveUnits => ActiveUnitsInternal;
    public bool IsSelected { get; private set; }

    public void Initialize(Material indicatorMaterial)
    {
        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _selectionIndicator.name = "SelectionIndicator";
        _selectionIndicator.transform.SetParent(transform, false);
        _selectionIndicator.transform.localPosition = new Vector3(0f, -0.98f, 0f);
        _selectionIndicator.transform.localScale = new Vector3(1.3f, 0.025f, 1.3f);

        Collider indicatorCollider = _selectionIndicator.GetComponent<Collider>();
        indicatorCollider.enabled = false;
        _selectionIndicator.GetComponent<Renderer>().sharedMaterial = indicatorMaterial;
        _selectionIndicator.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        if (_selectionIndicator != null)
        {
            _selectionIndicator.SetActive(selected);
        }
    }

    private void OnEnable()
    {
        if (!ActiveUnitsInternal.Contains(this))
        {
            ActiveUnitsInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveUnitsInternal.Remove(this);
    }
}
