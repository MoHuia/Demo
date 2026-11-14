public class HitFlyPlayerState : PlayerState
{
    private const float k_RecoveryTime = 70f / 60f;

    protected override void OnEnter(Player player)
    {
        ResetAttackBuffer();

        player.SetRootMotionEnabled(true);
        player.BeginHitFly();
    }

    protected override void OnStep(Player player)
    {
        bool grounded = player.controller.isGrounded && !player.isCarriedByCharge;
        bool canRecover = grounded &&
            player.animator.CanMoveAfterAnimationTime(player.hitAnimation, k_RecoveryTime);

        if (canRecover && player.TryStartDash())
        {
            return;
        }

        if (TryCancelIntoAttack(player, canRecover, allowHeldAttack: true))
        {
            return;
        }

        if (canRecover && player.TryResumeWalk(useWalkStart: true))
        {
            return;
        }

        if (grounded && player.animator.IsAnimationFinished(player.hitAnimation))
        {
            player.states.Change<IdlePlayerState>();
        }
    }

    protected override void OnExit(Player player)
    {
        player.SetRootMotionEnabled(false);
    }
}
