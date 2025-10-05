using UnityEngine;

/// <summary>
/// Counts beats based on BPM and manages timing windows for input validation.
/// Relies on AudioManager for accurate song time.
/// </summary>
public class Metronome : MonoBehaviour
{
    [Header("Timing Settings")]
    [SerializeField] private float bpm = 150f;
    [SerializeField] private float errorMarginMs = 80f; // ±80ms window for input

    private AudioManager audioManager;
    private bool isRunning = false;

    // Beat tracking
    private int currentBeat = -1; // -1 means no beat has occurred yet
    private double nextBeatTimeMs = 0.0;
    private double beatDurationMs = 0.0;

    // Window tracking
    private int? activeBeat = null; // null when no window is open
    private double windowOpenTimeMs = 0.0;
    private double windowCloseTimeMs = 0.0;

    // Events
    public event System.Action<int> OnBeat;
    public event System.Action<int> OnWindowOpen;
    public event System.Action<int> OnWindowClose;

    // Public accessors
    public float BPM => bpm;
    public float ErrorMarginMs => errorMarginMs;
    public int CurrentBeat => currentBeat;
    public int? ActiveBeat => activeBeat;
    public bool IsRunning => isRunning;

    public void Initialize(AudioManager manager, float beatsPerMinute, float errorMargin)
    {
        audioManager = manager;
        bpm = beatsPerMinute;
        errorMarginMs = errorMargin;

        CalculateBeatDuration();

        Debug.Log($"Metronome: Initialized with BPM={bpm}, ErrorMargin={errorMarginMs}ms, BeatDuration={beatDurationMs}ms");
    }

    public void StartCounting()
    {
        if (audioManager == null)
        {
            Debug.LogError("Metronome: AudioManager not initialized!");
            return;
        }

        isRunning = true;
        currentBeat = -1;
        nextBeatTimeMs = 0.0; // First beat at time 0
        activeBeat = null;

        Debug.Log("Metronome: Started counting");
    }

    public void Stop()
    {
        isRunning = false;

        // Close any open window
        if (activeBeat.HasValue)
        {
            OnWindowClose?.Invoke(activeBeat.Value);
            activeBeat = null;
        }

        Debug.Log("Metronome: Stopped counting");
    }

    private void Update()
    {
        if (!isRunning || audioManager == null || !audioManager.IsPlaying)
            return;

        double currentTimeMs = audioManager.SongTimeMs;

        // Check if we've reached the next beat
        if (currentTimeMs >= nextBeatTimeMs)
        {
            currentBeat++;
            OnBeat?.Invoke(currentBeat);

            // Calculate next beat time
            nextBeatTimeMs += beatDurationMs;

            Debug.Log($"Metronome: Beat {currentBeat} at {currentTimeMs:F2}ms");
        }

        // Manage timing window
        UpdateTimingWindow(currentTimeMs);
    }

    private void UpdateTimingWindow(double currentTimeMs)
    {
        double nextWindowOpenTime = nextBeatTimeMs - errorMarginMs;
        double nextWindowCloseTime = nextBeatTimeMs + errorMarginMs;

        // Open window if we've entered the window and it's not already open
        if (currentTimeMs >= nextWindowOpenTime && !activeBeat.HasValue)
        {
            activeBeat = currentBeat + 1; // Window is for the NEXT beat
            windowOpenTimeMs = nextWindowOpenTime;
            windowCloseTimeMs = nextWindowCloseTime;

            OnWindowOpen?.Invoke(activeBeat.Value);
            Debug.Log($"Metronome: Window opened for beat {activeBeat.Value} at {currentTimeMs:F2}ms");
        }

        // Close window if we've passed it
        if (activeBeat.HasValue && currentTimeMs >= windowCloseTimeMs)
        {
            int closingBeat = activeBeat.Value;
            OnWindowClose?.Invoke(closingBeat);
            activeBeat = null;

            Debug.Log($"Metronome: Window closed for beat {closingBeat} at {currentTimeMs:F2}ms");
        }
    }

    private void CalculateBeatDuration()
    {
        beatDurationMs = (60.0 / bpm) * 1000.0;
    }

    /// <summary>
    /// Updates BPM dynamically (useful for tempo changes).
    /// </summary>
    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        CalculateBeatDuration();
        Debug.Log($"Metronome: BPM changed to {bpm}");
    }

    /// <summary>
    /// Updates error margin dynamically.
    /// </summary>
    public void SetErrorMargin(float newMarginMs)
    {
        errorMarginMs = newMarginMs;
        Debug.Log($"Metronome: Error margin changed to {errorMarginMs}ms");
    }
}