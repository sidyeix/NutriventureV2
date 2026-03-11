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
    [SerializeField] private TMP_Text starsEarnedText;
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
    [SerializeField] private int playerCameraPriority = 15;

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

    [Header("K2-style Button Settings")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private bool completeRestartOnConfirm = true;
    [SerializeField] private string sceneToReload = "";
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float buttonClickVolume = 0.7f;

    [Header("Key Unlocked Canvas")]
    [SerializeField] private GameObject keyUnlockedCanvas;
    [SerializeField] private Button continueKeyButton;
    [SerializeField] private KeyUnlockedCanvasController keyUnlockedController;

    [Header("Key Image Display")]
    [SerializeField] private GameObject KeyImageunlocking;

    [Header("🔑 KEY COLLECTION SETTINGS")]
    [SerializeField] private bool isKeyKingdom = true;
    [SerializeField] private string keyName = "OCR";
    [SerializeField] private int starsRequiredForKey = 2;

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
    private const float THREE_STAR_TIME_MAX = 600f;
    private const float TWO_STAR_TIME_MAX = 900f;

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

    // 🔥 KEY COLLECTION TRACKING
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

        if (collectKeyScript == null)
            collectKeyScript = FindObjectOfType<K4_CollectKey>();

        if (gameTimer == null)
        {
            gameTimer = GameTimer.Instance;
            if (gameTimer == null)
            {
                gameTimer = FindObjectOfType<GameTimer>();
            }

            if (gameTimer != null)
            {
#if UNITY_EDITOR
                Debug.Log($"Found GameTimer: {gameTimer.gameObject.name}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("GameTimer not found! Time-based scoring may not work properly.");
#endif
            }
        }

        cinemachineBrain = FindObjectOfType<CinemachineBrain>();
        if (cinemachineBrain != null)
        {
            originalBlendDefinition = cinemachineBrain.m_DefaultBlend;
        }

        if (characterAnimator == null && playerController != null)
        {
            characterAnimator = playerController.GetComponentInChildren<Animator>();
        }

        FindCameras();

        if (startingPoint == null)
        {
            GameObject startPointObj = GameObject.Find("StartingPoint");
            if (startPointObj != null)
            {
                startingPoint = startPointObj.transform;
            }
        }

        if (keyUnlockedController == null && keyUnlockedCanvas != null)
        {
            keyUnlockedController = keyUnlockedCanvas.GetComponent<KeyUnlockedCanvasController>();
        }

        if (keyUnlockedController == null)
        {
            keyUnlockedController = FindObjectOfType<KeyUnlockedCanvasController>();
            if (keyUnlockedController != null && keyUnlockedCanvas == null)
            {
                keyUnlockedCanvas = keyUnlockedController.gameObject;
            }
        }

        if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }

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
#if UNITY_EDITOR
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
#endif
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

        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);

        if (KeyImageunlocking != null)
        {
            KeyImageunlocking.SetActive(false);
#if UNITY_EDITOR
            Debug.Log("KeyImageunlocking initialized as DISABLED");
#endif
        }

        if (starsContainer != null)
            starsContainer.SetActive(false);

        if (gameEndVirtualCamera != null)
        {
            gameEndVirtualCamera.Priority = 0;
            gameEndVirtualCamera.gameObject.SetActive(false);
        }

        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        ResetStarAnimator();
    }

    private void ResetStarAnimator()
    {
        if (starsAnimator != null)
        {
#if UNITY_EDITOR
            Debug.Log("Resetting star animator...");
#endif
            starsAnimator.SetInteger(starParameter, 0);
            starsAnimator.updateMode = AnimatorUpdateMode.Normal;
            starsAnimator.Update(0f);
#if UNITY_EDITOR
            Debug.Log("Star animator reset to default state");
#endif
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
#if UNITY_EDITOR
        Debug.Log($"=== SHOWING KINGDOM 4 END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");
#endif

        this.playerWon = playerWon;
        isCountingAnimationComplete = false;
        isProcessingButton = false;
        isGameOver = true;
        isSummaryActive = true;

        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        CheckKeyCollectionStatus();

        CollectGameData();
        CalculateAllMetrics(playerWon);

        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();

        ForceSwitchToGameEndCamera();

        TeleportPlayerToResultPoint();
        SetupUI(playerWon);
        UpdateKeyImageDisplay();

        if (gameSummaryParent != null)
        {
            gameSummaryParent.SetActive(true);
#if UNITY_EDITOR
            Debug.Log("Game summary parent activated");
#endif

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("gameSummaryParent is null! Cannot show game summary.");
#endif
            return;
        }

        if (buttonContainer != null)
        {
            buttonContainer.SetActive(false);
        }

        StartCoroutine(GameEndSequence());

        DebugCameraState();
    }

    private void CheckKeyCollectionStatus()
    {
        if (collectKeyScript != null)
        {
            keyWasCollected = collectKeyScript.HasKey();
            healthAtKeyCollection = collectKeyScript.GetHealthAtKeyCollection();
#if UNITY_EDITOR
            Debug.Log($"Key collection status - Collected: {keyWasCollected}, Health: {healthAtKeyCollection}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("CollectKeyScript not found");
#endif
        }
    }

    private void CollectGameData()
    {
        if (scoreManager != null)
        {
            finalScore = scoreManager.GetFinalScore();
            wagonHits = scoreManager.totalWagonHits;
            allergensCollected = scoreManager.allergensFound;
            maxComboAchieved = scoreManager.maxComboAchieved;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("ScoreManager not found!");
#endif
            scoreManager = FindObjectOfType<Kingdom4ScoreManager>();
        }

        if (gameTimer != null)
        {
            completionTime = gameTimer.ElapsedTime;
#if UNITY_EDITOR
            Debug.Log($"Timer elapsed time: {completionTime}s");
#endif
            gameTimer.StopTimer();
        }
        else
        {
            completionTime = Time.time - gameStartTime;
#if UNITY_EDITOR
            Debug.LogWarning("GameTimer not found, using system time");
#endif
        }

        if (gameManager != null)
        {
            completedAllPhases = gameManager.currentPhase == AllerthriaGameManager.GamePhase.EndGame;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("GameManager not found!");
#endif
            gameManager = FindObjectOfType<AllerthriaGameManager>();
        }

        GetRemainingHealth();

#if UNITY_EDITOR
        Debug.Log($"Game Data Collected: Time={completionTime}s, Hearts={remainingHearts}, Allergens={allergensCollected}, WagonHits={wagonHits}, MaxCombo={maxComboAchieved}");
#endif
    }

    private void GetRemainingHealth()
    {
        if (healthManager != null)
        {
            remainingHearts = Mathf.CeilToInt(healthManager.currentHealth);
        }
        else if (gameManager != null)
        {
            remainingHearts = 3;
        }
        else
        {
            remainingHearts = 3;
        }
    }

    private void CalculateAllMetrics(bool playerWon)
    {
        starsEarned = CalculateStarRating(playerWon);
        currentStars = starsEarned;
        CalculateRewards();
    }

    private int CalculateStarRating(bool playerWon)
    {
        if (!playerWon || remainingHearts <= 0)
            return 0;

        int maxStarsByTime;
        if (completionTime < THREE_STAR_TIME_MAX)
            maxStarsByTime = 3;
        else if (completionTime < TWO_STAR_TIME_MAX)
            maxStarsByTime = 2;
        else
            maxStarsByTime = 1;

#if UNITY_EDITOR
        Debug.Log($"Max stars by time ({FormatTime(completionTime)}): {maxStarsByTime}");
#endif

        int starsByHearts;
        if (remainingHearts >= 4)
            starsByHearts = 3;
        else if (remainingHearts >= 3)
            starsByHearts = 2;
        else if (remainingHearts >= 1)
            starsByHearts = 1;
        else
            starsByHearts = 0;

#if UNITY_EDITOR
        Debug.Log($"Stars by hearts ({remainingHearts} hearts): {starsByHearts}");
#endif

        int finalStars = Mathf.Min(maxStarsByTime, starsByHearts);

#if UNITY_EDITOR
        Debug.Log($"Final stars: {finalStars}");
#endif
        return finalStars;
    }

    private void CalculateRewards()
    {
        switch (starsEarned)
        {
            case 3:
                baseCoins = 2000;
                baseExp = 2000;
                break;
            case 2:
                baseCoins = 1200;
                baseExp = 1200;
                break;
            case 1:
                baseCoins = 600;
                baseExp = 600;
                break;
            default:
                baseCoins = 100;
                baseExp = 100;
                break;
        }

        if (allergensCollected == 9)
        {
            baseCoins += 500;
            baseExp += 500;
#if UNITY_EDITOR
            Debug.Log("Bonus: Perfect allergen collection +500");
#endif
        }
        else if (allergensCollected >= 7)
        {
            baseCoins += 300;
            baseExp += 300;
#if UNITY_EDITOR
            Debug.Log("Bonus: Good allergen collection +300");
#endif
        }
        else if (allergensCollected >= 5)
        {
            baseCoins += 150;
            baseExp += 150;
#if UNITY_EDITOR
            Debug.Log("Bonus: Fair allergen collection +150");
#endif
        }

        if (wagonHits == 0)
        {
            baseCoins += 300;
            baseExp += 300;
#if UNITY_EDITOR
            Debug.Log("Bonus: No wagon hits +300");
#endif
        }
        else if (wagonHits <= 2)
        {
            baseCoins += 150;
            baseExp += 150;
#if UNITY_EDITOR
            Debug.Log("Bonus: Few wagon hits +150");
#endif
        }

        if (maxComboAchieved >= 8)
        {
            baseCoins += 400;
            baseExp += 400;
#if UNITY_EDITOR
            Debug.Log("Bonus: Excellent combo +400");
#endif
        }
        else if (maxComboAchieved >= 5)
        {
            baseCoins += 200;
            baseExp += 200;
#if UNITY_EDITOR
            Debug.Log("Bonus: Good combo +200");
#endif
        }
        else if (maxComboAchieved >= 3)
        {
            baseCoins += 100;
            baseExp += 100;
#if UNITY_EDITOR
            Debug.Log("Bonus: Fair combo +100");
#endif
        }

        int heartBonus = (remainingHearts - 1) * 100;
        baseCoins += heartBonus;
        baseExp += heartBonus;
#if UNITY_EDITOR
        Debug.Log($"Bonus: {remainingHearts} hearts remaining +{heartBonus}");
#endif

        int scoreBonus = Mathf.FloorToInt(finalScore * 0.1f);
#if UNITY_EDITOR
        Debug.Log($"Bonus: Score bonus (10% of {finalScore}) +{scoreBonus}");
#endif

        totalCoins = baseCoins + scoreBonus;
        totalExp = baseExp + scoreBonus;

#if UNITY_EDITOR
        Debug.Log($"Final Rewards: Coins={totalCoins}, Exp={totalExp}, Stars={starsEarned}");
#endif
    }

    private IEnumerator GameEndSequence()
    {
#if UNITY_EDITOR
        Debug.Log("Starting game end sequence...");
#endif

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.5f);

#if UNITY_EDITOR
        Debug.Log("Animating stars...");
#endif
        yield return StartCoroutine(PlayStarAnimationWithDelay());

#if UNITY_EDITOR
        Debug.Log("Animating counting numbers...");
#endif
        yield return StartCoroutine(AnimateCountingNumbers());

        isCountingAnimationComplete = true;
#if UNITY_EDITOR
        Debug.Log("Counting animation complete, showing buttons...");
#endif

        if (buttonContainer != null)
        {
            buttonContainer.SetActive(true);
#if UNITY_EDITOR
            Debug.Log("Button container activated");
#endif

            if (!playerWon && homeButton != null)
            {
                homeButton.interactable = false;
#if UNITY_EDITOR
                Debug.Log("Home button disabled on lose");
#endif
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("buttonContainer is null!");
#endif
        }

        countAnimationCoroutine = null;
    }

    private IEnumerator PlayStarAnimationWithDelay()
    {
        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.3f);
        PlayStarAnimationDirect();
        yield return CoroutineYieldCache.WaitForSecondsRealtime(1f);
    }

    private void PlayStarAnimationDirect()
    {
        if (starsAnimator != null)
        {
#if UNITY_EDITOR
            Debug.Log($"=== PLAYING STAR ANIMATION DIRECT: {currentStars} stars ===");
#endif

            if (!starsAnimator.gameObject.activeSelf)
            {
#if UNITY_EDITOR
                Debug.Log("Activating star animator GameObject");
#endif
                starsAnimator.gameObject.SetActive(true);
            }

            if (!starsAnimator.enabled)
            {
#if UNITY_EDITOR
                Debug.Log("Enabling star animator component");
#endif
                starsAnimator.enabled = true;
            }

            starsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            starsAnimator.SetInteger(starParameter, 0);
            starsAnimator.Update(0f);

            StartCoroutine(PlayStarAnimationAfterReset());
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Star animator is null! Cannot play animation.");
#endif
        }
    }

    private IEnumerator PlayStarAnimationAfterReset()
    {
        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        starsAnimator.SetInteger(starParameter, currentStars);
        starsAnimator.Update(0f);

        int currentValue = starsAnimator.GetInteger(starParameter);
#if UNITY_EDITOR
        Debug.Log($"Star parameter set to: {currentValue} (requested: {currentStars})");
#endif

        AnimatorStateInfo stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
#if UNITY_EDITOR
        Debug.Log($"Current animation state: {stateInfo.fullPathHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Is in transition: {starsAnimator.IsInTransition(0)}");
#endif

        if (stateInfo.normalizedTime == 0 && currentStars > 0)
        {
#if UNITY_EDITOR
            Debug.Log("Attempting to play animation directly...");
#endif
            ForcePlayStarAnimation(currentStars);
        }

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);
        stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
#if UNITY_EDITOR
        Debug.Log($"After 0.1s - State normalized time: {stateInfo.normalizedTime}");
#endif
    }

    private void ForcePlayStarAnimation(int stars)
    {
        if (starsAnimator != null && stars > 0 && stars <= 3)
        {
            string stateName = starStateNames[stars];
#if UNITY_EDITOR
            Debug.Log($"Force playing animation state: {stateName}");
#endif

            starsAnimator.Play(stateName, 0, 0f);
            starsAnimator.Update(0f);
        }
    }

    private IEnumerator AnimateCountingNumbers()
    {
#if UNITY_EDITOR
        Debug.Log("Starting counting animation...");
#endif

        if (pointsText == null || coinsText == null || expText == null || timeText == null || starsEarnedText == null)
        {
#if UNITY_EDITOR
            Debug.LogError("One or more UI text references are null!");
#endif
            yield break;
        }

        float targetTimePlayed = completionTime;
        int targetGameScore = finalScore;

        currentTimePlayed = targetTimePlayed;
        currentGameScore = targetGameScore;
        targetCoinsEarned = totalCoins;

        elapsedAnimationTime = 0f;
        int lastIntegerValue = 0;

        timeText.text = "00:00";
        pointsText.text = "0";
        coinsText.text = "0";
        expText.text = "0";
        starsEarnedText.text = "0/3";

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.3f);

        int numberOfTicks = Mathf.Clamp(targetGameScore / 50, 10, 30);
        float tickInterval = countAnimationDuration / numberOfTicks;
        float nextTickTime = 0f;

#if UNITY_EDITOR
        Debug.Log($"Audio: Will play {numberOfTicks} ticks every {tickInterval:F2} seconds");
#endif

        while (elapsedAnimationTime < countAnimationDuration)
        {
            elapsedAnimationTime += Time.unscaledDeltaTime;
            float progress = elapsedAnimationTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            float currentTime = Mathf.Lerp(0, targetTimePlayed, smoothProgress);
            timeText.text = FormatTime(currentTime);

            float currentScore = Mathf.Lerp(0, targetGameScore, smoothProgress);
            int currentInteger = Mathf.FloorToInt(currentScore);

            if (elapsedAnimationTime >= nextTickTime)
            {
                if (countTickSound != null && countAudioSource != null)
                {
                    countAudioSource.Stop();
                    countAudioSource.PlayOneShot(countTickSound, 0.5f);
#if UNITY_EDITOR
                    Debug.Log($"✓ Tick sound played at {elapsedAnimationTime:F2}s - Score: {currentInteger}");
#endif
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Count tick sound or audio source is null!");
#endif
                }

                nextTickTime += tickInterval;
            }

            pointsText.text = Mathf.FloorToInt(currentScore).ToString("N0");

            float currentCoins = Mathf.Lerp(0, targetCoinsEarned, smoothProgress);
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");

            float currentExp = Mathf.Lerp(0, totalExp, smoothProgress);
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");

            int currentStarsText = Mathf.FloorToInt(Mathf.Lerp(0, currentStars, smoothProgress));
            starsEarnedText.text = $"{currentStarsText}/3";

            yield return null;
        }

        timeText.text = FormatTime(targetTimePlayed);
        pointsText.text = targetGameScore.ToString("N0");
        coinsText.text = targetCoinsEarned.ToString("N0");
        expText.text = totalExp.ToString("N0");
        starsEarnedText.text = $"{currentStars}/3";

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        if (countCompleteSound != null && countAudioSource != null)
        {
            if (countAudioSource.isPlaying)
            {
                countAudioSource.Stop();
            }

            countAudioSource.PlayOneShot(countCompleteSound, 0.7f);
#if UNITY_EDITOR
            Debug.Log("✓ Completion sound played");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Count complete sound or audio source is null!");
#endif
        }

#if UNITY_EDITOR
        Debug.Log("Counting animation complete!");
#endif
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

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

    private void SetupUI(bool playerWon)
    {
        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("resultBackground is null!");
#endif
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
            bool shouldShowKeyImage = gameSummaryParent != null &&
                                     gameSummaryParent.activeSelf &&
                                     keyWasCollected &&
                                     starsEarned >= 2 &&
                                     playerWon;

            KeyImageunlocking.SetActive(shouldShowKeyImage);

#if UNITY_EDITOR
            Debug.Log($"KeyImageunlocking: {(shouldShowKeyImage ? "SHOWN" : "HIDDEN")} " +
                     $"- Stars: {starsEarned} (need 2+) " +
                     $"- KeyCollected: {keyWasCollected}");
#endif
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
                    keyWasCollected = true;
#if UNITY_EDITOR
                    Debug.Log("Showing key unlocked object - first time completion with 2+ stars!");
#endif
                }
            }
        }

        keyUnlockedObject.SetActive(shouldShowKey);
    }

    private void HandleCharacterAnimation(bool playerWon, int stars)
    {
        if (characterAnimator == null)
        {
#if UNITY_EDITOR
            Debug.LogError("CharacterAnimator is null! Cannot play animation.");
#endif
            return;
        }

        characterAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        characterAnimator.SetBool(danceParameter, false);
        characterAnimator.SetBool(thinkParameter, false);

        characterAnimator.Update(0f);

#if UNITY_EDITOR
        Debug.Log($"=== SETTING CHARACTER ANIMATION ===");
        Debug.Log($"playerWon: {playerWon}, stars: {stars}");
#endif

        if (playerWon && stars > 0)
        {
            characterAnimator.SetBool(danceParameter, true);
#if UNITY_EDITOR
            Debug.Log($"Set {danceParameter} = TRUE (WIN with {stars} stars)");
#endif
        }
        else
        {
            characterAnimator.SetBool(thinkParameter, true);
#if UNITY_EDITOR
            Debug.Log($"Set {thinkParameter} = TRUE (LOSE or 0 stars)");
#endif
        }

        characterAnimator.Update(0f);

        bool danceValue = characterAnimator.GetBool(danceParameter);
        bool thinkValue = characterAnimator.GetBool(thinkParameter);
#if UNITY_EDITOR
        Debug.Log($"After setting - Dance: {danceValue}, Think: {thinkValue}");
#endif
    }

    private void ResetCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(danceParameter, false);
            characterAnimator.SetBool(thinkParameter, false);

            characterAnimator.updateMode = AnimatorUpdateMode.Normal;
            characterAnimator.Update(0f);

#if UNITY_EDITOR
            Debug.Log("Character animation reset to normal");
#endif
        }
    }

    private void DisableObjectsOnGameEnd()
    {
        foreach (GameObject obj in objectsToDisableOnGameEnd)
        {
            if (obj != null && obj.activeSelf)
            {
                if (backgroundMusicSource != null && obj == backgroundMusicSource.gameObject)
                {
#if UNITY_EDITOR
                    Debug.Log($"Skipping disable of background music: {obj.name}");
#endif
                    continue;
                }

                obj.SetActive(false);
            }
        }

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

    private void ForceSwitchToGameEndCamera()
    {
        if (gameEndVirtualCamera == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Game end virtual camera is null!");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("=== FORCE SWITCHING TO GAME END CAMERA ===");
#endif

        SetCameraBlendToCut();

        if (playerFollowCamera != null)
        {
            playerFollowCamera.enabled = false;
            playerFollowCamera.Priority = 0;
            playerFollowCamera.gameObject.SetActive(false);

#if UNITY_EDITOR
            Debug.Log($"Player camera DISABLED, GameObject deactivated, and priority set to 0");
#endif
        }

        if (!gameEndVirtualCamera.gameObject.activeSelf)
        {
            gameEndVirtualCamera.gameObject.SetActive(true);
        }

        gameEndVirtualCamera.enabled = true;
        gameEndVirtualCamera.Priority = gameEndCameraPriority;

        if (cinemachineBrain != null)
        {
            cinemachineBrain.ManualUpdate();
        }

#if UNITY_EDITOR
        Debug.Log($"Game end camera ENABLED with priority: {gameEndCameraPriority}");
#endif

        StartCoroutine(VerifyForcedCameraSwitch());
    }

    private IEnumerator VerifyForcedCameraSwitch()
    {
        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.2f);

        if (cinemachineBrain != null)
        {
            var activeCam = cinemachineBrain.ActiveVirtualCamera;
#if UNITY_EDITOR
            Debug.Log($"Active Virtual Camera after force switch: {(activeCam != null ? activeCam.Name : "None")}");
#endif

            if (activeCam == null || activeCam.VirtualCameraGameObject != gameEndVirtualCamera.gameObject)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Game end camera not active! Forcing again...");
#endif

                if (playerFollowCamera != null)
                {
                    playerFollowCamera.gameObject.SetActive(false);
                }

                if (gameEndVirtualCamera != null)
                {
                    gameEndVirtualCamera.gameObject.SetActive(true);
                    gameEndVirtualCamera.Priority = 999;

                    if (cinemachineBrain != null)
                    {
                        cinemachineBrain.ManualUpdate();
                    }
                }
            }
        }
    }

    public void OnAcceptTimelineEndedAndGameStarting()
    {
#if UNITY_EDITOR
        Debug.Log("Accept timeline ended - preparing for game to start");
#endif

        if (playerFollowCamera != null)
        {
            playerFollowCamera.gameObject.SetActive(true);
            playerFollowCamera.enabled = true;
            playerFollowCamera.Priority = playerCameraPriority;

#if UNITY_EDITOR
            Debug.Log("Player camera prepared for gameplay");
#endif
        }
    }

    public void ForceResetCamera()
    {
#if UNITY_EDITOR
        Debug.Log("Force reset camera called on Kingdom4GameEndManager");
#endif

        if (playerFollowCamera != null)
        {
            playerFollowCamera.gameObject.SetActive(true);
            playerFollowCamera.enabled = true;
            playerFollowCamera.Priority = playerCameraPriority;
        }

        if (gameEndVirtualCamera != null)
        {
            gameEndVirtualCamera.Priority = 0;
        }

        if (cinemachineBrain != null)
        {
            cinemachineBrain.ManualUpdate();
            HardResetCamera();
        }
    }

    private void HardResetCamera()
    {
#if UNITY_EDITOR
        Debug.Log("Hard resetting camera...");
#endif

        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindObjectOfType<CinemachineBrain>();
            if (cinemachineBrain == null) return;
        }

        var defaultBlend = cinemachineBrain.m_DefaultBlend;

        cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
        cinemachineBrain.ManualUpdate();

        StartCoroutine(RestoreBlendAfterFrame(defaultBlend));
    }

    private IEnumerator RestoreBlendAfterFrame(CinemachineBlendDefinition originalBlend)
    {
        yield return new WaitForEndOfFrame();

        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend = originalBlend;
            cinemachineBrain.ManualUpdate();
        }
    }

    private IEnumerator DelayedCameraHardReset()
    {
        yield return new WaitForEndOfFrame();
        HardResetCamera();
    }

    private void SwitchToPlayerCameraWithBlend()
    {
#if UNITY_EDITOR
        Debug.Log("=== SWITCHING TO PLAYER CAMERA ===");
#endif

        if (playerFollowCamera != null)
        {
            RestoreOriginalCameraBlend();

            if (gameEndVirtualCamera != null)
            {
                gameEndVirtualCamera.Priority = 0;
#if UNITY_EDITOR
                Debug.Log("Game end camera priority set to 0");
#endif
            }

            playerFollowCamera.gameObject.SetActive(true);
            playerFollowCamera.enabled = true;
            playerFollowCamera.Priority = playerCameraPriority;

            if (cinemachineBrain != null)
            {
                cinemachineBrain.ManualUpdate();
            }

#if UNITY_EDITOR
            Debug.Log($"Player camera re-enabled with priority: {playerCameraPriority}");
#endif

            StartCoroutine(DelayedCameraHardReset());
            StartCoroutine(VerifyPlayerCameraSwitch());
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Player follow camera is null!");
#endif
        }
    }

    private IEnumerator VerifyPlayerCameraSwitch()
    {
        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        if (cinemachineBrain != null)
        {
            var activeCam = cinemachineBrain.ActiveVirtualCamera;
#if UNITY_EDITOR
            Debug.Log($"Active Virtual Camera after player switch: {(activeCam != null ? activeCam.Name : "None")}");
#endif

            if (activeCam != null && activeCam.VirtualCameraGameObject == playerFollowCamera.gameObject)
            {
#if UNITY_EDITOR
                Debug.Log("✓ Player camera is now active");
#endif
            }
        }
    }

    [ContextMenu("Debug Camera State")]
    public void DebugCameraState()
    {
#if UNITY_EDITOR
        Debug.Log("=== CAMERA STATE DEBUG ===");
        Debug.Log($"GameEnd Camera: {(gameEndVirtualCamera != null ? gameEndVirtualCamera.gameObject.name : "NULL")}");
#endif
        if (gameEndVirtualCamera != null)
        {
#if UNITY_EDITOR
            Debug.Log($"- Active: {gameEndVirtualCamera.gameObject.activeSelf}");
            Debug.Log($"- Priority: {gameEndVirtualCamera.Priority}");
            Debug.Log($"- Enabled: {gameEndVirtualCamera.enabled}");
#endif
        }

#if UNITY_EDITOR
        Debug.Log($"Player Camera: {(playerFollowCamera != null ? playerFollowCamera.gameObject.name : "NULL")}");
#endif
        if (playerFollowCamera != null)
        {
#if UNITY_EDITOR
            Debug.Log($"- Active: {playerFollowCamera.gameObject.activeSelf}");
            Debug.Log($"- Priority: {playerFollowCamera.Priority}");
            Debug.Log($"- Enabled: {playerFollowCamera.enabled}");
#endif
        }

        if (cinemachineBrain != null)
        {
            var activeCam = cinemachineBrain.ActiveVirtualCamera;
#if UNITY_EDITOR
            Debug.Log($"Active Virtual Camera: {(activeCam != null ? activeCam.Name : "None")}");
            Debug.Log($"Current Blend: {cinemachineBrain.ActiveBlend}");
            Debug.Log($"Default Blend Style: {cinemachineBrain.m_DefaultBlend.m_Style}");
#endif
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        if (playerController != null && resultCharacterSpawnPoint != null)
        {
            CharacterController charController = playerController.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }

            playerController.transform.position = resultCharacterSpawnPoint.position;
            playerController.transform.rotation = resultCharacterSpawnPoint.rotation;

            if (charController != null)
            {
                charController.enabled = true;
            }

#if UNITY_EDITOR
            Debug.Log($"Player teleported to result point: {resultCharacterSpawnPoint.position}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Player controller or result spawn point not found!");
#endif
        }
    }

    private void TeleportPlayerToStartingPoint()
    {
        if (playerController != null && startingPoint != null)
        {
            CharacterController charController = playerController.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }

            playerController.transform.position = startingPoint.position;
            playerController.transform.rotation = startingPoint.rotation;

            if (charController != null)
            {
                charController.enabled = true;
            }

#if UNITY_EDITOR
            Debug.Log($"Player teleported to starting point: {startingPoint.position}");
#endif
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
#if UNITY_EDITOR
        Debug.Log("Game end state reset");
#endif
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
#if UNITY_EDITOR
            Debug.Log("Player control enabled");
#endif
        }
    }

    public void ResetKingdom4Game()
    {
        ResetObjectsToInitialState();

        if (gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }

        if (gameManager != null)
        {
            gameManager.hasScroll = false;
            gameManager.collectedAllergens.Clear();
            gameManager.hasKey = false;
            gameManager.StartPhase(AllerthriaGameManager.GamePhase.ScrollQuest);
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        if (healthManager != null)
        {
            healthManager.ResetHealth();
        }

        if (collectKeyScript != null)
        {
            collectKeyScript.ForceFullReset();
        }

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

    private bool CheckIfPlayerHasOCRScannerKey()
    {
        if (GameDataManager.Instance == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("GameDataManager.Instance is null!");
#endif
            return false;
        }

        if (GameDataManager.Instance.CurrentGameData == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("CurrentGameData is null!");
#endif
            return false;
        }

        bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
#if UNITY_EDITOR
        Debug.Log($"CheckIfPlayerHasOCRScannerKey: {hasKey}");
#endif
        return hasKey;
    }

    // 🔥 KEY SAVE METHOD WITH EVENT TRIGGER
    private void SaveOCRScannerKeyToGameData()
    {
        if (GameDataManager.Instance == null)
        {
#if UNITY_EDITOR
            Debug.LogError("GameDataManager.Instance is null! Cannot save key.");
#endif
            return;
        }

        if (GameDataManager.Instance.CurrentGameData == null)
        {
#if UNITY_EDITOR
            Debug.LogError("CurrentGameData is null! Cannot save key.");
#endif
            return;
        }

        GameDataManager.Instance.CurrentGameData.CollectOCRScannerKey();
        GameDataManager.Instance.CurrentGameData.CollectKingdomKey("ocr");
        GameDataManager.Instance.SaveGameData();

#if UNITY_EDITOR
        Debug.Log("✓ OCR Scanner key successfully saved to GameData! HasOCRScannerKey is now TRUE");
#endif

        keySavedToDatabase = true;

        // 🔥 TRIGGER THE KEY COLLECTION EVENT
        KeyCollectionEvents.TriggerKeyCollected("OCR");
#if UNITY_EDITOR
        Debug.Log("🔥 Key Collection Event Triggered: OCR");
#endif
    }

    private void AddCoinsToDatabase()
    {
        if (coinsAddedToDatabase || GameDataManager.Instance == null) return;

        GameDataManager.Instance.CurrentGameData.nutriCoins += totalCoins;
        GameDataManager.Instance.SaveGameData();
        coinsAddedToDatabase = true;

#if UNITY_EDITOR
        Debug.Log($"Added {totalCoins} coins to database");
#endif
    }

    // ==================== HOME BUTTON HANDLER ====================

    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete || isProcessingButton)
        {
#if UNITY_EDITOR
            Debug.Log("Counting animation not complete yet or button already processing, ignoring click");
#endif
            return;
        }

        if (!playerWon)
        {
#if UNITY_EDITOR
            Debug.Log("Home button is disabled on lose screen");
#endif
            return;
        }

        isProcessingButton = true;
        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (homeButton != null)
            homeButton.interactable = false;

        // 🔥 SAVE KEY IF COLLECTED AND NOT SAVED
        if (keyWasCollected && !keySavedToDatabase && starsEarned >= starsRequiredForKey)
        {
            SaveOCRScannerKeyToGameData();
        }

        bool hasOCRScannerKey = CheckIfPlayerHasOCRScannerKey();
#if UNITY_EDITOR
        Debug.Log($"OCR Scanner Key status from GameData: {(hasOCRScannerKey ? "TRUE" : "FALSE")}");
#endif

        bool keyCollectedThisSession = keyWasCollected && !keySavedToDatabase && starsEarned >= starsRequiredForKey;

        if (keyCollectedThisSession && !hasOCRScannerKey)
        {
#if UNITY_EDITOR
            Debug.Log("Key collected this session with 2+ stars and not in GameData - showing KeyUnlockedCanvas");
#endif
            StartCoroutine(ReturnToPreSummaryStateAndShowKeyUnlockCanvas());
        }
        else if (hasOCRScannerKey)
        {
#if UNITY_EDITOR
            Debug.Log("Player already has OCR Scanner Key in GameData - returning to game (no canvas)");
#endif
            StartCoroutine(ReturnToPreSummaryStateOnly());
        }
        else
        {
#if UNITY_EDITOR
            Debug.Log("No key or less than 2 stars - returning to game fully");
#endif
            StartCoroutine(ReturnToGameFully());
        }
    }

    private IEnumerator ReturnToPreSummaryStateAndShowKeyUnlockCanvas()
    {
#if UNITY_EDITOR
        Debug.Log("Returning to pre-summary state and showing key unlock canvas");
#endif

        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        ResetCharacterAnimation();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        SwitchToPlayerCameraWithBlend();

        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        isGameOver = false;
        isSummaryActive = false;

#if UNITY_EDITOR
        Debug.Log($"Player at start position, input enabled");
#endif

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.5f);

        if (keyUnlockedController != null)
        {
#if UNITY_EDITOR
            Debug.Log("Showing KeyUnlockedCanvas via controller");
#endif
            keyUnlockedController.ShowKeyUnlockedCanvas(OnKeyUnlockCanvasContinue);
        }
        else if (keyUnlockedCanvas != null)
        {
#if UNITY_EDITOR
            Debug.Log("Activating KeyUnlockedCanvas GameObject directly");
#endif
            keyUnlockedCanvas.SetActive(true);

            if (continueKeyButton != null)
            {
                continueKeyButton.onClick.RemoveAllListeners();
                continueKeyButton.onClick.AddListener(OnContinueKeyButtonClicked);
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("KeyUnlockedCanvas GameObject is not assigned!");
#endif
            FinishHomeButtonSequence();
        }
    }

    private void OnKeyUnlockCanvasContinue()
    {
#if UNITY_EDITOR
        Debug.Log("Key unlock canvas continue callback received - SAVING OCR SCANNER KEY TO GAMEDATA");
#endif

        SaveOCRScannerKeyToGameData();

        if (keyUnlockedController != null)
        {
            keyUnlockedController.ForceHide();
        }
        else if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }

        FinishHomeButtonSequence();
    }

    public void OnContinueKeyButtonClicked()
    {
#if UNITY_EDITOR
        Debug.Log("ContinueKeyButton clicked directly - SAVING OCR SCANNER KEY TO GAMEDATA");
#endif

        SaveOCRScannerKeyToGameData();

        // 🔥 TRIGGER THE EVENT AGAIN TO BE SAFE
        KeyCollectionEvents.TriggerKeyCollected("OCR");

        if (keyUnlockedController != null)
        {
            keyUnlockedController.ForceHide();
        }
        else if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(false);
        }

        FinishHomeButtonSequence();
    }

    private IEnumerator ReturnToPreSummaryStateOnly()
    {
#if UNITY_EDITOR
        Debug.Log("Returning to pre-summary state only (no animation)");
#endif

        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        ResetCharacterAnimation();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        SwitchToPlayerCameraWithBlend();

        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;

        if (homeButton != null)
            homeButton.interactable = true;

#if UNITY_EDITOR
        Debug.Log($"Player at start position, input enabled");
#endif

        FinishHomeButtonSequence();
    }

    private IEnumerator ReturnToGameFully()
    {
#if UNITY_EDITOR
        Debug.Log("Returning to game fully");
#endif

        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        ResetCharacterAnimation();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        SwitchToPlayerCameraWithBlend();

        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;

        if (homeButton != null)
            homeButton.interactable = true;

#if UNITY_EDITOR
        Debug.Log($"Player at start position, input enabled");
#endif

        FinishHomeButtonSequence();
    }

    private void FinishHomeButtonSequence()
    {
#if UNITY_EDITOR
        Debug.Log("Home button sequence complete");
#endif

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
#if UNITY_EDITOR
            Debug.Log("Counting animation not complete yet or button already processing, ignoring click");
#endif
            return;
        }

        isProcessingButton = true;
        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (keyWasCollected && !keySavedToDatabase && starsEarned >= starsRequiredForKey)
        {
            SaveOCRScannerKeyToGameData();
        }

        if (restartButton != null)
            restartButton.interactable = false;

        if (completeRestartOnConfirm)
        {
#if UNITY_EDITOR
            Debug.Log("Complete restart requested - reloading scene");
#endif
            StartCoroutine(CompleteRestartGame());
        }
        else
        {
            StartCoroutine(SoftRestartGame());
        }
    }

    private IEnumerator SoftRestartGame()
    {
#if UNITY_EDITOR
        Debug.Log("Starting soft restart...");
#endif

        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        ResetCharacterAnimation();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        SwitchToPlayerCameraWithBlend();
        EnablePlayerControl();
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        isGameOver = false;
        isSummaryActive = false;
        isProcessingButton = false;

        if (restartButton != null)
            restartButton.interactable = true;

#if UNITY_EDITOR
        Debug.Log("Game soft restarted");
#endif
    }

    private IEnumerator CompleteRestartGame()
    {
#if UNITY_EDITOR
        Debug.Log("Starting complete game restart...");
#endif

        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        ResetPersistentData();

        Time.timeScale = originalTimeScale;

        yield return CoroutineYieldCache.WaitForSecondsRealtime(0.1f);

        ReloadCurrentScene();
    }

    private void ResetPersistentData()
    {
#if UNITY_EDITOR
        Debug.Log("Resetting persistent data...");
#endif

        if (collectKeyScript != null)
        {
            var method = collectKeyScript.GetType().GetMethod("GlobalResetAllKeys");
            if (method != null && method.IsStatic)
            {
                method.Invoke(null, null);
            }
        }

        keyWasCollected = false;
        keySavedToDatabase = false;
        coinsAddedToDatabase = false;

#if UNITY_EDITOR
        Debug.Log("Persistent data reset complete");
#endif
    }

    private void ReloadCurrentScene()
    {
#if UNITY_EDITOR
        Debug.Log("Reloading scene for complete restart...");
#endif

        string sceneName = string.IsNullOrEmpty(sceneToReload) ?
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name : sceneToReload;

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void PlayLobbyMusic()
    {
        if (backgroundMusicSource == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play lobby music.");
#endif
            return;
        }

        if (lobbyMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = lobbyMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();

#if UNITY_EDITOR
            Debug.Log($"Changed to lobby music: {lobbyMusicClip.name}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Lobby music clip not assigned!");
#endif
        }
    }

    private void PlayRestartMusic()
    {
        if (backgroundMusicSource == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play restart music.");
#endif
            return;
        }

        if (restartMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = restartMusicClip;
            backgroundMusicSource.loop = false;
            backgroundMusicSource.Play();

#if UNITY_EDITOR
            Debug.Log($"Changed to restart music: {restartMusicClip.name}");
#endif
        }
    }

    private void HandleBackgroundMusic(bool isWin)
    {
        if (backgroundMusicSource == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Background music source is null. Skipping music change.");
#endif
            return;
        }

        AudioClip musicToPlay = isWin ? winMusicClip : loseMusicClip;

        if (musicToPlay != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = musicToPlay;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();

#if UNITY_EDITOR
            Debug.Log($"Changed background music to: {(isWin ? "WIN" : "LOSE")} music");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No {(isWin ? "win" : "lose")} music clip assigned!");
#endif
        }
    }

    private void RestoreBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null)
        {
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
#if UNITY_EDITOR
            Debug.Log("=== HANDLE KINGDOM 4 COMPLETE ===");
#endif
            ShowGameEndScreen(true);
        }
        catch (System.Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"Error in HandleKingdom4Complete: {e.Message}\n{e.StackTrace}");
#endif
        }
    }

    public void HandleKingdom4GameOver()
    {
        try
        {
#if UNITY_EDITOR
            Debug.Log("=== HANDLE KINGDOM 4 GAME OVER ===");
#endif
            ShowGameEndScreen(false);
        }
        catch (System.Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"Error in HandleKingdom4GameOver: {e.Message}\n{e.StackTrace}");
#endif
        }
    }

    public void TriggerKingdom4Complete() => HandleKingdom4Complete();
    public void TriggerKingdom4GameOver() => HandleKingdom4GameOver();

    public void OnGameStarted()
    {
#if UNITY_EDITOR
        Debug.Log("Game started notification received in GameEndManager");
#endif
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
    public bool WasKeyCollectedThisSession() => keyWasCollected;
    public bool IsKeySavedToDatabase() => keySavedToDatabase;

    // ==================== DEBUG METHODS ====================

    [ContextMenu("Test Star Logic")]
    public void TestStarLogic()
    {
#if UNITY_EDITOR
        Debug.Log("=== TESTING STAR LOGIC ===");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 1: 5 hearts, 9 minutes");
#endif
        completionTime = 540f;
        remainingHearts = 5;
        int stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 3)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 2: 5 hearts, 11 minutes");
#endif
        completionTime = 660f;
        remainingHearts = 5;
        stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 2)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 3: 3 hearts, 9 minutes");
#endif
        completionTime = 540f;
        remainingHearts = 3;
        stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 2)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 4: 2 hearts, 9 minutes");
#endif
        completionTime = 540f;
        remainingHearts = 2;
        stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 1)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 5: 5 hearts, 16 minutes");
#endif
        completionTime = 960f;
        remainingHearts = 5;
        stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 1)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest 6: 0 hearts");
#endif
        remainingHearts = 0;
        stars = CalculateStarRating(true);
#if UNITY_EDITOR
        Debug.Log($"Result: {stars} stars (Expected: 0)");
#endif
    }

    [ContextMenu("Test Show Game Summary")]
    public void TestShowGameSummary()
    {
#if UNITY_EDITOR
        Debug.Log("=== TESTING GAME SUMMARY ===");
#endif

        completionTime = 540f;
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
#if UNITY_EDITOR
            Debug.Log("Testing star animations...");
#endif

            if (!starsAnimator.gameObject.activeSelf)
                starsAnimator.gameObject.SetActive(true);

            if (!starsAnimator.enabled)
                starsAnimator.enabled = true;

            starsAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            for (int i = 0; i <= 3; i++)
            {
#if UNITY_EDITOR
                Debug.Log($"\n=== Testing star value: {i} ===");
#endif

                starsAnimator.SetInteger(starParameter, 0);
                starsAnimator.Update(0f);

                System.Threading.Thread.Sleep(100);

                starsAnimator.SetInteger(starParameter, i);
                starsAnimator.Update(0f);

                AnimatorStateInfo stateInfo = starsAnimator.GetCurrentAnimatorStateInfo(0);
#if UNITY_EDITOR
                Debug.Log($"Current state: {stateInfo.fullPathHash}");
                Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
                Debug.Log($"Is in transition: {starsAnimator.IsInTransition(0)}");
#endif

                if (stateInfo.normalizedTime == 0 && i > 0)
                {
#if UNITY_EDITOR
                    Debug.Log("Animation not playing, trying direct play...");
#endif
                    ForcePlayStarAnimation(i);
                }

                System.Threading.Thread.Sleep(500);
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Star animator is null!");
#endif
        }
    }

    [ContextMenu("Test Key Award Logic")]
    public void TestKeyAwardLogic()
    {
#if UNITY_EDITOR
        Debug.Log("=== TESTING KEY AWARD LOGIC ===");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest: 3 stars, key collected");
#endif
        starsEarned = 3;
        keyWasCollected = true;
#if UNITY_EDITOR
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: true)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest: 2 stars, key collected");
#endif
        starsEarned = 2;
        keyWasCollected = true;
#if UNITY_EDITOR
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: true)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest: 1 star, key collected");
#endif
        starsEarned = 1;
        keyWasCollected = true;
#if UNITY_EDITOR
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: false)");
#endif

#if UNITY_EDITOR
        Debug.Log("\nTest: 3 stars, no key");
#endif
        starsEarned = 3;
        keyWasCollected = false;
#if UNITY_EDITOR
        Debug.Log($"Key awarded: {(starsEarned >= 2 && keyWasCollected)} (Expected: false)");
#endif
    }

    [ContextMenu("Test OCR Scanner Key Saving")]
    public void TestOCRScannerKeySaving()
    {
#if UNITY_EDITOR
        Debug.Log("=== TESTING OCR SCANNER KEY SAVING ===");
#endif
        isFirstTimeCompletion = true;
        SaveOCRScannerKeyToGameData();

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
#if UNITY_EDITOR
            Debug.Log($"After save - HasOCRScannerKey: {hasKey}");
#endif
        }
    }

    [ContextMenu("Test Key Collection Flow")]
    public void TestKeyCollectionFlow()
    {
#if UNITY_EDITOR
        Debug.Log("=== TESTING KEY COLLECTION FLOW ===");
#endif
        keyWasCollected = true;
        keySavedToDatabase = false;
        starsEarned = 3;
#if UNITY_EDITOR
        Debug.Log($"Set keyWasCollected=true, starsEarned=3, keySavedToDatabase={keySavedToDatabase}");
#endif
    }

    [ContextMenu("Test Win with Key (3 stars)")]
    public void TestWinWithKey3Stars()
    {
        if (!isGameOver && !isSummaryActive)
        {
            keyWasCollected = true;
            remainingHearts = 5;
            completionTime = 540f;
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
            completionTime = 540f;
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
            completionTime = 540f;
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
            completionTime = 540f;
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
#if UNITY_EDITOR
            Debug.LogError("GameDataManager or CurrentGameData is null!");
#endif
            return;
        }

        bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
#if UNITY_EDITOR
        Debug.Log($"OCR Scanner Key status: {(hasKey ? "COLLECTED" : "NOT COLLECTED")}");
        Debug.Log($"Key collected this session: {keyWasCollected}");
        Debug.Log($"Key saved to database: {keySavedToDatabase}");
        Debug.Log($"Current stars earned: {starsEarned}");
#endif
    }

    [ContextMenu("Collect OCR Scanner Key (Test)")]
    public void TestCollectOCRScannerKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.CollectOCRScannerKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
#if UNITY_EDITOR
            Debug.Log("OCR Scanner Key collected and saved to GameData");
#endif

            // 🔥 TRIGGER EVENT FOR TEST
            KeyCollectionEvents.TriggerKeyCollected("OCR");
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
#if UNITY_EDITOR
            Debug.Log("OCR Scanner Key reset in GameData");
#endif
        }
    }

    [ContextMenu("Debug Camera State")]
    public void DebugCameraStateMenu()
    {
        DebugCameraState();
    }
}
