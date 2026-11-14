using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private InputActionAsset runtimeActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction dashAction;

    private InputAction attackAction;

    public Vector2 MoveInput => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

    public Vector3 GetMovementDirection()
    {
        Vector2 input = MoveInput;

        return new Vector3(input.x, 0f, input.y).normalized;
    }

    public Vector3 GetMovementCameraDirection(Transform cameraTransform)
    {
        var direction = GetMovementDirection();

        var rotation = Quaternion.AngleAxis(cameraTransform.eulerAngles.y, Vector3.up);
        return rotation * direction;
    }

    public Vector2 LookInput => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

    public bool IsLookingWithMouse()
    {
        return lookAction != null &&
               lookAction.activeControl != null &&
               lookAction.activeControl.device is Mouse;
    }

    public bool GetDashDown()
    {
        return dashAction != null && dashAction.WasPressedThisFrame();
    }

    public bool GetAttackDown()
    {
        return attackAction != null && attackAction.WasPerformedThisFrame();
    }

    internal bool IsAttackHeld => attackAction != null && attackAction.IsPressed();

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("PlayerInputManager needs an Input Actions asset.", this);
            enabled = false;
            return;
        }

        runtimeActions = Instantiate(inputActions);
        moveAction = runtimeActions.FindAction("Player/Move", true);
        lookAction = runtimeActions.FindAction("Player/Look", true);
        dashAction = runtimeActions.FindAction("Player/Dash", true);
        attackAction = runtimeActions.FindAction("Player/Attack", true);
    }

    private void OnEnable() => runtimeActions?.Enable();
    private void OnDisable() => runtimeActions?.Disable();
    private void OnDestroy()
    {
        if (runtimeActions != null) Destroy(runtimeActions);
    }
}
