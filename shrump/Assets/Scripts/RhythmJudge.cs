using UnityEngine;

/// <summary>
/// Validates player input timing against the current goal from the Composer.
/// Handles two-lane input (Left/Right arrow keys).
/// </summary>
public class RhythmJudge : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Metronome metronome;
    [SerializeField] private RhythmGameManager gameManager;
    [SerializeField] private ChartComposer composer;

    private int targetBeat = -1;
    private RhythmChart.Lane targetLane;
    private bool isListening = false;
    private bool hasAttemptedCurrentBeat = false;

    // Events
    public event System.Action<int, RhythmChart.Lane> OnSuccess;
    public event System.Action<int, RhythmChart.Lane> OnFailure;

    private void Start()
    {
        ValidateReferences();

        if (metronome != null)
        {
            metronome.OnWindowClose += HandleWindowClose;
            Debug.Log("RhythmJudge: Subscribed to metronome WindowClose event");
        }

        if (composer != null)
        {
            composer.OnNewGoal += HandleNewGoal;
            composer.OnChartComplete += HandleChartComplete;
            Debug.Log("RhythmJudge: Subscribed to composer events");
        }
    }

    private void Update()
    {
        if (!isListening || targetBeat < 0) return;

        // Debug current state every frame
        DebugState();

        // Check for left arrow
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("<color=yellow>LEFT ARROW PRESSED</color>");
            ValidateInput(RhythmChart.Lane.Left);
        }

        // Check for right arrow
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("<color=yellow>RIGHT ARROW PRESSED</color>");
            ValidateInput(RhythmChart.Lane.Right);
        }
    }

    private void DebugState()
    {
        // Only log every 30 frames to avoid spam
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"Judge State: isListening={isListening}, targetBeat={targetBeat}, targetLane={targetLane}, activeBeat={metronome.ActiveBeat}");
        }
    }

    public void StartListening()
    {
        if (metronome == null || gameManager == null || composer == null)
        {
            Debug.LogError("RhythmJudge: Cannot start - missing references!");
            return;
        }

        isListening = true;
        Debug.Log("RhythmJudge: Started listening for input");
    }

    public void StopListening()
    {
        isListening = false;
        Debug.Log("RhythmJudge: Stopped listening for input");
    }

    private void HandleNewGoal(int beat, RhythmChart.Lane lane)
    {
        targetBeat = beat;
        targetLane = lane;
        hasAttemptedCurrentBeat = false;
        Debug.Log($"<color=magenta>RhythmJudge: RECEIVED NEW GOAL - Beat {beat}, Lane {lane}</color>");
    }

    private void ValidateInput(RhythmChart.Lane pressedLane)
    {
        // Prevent processing if already attempted this beat
        if (hasAttemptedCurrentBeat)
        {
            Debug.Log("<color=yellow>Already attempted this beat, ignoring input</color>");
            return;
        }

        // Mark that we've attempted this beat
        hasAttemptedCurrentBeat = true;

        // Step 1: Check if correct lane
        if (pressedLane != targetLane)
        {
            RegisterFailure();
            Debug.Log($"<color=red>Wrong lane!</color> Expected {targetLane}, pressed {pressedLane}");

            // Advance to next note even on wrong lane
            if (composer != null)
            {
                composer.AdvanceToNextNote();
            }
            return;
        }

        // Step 2: Get active beat from metronome
        int? activeBeat = metronome.ActiveBeat;

        // Step 3: Check if we're in a timing window
        if (!activeBeat.HasValue)
        {
            RegisterFailure();
            Debug.Log("<color=red>Not in timing window!</color>");

            // Advance to next note even on bad timing
            if (composer != null)
            {
                composer.AdvanceToNextNote();
            }
            return;
        }

        // Step 4: Check if it's the correct beat
        if (activeBeat.Value != targetBeat)
        {
            RegisterFailure();
            Debug.Log($"<color=red>Wrong beat!</color> Expected {targetBeat}, active is {activeBeat.Value}");

            // Advance to next note
            if (composer != null)
            {
                composer.AdvanceToNextNote();
            }
            return;
        }

        // Success!
        RegisterSuccess();
        Debug.Log($"<color=green>SUCCESS!</color> Hit beat {targetBeat} on {targetLane} lane");

        if (composer != null)
        {
            composer.AdvanceToNextNote();
        }
    }


    private void HandleWindowClose(int beat)
    {
        Debug.Log($"<color=orange>RhythmJudge: Window closed for beat {beat}. Target beat is {targetBeat}, hasAttempted={hasAttemptedCurrentBeat}</color>");

        // Only register miss if:
        // 1. This is the exact beat we're waiting for
        // 2. Player hasn't attempted it yet
        if (beat == targetBeat && targetBeat >= 0 && !hasAttemptedCurrentBeat)
        {
            RegisterFailure();
            Debug.Log($"<color=red>MISS!</color> Missed beat {beat} on {targetLane} lane (no input)");

            if (composer != null)
            {
                composer.AdvanceToNextNote();
            }
        }
    }

    private void HandleChartComplete()
    {
        Debug.Log("<color=cyan>Chart Complete!</color>");
        StopListening();
    }

    private void RegisterSuccess()
    {
        OnSuccess?.Invoke(targetBeat, targetLane);

        if (gameManager != null)
        {
            gameManager.RegisterHit();
        }
    }

    private void RegisterFailure()
    {
        OnFailure?.Invoke(targetBeat, targetLane);

        if (gameManager != null)
        {
            gameManager.RegisterMiss();
        }
    }

    private void ValidateReferences()
    {
        if (metronome == null)
        {
            Debug.LogError("RhythmJudge: Metronome reference missing!");
        }

        if (gameManager == null)
        {
            Debug.LogError("RhythmJudge: RhythmGameManager reference missing!");
        }

        if (composer == null)
        {
            Debug.LogError("RhythmJudge: ChartComposer reference missing!");
        }
    }

    private void OnDestroy()
    {
        if (metronome != null)
        {
            metronome.OnWindowClose -= HandleWindowClose;
        }

        if (composer != null)
        {
            composer.OnNewGoal -= HandleNewGoal;
            composer.OnChartComplete -= HandleChartComplete;
        }
    }
}