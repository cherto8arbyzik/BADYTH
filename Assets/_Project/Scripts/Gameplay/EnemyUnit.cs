using System.Collections.Generic;
using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class EnemyUnit : MonoBehaviour
{
    private static readonly List<EnemyUnit> ActiveEnemiesInternal = new();

    [SerializeField] private int maxHealth = 18;
    [SerializeField] private float attackRange = 1.05f;
    [SerializeField] private float attackInterval = 0.8f;
    [SerializeField] private int attackDamage = 4;

    private NavigationAgent _agent;
    private CampCore _target;
    private int _health;
    private float _attackTimer;

    public static IReadOnlyList<EnemyUnit> ActiveEnemies => ActiveEnemiesInternal;
    public bool IsAlive => _health > 0;

    public void Initialize(NavigationAgent agent, CampCore target)
    {
        _agent = agent;
        _target = target;
        _health = maxHealth;

        if (_agent != null && _target != null)
        {
            _agent.SetDestination(_target.RallyPoint);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        _health = Mathf.Max(0, _health - Mathf.Max(0, amount));
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (!ActiveEnemiesInternal.Contains(this))
        {
            ActiveEnemiesInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveEnemiesInternal.Remove(this);
    }

    private void Update()
    {
        if (_target == null || _target.IsDestroyed || !IsAlive)
        {
            return;
        }

        Vector3 offset = _target.RallyPoint - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude > attackRange * attackRange)
        {
            return;
        }

        if (_agent != null)
        {
            _agent.Stop();
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer < attackInterval)
        {
            return;
        }

        _attackTimer = 0f;
        _target.TakeDamage(attackDamage);
    }
}
}
