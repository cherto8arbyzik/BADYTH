using System;
using System.Collections.Generic;
using System.Text;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public enum BuildingCategory
{
    Settlement,
    Gathering,
    Craft,
    Defense,
    Sacred
}

public enum BuildingPlacementRequirement
{
    Anywhere,
    LakeShore
}

public enum BuildingEffectType
{
    Housing,
    Storage,
    Morale,
    WoodProduction,
    FoodProduction,
    HerbProduction,
    ClayProduction,
    CraftSpeed,
    Equipment,
    LeatherProduction,
    GrainProduction,
    Detection,
    Garrison,
    Fortification,
    GateDefense,
    Faith,
    Healing,
    RareHerbs,
    Ward,
    Fishing
}

[Serializable]
public sealed class BuildingVisualVariant
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField] private string modelName;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public string ModelName => modelName;

    public BuildingVisualVariant(
        string variantId,
        string variantDisplayName,
        string variantDescription,
        string variantModelName)
    {
        id = variantId;
        displayName = variantDisplayName;
        description = variantDescription;
        modelName = variantModelName;
    }
}

public sealed class BuildingDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private string description;
    [SerializeField] private BuildingCategory category;
    [SerializeField] private ResourceAmount[] constructionCosts;
    [SerializeField] private float footprint;
    [SerializeField] private string modelName;
    [SerializeField] private float constructionWork;
    [SerializeField] private BuildingEffectType effectType;
    [SerializeField] private int effectValue;
    [SerializeField] private SettlementTier requiredTier;
    [SerializeField] private DiscoveryType requiredDiscovery;
    [SerializeField] private int workerCapacity;
    [SerializeField] private ProductionRecipe productionRecipe;
    [SerializeField] private BuildingPlacementRequirement placementRequirement;
    [SerializeField] private BuildingVisualVariant[] visualVariants = Array.Empty<BuildingVisualVariant>();

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public BuildingCategory Category => category;
    public IReadOnlyList<ResourceAmount> ConstructionCosts => constructionCosts;
    public int WoodCost => GetCost(ResourceType.Timber);
    public float Footprint => footprint;
    public string ModelName => modelName;
    public float ConstructionWork => constructionWork;
    public BuildingEffectType EffectType => effectType;
    public int EffectValue => effectValue;
    public SettlementTier RequiredTier => requiredTier;
    public DiscoveryType RequiredDiscovery => requiredDiscovery;
    public int WorkerCapacity => workerCapacity;
    public ProductionRecipe ProductionRecipe => productionRecipe;
    public BuildingPlacementRequirement PlacementRequirement => placementRequirement;
    public IReadOnlyList<BuildingVisualVariant> VisualVariants => visualVariants ?? Array.Empty<BuildingVisualVariant>();
    public bool HasVisualVariants => visualVariants != null && visualVariants.Length > 1;
    public bool HasProduction => productionRecipe != null && productionRecipe.Outputs.Count > 0;
    public string CostSummary => BuildCostSummary();
    public string EffectSummary => BuildEffectSummary();

    public void Configure(
        string definitionId,
        string definitionName,
        string definitionDescription,
        BuildingCategory definitionCategory,
        ResourceAmount[] definitionConstructionCosts,
        float definitionFootprint,
        string definitionModelName,
        float definitionConstructionWork,
        BuildingEffectType definitionEffectType,
        int definitionEffectValue,
        SettlementTier definitionRequiredTier,
        DiscoveryType definitionRequiredDiscovery,
        int definitionWorkerCapacity,
        ProductionRecipe definitionProductionRecipe,
        BuildingPlacementRequirement definitionPlacementRequirement = BuildingPlacementRequirement.Anywhere)
    {
        id = definitionId;
        displayName = definitionName;
        description = definitionDescription;
        category = definitionCategory;
        constructionCosts = definitionConstructionCosts ?? Array.Empty<ResourceAmount>();
        footprint = Mathf.Max(2f, definitionFootprint);
        modelName = definitionModelName;
        constructionWork = Mathf.Max(1f, definitionConstructionWork);
        effectType = definitionEffectType;
        effectValue = Mathf.Max(0, definitionEffectValue);
        requiredTier = definitionRequiredTier;
        requiredDiscovery = definitionRequiredDiscovery;
        workerCapacity = Mathf.Max(0, definitionWorkerCapacity);
        productionRecipe = definitionProductionRecipe;
        placementRequirement = definitionPlacementRequirement;
        visualVariants = Array.Empty<BuildingVisualVariant>();
    }

    public void ConfigureVisualVariants(params BuildingVisualVariant[] variants)
    {
        visualVariants = variants ?? Array.Empty<BuildingVisualVariant>();
    }

    public int GetCost(ResourceType type)
    {
        if (constructionCosts == null)
        {
            return 0;
        }

        foreach (ResourceAmount cost in constructionCosts)
        {
            if (cost.Type == type)
            {
                return cost.Amount;
            }
        }

        return 0;
    }

    private string BuildCostSummary()
    {
        if (constructionCosts == null || constructionCosts.Length == 0)
        {
            return "только работа";
        }

        StringBuilder builder = new();
        foreach (ResourceAmount cost in constructionCosts)
        {
            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(ResourceNames.GetShort(cost.Type));
            builder.Append(" ");
            builder.Append(cost.Amount);
        }

        return builder.ToString();
    }

    private string BuildEffectSummary()
    {
        switch (effectType)
        {
            case BuildingEffectType.Housing:
                return $"+{effectValue} мест для жителей";
            case BuildingEffectType.Storage:
                return $"+{effectValue} к запасам";
            case BuildingEffectType.Morale:
                return $"+{effectValue} к духу поселения";
            case BuildingEffectType.Detection:
                return $"+{effectValue} к обзору";
            case BuildingEffectType.Garrison:
                return $"+{effectValue} мест дружины";
            case BuildingEffectType.Fortification:
            case BuildingEffectType.GateDefense:
            case BuildingEffectType.Ward:
                return $"+{effectValue} к защите";
            case BuildingEffectType.Healing:
                return $"+{effectValue}% к лечению";
            case BuildingEffectType.Faith:
                return $"+{effectValue} веры";
            default:
                return $"+{effectValue}% эффективности";
        }
    }
}
}
