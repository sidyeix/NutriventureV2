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
    [SerializeField] private int normalCameraPriority = 20; // CHANGED: Set this to 20 instead of 10

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
            // Set the desired normal camera priority to 20
            normalCameraPriority = 20; // CHANGED: Set this to 20 instead of 10
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
        if (!hasReachedStartPoint) return; // Only enable if we actually started the sequence

        Debug.Log("Enabling all controls and UI (game end)");

        // Step 1: Switch back to normal camera with priority 20
        SwitchToNormalCamera();

        // Step 2: Re-enable player input
        EnablePlayerInput();

        // Step 3: Re-enable all UI elements that were disabled
        ReEnableAllUI();

        // Step 4: Reset sequence state
        hasReachedStartPoint = false;

        Debug.Log("Game ended - All controls and UI restored, camera priority set to 20"); // CHANGED: Updated log message
    }

    // NEW: Separate method for ending just the assessment (without resetting everything)
    public void EndAssessmentOnly()
    {
        if (!hasReachedStartPoint) return;

        Debug.Log("Ending assessment only");

        // Switch back to normal camera with priority 20
        SwitchToNormalCamera();

        // Re-enable player input
        EnablePlayerInput();

        // Re-enable all UI elements that were disabled
        ReEnableAllUI();

        // Don't reset hasReachedStartPoint if you want to keep the sequence state
        // hasReachedStartPoint = false;
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
            // Switch back to normal camera with priority 20
            sequenceFollowCamera.Priority = 0;
            normalFollowCamera.Priority = normalCameraPriority;

            Debug.Log($"Switched back to normal camera (Priority: {normalCameraPriority})");
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

    // The rest of your methods remain the same...
    private IEnumerator MoveToStartingPointSmooth()
    {
        Debug.Log("Moving to starting point with smooth stop...");

        while (Vector3.Distance(playerController.transform.position, startingPoint.position) > arrivalDistance)
        {
            float distanceRemaining = Vector3.Distance(playerController.transform.position, startingPoint.position);
            Vector3 direction = (startingPoint.position - playerController.transform.position).normalized;
            float currentSpeed = CalculateSpeedBasedOnDistance(distanceRemaining);
            Vector3 moveVector = direction * currentSpeed * Time.deltaTime;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveVector);
            }
            else
            {
                playerController.transform.position += moveVector;
            }

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                playerController.transform.rotation = Quaternion.Slerp(
                    playerController.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            UpdateAnimatorSpeed(currentSpeed);
            yield return null;
        }

        Debug.Log("Reached starting point proximity");
    }

    private float CalculateSpeedBasedOnDistance(float distance)
    {
        if (distance <= arrivalDistance)
        {
            return 0f;
        }
        else if (distance <= slowDownDistance)
        {
            float t = (distance - arrivalDistance) / (slowDownDistance - arrivalDistance);
            return Mathf.Lerp(0.5f, autoMoveSpeed, t);
        }
        else
        {
            return autoMoveSpeed;
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
        playerController.transform.position = startingPoint.position;

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
            float animSpeed = Mathf.Clamp(currentSpeed / 5.33f * 5f, 0f, 5f);
            playerAnimator.SetFloat(animIDSpeed, animSpeed);
            playerAnimator.SetFloat(animIDMotionSpeed, 1f);
        }
    }

    private void EnableObjectInteraction()
    {
        Debug.Log("Object interaction enabled - ready for tapping");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Public methods for external access
    public bool IsSequenceComplete() => hasReachedStartPoint;
    public bool IsInSequence() => isInStartingSequence;
    public bool CanInteractWithObjects() => hasReachedStartPoint && !isInStartingSequence;

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
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(startingPoint.position, 0.3f);
            Gizmos.DrawLine(startingPoint.position, startingPoint.position + startingPoint.forward * 2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startingPoint.position, slowDownDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(startingPoint.position, arrivalDistance);
        }
    }

    void OnValidate()
    {
        autoMoveSpeed = Mathf.Max(0.1f, autoMoveSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        arrivalDistance = Mathf.Max(0.1f, arrivalDistance);
        slowDownDistance = Mathf.Max(arrivalDistance + 0.1f, slowDownDistance);
        sequenceCameraPriority = Mathf.Max(1, sequenceCameraPriority);

        // Ensure normal camera priority is 20
        normalCameraPriority = 20; // CHANGED: Set to 20 instead of 10
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

    // NEW: Force camera reset (call this if needed)
    public void ForceCameraReset()
    {
        SwitchToNormalCamera();
        Debug.Log("Forced camera reset to normal priority");
    }
}