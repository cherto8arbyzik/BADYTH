using System.Collections.Generic;
using Hollowwest.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class GridNavigationServiceTests
{
    [Test]
    public void PathDetoursThroughGapAndNeverUsesBlockedCells()
    {
        GridNavigationService grid = new(Vector3.zero, 12, 12, 1f);

        for (int y = 0; y < 11; y++)
        {
            grid.SetCellBlocked(new Vector2Int(5, y));
        }

        List<Vector3> path = new();
        bool found = grid.TryFindPath(
            grid.CellToWorld(new Vector2Int(1, 5)),
            grid.CellToWorld(new Vector2Int(10, 5)),
            path);

        Assert.That(found, Is.True);
        Assert.That(path, Is.Not.Empty);
        Assert.That(path.Exists(point => grid.WorldToCell(point).y == 11), Is.True);

        foreach (Vector3 waypoint in path)
        {
            Assert.That(grid.IsBlocked(grid.WorldToCell(waypoint)), Is.False);
        }
    }

    [Test]
    public void SealedWallReturnsNoPath()
    {
        GridNavigationService grid = new(Vector3.zero, 10, 10, 1f);

        for (int y = 0; y < 10; y++)
        {
            grid.SetCellBlocked(new Vector2Int(5, y));
        }

        List<Vector3> path = new();
        bool found = grid.TryFindPath(
            grid.CellToWorld(new Vector2Int(1, 5)),
            grid.CellToWorld(new Vector2Int(8, 5)),
            path);

        Assert.That(found, Is.False);
        Assert.That(path, Is.Empty);
    }

    [Test]
    public void DiagonalMoveCannotCutBlockedCorner()
    {
        GridNavigationService grid = new(Vector3.zero, 3, 3, 1f);
        grid.SetCellBlocked(new Vector2Int(1, 0));
        grid.SetCellBlocked(new Vector2Int(0, 1));

        List<Vector3> path = new();
        bool found = grid.TryFindPath(
            grid.CellToWorld(new Vector2Int(0, 0)),
            grid.CellToWorld(new Vector2Int(1, 1)),
            path);

        Assert.That(found, Is.False);
    }

    [Test]
    public void ReservationRelease_PreservesCellsBlockedBeforeReservation()
    {
        GridNavigationService grid = new(Vector3.zero, 10, 10, 1f);
        Vector2Int permanentObstacle = new(3, 3);
        Vector2Int constructionCell = new(2, 2);
        grid.SetCellBlocked(permanentObstacle);

        IReadOnlyList<Vector2Int> reservation = grid.ReserveBlocked(
            new Bounds(new Vector3(3.5f, 0f, 3.5f), new Vector3(3f, 1f, 3f)));
        bool containsPermanentObstacle = false;
        foreach (Vector2Int cell in reservation)
        {
            containsPermanentObstacle |= cell == permanentObstacle;
        }

        Assert.That(grid.IsBlocked(permanentObstacle), Is.True);
        Assert.That(grid.IsBlocked(constructionCell), Is.True);
        Assert.That(containsPermanentObstacle, Is.False);

        grid.ReleaseBlocked(reservation);

        Assert.That(grid.IsBlocked(permanentObstacle), Is.True);
        Assert.That(grid.IsBlocked(constructionCell), Is.False);
    }
}
}
