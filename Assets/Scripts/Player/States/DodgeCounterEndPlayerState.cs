public class DodgeCounterEndPlayerState : PlayerState
{
    internal const string AnimationState = "Base Layer.DodgeCounterEnd";
    protected virtual string recoveryAnimation => AnimationState;

    protected override void OnEnter(Player player)
    {
        player.SetRootMotionEnabled(true);
    }

    protected override void OnExit(Player player)
    {
        player.SetRootMotionEnabled(false);
    }

    protected override void OnStep(Player player)
    {
        if (player.TryStartDash())
        {
            return;
        }

        if (player.inputs.GetAttackDown())
        {
            player.states.Change<NormalAttackPlayerState>();
            return;
        }

        if (player.TryResumeWalk())
        {
            return;
        }

        if (player.animator.IsAnimationFinished(recoveryAnimation))
        {
            player.states.Change<IdlePlayerState>();
        }
    }
}
