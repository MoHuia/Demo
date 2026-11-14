public class WalkPlayerState : PlayerState
{
    private const float k_RunAfterMoveTime = 3f;
    private float m_walkStartTime;

    protected override void OnEnter(Player player)
    {
        m_walkStartTime = player.states.last is WalkStartPlayerState start
            ? start.timeSinceEntered
            : 0f;
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

        if (m_walkStartTime + timeSinceEntered >= k_RunAfterMoveTime)
        {
            player.states.Change<RunPlayerState>();
        }
    }
}
