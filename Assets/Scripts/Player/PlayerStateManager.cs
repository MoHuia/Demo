using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerStateManager : EntityStateManager<Player>
{
    protected override List<EntityState<Player>> GetStateList() => new List<EntityState<Player>>
    {
        new IdlePlayerState(),
        new WalkStartPlayerState(),
        new WalkPlayerState(),
        new RunPlayerState(),
        new RunEndPlayerState(),
        new DashIdlePlayerState(),
        new DashRunPlayerState(),
        new TurnBackPlayerState(),
        new NormalAttackPlayerState(),
        new NormalAttackEndPlayerState(),
        new HurtPlayerState(),
        new DodgeCounterEndPlayerState(),
        new HitFlyPlayerState(),
        new DodgeCounterPlayerState(),
        new DeadPlayerState(),
        new DodgeRushPlayerState(),
        new DodgeRushEndPlayerState()
    };
}
