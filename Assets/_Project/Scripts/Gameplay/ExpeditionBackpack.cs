using System;
using System.Collections.Generic;
using System.Text;
using Hollowwest.Economy;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class ExpeditionBackpack
{
    private readonly List<ExpeditionBackpackSlot> _slots = new();

    public ExpeditionBackpack(int width = 8, int height = 5, int stackCapacity = 10)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        StackCapacity = Mathf.Max(1, stackCapacity);
    }

    public event Action Changed;

    public int Width { get; }
    public int Height { get; }
    public int SlotCapacity => Width * Height;
    public int StackCapacity { get; }
    public int SlotsUsed => _slots.Count;
    public bool IsFull => SlotsUsed >= SlotCapacity && !HasStackSpace();
    public IReadOnlyList<ExpeditionBackpackSlot> Slots => _slots;

    public int Get(ResourceType type)
    {
        int total = 0;
        foreach (ExpeditionBackpackSlot slot in _slots)
        {
            if (slot.Type == type)
            {
                total += slot.Amount;
            }
        }

        return total;
    }

    public int TryAdd(ResourceType type, int amount)
    {
        int requested = Mathf.Max(0, amount);
        if (requested == 0)
        {
            return 0;
        }

        int remaining = requested;
        for (int index = 0; index < _slots.Count && remaining > 0; index++)
        {
            ExpeditionBackpackSlot slot = _slots[index];
            if (slot.Type != type || slot.Amount >= StackCapacity)
            {
                continue;
            }

            int added = Mathf.Min(remaining, StackCapacity - slot.Amount);
            _slots[index] = slot.WithAmount(slot.Amount + added);
            remaining -= added;
        }

        while (remaining > 0 && TryFindFreeCell(out int x, out int y))
        {
            int added = Mathf.Min(remaining, StackCapacity);
            _slots.Add(new ExpeditionBackpackSlot(type, added, x, y));
            remaining -= added;
        }

        int accepted = requested - remaining;
        if (accepted > 0)
        {
            Changed?.Invoke();
        }

        return accepted;
    }

    public IReadOnlyList<ResourceAmount> GetContents()
    {
        Dictionary<ResourceType, int> totals = new();
        foreach (ExpeditionBackpackSlot slot in _slots)
        {
            totals.TryGetValue(slot.Type, out int amount);
            totals[slot.Type] = amount + slot.Amount;
        }

        List<ResourceAmount> result = new(totals.Count);
        foreach (KeyValuePair<ResourceType, int> entry in totals)
        {
            result.Add(new ResourceAmount(entry.Key, entry.Value));
        }

        result.Sort((left, right) => left.Type.CompareTo(right.Type));
        return result;
    }

    public void LoseHalf()
    {
        IReadOnlyList<ResourceAmount> contents = GetContents();
        _slots.Clear();
        foreach (ResourceAmount resource in contents)
        {
            TryAdd(resource.Type, resource.Amount / 2);
        }

        Changed?.Invoke();
    }

    public string GetSummary()
    {
        if (_slots.Count == 0)
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
        foreach (ExpeditionBackpackSlot slot in _slots)
        {
            if (slot.Amount < StackCapacity)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindFreeCell(out int freeX, out int freeY)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool occupied = false;
                foreach (ExpeditionBackpackSlot slot in _slots)
                {
                    if (slot.X == x && slot.Y == y)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    freeX = x;
                    freeY = y;
                    return true;
                }
            }
        }

        freeX = -1;
        freeY = -1;
        return false;
    }
}

public readonly struct ExpeditionBackpackSlot
{
    public ExpeditionBackpackSlot(ResourceType type, int amount, int x, int y)
    {
        Type = type;
        Amount = amount;
        X = x;
        Y = y;
    }

    public ResourceType Type { get; }
    public int Amount { get; }
    public int X { get; }
    public int Y { get; }

    public ExpeditionBackpackSlot WithAmount(int amount)
    {
        return new ExpeditionBackpackSlot(Type, amount, X, Y);
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
