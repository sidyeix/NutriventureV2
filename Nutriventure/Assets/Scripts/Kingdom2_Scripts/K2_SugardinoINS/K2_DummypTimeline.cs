using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class K2_DummypTimeline : MonoBehaviour
{
    [Header("Dummy Product Collection Detection")]
    [SerializeField] private CollectProducts collectProductsScript; // Reference to CollectProducts script
    [SerializeField] private ProductInformationManager productInfoManager; // Reference to track collection
    
    [Header("Second Timeline References")]
    [SerializeField] private GameObject cutscene2ParentObject; // "Cutscene2Things" parent object
    [SerializeField] private PlayableDirector npcCutscene2Director; // "NPC_Cutscene2" PlayableDirector
    
    [Header("Third Timeline References")]
    [SerializeField] private GameObject cutscene3ParentObject; // "Cutscene3" parent object
    [SerializeField] private PlayableDirector npcTimeline3Director; // "NPC_Timeline3" PlayableDirector
    
    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject; // Reference to player (with ThirdPersonController)
    
    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler; // "Audio_Handler" GameObject
    [SerializeField] private GameObject gameUICanvas; // "UI_Canvas_StarterAssetsInputs_Joysticks"
    
    [Header("Dialogue Canvas")]
    [SerializeField] private GameObject dialogueCanvas; // Dialogue box canvas for second timeline
    [SerializeField] private GameObject dialogueCanvas3; // Dialogue box canvas for third timeline
    
    [Header("Skip Button Settings")]
    [SerializeField] private Button skipButton; // Skip button for cutscenes
    [SerializeField] private bool enableSkipButton = true; // Whether skip button is enabled
    [SerializeField] private float skipButtonDelay = 2f; // Delay before skip button appears
    
    [Header("Events")]
    public UnityEvent onSecondCutsceneStart;
    public UnityEvent onSecondCutsceneEnd;
    public UnityEvent onThirdCutsceneStart;
    public UnityEvent onThirdCutsceneEnd;
    public UnityEvent onCutsceneSkipped; // Event fired when cutscene is skipped
    
    // Simple state tracking
    private bool isSecondCutscenePlaying = false;
    private bool isThirdCutscenePlaying = false;
    private bool waitingForFinalPanelConfirm = false;
    
    // Monster tracking
    private List<MonsterObstacle> allMonsters = new List<MonsterObstacle>();
    private List<bool> monsterPauseStates = new List<bool>(); // Track which monsters were already paused
    
    // Skip button variables
    private float skipButtonTimer = 0f;
    private bool skipButtonReady = false;
    
    void Start()
    {
        Debug.Log("K2_DummypTimeline Start called");
        
        // Initialize everything in a safe way
        SafeInitialize();
    }
    
    void Update()
    {
        // Debug: Check if we should be triggering
        if (productInfoManager != null && productInfoManager.IsAllCollected() && !isSecondCutscenePlaying && !isThirdCutscenePlaying && !waitingForFinalPanelConfirm)
        {
            Debug.Log("DEBUG: All products collected but not waiting for panel. Checking conditions...");
        }

            // ENFORCE: Gameplay UI & Audio must stay OFF during any cutscene
        if (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            if (gameUICanvas != null && gameUICanvas.activeSelf)
                gameUICanvas.SetActive(false);

            if (audioHandler != null && audioHandler.activeSelf)
                audioHandler.SetActive(false);
        }
        
        // Handle skip button timer
        if ((isSecondCutscenePlaying || isThirdCutscenePlaying) && enableSkipButton && !skipButtonReady)
        {
            skipButtonTimer += Time.unscaledDeltaTime; // Use unscaled time since game might be paused
            
            if (skipButtonTimer >= skipButtonDelay)
            {
                skipButtonReady = true;
                ShowSkipButton();
            }
        }
    }
    
    void SafeInitialize()
    {
        // Find all monsters in the scene
        FindAllMonsters();
        
        // Disable cutscene parent at start
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene2 parent object not assigned!");
        }
        
        // Disable third cutscene parent at start
        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
            Debug.Log("Cutscene3 parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene3 parent object not assigned!");
        }
        
        // Initialize PlayableDirector without touching it much
        if (npcCutscene2Director != null)
        {
            // Don't call Stop() or subscribe to events in Start
            // Just ensure it's not playing
            if (npcCutscene2Director.state == PlayState.Playing)
            {
                Debug.LogWarning("Timeline was already playing, stopping it");
                npcCutscene2Director.Stop();
            }
        }
        else
        {
            Debug.LogError("PlayableDirector for cutscene2 not assigned!");
        }
        
        // Initialize third timeline director
        if (npcTimeline3Director != null)
        {
            // Don't call Stop() or subscribe to events in Start
            // Just ensure it's not playing
            if (npcTimeline3Director.state == PlayState.Playing)
            {
                Debug.LogWarning("Timeline3 was already playing, stopping it");
                npcTimeline3Director.Stop();
            }
        }
        else
        {
            Debug.LogError("PlayableDirector for cutscene3 (NPC_Timeline3) not assigned!");
        }
        
        // Initialize skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            skipButton.gameObject.SetActive(false); // Hidden by default
            Debug.Log("Skip button initialized");
        }
        else
        {
            Debug.LogWarning("Skip button not assigned in Inspector!");
        }
        
        // Disable dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled");
        }
        
        // Disable third dialogue canvas
        if (dialogueCanvas3 != null)
        {
            dialogueCanvas3.SetActive(false);
            Debug.Log("Dialogue canvas3 disabled");
        }
        
        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
            }
        }
        
        // Find CollectProducts if not assigned
        if (collectProductsScript == null)
        {
            collectProductsScript = FindObjectOfType<CollectProducts>();
            if (collectProductsScript != null)
            {
                Debug.Log("Found CollectProducts script");
            }
        }
        
        // Find ProductInformationManager if not assigned
        if (productInfoManager == null)
        {
            productInfoManager = FindObjectOfType<ProductInformationManager>();
            if (productInfoManager != null)
            {
                Debug.Log("Found ProductInformationManager script");
                // Subscribe to panel events
                ProductInformationManager.OnProductPanelHidden += OnProductPanelHidden;
                Debug.Log("Subscribed to ProductInformationManager.OnProductPanelHidden event");
            }
            else
            {
                Debug.LogError("ProductInformationManager not found in scene!");
            }
        }
        else
        {
            // Subscribe to panel events
            ProductInformationManager.OnProductPanelHidden += OnProductPanelHidden;
            Debug.Log("Subscribed to ProductInformationManager.OnProductPanelHidden event");
        }
        
        // Find Audio Handler if not assigned
        if (audioHandler == null)
        {
            audioHandler = GameObject.Find("Audio_Handler");
            if (audioHandler != null)
            {
                Debug.Log("Found Audio Handler");
            }
        }
        
        // Find Game UI Canvas if not assigned
        if (gameUICanvas == null)
        {
            gameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
            if (gameUICanvas != null)
            {
                Debug.Log("Found Game UI Canvas");
            }
        }
        
        Debug.Log("K2_DummypTimeline initialized successfully");
    }
    
    void OnEnable()
    {
        Debug.Log("K2_DummypTimeline enabled");
        
        // Subscribe to timeline events when script is enabled
        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped += OnSecondCutsceneFinished;
            npcCutscene2Director.played += OnSecondCutscenePlayed;
            Debug.Log("Subscribed to timeline2 events");
        }
        
        // Subscribe to third timeline events
        if (npcTimeline3Director != null)
        {
            npcTimeline3Director.stopped += OnThirdCutsceneFinished;
            npcTimeline3Director.played += OnThirdCutscenePlayed;
            Debug.Log("Subscribed to timeline3 events");
        }
    }
    
    void OnDisable()
    {
        Debug.Log("K2_DummypTimeline disabled");
        
        // Unsubscribe from timeline events
        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped -= OnSecondCutsceneFinished;
            npcCutscene2Director.played -= OnSecondCutscenePlayed;
            Debug.Log("Unsubscribed from timeline2 events");
        }
        
        // Unsubscribe from third timeline events
        if (npcTimeline3Director != null)
        {
            npcTimeline3Director.stopped -= OnThirdCutsceneFinished;
            npcTimeline3Director.played -= OnThirdCutscenePlayed;
            Debug.Log("Unsubscribed from timeline3 events");
        }
        
        // Unsubscribe from panel events
        ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
        Debug.Log("Unsubscribed from ProductInformationManager.OnProductPanelHidden event");
        
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }
    
    void OnDestroy()
    {
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
    }
    
    // Find all monsters in the scene
    private void FindAllMonsters()
    {
        MonsterObstacle[] foundMonsters = FindObjectsOfType<MonsterObstacle>();
        allMonsters.Clear();
        monsterPauseStates.Clear();
        
        foreach (MonsterObstacle monster in foundMonsters)
        {
            allMonsters.Add(monster);
            monsterPauseStates.Add(monster.IsPaused()); // Store current pause state
            Debug.Log($"Found monster: {monster.name}, Current Pause State: {monster.IsPaused()}");
        }
        
        Debug.Log($"Found {allMonsters.Count} monsters in scene");
    }
    
    // Pause all monsters during cutscene
    private void PauseAllMonsters()
    {
        Debug.Log("Pausing all monsters for cutscene...");
        
        // Clear and rebuild lists
        monsterPauseStates.Clear();
        
        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i] != null)
            {
                // Store current pause state before pausing
                monsterPauseStates.Add(allMonsters[i].IsPaused());
                
                // Pause the monster
                allMonsters[i].PauseMonster();
                Debug.Log($"Paused monster: {allMonsters[i].name}");
            }
            else
            {
                monsterPauseStates.Add(false); // Default if monster is null
            }
        }
        
        Debug.Log($"Paused {allMonsters.Count} monsters");
    }
    
    // Resume all monsters after cutscene
    private void ResumeAllMonsters()
    {
        Debug.Log("Resuming monsters after cutscene...");
        
        int resumedCount = 0;
        
        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i] != null)
            {
                // Only resume if it wasn't already paused before the cutscene
                if (i < monsterPauseStates.Count && !monsterPauseStates[i])
                {
                    allMonsters[i].ResumeMonster();
                    resumedCount++;
                    Debug.Log($"Resumed monster: {allMonsters[i].name}");
                }
                else if (i >= monsterPauseStates.Count)
                {
                    // If we don't have a stored state, resume anyway
                    allMonsters[i].ResumeMonster();
                    resumedCount++;
                    Debug.Log($"Resumed monster (no stored state): {allMonsters[i].name}");
                }
                else
                {
                    Debug.Log($"Monster {allMonsters[i].name} was already paused before cutscene, leaving paused");
                }
            }
        }
        
        Debug.Log($"Resumed {resumedCount} monsters");
    }
    
    // Force all monsters to return to patrol
    private void ForceAllMonstersToPatrol()
    {
        Debug.Log("Forcing all monsters to return to patrol...");
        
        int forcedCount = 0;
        
        foreach (MonsterObstacle monster in allMonsters)
        {
            if (monster != null)
            {
                monster.ForceReturnToPatrol();
                forcedCount++;
                Debug.Log($"Forced monster to patrol: {monster.name}");
            }
        }
        
        Debug.Log($"Forced {forcedCount} monsters to return to patrol");
    }
    
    // Event handler for product panel hidden
    private void OnProductPanelHidden()
    {
        Debug.Log("=== PRODUCT PANEL HIDDEN EVENT RECEIVED ===");
        
        // Check if we were waiting for the final panel to close
        if (waitingForFinalPanelConfirm)
        {
            Debug.Log("Was waiting for final panel confirm, checking collection...");
            
            // Check if all products are collected
            if (productInfoManager != null)
            {
                bool allCollected = productInfoManager.IsAllCollected();
                Debug.Log($"All products collected? {allCollected}");
                
                if (allCollected)
                {
                    Debug.Log("=== ALL PRODUCTS COLLECTED - STARTING THIRD CUTSCENE ===");
                    waitingForFinalPanelConfirm = false;
                    StartThirdCutscene();
                }
                else
                {
                    Debug.Log($"Not all products collected yet. Current: {productInfoManager.GetCollectedCount()}");
                    waitingForFinalPanelConfirm = false;
                }
            }
            else
            {
                Debug.LogError("ProductInfoManager is null!");
                waitingForFinalPanelConfirm = false;
            }
        }
        else
        {
            Debug.Log("Not waiting for final panel confirm (normal panel close)");
        }
    }
    
    // Public method to start the second cutscene
    // Call this from ProductInformationManager when dummy product info panel is confirmed
    public void StartSecondCutscene()
    {
        Debug.Log("=== STARTING SECOND CUTSCENE ===");
        
        if (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            Debug.LogWarning("A cutscene is already playing!");
            return;
        }
        
        // Validate everything before starting
        if (!ValidateComponentsForSecondCutscene())
        {
            Debug.LogError("Failed to validate components for second cutscene!");
            return;
        }
        
        isSecondCutscenePlaying = true;
        
        // Reset skip button state
        ResetSkipButtonState();
        
        // Pause all monsters BEFORE freezing player
        PauseAllMonsters();
        
        // Force all monsters to return to patrol (so they're not hunting when cutscene ends)
        ForceAllMonstersToPatrol();
        
        // Freeze player movement
        FreezePlayer();
        
        // Enable cutscene parent first
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(true);
            Debug.Log("Cutscene2 parent enabled");
        }
        
        // Disable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }
        
        // Disable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }
        
        // IMPORTANT: Wait one frame before playing timeline
        // This ensures everything is properly activated
        StartCoroutine(PlayTimelineAfterFrame(npcCutscene2Director, true));
    }
    
    // Public method to start the third cutscene
    public void StartThirdCutscene()
    {
        Debug.Log("=== STARTING THIRD CUTSCENE ===");
        
        if (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            Debug.LogWarning("A cutscene is already playing!");
            return;
        }
        
        // Validate everything before starting
        if (!ValidateComponentsForThirdCutscene())
        {
            Debug.LogError("Failed to validate components for third cutscene!");
            return;
        }
        
        isThirdCutscenePlaying = true;
        
        // Reset skip button state
        ResetSkipButtonState();
        
        // Pause all monsters BEFORE freezing player
        PauseAllMonsters();
        
        // Force all monsters to return to patrol (so they're not hunting when cutscene ends)
        ForceAllMonstersToPatrol();
        
        // Freeze player movement
        FreezePlayer();
        
        // Enable cutscene parent first
        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(true);
            Debug.Log("Cutscene3 parent enabled");
        }
        
        // Disable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }
        
        // Disable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }
        
        // IMPORTANT: Wait one frame before playing timeline
        // This ensures everything is properly activated
        StartCoroutine(PlayTimelineAfterFrame(npcTimeline3Director, false));
    }
    
    private IEnumerator PlayTimelineAfterFrame(PlayableDirector director, bool isSecondCutscene)
    {
        // Wait for end of frame to ensure everything is set up
        yield return new WaitForEndOfFrame();
        
        // Additional small delay to ensure all components are ready
        yield return new WaitForSeconds(0.1f);
        
        // Now play the timeline
        if (director != null)
        {
            Debug.Log($"Playing timeline: {director.name}...");
            director.Play();
        }
        else
        {
            Debug.LogError("Cannot play timeline: Director is null!");
        }
        
        // Invoke appropriate start event
        if (director == npcCutscene2Director)
        {
            onSecondCutsceneStart?.Invoke();
            Debug.Log("Second cutscene started successfully");
        }
        else if (director == npcTimeline3Director)
        {
            onThirdCutsceneStart?.Invoke();
            Debug.Log("Third cutscene started successfully");
        }
    }
    
    bool ValidateComponentsForSecondCutscene()
    {
        bool allValid = true;
        
        // Check player
        if (playerObject == null)
        {
            Debug.LogError("Player object is null!");
            allValid = false;
        }
        
        // Check CollectProducts script
        if (collectProductsScript == null)
        {
            Debug.LogError("CollectProducts script is null!");
            allValid = false;
        }
        else if (!collectProductsScript.HasCollectedDummyProduct())
        {
            Debug.LogWarning("Dummy product not collected yet!");
            allValid = false;
        }
        
        // Check timeline director
        if (npcCutscene2Director == null)
        {
            Debug.LogError("Second timeline director is null!");
            allValid = false;
        }
        
        // Check cutscene parent
        if (cutscene2ParentObject == null)
        {
            Debug.LogError("Cutscene2 parent object is null!");
            allValid = false;
        }
        
        Debug.Log($"Second cutscene validation: {(allValid ? "PASSED" : "FAILED")}");
        return allValid;
    }
    
    bool ValidateComponentsForThirdCutscene()
    {
        bool allValid = true;
        
        // Check player
        if (playerObject == null)
        {
            Debug.LogError("Player object is null!");
            allValid = false;
        }
        
        // Check product info manager
        if (productInfoManager == null)
        {
            Debug.LogError("ProductInformationManager script is null!");
            allValid = false;
        }
        else if (!productInfoManager.IsAllCollected())
        {
            Debug.LogWarning("Not all products collected yet!");
            allValid = false;
        }
        
        // Check timeline director
        if (npcTimeline3Director == null)
        {
            Debug.LogError("Third timeline director is null!");
            allValid = false;
        }
        
        // Check cutscene parent
        if (cutscene3ParentObject != null)
        {
            Debug.Log("Cutscene3 parent object found");
        }
        else
        {
            Debug.LogError("Cutscene3 parent object is null!");
            allValid = false;
        }
        
        Debug.Log($"Third cutscene validation: {(allValid ? "PASSED" : "FAILED")}");
        return allValid;
    }
    
    void FreezePlayer()
    {
        if (playerObject == null) 
        {
            Debug.LogError("Cannot freeze player: Player object is null!");
            return;
        }
        
        // Disable ThirdPersonController
        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("ThirdPersonController not found on player!");
        }
        
        // Disable Animator
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("Player animator disabled");
        }
        else
        {
            Debug.LogWarning("Animator not found on player!");
        }
        
        // Reset inputs
        StarterAssetsInputs inputs = playerObject.GetComponent<StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.move = Vector2.zero;
            inputs.look = Vector2.zero;
            inputs.sprint = false;
            inputs.jump = false;
            Debug.Log("Player inputs reset");
        }
        else
        {
            Debug.LogWarning("StarterAssetsInputs not found on player!");
        }
        
        // Disable PlayerInput (Input System)
        PlayerInput playerInput = playerObject.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log("Player input system disabled");
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found on player!");
        }
        
        // Stop audio
        AudioSource audioSource = playerObject.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("Player audio stopped");
        }
        
        // Stop physics movement
        Rigidbody rb = playerObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("Player physics stopped");
        }
        
        Debug.Log("Player frozen successfully");
    }
    
    void OnSecondCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Second timeline started playing");

        // FORCE UI & AUDIO OFF (in case something re-enabled them)
        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        if (audioHandler != null) audioHandler.SetActive(false);

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled");
        }
    }
    
    void OnThirdCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Third timeline started playing");

        // FORCE UI & AUDIO OFF (THIS FIXES THE MID-CUTSCENE POP)
        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        if (audioHandler != null) audioHandler.SetActive(false);

        if (dialogueCanvas3 != null)
        {
            dialogueCanvas3.SetActive(true);
            Debug.Log("Dialogue canvas3 enabled");
        }
    }
    
    void OnSecondCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Second timeline finished playing");
        
        if (isSecondCutscenePlaying)
        {
            FinishSecondCutscene(false); // false = not skipped
        }
    }
    
    void OnThirdCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Third timeline finished playing");
        
        if (isThirdCutscenePlaying)
        {
            FinishThirdCutscene(false); // false = not skipped
        }
    }
    
    void FinishSecondCutscene(bool wasSkipped = false)
    {
        Debug.Log($"=== FINISHING SECOND CUTSCENE (Skipped: {wasSkipped}) ===");
        
        // Hide skip button
        HideSkipButton();
        
        // Hide dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled");
        }
        
        // Resume monsters BEFORE unfreezing player
        ResumeAllMonsters();
        
        // Disable cutscene parent
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }
        
        // Re-enable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }
        
        // Re-enable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }
        
        // Unfreeze player (AFTER monsters are resumed)
        UnfreezePlayer();
        
        // Update state
        isSecondCutscenePlaying = false;
        
        // If skipped, invoke skipped event
        if (wasSkipped)
        {
            onCutsceneSkipped?.Invoke();
        }
        
        // Invoke end event
        onSecondCutsceneEnd?.Invoke();
        
        Debug.Log($"Second cutscene {(wasSkipped ? "skipped" : "finished")} successfully");
    }
    
    void FinishThirdCutscene(bool wasSkipped = false)
    {
        Debug.Log($"=== FINISHING THIRD CUTSCENE (Skipped: {wasSkipped}) ===");
        
        // Hide skip button
        HideSkipButton();
        
        // Hide dialogue canvas
        if (dialogueCanvas3 != null)
        {
            dialogueCanvas3.SetActive(false);
            Debug.Log("Dialogue canvas3 disabled");
        }
        
        // Resume monsters BEFORE unfreezing player
        ResumeAllMonsters();
        
        // Disable cutscene parent
        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
            Debug.Log("Cutscene3 parent disabled");
        }
        
        // Re-enable game UI
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }
        
        // Re-enable audio handler
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }
        
        // Unfreeze player (AFTER monsters are resumed)
        UnfreezePlayer();
        
        // Update state
        isThirdCutscenePlaying = false;
        
        // If skipped, invoke skipped event
        if (wasSkipped)
        {
            onCutsceneSkipped?.Invoke();
        }
        
        // Invoke end event
        onThirdCutsceneEnd?.Invoke();
        
        Debug.Log($"Third cutscene {(wasSkipped ? "skipped" : "finished")} successfully");
    }
    
    void UnfreezePlayer()
    {
        if (playerObject == null) 
        {
            Debug.LogError("Cannot unfreeze player: Player object is null!");
            return;
        }
        
        // Re-enable ThirdPersonController
        ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = true;
            Debug.Log("Player controller enabled");
        }
        
        // Re-enable Animator
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            
            // Reset animation states
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("MotionSpeed", 0f);
            Debug.Log("Player animator enabled and reset");
        }
        
        // Re-enable PlayerInput
        PlayerInput playerInput = playerObject.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
            Debug.Log("Player input system enabled");
        }
        
        Debug.Log("Player unfrozen successfully");
    }
    
    // Skip button click handler
    private void OnSkipButtonClicked()
    {
        if (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            SkipCurrentCutscene();
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
    
    // Reset skip button state
    private void ResetSkipButtonState()
    {
        skipButtonTimer = 0f;
        skipButtonReady = false;
        
        // Hide skip button
        HideSkipButton();
    }
    
    // Skip the current cutscene
    public void SkipCurrentCutscene()
    {
        if (isSecondCutscenePlaying && npcCutscene2Director != null)
        {
            Debug.Log("Skipping second cutscene");
            
            // FAST FORWARD TO END: Set timeline to the end before stopping
            double currentTime = npcCutscene2Director.time;
            double duration = npcCutscene2Director.duration;
            
            if (duration > 0)
            {
                // Fast forward to the end
                npcCutscene2Director.time = duration;
                
                // Evaluate the timeline at the end time
                npcCutscene2Director.Evaluate();
                
                // Trigger all bindings that should happen at the end
                TriggerAllBindings(npcCutscene2Director);
            }
            
            // Stop the director
            npcCutscene2Director.Stop();
            
            // Manually call the finish function
            FinishSecondCutscene(true);
        }
        else if (isThirdCutscenePlaying && npcTimeline3Director != null)
        {
            Debug.Log("Skipping third cutscene");
            
            // FAST FORWARD TO END: Set timeline to the end before stopping
            double currentTime = npcTimeline3Director.time;
            double duration = npcTimeline3Director.duration;
            
            if (duration > 0)
            {
                // Fast forward to the end
                npcTimeline3Director.time = duration;
                
                // Evaluate the timeline at the end time
                npcTimeline3Director.Evaluate();
                
                // Trigger all bindings that should happen at the end
                TriggerAllBindings(npcTimeline3Director);
            }
            
            // Stop the director
            npcTimeline3Director.Stop();
            
            // Manually call the finish function
            FinishThirdCutscene(true);
        }
    }
    
    // NEW: Trigger all timeline bindings to ensure end-of-timeline events fire
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
                    
                    // You can add more specific handling for other track types here
                    // For example, Activation tracks, Audio tracks, etc.
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
    
    // Public method to manually start second cutscene from other scripts
    public void ManualStartSecondCutscene()
    {
        StartSecondCutscene();
    }
    
    // Public method to manually start third cutscene from other scripts
    public void ManualStartThirdCutscene()
    {
        StartThirdCutscene();
    }
    
    // Check if a cutscene is currently playing
    public bool IsAnyCutscenePlaying()
    {
        return isSecondCutscenePlaying || isThirdCutscenePlaying;
    }
    
    // Reset all cutscenes
    public void ResetAllCutscenes()
    {
        isSecondCutscenePlaying = false;
        isThirdCutscenePlaying = false;
        waitingForFinalPanelConfirm = false;
        
        // Reset skip button state
        ResetSkipButtonState();
        
        // Hide dialogue canvases
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
        }
        
        if (dialogueCanvas3 != null)
        {
            dialogueCanvas3.SetActive(false);
        }
        
        // Resume monsters
        ResumeAllMonsters();
        
        // Disable cutscene parent objects
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
        }
        
        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
        }
        
        // Ensure player is unfrozen
        UnfreezePlayer();
        
        Debug.Log("All cutscenes reset");
    }
    
    // This method should be called when the last product is collected
    // It sets up the waiting state for the final panel confirm
    public void OnLastProductCollected()
    {
        if (productInfoManager != null && productInfoManager.IsAllCollected())
        {
            Debug.Log("=== ALL 8 PRODUCTS COLLECTED ===");
            Debug.Log("Waiting for final panel confirm button click...");
            waitingForFinalPanelConfirm = true;
        }
        else
        {
            Debug.Log("Not all products collected yet.");
            if (productInfoManager != null)
            {
                Debug.Log($"Current: {productInfoManager.GetCollectedCount()}/8");
            }
            waitingForFinalPanelConfirm = false;
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
    
    // Debug method to test monster control
    [ContextMenu("Test Pause All Monsters")]
    public void TestPauseAllMonsters()
    {
        PauseAllMonsters();
    }
    
    [ContextMenu("Test Resume All Monsters")]
    public void TestResumeAllMonsters()
    {
        ResumeAllMonsters();
    }
    
    [ContextMenu("Test Force Monsters to Patrol")]
    public void TestForceMonstersToPatrol()
    {
        ForceAllMonstersToPatrol();
    }
    
    // Debug method to test cutscenes
    [ContextMenu("Test Start Second Cutscene")]
    public void TestStartSecondCutscene()
    {
        Debug.Log("=== TESTING SECOND CUTSCENE ===");
        StartSecondCutscene();
    }
    
    [ContextMenu("Test Start Third Cutscene")]
    public void TestStartThirdCutscene()
    {
        Debug.Log("=== TESTING THIRD CUTSCENE ===");
        StartThirdCutscene();
    }
    
    [ContextMenu("Test Simulate Last Product Collected")]
    public void TestSimulateLastProductCollected()
    {
        OnLastProductCollected();
    }
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log("=== COLLECTION DEBUG ===");
        Debug.Log($"ProductInfoManager: {productInfoManager != null}");
        
        if (productInfoManager != null)
        {
            Debug.Log($"Collected Count: {productInfoManager.GetCollectedCount()}");
            Debug.Log($"Is All Collected: {productInfoManager.IsAllCollected()}");
            Debug.Log($"Total Products: {productInfoManager.productDatabase?.GetTotalCount()}");
        }
        
        Debug.Log($"Waiting for Final Panel: {waitingForFinalPanelConfirm}");
        Debug.Log($"Is Second Cutscene Playing: {isSecondCutscenePlaying}");
        Debug.Log($"Is Third Cutscene Playing: {isThirdCutscenePlaying}");
        Debug.Log($"Monster Count: {allMonsters.Count}");
    }
    
    // Editor method to auto-find references
    #if UNITY_EDITOR
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        // Find all monsters
        FindAllMonsters();
        
        // Find player
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            playerObject = foundPlayer;
            Debug.Log("Auto-found player: " + playerObject.name);
        }
        
        // Find CollectProducts script
        CollectProducts foundCollectScript = FindObjectOfType<CollectProducts>();
        if (foundCollectScript != null)
        {
            collectProductsScript = foundCollectScript;
            Debug.Log("Auto-found CollectProducts script");
        }
        
        // Find ProductInformationManager script
        ProductInformationManager foundProductInfo = FindObjectOfType<ProductInformationManager>();
        if (foundProductInfo != null)
        {
            productInfoManager = foundProductInfo;
            Debug.Log("Auto-found ProductInformationManager script");
        }
        
        // Find Audio Handler
        GameObject foundAudioHandler = GameObject.Find("Audio_Handler");
        if (foundAudioHandler != null)
        {
            audioHandler = foundAudioHandler;
            Debug.Log("Auto-found Audio Handler");
        }
        
        // Find Game UI Canvas
        GameObject foundGameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
        if (foundGameUICanvas != null)
        {
            gameUICanvas = foundGameUICanvas;
            Debug.Log("Auto-found Game UI Canvas");
        }
        
        // Try to find cutscene3 parent object
        if (cutscene3ParentObject == null)
        {
            GameObject foundCutscene3 = GameObject.Find("Cutscene3");
            if (foundCutscene3 != null)
            {
                cutscene3ParentObject = foundCutscene3;
                Debug.Log("Auto-found Cutscene3 parent object");
            }
        }
        
        // Try to find NPC_Timeline3 director
        if (npcTimeline3Director == null)
        {
            PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
            foreach (PlayableDirector director in allDirectors)
            {
                if (director.name.Contains("NPC_Timeline3"))
                {
                    npcTimeline3Director = director;
                    Debug.Log("Auto-found NPC_Timeline3 PlayableDirector");
                    break;
                }
            }
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    #endif
}