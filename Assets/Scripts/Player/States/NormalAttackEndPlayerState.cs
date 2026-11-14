public class NormalAttackEndPlayerState : PlayerState
{
    internal int attackIndex { get; private set; }
    internal string animationState => NormalAttackPlayerState.GetEndAnimation(attackIndex);

    protected override void OnEnter(Player player)
    {
        attackIndex = player.states.last is NormalAttackPlayerState attack ? attack.attackIndex : 0;

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

        if (player.animator.IsAnimationFinished(animationState))
        {
            player.states.Change<IdlePlayerState>();
        }
    }
}
