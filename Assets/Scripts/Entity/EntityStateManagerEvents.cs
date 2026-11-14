using System;
using UnityEngine.Events;

[Serializable]
public class EntityStateManagerEvents
{
    public UnityEvent onChange = new UnityEvent();

    public UnityEvent<Type> onEnter = new UnityEvent<Type>();

    public UnityEvent<Type> onExit = new UnityEvent<Type>();
}
