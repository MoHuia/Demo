using UnityEngine;

public class DashRunPlayerState : DodgePlayerState
{
    protected override int invulnerableFrames => 21;
    protected override int recoveryFrame => 24;

    protected override void OnEnter(Player player)
    {
        base.OnEnter(player);
        if (player.dashDirection.sqrMagnitude > 0.01f)
        {
            player.transform.rotation = Quaternion.LookRotation(player.dashDirection);
        }
    }

    protected override void ResumeMovement(Player player) => player.states.Change<RunPlayerState>();
}
