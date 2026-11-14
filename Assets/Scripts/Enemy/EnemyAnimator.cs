using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private static readonly int k_IdleHash = Animator.StringToHash("Base Layer.Idle");
    private static readonly int k_HitHash = Animator.StringToHash("Base Layer.Hit");
    private static readonly int k_DeathHash = Animator.StringToHash("Base Layer.Death");
    private const string k_ChaseState = "Base Layer.Chase";
    private static readonly int k_ChaseHash = Animator.StringToHash(k_ChaseState);
    private const string k_HitShakeLayer = "HitShake Layer";
    private static readonly int k_HitShakeHash = Animator.StringToHash("HitShake Layer.Hit_Shake");
    private static readonly int k_EmptyHitShakeHash = Animator.StringToHash("HitShake Layer.Empty");
    private int m_attackHash;
    private int m_hitShakeLayer;
    private bool m_hitShakeQueued;
    private Animator m_animator;
    private EnemyStateManager m_states;
    private CombatAttackAudio m_attackAudio;
    internal Animator modelAnimator => m_animator;
    internal bool hasHitShakeLayer => m_hitShakeLayer >= 0;
    internal int attackPoseVersion { get; private set; }

    internal bool hasAttackPose => m_attackHash != 0 && !m_animator.IsInTransition(0) &&
                                   m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash == m_attackHash;

    private void Awake()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_states = GetComponent<EnemyStateManager>();
        m_attackAudio = GetComponent<CombatAttackAudio>();
        m_hitShakeLayer = m_animator.GetLayerIndex(k_HitShakeLayer);
    }

    private void OnEnable()
    {
        m_states.events.onChange.AddListener(HandleStateChanged);
        HandleStateChanged();
    }

    private void OnDisable()
    {
        m_states.events.onChange.RemoveListener(HandleStateChanged);
        m_hitShakeQueued = false;
    }

    private void HandleStateChanged()
    {
        if (!(m_states.current is AttackEnemyState))
        {
            m_attackAudio?.Clear();
        }

        if (m_states.current is DeadEnemyState)
        {
            m_hitShakeQueued = false;
            if (hasHitShakeLayer)
            {
                m_animator.Play(k_EmptyHitShakeHash, m_hitShakeLayer, 0f);
            }
            m_animator.CrossFadeInFixedTime(k_DeathHash, 0.12f, 0, 0f);
            return;
        }

        if (m_states.current is HurtEnemyState)
        {
            m_animator.Play(k_HitHash, 0, 0f);
        }
        else if (m_states.current is IdleEnemyState)
        {
            m_animator.CrossFadeInFixedTime(k_IdleHash, 0.20f, 0, 0f);
        }
        else if (m_states.current is ChaseEnemyState && m_animator.HasState(0, k_ChaseHash))
        {
            m_animator.CrossFadeInFixedTime(k_ChaseHash, 0.18f, 0, 0f);
            m_attackAudio?.Begin(m_animator, k_ChaseState, k_ChaseHash, 0f);
        }
    }

    public bool IsHitFinished() => IsStateFinished(k_HitHash);

    internal void QueueHitShake() => m_hitShakeQueued = hasHitShakeLayer;

    internal void PlayQueuedHitShake()
    {
        if (!m_hitShakeQueued)
        {
            return;
        }

        m_hitShakeQueued = false;

        if (!m_animator.GetCurrentAnimatorStateInfo(m_hitShakeLayer).IsName("HitShake Layer.Hit_Shake"))
        {
            m_animator.Play(k_HitShakeHash, m_hitShakeLayer, 0f);
        }
    }

    public bool IsAttackFinished() => m_attackHash != 0 && IsStateFinished(m_attackHash);

    internal bool PlayAttack(string stateName, float startTime = 0f, float blendTime = 0.14f)
    {
        string fullStateName = "Base Layer." + stateName;
        int hash = Animator.StringToHash(fullStateName);
        if (!m_animator.HasState(0, hash))
        {
            Debug.LogError($"怪物 Animator 找不到攻击状态 {stateName}", this);
            return false;
        }

        m_attackHash = hash;
        attackPoseVersion++;

        m_animator.CrossFadeInFixedTime(hash, blendTime, 0, startTime);
        m_attackAudio?.Begin(m_animator, fullStateName, hash, startTime);
        return true;
    }

    internal void PlayAttackImpact(AudioClip clip) => m_attackAudio?.PlayImpact(clip);

    internal bool TryGetAttackTime(out float seconds)
    {
        return TryGetAttackTime(out seconds, out _);
    }

    internal bool TryGetAttackTime(out float seconds, out float progress)
    {
        var state = m_animator.GetCurrentAnimatorStateInfo(0);
        if (state.fullPathHash != m_attackHash && m_animator.IsInTransition(0))
        {
            state = m_animator.GetNextAnimatorStateInfo(0);
        }

        progress = state.normalizedTime;
        seconds = progress * state.length;
        return m_attackHash != 0 && state.fullPathHash == m_attackHash;
    }

    private bool IsStateFinished(int stateHash)
    {
        var state = m_animator.GetCurrentAnimatorStateInfo(0);
        return state.fullPathHash == stateHash && state.normalizedTime >= 1f;
    }
}
