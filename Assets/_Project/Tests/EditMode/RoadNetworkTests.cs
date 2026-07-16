using Hollowwest.Gameplay;
using Hollowwest.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class RoadNetworkTests
{
    [Test]
    public void RegisteredSegment_DetectsPointsOnlyInsideRoadWidth()
    {
        RoadNetwork network = new();

        Assert.That(network.RegisterSegment(Vector3.zero, new Vector3(10f, 0f, 0f), 3f), Is.True);
        Assert.That(network.IsOnRoad(new Vector3(5f, 0f, 1.4f)), Is.True);
        Assert.That(network.IsOnRoad(new Vector3(5f, 0f, 1.6f)), Is.False);
        Assert.That(network.IsOnRoad(new Vector3(12f, 0f, 0f)), Is.False);
    }

    [Test]
    public void OverlapsArea_AccountsForRoadAndObjectWidths()
    {
        RoadNetwork network = new();
        network.RegisterSegment(new Vector3(-10f, 0f, 0f), new Vector3(10f, 0f, 0f), 3.2f);

        Assert.That(network.OverlapsArea(new Vector3(0f, 0f, 4f), 2.5f), Is.True);
        Assert.That(network.OverlapsArea(new Vector3(0f, 0f, 5f), 2.5f), Is.False);
    }

    [Test]
    public void InvalidSegment_IsNotRegistered()
    {
        RoadNetwork network = new();

        Assert.That(network.RegisterSegment(Vector3.zero, new Vector3(0.2f, 0f, 0f), 3f), Is.False);
        Assert.That(network.SegmentCount, Is.Zero);
    }

    [Test]
    public void NavigationAgent_UsesConfiguredRoadSpeedMultiplier()
    {
        RoadNetwork network = new();
        network.RegisterSegment(Vector3.zero, new Vector3(10f, 0f, 0f), 3f);
        GameObject owner = new("Road Speed Agent Test");

        try
        {
            NavigationAgent agent = owner.AddComponent<NavigationAgent>();
            agent.ConfigureRoadMovement(network, 1.5f);

            owner.transform.position = new Vector3(5f, 0f, 0f);
            Assert.That(agent.CurrentSpeedMultiplier, Is.EqualTo(1.5f));

            owner.transform.position = new Vector3(5f, 0f, 5f);
            Assert.That(agent.CurrentSpeedMultiplier, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void UpgradedRoad_ProvidesItsOwnHigherSpeedMultiplier()
    {
        RoadNetwork network = new();
        network.RegisterSegment(Vector3.zero, new Vector3(10f, 0f, 0f), 3f, 2f);

        Assert.That(network.GetSpeedMultiplierAt(new Vector3(5f, 0f, 0f)), Is.EqualTo(2f));
        Assert.That(network.GetSpeedMultiplierAt(new Vector3(5f, 0f, 5f)), Is.EqualTo(1f));
    }

    [Test]
    public void NearestPoint_SnapsToMiddleOfCompletedRoad()
    {
        RoadNetwork network = new();
        network.RegisterSegment(Vector3.zero, new Vector3(10f, 0f, 0f), 3f);

        bool snapped = network.TryGetNearestPoint(new Vector3(6f, 0f, 1.8f), 2.5f, out Vector3 point);

        Assert.That(snapped, Is.True);
        Assert.That(point, Is.EqualTo(new Vector3(6f, 0f, 0f)));
        Assert.That(network.TryGetNearestPoint(new Vector3(6f, 0f, 3f), 2.5f, out _), Is.False);
    }

    [Test]
    public void NearestPoint_CanIgnoreCurrentRoadOrigin()
    {
        RoadNetwork network = new();
        network.RegisterSegment(Vector3.zero, new Vector3(-10f, 0f, 0f), 3f);

        bool snapped = network.TryGetNearestPoint(
            new Vector3(-0.5f, 0f, 0.4f),
            2.5f,
            Vector3.zero,
            2f,
            out _);

        Assert.That(snapped, Is.False);
    }
}
}
