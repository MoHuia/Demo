using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerRunLean : MonoBehaviour
{
    [SerializeField] private Transform leanBone;
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float fullLeanTurnSpeed = 180f;
    [SerializeField] private float leanSpeed = 90f;

    private Player m_player;
    private Vector3 m_previousForward;
    private bool m_wasRunning;
    private float m_leanAngle;

    private Quaternion m_animationRotation;
    private bool m_leanApplied;

    private void Awake()
    {
        m_player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        m_previousForward = transform.forward;
        m_wasRunning = false;
        m_leanAngle = 0f;
    }

    private void Update()
    {
        if (!m_player.isInHitStop)
        {
            RestoreAnimationPose();
        }
    }

    private void LateUpdate()
    {
        if (m_player.isInHitStop)
        {
            return;
        }

        Vector3 forward = transform.forward;
        bool running = m_player.states != null && m_player.states.current is RunPlayerState;

        float deltaTime = Time.deltaTime;
        float targetLean = 0f;

        if (running && m_wasRunning && deltaTime > 0f)
        {
            float yaw = Vector3.SignedAngle(m_previousForward, forward, Vector3.up);

            float turnSpeed = yaw / deltaTime;

            float strength = Mathf.Clamp(turnSpeed / Mathf.Max(1f, fullLeanTurnSpeed), -1f, 1f);

            targetLean = strength * maxLeanAngle;
        }

        m_previousForward = forward;
        m_wasRunning = running;

        if (!running || leanBone == null)
        {
            m_leanAngle = 0f;
            return;
        }

        m_leanAngle = Mathf.MoveTowards(
            m_leanAngle, targetLean, leanSpeed * deltaTime
        );

        m_animationRotation = leanBone.localRotation;
        leanBone.rotation =
            Quaternion.AngleAxis(-m_leanAngle, forward) * leanBone.rotation;
        m_leanApplied = true;
    }

    private void OnDisable()
    {
        RestoreAnimationPose();
    }

    private void RestoreAnimationPose()
    {
        if (m_leanApplied && leanBone != null)
        {
            leanBone.localRotation = m_animationRotation;
        }

        m_leanApplied = false;
    }
}
