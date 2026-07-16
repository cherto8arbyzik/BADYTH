using System;
using System.Collections.Generic;
using System.Text;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionBackpack
{
    private readonly Dictionary<ResourceType, int> _contents = new();

    public ExpeditionBackpack(int slotCapacity = 8, int stackCapacity = 10)
    {
        SlotCapacity = Mathf.Max(1, slotCapacity);
        StackCapacity = Mathf.Max(1, stackCapacity);
    }

    public event Action Changed;

    public int SlotCapacity { get; }
    public int StackCapacity { get; }
    public int SlotsUsed => _contents.Count;
    public bool IsFull => SlotsUsed >= SlotCapacity && !HasStackSpace();

    public int Get(ResourceType type)
    {
        return _contents.TryGetValue(type, out int amount) ? amount : 0;
    }

    public int TryAdd(ResourceType type, int amount)
    {
        int requested = Mathf.Max(0, amount);
        if (requested == 0)
        {
            return 0;
        }

        int current = Get(type);
        if (current == 0 && SlotsUsed >= SlotCapacity)
        {
            return 0;
        }

        int accepted = Mathf.Min(requested, StackCapacity - current);
        if (accepted <= 0)
        {
            return 0;
        }

        _contents[type] = current + accepted;
        Changed?.Invoke();
        return accepted;
    }

    public IReadOnlyList<ResourceAmount> GetContents()
    {
        List<ResourceAmount> result = new(_contents.Count);
        foreach (KeyValuePair<ResourceType, int> entry in _contents)
        {
            result.Add(new ResourceAmount(entry.Key, entry.Value));
        }

        result.Sort((left, right) => left.Type.CompareTo(right.Type));
        return result;
    }

    public void LoseHalf()
    {
        List<ResourceType> empty = new();
        List<ResourceType> keys = new(_contents.Keys);
        foreach (ResourceType type in keys)
        {
            int remaining = _contents[type] / 2;
            if (remaining <= 0)
            {
                empty.Add(type);
            }
            else
            {
                _contents[type] = remaining;
            }
        }

        foreach (ResourceType type in empty)
        {
            _contents.Remove(type);
        }

        Changed?.Invoke();
    }

    public string GetSummary()
    {
        if (_contents.Count == 0)
        {
            return "рюкзак пуст";
        }

        StringBuilder builder = new();
        foreach (ResourceAmount resource in GetContents())
        {
            if (builder.Length > 0)
            {
                builder.Append("   ");
            }

            builder.Append(ResourceNames.GetShort(resource.Type));
            builder.Append(' ');
            builder.Append(resource.Amount);
        }

        return builder.ToString();
    }

    private bool HasStackSpace()
    {
        foreach (int amount in _contents.Values)
        {
            if (amount < StackCapacity)
            {
                return true;
            }
        }

        return false;
    }
}

public interface IExpeditionInteractable
{
    Vector3 InteractionPosition { get; }
    string Prompt { get; }
    bool CanInteract(ExpeditionHeroController hero);
    void Interact(ExpeditionHeroController hero);
}

}
