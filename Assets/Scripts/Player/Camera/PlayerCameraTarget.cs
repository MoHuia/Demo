using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerCameraTarget : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float heightOffset = 0.3f;
    [SerializeField] private Renderer[] modelRenderers;
    [SerializeField] private Transform aimTarget;
    [SerializeField, Min(0f)] private float followSmoothTime = 0.1f;

    [Header("视角")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadLookSpeed = 120f;
    [SerializeField] private float initialPitch = 25f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 60f;

    [SerializeField, Min(0.01f)] private float lookSmoothTime = 0.07f;
    [SerializeField] private bool canOrbitWithVelocity = true;
    [SerializeField, Min(0f)] private float orbitVelocityMultiplier = 5f;

    private float m_modelCenterHeight;
    private Vector2 m_followPosition;
    private Vector2 m_followVelocity;

    private PlayerInputManager inputs;
    private Player m_player;

    private float yaw;
    private float pitch;

    private float targetYaw;
    private float targetPitch;

    private float yawVelocity;
    private float pitchVelocity;

    private void Awake()
    {
        if (player != null)
        {
            inputs = player.GetComponent<PlayerInputManager>();
            m_player = player.GetComponent<Player>();
        }

        if (player == null || inputs == null || m_player == null)
        {
            Debug.LogError("相机目标点需要指定带输入组件的玩家", this);
            enabled = false;
            return;
        }

        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);

        targetYaw = yaw;
        targetPitch = pitch;
    }

    private void Start()
    {
        if (modelRenderers == null || modelRenderers.Length == 0)
        {
            return;
        }

        float bottom = float.PositiveInfinity;
        float top = float.NegativeInfinity;
        foreach (var part in modelRenderers)
        {
            if (part == null)
            {
                continue;
            }

            Bounds bounds = part.bounds;
            bottom = Mathf.Min(bottom, bounds.min.y);
            top = Mathf.Max(top, bounds.max.y);
        }

        if (bottom <= top)
        {
            m_modelCenterHeight = (bottom + top) * 0.5f - player.position.y;
        }
    }

    private void OnEnable()
    {
        if (player != null)
        {
            m_followPosition = new Vector2(player.position.x, player.position.z);
            m_followVelocity = Vector2.zero;
        }

        SetCursorLocked(true);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }

        Vector2 lookInput = inputs.LookInput;

        float scale = inputs.IsLookingWithMouse() ? mouseSensitivity : gamepadLookSpeed * dt;
        targetYaw += lookInput.x * scale;
        targetPitch = Mathf.Clamp(targetPitch - lookInput.y * scale, minPitch, maxPitch);

        if (lookInput.sqrMagnitude <= 0.01f)
        {
            HandleVelocityOrbit(dt);
        }

        yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, lookSmoothTime, Mathf.Infinity, dt);
        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothTime, Mathf.Infinity, dt);

        float wrappedYaw = Mathf.Repeat(yaw, 360f);
        targetYaw += wrappedYaw - yaw;
        yaw = wrappedYaw;
    }

    private void LateUpdate()
    {
        Vector3 playerPosition = player.position;
        Vector2 targetPosition = new Vector2(playerPosition.x, playerPosition.z);
        if (followSmoothTime <= 0f)
        {
            m_followPosition = targetPosition;
            m_followVelocity = Vector2.zero;
        }
        else if (Time.deltaTime > 0f)
        {
            m_followPosition = Vector2.SmoothDamp(
                m_followPosition, targetPosition, ref m_followVelocity, followSmoothTime);
        }

        Vector3 modelCenter = new Vector3(
            m_followPosition.x, playerPosition.y + m_modelCenterHeight, m_followPosition.y);
        transform.position = modelCenter + Vector3.up * heightOffset;
        if (aimTarget != null)
        {
            aimTarget.position = modelCenter;
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void HandleVelocityOrbit(float dt)
    {
        if (!canOrbitWithVelocity || !m_player.controller.isGrounded)
        {
            return;
        }

        var state = m_player.states.current;
        if (!(state is WalkStartPlayerState || state is WalkPlayerState || state is RunPlayerState))
        {
            return;
        }

        float sidewaysSpeed = Vector3.Dot(m_player.lateralVelocity, transform.right);
        targetYaw += sidewaysSpeed * orbitVelocityMultiplier * dt;
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
