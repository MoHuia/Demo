
// 移动状态：根据键盘方向更新水平速度，松开按键后返回 Idle
public class WalkPlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
        // 当前移动不依赖动画或额外初始化，实际速度在 OnStep 中计算
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

        if (direction.sqrMagnitude > 0)
        {
            // 有输入时逐渐加速
            player.Accelerate(direction);
        }
        else
        {
            // 松开按键后仍留在 Walk 中，继续减速
            player.Friction();

            // 完全停止后再进入 Idle
            if (player.lateralVelocity.sqrMagnitude <= 0)
            {
                player.states.Change<IdlePlayerState>();
            }
        }
    }

    protected override void OnExit(Player player)
    {
        // 当前没有需要清理的移动状态数据，保留生命周期入口
    }
}
