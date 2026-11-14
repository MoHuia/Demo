using UnityEngine;

public class IdleEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy)
    {
    }

    protected override void OnExit(Enemy enemy)
    {
    }

    protected override void OnStep(Enemy enemy)
    {
        if (!enemy.TryGetChaseDirection(out Vector3 direction))
        {
            return;
        }

        if (!enemy.IsInAttackRange(direction))
        {
            enemy.states.Change<ChaseEnemyState>();
            return;
        }

        enemy.TurnToward(direction);
        if (enemy.CanStartAttack(direction) &&
            (!(enemy.states.last is AttackEnemyState) || timeSinceEntered >= enemy.recovery))
        {
            enemy.states.Change<AttackEnemyState>();
        }
    }
}
