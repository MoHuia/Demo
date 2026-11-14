public class DashIdlePlayerState : DodgePlayerState
{
    internal const string AnimationState = "Base Layer.DashIdle";

    protected override int invulnerableFrames => 28;
    protected override int recoveryFrame => 34;

    protected override bool CanReturnToIdle(Player player) =>
        player.animator.IsAnimationFinished(AnimationState);

    protected override void ResumeMovement(Player player) => player.states.Change<WalkStartPlayerState>();
}
