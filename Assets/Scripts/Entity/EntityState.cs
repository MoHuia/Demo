using UnityEngine;

public abstract class EntityState<T> where T : Entity<T>
{
    public float timeSinceEntered { get; private set; }

    public void Enter(T entity)
    {
        timeSinceEntered = 0f;
        OnEnter(entity);
    }

    public void Exit(T entity) => OnExit(entity);

    public void Step(T entity)
    {
        timeSinceEntered += Time.deltaTime;
        OnStep(entity);
    }

    protected abstract void OnEnter(T entity);
    protected abstract void OnExit(T entity);
    protected abstract void OnStep(T entity);
}
