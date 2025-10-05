using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns note visuals based on chart data, scheduling them to arrive at the hit zone on beat.
/// </summary>
public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChartComposer composer;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private HitZone hitZone;
    [SerializeField] private RhythmJudge judge;

    [Header("Prefabs")]
    [SerializeField] private GameObject noteVisualPrefab;

    [Header("Spawn Settings")]
    [Tooltip("How many beats ahead of the hit zone notes will spawn. Higher value = slower notes, more warning.")]
    [SerializeField] private float scrollSpeed = 4.0f; // Notes appear 4 beats before they should be hit
    [SerializeField] private float spawnHeight = 10f; // How far above hit zone to spawn

    private float dynamicApproachTime;
    private bool isActive = false;
    private int nextNoteToSpawn = 0;
    private List<NoteVisual> activeNotes = new List<NoteVisual>();
    private float beatDurationMs;

    /// <summary>
    /// Starts spawning notes based on the loaded chart.
    /// </summary>
    public void StartSpawning(float bpm)
    {
        if (composer == null || composer.Chart == null)
        {
            Debug.LogError("NoteSpawner: No chart loaded!");
            return;
        }

        if (noteVisualPrefab == null)
        {
            Debug.LogError("NoteSpawner: Note visual prefab not assigned!");
            return;
        }

        beatDurationMs = (60.0f / bpm) * 1000.0f;
        // --- NEW CODE ---
        // Calculate the approach time in seconds based on our scroll speed (in beats)
        float secondsPerBeat = 60.0f / bpm;
        dynamicApproachTime = scrollSpeed * secondsPerBeat;
        // --- END NEW CODE ---

        nextNoteToSpawn = 0;
        isActive = true;

        // Subscribe to judge events
        if (judge != null)
        {
            judge.OnSuccess += HandleNoteHit;
            judge.OnFailure += HandleNoteMiss;
        }

        Debug.Log($"NoteSpawner: Started spawning with {composer.TotalNotes} notes, approach time {dynamicApproachTime}s");
    }

    /// <summary>
    /// Stops spawning and clears all active notes.
    /// </summary>
    public void StopSpawning()
    {
        isActive = false;

        // Clean up active notes
        foreach (var note in activeNotes)
        {
            if (note != null)
            {
                Destroy(note.gameObject);
            }
        }
        activeNotes.Clear();

        // Unsubscribe from events
        if (judge != null)
        {
            judge.OnSuccess -= HandleNoteHit;
            judge.OnFailure -= HandleNoteMiss;
        }

        Debug.Log("NoteSpawner: Stopped spawning");
    }

    private void Update()
    {
        if (!isActive) return;

        CheckForNoteSpawns();
    }

    private void CheckForNoteSpawns()
    {
        // Check if there are more notes to spawn
        if (nextNoteToSpawn >= composer.TotalNotes)
        {
            return;
        }

        // Get next note from chart
        RhythmChart.ChartNote nextNote = composer.Chart.GetNote(nextNoteToSpawn);


        // Calculate when this note's beat will occur
        double noteBeatTimeMs = nextNote.beatNumber * beatDurationMs;

        // Calculate when we should spawn it (approachTime before the beat)
        double noteSpawnTimeMs = noteBeatTimeMs - (dynamicApproachTime * 1000.0);

        // Check if it's time to spawn
        if (audioManager.SongTimeMs >= noteSpawnTimeMs)
        {
            SpawnNote(nextNote);
            nextNoteToSpawn++;
        }
    }

    private void SpawnNote(RhythmChart.ChartNote noteData)
    {
        if (noteVisualPrefab == null || hitZone == null)
        {
            Debug.LogError("NoteSpawner: Missing prefab or hit zone reference!");
            return;
        }

        // Get target position from hit zone
        Vector3 targetPosition = hitZone.GetLanePosition(noteData.lane);

        // Calculate spawn position (above hit zone)
        Vector3 spawnPosition = targetPosition + Vector3.up * spawnHeight;

        // Instantiate note
        GameObject noteObj = Instantiate(noteVisualPrefab, spawnPosition, Quaternion.identity, transform);
        NoteVisual noteVisual = noteObj.GetComponent<NoteVisual>();


        if (noteVisual == null)
        {
            Debug.LogError("NoteSpawner: Note prefab missing NoteVisual component!");
            Destroy(noteObj);
            return;
        }

        // Initialize note
        noteVisual.Initialize(noteData.beatNumber, noteData.lane, spawnPosition, targetPosition, dynamicApproachTime);
        noteVisual.OnReachedTarget += HandleNoteReachedTarget;

        activeNotes.Add(noteVisual);

        Debug.Log($"NoteSpawner: Spawned note for beat {noteData.beatNumber}, lane {noteData.lane}");
    }

    private void HandleNoteReachedTarget(NoteVisual note)
    {
        // Note reached hit zone without being hit (this is just cleanup)
        note.OnReachedTarget -= HandleNoteReachedTarget;
        activeNotes.Remove(note);
    }

    public void HandleNoteHit(int beat, RhythmChart.Lane lane)
    {
        // Find and destroy the note that was hit
        NoteVisual hitNote = FindNoteByBeat(beat);
        if (hitNote != null)
        {
            hitNote.OnHit();
            activeNotes.Remove(hitNote);

            // Show hit zone feedback
            if (hitZone != null)
            {
                hitZone.ShowHitFeedback(lane);
            }
        }
    }

    public void HandleNoteMiss(int beat, RhythmChart.Lane lane)
    {
        // Find and destroy the note that was missed
        NoteVisual missedNote = FindNoteByBeat(beat);
        if (missedNote != null)
        {
            missedNote.OnMiss();
            activeNotes.Remove(missedNote);

            // Show miss feedback
            if (hitZone != null)
            {
                hitZone.ShowMissFeedback(lane);
            }
        }
    }

    private NoteVisual FindNoteByBeat(int beat)
    {
        foreach (var note in activeNotes)
        {
            if (note != null && note.BeatNumber == beat)
            {
                return note;
            }
        }
        return null;
    }
}