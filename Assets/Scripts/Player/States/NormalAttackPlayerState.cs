using UnityEngine;

public class NormalAttackPlayerState : PlayerState
{
    private const float k_Frame = 1f / 60f;

    private readonly struct AttackHit
    {
        public readonly int frame;
        public readonly int targetStopFrames;
        public readonly int attackerStopFrames;
        public readonly float shakeAngle;
        public readonly float actionShakeRadius;
        public readonly float hitCylinderRadius;
        public readonly float hitCylinderForward;

        public AttackHit(int frame, int targetStopFrames, int attackerStopFrames, float shakeAngle,
            float actionShakeRadius = 0f, float hitCylinderRadius = 0f, float hitCylinderForward = 0f)
        {
            this.frame = frame;
            this.targetStopFrames = targetStopFrames;
            this.attackerStopFrames = attackerStopFrames;
            this.shakeAngle = shakeAngle;
            this.actionShakeRadius = actionShakeRadius;
            this.hitCylinderRadius = hitCylinderRadius;
            this.hitCylinderForward = hitCylinderForward;
        }
    }

    private static readonly (string StateName, string EndStateName, float ChainTime, AttackHit[] Hits)[] k_Attacks =
    {
        ("Base Layer.Attack01", "Base Layer.Attack01End", 0.4f, new[] { new AttackHit(12, 5, 3, 60f) }),
        ("Base Layer.Attack02", "Base Layer.Attack02End", 0.4f, new[] { new AttackHit(10, 5, 3, 165f) }),
        ("Base Layer.Attack03", "Base Layer.Attack03End", 0.4f, new[]
        {
            new AttackHit(8, 3, 5, 135f, 0.15f),
            new AttackHit(10, 5, 3, 135f)
        }),
        ("Base Layer.Attack04", "Base Layer.Attack04End", 0.4f, new[]
        {
            new AttackHit(10, 3, 5, 90f),
            new AttackHit(19, 3, 5, 180f),
            new AttackHit(43, 7, 6, 270f, 0.25f, hitCylinderRadius: 2f, hitCylinderForward: 0.5f)
        }),
        ("Base Layer.Attack05", "Base Layer.Attack05End", 0.8f, new[]
        {
            new AttackHit(9, 3, 5, 90f),
            new AttackHit(14, 3, 5, 90f),
            new AttackHit(35, 2, 4, 270f),
            new AttackHit(38, 7, 6, 270f, 0.25f, hitCylinderRadius: 2.3f)
        })
    };

    private int m_attackIndex;

    private bool m_nextAttackQueued;
    private int m_nextHitIndex;
    internal int attackIndex => m_attackIndex;
    internal string animationState => k_Attacks[m_attackIndex].StateName;
    internal Enemy attackTarget { get; private set; }

    internal static string GetEndAnimation(int index) => k_Attacks[index].EndStateName;

    internal void BufferNextAttack() => m_nextAttackQueued = true;

    private static int GetNextAttackIndex(int index) => (index + 1) % k_Attacks.Length;

    private void AimAtTarget(Player player)
    {
        attackTarget = player.GetComponent<PlayerAttackHitbox>()?.FindAttackTarget(20f, 180f);
        if (attackTarget == null)
        {
            return;
        }

        Vector3 offset = attackTarget.transform.position - player.transform.position;
        offset.y = 0f;
        player.transform.rotation = Quaternion.LookRotation(offset);
    }

    protected override void OnEnter(Player player)
    {
        m_nextHitIndex = 0;

        m_attackIndex = 0;
        m_nextAttackQueued = false;
        player.SetRootMotionEnabled(true);
        AimAtTarget(player);
    }

    protected override void OnExit(Player player)
    {
        player.SetRootMotionEnabled(false);
    }

    protected override void OnStep(Player player)
    {
        var attack = k_Attacks[m_attackIndex];

        if (player.inputs.GetAttackDown())
        {
            BufferNextAttack();
        }

        int hitIndexBeforeStep = m_nextHitIndex;

        while (m_nextHitIndex < attack.Hits.Length &&
            player.animator.CanMoveAfterAnimationTime(attack.StateName, attack.Hits[m_nextHitIndex].frame * k_Frame))
        {
            int hitIndex = m_nextHitIndex++;
            AttackHit hit = attack.Hits[hitIndex];

            if (hit.actionShakeRadius > 0f)
            {
                player.RequestCameraShake(new CombatCameraShakeSettings
                {
                    radius = hit.actionShakeRadius,
                    frequency = 20f,
                    duration = 0.6f,
                    verticalAngle = hit.shakeAngle
                });
            }

            player.GetComponent<PlayerAttackHitbox>()?.CheckNormalHit(
                hitIndex, attack.Hits.Length, hit.targetStopFrames, hit.attackerStopFrames, hit.shakeAngle,
                hit.hitCylinderRadius, hit.hitCylinderForward);
            if (player.isInHitStop)
            {
                return;
            }
        }

        if (player.TryStartDash())
        {
            return;
        }

        if (m_nextAttackQueued && m_nextHitIndex == attack.Hits.Length &&
            m_nextHitIndex == hitIndexBeforeStep &&
            player.animator.CanMoveAfterAnimationTime(attack.StateName, attack.ChainTime))
        {
            m_nextAttackQueued = false;
            m_attackIndex = GetNextAttackIndex(m_attackIndex);

            m_nextHitIndex = 0;
            AimAtTarget(player);

            player.animator.Play(animationState);
            return;
        }

        if (!player.animator.IsAnimationFinished(attack.StateName))
        {
            return;
        }

        if (!player.TryResumeWalk())
        {
            player.states.Change<NormalAttackEndPlayerState>();
        }
    }
}
