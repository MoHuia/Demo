
using UnityEngine;

// 实体共用的数据：控制器和速度，供 Player 等不同实体复用
public abstract class EntityBase : MonoBehaviour
{
    // 实际执行位移并处理碰撞的组件；外部可读取，当前类及子类可赋值
    public CharacterController CharacterController { get; protected set; }
    
    // 每秒移动的世界空间速度修改它只会改变数据，HandleController 才执行位移
    public Vector3 velocity { get; set; }

    // 水平速度是 velocity 的 XZ 部分，不是另一份独立的数据
    // 赋值时保留原来的 Y 速度，方便以后单独处理重力或跳跃
    public Vector3 lateralVelocity
    {
        get { return new Vector3(velocity.x, 0, velocity.z); }
        set { velocity = new Vector3(value.x, velocity.y, value.z); }
    }
    
    // 垂直速度是 velocity 的 Y 分量，赋值时保留水平速度
    public Vector3 verticalVelocity
    {
        get { return new Vector3(0, velocity.y, 0); }
        set { velocity = new Vector3(velocity.x, value.y, velocity.z); }
    }

    // 最近一次移动后是否接触地面
    public bool isGrounded { get; protected set; }
}

// T 表示具体实体类型，例如 Player : Entity<Player>
// 约束保证管理器和状态接收的是与当前实体匹配的类型
public abstract class Entity<T> : EntityBase where T : Entity<T>
{
    // 保存同一个 GameObject 上的状态管理器，由实体统一驱动其 Step
    public EntityStateManager<T> states { get; protected set; }

    // 沿输入方向加速，同时逐渐消除偏离输入方向的速度
    public virtual void Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)
    {
        if (direction.sqrMagnitude <= 0)
        {
            return;
        }

        // 速度投影计算需要的方向向量
        direction = direction.normalized;

        // 当前速度在输入方向上的分量
        var speed = Vector3.Dot(direction, lateralVelocity);
        var forwardVelocity = direction * speed;
        
        // 当前速度中偏离输入方向的部分
        var turningVelocity = lateralVelocity - forwardVelocity;
        
        // 尚未达到最高速度，或正在反向移动时，继续向输入方向加速
        if (lateralVelocity.magnitude < topSpeed || speed < 0)
        {
            speed += acceleration * Time.deltaTime;
            speed = Mathf.Clamp(speed, -topSpeed, topSpeed);
        }

        // 逐渐消除偏离输入方向的速度
        turningVelocity = Vector3.MoveTowards(turningVelocity, Vector3.zero, turningDrag * Time.deltaTime);
        
        // 合成新的水平速度，写回实体
        lateralVelocity = direction * speed + turningVelocity;
    }

    // 逐渐降低水平速度，直到完全停止
    public virtual void Decelerate(float deceleration)
    {
        lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, deceleration * Time.deltaTime);
    }
    
    // 空中持续增加向下的速度
    public virtual void Gravity(float gravity)
    {
        if (!isGrounded)
        {
            verticalVelocity += Vector3.down * gravity * Time.deltaTime;
        }
    }

    // 接触地面时保留一个小的向下速度，维持地面接触
    public virtual void SnapToGround(float force)
    {
        if (isGrounded && verticalVelocity.y <= 0)
        {
            verticalVelocity = Vector3.down * force;
        }
    }

    // 在 Move 执行后读取控制器的地面接触结果
    protected virtual void HandleGround()
    {
        isGrounded = CharacterController.isGrounded && verticalVelocity.y <= 0;
    }
    
    protected virtual void Awake()
    {
        // 优先使用已经配置好尺寸和中心点的控制器
        CharacterController = GetComponent<CharacterController>();

        if (!CharacterController)
        {
            // 未配置时补一个默认控制器；模型适配尺寸仍需要在 Inspector 调整
            CharacterController = gameObject.AddComponent<CharacterController>();
        }

        // 不设置最小位移门槛，让小幅度移动也能执行
        CharacterController.minMoveDistance = 0;

        InitializeStateManager();
    }

    protected virtual void Update()
    {
        // 先执行当前状态，更新本帧速度
        HandleStates();

        // 再根据更新后的速度执行位移
        HandleController();
        
        // 根据这次移动的结果更新落地标记
        HandleGround();
    }

    protected virtual void HandleController()
    {
        if (CharacterController.enabled)
        {
            var collisions = CharacterController.Move(velocity * Time.deltaTime);

            // 上升时撞到顶部，停止继续向上积累位移
            if ((collisions & CollisionFlags.Above) != 0 && verticalVelocity.y > 0)
            {
                verticalVelocity = Vector3.zero;
            }
        }
    }
    
    protected virtual void InitializeStateManager()
    {
        // 对 Player 来说，获取的实际组件是 PlayerStateManager
        states = GetComponent<EntityStateManager<T>>();
    }

    protected virtual void HandleStates()
    {
        // Step 是自定义方法；因为 Update 每帧调用它，当前状态才会持续执行
        states.Step();
    }
}
