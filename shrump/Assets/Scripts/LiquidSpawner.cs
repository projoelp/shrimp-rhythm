using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject liquidParticlePrefab;
    [SerializeField] private bool spawnDualParticles = true;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private Vector2 spawnVelocity = Vector2.down * 100f;

    [Header("Spawn Rate Settings")]
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private bool autoSpawn = false;

    [Header("Fixed Amount Spawning")]
    [SerializeField] private int particlesToSpawn = 50; // Total particles to spawn when activated
    [SerializeField] private KeyCode fixedSpawnKey = KeyCode.F; // Key to spawn fixed amount

    private bool toggle_particleSpawn = false;
    private float lastSpawnTime;
    private bool isSpawningFixedAmount = false;
    private int spawnedCount = 0;
    private int targetSpawnCount = 0;

    private void Update()
    {
        // Toggle continuous auto-spawn
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            autoSpawn = !autoSpawn;
            Debug.Log($"Auto-spawn: {autoSpawn}");
        }

        // Start/stop fixed amount spawning
        if (Input.GetKeyDown(fixedSpawnKey))
        {
            if (isSpawningFixedAmount)
            {
                // Stop current fixed spawning
                isSpawningFixedAmount = false;
                spawnedCount = 0;
                Debug.Log("Fixed amount spawning stopped");
            }
            else
            {
                // Start new fixed spawning
                StartFixedSpawning();
            }
        }

        // Handle continuous auto-spawn
        if (autoSpawn && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnParticle(transform.position, spawnVelocity);
            lastSpawnTime = Time.time;
        }

        // Handle fixed amount spawning
        if (isSpawningFixedAmount && Time.time - lastSpawnTime >= spawnInterval)
        {
            if (spawnedCount < targetSpawnCount)
            {
                SpawnParticle(transform.position, spawnVelocity);
                lastSpawnTime = Time.time;
                spawnedCount++;

                // Check if we've spawned all particles
                if (spawnedCount >= targetSpawnCount)
                {
                    isSpawningFixedAmount = false;
                    Debug.Log($"Finished spawning {targetSpawnCount} particles");
                }
            }
        }
    }

    private void StartFixedSpawning()
    {
        isSpawningFixedAmount = true;
        spawnedCount = 0;
        targetSpawnCount = particlesToSpawn;
        lastSpawnTime = Time.time; // Reset timer to spawn immediately
        Debug.Log($"Starting fixed amount spawning: {particlesToSpawn} particles");
    }

    public void SpawnParticle(Vector2 position, Vector2 velocity)
    {
        if (liquidParticlePrefab == null)
        {
            Debug.LogError("Liquid particle not assigned");
            return;
        }

        if (spawnDualParticles)
        {
            Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            //Debug.Log("spawned particle");

            Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));
            Instantiate(liquidParticlePrefab, position + offset, Quaternion.identity);
            //Debug.Log("spawned particle");
        }
        else
        {
            Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            //Debug.Log("spawned particle");
        }

        var particles = Physics2D.OverlapCircleAll(position, 5f);
        foreach (var p in particles)
        {
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = velocity;
            }
        }
    }

    // Public method to start fixed spawning from other scripts
    public void StartFixedSpawning(int amount)
    {
        particlesToSpawn = amount;
        StartFixedSpawning();
    }

    // Public method to stop all spawning
    public void StopAllSpawning()
    {
        autoSpawn = false;
        isSpawningFixedAmount = false;
        spawnedCount = 0;
    }

    // Public properties to check spawning status
    public bool IsSpawningFixedAmount => isSpawningFixedAmount;
    public int SpawnedCount => spawnedCount;
    public int TargetSpawnCount => targetSpawnCount;
    public float SpawnProgress => targetSpawnCount > 0 ? (float)spawnedCount / targetSpawnCount : 0f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.1f, 0.5f));

        // Draw different color when fixed spawning is active
        if (isSpawningFixedAmount)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}