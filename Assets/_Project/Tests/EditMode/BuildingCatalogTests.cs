using System.Collections.Generic;
using System.Linq;
using Hollowwest.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Hollowwest.Tests
{

public sealed class BuildingCatalogTests
{
    [Test]
    public void PrototypeCatalog_ContainsUniqueBuildingsAndExpandedGatheringCategory()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            Assert.That(catalog.Count, Is.EqualTo(21));
            Assert.That(catalog.Select(definition => definition.Id).Distinct().Count(), Is.EqualTo(21));

            foreach (BuildingCategory category in System.Enum.GetValues(typeof(BuildingCategory)))
            {
                int expected = category == BuildingCategory.Gathering ? 5 : 4;
                Assert.That(catalog.Count(definition => definition.Category == category), Is.EqualTo(expected));
            }
        }
        finally
        {
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void FishingStation_RequiresLakeShoreAndProducesFood()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            BuildingDefinition fishing = catalog.Single(definition => definition.Id == "fishing_station");
            Assert.That(fishing.PlacementRequirement, Is.EqualTo(BuildingPlacementRequirement.LakeShore));
            Assert.That(fishing.HasProduction, Is.True);
            Assert.That(fishing.ProductionRecipe.Outputs.Any(output => output.Type == Hollowwest.Economy.ResourceType.Food), Is.True);
        }
        finally
        {
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void PrototypeCatalog_StartsWithCampIzbaAndMultiResourceCost()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            BuildingDefinition izba = catalog[0];
            Assert.That(izba.Id, Is.EqualTo("izba"));
            Assert.That(izba.WoodCost, Is.EqualTo(20));
            Assert.That(izba.GetCost(Hollowwest.Economy.ResourceType.Stone), Is.EqualTo(8));
            Assert.That(izba.RequiredTier, Is.EqualTo(SettlementTier.Camp));
            Assert.That(izba.ModelName, Is.EqualTo("Custom/Buildings/Izba_Rowanhearth/PF_Izba_Rowanhearth"));
            Assert.That(izba.Footprint, Is.GreaterThan(2f));
            Assert.That(izba.ConstructionWork, Is.GreaterThanOrEqualTo(10f));
        }
        finally
        {
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void Catalog_ConnectsProductionAndExpeditionUnlocks()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            BuildingDefinition lumberCamp = catalog.Single(definition => definition.Id == "lumber_camp");
            BuildingDefinition smithy = catalog.Single(definition => definition.Id == "smithy");

            Assert.That(lumberCamp.HasProduction, Is.True);
            Assert.That(lumberCamp.WorkerCapacity, Is.EqualTo(3));
            Assert.That(smithy.RequiredTier, Is.EqualTo(SettlementTier.Gorodishche));
            Assert.That(smithy.RequiredDiscovery, Is.EqualTo(DiscoveryType.AncientBlueprints));
        }
        finally
        {
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void PlacementRules_RejectRoadAndWorldEdge()
    {
        Bounds bounds = new(Vector3.zero, new Vector3(100f, 1f, 80f));
        Vector3[] road =
        {
            new(-30f, 0f, 0f),
            new(30f, 0f, 0f)
        };

        Assert.That(BuildingPlacementRules.IsInsideBounds(bounds, new Vector3(0f, 0f, 15f), 6f), Is.True);
        Assert.That(BuildingPlacementRules.IsInsideBounds(bounds, new Vector3(49f, 0f, 0f), 6f), Is.False);
        Assert.That(BuildingPlacementRules.OverlapsRoad(Vector3.zero, 6f, road, 1.6f), Is.True);
        Assert.That(BuildingPlacementRules.OverlapsRoad(new Vector3(0f, 0f, 15f), 6f, road, 1.6f), Is.False);
    }

    [Test]
    public void Izba_IncreasesHousingCapacityByTwo()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();
        GameObject owner = new("Settlement State Test");

        try
        {
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 3, 300);
            settlement.RegisterBuilding(catalog[0]);

            Assert.That(settlement.CurrentResidents, Is.EqualTo(3));
            Assert.That(settlement.HousingCapacity, Is.EqualTo(5));

            settlement.UnregisterBuilding(catalog[0]);
            Assert.That(settlement.HousingCapacity, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(owner);
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void ConstructionSite_RegistersEffectOnlyAfterCompletion()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();
        GameObject settlementOwner = new("Settlement State Test");
        GameObject buildingsRoot = new("Buildings Root Test");

        try
        {
            SettlementState settlement = settlementOwner.AddComponent<SettlementState>();
            settlement.Initialize(3, 3, 300);
            Hollowwest.Navigation.GridNavigationService navigation = new(
                new Vector3(-20f, 0f, -20f),
                40,
                40,
                1f);

            TownBuilding building = TownConstructionFactory.CreateConstructionSite(
                catalog[0],
                buildingsRoot.transform,
                Vector3.zero,
                0f,
                navigation,
                settlement,
                null);

            Assert.That(building.IsUnderConstruction, Is.True);
            Assert.That(building.CanDemolish, Is.True);
            Assert.That(settlement.HousingCapacity, Is.EqualTo(3));

            Transform constructionPieces = building.transform.Find("Visible Construction Materials");
            Assert.That(constructionPieces, Is.Not.Null);
            Renderer[] pieceRenderers = constructionPieces.GetComponentsInChildren<Renderer>(true);
            Assert.That(pieceRenderers.Length, Is.GreaterThan(20));
            Assert.That(pieceRenderers.Count(renderer => renderer.enabled), Is.EqualTo(0));

            building.SetConstructionProgress(0.5f);
            int visiblePieces = pieceRenderers.Count(renderer => renderer.enabled);
            Assert.That(visiblePieces, Is.GreaterThan(0));
            Assert.That(visiblePieces, Is.LessThan(pieceRenderers.Length));

            building.CompleteConstruction();
            Assert.That(building.IsOperational, Is.True);
            Assert.That(constructionPieces.gameObject.activeSelf, Is.False);
            Assert.That(settlement.HousingCapacity, Is.EqualTo(5));

            Assert.That(building.Demolish(), Is.True);
            Assert.That(settlement.HousingCapacity, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(buildingsRoot);
            Object.DestroyImmediate(settlementOwner);
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void PrototypeCatalog_OffersTwoVisualVariantsThroughSingleIzbaDefinition()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();

        try
        {
            BuildingDefinition izba = catalog.Single(definition => definition.Id == "izba");

            Assert.That(catalog.Any(definition => definition.Id == "izba_hearthward"), Is.False);
            Assert.That(izba.DisplayName, Is.EqualTo("Изба"));
            Assert.That(izba.HasVisualVariants, Is.True);
            Assert.That(izba.VisualVariants.Count, Is.EqualTo(2));
            Assert.That(
                izba.VisualVariants.Select(variant => variant.ModelName),
                Is.EquivalentTo(new[]
                {
                    "Custom/Buildings/Izba_Rowanhearth/PF_Izba_Rowanhearth",
                    "Custom/Buildings/Izba/PF_Izba_Hearthward"
                }));
            Assert.That(izba.EffectType, Is.EqualTo(BuildingEffectType.Housing));
            Assert.That(izba.EffectValue, Is.EqualTo(2));
            Assert.That(izba.WoodCost, Is.EqualTo(20));
            Assert.That(izba.GetCost(Hollowwest.Economy.ResourceType.Stone), Is.EqualTo(8));
        }
        finally
        {
            DestroyCatalog(catalog);
        }
    }

    [Test]
    public void IzbaVisualVariants_ShareSingleProgressionIdentity()
    {
        IReadOnlyList<BuildingDefinition> catalog = BuildingCatalog.CreatePrototypeCatalog();
        GameObject owner = new("Izba Family Test");

        try
        {
            SettlementState settlement = owner.AddComponent<SettlementState>();
            settlement.Initialize(3, 3, 300);

            Assert.That(settlement.HasBuilding("izba"), Is.False);

            settlement.RegisterBuilding(catalog.Single(definition => definition.Id == "izba"));

            Assert.That(settlement.HasBuilding("izba"), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            DestroyCatalog(catalog);
        }
    }

    private static void DestroyCatalog(IReadOnlyList<BuildingDefinition> catalog)
    {
        foreach (BuildingDefinition definition in catalog)
        {
            Object.DestroyImmediate(definition);
        }
    }
}
}
