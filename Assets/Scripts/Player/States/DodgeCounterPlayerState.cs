using UnityEngine;

public class DodgeCounterPlayerState : PlayerState
{
    internal const string AnimationState = "Base Layer.DodgeCounter";

    private const float k_Frame = 1f / 60f;
    private static readonly int[] k_HitFrames = { 10, 12, 14 };
    private static readonly int[] k_TargetStopFrames = { 2, 2, 7 };
    private static readonly int[] k_PlayerStopFrames = { 4, 4, 6 };

    private const float k_DamageShare = 1f / 3f;
    private int m_hitIndex;
    private bool m_shakePlayed;

    internal Enemy attackTarget { get; private set; }

    internal bool canAssistApproach => m_hitIndex == 0;
    public override bool isInvulnerable => timeSinceEntered < 22 * k_Frame;

    protected override void OnEnter(Player player)
    {
        m_hitIndex = 0;
        m_shakePlayed = false;
        ResetAttackBuffer();

        attackTarget = player.dodgeCounterTarget;
        var target = attackTarget;

        if (target != null && target.health.currentHealth > 0)
        {
            target.EndPerfectDodgeSlow();
            Vector3 direction = target.transform.position - player.transform.position;

            direction.y = 0f;
            if (direction.sqrMagnitude > 1f / 10_000)
            {
                player.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        player.SetRootMotionEnabled(true);
    }

    protected override void OnStep(Player player)
    {
        if (!m_shakePlayed && player.animator.CanMoveAfterAnimationTime(AnimationState, 11 * k_Frame))
        {
            m_shakePlayed = true;
            player.RequestCameraShake(new CombatCameraShakeSettings
            {
                radius = 0.15f, frequency = 20f, duration = 0.6f, verticalAngle = 90f
            });
        }

        while (m_hitIndex < k_HitFrames.Length &&
               player.animator.CanMoveAfterAnimationTime(AnimationState, k_HitFrames[m_hitIndex] * k_Frame))
        {
            int hit = m_hitIndex++;
            player.GetComponent<PlayerAttackHitbox>()?.CheckHit(k_DamageShare, k_TargetStopFrames[hit], k_PlayerStopFrames[hit], hit);
            if (player.isInHitStop)
            {
                return;
            }
        }

        if (player.animator.CanMoveAfterAnimationTime(AnimationState, 16 * k_Frame) && player.TryStartDash())
        {
            return;
        }

        bool canCancel = m_hitIndex == k_HitFrames.Length &&
            player.animator.CanMoveAfterAnimationTime(AnimationState, 0.45f);
        if (TryCancelIntoAttack(player, canCancel))
        {
            return;
        }

        if (!player.animator.CanMoveAfterAnimationTime(AnimationState, 70 * k_Frame) &&
            !player.animator.IsAnimationFinished(AnimationState))
        {
            return;
        }

        player.states.Change<DodgeCounterEndPlayerState>();
    }

    protected override void OnExit(Player player)
    {
        attackTarget = null;
        player.SetRootMotionEnabled(false);
    }
}
