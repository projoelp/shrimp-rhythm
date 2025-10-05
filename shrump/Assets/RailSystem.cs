using UnityEngine;

/// <summary>
/// Visualizes the rails/lanes that notes travel along.
/// </summary>
public class RailVisualizer : MonoBehaviour
{
    [Header("Rail Settings")]
    [SerializeField] private Transform leftRailStart;
    [SerializeField] private Transform leftRailEnd;
    [SerializeField] private Transform rightRailStart;
    [SerializeField] private Transform rightRailEnd;

    [Header("Visual Settings")]
    [SerializeField] private Color railColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float railWidth = 0.1f;
    [SerializeField] private int segmentCount = 20;

    [Header("Line Renderer")]
    [SerializeField] private LineRenderer leftRailLineRenderer;
    [SerializeField] private LineRenderer rightRailLineRenderer;

    private void Start()
    {
        SetupRailRenderers();
    }

    private void SetupRailRenderers()
    {
        // Setup left rail
        if (leftRailLineRenderer != null && leftRailStart != null && leftRailEnd != null)
        {
            ConfigureLineRenderer(leftRailLineRenderer, leftRailStart.position, leftRailEnd.position);
        }

        // Setup right rail
        if (rightRailLineRenderer != null && rightRailStart != null && rightRailEnd != null)
        {
            ConfigureLineRenderer(rightRailLineRenderer, rightRailStart.position, rightRailEnd.position);
        }
    }

    private void ConfigureLineRenderer(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = railWidth;
        lineRenderer.endWidth = railWidth;
        lineRenderer.startColor = railColor;
        lineRenderer.endColor = railColor;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.sortingOrder = -1; // Behind notes
    }

    private void OnDrawGizmos()
    {
        // Draw rail paths in editor
        if (leftRailStart != null && leftRailEnd != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftRailStart.position, leftRailEnd.position);
        }

        if (rightRailStart != null && rightRailEnd != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(rightRailStart.position, rightRailEnd.position);
        }
    }
}