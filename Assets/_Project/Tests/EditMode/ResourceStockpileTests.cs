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
}
}
