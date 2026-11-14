using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class CombatAttackAudio : MonoBehaviour
{
    [Serializable]
    private struct Cue
    {
        [Min(0)] public int frame;
        public AudioClip clip;
        public AudioClip[] variants;
    }

    [Serializable]
    private sealed class Track
    {
        public string state;
        public Cue[] cues;
        public AudioClip[] impacts;
    }

    private const float FramesPerSecond = 60f;
    [SerializeField] private Track[] tracks = Array.Empty<Track>();
    private AudioSource m_source;
    private Animator m_animator;
    private int m_stateHash;
    private Track m_track;
    private int m_nextCue;
    private int m_cueLoop;
    private int m_dodgeExitHash;
    private Cue m_dodgeExitCue;

    private void Awake() => m_source = GetComponent<AudioSource>();

    internal void Begin(Animator animator, string state, int stateHash, float startTime)
    {
        m_dodgeExitHash = 0;

        if (m_track?.state == "Base Layer.DashRun" &&
            (state == "Base Layer.Run" || state == "Base Layer.Idle") &&
            m_nextCue == m_track.cues.Length - 1)
        {
            m_dodgeExitHash = m_stateHash;
            m_dodgeExitCue = m_track.cues[m_nextCue];
        }

        m_animator = animator;
        m_stateHash = stateHash;
        m_nextCue = 0;
        m_cueLoop = 0;
        m_track = Array.Find(tracks, track => track.state == state);
        if (m_track == null)
        {
            return;
        }

        Cue[] cues = m_track.cues;
        while (m_nextCue < cues.Length && cues[m_nextCue].frame / FramesPerSecond < startTime)
        {
            m_nextCue++;
        }
    }

    internal void Clear()
    {
        m_track = null;
        m_dodgeExitHash = 0;
    }

    internal void PlayImpact(int hitIndex)
    {
        AudioClip[] impacts = m_track?.impacts;
        if (impacts != null && hitIndex >= 0 && hitIndex < impacts.Length)
        {
            PlayImpact(impacts[hitIndex]);
        }
    }

    internal void PlayImpact(AudioClip clip)
    {
        if (isActiveAndEnabled && clip != null)
        {
            m_source.PlayOneShot(clip);
        }
    }

    private void LateUpdate()
    {
        if (m_dodgeExitHash != 0)
        {
            AnimatorStateInfo outgoing = m_animator.GetCurrentAnimatorStateInfo(0);
            float outgoingTime = outgoing.normalizedTime * outgoing.length;
            if (outgoing.fullPathHash != m_dodgeExitHash)
            {
                m_dodgeExitHash = 0;
            }
            else if (!float.IsInfinity(outgoingTime) && m_dodgeExitCue.frame / FramesPerSecond <= outgoingTime)
            {
                PlayCue(m_dodgeExitCue);
                m_dodgeExitHash = 0;
            }
        }

        Cue[] cues = m_track?.cues;
        if (cues == null || cues.Length == 0)
        {
            return;
        }

        AnimatorStateInfo state = m_animator.IsInTransition(0)
            ? m_animator.GetNextAnimatorStateInfo(0)
            : m_animator.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash != m_stateHash || float.IsInfinity(state.length))
        {
            return;
        }

        int currentLoop = state.loop ? Mathf.FloorToInt(state.normalizedTime) : 0;
        if (m_cueLoop < currentLoop)
        {
            if (m_cueLoop == currentLoop - 1)
            {
                PlayCuesThrough(cues, state.length);
            }

            m_cueLoop = currentLoop;
            m_nextCue = 0;
        }

        PlayCuesThrough(cues, (state.normalizedTime - currentLoop) * state.length);
    }

    private void PlayCuesThrough(Cue[] cues, float time)
    {
        while (m_nextCue < cues.Length && cues[m_nextCue].frame / FramesPerSecond <= time)
        {
            PlayCue(cues[m_nextCue++]);
        }
    }

    private void PlayCue(Cue cue)
    {
        int variant = cue.variants is { Length: > 0 }
            ? UnityEngine.Random.Range(0, cue.variants.Length + 1)
            : 0;
        m_source.PlayOneShot(variant == 0 ? cue.clip : cue.variants[variant - 1]);
    }

    private void OnDisable() => Clear();
}
