public class DeadPlayerState : PlayerState
{
    internal const string AnimationState = "Base Layer.Death";

    protected override void OnEnter(Player player)
    {
        player.SetRootMotionEnabled(false);
    }

    protected override void OnStep(Player player) { }

    protected override void OnExit(Player player) { }
}
