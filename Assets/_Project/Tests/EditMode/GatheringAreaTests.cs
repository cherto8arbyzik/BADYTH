using Hollowwest.Economy;
using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class GatheringAreaTests
{
    [Test]
    public void IssueOrder_FiltersNodesInsideSelectedArea()
    {
        GameObject controllerOwner = new("Gathering Controller Test");
        GameObject timberOwner = new("Timber Test Node");
        GameObject stoneOwner = new("Stone Test Node");
        GameObject outsideOwner = new("Outside Test Node");

        try
        {
            timberOwner.transform.position = new Vector3(1000f, 0f, 1000f);
            stoneOwner.transform.position = new Vector3(1005f, 0f, 1000f);
            outsideOwner.transform.position = new Vector3(1040f, 0f, 1000f);
            timberOwner.AddComponent<ResourceNode>().Configure(ResourceType.Timber, 20, 2);
            stoneOwner.AddComponent<ResourceNode>().Configure(ResourceType.Stone, 20, 2);
            outsideOwner.AddComponent<ResourceNode>().Configure(ResourceType.Timber, 20, 2);

            GatheringAreaController controller = controllerOwner.AddComponent<GatheringAreaController>();
            Bounds area = new(new Vector3(1002.5f, 0f, 1000f), new Vector3(16f, 4f, 16f));

            Assert.That(controller.IssueOrder(area, ResourceType.Timber), Is.EqualTo(1));
            Assert.That(controller.OrderedNodeCount, Is.EqualTo(1));
            Assert.That(controller.IssueOrder(area, null), Is.EqualTo(2));
        }
        finally
        {
            Object.DestroyImmediate(outsideOwner);
            Object.DestroyImmediate(stoneOwner);
            Object.DestroyImmediate(timberOwner);
            Object.DestroyImmediate(controllerOwner);
        }
    }
}
}
