using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float attractionSpeed = 5f;
    [SerializeField] private float collectRadius = 0.5f; // Particles within this distance are destroyed

    private bool isCollecting = false;
    private Vector3 targetPosition;
    private float collectionDuration;
    private Action onCollectionComplete;
    private List<LiquidParticle> particlesToCollect = new List<LiquidParticle>();

    public void StartCollection(Vector3 target, float duration, Action onComplete)
    {
        if (isCollecting)
        {
            Debug.LogWarning("Collection already in progress!");
            return;
        }

        targetPosition = target;
        collectionDuration = duration;
        onCollectionComplete = onComplete;

        // Find all liquid particles in the scene
        var allParticles = FindObjectsByType<LiquidParticle>(FindObjectsSortMode.None);
        particlesToCollect = new List<LiquidParticle>(allParticles);

        Debug.Log($"Starting collection of {particlesToCollect.Count} particles to {target}");

        isCollecting = true;
        StartCoroutine(CollectionRoutine());
    }

    private IEnumerator CollectionRoutine()
    {
        float elapsed = 0f;

        while (elapsed < collectionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / collectionDuration;

            // Move particles toward target
            for (int i = particlesToCollect.Count - 1; i >= 0; i--)
            {
                var particle = particlesToCollect[i];

                // Skip if particle was destroyed
                if (particle == null)
                {
                    particlesToCollect.RemoveAt(i);
                    continue;
                }

                // Lerp particle toward target
                Vector3 startPos = particle.transform.position;
                particle.transform.position = Vector3.Lerp(startPos, targetPosition, attractionSpeed * Time.deltaTime);

                // Check if particle reached target
                float distance = Vector3.Distance(particle.transform.position, targetPosition);
                if (distance < collectRadius)
                {
                    Destroy(particle.gameObject);
                    particlesToCollect.RemoveAt(i);
                }
            }

            yield return null;
        }

        // Destroy any remaining particles
        foreach (var particle in particlesToCollect)
        {
            if (particle != null)
            {
                Destroy(particle.gameObject);
            }
        }

        particlesToCollect.Clear();
        isCollecting = false;

        Debug.Log("Collection complete!");
        onCollectionComplete?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (isCollecting)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, collectRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPosition, 0.2f);
        }
    }
}