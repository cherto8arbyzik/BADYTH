using System;

namespace Hollowwest.Economy
{

[Serializable]
public struct ResourceAmount
{
    public ResourceType Type;
    public int Amount;

    public ResourceAmount(ResourceType type, int amount)
    {
        Type = type;
        Amount = Math.Max(0, amount);
    }
}
}
