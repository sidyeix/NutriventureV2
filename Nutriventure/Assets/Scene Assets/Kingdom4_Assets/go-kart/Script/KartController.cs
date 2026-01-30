using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class KartController : MonoBehaviour
{
    [Header("UI Feedback")]
    public GameObject invertedControlsUI; // Drag a UI panel here
    public TMP_Text countdownText; // Changed to TMP_Text for TextMeshPro
    public float uiWarningDuration = 3f; // How long to show the warning
    
    public float speed = 10f;
    public float turnSpeed = 100f;
    public float autoBrakeDistance = 3f;
    public float stopDistance = 2f;

    public bool useTriggerDetection = true;
    public string destinationTag = "Destination";
    
    // Spilled Milk Settings
    public string spilledMilkTag = "SpilledMilk";
    public float milkEffectDuration = 5f;
    public bool invertSteering = true;
    public bool invertAcceleration = false;

    public Transform destination;
    public bool hasDestination = false;
    public bool autoExitOnArrival = true;
    public float autoExitDelay = 2f;

    public float mobileVertical;
    public float mobileHorizontal;

    private Rigidbody rb;
    private Vector2 input;
    private bool isAutoMoving = true;
    private float currentSpeed = 0f;
    private bool isStopped = false;
    private bool hasArrived = false;
    private bool hasTriggered = false;
    
    // Control Inversion Variables
    private bool controlsInverted = false;
    private float milkEffectTimer = 0f;
    private AudioSource audioSource;
    public ParticleSystem milkSplashEffect;
    public AudioClip milkHitSound;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.3f, 0);
        currentSpeed = speed;
        enabled = false;
        hasTriggered = false;
        
        // Setup audio source for milk effect
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && milkHitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Initialize UI to be hidden
        if (invertedControlsUI != null)
        {
            invertedControlsUI.SetActive(false);
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void TriggerSpilledMilkEffect()
    {
        // Activate control inversion
        controlsInverted = true;
        milkEffectTimer = milkEffectDuration;
        
        // Play effects
        PlayMilkEffects();
        
        // Show UI warning
        ShowInvertedControlsWarning();
        
        Debug.Log($"🚨 Controls inverted! Left and right swapped for {milkEffectDuration} seconds.");
    }

    void ShowInvertedControlsWarning()
    {
        if (invertedControlsUI != null)
        {
            invertedControlsUI.SetActive(true);
            
            // Set initial warning text
            if (countdownText != null)
            {
                countdownText.text = $"<color=red>CONTROLS INVERTED!</color>\n<color=red>Time: {Mathf.Ceil(milkEffectTimer)}s</color>";
                countdownText.gameObject.SetActive(true);
            }
            
            // Hide warning panel after a few seconds (but keep countdown visible)
            StartCoroutine(HideWarningAfterDelay(uiWarningDuration));
        }
        else if (countdownText != null)
        {
            // If no panel, just show the text
            countdownText.text = $"<color=red>CONTROLS INVERTED!</color>\n<color=red>Time: {Mathf.Ceil(milkEffectTimer)}s</color>";
            countdownText.gameObject.SetActive(true);
        }
    }

    IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only hide the panel, keep the countdown text visible
        if (invertedControlsUI != null && invertedControlsUI != countdownText.gameObject)
        {
            invertedControlsUI.SetActive(false);
        }
    }

    void Update()
    {
        // Handle milk effect timer and UI
        if (controlsInverted)
        {
            milkEffectTimer -= Time.deltaTime;
            
            // Update countdown text with TMP rich text - RED NUMBERS
            if (countdownText != null)
            {
                // Make sure text is visible
                if (!countdownText.gameObject.activeSelf)
                {
                    countdownText.gameObject.SetActive(true);
                }
                
                // Update with countdown using TextMeshPro rich text - RED NUMBERS
                countdownText.text = $"<color=red>CONTROLS INVERTED!</color>\n" +
                                     $"<size=24>Time: <color=red>{Mathf.Ceil(milkEffectTimer)}</color>s</size>";
            }
            
            if (milkEffectTimer <= 0)
            {
                ResetControls();
            }
        }
        else
        {
            // Hide countdown when not inverted
            if (countdownText != null && countdownText.gameObject.activeSelf)
            {
                // Check if we're showing the "back to normal" message
                if (!countdownText.text.Contains("✓"))
                {
                    countdownText.gameObject.SetActive(false);
                }
            }
        }
        
        ReadKeyboardAndGamepadInput();

        float vertical = input.y;
        float horizontal = input.x;

        if (mobileVertical != 0) vertical = mobileVertical;
        if (mobileHorizontal != 0) horizontal = mobileHorizontal;
        
        // Apply control inversion if active
        if (controlsInverted)
        {
            if (invertSteering)
            {
                horizontal = -horizontal;
                
                // Also invert mobile inputs
                if (mobileHorizontal != 0)
                {
                    horizontal = -mobileHorizontal;
                }
            }
            
            if (invertAcceleration)
            {
                vertical = -vertical;
                
                // Also invert mobile vertical input
                if (mobileVertical != 0)
                {
                    vertical = -mobileVertical;
                }
            }
        }

        if (isAutoMoving && !isStopped && !hasArrived)
        {
            vertical = 1f;
        }

        if (hasDestination && destination != null && !hasArrived)
        {
            if (useTriggerDetection)
            {
                currentSpeed = speed;

                float distanceToDestination = Vector3.Distance(transform.position, destination.position);
                if (distanceToDestination <= autoBrakeDistance * 1.5f)
                {
                    AutoSteerToDestination();
                }
            }
            else
            {
                HandleDestination();
            }
        }

        Move(vertical, horizontal);
    }

    void ResetControls()
    {
        controlsInverted = false;
        milkEffectTimer = 0f;
        
        // Hide the warning panel
        if (invertedControlsUI != null)
        {
            invertedControlsUI.SetActive(false);
        }
        
        // Show "back to normal" message
        if (countdownText != null)
        {
            countdownText.text = "<color=green>✓ CONTROLS NORMAL</color>";
            countdownText.gameObject.SetActive(true);
            
            // Hide after 2 seconds
            StartCoroutine(HideNormalMessageAfterDelay(2f));
        }
        
        Debug.Log("✅ Controls returned to normal!");
    }

    IEnumerator HideNormalMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void HandleDestination()
    {
        if (isStopped || hasArrived) return;

        float distanceToDestination = Vector3.Distance(transform.position, destination.position);

        if (distanceToDestination <= stopDistance)
        {
            ArrivedAtDestination();
            return;
        }

        if (distanceToDestination <= autoBrakeDistance)
        {
            float brakeFactor = Mathf.Clamp01(distanceToDestination / autoBrakeDistance);
            currentSpeed = speed * brakeFactor;

            if (distanceToDestination <= autoBrakeDistance * 1.5f)
            {
                AutoSteerToDestination();
            }
        }
        else
        {
            currentSpeed = speed;
        }
    }

    void AutoSteerToDestination()
    {
        if (destination == null) return;

        Vector3 directionToDest = (destination.position - transform.position).normalized;
        directionToDest.y = 0;

        float angle = Vector3.SignedAngle(transform.forward, directionToDest, Vector3.up);
        float autoSteer = Mathf.Clamp(angle / 45f, -1f, 1f) * 0.5f;

        float playerInput = input.x;
        if (mobileHorizontal != 0) playerInput = mobileHorizontal;
        
        // Apply inversion to auto-steer if needed
        if (controlsInverted && invertSteering)
        {
            autoSteer = -autoSteer;
        }

        if (Mathf.Abs(playerInput) < 0.1f)
        {
            input.x = autoSteer;
        }
        else
        {
            input.x = Mathf.Clamp(playerInput + autoSteer * 0.3f, -1f, 1f);
        }
    }

    // Add this when wagon phase is complete
    public void CompleteWagonPhase()
    {
        if (AllerthriaGameManager.Instance != null && 
            AllerthriaGameManager.Instance.currentPhase == 
            AllerthriaGameManager.GamePhase.WagonPhase)
        {
            // Check if player completed wagon challenge successfully
            AllerthriaGameManager.Instance.CompleteWagonPhase();
        }
    }

    void PlayMilkEffects()
    {
        // Play sound
        if (milkHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(milkHitSound);
        }
        
        // Play particle effect
        if (milkSplashEffect != null)
        {
            Vector3 effectPosition = transform.position + Vector3.up * 0.5f;
            ParticleSystem particles = Instantiate(milkSplashEffect, effectPosition, Quaternion.identity);
            particles.Play();
            
            // Destroy particles after they finish
            Destroy(particles.gameObject, particles.main.duration);
        }
    }

    IEnumerator DeactivateMilkObject(GameObject milkObject)
    {
        // Optional fade out effect
        Renderer milkRenderer = milkObject.GetComponent<Renderer>();
        if (milkRenderer != null)
        {
            Material mat = milkRenderer.material;
            Color originalColor = mat.color;
            
            float fadeDuration = 1f;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeDuration)
            {
                if (milkObject == null) yield break;
                
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                
                yield return null;
            }
        }
        
        // Deactivate after fade
        if (milkObject != null)
        {
            milkObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Allergen"))
        {
            // Update score
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.WagonHitAllergen();
            }
        }
        else if (other.CompareTag("Destination"))
        {
            // Player reached destination successfully
            CompleteWagonPhase();
        }
        else if (other.CompareTag(spilledMilkTag))
        {
            TriggerSpilledMilkEffect();
            
            // Disable the milk collider to prevent multiple triggers
            Collider milkCollider = other.GetComponent<Collider>();
            if (milkCollider != null)
            {
                milkCollider.enabled = false;
            }
            
            // Optional: Hide or deactivate the milk object
            StartCoroutine(DeactivateMilkObject(other.gameObject));
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!useTriggerDetection || hasArrived || !hasDestination || hasTriggered) return;

        if (destination != null && other.transform == destination)
        {
            ArrivedAtDestination();
            hasTriggered = true;
        }
    }

    void ArrivedAtDestination()
    {
        if (hasArrived) return;

        hasArrived = true;
        isStopped = true;
        currentSpeed = 0f;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SetControllable(false);

        if (autoExitOnArrival)
        {
            StartCoroutine(AutoExitAfterDelay(autoExitDelay));
        }
    }

    IEnumerator AutoExitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.AutoExitKart();
        }
    }

    void ReadKeyboardAndGamepadInput()
    {
        Vector2 newInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) newInput.x = -1;
            if (Keyboard.current.dKey.isPressed) newInput.x = 1;

            if (Keyboard.current.spaceKey.isPressed)
            {
                currentSpeed = 0f;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) newInput.x = stick.x;

            if (Gamepad.current.buttonSouth.isPressed)
            {
                currentSpeed = 0f;
            }

            if (Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                ManualExit();
            }
        }

        input = newInput;
    }

    void ManualExit()
    {
        if (hasArrived) return;

        KartTrigger kartTrigger = FindAnyObjectByType<KartTrigger>();
        if (kartTrigger != null)
        {
            kartTrigger.ExitKart();
        }
    }

    void Move(float forward, float turn)
    {
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
    }

    public void ClearDestination()
    {
        hasDestination = false;
        hasArrived = false;
        hasTriggered = false;
        isStopped = false;
        currentSpeed = speed;
    }

    public void Mobile_TurnLeft(bool isPressed)
    {
        mobileHorizontal = isPressed ? -1f : 0f;
    }

    public void Mobile_TurnRight(bool isPressed)
    {
        mobileHorizontal = isPressed ? 1f : 0f;
    }

    public void Mobile_Brake(bool isPressed)
    {
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
    
    // Public methods for the spilled milk effect
    public bool IsControlsInverted()
    {
        return controlsInverted;
    }
    
    public float GetRemainingMilkEffectTime()
    {
        return Mathf.Max(0f, milkEffectTimer);
    }
    
    public void TriggerMilkEffectManually(float duration = -1f)
    {
        if (duration > 0)
        {
            milkEffectDuration = duration;
        }
        TriggerSpilledMilkEffect();
    }
    
    public void CancelMilkEffect()
    {
        milkEffectTimer = 0f;
        ResetControls();
    }

    public bool HasArrived => hasArrived;
    public Transform CurrentDestination => destination;
    public bool UseTriggerDetection => useTriggerDetection;

    void OnDrawGizmosSelected()
    {
        if (hasDestination && destination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, destination.position);

            if (!useTriggerDetection)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(destination.position, stopDistance);

                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(destination.position, autoBrakeDistance);
            }
        }
        
        // Draw milk effect status
        if (controlsInverted)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            // Draw an X to indicate inverted controls
            Gizmos.color = Color.red;
            Vector3 pos = transform.position + Vector3.up * 3f;
            Gizmos.DrawLine(pos + Vector3.left, pos + Vector3.right);
            Gizmos.DrawLine(pos + Vector3.forward, pos + Vector3.back);
        }
    }
}