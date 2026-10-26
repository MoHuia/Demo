
using UnityEngine;

// 静止状态：停止水平移动，并等待键盘方向输入
public class IdlePlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
        // 每次进入 Idle 都停止水平移动，保留 velocity 中的垂直分量
        player.lateralVelocity = Vector3.zero;
    }
    
    protected override void OnStep(Player player)
    {
        player.Gravity();
        player.SnapToGround();
        
        player.Jump();
        player.Fall();
        
        // 起跳或离地后，结束旧地面状态的本次逻辑
        if (!player.isGrounded)
        {
            return;
        }
        
        var direction = player.InputManager.GetMovementDirection();

        // 平方长度大于 0 表示有方向输入，此处只判断是否为零，不需要开平方
        if (direction.sqrMagnitude > 0)
        {
            // 管理器会立刻执行 Idle.Exit 和 Walk.Enter；Walk.OnStep 在下次 Step 执行
            player.states.Change<WalkPlayerState>();
        }
    }

    protected override void OnExit(Player player)
    {
        // 当前没有需要清理的静止状态数据，保留生命周期入口
    }
}
