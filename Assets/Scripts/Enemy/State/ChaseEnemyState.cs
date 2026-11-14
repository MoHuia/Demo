using UnityEngine;

public class ChaseEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy) { }

    protected override void OnStep(Enemy enemy)
    {
        if (!enemy.TryGetChaseDirection(out Vector3 direction))
        {
            enemy.states.Change<IdleEnemyState>();
            return;
        }

        enemy.TurnToward(direction);
        if (enemy.CanStartAttack(direction) &&
            (!(enemy.states.last is AttackEnemyState) || timeSinceEntered >= enemy.recovery))
        {
            enemy.states.Change<AttackEnemyState>();
            return;
        }

        enemy.AdvanceToward(direction);
    }

    protected override void OnExit(Enemy enemy) => enemy.StopMoving();
}
