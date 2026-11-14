using System.Collections.Generic;

public class EnemyStateManager : EntityStateManager<Enemy>
{
    protected override List<EntityState<Enemy>> GetStateList()
    {
        return new List<EntityState<Enemy>>
        {
            new IdleEnemyState(),
            new HurtEnemyState(),
            new DeadEnemyState(),
            new AttackEnemyState(),
            new ChaseEnemyState()
        };
    }
}
