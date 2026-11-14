using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private LayerMask hurtboxLayers;
    [SerializeField] private Vector3 localCenter = new Vector3(0f, 0.9f, 1.2f);
    [SerializeField, Min(0.01f)] private float radius = 0.65f;
    [SerializeField, Min(1)] private int damage = 10;
    private Player m_player;

    private void Awake() => m_player = GetComponentInParent<Player>();

    private static readonly CombatCameraShakeSettings k_HitShake = new CombatCameraShakeSettings
    {
        radius = 0.035f, frequency = 12f, duration = 0.6f, verticalAngle = 90f, ignoreTimeScale = true
    };

    public void CheckHit() => ResolveHit(damage, 0, 0, default);

    internal void CheckHit(float damageScale, int targetStopFrames, int attackerStopFrames, int hitIndex = 0) =>
        ResolveHit(Mathf.Max(1, Mathf.RoundToInt(damage * damageScale)),
            targetStopFrames, attackerStopFrames, k_HitShake, hitIndex: hitIndex);

    internal void CheckNormalHit(int hitIndex, int hitCount, int targetStopFrames, int attackerStopFrames,
        float shakeAngle, float cylinderRadius, float cylinderForward)
    {
        int remainder = damage % hitCount;
        int hitDamage = Mathf.Max(1, damage / hitCount + (hitIndex >= hitCount - remainder ? 1 : 0));
        var hitShake = k_HitShake;
        hitShake.verticalAngle = shakeAngle;
        ResolveHit(hitDamage, targetStopFrames, attackerStopFrames, hitShake, cylinderRadius, cylinderForward, hitIndex);
    }

    private void ResolveHit(int hitDamage, int targetStopFrames, int attackerStopFrames,
        CombatCameraShakeSettings hitShake, float cylinderRadius = 0f, float cylinderForward = 0f, int hitIndex = -1)
    {
        const float cylinderHalfHeight = 1f;
        bool useCylinder = cylinderRadius > 0f;
        Vector3 center = transform.TransformPoint(useCylinder
            ? new Vector3(0f, cylinderHalfHeight, cylinderForward) : localCenter);
        var colliders = useCylinder
            ? Physics.OverlapBox(center, new Vector3(cylinderRadius, cylinderHalfHeight, cylinderRadius),
                Quaternion.identity, hurtboxLayers, QueryTriggerInteraction.Collide)
            : Physics.OverlapSphere(center, radius, hurtboxLayers, QueryTriggerInteraction.Collide);

        var hitEnemies = new HashSet<Enemy>();
        bool landed = false;

        foreach (var collider in colliders)
        {
            if (useCylinder && !IntersectsCylinder(collider, center, cylinderRadius, cylinderHalfHeight))
            {
                continue;
            }

            var enemy = collider.GetComponentInParent<Enemy>();

            if (enemy != null && hitEnemies.Add(enemy) && enemy.ApplyDamage(hitDamage))
            {
                m_player?.RecordCombatHit();
                landed = true;

                if (targetStopFrames > 0 && enemy.health.currentHealth > 0)
                {
                    enemy.PlayTimedHitStop(enemy.animator.modelAnimator, targetStopFrames);
                }
            }
        }

        if (!landed || m_player == null)
        {
            return;
        }

        m_player.PlayTimedHitStop(m_player.animator.animator, attackerStopFrames);
        m_player.animator.PlayAttackImpact(hitIndex);
        m_player.RequestCameraShake(hitShake);
    }

    private static bool IntersectsCylinder(Collider hurtbox, Vector3 center, float radius, float halfHeight)
    {
        const float contactToleranceSquared = 1f / 1_000_000;
        const float convergenceToleranceSquared = 1f / 10_000_000_000;
        Vector3 point = center;
        for (int i = 0; i < 32; i++)
        {
            Vector3 closest = hurtbox.ClosestPoint(point);
            Vector3 offset = closest - center;
            float height = Mathf.Clamp(offset.y, -halfHeight, halfHeight);
            offset.y = 0f;
            Vector3 projected = center + Vector3.ClampMagnitude(offset, radius) + Vector3.up * height;
            if ((closest - projected).sqrMagnitude <= contactToleranceSquared)
            {
                return true;
            }
            if ((point - projected).sqrMagnitude <= convergenceToleranceSquared)
            {
                return false;
            }
            point = projected;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(localCenter), radius);
    }

    public Enemy FindAttackTarget(float range, float maxAngle)
    {
        var colliders = Physics.OverlapSphere(
            transform.position, range, hurtboxLayers, QueryTriggerInteraction.Collide);

        Enemy bestTarget = null;
        float bestDistanceSquared = range * range;

        foreach (var collider in colliders)
        {
            var enemy = collider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.health.currentHealth <= 0)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;

            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared <= 0.01f || distanceSquared > bestDistanceSquared ||
                Vector3.Angle(transform.forward, offset) > maxAngle)
            {
                continue;
            }

            bestTarget = enemy;
            bestDistanceSquared = distanceSquared;
        }

        return bestTarget;
    }
}
