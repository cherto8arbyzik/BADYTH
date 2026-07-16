using Hollowwest.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class ResourceStockpileTests
{
    [Test]
    public void TrySpendWood_OnlySpendsWhenEnoughWoodIsAvailable()
    {
        GameObject owner = new("Stockpile Test");

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.AddWood(30);

            Assert.That(stockpile.TrySpendWood(40), Is.False);
            Assert.That(stockpile.Wood, Is.EqualTo(30));
            Assert.That(stockpile.TrySpendWood(20), Is.True);
            Assert.That(stockpile.Wood, Is.EqualTo(10));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }


    [Test]
    public void MultiResourceSpend_IsAtomicAndStorageIsBounded()
    {
        GameObject owner = new("Stockpile Test");

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.Initialize(20, new[]
            {
                new ResourceAmount(ResourceType.Timber, 10),
                new ResourceAmount(ResourceType.Stone, 5)
            });

            ResourceAmount[] expensive =
            {
                new(ResourceType.Timber, 6),
                new(ResourceType.Stone, 8)
            };
            Assert.That(stockpile.TrySpend(expensive), Is.False);
            Assert.That(stockpile.Get(ResourceType.Timber), Is.EqualTo(10));
            Assert.That(stockpile.Get(ResourceType.Stone), Is.EqualTo(5));

            Assert.That(stockpile.Add(ResourceType.Food, 20), Is.EqualTo(5));
            Assert.That(stockpile.UsedCapacity, Is.EqualTo(20));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
}
