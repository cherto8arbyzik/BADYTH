using System.Linq;
using Hollowwest.Economy;
using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class SettlementEconomyTests
{
    [Test]
    public void FoodCycle_ConsumesFoodAndPenalizesHunger()
    {
        GameObject owner = new("Economy Test");

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.Initialize(100, new[] { new ResourceAmount(ResourceType.Food, 8) });
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 3, 100);
            SettlementEconomy economy = owner.AddComponent<SettlementEconomy>();
            economy.Initialize(stockpile, settlement, 60f);

            economy.ProcessFoodCycle();
            Assert.That(stockpile.Get(ResourceType.Food), Is.EqualTo(2));
            Assert.That(settlement.CurrentMorale, Is.EqualTo(61));

            economy.ProcessFoodCycle();
            Assert.That(stockpile.Get(ResourceType.Food), Is.Zero);
            Assert.That(settlement.CurrentMorale, Is.EqualTo(53));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void Expedition_ConsumesSuppliesAndUnlocksBlueprints()
    {
        GameObject owner = new("Expedition Test");

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.Initialize(200, new[]
            {
                new ResourceAmount(ResourceType.Food, 20),
                new ResourceAmount(ResourceType.Provisions, 10)
            });
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 4, 200);
            ExpeditionSystem expedition = owner.AddComponent<ExpeditionSystem>();
            expedition.Initialize(stockpile, settlement, 20f);

            Assert.That(expedition.TryStart(out _), Is.True);
            expedition.CompleteExpedition();

            Assert.That(stockpile.Get(ResourceType.OldIron), Is.EqualTo(12));
            Assert.That(settlement.HasDiscovery(DiscoveryType.AncientBlueprints), Is.True);
            Assert.That(settlement.CurrentResidents, Is.EqualTo(4));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void SettlementTier_RequiresBuildingsResidentsAndFood()
    {
        GameObject owner = new("Progression Test");
        var catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            ResourceStockpile stockpile = owner.AddComponent<ResourceStockpile>();
            stockpile.Initialize(200, new[] { new ResourceAmount(ResourceType.Food, 50) });
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 5, 200);
            settlement.TryAddResidents(2);
            settlement.RegisterBuilding(catalog.Single(definition => definition.Id == "izba"));
            settlement.RegisterBuilding(catalog.Single(definition => definition.Id == "storehouse"));

            Assert.That(settlement.TryAdvanceTier(stockpile), Is.True);
            Assert.That(settlement.CurrentTier, Is.EqualTo(SettlementTier.Posad));
        }
        finally
        {
            foreach (BuildingDefinition definition in catalog)
            {
                Object.DestroyImmediate(definition);
            }
            Object.DestroyImmediate(owner);
        }
    }
}
}
