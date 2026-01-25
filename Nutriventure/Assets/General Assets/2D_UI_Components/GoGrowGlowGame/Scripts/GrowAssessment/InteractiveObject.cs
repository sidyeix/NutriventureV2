using StarterAssets;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractiveObject : MonoBehaviour, IPointerClickHandler
{
    [Header("Object Points")]
    [SerializeField] private Transform animationPoint;
    [SerializeField] private Transform movePoint;
    [SerializeField] private Collider animationPointCollider;

    [Header("Object Settings")]
    [SerializeField] private bool isGrowFood = false;
    [SerializeField] private string objectName = "Object";

    [Header("Animation Settings")]
    [SerializeField] private string playerSmashTrigger = "isSmash";
    [SerializeField] private string playerDizzyBool = "isDizzy";
    [SerializeField] private string playerWalkingBackBool = "isWalkingBackward";
    [SerializeField] private string objectCorrectBool = "isCorrect";
    [SerializeField] private string objectWrongBool = "isWrong";
    [SerializeField] private float animationExitTime = 1.3f;

    [Header("Food Settings")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Transform foodSpawnPoint;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float backwardMoveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float arrivalDistance = 0.3f;
    [SerializeField] private float smashAnimationLength = 1.5f;
    [SerializeField] private float rotationStopDistance = 1f;

    [Header("Delay Settings")]
    [SerializeField] private float beforeSmashDelay = 0f;
    [SerializeField] private float beforeMoveDelay = 0.5f;
    [SerializeField] private float afterSmashDelay = 0.5f;

    [Header("Rotation Settings")]
    [SerializeField] private bool useAnimationPointForward = true;
    [SerializeField] private bool useMovePointForward = true;
    [SerializeField] private bool useColliderForFinalRotation = true;

    [Header("Point System")]
    [SerializeField] private int correctAnswerPoints = 1000;
    [SerializeField] private int wrongAnswerPoints = 500;

    [Header("Energy System")]
    [SerializeField] private float correctEnergyGain = 20f;
    [SerializeField] private float wrongEnergyDeduction = 25f;

    // References
    private Animator objectAnimator;
    private Animator playerAnimator;
    private ThirdPersonController playerController;
    private StartingSequenceManager sequenceManager;
    private CharacterController characterController;
    private ObjectGroupManager currentGroupManager;
    private GrowAssessmentManager assessmentManager;
    private GameObject spawnedFood;

    // State
    private bool isInteractable = true;
    private Vector3 latestPosition;
    private Collider objectCollider;
    private bool isProcessingInteraction = false;
    private Coroutine wrongAnimationResetCoroutine;
    private bool hasReachedAnimationPoint = false;

    void Start()
    {
        // Get references
        objectAnimator = GetComponent<Animator>();
        playerController = FindObjectOfType<ThirdPersonController>();
        sequenceManager = FindObjectOfType<StartingSequenceManager>();

        // Find assessment manager
        assessmentManager = GrowAssessmentManager.Instance;
        if (assessmentManager == null)
        {
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();
        }

        // Find group manager in parent hierarchy
        Transform parent = transform.parent;
        while (parent != null && currentGroupManager == null)
        {
            currentGroupManager = parent.GetComponent<ObjectGroupManager>();
            parent = parent.parent;
        }

        if (playerController != null)
        {
            playerAnimator = playerController.GetComponent<Animator>();
            characterController = playerController.GetComponent<CharacterController>();
            latestPosition = playerController.transform.position;
        }

        objectCollider = GetComponent<Collider>();
        AddPhysicsRaycasterToCamera();

        // Register with assessment manager if this is part of grow assessment
        if (assessmentManager != null && isGrowFood)
        {
            assessmentManager.RegisterAssessmentObject(this);
            Debug.Log($"Registered {objectName} with Assessment Manager (Correct Answer)");
        }

        // Validate
        if (animationPoint == null)
            Debug.LogError($"Animation Point not set on {objectName}");
        if (movePoint == null && isGrowFood)
            Debug.LogWarning($"Move Point not set on {objectName} (needed for correct choices)");

        // NEW: Spawn food immediately when the game starts
        if (foodPrefab != null && foodSpawnPoint != null && !isGrowFood) // Spawn junk food immediately
        {
            SpawnFoodImmediately();
        }
    }

    // NEW: Called by ObjectGroupManager when food prefab is assigned
    public void SpawnAssignedFood(GameObject prefab)
    {
        if (prefab == null || foodSpawnPoint == null) return;

        // Destroy existing food first
        DestroySpawnedFood();

        // Spawn new food
        spawnedFood = Instantiate(prefab, foodSpawnPoint.position, Quaternion.identity, foodSpawnPoint);
        Debug.Log($"Spawned assigned food for {objectName}: {prefab.name}");
    }

    private void AddPhysicsRaycasterToCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<PhysicsRaycaster>() == null)
        {
            mainCamera.gameObject.AddComponent<PhysicsRaycaster>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable || !sequenceManager.CanInteractWithObjects() || isProcessingInteraction)
            return;

        OnObjectClick();
    }

    public void OnObjectClick()
    {
        if (!isInteractable || !sequenceManager.CanInteractWithObjects() || isProcessingInteraction)
            return;

        Debug.Log($"Clicked {objectName} (Grow Food: {isGrowFood})");

        // Store current position as latest position BEFORE moving
        if (playerController != null)
        {
            latestPosition = playerController.transform.position;

            // Update respawn point in assessment manager
            if (assessmentManager != null)
            {
                assessmentManager.UpdateRespawnPoint(latestPosition);
            }
        }

        StartCoroutine(ObjectInteractionSequence());
    }

    private IEnumerator ObjectInteractionSequence()
    {
        isInteractable = false;
        isProcessingInteraction = true;
        hasReachedAnimationPoint = false;
        if (objectCollider != null)
            objectCollider.enabled = false;

        // Stop any existing wrong animation reset
        if (wrongAnimationResetCoroutine != null)
        {
            StopCoroutine(wrongAnimationResetCoroutine);
            wrongAnimationResetCoroutine = null;
        }

        // 1. Move to animation point
        yield return StartCoroutine(MoveCharacterToAnimationPoint());

        // 2. Optional: Delay before smash animation
        if (beforeSmashDelay > 0)
        {
            Debug.Log($"Waiting {beforeSmashDelay}s before smash animation");
            yield return new WaitForSeconds(beforeSmashDelay);
        }

        // 3. Play smash animations
        PlaySmashAnimations();

        // 4. Wait for player's smash animation length (1.5s)
        yield return new WaitForSeconds(smashAnimationLength);

        // 5. Reset player smash animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(playerSmashTrigger, false);
        }

        // 6. Handle based on food type
        if (isGrowFood)
        {
            // Play correct answer audio
            if (currentGroupManager != null)
            {
                currentGroupManager.PlayCorrectAnswerAudio();
            }

            // CORRECT OBJECT FLOW
            HandleCorrectAnswer();

            // Wait after smash delay
            yield return new WaitForSeconds(afterSmashDelay);

            // Wait before move delay
            yield return new WaitForSeconds(beforeMoveDelay);

            // NEW: Do NOT destroy food yet! Keep it visible while moving

            // Set isEntry to false for all objects before moving to move point
            if (currentGroupManager != null)
            {
                currentGroupManager.SetGroupEntryAnimation(false);
                Debug.Log("Set isEntry to false for all objects in group");
            }

            // Handle correct choice - THIS is where food will be destroyed
            yield return HandleCorrectChoice();
        }
        else
        {
            // Play incorrect answer audio
            if (currentGroupManager != null)
            {
                currentGroupManager.PlayIncorrectAnswerAudio();
            }

            // WRONG OBJECT FLOW
            HandleWrongAnswer();

            // Play dizzy animation during afterSmashDelay
            Debug.Log("Wrong object: Playing dizzy animation");
            if (playerAnimator != null)
            {
                playerAnimator.SetBool(playerDizzyBool, true);
            }

            // Enable dizzy effect through group manager
            if (currentGroupManager != null)
            {
                currentGroupManager.EnableDizzyEffect();
            }

            // Start dizzy audio through group manager
            if (currentGroupManager != null)
            {
                currentGroupManager.PlayDizzyAudio();
            }

            // Wait for dizzy animation (afterSmashDelay)
            yield return new WaitForSeconds(afterSmashDelay);

            // Stop dizzy animation
            if (playerAnimator != null)
            {
                playerAnimator.SetBool(playerDizzyBool, false);
            }

            // Disable dizzy effect through group manager
            if (currentGroupManager != null)
            {
                currentGroupManager.DisableDizzyEffect();
            }

            // Stop dizzy audio through group manager
            if (currentGroupManager != null)
            {
                currentGroupManager.StopDizzyAudio();
            }

            // Wait for object animation to complete (animationExitTime)
            yield return new WaitForSeconds(animationExitTime);

            // Wait before move delay
            yield return new WaitForSeconds(beforeMoveDelay);

            // NEW: Do NOT destroy food for wrong answer - keep it for retry

            // Handle wrong choice (with backward walking) - IMMEDIATELY AFTER DIZZY
            yield return HandleWrongChoice();
        }

        // 7. Cleanup
        isProcessingInteraction = false;
        Debug.Log($"{objectName} interaction complete");
    }

    // ====== MOVEMENT AND ANIMATION METHODS ======

    private IEnumerator MoveCharacterToAnimationPoint()
    {
        if (playerController == null || animationPoint == null) yield break;

        Transform player = playerController.transform;
        Vector3 targetPos = animationPoint.position;
        targetPos.y = player.position.y; // Keep same height

        // Get the final target rotation
        Quaternion finalTargetRotation = GetTargetRotationAtAnimationPoint();

        // 1. Face target
        yield return StartCoroutine(FaceTargetBeforeMove(targetPos));

        // 2. Move directly
        Vector3 startPos = player.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        // Smooth rotation parameters
        float rotationStartDistance = 1.0f; // Start rotating when 1 unit away
        bool hasStartedRotation = false;
        Quaternion startRotation = player.rotation;
        Vector3 initialDirection = (targetPos - startPos).normalized;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Current position
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            player.position = currentPos;

            // Calculate distance to target
            float remainingDistance = Vector3.Distance(currentPos, targetPos);

            // Start smooth rotation when close to target
            if (!hasStartedRotation && remainingDistance <= rotationStartDistance)
            {
                hasStartedRotation = true;
                startRotation = player.rotation;
            }

            if (hasStartedRotation)
            {
                // Calculate rotation progress (0 to 1)
                float rotationProgress = 1f - Mathf.Clamp01(remainingDistance / rotationStartDistance);

                // Smooth the rotation progress
                rotationProgress = Mathf.SmoothStep(0f, 1f, rotationProgress);

                // Smoothly rotate towards final rotation
                player.rotation = Quaternion.Lerp(
                    startRotation,
                    finalTargetRotation,
                    rotationProgress
                );
            }
            else
            {
                // Keep facing movement direction while moving
                player.rotation = Quaternion.LookRotation(initialDirection);
            }

            UpdatePlayerAnimatorSpeed(moveSpeed);
            yield return null;
        }

        // 3. Exact position snap
        player.position = targetPos;

        // 4. Final smooth rotation (in case we need a bit more)
        if (Quaternion.Angle(player.rotation, finalTargetRotation) > 1f)
        {
            float rotationTime = 0f;
            float maxRotationTime = 0.3f; // Quick final adjustment
            Quaternion currentRotation = player.rotation;

            while (rotationTime < maxRotationTime &&
                   Quaternion.Angle(player.rotation, finalTargetRotation) > 0.5f)
            {
                rotationTime += Time.deltaTime;
                float t = rotationTime / maxRotationTime;

                player.rotation = Quaternion.Lerp(
                    currentRotation,
                    finalTargetRotation,
                    t
                );
                yield return null;
            }
        }

        // Final exact rotation
        player.rotation = finalTargetRotation;

        UpdatePlayerAnimatorSpeed(0f);
        hasReachedAnimationPoint = true;
    }

    private void PlaySmashAnimations()
    {
        // Play player smash animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(playerSmashTrigger, true);
        }

        // Play object animation
        if (objectAnimator != null)
        {
            if (isGrowFood)
            {
                // Set isCorrect to true (stays true)
                objectAnimator.SetBool(objectCorrectBool, true);
                objectAnimator.SetBool(objectWrongBool, false);
                Debug.Log($"Set {objectCorrectBool} = true (will stay true)");
            }
            else
            {
                // Set isWrong to true
                objectAnimator.SetBool(objectWrongBool, true);
                objectAnimator.SetBool(objectCorrectBool, false);
                Debug.Log($"Set {objectWrongBool} = true");

                // Start coroutine to reset isWrong after the animation's natural exit time
                wrongAnimationResetCoroutine = StartCoroutine(ResetWrongAnimationWithExitTime());
            }
        }
    }

    private IEnumerator ResetWrongAnimationWithExitTime()
    {
        // Wait for the animation to play through its natural exit time
        Debug.Log($"Waiting {animationExitTime}s for wrong animation to complete naturally");
        yield return new WaitForSeconds(animationExitTime);

        // Now reset the animation
        if (objectAnimator != null)
        {
            objectAnimator.SetBool(objectWrongBool, false);
            Debug.Log($"Reset {objectWrongBool} = false (after natural exit)");
        }

        wrongAnimationResetCoroutine = null;
    }

    private IEnumerator HandleCorrectChoice()
    {
        Debug.Log($"{objectName} was the CORRECT choice!");

        // NEW: Food is already spawned, just keep it visible
        Debug.Log($"Food already visible for {objectName}, keeping it for player to see");

        // Move to move point
        if (movePoint != null)
        {
            yield return StartCoroutine(MoveCharacterToMovePoint());
            latestPosition = movePoint.position;

            // Update respawn point
            if (assessmentManager != null)
            {
                assessmentManager.UpdateRespawnPoint(latestPosition);
            }

            // NEW: NOW destroy the food when player reaches move point
            DestroySpawnedFood();
            Debug.Log($"Destroyed food for {objectName} after reaching move point");
        }
    }

    private IEnumerator HandleWrongChoice()
    {
        Debug.Log($"{objectName} was the WRONG choice!");

        // Safety check: Stop dizzy audio and disable dizzy effect through group manager
        if (currentGroupManager != null)
        {
            currentGroupManager.StopDizzyAudio();
            currentGroupManager.DisableDizzyEffect();
            Debug.Log("Safety stop: Stopped dizzy audio and disabled effect in HandleWrongChoice");
        }

        // NEW: Food stays visible for retry
        Debug.Log($"Food stays visible for {objectName} for retry");

        // Return to group starting point instead of latest position
        if (playerController != null)
        {
            yield return StartCoroutine(MoveCharacterBackwardToGroupStart());
        }

        // Re-enable this object for retry
        isInteractable = true;
        if (objectCollider != null)
            objectCollider.enabled = true;
    }

    private IEnumerator MoveCharacterToMovePoint()
    {
        if (playerController == null || movePoint == null) yield break;

        Debug.Log($"Moving character to {objectName}'s move point");

        Transform player = playerController.transform;
        Vector3 targetPos = movePoint.position;
        targetPos.y = player.position.y; // Keep same height

        // Get the final target rotation at move point
        Quaternion finalTargetRotation = GetTargetRotationAtMovePoint();

        // 1. Face target first
        yield return StartCoroutine(FaceTargetBeforeMove(targetPos));

        // 2. Move directly
        Vector3 startPos = player.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        // Start rotation slightly before reaching the target
        float rotationStartDistance = 1.0f; // Start rotating when 1 unit away
        bool hasStartedRotation = false;
        Quaternion startRotation = player.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Current position
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            player.position = currentPos;

            // Calculate distance to target
            float remainingDistance = Vector3.Distance(currentPos, targetPos);

            // Start smooth rotation when close to target
            if (!hasStartedRotation && remainingDistance <= rotationStartDistance)
            {
                hasStartedRotation = true;
                startRotation = player.rotation;
            }

            if (hasStartedRotation)
            {
                // Calculate rotation progress (0 to 1)
                float rotationProgress = 1f - Mathf.Clamp01(remainingDistance / rotationStartDistance);

                // Smooth the rotation progress
                rotationProgress = Mathf.SmoothStep(0f, 1f, rotationProgress);

                // Smoothly rotate towards final rotation
                player.rotation = Quaternion.Lerp(
                    startRotation,
                    finalTargetRotation,
                    rotationProgress
                );
            }
            else
            {
                // Keep facing movement direction while moving
                player.rotation = Quaternion.LookRotation((targetPos - startPos).normalized);
            }

            UpdatePlayerAnimatorSpeed(moveSpeed);
            yield return null;
        }

        // 3. Exact position snap
        player.position = targetPos;

        // 4. Final smooth rotation (in case we need a bit more)
        if (Quaternion.Angle(player.rotation, finalTargetRotation) > 1f)
        {
            float rotationTime = 0f;
            float maxRotationTime = 0.3f; // Quick final adjustment
            Quaternion currentRotation = player.rotation;

            while (rotationTime < maxRotationTime &&
                   Quaternion.Angle(player.rotation, finalTargetRotation) > 0.5f)
            {
                rotationTime += Time.deltaTime;
                float t = rotationTime / maxRotationTime;

                player.rotation = Quaternion.Lerp(
                    currentRotation,
                    finalTargetRotation,
                    t
                );
                yield return null;
            }
        }

        // Final exact rotation
        player.rotation = finalTargetRotation;

        UpdatePlayerAnimatorSpeed(0f);
        Debug.Log($"Character reached and properly facing at {objectName}'s move point");
    }

    private IEnumerator MoveCharacterBackwardToGroupStart()
    {
        if (playerController == null) yield break;

        // Get group starting point from group manager
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (currentGroupManager != null)
        {
            Transform groupStart = currentGroupManager.GetGroupStartingPoint();
            if (groupStart != null)
            {
                targetPosition = groupStart.position;
                targetRotation = groupStart.rotation;
                Debug.Log($"Moving to group starting point at {targetPosition}");
            }
            else
            {
                targetPosition = latestPosition;
                targetRotation = playerController.transform.rotation;
                Debug.LogWarning("Group starting point not found, using latest position");
            }
        }
        else
        {
            targetPosition = latestPosition;
            targetRotation = playerController.transform.rotation;
            Debug.LogWarning("Group manager not found, using latest position");
        }

        Transform player = playerController.transform;
        Vector3 targetPos = targetPosition;
        targetPos.y = player.position.y; // Keep same height

        // For backward movement, we need to face opposite direction
        Vector3 dirToTarget = targetPos - player.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude > 0.001f)
        {
            // Face away from target for backward walking
            Quaternion backwardRotation = Quaternion.LookRotation(-dirToTarget.normalized);
            yield return StartCoroutine(RotateCharacterToTargetSmooth(backwardRotation));
        }

        // Start backward walking animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(playerWalkingBackBool, true);
            Debug.Log("Starting backward walking animation");
        }

        // Move directly backward
        Vector3 startPos = player.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / backwardMoveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Linear interpolation - no overshoot
            player.position = Vector3.Lerp(startPos, targetPos, t);

            // Keep facing backward during movement
            Vector3 currentDir = targetPos - player.position;
            currentDir.y = 0f;
            if (currentDir.sqrMagnitude > 0.001f)
            {
                player.rotation = Quaternion.LookRotation(-currentDir.normalized);
            }

            UpdatePlayerAnimatorSpeed(backwardMoveSpeed);
            yield return null;
        }

        // Exact snap
        player.position = targetPos;

        // Rotate to face the group starting point's forward direction
        yield return StartCoroutine(RotateCharacterToTargetSmooth(targetRotation));

        // Stop backward walking animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(playerWalkingBackBool, false);
            Debug.Log("Stopped backward walking animation");
        }

        UpdatePlayerAnimatorSpeed(0f);
    }

    // ====== HELPER METHODS ======

    private Quaternion GetTargetRotationAtAnimationPoint()
    {
        if (animationPoint == null) return Quaternion.identity;

        if (useAnimationPointForward && animationPoint.forward != Vector3.zero)
        {
            return Quaternion.LookRotation(animationPoint.forward);
        }
        else
        {
            Vector3 directionToObject = transform.position - animationPoint.position;
            directionToObject.y = 0;
            if (directionToObject != Vector3.zero)
                return Quaternion.LookRotation(directionToObject.normalized);
        }

        return playerController != null ? playerController.transform.rotation : Quaternion.identity;
    }

    private Quaternion GetTargetRotationAtMovePoint()
    {
        if (movePoint == null) return Quaternion.identity;

        if (useMovePointForward && movePoint.forward != Vector3.zero)
        {
            return Quaternion.LookRotation(movePoint.forward);
        }
        else
        {
            if (animationPoint != null)
            {
                Vector3 directionToAnimationPoint = animationPoint.position - movePoint.position;
                directionToAnimationPoint.y = 0;
                if (directionToAnimationPoint != Vector3.zero)
                    return Quaternion.LookRotation(directionToAnimationPoint.normalized);
            }
        }

        return playerController != null ? playerController.transform.rotation : Quaternion.identity;
    }

    private IEnumerator RotateCharacterToTargetSmooth(Quaternion targetRotation)
    {
        if (playerController == null) yield break;

        float angleThreshold = 0.1f;
        float maxRotationTime = 0.5f;
        float elapsedTime = 0f;

        Quaternion startRotation = playerController.transform.rotation;

        while (Quaternion.Angle(playerController.transform.rotation, targetRotation) > angleThreshold &&
               elapsedTime < maxRotationTime)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / maxRotationTime);
            playerController.transform.rotation = Quaternion.Lerp(
                startRotation,
                targetRotation,
                t
            );
            yield return null;
        }

        playerController.transform.rotation = targetRotation;
    }

    private IEnumerator FaceTargetBeforeMove(Vector3 targetPos)
    {
        if (playerController == null) yield break;

        Transform player = playerController.transform;

        Vector3 dir = targetPos - player.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        float angle = Quaternion.Angle(player.rotation, targetRot);

        if (angle < 5f)
        {
            player.rotation = targetRot;
            yield break;
        }

        float rotationTime = 0f;
        float maxRotationTime = Mathf.Clamp(angle / 90f, 0.5f, 1.5f);

        while (rotationTime < maxRotationTime &&
               Quaternion.Angle(player.rotation, targetRot) > 1f)
        {
            rotationTime += Time.deltaTime;
            float t = rotationTime / maxRotationTime;

            t = Mathf.SmoothStep(0f, 1f, t);

            player.rotation = Quaternion.Lerp(
                player.rotation,
                targetRot,
                t
            );
            yield return null;
        }

        player.rotation = targetRot;
    }

    private void UpdatePlayerAnimatorSpeed(float speed)
    {
        if (playerAnimator != null)
        {
            float animSpeed = Mathf.Clamp(speed / 5.33f * 5f, 0f, 5f);
            playerAnimator.SetFloat("Speed", animSpeed);
            playerAnimator.SetFloat("MotionSpeed", speed > 0 ? 1f : 0f);
        }
    }

    // ====== FOOD MANAGEMENT METHODS ======

    // Method to destroy spawned food (ONLY when player reaches move point or game resets)
    public void DestroySpawnedFood()
    {
        if (spawnedFood != null)
        {
            Destroy(spawnedFood);
            spawnedFood = null;
            Debug.Log($"Destroyed spawned food for {objectName}");
        }

        // Also destroy any child food objects at the spawn point (safety cleanup)
        if (foodSpawnPoint != null)
        {
            foreach (Transform child in foodSpawnPoint)
            {
                if (child != foodSpawnPoint &&
                    (child.name.Contains("Food") || child.name.Contains("food") ||
                     child.name.Contains("Prefab") || child.name.Contains("prefab")))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // Method to spawn food immediately (used at Start or when group activates)
    public void SpawnFoodImmediately()
    {
        if (foodPrefab != null && foodSpawnPoint != null)
        {
            // Don't destroy existing food - just spawn new one
            if (spawnedFood == null)
            {
                spawnedFood = Instantiate(foodPrefab, foodSpawnPoint.position, Quaternion.identity, foodSpawnPoint);
                Debug.Log($"Spawned food immediately for {objectName}");
            }
        }
    }

    // Updated ResetObject method - destroys food ONLY when game resets
    public void ResetObject()
    {
        Debug.Log($"Resetting object: {objectName}");

        // Reset all state variables
        isInteractable = true;
        isProcessingInteraction = false;
        hasReachedAnimationPoint = false;

        // Reset collider
        if (objectCollider != null)
        {
            objectCollider.enabled = true;
        }

        // Destroy spawned food ONLY when game resets completely
        DestroySpawnedFood();

        // Reset animator
        if (objectAnimator != null)
        {
            objectAnimator.SetBool(objectCorrectBool, false);
            objectAnimator.SetBool(objectWrongBool, false);
            objectAnimator.Rebind();
            objectAnimator.Update(0f);
        }

        // Stop any coroutines
        if (wrongAnimationResetCoroutine != null)
        {
            StopCoroutine(wrongAnimationResetCoroutine);
            wrongAnimationResetCoroutine = null;
        }

        Debug.Log($"Object reset complete: {objectName}");
    }

    // ====== ANSWER HANDLING METHODS ======

    private void HandleCorrectAnswer()
    {
        Debug.Log($"Correct answer! Awarding {correctAnswerPoints} points and {correctEnergyGain} energy");

        if (assessmentManager != null)
        {
            assessmentManager.OnCorrectAnswerSelected();
        }

        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(correctAnswerPoints);
            GoGrowGlowGameManager.Instance.AddEnergy(correctEnergyGain);
            Debug.Log($"DIRECT ENERGY UPDATE: Added {correctEnergyGain} energy via Game Manager");
        }
        else
        {
            Debug.LogError("Game Manager Instance is also NULL!");
        }
    }

    private void HandleWrongAnswer()
    {
        Debug.Log($"Wrong answer! Deducting {wrongAnswerPoints} points and {wrongEnergyDeduction} energy");

        if (assessmentManager != null)
        {
            assessmentManager.OnWrongAnswerSelected();
        }

        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(-wrongAnswerPoints);
            GoGrowGlowGameManager.Instance.RemoveEnergy(wrongEnergyDeduction);
            Debug.Log($"DIRECT ENERGY UPDATE: Removed {wrongEnergyDeduction} energy via Game Manager");
        }
        else
        {
            Debug.LogError("Game Manager Instance is also NULL!");
        }
    }

    // ====== PUBLIC METHODS ======

    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        if (objectCollider != null)
            objectCollider.enabled = interactable;
    }

    public void SetIsGrowFood(bool isGrow)
    {
        isGrowFood = isGrow;

        if (assessmentManager != null && isGrowFood && !assessmentManager.IsAssessmentActive())
        {
            assessmentManager.RegisterAssessmentObject(this);
            Debug.Log($"Updated {objectName} as correct answer with Assessment Manager");
        }
    }

    public void SetFoodPrefab(GameObject prefab)
    {
        foodPrefab = prefab;
        // Spawn food immediately when prefab is assigned
        SpawnFoodImmediately();
    }

    public void SetDelaySettings(float beforeSmash, float beforeMove, float afterSmash)
    {
        beforeSmashDelay = beforeSmash;
        beforeMoveDelay = beforeMove;
        afterSmashDelay = afterSmash;
    }

    public void SetAnimationExitTime(float exitTime)
    {
        animationExitTime = exitTime;
    }

    public void SetGroupManager(ObjectGroupManager manager)
    {
        currentGroupManager = manager;
    }

    public void SetAssessmentManager(GrowAssessmentManager manager)
    {
        assessmentManager = manager;

        if (assessmentManager != null && isGrowFood)
        {
            assessmentManager.RegisterAssessmentObject(this);
        }
    }

    public void SetPointValues(int correctPoints, int wrongPoints)
    {
        correctAnswerPoints = correctPoints;
        wrongAnswerPoints = wrongPoints;
        Debug.Log($"Set points: Correct={correctPoints}, Wrong={wrongPoints}");
    }

    public void SetEnergyValues(float correctEnergy, float wrongEnergy)
    {
        correctEnergyGain = correctEnergy;
        wrongEnergyDeduction = wrongEnergy;
        Debug.Log($"Set energy: Correct=+{correctEnergy}, Wrong=-{wrongEnergy}");
    }

    public bool IsGrowFood() => isGrowFood;
    public Transform GetAnimationPoint() => animationPoint;
    public Transform GetMovePoint() => movePoint;
    public Transform GetFoodSpawnPoint() => foodSpawnPoint;
    public bool IsInteractable() => isInteractable;
    public bool IsProcessingInteraction() => isProcessingInteraction;
    public int GetCorrectAnswerPoints() => correctAnswerPoints;
    public int GetWrongAnswerPoints() => wrongAnswerPoints;
    public float GetCorrectEnergyGain() => correctEnergyGain;
    public float GetWrongEnergyDeduction() => wrongEnergyDeduction;

    public void SetSpawnedFood(GameObject food)
    {
        spawnedFood = food;
    }

    public GameObject GetSpawnedFood()
    {
        return spawnedFood;
    }

    void OnDrawGizmosSelected()
    {
        if (animationPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(animationPoint.position, 0.2f);
            Gizmos.DrawLine(animationPoint.position, animationPoint.position + animationPoint.forward * 1f);
            Gizmos.DrawLine(animationPoint.position, transform.position);
        }

        if (movePoint != null && isGrowFood)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(movePoint.position, 0.2f);
            Gizmos.DrawLine(movePoint.position, movePoint.position + movePoint.forward * 1f);
            if (animationPoint != null)
                Gizmos.DrawLine(animationPoint.position, movePoint.position);
        }

        Gizmos.color = isGrowFood ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.2f);
    }
}