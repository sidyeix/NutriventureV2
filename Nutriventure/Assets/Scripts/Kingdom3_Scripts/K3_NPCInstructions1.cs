using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class K3_NPCinstructions1 : MonoBehaviour
{
    [Header("Cutscene References")]
    [SerializeField] private GameObject cutsceneParentObject; // "CutsceneThings" parent object
    [SerializeField] private PlayableDirector npcCutsceneDirector; // PlayableDirector for this timeline
    
    [Header("NPC References")]
    [SerializeField] private GameObject arrowIndicatorCanvas; // The floating arrow UI
    [SerializeField] private Transform npcTransform; // Reference to NPC (optional)
    
    [Header("Dialogue Canvas")]
    [SerializeField] private GameObject dialogueCanvas; // NPC dialogue box canvas
    [SerializeField] private bool showDialogueDuringCutscene = true; // Toggle for dialogue visibility
    [SerializeField] private TMP_Text npcNameText; // TextMeshPro for NPC name
    
    [Header("Subtitle System - CRITICAL: Assign These")]
    [SerializeField] private GameObject subtitleCanvas; // Separate canvas for subtitles (optional)
    [SerializeField] private TextMeshProUGUI subtitleTextUI; // MUST be TextMeshProUGUI for K2_SubtitleController
    [SerializeField] private K2_SubtitleController subtitleController; // Subtitle controller component
    
    [Header("Skip Button")]
    [SerializeField] private Button skipButton; // Button to skip the cutscene
    [SerializeField] private bool enableSkipButton = true; // Whether skip button is enabled
    [SerializeField] private float skipButtonDelay = 2f; // Delay before skip button appears
    
    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; // Reference to player (with ThirdPersonController)
    
    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler; // "Audio_Handler" GameObject
    [SerializeField] private GameObject gameUICanvas; // "UI_Canvas_StarterAssetsInputs_Joysticks"
    
    [Header("UI Elements to Control After Cutscene")]
    [SerializeField] private GameObject healthContainer; // Health Container (initially disabled)
    [SerializeField] private GameObject pointsPanel; // Points Panel (initially disabled)
    [SerializeField] private GameObject timerPanel; // Timer Panel (initially disabled)
    [SerializeField] private GameObject profileGameObject; // Profile GameObject (initially enabled)
    
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
    
    // NPC name text state
    private bool npcNameTextWasActive = false;
    
    // Subtitle tracking
    private bool subtitleCanvasWasActive = false;
    private bool subtitleTextWasActive = false;
    
    // NEW: Track original UI element states
    private bool healthContainerOriginalState = false;
    private bool pointsPanelOriginalState = false;
    private bool timerPanelOriginalState = false;
    private bool profileGameObjectOriginalState = false;
    
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
        
        // Initialize NPC name text
        if (npcNameText != null)
        {
            // Store whether it was active before initialization
            npcNameTextWasActive = npcNameText.gameObject.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            npcNameText.gameObject.SetActive(false);
            Debug.Log($"NPC name text initialized: {npcNameText.name}, was active: {npcNameTextWasActive}");
        }
        else
        {
            Debug.Log("No NPC name text assigned - skipping NPC name display");
        }
        
        // Initialize subtitle system
        InitializeSubtitleSystem();
        
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
        
        // NEW: Store original UI element states
        StoreOriginalUIStates();
        
        // NEW: Log UI element assignment status
        LogUIElementStatus();
    }
    
    // NEW: Store original UI element states
    private void StoreOriginalUIStates()
    {
        if (healthContainer != null)
            healthContainerOriginalState = healthContainer.activeSelf;
        
        if (pointsPanel != null)
            pointsPanelOriginalState = pointsPanel.activeSelf;
        
        if (timerPanel != null)
            timerPanelOriginalState = timerPanel.activeSelf;
        
        if (profileGameObject != null)
            profileGameObjectOriginalState = profileGameObject.activeSelf;
        
        Debug.Log($"UI Original states: Health={healthContainerOriginalState}, Points={pointsPanelOriginalState}, Timer={timerPanelOriginalState}, Profile={profileGameObjectOriginalState}");
    }
    
    // NEW: Log UI element assignment status
    private void LogUIElementStatus()
    {
        Debug.Log("=== UI ELEMENTS STATUS ===");
        Debug.Log($"Health Container: {(healthContainer != null ? healthContainer.name : "NOT ASSIGNED")} - Current: {(healthContainer != null ? healthContainer.activeSelf.ToString() : "N/A")}");
        Debug.Log($"Points Panel: {(pointsPanel != null ? pointsPanel.name : "NOT ASSIGNED")} - Current: {(pointsPanel != null ? pointsPanel.activeSelf.ToString() : "N/A")}");
        Debug.Log($"Timer Panel: {(timerPanel != null ? timerPanel.name : "NOT ASSIGNED")} - Current: {(timerPanel != null ? timerPanel.activeSelf.ToString() : "N/A")}");
        Debug.Log($"Profile GameObject: {(profileGameObject != null ? profileGameObject.name : "NOT ASSIGNED")} - Current: {(profileGameObject != null ? profileGameObject.activeSelf.ToString() : "N/A")}");
    }
    
    void InitializeSubtitleSystem()
    {
        // Initialize subtitle canvas
        if (subtitleCanvas != null)
        {
            // Store initial state
            subtitleCanvasWasActive = subtitleCanvas.activeSelf;
            // Ensure subtitle canvas is disabled initially
            subtitleCanvas.SetActive(false);
            Debug.Log($"Subtitle canvas initialized: {subtitleCanvas.name}, was active: {subtitleCanvasWasActive}");
        }
        else
        {
            // If no separate subtitle canvas, use dialogue canvas
            if (dialogueCanvas != null)
            {
                subtitleCanvas = dialogueCanvas;
                subtitleCanvasWasActive = dialogueCanvas.activeSelf;
                Debug.Log($"Using dialogue canvas as subtitle canvas: {subtitleCanvas.name}");
            }
        }
        
        // Initialize subtitle text - CRITICAL: Must be TextMeshProUGUI
        if (subtitleTextUI != null)
        {
            // Store initial state
            subtitleTextWasActive = subtitleTextUI.gameObject.activeSelf;
            // Ensure subtitle text is disabled initially
            subtitleTextUI.gameObject.SetActive(false);
            // Clear any existing text
            subtitleTextUI.text = "";
            Debug.Log($"Subtitle text initialized: {subtitleTextUI.name}, was active: {subtitleTextWasActive}");
        }
        else
        {
            Debug.LogWarning("No subtitle text assigned! Subtitles won't display.");
            
            // Try to find a TextMeshProUGUI component automatically
            if (subtitleCanvas != null)
            {
                subtitleTextUI = subtitleCanvas.GetComponentInChildren<TextMeshProUGUI>(true);
                if (subtitleTextUI != null)
                {
                    subtitleTextUI.gameObject.SetActive(false);
                    subtitleTextUI.text = "";
                    Debug.Log($"Found subtitle text automatically: {subtitleTextUI.name}");
                }
            }
        }
        
        // Initialize subtitle controller
        if (subtitleController == null)
        {
            // Try to find it automatically
            subtitleController = FindObjectOfType<K2_SubtitleController>();
            
            if (subtitleController == null)
            {
                // If not found, try to find it on the subtitle canvas
                if (subtitleCanvas != null)
                {
                    subtitleController = subtitleCanvas.GetComponentInChildren<K2_SubtitleController>(true);
                }
                
                if (subtitleController == null && subtitleTextUI != null)
                {
                    // Create a subtitle controller on this GameObject
                    subtitleController = gameObject.AddComponent<K2_SubtitleController>();
                    subtitleController.subtitleTextUI = subtitleTextUI;
                    Debug.Log($"Created subtitle controller for text: {subtitleTextUI.name}");
                }
            }
            
            if (subtitleController != null)
            {
                Debug.Log($"Subtitle controller found/created: {subtitleController.name}");
                
                // Ensure subtitle controller has the text reference
                if (subtitleController.subtitleTextUI == null && subtitleTextUI != null)
                {
                    subtitleController.subtitleTextUI = subtitleTextUI;
                    Debug.Log($"Assigned subtitle text to controller: {subtitleTextUI.name}");
                }
            }
            else
            {
                Debug.LogWarning("Subtitle controller not found or created. Timeline subtitles may not work.");
            }
        }
        else
        {
            Debug.Log($"Subtitle controller assigned: {subtitleController.name}");
            
            // Ensure subtitle controller has the text reference
            if (subtitleController.subtitleTextUI == null && subtitleTextUI != null)
            {
                subtitleController.subtitleTextUI = subtitleTextUI;
                Debug.Log($"Assigned subtitle text to controller: {subtitleTextUI.name}");
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
        TriggerCutsceneWithNPCName(null); // Default call without custom name
    }
    
    // Overload method to trigger cutscene with custom NPC name
    public void TriggerCutscene(string customNPCName = null)
    {
        TriggerCutsceneWithNPCName(customNPCName);
    }
    
    private void TriggerCutsceneWithNPCName(string customNPCName = null)
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
        
        // Enable NPC name text
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(true);
            
            // Set custom NPC name if provided
            if (!string.IsNullOrEmpty(customNPCName))
            {
                npcNameText.text = customNPCName;
                Debug.Log($"NPC name text set to: '{customNPCName}'");
            }
            else
            {
                Debug.Log($"NPC name text activated: {npcNameText.name}");
            }
        }
        
        // Clear any existing subtitles before starting
        ClearSubtitles();
        
        // Play the cutscene
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.Play();
        }
        
        // Invoke start event
        onCutsceneStart?.Invoke();
        
        Debug.Log($"NPC Cutscene triggered - Player completely frozen, NPC name: {(npcNameText != null && npcNameText.gameObject.activeSelf ? "SHOWN" : "HIDDEN")}");
        Debug.Log($"Subtitle system: {(subtitleTextUI != null ? "ASSIGNED" : "NOT ASSIGNED")}");
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
                if (script is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
            }
        }
        
        Debug.Log("Player completely frozen - Controller, Animator, Audio, and Inputs disabled");
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
        
        // CRITICAL: Enable subtitle canvas and text
        EnableSubtitleDisplay();
    }
    
    // Enable subtitle display
    private void EnableSubtitleDisplay()
    {
        // Enable subtitle canvas
        if (subtitleCanvas != null)
        {
            subtitleCanvas.SetActive(true);
            Debug.Log($"Subtitle canvas enabled: {subtitleCanvas.name}");
        }
        
        // Enable subtitle text
        if (subtitleTextUI != null)
        {
            subtitleTextUI.gameObject.SetActive(true);
            Debug.Log($"Subtitle text enabled: {subtitleTextUI.name}");
        }
        else
        {
            Debug.LogWarning("Subtitle text UI is null! Subtitles won't display.");
        }
        
        // Ensure subtitle controller has the text reference
        if (subtitleController != null && subtitleController.subtitleTextUI == null && subtitleTextUI != null)
        {
            subtitleController.subtitleTextUI = subtitleTextUI;
            Debug.Log($"Assigned subtitle text to controller: {subtitleTextUI.name}");
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
        
        // Hide NPC name text when cutscene is paused
        if (npcNameText != null && npcNameText.gameObject.activeSelf)
        {
            npcNameText.gameObject.SetActive(false);
        }
        
        // Clear subtitles when paused
        ClearSubtitles();
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
        if (isCutscenePlaying && skipButtonReady)
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
            Debug.Log("NPC cutscene skipped by player");
            
            // FAST FORWARD TO END: Set timeline to the end before stopping
            double currentTime = npcCutsceneDirector.time;
            double duration = npcCutsceneDirector.duration;
            
            if (duration > 0)
            {
                Debug.Log($"Fast-forwarding from {currentTime} to {duration}");
                
                // Fast forward to the end
                npcCutsceneDirector.time = duration;
                
                // Evaluate the timeline at the end time
                npcCutsceneDirector.Evaluate();
                
                // Trigger all bindings that should happen at the end
                TriggerAllBindings(npcCutsceneDirector);
            }
            
            // Stop the director
            npcCutsceneDirector.Stop();
            
            // Finish the cutscene with skipped flag
            FinishCutscene(true); // true = skipped
            
            // Invoke skipped event
            onCutsceneSkipped?.Invoke();
        }
    }

    private void TriggerAllBindings(PlayableDirector director)
    {
        if (director == null) return;
        
        // Get all PlayableBindings
        var bindings = director.playableAsset.outputs;
        
        foreach (var binding in bindings)
        {
            try
            {
                // Get the bound object
                var boundObject = director.GetGenericBinding(binding.sourceObject);
                
                if (boundObject != null)
                {
                    // If it's an animation track, force it to evaluate at the end
                    if (binding.outputTargetType == typeof(Animator))
                    {
                        Animator animator = boundObject as Animator;
                        if (animator != null)
                        {
                            // Ensure the animator is updated
                            animator.Update(0f);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error evaluating binding: {e.Message}");
            }
        }
        
        // Force evaluation of all playables
        director.Evaluate();
        
        Debug.Log("Timeline bindings triggered for skip");
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
        
        // Clear subtitles when cutscene ends
        ClearSubtitles();
        
        // Disable subtitle display
        DisableSubtitleDisplay();
        
        // Disable NPC name text after cutscene
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(false);
            Debug.Log("NPC name text disabled after cutscene");
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
        
        // NEW: Handle UI elements after cutscene
        HandlePostCutsceneUI();
        
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
            Debug.Log("NPC Cutscene skipped - Player unfrozen, NPC name text disabled, subtitles cleared");
        }
        else
        {
            Debug.Log("NPC Cutscene finished - Player unfrozen, NPC name text disabled, subtitles cleared");
        }
    }
    
    // NEW: Handle UI elements after cutscene
    private void HandlePostCutsceneUI()
    {
        Debug.Log("=== HANDLING POST-CUTSCENE UI ===");
        
        // Enable initially disabled UI elements
        if (healthContainer != null)
        {
            healthContainer.SetActive(true);
            Debug.Log($"Health Container enabled: {healthContainer.name}");
        }
        else
        {
            Debug.LogWarning("Health Container not assigned in inspector!");
        }
        
        if (pointsPanel != null)
        {
            pointsPanel.SetActive(true);
            Debug.Log($"Points Panel enabled: {pointsPanel.name}");
        }
        else
        {
            Debug.LogWarning("Points Panel not assigned in inspector!");
        }
        
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
            Debug.Log($"Timer Panel enabled: {timerPanel.name}");
        }
        else
        {
            Debug.LogWarning("Timer Panel not assigned in inspector!");
        }
        
        // Disable Profile GameObject
        if (profileGameObject != null)
        {
            profileGameObject.SetActive(false);
            Debug.Log($"Profile GameObject disabled: {profileGameObject.name}");
        }
        else
        {
            Debug.LogWarning("Profile GameObject not assigned in inspector!");
        }
        
        // Verify changes
        Debug.Log("=== POST-CUTSCENE UI STATE ===");
        if (healthContainer != null) Debug.Log($"Health Container: {(healthContainer.activeSelf ? "ENABLED" : "DISABLED")}");
        if (pointsPanel != null) Debug.Log($"Points Panel: {(pointsPanel.activeSelf ? "ENABLED" : "DISABLED")}");
        if (timerPanel != null) Debug.Log($"Timer Panel: {(timerPanel.activeSelf ? "ENABLED" : "DISABLED")}");
        if (profileGameObject != null) Debug.Log($"Profile GameObject: {(profileGameObject.activeSelf ? "ENABLED" : "DISABLED")}");
    }
    
    // Disable subtitle display
    private void DisableSubtitleDisplay()
    {
        // Disable subtitle text (but keep canvas active if it's shared with dialogue)
        if (subtitleTextUI != null)
        {
            subtitleTextUI.gameObject.SetActive(false);
            Debug.Log($"Subtitle text disabled: {subtitleTextUI.name}");
        }
        
        // Only disable subtitle canvas if it's separate from dialogue canvas
        if (subtitleCanvas != null && subtitleCanvas != dialogueCanvas)
        {
            subtitleCanvas.SetActive(false);
            Debug.Log($"Subtitle canvas disabled: {subtitleCanvas.name}");
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
    
    // Clear subtitles
    private void ClearSubtitles()
    {
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
            Debug.Log("Subtitles cleared via controller");
        }
        else if (subtitleTextUI != null)
        {
            subtitleTextUI.text = "";
            Debug.Log("Subtitle text cleared directly");
        }
    }
    
    // Method to update NPC name text during cutscene
    public void UpdateNPCName(string newName)
    {
        if (npcNameText != null && isCutscenePlaying)
        {
            npcNameText.text = newName;
            Debug.Log($"NPC name updated to: '{newName}'");
        }
        else if (npcNameText == null)
        {
            Debug.LogWarning("Cannot update NPC name - no NPC name text assigned!");
        }
        else if (!isCutscenePlaying)
        {
            Debug.LogWarning("Cannot update NPC name - cutscene is not playing!");
        }
    }
    
    // Method to show/hide NPC name text during cutscene
    public void SetNPCNameActive(bool active)
    {
        if (npcNameText != null && isCutscenePlaying)
        {
            npcNameText.gameObject.SetActive(active);
            Debug.Log($"NPC name text {(active ? "shown" : "hidden")}");
        }
        else if (npcNameText == null)
        {
            Debug.LogWarning("Cannot show/hide NPC name - no NPC name text assigned!");
        }
    }
    
    // Method to assign NPC name text at runtime
    public void SetNPCNameText(TMP_Text newNameText)
    {
        // Disable old name if exists
        if (npcNameText != null && npcNameText.gameObject.activeSelf)
        {
            npcNameText.gameObject.SetActive(false);
        }
        
        npcNameText = newNameText;
        
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(isCutscenePlaying);
            Debug.Log($"NPC name text assigned: {npcNameText.name}");
        }
    }
    
    // Method to get current NPC name
    public string GetCurrentNPCName()
    {
        return npcNameText != null ? npcNameText.text : "";
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
        
        // Hide dialogue canvas on reset
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
        
        // Hide NPC name text on reset
        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(false);
        }
        
        // Clear subtitles on reset
        ClearSubtitles();
        
        // Disable subtitle display on reset
        DisableSubtitleDisplay();
        
        // NEW: Reset UI elements to original states on reset
        if (healthContainer != null)
            healthContainer.SetActive(healthContainerOriginalState);
        
        if (pointsPanel != null)
            pointsPanel.SetActive(pointsPanelOriginalState);
        
        if (timerPanel != null)
            timerPanel.SetActive(timerPanelOriginalState);
        
        if (profileGameObject != null)
            profileGameObject.SetActive(profileGameObjectOriginalState);
        
        // Ensure player is unfrozen on reset
        UnfreezePlayer();
        
        Debug.Log("NPC interaction reset - UI restored to original states");
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
    
    // Check if NPC name text is active
    public bool IsNPCNameActive()
    {
        return npcNameText != null && npcNameText.gameObject.activeSelf;
    }
    
    // Check if skip button is ready/visible
    public bool IsSkipButtonReady()
    {
        return skipButtonReady;
    }
    
    // Check if subtitle text is assigned
    public bool IsSubtitleTextAssigned()
    {
        return subtitleTextUI != null;
    }
    
    // Check if subtitle controller is assigned
    public bool IsSubtitleControllerAssigned()
    {
        return subtitleController != null;
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
    
    // NEW: Individual methods to control UI elements after cutscene
    public void EnableHealthContainer()
    {
        if (healthContainer != null)
        {
            healthContainer.SetActive(true);
            Debug.Log("Health Container manually enabled");
        }
    }
    
    public void EnablePointsPanel()
    {
        if (pointsPanel != null)
        {
            pointsPanel.SetActive(true);
            Debug.Log("Points Panel manually enabled");
        }
    }
    
    public void EnableTimerPanel()
    {
        if (timerPanel != null)
        {
            timerPanel.SetActive(true);
            Debug.Log("Timer Panel manually enabled");
        }
    }
    
    public void DisableProfileGameObject()
    {
        if (profileGameObject != null)
        {
            profileGameObject.SetActive(false);
            Debug.Log("Profile GameObject manually disabled");
        }
    }
    
    // NEW: Method to check current UI state
    public void LogCurrentUIState()
    {
        Debug.Log("=== CURRENT UI STATE ===");
        Debug.Log($"Health Container: {(healthContainer != null ? healthContainer.activeSelf.ToString() : "NOT ASSIGNED")}");
        Debug.Log($"Points Panel: {(pointsPanel != null ? pointsPanel.activeSelf.ToString() : "NOT ASSIGNED")}");
        Debug.Log($"Timer Panel: {(timerPanel != null ? timerPanel.activeSelf.ToString() : "NOT ASSIGNED")}");
        Debug.Log($"Profile GameObject: {(profileGameObject != null ? profileGameObject.activeSelf.ToString() : "NOT ASSIGNED")}");
    }
    
    // Test method to show subtitle manually
    [ContextMenu("Test Show Subtitle")]
    public void TestShowSubtitle()
    {
        if (subtitleController != null)
        {
            // Ensure subtitle text is enabled
            if (subtitleTextUI != null && !subtitleTextUI.gameObject.activeSelf)
            {
                subtitleTextUI.gameObject.SetActive(true);
            }
            
            subtitleController.ShowSubtitle("This is a test subtitle! The text should appear.", 0.03f);
            Debug.Log("Test subtitle shown via controller");
        }
        else if (subtitleTextUI != null)
        {
            // Enable text if not already enabled
            if (!subtitleTextUI.gameObject.activeSelf)
            {
                subtitleTextUI.gameObject.SetActive(true);
            }
            
            subtitleTextUI.text = "Test subtitle - Direct text assignment";
            Debug.Log("Test subtitle shown (direct text assignment)");
        }
        else
        {
            Debug.LogWarning("Cannot show test subtitle - no subtitle text or controller assigned!");
        }
    }
    
    // Context menu for testing
    [ContextMenu("Test Trigger Cutscene")]
    public void TestTriggerCutscene()
    {
        TriggerCutscene();
    }
    
    [ContextMenu("Test Trigger Cutscene with Custom Name")]
    public void TestTriggerCutsceneWithCustomName()
    {
        TriggerCutscene("SIR KALEB");
    }
    
    [ContextMenu("Test Skip Cutscene")]
    public void TestSkipCutscene()
    {
        SkipCutscene();
    }
    
    [ContextMenu("Update NPC Name to 'TEST NPC'")]
    public void TestUpdateNPCName()
    {
        UpdateNPCName("TEST NPC");
    }
    
    [ContextMenu("Toggle NPC Name Visibility")]
    public void TestToggleNPCName()
    {
        SetNPCNameActive(!IsNPCNameActive());
    }
    
    // NEW: Test method for post-cutscene UI
    [ContextMenu("Test Post-Cutscene UI")]
    public void TestPostCutsceneUI()
    {
        Debug.Log("=== TESTING POST-CUTSCENE UI ===");
        HandlePostCutsceneUI();
    }
    
    // NEW: Test method to reset UI to original states
    [ContextMenu("Reset UI to Original States")]
    public void ResetUIToOriginalStates()
    {
        if (healthContainer != null)
            healthContainer.SetActive(healthContainerOriginalState);
        
        if (pointsPanel != null)
            pointsPanel.SetActive(pointsPanelOriginalState);
        
        if (timerPanel != null)
            timerPanel.SetActive(timerPanelOriginalState);
        
        if (profileGameObject != null)
            profileGameObject.SetActive(profileGameObjectOriginalState);
        
        Debug.Log("UI reset to original states");
        LogCurrentUIState();
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== NPC CUTSCENE DEBUG ===");
        Debug.Log($"Is Cutscene Playing: {isCutscenePlaying}");
        Debug.Log($"Has Triggered: {hasTriggered}");
        Debug.Log($"Dialogue Canvas Active: {(dialogueCanvas != null ? dialogueCanvas.activeSelf : false)}");
        Debug.Log($"NPC Name Text Assigned: {npcNameText != null}");
        Debug.Log($"NPC Name Text Active: {IsNPCNameActive()}");
        Debug.Log($"Current NPC Name: {(npcNameText != null ? $"'{npcNameText.text}'" : "N/A")}");
        Debug.Log($"Subtitle Canvas: {(subtitleCanvas != null ? subtitleCanvas.name : "NOT ASSIGNED")}");
        Debug.Log($"Subtitle Text UI: {(subtitleTextUI != null ? $"{subtitleTextUI.name} (Active: {subtitleTextUI.gameObject.activeSelf})" : "NOT ASSIGNED")}");
        Debug.Log($"Subtitle Controller: {(subtitleController != null ? $"{subtitleController.name} (Has Text: {subtitleController.subtitleTextUI != null})" : "NOT ASSIGNED")}");
        Debug.Log($"Skip Button Ready: {skipButtonReady}");
        Debug.Log($"Time Until Skip: {GetSkipButtonTimeRemaining():F1}s");
        Debug.Log($"Timeline Director State: {(npcCutsceneDirector != null ? npcCutsceneDirector.state.ToString() : "NULL")}");
        Debug.Log($"Timeline Time: {(npcCutsceneDirector != null ? $"{npcCutsceneDirector.time:F2}s/{npcCutsceneDirector.duration:F2}s" : "NULL")}");
        Debug.Log($"Cutscene Parent Active: {(cutsceneParentObject != null ? cutsceneParentObject.activeSelf : "NULL")}");
        
        // NEW: UI State Debug
        Debug.Log($"=== UI STATE ===");
        Debug.Log($"Health Container: {(healthContainer != null ? healthContainer.name + " - " + (healthContainer.activeSelf ? "ENABLED" : "DISABLED") : "NOT ASSIGNED")}");
        Debug.Log($"Points Panel: {(pointsPanel != null ? pointsPanel.name + " - " + (pointsPanel.activeSelf ? "ENABLED" : "DISABLED") : "NOT ASSIGNED")}");
        Debug.Log($"Timer Panel: {(timerPanel != null ? timerPanel.name + " - " + (timerPanel.activeSelf ? "ENABLED" : "DISABLED") : "NOT ASSIGNED")}");
        Debug.Log($"Profile GameObject: {(profileGameObject != null ? profileGameObject.name + " - " + (profileGameObject.activeSelf ? "ENABLED" : "DISABLED") : "NOT ASSIGNED")}");
        Debug.Log($"=== END DEBUG ===");
    }
    
    // NEW: Auto-find UI elements in editor
    #if UNITY_EDITOR
    [ContextMenu("Auto-Find UI Elements")]
    public void AutoFindUIElements()
    {
        // Try to find Health Container
        if (healthContainer == null)
        {
            healthContainer = GameObject.Find("Health Container");
            if (healthContainer != null) Debug.Log("Auto-found Health Container");
        }
        
        // Try to find Points Panel
        if (pointsPanel == null)
        {
            pointsPanel = GameObject.Find("Points Panel");
            if (pointsPanel != null) Debug.Log("Auto-found Points Panel");
        }
        
        // Try to find Timer Panel
        if (timerPanel == null)
        {
            timerPanel = GameObject.Find("Timer Panel");
            if (timerPanel != null) Debug.Log("Auto-found Timer Panel");
        }
        
        // Try to find Profile GameObject
        if (profileGameObject == null)
        {
            profileGameObject = GameObject.Find("Profile");
            if (profileGameObject != null) Debug.Log("Auto-found Profile GameObject");
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
}