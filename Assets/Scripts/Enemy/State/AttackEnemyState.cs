using UnityEngine;

public class AttackEnemyState : EnemyState
{
    private const float k_FramesPerSecond = 60f;
    private const int k_ChargeCheckStartFrame = 88;
    private const int k_ChargeCheckEndFrame = 166;
    private const int k_HitTargetStartFrame = 167;
    private EnemyAttackHitbox m_hitbox;
    private EnemyAttackHitbox.Attack m_attack;
    private int m_actionShakeIndex;
    private EnemyAttackHitbox.HitWindow[] m_hits;
    private int m_nextAttack;
    private int m_hitIndex;
    private bool m_sampledWindow;
    private Vector3 m_previousCenter;
    private Vector3 m_previousSecondCenter;
    private bool m_playing;
    private float m_aimUntil;
    private bool m_chargeBranched;
    private bool m_chargeHitWall;
    private bool m_waitingForWallEnd;
    private bool m_hitTargetBranch;
    private bool m_hasLandedLightHit;
    private Player m_aimTarget;
    private int m_detectionIndex;

    internal EnemyAttackHitbox.HitWindow activeHit { get; private set; }
    internal EnemyAttackHitbox.HitWindow nextHit => m_hits != null && m_hitIndex < m_hits.Length
        ? m_hits[m_hitIndex] : null;
    internal EnemyAttackHitbox.DetectionWindow activeDetection { get; private set; }
    internal bool detectedTarget { get; private set; }
    internal Player chargeTarget { get; private set; }
    internal bool isCharging => !m_chargeBranched && !string.IsNullOrEmpty(m_attack?.hitTargetState);

    private bool UsesDodgeProbes => m_attack?.detections?.Length > 0;

    protected override void OnEnter(Enemy enemy)
    {
        m_hitbox = enemy.GetComponent<EnemyAttackHitbox>();
        m_attack = m_hitbox?.GetAttack(ref m_nextAttack);
        m_actionShakeIndex = 0;
        m_hits = m_attack?.hits;
        m_hitIndex = 0;
        m_detectionIndex = 0;
        m_aimTarget = enemy.target;
        activeDetection = null;
        detectedTarget = false;
        m_chargeBranched = false;
        m_chargeHitWall = false;
        m_waitingForWallEnd = false;
        m_hitTargetBranch = false;
        m_hasLandedLightHit = false;
        m_sampledWindow = false;
        m_aimUntil = m_hits != null && m_hits.Length > 0 ? Mathf.Max(0f, GetHitTime(m_hits[0]) - 0.22f) : 0f;
        if (m_attack != null && m_attack.aimUntilFrame > 0)
        {
            m_aimUntil = m_attack.aimUntilFrame / k_FramesPerSecond;
        }
        if (isCharging)
        {
            m_aimUntil = 60f / k_FramesPerSecond;
        }
        m_playing = enemy.animator.PlayAttack(m_attack?.stateName ?? "Attack01");
    }

    protected override void OnStep(Enemy enemy)
    {
        if (m_playing && !enemy.animator.IsAttackFinished())
        {
            if (!enemy.animator.TryGetAttackTime(out float time))
            {
                return;
            }

            bool canApproach = time < m_aimUntil && (!m_chargeBranched || m_hitTargetBranch);
            float turnScale = GetAimSpeed(time, canApproach);
            if ((canApproach || turnScale > 0f) && enemy.target == m_aimTarget)
            {
                if (enemy.TryGetChaseDirection(out Vector3 direction))
                {
                    if (turnScale > 0f)
                    {
                        enemy.TurnToward(direction, turnScale);
                    }
                    if (canApproach && m_attack?.aimInPlace != true)
                    {
                        enemy.CloseAttackGap(direction);
                    }
                }
                else if (canApproach && m_attack?.aimInPlace == true)
                {
                    FinishAttack(enemy);
                    return;
                }
            }

            if (m_attack == null || m_attack.finishFrame == 0 ||
                time < m_attack.finishFrame / k_FramesPerSecond)
            {
                return;
            }
        }

        FinishAttack(enemy);
    }

    protected override void OnExit(Enemy enemy)
    {
        ReleaseChargeTarget(enemy);
        activeHit = null;
        activeDetection = null;
        detectedTarget = false;
    }

    private float GetAimSpeed(float time, bool canApproach)
    {
        if (m_chargeBranched && !m_hitTargetBranch)
        {
            return 0f;
        }
        var windows = m_attack?.aimWindows;
        if (windows == null || windows.Length == 0)
        {
            return canApproach ? 1f : 0f;
        }

        float frame = time * k_FramesPerSecond;
        float speed = 0f;
        foreach (var window in windows)
        {
            speed = Mathf.Max(speed, window.GetSpeed(frame));
        }
        return speed;
    }

    private void FinishAttack(Enemy enemy)
    {
        if (m_waitingForWallEnd)
        {
            m_waitingForWallEnd = false;
            m_playing = enemy.animator.PlayAttack(m_attack.hitWallEndState, 0f, 0.08f);
        }
        else if (enemy.TryGetChaseDirection(out Vector3 separation) && !enemy.IsInAttackRange(separation))
        {
            enemy.states.Change<ChaseEnemyState>();
        }
        else
        {
            enemy.states.Change<IdleEnemyState>();
        }
    }

    internal void CheckContact(Enemy enemy)
    {
        activeHit = null;
        var target = enemy.target;
        if (!enemy.animator.TryGetAttackTime(out float time, out float progress))
        {
            return;
        }

        UpdateDetection(enemy, target, time);

        if (TryBranchCharge(enemy, target, time, progress) || target == null)
        {
            return;
        }

        var actionShakes = m_attack?.actionShakes;
        while (actionShakes != null && m_actionShakeIndex < actionShakes.Length &&
               time >= actionShakes[m_actionShakeIndex].frame / k_FramesPerSecond)
        {
            target.RequestCameraShake(actionShakes[m_actionShakeIndex++].settings);
        }

        if (m_hits == null || !target.isAlive || enemy.health.currentHealth == 0)
        {
            return;
        }

        while (m_hitIndex < m_hits.Length)
        {
            var hit = m_hits[m_hitIndex];
            if (time < GetHitTime(hit))
            {
                return;
            }

            if (hit.attackFrame > 0)
            {
                m_hitIndex++;
                activeHit = hit;
                bool contacted = m_hitbox.CheckCylinderContact(target, hit);
                if (contacted)
                {
                    ApplyHit(enemy, target, hit);
                }
                else if (!UsesDodgeProbes && target.states.current is DodgePlayerState dodge &&
                         dodge.TryGetDodgeOrigin(out Bounds origin) && m_hitbox.CheckCylinderContact(origin, hit))
                {
                    dodge.ConfirmPerfectDodge(target, enemy);
                }
                if (hit.launch)
                {
                    ReleaseChargeTarget(enemy);
                }
                if (contacted && (!target.isAlive || enemy.isInHitStop))
                {
                    return;
                }
                continue;
            }

            if (time < hit.start + hit.duration)
            {
                CheckSwing(enemy, target, hit);
                return;
            }
            m_hitIndex++;
            m_sampledWindow = false;
        }
    }

    private static float GetHitTime(EnemyAttackHitbox.HitWindow hit) =>
        hit.attackFrame > 0 ? hit.attackFrame / k_FramesPerSecond : hit.start;

    private void CheckSwing(Enemy enemy, Player target, EnemyAttackHitbox.HitWindow hit)
    {
        activeHit = hit;
        Vector3 center = m_hitbox.GetCenter(hit, hit.point);
        Vector3 secondCenter = m_hitbox.GetCenter(hit, hit.secondPoint);
        bool contact = m_hitbox.CheckContact(target, m_sampledWindow ? m_previousCenter : center, center, hit.radius) ||
            (hit.secondPoint != null && m_hitbox.CheckContact(target,
                m_sampledWindow ? m_previousSecondCenter : secondCenter, secondCenter, hit.radius));

        if (!contact && !UsesDodgeProbes &&
            target.states.current is DodgePlayerState dodge && dodge.TryGetDodgeOrigin(out Bounds origin) &&
            (EnemyAttackHitbox.CheckDodgeOrigin(origin, m_sampledWindow ? m_previousCenter : center, center, hit.radius) ||
             (hit.secondPoint != null && EnemyAttackHitbox.CheckDodgeOrigin(origin,
                 m_sampledWindow ? m_previousSecondCenter : secondCenter, secondCenter, hit.radius))))
        {
            dodge.ConfirmPerfectDodge(target, enemy);
        }

        m_previousCenter = center;
        m_previousSecondCenter = secondCenter;
        m_sampledWindow = true;

        if (contact)
        {
            m_hitIndex++;
            m_sampledWindow = false;
            ApplyHit(enemy, target, hit);
        }
    }

    private void ApplyHit(Enemy enemy, Player target, EnemyAttackHitbox.HitWindow hit)
    {
        bool lightHit = hit.reaction == PlayerHitReaction.Light;
        int stopFrames = lightHit && m_hasLandedLightHit && hit.followUpHitStopFrames > 0
            ? hit.followUpHitStopFrames
            : hit.hitStopFrames;
        if (target.TryApplyDamage(hit.damage, enemy, hit.reaction, stopFrames / k_FramesPerSecond,
                allowHitBasedDodge: !UsesDodgeProbes))
        {
            enemy.animator.PlayAttackImpact(hit.impactAudio);
            m_hasLandedLightHit |= lightHit;

            var actionShakes = m_attack?.actionShakes;
            if (hit.attackFrame == 0 && actionShakes != null && m_actionShakeIndex < actionShakes.Length &&
                actionShakes[m_actionShakeIndex].frame / k_FramesPerSecond < hit.start + hit.duration)
            {
                target.RequestCameraShake(actionShakes[m_actionShakeIndex++].settings);
            }

            if (m_hitTargetBranch && !hit.launch && target.isAlive && chargeTarget == null)
            {
                chargeTarget = target;
                target.BeginChargeCarry(enemy);
            }
            target.RequestCameraShake(hit.hitShake);
        }
    }

    internal bool HasPendingDodgeProbe(Enemy enemy, Bounds origin, float remainingDodgeTime)
    {
        if (!UsesDodgeProbes || m_hits == null || m_chargeHitWall || enemy.health.currentHealth <= 0 ||
            !enemy.animator.TryGetAttackTime(out float time))
        {
            return false;
        }

        float lastProbeStart = time + remainingDodgeTime;
        foreach (var probe in m_attack.detections)
        {
            float start = probe.frame / k_FramesPerSecond;
            if (start > lastProbeStart)
            {
                break;
            }
            if (time < start + probe.duration && CanCheckDodgeProbe(probe) &&
                CheckDodgeProbe(enemy, probe, origin))
            {
                return true;
            }
        }
        return false;
    }

    private bool CanCheckDodgeProbe(EnemyAttackHitbox.DetectionWindow probe) =>
        !m_hitTargetBranch || probe.frame != 76;

    private bool CheckDodgeProbe(Enemy enemy, EnemyAttackHitbox.DetectionWindow probe, Bounds bounds) =>
        m_hitbox.CheckSectorContact(bounds, enemy.transform.TransformPoint(probe.offset),
            probe.radius, probe.height, probe.angle);

    private void UpdateDetection(Enemy enemy, Player target, float time)
    {
        activeDetection = null;
        detectedTarget = false;
        var detections = m_attack?.detections;
        if (detections == null || m_hits == null || m_chargeHitWall || enemy.health.currentHealth <= 0 ||
            target == null || !target.isAlive || !target.controller.enabled)
        {
            return;
        }

        while (m_detectionIndex < detections.Length &&
               time >= detections[m_detectionIndex].frame / k_FramesPerSecond)
        {
            m_detectionIndex++;
        }
        var dodge = target.states.current as DodgePlayerState;
        Bounds origin = default;
        bool hasDummy = dodge != null && dodge.TryGetDodgeOrigin(out origin);
        for (int i = m_detectionIndex - 1; i >= 0; i--)
        {
            var probe = detections[i];
            if (time >= probe.frame / k_FramesPerSecond + probe.duration || !CanCheckDodgeProbe(probe))
            {
                continue;
            }

            activeDetection ??= probe;

            if (hasDummy && CheckDodgeProbe(enemy, probe, origin))
            {
                dodge.ConfirmPerfectDodge(target, enemy);
                hasDummy = false;
            }

            if (!detectedTarget && CheckDodgeProbe(enemy, probe, target.controller.bounds))
            {
                activeDetection = probe;
                detectedTarget = true;
            }
        }
    }

    internal void NotifyWallContact(Enemy enemy)
    {
        if (!m_chargeBranched && !string.IsNullOrEmpty(m_attack?.hitWallState) &&
            enemy.animator.TryGetAttackTime(out _, out float progress) && progress > 0.2f)
        {
            m_chargeHitWall = true;
        }
    }

    private bool TryBranchCharge(Enemy enemy, Player target, float time, float progress)
    {
        if (m_chargeBranched || enemy.health.currentHealth == 0)
        {
            return false;
        }

        if (m_chargeHitWall)
        {
            m_chargeHitWall = false;
            if (enemy.animator.PlayAttack(m_attack.hitWallState, 0f, 0.08f))
            {
                m_chargeBranched = true;
                m_waitingForWallEnd = !string.IsNullOrEmpty(m_attack.hitWallEndState);
                m_actionShakeIndex = m_attack.actionShakes?.Length ?? 0;
                m_hits = null;
                activeDetection = null;
                detectedTarget = false;
                return true;
            }
        }

        if (!string.IsNullOrEmpty(m_attack?.hitTargetState) && target != null && target.isAlive &&
            progress < 0.38f && time >= k_ChargeCheckStartFrame / k_FramesPerSecond &&
            time <= k_ChargeCheckEndFrame / k_FramesPerSecond &&
            m_hitbox.CheckSectorContact(target, enemy.transform.TransformPoint(0f, 0.75f, 0f), 5f, 1.5f, 210f) &&
            enemy.animator.PlayAttack(m_attack.hitTargetState, k_HitTargetStartFrame / k_FramesPerSecond, 0.1f))
        {
            var actionShakes = m_attack.actionShakes;
            while (actionShakes != null && m_actionShakeIndex < actionShakes.Length &&
                   actionShakes[m_actionShakeIndex].frame < k_HitTargetStartFrame)
            {
                m_actionShakeIndex++;
            }
            m_chargeBranched = true;
            m_hitTargetBranch = true;
            return true;
        }
        return false;
    }

    internal void ReleaseChargeTarget(Enemy enemy)
    {
        chargeTarget?.EndChargeCarry(enemy);
        chargeTarget = null;
    }
}
