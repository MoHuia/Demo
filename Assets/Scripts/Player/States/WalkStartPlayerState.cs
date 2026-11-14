public class WalkStartPlayerState : PlayerState
{
    protected const string k_WalkStartState = "Base Layer.WalkStart";

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
        var direction = player.inputs.GetMovementCameraDirection(
            player.cameraTarget
        );

        if (direction.sqrMagnitude <= 0.01f)
        {
            player.states.Change<RunEndPlayerState>();
            return;
        }

        player.FaceDirectionSmooth(direction);

        if (player.animator.IsAnimationFinished(k_WalkStartState))
        {
            player.states.Change<WalkPlayerState>();
        }
    }
}
