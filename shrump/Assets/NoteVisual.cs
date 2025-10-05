using UnityEngine;

/// <summary>
/// Visual representation of a single note.
/// Moves from spawn position to hit zone at constant speed.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NoteVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color leftLaneColor = new Color(0.3f, 0.6f, 1.0f); // Blue
    [SerializeField] private Color rightLaneColor = new Color(1.0f, 0.3f, 0.6f); // Pink

    private SpriteRenderer spriteRenderer;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float travelTime;
    private float elapsedTime = 0f;
    private bool isMoving = false;

    // Note data
    private int beatNumber;
    private RhythmChart.Lane lane;

    // Events
    public event System.Action<NoteVisual> OnReachedTarget;

    public int BeatNumber => beatNumber;
    public RhythmChart.Lane Lane => lane;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!isMoving) return;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / travelTime);

        // Move toward target
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        // Check if reached target
        if (progress >= 1.0f)
        {
            isMoving = false;
            OnReachedTarget?.Invoke(this);

            // Auto-destroy after reaching target (cleanup for missed notes)
            Destroy(gameObject, 0.2f); // Small delay to allow judge to process
        }
    }

    /// <summary>
    /// Initializes the note and starts its movement.
    /// </summary>
    /// <param name="beat">Which beat this note represents</param>
    /// <param name="noteLane">Which lane this note belongs to</param>
    /// <param name="startPos">Starting position (top of screen)</param>
    /// <param name="targetPos">Target position (hit zone)</param>
    /// <param name="duration">How long to travel (approach time)</param>
    public void Initialize(int beat, RhythmChart.Lane noteLane, Vector3 startPos, Vector3 targetPos, float duration)
    {
        beatNumber = beat;
        lane = noteLane;
        startPosition = startPos;
        targetPosition = targetPos;
        travelTime = duration;
        elapsedTime = 0f;
        isMoving = true;

        transform.position = startPosition;

        // Set color based on lane
        if (spriteRenderer != null)
        {
            spriteRenderer.color = lane == RhythmChart.Lane.Left ? leftLaneColor : rightLaneColor;
        }
    }

    /// <summary>
    /// Called when the note is successfully hit by the player.
    /// </summary>
    public void OnHit()
    {
        isMoving = false;
        // TODO: Play hit animation/particles
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when the note is missed by the player.
    /// </summary>
    public void OnMiss()
    {
        isMoving = false;
        OnReachedTarget = null; // Clear event to prevent double-processing
                                // TODO: Play miss animation
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            // Draw line showing travel path
            Gizmos.color = lane == RhythmChart.Lane.Left ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}