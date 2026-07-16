using System;
using System.Collections.Generic;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public static class BuildingCatalog
{
    private const float ConstructionWorkPerFootprint = 3f;
    private const float MinimumConstructionWork = 10f;

    public static IReadOnlyList<BuildingDefinition> CreatePrototypeCatalog()
    {
        BuildingDefinition izba = Create(
            "izba",
            "Изба",
            "Жильё для семьи. Перед размещением выберите облик сруба; характеристики обоих вариантов одинаковы.",
            BuildingCategory.Settlement,
            6f,
            "Custom/Buildings/Izba_Rowanhearth/PF_Izba_Rowanhearth",
            SettlementTier.Camp,
            DiscoveryType.None,
            0,
            Costs(A(ResourceType.Timber, 20), A(ResourceType.Stone, 8)));
        izba.ConfigureVisualVariants(
            new BuildingVisualVariant(
                "rowanhearth",
                "Рябиновый сруб",
                "Резная изба Клода с рябиновым оберегом и выразительным коньком-охлупнем.",
                "Custom/Buildings/Izba_Rowanhearth/PF_Izba_Rowanhearth"),
            new BuildingVisualVariant(
                "hearthward",
                "Очажный сруб",
                "Наша детальная изба: тёплые окна, дранка, поленница и защитные коньки на крыше.",
                "Custom/Buildings/Izba/PF_Izba_Hearthward"));

        return new List<BuildingDefinition>(21)
        {
            izba,
            Create("longhouse", "Длинный дом", "Общий кров для большой группы переселенцев.", BuildingCategory.Settlement, 8f, "House_4", SettlementTier.Posad, DiscoveryType.None, 0,
                Costs(A(ResourceType.Plank, 28), A(ResourceType.Stone, 16), A(ResourceType.Brick, 8), A(ResourceType.Tool, 2))),
            Create("storehouse", "Амбар", "Увеличивает общий предел хранения ресурсов.", BuildingCategory.Settlement, 6.8f, "Stable", SettlementTier.Camp, DiscoveryType.None, 0,
                Costs(A(ResourceType.Timber, 24), A(ResourceType.Stone, 12))),
            Create("common_yard", "Общий двор", "Место отдыха, собраний и распределения работ.", BuildingCategory.Settlement, 7.4f, "Inn", SettlementTier.Posad, DiscoveryType.None, 0,
                Costs(A(ResourceType.Plank, 18), A(ResourceType.Stone, 12), A(ResourceType.Brick, 6))),

            Create("lumber_camp", "Лесной стан", "Заготавливает возобновляемую древесину острова.", BuildingCategory.Gathering, 6.6f, "Sawmill", SettlementTier.Camp, DiscoveryType.None, 3,
                Costs(A(ResourceType.Timber, 18), A(ResourceType.Stone, 5), A(ResourceType.Tool, 1)),
                Recipe("рубка леса", 6f, null, A(ResourceType.Timber, 4))),
            Create("hunter_hut", "Ловчая изба", "Приносит пищу и шкуры с охотничьих угодий.", BuildingCategory.Gathering, 5.8f, "House_2", SettlementTier.Camp, DiscoveryType.None, 2,
                Costs(A(ResourceType.Timber, 16), A(ResourceType.Stone, 6), A(ResourceType.Tool, 1)),
                Recipe("охота", 8f, null, A(ResourceType.Food, 4), A(ResourceType.Hide, 1))),
            Create("forager_shed", "Дом травницы", "Собирает травы, грибы и лечебные коренья.", BuildingCategory.Gathering, 5.6f, "House_3", SettlementTier.Camp, DiscoveryType.None, 2,
                Costs(A(ResourceType.Timber, 14), A(ResourceType.Stone, 4)),
                Recipe("сбор трав", 7f, null, A(ResourceType.Herb, 3), A(ResourceType.Food, 1))),
            Create("clay_yard", "Глиняный двор", "Добывает глину у озёр и обжигает простой кирпич.", BuildingCategory.Gathering, 6.4f, "Stable", SettlementTier.Camp, DiscoveryType.None, 2,
                Costs(A(ResourceType.Timber, 20), A(ResourceType.Stone, 10), A(ResourceType.Tool, 1)),
                Recipe("добыча глины", 8f, null, A(ResourceType.Clay, 3), A(ResourceType.Brick, 1))),
            Create("fishing_station", "Рыбацкий стан", "Береговой помост, лодка и коптильня. Даёт стабильную пищу, но ставится только у воды.", BuildingCategory.Gathering, 6.2f, "House_3", SettlementTier.Camp, DiscoveryType.None, 2,
                Costs(A(ResourceType.Timber, 18), A(ResourceType.Stone, 4), A(ResourceType.Tool, 1)),
                Recipe("рыбный промысел", 7f, null, A(ResourceType.Food, 5)),
                BuildingPlacementRequirement.LakeShore),

            Create("workshop", "Ремесленный двор", "Распускает брёвна на доски и делает простые инструменты.", BuildingCategory.Craft, 6.4f, "House_3", SettlementTier.Posad, DiscoveryType.None, 3,
                Costs(A(ResourceType.Plank, 25), A(ResourceType.Stone, 12), A(ResourceType.Tool, 2)),
                Recipe("доски и инструменты", 10f, Costs(A(ResourceType.Timber, 2), A(ResourceType.Stone, 1)), A(ResourceType.Plank, 3), A(ResourceType.Tool, 1))),
            Create("smithy", "Кузница", "Перерабатывает старое железо в хорошие инструменты и снаряжение.", BuildingCategory.Craft, 7.2f, "Blacksmith", SettlementTier.Gorodishche, DiscoveryType.AncientBlueprints, 3,
                Costs(A(ResourceType.Stone, 30), A(ResourceType.Brick, 20), A(ResourceType.Plank, 15), A(ResourceType.OldIron, 6), A(ResourceType.Tool, 3)),
                Recipe("железные инструменты", 12f, Costs(A(ResourceType.OldIron, 2), A(ResourceType.Timber, 1)), A(ResourceType.Tool, 3))),
            Create("tannery", "Кожевня", "Выделывает шкуры для одежды и снаряжения.", BuildingCategory.Craft, 6.2f, "Sawmill", SettlementTier.Posad, DiscoveryType.None, 2,
                Costs(A(ResourceType.Plank, 20), A(ResourceType.Stone, 8), A(ResourceType.Tool, 1)),
                Recipe("выделка кожи", 9f, Costs(A(ResourceType.Hide, 2), A(ResourceType.Herb, 1)), A(ResourceType.Leather, 2))),
            Create("mill", "Мельница", "Перерабатывает зерно в пищу и походный провиант.", BuildingCategory.Craft, 8f, "Mill", SettlementTier.Gorodishche, DiscoveryType.GrainSeeds, 3,
                Costs(A(ResourceType.Plank, 35), A(ResourceType.Stone, 20), A(ResourceType.Brick, 12), A(ResourceType.Tool, 3)),
                Recipe("поля и помол", 12f, null, A(ResourceType.Grain, 1), A(ResourceType.Food, 4), A(ResourceType.Provisions, 2))),

            Create("watchtower", "Дозорная башня", "Заранее обнаруживает врагов и события вокруг острова.", BuildingCategory.Defense, 5.4f, "Bell_Tower", SettlementTier.Posad, DiscoveryType.None, 1,
                Costs(A(ResourceType.Plank, 20), A(ResourceType.Stone, 12), A(ResourceType.Tool, 1))),
            Create("guardhouse", "Караульная изба", "Даёт место дружине и постоянным патрулям.", BuildingCategory.Defense, 6f, "House_2", SettlementTier.Gorodishche, DiscoveryType.AncientBlueprints, 3,
                Costs(A(ResourceType.Plank, 28), A(ResourceType.Stone, 18), A(ResourceType.Brick, 10), A(ResourceType.OldIron, 4), A(ResourceType.Tool, 2))),
            Create("palisade_yard", "Плотницкий рубеж", "Снабжает поселение частоколом и ловушками.", BuildingCategory.Defense, 6.8f, "Sawmill", SettlementTier.Gorodishche, DiscoveryType.AncientBlueprints, 2,
                Costs(A(ResourceType.Plank, 35), A(ResourceType.Stone, 15), A(ResourceType.Tool, 4))),
            Create("fortified_gate", "Укреплённые ворота", "Контролируют главный вход и держат тяжёлый натиск.", BuildingCategory.Defense, 7.6f, "Stable", SettlementTier.Stronghold, DiscoveryType.AncientBlueprints, 2,
                Costs(A(ResourceType.Stone, 45), A(ResourceType.Brick, 35), A(ResourceType.Plank, 25), A(ResourceType.OldIron, 10), A(ResourceType.Tool, 5))),

            Create("kapishche", "Капище", "Открывает обряды старых богов и ослабляет скверну.", BuildingCategory.Sacred, 6.2f, "Bell_Tower", SettlementTier.Gorodishche, DiscoveryType.SacredRelic, 1,
                Costs(A(ResourceType.Stone, 30), A(ResourceType.Plank, 12), A(ResourceType.Relic, 1))),
            Create("healer_banya", "Баня-знахарня", "Превращает травы в лекарства для жителей и вылазок.", BuildingCategory.Sacred, 6f, "House_1", SettlementTier.Posad, DiscoveryType.None, 2,
                Costs(A(ResourceType.Plank, 18), A(ResourceType.Stone, 8), A(ResourceType.Brick, 4)),
                Recipe("лекарства", 10f, Costs(A(ResourceType.Herb, 3)), A(ResourceType.Medicine, 2))),
            Create("herb_garden", "Обережный сад", "Устойчиво выращивает редкие лечебные травы.", BuildingCategory.Sacred, 6.4f, "House_2", SettlementTier.Gorodishche, DiscoveryType.GrainSeeds, 2,
                Costs(A(ResourceType.Timber, 10), A(ResourceType.Stone, 5)),
                Recipe("выращивание трав", 9f, null, A(ResourceType.Herb, 4))),
            Create("ward_house", "Дом оберегов", "Создаёт защитные знаки против ночной нечисти.", BuildingCategory.Sacred, 6.4f, "House_4", SettlementTier.Stronghold, DiscoveryType.WardStone, 2,
                Costs(A(ResourceType.Plank, 25), A(ResourceType.Brick, 15), A(ResourceType.WardStone, 3), A(ResourceType.OldIron, 2)))
        };
    }

    private static BuildingDefinition Create(
        string id,
        string displayName,
        string description,
        BuildingCategory category,
        float footprint,
        string modelName,
        SettlementTier tier,
        DiscoveryType discovery,
        int workerCapacity,
        ResourceAmount[] costs,
        ProductionRecipe recipe = null,
        BuildingPlacementRequirement placementRequirement = BuildingPlacementRequirement.Anywhere)
    {
        ResolveEffect(id, out BuildingEffectType effectType, out int effectValue);
        BuildingDefinition definition = ScriptableObject.CreateInstance<BuildingDefinition>();
        definition.name = id;
        definition.hideFlags = HideFlags.DontSave;
        definition.Configure(
            id,
            displayName,
            description,
            category,
            costs,
            footprint,
            modelName,
            Mathf.Max(MinimumConstructionWork, footprint * ConstructionWorkPerFootprint),
            effectType,
            effectValue,
            tier,
            discovery,
            workerCapacity,
            recipe,
            placementRequirement);
        return definition;
    }

    private static ResourceAmount A(ResourceType type, int amount)
    {
        return new ResourceAmount(type, amount);
    }

    private static ResourceAmount[] Costs(params ResourceAmount[] costs)
    {
        return costs ?? Array.Empty<ResourceAmount>();
    }

    private static ProductionRecipe Recipe(
        string name,
        float seconds,
        ResourceAmount[] inputs,
        params ResourceAmount[] outputs)
    {
        return new ProductionRecipe(name, inputs, outputs, seconds);
    }

    private static void ResolveEffect(string id, out BuildingEffectType effectType, out int effectValue)
    {
        effectType = BuildingEffectType.CraftSpeed;
        effectValue = 10;

        switch (id)
        {
            case "izba": effectType = BuildingEffectType.Housing; effectValue = 2; break;
            case "longhouse": effectType = BuildingEffectType.Housing; effectValue = 5; break;
            case "storehouse": effectType = BuildingEffectType.Storage; effectValue = 150; break;
            case "common_yard": effectType = BuildingEffectType.Morale; effectValue = 12; break;
            case "lumber_camp": effectType = BuildingEffectType.WoodProduction; effectValue = 25; break;
            case "hunter_hut": effectType = BuildingEffectType.FoodProduction; effectValue = 20; break;
            case "forager_shed": effectType = BuildingEffectType.HerbProduction; effectValue = 20; break;
            case "clay_yard": effectType = BuildingEffectType.ClayProduction; effectValue = 20; break;
            case "fishing_station": effectType = BuildingEffectType.Fishing; effectValue = 25; break;
            case "workshop": effectType = BuildingEffectType.CraftSpeed; effectValue = 18; break;
            case "smithy": effectType = BuildingEffectType.Equipment; effectValue = 20; break;
            case "tannery": effectType = BuildingEffectType.LeatherProduction; effectValue = 20; break;
            case "mill": effectType = BuildingEffectType.GrainProduction; effectValue = 30; break;
            case "watchtower": effectType = BuildingEffectType.Detection; effectValue = 20; break;
            case "guardhouse": effectType = BuildingEffectType.Garrison; effectValue = 3; break;
            case "palisade_yard": effectType = BuildingEffectType.Fortification; effectValue = 18; break;
            case "fortified_gate": effectType = BuildingEffectType.GateDefense; effectValue = 30; break;
            case "kapishche": effectType = BuildingEffectType.Faith; effectValue = 20; break;
            case "healer_banya": effectType = BuildingEffectType.Healing; effectValue = 25; break;
            case "herb_garden": effectType = BuildingEffectType.RareHerbs; effectValue = 25; break;
            case "ward_house": effectType = BuildingEffectType.Ward; effectValue = 25; break;
        }
    }
}
}
