using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro

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
    [SerializeField] private GameObject dialogueCanvas; // SINGLE dialogue box canvas for BOTH timelines
    
    [Header("NPC Name Texts")]
    [SerializeField] private TMP_Text secondCutsceneNPCText; // TextMeshPro for second cutscene NPC name
    [SerializeField] private TMP_Text thirdCutsceneNPCText; // TextMeshPro for third cutscene NPC name
    
    [Header("Subtitle Controller")]
    [SerializeField] private K2_SubtitleController subtitleController; // Reference to subtitle controller
    
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
    
    // NPC text states
    private bool secondNPCTextWasActive = false;
    private bool thirdNPCTextWasActive = false;
    
    // Player components cache
    private ThirdPersonController cachedController;
    private Animator cachedAnimator;
    private StarterAssetsInputs cachedInputs;
    private PlayerInput cachedPlayerInput;
    private AudioSource cachedAudioSource;
    private Rigidbody cachedRigidbody;
    
    // NEW: Track original states
    private bool dialogueCanvasOriginalState = false;
    private bool subtitleControllerOriginalState = false;
    private bool secondNPCTextOriginalState = false;
    private bool thirdNPCTextOriginalState = false;
    
    // NEW: Protection system
    private Coroutine protectionCoroutine = null;
    private const float PROTECTION_CHECK_INTERVAL = 0.1f; // Check every 0.1 seconds
    
    void Start()
    {
        Debug.Log("K2_DummypTimeline Start called");
        
        // Initialize everything in a safe way
        SafeInitialize();
        
        // Cache player components
        CachePlayerComponents();
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
        
        // Find subtitle controller if not assigned
        if (subtitleController == null)
        {
            subtitleController = FindObjectOfType<K2_SubtitleController>();
            if (subtitleController != null)
            {
                Debug.Log("Found K2_SubtitleController");
            }
            else
            {
                Debug.LogWarning("K2_SubtitleController not found in scene!");
            }
        }
        
        // Store original states BEFORE doing anything
        StoreOriginalStates();
        
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
        
        // Disable dialogue canvas (SINGLE CANVAS FOR BOTH)
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled");
        }
        
        // Initialize second cutscene NPC text
        if (secondCutsceneNPCText != null)
        {
            // Store whether it was active before initialization
            secondNPCTextWasActive = secondCutsceneNPCText.gameObject.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            secondCutsceneNPCText.gameObject.SetActive(false);
            Debug.Log($"Second cutscene NPC text initialized: {secondCutsceneNPCText.name}, was active: {secondNPCTextWasActive}");
        }
        else
        {
            Debug.Log("No second cutscene NPC text assigned - skipping NPC name display");
        }
        
        // Initialize third cutscene NPC text
        if (thirdCutsceneNPCText != null)
        {
            // Store whether it was active before initialization
            thirdNPCTextWasActive = thirdCutsceneNPCText.gameObject.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            thirdCutsceneNPCText.gameObject.SetActive(false);
            Debug.Log($"Third cutscene NPC text initialized: {thirdCutsceneNPCText.name}, was active: {thirdNPCTextWasActive}");
        }
        else
        {
            Debug.Log("No third cutscene NPC text assigned - skipping NPC name display");
        }
        
        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
                // Cache components after finding player
                CachePlayerComponents();
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
    
    // NEW: Store original states
    private void StoreOriginalStates()
    {
        if (dialogueCanvas != null)
            dialogueCanvasOriginalState = dialogueCanvas.activeSelf;
        
        if (subtitleController != null)
            subtitleControllerOriginalState = subtitleController.gameObject.activeSelf;
        
        if (secondCutsceneNPCText != null)
            secondNPCTextOriginalState = secondCutsceneNPCText.gameObject.activeSelf;
        
        if (thirdCutsceneNPCText != null)
            thirdNPCTextOriginalState = thirdCutsceneNPCText.gameObject.activeSelf;
        
        Debug.Log($"Original states stored: Dialogue={dialogueCanvasOriginalState}, Subtitle={subtitleControllerOriginalState}");
    }
    
    // NEW: Start protection system
    private void StartProtectionSystem()
    {
        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
        }
        protectionCoroutine = StartCoroutine(ProtectionSystemCoroutine());
    }
    
    // NEW: Stop protection system
    private void StopProtectionSystem()
    {
        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
            protectionCoroutine = null;
        }
    }
    
    // NEW: Protection system coroutine
    private IEnumerator ProtectionSystemCoroutine()
    {
        Debug.Log("Starting protection system for cutscene...");
        
        while (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            yield return new WaitForSeconds(PROTECTION_CHECK_INTERVAL);
            
            // FORCE critical components to stay active during cutscene
            ForceComponentsActive();
        }
        
        Debug.Log("Protection system stopped");
    }
    
    // NEW: Force components to stay active
    private void ForceComponentsActive()
    {
        bool anyComponentWasFixed = false;
        
        // Dialogue canvas MUST stay active
        if (dialogueCanvas != null && !dialogueCanvas.activeSelf)
        {
            dialogueCanvas.SetActive(true);
            Debug.LogWarning("DIALOGUE CANVAS WAS DEACTIVATED! Forced back active.");
            anyComponentWasFixed = true;
        }
        
        // Subtitle controller MUST stay active
        if (subtitleController != null && !subtitleController.gameObject.activeSelf)
        {
            subtitleController.gameObject.SetActive(true);
            Debug.LogWarning("SUBTITLE CONTROLLER WAS DEACTIVATED! Forced back active.");
            anyComponentWasFixed = true;
        }
        
        // If any component was fixed, log a warning
        if (anyComponentWasFixed)
        {
            Debug.LogWarning("Timeline is deactivating critical components! Protection system is keeping them active.");
        }
    }
    
    // Cache player components to avoid GetComponent calls during cutscene
    private void CachePlayerComponents()
    {
        if (playerObject == null) return;
        
        cachedController = playerObject.GetComponent<ThirdPersonController>();
        cachedAnimator = playerObject.GetComponent<Animator>();
        cachedInputs = playerObject.GetComponent<StarterAssetsInputs>();
        cachedPlayerInput = playerObject.GetComponent<PlayerInput>();
        cachedAudioSource = playerObject.GetComponent<AudioSource>();
        cachedRigidbody = playerObject.GetComponent<Rigidbody>();
        
        Debug.Log($"Cached player components for {playerObject.name}");
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
        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
            Debug.Log("Unsubscribed from ProductInformationManager.OnProductPanelHidden event");
        }
        
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
        
        // Stop protection system
        StopProtectionSystem();
    }
    
    void OnDestroy()
    {
        // Remove skip button listener
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
        
        // Unsubscribe from panel events
        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
        }
        
        // Stop protection system
        StopProtectionSystem();
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
        StartSecondCutsceneWithNPCName(null); // Default call without custom name
    }
    
    // NEW: Overload method to start second cutscene with custom NPC name
    public void StartSecondCutscene(string customNPCName = null)
    {
        StartSecondCutsceneWithNPCName(customNPCName);
    }
    
    private void StartSecondCutsceneWithNPCName(string customNPCName = null)
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
        
        // Start protection system BEFORE anything else
        StartProtectionSystem();
        
        // Ensure subtitle controller is active
        ActivateSubtitleController();
        
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
        
        // Enable second cutscene NPC text
        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(true);
            
            // Set custom NPC name if provided
            if (!string.IsNullOrEmpty(customNPCName))
            {
                secondCutsceneNPCText.text = customNPCName;
                Debug.Log($"Second cutscene NPC name text set to: '{customNPCName}'");
            }
            else
            {
                Debug.Log($"Second cutscene NPC name text activated: {secondCutsceneNPCText.name}");
            }
        }
        
        // IMPORTANT: Wait one frame before playing timeline
        // This ensures everything is properly activated
        StartCoroutine(PlayTimelineAfterFrame(npcCutscene2Director, true));
    }
    
    // Public method to start the third cutscene
    public void StartThirdCutscene()
    {
        StartThirdCutsceneWithNPCName(null); // Default call without custom name
    }
    
    // NEW: Overload method to start third cutscene with custom NPC name
    public void StartThirdCutscene(string customNPCName = null)
    {
        StartThirdCutsceneWithNPCName(customNPCName);
    }
    
    private void StartThirdCutsceneWithNPCName(string customNPCName = null)
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
        
        // Start protection system BEFORE anything else
        StartProtectionSystem();
        
        // Ensure subtitle controller is active
        ActivateSubtitleController();
        
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
        
        // Enable third cutscene NPC text
        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(true);
            
            // Set custom NPC name if provided
            if (!string.IsNullOrEmpty(customNPCName))
            {
                thirdCutsceneNPCText.text = customNPCName;
                Debug.Log($"Third cutscene NPC name text set to: '{customNPCName}'");
            }
            else
            {
                Debug.Log($"Third cutscene NPC name text activated: {thirdCutsceneNPCText.name}");
            }
        }
        
        // IMPORTANT: Wait one frame before playing timeline
        // This ensures everything is properly activated
        StartCoroutine(PlayTimelineAfterFrame(npcTimeline3Director, false));
    }
    
    // Ensure subtitle controller is active before playing timeline
    private void ActivateSubtitleController()
    {
        if (subtitleController != null)
        {
            if (!subtitleController.gameObject.activeSelf)
            {
                subtitleController.gameObject.SetActive(true);
                Debug.Log("Activated subtitle controller");
            }
            else
            {
                Debug.Log("Subtitle controller was already active");
            }
        }
    }
    
    private IEnumerator PlayTimelineAfterFrame(PlayableDirector director, bool isSecondCutscene)
    {
        // Wait for end of frame to ensure everything is set up
        yield return new WaitForEndOfFrame();
        
        // Additional small delay to ensure all components are ready
        yield return new WaitForSeconds(0.1f);
        
        // CRITICAL: Force components active one more time before playing
        ForceComponentsActive();
        
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
        if (cachedController != null)
        {
            cachedController.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("ThirdPersonController not found on player!");
        }
        
        // Disable Animator
        if (cachedAnimator != null)
        {
            cachedAnimator.enabled = false;
            Debug.Log("Player animator disabled");
        }
        else
        {
            Debug.LogWarning("Animator not found on player!");
        }
        
        // Reset inputs
        if (cachedInputs != null)
        {
            cachedInputs.move = Vector2.zero;
            cachedInputs.look = Vector2.zero;
            cachedInputs.sprint = false;
            cachedInputs.jump = false;
            Debug.Log("Player inputs reset");
        }
        else
        {
            Debug.LogWarning("StarterAssetsInputs not found on player!");
        }
        
        // Disable PlayerInput (Input System)
        if (cachedPlayerInput != null)
        {
            cachedPlayerInput.enabled = false;
            Debug.Log("Player input system disabled");
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found on player!");
        }
        
        // Stop audio
        if (cachedAudioSource != null)
        {
            cachedAudioSource.Stop();
            Debug.Log("Player audio stopped");
        }
        
        // Stop physics movement
        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
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

        // Enable SINGLE dialogue canvas for both cutscenes
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled for second cutscene");
        }
    }
    
    void OnThirdCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Third timeline started playing");

        // FORCE UI & AUDIO OFF (THIS FIXES THE MID-CUTSCENE POP)
        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        if (audioHandler != null) audioHandler.SetActive(false);

        // Enable SINGLE dialogue canvas for both cutscenes
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled for third cutscene");
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
        
        // Stop protection system
        StopProtectionSystem();
        
        // Hide skip button
        HideSkipButton();
        
        // Hide dialogue canvas (SINGLE CANVAS FOR BOTH)
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
            Debug.Log($"Dialogue canvas restored to original state: {(dialogueCanvasOriginalState ? "active" : "inactive")}");
        }
        
        // Restore subtitle controller
        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
            Debug.Log($"Subtitle controller restored to original state: {(subtitleControllerOriginalState ? "active" : "inactive")}");
        }
        
        // Disable second cutscene NPC text
        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(secondNPCTextOriginalState);
            Debug.Log("Second cutscene NPC text disabled");
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
        
        // Stop protection system
        StopProtectionSystem();
        
        // Hide skip button
        HideSkipButton();
        
        // Hide dialogue canvas (SINGLE CANVAS FOR BOTH)
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
            Debug.Log($"Dialogue canvas restored to original state: {(dialogueCanvasOriginalState ? "active" : "inactive")}");
        }
        
        // Restore subtitle controller
        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
            Debug.Log($"Subtitle controller restored to original state: {(subtitleControllerOriginalState ? "active" : "inactive")}");
        }
        
        // Disable third cutscene NPC text
        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(thirdNPCTextOriginalState);
            Debug.Log("Third cutscene NPC text disabled");
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
            Debug.LogError("Cannot unfreeze player: Player object is null! Trying to find player...");
            
            // Try to find player again
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
                // Re-cache components
                CachePlayerComponents();
            }
            else
            {
                Debug.LogError("Player not found even after search!");
                return;
            }
        }
        
        // Re-enable ThirdPersonController
        if (cachedController != null)
        {
            cachedController.enabled = true;
            Debug.Log("Player controller enabled");
        }
        
        // Re-enable Animator
        if (cachedAnimator != null)
        {
            cachedAnimator.enabled = true;
            
            // Reset animation states
            cachedAnimator.SetFloat("Speed", 0f);
            cachedAnimator.SetFloat("MotionSpeed", 0f);
            Debug.Log("Player animator enabled and reset");
        }
        
        // Re-enable PlayerInput
        if (cachedPlayerInput != null)
        {
            cachedPlayerInput.enabled = true;
            Debug.Log("Player input system enabled");
        }
        
        Debug.Log("Player unfrozen successfully");
    }
    
    // NEW: Method to update second cutscene NPC name during cutscene
    public void UpdateSecondCutsceneNPCName(string newName)
    {
        if (secondCutsceneNPCText != null && isSecondCutscenePlaying)
        {
            secondCutsceneNPCText.text = newName;
            Debug.Log($"Second cutscene NPC name updated to: '{newName}'");
        }
        else if (secondCutsceneNPCText == null)
        {
            Debug.LogWarning("Cannot update second cutscene NPC name - no text assigned!");
        }
        else if (!isSecondCutscenePlaying)
        {
            Debug.LogWarning("Cannot update second cutscene NPC name - cutscene is not playing!");
        }
    }
    
    // NEW: Method to update third cutscene NPC name during cutscene
    public void UpdateThirdCutsceneNPCName(string newName)
    {
        if (thirdCutsceneNPCText != null && isThirdCutscenePlaying)
        {
            thirdCutsceneNPCText.text = newName;
            Debug.Log($"Third cutscene NPC name updated to: '{newName}'");
        }
        else if (thirdCutsceneNPCText == null)
        {
            Debug.LogWarning("Cannot update third cutscene NPC name - no text assigned!");
        }
        else if (!isThirdCutscenePlaying)
        {
            Debug.LogWarning("Cannot update third cutscene NPC name - cutscene is not playing!");
        }
    }
    
    // NEW: Method to show/hide second cutscene NPC name during cutscene
    public void SetSecondCutsceneNPCNameActive(bool active)
    {
        if (secondCutsceneNPCText != null && isSecondCutscenePlaying)
        {
            secondCutsceneNPCText.gameObject.SetActive(active);
            Debug.Log($"Second cutscene NPC name text {(active ? "shown" : "hidden")}");
        }
        else if (secondCutsceneNPCText == null)
        {
            Debug.LogWarning("Cannot show/hide second cutscene NPC name - no text assigned!");
        }
    }
    
    // NEW: Method to show/hide third cutscene NPC name during cutscene
    public void SetThirdCutsceneNPCNameActive(bool active)
    {
        if (thirdCutsceneNPCText != null && isThirdCutscenePlaying)
        {
            thirdCutsceneNPCText.gameObject.SetActive(active);
            Debug.Log($"Third cutscene NPC name text {(active ? "shown" : "hidden")}");
        }
        else if (thirdCutsceneNPCText == null)
        {
            Debug.LogWarning("Cannot show/hide third cutscene NPC name - no text assigned!");
        }
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
    
    // NEW: Check if second cutscene NPC name is active
    public bool IsSecondCutsceneNPCNameActive()
    {
        return secondCutsceneNPCText != null && secondCutsceneNPCText.gameObject.activeSelf;
    }
    
    // NEW: Check if third cutscene NPC name is active
    public bool IsThirdCutsceneNPCNameActive()
    {
        return thirdCutsceneNPCText != null && thirdCutsceneNPCText.gameObject.activeSelf;
    }
    
    // Reset all cutscenes
    public void ResetAllCutscenes()
    {
        isSecondCutscenePlaying = false;
        isThirdCutscenePlaying = false;
        waitingForFinalPanelConfirm = false;
        
        // Reset skip button state
        ResetSkipButtonState();
        
        // Stop protection system
        StopProtectionSystem();
        
        // Hide dialogue canvas (SINGLE CANVAS)
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
        }
        
        // Restore subtitle controller
        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
        }
        
        // Hide NPC name texts
        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(secondNPCTextOriginalState);
        }
        
        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(thirdNPCTextOriginalState);
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
    
    // NEW: Test methods for NPC names
    [ContextMenu("Test Start Second Cutscene with Custom Name")]
    public void TestStartSecondCutsceneWithCustomName()
    {
        StartSecondCutscene("SIR KALEB");
    }
    
    [ContextMenu("Test Start Third Cutscene with Custom Name")]
    public void TestStartThirdCutsceneWithCustomName()
    {
        StartThirdCutscene("QUEEN SUGARIA");
    }
    
    [ContextMenu("Update Second Cutscene NPC Name")]
    public void TestUpdateSecondCutsceneNPCName()
    {
        UpdateSecondCutsceneNPCName("UPDATED NPC NAME");
    }
    
    [ContextMenu("Update Third Cutscene NPC Name")]
    public void TestUpdateThirdCutsceneNPCName()
    {
        UpdateThirdCutsceneNPCName("UPDATED QUEEN");
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
        Debug.Log($"Second Cutscene NPC Text: {(secondCutsceneNPCText != null ? secondCutsceneNPCText.name : "NOT ASSIGNED")}");
        Debug.Log($"Third Cutscene NPC Text: {(thirdCutsceneNPCText != null ? thirdCutsceneNPCText.name : "NOT ASSIGNED")}");
        Debug.Log($"Monster Count: {allMonsters.Count}");
        Debug.Log($"Subtitle Controller: {(subtitleController != null ? "FOUND" : "NOT FOUND")}");
        Debug.Log($"Player Object: {(playerObject != null ? playerObject.name : "NULL")}");
        Debug.Log($"Dialogue Canvas: {(dialogueCanvas != null ? dialogueCanvas.name : "NOT ASSIGNED")}");
        Debug.Log($"Protection System: {(protectionCoroutine != null ? "ACTIVE" : "INACTIVE")}");
    }
    
    // Editor method to auto-find references
    #if UNITY_EDITOR
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        // Find all monsters
        FindAllMonsters();
        
        // Find subtitle controller
        subtitleController = FindObjectOfType<K2_SubtitleController>();
        if (subtitleController != null)
        {
            Debug.Log("Auto-found subtitle controller: " + subtitleController.name);
        }
        
        // Find player
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            playerObject = foundPlayer;
            Debug.Log("Auto-found player: " + playerObject.name);
            CachePlayerComponents();
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
        
        // Try to find Dialogue Canvas
        if (dialogueCanvas == null)
        {
            // Look for common dialogue canvas names
            string[] canvasNames = { "DialogueCanvas", "Dialogue_Canvas", "DialogueBox", "DialogCanvas", "SubtitleCanvas" };
            foreach (string canvasName in canvasNames)
            {
                GameObject foundCanvas = GameObject.Find(canvasName);
                if (foundCanvas != null)
                {
                    dialogueCanvas = foundCanvas;
                    Debug.Log($"Auto-found Dialogue Canvas: {dialogueCanvas.name}");
                    break;
                }
            }
            
            // If still not found, look for any canvas with "dialogue" in the name
            if (dialogueCanvas == null)
            {
                Canvas[] allCanvases = FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in allCanvases)
                {
                    if (canvas.name.ToLower().Contains("dialogue"))
                    {
                        dialogueCanvas = canvas.gameObject;
                        Debug.Log($"Auto-found Dialogue Canvas by name: {dialogueCanvas.name}");
                        break;
                    }
                }
            }
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