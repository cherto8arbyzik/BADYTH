using System.Collections.Generic;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class SettlementState : MonoBehaviour
{
    private readonly Dictionary<BuildingEffectType, int> _bonuses = new();
    private readonly Dictionary<string, int> _buildingCounts = new();
    private int _baseHousing;
    private int _baseStorage;
    private DiscoveryType _discoveries;

    public int CurrentResidents { get; private set; }
    public int HousingCapacity => _baseHousing + GetEffectTotal(BuildingEffectType.Housing);
    public int StorageCapacity => _baseStorage + GetEffectTotal(BuildingEffectType.Storage);
    public int CurrentMorale { get; private set; }
    public SettlementTier CurrentTier { get; private set; }
    public DiscoveryType Discoveries => _discoveries;

    public void Initialize(int currentResidents, int baseHousing, int baseStorage)
    {
        CurrentResidents = Mathf.Max(0, currentResidents);
        _baseHousing = Mathf.Max(CurrentResidents, baseHousing);
        _baseStorage = Mathf.Max(0, baseStorage);
        _bonuses.Clear();
        _buildingCounts.Clear();
        _discoveries = DiscoveryType.None;
        CurrentTier = SettlementTier.Camp;
        CurrentMorale = 60;
    }

    public void RegisterBuilding(BuildingDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        _bonuses.TryGetValue(definition.EffectType, out int current);
        _bonuses[definition.EffectType] = current + definition.EffectValue;
        _buildingCounts.TryGetValue(definition.Id, out int buildingCount);
        _buildingCounts[definition.Id] = buildingCount + 1;
    }

    public void UnregisterBuilding(BuildingDefinition definition)
    {
        if (definition == null || !_bonuses.TryGetValue(definition.EffectType, out int current))
        {
            return;
        }

        int remaining = Mathf.Max(0, current - definition.EffectValue);
        if (remaining == 0)
        {
            _bonuses.Remove(definition.EffectType);
        }
        else
        {
            _bonuses[definition.EffectType] = remaining;
        }

        if (_buildingCounts.TryGetValue(definition.Id, out int buildingCount))
        {
            int remainingBuildings = Mathf.Max(0, buildingCount - 1);
            if (remainingBuildings == 0)
            {
                _buildingCounts.Remove(definition.Id);
            }
            else
            {
                _buildingCounts[definition.Id] = remainingBuildings;
            }
        }
    }

    public int GetEffectTotal(BuildingEffectType effectType)
    {
        return _bonuses.TryGetValue(effectType, out int value) ? value : 0;
    }

    public bool HasBuilding(string buildingId)
    {
        return !string.IsNullOrEmpty(buildingId) &&
               _buildingCounts.TryGetValue(buildingId, out int count) &&
               count > 0;
    }

    public bool HasDiscovery(DiscoveryType discovery)
    {
        return discovery == DiscoveryType.None || (_discoveries & discovery) == discovery;
    }

    public void AddDiscovery(DiscoveryType discovery)
    {
        _discoveries |= discovery;
    }

    public bool IsBuildingUnlocked(BuildingDefinition definition)
    {
        return definition != null &&
               CurrentTier >= definition.RequiredTier &&
               HasDiscovery(definition.RequiredDiscovery);
    }

    public string GetBuildingLockReason(BuildingDefinition definition)
    {
        if (definition == null)
        {
            return "Нет чертежа";
        }

        if (CurrentTier < definition.RequiredTier)
        {
            return $"Нужен уровень: {GetTierName(definition.RequiredTier)}";
        }

        if (!HasDiscovery(definition.RequiredDiscovery))
        {
            return GetDiscoveryName(definition.RequiredDiscovery);
        }

        return string.Empty;
    }

    public int TryAddResidents(int amount)
    {
        int accepted = Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, HousingCapacity - CurrentResidents));
        CurrentResidents += accepted;
        return accepted;
    }

    public void ApplyFoodCycle(bool fullyFed)
    {
        int moraleBonus = GetEffectTotal(BuildingEffectType.Morale) / 6;
        int delta = fullyFed ? 1 + moraleBonus : -8;
        CurrentMorale = Mathf.Clamp(CurrentMorale + delta, 0, 100);
    }

    public bool CanAdvanceTier(ResourceStockpile stockpile, out string reason)
    {
        if (CurrentTier == SettlementTier.Stronghold)
        {
            reason = "Поселение достигло высшего уровня";
            return false;
        }

        if (stockpile == null)
        {
            reason = "Нет доступа к запасам";
            return false;
        }

        switch (CurrentTier)
        {
            case SettlementTier.Camp:
                if (CurrentResidents < 5) { reason = "Нужно 5 жителей"; return false; }
                if (!HasBuilding("izba")) { reason = "Нужна изба"; return false; }
                if (!HasBuilding("storehouse")) { reason = "Нужен амбар"; return false; }
                if (stockpile.Get(ResourceType.Food) < 40) { reason = "Нужно 40 пищи"; return false; }
                break;
            case SettlementTier.Posad:
                if (CurrentResidents < 8) { reason = "Нужно 8 жителей"; return false; }
                if (!HasBuilding("workshop")) { reason = "Нужен ремесленный двор"; return false; }
                if (!HasBuilding("watchtower")) { reason = "Нужна дозорная башня"; return false; }
                if (!HasDiscovery(DiscoveryType.AncientBlueprints)) { reason = "Найдите древние чертежи"; return false; }
                break;
            case SettlementTier.Gorodishche:
                if (CurrentResidents < 12) { reason = "Нужно 12 жителей"; return false; }
                if (!HasBuilding("smithy")) { reason = "Нужна кузница"; return false; }
                if (!HasBuilding("kapishche")) { reason = "Нужно капище"; return false; }
                if (!HasDiscovery(DiscoveryType.WardStone)) { reason = "Найдите обережный камень"; return false; }
                break;
        }

        reason = "Улучшить поселение";
        return true;
    }

    public bool TryAdvanceTier(ResourceStockpile stockpile)
    {
        if (!CanAdvanceTier(stockpile, out _))
        {
            return false;
        }

        CurrentTier++;
        return true;
    }

    public static string GetTierName(SettlementTier tier)
    {
        return tier switch
        {
            SettlementTier.Camp => "Стан",
            SettlementTier.Posad => "Посад",
            SettlementTier.Gorodishche => "Городище",
            SettlementTier.Stronghold => "Оплот",
            _ => tier.ToString()
        };
    }

    private static string GetDiscoveryName(DiscoveryType discovery)
    {
        return discovery switch
        {
            DiscoveryType.AncientBlueprints => "Нужны древние чертежи",
            DiscoveryType.GrainSeeds => "Нужно найти семена",
            DiscoveryType.SacredRelic => "Нужно найти священную реликвию",
            DiscoveryType.WardStone => "Нужно найти обережный камень",
            DiscoveryType.SkyGlass => "Нужно найти небесное стекло",
            _ => "Нужно открытие с вылазки"
        };
    }
}
}
