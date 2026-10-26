using System.Collections.Generic;

// 玩家专用管理器：复用通用切换逻辑，只提供玩家有哪些状态
public class PlayerStateManager : EntityStateManager<Player>
{
    protected override List<EntityState<Player>> GetStateList()
    {
        // 初始化时创建对象，之后由字典按类型查找并复用
        // Idle 排在第一位，所以正常启动时先进入 Idle
        return new List<EntityState<Player>>
        {
            new IdlePlayerState(),
            new WalkPlayerState(),
            new FallPlayerState()
        };
    }
}
