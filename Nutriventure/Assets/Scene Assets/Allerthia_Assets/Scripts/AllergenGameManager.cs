using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;

/// <summary>
/// Central game manager for the Allerthia Kingdom allergen collection game.
/// Handles the full game flow: scroll grab → instruction → timeline → game start → collection → finish.
/// </summary>
public class AllergenGameManager : MonoBehaviour
{
    public static AllergenGameManager Instance { get; private set; }

    public enum GameState
    {
        Idle,           // Waiting for player to grab scroll
        ScrollGrabbed,  // Scroll grabbed, showing instruction
        Playing,        // Game is active
        Finished        // All allergens collected or time ran out
    }

    [Header("Database")]
    public AllergenProductData allergenProductData;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public bool IsGameActive => currentState == GameState.Playing;
    public GameState CurrentState => currentState;

    // ─── SCROLL GRAB ───────────────────────────────────────────
    [Header("Scroll Grab")]
    [Tooltip("The Allerthia scroll 3D object in the scene")]
    public GameObject allerthiaScrollObject;
    [Tooltip("Collider trigger that gates showing the grab button")]
    public Collider scrollTrigger;
    [Tooltip("The Grab button (shown/hidden when player enters/exits scroll collider)")]
    public Button grabButton;

    // ─── INSTRUCTION / MECHANICS BOARD ─────────────────────────
    [Header("Instruction Panel")]
    [Tooltip("The instruction/mechanics panel canvas")]
    public GameObject instructionPanel;
    [Tooltip("Ready button inside the instruction panel (starts the game)")]
    public Button readyButton;
    [Tooltip("Close button inside the instruction panel (only shown after game starts)")]
    public Button instructionCloseButton;
    [Tooltip("Button to reopen the mechanics board during gameplay")]
    public Button openMechanicsButton;

    [Header("Scroll UI")]
    [Tooltip("UI button that opens the 3D scroll object")]
    public Button scrollUIButton;
    [Tooltip("3D object that contains the world-space scroll canvas")]
    public GameObject scrollUIObject;
    [Tooltip("Animator on ScrollUIObject with bool parameter 'isOpen'")]
    public Animator scrollUIObjectAnimator;
    public string scrollOpenParameter = "isOpen";
    [Tooltip("Optional close button on the scroll canvas")]
    public Button scrollCloseButton;
    [Tooltip("Virtual camera used when viewing the 3D scroll UI")]
    public CinemachineVirtualCamera scrollUIVirtualCamera;
    public int scrollCameraOpenPriority = 50;
    public int scrollCameraClosedPriority = 0;
    public float scrollCloseDelaySeconds = 1.5f;

    [Header("Scroll Grid")]
    public Canvas scrollCanvas;
    public Transform allergenGridParent;
    public GameObject allergenButtonPrefab;

    [Header("Product Showcase")]
    [Tooltip("Spawn point under ProductShowcase object used only for preview display")]
    public Transform productShowcaseSpawnPoint;
    public bool rotateShowcaseSpawnPoint = true;
    public Vector3 showcaseRotationAxis = Vector3.up;
    public float showcaseRotationSpeed = 40f;

    [Header("Disable While Scroll Open")]
    public List<GameObject> objectsToDisableWhenScrollOpen = new List<GameObject>();

    [Header("Scroll Info Panel")]
    public TMP_Text scrollInfoNameText;
    public TMP_Text scrollInfoDescriptionText;
    public TMP_Text scrollInfoFunFactText;
    public TMP_Text scrollCollectedTrackerText;

    [Header("External Tracker UI")]
    [Tooltip("Optional tracker text outside the ObjectUIScroll. Format: 'Allergens: X/Y'")]
    public TMP_Text externalAllergenTrackerText;

    [Header("Scroll Button Colors")]
    public Color scrollButtonDefaultColor = Color.white;
    public Color scrollButtonSelectedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color scrollButtonUncollectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color scrollButtonLockedIconColor = Color.black;
    public Color scrollButtonUnlockedIconColor = Color.white;
    public string lockedAllergenName = "XXXXX";

    // ─── TIMELINE ──────────────────────────────────────────────
    [Header("Timeline")]
    [Tooltip("The AlerthiaInstruction PlayableDirector")]
    public PlayableDirector instructionTimeline;

    [Header("Allergen Completion Cutscene")]
    [Tooltip("Cutscene that plays once when player has collected all required allergens.")]
    public PlayableDirector allAllergensCollectedCutscene;
    [Tooltip("Target runtime-collected allergens needed to trigger completion cutscene.")]
    public int requiredAllergenCountForCutscene = 9;
    [Tooltip("If true, gameplay/timer stops when completion cutscene starts.")]
    public bool stopGameplayWhenCompletionCutsceneStarts = true;

    // ─── TIMER ─────────────────────────────────────────────────
    [Header("Timer")]
    public TMP_Text timerText;
    [Tooltip("Max time in seconds (0 = no time limit)")]
    public float maxGameTime = 600f;
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    [Header("Points")]
    public TMP_Text pointsText;
    public int pointsPerPickup = 100;
    private int currentPoints = 0;

    // ─── HEALTH / HEARTS SYSTEM ───────────────────────────────
    [Header("Heart System")]
    public int maxHearts = 5;
    public Transform heartsContainer;
    public GameObject heartUIPrefab;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Damage Settings")]
    public float damageCooldown = 1f;

    [Header("Damage Overlay")]
    public GameObject damageOverlayObject;
    public float overlayBlinkDuration = 2f;
    public float overlayBlinkInterval = 0.15f;

    [Header("Game End Reference")]
    public Kingdom4GameEndManager gameEndManager;

    [HideInInspector] public float currentHealth;
    private float lastDamageTime;
    private readonly List<Image> heartImages = new List<Image>();
    private Coroutine overlayCoroutine;

    // ─── SPAWN MANAGER ────────────────────────────────────────
    [Header("Spawn Manager")]
    [Tooltip("Reference to the AllergenSpawnManager that handles prefab spawning")]
    public AllergenSpawnManager allergenSpawnManager;

    // ─── GAME START/END OBJECT MANAGEMENT ──────────────────────
    [Header("Objects to Disable When Game Starts")]
    public List<GameObject> objectsToDisableOnStart = new List<GameObject>();
    [Header("Objects to Enable When Game Starts")]
    public List<GameObject> objectsToEnableOnStart = new List<GameObject>();

    [Header("Objects to Revert Position on Scroll Grab")]
    [Tooltip("Their transforms are saved on scene start and restored when the scroll is grabbed (and on restart).")]
    public List<GameObject> objectsToRevertOnScrollGrab = new List<GameObject>();
    private struct SavedTransform { public Vector3 pos; public Quaternion rot; public Vector3 scale; }
    private readonly Dictionary<GameObject, SavedTransform> savedScrollGrabTransforms = new Dictionary<GameObject, SavedTransform>();

    [Header("Food Interaction")]
    [Tooltip("Raycast origin for food detection (usually player armature/chest)")]
    public Transform playerArmature;
    [Tooltip("Optional camera reference; if empty, Camera.main is used")]
    public Camera interactionCamera;
    [Tooltip("Local-space offset from playerArmature for ray origin. Increase Y to cast higher.")]
    public Vector3 pickupRayOriginOffset = new Vector3(0f, 1.2f, 0f);
    public float pickupRayDistance = 4f;
    public string interactableTag = "Interactable";

    [Header("Ray Debug")]
    public bool showPickupRay = true;
    public Color pickupRayMissColor = Color.red;
    public Color pickupRayHitColor = Color.green;

    [Header("Pickup SFX")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)] public float pickupSfxVolume = 1f;

    // ─── K4 GAME INTEGRATION ──────────────────────────────────
    [Header("K4 Rock Obstacle Managers")]
    [Tooltip("All K4_RockObstacleManagers in the scene. Their SpawnAll() is called on game start.")]
    public K4_RockObstacleManager[] rockObstacleManagers;

    [Header("K4 Stone Obstacles")]
    [Tooltip("All K4_StoneObstacles in the scene. Reset on game restart.")]
    public K4_StoneObstacles[] stoneObstacles;

    [Header("K4 NPC Challenge Managers")]
    [Tooltip("All NPC challenge stations. Reset on game restart.")]
    public NPCChallengeManager[] npcChallengeManagers;

    [Header("K4 Animators to Reset")]
    [Tooltip("Additional Animators (ladders, elevators) to rebind on restart.")]
    public Animator[] animatorsToReset;

    [Header("K4 Scroll Camera Blend")]
    [Tooltip("Smooth transition when opening/closing the scroll camera.")]
    public float scrollCameraBlendDuration = 0.5f;

    // ─── INTERNAL STATE ────────────────────────────────────────
    private bool scrollAlreadyGrabbed = false;
    private bool isPlayerInScrollTrigger = false;
    private IngredientInteractable currentTargetIngredient;
    private readonly HashSet<string> collectedAllergenIDs = new HashSet<string>();
    private readonly HashSet<string> runtimeCollectedAllergenIDs = new HashSet<string>();
    private readonly List<GameObject> spawnedScrollButtons = new List<GameObject>();
    private readonly Dictionary<string, AllergenProductData.ProductInfo> productInfoById = new Dictionary<string, AllergenProductData.ProductInfo>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> scrollButtonById = new Dictionary<string, Button>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Image> scrollButtonRootImageById = new Dictionary<string, Image>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Image> scrollButtonIconById = new Dictionary<string, Image>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TMP_Text> scrollButtonNameById = new Dictionary<string, TMP_Text>(System.StringComparer.OrdinalIgnoreCase);
    private const string initialUnlockedProductId = "peanut";
    private const string initialUnlockedDisplayName = "Peanut";
    private string selectedScrollProductId = string.Empty;
    private Coroutine timelineFallbackCoroutine;
    private Coroutine closeScrollCoroutine;
    private readonly Dictionary<GameObject, bool> scrollOpenPreviousActiveState = new Dictionary<GameObject, bool>();
    private GameObject spawnedShowcaseProduct;
    private bool hasPlayedAllergenCompletionCutscene = false;
    private bool pendingAllergenCompletionCutscene = false;

    [Header("NPC Challenge Tracker")]
    [Tooltip("UI text showing completed/total NPC challenges, e.g. '2/4'")]
    public TMP_Text npcChallengeTrackerText;
    private int completedNPCChallenges = 0;

    [Header("Player Tag")]
    public string playerTag = "Player";

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupTriggerRelay();
    }

    void Start()
    {
        // Always start fresh for this scene flow (no persistence yet)
        scrollAlreadyGrabbed = false;

        // Ensure scroll is visible at start
        if (allerthiaScrollObject != null)
        {
            allerthiaScrollObject.SetActive(true);
        }

        // Initial UI state
        SetButtonActive(grabButton, false);
        SetCanvasActive(instructionPanel, false);
        if (scrollUIObject != null) scrollUIObject.SetActive(false);

        if (readyButton != null) readyButton.gameObject.SetActive(false);
        if (instructionCloseButton != null) instructionCloseButton.gameObject.SetActive(false);
        if (openMechanicsButton != null) openMechanicsButton.gameObject.SetActive(false);

        SaveScrollGrabObjectTransforms();
        SetupButtonListeners();
        ResolveSpawnManager();
        EnsureScrollCanvasClickable();
        SeedInitialUnlockedProducts();
        BuildScrollButtons();
        UpdateTimerUI();
        UpdatePointsUI();
        UpdateCollectedTrackerUI();
        UpdateNPCChallengeTrackerUI();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }

        if (rotateShowcaseSpawnPoint && productShowcaseSpawnPoint != null)
        {
            productShowcaseSpawnPoint.Rotate(showcaseRotationAxis, showcaseRotationSpeed * Time.deltaTime, Space.Self);
        }

        if (currentState == GameState.Playing)
        {
            UpdateRaycastTargetAndGrabButton();
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  BUTTON SETUP
    // ══════════════════════════════════════════════════════════════

    private void SetupButtonListeners()
    {
        if (grabButton != null)
        {
            grabButton.onClick.RemoveAllListeners();
            grabButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                OnGrabButtonClicked();
            });
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                OnReadyButtonClicked();
            });
        }

        if (instructionCloseButton != null)
        {
            instructionCloseButton.onClick.RemoveAllListeners();
            instructionCloseButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                OnCloseInstructionPanel();
            });
        }

        if (openMechanicsButton != null)
        {
            openMechanicsButton.onClick.RemoveAllListeners();
            openMechanicsButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                OnOpenMechanicsBoard();
            });
        }

        if (scrollUIButton != null)
        {
            scrollUIButton.onClick.RemoveAllListeners();
            scrollUIButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                OpenScrollUI();
            });
        }

        if (scrollCloseButton != null)
        {
            scrollCloseButton.onClick.RemoveAllListeners();
            scrollCloseButton.onClick.AddListener(() =>
            {
                PlayButtonClickSfx();
                CloseScrollUI();
            });
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  SCROLL GRAB FLOW
    // ══════════════════════════════════════════════════════════════

    /// <summary>Called by the collider trigger when player enters the scroll zone.</summary>
    public void ShowGrabCanvas()
    {
        isPlayerInScrollTrigger = true;
        if (scrollAlreadyGrabbed || currentState != GameState.Idle) return;
        SetButtonActive(grabButton, true);
    }

    /// <summary>Called by the collider trigger when player exits the scroll zone.</summary>
    public void HideGrabCanvas()
    {
        isPlayerInScrollTrigger = false;
        SetButtonActive(grabButton, false);
    }

    private void OnGrabButtonClicked()
    {
        if (currentState == GameState.Idle)
        {
            OnGrabScrollClicked();
            return;
        }

        if (currentState == GameState.Playing)
        {
            OnGrabIngredientClicked();
        }
    }

    private void SetupTriggerRelay()
    {
        if (scrollTrigger == null)
        {
            scrollTrigger = GetComponent<Collider>();
        }

        if (scrollTrigger != null && !scrollTrigger.isTrigger)
        {
            scrollTrigger.isTrigger = true;
        }

        if (scrollTrigger != null && scrollTrigger.gameObject != gameObject)
        {
            var relay = scrollTrigger.GetComponent<AllergenScrollTriggerRelay>();
            if (relay == null)
            {
                relay = scrollTrigger.gameObject.AddComponent<AllergenScrollTriggerRelay>();
            }
            relay.manager = this;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleScrollTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleScrollTriggerExit(other);
    }

    public void HandleScrollTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        ShowGrabCanvas();
    }

    public void HandleScrollTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        HideGrabCanvas();
    }

    private void OnGrabScrollClicked()
    {
        if (currentState != GameState.Idle)
            return;

        Debug.Log("AllergenGameManager: Scroll grabbed!");

        scrollAlreadyGrabbed = true;
        currentState = GameState.ScrollGrabbed;

        // Revert door / entrance objects to their saved positions
        RevertScrollGrabObjectTransforms();

        // Hide scroll object and grab button
        if (allerthiaScrollObject != null) allerthiaScrollObject.SetActive(false);
        SetButtonActive(grabButton, false);

        // Immediately play instruction timeline (or start game if missing)
        PlayInstructionTimeline();
    }

    public void OpenScrollUI()
    {
        if (closeScrollCoroutine != null)
        {
            StopCoroutine(closeScrollCoroutine);
            closeScrollCoroutine = null;
        }

        SetScrollOpenObjectsState(true);

        if (scrollUIObject != null)
        {
            scrollUIObject.SetActive(true);
        }

        if (scrollUIObjectAnimator != null)
        {
            scrollUIObjectAnimator.SetBool(scrollOpenParameter, true);
        }

        if (scrollUIVirtualCamera != null)
        {
            ApplyScrollCameraBlend();
            scrollUIVirtualCamera.Priority = scrollCameraOpenPriority;
        }

        RefreshScrollButtonsVisualState();

        if (!string.IsNullOrEmpty(selectedScrollProductId))
        {
            UpdateScrollInfoPanel(selectedScrollProductId);
        }
        else
        {
            ClearScrollInfoPanel();
        }
    }

    public void CloseScrollUI()
    {
        if (scrollUIObjectAnimator != null)
        {
            scrollUIObjectAnimator.SetBool(scrollOpenParameter, false);
        }

        if (scrollUIVirtualCamera != null)
        {
            ApplyScrollCameraBlend();
            scrollUIVirtualCamera.Priority = scrollCameraClosedPriority;
        }

        SetScrollOpenObjectsState(false);

        if (closeScrollCoroutine != null)
        {
            StopCoroutine(closeScrollCoroutine);
        }

        closeScrollCoroutine = StartCoroutine(CloseScrollUIAfterDelay());

        // If this was the last allergen collected, play the completion cutscene now
        if (pendingAllergenCompletionCutscene)
        {
            pendingAllergenCompletionCutscene = false;
            TryPlayAllergensCompletionCutscene();
        }
    }

    private IEnumerator CloseScrollUIAfterDelay()
    {
        float waitTime = Mathf.Max(0f, scrollCloseDelaySeconds);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }

        if (scrollUIObject != null)
        {
            scrollUIObject.SetActive(false);
        }

        closeScrollCoroutine = null;
    }

    private void OnGrabIngredientClicked()
    {
        if (currentTargetIngredient == null)
            return;

        IngredientInteractable target = currentTargetIngredient;
        currentTargetIngredient = null;
        target.NotifyCollectedToManager();
        SetButtonActive(grabButton, false);
    }

    public void RegisterCollectedIngredient(string ingredientId, GameObject ingredientObject)
    {
        if (currentState != GameState.Playing)
            return;

        if (!string.IsNullOrEmpty(ingredientId))
        {
            string normalizedId = ingredientId.Trim();
            collectedAllergenIDs.Add(normalizedId);
            runtimeCollectedAllergenIDs.Add(normalizedId);
            selectedScrollProductId = normalizedId;
        }

        currentPoints += pointsPerPickup;
        UpdatePointsUI();
        UpdateCollectedTrackerUI();
        PlayPickupSfx();
        RefreshScrollButtonsVisualState();

        if (allergenSpawnManager != null && ingredientObject != null)
        {
            allergenSpawnManager.OnAllergenCollected(ingredientObject);
        }

        if (ingredientObject != null)
        {
            Destroy(ingredientObject);
        }

        OpenScrollUI();
        UpdateScrollInfoPanel(ingredientId);
        RefreshScrollButtonsVisualState();

        if (!string.IsNullOrEmpty(ingredientId))
        {
            SpawnShowcaseProductById(ingredientId);
        }

        // Don't play cutscene immediately — wait for the player to close the scroll UI
        int target = Mathf.Max(1, requiredAllergenCountForCutscene);
        if (!hasPlayedAllergenCompletionCutscene && collectedAllergenIDs.Count >= target)
        {
            pendingAllergenCompletionCutscene = true;
        }
    }

    private void TryPlayAllergensCompletionCutscene()
    {
        if (hasPlayedAllergenCompletionCutscene)
            return;

        int target = Mathf.Max(1, requiredAllergenCountForCutscene);
        if (collectedAllergenIDs.Count < target)
            return;

        hasPlayedAllergenCompletionCutscene = true;

        if (allAllergensCollectedCutscene == null)
            return;

        // Do NOT stop timer or set Finished — collecting all allergens is not the end of the game.
        allAllergensCollectedCutscene.stopped -= OnAllAllergensCutsceneFinished;
        allAllergensCollectedCutscene.stopped += OnAllAllergensCutsceneFinished;
        allAllergensCollectedCutscene.gameObject.SetActive(true);
        allAllergensCollectedCutscene.Play();
    }

    private void OnAllAllergensCutsceneFinished(PlayableDirector director)
    {
        director.stopped -= OnAllAllergensCutsceneFinished;
    }

    // ══════════════════════════════════════════════════════════════
    //  INSTRUCTION PANEL
    // ══════════════════════════════════════════════════════════════

    private void ShowInstructionForFirstTime()
    {
        SetCanvasActive(instructionPanel, true);

        // Show Ready button, hide Close button (first time only)
        if (readyButton != null) readyButton.gameObject.SetActive(true);
        if (instructionCloseButton != null) instructionCloseButton.gameObject.SetActive(false);

        Debug.Log("AllergenGameManager: Instruction panel shown (first time, Ready visible)");
    }

    private void OnOpenMechanicsBoard()
    {
        SetCanvasActive(instructionPanel, true);

        // During gameplay: show Close button, hide Ready button
        if (readyButton != null) readyButton.gameObject.SetActive(false);
        if (instructionCloseButton != null) instructionCloseButton.gameObject.SetActive(true);

        Debug.Log("AllergenGameManager: Mechanics board reopened");
    }

    private void OnCloseInstructionPanel()
    {
        SetCanvasActive(instructionPanel, false);
        Debug.Log("AllergenGameManager: Instruction panel closed");
    }

    private void PlayInstructionTimeline()
    {
        SetCanvasActive(instructionPanel, false);

        if (instructionTimeline != null)
        {
            instructionTimeline.gameObject.SetActive(true);
            instructionTimeline.stopped -= OnInstructionTimelineFinished;
            instructionTimeline.stopped += OnInstructionTimelineFinished;
            instructionTimeline.Play();

            if (timelineFallbackCoroutine != null)
            {
                StopCoroutine(timelineFallbackCoroutine);
            }
            timelineFallbackCoroutine = StartCoroutine(InstructionTimelineFallback());
        }
        else
        {
            Debug.LogWarning("AllergenGameManager: No instruction timeline assigned, starting game directly");
            StartGameIfNeeded();
        }
    }

    private IEnumerator InstructionTimelineFallback()
    {
        while (currentState == GameState.ScrollGrabbed)
        {
            if (instructionTimeline == null)
            {
                break;
            }

            // If stopped event fails to fire for any reason, this still advances to gameplay.
            if (instructionTimeline.state != PlayState.Playing)
            {
                StartGameIfNeeded();
                timelineFallbackCoroutine = null;
                yield break;
            }

            if (instructionTimeline.duration > 0 && instructionTimeline.time >= instructionTimeline.duration - 0.02d)
            {
                StartGameIfNeeded();
                timelineFallbackCoroutine = null;
                yield break;
            }

            yield return null;
        }

        timelineFallbackCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════════
    //  READY → TIMELINE → GAME START
    // ══════════════════════════════════════════════════════════════

    private void OnReadyButtonClicked()
    {
        Debug.Log("AllergenGameManager: Ready button clicked — playing instruction timeline");

        // Hide instruction panel
        SetCanvasActive(instructionPanel, false);

        PlayInstructionTimeline();
    }

    private void OnInstructionTimelineFinished(PlayableDirector director)
    {
        director.stopped -= OnInstructionTimelineFinished;
        Debug.Log("AllergenGameManager: Instruction timeline finished, starting game");
        StartGameIfNeeded();
    }

    private void StartGameIfNeeded()
    {
        if (currentState == GameState.Playing)
            return;

        StartGame();
    }

    // ══════════════════════════════════════════════════════════════
    //  GAME START
    // ══════════════════════════════════════════════════════════════

    private void StartGame()
    {
        currentState = GameState.Playing;
        Debug.Log("AllergenGameManager: Game started!");

        // Manage scene objects
        foreach (var obj in objectsToDisableOnStart)
            if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsToEnableOnStart)
            if (obj != null) obj.SetActive(true);

        // Show the mechanics reopen button
        if (openMechanicsButton != null) openMechanicsButton.gameObject.SetActive(true);

        // Reset and start timer
        elapsedTime = 0f;
        isTimerRunning = true;

        // Reset points and collected tracking
        currentPoints = 0;
        collectedAllergenIDs.Clear();
        runtimeCollectedAllergenIDs.Clear();
        hasPlayedAllergenCompletionCutscene = false;
        pendingAllergenCompletionCutscene = false;
        completedNPCChallenges = 0;
        SeedInitialUnlockedProducts();
        UpdatePointsUI();
        UpdateCollectedTrackerUI();
        UpdateNPCChallengeTrackerUI();

        // Initialize hearts
        InitializeHealth();

        // Spawn allergens via the spawn manager
        ResolveSpawnManager();
        if (allergenSpawnManager != null)
        {
            Debug.Log($"AllergenGameManager: Spawning allergens using manager '{allergenSpawnManager.name}'.");
            allergenSpawnManager.ForceRespawnAllergens();
        }
        else
        {
            Debug.LogWarning("AllergenGameManager: No AllergenSpawnManager assigned!");
        }

        // Spawn rock obstacle foods
        if (rockObstacleManagers != null)
        {
            foreach (var rom in rockObstacleManagers)
            {
                if (rom != null) rom.SpawnAll();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  K4 POINTS
    // ══════════════════════════════════════════════════════════════

    public void AddPoints(int amount)
    {
        currentPoints += amount;
        UpdatePointsUI();
    }

    public void DeductPoints(int amount)
    {
        currentPoints = Mathf.Max(0, currentPoints - amount);
        UpdatePointsUI();
    }

    // ══════════════════════════════════════════════════════════════
    //  NPC CHALLENGE TRACKER
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by NPCChallengeManager when a challenge is completed.
    /// </summary>
    public void NotifyNPCChallengeCompleted()
    {
        completedNPCChallenges++;
        UpdateNPCChallengeTrackerUI();
    }

    private void UpdateNPCChallengeTrackerUI()
    {
        if (npcChallengeTrackerText == null) return;

        int total = npcChallengeManagers != null ? npcChallengeManagers.Length : 0;
        npcChallengeTrackerText.text = $"Helped: {completedNPCChallenges}/{total}";
    }

    // ══════════════════════════════════════════════════════════════
    //  HEALTH / HEARTS
    // ══════════════════════════════════════════════════════════════

    public void InitializeHealth()
    {
        currentHealth = maxHearts;
        ClearHeartUI();
        CreateHeartUI();
        UpdateHeartUI();

        if (damageOverlayObject != null)
            damageOverlayObject.SetActive(false);
    }

    private void ClearHeartUI()
    {
        if (heartsContainer == null) return;

        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);

        heartImages.Clear();
    }

    private void CreateHeartUI()
    {
        if (heartsContainer == null || heartUIPrefab == null) return;

        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartGO = Instantiate(heartUIPrefab, heartsContainer);
            heartGO.name = "Heart_" + i;
            heartGO.transform.localScale = Vector3.one;

            Image img = heartGO.GetComponent<Image>();
            if (img == null)
                img = heartGO.AddComponent<Image>();

            if (fullHeartSprite != null)
                img.sprite = fullHeartSprite;

            heartImages.Add(img);
        }
    }

    private void UpdateHeartUI()
    {
        float hp = currentHealth;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;

            if (hp >= 1f)
            {
                heartImages[i].sprite = fullHeartSprite;
                hp -= 1f;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (Time.time < lastDamageTime + damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        ShowDamageOverlay();
        UpdateHeartUI();

        if (currentHealth <= 0f)
            OnPlayerDied();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHearts, currentHealth + amount);
        UpdateHeartUI();
    }

    public void ResetHealth()
    {
        currentHealth = maxHearts;
        UpdateHeartUI();
        HideDamageOverlay();
    }

    private void OnPlayerDied()
    {
        Debug.Log("Player Died — showing game summary");

        if (gameEndManager != null)
            gameEndManager.HandleKingdom4GameOver();
    }

    private void ShowDamageOverlay()
    {
        if (damageOverlayObject == null) return;

        if (overlayCoroutine != null)
            StopCoroutine(overlayCoroutine);

        overlayCoroutine = StartCoroutine(DamageOverlayRoutine());
    }

    private void HideDamageOverlay()
    {
        if (overlayCoroutine != null)
        {
            StopCoroutine(overlayCoroutine);
            overlayCoroutine = null;
        }

        if (damageOverlayObject != null)
            damageOverlayObject.SetActive(false);
    }

    private IEnumerator DamageOverlayRoutine()
    {
        CanvasGroup cg = damageOverlayObject.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = damageOverlayObject.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        bool visible = false;

        while (elapsed < overlayBlinkDuration)
        {
            visible = !visible;
            cg.alpha = visible ? 1f : 0f;
            damageOverlayObject.SetActive(visible);
            yield return new WaitForSeconds(overlayBlinkInterval);
            elapsed += overlayBlinkInterval;
        }

        cg.alpha = 0f;
        damageOverlayObject.SetActive(false);
        overlayCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════════
    //  K4 FULL RESTART
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resets every K4 system back to its initial state so the game
    /// can be started again as if it were freshly loaded.
    /// </summary>
    public void RestartK4Game()
    {
        // Reset rock obstacle spawned foods
        if (rockObstacleManagers != null)
        {
            foreach (var rom in rockObstacleManagers)
            {
                if (rom != null) rom.ClearAll();
            }
        }

        // Reset stone obstacles (animation + trigger)
        if (stoneObstacles != null)
        {
            foreach (var so in stoneObstacles)
            {
                if (so != null) so.ResetObstacle();
            }
        }

        // Reset NPC challenge managers
        if (npcChallengeManagers != null)
        {
            foreach (var npc in npcChallengeManagers)
            {
                if (npc != null) npc.ResetChallenge();
            }
        }

        // Reset additional animators (ladders, elevators, etc.)
        if (animatorsToReset != null)
        {
            foreach (var anim in animatorsToReset)
            {
                if (anim != null)
                {
                    anim.Rebind();
                    anim.Update(0f);
                }
            }
        }

        // Reset core session state (timer, points, allergens)
        ResetSessionState();

        // Reset health/hearts UI
        ResetHealth();

        // Reverse the start toggles: re-enable what was disabled, disable what was enabled
        foreach (var obj in objectsToDisableOnStart)
            if (obj != null) obj.SetActive(true);
        foreach (var obj in objectsToEnableOnStart)
            if (obj != null) obj.SetActive(false);

        // Revert scroll-grab objects back to their saved positions
        RevertScrollGrabObjectTransforms();
    }

    private void SaveScrollGrabObjectTransforms()
    {
        savedScrollGrabTransforms.Clear();
        foreach (var obj in objectsToRevertOnScrollGrab)
        {
            if (obj != null)
                savedScrollGrabTransforms[obj] = new SavedTransform
                {
                    pos = obj.transform.position,
                    rot = obj.transform.rotation,
                    scale = obj.transform.localScale
                };
        }
    }

    private void RevertScrollGrabObjectTransforms()
    {
        foreach (var kvp in savedScrollGrabTransforms)
        {
            if (kvp.Key != null)
            {
                kvp.Key.transform.position = kvp.Value.pos;
                kvp.Key.transform.rotation = kvp.Value.rot;
                kvp.Key.transform.localScale = kvp.Value.scale;
            }
        }
    }

    private void ResolveSpawnManager()
    {
        if (allergenSpawnManager == null)
        {
            allergenSpawnManager = FindFirstObjectByType<AllergenSpawnManager>();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
    }

    private void UpdatePointsUI()
    {
        if (pointsText != null)
            pointsText.text = currentPoints.ToString();
    }

    private void EnsureScrollCanvasClickable()
    {
        if (scrollCanvas == null)
            return;

        GraphicRaycaster raycaster = scrollCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = scrollCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (scrollCanvas.renderMode == RenderMode.WorldSpace)
        {
            if (interactionCamera != null)
            {
                scrollCanvas.worldCamera = interactionCamera;
            }
            else if (Camera.main != null)
            {
                scrollCanvas.worldCamera = Camera.main;
            }
        }
    }

    private void SeedInitialUnlockedProducts()
    {
        string matchedProductId = null;

        if (allergenProductData != null && allergenProductData.allProducts != null)
        {
            foreach (var product in allergenProductData.allProducts)
            {
                if (product == null || string.IsNullOrEmpty(product.productID))
                    continue;

                string candidateId = product.productID.Trim();
                string candidateDisplayName = string.IsNullOrEmpty(product.displayName) ? string.Empty : product.displayName.Trim();

                if (string.Equals(candidateId, initialUnlockedProductId, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedProductId = candidateId;
                    break;
                }

                if (matchedProductId == null && string.Equals(candidateDisplayName, initialUnlockedDisplayName, System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedProductId = candidateId;
                }
            }
        }

        if (string.IsNullOrEmpty(matchedProductId))
        {
            matchedProductId = initialUnlockedProductId;
        }

        collectedAllergenIDs.Add(matchedProductId);
        selectedScrollProductId = matchedProductId;
    }

    private void BuildScrollButtons()
    {
        ClearScrollButtons();
        productInfoById.Clear();
        scrollButtonById.Clear();
        scrollButtonRootImageById.Clear();
        scrollButtonIconById.Clear();
        scrollButtonNameById.Clear();

        if (allergenProductData == null || allergenProductData.allProducts == null)
            return;
        if (allergenGridParent == null || allergenButtonPrefab == null)
            return;

        foreach (var product in allergenProductData.allProducts)
        {
            if (product == null)
                continue;

            GameObject buttonObj = Instantiate(allergenButtonPrefab, allergenGridParent);
            spawnedScrollButtons.Add(buttonObj);

            if (!string.IsNullOrEmpty(product.productID))
            {
                productInfoById[product.productID.Trim()] = product;
            }

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                AllergenProductData.ProductInfo capturedProduct = product;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    PlayButtonClickSfx();
                    OnScrollAllergenButtonClicked(capturedProduct);
                });
            }

            BindAllergenButtonVisuals(buttonObj, product, button);
        }

        if (allergenProductData.allProducts.Length > 0)
        {
            string firstCollectedId = string.Empty;
            foreach (var product in allergenProductData.allProducts)
            {
                if (product != null && !string.IsNullOrEmpty(product.productID) && collectedAllergenIDs.Contains(product.productID))
                {
                    firstCollectedId = product.productID;
                    break;
                }
            }

            selectedScrollProductId = firstCollectedId;
            if (!string.IsNullOrEmpty(firstCollectedId))
            {
                UpdateScrollInfoPanel(firstCollectedId);
            }
            else
            {
                ClearScrollInfoPanel();
            }
        }

        RefreshScrollButtonsVisualState();
    }

    private void ClearScrollButtons()
    {
        foreach (GameObject buttonObj in spawnedScrollButtons)
        {
            if (buttonObj != null)
            {
                Destroy(buttonObj);
            }
        }
        spawnedScrollButtons.Clear();
    }

    private void BindAllergenButtonVisuals(GameObject buttonObj, AllergenProductData.ProductInfo product, Button button)
    {
        if (buttonObj == null || product == null)
            return;

        Image iconImage = null;
        Image[] images = buttonObj.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img == null)
                continue;

            string n = img.gameObject.name.ToLowerInvariant();
            if (n.Contains("allergenimage") || n.Contains("icon") || n.Contains("image"))
            {
                img.sprite = product.productIcon;
                img.preserveAspect = true;
                iconImage = img;
                break;
            }
        }

        TMP_Text nameTextRef = null;
        TMP_Text[] texts = buttonObj.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text txt in texts)
        {
            if (txt == null)
                continue;

            string n = txt.gameObject.name.ToLowerInvariant();
            if (n.Contains("allergenname") || n.Contains("name") || n.Contains("title"))
            {
                txt.text = product.displayName;
                nameTextRef = txt;
                break;
            }
        }

        buttonObj.name = $"AllergenButton_{product.productID}";

        if (!string.IsNullOrEmpty(product.productID))
        {
            string key = product.productID.Trim();
            if (button != null) scrollButtonById[key] = button;
            Image rootImage = buttonObj.GetComponent<Image>();
            if (rootImage != null) scrollButtonRootImageById[key] = rootImage;
            if (iconImage != null) scrollButtonIconById[key] = iconImage;
            if (nameTextRef != null) scrollButtonNameById[key] = nameTextRef;
        }
    }

    private void OnScrollAllergenButtonClicked(AllergenProductData.ProductInfo product)
    {
        if (product == null)
            return;

        selectedScrollProductId = product.productID;
        UpdateScrollInfoPanel(product);
        RefreshScrollButtonsVisualState();
        SpawnShowcaseProduct(product);
    }

    private void RefreshScrollButtonsVisualState()
    {
        if (allergenProductData == null || allergenProductData.allProducts == null)
            return;

        foreach (var product in allergenProductData.allProducts)
        {
            if (product == null || string.IsNullOrEmpty(product.productID))
                continue;

            string id = product.productID.Trim();
            bool isSelected = id == selectedScrollProductId;
            bool isCollected = collectedAllergenIDs.Contains(id);

            Button button;
            if (scrollButtonById.TryGetValue(id, out button) && button != null)
            {
                button.interactable = isCollected;
            }

            Image rootImage;
            if (scrollButtonRootImageById.TryGetValue(id, out rootImage) && rootImage != null)
            {
                if (isSelected && isCollected)
                {
                    rootImage.color = scrollButtonSelectedColor;
                }
                else if (!isCollected)
                {
                    rootImage.color = scrollButtonUncollectedColor;
                }
                else
                {
                    rootImage.color = scrollButtonDefaultColor;
                }
            }

            Image iconImage;
            if (scrollButtonIconById.TryGetValue(id, out iconImage) && iconImage != null)
            {
                iconImage.color = isCollected ? scrollButtonUnlockedIconColor : scrollButtonLockedIconColor;
            }

            TMP_Text nameText;
            if (scrollButtonNameById.TryGetValue(id, out nameText) && nameText != null)
            {
                nameText.text = isCollected ? product.displayName : lockedAllergenName;
            }
        }
    }

    private void UpdateScrollInfoPanel(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return;

        string key = productId.Trim();
        AllergenProductData.ProductInfo product = null;

        if (!productInfoById.TryGetValue(key, out product) && allergenProductData != null)
        {
            product = allergenProductData.GetProductInfo(key);
        }

        if (product == null)
            return;

        UpdateScrollInfoPanel(product);
    }

    private void UpdateScrollInfoPanel(AllergenProductData.ProductInfo product)
    {
        if (product == null)
            return;

        if (!collectedAllergenIDs.Contains(product.productID))
        {
            ClearScrollInfoPanel();
            return;
        }

        if (scrollInfoNameText != null)
            scrollInfoNameText.text = product.displayName;
        if (scrollInfoDescriptionText != null)
            scrollInfoDescriptionText.text = product.description;
        if (scrollInfoFunFactText != null)
            scrollInfoFunFactText.text = product.funFact;
    }

    private void ClearScrollInfoPanel()
    {
        if (scrollInfoNameText != null)
            scrollInfoNameText.text = lockedAllergenName;
        if (scrollInfoDescriptionText != null)
            scrollInfoDescriptionText.text = lockedAllergenName;
        if (scrollInfoFunFactText != null)
            scrollInfoFunFactText.text = lockedAllergenName;
    }

    private void SpawnShowcaseProductById(string productId)
    {
        if (string.IsNullOrEmpty(productId))
            return;

        string key = productId.Trim();
        AllergenProductData.ProductInfo product;
        if (!productInfoById.TryGetValue(key, out product) && allergenProductData != null)
        {
            product = allergenProductData.GetProductInfo(key);
        }

        SpawnShowcaseProduct(product);
    }

    private void SpawnShowcaseProduct(AllergenProductData.ProductInfo product)
    {
        ClearShowcaseProduct();

        if (product == null || productShowcaseSpawnPoint == null)
            return;
        if (product.productPrefab == null)
            return;

        spawnedShowcaseProduct = Instantiate(product.productPrefab, productShowcaseSpawnPoint);
        spawnedShowcaseProduct.transform.localPosition = Vector3.zero;
        spawnedShowcaseProduct.transform.localRotation = Quaternion.identity;
        spawnedShowcaseProduct.transform.localScale = Vector3.one;
    }

    private void ClearShowcaseProduct()
    {
        if (spawnedShowcaseProduct != null)
        {
            Destroy(spawnedShowcaseProduct);
            spawnedShowcaseProduct = null;
        }
    }

    private void SetScrollOpenObjectsState(bool isOpen)
    {
        if (isOpen)
        {
            scrollOpenPreviousActiveState.Clear();
            foreach (GameObject obj in objectsToDisableWhenScrollOpen)
            {
                if (obj == null) continue;
                scrollOpenPreviousActiveState[obj] = obj.activeSelf;
                if (obj.activeSelf)
                {
                    obj.SetActive(false);
                }
            }
        }
        else
        {
            foreach (var kvp in scrollOpenPreviousActiveState)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.SetActive(kvp.Value);
                }
            }
            scrollOpenPreviousActiveState.Clear();
        }
    }

    private void UpdateRaycastTargetAndGrabButton()
    {
        Transform originTransform = playerArmature;
        if (originTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                originTransform = player.transform;
            }
        }

        if (originTransform == null)
        {
            currentTargetIngredient = null;
            SetButtonActive(grabButton, false);
            return;
        }

        RaycastHit hit;
        Vector3 origin = originTransform.TransformPoint(pickupRayOriginOffset);
        Vector3 direction = originTransform.forward;

        if (Physics.Raycast(origin, direction, out hit, pickupRayDistance))
        {
            if (showPickupRay)
            {
                Debug.DrawRay(origin, direction * hit.distance, pickupRayHitColor);
            }

            GameObject hitObject = hit.collider.gameObject;

            bool tagMatch = hitObject.CompareTag(interactableTag);
            int interactableLayer = LayerMask.NameToLayer(interactableTag);
            bool layerMatch = interactableLayer >= 0 && hitObject.layer == interactableLayer;

            if (tagMatch || layerMatch)
            {
                IngredientInteractable ingredient = hit.collider.GetComponentInParent<IngredientInteractable>();
                if (ingredient != null)
                {
                    currentTargetIngredient = ingredient;
                    SetButtonActive(grabButton, true);
                    return;
                }
            }
        }
        else if (showPickupRay)
        {
            Debug.DrawRay(origin, direction * pickupRayDistance, pickupRayMissColor);
        }

        currentTargetIngredient = null;
        SetButtonActive(grabButton, false);
    }

    private void PlayPickupSfx()
    {
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, pickupSfxVolume);
        }
    }

    private void PlayButtonClickSfx()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    private void UpdateCollectedTrackerUI()
    {
        int total = allergenProductData != null && allergenProductData.allProducts != null
            ? allergenProductData.allProducts.Length
            : 0;
        int collectedCount = collectedAllergenIDs.Count;

        if (scrollCollectedTrackerText != null)
            scrollCollectedTrackerText.text = $"{collectedCount}/{total}";

        if (externalAllergenTrackerText != null)
            externalAllergenTrackerText.text = $"Allergens: {collectedCount}/{total}";
    }

    // ══════════════════════════════════════════════════════════════
    //  SCROLL CAMERA BLEND
    // ══════════════════════════════════════════════════════════════

    private Coroutine scrollBlendRestoreCoroutine;

    private void ApplyScrollCameraBlend()
    {
        CinemachineBrain brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain == null) return;

        if (scrollBlendRestoreCoroutine != null)
            StopCoroutine(scrollBlendRestoreCoroutine);

        CinemachineBlendDefinition original = brain.m_DefaultBlend;
        brain.m_DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Style.EaseInOut,
            Mathf.Max(0.1f, scrollCameraBlendDuration));

        scrollBlendRestoreCoroutine = StartCoroutine(RestoreScrollBlend(brain, original));
    }

    private IEnumerator RestoreScrollBlend(CinemachineBrain brain, CinemachineBlendDefinition original)
    {
        yield return new WaitForSeconds(scrollCameraBlendDuration + 0.05f);

        if (brain != null)
            brain.m_DefaultBlend = original;

        scrollBlendRestoreCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void SetCanvasActive(GameObject canvas, bool active)
    {
        if (canvas != null) canvas.SetActive(active);
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button != null) button.gameObject.SetActive(active);
    }

    // ══════════════════════════════════════════════════════════════
    //  PUBLIC ACCESSORS
    // ══════════════════════════════════════════════════════════════

    public float GetElapsedTime() => elapsedTime;
    public int GetPoints() => currentPoints;
    public bool IsScrollGrabbed() => scrollAlreadyGrabbed;
    public bool IsCollected(string ingredientId) => collectedAllergenIDs.Contains(ingredientId);

    void OnDestroy()
    {
        if (instructionTimeline != null)
        {
            instructionTimeline.stopped -= OnInstructionTimelineFinished;
        }
        if (allAllergensCollectedCutscene != null)
        {
            allAllergensCollectedCutscene.stopped -= OnAllAllergensCutsceneFinished;
        }
        if (timelineFallbackCoroutine != null)
        {
            StopCoroutine(timelineFallbackCoroutine);
            timelineFallbackCoroutine = null;
        }
        if (closeScrollCoroutine != null)
        {
            StopCoroutine(closeScrollCoroutine);
            closeScrollCoroutine = null;
        }
        if (Instance == this) Instance = null;
        ResetSessionState();
    }

    void OnDisable()
    {
        // Ensure state is clean when stopping play mode or disabling the manager
        if (timelineFallbackCoroutine != null)
        {
            StopCoroutine(timelineFallbackCoroutine);
            timelineFallbackCoroutine = null;
        }
        if (closeScrollCoroutine != null)
        {
            StopCoroutine(closeScrollCoroutine);
            closeScrollCoroutine = null;
        }
        ResetSessionState();
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        if (currentState == GameState.Playing)
            isTimerRunning = true;
    }

    private void ResetSessionState()
    {
        // Reset gameplay flags
        currentState = GameState.Idle;
        scrollAlreadyGrabbed = false;
        isPlayerInScrollTrigger = false;
        isTimerRunning = false;
        elapsedTime = 0f;
        currentPoints = 0;
        currentTargetIngredient = null;
        collectedAllergenIDs.Clear();
        runtimeCollectedAllergenIDs.Clear();
        hasPlayedAllergenCompletionCutscene = false;
        pendingAllergenCompletionCutscene = false;
        completedNPCChallenges = 0;
        selectedScrollProductId = string.Empty;
        SeedInitialUnlockedProducts();
        ClearShowcaseProduct();

        // Restore scroll visibility
        if (allerthiaScrollObject != null)
        {
            allerthiaScrollObject.SetActive(true);
        }

        // Hide UI elements
        SetButtonActive(grabButton, false);
        SetCanvasActive(instructionPanel, false);
        if (readyButton != null) readyButton.gameObject.SetActive(false);
        if (instructionCloseButton != null) instructionCloseButton.gameObject.SetActive(false);
        if (openMechanicsButton != null) openMechanicsButton.gameObject.SetActive(false);
        if (scrollUIObjectAnimator != null) scrollUIObjectAnimator.SetBool(scrollOpenParameter, false);
        if (scrollUIVirtualCamera != null) scrollUIVirtualCamera.Priority = scrollCameraClosedPriority;
        if (scrollUIObject != null) scrollUIObject.SetActive(false);
        SetScrollOpenObjectsState(false);

        // Ensure grab UI hidden after reset
        SetButtonActive(grabButton, false);
        UpdateTimerUI();
        UpdatePointsUI();
        UpdateCollectedTrackerUI();
        RefreshScrollButtonsVisualState();
    }
}
