using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class UnitCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 3.6f;
    [SerializeField] private float attackInterval = 0.55f;
    [SerializeField] private int attackDamage = 6;

    private float _attackTimer;

    private void Update()
    {
        _attackTimer += Time.deltaTime;
        if (_attackTimer < attackInterval)
        {
            return;
        }

        EnemyUnit target = FindNearestEnemy();
        if (target == null)
        {
            return;
        }

        _attackTimer = 0f;
        target.TakeDamage(attackDamage);
    }

    private EnemyUnit FindNearestEnemy()
    {
        EnemyUnit best = null;
        float bestDistance = attackRange * attackRange;

        foreach (EnemyUnit enemy in EnemyUnit.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            float distance = offset.sqrMagnitude;
            if (distance <= bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }

        return best;
    }
}
}
