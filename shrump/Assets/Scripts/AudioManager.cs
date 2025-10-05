using UnityEngine;

/// <summary>
/// Manages audio playback and provides sample-accurate timing for the rhythm game.
/// This is the source of truth for all timing calculations.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float songOffsetMs = 0f; // Offset to account for silence at start of audio file

    private AudioSource audioSource;
    private double songStartDspTime; // DSP time when song started playing
    private bool isPlaying = false;
    private float currentOffsetMs = 0f; // Runtime offset (can be different from serialized default)

    // Events
    public event System.Action OnSongEnd;

    /// <summary>
    /// Current song time in milliseconds, accounting for offset.
    /// This is the source of truth for all rhythm timing.
    /// Negative values occur during the "approach time" before beat 0.
    /// </summary>
    public double SongTimeMs
    {
        get
        {
            if (!isPlaying) return -currentOffsetMs; // Return negative offset when not playing
            return ((AudioSettings.dspTime - songStartDspTime) * 1000.0) - currentOffsetMs;
        }
    }

    public bool IsPlaying => isPlaying;
    public float SongDuration => audioClip != null ? audioClip.length : 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        // Check if song has ended
        if (isPlaying && !audioSource.isPlaying)
        {
            Stop();
            OnSongEnd?.Invoke();
        }
    }

    /// <summary>
    /// Starts playing the audio clip at the next DSP time slot.
    /// Uses the default offset specified in the inspector.
    /// </summary>
    public void Play()
    {
        Play(audioClip, songOffsetMs);
    }

    /// <summary>
    /// Plays a specific audio clip with a custom offset.
    /// The offset creates a delay before beat 0 - useful for note approach time.
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="offsetMs">Time in milliseconds before beat 0 (can be negative for files with silence)</param>
    public void Play(AudioClip clip, float offsetMs = 0f)
    {
        if (clip == null)
        {
            Debug.LogError("AudioManager: No audio clip provided!");
            return;
        }

        if (isPlaying)
        {
            Debug.LogWarning("AudioManager: Already playing. Stopping current playback.");
            Stop();
        }

        currentOffsetMs = offsetMs;

        // Schedule playback at the next DSP time to ensure sample accuracy
        songStartDspTime = AudioSettings.dspTime;
        audioSource.clip = clip;
        audioSource.Play();
        isPlaying = true;

        Debug.Log($"AudioManager: Started playback at DSP time {songStartDspTime} with offset {currentOffsetMs}ms");
        Debug.Log($"AudioManager: Beat 0 will occur at song time 0ms (DSP time {songStartDspTime + (currentOffsetMs / 1000.0)})");
    }

    /// <summary>
    /// Stops playback and resets timing.
    /// </summary>
    public void Stop()
    {
        audioSource.Stop();
        isPlaying = false;
        songStartDspTime = 0.0;

        Debug.Log("AudioManager: Playback stopped");
    }

    /// <summary>
    /// Pauses playback (DSP time continues running).
    /// </summary>
    public void Pause()
    {
        if (isPlaying)
        {
            audioSource.Pause();
            Debug.Log("AudioManager: Playback paused");
        }
    }

    /// <summary>
    /// Resumes playback from pause.
    /// </summary>
    public void Resume()
    {
        if (isPlaying && !audioSource.isPlaying)
        {
            audioSource.UnPause();
            Debug.Log("AudioManager: Playback resumed");
        }
    }
}