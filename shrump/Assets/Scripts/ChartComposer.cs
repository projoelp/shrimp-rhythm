using UnityEngine;

/// <summary>
/// The Composer - manages chart progression and provides the current goal to the Judge.
/// </summary>
public class ChartComposer : MonoBehaviour
{
    [Header("Chart Data")]
    [SerializeField] private RhythmChart chart;

    private int currentNoteIndex = 0;
    private bool isActive = false;

    // Events - now using Lane instead of KeyCode
    public event System.Action<int, RhythmChart.Lane> OnNewGoal;
    public event System.Action OnChartComplete;

    // Public accessors
    public RhythmChart Chart => chart;
    public int CurrentNoteIndex => currentNoteIndex;
    public int TotalNotes => chart != null ? chart.NoteCount : 0;
    public bool IsComplete => currentNoteIndex >= TotalNotes;
    public float Progress => TotalNotes > 0 ? (float)currentNoteIndex / TotalNotes : 0f;

    public bool LoadChart(RhythmChart newChart)
    {
        if (newChart == null)
        {
            Debug.LogError("ChartComposer: Cannot load null chart!");
            return false;
        }

        if (!newChart.ValidateChart(out string error))
        {
            Debug.LogError($"ChartComposer: Chart validation failed - {error}");
            return false;
        }

        chart = newChart;
        currentNoteIndex = 0;

        Debug.Log($"ChartComposer: Loaded chart '{chart.chartName}' with {chart.NoteCount} notes");
        return true;
    }

    public void StartChart()
    {
        if (chart == null)
        {
            Debug.LogError("ChartComposer: No chart loaded!");
            return;
        }

        currentNoteIndex = 0;
        isActive = true;

        Debug.Log($"<color=cyan>ChartComposer: Starting chart with {chart.NoteCount} notes</color>");
        EmitCurrentGoal();

        Debug.Log("ChartComposer: Started chart playback");
    }

    public void StopChart()
    {
        isActive = false;
        Debug.Log("ChartComposer: Stopped chart playback");
    }

    public void AdvanceToNextNote()
    {
        if (!isActive) return;

        currentNoteIndex++;

        if (IsComplete)
        {
            Debug.Log("ChartComposer: Chart complete!");
            OnChartComplete?.Invoke();
            isActive = false;
        }
        else
        {
            EmitCurrentGoal();
        }
    }

    public RhythmChart.ChartNote GetCurrentNote()
    {
        if (chart == null || IsComplete)
        {
            return default;
        }

        return chart.GetNote(currentNoteIndex);
    }

    private void EmitCurrentGoal()
    {
        var note = GetCurrentNote();
        Debug.Log($"<color=cyan>ChartComposer: Emitting goal - Beat {note.beatNumber}, Lane {note.lane}</color>");
        OnNewGoal?.Invoke(note.beatNumber, note.lane);

        Debug.Log($"ChartComposer: New goal - Beat {note.beatNumber}, Lane {note.lane}");
    }

    public void GetChartInfo(out float bpm, out AudioClip audio)
    {
        if (chart != null)
        {
            bpm = chart.bpm;
            audio = chart.audioClip;
        }
        else
        {
            bpm = 120f;
            audio = null;
        }
    }
}