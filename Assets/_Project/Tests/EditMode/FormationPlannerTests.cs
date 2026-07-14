using System.Collections.Generic;
using Hollowwest.Core;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class FormationPlannerTests
{
    [Test]
    public void CenteredGridProducesRequestedCountAroundCenter()
    {
        Vector3 center = new(8f, 0f, -3f);
        List<Vector3> slots = new();

        FormationPlanner.BuildCenteredGrid(center, 6, 1.5f, slots);

        Assert.That(slots, Has.Count.EqualTo(6));

        Vector3 average = Vector3.zero;
        foreach (Vector3 slot in slots)
        {
            average += slot;
        }

        average /= slots.Count;
        Assert.That(average.x, Is.EqualTo(center.x).Within(0.001f));
        Assert.That(average.z, Is.EqualTo(center.z).Within(0.001f));
    }

    [Test]
    public void AssignmentUsesEverySlotAtMostOnce()
    {
        List<Vector3> units = new()
        {
            new Vector3(-3f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(3f, 0f, 0f)
        };

        List<Vector3> slots = new()
        {
            new Vector3(3f, 0f, 8f),
            new Vector3(1f, 0f, 8f),
            new Vector3(-1f, 0f, 8f),
            new Vector3(-3f, 0f, 8f)
        };

        List<int> assignments = new();
        FormationPlanner.AssignNearestSlots(units, slots, assignments);

        Assert.That(assignments, Has.Count.EqualTo(units.Count));
        Assert.That(new HashSet<int>(assignments), Has.Count.EqualTo(slots.Count));
        Assert.That(assignments, Has.All.GreaterThanOrEqualTo(0));
    }
}
}
