using Hollowwest.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class StartingMap3NavigationFootprintTests
{
    private static readonly Vector2[] Square =
    {
        new(-5f, -5f),
        new(5f, -5f),
        new(5f, 5f),
        new(-5f, 5f)
    };

    [Test]
    public void ContainsPolygon_HonoursInsideOutsideAndEdgeClearance()
    {
        Assert.That(
            StartingMap3NavigationFootprint.ContainsPolygon(Square, Vector2.zero),
            Is.True);
        Assert.That(
            StartingMap3NavigationFootprint.ContainsPolygon(Square, new Vector2(6f, 0f)),
            Is.False);
        Assert.That(
            StartingMap3NavigationFootprint.ContainsPolygon(
                Square,
                new Vector2(4.6f, 0f),
                0.8f),
            Is.False);
    }

    [Test]
    public void Map3Footprint_LoadsAuthoredRuntimePolygon()
    {
        Assert.That(
            StartingMap3NavigationFootprint.TryLoad(out StartingMap3NavigationFootprint footprint),
            Is.True);
        Assert.That(footprint.PointCount, Is.EqualTo(128));
        Assert.That(footprint.Contains(Vector3.zero, 0.8f), Is.True);
        Assert.That(footprint.Contains(new Vector3(150f, 0f, 0f)), Is.False);
    }
}
}
