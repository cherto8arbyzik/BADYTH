using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class LakeShoreArea : MonoBehaviour
{
    private static readonly List<LakeShoreArea> ActiveAreasInternal = new();

    private Vector3 _center;
    private Vector2 _radii;

    public static IReadOnlyList<LakeShoreArea> ActiveAreas => ActiveAreasInternal;

    public void Configure(Vector3 center, Vector2 radii)
    {
        _center = center;
        _radii = new Vector2(Mathf.Max(1f, radii.x), Mathf.Max(1f, radii.y));
    }

    public bool IsNearShore(Vector3 position)
    {
        float normalizedX = (position.x - _center.x) / _radii.x;
        float normalizedZ = (position.z - _center.z) / _radii.y;
        float normalizedDistance = Mathf.Sqrt(normalizedX * normalizedX + normalizedZ * normalizedZ);
        return normalizedDistance >= 0.88f && normalizedDistance <= 1.24f;
    }

    public static bool IsAnyShoreNear(Vector3 position)
    {
        foreach (LakeShoreArea area in ActiveAreasInternal)
        {
            if (area != null && area.IsNearShore(position))
            {
                return true;
            }
        }

        return false;
    }

    private void OnEnable()
    {
        if (!ActiveAreasInternal.Contains(this))
        {
            ActiveAreasInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveAreasInternal.Remove(this);
    }
}
}
