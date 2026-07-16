using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class PlacementGridOverlayTests
{
    [Test]
    public void Snap_RoundsHorizontalPositionToConfiguredGrid()
    {
        Vector3 snapped = PlacementGridOverlay.Snap(new Vector3(3.1f, 0.7f, -5.2f), 2f);

        Assert.That(snapped, Is.EqualTo(new Vector3(4f, 0.7f, -6f)));
    }
}
}
