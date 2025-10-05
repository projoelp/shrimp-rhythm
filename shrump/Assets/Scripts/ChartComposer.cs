using UnityEngine;

/// <summary>
/// Pure data provider - stores chart and provides current goal.
/// Does NOT advance itself - Manager controls progression.
/// </summary>
public class ChartComposer : MonoBehaviour
{
    [Header("Chart Data")]
    [SerializeField] private RhythmChart chart;

    private int currentNoteIndex = 0;
    private bool isActive = false;

    // Events - Composer only emits when state changes
    public event System.Action OnChartComplete;

    // Public accessors - read-only state
    public RhythmChart Chart => chart;
    public int CurrentNoteIndex => currentNoteIndex;
    public int TotalNotes => chart != null ? chart.NoteCount : 0;
    public bool IsComplete => currentNoteIndex >= TotalNotes;
    public float Progress => TotalNotes > 0 ? (float)currentNoteIndex / TotalNotes : 0f;

    public bool LoadChart(RhythmChart newChart)
    {
        if (newChart == null)
        {
            Debug.LogError("Composer: Cannot load null chart");
            return false;
        }

        if (!newChart.ValidateChart(out string error))
        {
            Debug.LogError($"Composer: Chart validation failed - {error}");
            return false;
        }

        chart = newChart;
        currentNoteIndex = 0;

        Debug.Log($"Composer: Loaded '{chart.chartName}' with {chart.NoteCount} notes");
        return true;
    }

    public void StartChart()
    {
        if (chart == null)
        {
            Debug.LogError("Composer: No chart loaded");
            return;
        }

        currentNoteIndex = 0;
        isActive = true;

        Debug.Log($"<color=cyan>Composer: Started chart playback</color>");
    }

    public void StopChart()
    {
        isActive = false;
        Debug.Log("Composer: Stopped");
    }

    /// <summary>
    /// Manager calls this to advance to next note after validation
    /// </summary>
    public void AdvanceToNextNote()
    {
        if (!isActive) return;

        currentNoteIndex++;

        if (IsComplete)
        {
            Debug.Log("Composer: Chart complete!");
            OnChartComplete?.Invoke();
            isActive = false;
        }
        else
        {
            Debug.Log($"<color=cyan>Composer: Advanced to note {currentNoteIndex}/{TotalNotes}</color>");
        }
    }

    /// <summary>
    /// Get the current note (Manager queries this)
    /// </summary>
    public RhythmChart.ChartNote GetCurrentNote()
    {
        if (chart == null || IsComplete)
        {
            return default;
        }

        return chart.GetNote(currentNoteIndex);
    }

    /// <summary>
    /// Get chart metadata (Manager queries this)
    /// </summary>
    public void GetChartInfo(out float bpm, out AudioClip audio, out float offsetMs)
    {
        if (chart != null)
        {
            bpm = chart.bpm;
            audio = chart.audioClip;
            offsetMs = chart.offsetMs;
        }
        else
        {
            bpm = 120f;
            audio = null;
            offsetMs = 2000f;
        }
    }
}