using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class FertilityPatch : MonoBehaviour
{
    private static readonly List<FertilityPatch> ActivePatchesInternal = new();

    private Vector3 _center;
    private Vector2 _radii;
    private float _fertility;

    public float Fertility => _fertility;

    public void Configure(Vector3 center, Vector2 radii, float fertility)
    {
        _center = center;
        _radii = new Vector2(Mathf.Max(1f, radii.x), Mathf.Max(1f, radii.y));
        _fertility = Mathf.Clamp01(fertility);
    }

    public static float Sample(Vector3 position)
    {
        float weighted = 0.42f;
        float influence = 1f;
        foreach (FertilityPatch patch in ActivePatchesInternal)
        {
            if (patch == null)
            {
                continue;
            }

            float x = (position.x - patch._center.x) / patch._radii.x;
            float z = (position.z - patch._center.z) / patch._radii.y;
            float distance = Mathf.Sqrt(x * x + z * z);
            if (distance >= 1f)
            {
                continue;
            }

            float patchInfluence = 1f - Mathf.SmoothStep(0.35f, 1f, distance);
            weighted += patch._fertility * patchInfluence;
            influence += patchInfluence;
        }

        return Mathf.Clamp01(weighted / influence);
    }

    private void OnEnable()
    {
        if (!ActivePatchesInternal.Contains(this))
        {
            ActivePatchesInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActivePatchesInternal.Remove(this);
    }
}
}
