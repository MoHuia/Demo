public class DeadEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy)
    {
        enemy.EndHitStop();
        enemy.EndPerfectDodgeSlow();

        enemy.StopMoving();
    }

    protected override void OnStep(Enemy enemy) { }

    protected override void OnExit(Enemy enemy)
    {
    }
}
