using UnityEngine;

/// <summary>
/// The Referee - ONLY component that wires everything together.
/// Coordinates all components through events.
/// All other components are independent and communicate through events only.
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    [Header("Core Components")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Metronome metronome;
    [SerializeField] private ChartComposer composer;
    [SerializeField] private RhythmJudge judge;

    [Header("Visual Components")]
    [SerializeField] private NoteSpawner noteSpawner;

    [Header("Timing Settings")]
    [SerializeField]
    [Tooltip("How forgiving the timing window is (±ms). Default 80ms = 160ms total window.")]
    private float timingWindowMs = 80f;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerHit = 100;
    [SerializeField] private int comboMultiplierThreshold = 10;

    // Game state
    private bool isGameActive = false;
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;
    private int hits = 0;
    private int misses = 0;

    // Events
    public event System.Action OnGameStart;
    public event System.Action OnGameEnd;
    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnComboChanged;

    // Public accessors
    public bool IsGameActive => isGameActive;
    public int Score => score;
    public int Combo => combo;
    public int MaxCombo => maxCombo;
    public int Hits => hits;
    public int Misses => misses;
    public float Accuracy => (hits + misses) > 0 ? (float)hits / (hits + misses) * 100f : 0f;

    // Singleton
    public static RhythmGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ValidateReferences();
        SubscribeToEvents();
    }

    private void ValidateReferences()
    {
        if (audioManager == null) Debug.LogError("Manager: AudioManager missing!");
        if (metronome == null) Debug.LogError("Manager: Metronome missing!");
        if (composer == null) Debug.LogError("Manager: Composer missing!");
        if (judge == null) Debug.LogError("Manager: Judge missing!");
        if (noteSpawner == null) Debug.LogWarning("Manager: NoteSpawner missing (optional)");
    }

    private void SubscribeToEvents()
    {
        if (audioManager != null)
        {
            audioManager.OnSongEnd += HandleSongEnd;
        }

        if (metronome != null)
        {
            metronome.OnWindowClose += HandleWindowClose;
        }

        if (composer != null)
        {
            composer.OnChartComplete += HandleChartComplete;
        }

        if (judge != null)
        {
            judge.OnSuccess += HandleJudgeSuccess;
            judge.OnFailure += HandleJudgeFailure;
        }

        Debug.Log("Manager: All events wired up");
    }

    public void StartGame()
    {
        if (isGameActive)
        {
            Debug.LogWarning("Manager: Game already active");
            return;
        }

        composer.GetChartInfo(out float bpm, out AudioClip audio, out float offsetMs);

        if (audio == null)
        {
            Debug.LogError("Manager: Chart has no audio");
            return;
        }

        Debug.Log($"<color=cyan>Manager: Chart loaded - BPM={bpm}, Offset={offsetMs}ms, TimingWindow=±{timingWindowMs}ms</color>");

        metronome.Initialize(audioManager, bpm, timingWindowMs);
        ResetGameState();

        audioManager.Play(audio, offsetMs);
        metronome.StartCounting();
        composer.StartChart();
        judge.StartListening();

        if (noteSpawner != null)
        {
            noteSpawner.StartSpawning(bpm);
        }

        UpdateJudgeGoal();

        isGameActive = true;
        OnGameStart?.Invoke();

        Debug.Log($"=== GAME STARTED === (BPM: {bpm}, Offset: {offsetMs}ms)");
    }

    public void StopGame()
    {
        if (!isGameActive)
        {
            Debug.LogWarning("Manager: Game not active");
            return;
        }

        audioManager.Stop();
        metronome.Stop();
        composer.StopChart();
        judge.StopListening();

        if (noteSpawner != null)
        {
            noteSpawner.StopSpawning();
        }

        isGameActive = false;
        OnGameEnd?.Invoke();

        LogFinalStats();
        Debug.Log("=== GAME STOPPED ===");
    }

    private void HandleJudgeSuccess(int beat, RhythmChart.Lane lane)
    {
        if (!isGameActive) return;

        hits++;
        combo++;

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }

        int multiplier = GetComboMultiplier();
        int earnedPoints = pointsPerHit * multiplier;
        score += earnedPoints;

        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(combo);

        Debug.Log($"<color=green>Manager: SUCCESS +{earnedPoints}pts | Combo {combo}x</color>");

        if (noteSpawner != null)
        {
            noteSpawner.HandleNoteHit(beat, lane);
        }

        composer.AdvanceToNextNote();
        UpdateJudgeGoal();
    }

    // --- CORRECTED FAILURE LOGIC ---
    private void HandleJudgeFailure(int beat, RhythmChart.Lane lane)
    {
        if (!isGameActive) return;

        misses++;
        combo = 0;

        OnComboChanged?.Invoke(combo);

        Debug.Log($"<color=red>Manager: FAILURE | Combo reset</color>");

        if (noteSpawner != null)
        {
            noteSpawner.HandleNoteMiss(beat, lane);
        }

        // A failure (wrong key or miss) still resolves the current note, so we must advance.
        composer.AdvanceToNextNote();
        UpdateJudgeGoal();
    }

    // --- CORRECTED WINDOW CLOSE LOGIC ---
    private void HandleWindowClose(int beat)
    {
        if (!isGameActive) return;

        var currentNote = composer.GetCurrentNote();

        // If window closed on our target beat, it's a miss.
        if (beat == currentNote.beatNumber)
        {
            // This will fire the OnFailure event.
            // Our corrected HandleJudgeFailure will now properly advance the chart.
            // We no longer need to advance the chart here.
            judge.NotifyMiss(beat);
        }
    }

    private void HandleChartComplete()
    {
        Debug.Log("Manager: Chart complete - waiting for song end");
    }

    private void HandleSongEnd()
    {
        if (isGameActive)
        {
            StopGame();
        }
    }

    private void UpdateJudgeGoal()
    {
        if (composer.IsComplete)
        {
            Debug.Log("Manager: No more notes");
            judge.SetGoal(-1, RhythmChart.Lane.Left); // Invalidate the judge's goal
            return;
        }

        var note = composer.GetCurrentNote();
        judge.SetGoal(note.beatNumber, note.lane);

        Debug.Log($"<color=yellow>Manager: Set judge goal -> Beat {note.beatNumber}, Lane {note.lane}</color>");
    }

    private int GetComboMultiplier()
    {
        return 1 + (combo / comboMultiplierThreshold);
    }

    private void ResetGameState()
    {
        score = 0;
        combo = 0;
        maxCombo = 0;
        hits = 0;
        misses = 0;

        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(combo);
    }

    private void LogFinalStats()
    {
        Debug.Log("=== FINAL STATS ===");
        Debug.Log($"Score: {score}");
        Debug.Log($"Hits: {hits} | Misses: {misses}");
        Debug.Log($"Accuracy: {Accuracy:F1}%");
        Debug.Log($"Max Combo: {maxCombo}");
    }

    private void OnDestroy()
    {
        if (audioManager != null)
            audioManager.OnSongEnd -= HandleSongEnd;

        if (metronome != null)
            metronome.OnWindowClose -= HandleWindowClose;

        if (composer != null)
            composer.OnChartComplete -= HandleChartComplete;

        if (judge != null)
        {
            judge.OnSuccess -= HandleJudgeSuccess;
            judge.OnFailure -= HandleJudgeFailure;
        }
    }

    public void SetTimingWindow(float newWindowMs)
    {
        timingWindowMs = newWindowMs;
        if (metronome != null)
        {
            metronome.SetErrorMargin(newWindowMs);
        }
        Debug.Log($"Manager: Timing window set to ±{timingWindowMs}ms");
    }
}