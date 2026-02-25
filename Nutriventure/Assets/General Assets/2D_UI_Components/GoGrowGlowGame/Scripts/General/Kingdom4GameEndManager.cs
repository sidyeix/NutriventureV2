using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Cinemachine;

public class Kingdom4GameEndManager : MonoBehaviour
{
    public static Kingdom4GameEndManager Instance { get; private set; }
    
    [Header("Star Rating System")]
    [SerializeField] private GameObject starsContainer;
    [SerializeField] private Animator starsAnimator;
    [SerializeField] private string starParameter = "Stars";

    [Header("Stars to Hide")]
    [SerializeField] private List<GameObject> starsToHide = new List<GameObject>();

    [Header("Game Summary UI")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private GameObject buttonContainer;
    [SerializeField] private float countAnimationDuration = 2f;

    [Header("Count Animation Audio")]
    [SerializeField] private AudioClip countTickSound;
    [SerializeField] private AudioClip countCompleteSound;
    [SerializeField] private AudioSource countAudioSource;

    [Header("Result Background")]
    [SerializeField] private Image resultBackground;
    [SerializeField] private Sprite winBackground;
    [SerializeField] private Sprite loseBackground;

    [Header("Game Objects Management")]
    [SerializeField] private GameObject gameSummaryParent;
    [SerializeField] private List<GameObject> objectsToEnableOnLose = new List<GameObject>();
    [SerializeField] private List<GameObject> objectsToEnableOnWin = new List<GameObject>();
    [SerializeField] private List<GameObject> objectsToDisableOnGameEnd = new List<GameObject>();
    [SerializeField] private List<GameObject> objectsToEnableOnHomeButton = new List<GameObject>();
    [SerializeField] private GameObject keyUnlockedObject;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera gameEndVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    [SerializeField] private int gameEndCameraPriority = 100;
    [SerializeField] private int playerCameraPriority = 10;

    [Header("Spawn Points")]
    [SerializeField] private Transform resultCharacterSpawnPoint;
    [SerializeField] private Transform lobbyPoint;
    [SerializeField] private Transform startingPoint;

    [Header("Quest System")]
    [SerializeField] private string kingdomID = "kingdom4_quests";
    [SerializeField] private string questID = "kingdom4_complete";

    [Header("Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button restartButton;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioClip winMusicClip;
    [SerializeField] private AudioClip loseMusicClip;
    [SerializeField] private AudioClip restartMusicClip;
    [SerializeField] private AudioClip lobbyMusicClip;

    [Header("Object Reset System")]
    [SerializeField] private List<GameObject> objectsToReset = new List<GameObject>();
    [SerializeField] private bool storeInitialPositionsOnStart = true;

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string danceParameter = "isDancing";
    [SerializeField] private string thinkParameter = "isThinking";

    [Header("UI Controls")]
    [SerializeField] private GameObject uiControlsCanvas;

    [Header("References")]
    [SerializeField] private AllerthriaGameManager gameManager;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private Kingdom4ScoreManager scoreManager;
    [SerializeField] private PlayerHealthManager healthManager;

    [Header("Timer Integration")]
    [SerializeField] private GameTimer gameTimer;

    // ===== K2-style Button Functionality =====
    [Header("K2-style Button Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool completeRestartOnConfirm = true;
    [SerializeField] private string sceneToReload = "";
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float buttonClickVolume = 0.7f;
    
    [Header("Key Unlocked Canvas (unlockdocrcanvas)")]
    [SerializeField] private GameObject keyUnlockedCanvas; // This is the unlockdocrcanvas
    [SerializeField] private Button continueKeyButton; // The continue button inside the canvas
    [SerializeField] private KeyUnlockedCanvasController keyUnlockedController;
    
    [Header("Key Image Display")]
    [SerializeField] private GameObject KeyImageunlocking;
    // ==============================================

    // Game end calculations
    private int starsEarned = 0;
    private int baseCoins = 0;
    private int baseExp = 0;
    private int totalCoins = 0;
    private int totalExp = 0;
    private float completionTime = 0f;
    private int finalScore = 0;
    private int allergensCollected = 0;
    private int wagonHits = 0;
    private int remainingHearts = 5;
    private int maxComboAchieved = 1;
    private bool completedAllPhases = false;
    private float gameStartTime;

    // Scoring constants
    private const int MAX_ALLERGENS = 9;
    private const int STARTING_HEARTS = 5;
    private const float THREE_STAR_TIME = 600f;    // 10 minutes or less
    private const float TWO_STAR_TIME = 900f;      // 15 minutes or less
    private const int MAX_WAGON_HITS_FOR_3_STARS = 0;
    private const int MAX_WAGON_HITS_FOR_2_STARS = 2;
    private const int MAX_WAGON_HITS_FOR_1_STAR = 4;
    private const int MAX_COMBO_FOR_3_STARS = 8;
    private const int MAX_COMBO_FOR_2_STARS = 5;
    private const int MAX_COMBO_FOR_1_STAR = 3;

    private Coroutine countAnimationCoroutine;
    private bool isFirstTimeCompletion = false;
    private bool isCountingAnimationComplete = false;

    private Dictionary<GameObject, TransformData> initialTransformData = new Dictionary<GameObject, TransformData>();

    private CinemachineBrain cinemachineBrain;
    private CinemachineBlendDefinition originalBlendDefinition;

    // Button state tracking
    private bool isProcessingButton = false;
    private float originalTimeScale;
    private bool playerWon = false;
    private bool keyWasCollected = false;
    private bool keySavedToDatabase = false;
    private int healthAtKeyCollection = 0;
    private K4_CollectKey collectKeyScript;
    
    // State tracking
    private bool isGameOver = false;
    private bool isSummaryActive = false;
    
    // Coin tracking
    private bool coinsAddedToDatabase = false;

    [System.Serializable]
    public class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<AllerthriaGameManager>();

        if (playerController == null)
            playerController = FindObjectOfType<ThirdPersonController>();

        if (questManager == null)
            questManager = QuestManager.Instance;

        if (scoreManager == null)
            scoreManager = FindObjectOfType<Kingdom4ScoreManager>();

        if (healthManager == null)
            healthManager = FindObjectOfType<PlayerHealthManager>();

        // Find collect key script
        if (collectKeyScript == null)
            collectKeyScript = FindObjectOfType<K4_CollectKey>();

        // Find GameTimer
        if (gameTimer == null)
        {
            gameTimer = GameTimer.Instance;
            if (gameTimer == null)
            {
                gameTimer = FindObjectOfType<GameTimer>();
            }
            
            if (gameTimer != null)
            {
                Debug.Log($"Found GameTimer: {gameTimer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("GameTimer not found! Time-based scoring may not work properly.");
            }
        }

        // Find Cinemachine Brain
        cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (cinemachineBrain != null)
        {
            originalBlendDefinition = cinemachineBrain.m_DefaultBlend;
        }

        // Find character animator if not assigned
        if (characterAnimator == null && playerController != null)
        {
            characterAnimator = playerController.GetComponentInChildren<Animator>();
        }

        // Find cameras if not assigned
        FindCameras();
        
        // Try to find starting point if not assigned
        if (startingPoint == null)
        {
            GameObject startPointObj = GameObject.Find("StartingPoint");
            if (startPointObj != null)
            {
                startingPoint = startPointObj.transform;
            }
        }

        // Find KeyUnlockedController if not assigned
        if (keyUnlockedController == null && keyUnlockedCanvas != null)
        {
            keyUnlockedController = keyUnlockedCanvas.GetComponent<KeyUnlockedCanvasController>();
        }
        
        // If still null, try to find it anywhere in the scene
        if (keyUnlockedController == null)
        {
            keyUnlockedController = FindObjectOfType<KeyUnlockedCanvasController>();
            if (keyUnlockedController != null && keyUnlockedCanvas == null)
            {
                keyUnlockedCanvas = keyUnlockedController.gameObject;
            }
        }
        
        // Initialize key unlock canvas to be hidden
        if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }
        
        // Set up continue button listener
        if (continueKeyButton != null)
        {
            continueKeyButton.onClick.RemoveAllListeners();
            continueKeyButton.onClick.AddListener(OnContinueKeyButtonClicked);
        }
    }

    private void FindCameras()
    {
        if (gameEndVirtualCamera == null)
        {
            CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();
            foreach (CinemachineVirtualCamera cam in cameras)
            {
                if (cam.gameObject.name.Contains("GameEnd") || cam.gameObject.name.Contains("Result"))
                {
                    gameEndVirtualCamera = cam;
                    break;
                }
            }
        }

        if (playerFollowCamera == null)
        {
            playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }
    }

    private void Start()
    {
        InitializeUI();
        
        if (storeInitialPositionsOnStart)
        {
            StoreInitialTransforms();
        }

        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
        }
    }

    private void InitializeUI()
    {
        if (buttonContainer != null)
            buttonContainer.SetActive(false);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(OnHomeClicked);
            homeButton.interactable = true;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
            restartButton.interactable = true;
        }

        // Initialize key unlock canvas
        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);

        // Initialize KeyImageunlocking
        if (KeyImageunlocking != null)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking initialized as DISABLED");
        }

        // Make sure stars container is hidden initially
        if (starsContainer != null)
            starsContainer.SetActive(false);

        // Make sure game end camera is disabled initially
        if (gameEndVirtualCamera != null)
        {
            gameEndVirtualCamera.Priority = 0;
            gameEndVirtualCamera.gameObject.SetActive(false);
        }

        // Make sure UI controls are enabled initially
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);
    }

    private void StoreInitialTransforms()
    {
        initialTransformData.Clear();

        foreach (GameObject obj in objectsToReset)
        {
            if (obj != null)
            {
                initialTransformData[obj] = new TransformData
                {
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    localScale = obj.transform.localScale
                };
            }
        }
    }

    // ==================== GAME END SCREEN LOGIC ====================
    
    public void ShowGameEndScreen(bool playerWon)
    {
        Debug.Log($"=== SHOWING KINGDOM 4 END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");
        
        this.playerWon = playerWon;
        isCountingAnimationComplete = false;
        isProcessingButton = false;
        isGameOver = true;
        isSummaryActive = true;
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Check if key was collected this session
        CheckKeyCollectionStatus();
        
        CollectGameData();
        CalculateAllMetrics(playerWon);
        
        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();
        SetupUI(playerWon);
        UpdateKeyImageDisplay(); // Show key image if conditions met
        
        if (gameSummaryParent != null)
        {
            gameSummaryParent.SetActive(true);
            Debug.Log("Game summary parent activated");
            
            // Start with panel fade in if CanvasGroup exists
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
            }
        }
        else
        {
            Debug.LogError("gameSummaryParent is null! Cannot show game summary.");
            return;
        }

        if (buttonContainer != null)
        {
            buttonContainer.SetActive(false);
        }

        StartCoroutine(GameEndSequence());
    }

    private void CheckKeyCollectionStatus()
    {
        if (collectKeyScript != null)
        {
            keyWasCollected = collectKeyScript.HasKey();
            healthAtKeyCollection = collectKeyScript.GetHealthAtKeyCollection();
            Debug.Log($"Key collection status - Collected: {keyWasCollected}, Health: {healthAtKeyCollection}");
        }
        else
        {
            Debug.Log("CollectKeyScript not found");
        }
    }

    private void CollectGameData()
    {
        // Get score from Kingdom4ScoreManager
        if (scoreManager != null)
        {
            finalScore = scoreManager.GetFinalScore();
            wagonHits = scoreManager.totalWagonHits;
            allergensCollected = scoreManager.allergensFound;
            maxComboAchieved = scoreManager.maxComboAchieved;
        }
        else
        {
            Debug.LogWarning("ScoreManager not found!");
            scoreManager = FindObjectOfType<Kingdom4ScoreManager>();
        }

        // Get completion time from GameTimer
        if (gameTimer != null)
        {
            completionTime = gameTimer.ElapsedTime;
            Debug.Log($"Timer elapsed time: {completionTime}s");
            
            // Stop the timer when game ends
            gameTimer.StopTimer();
        }
        else
        {
            // Fallback: Use system time
            completionTime = Time.time - gameStartTime;
            Debug.LogWarning("GameTimer not found, using system time");
        }
        
        // Check if all phases completed
        if (gameManager != null)
        {
            completedAllPhases = gameManager.currentPhase == AllerthriaGameManager.GamePhase.EndGame;
        }
        else
        {
            Debug.LogWarning("GameManager not found!");
            gameManager = FindObjectOfType<AllerthriaGameManager>();
        }

        // Get remaining hearts from PlayerHealthManager
        GetRemainingHealth();
        
        Debug.Log($"Game Data Collected: Time={completionTime}s, Hearts={remainingHearts}, Allergens={allergensCollected}, WagonHits={wagonHits}, MaxCombo={maxComboAchieved}");
    }

    private float GetElapsedTime()
    {
        if (gameTimer != null)
        {
            return gameTimer.ElapsedTime;
        }
        return Time.time - gameStartTime;
    }

    private void GetRemainingHealth()
    {
        if (healthManager != null)
        {
            remainingHearts = Mathf.CeilToInt(healthManager.currentHealth);
        }
        else if (gameManager != null)
        {
            // Fallback
            remainingHearts = 3;
        }
        else
        {
            remainingHearts = 3; // Default fallback
        }
    }

    private void CalculateAllMetrics(bool playerWon)
    {
        starsEarned = CalculateStarRating(playerWon);
        CalculateRewards();
    }

    // ==================== STAR RATING CALCULATION ====================
    
    private int CalculateStarRating(bool playerWon)
    {
        if (!playerWon || !completedAllPhases || remainingHearts <= 0)
            return 0;

        // Check time-based star rating
        int timeStars = CalculateTimeBasedStars();
        
        // Check performance-based star rating
        int performanceStars = CalculatePerformanceBasedStars();
        
        Debug.Log($"Time Stars: {timeStars}, Performance Stars: {performanceStars}");
        
        // Return the lower of the two (both conditions must be met)
        return Mathf.Min(timeStars, performanceStars);
    }

    private int CalculateTimeBasedStars()
    {
        if (completionTime <= THREE_STAR_TIME && remainingHearts >= 3)
            return 3;
        else if (completionTime <= TWO_STAR_TIME && remainingHearts >= 2)
            return 2;
        else if (remainingHearts >= 1)
            return 1;
        else
            return 0;
    }

    private int CalculatePerformanceBasedStars()
    {
        int phase1Score = 0;
        int phase2Score = 0;
        int phase3Score = 0;
        
        // Phase 1: Allergen Collection (9 total)
        if (allergensCollected == MAX_ALLERGENS) phase1Score = 3;
        else if (allergensCollected >= 7) phase1Score = 2;
        else if (allergensCollected >= 5) phase1Score = 1;
        
        // Phase 2: Wagon Hits
        if (wagonHits <= MAX_WAGON_HITS_FOR_3_STARS) phase2Score = 3;
        else if (wagonHits <= MAX_WAGON_HITS_FOR_2_STARS) phase2Score = 2;
        else if (wagonHits <= MAX_WAGON_HITS_FOR_1_STAR) phase2Score = 1;
        
        // Phase 3: Combo Multiplier
        if (maxComboAchieved >= MAX_COMBO_FOR_3_STARS) phase3Score = 3;
        else if (maxComboAchieved >= MAX_COMBO_FOR_2_STARS) phase3Score = 2;
        else if (maxComboAchieved >= MAX_COMBO_FOR_1_STAR) phase3Score = 1;
        
        // Calculate average performance (round up)
        int totalScore = phase1Score + phase2Score + phase3Score;
        int averageScore = Mathf.CeilToInt(totalScore / 3f);
        
        Debug.Log($"Performance Scoring: Phase1={phase1Score}, Phase2={phase2Score}, Phase3={phase3Score}, Average={averageScore}");
        
        return Mathf.Clamp(averageScore, 0, 3);
    }

    // ==================== REWARD CALCULATION ====================
    
    private void CalculateRewards()
    {
        // Base rewards based on stars
        switch (starsEarned)
        {
            case 3: // Perfect
                baseCoins = 2000;
                baseExp = 2000;
                break;
            case 2: // Good
                baseCoins = 1200;
                baseExp = 1200;
                break;
            case 1: // OK
                baseCoins = 600;
                baseExp = 600;
                break;
            default: // Failed
                baseCoins = 100;
                baseExp = 100;
                break;
        }

        // Bonus for perfect allergen collection
        if (allergensCollected == MAX_ALLERGENS)
        {
            baseCoins += 500;
            baseExp += 500;
            Debug.Log("Bonus: Perfect allergen collection +500");
        }

        // Bonus for no wagon hits
        if (wagonHits == 0)
        {
            baseCoins += 300;
            baseExp += 300;
            Debug.Log("Bonus: No wagon hits +300");
        }

        // Bonus for high combo multiplier
        if (maxComboAchieved >= MAX_COMBO_FOR_3_STARS)
        {
            baseCoins += 400;
            baseExp += 400;
            Debug.Log("Bonus: Max combo +400");
        }
        else if (maxComboAchieved >= MAX_COMBO_FOR_2_STARS)
        {
            baseCoins += 200;
            baseExp += 200;
            Debug.Log("Bonus: Good combo +200");
        }

        // Bonus for remaining hearts
        int heartBonus = (remainingHearts - 1) * 100;
        baseCoins += heartBonus;
        baseExp += heartBonus;
        Debug.Log($"Bonus: {remainingHearts} hearts remaining +{heartBonus}");

        // Score bonus (10% of final score)
        int scoreBonus = Mathf.FloorToInt(finalScore * 0.1f);
        Debug.Log($"Bonus: Score bonus (10% of {finalScore}) +{scoreBonus}");
        
        totalCoins = baseCoins + scoreBonus;
        totalExp = baseExp + scoreBonus;
        
        Debug.Log($"Final Rewards: Coins={totalCoins}, Exp={totalExp}, Stars={starsEarned}");
    }

    // ==================== UI ANIMATION ====================
    
    private IEnumerator GameEndSequence()
    {
        Debug.Log("Starting game end sequence...");
        
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("Animating stars...");
        yield return StartCoroutine(AnimateStars());

        Debug.Log("Animating counting numbers...");
        yield return StartCoroutine(AnimateCountingNumbers());

        isCountingAnimationComplete = true;
        Debug.Log("Counting animation complete, showing buttons...");

        if (buttonContainer != null)
        {
            buttonContainer.SetActive(true);
            Debug.Log("Button container activated");
            
            // Disable home button on lose
            if (!playerWon && homeButton != null)
            {
                homeButton.interactable = false;
                Debug.Log("Home button disabled on lose");
            }
        }
        else
        {
            Debug.LogWarning("buttonContainer is null!");
        }
        
        countAnimationCoroutine = null;
    }

    private IEnumerator AnimateCountingNumbers()
    {
        if (pointsText == null || coinsText == null || expText == null || timeText == null)
        {
            Debug.LogError("One or more UI text references are null!");
            yield break;
        }

        // Initialize all values to 0
        pointsText.text = "0";
        coinsText.text = "0";
        expText.text = "0";
        timeText.text = "00:00";

        yield return new WaitForSecondsRealtime(0.3f);

        float elapsedTime = 0f;
        int lastIntegerValue = 0;

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Animate all values
            float currentScore = Mathf.Lerp(0, finalScore, smoothProgress);
            float currentCoins = Mathf.Lerp(0, totalCoins, smoothProgress);
            float currentExp = Mathf.Lerp(0, totalExp, smoothProgress);
            float currentTime = Mathf.Lerp(0, completionTime, smoothProgress);

            // Play tick sound on score increase
            int currentInteger = Mathf.FloorToInt(currentScore);
            if (currentInteger > lastIntegerValue && countTickSound != null && countAudioSource != null)
            {
                countAudioSource.PlayOneShot(countTickSound);
                lastIntegerValue = currentInteger;
            }

            // Update UI
            pointsText.text = Mathf.FloorToInt(currentScore).ToString("N0");
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");
            timeText.text = FormatTime(currentTime);

            yield return null;
        }

        // Set final values
        pointsText.text = finalScore.ToString("N0");
        coinsText.text = totalCoins.ToString("N0");
        expText.text = totalExp.ToString("N0");
        timeText.text = FormatTime(completionTime);

        // Play complete sound
        if (countCompleteSound != null && countAudioSource != null)
        {
            countAudioSource.PlayOneShot(countCompleteSound);
        }
        
        Debug.Log("Counting animation complete!");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private IEnumerator AnimateStars()
    {
        if (starsContainer == null || starsAnimator == null)
        {
            Debug.LogError("Stars container or animator not assigned!");
            yield break;
        }

        starsContainer.SetActive(true);

        yield return new WaitForSecondsRealtime(0.3f);

        starsAnimator.SetInteger(starParameter, 0);
        starsAnimator.Play("Default", -1, 0f);

        yield return null;

        starsAnimator.SetInteger(starParameter, starsEarned);
        starsAnimator.Update(0f);

        if (starsEarned > 0)
        {
            string triggerName = $"Show{starsEarned}Star" + (starsEarned > 1 ? "s" : "");
            starsAnimator.SetTrigger(triggerName);
            Debug.Log($"Playing star animation trigger: {triggerName}");
        }

        yield return new WaitForSecondsRealtime(1f);
    }

    // ==================== PANEL FADE ANIMATION ====================
    
    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
        if (panelCanvasGroup == null) yield break;
        
        panelCanvasGroup.alpha = startAlpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;
    }

    // ==================== HELPER METHODS ====================
    
    private void SetupUI(bool playerWon)
    {
        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
        }
        else
        {
            Debug.LogWarning("resultBackground is null!");
        }

        HandleCharacterAnimation(playerWon, starsEarned);
        HandleBackgroundMusic(playerWon && starsEarned > 0);

        if (!playerWon)
        {
            HandleLose();
        }
        else
        {
            HandleWin();
        }

        HandleKeyUnlockedObject(playerWon);
    }

    private void UpdateKeyImageDisplay()
    {
        if (KeyImageunlocking != null)
        {
            // Only show if:
            // 1. Summary is active
            // 2. Key was collected this session
            // 3. Player earned at least 2 stars
            // 4. Player won
            bool shouldShowKeyImage = gameSummaryParent != null && 
                                     gameSummaryParent.activeSelf &&
                                     keyWasCollected && 
                                     starsEarned >= 2 &&
                                     playerWon;
            
            KeyImageunlocking.SetActive(shouldShowKeyImage);
            
            Debug.Log($"KeyImageunlocking: {(shouldShowKeyImage ? "SHOWN" : "HIDDEN")} " +
                     $"- SummaryActive: {(gameSummaryParent != null ? gameSummaryParent.activeSelf : false)} " +
                     $"- KeyCollected: {keyWasCollected} " +
                     $"- Stars: {starsEarned} " +
                     $"- PlayerWon: {playerWon}");
        }
    }

    private void HideStarsWhenShowingSummary()
    {
        foreach (GameObject star in starsToHide)
        {
            if (star != null && star.activeSelf)
            {
                star.SetActive(false);
            }
        }
    }

    private void HandleKeyUnlockedObject(bool playerWon)
    {
        if (keyUnlockedObject == null) return;

        keyUnlockedObject.SetActive(false);
        bool shouldShowKey = false;

        if (questManager != null && playerWon && starsEarned >= 2)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                if (quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress)
                {
                    shouldShowKey = true;
                    isFirstTimeCompletion = true;
                    Debug.Log("Showing key unlocked object - first time completion!");
                }
            }
        }

        keyUnlockedObject.SetActive(shouldShowKey);
    }

    private void HandleCharacterAnimation(bool playerWon, int stars)
    {
        if (characterAnimator == null) 
        {
            Debug.LogError("CharacterAnimator is null! Cannot play animation.");
            return;
        }

        // Set animator to work with paused time
        characterAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        
        // Reset all animation parameters first
        characterAnimator.SetBool(danceParameter, false);
        characterAnimator.SetBool(thinkParameter, false);
        
        // Force update to ensure reset takes effect
        characterAnimator.Update(0f);
        
        Debug.Log($"=== SETTING CHARACTER ANIMATION ===");
        Debug.Log($"playerWon: {playerWon}, stars: {stars}");
        
        // WIN condition: player won AND at least 1 star
        if (playerWon && stars > 0)
        {
            characterAnimator.SetBool(danceParameter, true);
            Debug.Log($"Set {danceParameter} = TRUE (WIN with {stars} stars)");
        }
        // LOSE or 0 stars: show thinking
        else
        {
            characterAnimator.SetBool(thinkParameter, true);
            Debug.Log($"Set {thinkParameter} = TRUE (LOSE or 0 stars)");
        }
        
        // Force another update to apply immediately
        characterAnimator.Update(0f);
        
        // Debug check
        bool danceValue = characterAnimator.GetBool(danceParameter);
        bool thinkValue = characterAnimator.GetBool(thinkParameter);
        Debug.Log($"After setting - Dance: {danceValue}, Think: {thinkValue}");
    }

    private void ResetCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            // Reset all animation parameters
            characterAnimator.SetBool(danceParameter, false);
            characterAnimator.SetBool(thinkParameter, false);
            
            // Restore normal update mode
            characterAnimator.updateMode = AnimatorUpdateMode.Normal;
            characterAnimator.Update(0f);
            
            Debug.Log("Character animation reset to normal");
        }
    }

    private void DisableObjectsOnGameEnd()
    {
        foreach (GameObject obj in objectsToDisableOnGameEnd)
        {
            if (obj != null && obj.activeSelf)
            {
                // Skip disabling if this is the background music source
                if (backgroundMusicSource != null && obj == backgroundMusicSource.gameObject)
                {
                    Debug.Log($"Skipping disable of background music: {obj.name}");
                    continue;
                }

                obj.SetActive(false);
            }
        }

        // Also disable UI controls canvas if assigned
        if (uiControlsCanvas != null && uiControlsCanvas.activeSelf)
        {
            uiControlsCanvas.SetActive(false);
        }
    }

    private void SetCameraBlendToCut()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        }
    }

    private void RestoreOriginalCameraBlend()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend = originalBlendDefinition;
        }
    }

    private void SwitchToGameEndCameraWithCut()
    {
        if (gameEndVirtualCamera != null)
        {
            SetCameraBlendToCut();

            if (playerFollowCamera != null)
            {
                playerFollowCamera.Priority = 0;
                playerFollowCamera.gameObject.SetActive(false);
            }

            gameEndVirtualCamera.gameObject.SetActive(true);
            gameEndVirtualCamera.Priority = gameEndCameraPriority;
            Debug.Log("Switched to game end camera with CUT blend");
        }
        else
        {
            Debug.LogWarning("Game end virtual camera not found!");
        }
    }

    private void SwitchToPlayerCameraWithBlend()
    {
        if (playerFollowCamera != null)
        {
            // Don't set to cut here - use the restored blend
            if (gameEndVirtualCamera != null)
            {
                gameEndVirtualCamera.Priority = 0;
                gameEndVirtualCamera.gameObject.SetActive(false);
            }

            playerFollowCamera.gameObject.SetActive(true);
            playerFollowCamera.Priority = playerCameraPriority;

            if (cinemachineBrain != null)
            {
                cinemachineBrain.ManualUpdate();
            }
            
            Debug.Log("Switched to player camera with blend");
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        if (playerController != null && resultCharacterSpawnPoint != null)
        {
            playerController.transform.position = resultCharacterSpawnPoint.position;
            playerController.transform.rotation = resultCharacterSpawnPoint.rotation;
            Debug.Log($"Player teleported to result point: {resultCharacterSpawnPoint.position}");
        }
        else
        {
            Debug.LogWarning("Player controller or result spawn point not found!");
        }
    }

    private void TeleportPlayerToStartingPoint()
    {
        if (playerController != null && startingPoint != null)
        {
            playerController.transform.position = startingPoint.position;
            playerController.transform.rotation = startingPoint.rotation;
            Debug.Log($"Player teleported to starting point: {startingPoint.position}");
        }
    }

    private void HandleLose()
    {
        foreach (GameObject obj in objectsToEnableOnLose)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    private void HandleWin()
    {
        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    private void EnableObjectsOnHomeButton()
    {
        foreach (GameObject obj in objectsToEnableOnHomeButton)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    private void OnButtonClicked()
    {
        DisableWinLoseObjects();
        EnablePlayerControl();
    }

    public void ResetGameEndState()
    {
        ResetCharacterAnimation();
        SwitchToPlayerCameraWithBlend();
        DisableWinLoseObjects();

        if (starsAnimator != null) starsAnimator.SetInteger(starParameter, 0);
        if (starsContainer != null) starsContainer.SetActive(false);

        if (keyUnlockedObject != null && keyUnlockedObject.activeSelf)
            keyUnlockedObject.SetActive(false);
            
        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
            KeyImageunlocking.SetActive(false);
            
        if (keyUnlockedCanvas != null && keyUnlockedCanvas.activeSelf)
            keyUnlockedCanvas.SetActive(false);

        if (pointsText != null) pointsText.text = "0";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";
        if (timeText != null) timeText.text = "00:00";

        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (gameSummaryParent != null) gameSummaryParent.SetActive(false);
        
        RestoreOriginalCameraBlend();
        Debug.Log("Game end state reset");
    }

    private void DisableWinLoseObjects()
    {
        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null && obj.activeSelf) obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToEnableOnLose)
        {
            if (obj != null && obj.activeSelf) obj.SetActive(false);
        }
    }

    private void EnablePlayerControl()
    {
        if (playerController != null) 
        {
            playerController.enabled = true;
            Debug.Log("Player control enabled");
        }
    }

    public void ResetKingdom4Game()
    {
        ResetObjectsToInitialState();
        
        // Reset timer
        if (gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }
        
        // Reset AllerthriaGameManager
        if (gameManager != null)
        {
            gameManager.hasScroll = false;
            gameManager.collectedAllergens.Clear();
            gameManager.hasKey = false;
            gameManager.StartPhase(AllerthriaGameManager.GamePhase.ScrollQuest);
        }

        // Reset Kingdom4ScoreManager
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        
        // Reset PlayerHealthManager
        if (healthManager != null)
        {
            healthManager.ResetHealth();
        }
        
        // Reset key collection script
        if (collectKeyScript != null)
        {
            collectKeyScript.ForceFullReset();
        }
        
        // Reset key tracking flags
        keyWasCollected = false;
        keySavedToDatabase = false;
        coinsAddedToDatabase = false;
    }

    public void ResetObjectsToInitialState()
    {
        foreach (var kvp in initialTransformData)
        {
            GameObject obj = kvp.Key;
            TransformData data = kvp.Value;

            if (obj != null)
            {
                obj.transform.position = data.position;
                obj.transform.rotation = data.rotation;
                obj.transform.localScale = data.localScale;

                // Reset Rigidbody if exists
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }
    }

    // ==================== OCR SCANNER KEY METHODS ====================
    
    /// <summary>
    /// Checks if the player already has the OCR Scanner Key in GameData
    /// </summary>
    private bool CheckIfPlayerHasOCRScannerKey()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is null!");
            return false;
        }

        if (GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogWarning("CurrentGameData is null!");
            return false;
        }
        
        bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
        Debug.Log($"CheckIfPlayerHasOCRScannerKey: {hasKey}");
        return hasKey;
    }
    
    /// <summary>
    /// Saves the OCR Scanner Key to GameData (sets HasOCRScannerKey to true)
    /// </summary>
    private void SaveOCRScannerKeyToGameData()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("GameDataManager.Instance is null! Cannot save key.");
            return;
        }

        if (GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogError("CurrentGameData is null! Cannot save key.");
            return;
        }

        // Save the OCR Scanner key - this sets ocrScannerKeyCollected to true
        GameDataManager.Instance.CurrentGameData.CollectOCRScannerKey();
        
        // Also save using the generic method for consistency
        GameDataManager.Instance.CurrentGameData.CollectKingdomKey("ocr");
        
        // Make sure to save the GameData
        GameDataManager.Instance.SaveGameData();
        
        Debug.Log("✓ OCR Scanner key successfully saved to GameData! HasOCRScannerKey is now TRUE");
        
        keySavedToDatabase = true;
    }

    private void AddCoinsToDatabase()
    {
        if (coinsAddedToDatabase || GameDataManager.Instance == null) return;

        GameDataManager.Instance.CurrentGameData.nutriCoins += totalCoins;
        GameDataManager.Instance.SaveGameData();
        coinsAddedToDatabase = true;

        Debug.Log($"Added {totalCoins} coins to database");
    }

    // ==================== HOME BUTTON HANDLER WITH OCR KEY FLOW ====================
    
    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete || isProcessingButton) 
        {
            Debug.Log("Counting animation not complete yet or button already processing, ignoring click");
            return;
        }

        // Don't proceed if on lose screen (home button disabled)
        if (!playerWon)
        {
            Debug.Log("Home button is disabled on lose screen");
            return;
        }

        isProcessingButton = true;
        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (homeButton != null)
            homeButton.interactable = false;

        // Check if player already HAS the OCR Scanner Key in saved data
        bool hasOCRScannerKey = CheckIfPlayerHasOCRScannerKey();
        Debug.Log($"OCR Scanner Key status from GameData: {(hasOCRScannerKey ? "TRUE" : "FALSE")}");

        // Check if key was collected THIS SESSION (but not saved yet)
        bool keyCollectedThisSession = keyWasCollected && !keySavedToDatabase;
        
        // DECISION FLOW:
        // 1. If key was collected this session AND player doesn't have it in GameData -> Show unlock canvas
        // 2. If player already has key in GameData -> Return to game (no canvas)
        // 3. If no key at all -> Return to game (no canvas)
        
        if (keyCollectedThisSession && !hasOCRScannerKey)
        {
            // KEY WAS COLLECTED THIS SESSION AND NOT IN GAMEDATA YET - SHOW UNLOCK CANVAS
            Debug.Log("Key collected this session and not in GameData - showing KeyUnlockedCanvas");
            StartCoroutine(ReturnToPreSummaryStateAndShowKeyUnlockCanvas());
        }
        else if (hasOCRScannerKey)
        {
            // PLAYER ALREADY HAS KEY IN GAMEDATA - NO CANVAS
            Debug.Log("Player already has OCR Scanner Key in GameData - returning to game (no canvas)");
            StartCoroutine(ReturnToPreSummaryStateOnly());
        }
        else
        {
            // NO KEY AT ALL - NO CANVAS
            Debug.Log("No key collected - returning to game fully");
            StartCoroutine(ReturnToGameFully());
        }
    }

    // Return to pre-summary state AND show key unlock canvas
    private IEnumerator ReturnToPreSummaryStateAndShowKeyUnlockCanvas()
    {
        Debug.Log("Returning to pre-summary state and showing key unlock canvas");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
        
        // Stop character animation
        ResetCharacterAnimation();
        
        // Restore background music
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Switch back to player camera with blend
        SwitchToPlayerCameraWithBlend();
        
        // Enable player control and UI
        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);
        
        // Reset game state
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();
        
        // Reset flags
        isGameOver = false;
        isSummaryActive = false;
        
        Debug.Log($"Player at start position, input enabled");
        
        // Small delay before showing key unlock canvas
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Now show KeyUnlockedCanvas (unlockdocrcanvas)
        if (keyUnlockedController != null)
        {
            Debug.Log("Showing KeyUnlockedCanvas via controller");
            keyUnlockedController.ShowKeyUnlockedCanvas(OnKeyUnlockCanvasContinue);
        }
        else if (keyUnlockedCanvas != null)
        {
            Debug.Log("Activating KeyUnlockedCanvas GameObject directly");
            keyUnlockedCanvas.SetActive(true);
            
            // Make sure the continue button is set up
            if (continueKeyButton != null)
            {
                // Remove any existing listeners to avoid duplicates
                continueKeyButton.onClick.RemoveAllListeners();
                continueKeyButton.onClick.AddListener(OnContinueKeyButtonClicked);
            }
        }
        else
        {
            Debug.LogError("KeyUnlockedCanvas GameObject is not assigned!");
            FinishHomeButtonSequence();
        }
    }

    // Callback for when Continue button in key unlock canvas is clicked (via controller)
    private void OnKeyUnlockCanvasContinue()
    {
        Debug.Log("Key unlock canvas continue callback received - SAVING OCR SCANNER KEY TO GAMEDATA");
        
        // Save the OCR Scanner key to GameData (sets HasOCRScannerKey to true)
        SaveOCRScannerKeyToGameData();
        
        // Hide key unlock canvas
        if (keyUnlockedController != null)
        {
            keyUnlockedController.ForceHide();
        }
        else if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }
        
        // Finish the home button sequence
        FinishHomeButtonSequence();
    }

    // Handle ContinueKeyButton click directly - THIS IS THE BUTTON INSIDE unlockdocrcanvas
    public void OnContinueKeyButtonClicked()
    {
        Debug.Log("ContinueKeyButton clicked directly - SAVING OCR SCANNER KEY TO GAMEDATA");
        
        // Save the OCR Scanner key to GameData (sets HasOCRScannerKey to true)
        SaveOCRScannerKeyToGameData();
        
        // Hide KeyUnlockedCanvas
        if (keyUnlockedController != null)
        {
            keyUnlockedController.ForceHide();
        }
        else if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }
        
        // Finish the sequence
        FinishHomeButtonSequence();
    }

    // Return to pre-summary state ONLY (no animation)
    private IEnumerator ReturnToPreSummaryStateOnly()
    {
        Debug.Log("Returning to pre-summary state only (no animation)");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
        
        // Stop character animation
        ResetCharacterAnimation();
        
        // Restore background music
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Switch back to player camera with blend
        SwitchToPlayerCameraWithBlend();
        
        // Enable player control and UI
        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);
        
        // Reset game state
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();
        
        // Reset flags
        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;
        
        if (homeButton != null)
            homeButton.interactable = true;
        
        Debug.Log($"Player at start position, input enabled");
        
        FinishHomeButtonSequence();
    }

    // Fully return to game (for no key)
    private IEnumerator ReturnToGameFully()
    {
        Debug.Log("Returning to game fully");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
        
        // Stop character animation
        ResetCharacterAnimation();
        
        // Restore background music
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Switch back to player camera with blend
        SwitchToPlayerCameraWithBlend();
        
        // Enable player control and UI
        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);
        
        // Reset game state
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();
        
        // Reset flags
        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;
        
        if (homeButton != null)
            homeButton.interactable = true;
        
        Debug.Log($"Player at start position, input enabled");
        
        FinishHomeButtonSequence();
    }

    private void FinishHomeButtonSequence()
    {
        Debug.Log("Home button sequence complete");
        
        // Reset processing flag
        isProcessingButton = false;
        
        if (homeButton != null)
            homeButton.interactable = true;
            
        if (restartButton != null)
            restartButton.interactable = true;
    }

    private void OnRestartClicked()
    {
        if (!isCountingAnimationComplete || isProcessingButton) 
        {
            Debug.Log("Counting animation not complete yet or button already processing, ignoring click");
            return;
        }

        isProcessingButton = true;
        PlayButtonClickSound();
        AddCoinsToDatabase();

        // Save key if it was collected (only if we're doing a restart that saves)
        if (keyWasCollected && !keySavedToDatabase)
        {
            SaveOCRScannerKeyToGameData();
        }

        if (restartButton != null)
            restartButton.interactable = false;

        if (completeRestartOnConfirm)
        {
            Debug.Log("Complete restart requested - reloading scene");
            StartCoroutine(CompleteRestartGame());
        }
        else
        {
            StartCoroutine(SoftRestartGame());
        }
    }

    private IEnumerator SoftRestartGame()
    {
        Debug.Log("Starting soft restart...");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
        
        // Reset everything
        ResetCharacterAnimation();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;
        
        SwitchToPlayerCameraWithBlend();
        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);
        
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();
        
        // Reset flags
        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;
        
        if (restartButton != null)
            restartButton.interactable = true;
        
        Debug.Log("Game soft restarted");
    }

    private IEnumerator CompleteRestartGame()
    {
        Debug.Log("Starting complete game restart...");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
        
        // Reset persistent data
        ResetPersistentData();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Reload scene
        ReloadCurrentScene();
    }

    private void ResetPersistentData()
    {
        Debug.Log("Resetting persistent data...");
        
        // Reset all keys globally
        if (collectKeyScript != null)
        {
            // Use the static method if available
            var method = collectKeyScript.GetType().GetMethod("GlobalResetAllKeys");
            if (method != null && method.IsStatic)
            {
                method.Invoke(null, null);
            }
        }
        
        // Reset key tracking
        keyWasCollected = false;
        keySavedToDatabase = false;
        coinsAddedToDatabase = false;
        
        Debug.Log("Persistent data reset complete");
    }

    private void ReloadCurrentScene()
    {
        Debug.Log("Reloading scene for complete restart...");
        
        string sceneName = string.IsNullOrEmpty(sceneToReload) ? 
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : sceneToReload;
        
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void PlayLobbyMusic()
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play lobby music.");
            return;
        }

        if (lobbyMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = lobbyMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();

            Debug.Log($"Changed to lobby music: {lobbyMusicClip.name}");
        }
        else
        {
            Debug.LogWarning("Lobby music clip not assigned!");
        }
    }

    private void PlayRestartMusic()
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play restart music.");
            return;
        }

        if (restartMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = restartMusicClip;
            backgroundMusicSource.loop = false;
            backgroundMusicSource.Play();

            Debug.Log($"Changed to restart music: {restartMusicClip.name}");
        }
    }

    private void HandleBackgroundMusic(bool isWin)
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("Background music source is null. Skipping music change.");
            return;
        }

        AudioClip musicToPlay = isWin ? winMusicClip : loseMusicClip;

        if (musicToPlay != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = musicToPlay;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();

            Debug.Log($"Changed background music to: {(isWin ? "WIN" : "LOSE")} music");
        }
        else
        {
            Debug.LogWarning($"No {(isWin ? "win" : "lose")} music clip assigned!");
        }
    }

    private void RestoreBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null)
        {
            // Restore to full volume
            backgroundMusicSource.volume = 1f;
        }
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && countAudioSource != null)
        {
            countAudioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
        else
        {
            // Fallback to AudioHandler if exists
            AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
            if (audioHandler != null)
            {
                System.Reflection.MethodInfo method = audioHandler.GetType().GetMethod("PlayButtonClick");
                if (method != null)
                    method.Invoke(audioHandler, null);
            }
        }
    }

    // ==================== PUBLIC API ====================
    
    public void HandleKingdom4Complete()
    {
        try
        {
            Debug.Log("=== HANDLE KINGDOM 4 COMPLETE ===");
            ShowGameEndScreen(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in HandleKingdom4Complete: {e.Message}\n{e.StackTrace}");
        }
    }

    public void HandleKingdom4GameOver()
    {
        try
        {
            Debug.Log("=== HANDLE KINGDOM 4 GAME OVER ===");
            ShowGameEndScreen(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in HandleKingdom4GameOver: {e.Message}\n{e.StackTrace}");
        }
    }

    public void TriggerKingdom4Complete() => HandleKingdom4Complete();
    public void TriggerKingdom4GameOver() => HandleKingdom4GameOver();

    // New method for timer integration
    public void OnGameStarted()
    {
        Debug.Log("Game started notification received in GameEndManager");
        gameStartTime = Time.time;
    }

    // ==================== GETTERS ====================
    
    public int GetStarsEarned() => starsEarned;
    public int GetTotalCoins() => totalCoins;
    public int GetTotalExp() => totalExp;
    public int GetFinalScore() => finalScore;
    public bool IsFirstTimeCompletion() => isFirstTimeCompletion;
    public int GetAllergensCollected() => allergensCollected;
    public int GetWagonHits() => wagonHits;
    public float GetCompletionTime() => completionTime;
    public int GetRemainingHearts() => remainingHearts;
    public Transform GetResultSpawnPoint() => resultCharacterSpawnPoint;

    // New getters for key state
    public bool WasKeyCollectedThisSession() => keyWasCollected;
    public bool IsKeySavedToDatabase() => keySavedToDatabase;

    // ==================== DEBUG METHODS ====================
    
    [ContextMenu("Test Scoring System")]
    public void TestScoringSystem()
    {
        // Test 3-star scenario
        Debug.Log("=== TEST 3-STAR SCENARIO ===");
        completionTime = 550f; // 9:10
        remainingHearts = 4;
        allergensCollected = 9;
        wagonHits = 0;
        maxComboAchieved = 8;
        finalScore = 2500;
        completedAllPhases = true;
        
        int stars = CalculateStarRating(true);
        CalculateRewards();
        Debug.Log($"Stars: {stars}, Coins: {totalCoins}, Exp: {totalExp}");
        
        // Test 2-star scenario
        Debug.Log("\n=== TEST 2-STAR SCENARIO ===");
        completionTime = 800f; // 13:20
        remainingHearts = 2;
        allergensCollected = 7;
        wagonHits = 1;
        maxComboAchieved = 5;
        finalScore = 1800;
        
        stars = CalculateStarRating(true);
        CalculateRewards();
        Debug.Log($"Stars: {stars}, Coins: {totalCoins}, Exp: {totalExp}");
        
        // Test 1-star scenario
        Debug.Log("\n=== TEST 1-STAR SCENARIO ===");
        completionTime = 1000f; // 16:40
        remainingHearts = 1;
        allergensCollected = 5;
        wagonHits = 3;
        maxComboAchieved = 3;
        finalScore = 1200;
        
        stars = CalculateStarRating(true);
        CalculateRewards();
        Debug.Log($"Stars: {stars}, Coins: {totalCoins}, Exp: {totalExp}");
    }

    [ContextMenu("Test Show Game Summary")]
    public void TestShowGameSummary()
    {
        Debug.Log("=== TESTING GAME SUMMARY ===");
        
        // Set test data for 3 stars
        completionTime = 550f;
        remainingHearts = 4;
        allergensCollected = 9;
        wagonHits = 0;
        maxComboAchieved = 8;
        finalScore = 2500;
        completedAllPhases = true;
        
        ShowGameEndScreen(true);
    }

    [ContextMenu("Test OCR Scanner Key Saving")]
    public void TestOCRScannerKeySaving()
    {
        Debug.Log("=== TESTING OCR SCANNER KEY SAVING ===");
        isFirstTimeCompletion = true;
        SaveOCRScannerKeyToGameData();
        
        // Verify the save
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
            Debug.Log($"After save - HasOCRScannerKey: {hasKey}");
        }
    }

    [ContextMenu("Test Key Collection Flow")]
    public void TestKeyCollectionFlow()
    {
        Debug.Log("=== TESTING KEY COLLECTION FLOW ===");
        keyWasCollected = true;
        keySavedToDatabase = false;
        Debug.Log($"Set keyWasCollected=true, keySavedToDatabase={keySavedToDatabase}");
    }

    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = true;
            remainingHearts = 4;
            ShowGameEndScreen(true);
        }
    }

    [ContextMenu("Test Win without Key")]
    public void TestWinWithoutKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = false;
            remainingHearts = 4;
            ShowGameEndScreen(true);
        }
    }

    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        if (!isGameOver && !isSummaryActive)
        {
            ShowGameEndScreen(false);
        }
    }

    [ContextMenu("Check OCR Scanner Key Status")]
    public void CheckOCRScannerKeyStatus()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogError("GameDataManager or CurrentGameData is null!");
            return;
        }
        
        bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
        Debug.Log($"OCR Scanner Key status: {(hasKey ? "COLLECTED" : "NOT COLLECTED")}");
        Debug.Log($"Key collected this session: {keyWasCollected}");
        Debug.Log($"Key saved to database: {keySavedToDatabase}");
    }

    [ContextMenu("Collect OCR Scanner Key (Test)")]
    public void TestCollectOCRScannerKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.CollectOCRScannerKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
            Debug.Log("OCR Scanner Key collected and saved to GameData");
        }
    }

    [ContextMenu("Reset OCR Scanner Key (Test)")]
    public void TestResetOCRScannerKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.ResetOCRScannerKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = false;
            keyWasCollected = false;
            Debug.Log("OCR Scanner Key reset in GameData");
        }
    }

    [ContextMenu("Check UI References")]
    public void CheckUIReferences()
    {
        Debug.Log("=== CHECKING UI REFERENCES ===");
        Debug.Log($"gameSummaryParent: {(gameSummaryParent != null ? "SET" : "NULL")}");
        Debug.Log($"pointsText: {(pointsText != null ? "SET" : "NULL")}");
        Debug.Log($"coinsText: {(coinsText != null ? "SET" : "NULL")}");
        Debug.Log($"expText: {(expText != null ? "SET" : "NULL")}");
        Debug.Log($"timeText: {(timeText != null ? "SET" : "NULL")}");
        Debug.Log($"buttonContainer: {(buttonContainer != null ? "SET" : "NULL")}");
        Debug.Log($"resultBackground: {(resultBackground != null ? "SET" : "NULL")}");
        Debug.Log($"starsContainer: {(starsContainer != null ? "SET" : "NULL")}");
        Debug.Log($"starsAnimator: {(starsAnimator != null ? "SET" : "NULL")}");
        Debug.Log($"panelCanvasGroup: {(panelCanvasGroup != null ? "SET" : "NULL")}");
        Debug.Log($"keyUnlockedCanvas: {(keyUnlockedCanvas != null ? "SET" : "NULL")}");
        Debug.Log($"continueKeyButton: {(continueKeyButton != null ? "SET" : "NULL")}");
        Debug.Log($"keyUnlockedController: {(keyUnlockedController != null ? "SET" : "NULL")}");
        Debug.Log($"KeyImageunlocking: {(KeyImageunlocking != null ? "SET" : "NULL")}");
        Debug.Log("==============================");
    }
}