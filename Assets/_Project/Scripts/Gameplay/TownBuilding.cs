using System.Collections.Generic;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class TownBuilding : MonoBehaviour
{
    private readonly List<Material> _visualMaterials = new();
    private readonly List<Color> _restoredColors = new();

    private GameObject _damageVisual;
    private GameObject _selectionIndicator;

    public string DisplayName { get; private set; }
    public int RestorationCost { get; private set; }
    public bool IsRuined { get; private set; }
    public bool IsSelected { get; private set; }

    public void Initialize(
        string displayName,
        int restorationCost,
        bool isRuined,
        Renderer[] renderers,
        GameObject damageVisual,
        Bounds worldBounds)
    {
        DisplayName = displayName;
        RestorationCost = Mathf.Max(0, restorationCost);
        IsRuined = isRuined;
        _damageVisual = damageVisual;

        CaptureMaterials(renderers);
        CreateSelectionIndicator(worldBounds);

        if (IsRuined)
        {
            ApplyRuinedTint();
        }
        else if (_damageVisual != null)
        {
            _damageVisual.SetActive(false);
        }
    }

    public bool TryRestore(ResourceStockpile stockpile)
    {
        if (!IsRuined || stockpile == null || !stockpile.TrySpendWood(RestorationCost))
        {
            return false;
        }

        IsRuined = false;
        RestoreMaterialColors();

        if (_damageVisual != null)
        {
            _damageVisual.SetActive(false);
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

            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                material.hideFlags = HideFlags.DontSave;
                _visualMaterials.Add(material);
                _restoredColors.Add(material.color);
            }
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
}
}
