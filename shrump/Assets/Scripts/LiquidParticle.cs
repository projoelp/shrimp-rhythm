using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidParticle : MonoBehaviour
{
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float offscreenMargin = 200f;
    [SerializeField] private float jointDistance = 50f;
    [SerializeField] private float jointMinDistance = 10f;
    [SerializeField] private float jointSoftneess = 0.5f;

    private Rigidbody2D rb;
    private float spawnTime;
    private Camera mainCamera;
    private bool hasCreatedJoint = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnTime = Time.time;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        Invoke(nameof(TryCreateJoint), 0.1f);
    }

    private void TryCreateJoint()
    {
        if (hasCreatedJoint) return;
        var nearbyParticles = Physics2D.OverlapCircleAll(transform.position, jointDistance);

        foreach (var collider in nearbyParticles) 
        { 
            if (collider.gameObject == gameObject) continue;

            var otherParticle = collider.GetComponent<LiquidParticle>();
            if (otherParticle == null) continue;

            float distance = Vector2.Distance(transform.position, collider.transform.position);

            if (distance > jointMinDistance && distance < jointDistance)
            {
                CreateJoint(otherParticle.GetComponent<Rigidbody2D>(), distance);
                hasCreatedJoint = true;
            }
        }
    }

    private void CreateJoint(Rigidbody2D other, float distance)
    {
        SpringJoint2D joint = gameObject.AddComponent<SpringJoint2D>();
        joint.connectedBody = other;
        joint.autoConfigureDistance = false;
        joint.distance = distance;
        joint.dampingRatio = jointSoftneess;
        joint.frequency = 1f;

    }

    private void Update()
    {
        if (hasCreatedJoint) Debug.Log("created a joint");
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
            Debug.Log("Destroyed object");
        }

        if (IsOffScreen())
        { 
            Destroy(gameObject);
        }
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
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
}
