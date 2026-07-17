using System.Collections.Generic;
using System.Linq;
using Hollowwest.Gameplay;
using Hollowwest.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class WaterObstacleAreaTests
{
    [Test]
    public void AuthoredWater_BlocksNavigationBuildingAndRoadQueriesWithoutRenderingGeometry()
    {
        GridNavigationService navigation = new(new Vector3(-40f, 0f, -40f), 100, 100, 1f);
        GameObject root = new("Water Obstacle Test");

        try
        {
            Vector3 center = new(8f, 0f, 12f);
            WaterObstacleArea area = WaterObstacleArea.Create(
                root.transform,
                "Test Pond",
                center,
                new Vector2(20f, 16f),
                navigation,
                0.8f);
            Physics.SyncTransforms();

            Assert.That(navigation.IsBlocked(navigation.WorldToCell(center)), Is.True);
            Assert.That(
                navigation.IsBlocked(navigation.WorldToCell(center + new Vector3(13f, 0f, 0f))),
                Is.False);
            Assert.That(area.GetComponent<Renderer>(), Is.Null);

            Collider waterCollider = area.GetComponent<BoxCollider>();
            Collider[] buildingOverlaps = Physics.OverlapBox(
                center + Vector3.up * 2.5f,
                new Vector3(2.6f, 2.5f, 2.2f),
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);
            Collider[] roadOverlaps = Physics.OverlapSphere(
                center + Vector3.up * 0.22f,
                1.15f,
                ~0,
                QueryTriggerInteraction.Ignore);

            Assert.That(buildingOverlaps.Contains(waterCollider), Is.True);
            Assert.That(roadOverlaps.Contains(waterCollider), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NavigationPath_DetoursAroundWaterBounds()
    {
        GridNavigationService navigation = new(Vector3.zero, 30, 20, 1f);
        GameObject root = new("Water Detour Test");

        try
        {
            WaterObstacleArea.Create(
                root.transform,
                "Test Pond",
                new Vector3(15f, 0f, 10f),
                new Vector2(8f, 8f),
                navigation,
                0f);

            List<Vector3> path = new();
            bool found = navigation.TryFindPath(
                new Vector3(4.5f, 0f, 10.5f),
                new Vector3(25.5f, 0f, 10.5f),
                path);

            Assert.That(found, Is.True);
            Assert.That(path, Is.Not.Empty);
            Assert.That(path.All(point => !navigation.IsBlocked(navigation.WorldToCell(point))), Is.True);
            Assert.That(path.Any(point => point.z < 6f || point.z > 14f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
}
