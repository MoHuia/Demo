using UnityEngine;

public class RunPlayerState : PlayerState
{
    internal const float TurnBackAngle = 145f;

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

        if (direction.sqrMagnitude <= 0.01f)
        {
            player.states.Change<RunEndPlayerState>();
            return;
        }

        float angle = Vector3.Angle(player.transform.forward, direction);

        if (angle >= TurnBackAngle)
        {
            player.states.Change<TurnBackPlayerState>();
            return;
        }

        player.FaceDirectionSmooth(direction);
    }
}
