using UnityEngine;

// 状态是普通 C# 对象，由管理器创建和调用，不作为组件挂到 GameObject 上
// 统一状态生命周期，具体行为由子类实现 OnEnter、OnStep、OnExit
public abstract class EntityState<T> where T : Entity<T>
{
    // 自上一次进入该状态起，通过 Step 累计的游戏时间，单位为秒
    public float timeSinceEntered { get; protected set; }

    // 管理器进入状态时调用，先重置共用数据，再执行具体状态的进入逻辑
    public void Enter(T entity)
    {
        timeSinceEntered = 0;
        OnEnter(entity);
    }
    
    // 每调用一次，就执行一次当前状态逻辑，并累计一次本帧时间
    public void Step(T entity)
    {
        OnStep(entity);
        timeSinceEntered += Time.deltaTime;
    }

    // 管理器切换到其他状态前，调用旧状态的退出逻辑
    public void Exit(T entity)
    {
        OnExit(entity);
    }

    // abstract 要求具体状态实现这些方法；不需要行为时，也可以提供空的方法体
    protected abstract void OnEnter(T entity);

    protected abstract void OnStep(T entity);

    protected abstract void OnExit(T entity);
}
