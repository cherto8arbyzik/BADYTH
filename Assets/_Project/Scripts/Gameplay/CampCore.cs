using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class CampCore : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDestroyed => Health <= 0;
    public Vector3 RallyPoint => transform.position;

    private void Awake()
    {
        Health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDestroyed)
        {
            return;
        }

        Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
    }
}
}
