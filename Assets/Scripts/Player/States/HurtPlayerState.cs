public class HurtPlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
        player.SetRootMotionEnabled(false);
    }

    protected override void OnExit(Player player) { }

    protected override void OnStep(Player player)
    {
        if (player.isCarriedByCharge)
        {
            return;
        }

        int dodgeFrame = player.hitReaction == PlayerHitReaction.Heavy ? 30 : 15;
        if (player.animator.CanMoveAfterAnimationTime(player.hitAnimation, dodgeFrame / 60f) &&
            player.TryStartDash())
        {
            return;
        }

        if (!player.animator.IsAnimationFinished(player.hitAnimation))
        {
            return;
        }

        if (!player.TryResumeWalk())
        {
            player.states.Change<IdlePlayerState>();
        }
    }
}
