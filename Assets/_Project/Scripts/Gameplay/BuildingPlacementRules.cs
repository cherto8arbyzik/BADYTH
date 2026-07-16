using UnityEngine;

namespace Hollowwest.Gameplay
{

public static class BuildingPlacementRules
{
    public static bool IsInsideBounds(Bounds worldBounds, Vector3 position, float footprint)
    {
        float margin = Mathf.Max(1f, footprint * 0.5f);
        return position.x >= worldBounds.min.x + margin &&
               position.x <= worldBounds.max.x - margin &&
               position.z >= worldBounds.min.z + margin &&
               position.z <= worldBounds.max.z - margin;
    }

    public static bool OverlapsRoad(
        Vector3 position,
        float footprint,
        Vector3[] roadPoints,
        float roadHalfWidth)
    {
        if (roadPoints == null || roadPoints.Length < 2)
        {
            return false;
        }

        Vector2 point = new(position.x, position.z);
        float requiredDistance = roadHalfWidth + footprint * 0.42f;

        for (int index = 0; index < roadPoints.Length - 1; index++)
        {
            Vector2 start = new(roadPoints[index].x, roadPoints[index].z);
            Vector2 end = new(roadPoints[index + 1].x, roadPoints[index + 1].z);
            Vector2 segment = end - start;
            float denominator = segment.sqrMagnitude;
            float t = denominator <= 0.0001f
                ? 0f
                : Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);
            float distance = Vector2.Distance(point, start + segment * t);
            if (distance < requiredDistance)
            {
                return true;
            }
        }

        return false;
    }
}
}
