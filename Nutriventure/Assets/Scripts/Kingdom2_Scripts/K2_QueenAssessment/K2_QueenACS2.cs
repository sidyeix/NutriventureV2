using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using StarterAssets;
using UnityEngine.Events;
using TMPro; // Added for TextMeshPro

public class K2_QueenACS2 : MonoBehaviour
{
    [Header("Cutscene References")]
    [SerializeField] private GameObject cutsceneParentObject; // "CutsceneQueen2" parent object
    [SerializeField] private PlayableDirector npcCutsceneDirector; // "Queen_Timeline2" PlayableDirector
    
    [Header("UI References")]
    [SerializeField] private GameObject dialogueCanvas; // "K2_QueenACV2" dialogue canvas
    [SerializeField] private bool showDialogueDuringCutscene = true; // Toggle for dialogue visibility
    [SerializeField] private TMP_Text queenNameText; // NEW: TextMeshPro for Queen's name in second cutscene
    
    [Header("Skip Button")]
    [SerializeField] private Button skipButton; // Button to skip the cutscene
    [SerializeField] private bool enableSkipButton = true; // Whether skip button is enabled
    [SerializeField] private float skipButtonDelay = 2f; // Delay before skip button appears
    
    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; // Reference to player (with ThirdPersonController)
    
    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler; // "Audio_Handler" GameObject
    [SerializeField] private GameObject gameUICanvas; // "UI_Canvas_StarterAssetsInputs_Joysticks"
    
    [Header("Trigger Settings")]
    [SerializeField] private K2_QA2system qa2System; // Reference to QA2 system to check completion
    [SerializeField] private int requiredCorrectAnswers = 5; // Number of products that must be answered correctly
    
    [Header("Events")]
    public UnityEvent onCutsceneStart;
    public UnityEvent onCutsceneEnd;
    public UnityEvent onCutsceneSkipped; // Event fired when cutscene is skipped
    public UnityEvent onAllProductsAnswered; // Event fired when all products are answered (before cutscene)
    
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
    
    // Track if we have stored player references
    private bool hasPlayerReferences = false;
    
    // Completion check
    private bool isCheckingCompletion = false;
    
    // Queen name text state
    private bool queenNameTextWasActive = false;
    
    void Start()
    {
        InitializeComponents();
        StartCompletionCheck();
    }
    
    void InitializeComponents()
    {
        // Ensure cutscene parent is disabled initially
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(false);
            Debug.Log("Cutscene parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene parent object not assigned!");
        }
        
        // Ensure PlayableDirector is stopped
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.Stop();
            npcCutsceneDirector.stopped += OnCutsceneFinished;
            
            // Subscribe to timeline events - CRITICAL FOR DIALOGUE
            npcCutsceneDirector.played += OnCutscenePlayed;
            npcCutsceneDirector.paused += OnCutscenePaused;
            Debug.Log("Timeline director initialized with event subscriptions");
        }
        else
        {
            Debug.LogError("Timeline director not assigned!");
        }
        
        // Initialize dialogue canvas
        if (dialogueCanvas != null)
        {
            // Hide dialogue canvas initially
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas initialized and hidden");
        }
        else
        {
            Debug.LogWarning("Dialogue canvas not assigned!");
        }
        
        // Initialize queen name text
        if (queenNameText != null)
        {
            // Store whether it was active before initialization
            queenNameTextWasActive = queenNameText.gameObject.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            queenNameText.gameObject.SetActive(false);
            Debug.Log($"Queen name text initialized: {queenNameText.name}, was active: {queenNameTextWasActive}");
        }
        else
        {
            Debug.Log("No queen name text assigned - skipping queen name display");
        }
        
        // Initialize skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            skipButton.gameObject.SetActive(false); // Hidden by default
            Debug.Log("Skip button initialized");
        }
        
        // Ensure game UI canvas is enabled initially (if reference exists)
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }
        
        // Ensure audio handler is enabled initially (if reference exists)
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }
        
        // Try to get player component references if player object is assigned
        if (playerObject != null)
        {
            InitializePlayerComponents();
        }
        else
        {
            Debug.LogWarning("Player object not assigned! Will try to find on trigger.");
        }
        
        // Try to find QA2 system if not assigned
        if (qa2System == null)
        {
            qa2System = FindObjectOfType<K2_QA2system>();
            if (qa2System != null)
            {
                Debug.Log("Found QA2 system automatically");
            }
            else
            {
                Debug.LogWarning("QA2 system not found! Cutscene won't trigger automatically.");
            }
        }
        
        Debug.Log("Queen cutscene 2 manager initialized successfully");
    }
    
    void OnDestroy()
    {
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.stopped -= OnCutsceneFinished;
            npcCutsceneDirector.played -= OnCutscenePlayed;
            npcCutsceneDirector.paused -= OnCutscenePaused;
            Debug.Log("Unsubscribed from timeline events");
        }
        
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
        
        StopCompletionCheck();
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
                Debug.Log("Skip button ready");
            }
        }
    }
    
    void StartCompletionCheck()
    {
        if (qa2System != null && !isCheckingCompletion)
        {
            isCheckingCompletion = true;
            Debug.Log("Started checking for QA2 completion");
            // We'll check periodically in a coroutine
            StartCoroutine(CheckForCompletionRoutine());
        }
    }
    
    void StopCompletionCheck()
    {
        isCheckingCompletion = false;
        Debug.Log("Stopped checking for QA2 completion");
    }
    
    IEnumerator CheckForCompletionRoutine()
    {
        while (isCheckingCompletion && !hasTriggered && !isCutscenePlaying)
        {
            yield return new WaitForSeconds(1f); // Check every second
            
            if (qa2System != null && qa2System.GetCorrectlyAnsweredCount() >= requiredCorrectAnswers)
            {
                Debug.Log($"QA2 completion detected: {qa2System.GetCorrectlyAnsweredCount()}/{requiredCorrectAnswers} products answered correctly");
                OnAllProductsAnswered();
                break;
            }
        }
    }
    
    void OnAllProductsAnswered()
    {
        if (hasTriggered || isCutscenePlaying) return;
        
        Debug.Log("All products answered correctly! Triggering completion event...");
        onAllProductsAnswered?.Invoke();
        
        // Auto-trigger the cutscene after a short delay
        StartCoroutine(DelayedCutsceneTrigger());
    }
    
    IEnumerator DelayedCutsceneTrigger()
    {
        // Give a moment for any other systems to react
        yield return new WaitForSeconds(1f);
        
        TriggerCutscene();
    }
    
    void InitializePlayerComponents()
    {
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<ThirdPersonController>();
            playerAnimator = playerObject.GetComponent<Animator>();
            playerAudioSource = playerObject.GetComponent<AudioSource>();
            playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
            playerInputComponent = playerObject.GetComponent<PlayerInput>();
            
            hasPlayerReferences = playerController != null || playerAnimator != null || 
                                 playerInputs != null || playerInputComponent != null;
            
            Debug.Log($"Player components initialized: {hasPlayerReferences}");
        }
        else
        {
            Debug.LogWarning("Cannot initialize player components: playerObject is null!");
            hasPlayerReferences = false;
        }
    }
    
    public void TriggerCutscene()
    {
        TriggerCutsceneWithQueenName(null); // Default call without custom name
    }
    
    // NEW: Overload method to trigger cutscene with custom queen name
    public void TriggerCutscene(string customQueenName = null)
    {
        TriggerCutsceneWithQueenName(customQueenName);
    }
    
    private void TriggerCutsceneWithQueenName(string customQueenName = null)
    {
        if (hasTriggered || isCutscenePlaying) return;
        
        hasTriggered = true;
        isCutscenePlaying = true;
        skipButtonTimer = 0f;
        skipButtonReady = false;
        
        // Store original player states (if we have player references)
        if (hasPlayerReferences)
        {
            StoreOriginalPlayerStates();
        }
        
        // Completely freeze the player (if we have player references)
        if (hasPlayerReferences)
        {
            FreezePlayerCompletely();
        }
        else
        {
            Debug.LogWarning("No player references found, skipping freeze logic");
        }
        
        // Enable cutscene parent object
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(true);
            Debug.Log("Cutscene parent enabled");
        }
        
        // Disable game UI during cutscene
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }
        
        // Disable audio handler during cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }
        
        // Enable queen name text
        if (queenNameText != null)
        {
            queenNameText.gameObject.SetActive(true);
            
            // Set custom queen name if provided
            if (!string.IsNullOrEmpty(customQueenName))
            {
                queenNameText.text = customQueenName;
                Debug.Log($"Queen name text set to: '{customQueenName}'");
            }
            else
            {
                Debug.Log($"Queen name text activated: {queenNameText.name}");
            }
        }
        
        // Play the cutscene
        if (npcCutsceneDirector != null)
        {
            npcCutsceneDirector.Play();
            Debug.Log("Timeline started playing");
        }
        else
        {
            Debug.LogError("Cannot play cutscene: Director is null!");
        }
        
        // Invoke start event
        onCutsceneStart?.Invoke();
        
        Debug.Log("Queen cutscene 2 triggered - Player completely frozen, Queen name: " + 
                 (queenNameText != null && queenNameText.gameObject.activeSelf ? "SHOWN" : "HIDDEN"));
    }
    
    private void StoreOriginalPlayerStates()
    {
        if (playerController != null)
        {
            wasControllerEnabled = playerController.enabled;
            Debug.Log($"Stored controller state: {wasControllerEnabled}");
        }
        
        if (playerAnimator != null)
        {
            wasAnimatorEnabled = playerAnimator.enabled;
            originalAnimatorSpeed = playerAnimator.speed;
            Debug.Log($"Stored animator state: {wasAnimatorEnabled}");
        }
        
        if (playerAudioSource != null)
        {
            wasAudioSourceEnabled = playerAudioSource.enabled;
            Debug.Log($"Stored audio source state: {wasAudioSourceEnabled}");
        }
        
        if (playerInputs != null)
        {
            originalMoveInput = playerInputs.move;
            originalSprintState = playerInputs.sprint;
            originalJumpState = playerInputs.jump;
            Debug.Log("Stored input states");
        }
        
        // Store PlayerInput state
        if (playerInputComponent != null)
        {
            playerInputWasEnabled = playerInputComponent.enabled;
            Debug.Log($"Stored PlayerInput state: {playerInputWasEnabled}");
        }
    }
    
    private void FreezePlayerCompletely()
    {
        if (playerObject == null) 
        {
            Debug.LogWarning("Cannot freeze player: Player object is null!");
            return;
        }
        
        // 1. Disable the ThirdPersonController
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("ThirdPersonController not found on player!");
        }
        
        // 2. Stop all animations completely
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
            Debug.Log("Player animator disabled");
        }
        
        // 3. Stop all audio
        if (playerAudioSource != null)
        {
            playerAudioSource.enabled = false;
            playerAudioSource.Stop();
            Debug.Log("Player audio source stopped");
            
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
            Debug.Log("Player inputs reset and disabled");
        }
        
        // 5. Stop any physics movement
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            Debug.Log("Player physics stopped");
        }
        
        // 6. Disable CharacterController movement
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("CharacterController disabled");
        }
        
        // 7. Disable PlayerInput component (Input System)
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = false;
            Debug.Log("PlayerInput component disabled");
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
    
    // Event handler when cutscene starts playing - THIS IS CRITICAL FOR DIALOGUE
    private void OnCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Queen cutscene 2 started playing event received");
        
        // Show dialogue canvas when cutscene starts
        if (showDialogueDuringCutscene && dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas ACTIVATED - This should make it visible");
        }
        else if (!showDialogueDuringCutscene)
        {
            Debug.Log("Dialogue canvas disabled by setting (showDialogueDuringCutscene = false)");
        }
        else
        {
            Debug.LogWarning("Dialogue canvas is null!");
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
            Debug.Log("Dialogue canvas deactivated on pause");
        }
        
        // Hide queen name text when cutscene is paused
        if (queenNameText != null && queenNameText.gameObject.activeSelf)
        {
            queenNameText.gameObject.SetActive(false);
        }
    }
    
    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Check if this was triggered by skip (to avoid double-finishing)
        if (isCutscenePlaying)
        {
            Debug.Log("Queen cutscene 2 finished playing");
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
            Debug.Log("Skip button hidden");
        }
    }
    
    // Public method to skip cutscene
    public void SkipCutscene()
    {
        if (isCutscenePlaying && npcCutsceneDirector != null)
        {
            Debug.Log("Queen cutscene 2 skipped by player");
            
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
    
    // Trigger all timeline bindings to ensure end-of-timeline events fire
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
        
        // Disable queen name text after cutscene
        if (queenNameText != null)
        {
            queenNameText.gameObject.SetActive(false);
            Debug.Log("Queen name text disabled after cutscene");
        }
        
        // Disable cutscene parent object
        if (cutsceneParentObject != null)
        {
            cutsceneParentObject.SetActive(false);
            Debug.Log("Cutscene parent disabled");
        }
        
        // Re-enable game UI after cutscene
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }
        
        // Re-enable audio handler after cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }
        
        // Unfreeze the player (if we have player references)
        if (hasPlayerReferences)
        {
            UnfreezePlayer();
        }
        else
        {
            Debug.LogWarning("No player references found, skipping unfreeze logic");
        }
        
        // Invoke end event
        onCutsceneEnd?.Invoke();
        
        if (wasSkipped)
        {
            Debug.Log("Queen cutscene 2 skipped - Player unfrozen");
        }
        else
        {
            Debug.Log("Queen cutscene 2 finished - Player unfrozen");
        }
    }
    
    // NEW: Method to update queen name during cutscene
    public void UpdateQueenName(string newName)
    {
        if (queenNameText != null && isCutscenePlaying)
        {
            queenNameText.text = newName;
            Debug.Log($"Queen name updated to: '{newName}'");
        }
        else if (queenNameText == null)
        {
            Debug.LogWarning("Cannot update queen name - no queen name text assigned!");
        }
        else if (!isCutscenePlaying)
        {
            Debug.LogWarning("Cannot update queen name - cutscene is not playing!");
        }
    }
    
    // NEW: Method to show/hide queen name during cutscene
    public void SetQueenNameActive(bool active)
    {
        if (queenNameText != null && isCutscenePlaying)
        {
            queenNameText.gameObject.SetActive(active);
            Debug.Log($"Queen name text {(active ? "shown" : "hidden")}");
        }
        else if (queenNameText == null)
        {
            Debug.LogWarning("Cannot show/hide queen name - no queen name text assigned!");
        }
    }
    
    // NEW: Method to assign queen name text at runtime
    public void SetQueenNameText(TMP_Text newQueenText)
    {
        // Disable old queen name if exists
        if (queenNameText != null && queenNameText.gameObject.activeSelf)
        {
            queenNameText.gameObject.SetActive(false);
        }
        
        queenNameText = newQueenText;
        
        if (queenNameText != null)
        {
            queenNameText.gameObject.SetActive(isCutscenePlaying);
            Debug.Log($"Queen name text assigned: {queenNameText.name}");
        }
    }
    
    // NEW: Method to get current queen name
    public string GetCurrentQueenName()
    {
        return queenNameText != null ? queenNameText.text : "";
    }
    
    // NEW: Check if queen name text is active
    public bool IsQueenNameActive()
    {
        return queenNameText != null && queenNameText.gameObject.activeSelf;
    }
    
    private void UnfreezePlayer()
    {
        if (playerObject == null) 
        {
            Debug.LogWarning("Cannot unfreeze player: Player object is null!");
            return;
        }
        
        // 1. Re-enable the ThirdPersonController (if it was enabled before)
        if (playerController != null)
        {
            playerController.enabled = wasControllerEnabled;
            Debug.Log($"Player controller re-enabled: {wasControllerEnabled}");
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
                Debug.Log("Player animator re-enabled and reset");
            }
        }
        
        // 3. Re-enable audio
        if (playerAudioSource != null)
        {
            playerAudioSource.enabled = wasAudioSourceEnabled;
            Debug.Log($"Player audio re-enabled: {wasAudioSourceEnabled}");
            
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
            Debug.Log("Player inputs re-enabled");
        }
        
        // 5. Re-enable physics
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("Player physics re-enabled");
        }
        
        // 6. Re-enable CharacterController
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log("CharacterController re-enabled");
        }
        
        // 7. Re-enable PlayerInput component (Input System)
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = playerInputWasEnabled;
            Debug.Log($"PlayerInput component re-enabled: {playerInputWasEnabled}");
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
    
    // Public method to manually trigger cutscene from other scripts
    public void ManualTriggerCutscene()
    {
        if (!hasTriggered && !isCutscenePlaying)
        {
            TriggerCutscene();
        }
    }
    
    // Check if all products are answered correctly
    public bool AreAllProductsAnswered()
    {
        if (qa2System == null)
        {
            Debug.LogWarning("QA2 System not assigned!");
            return false;
        }
        
        return qa2System.GetCorrectlyAnsweredCount() >= requiredCorrectAnswers;
    }
    
    // Get the current answer count
    public int GetCurrentCorrectAnswers()
    {
        if (qa2System == null) return 0;
        return qa2System.GetCorrectlyAnsweredCount();
    }
    
    // Get required answers
    public int GetRequiredCorrectAnswers()
    {
        return requiredCorrectAnswers;
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
    
    // Set required correct answers
    public void SetRequiredCorrectAnswers(int count)
    {
        requiredCorrectAnswers = Mathf.Max(1, count);
        Debug.Log($"Required correct answers set to: {requiredCorrectAnswers}");
    }
    
    // Set QA2 system reference
    public void SetQA2System(K2_QA2system system)
    {
        qa2System = system;
        if (qa2System != null)
        {
            Debug.Log($"QA2 System set: {qa2System.gameObject.name}");
        }
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
    
    // Check if cutscene is currently playing
    public bool IsCutscenePlaying()
    {
        return isCutscenePlaying;
    }
    
    // Check if cutscene has been triggered
    public bool HasCutsceneTriggered()
    {
        return hasTriggered;
    }
    
    // Check if we have player references
    public bool HasPlayerReferences()
    {
        return hasPlayerReferences;
    }
    
    // Try to find player if not already set
    public void FindPlayer()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                InitializePlayerComponents();
                Debug.Log("Found player: " + playerObject.name);
            }
            else
            {
                Debug.LogWarning("Could not find player object!");
            }
        }
    }
    
    // Try to find QA2 system if not set
    public void FindQA2System()
    {
        if (qa2System == null)
        {
            qa2System = FindObjectOfType<K2_QA2system>();
            if (qa2System != null)
            {
                Debug.Log("Found QA2 system: " + qa2System.gameObject.name);
            }
            else
            {
                Debug.LogWarning("Could not find QA2 system!");
            }
        }
    }
    
    // Start checking for completion
    public void StartCheckingForCompletion()
    {
        if (!isCheckingCompletion)
        {
            StartCompletionCheck();
        }
    }
    
    // Stop checking for completion
    public void StopCheckingForCompletion()
    {
        StopCompletionCheck();
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        // Find player
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            playerObject = foundPlayer;
            Debug.Log("Auto-found player: " + playerObject.name);
            InitializePlayerComponents();
        }
        
        // Find audio handler
        if (audioHandler == null)
        {
            audioHandler = GameObject.Find("Audio_Handler");
            if (audioHandler != null)
            {
                Debug.Log("Auto-found Audio Handler");
            }
        }
        
        // Find game UI canvas
        if (gameUICanvas == null)
        {
            gameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
            if (gameUICanvas != null)
            {
                Debug.Log("Auto-found Game UI Canvas");
            }
        }
        
        // Try to find dialogue canvas
        if (dialogueCanvas == null)
        {
            GameObject foundDialogueCanvas = GameObject.Find("K2_QueenACV2");
            if (foundDialogueCanvas != null)
            {
                dialogueCanvas = foundDialogueCanvas;
                Debug.Log("Auto-found Dialogue Canvas");
            }
        }
        
        // Try to find QA2 system
        if (qa2System == null)
        {
            qa2System = FindObjectOfType<K2_QA2system>();
            if (qa2System != null)
            {
                Debug.Log("Auto-found QA2 System");
            }
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
    
    // Debug methods
    [ContextMenu("Test Trigger Cutscene")]
    public void TestTriggerCutscene()
    {
        if (!hasTriggered)
        {
            Debug.Log("Testing trigger cutscene...");
            TriggerCutscene();
        }
        else
        {
            Debug.Log("Cutscene already triggered!");
        }
    }
    
    [ContextMenu("Test Trigger Cutscene with Custom Name")]
    public void TestTriggerCutsceneWithCustomName()
    {
        if (!hasTriggered)
        {
            Debug.Log("Testing trigger cutscene with custom name...");
            TriggerCutscene("QUEEN SUGARIA II");
        }
        else
        {
            Debug.Log("Cutscene already triggered!");
        }
    }
    
    [ContextMenu("Test Skip Cutscene")]
    public void TestSkipCutscene()
    {
        if (isCutscenePlaying)
        {
            Debug.Log("Testing skip cutscene...");
            SkipCutscene();
        }
        else
        {
            Debug.Log("No cutscene is currently playing!");
        }
    }
    
    [ContextMenu("Test Update Queen Name")]
    public void TestUpdateQueenName()
    {
        if (isCutscenePlaying)
        {
            UpdateQueenName("SUGAR QUEEN");
        }
        else
        {
            Debug.Log("Cannot update queen name - no cutscene playing!");
        }
    }
    
    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log("=== K2_QueenACS2 Status ===");
        Debug.Log($"Cutscene Triggered: {hasTriggered}");
        Debug.Log($"Cutscene Playing: {isCutscenePlaying}");
        Debug.Log($"QA2 System: {(qa2System != null ? qa2System.gameObject.name : "Not Assigned")}");
        Debug.Log($"Correct Answers: {GetCurrentCorrectAnswers()}/{requiredCorrectAnswers}");
        Debug.Log($"All Products Answered: {AreAllProductsAnswered()}");
        Debug.Log($"Player References: {hasPlayerReferences}");
        Debug.Log($"Skip Button Ready: {skipButtonReady}");
        Debug.Log($"Queen Name Text: {(queenNameText != null ? queenNameText.name : "Not Assigned")}");
        Debug.Log($"Queen Name Active: {IsQueenNameActive()}");
        Debug.Log($"Current Queen Name: {(queenNameText != null ? $"'{queenNameText.text}'" : "N/A")}");
    }
    
    [ContextMenu("Force Check Completion")]
    public void ForceCheckCompletion()
    {
        if (qa2System != null)
        {
            int correctCount = qa2System.GetCorrectlyAnsweredCount();
            Debug.Log($"Force checking: {correctCount}/{requiredCorrectAnswers} correct answers");
            
            if (correctCount >= requiredCorrectAnswers && !hasTriggered && !isCutscenePlaying)
            {
                Debug.Log("Forcing cutscene trigger...");
                OnAllProductsAnswered();
            }
        }
        else
        {
            Debug.LogWarning("QA2 System not assigned!");
        }
    }
}