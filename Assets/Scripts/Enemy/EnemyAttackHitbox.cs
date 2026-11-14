using System;
using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    private const float k_MinSweepDistanceSquared = 1f / 1_000_000;

    [SerializeField, Min(0f)] private float hitTime = 1f;
    [SerializeField] private Vector3 localCenter = new Vector3(0f, 1.2f, 2f);
    [SerializeField, Min(0.01f)] private float radius = 0.9f;
    [SerializeField, Min(1)] private int damage = 15;
    [SerializeField] private bool launchOnHit;
    [SerializeField] private Transform strikePoint;
    [SerializeField, Min(0.01f)] private float strikeRadius = 0.4f;
    [SerializeField, Min(0.01f)] private float hitWindow = 0.12f;

    [SerializeField] private Attack[] attackSequence;

    [Serializable]
    internal sealed class Attack
    {
        public string stateName;
        public HitWindow[] hits;
        public DetectionWindow[] detections;
        public AimWindow[] aimWindows;
        public ActionShake[] actionShakes;
        [Min(0)] public int aimUntilFrame;
        public bool aimInPlace;
        [Min(0)] public int finishFrame;
        public string hitTargetState;
        public string hitWallState;
        public string hitWallEndState;
    }

    [Serializable]
    internal sealed class AimWindow
    {
        public float startFrame;
        public float endFrame;
        public float startSpeed;
        public float endSpeed;

        internal float GetSpeed(float frame)
        {
            return frame >= startFrame && frame < endFrame
                ? Mathf.Lerp(startSpeed, endSpeed, Mathf.InverseLerp(startFrame, endFrame, frame))
                : 0f;
        }
    }

    [Serializable]
    internal sealed class DetectionWindow
    {
        public int frame;
        public float duration;
        public Vector3 offset;
        public float radius;
        public float height;
        public float angle;
    }

    [Serializable]
    internal sealed class ActionShake
    {
        [Min(0)] public int frame;
        public CombatCameraShakeSettings settings;
    }

    [Serializable]
    internal sealed class HitWindow
    {
        [Min(0f)] public float start;
        [Min(0.01f)] public float duration;
        [Min(0)] public int attackFrame;
        [Min(0f)] public float cylinderHeight;
        [Range(0f, 360f)] public float sectorAngle;
        public Transform point;
        public Transform secondPoint;
        public Vector3 offset;
        [Min(0.01f)] public float radius;
        [Min(1)] public int damage;
        public bool launch;
        public bool heavyReaction;
        internal PlayerHitReaction reaction => launch ? PlayerHitReaction.Launch :
            heavyReaction ? PlayerHitReaction.Heavy : PlayerHitReaction.Light;
        [Min(0)] public int hitStopFrames;
        public AudioClip impactAudio;
        [Min(0)] public int followUpHitStopFrames;
        public CombatCameraShakeSettings hitShake;
    }

    internal Attack GetAttack(ref int nextIndex)
    {
        if (attackSequence == null || attackSequence.Length == 0)
        {
            return new Attack
            {
                stateName = "Attack01",
                hits = new[] { new HitWindow
                {
                    start = hitTime, duration = strikePoint != null ? hitWindow : float.PositiveInfinity,
                    point = strikePoint, offset = strikePoint != null ? Vector3.zero : localCenter,
                    radius = strikePoint != null ? strikeRadius : radius, damage = damage, launch = launchOnHit
                } }
            };
        }

        Attack attack = attackSequence[nextIndex];
        nextIndex = (nextIndex + 1) % attackSequence.Length;
        return attack;
    }

    internal Vector3 GetCenter(HitWindow hit, Transform point) =>
        (point != null ? point : transform).TransformPoint(hit.offset);

    internal bool CheckCylinderContact(Player target, HitWindow hit)
    {
        return target.controller.enabled && CheckCylinderContact(target.controller.bounds, hit);
    }

    internal bool CheckCylinderContact(Bounds bounds, HitWindow hit)
    {
        return CheckSectorContact(bounds, GetCenter(hit, hit.point), hit.radius, hit.cylinderHeight, hit.sectorAngle);
    }

    internal bool CheckSectorContact(Player target, Vector3 center, float radius, float height, float angle)
    {
        return target.controller.enabled && CheckSectorContact(target.controller.bounds, center, radius, height, angle);
    }

    internal bool CheckSectorContact(Bounds targetBounds, Vector3 center, float radius, float height, float angle)
    {
        float halfHeight = height * 0.5f;
        if (targetBounds.min.y > center.y + halfHeight || targetBounds.max.y < center.y - halfHeight)
        {
            return false;
        }

        Vector3 horizontal = targetBounds.center - center;
        horizontal.y = 0f;
        float capsuleRadius = targetBounds.extents.x;
        float reach = radius + capsuleRadius;
        if (horizontal.sqrMagnitude > reach * reach)
        {
            return false;
        }

        float outsideAngle = Vector3.Angle(transform.forward, horizontal) - angle * 0.5f;
        if (angle <= 0f || angle >= 360f || outsideAngle <= 0f)
        {
            return true;
        }

        float distance = horizontal.magnitude;
        float projection = distance * Mathf.Cos(outsideAngle * Mathf.Deg2Rad);
        float closest = Mathf.Clamp(projection, 0f, radius);
        float separationSquared = distance * distance + closest * closest - 2f * projection * closest;
        return separationSquared <= capsuleRadius * capsuleRadius;
    }

    internal static bool CheckDodgeOrigin(Bounds bounds, Vector3 previousCenter, Vector3 center, float radius)
    {
        float capsuleRadius = bounds.extents.x;
        float segmentHeight = Mathf.Max(0f, bounds.extents.y - capsuleRadius);
        Vector3 delta = center - previousCenter;
        float horizontalLength = delta.x * delta.x + delta.z * delta.z;
        Vector3 toBounds = bounds.center - previousCenter;
        float progress = horizontalLength > k_MinSweepDistanceSquared
            ? Mathf.Clamp01((toBounds.x * delta.x + toBounds.z * delta.z) / horizontalLength)
            : 1f;
        Vector3 separation = previousCenter + delta * progress - bounds.center;
        separation.y = Mathf.Max(0f, Mathf.Abs(separation.y) - segmentHeight);
        float reach = radius + capsuleRadius;
        return separation.sqrMagnitude <= reach * reach;
    }

    internal bool CheckContact(Player target, Vector3 previousCenter, Vector3 center, float hitRadius)
    {
        int targetLayer = 1 << target.controller.gameObject.layer;

        Collider[] hits = (previousCenter - center).sqrMagnitude > k_MinSweepDistanceSquared
            ? Physics.OverlapCapsule(previousCenter, center, hitRadius, targetLayer, QueryTriggerInteraction.Ignore)
            : Physics.OverlapSphere(center, hitRadius, targetLayer, QueryTriggerInteraction.Ignore);

        foreach (var collider in hits)
        {
            if (collider == target.controller)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        var attack = GetComponent<Enemy>()?.states?.current as AttackEnemyState;
        if (attack == null)
        {
            return;
        }

        var hit = attack.activeHit ?? attack.nextHit;
        if (hit != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Vector3 center = GetCenter(hit, hit.point);
            if (hit.attackFrame > 0)
            {
                DrawSector(center, hit.radius, hit.cylinderHeight, hit.sectorAngle);
            }
            else
            {
                Gizmos.DrawWireSphere(center, hit.radius);
                if (hit.secondPoint != null)
                {
                    Gizmos.DrawWireSphere(GetCenter(hit, hit.secondPoint), hit.radius);
                }
            }
        }

        var probe = attack.activeDetection;
        if (probe != null)
        {
            Gizmos.color = attack.detectedTarget ? Color.green : Color.cyan;
            DrawSector(transform.TransformPoint(probe.offset), probe.radius, probe.height, probe.angle);
        }
    }

    private void DrawSector(Vector3 center, float radius, float height, float angle)
    {
        const int segments = 32;
        bool fullCircle = angle <= 0f || angle >= 360f;
        float span = fullCircle ? 360f : angle;
        Vector3 up = Vector3.up * (height * 0.5f);
        Vector3 previous = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            Vector3 edge = center + Quaternion.AngleAxis(span * (i / (float)segments - 0.5f), Vector3.up) *
                transform.forward * radius;
            if (i > 0)
            {
                Gizmos.DrawLine(previous + up, edge + up);
                Gizmos.DrawLine(previous - up, edge - up);
            }
            if (i % 8 == 0)
            {
                Gizmos.DrawLine(edge - up, edge + up);
            }
            if (!fullCircle && (i == 0 || i == segments))
            {
                Gizmos.DrawLine(center + up, edge + up);
                Gizmos.DrawLine(center - up, edge - up);
            }
            previous = edge;
        }
    }
}
