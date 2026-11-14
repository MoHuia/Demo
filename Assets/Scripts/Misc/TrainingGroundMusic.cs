using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class TrainingGroundMusic : MonoBehaviour
{
    [SerializeField] private AudioSource loopSource;
    private AudioSource m_introSource;

    private void Awake() => m_introSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        double start = AudioSettings.dspTime + 0.2;
        AudioClip intro = m_introSource.clip;
        m_introSource.PlayScheduled(start);
        loopSource.PlayScheduled(start + (double)intro.samples / intro.frequency);
    }

    private void OnDisable()
    {
        m_introSource.Stop();
        loopSource.Stop();
    }
}
