using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class LiquidParticle : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float offscreenMargin = 200f;

    [Header("Joint Settings")]
    [SerializeField] private float jointSearchRadius = 0.1f;   // how far to look for neighbors
    [SerializeField] private float jointTargetDistance = 0.02f; // how tightly grains stick
    [SerializeField] private int maxConnections = 10;            // how many neighbors one grain can stick to

    [Header("Joint Break Settings")]
    [SerializeField] private float breakDistanceThreshold = 0.15f; // max distance allowed
    [SerializeField] private float breakYThreshold = 0.08f;        // max vertical gap allowed

    private Rigidbody2D rb;
    private float spawnTime;
    private Camera mainCamera;
    private int currentConnections = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spawnTime = Time.time;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Delay slightly so particles spawn before searching for neighbors
        Invoke(nameof(TryCreateJoints), 1f);
    }

    private void Update()
    {
        if (Time.time - spawnTime > lifetime)
        {
            Debug.Log($"{gameObject.name}: Destroyed due to lifetime.");
            Destroy(gameObject);
        }

        if (IsOffScreen())
        {
            Debug.Log($"{gameObject.name}: Destroyed for leaving screen.");
            Destroy(gameObject);
        }

        CheckBadJoints();
    }

    private void CheckBadJoints()
    {
        DistanceJoint2D[] joints = GetComponents<DistanceJoint2D>();
        foreach (var joint in joints)
        {
            if (joint == null || joint.connectedBody == null) continue;

            float yDiff = Mathf.Abs(transform.position.y - joint.connectedBody.position.y);

            // Only break if vertical gap exceeds threshold
            if (yDiff > breakYThreshold)
            {
                Debug.Log($"{gameObject.name}: Breaking joint with {joint.connectedBody.name} (y={yDiff:F3})");
                Destroy(joint);
                currentConnections = Mathf.Max(0, currentConnections - 1);
            }
        }
    }


    private void TryCreateJoints()
    {
        if (currentConnections >= maxConnections) return;

        var nearby = Physics2D.OverlapCircleAll(transform.position, jointSearchRadius);
        Debug.Log($"{gameObject.name}: Found {nearby.Length} colliders nearby.");

        foreach (var collider in nearby)
        {
            if (collider.gameObject == gameObject) continue;

            var otherParticle = collider.GetComponent<LiquidParticle>();
            if (otherParticle == null) continue;

            if (currentConnections >= maxConnections) break;

            if (AlreadyConnectedTo(collider.attachedRigidbody)) continue;

            CreateJoint(collider.attachedRigidbody);
        }

        if (currentConnections == 0)
        {
            Debug.Log($"{gameObject.name}: No joints created.");
        }
    }

    private bool AlreadyConnectedTo(Rigidbody2D other)
    {
        DistanceJoint2D[] joints = GetComponents<DistanceJoint2D>();
        foreach (var j in joints)
        {
            if (j.connectedBody == other) return true;
        }
        return false;
    }

    private void CreateJoint(Rigidbody2D other)
    {
        DistanceJoint2D joint = gameObject.AddComponent<DistanceJoint2D>();
        joint.autoConfigureDistance = false;
        joint.connectedBody = other;
        joint.distance = jointTargetDistance;
        joint.maxDistanceOnly = false;

        currentConnections++;
        Debug.Log($"{gameObject.name}: Created joint with {other.gameObject.name}");
    }

    private bool IsOffScreen()
    {
        if (mainCamera == null) return false;

        Vector2 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        return screenPos.x < -offscreenMargin ||
               screenPos.x > Screen.width + offscreenMargin ||
               screenPos.y < -offscreenMargin ||
               screenPos.y > Screen.height + offscreenMargin;
    }

    public Vector2 GetVelocity()
    {
        return rb != null ? rb.velocity : Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        // Search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, jointSearchRadius);

        // Target sticking distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, jointTargetDistance);

        // Draw connections
        Gizmos.color = Color.green;
        DistanceJoint2D[] joints = GetComponents<DistanceJoint2D>();
        foreach (var j in joints)
        {
            if (j.connectedBody != null)
            {
                Gizmos.DrawLine(transform.position, j.connectedBody.transform.position);
            }
        }
    }
}
