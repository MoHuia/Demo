// 空中状态，处理上升、下降和空中水平移动
public class FallPlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
    }

    protected override void OnStep(Player player)
    {
        player.Gravity();
        player.SnapToGround();

        // 落地后的这一帧，如果刚好按下 Space，也允许再次起跳
        // Jump 内部会判断 isGrounded，因此空中不会触发二段跳
        player.Jump();

        var direction = player.InputManager.GetMovementDirection();

        // 空中暂时复用现有水平加速参数
        // 没有输入时保留水平惯性
        if (direction.sqrMagnitude > 0)
        {
            player.Accelerate(direction);
        }

        if (player.isGrounded)
        {
            // 有输入或仍有水平速度时，交给 Walk 继续移动或减速
            if (direction.sqrMagnitude > 0 || player.lateralVelocity.sqrMagnitude > 0)
            {
                player.states.Change<WalkPlayerState>();
            }
            else
            {
                player.states.Change<IdlePlayerState>();
            }
        }
    }

    protected override void OnExit(Player player)
    {
    }
}