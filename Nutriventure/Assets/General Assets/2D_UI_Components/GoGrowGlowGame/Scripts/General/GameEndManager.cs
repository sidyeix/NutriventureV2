using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Cinemachine;
using UnityEngine.Playables;

public class GameEndManager : MonoBehaviour
{
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
    [SerializeField] private float minTickInterval = 0.05f;

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

    [Header("Objects to Disable on Home/Restart")]
    [SerializeField] private List<GameObject> objectsToDisableOnHomeOrRestart = new List<GameObject>();

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera gameEndVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    [SerializeField] private int gameEndCameraPriority = 100;
    [SerializeField] private int playerCameraPriority = 20;

    [Header("Spawn Points")]
    [SerializeField] private Transform resultCharacterSpawnPoint;
    [SerializeField] private Transform lobbyPoint;

    [Header("Player Armature - DRAG HERE")]
    [SerializeField] private Transform playerArmature;

    [Header("Quest System")]
    [SerializeField] private string kingdomID = "general_quests";
    [SerializeField] private string questID = "0001";

    [Header("Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private GameObject backgroundMusicObject;
    [SerializeField] private AudioClip winMusicClip;
    [SerializeField] private AudioClip loseMusicClip;
    [SerializeField] private AudioClip restartMusicClip;
    [SerializeField] private AudioClip lobbyMusicClip;

    [Header("Object Reset System")]
    [SerializeField] private List<GameObject> objectsToReset = new List<GameObject>();
    [SerializeField] private bool storeInitialPositionsOnStart = true;

    [Header("Animator Reset System")]
    [SerializeField] private List<Animator> animatorsToReset = new List<Animator>();
    [SerializeField] private string defaultStateName = "Default";

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string danceParameter = "isDancing";
    [SerializeField] private string thinkParameter = "isThinking";
    [SerializeField] private string freefallParameter = "FreeFall";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string jumpParameter = "Jump";

    [Header("UI Controls")]
    [SerializeField] private GameObject uiControlsCanvas;

    [Header("PLAYABLE DIRECTOR OBJECT CONTROL - HOME BUTTON")]
    [SerializeField] private GameObject playableDirectorObject;

    [Header("PLAYABLE DIRECTOR - RESTART BUTTON TIMELINE")]
    [SerializeField] private PlayableDirector restartPlayableDirector;
    [SerializeField] private PlayableAsset restartPlayableAsset;
    [SerializeField] private float restartTimelineDelay = 0.5f;

    [Header("References")]
    [SerializeField] private GoGrowGlowGameManager gameManager;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private GrowAssessmentManager growAssessmentManager;
    [SerializeField] private GlowPartManager glowPartManager;
    [SerializeField] private StartingSequenceManager startingSequenceManager;

    [Header("Coin Reward UI Feedback")]
    [SerializeField] private GameObject coinRewardFeedbackPrefab;
    [SerializeField] private RectTransform coinRewardSpawnPoint;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private float coinFeedbackSlideDuration = 0.5f;
    [SerializeField] private float coinFeedbackFadeOutDuration = 0.3f;
    [SerializeField] private float coinFeedbackSlideUpAmount = 50f;
    [SerializeField] private string coinFeedbackPrefix = "+";
    [SerializeField] private string coinFeedbackSuffix = "";

    [Header("🔑 KEY COLLECTION SETTINGS")]
    [SerializeField] private bool isKeyKingdom = true; // Set to true for Sugar Kingdom
    [SerializeField] private string keyName = "Sugaria"; // "Sugaria", "Preservia", "Allerthia", "OCR"
    [SerializeField] private int starsRequiredForKey = 2; // Minimum stars needed to get the key

    // Game end calculations
    private int starsEarned = 0;
    private int baseCoins = 0;
    private int baseExp = 0;
    private int totalCoins = 0;
    private int totalExp = 0;
    private float completionTime = 0f;
    private int playerPoints = 0;
    private int remainingHearts = 0;

    private Coroutine countAnimationCoroutine;
    private bool isFirstTimeCompletion = false;
    private bool isCountingAnimationComplete = false;

    // Audio control variables
    private bool isCountAudioPlaying = false;
    private float lastTickTime = 0f;

    // Reward tracking
    private bool hasAddedRewards = false;

    // 🔥 KEY COLLECTION TRACKING
    private bool keyWasCollected = false;
    private bool keySavedToDatabase = false;

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
        if (gameManager == null)
            gameManager = FindObjectOfType<GoGrowGlowGameManager>();

        if (playerController == null)
            playerController = FindObjectOfType<ThirdPersonController>();

        if (questManager == null)
            questManager = QuestManager.Instance;

        if (growAssessmentManager == null)
            growAssessmentManager = FindObjectOfType<GrowAssessmentManager>();

        if (glowPartManager == null)
            glowPartManager = FindObjectOfType<GlowPartManager>();

        if (startingSequenceManager == null)
            startingSequenceManager = FindObjectOfType<StartingSequenceManager>();

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

        // Find the main canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogWarning("No Canvas found in scene! Coin feedback will not display correctly.");
            }
        }

        // Find the coin text if spawn point not assigned
        if (coinRewardSpawnPoint == null)
        {
            GameObject coinTextObj = GameObject.Find("CoinText");
            if (coinTextObj != null)
            {
                coinRewardSpawnPoint = coinTextObj.GetComponent<RectTransform>();
                Debug.Log("CoinRewardSpawnPoint automatically set to CoinText");
            }
            else
            {
                Debug.LogWarning("CoinRewardSpawnPoint is not assigned! Please drag the CoinText RectTransform to the field.");
            }
        }

        // Find background music object if not assigned
        if (backgroundMusicObject == null && backgroundMusicSource != null)
        {
            backgroundMusicObject = backgroundMusicSource.gameObject;
        }

        // If still null, try to find by name
        if (backgroundMusicObject == null)
        {
            GameObject foundMusic = GameObject.Find("BackgroundMusic");
            if (foundMusic == null)
                foundMusic = GameObject.Find("BGM");
            if (foundMusic == null)
                foundMusic = GameObject.Find("Music");

            if (foundMusic != null)
            {
                backgroundMusicObject = foundMusic;
                backgroundMusicSource = foundMusic.GetComponent<AudioSource>();
                Debug.Log("BackgroundMusic automatically found by name");
            }
        }

        // Warn if playable director object is not assigned
        if (playableDirectorObject == null)
        {
            Debug.LogWarning("Playable Director Object is not assigned in the Inspector! Home button timeline control will not work.");
        }

        // Validate Playable Director setup
        if (restartPlayableDirector == null)
        {
            Debug.LogWarning("Restart Playable Director is not assigned! Timeline will not play on restart.");
        }

        if (restartPlayableAsset == null)
        {
            Debug.LogWarning("Restart Playable Asset is not assigned! Timeline will not play on restart.");
        }
    }

    private void Start()
    {
        if (buttonContainer != null)
            buttonContainer.SetActive(false);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        // Disable next button as requested
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

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

        if (storeInitialPositionsOnStart)
        {
            StoreInitialTransforms();
        }

        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
        }

        // Reset flags on start
        hasAddedRewards = false;
        keyWasCollected = false;
        keySavedToDatabase = false;
    }

    // ========== PLAYABLE DIRECTOR OBJECT CONTROL - HOME BUTTON ==========

    private void DisablePlayableDirectorObject()
    {
        if (playableDirectorObject != null)
        {
            playableDirectorObject.SetActive(false);
            Debug.Log("Playable Director Object DISABLED - Home button clicked");
        }
    }

    // ========== PLAYABLE DIRECTOR CONTROL - RESTART BUTTON ==========

    private void PlayRestartTimelineSequence()
    {
        if (restartPlayableDirector == null)
        {
            Debug.LogError("Cannot play restart timeline - Playable Director is not assigned!");
            return;
        }

        if (restartPlayableAsset == null)
        {
            Debug.LogError("Cannot play restart timeline - Playable Asset is not assigned!");
            return;
        }

        restartPlayableDirector.enabled = true;

        if (restartPlayableDirector.state == PlayState.Playing)
        {
            restartPlayableDirector.Stop();
        }

        restartPlayableDirector.playableAsset = restartPlayableAsset;
        restartPlayableDirector.Play();

        Debug.Log("Restart timeline STARTED");
    }

    // ========== OBJECT DISABLE ON HOME/RESTART ==========

    private void DisableObjectsOnHomeOrRestart()
    {
        foreach (GameObject obj in objectsToDisableOnHomeOrRestart)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled object: {obj.name} on Home/Restart button click");
            }
        }
    }

    // ========== ENABLE BACKGROUND MUSIC OBJECT ==========

    private void ForceEnableBackgroundMusic()
    {
        if (backgroundMusicObject != null)
        {
            if (!backgroundMusicObject.activeSelf)
            {
                backgroundMusicObject.SetActive(true);
                Debug.Log("BackgroundMusic GameObject ENABLED");
            }

            if (backgroundMusicSource != null)
            {
                if (!backgroundMusicSource.enabled)
                {
                    backgroundMusicSource.enabled = true;
                }

                if (!backgroundMusicSource.isPlaying && backgroundMusicSource.clip != null)
                {
                    backgroundMusicSource.Play();
                }
            }
        }
        else
        {
            GameObject foundMusic = GameObject.Find("BackgroundMusic");
            if (foundMusic == null)
                foundMusic = GameObject.Find("BGM");
            if (foundMusic == null)
                foundMusic = GameObject.Find("Music");

            if (foundMusic != null)
            {
                backgroundMusicObject = foundMusic;
                backgroundMusicSource = foundMusic.GetComponent<AudioSource>();

                if (!backgroundMusicObject.activeSelf)
                {
                    backgroundMusicObject.SetActive(true);
                }

                if (backgroundMusicSource != null && !backgroundMusicSource.isPlaying && backgroundMusicSource.clip != null)
                {
                    backgroundMusicSource.Play();
                }
            }
        }
    }

    // ========== ANIMATOR STATE RESET ==========

    private void ResetAnimatorBeforeTeleport()
    {
        if (characterAnimator == null) return;

        if (!string.IsNullOrEmpty(freefallParameter))
            characterAnimator.SetBool(freefallParameter, false);

        if (!string.IsNullOrEmpty(groundedParameter))
            characterAnimator.SetBool(groundedParameter, true);

        if (!string.IsNullOrEmpty(jumpParameter))
            characterAnimator.SetBool(jumpParameter, false);

        characterAnimator.ResetTrigger("Jump");
        characterAnimator.ResetTrigger("jump");
    }

    // ========== CONTINUE BUTTON RESET LOGIC ==========

    private void ResetAllContinueButtons()
    {
        ContinueButton[] continueButtons = FindObjectsOfType<ContinueButton>(true);
        if (continueButtons.Length == 0) return;

        foreach (ContinueButton continueButton in continueButtons)
        {
            if (continueButton != null)
            {
                continueButton.ResetButton();
            }
        }
    }

    public void TriggerContinueButtonReset()
    {
        ResetAllContinueButtons();
    }

    // ========== TELEPORTATION METHODS ==========

    private void TeleportPlayerToTransform(Transform targetTransform, string locationName)
    {
        if (playerController == null || targetTransform == null)
        {
            Debug.LogError($"Cannot teleport player - PlayerController or {locationName} Transform is null!");
            return;
        }

        Vector3 targetPosition = targetTransform.position;
        Quaternion targetRotation = targetTransform.rotation;

        Debug.Log($"Teleporting player to {locationName}: {targetPosition}");

        ResetAnimatorBeforeTeleport();

        if (playerArmature != null && playerController.transform != null)
        {
            if (playerArmature.parent != playerController.transform)
            {
                playerArmature.SetParent(playerController.transform);
            }

            playerArmature.localPosition = Vector3.zero;
            playerArmature.localRotation = Quaternion.identity;
        }

        playerController.transform.position = targetPosition;
        playerController.transform.rotation = targetRotation;

        if (characterAnimator != null)
        {
            characterAnimator.Update(0f);
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        TeleportPlayerToTransform(resultCharacterSpawnPoint, "Result Point");
    }

    private void TeleportPlayerToLobbyPoint()
    {
        TeleportPlayerToTransform(lobbyPoint, "Lobby Point");
    }

    // ========== AUDIO METHODS ==========

    private void StopCountAudio()
    {
        if (countAudioSource != null && countAudioSource.isPlaying)
        {
            countAudioSource.Stop();
        }
        isCountAudioPlaying = false;
    }

    private void PlayCountTick()
    {
        if (countTickSound == null || countAudioSource == null) return;
        if (Time.time - lastTickTime < minTickInterval) return;

        countAudioSource.PlayOneShot(countTickSound);
        lastTickTime = Time.time;
        isCountAudioPlaying = true;
    }

    private void PlayCountCompleteSound()
    {
        if (countCompleteSound != null && countAudioSource != null)
        {
            countAudioSource.PlayOneShot(countCompleteSound);
            Debug.Log("Count Complete Sound played");
        }
    }

    private void PlayLobbyMusic()
    {
        if (backgroundMusicSource == null) return;

        if (!backgroundMusicSource.gameObject.activeSelf)
            backgroundMusicSource.gameObject.SetActive(true);

        if (!backgroundMusicSource.enabled)
            backgroundMusicSource.enabled = true;

        if (lobbyMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = lobbyMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
    }

    private void PlayRestartMusic()
    {
        if (backgroundMusicSource == null) return;

        if (!backgroundMusicSource.gameObject.activeSelf)
            backgroundMusicSource.gameObject.SetActive(true);

        if (!backgroundMusicSource.enabled)
            backgroundMusicSource.enabled = true;

        if (restartMusicClip != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = restartMusicClip;
            backgroundMusicSource.loop = false;
            backgroundMusicSource.Play();
        }
    }

    // ========== TRANSFORM STORAGE ==========

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

    // ========== COIN REWARD FEEDBACK METHODS ==========

    private void ShowCoinRewardFeedback(int coinsAmount)
    {
        if (coinRewardFeedbackPrefab == null)
        {
            Debug.LogWarning("Coin Reward Feedback Prefab is not assigned!");
            return;
        }

        if (parentCanvas == null)
        {
            Debug.LogWarning("Parent Canvas is not assigned! Cannot show coin feedback.");
            return;
        }

        if (coinsAmount <= 0) return;

        PlayCountCompleteSound();

        GameObject feedbackObject = Instantiate(coinRewardFeedbackPrefab, parentCanvas.transform);
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        if (coinRewardSpawnPoint != null)
        {
            rectTransform.position = coinRewardSpawnPoint.position;
            rectTransform.anchorMin = coinRewardSpawnPoint.anchorMin;
            rectTransform.anchorMax = coinRewardSpawnPoint.anchorMax;
            rectTransform.pivot = coinRewardSpawnPoint.pivot;
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        TMP_Text feedbackText = feedbackObject.GetComponentInChildren<TMP_Text>();
        if (feedbackText != null)
        {
            feedbackText.text = $"{coinFeedbackPrefix}{coinsAmount}{coinFeedbackSuffix}";
        }

        StartCoroutine(AnimateCoinRewardFeedback(feedbackObject));
    }

    private IEnumerator AnimateCoinRewardFeedback(GameObject feedbackObject)
    {
        if (feedbackObject == null) yield break;

        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = feedbackObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = feedbackObject.AddComponent<CanvasGroup>();
        }

        Vector2 startAnchoredPosition = rectTransform.anchoredPosition;
        Vector2 endAnchoredPosition = startAnchoredPosition + new Vector2(0, coinFeedbackSlideUpAmount);

        float elapsedTime = 0f;

        while (elapsedTime < coinFeedbackSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / coinFeedbackSlideDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPosition, endAnchoredPosition, smoothT);
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < coinFeedbackFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / coinFeedbackFadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            yield return null;
        }

        Destroy(feedbackObject);
    }

    // ========== REWARD ADDITION METHODS ==========

    private void AddRewardsToGameData()
    {
        if (hasAddedRewards || GameDataManager.Instance == null) return;

        if (GameDataManager.Instance.CurrentGameData != null)
        {
            int oldCoins = GameDataManager.Instance.CurrentGameData.nutriCoins;

            GameDataManager.Instance.CurrentGameData.nutriCoins += totalCoins;

            float expToAdd = totalExp;
            GameDataManager.Instance.CurrentGameData.currentXP += expToAdd;

            while (GameDataManager.Instance.CurrentGameData.currentXP >= GameDataManager.Instance.CurrentGameData.xpToNextLevel)
            {
                GameDataManager.Instance.CurrentGameData.playerLevel++;
                GameDataManager.Instance.CurrentGameData.currentXP -= GameDataManager.Instance.CurrentGameData.xpToNextLevel;
                GameDataManager.Instance.CurrentGameData.xpToNextLevel *= 1.5f;
                Debug.Log($"Level Up! New Level: {GameDataManager.Instance.CurrentGameData.playerLevel}");
            }

            GameDataManager.Instance.SaveGameData();

            Debug.Log($"Rewards added to GameData: +{totalCoins} Coins (was {oldCoins}, now {GameDataManager.Instance.CurrentGameData.nutriCoins}), +{totalExp} EXP");

            Player_Data playerData = FindObjectOfType<Player_Data>();
            if (playerData != null)
            {
                playerData.ForceUpdateAllUI();
            }

            ShowCoinRewardFeedback(totalCoins);
            hasAddedRewards = true;
        }
    }

    // 🔥 KEY COLLECTION METHOD - SAVES THE KEY AND TRIGGERS EVENT
    private void SaveKeyToDatabase()
    {
        if (keySavedToDatabase || GameDataManager.Instance == null || !isKeyKingdom) return;
        
        if (keyWasCollected && starsEarned >= starsRequiredForKey)
        {
            // Save the appropriate key based on keyName
            switch (keyName.ToLower())
            {
                case "sugaria":
                    GameDataManager.Instance.CurrentGameData.CollectSugariaKey();
                    Debug.Log("✅ Sugaria Key saved to GameData!");
                    break;
                case "preservia":
                    GameDataManager.Instance.CurrentGameData.CollectPreserviaKey();
                    Debug.Log("✅ Preservia Key saved to GameData!");
                    break;
                case "allerthia":
                    GameDataManager.Instance.CurrentGameData.CollectAllerthiaKey();
                    Debug.Log("✅ Allerthia Key saved to GameData!");
                    break;
                case "ocr":
                    GameDataManager.Instance.CurrentGameData.CollectOCRScannerKey();
                    Debug.Log("✅ OCR Scanner Key saved to GameData!");
                    break;
                default:
                    Debug.LogWarning($"Unknown key name: {keyName}");
                    return;
            }
            
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
            
            // 🔥 TRIGGER THE KEY COLLECTION EVENT - THIS UPDATES THE GLOBAL MAP
            KeyCollectionEvents.TriggerKeyCollected(keyName);
            Debug.Log($"🔥 Key Collection Event Triggered: {keyName}");
        }
    }

    // ========== GAME END SCREEN ==========

    public void ShowGameEndScreen(bool playerWon)
    {
        Debug.Log($"=== SHOWING GAME END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");

        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();

        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        starsEarned = CalculateStarRating(remainingHearts, completionTime);
        
        // Check if key should be awarded (player won and enough stars)
        keyWasCollected = playerWon && starsEarned >= starsRequiredForKey;
        
        CalculateRewards();

        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
        }

        HandleCharacterAnimation(playerWon, starsEarned);
        HandleBackgroundMusic(playerWon && starsEarned > 0);

        if (!playerWon) HandleLose();
        else HandleWin();

        HandleKeyUnlockedObject(playerWon);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(true);

        isCountingAnimationComplete = false;
        hasAddedRewards = false;
        keySavedToDatabase = false; // Reset for new game
        StartCoroutine(GameEndSequence());
    }

    private void HideStarsWhenShowingSummary()
    {
        foreach (GameObject star in starsToHide)
        {
            if (star != null && star.activeSelf)
                star.SetActive(false);
        }
    }

    private void HandleKeyUnlockedObject(bool playerWon)
    {
        if (keyUnlockedObject == null) return;

        keyUnlockedObject.SetActive(false);

        bool shouldShowKey = false;

        if (questManager != null && isKeyKingdom && playerWon && starsEarned >= starsRequiredForKey)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                if (quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress)
                {
                    shouldShowKey = true;
                    isFirstTimeCompletion = true;
                    keyWasCollected = true;
                    Debug.Log($"Showing key unlocked object for {keyName} - first time completion with {starsEarned} stars!");
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
            characterAnimator.SetBool(thinkParameter, true);
        else if (playerWon)
            characterAnimator.SetBool(danceParameter, true);
    }

    private void ResetCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(danceParameter, false);
            characterAnimator.SetBool(thinkParameter, false);
        }
    }

    private void DisableObjectsOnGameEnd()
    {
        foreach (GameObject obj in objectsToDisableOnGameEnd)
        {
            if (obj != null && obj.activeSelf)
            {
                if (backgroundMusicObject != null && obj == backgroundMusicObject)
                    continue;

                if (backgroundMusicSource != null && obj == backgroundMusicSource.gameObject)
                    continue;

                obj.SetActive(false);
            }
        }

        if (uiControlsCanvas != null && uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(false);
    }

    // ========== CAMERA METHODS ==========

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
                cinemachineBrain.ManualUpdate();
        }
    }

    private IEnumerator RestoreCameraBlendAfterTeleport()
    {
        yield return null;
        RestoreOriginalCameraBlend();
    }

    // ========== WIN/LOSE HANDLING ==========

    private void HandleLose()
    {
        foreach (GameObject obj in objectsToEnableOnLose)
        {
            if (obj != null && !obj.activeSelf)
                obj.SetActive(true);
        }
    }

    private void HandleWin()
    {
        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null && !obj.activeSelf)
                obj.SetActive(true);
        }
    }

    private void EnableObjectsOnHomeButton()
    {
        foreach (GameObject obj in objectsToEnableOnHomeButton)
        {
            if (obj != null && !obj.activeSelf)
                obj.SetActive(true);
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

    // ========== ANIMATION SEQUENCES ==========

    private IEnumerator GameEndSequence()
    {
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(AnimateStars());
        yield return StartCoroutine(AnimateCountingNumbers());

        isCountingAnimationComplete = true;

        if (buttonContainer != null)
            buttonContainer.SetActive(true);
    }

    private IEnumerator AnimateStars()
    {
        if (starsContainer == null || starsAnimator == null)
            yield break;

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
        }

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator AnimateCountingNumbers()
    {
        if (pointsText == null || timeText == null || coinsText == null || expText == null)
            yield break;

        StopCountAudio();

        pointsText.text = "0";
        timeText.text = "00:00";
        coinsText.text = "0";
        expText.text = "0";

        yield return new WaitForSeconds(0.3f);

        float elapsedTime = 0f;
        int lastIntegerValue = 0;
        bool hasPlayedCompleteSound = false;

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            float currentPoints = Mathf.Lerp(0, playerPoints, smoothProgress);
            float currentTime = Mathf.Lerp(0, completionTime, smoothProgress);
            float currentCoins = Mathf.Lerp(0, totalCoins, smoothProgress);
            float currentExp = Mathf.Lerp(0, totalExp, smoothProgress);

            int currentInteger = Mathf.FloorToInt(currentPoints);

            if (currentInteger > lastIntegerValue)
            {
                PlayCountTick();
                lastIntegerValue = currentInteger;
            }

            pointsText.text = Mathf.FloorToInt(currentPoints).ToString("N0");
            timeText.text = FormatTime(currentTime);
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");

            yield return null;
        }

        StopCountAudio();

        pointsText.text = playerPoints.ToString("N0");
        timeText.text = FormatTime(completionTime);
        coinsText.text = totalCoins.ToString("N0");
        expText.text = totalExp.ToString("N0");

        if (countCompleteSound != null && countAudioSource != null && !hasPlayedCompleteSound)
        {
            countAudioSource.PlayOneShot(countCompleteSound);
            hasPlayedCompleteSound = true;
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private int CalculateStarRating(int hearts, float time)
    {
        if (hearts == 0) return 0;
        if (hearts >= 3 && time <= 15 * 60) return 3;
        if (hearts == 2 && time <= 17 * 60) return 2;
        if (hearts == 1 && time >= 20 * 60) return 1;
        return 1;
    }

    private void CalculateRewards()
    {
        switch (starsEarned)
        {
            case 3: baseCoins = 1000; baseExp = 1000; break;
            case 2: baseCoins = 500; baseExp = 500; break;
            case 1: baseCoins = 100; baseExp = 100; break;
            default: baseCoins = 0; baseExp = 0; break;
        }

        int bonusExpFromPoints = Mathf.FloorToInt(playerPoints / 7f);
        int bonusCoinsFromPoints = Mathf.FloorToInt(playerPoints / 10f);

        int lifeBonusCoins = 0;
        if (remainingHearts >= 5) lifeBonusCoins = 300;
        else if (remainingHearts == 4) lifeBonusCoins = 200;
        else if (remainingHearts == 3) lifeBonusCoins = 100;
        else if (remainingHearts == 2) lifeBonusCoins = 50;

        totalExp = baseExp + bonusExpFromPoints;
        totalCoins = baseCoins + bonusCoinsFromPoints + lifeBonusCoins;
    }

    // ========== BUTTON HANDLERS ==========

    private void OnButtonClicked()
    {
        StopCountAudio();
        DisableWinLoseObjects();
        EnablePlayerControl();
    }

    private void EnablePlayerControl()
    {
        if (playerController != null) playerController.enabled = true;
    }

    // ========== HOME BUTTON ==========
    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== HOME BUTTON CLICKED ===");

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();
        AddRewardsToGameData();

        // 🔥 SAVE KEY TO DATABASE AND TRIGGER EVENT
        if (keyWasCollected && !keySavedToDatabase)
        {
            SaveKeyToDatabase();
        }

        CheckForKingKeyUnlock();

        PlayLobbyMusic();
        ResetGameEndState();
        ResetMinigamesForHomeButton();
        ResetAllContinueButtons();

        DisableObjectsOnHomeOrRestart();
        DisablePlayableDirectorObject();

        SwitchToPlayerCameraWithCut();
        TeleportPlayerToLobbyPoint();

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
            }
        }

        StartCoroutine(RestoreCameraBlendAfterTeleport());
        ForceEnableBackgroundMusic();

        Debug.Log("=== HOME BUTTON COMPLETE ===");
    }

    // ========== RESTART BUTTON ==========
    private void OnRestartClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== RESTART BUTTON CLICKED ===");

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();

        AddRewardsToGameData();

        // 🔥 SAVE KEY TO DATABASE AND TRIGGER EVENT
        if (keyWasCollected && !keySavedToDatabase)
        {
            SaveKeyToDatabase();
        }

        PlayRestartMusic();
        ResetGameEndState();

        DisableObjectsOnHomeOrRestart();

        Debug.Log("STEP 1: Performing complete game reset...");
        ResetMinigames();
        ResetAllContinueButtons();

        if (gameManager != null)
        {
            gameManager.FullGameReset();
            Debug.Log("GameManager fully reset");
        }

        Debug.Log("STEP 2: Teleporting to lobby point...");
        TeleportPlayerToLobbyPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        Debug.Log("STEP 3: Playing restart timeline...");
        StartCoroutine(PlayRestartTimeline());

        StartCoroutine(RestoreCameraBlendAfterTeleport());

        ForceEnableBackgroundMusic();

        Debug.Log("=== RESTART BUTTON COMPLETE ===");
        Debug.Log($"Rewards added: +{totalCoins} Coins, +{totalExp} EXP");
    }

    // Coroutine to play restart timeline
    private IEnumerator PlayRestartTimeline()
    {
        if (restartPlayableDirector == null)
        {
            Debug.LogError("Cannot play restart timeline - Playable Director is not assigned!");
            yield break;
        }

        if (restartPlayableAsset == null)
        {
            Debug.LogError("Cannot play restart timeline - Playable Asset is not assigned!");
            yield break;
        }

        if (restartTimelineDelay > 0)
        {
            Debug.Log($"Waiting {restartTimelineDelay}s before playing restart timeline...");
            yield return new WaitForSeconds(restartTimelineDelay);
        }

        Debug.Log("Playing restart timeline...");

        if (restartPlayableDirector.state == PlayState.Playing)
        {
            restartPlayableDirector.Stop();
        }

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        restartPlayableDirector.enabled = true;
        restartPlayableDirector.playableAsset = restartPlayableAsset;
        restartPlayableDirector.Play();

        while (restartPlayableDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        Debug.Log("Restart timeline finished - Game is now ready to play");

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private void OnNextClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== NEXT BUTTON CLICKED ===");

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();

        AddRewardsToGameData();

        // 🔥 SAVE KEY TO DATABASE AND TRIGGER EVENT
        if (keyWasCollected && !keySavedToDatabase)
        {
            SaveKeyToDatabase();
        }

        PlayLobbyMusic();
        ResetGameEndState();
        ResetMinigamesForHomeButton();

        if (isFirstTimeCompletion && questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                questManager.CompleteTask(questID, $"{questID}_task_1");
                questManager.ClaimQuest(questID);
            }
        }

        TeleportPlayerToLobbyPoint();
        StartCoroutine(RestoreCameraBlendAfterTeleport());

        ForceEnableBackgroundMusic();
    }

    // ========== RESET METHODS ==========

    public void ResetMinigamesForHomeButton()
    {
        Debug.Log("=== COMPLETE MINIGAMES RESET (HOME BUTTON - NO STARTING SEQUENCE) ===");

        if (growAssessmentManager != null)
        {
            growAssessmentManager.EndGrowAssessment();
            growAssessmentManager.CompleteResetForNewGame();
        }

        ResetGlowTowers();
        ResetDayNightTransition();

        ResetTorchMinigame();

        AssessmentTrigger assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
        if (assessmentTrigger != null) assessmentTrigger.ForceResetForNewGame();

        EndGameTrigger endGameTrigger = FindObjectOfType<EndGameTrigger>();
        if (endGameTrigger != null) endGameTrigger.ResetEndTrigger();

        ResetObjectsToInitialState();
        ResetOneTimeAnimations();

        Debug.Log("=== MINIGAMES COMPLETELY RESET (HOME BUTTON) ===");
    }

    public void ResetMinigames()
    {
        Debug.Log("=== COMPLETE MINIGAMES RESET (RESTART BUTTON) ===");

        if (growAssessmentManager != null)
        {
            growAssessmentManager.EndGrowAssessment();
            growAssessmentManager.CompleteResetForNewGame();
        }

        ResetGlowTowers();
        ResetDayNightTransition();

        if (startingSequenceManager != null)
        {
            startingSequenceManager.EnableAllControlsAndUI();
            var resetMethod = startingSequenceManager.GetType().GetMethod("ForceCameraReset");
            if (resetMethod != null)
                resetMethod.Invoke(startingSequenceManager, new object[] { 0f });

            Debug.Log("StartingSequenceManager reset for restart");
        }

        ResetTorchMinigame();

        AssessmentTrigger assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
        if (assessmentTrigger != null) assessmentTrigger.ForceResetForNewGame();

        EndGameTrigger endGameTrigger = FindObjectOfType<EndGameTrigger>();
        if (endGameTrigger != null) endGameTrigger.ResetEndTrigger();

        ResetObjectsToInitialState();
        ResetOneTimeAnimations();

        Debug.Log("=== MINIGAMES COMPLETELY RESET (RESTART BUTTON) ===");
    }

    public void ResetGameEndState()
    {
        StopCountAudio();
        ResetCharacterAnimation();
        SwitchToPlayerCameraWithCut();
        DisableWinLoseObjects();

        if (starsAnimator != null) starsAnimator.SetInteger(starParameter, 0);
        if (starsContainer != null) starsContainer.SetActive(false);
        if (keyUnlockedObject != null && keyUnlockedObject.activeSelf) keyUnlockedObject.SetActive(false);

        if (pointsText != null) pointsText.text = "0";
        if (timeText != null) timeText.text = "00:00";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";

        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (gameSummaryParent != null) gameSummaryParent.SetActive(false);
    }

    private void ResetGlowTowers()
    {
        GlowTower[] allTowers = FindObjectsOfType<GlowTower>();
        foreach (GlowTower tower in allTowers)
        {
            if (tower != null) tower.ResetTower();
        }

        if (glowPartManager != null)
        {
            System.Type type = glowPartManager.GetType();
            var newGameReset = type.GetMethod("CompleteResetForNewGame");
            if (newGameReset != null) newGameReset.Invoke(glowPartManager, null);
        }

        GlowPartTrigger[] glowTriggers = FindObjectsOfType<GlowPartTrigger>();
        foreach (GlowPartTrigger trigger in glowTriggers)
        {
            if (trigger != null) trigger.ResetTrigger();
        }
    }

    private void ResetDayNightTransition()
    {
        DayNightLightingTransition dayNight = FindObjectOfType<DayNightLightingTransition>();
        if (dayNight != null)
        {
            System.Type type = dayNight.GetType();
            var resetMethod = type.GetMethod("ResetTransition");
            if (resetMethod != null) resetMethod.Invoke(dayNight, null);
            else dayNight.SetToDay();
        }
    }

    private void ResetOneTimeAnimations()
    {
        foreach (Animator animator in animatorsToReset)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.Play(defaultStateName, -1, 0f);
                animator.Update(0f);

                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    switch (param.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            animator.SetBool(param.name, false);
                            break;
                        case AnimatorControllerParameterType.Float:
                            animator.SetFloat(param.name, 0f);
                            break;
                        case AnimatorControllerParameterType.Int:
                            animator.SetInteger(param.name, 0);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            animator.ResetTrigger(param.name);
                            break;
                    }
                }
            }
        }
    }

    private void ResetTorchMinigame()
    {
        TorchMinigameManager torchManager = FindObjectOfType<TorchMinigameManager>();
        if (torchManager != null)
        {
            System.Type torchManagerType = typeof(TorchMinigameManager);
            var resetMethod = torchManagerType.GetMethod("CompleteMinigameReset");
            if (resetMethod != null) resetMethod.Invoke(torchManager, null);
        }
    }

    // ========== PUBLIC HANDLERS ==========

    public void HandleGameOver()
    {
        StopCountAudio();

        if (gameManager != null && gameManager.IsGameActive())
            gameManager.EndGame();

        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = 0;
        starsEarned = 0;
        keyWasCollected = false; // No key on game over
        CalculateRewards();
        ShowGameEndScreen(false);
    }

    public void HandleLevelComplete()
    {
        StopCountAudio();

        if (gameManager != null && gameManager.IsGameActive())
            gameManager.EndGame();

        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());
        starsEarned = CalculateStarRating(remainingHearts, completionTime);
        
        // Check if key should be awarded (player won and enough stars)
        keyWasCollected = starsEarned >= starsRequiredForKey;
        
        ShowGameEndScreen(true);
    }

    public void TriggerLevelComplete() => HandleLevelComplete();
    public void TriggerGameOver() => HandleGameOver();

    private void HandleBackgroundMusic(bool isWin)
    {
        if (backgroundMusicSource == null) return;

        AudioClip musicToPlay = isWin ? winMusicClip : loseMusicClip;

        if (musicToPlay != null)
        {
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = musicToPlay;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
    }

    public void ShowGameEndAfterKingTimeline()
    {
        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();

        if (GoGrowGlowGameManager.Instance != null)
        {
            completionTime = GoGrowGlowGameManager.Instance.GetGameTimer();
            playerPoints = GoGrowGlowGameManager.Instance.GetCurrentScore();
            remainingHearts = Mathf.CeilToInt(GoGrowGlowGameManager.Instance.GetCurrentLifeAmount());
        }

        starsEarned = CalculateStarRating(remainingHearts, completionTime);
        keyWasCollected = starsEarned >= starsRequiredForKey;
        CalculateRewards();

        if (resultBackground != null && winBackground != null)
            resultBackground.sprite = winBackground;

        HandleCharacterAnimation(true, starsEarned);
        HandleBackgroundMusic(true);
        HandleWin();

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(true);

        isCountingAnimationComplete = false;
        hasAddedRewards = false;
        keySavedToDatabase = false;
        StartCoroutine(GameEndSequence());
    }

    // ========== PUBLIC GETTERS ==========

    public Transform GetResultSpawnPoint() => resultCharacterSpawnPoint;

    public void CompleteQuestAfterKingTimeline(string questID)
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

    private void CheckForKingKeyUnlock()
    {
        KingVitronTimelineButton kingButton = FindObjectOfType<KingVitronTimelineButton>();
        if (kingButton != null)
        {
            kingButton.SetStarsEarned(starsEarned);
            kingButton.CheckKeyUnlockAfterHomeButton();
        }
    }

    public int GetStarsEarned() => starsEarned;
    public int GetTotalCoins() => totalCoins;
    public int GetTotalExp() => totalExp;
    public float GetCompletionTime() => completionTime;
    public int GetPlayerPoints() => playerPoints;
    public bool IsFirstTimeCompletion() => isFirstTimeCompletion;
    public bool WasKeyCollected() => keyWasCollected;

    public void ForceStopCountAudio()
    {
        StopCountAudio();
    }

    // ========== DEBUG METHODS ==========
    
    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        starsEarned = 3;
        keyWasCollected = true;
        HandleLevelComplete();
    }

    [ContextMenu("Test Win without Key")]
    public void TestWinWithoutKey()
    {
        starsEarned = 1;
        keyWasCollected = false;
        HandleLevelComplete();
    }

    [ContextMenu("Test Force Save Sugaria Key")]
    public void TestForceSaveSugariaKey()
    {
        keyWasCollected = true;
        starsEarned = 3;
        SaveKeyToDatabase();
    }
}