using UnityEngine;

public class DodgeRushPlayerState : PlayerState
{
    internal const string AnimationState = "Base Layer.Attack_Rush";
    private const float k_Frame = 1f / 60f;
    private bool m_shakePlayed;
    private bool m_hitChecked;
    internal Enemy attackTarget { get; private set; }

    protected override void OnEnter(Player player)
    {
        m_shakePlayed = m_hitChecked = false;
        ResetAttackBuffer();
        attackTarget = player.GetComponent<PlayerAttackHitbox>()?.FindAttackTarget(5f, 180f);
        if (attackTarget != null)
        {
            Vector3 direction = Vector3.ProjectOnPlane(attackTarget.transform.position - player.transform.position, Vector3.up);
            player.transform.rotation = Quaternion.LookRotation(direction);
        }
        player.SetRootMotionEnabled(true);
    }

    protected override void OnStep(Player player)
    {
        if (player.TryStartDash())
        {
            return;
        }

        if (!m_shakePlayed && player.animator.CanMoveAfterAnimationTime(AnimationState, 4 * k_Frame))
        {
            m_shakePlayed = true;
            player.RequestCameraShake(new CombatCameraShakeSettings
            {
                radius = 0.25f, frequency = 20f, duration = 0.6f, verticalAngle = 90f
            });
        }
        if (!m_hitChecked && player.animator.CanMoveAfterAnimationTime(AnimationState, 8 * k_Frame))
        {
            m_hitChecked = true;
            player.GetComponent<PlayerAttackHitbox>()?.CheckHit(1f, 5, 3);
        }
        if (player.isInHitStop)
        {
            return;
        }
        if (TryCancelIntoAttack(player, player.animator.CanMoveAfterAnimationTime(AnimationState, 30 * k_Frame)))
        {
            return;
        }
        if (player.animator.CanMoveAfterAnimationTime(AnimationState, 44 * k_Frame) ||
            player.animator.IsAnimationFinished(AnimationState))
        {
            player.states.Change<DodgeRushEndPlayerState>();
        }
    }

    protected override void OnExit(Player player)
    {
        attackTarget = null;
        player.SetRootMotionEnabled(false);
    }
}
