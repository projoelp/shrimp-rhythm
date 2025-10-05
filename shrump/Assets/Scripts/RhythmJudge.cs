using UnityEngine;

/// <summary>
/// Pure validator - only validates input timing and emits events.
/// No references to other components except Metronome (for timing windows).
/// Manager handles all coordination.
/// </summary>
public class RhythmJudge : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Metronome metronome;

    // Current goal state (set by Manager)
    private int targetBeat = -1;
    private RhythmChart.Lane targetLane;
    private bool isListening = false;

    // Events - Judge only emits, never consumes
    public event System.Action<int, RhythmChart.Lane> OnSuccess;
    public event System.Action<int, RhythmChart.Lane> OnFailure;

    private void Start()
    {
        if (metronome == null)
        {
            Debug.LogError("RhythmJudge: Metronome reference missing!");
        }
    }

    private void Update()
    {
        if (!isListening || targetBeat < 0) return;

        // Listen for input
        if (Input.GetKeyDown(KeyCode.A))
        {
            ValidateInput(RhythmChart.Lane.Left);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            ValidateInput(RhythmChart.Lane.Right);
        }
    }

    public void SetGoal(int beat, RhythmChart.Lane lane)
    {
        targetBeat = beat;
        targetLane = lane;
        Debug.Log($"<color=magenta>Judge: New goal set - Beat {beat}, Lane {lane}</color>");
    }

    // --- CORRECTED NOTIFY MISS LOGIC ---
    public void NotifyMiss(int beat)
    {
        // Check if the window that closed corresponds to our current target beat.
        if (beat == targetBeat)
        {
            Debug.Log($"<color=red>Judge: MISS - Window closed on beat {beat}</color>");

            // Store the details of the missed note before we clear the target.
            int missedBeat = targetBeat;
            RhythmChart.Lane missedLane = targetLane;

            // Clear the judge's target *before* telling the manager to set the next one.
            targetBeat = -1;

            // Now, invoke the failure event. The manager will handle advancing the chart.
            OnFailure?.Invoke(missedBeat, missedLane);
        }
    }

    public void StartListening()
    {
        isListening = true;
        Debug.Log("Judge: Started listening");
    }

    public void StopListening()
    {
        isListening = false;
        targetBeat = -1;
        Debug.Log("Judge: Stopped listening");
    }

    private void ValidateInput(RhythmChart.Lane pressedLane)
    {
        int? activeBeat = metronome.ActiveBeat;

        // A press is only judged if it happens during the timing window
        // of the specific beat we are waiting for.
        if (activeBeat.HasValue && activeBeat.Value == targetBeat)
        {
            // We are in the correct timing window. Now check the lane.
            if (pressedLane == targetLane)
            {
                // --- SUCCESS ---
                Debug.Log($"<color=green>Judge: SUCCESS - Beat {targetBeat}, Lane {targetLane}</color>");

                int successBeat = targetBeat;
                RhythmChart.Lane successLane = targetLane;

                targetBeat = -1;
                OnSuccess?.Invoke(successBeat, successLane);
            }
            else
            {
                // --- FAILURE (Wrong Lane) ---
                Debug.Log($"<color=red>Judge: Wrong lane - Expected {targetLane}, got {pressedLane}</color>");
                OnFailure?.Invoke(targetBeat, targetLane);
            }
        }
        else
        {
            // --- IGNORED INPUT ---
            // The key was pressed outside the correct timing window. Do nothing.
            Debug.Log("Judge: Ignored input (mistimed or irrelevant).");
        }
    }
}