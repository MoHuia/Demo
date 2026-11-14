using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;

    [Header("参数名称")]
    public string stateName = "State";
    public string lastStateName = "Last State";
    public string lateralSpeedName = "Lateral Speed";

    protected Player m_player;
    protected PlayerStateManager m_states;
    private CombatAttackAudio m_attackAudio;

    protected int m_stateHash;
    protected int m_lastStateHash;
    protected int m_lateralSpeedHash;

    protected readonly Dictionary<Type, string> m_animations = new Dictionary<Type, string>
    {
        { typeof(IdlePlayerState), "Base Layer.Idle" },
        { typeof(WalkStartPlayerState), "Base Layer.WalkStart" },
        { typeof(WalkPlayerState), "Base Layer.Walk" },
        { typeof(RunPlayerState), "Base Layer.Run" },
        { typeof(RunEndPlayerState), "Base Layer.RunEnd" },
        { typeof(DashIdlePlayerState), DashIdlePlayerState.AnimationState },
        { typeof(DashRunPlayerState), "Base Layer.DashRun" },
        { typeof(TurnBackPlayerState), "Base Layer.TurnBack"},
        { typeof(DodgeCounterPlayerState), DodgeCounterPlayerState.AnimationState },
        { typeof(DodgeCounterEndPlayerState), DodgeCounterEndPlayerState.AnimationState },
        { typeof(DodgeRushPlayerState), DodgeRushPlayerState.AnimationState },
        { typeof(DodgeRushEndPlayerState), DodgeRushEndPlayerState.AnimationState },
        { typeof(DeadPlayerState), DeadPlayerState.AnimationState }
    };

    internal float reactionBlendDuration => m_states.last is HurtPlayerState ? 0.05f : 0.08f;

    protected virtual void Awake()
    {
        InitializePlayer();
        m_attackAudio = GetComponent<CombatAttackAudio>();

        if (animator == null || m_states == null)
        {
            Debug.LogError("玩家动画组件缺少 Animator 或状态管理器", this);
            enabled = false;
            return;
        }

        InitializeParametersHash();
    }

    protected virtual void OnEnable()
    {
        InitializeAnimatorEvents();

        HandleStateChanged();
    }

    protected virtual void LateUpdate()
    {
        HandleAnimatorParameters();
    }

    protected virtual void OnDisable()
    {
        if (m_states != null)
        {
            m_states.events.onChange.RemoveListener(HandleStateChanged);
        }
    }

    protected virtual void InitializePlayer()
    {
        m_player = GetComponent<Player>();

        m_states = GetComponent<PlayerStateManager>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    protected virtual void InitializeParametersHash()
    {
        m_stateHash = Animator.StringToHash(stateName);
        m_lastStateHash = Animator.StringToHash(lastStateName);
        m_lateralSpeedHash = Animator.StringToHash(lateralSpeedName);
    }

    protected virtual void InitializeAnimatorEvents()
    {
        m_states.events.onChange.AddListener(HandleStateChanged);
    }

    protected virtual void HandleStateChanged()
    {
        if (m_states.current == null)
        {
            return;
        }

        HandleAnimatorParameters();

        if (m_states.current is HurtPlayerState || m_states.current is HitFlyPlayerState)
        {
            Play(m_player.hitAnimation, reactionBlendDuration);
            return;
        }

        if (m_states.current is NormalAttackPlayerState attack)
        {
            Play(attack.animationState);
            return;
        }

        if (m_states.current is NormalAttackEndPlayerState recovery)
        {
            Play(recovery.animationState);
            return;
        }

        var type = m_states.current.GetType();
        if (!m_animations.TryGetValue(type, out var stateName))
        {
            Debug.LogError($"没有配置状态 {type.Name} 对应的动画", this);
            return;
        }

        if (m_states.current is WalkStartPlayerState &&
            (m_states.last is DashIdlePlayerState || m_states.last is HitFlyPlayerState))
        {
            Play(stateName, 0.2f);
            return;
        }

        bool isDash = m_states.current is DashIdlePlayerState ||
                      m_states.current is DashRunPlayerState;
        Play(stateName, isDash ? 0.05f : 0.1f);
    }

    protected virtual void HandleAnimatorParameters()
    {
        if (m_states.current == null)
        {
            return;
        }

        animator.SetInteger(m_stateHash, m_states.index);
        animator.SetInteger(m_lastStateHash, m_states.lastIndex);

        animator.SetFloat(
            m_lateralSpeedHash,
            m_player.lateralVelocity.magnitude
        );
    }

    public void Play(string fullStateName, float transitionTime = 0.1f, float startTime = 0f)
    {
        int hash = Animator.StringToHash(fullStateName);
        if (!animator.HasState(0, hash))
        {
            Debug.LogError($"Animator 中找不到状态 {fullStateName}", this);
            return;
        }

        animator.CrossFadeInFixedTime(hash, transitionTime, 0, startTime);
        m_attackAudio?.Begin(animator, fullStateName, hash, startTime);
    }

    internal void PlayAttackImpact(int hitIndex) => m_attackAudio?.PlayImpact(hitIndex);

    internal bool HasVisibleReaction(int stateHash)
    {
        if (animator.IsInTransition(0))
        {
            return animator.GetNextAnimatorStateInfo(0).fullPathHash == stateHash &&
                   animator.GetAnimatorTransitionInfo(0).normalizedTime >= 0.5f;
        }

        return animator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash;
    }

    public bool IsAnimationFinished(string fullStateName, int layer = 0)
    {
        if (animator == null)
        {
            return false;
        }

        if (animator.IsInTransition(layer))
        {
            return false;
        }

        var state = animator.GetCurrentAnimatorStateInfo(layer);

        return state.IsName(fullStateName) && !state.loop && state.normalizedTime >= 1f;
    }

    public bool CanMoveAfterAnimationTime(string fullStateName, float minimumTime, int layer = 0)
    {
        if (animator == null || animator.IsInTransition(layer))
        {
            return false;
        }

        var state = animator.GetCurrentAnimatorStateInfo(layer);

        if (!state.IsName(fullStateName))
        {
            return false;
        }

        float animationTime = state.normalizedTime * state.length;

        return !float.IsInfinity(animationTime) && animationTime >= minimumTime;
    }
}
