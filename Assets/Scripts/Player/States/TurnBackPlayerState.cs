public class TurnBackPlayerState : PlayerState
{
    protected const string k_TurnBackState = "Base Layer.TurnBack";

    protected const float k_MoveCancelTime = 0.6f;

    protected override void OnEnter(Player player)
    {
        ResetAttackBuffer();

        player.SetRootMotionEnabled(true, true);
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

        bool canCancel = player.animator.CanMoveAfterAnimationTime(k_TurnBackState, k_MoveCancelTime);
        if (TryCancelIntoAttack(player, canCancel) || !canCancel)
        {
            return;
        }

        var direction = player.inputs.GetMovementCameraDirection(player.cameraTarget);

        if(direction.sqrMagnitude > 0.01f)
        {
            player.states.Change<RunPlayerState>();
            return;
        }

        player.states.Change<RunEndPlayerState>();
    }
}
