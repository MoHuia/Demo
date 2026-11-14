using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[Serializable]
internal struct CombatCameraShakeSettings
{
    [Min(0f)] public float radius;
    [Min(0f)] public float frequency;
    [Min(0f)] public float duration;
    public float verticalAngle;
    public bool ignoreTimeScale;
    public bool softDecayStart;
}

[DisallowMultipleComponent]
public class CombatCameraShake : CinemachineExtension
{
    [SerializeField] private Player player;

    private static readonly AnimationCurve k_Decay = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, -3.075f),
        new Keyframe(1f, 0f, 0f, 0f));
    private static readonly AnimationCurve k_SoftStartDecay = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0f, 0f, 0f));

    private struct Shake
    {
        public CombatCameraShakeSettings settings;
        public float startedAt;
    }

    private readonly List<Shake> m_shakes = new List<Shake>();

    protected override void OnEnable()
    {
        base.OnEnable();
        if (player != null)
        {
            player.cameraShakeRequested += Play;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.cameraShakeRequested -= Play;
        }
        m_shakes.Clear();
    }

    private void Play(CombatCameraShakeSettings settings)
    {
        if (isActiveAndEnabled && settings.radius > 0f && settings.duration > 0f)
        {
            m_shakes.Add(new Shake { settings = settings, startedAt = GetTime(settings) });
        }
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        Vector3 weightedOffset = Vector3.zero;
        float totalWeight = 0f;
        for (int i = m_shakes.Count - 1; i >= 0; i--)
        {
            var shake = m_shakes[i];
            var settings = shake.settings;
            float elapsed = GetTime(settings) - shake.startedAt;
            if (elapsed >= settings.duration)
            {
                m_shakes.RemoveAt(i);
                continue;
            }

            var decay = settings.softDecayStart ? k_SoftStartDecay : k_Decay;
            float strength = settings.radius * decay.Evaluate(elapsed / settings.duration);
            float angle = settings.verticalAngle * Mathf.Deg2Rad;

            float wave = Mathf.Sin(elapsed * settings.frequency * 2f * Mathf.PI);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (strength * wave);

            float attack = Mathf.SmoothStep(0f, 1f, elapsed * settings.frequency * 4f);
            float weight = strength * strength * attack;
            weightedOffset += offset * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0f)
        {
            state.PositionCorrection += state.CorrectedOrientation * (weightedOffset / totalWeight);
        }
    }

    private static float GetTime(CombatCameraShakeSettings settings) =>
        settings.ignoreTimeScale ? Time.unscaledTime : Time.time;
}
