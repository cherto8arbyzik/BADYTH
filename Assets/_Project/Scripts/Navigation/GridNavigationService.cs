using System;
using System.Collections.Generic;
using Hollowwest.Core;
using UnityEngine;

namespace Hollowwest.Navigation
{

public sealed class GridNavigationService : INavigationService
{
    private static readonly Vector2Int[] Directions =
    {
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1),
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1)
    };

    private readonly Vector3 _origin;
    private readonly int _width;
    private readonly int _depth;
    private readonly float _cellSize;
    private readonly bool[,] _blocked;
    private readonly Func<Vector3, Vector3> _surfaceProjector;

    public GridNavigationService(
        Vector3 origin,
        int width,
        int depth,
        float cellSize,
        Func<Vector3, Vector3> surfaceProjector = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (depth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }

        if (cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize));
        }

        _origin = origin;
        _width = width;
        _depth = depth;
        _cellSize = cellSize;
        _blocked = new bool[width, depth];
        _surfaceProjector = surfaceProjector;
    }

    public int Width => _width;
    public int Depth => _depth;

    public Vector2Int WorldToCell(Vector3 world)
    {
        int x = Mathf.FloorToInt((world.x - _origin.x) / _cellSize);
        int y = Mathf.FloorToInt((world.z - _origin.z) / _cellSize);
        return new Vector2Int(x, y);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        Vector3 world = CellToWorldUnprojected(cell);
        return _surfaceProjector == null ? world : _surfaceProjector(world);
    }

    public Vector3 CellToWorldUnprojected(Vector2Int cell)
    {
        return _origin + new Vector3(
            (cell.x + 0.5f) * _cellSize,
            0f,
            (cell.y + 0.5f) * _cellSize);
    }

    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _depth;
    }

    public bool IsBlocked(Vector2Int cell)
    {
        return !IsInside(cell) || _blocked[cell.x, cell.y];
    }

    public void SetCellBlocked(Vector2Int cell, bool blocked = true)
    {
        if (!IsInside(cell))
        {
            throw new ArgumentOutOfRangeException(nameof(cell));
        }

        _blocked[cell.x, cell.y] = blocked;
    }

    public void SetBlocked(Bounds worldBounds, float clearance = 0f)
    {
        SetBlocked(worldBounds, clearance, true, null);
    }

    public IReadOnlyList<Vector2Int> ReserveBlocked(Bounds worldBounds, float clearance = 0f)
    {
        List<Vector2Int> reservation = new();
        SetBlocked(worldBounds, clearance, true, reservation);
        return reservation;
    }

    public void ReleaseBlocked(IReadOnlyList<Vector2Int> reservation)
    {
        if (reservation == null)
        {
            return;
        }

        foreach (Vector2Int cell in reservation)
        {
            if (IsInside(cell))
            {
                _blocked[cell.x, cell.y] = false;
            }
        }
    }

    public bool TryFindPath(Vector3 start, Vector3 destination, List<Vector3> output)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        output.Clear();

        if (!TryFindNearestWalkable(WorldToCell(start), out Vector2Int startCell) ||
            !TryFindNearestWalkable(WorldToCell(destination), out Vector2Int destinationCell))
        {
            return false;
        }

        if (startCell == destinationCell)
        {
            output.Add(CellToWorld(destinationCell));
            return true;
        }

        float[,] scores = new float[_width, _depth];
        bool[,] closed = new bool[_width, _depth];
        bool[,] hasParent = new bool[_width, _depth];
        Vector2Int[,] parents = new Vector2Int[_width, _depth];

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _depth; y++)
            {
                scores[x, y] = float.PositiveInfinity;
            }
        }

        MinHeap frontier = new();
        scores[startCell.x, startCell.y] = 0f;
        frontier.Push(startCell, Heuristic(startCell, destinationCell));

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Pop();
            if (closed[current.x, current.y])
            {
                continue;
            }

            if (current == destinationCell)
            {
                BuildWorldPath(startCell, destinationCell, parents, hasParent, output);
                return output.Count > 0;
            }

            closed[current.x, current.y] = true;

            foreach (Vector2Int direction in Directions)
            {
                Vector2Int neighbour = current + direction;
                if (IsBlocked(neighbour) || closed[neighbour.x, neighbour.y])
                {
                    continue;
                }

                bool diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal &&
                    (IsBlocked(current + new Vector2Int(direction.x, 0)) ||
                     IsBlocked(current + new Vector2Int(0, direction.y))))
                {
                    continue;
                }

                float moveCost = diagonal ? 1.4142135f : 1f;
                float tentativeScore = scores[current.x, current.y] + moveCost;

                if (tentativeScore >= scores[neighbour.x, neighbour.y])
                {
                    continue;
                }

                scores[neighbour.x, neighbour.y] = tentativeScore;
                parents[neighbour.x, neighbour.y] = current;
                hasParent[neighbour.x, neighbour.y] = true;
                frontier.Push(neighbour, tentativeScore + Heuristic(neighbour, destinationCell));
            }
        }

        return false;
    }

    private bool TryFindNearestWalkable(Vector2Int requested, out Vector2Int result)
    {
        requested = new Vector2Int(
            Mathf.Clamp(requested.x, 0, _width - 1),
            Mathf.Clamp(requested.y, 0, _depth - 1));

        if (!IsBlocked(requested))
        {
            result = requested;
            return true;
        }

        int maximumRadius = Mathf.Max(_width, _depth);
        for (int radius = 1; radius < maximumRadius; radius++)
        {
            Vector2Int best = default;
            float bestDistance = float.PositiveInfinity;

            for (int x = requested.x - radius; x <= requested.x + radius; x++)
            {
                for (int y = requested.y - radius; y <= requested.y + radius; y++)
                {
                    bool onEdge = x == requested.x - radius || x == requested.x + radius ||
                                  y == requested.y - radius || y == requested.y + radius;
                    if (!onEdge)
                    {
                        continue;
                    }

                    Vector2Int candidate = new(x, y);
                    if (IsBlocked(candidate))
                    {
                        continue;
                    }

                    float distance = (candidate - requested).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }
            }

            if (!float.IsPositiveInfinity(bestDistance))
            {
                result = best;
                return true;
            }
        }

        result = default;
        return false;
    }

    private void SetBlocked(
        Bounds worldBounds,
        float clearance,
        bool blocked,
        List<Vector2Int> changedCells)
    {
        Bounds expanded = worldBounds;
        expanded.Expand(new Vector3(clearance * 2f, 0f, clearance * 2f));

        Vector2Int minimum = WorldToCell(expanded.min);
        Vector2Int maximum = WorldToCell(expanded.max);

        int minX = Mathf.Clamp(minimum.x, 0, _width - 1);
        int maxX = Mathf.Clamp(maximum.x, 0, _width - 1);
        int minY = Mathf.Clamp(minimum.y, 0, _depth - 1);
        int maxY = Mathf.Clamp(maximum.y, 0, _depth - 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (_blocked[x, y] == blocked)
                {
                    continue;
                }

                _blocked[x, y] = blocked;
                changedCells?.Add(new Vector2Int(x, y));
            }
        }
    }

    private void BuildWorldPath(
        Vector2Int start,
        Vector2Int destination,
        Vector2Int[,] parents,
        bool[,] hasParent,
        List<Vector3> output)
    {
        List<Vector2Int> reversed = new();
        Vector2Int current = destination;

        while (current != start)
        {
            reversed.Add(current);
            if (!hasParent[current.x, current.y])
            {
                output.Clear();
                return;
            }

            current = parents[current.x, current.y];
        }

        for (int index = reversed.Count - 1; index >= 0; index--)
        {
            output.Add(CellToWorld(reversed[index]));
        }
    }

    private static float Heuristic(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        int diagonal = Mathf.Min(dx, dy);
        int straight = Mathf.Max(dx, dy) - diagonal;
        return diagonal * 1.4142135f + straight;
    }

    private readonly struct OpenNode
    {
        public OpenNode(Vector2Int cell, float priority)
        {
            Cell = cell;
            Priority = priority;
        }

        public Vector2Int Cell { get; }
        public float Priority { get; }
    }

    private sealed class MinHeap
    {
        private readonly List<OpenNode> _items = new();

        public int Count => _items.Count;

        public void Push(Vector2Int cell, float priority)
        {
            _items.Add(new OpenNode(cell, priority));
            int index = _items.Count - 1;

            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_items[parent].Priority <= _items[index].Priority)
                {
                    break;
                }

                (_items[parent], _items[index]) = (_items[index], _items[parent]);
                index = parent;
            }
        }

        public Vector2Int Pop()
        {
            OpenNode root = _items[0];
            int lastIndex = _items.Count - 1;
            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);

            int index = 0;
            while (index < _items.Count)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                int smallest = index;

                if (left < _items.Count && _items[left].Priority < _items[smallest].Priority)
                {
                    smallest = left;
                }

                if (right < _items.Count && _items[right].Priority < _items[smallest].Priority)
                {
                    smallest = right;
                }

                if (smallest == index)
                {
                    break;
                }

                (_items[index], _items[smallest]) = (_items[smallest], _items[index]);
                index = smallest;
            }

            return root.Cell;
        }
    }
}
}
