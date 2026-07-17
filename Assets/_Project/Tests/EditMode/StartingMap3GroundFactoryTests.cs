using Hollowwest.Selection;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class StartingMap3GroundFactoryTests
{
    private const float RuntimeScale = 147.69389266f;
    private static readonly Vector3 RuntimeGroundCenter =
        new(0.000596525f, 0.42028886f, 0.00081149f);

    private GameObject _parent;

    [TearDown]
    public void TearDown()
    {
        if (_parent != null)
        {
            Object.DestroyImmediate(_parent);
        }
    }

    [Test]
    public void TryCreate_UsesDedicatedLowPolyStaticMeshCollider()
    {
        _parent = new GameObject("Aligned Map 3 Root");
        _parent.transform.localScale = Vector3.one * RuntimeScale;
        _parent.transform.localPosition = -RuntimeGroundCenter * RuntimeScale;

        bool created = StartingMap3GroundFactory.TryCreate(
            _parent.transform,
            out GameObject ground);
        Physics.SyncTransforms();

        Assert.That(created, Is.True);
        Assert.That(ground.GetComponent<GroundSurface>(), Is.Not.Null);
        MeshCollider collider = ground.GetComponentInChildren<MeshCollider>();
        Assert.That(collider, Is.Not.Null);
        Assert.That(collider.convex, Is.False);
        Assert.That(collider.sharedMesh.vertexCount, Is.LessThan(20000));
        Assert.That(collider.gameObject.isStatic, Is.True);
        Assert.That(
            GroundSurface.TryProjectPoint(Vector3.zero, out Vector3 projected),
            Is.True);
        Assert.That(projected.y, Is.InRange(-2f, 12f));
    }
}
}
