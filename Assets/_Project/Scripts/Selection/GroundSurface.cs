using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Selection
{

public sealed class GroundSurface : MonoBehaviour
{
    private const float RayPadding = 4f;
    private static readonly List<GroundSurface> ActiveSurfaces = new();

    private Collider[] _colliders;

    public static bool TryProjectPoint(
        Vector3 worldPoint,
        out Vector3 surfacePoint,
        float verticalOffset = 0f)
    {
        return TryProjectPoint(
            worldPoint,
            out surfacePoint,
            out _,
            verticalOffset);
    }

    public static bool TryProjectPoint(
        Vector3 worldPoint,
        out Vector3 surfacePoint,
        out Vector3 surfaceNormal,
        float verticalOffset = 0f)
    {
        RefreshActiveSurfacesIfNeeded();
        bool found = false;
        RaycastHit bestHit = default;

        foreach (GroundSurface surface in ActiveSurfaces)
        {
            if (surface == null || !surface.isActiveAndEnabled ||
                !surface.TryProject(worldPoint, out RaycastHit hit))
            {
                continue;
            }

            if (!found || hit.point.y > bestHit.point.y)
            {
                bestHit = hit;
                found = true;
            }
        }

        if (!found)
        {
            surfacePoint = default;
            surfaceNormal = Vector3.up;
            return false;
        }

        surfacePoint = bestHit.point + Vector3.up * verticalOffset;
        surfaceNormal = bestHit.normal;
        return true;
    }

    public static bool TryProjectFootprint(
        Vector3 center,
        Quaternion rotation,
        Vector2 halfExtents,
        float maximumHeightDifference,
        out Vector3 projectedCenter)
    {
        Vector3 right = rotation * Vector3.right * halfExtents.x;
        Vector3 forward = rotation * Vector3.forward * halfExtents.y;
        Vector3[] samples =
        {
            center,
            center - right - forward,
            center - right + forward,
            center + right - forward,
            center + right + forward
        };

        float minimumHeight = float.PositiveInfinity;
        float maximumHeight = float.NegativeInfinity;
        float heightSum = 0f;
        foreach (Vector3 sample in samples)
        {
            if (!TryProjectPoint(sample, out Vector3 projected, out Vector3 normal) ||
                Vector3.Angle(normal, Vector3.up) > 24f)
            {
                projectedCenter = default;
                return false;
            }

            minimumHeight = Mathf.Min(minimumHeight, projected.y);
            maximumHeight = Mathf.Max(maximumHeight, projected.y);
            heightSum += projected.y;
        }

        if (maximumHeight - minimumHeight > Mathf.Max(0f, maximumHeightDifference))
        {
            projectedCenter = default;
            return false;
        }

        projectedCenter = center;
        projectedCenter.y = heightSum / samples.Length;
        return true;
    }

    public bool TryProject(Vector3 worldPoint, out RaycastHit bestHit)
    {
        EnsureColliders();
        bool found = false;
        bestHit = default;

        foreach (Collider surfaceCollider in _colliders)
        {
            if (surfaceCollider == null || !surfaceCollider.enabled ||
                !surfaceCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = surfaceCollider.bounds;
            if (worldPoint.x < bounds.min.x || worldPoint.x > bounds.max.x ||
                worldPoint.z < bounds.min.z || worldPoint.z > bounds.max.z)
            {
                continue;
            }

            float originY = Mathf.Max(worldPoint.y + RayPadding, bounds.max.y + RayPadding);
            Ray ray = new(new Vector3(worldPoint.x, originY, worldPoint.z), Vector3.down);
            float distance = originY - bounds.min.y + RayPadding;
            if (!surfaceCollider.Raycast(ray, out RaycastHit hit, distance))
            {
                continue;
            }

            if (!found || hit.point.y > bestHit.point.y)
            {
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private void OnEnable()
    {
        EnsureColliders();
        if (!ActiveSurfaces.Contains(this))
        {
            ActiveSurfaces.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveSurfaces.Remove(this);
    }

    private void OnTransformChildrenChanged()
    {
        _colliders = null;
    }

    private void EnsureColliders()
    {
        _colliders ??= GetComponentsInChildren<Collider>(true);
    }

    private static void RefreshActiveSurfacesIfNeeded()
    {
        ActiveSurfaces.RemoveAll(surface => surface == null);
        if (ActiveSurfaces.Count > 0)
        {
            return;
        }

        foreach (GroundSurface surface in
                 FindObjectsByType<GroundSurface>(FindObjectsSortMode.None))
        {
            if (surface.isActiveAndEnabled && !ActiveSurfaces.Contains(surface))
            {
                ActiveSurfaces.Add(surface);
            }
        }
    }
}
}
