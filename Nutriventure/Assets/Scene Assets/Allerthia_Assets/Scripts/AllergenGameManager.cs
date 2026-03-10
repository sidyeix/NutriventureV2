using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    public AllergenDatabase allergenDatabase;

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public bool IsGameActive => currentState == GameState.Playing;
    public GameState CurrentState => currentState;

    // ─── SCROLL GRAB ───────────────────────────────────────────
    [Header("Scroll Grab")]
    [Tooltip("The Allerthia scroll 3D object in the scene")]
    public GameObject allerthiaScrollObject;
    [Tooltip("Canvas containing the Grab button (shown when player enters collider)")]
    public GameObject grabCanvas;
    [Tooltip("The Grab button inside grabCanvas")]
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

    // ─── TIMELINE ──────────────────────────────────────────────
    [Header("Timeline")]
    [Tooltip("The AlerthiaInstruction PlayableDirector")]
    public PlayableDirector instructionTimeline;

    // ─── TIMER ─────────────────────────────────────────────────
    [Header("Timer")]
    public TMP_Text timerText;
    [Tooltip("Max time in seconds (0 = no time limit)")]
    public float maxGameTime = 600f;
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    // ─── SCORE / COLLECTION ────────────────────────────────────
    [Header("Score")]
    public TMP_Text scoreText;
    [Tooltip("Points awarded per allergen collected")]
    public int pointsPerAllergen = 100;
    private int currentScore = 0;

    [Header("Collection Tracking")]
    public TMP_Text collectionCountText;
    private List<string> collectedAllergenIDs = new List<string>();

    // ─── SPAWNING ──────────────────────────────────────────────
    [Header("Spawning")]
    [Tooltip("Transform points where allergen prefabs can spawn")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("Height offset above spawn point")]
    public float spawnHeightOffset = 0.5f;
    private List<GameObject> spawnedAllergens = new List<GameObject>();

    // ─── GAME START/END OBJECT MANAGEMENT ──────────────────────
    [Header("Objects to Disable When Game Starts")]
    public List<GameObject> objectsToDisableOnStart = new List<GameObject>();
    [Header("Objects to Enable When Game Starts")]
    public List<GameObject> objectsToEnableOnStart = new List<GameObject>();

    // ─── ALLERGEN INFO DISPLAY ─────────────────────────────────
    [Header("Allergen Info Panel")]
    [Tooltip("The panel that shows allergen info when collected")]
    public GameObject allergenInfoPanel;
    public Image allergenInfoImage;
    public TMP_Text allergenInfoName;
    public TMP_Text allergenInfoDescription;
    public TMP_Text allergenInfoFact;
    public TMP_Text allergenInfoFoodExamples;
    public Button allergenInfoCloseButton;

    // ─── PLAYER GRAB BUTTON (for allergen pickups) ─────────────
    [Header("Player Allergen Grab")]
    [Tooltip("Canvas with the grab button shown when near an allergen")]
    public GameObject allergenGrabCanvas;
    [Tooltip("The grab button inside allergenGrabCanvas")]
    public Button allergenGrabButton;

    // ─── GAME COMPLETE ─────────────────────────────────────────
    [Header("Game Complete")]
    [Tooltip("Panel shown when all allergens are collected")]
    public GameObject gameCompletePanel;

    // ─── INTERNAL STATE ────────────────────────────────────────
    private bool scrollAlreadyGrabbed = false;
    private AllergenPickup currentNearbyAllergen;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Check if scroll was already grabbed (from GameData)
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            scrollAlreadyGrabbed = GameDataManager.Instance.CurrentGameData.allerthiaScrollGrabbed;
        }

        // Initial UI state
        SetCanvasActive(grabCanvas, false);
        SetCanvasActive(instructionPanel, false);
        SetCanvasActive(allergenInfoPanel, false);
        SetCanvasActive(allergenGrabCanvas, false);
        SetCanvasActive(gameCompletePanel, false);

        if (readyButton != null) readyButton.gameObject.SetActive(false);
        if (instructionCloseButton != null) instructionCloseButton.gameObject.SetActive(false);
        if (openMechanicsButton != null) openMechanicsButton.gameObject.SetActive(false);

        // Hide scroll if already grabbed
        if (scrollAlreadyGrabbed && allerthiaScrollObject != null)
        {
            allerthiaScrollObject.SetActive(false);
        }

        SetupButtonListeners();
        UpdateCollectionUI();
        UpdateScoreUI();
        UpdateTimerUI();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();

            if (maxGameTime > 0 && elapsedTime >= maxGameTime)
            {
                OnTimerExpired();
            }
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
            grabButton.onClick.AddListener(OnGrabScrollClicked);
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (instructionCloseButton != null)
        {
            instructionCloseButton.onClick.RemoveAllListeners();
            instructionCloseButton.onClick.AddListener(OnCloseInstructionPanel);
        }

        if (openMechanicsButton != null)
        {
            openMechanicsButton.onClick.RemoveAllListeners();
            openMechanicsButton.onClick.AddListener(OnOpenMechanicsBoard);
        }

        if (allergenInfoCloseButton != null)
        {
            allergenInfoCloseButton.onClick.RemoveAllListeners();
            allergenInfoCloseButton.onClick.AddListener(OnCloseAllergenInfo);
        }

        if (allergenGrabButton != null)
        {
            allergenGrabButton.onClick.RemoveAllListeners();
            allergenGrabButton.onClick.AddListener(OnGrabAllergenClicked);
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  SCROLL GRAB FLOW
    // ══════════════════════════════════════════════════════════════

    /// <summary>Called by the collider trigger when player enters the scroll zone.</summary>
    public void ShowGrabCanvas()
    {
        if (scrollAlreadyGrabbed || currentState != GameState.Idle) return;
        SetCanvasActive(grabCanvas, true);
    }

    /// <summary>Called by the collider trigger when player exits the scroll zone.</summary>
    public void HideGrabCanvas()
    {
        SetCanvasActive(grabCanvas, false);
    }

    private void OnGrabScrollClicked()
    {
        Debug.Log("AllergenGameManager: Scroll grabbed!");

        scrollAlreadyGrabbed = true;
        currentState = GameState.ScrollGrabbed;

        // Save that scroll was grabbed
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.allerthiaScrollGrabbed = true;
            GameDataManager.Instance.SaveGameData();
        }

        // Hide scroll object and grab canvas
        if (allerthiaScrollObject != null) allerthiaScrollObject.SetActive(false);
        SetCanvasActive(grabCanvas, false);

        // Show instruction panel with Ready button visible, Close button hidden
        ShowInstructionForFirstTime();
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

    // ══════════════════════════════════════════════════════════════
    //  READY → TIMELINE → GAME START
    // ══════════════════════════════════════════════════════════════

    private void OnReadyButtonClicked()
    {
        Debug.Log("AllergenGameManager: Ready button clicked — playing instruction timeline");

        // Hide instruction panel
        SetCanvasActive(instructionPanel, false);

        // Play the AlerthiaInstruction timeline
        if (instructionTimeline != null)
        {
            instructionTimeline.gameObject.SetActive(true);
            instructionTimeline.stopped += OnInstructionTimelineFinished;
            instructionTimeline.Play();
        }
        else
        {
            Debug.LogWarning("AllergenGameManager: No instruction timeline assigned, starting game directly");
            StartGame();
        }
    }

    private void OnInstructionTimelineFinished(PlayableDirector director)
    {
        director.stopped -= OnInstructionTimelineFinished;
        Debug.Log("AllergenGameManager: Instruction timeline finished, starting game");
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

        // Reset score
        currentScore = 0;
        collectedAllergenIDs.Clear();
        UpdateScoreUI();
        UpdateCollectionUI();

        // Spawn allergens at spawn points
        SpawnAllergens();
    }

    // ══════════════════════════════════════════════════════════════
    //  SPAWNING
    // ══════════════════════════════════════════════════════════════

    private void SpawnAllergens()
    {
        if (allergenDatabase == null || allergenDatabase.allergens.Count == 0)
        {
            Debug.LogError("AllergenGameManager: No allergen database or no allergens defined!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("AllergenGameManager: No spawn points assigned!");
            return;
        }

        // Clear any previously spawned allergens
        ClearSpawnedAllergens();

        // Build a shuffled list of spawn points
        List<Transform> shuffledPoints = new List<Transform>(spawnPoints);
        ShuffleList(shuffledPoints);

        // Spawn one of each allergen at random spawn points
        int allergenCount = allergenDatabase.allergens.Count;
        int pointCount = shuffledPoints.Count;

        for (int i = 0; i < allergenCount; i++)
        {
            var allergenData = allergenDatabase.allergens[i];

            if (allergenData.allergenPrefab == null)
            {
                Debug.LogWarning($"AllergenGameManager: No prefab for allergen '{allergenData.allergenName}', skipping");
                continue;
            }

            // Wrap around spawn points if there are more allergens than points
            Transform spawnPoint = shuffledPoints[i % pointCount];
            Vector3 spawnPos = spawnPoint.position + Vector3.up * spawnHeightOffset;

            GameObject spawned = Instantiate(allergenData.allergenPrefab, spawnPos, spawnPoint.rotation);

            // Attach or configure the AllergenPickup component
            AllergenPickup pickup = spawned.GetComponent<AllergenPickup>();
            if (pickup == null)
                pickup = spawned.AddComponent<AllergenPickup>();

            pickup.Initialize(allergenData.allergenID);

            spawnedAllergens.Add(spawned);
            Debug.Log($"AllergenGameManager: Spawned '{allergenData.allergenName}' at {spawnPoint.name}");
        }
    }

    private void ClearSpawnedAllergens()
    {
        foreach (var obj in spawnedAllergens)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedAllergens.Clear();
    }

    // ══════════════════════════════════════════════════════════════
    //  ALLERGEN PICKUP (called by AllergenPickup triggers)
    // ══════════════════════════════════════════════════════════════

    /// <summary>Called by AllergenPickup when the player enters its trigger zone.</summary>
    public void OnPlayerNearAllergen(AllergenPickup pickup)
    {
        if (currentState != GameState.Playing) return;
        currentNearbyAllergen = pickup;
        SetCanvasActive(allergenGrabCanvas, true);
    }

    /// <summary>Called by AllergenPickup when the player exits its trigger zone.</summary>
    public void OnPlayerLeftAllergen(AllergenPickup pickup)
    {
        if (currentNearbyAllergen == pickup)
        {
            currentNearbyAllergen = null;
            SetCanvasActive(allergenGrabCanvas, false);
        }
    }

    private void OnGrabAllergenClicked()
    {
        if (currentNearbyAllergen == null) return;

        string allergenID = currentNearbyAllergen.AllergenID;

        // Avoid double-collecting
        if (collectedAllergenIDs.Contains(allergenID))
        {
            Debug.Log($"AllergenGameManager: Allergen '{allergenID}' already collected");
            return;
        }

        Debug.Log($"AllergenGameManager: Collecting allergen '{allergenID}'");

        // Add to collected
        collectedAllergenIDs.Add(allergenID);

        // Add score
        currentScore += pointsPerAllergen;

        // Destroy the pickup object
        GameObject pickupObj = currentNearbyAllergen.gameObject;
        currentNearbyAllergen = null;
        SetCanvasActive(allergenGrabCanvas, false);
        Destroy(pickupObj);

        // Show allergen info
        ShowAllergenInfo(allergenID);

        // Update UI
        UpdateScoreUI();
        UpdateCollectionUI();

        // Check if all collected
        if (collectedAllergenIDs.Count >= allergenDatabase.TotalAllergenCount)
        {
            OnAllAllergensCollected();
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ALLERGEN INFO DISPLAY
    // ══════════════════════════════════════════════════════════════

    private void ShowAllergenInfo(string allergenID)
    {
        var data = allergenDatabase.GetAllergenByID(allergenID);
        if (data == null) return;

        if (allergenInfoImage != null) allergenInfoImage.sprite = data.allergenImage;
        if (allergenInfoName != null) allergenInfoName.text = data.allergenName;
        if (allergenInfoDescription != null) allergenInfoDescription.text = data.description;
        if (allergenInfoFact != null) allergenInfoFact.text = data.fact;
        if (allergenInfoFoodExamples != null) allergenInfoFoodExamples.text = data.foodExamples;

        SetCanvasActive(allergenInfoPanel, true);

        // Pause timer while reading info
        isTimerRunning = false;
    }

    private void OnCloseAllergenInfo()
    {
        SetCanvasActive(allergenInfoPanel, false);

        // Resume timer if game is still playing
        if (currentState == GameState.Playing)
            isTimerRunning = true;
    }

    // ══════════════════════════════════════════════════════════════
    //  GAME END
    // ══════════════════════════════════════════════════════════════

    private void OnAllAllergensCollected()
    {
        Debug.Log("AllergenGameManager: All allergens collected!");
        EndGame();
    }

    private void OnTimerExpired()
    {
        Debug.Log("AllergenGameManager: Timer expired!");
        EndGame();
    }

    private void EndGame()
    {
        currentState = GameState.Finished;
        isTimerRunning = false;

        if (openMechanicsButton != null) openMechanicsButton.gameObject.SetActive(false);

        SetCanvasActive(gameCompletePanel, true);

        Debug.Log($"AllergenGameManager: Game finished! Score: {currentScore}, Collected: {collectedAllergenIDs.Count}/{allergenDatabase.TotalAllergenCount}, Time: {FormatTime(elapsedTime)}");
    }

    // ══════════════════════════════════════════════════════════════
    //  UI UPDATES
    // ══════════════════════════════════════════════════════════════

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString();
    }

    private void UpdateCollectionUI()
    {
        if (collectionCountText != null)
        {
            int total = allergenDatabase != null ? allergenDatabase.TotalAllergenCount : 0;
            collectionCountText.text = $"{collectedAllergenIDs.Count}/{total}";
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
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

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  PUBLIC ACCESSORS
    // ══════════════════════════════════════════════════════════════

    public int GetScore() => currentScore;
    public float GetElapsedTime() => elapsedTime;
    public int GetCollectedCount() => collectedAllergenIDs.Count;
    public bool IsAllergenCollected(string id) => collectedAllergenIDs.Contains(id);
    public List<string> GetCollectedAllergenIDs() => new List<string>(collectedAllergenIDs);
    public bool IsScrollGrabbed() => scrollAlreadyGrabbed;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
