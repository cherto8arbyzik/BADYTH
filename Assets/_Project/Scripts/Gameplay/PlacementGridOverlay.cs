using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class PlacementGridOverlay : MonoBehaviour
{
    private GameObject _gridRoot;
    private Mesh _mesh;
    private Material _material;
    private float _cellSize;

    public void Initialize(float cellSize, int halfCellCount)
    {
        _cellSize = Mathf.Max(0.25f, cellSize);
        int radius = Mathf.Max(2, halfCellCount);

        _gridRoot = new GameObject("Building Placement Grid");
        _gridRoot.transform.SetParent(transform, false);

        List<Vector3> vertices = new((radius * 2 + 1) * 4);
        float extent = radius * _cellSize;
        for (int index = -radius; index <= radius; index++)
        {
            float offset = index * _cellSize;
            vertices.Add(new Vector3(offset, 0f, -extent));
            vertices.Add(new Vector3(offset, 0f, extent));
            vertices.Add(new Vector3(-extent, 0f, offset));
            vertices.Add(new Vector3(extent, 0f, offset));
        }

        int[] indices = new int[vertices.Count];
        for (int index = 0; index < indices.Length; index++)
        {
            indices[index] = index;
        }

        _mesh = new Mesh
        {
            name = "Building Placement Grid Mesh",
            hideFlags = HideFlags.DontSave
        };
        _mesh.SetVertices(vertices);
        _mesh.SetIndices(indices, MeshTopology.Lines, 0);
        _mesh.RecalculateBounds();

        _material = new Material(Shader.Find("Standard"))
        {
            color = new Color(0.28f, 0.78f, 0.88f, 0.24f),
            hideFlags = HideFlags.DontSave,
            renderQueue = 3100
        };
        _material.SetFloat("_Mode", 3f);
        _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetInt("_ZWrite", 0);
        _material.EnableKeyword("_ALPHABLEND_ON");

        _gridRoot.AddComponent<MeshFilter>().sharedMesh = _mesh;
        MeshRenderer renderer = _gridRoot.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        _gridRoot.SetActive(false);
    }

    public void ShowAt(Vector3 worldPosition)
    {
        if (_gridRoot == null)
        {
            return;
        }

        Vector3 snapped = Snap(worldPosition, _cellSize);
        snapped.y = 0.095f;
        _gridRoot.transform.position = snapped;
        _gridRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_gridRoot != null)
        {
            _gridRoot.SetActive(false);
        }
    }

    public static Vector3 Snap(Vector3 position, float cellSize)
    {
        float safeCellSize = Mathf.Max(0.001f, cellSize);
        position.x = Mathf.Round(position.x / safeCellSize) * safeCellSize;
        position.z = Mathf.Round(position.z / safeCellSize) * safeCellSize;
        return position;
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }

        if (_mesh != null)
        {
            Destroy(_mesh);
        }
    }
}
}
