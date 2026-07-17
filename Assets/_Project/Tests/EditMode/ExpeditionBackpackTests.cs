using System.Collections.Generic;
using Hollowwest.Economy;
using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class ExpeditionBackpackTests
{
    [Test]
    public void Backpack_EnforcesSlotsAndStackCaps()
    {
        ExpeditionBackpack backpack = new(2, 1, 5);

        Assert.That(backpack.TryAdd(ResourceType.Timber, 8), Is.EqualTo(8));
        Assert.That(backpack.TryAdd(ResourceType.Stone, 3), Is.Zero);
        Assert.That(backpack.TryAdd(ResourceType.Food, 1), Is.Zero);
        Assert.That(backpack.TryAdd(ResourceType.Timber, 4), Is.EqualTo(2));
        Assert.That(backpack.Get(ResourceType.Timber), Is.EqualTo(10));
        Assert.That(backpack.Get(ResourceType.Stone), Is.Zero);
        Assert.That(backpack.SlotsUsed, Is.EqualTo(2));
        Assert.That(backpack.IsFull, Is.True);
    }

    [Test]
    public void Backpack_LoseHalfRoundsDownAndRemovesEmptyStacks()
    {
        ExpeditionBackpack backpack = new(2, 2, 10);
        backpack.TryAdd(ResourceType.Timber, 9);
        backpack.TryAdd(ResourceType.Stone, 1);
        backpack.TryAdd(ResourceType.Herb, 6);

        backpack.LoseHalf();

        Assert.That(backpack.Get(ResourceType.Timber), Is.EqualTo(4));
        Assert.That(backpack.Get(ResourceType.Stone), Is.Zero);
        Assert.That(backpack.Get(ResourceType.Herb), Is.EqualTo(3));
        Assert.That(backpack.SlotsUsed, Is.EqualTo(2));
    }

    [Test]
    public void Backpack_UsesVisibleGridCoordinatesAndMultipleStacks()
    {
        ExpeditionBackpack backpack = new(2, 2, 3);

        Assert.That(backpack.TryAdd(ResourceType.Timber, 7), Is.EqualTo(7));
        Assert.That(backpack.SlotsUsed, Is.EqualTo(3));
        Assert.That(backpack.Slots[0].X, Is.EqualTo(0));
        Assert.That(backpack.Slots[0].Y, Is.EqualTo(0));
        Assert.That(backpack.Slots[2].X, Is.EqualTo(0));
        Assert.That(backpack.Slots[2].Y, Is.EqualTo(1));
    }

    [Test]
    public void PlayableExpedition_ReturnsLootAndUnlocksAnomalyDiscovery()
    {
        GameObject owner = new("Playable Expedition Test");

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.Initialize(100, new List<ResourceAmount>());
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 4, 100);
            ExpeditionSystem expedition = owner.AddComponent<ExpeditionSystem>();
            expedition.Initialize(stockpile, settlement, 20f);

            expedition.CompletePlayableExpedition(
                new[]
                {
                    new ResourceAmount(ResourceType.Timber, 5),
                    new ResourceAmount(ResourceType.OldIron, 3)
                },
                true,
                false);

            Assert.That(stockpile.Get(ResourceType.Timber), Is.EqualTo(5));
            Assert.That(stockpile.Get(ResourceType.OldIron), Is.EqualTo(3));
            Assert.That(settlement.HasDiscovery(DiscoveryType.AncientBlueprints), Is.True);
            Assert.That(settlement.CurrentResidents, Is.EqualTo(4));
            Assert.That(expedition.CompletedExpeditions, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}

}
