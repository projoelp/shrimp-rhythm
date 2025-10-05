using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NoteVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color leftLaneColor = new Color(0.3f, 0.6f, 1.0f);
    [SerializeField] private Color rightLaneColor = new Color(1.0f, 0.3f, 0.6f);

    [Header("3D Settings")]
    [SerializeField] private bool scaleWithDepth = true;
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float endScale = 1.0f;

    private SpriteRenderer spriteRenderer;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private float travelTime;
    private float elapsedTime = 0f;
    private bool isMoving = false;

    private int beatNumber;
    private RhythmChart.Lane lane;

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

        // Move toward target in 3D space
        transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

        // Scale with depth for perspective effect
        if (scaleWithDepth)
        {
            float scale = Mathf.Lerp(startScale, endScale, progress);
            transform.localScale = Vector3.one * scale;
        }

        // Check if reached target
        if (progress >= 1.0f)
        {
            isMoving = false;
            OnReachedTarget?.Invoke(this);
            Destroy(gameObject, 0.2f);
        }
    }

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
        transform.localScale = Vector3.one * startScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = lane == RhythmChart.Lane.Left ? leftLaneColor : rightLaneColor;
        }
    }

    public void OnHit()
    {
        isMoving = false;
        OnReachedTarget = null;
        // TODO: Play hit animation/particles
        Destroy(gameObject);
    }

    public void OnMiss()
    {
        isMoving = false;
        OnReachedTarget = null;
        // TODO: Play miss animation
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            Gizmos.color = lane == RhythmChart.Lane.Left ? Color.cyan : Color.magenta;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}