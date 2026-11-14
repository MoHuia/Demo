public class HurtEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy) { }
    protected override void OnExit(Enemy enemy) { }

    protected override void OnStep(Enemy enemy)
    {
        if (enemy.animator.IsHitFinished())
        {
            enemy.states.Change<IdleEnemyState>();
        }
    }
}
