using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LiquidRenderer : MonoBehaviour
{
    [Header("Shader Settings")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Transform renderQuad; // Reference to the quad with the shader
    [SerializeField] private int maxShaderParticles = 50;
    [SerializeField] private float particleRadius = 0.00002f;
    [SerializeField] private float threshold = 0.008f;
    [SerializeField] private float smoothing = 0.018f;
    [SerializeField] private float streamStretch = 2.8f;
    [SerializeField] private Color liquidColor = new Color(0.725f, 0.529f, 1.0f, 1.0f);
    [SerializeField] private Color outlineColor = new Color(0.769f, 0.749f, 0.925f, 1.0f);
    [SerializeField] private float outlineWidth = 0.002f;

    private Camera mainCamera;
    private Vector2 screenSize;

    private void Start()
    {
        mainCamera = Camera.main;
        UpdateScreenSize();
        // SetupShaderParameters(); // Commented out - edit material directly instead
    }

    private void UpdateScreenSize()
    {
        screenSize = new Vector2(Screen.width, Screen.height);
    }

    private void SetupShaderParameters()
    {
        if (waterMaterial == null)
        {
            Debug.LogError("Water material not assigned!");
            return;
        }

        waterMaterial.SetFloat("_ParticleRadius", particleRadius);
        waterMaterial.SetFloat("_Threshold", threshold);
        waterMaterial.SetFloat("_Smoothing", smoothing);
        waterMaterial.SetFloat("_StreamStretch", streamStretch);
        waterMaterial.SetVector("_PourDirection", new Vector4(0, 1, 0, 0));
        waterMaterial.SetColor("_LiquidColor", liquidColor);
        waterMaterial.SetColor("_OutlineColor", outlineColor);
        waterMaterial.SetFloat("_OutlineWidth", outlineWidth);
    }

    private void Update()
    {
        UpdateShaderPositions();
    }

    private void UpdateShaderPositions()
    {
        if (waterMaterial == null) return;

        // Find all liquid particles in the scene
        var particles = FindObjectsByType<LiquidParticle>(FindObjectsSortMode.None);

        // Sort by Y position (prioritize upper particles for streaming effect)
        var sortedParticles = particles.OrderBy(p => p.transform.position.y).ToList();

        int count = Mathf.Min(sortedParticles.Count, maxShaderParticles);

        float[] positions = new float[maxShaderParticles * 2];
        float[] velocities = new float[maxShaderParticles * 2];

        // Fill with particle data - now using world positions directly
        for (int i = 0; i < count; i++)
        {
            var particle = sortedParticles[i];
            if (particle == null) continue;

            Vector2 worldPos = particle.transform.position;

            positions[i * 2] = worldPos.x;
            positions[i * 2 + 1] = worldPos.y;

            Vector2 vel = particle.GetVelocity().normalized;
            velocities[i * 2] = vel.x;
            velocities[i * 2 + 1] = vel.y;
        }

        // Fill remaining with invalid markers
        for (int i = count; i < maxShaderParticles; i++)
        {
            positions[i * 2] = -1000f; // Use -1000 instead of -1 for world space
            positions[i * 2 + 1] = -1000f;
            velocities[i * 2] = 0f;
            velocities[i * 2 + 1] = 1f;
        }

        waterMaterial.SetInt("_ParticleCount", count);
        waterMaterial.SetFloatArray("_ParticlePositions", positions);
        waterMaterial.SetFloatArray("_ParticleVelocities", velocities);
    }

    // Public methods for adjusting liquid appearance
    public void SetLiquidTightness(float tightness)
    {
        if (waterMaterial != null)
        {
            float baseThreshold = 0.008f;
            float baseSmoothing = 0.018f;
            waterMaterial.SetFloat("_Threshold", baseThreshold * tightness);
            waterMaterial.SetFloat("_Smoothing", baseSmoothing * (2f - tightness));
        }
    }

    public void SetStreamStretch(float stretch)
    {
        if (waterMaterial != null)
        {
            waterMaterial.SetFloat("_StreamStretch", stretch);
        }
    }

    public void SetPourDirection(Vector2 direction)
    {
        if (waterMaterial != null && direction.magnitude > 0.1f)
        {
            Vector2 normalizedDir = direction.normalized;
            waterMaterial.SetVector("_PourDirection", new Vector4(normalizedDir.x, normalizedDir.y, 0, 0));
        }
    }
}