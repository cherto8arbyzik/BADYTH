using System;
using System.Linq;
using UnityEngine;

namespace Hollowwest.Selection
{

public static class StartingMap3GroundFactory
{
    public const string ModelPath =
        "Models/Custom/Environment/StartingIsland/Map3/Collision/StartingMap3_Collision";

    public static bool TryCreate(Transform alignedParent, out GameObject root)
    {
        return TryCreate(
            alignedParent,
            path => Resources.Load<GameObject>(path),
            out root);
    }

    public static bool TryCreate(
        Transform alignedParent,
        Func<string, GameObject> modelLoader,
        out GameObject root)
    {
        root = null;
        if (alignedParent == null || modelLoader == null)
        {
            return false;
        }

        GameObject sourcePrefab = modelLoader(ModelPath);
        if (sourcePrefab == null)
        {
            return false;
        }

        GameObject groundRoot = new("Starting Map 3 Gameplay Ground");
        groundRoot.transform.SetParent(alignedParent, false);

        GameObject source = UnityEngine.Object.Instantiate(
            sourcePrefab,
            groundRoot.transform,
            false);
        source.name = "StartingMap3_Collision";

        foreach (Renderer renderer in source.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (Collider importedCollider in source.GetComponentsInChildren<Collider>(true))
        {
            importedCollider.enabled = false;
            DestroyObject(importedCollider);
        }

        MeshFilter[] meshFilters = source
            .GetComponentsInChildren<MeshFilter>(true)
            .Where(filter => filter.sharedMesh != null)
            .ToArray();
        if (meshFilters.Length == 0)
        {
            DestroyObject(groundRoot);
            return false;
        }

        foreach (MeshFilter meshFilter in meshFilters)
        {
            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;
            meshFilter.gameObject.isStatic = true;
        }

        groundRoot.isStatic = true;
        groundRoot.AddComponent<GroundSurface>();
        Physics.SyncTransforms();
        root = groundRoot;
        return true;
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(target);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
}
