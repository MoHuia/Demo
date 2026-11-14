using UnityEngine;

[RequireComponent(typeof(Health), typeof(EnemyStateManager), typeof(EnemyAnimator))]
public class Enemy : Entity<Enemy>
{
    public Health health { get; private set; }
    public EnemyStateManager states { get; private set; }
    public EnemyAnimator animator { get; private set; }

    [SerializeField] private Player attackTarget;
    [SerializeField, Min(0.1f)] private float chaseRange = 18f;
    [SerializeField, Min(0.1f)] private float attackRange = 3.6f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 4.8f;
    [SerializeField, Min(0f)] private float attackRecovery = 0.32f;
    [SerializeField, Min(1f)] private float turnSpeed = 480f;
    [SerializeField] private Transform attackMotionRoot;
    [SerializeField] private Transform attackBodyRoot;
    [SerializeField] private BoxCollider[] solidArms;

    private Vector3 m_previousAttackOffset;
    private int m_previousAttackPoseVersion = -1;
    private bool m_hasAttackBodyPose;
    private Vector3 m_previousAttackBodyPose;
    private Vector3 m_correctedAttackBodyPose;
    private const float k_PerfectDodgeDuration = 0.6f;

    private static readonly AnimationCurve k_PerfectDodgeSlow = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(0.315f, 0.668f, -2.647f, -2.647f),
        new Keyframe(0.371f, 0.446f, -3.761f, -3.761f),
        new Keyframe(0.770f, 0.515f, 2.332f, 2.332f),
        new Keyframe(1f, 1f, 0.020f, 0.020f));
    private float m_slowStartedAt = float.NegativeInfinity;
    private float m_speedBeforeSlow = 1f;
    private float m_slowMultiplier = 1f;
    private bool m_endSlowAfterHitStop;

    protected override float movementDeltaTime => Time.deltaTime * m_slowMultiplier;

    public Player target => attackTarget;
    internal float recovery => attackRecovery;

    protected override void Awake()
    {
        base.Awake();
        health = GetComponent<Health>();
        states = GetComponent<EnemyStateManager>();
        animator = GetComponent<EnemyAnimator>();
    }

    private void OnEnable() => target?.RegisterAttacker(this);

    private void Start()
    {
        if (target != null && solidArms != null)
        {
            foreach (var arm in solidArms)
            {
                if (arm != null)
                {
                    Physics.IgnoreCollision(arm, target.controller);
                }
            }
        }

        states.Initialize(this);
    }

    protected override void Update()
    {
        if (!isInHitStop && !float.IsNegativeInfinity(m_slowStartedAt))
        {
            float progress = (Time.unscaledTime - m_slowStartedAt) / k_PerfectDodgeDuration;
            if (m_endSlowAfterHitStop || progress >= 1f)
            {
                EndPerfectDodgeSlow();
            }
            else
            {
                m_slowMultiplier = k_PerfectDodgeSlow.Evaluate(progress);
                animator.modelAnimator.speed = m_speedBeforeSlow * m_slowMultiplier;
            }
        }
        base.Update();

        if (!isInHitStop)
        {
            animator.PlayQueuedHitShake();
        }
    }

    protected override void LateUpdate()
    {
        var attack = states.current as AttackEnemyState;

        if (attackMotionRoot != null && attackBodyRoot != null)
        {
            Vector3 offset = attackMotionRoot.localPosition;
            offset.y = 0f;

            bool useAttackMotion = attack != null && animator.hasAttackPose;
            if (!isInHitStop && useAttackMotion && m_previousAttackPoseVersion == animator.attackPoseVersion)
            {
                Vector3 localDelta = offset - m_previousAttackOffset;
                Vector3 worldDelta = attackMotionRoot.parent.TransformVector(localDelta);

                AddRootMotion(attack.isCharging ? worldDelta :
                    LimitAttackAdvance(worldDelta, attack.chargeTarget));
            }

            Vector3 bodyPose = attackBodyRoot.localPosition;
            if (m_hasAttackBodyPose && bodyPose == m_correctedAttackBodyPose)
            {
                bodyPose = m_previousAttackBodyPose;
            }
            m_previousAttackBodyPose = bodyPose;
            m_correctedAttackBodyPose = bodyPose - offset;
            attackBodyRoot.localPosition = m_correctedAttackBodyPose;
            m_hasAttackBodyPose = true;
            m_previousAttackOffset = offset;
            m_previousAttackPoseVersion = useAttackMotion ? animator.attackPoseVersion : -1;
        }

        if (isInHitStop)
        {
            return;
        }

        base.LateUpdate();

        attack?.CheckContact(this);
    }

    protected override void OnDisable()
    {
        target?.UnregisterAttacker(this);
        if (states != null && states.current is AttackEnemyState attack)
        {
            attack.ReleaseChargeTarget(this);
        }
        base.OnDisable();
        EndPerfectDodgeSlow();
    }

    protected override void HandleStates()
    {
        states.Step();
    }

    internal bool TryGetChaseDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (target == null || !target.isAlive)
        {
            return false;
        }

        direction = target.transform.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude <= chaseRange * chaseRange;
    }

    internal void TurnToward(Vector3 direction, float speedScale = 1f)
    {
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direction), turnSpeed * speedScale * movementDeltaTime);
        }
    }

    internal bool IsInAttackRange(Vector3 direction)
    {
        float threshold = attackRange + 0.01f;
        return direction.sqrMagnitude <= threshold * threshold;
    }

    internal bool CanStartAttack(Vector3 direction) =>
        IsInAttackRange(direction) &&
        Vector3.Angle(transform.forward, direction) <= 20f;

    internal void AdvanceToward(Vector3 direction)
    {
        float step = Mathf.Min(chaseSpeed * movementDeltaTime, Mathf.Max(0f, direction.magnitude - attackRange));
        AddRootMotion(direction.normalized * step);
    }

    internal void CloseAttackGap(Vector3 direction)
    {
        if (Vector3.Angle(transform.forward, direction) <= 25f)
        {
            float step = Mathf.Min(2.8f * movementDeltaTime, Mathf.Max(0f, direction.magnitude - 2.8f));
            AddRootMotion(direction.normalized * step);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (states.current is AttackEnemyState attack &&
            hit.collider.GetComponentInParent<Player>() == null &&
            hit.collider.GetComponentInParent<Enemy>() == null &&
            Mathf.Abs(hit.normal.y) < 0.5f && Vector3.Dot(transform.forward, hit.normal) < -0.3f &&
            Vector3.Dot(transform.forward, hit.moveDirection) > 0.15f)
        {
            attack.NotifyWallContact(this);
        }
    }

    private Vector3 LimitAttackAdvance(Vector3 delta, Player chargeTarget)
    {
        if (target == null || !target.isAlive)
        {
            return delta;
        }

        Vector3 forward = transform.forward;
        float advance = Vector3.Dot(delta, forward);
        float remaining = chargeTarget == target && advance > 0f
            ? target.MoveWithCharge(this, forward, advance)
            : Mathf.Max(0f, Vector3.Dot(target.transform.position - transform.position, forward) - 1.4f);
        return delta - forward * Mathf.Max(0f, advance - remaining);
    }

    public bool ApplyDamage(int amount)
    {
        int previousHealth = health.currentHealth;
        health.TakeDamage(amount);

        if (health.currentHealth == previousHealth)
        {
            return false;
        }

        if (health.currentHealth == 0)
        {
            states.Change<DeadEnemyState>();
            return true;
        }

        animator.QueueHitShake();

        if (states.current is AttackEnemyState)
        {
            return true;
        }

        if (states.current is HurtEnemyState)
        {
            if (!animator.hasHitShakeLayer)
            {
                states.RestartCurrent();
            }
        }
        else
        {
            states.Change<HurtEnemyState>();
        }

        return true;
    }

    internal void StartPerfectDodgeSlow()
    {
        if (isInHitStop)
        {
            return;
        }
        if (float.IsNegativeInfinity(m_slowStartedAt))
        {
            m_speedBeforeSlow = animator.modelAnimator.speed;
        }
        m_slowStartedAt = Time.unscaledTime;
    }

    internal void EndPerfectDodgeSlow()
    {
        if (float.IsNegativeInfinity(m_slowStartedAt))
        {
            return;
        }
        if (isInHitStop)
        {
            m_endSlowAfterHitStop = true;
            return;
        }
        animator.modelAnimator.speed = m_speedBeforeSlow;
        m_slowStartedAt = float.NegativeInfinity;
        m_slowMultiplier = 1f;
        m_endSlowAfterHitStop = false;
    }
}
