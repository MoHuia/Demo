using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(CharacterController), typeof(PlayerInputManager), typeof(PlayerStateManager))]
public class Player : Entity<Player>
{
    private const float k_DashBufferTime = 0.2f;
    private const float k_HitFlyStopTime = 0.2f;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float rotationSpeed = 20f;
    [SerializeField, Min(0f)] private float hitFlyUpSpeed = 5.4f;
    [SerializeField, Range(0f, 1f)] private float hitFlyTravelScale = 0.7f;

    private Vector3 m_hitFlyDirection;
    private Health m_health;
    internal float lastCombatHitTime { get; private set; } = float.NegativeInfinity;
    internal float lastPerfectDodgeTime { get; private set; } = float.NegativeInfinity;
    private readonly HashSet<Enemy> m_attackers = new HashSet<Enemy>();
    private float m_dashBufferedUntil = float.NegativeInfinity;
    private Vector3 m_bufferedDashDirection;
    private Enemy m_hitStopAttacker;
    private Coroutine m_hitStopRoutine;
    private Enemy m_chargeAttacker;

    public PlayerInputManager inputs { get; private set; }
    public PlayerStateManager states { get; private set; }
    public PlayerAnimator animator { get; private set; }
    public bool isAlive => m_health == null || m_health.currentHealth > 0;
    public Transform cameraTarget => cameraTransform;
    public bool useRootMotion { get; private set; }
    public bool useRootRotation { get; private set; }

    internal Enemy dodgeCounterTarget { get; private set; }
    internal Vector3 dashDirection { get; private set; }
    internal bool isCarriedByCharge => m_chargeAttacker != null;
    internal string hitAnimation { get; private set; } = "Base Layer.Hit_L_F";
    internal PlayerHitReaction hitReaction { get; private set; }

    internal event System.Action<CombatCameraShakeSettings> cameraShakeRequested;

    protected override void Awake()
    {
        base.Awake();
        inputs = GetComponent<PlayerInputManager>();
        states = GetComponent<PlayerStateManager>();
        animator = GetComponent<PlayerAnimator>();
        m_health = GetComponent<Health>();

        if (animator == null)
        {
            Debug.LogError("玩家缺少 PlayerAnimator", this);
            enabled = false;
            return;
        }

        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("Player needs a camera reference.", this);
            enabled = false;
        }
    }

    private void Start() => states.Initialize(this);

    protected override void Update()
    {
        if (isInHitStop && Time.timeScale > 0f && inputs.GetAttackDown() &&
            states.current is NormalAttackPlayerState attack)
        {
            attack.BufferNextAttack();
        }

        if (isInHitStop && m_dashBufferedUntil > Time.time - Time.deltaTime)
        {
            m_dashBufferedUntil += Time.deltaTime;
        }
        if (isAlive && Time.timeScale > 0f && inputs.GetDashDown())
        {
            m_dashBufferedUntil = Time.time + k_DashBufferTime;
            m_bufferedDashDirection = inputs.GetMovementCameraDirection(cameraTarget);
        }

        base.Update();
    }

    protected override void OnDisable()
    {
        m_chargeAttacker = null;
        CancelHitStop();
        base.OnDisable();
    }

    protected override void HandleStates()
    {
        if (Time.timeScale <= 0f || !isAlive)
        {
            return;
        }

        bool canStartAction =
            states.current is IdlePlayerState ||
            states.current is WalkStartPlayerState ||
            states.current is WalkPlayerState ||
            states.current is RunPlayerState ||
            states.current is RunEndPlayerState;

        if (canStartAction && TryStartDash())
        {
            return;
        }

        if (canStartAction && inputs.GetAttackDown())
        {
            states.Change<NormalAttackPlayerState>();
            return;
        }

        states.Step();
    }

    public void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, rotationSpeed);

    internal bool TryResumeWalk(bool useWalkStart = false)
    {
        var direction = inputs.GetMovementCameraDirection(cameraTarget);
        if (direction.sqrMagnitude <= 0.01f)
        {
            return false;
        }

        if (useWalkStart)
        {
            states.Change<WalkStartPlayerState>();
        }
        else
        {
            states.Change<WalkPlayerState>();
        }
        FaceDirectionSmooth(direction);
        return true;
    }

    public void SetRootMotionEnabled(bool value, bool applyRotation = false)
    {
        useRootMotion = value;
        useRootRotation = value && applyRotation;

        if (!value)
        {
            StopMoving();
        }
    }

    public void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
    {
        if (!useRootMotion)
        {
            return;
        }

        if (states.current is HitFlyPlayerState)
        {
            ApplyRootYaw(deltaRotation);
            if (!isInHitStop)
            {
                float travel = Vector3.ProjectOnPlane(deltaPosition, Vector3.up).magnitude;
                AddRootMotion(m_hitFlyDirection * (travel * hitFlyTravelScale));
            }
            return;
        }
        if (isInHitStop)
        {
            return;
        }

        var normalAttack = states.current as NormalAttackPlayerState;
        var dodgeCounter = states.current as DodgeCounterPlayerState;
        var rush = states.current as DodgeRushPlayerState;
        Enemy target = normalAttack?.attackTarget ?? dodgeCounter?.attackTarget ?? rush?.attackTarget;

        if (target != null && target.health.currentHealth > 0)
        {
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0f;

            float stopDistance = controller.radius + target.controller.radius + 0.35f;
            float distance = offset.magnitude;
            float remainingDistance = Mathf.Max(0f, distance - stopDistance);
            Vector3 direction = offset.normalized;

            float forwardTravel = Vector3.Dot(deltaPosition, direction);

            if (dodgeCounter != null && dodgeCounter.canAssistApproach &&
                distance <= 3.5f && forwardTravel >= 0f)
            {
                forwardTravel = Mathf.Max(forwardTravel, 4f * Time.deltaTime);
            }

            if (forwardTravel > 0f)
            {
                deltaPosition = direction * Mathf.Min(forwardTravel, remainingDistance);
            }
        }

        AddRootMotion(deltaPosition);

        if (useRootRotation)
        {
            ApplyRootYaw(deltaRotation);
        }
    }

    private void ApplyRootYaw(Quaternion deltaRotation)
    {
        float yaw = deltaRotation.eulerAngles.y;
        transform.rotation *= Quaternion.Euler(0f, yaw, 0f);
    }

    public bool TryStartDash()
    {
        if (Time.time >= m_dashBufferedUntil)
        {
            return false;
        }

        m_dashBufferedUntil = float.NegativeInfinity;

        dashDirection = m_bufferedDashDirection;
        if (dashDirection.sqrMagnitude > 0.01f)
        {
            if (states.current is DashRunPlayerState)
            {
                states.RestartCurrent();
            }
            else
            {
                states.Change<DashRunPlayerState>();
            }
        }
        else if (states.current is DashIdlePlayerState)
        {
            states.RestartCurrent();
        }
        else
        {
            states.Change<DashIdlePlayerState>();
        }

        if (inputs.GetAttackDown() && states.current is DodgePlayerState dodge)
        {
            dodge.BufferAttack();
        }

        return true;
    }

    internal void StartDodgeCounter(Enemy attacker)
    {
        dodgeCounterTarget = attacker;
        states.Change<DodgeCounterPlayerState>();
        dodgeCounterTarget = null;
    }

    internal void RecordPerfectDodge()
    {
        lastPerfectDodgeTime = Time.unscaledTime;
        RecordCombatHit();
    }

    internal void RegisterAttacker(Enemy attacker) => m_attackers.Add(attacker);

    internal void UnregisterAttacker(Enemy attacker) => m_attackers.Remove(attacker);

    internal bool HasPendingDodgeProbe(Bounds origin, float remainingTime)
    {
        foreach (var attacker in m_attackers)
        {
            if (attacker.states?.current is AttackEnemyState attack &&
                attack.HasPendingDodgeProbe(attacker, origin, remainingTime))
            {
                return true;
            }
        }
        return false;
    }

    public void ApplyDamage(int amount, Enemy attacker = null, bool launchOnHit = false)
    {
        TryApplyDamage(amount, attacker, launchOnHit ? PlayerHitReaction.Launch : PlayerHitReaction.Light, 0f);
    }

    internal bool TryApplyDamage(int amount, Enemy attacker, PlayerHitReaction reaction, float hitStopSeconds,
        bool allowHitBasedDodge = true)
    {
        if (amount <= 0 || m_health == null || !isAlive)
        {
            return false;
        }

        if (states.current is PlayerState state && state.isInvulnerable)
        {
            if (allowHitBasedDodge)
            {
                (state as DodgePlayerState)?.ConfirmPerfectDodge(this, attacker);
            }
            return false;
        }

        int previousHealth = m_health.currentHealth;
        m_health.TakeDamage(amount);

        if (m_health.currentHealth < previousHealth)
        {
            RecordCombatHit();

            m_dashBufferedUntil = float.NegativeInfinity;

            if (!isAlive)
            {
                CancelHitStop();
                states.Change<DeadPlayerState>();
                if (hitStopSeconds > 0f)
                {
                    m_hitStopRoutine = StartCoroutine(PlayHitStop(attacker, hitStopSeconds, DeadPlayerState.AnimationState));
                }
                return true;
            }

            if (isInHitStop)
            {
                return true;
            }
            if (states.current is HitFlyPlayerState)
            {
                if (hitStopSeconds > 0f)
                {
                    m_hitStopRoutine = StartCoroutine(PlayHitStop(attacker, hitStopSeconds));
                }
                return true;
            }

            hitReaction = reaction;
            hitAnimation = SelectHitAnimation(attacker, reaction);
            if (reaction == PlayerHitReaction.Launch)
            {
                Vector3 away = attacker != null ? transform.position - attacker.transform.position : Vector3.zero;
                away.y = 0f;

                m_hitFlyDirection = away.sqrMagnitude > 1f / 10_000 ? away.normalized : -transform.forward;
            }
            StartHitReaction(reaction);
            if (hitStopSeconds > 0f || reaction == PlayerHitReaction.Launch)
            {
                float duration = hitStopSeconds > 0f ? hitStopSeconds : k_HitFlyStopTime;
                m_hitStopRoutine = StartCoroutine(PlayHitStop(attacker, duration, hitAnimation));
            }
            return true;
        }
        return false;
    }

    private string SelectHitAnimation(Enemy attacker, PlayerHitReaction reaction)
    {
        bool fromBack = attacker != null &&
            Vector3.Dot(Vector3.ProjectOnPlane(transform.forward, Vector3.up),
                attacker.transform.position - transform.position) < 0f;
        switch (reaction)
        {
            case PlayerHitReaction.Heavy:
                return fromBack ? "Base Layer.Hit_H_B" : "Base Layer.Hit_H_F";
            case PlayerHitReaction.Launch:
                return fromBack ? "Base Layer.HitFly_B" : "Base Layer.HitFly_F";
            default:
                return fromBack ? "Base Layer.Hit_L_B" : "Base Layer.Hit_L_F";
        }
    }

    private void StartHitReaction(PlayerHitReaction reaction)
    {
        if (reaction == PlayerHitReaction.Launch)
        {
            states.Change<HitFlyPlayerState>();
        }
        else if (states.current is HurtPlayerState)
        {
            states.RestartCurrent();
        }
        else
        {
            states.Change<HurtPlayerState>();
        }
    }

    internal void BeginHitFly() => verticalVelocity = hitFlyUpSpeed;

    internal void BeginChargeCarry(Enemy attacker) => m_chargeAttacker = attacker;

    internal void EndChargeCarry(Enemy attacker)
    {
        if (m_chargeAttacker == attacker)
        {
            m_chargeAttacker = null;
        }
    }

    internal float MoveWithCharge(Enemy attacker, Vector3 direction, float distance)
    {
        if (m_chargeAttacker != attacker || !isAlive || isInHitStop || !controller.enabled)
        {
            return 0f;
        }

        Vector3 before = transform.position;
        controller.Move(direction * distance);
        return Mathf.Clamp(Vector3.Dot(transform.position - before, direction), 0f, distance);
    }

    internal void RecordCombatHit() => lastCombatHitTime = Time.time;

    internal void RequestCameraShake(CombatCameraShakeSettings settings)
    {
        if (settings.radius > 0f)
        {
            cameraShakeRequested?.Invoke(settings);
        }
    }

    private IEnumerator PlayHitStop(Enemy attacker, float duration, string reactionState = null)
    {
        float startedAt = Time.unscaledTime;
        float endTime = startedAt + duration;
        BeginHitStop(reactionState == null ? animator.animator : null);
        m_hitStopAttacker = attacker;
        if (attacker != null)
        {
            attacker.BeginHitStop(attacker.animator.modelAnimator);
        }

        if (reactionState != null)
        {
            int stateHash = Animator.StringToHash(reactionState);

            float poseReadyTime = startedAt +
                (reactionState == DeadPlayerState.AnimationState ? 0.05f : animator.reactionBlendDuration * 0.5f);
            do
            {
                yield return null;
            } while (Time.unscaledTime < endTime &&
                     (Time.unscaledTime < poseReadyTime || !animator.HasVisibleReaction(stateHash)));

            if (Time.unscaledTime < endTime)
            {
                FreezeHitStopAnimator(animator.animator);
            }
        }

        float remaining = endTime - Time.unscaledTime;
        if (remaining > 0f)
        {
            yield return new WaitForSecondsRealtime(remaining);
        }

        FinishHitStop();
    }

    private void FinishHitStop()
    {
        EndHitStop();
        if (m_hitStopAttacker != null)
        {
            m_hitStopAttacker.EndHitStop();
        }

        m_hitStopAttacker = null;
        m_hitStopRoutine = null;
    }

    private void CancelHitStop()
    {
        if (m_hitStopRoutine != null)
        {
            StopCoroutine(m_hitStopRoutine);
            FinishHitStop();
        }
    }
}
