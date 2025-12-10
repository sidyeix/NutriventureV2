using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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
    
    [Header("Events")]
    public UnityEvent onSecondCutsceneStart;
    public UnityEvent onSecondCutsceneEnd;
    public UnityEvent onThirdCutsceneStart;
    public UnityEvent onThirdCutsceneEnd;
    
    // Simple state tracking
    private bool isSecondCutscenePlaying = false;
    private bool isThirdCutscenePlaying = false;
    private bool waitingForFinalPanelConfirm = false;
    
    // Monster tracking
    private List<MonsterObstacle> allMonsters = new List<MonsterObstacle>();
    private List<bool> monsterPauseStates = new List<bool>(); // Track which monsters were already paused
    
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
        
        // Show dialogue canvas when timeline starts
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled");
        }
    }
    
    void OnThirdCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Third timeline started playing");
        
        // Show dialogue canvas when timeline starts
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
            FinishSecondCutscene();
        }
    }
    
    void OnThirdCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Third timeline finished playing");
        
        if (isThirdCutscenePlaying)
        {
            FinishThirdCutscene();
        }
    }
    
    void FinishSecondCutscene()
    {
        Debug.Log("=== FINISHING SECOND CUTSCENE ===");
        
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
        
        // Invoke end event
        onSecondCutsceneEnd?.Invoke();
        
        Debug.Log("Second cutscene finished successfully");
    }
    
    void FinishThirdCutscene()
    {
        Debug.Log("=== FINISHING THIRD CUTSCENE ===");
        
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
        
        // Invoke end event
        onThirdCutsceneEnd?.Invoke();
        
        Debug.Log("Third cutscene finished successfully");
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
    
    // Skip the current cutscene
    public void SkipCurrentCutscene()
    {
        if (isSecondCutscenePlaying && npcCutscene2Director != null)
        {
            Debug.Log("Skipping second cutscene");
            npcCutscene2Director.Stop();
        }
        else if (isThirdCutscenePlaying && npcTimeline3Director != null)
        {
            Debug.Log("Skipping third cutscene");
            npcTimeline3Director.Stop();
        }
    }
    
    // Reset all cutscenes
    public void ResetAllCutscenes()
    {
        isSecondCutscenePlaying = false;
        isThirdCutscenePlaying = false;
        waitingForFinalPanelConfirm = false;
        
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