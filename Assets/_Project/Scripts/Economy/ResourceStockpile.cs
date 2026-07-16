using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class ResourceStockpile : MonoBehaviour
{
    private readonly Dictionary<ResourceType, int> _amounts = new();

    [SerializeField] private int storageCapacity = 300;

    public event Action Changed;

    public int StorageCapacity => storageCapacity;
    public int UsedCapacity { get; private set; }
    public int Wood => Get(ResourceType.Timber);

    public void Initialize(int capacity, IReadOnlyList<ResourceAmount> startingResources)
    {
        _amounts.Clear();
        UsedCapacity = 0;
        storageCapacity = Mathf.Max(0, capacity);

        if (startingResources != null)
        {
            foreach (ResourceAmount resource in startingResources)
            {
                Add(resource.Type, resource.Amount);
            }
        }

        Changed?.Invoke();
    }

    public int Get(ResourceType type)
    {
        return _amounts.TryGetValue(type, out int amount) ? amount : 0;
    }

    public int Add(ResourceType type, int amount)
    {
        int requested = Mathf.Max(0, amount);
        int accepted = Mathf.Min(requested, Mathf.Max(0, storageCapacity - UsedCapacity));
        if (accepted == 0)
        {
            return 0;
        }

        _amounts[type] = Get(type) + accepted;
        UsedCapacity += accepted;
        Changed?.Invoke();
        return accepted;
    }

    public void AddWood(int amount)
    {
        Add(ResourceType.Timber, amount);
    }

    public bool TrySpendWood(int amount)
    {
        return TrySpend(ResourceType.Timber, amount);
    }

    public bool Has(IReadOnlyList<ResourceAmount> costs)
    {
        if (costs == null)
        {
            return true;
        }

        foreach (ResourceAmount cost in costs)
        {
            if (Get(cost.Type) < Mathf.Max(0, cost.Amount))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanAccept(IReadOnlyList<ResourceAmount> resources)
    {
        if (resources == null)
        {
            return true;
        }

        int requiredCapacity = 0;
        foreach (ResourceAmount resource in resources)
        {
            requiredCapacity += Mathf.Max(0, resource.Amount);
        }

        return UsedCapacity + requiredCapacity <= storageCapacity;
    }

    public bool AddAll(IReadOnlyList<ResourceAmount> resources)
    {
        if (!CanAccept(resources))
        {
            return false;
        }

        if (resources != null)
        {
            foreach (ResourceAmount resource in resources)
            {
                int amount = Mathf.Max(0, resource.Amount);
                _amounts[resource.Type] = Get(resource.Type) + amount;
                UsedCapacity += amount;
            }
        }

        Changed?.Invoke();
        return true;
    }

    public bool TrySpend(IReadOnlyList<ResourceAmount> costs)
    {
        if (!Has(costs))
        {
            return false;
        }

        if (costs != null)
        {
            foreach (ResourceAmount cost in costs)
            {
                SpendUnchecked(cost.Type, cost.Amount);
            }
        }

        Changed?.Invoke();
        return true;
    }

    public bool TrySpend(ResourceType type, int amount)
    {
        int cost = Mathf.Max(0, amount);
        if (Get(type) < cost)
        {
            return false;
        }

        SpendUnchecked(type, cost);
        Changed?.Invoke();
        return true;
    }

    public void SetStorageCapacity(int capacity)
    {
        int adjustedCapacity = Mathf.Max(UsedCapacity, capacity);
        if (storageCapacity == adjustedCapacity)
        {
            return;
        }

        storageCapacity = adjustedCapacity;
        Changed?.Invoke();
    }

    public string GetMissingSummary(IReadOnlyList<ResourceAmount> costs)
    {
        if (costs == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach (ResourceAmount cost in costs)
        {
            int missing = Mathf.Max(0, cost.Amount - Get(cost.Type));
            if (missing == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(ResourceNames.Get(cost.Type));
            builder.Append(" ");
            builder.Append(missing);
        }

        return builder.ToString();
    }

    private void SpendUnchecked(ResourceType type, int amount)
    {
        int spent = Mathf.Min(Get(type), Mathf.Max(0, amount));
        int remaining = Get(type) - spent;
        if (remaining == 0)
        {
            _amounts.Remove(type);
        }
        else
        {
            _amounts[type] = remaining;
        }

        UsedCapacity = Mathf.Max(0, UsedCapacity - spent);
    }
}
}
