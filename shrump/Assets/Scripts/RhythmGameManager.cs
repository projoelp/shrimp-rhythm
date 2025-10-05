using UnityEngine;

/// <summary>
/// The Referee - coordinates all rhythm game components and manages game state.
/// This is the single entry point for starting/stopping the game.
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Metronome metronome;

    [Header("Game Settings")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float errorMarginMs = 80f;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerHit = 100;
    [SerializeField] private int comboMultiplierThreshold = 10; // Combo milestones for multiplier

    [Header("Chart")]
    [SerializeField] private ChartComposer composer;

    [Header("Visual Display")]
    [SerializeField] private NoteSpawner noteSpawner;


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

    // Singleton pattern for easy access
    public static RhythmGameManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Validate references
        if (audioManager == null)
        {
            Debug.LogError("RhythmGameManager: AudioManager reference missing!");
            return;
        }

        if (metronome == null)
        {
            Debug.LogError("RhythmGameManager: Metronome reference missing!");
            return;
        }

        // Initialize metronome
        metronome.Initialize(audioManager, bpm, errorMarginMs);

        // Subscribe to audio manager events
        audioManager.OnSongEnd += HandleSongEnd;

        Debug.Log("RhythmGameManager: Initialized and ready");

        // Validate composer
        if (composer == null)
        {
            Debug.LogError("RhythmGameManager: ChartComposer reference missing!");
            return;
        }

        // Subscribe to composer events
        composer.OnChartComplete += HandleChartComplete;

        Debug.Log("RhythmGameManager: Initialized and ready");
    }

    /// <summary>
    /// Starts the rhythm game.
    /// </summary>
    // Modify the StartGame() method:

    public void StartGame()
    {
        if (isGameActive)
        {
            Debug.LogWarning("RhythmGameManager: Game already active!");
            return;
        }

        // Get chart info
        composer.GetChartInfo(out float chartBpm, out AudioClip chartAudio);

        if (chartAudio == null)
        {
            Debug.LogError("RhythmGameManager: Chart has no audio clip!");
            return;
        }

        if (noteSpawner != null)
        {
            noteSpawner.StartSpawning(bpm);
        }

        // Get offset from chart
        float chartOffset = composer.Chart != null ? composer.Chart.offsetMs : 2000f;

        // Update BPM from chart
        bpm = chartBpm;
        metronome.Initialize(audioManager, bpm, errorMarginMs);

        // Reset game state
        ResetGameState();

        // Start components in order
        audioManager.Play(chartAudio, chartOffset); // Pass offset here
        metronome.StartCounting();
        composer.StartChart();

        isGameActive = true;
        OnGameStart?.Invoke();

        Debug.Log($"=== GAME STARTED === (Offset: {chartOffset}ms)");
    }


    /// <summary>
    /// Stops the game immediately.
    /// </summary>
    public void StopGame()
    {
        if (!isGameActive)
        {
            Debug.LogWarning("RhythmGameManager: Game not active!");
            return;
        }

        if (noteSpawner != null)
        {
            noteSpawner.StopSpawning();
        }

        // Stop components
        audioManager.Stop();
        metronome.Stop();

        isGameActive = false;
        OnGameEnd?.Invoke();

        LogFinalStats();
        Debug.Log("=== GAME STOPPED ===");
    }

    private void HandleChartComplete()
    {
        Debug.Log("=== CHART COMPLETE ===");
        // Game continues until song ends, but no more notes to play
    }

    /// <summary>
    /// Called by Judge when player successfully hits a note.
    /// </summary>
    public void RegisterHit()
    {
        if (!isGameActive) return;

        hits++;
        combo++;

        // Update max combo
        if (combo > maxCombo)
        {
            maxCombo = combo;
        }

        // Calculate score with combo multiplier
        int multiplier = GetComboMultiplier();
        int earnedPoints = pointsPerHit * multiplier;
        score += earnedPoints;

        OnScoreChanged?.Invoke(score);
        OnComboChanged?.Invoke(combo);

        Debug.Log($"<color=green>HIT!</color> +{earnedPoints} pts | Combo: {combo}x | Score: {score}");
    }

    /// <summary>
    /// Called by Judge when player misses a note.
    /// </summary>
    public void RegisterMiss()
    {
        if (!isGameActive) return;

        misses++;
        combo = 0; // Reset combo on miss

        OnComboChanged?.Invoke(combo);

        Debug.Log($"<color=red>MISS!</color> Combo reset | Misses: {misses}");
    }

    private int GetComboMultiplier()
    {
        // Simple multiplier: 1x at 0-9 combo, 2x at 10-19, 3x at 20+, etc.
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

    private void HandleSongEnd()
    {
        if (isGameActive)
        {
            StopGame();
        }
    }

    private void LogFinalStats()
    {
        Debug.Log("=== FINAL STATS ===");
        Debug.Log($"Score: {score}");
        Debug.Log($"Hits: {hits}");
        Debug.Log($"Misses: {misses}");
        Debug.Log($"Accuracy: {Accuracy:F1}%");
        Debug.Log($"Max Combo: {maxCombo}");
    }

    private void OnDestroy()
    {
        if (audioManager != null)
        {
            audioManager.OnSongEnd -= HandleSongEnd;
        }

        if (composer != null)
        {
            composer.OnChartComplete -= HandleChartComplete;
        }
    }

    // Public utility methods
    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        if (metronome != null)
        {
            metronome.SetBPM(newBpm);
        }
    }

    public void SetErrorMargin(float newMarginMs)
    {
        errorMarginMs = newMarginMs;
        if (metronome != null)
        {
            metronome.SetErrorMargin(newMarginMs);
        }
    }
}