using UnityEngine;

public class HitZone : MonoBehaviour
{
    [Header("Lane Settings")]
    [SerializeField] private Transform leftLaneTransform;
    [SerializeField] private Transform rightLaneTransform;
    [SerializeField] private float laneSpacing = 2.0f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer leftLaneRenderer;
    [SerializeField] private SpriteRenderer rightLaneRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private Color missColor = Color.red;
    [SerializeField] private float feedbackDuration = 0.1f;

    private float leftLaneFeedbackTimer = 0f;
    private float rightLaneFeedbackTimer = 0f;

    private void Start()
    {
        // Position lanes with 3D spacing
        if (leftLaneTransform != null)
        {
            leftLaneTransform.localPosition = new Vector3(-laneSpacing / 2f, 0f, 0f);
        }

        if (rightLaneTransform != null)
        {
            rightLaneTransform.localPosition = new Vector3(laneSpacing / 2f, 0f, 0f);
        }

        ResetColors();
    }

    private void Update()
    {
        if (leftLaneFeedbackTimer > 0f)
        {
            leftLaneFeedbackTimer -= Time.deltaTime;
            if (leftLaneFeedbackTimer <= 0f)
            {
                ResetLaneColor(RhythmChart.Lane.Left);
            }
        }

        if (rightLaneFeedbackTimer > 0f)
        {
            rightLaneFeedbackTimer -= Time.deltaTime;
            if (rightLaneFeedbackTimer <= 0f)
            {
                ResetLaneColor(RhythmChart.Lane.Right);
            }
        }
    }

    public void ShowHitFeedback(RhythmChart.Lane lane)
    {
        SpriteRenderer renderer = GetLaneRenderer(lane);
        if (renderer != null)
        {
            renderer.color = pressedColor;
            SetFeedbackTimer(lane, feedbackDuration);
        }
    }

    public void ShowMissFeedback(RhythmChart.Lane lane)
    {
        SpriteRenderer renderer = GetLaneRenderer(lane);
        if (renderer != null)
        {
            renderer.color = missColor;
            SetFeedbackTimer(lane, feedbackDuration);
        }
    }

    /// <summary>
    /// Gets the world position for a specific lane.
    /// Returns 3D position including Z depth.
    /// </summary>
    public Vector3 GetLanePosition(RhythmChart.Lane lane)
    {
        Transform laneTransform = lane == RhythmChart.Lane.Left ? leftLaneTransform : rightLaneTransform;
        return laneTransform != null ? laneTransform.position : transform.position;
    }

    private SpriteRenderer GetLaneRenderer(RhythmChart.Lane lane)
    {
        return lane == RhythmChart.Lane.Left ? leftLaneRenderer : rightLaneRenderer;
    }

    private void SetFeedbackTimer(RhythmChart.Lane lane, float duration)
    {
        if (lane == RhythmChart.Lane.Left)
        {
            leftLaneFeedbackTimer = duration;
        }
        else
        {
            rightLaneFeedbackTimer = duration;
        }
    }

    private void ResetLaneColor(RhythmChart.Lane lane)
    {
        SpriteRenderer renderer = GetLaneRenderer(lane);
        if (renderer != null)
        {
            renderer.color = normalColor;
        }
    }

    private void ResetColors()
    {
        if (leftLaneRenderer != null)
        {
            leftLaneRenderer.color = normalColor;
        }

        if (rightLaneRenderer != null)
        {
            rightLaneRenderer.color = normalColor;
        }
    }
}