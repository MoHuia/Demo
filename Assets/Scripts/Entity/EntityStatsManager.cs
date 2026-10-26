
using UnityEngine;

// 管理一种实体的配置资源，T 必须是对应的数值配置类型
public abstract class EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>
{
    // 可供选择的配置资源
    public T[] stats;

    // 当前使用的配置
    public T current { get; protected set; }

    // 根据数组索引切换配置
    public virtual void Change(int to)
    {
        if (to >= 0 && to < stats.Length)
        {
            if (current != stats[to])
            {
                current = stats[to];
            }
        }
    }

    protected virtual void Start()
    {
        // 启动时默认使用第一份配置
        if (stats.Length > 0)
        {
            current = stats[0];
        }
    }
}