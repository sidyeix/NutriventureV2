using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Added for new Input System

public class K2_NPCtrigInstructs : MonoBehaviour
{
    [Header("Cutscene References")]
    [SerializeField] private GameObject cutsceneParentObject; // "CutsceneThings" parent object
    [SerializeField] private PlayableDirector npcCutsceneDirector; // "NPC_Cutscene" PlayableDirector
    [SerializeField] private GameObject dummyItem; // The item that will be picked up
    
    [Header("NPC References")]
    [SerializeField] private GameObject arrowIndicatorCanvas; // The floating arrow UI
    [SerializeField] private Transform npcTransform; // Reference to NPC (optional)
    
    [Header("Dialogue Canvas")]
    [SerializeField] private GameObject dialogueCanvas; // NPC dialogue box canvas
    [SerializeField] private bool showDialogueDuringCutscene = true; // Toggle for dialogue visibility
    
    [Header("Skip Button")]
    [SerializeField] private Button skipButton; // Button to skip the cutscene
    [SerializeField] private bool enableSkipButton = true; // Whether skip button is enabled
    [SerializeField] private float skipButtonDelay = 2f; // Delay before skip button appears
    
    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; // Reference to player (with ThirdPersonController)
    
    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler; // "Audio_Handler" GameObject
    [SerializeField] private GameObject gameUICanvas; // "UI_Canvas_StarterAssetsInputs_Joysticks"
    
    [Header("Cutscene Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool requirePlayerFacingNPC = false;
    [SerializeField] private float facingAngleThreshold = 45f;
    [SerializeField] private bool oneTimeInteraction = true; // Can only trigger once
    
    [Header("Events")]
    public UnityEvent onCutsceneStart;
    public UnityEvent onCutsceneEnd;
    public UnityEvent onCutsceneSkipped; // Event fired when cutscene is skipped
    
    private bool isCutscenePlaying = false;
    private bool hasTriggered = false;
    private Transform playerTransform;
    private Vector3 dummyItemOriginalPosition;
    private Quaternion dummyItemOriginalRotation;
    private ThirdPersonController playerController; // Reference to the controller
    private Animator playerAnimator; // Reference to player's Animator
    private AudioSource playerAudioSource; // Reference to player's AudioSource
    private StarterAssetsInputs playerInputs; // Reference to player inputs
    
    // Store original states
    private bool wasControllerEnabled = true;
    private bool wasAnimatorEnabled = true;
    private bool wasAudioSourceEnabled = true;
    private Vector2 originalMoveInput;
    private bool originalSprintState;
    private bool originalJumpState;
    private float originalAnimatorSpeed;
    
    // Skip button timer
    private float skipButtonTimer = 0f;
    private bool skipButtonReady = false;
    
    // Input System reference
    private PlayerInput playerInputComponent;
    private bool playerInputWasEnabled = true;
    
    void Start()
    {
        InitializeComponents();
    }
    
    void InitializeComponents()
    {
        // Ensure cutscene parent is disabled initially
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(false);
        }
        
        // Ensure PlayableDirector is stopped
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.Stop();
            npcCutsceneDirector.stopped += OnCutsceneFinished;
            
            // Subscribe to timeline events
            npcCutsceneDirector.played += OnCutscenePlayed;
            npcCutsceneDirector.paused += OnCutscenePaused;
        }
        
        // Store dummy item's original position/rotation if it exists
        if (dummyItem != null)
        {
            dummyItemOriginalPosition = dummyItem.transform.position;
            dummyItemOriginalRotation = dummyItem.transform.rotation;
            dummyItem.SetActive(false); // Disabled initially
        }
        
        // Ensure arrow indicator is visible initially
        if (arrowIndicatorCanvas != null)
        {
            arrowIndicatorCanvas.SetActive(true);
        }
        
        // Initialize dialogue canvas
        if (dialogueCanvas != null)
        {
            // Hide dialogue canvas initially
            dialogueCanvas.SetActive(false);
        }
        
        // Initialize skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            skipButton.gameObject.SetActive(false); // Hidden by default
        }
        
        // Ensure game UI canvas is enabled initially (if reference exists)
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
        }
        
        // Ensure audio handler is enabled initially (if reference exists)
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }
        
        // Get player component references
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<ThirdPersonController>();
            playerAnimator = playerObject.GetComponent<Animator>();
            playerAudioSource = playerObject.GetComponent<AudioSource>();
            playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
            playerInputComponent = playerObject.GetComponent<PlayerInput>();
            
            if (playerController == null)
            {
                Debug.LogWarning("ThirdPersonController not found on player object!");
            }
        }
    }
    
    void OnDestroy()
    {
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.stopped -= OnCutsceneFinished;
            npcCutsceneDirector.played -= OnCutscenePlayed;
            npcCutsceneDirector.paused -= OnCutscenePaused;
        }
        
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }
    
    void Update()
    {
        // Handle skip button timer
        if (isCutscenePlaying && enableSkipButton && !skipButtonReady)
        {
            skipButtonTimer += Time.deltaTime;
            
            if (skipButtonTimer >= skipButtonDelay)
            {
                skipButtonReady = true;
                ShowSkipButton();
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Auto-trigger when player enters trigger collider
        if (other.CompareTag("Player") && !hasTriggered && !isCutscenePlaying)
        {
            playerTransform = other.transform;
            
            // If player object not assigned, get it from the collider
            if (playerObject == null)
            {
                playerObject = other.gameObject;
                InitializePlayerComponents();
            }
            
            TriggerCutscene();
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        // Alternative: Use Input System for interaction (mobile touch/button)
        if (other.CompareTag("Player") && !hasTriggered && !isCutscenePlaying)
        {
            playerTransform = other.transform;
            
            // If player object not assigned, get it from the collider
            if (playerObject == null)
            {
                playerObject = other.gameObject;
                InitializePlayerComponents();
            }
            
            // Check for interaction using the new Input System
            // This could be a touch button, gamepad button, etc.
            // You'll need to configure this in your Input Actions
        }
    }
    
    void InitializePlayerComponents()
    {
        playerController = playerObject.GetComponent<ThirdPersonController>();
        playerAnimator = playerObject.GetComponent<Animator>();
        playerAudioSource = playerObject.GetComponent<AudioSource>();
        playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
        playerInputComponent = playerObject.GetComponent<PlayerInput>();
    }
    
    bool IsPlayerFacingNPC()
    {
        if (playerTransform == null || npcTransform == null) return true;
        
        Vector3 directionToNPC = (npcTransform.position - playerTransform.position).normalized;
        Vector3 playerForward = playerTransform.forward;
        
        float angle = Vector3.Angle(playerForward, directionToNPC);
        
        return angle <= facingAngleThreshold;
    }
    
    // Public method to trigger cutscene from UI button (for mobile)
    public void TriggerCutsceneFromUI()
    {
        if (!hasTriggered && !isCutscenePlaying && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            
            if (distance <= interactionRange)
            {
                // Check if player is facing NPC (if required)
                if (!requirePlayerFacingNPC || IsPlayerFacingNPC())
                {
                    TriggerCutscene();
                }
            }
        }
    }
    
    public void TriggerCutscene()
    {
        if (hasTriggered || isCutscenePlaying) return;
        
        hasTriggered = true;
        isCutscenePlaying = true;
        skipButtonTimer = 0f;
        skipButtonReady = false;
        
        // Store original player states
        StoreOriginalPlayerStates();
        
        // Completely freeze the player
        FreezePlayerCompletely();
        
        // Hide arrow indicator
        if (arrowIndicatorCanvas != null)
        {
            arrowIndicatorCanvas.SetActive(false);
        }
        
        // Enable cutscene parent object
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(true);
        }
        
        // Reset and enable dummy item
        SetupDummyItem();
        
        // Disable game UI during cutscene
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
        }
        
        // Disable audio handler during cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
        }
        
        // Play the cutscene
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.Play();
        }
        
        // Invoke start event
        onCutsceneStart?.Invoke();
        
        Debug.Log("NPC Cutscene triggered - Player completely frozen");
    }
    
    private void StoreOriginalPlayerStates()
    {
        if (playerController != null)
        {
            wasControllerEnabled = playerController.enabled;
        }
        
        if (playerAnimator != null)
        {
            wasAnimatorEnabled = playerAnimator.enabled;
            originalAnimatorSpeed = playerAnimator.speed;
        }
        
        if (playerAudioSource != null)
        {
            wasAudioSourceEnabled = playerAudioSource.enabled;
        }
        
        if (playerInputs != null)
        {
            originalMoveInput = playerInputs.move;
            originalSprintState = playerInputs.sprint;
            originalJumpState = playerInputs.jump;
        }
        
        // Store PlayerInput state
        if (playerInputComponent != null)
        {
            playerInputWasEnabled = playerInputComponent.enabled;
        }
    }
    
    private void FreezePlayerCompletely()
    {
        // 1. Disable the ThirdPersonController
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // 2. Stop all animations completely
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
        }
        
        // 3. Stop all audio
        if (playerAudioSource != null)
        {
            playerAudioSource.enabled = false;
            playerAudioSource.Stop();
            
            // Also stop any AudioSource components on children
            AudioSource[] allAudioSources = playerObject.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource audioSource in allAudioSources)
            {
                audioSource.enabled = false;
                audioSource.Stop();
            }
        }
        
        // 4. Reset all player inputs
        if (playerInputs != null)
        {
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.sprint = false;
            playerInputs.jump = false;
            
            // Disable the input component
            playerInputs.enabled = false;
        }
        
        // 5. Stop any physics movement
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // 6. Disable CharacterController movement
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // 7. Disable PlayerInput component (Input System)
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = false;
        }
        
        // 8. Find and disable any additional movement scripts
        MonoBehaviour[] allScripts = playerObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null && script.enabled && script != this)
            {
                // Skip specific scripts we don't want to disable
                if (script.GetType().Name.Contains("Camera") || 
                    script.GetType().Name.Contains("UI") ||
                    script.GetType().Name.Contains("Canvas"))
                {
                    continue;
                }
                
                // Disable anything that might affect movement
                script.enabled = false;
            }
        }
        
        Debug.Log("Player completely frozen - Controller, Animator, Audio, and Inputs disabled");
    }
    
    private void SetupDummyItem()
    {
        if (dummyItem != null)
        {
            // Reset item to original position/rotation
            dummyItem.transform.position = dummyItemOriginalPosition;
            dummyItem.transform.rotation = dummyItemOriginalRotation;
            
            // Ensure it's active
            dummyItem.SetActive(true);
            
            // If the item has a Rigidbody, reset its physics
            Rigidbody rb = dummyItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
            
            // If the item has a Collider, ensure it's enabled
            Collider collider = dummyItem.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
    }
    
    // Event handler when cutscene starts playing
    private void OnCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Cutscene started playing");
        
        // Show dialogue canvas when cutscene starts
        if (showDialogueDuringCutscene && dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas activated");
        }
    }
    
    // Event handler when cutscene is paused
    private void OnCutscenePaused(PlayableDirector director)
    {
        Debug.Log("Cutscene paused");
        
        // Hide dialogue canvas when cutscene is paused
        if (dialogueCanvas != null && dialogueCanvas.activeSelf)
        {
            dialogueCanvas.SetActive(false);
        }
    }
    
    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Check if this was triggered by skip (to avoid double-finishing)
        if (isCutscenePlaying)
        {
            FinishCutscene(false); // false = not skipped
        }
    }
    
    // Skip button click handler
    private void OnSkipButtonClicked()
    {
        if (isCutscenePlaying)
        {
            SkipCutscene();
        }
    }
    
    // Show skip button with delay
    private void ShowSkipButton()
    {
        if (skipButton != null && enableSkipButton)
        {
            skipButton.gameObject.SetActive(true);
            Debug.Log("Skip button activated");
        }
    }
    
    // Hide skip button
    private void HideSkipButton()
    {
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
    }
    
    // Public method to skip cutscene
    public void SkipCutscene()
    {
        if (isCutscenePlaying && npcCutsceneDirector != null)
        {
            // Stop the timeline
            npcCutsceneDirector.Stop();
            
            // Finish the cutscene with skipped flag
            FinishCutscene(true); // true = skipped
            
            // Invoke skipped event
            onCutsceneSkipped?.Invoke();
            
            Debug.Log("Cutscene skipped by player");
        }
    }
    
    private void FinishCutscene(bool wasSkipped = false)
    {
        isCutscenePlaying = false;
        
        // Hide skip button
        HideSkipButton();
        
        // Hide dialogue canvas when cutscene ends
        if (dialogueCanvas != null && dialogueCanvas.activeSelf)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas deactivated");
        }
        
        // Disable cutscene parent object
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(false);
        }
        
        // Re-enable game UI after cutscene
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
        }
        
        // Re-enable audio handler after cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }
        
        // Unfreeze the player
        UnfreezePlayer();
        
        // Note: Arrow indicator stays hidden after cutscene
        // If you want it to reappear (for repeatable interactions), use this:
        if (!oneTimeInteraction && arrowIndicatorCanvas != null)
        {
            arrowIndicatorCanvas.SetActive(true);
            hasTriggered = false; // Reset trigger for repeat interactions
        }
        
        // Invoke end event
        onCutsceneEnd?.Invoke();
        
        if (wasSkipped)
        {
            Debug.Log("NPC Cutscene skipped - Player unfrozen, dummy item remains visible");
        }
        else
        {
            Debug.Log("NPC Cutscene finished - Player unfrozen, dummy item remains visible");
        }
    }
    
    private void UnfreezePlayer()
    {
        // 1. Re-enable the ThirdPersonController (if it was enabled before)
        if (playerController != null)
        {
            playerController.enabled = wasControllerEnabled;
        }
        
        // 2. Re-enable animator
        if (playerAnimator != null)
        {
            playerAnimator.enabled = wasAnimatorEnabled;
            
            // Reset animation states
            if (wasAnimatorEnabled)
            {
                playerAnimator.SetFloat("Speed", 0f);
                playerAnimator.SetFloat("MotionSpeed", 0f);
                playerAnimator.SetBool("Grounded", true);
                playerAnimator.SetBool("Jump", false);
                playerAnimator.SetBool("FreeFall", false);
            }
        }
        
        // 3. Re-enable audio
        if (playerAudioSource != null)
        {
            playerAudioSource.enabled = wasAudioSourceEnabled;
            
            // Re-enable AudioSource components on children
            AudioSource[] allAudioSources = playerObject.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource audioSource in allAudioSources)
            {
                audioSource.enabled = wasAudioSourceEnabled;
            }
        }
        
        // 4. Re-enable and restore inputs
        if (playerInputs != null)
        {
            playerInputs.enabled = true;
            playerInputs.move = Vector2.zero; // Start with zero input
            playerInputs.look = Vector2.zero;
            playerInputs.sprint = false;
            playerInputs.jump = false;
        }
        
        // 5. Re-enable physics
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // 6. Re-enable CharacterController
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        // 7. Re-enable PlayerInput component (Input System)
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = playerInputWasEnabled;
        }
        
        // 8. Re-enable all other scripts
        MonoBehaviour[] allScripts = playerObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null && script != this)
            {
                // Skip specific scripts
                if (script.GetType().Name.Contains("Camera") || 
                    script.GetType().Name.Contains("UI") ||
                    script.GetType().Name.Contains("Canvas"))
                {
                    continue;
                }
                
                // Re-enable the script
                script.enabled = true;
            }
        }
        
        Debug.Log("Player unfrozen - All components restored");
    }
    
    // Reset the interaction (for debugging or game reset)
    public void ResetInteraction()
    {
        hasTriggered = false;
        isCutscenePlaying = false;
        skipButtonReady = false;
        skipButtonTimer = 0f;
        
        // Hide skip button
        HideSkipButton();
        
        if (arrowIndicatorCanvas != null)
        {
            arrowIndicatorCanvas.SetActive(true);
        }
        
        if (dummyItem != null)
        {
            dummyItem.SetActive(false);
        }
        
        // Hide dialogue canvas on reset
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
        
        // Ensure player is unfrozen on reset
        UnfreezePlayer();
        
        Debug.Log("NPC interaction reset");
    }
    
    // Optional: Gizmos for visualization
    void OnDrawGizmosSelected()
    {
        // Draw interaction range sphere
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw trigger collider bounds
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
        }
    }
    
    // Public method to manually trigger cutscene from other scripts
    public void ManualTriggerCutscene()
    {
        if (!hasTriggered && !isCutscenePlaying)
        {
            TriggerCutscene();
        }
    }
    
    // Enable/disable skip button functionality
    public void SetSkipButtonEnabled(bool enabled)
    {
        enableSkipButton = enabled;
        
        if (!enabled && skipButton != null)
        {
            HideSkipButton();
        }
        
        Debug.Log($"Skip button functionality {(enabled ? "enabled" : "disabled")}");
    }
    
    // Set skip button delay
    public void SetSkipButtonDelay(float delay)
    {
        skipButtonDelay = Mathf.Max(0f, delay);
        Debug.Log($"Skip button delay set to: {skipButtonDelay} seconds");
    }
    
    // Check if cutscene is currently playing
    public bool IsCutscenePlaying()
    {
        return isCutscenePlaying;
    }
    
    // Check if skip button is ready/visible
    public bool IsSkipButtonReady()
    {
        return skipButtonReady;
    }
    
    // Get remaining time until skip button appears
    public float GetSkipButtonTimeRemaining()
    {
        if (skipButtonReady) return 0f;
        return Mathf.Max(0f, skipButtonDelay - skipButtonTimer);
    }
    
    // Mobile-specific method: Trigger cutscene from proximity
    public void CheckAndTriggerCutscene()
    {
        if (hasTriggered || isCutscenePlaying || playerTransform == null) return;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distance <= interactionRange)
        {
            // Check if player is facing NPC (if required)
            if (!requirePlayerFacingNPC || IsPlayerFacingNPC())
            {
                TriggerCutscene();
            }
        }
    }
}