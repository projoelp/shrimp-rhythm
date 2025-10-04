using System;
using UnityEngine;

public class OrderManager : MonoBehaviour
{
    [Header("Order Settings")]
    [SerializeField] private float orderDuration = 30f;
    [SerializeField] private bool autoStartFirstOrder = true;
    [SerializeField] private int particlesPerOrder = 50;

    [Header("Burn Settings")]
    [SerializeField] private float timeToBurnRice = 5f;
    [SerializeField] private float panUprightThreshold = 10f;
    [SerializeField] private float panUprightDelay = 1f; // NEW: Delay after pan returns upright

    [Header("Dropped Rice Settings")]
    [SerializeField] private float noRiceFailTime = 3f; // NEW: How long with no rice before order fails

    [Header("Collection Settings")]
    [SerializeField] private Transform collectionPoint;
    [SerializeField] private float collectionDuration = 2f;

    [Header("Pan Detection")]
    [SerializeField] private Collider2D panCollider;
    [SerializeField] private float panCheckRadius = 1f;
    [SerializeField] private LayerMask particleLayer;

    [Header("References")]
    [SerializeField] private PanController panController;
    [SerializeField] private ParticleCollector particleCollector;
    [SerializeField] private LiquidSpawner liquidSpawner;

    // Order state
    private float orderTimer = 0f;
    private bool orderActive = false;
    private bool riceSpawned = false;
    private bool isCollecting = false;
    private int ordersCompleted = 0;

    // Burn state
    private bool riceBurned = false;
    private bool riceDropped = false; // NEW
    private bool waitingForCleanup = false;

    // Cleanup state
    private float panUprightTimer = 0f; // NEW: Tracks how long pan has been upright
    private bool panWasUpright = false; // NEW: Tracks if pan is currently upright

    // No rice timer
    private float noRiceTimer = 0f; // NEW: Tracks how long there's been no rice

    // Events
    public event Action<int> OnOrderStarted;
    public event Action<int> OnOrderCompleted;
    public event Action<float> OnOrderProgressUpdated;
    public event Action OnCollectionStarted;
    public event Action OnCollectionCompleted;
    public event Action OnRiceSpawned;
    public event Action OnRiceLanded;
    public event Action OnRiceBurned;
    public event Action OnRiceDropped; // NEW
    public event Action OnWaitingForCleanup;
    public event Action OnCleanupComplete;

    // Public properties
    public bool IsOrderActive => orderActive;
    public float OrderProgress => orderActive ? Mathf.Clamp01(orderTimer / orderDuration) : 0f;
    public int OrdersCompleted => ordersCompleted;
    public bool IsCollecting => isCollecting;
    public bool HasRiceInPan => HasParticlesInPan();
    public bool IsRiceBurned => riceBurned;
    public bool IsRiceDropped => riceDropped;
    public bool IsWaitingForCleanup => waitingForCleanup;

    private void Start()
    {
        if (autoStartFirstOrder)
        {
            StartNewOrder();
        }
    }

    private void Update()
    {
        // Handle cleanup waiting state
        if (waitingForCleanup)
        {
            HandleCleanupWaiting();
            return;
        }

        if (!orderActive || isCollecting) return;

        // Wait for rice to spawn
        if (!riceSpawned && liquidSpawner != null)
        {
            if (!liquidSpawner.IsSpawningFixedAmount && liquidSpawner.SpawnedCount > 0)
            {
                riceSpawned = true;
                OnRiceSpawned?.Invoke();
                Debug.Log("Rice spawning completed!");
            }
            return;
        }

        // Check if any particles are in the pan
        bool hasParticlesInPan = HasParticlesInPan();

        // Handle no rice in pan
        if (!hasParticlesInPan)
        {
            noRiceTimer += Time.deltaTime;

            // If no rice for too long and order was active, fail the order
            if (noRiceTimer >= noRiceFailTime && !riceBurned && !riceDropped)
            {
                SetRiceDropped();
            }

            return;
        }
        else
        {
            // Reset no rice timer when rice is in pan
            noRiceTimer = 0f;
        }

        // Check if pan is burning
        bool isPanBurning = IsPanBurning();

        if (isPanBurning && !riceBurned && !riceDropped)
        {
            // Apply burn time to all particles in pan
            ApplyBurnTimeToParticles(Time.deltaTime);

            // Check if any particle is fully burned
            if (AnyParticlesBurned())
            {
                SetRiceBurned();
            }
        }
        else if (!isPanBurning && !riceBurned && !riceDropped)
        {
            // Progress the order timer normally
            orderTimer += Time.deltaTime;
            OnOrderProgressUpdated?.Invoke(OrderProgress);

            if (orderTimer >= orderDuration)
            {
                CompleteOrder();
            }
        }

        // Allow manual completion only if rice isn't burned or dropped
        if (Input.GetKeyDown(KeyCode.Return) && hasParticlesInPan && !riceBurned && !riceDropped)
        {
            CompleteOrder();
        }
    }

    private void HandleCleanupWaiting()
    {
        // Check if pan is upright
        bool isPanUpright = IsPanUpright();

        // Track upright time with delay
        if (isPanUpright)
        {
            if (!panWasUpright)
            {
                // Pan just became upright
                panWasUpright = true;
                panUprightTimer = 0f;
                Debug.Log("Pan returned upright - starting delay timer...");
            }
            else
            {
                // Pan is still upright, increment timer
                panUprightTimer += Time.deltaTime;
            }
        }
        else
        {
            // Pan tilted again, reset
            if (panWasUpright)
            {
                Debug.Log("Pan tilted - delay timer reset");
            }
            panWasUpright = false;
            panUprightTimer = 0f;
        }

        // Check if all particles are gone
        bool allParticlesGone = !HasParticlesInPan();

        // Cleanup complete if: pan upright for delay duration AND all particles gone
        if (panWasUpright && panUprightTimer >= panUprightDelay && allParticlesGone)
        {
            // Cleanup complete!
            waitingForCleanup = false;
            panWasUpright = false;
            panUprightTimer = 0f;

            Debug.Log("Cleanup complete! Starting new order...");
            OnCleanupComplete?.Invoke();
            StartNewOrder();
        }
    }

    private bool IsPanUpright()
    {
        if (panController == null) return true;

        // Get the current Z rotation from the panPivot (not the controller's transform)
        Transform pivotTransform = panController.PanTransform;
        float zRotation = pivotTransform.localEulerAngles.z;

        // Normalize to -180 to 180
        if (zRotation > 180f) zRotation -= 360f;

        return Mathf.Abs(zRotation) <= panUprightThreshold;
    }

    private void SetRiceBurned()
    {
        if (riceBurned) return;

        riceBurned = true;
        orderActive = false;
        waitingForCleanup = true;

        Debug.Log("RICE IS BURNED! Dump it off screen and return pan upright to continue.");
        OnRiceBurned?.Invoke();
        OnWaitingForCleanup?.Invoke();
    }

    private void SetRiceDropped()
    {
        if (riceDropped) return;

        riceDropped = true;
        orderActive = false;
        waitingForCleanup = true;

        Debug.Log("ALL RICE DROPPED! Return pan upright to start new order.");
        OnRiceDropped?.Invoke();
        OnWaitingForCleanup?.Invoke();
    }

    private void ApplyBurnTimeToParticles(float deltaTime)
    {
        var particles = FindObjectsByType<LiquidParticle>(FindObjectsSortMode.None);

        foreach (var particle in particles)
        {
            if (particle != null && IsParticleInPan(particle))
            {
                particle.AddBurnTime(deltaTime);
            }
        }
    }

    private bool AnyParticlesBurned()
    {
        var particles = FindObjectsByType<LiquidParticle>(FindObjectsSortMode.None);

        foreach (var particle in particles)
        {
            if (particle != null && particle.IsBurned)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsParticleInPan(LiquidParticle particle)
    {
        if (panCollider == null || particle == null) return false;

        float distance = Vector2.Distance(particle.transform.position, panCollider.transform.position);
        return distance <= panCheckRadius;
    }

    private bool HasParticlesInPan()
    {
        if (panCollider == null) return false;

        Collider2D[] particles = Physics2D.OverlapCircleAll(
            panCollider.transform.position,
            panCheckRadius,
            particleLayer
        );

        return particles.Length > 0;
    }

    private bool IsPanBurning()
    {
        if (panController == null) return false;
        return panController.IsBurning;
    }

    public void StartNewOrder()
    {
        orderActive = true;
        orderTimer = 0f;
        riceSpawned = false;
        riceBurned = false;
        riceDropped = false;
        waitingForCleanup = false;
        noRiceTimer = 0f;
        panUprightTimer = 0f;
        panWasUpright = false;
        ordersCompleted++;

        Debug.Log($"Order #{ordersCompleted} started! Spawning rice...");
        OnOrderStarted?.Invoke(ordersCompleted);

        if (liquidSpawner != null)
        {
            liquidSpawner.StartFixedSpawning(particlesPerOrder);
        }
        else
        {
            Debug.LogWarning("LiquidSpawner not assigned!");
        }
    }

    private void CompleteOrder()
    {
        if (!orderActive || isCollecting || riceBurned || riceDropped) return;

        orderActive = false;
        isCollecting = true;

        Debug.Log($"Order #{ordersCompleted} completed! Starting collection...");
        OnOrderCompleted?.Invoke(ordersCompleted);
        OnCollectionStarted?.Invoke();

        if (particleCollector != null && collectionPoint != null)
        {
            particleCollector.StartCollection(collectionPoint.position, collectionDuration, OnCollectionFinished);
        }
        else
        {
            Debug.LogWarning("ParticleCollector or CollectionPoint not assigned!");
            OnCollectionFinished();
        }
    }

    private void OnCollectionFinished()
    {
        isCollecting = false;

        Debug.Log("Collection finished! Starting next order...");
        OnCollectionCompleted?.Invoke();

        StartNewOrder();
    }

    public void TriggerCollection()
    {
        if (orderActive && !isCollecting && HasParticlesInPan() && !riceBurned && !riceDropped)
        {
            CompleteOrder();
        }
    }

    private void OnDrawGizmos()
    {
        if (panCollider != null)
        {
            if (riceBurned)
            {
                Gizmos.color = Color.red;
            }
            else if (riceDropped)
            {
                Gizmos.color = Color.yellow;
            }
            else
            {
                Gizmos.color = HasParticlesInPan() ? Color.green : Color.gray;
            }
            Gizmos.DrawWireSphere(panCollider.transform.position, panCheckRadius);
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 400, 30), $"Order #{ordersCompleted}", style);

        if (waitingForCleanup)
        {
            if (riceBurned)
            {
                style.normal.textColor = Color.red;
                GUI.Label(new Rect(10, 40, 400, 30), "RICE BURNED!", style);
            }
            else if (riceDropped)
            {
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(10, 40, 400, 30), "ALL RICE DROPPED!", style);
            }

            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 70, 400, 30), "Return pan upright to continue", style);
            GUI.Label(new Rect(10, 100, 400, 30), $"Pan Upright: {IsPanUpright()}", style);

            if (panWasUpright && !HasParticlesInPan())
            {
                float delayProgress = Mathf.Clamp01(panUprightTimer / panUprightDelay);
                GUI.Label(new Rect(10, 130, 400, 30), $"Upright Timer: {delayProgress:P0}", style);
            }

            GUI.Label(new Rect(10, 160, 400, 30), $"Particles Gone: {!HasParticlesInPan()}", style);
        }
        else if (!riceSpawned && liquidSpawner != null)
        {
            style.normal.textColor = Color.white;
            float spawnProgress = liquidSpawner.SpawnProgress;
            GUI.Label(new Rect(10, 40, 400, 30), $"Spawning Rice: {spawnProgress:P0}", style);
        }
        else
        {
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 40, 400, 30), $"Cooking Progress: {OrderProgress:P0}", style);
            GUI.Label(new Rect(10, 70, 400, 30), $"Rice in Pan: {HasParticlesInPan()}", style);
            GUI.Label(new Rect(10, 100, 400, 30), $"Burning: {IsPanBurning()}", style);

            // Show no rice timer if rice is missing
            if (!HasParticlesInPan())
            {
                style.normal.textColor = Color.yellow;
                float timeLeft = noRiceFailTime - noRiceTimer;
                GUI.Label(new Rect(10, 130, 400, 30), $"No Rice! Fail in: {timeLeft:F1}s", style);
            }
            else if (HasParticlesInPan() && !riceBurned && !riceDropped)
            {
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(10, 130, 400, 30), "Press Enter to complete order", style);
            }
        }
    }
}