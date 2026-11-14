public abstract class PlayerState : EntityState<Player>
{
    public virtual bool isInvulnerable => false;

    private const float k_AttackBufferTime = 0.2f;
    private float m_attackBufferedUntil;

    protected void ResetAttackBuffer()
    {
        m_attackBufferedUntil = 0f;
    }

    protected bool TryCancelIntoAttack(Player player, bool canCancel, bool allowHeldAttack = false)
    {
        if (player.inputs.GetAttackDown())
        {
            m_attackBufferedUntil = timeSinceEntered + k_AttackBufferTime;
        }

        if (!canCancel || (timeSinceEntered >= m_attackBufferedUntil &&
                           !(allowHeldAttack && player.inputs.IsAttackHeld)))
        {
            return false;
        }

        ResetAttackBuffer();
        player.states.Change<NormalAttackPlayerState>();
        return true;
    }
}
