using UnityEngine;
using UnityEngine.InputSystem;

public class KartController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 10f;
    public float turnSpeed = 100f;
    public float autoBrakeDistance = 3f; // Distance before destination to start braking
    public float stopDistance = 2f; // Distance to consider "arrived"
    
    [Header("Trigger Detection")]
    public bool useTriggerDetection = true; // Toggle between distance or trigger detection
    public string destinationTag = "Destination"; // Tag to look for on destination triggers

    [Header("Destination Settings")]
    public Transform destination; // Assign a destination transform
    public bool hasDestination = false;
    public bool autoExitOnArrival = true; // Auto exit when reaching destination
    public float autoExitDelay = 2f; // Delay before auto-exit

    [Header("Optional Mobile Controls")]
    public float mobileVertical;
    public float mobileHorizontal;

    private Rigidbody rb;
    private Vector2 input;
    private bool isAutoMoving = true; // Always auto-move forward
    private float currentSpeed = 0f;
    private bool isStopped = false;
    private bool hasArrived = false;
    private bool hasTriggered = false; // Flag to track if we've triggered the destination

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0);
        currentSpeed = speed;
        enabled = false; // Disabled until player enters kart
        hasTriggered = false;
    }

    void Update()
    {
        ReadKeyboardAndGamepadInput();

        float vertical = input.y;
        float horizontal = input.x;

        // Mobile input overrides keyboard/gamepad
        if (mobileVertical != 0) vertical = mobileVertical;
        if (mobileHorizontal != 0) horizontal = mobileHorizontal;

        // Auto-forward movement - always move forward automatically
        if (isAutoMoving && !isStopped && !hasArrived)
        {
            vertical = 1f; // Always forward
        }

        // Check destination if set
        if (hasDestination && destination != null && !hasArrived)
        {
            if (useTriggerDetection)
            {
                // With trigger detection, we just maintain speed until trigger hits
                // Auto-braking is handled by the trigger logic
                currentSpeed = speed;
                
                // Optional: Still use distance-based steering assistance
                float distanceToDestination = Vector3.Distance(transform.position, destination.position);
                if (distanceToDestination <= autoBrakeDistance * 1.5f)
                {
                    AutoSteerToDestination();
                }
            }
            else
            {
                // Original distance-based logic
                HandleDestination();
            }
        }

        Move(vertical, horizontal);
    }

    void HandleDestination()
    {
        if (isStopped || hasArrived) return;

        float distanceToDestination = Vector3.Distance(transform.position, destination.position);
        
        // Check if we've reached the destination
        if (distanceToDestination <= stopDistance)
        {
            ArrivedAtDestination();
            return;
        }
        
        // Start braking when close to destination
        if (distanceToDestination <= autoBrakeDistance)
        {
            // Gradually slow down as we approach
            float brakeFactor = Mathf.Clamp01(distanceToDestination / autoBrakeDistance);
            currentSpeed = speed * brakeFactor;
            
            // Optional: Auto-steer toward destination when close
            if (distanceToDestination <= autoBrakeDistance * 1.5f)
            {
                AutoSteerToDestination();
            }
        }
        else
        {
            currentSpeed = speed; // Full speed when far from destination
        }
    }

    void AutoSteerToDestination()
    {
        if (destination == null) return;
        
        // Calculate direction to destination
        Vector3 directionToDest = (destination.position - transform.position).normalized;
        directionToDest.y = 0; // Keep it horizontal
        
        // Calculate angle between forward and destination direction
        float angle = Vector3.SignedAngle(transform.forward, directionToDest, Vector3.up);
        
        // Apply auto-steering (can be overridden by player input)
        float autoSteer = Mathf.Clamp(angle / 45f, -1f, 1f) * 0.5f; // 50% auto-steer
        
        // Blend with player input
        float playerInput = input.x;
        if (mobileHorizontal != 0) playerInput = mobileHorizontal;
        
        // Give priority to player input, but add auto-steer as assistance
        if (Mathf.Abs(playerInput) < 0.1f)
        {
            input.x = autoSteer;
        }
        else
        {
            // Player is steering, but we can still add a little guidance
            input.x = Mathf.Clamp(playerInput + autoSteer * 0.3f, -1f, 1f);
        }
    }

    // TRIGGER DETECTION - NEW CODE
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerDetection || hasArrived || !hasDestination) return;
        
        // Check if this is our destination trigger
        if (destination != null && other.transform == destination)
        {
            Debug.Log($"🎯 Kart entered destination trigger: {destination.name}");
            ArrivedAtDestination();
            hasTriggered = true;
            return;
        }
        
        // Alternative: Check by tag
        if (!string.IsNullOrEmpty(destinationTag) && other.CompareTag(destinationTag))
        {
            // Verify this is the intended destination
            if (destination != null && other.transform == destination)
            {
                Debug.Log($"🎯 Kart entered destination trigger by tag: {destination.name}");
                ArrivedAtDestination();
                hasTriggered = true;
            }
            else
            {
                Debug.Log($"ℹ️ Kart entered a {destinationTag} trigger, but it's not our current destination");
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!useTriggerDetection || hasArrived || !hasDestination || hasTriggered) return;
        
        // Fallback: If OnTriggerEnter didn't fire for some reason
        if (destination != null && other.transform == destination)
        {
            Debug.Log($"🎯 Kart staying in destination trigger: {destination.name}");
            ArrivedAtDestination();
            hasTriggered = true;
        }
    }

    void ArrivedAtDestination()
    {
        if (hasArrived) return; // Prevent multiple calls
        
        hasArrived = true;
        isStopped = true;
        currentSpeed = 0f;
        
        // Stop all movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        Debug.Log("🚗 Arrived at destination!");
        
        // Disable controls (kart will no longer respond to input)
        SetControllable(false);
        
        // Auto-exit if enabled
        if (autoExitOnArrival)
        {
            StartCoroutine(AutoExitAfterDelay(autoExitDelay));
        }
    }

    System.Collections.IEnumerator AutoExitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Find KartTrigger and exit - using FindAnyObjectByType
        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.AutoExitKart();
            Debug.Log("👤 Player automatically exited kart");
        }
        else
        {
            Debug.LogWarning("⚠️ No KartTrigger found for auto-exit");
        }
    }

    void ReadKeyboardAndGamepadInput()
    {
        Vector2 newInput = Vector2.zero;

        // --- Keyboard movement (Only A/D for steering, no W/S) ---
        if (Keyboard.current != null)
        {
            // Only steering controls - forward is automatic
            if (Keyboard.current.aKey.isPressed) newInput.x = -1;
            if (Keyboard.current.dKey.isPressed) newInput.x = 1;
            
            // Optional: Space for manual brake/stop
            if (Keyboard.current.spaceKey.isPressed)
            {
                currentSpeed = 0f;
            }
            
            // Manual exit with E key (still works)
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        // --- Gamepad stick movement ---
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) newInput.x = stick.x;
            
            // Optional: Button for manual brake
            if (Gamepad.current.buttonSouth.isPressed)
            {
                currentSpeed = 0f;
            }
            
            // Manual exit with Y button (Xbox) or Triangle (PlayStation)
            if (Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        input = newInput;
    }
    
    void ManualExit()
    {
        if (hasArrived) return; // Don't allow manual exit if already arrived
        
        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.ExitKart();
        }
    }

    void Move(float forward, float turn)
    {
        // Apply current speed (auto-adjusted for destination)
        Vector3 moveDir = transform.forward * forward * currentSpeed;
        rb.MovePosition(rb.position + moveDir * Time.deltaTime);

        float turnAmount = turn * turnSpeed * Time.deltaTime;
        Quaternion turnRotation = Quaternion.Euler(0, turnAmount, 0);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    public void SetControllable(bool controllable)
    {
        enabled = controllable;

        if (!controllable)
        {
            input = Vector2.zero;
            mobileVertical = 0f;
            mobileHorizontal = 0f;
            currentSpeed = 0f;
            isStopped = true;
            hasTriggered = false;
        }
        else
        {
            currentSpeed = speed;
            isStopped = false;
            hasArrived = false;
            hasTriggered = false;
            isAutoMoving = true;
        }
    }
    
    public void SetDestination(Transform newDestination)
    {
        destination = newDestination;
        hasDestination = true;
        hasArrived = false;
        hasTriggered = false;
        isStopped = false;
        currentSpeed = speed;
        
        if (enabled)
        {
            Debug.Log($"🎯 Destination set to: {newDestination.name}");
            
            // Ensure destination has a collider if using trigger detection
            if (useTriggerDetection && newDestination != null)
            {
                Collider collider = newDestination.GetComponent<Collider>();
                if (collider == null)
                {
                    Debug.LogWarning($"⚠️ Destination '{newDestination.name}' has no Collider component! Add a trigger collider.");
                }
                else if (!collider.isTrigger)
                {
                    Debug.LogWarning($"⚠️ Destination '{newDestination.name}' collider is not a trigger! Set 'Is Trigger' to true.");
                }
            }
        }
    }
    
    public void ClearDestination()
    {
        hasDestination = false;
        hasArrived = false;
        hasTriggered = false;
        isStopped = false;
        currentSpeed = speed;
    }

    // Mobile UI buttons - updated for auto-forward
    public void Mobile_TurnLeft(bool isPressed) {
        mobileHorizontal = isPressed ? -1f : 0f;
    }
    
    public void Mobile_TurnRight(bool isPressed) {
        mobileHorizontal = isPressed ? 1f : 0f;
    }
    
    public void Mobile_Brake(bool isPressed) {
        // Manual brake override
        if (isPressed)
        {
            currentSpeed = 0f;
        }
        else if (!isStopped && !hasArrived)
        {
            currentSpeed = speed;
        }
    }
    
    public void Mobile_ManualExit()
    {
        ManualExit();
    }

    // Public properties for other scripts
    public bool HasArrived => hasArrived;
    public Transform CurrentDestination => destination;
    public bool UseTriggerDetection => useTriggerDetection;

    // Draw gizmos for visualization
    void OnDrawGizmosSelected()
    {
        if (hasDestination && destination != null)
        {
            // Draw line to destination
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination.position);
            
            if (!useTriggerDetection)
            {
                // Only show distance gizmos when using distance-based detection
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(destination.position, stopDistance);
                
                Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
                Gizmos.DrawWireSphere(destination.position, autoBrakeDistance);
            }
        }
    }
}