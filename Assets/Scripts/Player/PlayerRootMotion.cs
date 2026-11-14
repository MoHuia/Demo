using UnityEngine;

[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class PlayerRootMotion : MonoBehaviour
{
    protected Animator m_animator;
    protected Player m_player;

    protected virtual void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_player = GetComponentInParent<Player>();

        if (m_player == null)
        {
            Debug.LogError("根运动组件需要放在 Player 的模型子物体上", this);
            enabled = false;
        }
    }

    protected virtual void OnAnimatorMove()
    {
        if (m_player == null || !m_player.isActiveAndEnabled) return;

        m_player.ApplyRootMotion(m_animator.deltaPosition, m_animator.deltaRotation);
    }
}
