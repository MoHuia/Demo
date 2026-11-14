using UnityEngine;

public abstract class DodgePlayerState : PlayerState
{
    private const float k_Frame = 1f / 60f;
    private const int k_RushStartFrame = 4;
    private bool m_attackBuffered;
    private Enemy m_perfectDodgeTarget;
    private bool m_confirmed;
    private Bounds m_startBounds;

    protected abstract int invulnerableFrames { get; }
    protected abstract int recoveryFrame { get; }

    protected virtual bool CanReturnToIdle(Player player) => true;

    public override bool isInvulnerable => timeSinceEntered < invulnerableFrames * k_Frame;

    protected override void OnEnter(Player player)
    {
        m_attackBuffered = false;
        m_perfectDodgeTarget = null;
        m_confirmed = false;
        m_startBounds = player.controller.bounds;
        player.SetRootMotionEnabled(true);
    }

    internal bool TryGetDodgeOrigin(out Bounds bounds)
    {
        bounds = m_startBounds;
        return isInvulnerable && !m_confirmed;
    }

    internal void BufferAttack() => m_attackBuffered = true;

    internal void ConfirmPerfectDodge(Player player, Enemy attacker)
    {
        if (!isInvulnerable || m_confirmed || attacker == null || attacker.health.currentHealth <= 0)
        {
            return;
        }

        m_confirmed = true;
        m_perfectDodgeTarget = attacker;
        attacker.StartPerfectDodgeSlow();
        player.RecordPerfectDodge();
        Debug.Log("极限闪避", player);
    }

    protected override void OnExit(Player player)
    {
        m_perfectDodgeTarget = null;
        player.SetRootMotionEnabled(false);
    }

    protected override void OnStep(Player player)
    {
        bool attackWasBuffered = m_attackBuffered;
        if (player.inputs.GetAttackDown())
        {
            BufferAttack();
        }

        if (!isInvulnerable && player.TryStartDash())
        {
            return;
        }

        bool inRecoveryWindow = timeSinceEntered < recoveryFrame * k_Frame;

        if (m_attackBuffered && (inRecoveryWindow || attackWasBuffered) &&
            m_perfectDodgeTarget != null && m_perfectDodgeTarget.health.currentHealth > 0)
        {
            player.StartDodgeCounter(m_perfectDodgeTarget);
            return;
        }

        if (inRecoveryWindow)
        {
            if (m_attackBuffered && timeSinceEntered >= k_RushStartFrame * k_Frame &&
                     (!isInvulnerable || !player.HasPendingDodgeProbe(m_startBounds, invulnerableFrames * k_Frame - timeSinceEntered)))
            {
                player.states.Change<DodgeRushPlayerState>();
            }
            return;
        }

        if (m_attackBuffered)
        {
            player.states.Change<NormalAttackPlayerState>();
            return;
        }

        Vector3 direction = player.inputs.GetMovementCameraDirection(player.cameraTarget);
        if (direction.sqrMagnitude > 0.01f)
        {
            ResumeMovement(player);
            player.FaceDirectionSmooth(direction);
        }
        else if (CanReturnToIdle(player))
        {
            player.states.Change<IdlePlayerState>();
        }
    }

    protected abstract void ResumeMovement(Player player);
}
