// Kingdom4GameEndManager.cs (UPDATED)
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
    [SerializeField] private PlayerHealthManager healthManager; // Changed from HealthSystem

    [Header("Timer Integration")]
    [SerializeField] private GameTimer gameTimer;

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
            // Don't record start time here - use timer instead
            // gameStartTime = Time.time;
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
            homeButton.onClick.AddListener(OnHomeClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

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
        
        isCountingAnimationComplete = false;
        
        CollectGameData();
        CalculateAllMetrics(playerWon);
        
        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();
        SetupUI(playerWon);
        
        if (gameSummaryParent != null)
        {
            gameSummaryParent.SetActive(true);
            Debug.Log("Game summary parent activated");
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
        
        yield return new WaitForSeconds(0.5f);

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

        yield return new WaitForSeconds(0.3f);

        float elapsedTime = 0f;
        int lastIntegerValue = 0;

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
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

        yield return new WaitForSeconds(0.3f);

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

        yield return new WaitForSeconds(1f);
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

        if (questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                if ((quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress) &&
                    starsEarned >= 2 &&
                    playerWon)
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
        if (characterAnimator == null) return;

        characterAnimator.SetBool(danceParameter, false);
        characterAnimator.SetBool(thinkParameter, false);

        if (stars == 0)
        {
            characterAnimator.SetBool(thinkParameter, true);
            Debug.Log("Character animation: Thinking (0 stars)");
        }
        else if (playerWon)
        {
            characterAnimator.SetBool(danceParameter, true);
            Debug.Log("Character animation: Dancing (win)");
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

    private void SwitchToPlayerCameraWithCut()
    {
        if (playerFollowCamera != null)
        {
            SetCameraBlendToCut();

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
            
            Debug.Log("Switched to player camera with CUT blend");
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
        SwitchToPlayerCameraWithCut();
        DisableWinLoseObjects();

        if (starsAnimator != null) starsAnimator.SetInteger(starParameter, 0);
        if (starsContainer != null) starsContainer.SetActive(false);

        if (keyUnlockedObject != null && keyUnlockedObject.activeSelf)
            keyUnlockedObject.SetActive(false);

        if (pointsText != null) pointsText.text = "0";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";
        if (timeText != null) timeText.text = "00:00";

        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (gameSummaryParent != null) gameSummaryParent.SetActive(false);
        
        RestoreOriginalCameraBlend();
        Debug.Log("Game end state reset");
    }

    private void ResetCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(danceParameter, false);
            characterAnimator.SetBool(thinkParameter, false);
            Debug.Log("Character animation reset");
        }
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
    }

    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete) 
        {
            Debug.Log("Counting animation not complete yet, ignoring click");
            return;
        }

        OnButtonClicked();
        PlayLobbyMusic();
        ResetGameEndState();
        ResetKingdom4Game();

        if (playerController != null && lobbyPoint != null)
        {
            playerController.transform.position = lobbyPoint.position;
            playerController.transform.rotation = lobbyPoint.rotation;
        }

        if (playerController != null && !playerController.gameObject.activeSelf)
            playerController.gameObject.SetActive(true);

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        if (isFirstTimeCompletion && questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                questManager.CompleteTask(questID, $"{questID}_task_1");
                questManager.ClaimQuest(questID);
                Debug.Log($"Quest {questID} completed and claimed!");
            }
        }
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

    private void OnRestartClicked()
    {
        if (!isCountingAnimationComplete) 
        {
            Debug.Log("Counting animation not complete yet, ignoring click");
            return;
        }

        OnButtonClicked();
        ResetGameEndState();
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        PlayRestartMusic();
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
        // Reset any game start state if needed
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

    public void CompleteKingdom4Quest(string questID)
    {
        if (QuestManager.Instance != null)
        {
            Quest quest = QuestManager.Instance.GetQuest(questID);
            if (quest != null && (quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress))
            {
                foreach (var task in quest.tasks)
                {
                    if (!task.isCompleted)
                        QuestManager.Instance.CompleteTask(questID, task.taskID);
                }
                QuestManager.Instance.ClaimQuest(questID);
                HandleKeyUnlockedObject(true);
            }
        }
    }

    // Add this method to get detailed scoring breakdown
    public string GetScoringBreakdown()
    {
        return $"Stars: {starsEarned}/3\n" +
               $"Time: {FormatTime(completionTime)}\n" +
               $"Allergens: {allergensCollected}/9\n" +
               $"Wagon Hits: {wagonHits}\n" +
               $"Max Combo: x{maxComboAchieved}\n" +
               $"Hearts Remaining: {remainingHearts}/5\n" +
               $"Final Score: {finalScore}\n" +
               $"Coins Earned: {totalCoins}\n" +
               $"Exp Earned: {totalExp}";
    }

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
        Debug.Log(GetScoringBreakdown());
        
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
        Debug.Log(GetScoringBreakdown());
        
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
        Debug.Log(GetScoringBreakdown());
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
        Debug.Log("==============================");
    }
}