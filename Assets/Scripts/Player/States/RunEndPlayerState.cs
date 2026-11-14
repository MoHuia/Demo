using UnityEngine;

public class RunEndPlayerState : PlayerState
{
    protected const string k_RunEndState = "Base Layer.RunEnd";

    protected const float k_ReverseInputWindow = 0.15f;

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
        var direction = player.inputs.GetMovementCameraDirection(player.cameraTarget);

        if (direction.sqrMagnitude > 0.01f)
        {
            if (player.states.last is RunPlayerState && timeSinceEntered <= k_ReverseInputWindow &&
                Vector3.Angle(player.transform.forward, direction) >= RunPlayerState.TurnBackAngle)
            {
                player.states.Change<TurnBackPlayerState>();
                return;
            }

            player.states.Change<WalkStartPlayerState>();

            player.FaceDirectionSmooth(direction);
            return;
        }

        if (player.animator.IsAnimationFinished(k_RunEndState))
        {
            player.states.Change<IdlePlayerState>();
        }
    }
}
