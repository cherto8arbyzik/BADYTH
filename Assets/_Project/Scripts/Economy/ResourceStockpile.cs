using UnityEngine;

namespace Hollowwest.Economy
{

public sealed class ResourceStockpile : MonoBehaviour
{
    public int Wood { get; private set; }

    public void AddWood(int amount)
    {
        Wood += Mathf.Max(0, amount);
    }
}
}
