using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class K2_DummypTimeline : MonoBehaviour
{
    [Header("Dummy Product Collection Detection")]
    [SerializeField] private CollectProducts collectProductsScript;
    [SerializeField] private ProductInformationManager productInfoManager;

    [Header("Second Timeline References")]
    [SerializeField] private GameObject cutscene2ParentObject;
    [SerializeField] private PlayableDirector npcCutscene2Director;

    [Header("Third Timeline References")]
    [SerializeField] private GameObject cutscene3ParentObject;
    [SerializeField] private PlayableDirector npcTimeline3Director;

    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject;

    [Header("Game Systems to Control")]
    [SerializeField] private GameObject audioHandler;
    [SerializeField] private GameObject gameUICanvas;

    [Header("Dialogue Canvas")]
    [SerializeField] private GameObject dialogueCanvas;

    [Header("NPC Name Texts")]
    [SerializeField] private TMP_Text secondCutsceneNPCText;
    [SerializeField] private TMP_Text thirdCutsceneNPCText;

    [Header("Subtitle Controller")]
    [SerializeField] private K2_SubtitleController subtitleController;

    [Header("Skip Button Settings")]
    [SerializeField] private Button skipButton;
    [SerializeField] private bool enableSkipButton = true;
    [SerializeField] private float skipButtonDelay = 2f;

    [Header("DYNAMIC UI CONTROL AFTER CUTSCENE 2")]
    [SerializeField] private List<GameObject> uiElementsToEnable = new List<GameObject>(); // Objects to ENABLE after cutscene 2
    [SerializeField] private List<GameObject> uiElementsToDisable = new List<GameObject>(); // Objects to DISABLE after cutscene 2

    [Header("Events")]
    public UnityEvent onSecondCutsceneStart;
    public UnityEvent onSecondCutsceneEnd;
    public UnityEvent onThirdCutsceneStart;
    public UnityEvent onThirdCutsceneEnd;
    public UnityEvent onCutsceneSkipped;

    // State tracking
    private bool isSecondCutscenePlaying = false;
    private bool isThirdCutscenePlaying = false;
    private bool waitingForFinalPanelConfirm = false;

    // Monster tracking
    private List<MonsterObstacle> allMonsters = new List<MonsterObstacle>();
    private List<bool> monsterPauseStates = new List<bool>();

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

    // Track original states
    private bool dialogueCanvasOriginalState = false;
    private bool subtitleControllerOriginalState = false;
    private bool secondNPCTextOriginalState = false;
    private bool thirdNPCTextOriginalState = false;

    // NEW: Track original states for dynamic UI elements
    private Dictionary<GameObject, bool> uiElementsToEnableOriginalStates = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, bool> uiElementsToDisableOriginalStates = new Dictionary<GameObject, bool>();

    // Protection system
    private Coroutine protectionCoroutine = null;
    private const float PROTECTION_CHECK_INTERVAL = 0.1f;

    void Start()
    {
        Debug.Log("K2_DummypTimeline Start called");
        SafeInitialize();
        CachePlayerComponents();
    }

    void Update()
    {
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
            skipButtonTimer += Time.unscaledDeltaTime;

            if (skipButtonTimer >= skipButtonDelay)
            {
                skipButtonReady = true;
                ShowSkipButton();
            }
        }
    }

    void SafeInitialize()
    {
        FindAllMonsters();

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

        // Store original states
        StoreOriginalStates();

        // Disable cutscene parents at start
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene2 parent object not assigned!");
        }

        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
            Debug.Log("Cutscene3 parent disabled");
        }
        else
        {
            Debug.LogError("Cutscene3 parent object not assigned!");
        }

        // Initialize PlayableDirectors
        if (npcCutscene2Director != null)
        {
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

        if (npcTimeline3Director != null)
        {
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
            skipButton.gameObject.SetActive(false);
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

        // Initialize NPC texts
        if (secondCutsceneNPCText != null)
        {
            secondNPCTextWasActive = secondCutsceneNPCText.gameObject.activeSelf;
            secondCutsceneNPCText.gameObject.SetActive(false);
            Debug.Log($"Second cutscene NPC text initialized, was active: {secondNPCTextWasActive}");
        }
        else
        {
            Debug.Log("No second cutscene NPC text assigned");
        }

        if (thirdCutsceneNPCText != null)
        {
            thirdNPCTextWasActive = thirdCutsceneNPCText.gameObject.activeSelf;
            thirdCutsceneNPCText.gameObject.SetActive(false);
            Debug.Log($"Third cutscene NPC text initialized, was active: {thirdNPCTextWasActive}");
        }
        else
        {
            Debug.Log("No third cutscene NPC text assigned");
        }

        // Find player if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
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

        // Log dynamic UI element status
        LogDynamicUIElementStatus();

        Debug.Log("K2_DummypTimeline initialized successfully");
    }

    // NEW: Log dynamic UI element assignment status
    private void LogDynamicUIElementStatus()
    {
        Debug.Log("=== DYNAMIC UI ELEMENTS STATUS ===");
        Debug.Log($"UI Elements to Enable ({uiElementsToEnable.Count}):");
        foreach (GameObject obj in uiElementsToEnable)
        {
            if (obj != null)
            {
                Debug.Log($"  - {obj.name} (current state: {(obj.activeSelf ? "enabled" : "disabled")})");
            }
            else
            {
                Debug.LogWarning("  - NULL reference in uiElementsToEnable list!");
            }
        }

        Debug.Log($"UI Elements to Disable ({uiElementsToDisable.Count}):");
        foreach (GameObject obj in uiElementsToDisable)
        {
            if (obj != null)
            {
                Debug.Log($"  - {obj.name} (current state: {(obj.activeSelf ? "enabled" : "disabled")})");
            }
            else
            {
                Debug.LogWarning("  - NULL reference in uiElementsToDisable list!");
            }
        }
    }

    // NEW: Store original states for all UI elements
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

        // Store original states for dynamic UI elements to enable
        uiElementsToEnableOriginalStates.Clear();
        foreach (GameObject obj in uiElementsToEnable)
        {
            if (obj != null && !uiElementsToEnableOriginalStates.ContainsKey(obj))
            {
                uiElementsToEnableOriginalStates.Add(obj, obj.activeSelf);
            }
        }

        // Store original states for dynamic UI elements to disable
        uiElementsToDisableOriginalStates.Clear();
        foreach (GameObject obj in uiElementsToDisable)
        {
            if (obj != null && !uiElementsToDisableOriginalStates.ContainsKey(obj))
            {
                uiElementsToDisableOriginalStates.Add(obj, obj.activeSelf);
            }
        }

        Debug.Log($"Original states stored: Dialogue={dialogueCanvasOriginalState}, Subtitle={subtitleControllerOriginalState}");
        Debug.Log($"Dynamic UI states stored: {uiElementsToEnableOriginalStates.Count} to enable, {uiElementsToDisableOriginalStates.Count} to disable");
    }

    // Start protection system
    private void StartProtectionSystem()
    {
        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
        }
        protectionCoroutine = StartCoroutine(ProtectionSystemCoroutine());
    }

    // Stop protection system
    private void StopProtectionSystem()
    {
        if (protectionCoroutine != null)
        {
            StopCoroutine(protectionCoroutine);
            protectionCoroutine = null;
        }
    }

    // Protection system coroutine
    private IEnumerator ProtectionSystemCoroutine()
    {
        Debug.Log("Starting protection system for cutscene...");

        while (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            yield return new WaitForSeconds(PROTECTION_CHECK_INTERVAL);
            ForceComponentsActive();
        }

        Debug.Log("Protection system stopped");
    }

    // Force components to stay active
    private void ForceComponentsActive()
    {
        bool anyComponentWasFixed = false;

        if (dialogueCanvas != null && !dialogueCanvas.activeSelf)
        {
            dialogueCanvas.SetActive(true);
            Debug.LogWarning("DIALOGUE CANVAS WAS DEACTIVATED! Forced back active.");
            anyComponentWasFixed = true;
        }

        if (subtitleController != null && !subtitleController.gameObject.activeSelf)
        {
            subtitleController.gameObject.SetActive(true);
            Debug.LogWarning("SUBTITLE CONTROLLER WAS DEACTIVATED! Forced back active.");
            anyComponentWasFixed = true;
        }

        if (anyComponentWasFixed)
        {
            Debug.LogWarning("Timeline is deactivating critical components! Protection system is keeping them active.");
        }
    }

    // Cache player components
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

        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped += OnSecondCutsceneFinished;
            npcCutscene2Director.played += OnSecondCutscenePlayed;
            Debug.Log("Subscribed to timeline2 events");
        }

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

        if (npcCutscene2Director != null)
        {
            npcCutscene2Director.stopped -= OnSecondCutsceneFinished;
            npcCutscene2Director.played -= OnSecondCutscenePlayed;
            Debug.Log("Unsubscribed from timeline2 events");
        }

        if (npcTimeline3Director != null)
        {
            npcTimeline3Director.stopped -= OnThirdCutsceneFinished;
            npcTimeline3Director.played -= OnThirdCutscenePlayed;
            Debug.Log("Unsubscribed from timeline3 events");
        }

        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
            Debug.Log("Unsubscribed from ProductInformationManager.OnProductPanelHidden event");
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }

        StopProtectionSystem();
    }

    void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }

        if (productInfoManager != null)
        {
            ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
        }

        StopProtectionSystem();
    }

    // Find all monsters
    private void FindAllMonsters()
    {
        MonsterObstacle[] foundMonsters = FindObjectsOfType<MonsterObstacle>();
        allMonsters.Clear();
        monsterPauseStates.Clear();

        foreach (MonsterObstacle monster in foundMonsters)
        {
            allMonsters.Add(monster);
            monsterPauseStates.Add(monster.IsPaused());
            Debug.Log($"Found monster: {monster.name}, Current Pause State: {monster.IsPaused()}");
        }

        Debug.Log($"Found {allMonsters.Count} monsters in scene");
    }

    // Pause all monsters
    private void PauseAllMonsters()
    {
        Debug.Log("Pausing all monsters for cutscene...");

        monsterPauseStates.Clear();

        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i] != null)
            {
                monsterPauseStates.Add(allMonsters[i].IsPaused());
                allMonsters[i].PauseMonster();
                Debug.Log($"Paused monster: {allMonsters[i].name}");
            }
            else
            {
                monsterPauseStates.Add(false);
            }
        }

        Debug.Log($"Paused {allMonsters.Count} monsters");
    }

    // Resume all monsters
    private void ResumeAllMonsters()
    {
        Debug.Log("Resuming monsters after cutscene...");

        int resumedCount = 0;

        for (int i = 0; i < allMonsters.Count; i++)
        {
            if (allMonsters[i] != null)
            {
                if (i < monsterPauseStates.Count && !monsterPauseStates[i])
                {
                    allMonsters[i].ResumeMonster();
                    resumedCount++;
                    Debug.Log($"Resumed monster: {allMonsters[i].name}");
                }
                else if (i >= monsterPauseStates.Count)
                {
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

    // Force all monsters to patrol
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

        if (waitingForFinalPanelConfirm)
        {
            Debug.Log("Was waiting for final panel confirm, checking collection...");

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

    // Start second cutscene
    public void StartSecondCutscene()
    {
        StartSecondCutsceneWithNPCName(null);
    }

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

        if (!ValidateComponentsForSecondCutscene())
        {
            Debug.LogError("Failed to validate components for second cutscene!");
            return;
        }

        isSecondCutscenePlaying = true;
        ResetSkipButtonState();
        StartProtectionSystem();
        ActivateSubtitleController();
        PauseAllMonsters();
        ForceAllMonstersToPatrol();
        FreezePlayer();

        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(true);
            Debug.Log("Cutscene2 parent enabled");
        }

        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }

        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }

        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(customNPCName))
            {
                secondCutsceneNPCText.text = customNPCName;
                Debug.Log($"Second cutscene NPC name text set to: '{customNPCName}'");
            }
            else
            {
                Debug.Log($"Second cutscene NPC name text activated");
            }
        }

        StartCoroutine(PlayTimelineAfterFrame(npcCutscene2Director, true));
    }

    // Start third cutscene
    public void StartThirdCutscene()
    {
        StartThirdCutsceneWithNPCName(null);
    }

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

        if (!ValidateComponentsForThirdCutscene())
        {
            Debug.LogError("Failed to validate components for third cutscene!");
            return;
        }

        isThirdCutscenePlaying = true;
        ResetSkipButtonState();
        StartProtectionSystem();
        ActivateSubtitleController();
        PauseAllMonsters();
        ForceAllMonstersToPatrol();
        FreezePlayer();

        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(true);
            Debug.Log("Cutscene3 parent enabled");
        }

        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
            Debug.Log("Game UI disabled");
        }

        if (audioHandler != null)
        {
            audioHandler.SetActive(false);
            Debug.Log("Audio handler disabled");
        }

        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(customNPCName))
            {
                thirdCutsceneNPCText.text = customNPCName;
                Debug.Log($"Third cutscene NPC name text set to: '{customNPCName}'");
            }
            else
            {
                Debug.Log($"Third cutscene NPC name text activated");
            }
        }

        StartCoroutine(PlayTimelineAfterFrame(npcTimeline3Director, false));
    }

    // Activate subtitle controller
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
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        ForceComponentsActive();

        if (director != null)
        {
            Debug.Log($"Playing timeline: {director.name}...");
            director.Play();
        }
        else
        {
            Debug.LogError("Cannot play timeline: Director is null!");
        }

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

        if (playerObject == null)
        {
            Debug.LogError("Player object is null!");
            allValid = false;
        }

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

        if (npcCutscene2Director == null)
        {
            Debug.LogError("Second timeline director is null!");
            allValid = false;
        }

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

        if (playerObject == null)
        {
            Debug.LogError("Player object is null!");
            allValid = false;
        }

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

        if (npcTimeline3Director == null)
        {
            Debug.LogError("Third timeline director is null!");
            allValid = false;
        }

        if (cutscene3ParentObject == null)
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

        if (cachedController != null)
        {
            cachedController.enabled = false;
            Debug.Log("Player controller disabled");
        }
        else
        {
            Debug.LogWarning("ThirdPersonController not found on player!");
        }

        if (cachedAnimator != null)
        {
            cachedAnimator.enabled = false;
            Debug.Log("Player animator disabled");
        }
        else
        {
            Debug.LogWarning("Animator not found on player!");
        }

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

        if (cachedPlayerInput != null)
        {
            cachedPlayerInput.enabled = false;
            Debug.Log("Player input system disabled");
        }
        else
        {
            Debug.LogWarning("PlayerInput component not found on player!");
        }

        if (cachedAudioSource != null)
        {
            cachedAudioSource.Stop();
            Debug.Log("Player audio stopped");
        }

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

        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        if (audioHandler != null) audioHandler.SetActive(false);

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("Dialogue canvas enabled for second cutscene");
        }
    }

    void OnThirdCutscenePlayed(PlayableDirector director)
    {
        Debug.Log("Third timeline started playing");

        if (gameUICanvas != null) gameUICanvas.SetActive(false);
        if (audioHandler != null) audioHandler.SetActive(false);

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
            FinishSecondCutscene(false);
        }
    }

    void OnThirdCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Third timeline finished playing");

        if (isThirdCutscenePlaying)
        {
            FinishThirdCutscene(false);
        }
    }

    void FinishSecondCutscene(bool wasSkipped = false)
    {
        Debug.Log($"=== FINISHING SECOND CUTSCENE (Skipped: {wasSkipped}) ===");

        StopProtectionSystem();
        HideSkipButton();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
            Debug.Log($"Dialogue canvas restored to original state: {(dialogueCanvasOriginalState ? "active" : "inactive")}");
        }

        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
            Debug.Log($"Subtitle controller restored to original state: {(subtitleControllerOriginalState ? "active" : "inactive")}");
        }

        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(secondNPCTextOriginalState);
            Debug.Log("Second cutscene NPC text disabled");
        }

        ResumeAllMonsters();

        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
            Debug.Log("Cutscene2 parent disabled");
        }

        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }

        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }

        // NEW: Handle dynamic UI elements after cutscene 2
        HandlePostCutscene2DynamicUI();

        UnfreezePlayer();

        isSecondCutscenePlaying = false;

        if (wasSkipped)
        {
            onCutsceneSkipped?.Invoke();
        }

        onSecondCutsceneEnd?.Invoke();

        Debug.Log($"Second cutscene {(wasSkipped ? "skipped" : "finished")} successfully");
    }

    // NEW: Handle dynamic UI elements after cutscene 2
    // Public so K2_GameStateManager can call it during resume to restore game-active UI state
    public void HandlePostCutscene2DynamicUI()
    {
        Debug.Log("=== HANDLING POST-CUTSCENE 2 DYNAMIC UI ===");

        // Enable all UI elements in the enable list
        if (uiElementsToEnable.Count > 0)
        {
            Debug.Log($"Enabling {uiElementsToEnable.Count} UI elements...");
            foreach (GameObject obj in uiElementsToEnable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"  - Enabled: {obj.name}");
                }
                else
                {
                    Debug.LogWarning("  - NULL reference in uiElementsToEnable list!");
                }
            }
        }
        else
        {
            Debug.Log("No UI elements to enable (list is empty)");
        }

        // Disable all UI elements in the disable list
        if (uiElementsToDisable.Count > 0)
        {
            Debug.Log($"Disabling {uiElementsToDisable.Count} UI elements...");
            foreach (GameObject obj in uiElementsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"  - Disabled: {obj.name}");
                }
                else
                {
                    Debug.LogWarning("  - NULL reference in uiElementsToDisable list!");
                }
            }
        }
        else
        {
            Debug.Log("No UI elements to disable (list is empty)");
        }

        // Verify changes
        LogPostCutscene2DynamicUIState();
    }

    // NEW: Log dynamic UI state after cutscene 2
    private void LogPostCutscene2DynamicUIState()
    {
        Debug.Log("=== POST-CUTSCENE 2 DYNAMIC UI STATE ===");

        Debug.Log("UI Elements that should be ENABLED:");
        if (uiElementsToEnable.Count > 0)
        {
            foreach (GameObject obj in uiElementsToEnable)
            {
                if (obj != null)
                {
                    Debug.Log($"  - {obj.name}: {(obj.activeSelf ? "ENABLED ✓" : "DISABLED ✗")}");
                }
            }
        }
        else
        {
            Debug.Log("  (None in list)");
        }

        Debug.Log("UI Elements that should be DISABLED:");
        if (uiElementsToDisable.Count > 0)
        {
            foreach (GameObject obj in uiElementsToDisable)
            {
                if (obj != null)
                {
                    Debug.Log($"  - {obj.name}: {(obj.activeSelf ? "ENABLED ✗" : "DISABLED ✓")}");
                }
            }
        }
        else
        {
            Debug.Log("  (None in list)");
        }
    }

    void FinishThirdCutscene(bool wasSkipped = false)
    {
        Debug.Log($"=== FINISHING THIRD CUTSCENE (Skipped: {wasSkipped}) ===");

        StopProtectionSystem();
        HideSkipButton();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
            Debug.Log($"Dialogue canvas restored to original state: {(dialogueCanvasOriginalState ? "active" : "inactive")}");
        }

        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
            Debug.Log($"Subtitle controller restored to original state: {(subtitleControllerOriginalState ? "active" : "inactive")}");
        }

        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(thirdNPCTextOriginalState);
            Debug.Log("Third cutscene NPC text disabled");
        }

        ResumeAllMonsters();

        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
            Debug.Log("Cutscene3 parent disabled");
        }

        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("Game UI enabled");
        }

        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
            Debug.Log("Audio handler enabled");
        }

        // NOTE: For cutscene 3, we DON'T modify the dynamic UI elements
        // They should remain as they were after cutscene 2
        Debug.Log("Third cutscene completed - dynamic UI elements unchanged (keeping post-cutscene 2 state)");

        UnfreezePlayer();

        isThirdCutscenePlaying = false;

        if (wasSkipped)
        {
            onCutsceneSkipped?.Invoke();
        }

        onThirdCutsceneEnd?.Invoke();

        Debug.Log($"Third cutscene {(wasSkipped ? "skipped" : "finished")} successfully");
    }

    void UnfreezePlayer()
    {
        if (playerObject == null)
        {
            Debug.LogError("Cannot unfreeze player: Player object is null! Trying to find player...");

            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                Debug.Log($"Found player: {playerObject.name}");
                CachePlayerComponents();
            }
            else
            {
                Debug.LogError("Player not found even after search!");
                return;
            }
        }

        if (cachedController != null)
        {
            cachedController.enabled = true;
            Debug.Log("Player controller enabled");
        }

        if (cachedAnimator != null)
        {
            cachedAnimator.enabled = true;
            cachedAnimator.SetFloat("Speed", 0f);
            cachedAnimator.SetFloat("MotionSpeed", 0f);
            Debug.Log("Player animator enabled and reset");
        }

        if (cachedPlayerInput != null)
        {
            cachedPlayerInput.enabled = true;
            Debug.Log("Player input system enabled");
        }

        Debug.Log("Player unfrozen successfully");
    }

    // Update NPC names
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

    // Show/hide NPC names
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

    // Skip button methods
    private void OnSkipButtonClicked()
    {
        if (isSecondCutscenePlaying || isThirdCutscenePlaying)
        {
            SkipCurrentCutscene();
        }
    }

    private void ShowSkipButton()
    {
        if (skipButton != null && enableSkipButton)
        {
            skipButton.gameObject.SetActive(true);
            Debug.Log("Skip button activated");
        }
    }

    private void HideSkipButton()
    {
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
    }

    private void ResetSkipButtonState()
    {
        skipButtonTimer = 0f;
        skipButtonReady = false;
        HideSkipButton();
    }

    public void SkipCurrentCutscene()
    {
        if (isSecondCutscenePlaying && npcCutscene2Director != null)
        {
            Debug.Log("Skipping second cutscene");

            double duration = npcCutscene2Director.duration;
            if (duration > 0)
            {
                npcCutscene2Director.time = duration;
                npcCutscene2Director.Evaluate();
                TriggerAllBindings(npcCutscene2Director);
            }

            npcCutscene2Director.Stop();
            FinishSecondCutscene(true);
        }
        else if (isThirdCutscenePlaying && npcTimeline3Director != null)
        {
            Debug.Log("Skipping third cutscene");

            double duration = npcTimeline3Director.duration;
            if (duration > 0)
            {
                npcTimeline3Director.time = duration;
                npcTimeline3Director.Evaluate();
                TriggerAllBindings(npcTimeline3Director);
            }

            npcTimeline3Director.Stop();
            FinishThirdCutscene(true);
        }
    }

    private void TriggerAllBindings(PlayableDirector director)
    {
        if (director == null) return;

        var bindings = director.playableAsset.outputs;

        foreach (var binding in bindings)
        {
            try
            {
                var boundObject = director.GetGenericBinding(binding.sourceObject);

                if (boundObject != null)
                {
                    if (binding.outputTargetType == typeof(Animator))
                    {
                        Animator animator = boundObject as Animator;
                        if (animator != null)
                        {
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

        director.Evaluate();
        Debug.Log("Timeline bindings triggered for skip");
    }

    // Public utility methods
    public void ManualStartSecondCutscene()
    {
        StartSecondCutscene();
    }

    public void ManualStartThirdCutscene()
    {
        StartThirdCutscene();
    }

    public bool IsAnyCutscenePlaying()
    {
        return isSecondCutscenePlaying || isThirdCutscenePlaying;
    }

    public bool IsSecondCutsceneNPCNameActive()
    {
        return secondCutsceneNPCText != null && secondCutsceneNPCText.gameObject.activeSelf;
    }

    public bool IsThirdCutsceneNPCNameActive()
    {
        return thirdCutsceneNPCText != null && thirdCutsceneNPCText.gameObject.activeSelf;
    }

    public void ResetAllCutscenes()
    {
        isSecondCutscenePlaying = false;
        isThirdCutscenePlaying = false;
        waitingForFinalPanelConfirm = false;

        ResetSkipButtonState();
        StopProtectionSystem();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(dialogueCanvasOriginalState);
        }

        if (subtitleController != null)
        {
            subtitleController.gameObject.SetActive(subtitleControllerOriginalState);
        }

        if (secondCutsceneNPCText != null)
        {
            secondCutsceneNPCText.gameObject.SetActive(secondNPCTextOriginalState);
        }

        if (thirdCutsceneNPCText != null)
        {
            thirdCutsceneNPCText.gameObject.SetActive(thirdNPCTextOriginalState);
        }

        ResumeAllMonsters();

        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
        }

        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
        }

        // Reset dynamic UI elements to original states
        ResetDynamicUIToOriginalStates();

        UnfreezePlayer();

        Debug.Log("All cutscenes reset to original states");
    }

    // NEW: Reset dynamic UI elements to original states
    private void ResetDynamicUIToOriginalStates()
    {
        Debug.Log("Resetting dynamic UI elements to original states...");

        foreach (var kvp in uiElementsToEnableOriginalStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(kvp.Value);
                Debug.Log($"  - {kvp.Key.name} restored to: {(kvp.Value ? "enabled" : "disabled")}");
            }
        }

        foreach (var kvp in uiElementsToDisableOriginalStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(kvp.Value);
                Debug.Log($"  - {kvp.Key.name} restored to: {(kvp.Value ? "enabled" : "disabled")}");
            }
        }
    }

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

    public void SetSkipButtonEnabled(bool enabled)
    {
        enableSkipButton = enabled;

        if (!enabled && skipButton != null)
        {
            HideSkipButton();
        }

        Debug.Log($"Skip button functionality {(enabled ? "enabled" : "disabled")}");
    }

    public void SetSkipButtonDelay(float delay)
    {
        skipButtonDelay = Mathf.Max(0f, delay);
        Debug.Log($"Skip button delay set to: {skipButtonDelay} seconds");
    }

    public bool IsSkipButtonReady()
    {
        return skipButtonReady;
    }

    public float GetSkipButtonTimeRemaining()
    {
        if (skipButtonReady) return 0f;
        return Mathf.Max(0f, skipButtonDelay - skipButtonTimer);
    }

    // NEW: Methods to dynamically add/remove UI elements at runtime
    public void AddUIElementToEnable(GameObject element)
    {
        if (element != null && !uiElementsToEnable.Contains(element))
        {
            uiElementsToEnable.Add(element);

            // Also store original state if not already stored
            if (!uiElementsToEnableOriginalStates.ContainsKey(element))
            {
                uiElementsToEnableOriginalStates.Add(element, element.activeSelf);
            }

            Debug.Log($"Added {element.name} to UI elements to enable list");
        }
    }

    public void RemoveUIElementFromEnable(GameObject element)
    {
        if (uiElementsToEnable.Contains(element))
        {
            uiElementsToEnable.Remove(element);
            Debug.Log($"Removed {element.name} from UI elements to enable list");
        }
    }

    public void AddUIElementToDisable(GameObject element)
    {
        if (element != null && !uiElementsToDisable.Contains(element))
        {
            uiElementsToDisable.Add(element);

            // Also store original state if not already stored
            if (!uiElementsToDisableOriginalStates.ContainsKey(element))
            {
                uiElementsToDisableOriginalStates.Add(element, element.activeSelf);
            }

            Debug.Log($"Added {element.name} to UI elements to disable list");
        }
    }

    public void RemoveUIElementFromDisable(GameObject element)
    {
        if (uiElementsToDisable.Contains(element))
        {
            uiElementsToDisable.Remove(element);
            Debug.Log($"Removed {element.name} from UI elements to disable list");
        }
    }

    public void ClearUIElementLists()
    {
        uiElementsToEnable.Clear();
        uiElementsToDisable.Clear();
        Debug.Log("Cleared all dynamic UI element lists");
    }

    // NEW: Method to log current dynamic UI state
    public void LogCurrentDynamicUIState()
    {
        Debug.Log("=== CURRENT DYNAMIC UI STATE ===");
        LogPostCutscene2DynamicUIState();
    }

    // NEW: Method to check if a UI element is in enable list
    public bool IsInEnableList(GameObject element)
    {
        return uiElementsToEnable.Contains(element);
    }

    // NEW: Method to check if a UI element is in disable list
    public bool IsInDisableList(GameObject element)
    {
        return uiElementsToDisable.Contains(element);
    }

    // Debug methods
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

        LogCurrentDynamicUIState();
    }

    [ContextMenu("Test Post-Cutscene 2 Dynamic UI")]
    public void TestPostCutscene2DynamicUI()
    {
        Debug.Log("=== TESTING POST-CUTSCENE 2 DYNAMIC UI ===");
        HandlePostCutscene2DynamicUI();
    }

    [ContextMenu("Reset Dynamic UI to Original States")]
    public void ResetDynamicUIToOriginalStatesTest()
    {
        ResetDynamicUIToOriginalStates();
        Debug.Log("Dynamic UI reset to original states");
        LogCurrentDynamicUIState();
    }

    // Editor method to auto-find references
#if UNITY_EDITOR
    [ContextMenu("Auto-Find References")]
    public void AutoFindReferences()
    {
        FindAllMonsters();
        
        subtitleController = FindObjectOfType<K2_SubtitleController>();
        if (subtitleController != null)
        {
            Debug.Log("Auto-found subtitle controller: " + subtitleController.name);
        }
        
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            playerObject = foundPlayer;
            Debug.Log("Auto-found player: " + playerObject.name);
            CachePlayerComponents();
        }
        
        CollectProducts foundCollectScript = FindObjectOfType<CollectProducts>();
        if (foundCollectScript != null)
        {
            collectProductsScript = foundCollectScript;
            Debug.Log("Auto-found CollectProducts script");
        }
        
        ProductInformationManager foundProductInfo = FindObjectOfType<ProductInformationManager>();
        if (foundProductInfo != null)
        {
            productInfoManager = foundProductInfo;
            Debug.Log("Auto-found ProductInformationManager script");
        }
        
        GameObject foundAudioHandler = GameObject.Find("Audio_Handler");
        if (foundAudioHandler != null)
        {
            audioHandler = foundAudioHandler;
            Debug.Log("Auto-found Audio Handler");
        }
        
        GameObject foundGameUICanvas = GameObject.Find("UI_Canvas_StarterAssetsInputs_Joysticks");
        if (foundGameUICanvas != null)
        {
            gameUICanvas = foundGameUICanvas;
            Debug.Log("Auto-found Game UI Canvas");
        }
        
        if (dialogueCanvas == null)
        {
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
        
        if (cutscene3ParentObject == null)
        {
            GameObject foundCutscene3 = GameObject.Find("Cutscene3");
            if (foundCutscene3 != null)
            {
                cutscene3ParentObject = foundCutscene3;
                Debug.Log("Auto-found Cutscene3 parent object");
            }
        }
        
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
        
        // Note: Dynamic UI lists cannot be auto-filled - they must be manually assigned
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}