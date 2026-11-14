using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public abstract class Entity<T> : MonoBehaviour where T : Entity<T>
{
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(0f)] private float groundStickSpeed = 2f;

    public CharacterController controller { get; private set; }

    public Vector3 lateralVelocity { get; protected set; }

    protected Vector3 m_rootMotionDelta;

    protected float verticalVelocity;

    internal bool isInHitStop { get; private set; }
    protected virtual float movementDeltaTime => Time.deltaTime;

    private Animator m_pausedAnimator;
    private float m_animatorSpeedBeforeHitStop;
    private float m_timedHitStopUntil = float.PositiveInfinity;

    protected virtual void Awake() => controller = GetComponent<CharacterController>();

    protected virtual void Update()
    {
        if (isInHitStop && Time.unscaledTime >= m_timedHitStopUntil)
        {
            EndHitStop();
        }
        if (isInHitStop)
        {
            return;
        }

        HandleStates();
        if (!isInHitStop)
        {
            ApplyGravity();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!isInHitStop)
        {
            HandleController();
        }
    }

    protected virtual void OnDisable()
    {
        EndHitStop();
        StopMoving();
    }

    protected abstract void HandleStates();

    protected virtual void AddRootMotion(Vector3 deltaPosition)
    {
        deltaPosition.y = 0f;
        m_rootMotionDelta += deltaPosition;
    }

    protected virtual void HandleController()
    {
        Vector3 displacement = m_rootMotionDelta;

        m_rootMotionDelta = Vector3.zero;

        if (!controller.enabled)
        {
            lateralVelocity = Vector3.zero;
            return;
        }

        displacement += Vector3.up * verticalVelocity * movementDeltaTime;

        Vector3 lastPosition = transform.position;
        controller.Move(displacement);

        Vector3 actualDelta = transform.position - lastPosition;
        actualDelta.y = 0f;

        lateralVelocity = Time.deltaTime > 0f ? actualDelta / Time.deltaTime : Vector3.zero;
    }

    public void StopMoving()
    {
        m_rootMotionDelta = Vector3.zero;
        lateralVelocity = Vector3.zero;
    }

    public void FaceDirection(Vector3 direction, float rotationSpeed)
    {
        if (direction.sqrMagnitude <= 0.01f) return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    protected virtual void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -groundStickSpeed;
            return;
        }

        verticalVelocity += gravity * movementDeltaTime;
    }

    internal void BeginHitStop(Animator animator)
    {
        if (isInHitStop)
        {
            return;
        }

        isInHitStop = true;
        StopMoving();
        FreezeHitStopAnimator(animator);
    }

    internal void FreezeHitStopAnimator(Animator animator)
    {
        if (!isInHitStop || m_pausedAnimator != null || animator == null)
        {
            return;
        }

        m_pausedAnimator = animator;
        m_animatorSpeedBeforeHitStop = animator.speed;
        animator.speed = 0f;
    }

    internal void PlayTimedHitStop(Animator animator, int frames)
    {
        if (frames <= 0 || isInHitStop || !isActiveAndEnabled)
        {
            return;
        }

        BeginHitStop(animator);
        m_timedHitStopUntil = Time.unscaledTime + frames / 60f;
    }

    internal void EndHitStop()
    {
        if (m_pausedAnimator != null)
        {
            m_pausedAnimator.speed = m_animatorSpeedBeforeHitStop;
        }

        m_pausedAnimator = null;
        isInHitStop = false;
        m_timedHitStopUntil = float.PositiveInfinity;
    }
}
