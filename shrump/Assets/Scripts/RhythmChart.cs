using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that stores rhythm chart data.
/// Designed for two-lane rhythm game (Left/Right inputs).
/// </summary>
[CreateAssetMenu(fileName = "NewRhythmChart", menuName = "Rhythm Game/Chart")]
public class RhythmChart : ScriptableObject
{
    public enum Lane
    {
        Left = 0,
        Right = 1
    }

    [System.Serializable]
    public struct ChartNote
    {
        public int beatNumber;  // Which beat this note occurs on
        public Lane lane;       // Which lane (Left or Right)

        public ChartNote(int beat, Lane noteLane)
        {
            beatNumber = beat;
            lane = noteLane;
        }

        // Helper to get the KeyCode for this lane
        // In RhythmChart.cs, modify GetKeyCode():
        public KeyCode GetKeyCode()
        {
            return lane == Lane.Left ? KeyCode.A : KeyCode.D;  // Use A/D instead of arrows
        }
    }

    [Header("Chart Info")]
    public string chartName = "Untitled Chart";
    public string songName = "Untitled Song";
    public float bpm = 120f;
    public AudioClip audioClip;

    [Header("Timing")]
    [Tooltip("Time in milliseconds before beat 0. Used for note approach time. Typically 2000-3000ms.")]
    public float offsetMs = 2000f; // Default 2 second approach time

    [Header("Chart Data")]
    public List<ChartNote> notes = new List<ChartNote>();

    /// <summary>
    /// Gets the note at a specific index.
    /// </summary>
    public ChartNote GetNote(int index)
    {
        if (index >= 0 && index < notes.Count)
        {
            return notes[index];
        }

        Debug.LogWarning($"RhythmChart: Invalid note index {index}");
        return default;
    }

    /// <summary>
    /// Gets the total number of notes in the chart.
    /// </summary>
    public int NoteCount => notes.Count;

    /// <summary>
    /// Finds the first note at or after a given beat number.
    /// </summary>
    public int FindNoteIndexAtBeat(int beatNumber)
    {
        for (int i = 0; i < notes.Count; i++)
        {
            if (notes[i].beatNumber >= beatNumber)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Validates the chart data for common errors.
    /// </summary>
    public bool ValidateChart(out string errorMessage)
    {
        errorMessage = "";

        if (notes.Count == 0)
        {
            errorMessage = "Chart has no notes!";
            return false;
        }

        if (audioClip == null)
        {
            errorMessage = "No audio clip assigned!";
            return false;
        }

        if (bpm <= 0)
        {
            errorMessage = "Invalid BPM!";
            return false;
        }

        // Check for notes sorted by beat number
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].beatNumber < notes[i - 1].beatNumber)
            {
                errorMessage = $"Notes not sorted! Note {i} (beat {notes[i].beatNumber}) comes before note {i - 1} (beat {notes[i - 1].beatNumber})";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Sorts notes by beat number (useful after manual editing).
    /// </summary>
    public void SortNotes()
    {
        notes.Sort((a, b) => a.beatNumber.CompareTo(b.beatNumber));
    }

#if UNITY_EDITOR
    /// <summary>
    /// Helper method for creating test charts in the editor.
    /// </summary>
    [ContextMenu("Generate Simple Test Chart (Alternating)")]
    private void GenerateAlternatingChart()
    {
        notes.Clear();

        // Simple alternating pattern: L-R-L-R
        for (int i = 0; i < 16; i++)
        {
            Lane lane = (i % 2 == 0) ? Lane.Left : Lane.Right;
            notes.Add(new ChartNote(i + 1, lane)); // Start at beat 1
        }

        chartName = "Test Chart (Alternating)";
        Debug.Log($"Generated alternating test chart with {notes.Count} notes");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Generate Test Chart (Left Only)")]
    private void GenerateLeftOnlyChart()
    {
        notes.Clear();

        // All left notes
        for (int i = 0; i < 16; i++)
        {
            notes.Add(new ChartNote(i + 1, Lane.Left));
        }

        chartName = "Test Chart (Left Only)";
        Debug.Log($"Generated left-only test chart with {notes.Count} notes");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Generate Test Chart (Right Only)")]
    private void GenerateRightOnlyChart()
    {
        notes.Clear();

        // All right notes
        for (int i = 0; i < 16; i++)
        {
            notes.Add(new ChartNote(i + 1, Lane.Right));
        }

        chartName = "Test Chart (Right Only)";
        Debug.Log($"Generated right-only test chart with {notes.Count} notes");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Generate Test Chart (Random)")]
    private void GenerateRandomChart()
    {
        notes.Clear();

        // Random pattern
        for (int i = 0; i < 32; i++)
        {
            Lane lane = Random.value > 0.5f ? Lane.Left : Lane.Right;
            notes.Add(new ChartNote(i + 1, lane));
        }

        chartName = "Test Chart (Random)";
        Debug.Log($"Generated random test chart with {notes.Count} notes");

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}