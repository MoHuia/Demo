
using UnityEngine;

// 添加 Player 组件时，让 Unity 同时补齐它依赖的输入和状态管理组件
[RequireComponent(typeof(PlayerInputManager))]
[RequireComponent(typeof(PlayerStateManager))]
[RequireComponent(typeof(PlayerStatsManager))]
// 玩家负责提供输入引用和玩家参数；每帧状态执行与实际位移沿用 Entity<Player>
public class Player : Entity<Player>
{
    // 状态通过 player.InputManager 读取方向，不需要自己查找输入组件
    public PlayerInputManager InputManager { get; protected set; }
    
    // 玩家数值管理器
    public PlayerStatsManager PlayerStats { get; protected set; }
    
    // 使用玩家自己的参数，调用父类的通用加速算法
    public virtual void Accelerate(Vector3 direction)
    {
        var current = PlayerStats.current;
        var running = InputManager.GetRun();

        // 按住 Shift 时使用疾跑参数
        var turningDrag = running
            ? current.runningTurningDrag
            : current.turningDrag;

        var acceleration = running
            ? current.runningAcceleration
            : current.acceleration;

        var topSpeed = running
            ? current.runningTopSpeed
            : current.topSpeed;

        Accelerate(direction, turningDrag, acceleration, topSpeed);

        // 限制最终水平速度，松开 Shift 后恢复普通上限
        lateralVelocity = Vector3.ClampMagnitude(
            lateralVelocity,
            topSpeed);
    }

    // 当前只处理普通平地，使用玩家的摩擦减速参数
    public virtual void Friction()
    {
        Decelerate(PlayerStats.current.friction);
    }
    
    // 使用当前配置施加重力，并限制下落速度
    public virtual void Gravity()
    {
        Gravity(PlayerStats.current.gravity);

        var speed = Mathf.Max(
            verticalVelocity.y,
            -PlayerStats.current.gravityTopSpeed);

        verticalVelocity = Vector3.up * speed;
    }

    // 使用当前配置维持地面接触
    public virtual void SnapToGround()
    {
        SnapToGround(PlayerStats.current.snapForce);
    }
    
    // 地面上按下跳跃键时起跳
    public virtual void Jump()
    {
        if (isGrounded && InputManager.GetJumpDown())
        {
            Jump(PlayerStats.current.jumpSpeed);
        }
    }

    // 设置向上初速度，并进入空中状态
    public virtual void Jump(float speed)
    {
        verticalVelocity = Vector3.up * speed;

        // 主动清除落地标记，让下一次重力计算正常执行
        isGrounded = false;

        states.Change<FallPlayerState>();
    }

    // 从平台边缘走出去时，也需要进入空中状态
    public virtual void Fall()
    {
        if (!isGrounded)
        {
            states.Change<FallPlayerState>();
        }
    }
    
    protected override void Awake()
    {
        // 先完成父类的控制器与状态管理器引用初始化，再补玩家自己的输入引用
        base.Awake();
        InitializeInputs();
        InitializeStats();
    }

    protected virtual void InitializeInputs()
    {
        InputManager = GetComponent<PlayerInputManager>();
    }

    protected virtual void InitializeStats()
    {
        PlayerStats = GetComponent<PlayerStatsManager>();
    }
}
