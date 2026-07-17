using Hollowwest.Selection;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class GroundSurfaceTests
{
    private GameObject _root;

    [TearDown]
    public void TearDown()
    {
        if (_root != null)
        {
            Object.DestroyImmediate(_root);
        }
    }

    [Test]
    public void TryProjectPoint_ReturnsHeightAndNormalFromMeshCollider()
    {
        CreateSurface();

        bool found = GroundSurface.TryProjectPoint(
            new Vector3(0f, 50f, 0f),
            out Vector3 projected,
            out Vector3 normal);

        Assert.That(found, Is.True);
        Assert.That(projected.y, Is.EqualTo(1f).Within(0.02f));
        Assert.That(Vector3.Angle(normal, new Vector3(-0.5f, 1f, 0f)), Is.LessThan(0.5f));
    }

    [Test]
    public void TryProjectFootprint_RejectsAreaOutsideSurface()
    {
        CreateSurface();

        bool supported = GroundSurface.TryProjectFootprint(
            new Vector3(1.8f, 0f, 0f),
            Quaternion.identity,
            new Vector2(0.5f, 0.5f),
            2f,
            out _);

        Assert.That(supported, Is.False);
    }

    private void CreateSurface()
    {
        _root = new GameObject("Test Ground");
        Mesh mesh = new()
        {
            vertices = new[]
            {
                new Vector3(-2f, 0f, -2f),
                new Vector3(-2f, 0f, 2f),
                new Vector3(2f, 2f, -2f),
                new Vector3(2f, 2f, 2f)
            },
            triangles = new[] { 0, 1, 2, 2, 1, 3 }
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        _root.AddComponent<MeshFilter>().sharedMesh = mesh;
        _root.AddComponent<MeshCollider>().sharedMesh = mesh;
        _root.AddComponent<GroundSurface>();
        Physics.SyncTransforms();
    }
}
}
