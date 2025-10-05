using UnityEngine;
using TMPro;

/// <summary>
/// Complete test harness for the rhythm game system.
/// </summary>
public class RhythmGameTestHarness : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmGameManager gameManager;
    [SerializeField] private RhythmJudge judge;
    [SerializeField] private Metronome metronome;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private ChartComposer composer;

    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI songTimeText;
    [SerializeField] private TextMeshProUGUI beatInfoText;
    [SerializeField] private TextMeshProUGUI windowStatusText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI instructionsText;

    private void Start()
    {
        // Subscribe to game manager events
        gameManager.OnGameStart += HandleGameStart;
        gameManager.OnGameEnd += HandleGameEnd;
        gameManager.OnScoreChanged += HandleScoreChanged;
        gameManager.OnComboChanged += HandleComboChanged;

        // Update instructions
        if (instructionsText != null)
        {
            instructionsText.text = "Press SPACE to start game\n" +
                                   "Press SPACE on each beat to score\n" +
                                   "Press ESC to stop game";
        }
    }

    private void Update()
    {
        // Game control
        if (Input.GetKeyDown(KeyCode.Space) && !gameManager.IsGameActive)
        {
            Debug.Log("Starting game from test harness...");
            gameManager.StartGame();
            judge.StartListening(); // Make sure this is here
            Debug.Log($"Judge listening: {judge != null}");
        }

        if (Input.GetKeyDown(KeyCode.Escape) && gameManager.IsGameActive)
        {
            gameManager.StopGame();
            judge.StopListening();
        }

        // Update UI
        if (gameManager.IsGameActive)
        {
            UpdateDebugUI();
        }
    }

    // Replace the UpdateDebugUI method in RhythmGameTestHarness:

    private void UpdateDebugUI()
    {
        if (songTimeText != null)
        {
            songTimeText.text = $"Song Time: {audioManager.SongTimeMs:F2}ms";
        }

        if (beatInfoText != null)
        {
            var currentNote = composer.GetCurrentNote();
            beatInfoText.text = $"Current Beat: {metronome.CurrentBeat}\n" +
                               $"BPM: {metronome.BPM}\n" +
                               $"Target Beat: {currentNote.beatNumber}\n" +
                               $"Target Lane: {currentNote.lane}";
        }

        if (windowStatusText != null)
        {
            if (metronome.ActiveBeat.HasValue)
            {
                var currentNote = composer.GetCurrentNote();
                string arrowSymbol = currentNote.lane == RhythmChart.Lane.Left ? "<-" : "->";
                windowStatusText.text = $"<color=green>PRESS {arrowSymbol} NOW!</color>\n" +
                                       $"Beat: {metronome.ActiveBeat.Value}";
            }
            else
            {
                windowStatusText.text = "<color=grey>Wait for window...</color>";
            }
        }

        if (statsText != null)
        {
            statsText.text = $"Hits: {gameManager.Hits}\n" +
                            $"Misses: {gameManager.Misses}\n" +
                            $"Accuracy: {gameManager.Accuracy:F1}%\n" +
                            $"Max Combo: {gameManager.MaxCombo}\n" +
                            $"Progress: {composer.CurrentNoteIndex}/{composer.TotalNotes}";
        }
    }

    private void HandleGameStart()
    {
        Debug.Log("<color=cyan>=== GAME STARTED ===</color>");
    }

    private void HandleGameEnd()
    {
        Debug.Log("<color=cyan>=== GAME ENDED ===</color>");
    }

    private void HandleScoreChanged(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {newScore}";
        }
    }

    private void HandleComboChanged(int newCombo)
    {
        if (comboText != null)
        {
            if (newCombo > 0)
            {
                comboText.text = $"Combo: {newCombo}x";
            }
            else
            {
                comboText.text = "Combo: -";
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStart -= HandleGameStart;
            gameManager.OnGameEnd -= HandleGameEnd;
            gameManager.OnScoreChanged -= HandleScoreChanged;
            gameManager.OnComboChanged -= HandleComboChanged;
        }
    }
}