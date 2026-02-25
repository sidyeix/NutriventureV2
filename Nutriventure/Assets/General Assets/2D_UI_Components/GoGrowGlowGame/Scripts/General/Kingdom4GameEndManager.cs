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
    private string[] starStateNames = new string[] { "Empty", "Star1", "Star2", "Star3" };
    private int currentStars = 0;

    [Header("Stars to Hide")]
    [SerializeField] private List<GameObject> starsToHide = new List<GameObject>();

    [Header("Game Summary UI")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text starsEarnedText; // Added for text display
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
    [SerializeField] private GameObject keyUnlockedCanvas;
    [SerializeField] private Button continueKeyButton;
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

    // Time thresholds (in seconds)
    private const float THREE_STAR_TIME_MAX = 600f;    // Less than 10 minutes
    private const float TWO_STAR_TIME_MAX = 900f;      // Less than 15 minutes
    // 15 minutes or more = 1 star max

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
    
    // Counting animation values
    private float currentTimePlayed = 0f;
    private int currentGameScore = 0;
    private int targetCoinsEarned = 0;
    private float elapsedAnimationTime = 0f;

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
        
        // Reset star animator
        ResetStarAnimator();
    }

    private void ResetStarAnimator()
    {
        if (starsAnimator != null)
        {
            Debug.Log("Resetting star animator...");
            starsAnimator.SetInteger(starParameter, 0);
            starsAnimator.updateMode = AnimatorUpdateMode.Normal;
            starsAnimator.Update(0f);
            Debug.Log("Star animator reset to default state");
        }
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
        UpdateKeyImageDisplay();
        
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
        currentStars = starsEarned;
        CalculateRewards();
    }

    // ==================== STAR RATING CALCULATION ====================
    // Logic:
    // - Stars based on time AND hearts remaining
    // - Time caps maximum possible stars
    // - Key is awarded only for 2 or 3 stars
    
    private int CalculateStarRating(bool playerWon)
    {
        if (!playerWon || remainingHearts <= 0)
            return 0;

        // First, determine the maximum possible stars based on time
        int maxStarsByTime;
        if (completionTime < THREE_STAR_TIME_MAX) // Less than 10 minutes
            maxStarsByTime = 3;
        else if (completionTime < TWO_STAR_TIME_MAX) // Less than 15 minutes
            maxStarsByTime = 2;
        else // 15 minutes or more
            maxStarsByTime = 1;

        Debug.Log($"Max stars by time ({FormatTime(completionTime)}): {maxStarsByTime}");

        // Then, determine stars based on hearts remaining
        int starsByHearts;
        if (remainingHearts >= 4) // 4-5 hearts
            starsByHearts = 3;
        else if (remainingHearts >= 3) // 3 hearts
            starsByHearts = 2;
        else if (remainingHearts >= 1) // 1-2 hearts
            starsByHearts = 1;
        else // 0 hearts
            starsByHearts = 0;

        Debug.Log($"Stars by hearts ({remainingHearts} hearts): {starsByHearts}");

        // The actual stars is the LOWER of the two (both conditions must be satisfied)
        int finalStars = Mathf.Min(maxStarsByTime, starsByHearts);
        
        Debug.Log($"Final stars: {finalStars}");
        return finalStars;
    }

    // ==================== REWARD CALCULATION ====================
    // Rewards based on stars + performance bonuses
    // Key is awarded for 2 or 3 stars (handled separately)
    
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
        if (allergensCollected == 9)
        {
            baseCoins += 500;
            baseExp += 500;
            Debug.Log("Bonus: Perfect allergen collection +500");
        }
        else if (allergensCollected >= 7)
        {
            baseCoins += 300;
            baseExp += 300;
            Debug.Log("Bonus: Good allergen collection +300");
        }
        else if (allergensCollected >= 5)
        {
            baseCoins += 150;
            baseExp += 150;
            Debug.Log("Bonus: Fair allergen collection +150");
        }

        // Bonus for no wagon hits
        if (wagonHits == 0)
        {
            baseCoins += 300;
            baseExp += 300;
            Debug.Log("Bonus: No wagon hits +300");
        }
        else if (wagonHits <= 2)
        {
            baseCoins += 150;
            baseExp += 150;
            Debug.Log("Bonus: Few wagon hits +150");
        }

        // Bonus for high combo multiplier
        if (maxComboAchieved >= 8)
        {
            baseCoins += 400;
            baseExp += 400;
            Debug.Log("Bonus: Excellent combo +400");
        }
        else if (maxComboAchieved >= 5)
        {
            baseCoins += 200;
            baseExp += 200;
            Debug.Log("Bonus: Good combo +200");
        }
        else if (maxComboAchieved >= 3)
        {
            baseCoins += 100;
            baseExp += 100;
            Debug.Log("Bonus: Fair combo +100");
        }

        // Bonus for remaining hearts (beyond the minimum for star rating)
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
        yield return StartCoroutine(PlayStarAnimationWithDelay());

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

    private IEnumerator PlayStarAnimationWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        PlayStarAnimationDirect();
        yield return new WaitForSecondsRealtime(1f); // Wait for star animation to complete
    }

    private void PlayStarAnimationDirect()
    {
        if (starsAnimator != null)
        {
            Debug.Log($"=== PLAYING STAR ANIMATION DIRECT: {currentStars} stars ===");
            
            if (!starsAnimator.gameObject.activeSelf)
            {
                Debug.Log("Activating star animator GameObject");
                starsAnimator.gameObject.SetActive(true);
            }
            
            if (!starsAnimator.enabled)
            {
                Debug.Log("Enabling star animator component");
                starsAnimator.enabled = true;
            }
            
            starsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            starsAnimator.SetInteger(starParameter, 0);
            starsAnimator.Update(0f);
            
            StartCoroutine(PlayStarAnimationAfterReset());
        }
        else
        {
            Debug.LogError("Star animator is null! Cannot play animation.");
        }
    }

    private IEnumerator PlayStarAnimationAfterReset()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        
        starsAnimator.SetInteger(starParameter, currentStars);
        starsAnimator.Update(0f);
        
        int currentValue = starsAnimator.GetInteger(starParameter);
        Debug.Log($"Star parameter set to: {currentValue} (requested: {currentStars})");
        
        AnimatorStateInfo stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current animation state: {stateInfo.fullPathHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Is in transition: {starsAnimator.IsInTransition(0)}");
        
        if (stateInfo.normalizedTime == 0 && currentStars > 0)
        {
            Debug.Log("Attempting to play animation directly...");
            ForcePlayStarAnimation(currentStars);
        }
        
        yield return new WaitForSecondsRealtime(0.1f);
        stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"After 0.1s - State normalized time: {stateInfo.normalizedTime}");
    }

    private void ForcePlayStarAnimation(int stars)
    {
        if (starsAnimator != null && stars > 0 && stars <= 3)
        {
            string stateName = starStateNames[stars];
            Debug.Log($"Force playing animation state: {stateName}");
            
            starsAnimator.Play(stateName, 0, 0f);
            starsAnimator.Update(0f);
        }
    }

    private IEnumerator AnimateCountingNumbers()
    {
        Debug.Log("Starting counting animation...");
        
        if (pointsText == null || coinsText == null || expText == null || timeText == null || starsEarnedText == null)
        {
            Debug.LogError("One or more UI text references are null!");
            yield break;
        }

        // Get target values
        float targetTimePlayed = completionTime;
        int targetGameScore = finalScore;
        
        // Store these for the animation
        currentTimePlayed = targetTimePlayed;
        currentGameScore = targetGameScore;
        targetCoinsEarned = totalCoins;

        // Reset animation
        elapsedAnimationTime = 0f;
        int lastIntegerValue = 0;

        // Start with all zeros
        timeText.text = "00:00";
        pointsText.text = "0";
        coinsText.text = "0";
        expText.text = "0";
        starsEarnedText.text = "0/3";

        yield return new WaitForSecondsRealtime(0.3f);

        // Calculate how many ticks we want
        int numberOfTicks = Mathf.Clamp(targetGameScore / 50, 10, 30);
        float tickInterval = countAnimationDuration / numberOfTicks;
        float nextTickTime = 0f;
        
        Debug.Log($"Audio: Will play {numberOfTicks} ticks every {tickInterval:F2} seconds");

        // Animate all values simultaneously
        while (elapsedAnimationTime < countAnimationDuration)
        {
            elapsedAnimationTime += Time.unscaledDeltaTime;
            float progress = elapsedAnimationTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Animate time
            float currentTime = Mathf.Lerp(0, targetTimePlayed, smoothProgress);
            timeText.text = FormatTime(currentTime);

            // Animate score
            float currentScore = Mathf.Lerp(0, targetGameScore, smoothProgress);
            int currentInteger = Mathf.FloorToInt(currentScore);
            
            // Play tick sound at regular intervals
            if (elapsedAnimationTime >= nextTickTime)
            {
                if (countTickSound != null && countAudioSource != null)
                {
                    countAudioSource.Stop(); // Stop any previous sound
                    countAudioSource.PlayOneShot(countTickSound, 0.5f);
                    Debug.Log($"✓ Tick sound played at {elapsedAnimationTime:F2}s - Score: {currentInteger}");
                }
                else
                {
                    Debug.LogWarning("Count tick sound or audio source is null!");
                }
                
                nextTickTime += tickInterval;
            }
            
            pointsText.text = Mathf.FloorToInt(currentScore).ToString("N0");

            // Animate coins
            float currentCoins = Mathf.Lerp(0, targetCoinsEarned, smoothProgress);
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");
            
            // Animate exp
            float currentExp = Mathf.Lerp(0, totalExp, smoothProgress);
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");

            // Animate stars (as text)
            int currentStarsText = Mathf.FloorToInt(Mathf.Lerp(0, currentStars, smoothProgress));
            starsEarnedText.text = $"{currentStarsText}/3";

            yield return null;
        }

        // Set final values
        timeText.text = FormatTime(targetTimePlayed);
        pointsText.text = targetGameScore.ToString("N0");
        coinsText.text = targetCoinsEarned.ToString("N0");
        expText.text = totalExp.ToString("N0");
        starsEarnedText.text = $"{currentStars}/3";

        // Wait a moment for last tick to finish
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Play completion sound
        if (countCompleteSound != null && countAudioSource != null)
        {
            // Stop any ongoing tick sounds
            if (countAudioSource.isPlaying)
            {
                countAudioSource.Stop();
            }
            
            countAudioSource.PlayOneShot(countCompleteSound, 0.7f);
            Debug.Log("✓ Completion sound played");
        }
        else
        {
            Debug.LogWarning("Count complete sound or audio source is null!");
        }

        Debug.Log("Counting animation complete!");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
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
            // Only show key image if:
            // 1. Summary is active
            // 2. Key was collected this session
            // 3. Player earned at least 2 stars (2 or 3 stars)
            // 4. Player won
            bool shouldShowKeyImage = gameSummaryParent != null && 
                                     gameSummaryParent.activeSelf &&
                                     keyWasCollected && 
                                     starsEarned >= 2 && // Key awarded for 2 or 3 stars
                                     playerWon;
            
            KeyImageunlocking.SetActive(shouldShowKeyImage);
            
            Debug.Log($"KeyImageunlocking: {(shouldShowKeyImage ? "SHOWN" : "HIDDEN")} " +
                     $"- Stars: {starsEarned} (need 2+) " +
                     $"- KeyCollected: {keyWasCollected}");
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

        // Show key unlocked object only for 2 or 3 stars AND first time completion
        if (questManager != null && playerWon && starsEarned >= 2)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                if (quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress)
                {
                    shouldShowKey = true;
                    isFirstTimeCompletion = true;
                    Debug.Log("Showing key unlocked object - first time completion with 2+ stars!");
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
            // Disable character controller temporarily to allow position change
            CharacterController charController = playerController.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }
            
            playerController.transform.position = resultCharacterSpawnPoint.position;
            playerController.transform.rotation = resultCharacterSpawnPoint.rotation;
            
            // Re-enable character controller
            if (charController != null)
            {
                charController.enabled = true;
            }
            
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
            // Disable character controller temporarily to allow position change
            CharacterController charController = playerController.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }
            
            playerController.transform.position = startingPoint.position;
            playerController.transform.rotation = startingPoint.rotation;
            
            // Re-enable character controller
            if (charController != null)
            {
                charController.enabled = true;
            }
            
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

        ResetStarAnimator();
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
        if (starsEarnedText != null) starsEarnedText.text = "0/3";

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
    // Key is awarded only for 2 or 3 stars
    
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

        // Check if key was collected THIS SESSION AND player earned 2+ stars (but not saved yet)
        bool keyCollectedThisSession = keyWasCollected && !keySavedToDatabase && starsEarned >= 2;
        
        // DECISION FLOW:
        // 1. If key was collected this session, player earned 2+ stars, and doesn't have it in GameData -> Show unlock canvas
        // 2. If player already has key in GameData -> Return to game (no canvas)
        // 3. If no key or less than 2 stars -> Return to game (no canvas)
        
        if (keyCollectedThisSession && !hasOCRScannerKey)
        {
            // KEY WAS COLLECTED THIS SESSION WITH 2+ STARS AND NOT IN GAMEDATA YET - SHOW UNLOCK CANVAS
            Debug.Log("Key collected this session with 2+ stars and not in GameData - showing KeyUnlockedCanvas");
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
            // NO KEY OR LESS THAN 2 STARS - NO CANVAS
            Debug.Log("No key or less than 2 stars - returning to game fully");
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
        
        // Force teleport to starting point with controller disabled/re-enabled
        TeleportPlayerToStartingPoint();
        
        // Small delay to ensure transform applies
        yield return new WaitForSecondsRealtime(0.1f);
        
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
        
        // Force teleport to starting point with controller disabled/re-enabled
        TeleportPlayerToStartingPoint();
        
        // Small delay to ensure transform applies
        yield return new WaitForSecondsRealtime(0.1f);
        
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
        
        // Force teleport to starting point with controller disabled/re-enabled
        TeleportPlayerToStartingPoint();
        
        // Small delay to ensure transform applies
        yield return new WaitForSecondsRealtime(0.1f);
        
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

        // Save key if it was collected AND player earned 2+ stars
        if (keyWasCollected && !keySavedToDatabase && starsEarned >= 2)
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
    
    [ContextMenu("Test Star Logic")]
    public void TestStarLogic()
    {
        Debug.Log("=== TESTING STAR LOGIC ===");
        
        // Test Case 1: 5 hearts, 9 minutes → should be 3 stars
        Debug.Log("\nTest 1: 5 hearts, 9 minutes");
        completionTime = 540f;
        remainingHearts = 5;
        int stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 3)");
        
        // Test Case 2: 5 hearts, 11 minutes → should be 2 stars (time capped)
        Debug.Log("\nTest 2: 5 hearts, 11 minutes");
        completionTime = 660f;
        remainingHearts = 5;
        stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 2)");
        
        // Test Case 3: 3 hearts, 9 minutes → should be 2 stars (hearts limit)
        Debug.Log("\nTest 3: 3 hearts, 9 minutes");
        completionTime = 540f;
        remainingHearts = 3;
        stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 2)");
        
        // Test Case 4: 2 hearts, 9 minutes → should be 1 star (hearts limit)
        Debug.Log("\nTest 4: 2 hearts, 9 minutes");
        completionTime = 540f;
        remainingHearts = 2;
        stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 1)");
        
        // Test Case 5: 5 hearts, 16 minutes → should be 1 star (time capped)
        Debug.Log("\nTest 5: 5 hearts, 16 minutes");
        completionTime = 960f;
        remainingHearts = 5;
        stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 1)");
        
        // Test Case 6: 0 hearts → should be 0 stars
        Debug.Log("\nTest 6: 0 hearts");
        remainingHearts = 0;
        stars = CalculateStarRating(true);
        Debug.Log($"Result: {stars} stars (Expected: 0)");
    }

    [ContextMenu("Test Show Game Summary")]
    public void TestShowGameSummary()
    {
        Debug.Log("=== TESTING GAME SUMMARY ===");
        
        // Set test data for 3 stars
        completionTime = 540f; // 9 minutes
        remainingHearts = 5;
        allergensCollected = 9;
        wagonHits = 0;
        maxComboAchieved = 8;
        finalScore = 2500;
        completedAllPhases = true;
        keyWasCollected = true;
        
        ShowGameEndScreen(true);
    }

    [ContextMenu("Test Star Animation")]
    public void TestStarAnimation()
    {
        if (starsAnimator != null)
        {
            Debug.Log("Testing star animations...");
            
            if (!starsAnimator.gameObject.activeSelf)
                starsAnimator.gameObject.SetActive(true);
            
            if (!starsAnimator.enabled)
                starsAnimator.enabled = true;
            
            starsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            for (int i = 0; i <= 3; i++)
            {
                Debug.Log($"\n=== Testing star value: {i} ===");
                
                starsAnimator.SetInteger(starParameter, 0);
                starsAnimator.Update(0f);
                
                System.Threading.Thread.Sleep(100);
                
                starsAnimator.SetInteger(starParameter, i);
                starsAnimator.Update(0f);
                
                AnimatorStateInfo stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"Current state: {stateInfo.fullPathHash}");
                Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
                Debug.Log($"Is in transition: {starsAnimator.IsInTransition(0)}");
                
                if (stateInfo.normalizedTime == 0 && i > 0)
                {
                    Debug.Log("Animation not playing, trying direct play...");
                    ForcePlayStarAnimation(i);
                }
                
                System.Threading.Thread.Sleep(500);
            }
        }
        else
        {
            Debug.LogWarning("Star animator is null!");
        }
    }

    [ContextMenu("Test Key Award Logic")]
    public void TestKeyAwardLogic()
    {
        Debug.Log("=== TESTING KEY AWARD LOGIC ===");
        
        // Test 3 stars with key → should award key
        Debug.Log("\nTest: 3 stars, key collected");
        starsEarned = 3;
        keyWasCollected = true;
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: true)");
        
        // Test 2 stars with key → should award key
        Debug.Log("\nTest: 2 stars, key collected");
        starsEarned = 2;
        keyWasCollected = true;
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: true)");
        
        // Test 1 star with key → should NOT award key
        Debug.Log("\nTest: 1 star, key collected");
        starsEarned = 1;
        keyWasCollected = true;
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: false)");
        
        // Test 3 stars without key → should NOT award key
        Debug.Log("\nTest: 3 stars, no key");
        starsEarned = 3;
        keyWasCollected = false;
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: false)");
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
        starsEarned = 3;
        Debug.Log($"Set keyWasCollected=true, starsEarned=3, keySavedToDatabase={keySavedToDatabase}");
    }

    [ContextMenu("Test Win with Key (3 stars)")]
    public void TestWinWithKey3Stars()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = true;
            remainingHearts = 5;
            completionTime = 540f; // 9 minutes
            ShowGameEndScreen(true);
        }
    }

    [ContextMenu("Test Win with Key (2 stars)")]
    public void TestWinWithKey2Stars()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = true;
            remainingHearts = 3;
            completionTime = 540f; // 9 minutes
            ShowGameEndScreen(true);
        }
    }

    [ContextMenu("Test Win with Key but 1 star")]
    public void TestWinWithKey1Star()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = true;
            remainingHearts = 1;
            completionTime = 540f; // 9 minutes
            ShowGameEndScreen(true);
        }
    }

    [ContextMenu("Test Win without Key (3 stars)")]
    public void TestWinWithoutKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = false;
            remainingHearts = 5;
            completionTime = 540f; // 9 minutes
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
        Debug.Log($"Current stars earned: {starsEarned}");
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
        Debug.Log($"starsEarnedText: {(starsEarnedText != null ? "SET" : "NULL")}");
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

    [ContextMenu("Debug Star Animator")]
    public void DebugStarAnimator()
    {
        if (starsAnimator == null)
        {
            Debug.LogError("Stars animator is null!");
            return;
        }
        
        Debug.Log("=== STAR ANIMATOR DEBUG ===");
        Debug.Log($"Animator GameObject: {starsAnimator.gameObject.name}");
        Debug.Log($"Animator enabled: {starsAnimator.enabled}");
        Debug.Log($"Animator active: {starsAnimator.gameObject.activeSelf}");
        Debug.Log($"Update mode: {starsAnimator.updateMode}");
        Debug.Log($"Controller: {starsAnimator.runtimeAnimatorController?.name}");
        
        Debug.Log($"Current '{starParameter}' value: {starsAnimator.GetInteger(starParameter)}");
        
        Debug.Log("All parameters:");
        foreach (var param in starsAnimator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = starsAnimator.GetFloat(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Int:
                    value = starsAnimator.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = starsAnimator.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "Trigger";
                    break;
            }
            Debug.Log($"- {param.name} (Type: {param.type}, Value: {value})");
        }
        
        AnimatorStateInfo stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current state hash: {stateInfo.fullPathHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Is in transition: {starsAnimator.IsInTransition(0)}");
    }
}