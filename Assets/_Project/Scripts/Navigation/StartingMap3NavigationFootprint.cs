using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Navigation
{

public sealed class StartingMap3NavigationFootprint
{
    public const string ResourcePath =
        "Models/Custom/Environment/StartingIsland/Map3/Collision/StartingMap3_NavigationFootprint";

    private readonly Vector2[] _points;

    private StartingMap3NavigationFootprint(Vector2[] points)
    {
        _points = points;
    }

    public int PointCount => _points.Length;

    public static bool TryLoad(out StartingMap3NavigationFootprint footprint)
    {
        footprint = null;
        TextAsset source = Resources.Load<TextAsset>(ResourcePath);
        if (source == null)
        {
            return false;
        }

        FootprintDocument document = JsonUtility.FromJson<FootprintDocument>(source.text);
        PointData[] sourcePoints = document?.footprint?.unityLocalPointsMetres;
        if (sourcePoints == null || sourcePoints.Length < 3)
        {
            return false;
        }

        Vector2[] points = new Vector2[sourcePoints.Length];
        for (int index = 0; index < sourcePoints.Length; index++)
        {
            points[index] = new Vector2(sourcePoints[index].x, sourcePoints[index].z);
        }

        footprint = new StartingMap3NavigationFootprint(points);
        return true;
    }

    public bool Contains(Vector3 worldPoint, float edgeClearance = 0f)
    {
        return ContainsPolygon(
            _points,
            new Vector2(worldPoint.x, worldPoint.z),
            edgeClearance);
    }

    public static bool ContainsPolygon(
        IReadOnlyList<Vector2> points,
        Vector2 point,
        float edgeClearance = 0f)
    {
        if (points == null || points.Count < 3)
        {
            return false;
        }

        bool inside = false;
        float minimumDistanceSquared = float.PositiveInfinity;
        for (int current = 0, previous = points.Count - 1;
             current < points.Count;
             previous = current++)
        {
            Vector2 start = points[previous];
            Vector2 end = points[current];
            bool crosses = (start.y > point.y) != (end.y > point.y) &&
                           point.x < (end.x - start.x) * (point.y - start.y) /
                           (end.y - start.y) + start.x;
            if (crosses)
            {
                inside = !inside;
            }

            minimumDistanceSquared = Mathf.Min(
                minimumDistanceSquared,
                DistanceToSegmentSquared(point, start, end));
        }

        float clearance = Mathf.Max(0f, edgeClearance);
        return inside && minimumDistanceSquared >= clearance * clearance;
    }

    private static float DistanceToSegmentSquared(
        Vector2 point,
        Vector2 start,
        Vector2 end)
    {
        Vector2 segment = end - start;
        float denominator = segment.sqrMagnitude;
        float progress = denominator <= 0.0001f
            ? 0f
            : Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);
        return (point - (start + segment * progress)).sqrMagnitude;
    }

    [Serializable]
    private sealed class FootprintDocument
    {
        public FootprintData footprint;
    }

    [Serializable]
    private sealed class FootprintData
    {
        public PointData[] unityLocalPointsMetres;
    }

    [Serializable]
    private sealed class PointData
    {
        public float x;
        public float y;
        public float z;
    }
}
}
