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

    private float lastSpawnTime;

    private void Update()
    {
        if (autoSpawn && Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnParticle(transform.position, spawnVelocity);
            lastSpawnTime = Time.time;
        }
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
            Debug.Log("spanwed particle");

            Vector2 offset = new Vector2(Random.Range(-2f, 2f), Random.Range(-2f, 2f));

            Instantiate(liquidParticlePrefab, position + offset, Quaternion.identity);
            Debug.Log("spanwed particle");
        }
        else
        {
            Instantiate(liquidParticlePrefab, position, Quaternion.identity);
            Debug.Log("spanwed particle");
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.1f, 0.5f));
    }

    
}
