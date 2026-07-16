using System.Collections.Generic;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class RoadNetwork
{
    private readonly List<RoadSegment> _segments = new();

    public int SegmentCount => _segments.Count;

    public void RegisterPolyline(IReadOnlyList<Vector3> points, float width, float speedMultiplier = 1.5f)
    {
        if (points == null)
        {
            return;
        }

        for (int index = 0; index < points.Count - 1; index++)
        {
            RegisterSegment(points[index], points[index + 1], width, speedMultiplier);
        }
    }

    public bool RegisterSegment(Vector3 start, Vector3 end, float width, float speedMultiplier = 1.5f)
    {
        start.y = 0f;
        end.y = 0f;
        if ((end - start).sqrMagnitude < 0.25f || width <= 0f)
        {
            return false;
        }

        _segments.Add(new RoadSegment(start, end, width, speedMultiplier));
        return true;
    }

    public bool IsOnRoad(Vector3 position, float extraWidth = 0f)
    {
        Vector2 point = new(position.x, position.z);
        foreach (RoadSegment segment in _segments)
        {
            float allowedDistance = segment.Width * 0.5f + Mathf.Max(0f, extraWidth);
            if (DistanceToSegment(point, segment.Start, segment.End) <= allowedDistance)
            {
                return true;
            }
        }

        return false;
    }

    public float GetSpeedMultiplierAt(Vector3 position, float extraWidth = 0f)
    {
        Vector2 point = new(position.x, position.z);
        float multiplier = 1f;
        foreach (RoadSegment segment in _segments)
        {
            float allowedDistance = segment.Width * 0.5f + Mathf.Max(0f, extraWidth);
            if (DistanceToSegment(point, segment.Start, segment.End) <= allowedDistance)
            {
                multiplier = Mathf.Max(multiplier, segment.SpeedMultiplier);
            }
        }

        return multiplier;
    }

    public bool OverlapsArea(Vector3 position, float radius)
    {
        Vector2 point = new(position.x, position.z);
        foreach (RoadSegment segment in _segments)
        {
            if (DistanceToSegment(point, segment.Start, segment.End) <=
                Mathf.Max(0f, radius) + segment.Width * 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryGetNearestPoint(Vector3 position, float maximumDistance, out Vector3 snappedPoint)
    {
        return TryGetNearestPoint(
            position,
            maximumDistance,
            new Vector3(float.PositiveInfinity, 0f, float.PositiveInfinity),
            0f,
            out snappedPoint);
    }

    public bool TryGetNearestPoint(
        Vector3 position,
        float maximumDistance,
        Vector3 excludedPoint,
        float exclusionRadius,
        out Vector3 snappedPoint)
    {
        Vector2 point = new(position.x, position.z);
        Vector2 excluded = new(excludedPoint.x, excludedPoint.z);
        float bestDistance = Mathf.Max(0f, maximumDistance);
        bool found = false;
        snappedPoint = position;

        foreach (RoadSegment segment in _segments)
        {
            Vector2 candidate = ClosestPointOnSegment(point, segment.Start, segment.End);
            if (exclusionRadius > 0f && Vector2.Distance(candidate, excluded) < exclusionRadius)
            {
                continue;
            }

            float distance = Vector2.Distance(point, candidate);
            if (distance > bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            snappedPoint = new Vector3(candidate.x, 0f, candidate.y);
            found = true;
        }

        return found;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        return Vector2.Distance(point, ClosestPointOnSegment(point, start, end));
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float denominator = segment.sqrMagnitude;
        float progress = denominator <= 0.0001f
            ? 0f
            : Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);
        return start + segment * progress;
    }

    private readonly struct RoadSegment
    {
        public RoadSegment(Vector3 start, Vector3 end, float width, float speedMultiplier)
        {
            Start = new Vector2(start.x, start.z);
            End = new Vector2(end.x, end.z);
            Width = width;
            SpeedMultiplier = Mathf.Max(1f, speedMultiplier);
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Width { get; }
        public float SpeedMultiplier { get; }
    }
}
}
