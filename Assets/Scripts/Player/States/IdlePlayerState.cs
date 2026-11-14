public class IdlePlayerState : PlayerState
{
    private const float k_RelaxedIdleDelay = 15f;
    private const string k_AfkAnimation = "Base Layer.IdleAFK";

    private float m_nextAfkTime;
    private bool m_afkPlaying;

    internal bool useRelaxedFraming => timeSinceEntered >= k_RelaxedIdleDelay;

    protected override void OnEnter(Player player)
    {
        m_nextAfkTime = k_RelaxedIdleDelay;
        m_afkPlaying = false;
        player.SetRootMotionEnabled(false);
    }

    protected override void OnExit(Player player) { }

    protected override void OnStep(Player player)
    {
        var direction = player.inputs.GetMovementCameraDirection(player.cameraTarget);

        if (direction.sqrMagnitude > 0.01f)
        {
            player.states.Change<WalkStartPlayerState>();

            player.FaceDirectionSmooth(direction);
            return;
        }

        if (m_afkPlaying)
        {
            if (player.animator.IsAnimationFinished(k_AfkAnimation))
            {
                m_afkPlaying = false;
                player.animator.Play("Base Layer.Idle");
            }

            return;
        }

        if (timeSinceEntered >= m_nextAfkTime)
        {
            m_nextAfkTime = timeSinceEntered + k_RelaxedIdleDelay;
            m_afkPlaying = true;
            player.animator.Play(k_AfkAnimation);
        }
    }
}
