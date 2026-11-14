using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class PlayerCameraFraming : MonoBehaviour
{
    [SerializeField] private Player m_player;

    [Header("镜头构图")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [SerializeField] private Vector2 idleFraming = new Vector2(4f, 0.47f);
    [SerializeField] private Vector2 movingFraming = new Vector2(5.4f, 0.55f);
    [SerializeField] private Vector2 combatFraming = new Vector2(8.2f, 0.67f);
    [SerializeField, Min(0f)] private float combatFramingHoldTime = 15f;
    [SerializeField, Min(0.01f)] private float movingFramingSmoothTime = 0.85f;
    [SerializeField, Min(0.01f)] private float combatFramingSmoothTime = 1.35f;
    [SerializeField, Min(0.01f)] private float idleFramingSmoothTime = 3.2f;

    [Header("攻击推近")]
    [SerializeField, Min(0f)] private float attackPushDistance = 0.5f;
    [SerializeField, Min(0.01f)] private float attackPushTime = 0.08f;
    [SerializeField, Min(0.01f)] private float attackReturnTime = 0.2f;

    [Header("极限闪避成功")]
    [SerializeField] private float perfectDodgeFovOffset = 5f;
    [SerializeField, Range(0f, 1f)] private float perfectDodgeVignetteIntensity = 0.32f;

    private Cinemachine3rdPersonFollow m_cameraBody;
    private CinemachineComposer m_composer;
    private bool m_useMovingFraming;
    private Vector2 m_currentFraming;
    private Vector2 m_framingVelocity;
    private float m_baseFieldOfView;
    private Vector3 m_baseShoulderOffset;
    private const float k_SourceNeutralFov = 60f;
    private const float k_AttackFrame = 1f / 60f;
    private const float k_AttackFramingReturnTime = 0.2f;

    private const float k_PerfectDodgeCameraIn = 0.5f;
    private const float k_PerfectDodgeCameraOut = 0.7f;
    private const float k_PerfectDodgeCameraY = -0.3f;
    private const float k_PerfectDodgeVignetteIn = 0.08f;
    private const float k_PerfectDodgeVignetteOut = 0.52f;
    private static readonly AnimationCurve k_PerfectDodgeReturn = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.410f, 0.185f, 1.195f, 1.195f),
        new Keyframe(1f, 1f, 0f, 0f));
    private float m_seenPerfectDodgeTime = float.NegativeInfinity;
    private float m_perfectDodgeBlend;
    private float m_perfectDodgeStartBlend;
    private Volume m_perfectDodgeVolume;
    private VolumeProfile m_perfectDodgeProfile;

    private Vector3 m_currentAttackFraming;
    private Vector3 m_attackFramingStart;
    private Vector3 m_attackFramingTarget;
    private float m_attackFramingElapsed;
    private float m_attackFramingDuration;
    private EntityState<Player> m_previousAttackState;
    private float m_attackPushStartedAt = float.NegativeInfinity;
    private float m_attackPushStartOffset;
    private float m_attackDistanceOffset;

    private void Awake()
    {
        if (m_player == null || virtualCamera == null)
        {
            Debug.LogError("镜头构图需要指定玩家和虚拟相机", this);
            enabled = false;
            return;
        }

        m_cameraBody = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        m_composer = virtualCamera.GetCinemachineComponent<CinemachineComposer>();
        m_baseFieldOfView = virtualCamera.m_Lens.FieldOfView;
        if (m_cameraBody != null)
        {
            m_baseShoulderOffset = m_cameraBody.ShoulderOffset;
        }

        m_currentFraming = idleFraming;
        ApplyFraming();
    }

    private void LateUpdate()
    {
        var state = m_player.states.current;

        if (state is WalkStartPlayerState || state is WalkPlayerState || state is RunPlayerState)
        {
            m_useMovingFraming = true;
        }
        else if (state is IdlePlayerState idle && idle.useRelaxedFraming)
        {
            m_useMovingFraming = false;
        }
        Vector2 targetFraming = m_useMovingFraming ? movingFraming : idleFraming;
        float smoothTime = m_useMovingFraming ? movingFramingSmoothTime : idleFramingSmoothTime;

        if (Time.time - m_player.lastCombatHitTime < combatFramingHoldTime)
        {
            targetFraming = combatFraming;
            smoothTime = combatFramingSmoothTime;
        }
        else if (targetFraming.x < m_currentFraming.x)
        {
            smoothTime = idleFramingSmoothTime;
        }

        if (Time.deltaTime > 0f)
        {
            m_currentFraming = Vector2.SmoothDamp(
                m_currentFraming, targetFraming, ref m_framingVelocity, smoothTime);
            UpdateNormalAttackFraming(state as NormalAttackPlayerState);
            UpdateDodgeAttackPush(state);
        }

        UpdatePerfectDodgeCamera();
        ApplyFraming();
    }

    private void OnDisable()
    {
        m_seenPerfectDodgeTime = float.NegativeInfinity;
        m_perfectDodgeBlend = m_perfectDodgeStartBlend = 0f;
        if (m_perfectDodgeVolume != null)
        {
            m_perfectDodgeVolume.weight = 0f;
        }

        if (m_baseFieldOfView > 0f && virtualCamera != null)
        {
            m_currentAttackFraming = m_attackFramingStart = m_attackFramingTarget = Vector3.zero;
            m_attackFramingElapsed = m_attackFramingDuration = 0f;
            m_attackPushStartedAt = float.NegativeInfinity;
            m_attackDistanceOffset = 0f;
            m_previousAttackState = null;
            ApplyFraming();
        }
    }

    private void OnDestroy()
    {
        if (m_perfectDodgeProfile != null)
        {
            foreach (var component in m_perfectDodgeProfile.components)
            {
                Destroy(component);
            }
            Destroy(m_perfectDodgeProfile);
        }
    }

    private void UpdateNormalAttackFraming(NormalAttackPlayerState attack)
    {
        Vector3 target = Vector3.zero;
        float duration = k_AttackFramingReturnTime;

        if (attack != null && attack.attackIndex == 3)
        {
            bool secondPose = m_player.animator.CanMoveAfterAnimationTime(attack.animationState, 18 * k_AttackFrame);
            target.x = secondPose ? -0.20f : 0.15f;
            duration = secondPose ? 0.75f : 0.25f;
        }
        else if (attack != null && attack.attackIndex == 4)
        {
            bool secondPose = m_player.animator.CanMoveAfterAnimationTime(attack.animationState, 33 * k_AttackFrame);
            target = secondPose
                ? new Vector3(-0.20f, 0f, 0f)
                : new Vector3(0.15f, 0.30f, 55f - k_SourceNeutralFov);
            duration = secondPose ? 0.15f : 0.25f;
        }

        if (target != m_attackFramingTarget)
        {
            m_attackFramingStart = m_currentAttackFraming;
            m_attackFramingTarget = target;
            m_attackFramingElapsed = 0f;
            m_attackFramingDuration = duration;
        }

        if (m_attackFramingElapsed < m_attackFramingDuration)
        {
            m_attackFramingElapsed += Time.deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, m_attackFramingElapsed / m_attackFramingDuration);
            m_currentAttackFraming = Vector3.Lerp(m_attackFramingStart, m_attackFramingTarget, blend);
        }
    }

    private void UpdateDodgeAttackPush(EntityState<Player> state)
    {
        bool isDodgeAttack = state is DodgeCounterPlayerState || state is DodgeRushPlayerState;

        if (isDodgeAttack && state != m_previousAttackState)
        {
            m_attackPushStartedAt = Time.time;
            m_attackPushStartOffset = m_attackDistanceOffset;
        }

        m_previousAttackState = state;

        float elapsed = Time.time - m_attackPushStartedAt;
        m_attackDistanceOffset = elapsed < attackPushTime
            ? Mathf.SmoothStep(m_attackPushStartOffset, attackPushDistance, elapsed / attackPushTime)
            : Mathf.SmoothStep(attackPushDistance, 0f, (elapsed - attackPushTime) / attackReturnTime);
    }

    private void UpdatePerfectDodgeCamera()
    {
        if (m_seenPerfectDodgeTime != m_player.lastPerfectDodgeTime)
        {
            m_seenPerfectDodgeTime = m_player.lastPerfectDodgeTime;
            m_perfectDodgeStartBlend = m_perfectDodgeBlend;
        }

        float elapsed = Time.unscaledTime - m_seenPerfectDodgeTime;
        m_perfectDodgeBlend = elapsed < k_PerfectDodgeCameraIn
            ? Mathf.SmoothStep(m_perfectDodgeStartBlend, 1f, elapsed / k_PerfectDodgeCameraIn)
            : 1f - k_PerfectDodgeReturn.Evaluate(Mathf.Clamp01((elapsed - k_PerfectDodgeCameraIn) / k_PerfectDodgeCameraOut));

        float vignetteBlend = elapsed < k_PerfectDodgeVignetteIn
            ? Mathf.SmoothStep(0f, 1f, elapsed / k_PerfectDodgeVignetteIn)
            : Mathf.SmoothStep(1f, 0f, (elapsed - k_PerfectDodgeVignetteIn) / k_PerfectDodgeVignetteOut);
        if (m_perfectDodgeVolume == null && vignetteBlend > 0f && perfectDodgeVignetteIntensity > 0f)
        {
            var effect = new GameObject("Perfect Dodge Vignette") { hideFlags = HideFlags.HideAndDontSave };
            effect.transform.SetParent(transform, false);
            m_perfectDodgeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            m_perfectDodgeProfile.hideFlags = HideFlags.HideAndDontSave;
            m_perfectDodgeProfile.Add<Vignette>().intensity.Override(perfectDodgeVignetteIntensity);
            m_perfectDodgeVolume = effect.AddComponent<Volume>();
            m_perfectDodgeVolume.isGlobal = true;
            m_perfectDodgeVolume.priority = 100f;
            m_perfectDodgeVolume.sharedProfile = m_perfectDodgeProfile;
        }

        if (m_perfectDodgeVolume != null)
        {
            m_perfectDodgeVolume.weight = vignetteBlend;
        }
    }

    private void ApplyFraming()
    {
        if (m_cameraBody != null)
        {
            m_cameraBody.ShoulderOffset = m_baseShoulderOffset +
                Vector3.up * (m_currentAttackFraming.x + k_PerfectDodgeCameraY * m_perfectDodgeBlend);
            m_cameraBody.CameraDistance = Mathf.Max(0f,
                m_currentFraming.x * (1f + m_currentAttackFraming.y) - m_attackDistanceOffset);
        }

        var lens = virtualCamera.m_Lens;
        lens.FieldOfView = m_baseFieldOfView + m_currentAttackFraming.z + perfectDodgeFovOffset * m_perfectDodgeBlend;
        virtualCamera.m_Lens = lens;

        if (m_composer != null)
        {
            m_composer.m_ScreenY = m_currentFraming.y;
        }
    }
}
