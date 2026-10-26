
using System;
using System.Collections.Generic;
using UnityEngine;

// 不带泛型的公共组件基类，保留参考项目的继承层次，方便以后加入共用事件
public abstract class EntityStateManager : MonoBehaviour
{
    
}

// 管理某一种实体的状态；PlayerStateManager 会将 T 确定为 Player
public abstract class EntityStateManager<T> : EntityStateManager where T : Entity<T>
{
    // 保存状态对象及其顺序，第一个对象作为初始状态
    protected List<EntityState<T>> m_list = new List<EntityState<T>>();

    // 按“状态类型 → 状态对象”登记，切换时复用对象，不需要每次重新创建
    protected Dictionary<Type, EntityState<T>> m_states = new Dictionary<Type, EntityState<T>>();
    
    // 外部可以读取当前/上一个状态，赋值由管理器及其子类负责
    public EntityState<T> current { get; protected set; }
    
    public EntityState<T> last { get; protected set; }
    
    // 这台管理器服务的实体组件；调用状态方法时，将它作为参数传进去
    public T Entity { get; protected set; }

    // 通用管理器不知道有哪些状态，交给具体管理器提供列表
    protected abstract List<EntityState<T>> GetStateList();

    protected virtual void InitializeEntity()
    {
        // 管理器与实体需要挂在同一物体上，例如 role 上的 PlayerStateManager 和 Player
        Entity = GetComponent<T>();
    }

    protected virtual void InitializeStates()
    {
        // 调用具体子类的实现，取得创建好的状态对象
        m_list = GetStateList();

        foreach (var state in m_list)
        {
            // GetType 从对象取得实际类型，例如某个 Walk 对象的类型是 WalkPlayerState
            var type = state.GetType();

            if (!m_states.ContainsKey(type))
            {
                // 防止重复类型作为键被加入字典；这里不会修改原列表
                m_states.Add(type,state);
            }
        }

        if (m_list.Count > 0)
        {
            // 也通过 Change 进入初始状态，确保第一次就会执行 OnEnter
            Change(m_list[0]);
        }
    }

    // 按类型请求切换，例如 player.states.Change<WalkPlayerState>()
    public virtual void Change<TState>() where TState : EntityState<T>
    {
        // typeof 从类型名取得 Type，用它查找初始化时登记的状态对象
        var type = typeof(TState);

        if (m_states.ContainsKey(type))
        {
            // 调用下方接收状态对象的重载，由它真正完成切换
            Change(m_states[type]);
        }
    }

    // 切换顺序：退出旧状态 → 记录旧状态 → 更换 current → 进入新状态
    public virtual void Change(EntityState<T> to)
    {
        // 忽略空目标、重复进入当前状态以及暂停期间的切换请求
        // 初始化也经过这里，所以如果以 timeScale = 0 启动，初始状态同样不会进入
        if (to == null || to == current || Time.timeScale <= 0)
        {
            return;
        }
        
        // 第一次进入状态时 current 为空；?. 会跳过退出调用
        current?.Exit(Entity);
        last = current;

        current = to;
        current.Enter(Entity);
    }

    // 管理器没有自己的 Update，由 Entity.Update 统一决定何时执行 Step
    public virtual void Step()
    {
        if (current != null && Time.timeScale > 0)
        {
            // 最终进入具体状态的 OnStep(Entity)暂停时不执行状态逻辑
            current.Step(Entity);
        }
    }

    protected virtual void Start()
    {
        // 正常场景启动时先完成各组件的 Awake，再在首次 Update 前建立状态列表
        // 先取得实体，才能将有效的实体对象传给初始状态的 Enter
        InitializeEntity();
        InitializeStates();
    }
}
