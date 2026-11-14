using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityStateManager : MonoBehaviour
{
    public EntityStateManagerEvents events = new EntityStateManagerEvents();
}

public abstract class EntityStateManager<T> : EntityStateManager
    where T : Entity<T>
{
    protected List<EntityState<T>> m_list = new List<EntityState<T>>();

    protected Dictionary<Type, EntityState<T>> m_states = new Dictionary<Type, EntityState<T>>();

    public T entity { get; protected set; }

    public EntityState<T> current { get; protected set; }

    public EntityState<T> last { get; protected set; }

    public int index => m_list.IndexOf(current);

    public int lastIndex => m_list.IndexOf(last);

    protected abstract List<EntityState<T>> GetStateList();

    public virtual void Initialize(T owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        entity = owner;
        current = null;
        last = null;

        m_states.Clear();
        m_list = GetStateList();

        if (m_list == null || m_list.Count == 0)
        {
            throw new InvalidOperationException("状态列表不能为空");
        }

        foreach (var state in m_list)
        {
            if (state == null)
            {
                throw new InvalidOperationException("状态列表中存在空状态");
            }

            var type = state.GetType();

            if (m_states.ContainsKey(type))
            {
                throw new InvalidOperationException("状态列表中存在重复类型");
            }

            m_states.Add(type, state);
        }

        Change(m_list[0]);
    }

    public virtual void Change(int to)
    {
        if (to >= 0 && to < m_list.Count)
        {
            Change(m_list[to]);
        }
    }

    public virtual void Change<TState>() where TState : EntityState<T>
    {
        var type = typeof(TState);

        if (!m_states.TryGetValue(type, out var next))
        {
            Debug.LogError($"没有注册状态 {type.Name}", this);
            return;
        }

        Change(next);
    }

    public virtual void Change(EntityState<T> to, bool restart = false)
    {
        if (to == null || Time.timeScale <= 0f)
        {
            return;
        }

        if (current == to && !restart)
        {
            return;
        }

        if (current != null)
        {
            current.Exit(entity);
            events.onExit.Invoke(current.GetType());
            last = current;
        }

        current = to;

        current.Enter(entity);
        events.onEnter.Invoke(current.GetType());

        events.onChange.Invoke();
    }

    public void RestartCurrent()
    {
        Change(current, restart: true);
    }

    public virtual void Step()
    {
        if (current != null && Time.timeScale > 0f)
        {
            current.Step(entity);
        }
    }

    public virtual bool IsCurrentOfType(Type type)
    {
        return current != null && current.GetType() == type;
    }
}
