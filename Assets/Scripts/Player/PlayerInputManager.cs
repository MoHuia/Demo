using UnityEngine;
using UnityEngine.InputSystem;


// 负责提供输入数据，状态根据输入计算速度；这里不直接移动角色
public class PlayerInputManager : MonoBehaviour
{
    // Inspector 中绑定 PlayerInputActions；当前资源只配置了键盘 WASD
    public InputActionAsset actions;

    // 缓存移动动作，避免每次读取时按名称查找
    protected InputAction m_movement;
    
    // 缓存跑步动作
    protected InputAction m_run;
    
    // 跳跃输入动作
    protected InputAction m_jump;

    protected virtual void CacheActions()
    {
        m_movement = actions["Movement"];
        m_run = actions["Run"];
        m_jump = actions["Jump"];
    }

    protected virtual void Awake()
    {
        // 提前取得动作对象，供之后的状态逻辑反复读取
        CacheActions();
    }

    protected virtual void OnEnable()
    {
        // 启用资源中的动作，让它们开始响应输入
        actions?.Enable();
    }

    protected virtual void OnDisable()
    {
        // 组件停用时停止这份资源中的输入动作
        actions?.Disable();
    }

    public virtual Vector3 GetMovementDirection()
    {
        // Vector2 表示左右、前后两个输入轴；使用它不代表加入手柄功能
        var value = m_movement.ReadValue<Vector2>();

        // 输入 X 映射到世界 X，输入 Y 映射到世界 Z，保持角色只在 XZ 平面移动
        return new Vector3(value.x, 0, value.y);
    }
    
    // 按住 Shift 时返回 true
    public virtual bool GetRun()
    {
        return m_run.IsPressed();
    }
    
    // 只在按下 Space 的那一帧返回 true
    public virtual bool GetJumpDown()
    {
        return m_jump.WasPressedThisFrame();
    }
}
