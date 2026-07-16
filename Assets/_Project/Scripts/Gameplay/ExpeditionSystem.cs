using System;
using System.Collections.Generic;
using System.Text;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionSystem : MonoBehaviour
{
    private static readonly ResourceAmount[] BasicCost =
    {
        new(ResourceType.Food, 10),
        new(ResourceType.Provisions, 5)
    };

    private static readonly ResourceAmount[] DangerousCost =
    {
        new(ResourceType.Food, 12),
        new(ResourceType.Provisions, 6),
        new(ResourceType.Medicine, 2)
    };

    private ResourceStockpile _stockpile;
    private SettlementState _settlement;
    private float _durationSeconds;
    private float _elapsed;

    public event Action<int> ResidentsRescued;

    public bool IsActive { get; private set; }
    public int CompletedExpeditions { get; private set; }
    public float Progress => ExpeditionTravelService.Instance != null
        ? ExpeditionTravelService.Instance.LoadProgress
        : !IsActive || _durationSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(_elapsed / _durationSeconds);
    public IReadOnlyList<ResourceAmount> CurrentCost => CompletedExpeditions < 2 ? BasicCost : DangerousCost;
    public string LastReport { get; private set; } = "Вылазка ещё не отправлялась";

    public void Initialize(ResourceStockpile stockpile, SettlementState settlement, float durationSeconds)
    {
        _stockpile = stockpile;
        _settlement = settlement;
        _durationSeconds = Mathf.Max(5f, durationSeconds);
    }

    public bool TryStart(out string reason)
    {
        if (IsActive)
        {
            reason = "Отряд уже находится на вылазке";
            LastReport = reason;
            return false;
        }

        if (_stockpile == null || _settlement == null)
        {
            reason = "Экономика не готова";
            LastReport = reason;
            return false;
        }

        if (!_stockpile.Has(CurrentCost))
        {
            reason = "Не хватает: " + _stockpile.GetMissingSummary(CurrentCost);
            LastReport = reason;
            return false;
        }

        if (_stockpile.StorageCapacity - _stockpile.UsedCapacity < 8)
        {
            reason = "Освободите хотя бы 8 мест в амбаре для добычи";
            LastReport = reason;
            return false;
        }

        _stockpile.TrySpend(CurrentCost);

        IsActive = true;
        _elapsed = 0f;
        LastReport = "Герой отправляется на внешний остров";
        if (Application.isPlaying && !ExpeditionTravelService.Begin(this, _stockpile))
        {
            IsActive = false;
            _stockpile.AddAll(CurrentCost);
            reason = "Переход на внешний остров уже выполняется";
            LastReport = reason;
            return false;
        }

        reason = LastReport;
        return true;
    }

    public string GetCostSummary()
    {
        StringBuilder builder = new();
        foreach (ResourceAmount cost in CurrentCost)
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

    private void Update()
    {
        if (!IsActive)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        if (_elapsed >= _durationSeconds)
        {
            CompleteExpedition();
        }
    }

    public void CompleteExpedition()
    {
        if (!IsActive && _elapsed <= 0f)
        {
            return;
        }

        IsActive = false;
        _elapsed = 0f;
        CompletedExpeditions++;

        List<ResourceAmount> rewards = BuildRewards(CompletedExpeditions, out DiscoveryType discovery);
        foreach (ResourceAmount reward in rewards)
        {
            _stockpile.Add(reward.Type, reward.Amount);
        }

        _settlement.AddDiscovery(discovery);
        int rescued = _settlement.TryAddResidents(1);
        if (rescued > 0)
        {
            ResidentsRescued?.Invoke(rescued);
        }

        LastReport = BuildRewardReport(rewards, discovery, rescued);
    }

    public void CompletePlayableExpedition(
        IReadOnlyList<ResourceAmount> loot,
        bool anomalyCompleted,
        bool defeated)
    {
        IsActive = false;
        _elapsed = 0f;
        CompletedExpeditions++;

        List<ResourceAmount> stored = new();
        if (loot != null)
        {
            foreach (ResourceAmount resource in loot)
            {
                int accepted = _stockpile.Add(resource.Type, resource.Amount);
                if (accepted > 0)
                {
                    stored.Add(new ResourceAmount(resource.Type, accepted));
                }
            }
        }

        DiscoveryType discovery = anomalyCompleted
            ? GetDiscoveryForExpedition(CompletedExpeditions)
            : DiscoveryType.None;
        if (discovery != DiscoveryType.None)
        {
            _settlement.AddDiscovery(discovery);
        }

        int rescued = anomalyCompleted ? _settlement.TryAddResidents(1) : 0;
        if (rescued > 0)
        {
            ResidentsRescued?.Invoke(rescued);
        }

        LastReport = BuildRewardReport(stored, discovery, rescued);
        if (defeated)
        {
            LastReport = "Раненый герой вернулся. " + LastReport;
        }
        else if (!anomalyCompleted)
        {
            LastReport += "; аномалия оставлена нетронутой";
        }
    }

    public void CancelPlayableExpedition(string reason, bool refundCost)
    {
        if (refundCost)
        {
            _stockpile.AddAll(CurrentCost);
        }

        IsActive = false;
        _elapsed = 0f;
        LastReport = reason;
    }

    private static DiscoveryType GetDiscoveryForExpedition(int expeditionNumber)
    {
        return expeditionNumber switch
        {
            1 => DiscoveryType.AncientBlueprints,
            2 => DiscoveryType.GrainSeeds,
            3 => DiscoveryType.SacredRelic,
            4 => DiscoveryType.WardStone,
            _ => DiscoveryType.SkyGlass
        };
    }

    private static List<ResourceAmount> BuildRewards(int expeditionNumber, out DiscoveryType discovery)
    {
        discovery = DiscoveryType.None;
        List<ResourceAmount> rewards = new();

        switch (expeditionNumber)
        {
            case 1:
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 12));
                rewards.Add(new ResourceAmount(ResourceType.Stone, 16));
                discovery = DiscoveryType.AncientBlueprints;
                break;
            case 2:
                rewards.Add(new ResourceAmount(ResourceType.Grain, 25));
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 8));
                discovery = DiscoveryType.GrainSeeds;
                break;
            case 3:
                rewards.Add(new ResourceAmount(ResourceType.Relic, 2));
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 10));
                discovery = DiscoveryType.SacredRelic;
                break;
            case 4:
                rewards.Add(new ResourceAmount(ResourceType.WardStone, 6));
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 12));
                discovery = DiscoveryType.WardStone;
                break;
            case 5:
                rewards.Add(new ResourceAmount(ResourceType.SkyGlass, 4));
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 15));
                discovery = DiscoveryType.SkyGlass;
                break;
            default:
                rewards.Add(new ResourceAmount(ResourceType.OldIron, 8 + expeditionNumber));
                rewards.Add(new ResourceAmount(ResourceType.Grain, 10));
                rewards.Add(new ResourceAmount(ResourceType.WardStone, 2));
                break;
        }

        return rewards;
    }

    private static string BuildRewardReport(
        IReadOnlyList<ResourceAmount> rewards,
        DiscoveryType discovery,
        int rescued)
    {
        StringBuilder builder = new("Добыча: ");
        for (int index = 0; index < rewards.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ResourceNames.GetShort(rewards[index].Type));
            builder.Append(" ");
            builder.Append(rewards[index].Amount);
        }

        if (discovery != DiscoveryType.None)
        {
            builder.Append("; найдено новое знание");
        }

        builder.Append(rescued > 0 ? "; спасён житель" : "; для выживших нет жилья");
        return builder.ToString();
    }
}
}
