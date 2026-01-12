using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class StartingSequenceManager : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private Transform startingPoint;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private StarterAssetsInputs playerInput;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator playerAnimator;

    [Header("Movement Settings")]
    [SerializeField] private float autoMoveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float arrivalDistance = 0.3f;
    [SerializeField] private float arrivalAngleThreshold = 5f;
    [SerializeField] private float slowDownDistance = 2f;

    [Header("Camera References")]
    [SerializeField] private CinemachineVirtualCamera normalFollowCamera;
    [SerializeField] private CinemachineVirtualCamera sequenceFollowCamera;
    [SerializeField] private int sequenceCameraPriority = 30;

    [Header("UI Management")]
    [SerializeField] private List<GameObject> uiElementsToDisable = new List<GameObject>();
    [SerializeField] private List<GameObject> uiElementsToEnable = new List<GameObject>();

    // State tracking
    private bool isInStartingSequence = false;
    private bool hasReachedStartPoint = false;
    private Coroutine currentSequenceCoroutine;
    private Vector3 originalPlayerPosition;

    // Animation IDs (cache for performance)
    private int animIDSpeed;
    private int animIDMotionSpeed;
    private int originalNormalCameraPriority;

    void Start()
    {
        // Cache animation parameter IDs (must match your controller)
        animIDSpeed = Animator.StringToHash("Speed");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        // Validate references
        ValidateReferences();

        // Store original camera priority
        if (normalFollowCamera != null)
        {
            originalNormalCameraPriority = normalFollowCamera.Priority;
        }
    }

    private void ValidateReferences()
    {
        if (playerController == null)
            playerController = FindObjectOfType<ThirdPersonController>();

        if (playerInput == null && playerController != null)
            playerInput = playerController.GetComponent<StarterAssetsInputs>();

        if (characterController == null && playerController != null)
            characterController = playerController.GetComponent<CharacterController>();

        if (playerAnimator == null && playerController != null)
            playerController.TryGetComponent(out playerAnimator);

        // Log warnings for missing cameras
        if (sequenceFollowCamera == null)
            Debug.LogWarning("Sequence Follow Camera not assigned - camera switching won't work!");

        if (normalFollowCamera == null)
            Debug.LogWarning("Normal Follow Camera not assigned - camera switching won't work!");
    }

    // This will be called by the trigger
    public void StartSequence()
    {
        if (isInStartingSequence || hasReachedStartPoint) return;

        Debug.Log("Starting sequence triggered");

        // Store original position for potential reset
        originalPlayerPosition = playerController.transform.position;

        // Stop any existing sequence
        if (currentSequenceCoroutine != null)
            StopCoroutine(currentSequenceCoroutine);

        // Start the sequence
        currentSequenceCoroutine = StartCoroutine(StartingSequenceRoutine());
    }

    private IEnumerator StartingSequenceRoutine()
    {
        isInStartingSequence = true;

        // Step 1: Immediately switch camera
        SwitchToSequenceCamera();

        // Step 2: Disable player input (but NOT the controller or animator)
        DisablePlayerInput();

        // Step 3: Disable UI elements (they stay disabled until game ends)
        DisableUIElements();

        // Step 4: Make player "run" to starting point with smooth stop
        yield return StartCoroutine(MoveToStartingPointSmooth());

        // Step 5: Ensure complete stop at exact position
        SnapToStartingPoint();

        // Step 6: Rotate to correct orientation
        yield return StartCoroutine(RotateToStartingOrientation());

        // Animation automatically goes to idle when speed = 0 (handled by blend tree)
        // We don't touch the animator at all after setting speed to 0

        // Step 7: Enable UI elements for object interaction (but NOT movement UI)
        EnableUIElements();

        // Step 8: Enable object interaction (tapping only)
        EnableObjectInteraction();

        hasReachedStartPoint = true;
        isInStartingSequence = false;
        currentSequenceCoroutine = null;

        Debug.Log("Starting sequence completed - Player is now idle at starting point");
    }

    // Method to enable all controls when game ends (called by end trigger)
    public void EnableAllControlsAndUI()
    {
        Debug.Log("Enabling all controls and UI (game end)");

        // Switch back to normal camera
        SwitchToNormalCamera();

        // Re-enable player input
        EnablePlayerInput();

        // Re-enable all UI elements that were disabled
        ReEnableAllUI();

        // Reset sequence state if needed
        hasReachedStartPoint = false;
    }

    // Reset player to original position (for wrong object choice later)
    public void ResetToOriginalPosition()
    {
        if (playerController != null)
        {
            playerController.transform.position = originalPlayerPosition;

            // Make sure animator knows we're at zero speed
            if (playerAnimator != null)
            {
                playerAnimator.SetFloat(animIDSpeed, 0f);
                playerAnimator.SetFloat(animIDMotionSpeed, 0f);
            }

            Debug.Log("Player reset to original position (idle)");
        }
    }

    private IEnumerator MoveToStartingPointSmooth()
    {
        Debug.Log("Moving to starting point with smooth stop...");

        while (Vector3.Distance(playerController.transform.position, startingPoint.position) > arrivalDistance)
        {
            // Calculate distance remaining
            float distanceRemaining = Vector3.Distance(playerController.transform.position, startingPoint.position);

            // Calculate direction to starting point
            Vector3 direction = (startingPoint.position - playerController.transform.position).normalized;

            // Calculate speed based on distance (slower as we get closer)
            float currentSpeed = CalculateSpeedBasedOnDistance(distanceRemaining);

            // Calculate movement
            Vector3 moveVector = direction * currentSpeed * Time.deltaTime;

            // Move the character using CharacterController
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveVector);
            }
            else
            {
                // Fallback to transform movement
                playerController.transform.position += moveVector;
            }

            // Smoothly rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                playerController.transform.rotation = Quaternion.Slerp(
                    playerController.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // Update animator speed (this triggers running animation)
            // The animator will automatically transition to idle when speed = 0
            UpdateAnimatorSpeed(currentSpeed);

            yield return null;
        }

        Debug.Log("Reached starting point proximity");
    }

    private float CalculateSpeedBasedOnDistance(float distance)
    {
        if (distance <= arrivalDistance)
        {
            return 0f; // Stop completely
        }
        else if (distance <= slowDownDistance)
        {
            // Gradually slow down when close
            float t = (distance - arrivalDistance) / (slowDownDistance - arrivalDistance);
            return Mathf.Lerp(0.5f, autoMoveSpeed, t);
        }
        else
        {
            return autoMoveSpeed; // Full speed
        }
    }

    private IEnumerator RotateToStartingOrientation()
    {
        Debug.Log("Rotating to starting orientation...");

        Quaternion targetRotation = startingPoint.rotation;
        float angleDifference;

        do
        {
            playerController.transform.rotation = Quaternion.Slerp(
                playerController.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            angleDifference = Quaternion.Angle(playerController.transform.rotation, targetRotation);
            yield return null;

        } while (angleDifference > arrivalAngleThreshold);

        playerController.transform.rotation = targetRotation;
        Debug.Log("Rotation completed");
    }

    private void SnapToStartingPoint()
    {
        // Snap to exact position
        playerController.transform.position = startingPoint.position;

        // Set animation speed to zero - animator will automatically go to idle
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat(animIDSpeed, 0f);
            playerAnimator.SetFloat(animIDMotionSpeed, 0f);
        }

        Debug.Log("Snapped to starting point - animator will handle idle transition");
    }

    private void UpdateAnimatorSpeed(float currentSpeed)
    {
        if (playerAnimator != null)
        {
            // Convert movement speed to animation speed parameter
            // Your controller uses Speed parameter where 0=idle, 2=walk, 5.33=run
            float animSpeed = Mathf.Clamp(currentSpeed / 5.33f * 5f, 0f, 5f);

            playerAnimator.SetFloat(animIDSpeed, animSpeed);
            playerAnimator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    private void SwitchToSequenceCamera()
    {
        if (sequenceFollowCamera != null && normalFollowCamera != null)
        {
            // Immediate switch - sequence camera takes priority
            normalFollowCamera.Priority = 0;
            sequenceFollowCamera.Priority = sequenceCameraPriority;

            Debug.Log($"Switched to sequence camera (Priority: {sequenceCameraPriority})");
        }
    }

    private void SwitchToNormalCamera()
    {
        if (sequenceFollowCamera != null && normalFollowCamera != null)
        {
            // Switch back to normal camera
            sequenceFollowCamera.Priority = 0;
            normalFollowCamera.Priority = originalNormalCameraPriority;

            Debug.Log($"Switched back to normal camera (Priority: {originalNormalCameraPriority})");
        }
    }

    private void DisablePlayerInput()
    {
        if (playerInput != null)
        {
            // Zero out all inputs to stop any current movement
            playerInput.move = Vector2.zero;
            playerInput.jump = false;
            playerInput.sprint = false;
            playerInput.crawl = false;
            playerInput.push = false;
        }

        // IMPORTANT: Do NOT disable the playerController component
        // We want the animator to keep working normally
        // Just prevent new input

        // Lock cursor (for PC)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Player input disabled (controller still active for animation)");
    }

    private void EnablePlayerInput()
    {
        // Just re-enable the input system
        // The controller was never disabled, so it's ready to go

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Player input enabled");
    }

    private void DisableUIElements()
    {
        Debug.Log($"Disabling {uiElementsToDisable.Count} UI elements (they stay disabled until game ends)");

        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
                Debug.Log($"Disabled: {uiElement.name}");
            }
        }
    }

    private void EnableUIElements()
    {
        Debug.Log($"Enabling {uiElementsToEnable.Count} UI elements for object interaction");

        foreach (GameObject uiElement in uiElementsToEnable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
                Debug.Log($"Enabled: {uiElement.name}");
            }
        }
    }

    private void ReEnableAllUI()
    {
        Debug.Log("Re-enabling all UI elements (game ended)");

        // Re-enable all elements that were disabled
        foreach (GameObject uiElement in uiElementsToDisable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
                Debug.Log($"Re-enabled: {uiElement.name}");
            }
        }

        // Disable sequence-specific UI elements
        foreach (GameObject uiElement in uiElementsToEnable)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
                Debug.Log($"Disabled sequence UI: {uiElement.name}");
            }
        }
    }

    private void EnableObjectInteraction()
    {
        Debug.Log("Object interaction enabled - ready for tapping");

        // Unlock cursor for mobile tapping
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Public methods for external access
    public bool IsSequenceComplete() => hasReachedStartPoint;
    public bool IsInSequence() => isInStartingSequence;
    public bool CanInteractWithObjects()
    {
        return hasReachedStartPoint && !isInStartingSequence &&
               (playerAnimator == null || !playerAnimator.GetBool("isWalkingBackward"));
    }

    public string GetCurrentState()
    {
        if (isInStartingSequence) return "Moving to Start Point";
        if (hasReachedStartPoint) return "Idle at Start Point - Ready for Object Interaction";
        return "Waiting for Trigger";
    }

    // For debugging in the inspector
    void OnDrawGizmosSelected()
    {
        if (startingPoint != null)
        {
            // Draw starting point
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startingPoint.position, 0.3f);
            Gizmos.DrawLine(startingPoint.position, startingPoint.position + startingPoint.forward * 2f);

            // Draw slow down radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startingPoint.position, slowDownDistance);

            // Draw arrival radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startingPoint.position, arrivalDistance);
        }
    }

    // Editor validation
    void OnValidate()
    {
        // Ensure values are sensible
        autoMoveSpeed = Mathf.Max(0.1f, autoMoveSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        arrivalDistance = Mathf.Max(0.1f, arrivalDistance);
        slowDownDistance = Mathf.Max(arrivalDistance + 0.1f, slowDownDistance);

        // Ensure camera priority is reasonable
        sequenceCameraPriority = Mathf.Max(1, sequenceCameraPriority);
    }

    public Vector3 GetPlayerPosition()
    {
        if (playerController != null)
        {
            return playerController.transform.position;
        }
        return Vector3.zero;
    }

    public bool IsPlayerMoving()
    {
        if (playerAnimator != null)
        {
            float speed = playerAnimator.GetFloat("Speed");
            return speed > 0.1f;
        }
        return false;
    }

    public bool IsPlayerAtStartingPoint()
    {
        if (playerController == null || startingPoint == null) return false;

        float distance = Vector3.Distance(
            playerController.transform.position,
            startingPoint.position
        );

        return distance < 0.5f;
    }
}