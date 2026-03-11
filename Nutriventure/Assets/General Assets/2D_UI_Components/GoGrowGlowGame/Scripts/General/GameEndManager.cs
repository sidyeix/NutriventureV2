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

    [Header("🔑 KEY UNLOCKED CANVAS")]
    [SerializeField] private GameObject keyUnlockedCanvas;
    [SerializeField] private Button keyUnlockedContinueButton;
    [SerializeField] private TMP_Text keyUnlockedTitleText;
    [SerializeField] private TMP_Text keyUnlockedDescriptionText;

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
    [SerializeField] private bool isKeyKingdom = true;
    [SerializeField] private string keyName = "Sugaria";
    [SerializeField] private int starsRequiredForKey = 2;

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

    // 🔑 KEY UNLOCK TRACKING
    private bool pendingKeyUnlock = false;
    private string pendingKeyName = "";

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
                #if UNITY_EDITOR
                Debug.LogWarning("No Canvas found in scene! Coin feedback will not display correctly.");
                #endif
            }
        }

        // Find the coin text if spawn point not assigned
        if (coinRewardSpawnPoint == null)
        {
            GameObject coinTextObj = GameObject.Find("CoinText");
            if (coinTextObj != null)
            {
                coinRewardSpawnPoint = coinTextObj.GetComponent<RectTransform>();
                #if UNITY_EDITOR
                Debug.Log("CoinRewardSpawnPoint automatically set to CoinText");
                #endif
            }
            else
            {
                #if UNITY_EDITOR
                Debug.LogWarning("CoinRewardSpawnPoint is not assigned! Please drag the CoinText RectTransform to the field.");
                #endif
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
                #if UNITY_EDITOR
                Debug.Log("BackgroundMusic automatically found by name");
                #endif
            }
        }

        // Warn if playable director object is not assigned
        if (playableDirectorObject == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Playable Director Object is not assigned in the Inspector! Home button timeline control will not work.");
            #endif
        }

        // Validate Playable Director setup
        if (restartPlayableDirector == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Restart Playable Director is not assigned! Timeline will not play on restart.");
            #endif
        }

        if (restartPlayableAsset == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Restart Playable Asset is not assigned! Timeline will not play on restart.");
            #endif
        }
    }

    private void Start()
    {
        if (buttonContainer != null)
            buttonContainer.SetActive(false);

        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        // Hide key unlocked canvas
        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);

        // Disable next button as requested
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        // Add listener to key unlocked continue button
        if (keyUnlockedContinueButton != null)
            keyUnlockedContinueButton.onClick.AddListener(OnKeyUnlockedContinueClicked);

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
            #if UNITY_EDITOR
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
            #endif
        }

        // Reset flags on start
        hasAddedRewards = false;
        pendingKeyUnlock = false;
    }

    // ========== PLAYABLE DIRECTOR OBJECT CONTROL - HOME BUTTON ==========

    private void DisablePlayableDirectorObject()
    {
        if (playableDirectorObject != null)
        {
            playableDirectorObject.SetActive(false);
            #if UNITY_EDITOR
            Debug.Log("Playable Director Object DISABLED - Home button clicked");
            #endif
        }
    }

    // ========== PLAYABLE DIRECTOR CONTROL - RESTART BUTTON ==========

    private void PlayRestartTimelineSequence()
    {
        if (restartPlayableDirector == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("Cannot play restart timeline - Playable Director is not assigned!");
            #endif
            return;
        }

        if (restartPlayableAsset == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("Cannot play restart timeline - Playable Asset is not assigned!");
            #endif
            return;
        }

        restartPlayableDirector.enabled = true;

        if (restartPlayableDirector.state == PlayState.Playing)
        {
            restartPlayableDirector.Stop();
        }

        restartPlayableDirector.playableAsset = restartPlayableAsset;
        restartPlayableDirector.Play();

        #if UNITY_EDITOR
        Debug.Log("Restart timeline STARTED");
        #endif
    }

    // ========== OBJECT DISABLE ON HOME/RESTART ==========

    private void DisableObjectsOnHomeOrRestart()
    {
        foreach (GameObject obj in objectsToDisableOnHomeOrRestart)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                #if UNITY_EDITOR
                Debug.Log($"Disabled object: {obj.name} on Home/Restart button click");
                #endif
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
                #if UNITY_EDITOR
                Debug.Log("BackgroundMusic GameObject ENABLED");
                #endif
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
            #if UNITY_EDITOR
            Debug.LogError($"Cannot teleport player - PlayerController or {locationName} Transform is null!");
            #endif
            return;
        }

        Vector3 targetPosition = targetTransform.position;
        Quaternion targetRotation = targetTransform.rotation;

        #if UNITY_EDITOR
        Debug.Log($"Teleporting player to {locationName}: {targetPosition}");
        #endif

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

        // Play respawn sound & effect at the lobby arrival point
        if (gameManager != null)
        {
            gameManager.PlayRespawnSound();
            gameManager.ShowRespawnEffect();
        }
    }

    /// <summary>
    /// Public accessor for teleporting the player to the lobby point.
    /// Used by ResumeGameCanvas when the player clicks "No" (Restart).
    /// </summary>
    public void TeleportToLobbyPoint()
    {
        TeleportPlayerToLobbyPoint();
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
            #if UNITY_EDITOR
            Debug.Log("Count Complete Sound played");
            #endif
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
            #if UNITY_EDITOR
            Debug.LogWarning("Coin Reward Feedback Prefab is not assigned!");
            #endif
            return;
        }

        if (parentCanvas == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Parent Canvas is not assigned! Cannot show coin feedback.");
            #endif
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
                #if UNITY_EDITOR
                Debug.Log($"Level Up! New Level: {GameDataManager.Instance.CurrentGameData.playerLevel}");
                #endif
            }

            GameDataManager.Instance.SaveGameData();

            #if UNITY_EDITOR
            Debug.Log($"Rewards added to GameData: +{totalCoins} Coins (was {oldCoins}, now {GameDataManager.Instance.CurrentGameData.nutriCoins}), +{totalExp} EXP");
            #endif

            Player_Data playerData = FindObjectOfType<Player_Data>();
            if (playerData != null)
            {
                playerData.ForceUpdateAllUI();
            }

            ShowCoinRewardFeedback(totalCoins);
            hasAddedRewards = true;
        }
    }

    // ========== GAME END SCREEN ==========

    public void ShowGameEndScreen(bool playerWon)
    {
        #if UNITY_EDITOR
        Debug.Log($"=== SHOWING GAME END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");
        #endif

        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();

        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        starsEarned = CalculateStarRating(remainingHearts, completionTime);

        CalculateRewards();

        // 🔑 Check if this is a key kingdom and conditions are met
        if (isKeyKingdom && playerWon && starsEarned >= starsRequiredForKey)
        {
            // Check if key hasn't been collected yet
            bool hasKey = false;
            switch (keyName.ToLower())
            {
                case "sugaria":
                    hasKey = GameDataManager.Instance.HasSugariaKey();
                    break;
                case "preservia":
                    hasKey = GameDataManager.Instance.HasPreserviaKey();
                    break;
                case "allerthia":
                    hasKey = GameDataManager.Instance.HasAllerthiaKey();
                    break;
                case "ocr":
                    hasKey = GameDataManager.Instance.HasOCRScannerKey();
                    break;
            }

            if (!hasKey)
            {
                pendingKeyUnlock = true;
                pendingKeyName = keyName;
                #if UNITY_EDITOR
                Debug.Log($"🔑 Key unlock pending for {keyName} with {starsEarned} stars!");
                #endif
            }
        }

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
                    #if UNITY_EDITOR
                    Debug.Log($"Showing key unlocked object for {keyName} - first time completion with {starsEarned} stars!");
                    #endif
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
        yield return CoroutineYieldCache.WaitForSeconds(0.5f);
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
        yield return CoroutineYieldCache.WaitForSeconds(0.3f);

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

        yield return CoroutineYieldCache.WaitForSeconds(1f);
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

        yield return CoroutineYieldCache.WaitForSeconds(0.3f);

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

        #if UNITY_EDITOR
        Debug.Log("=== HOME BUTTON CLICKED ===");
        #endif

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();
        AddRewardsToGameData();

        // 🔑 Check if we have a pending key unlock
        if (pendingKeyUnlock)
        {
            // Show the Key Unlocked Canvas instead of saving immediately
            ShowKeyUnlockedCanvas();
        }
        else
        {
            // No key to unlock, proceed with normal home button behavior
            ReturnToLobby();
        }
    }

    // 🔑 Show Key Unlocked Canvas
    private void ShowKeyUnlockedCanvas()
    {
        #if UNITY_EDITOR
        Debug.Log($"🔑 Showing Key Unlocked Canvas for {pendingKeyName}");
        #endif

        // Hide the game summary
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);

        // Show the key unlocked canvas
        if (keyUnlockedCanvas != null)
        {
            keyUnlockedCanvas.SetActive(true);

            // Update text in the canvas
            if (keyUnlockedTitleText != null)
            {
                keyUnlockedTitleText.text = $"{pendingKeyName} Key Unlocked!";
            }

            if (keyUnlockedDescriptionText != null)
            {
                keyUnlockedDescriptionText.text = $"You've earned the {pendingKeyName} Key with {starsEarned} stars!";
            }
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning("🔑 Key Unlocked Canvas is not assigned! Saving key immediately.");
            #endif
            ActuallySaveKey();
            ReturnToLobby();
        }
    }

    // 🔑 Handle Continue Button Click
    private void OnKeyUnlockedContinueClicked()
    {
        #if UNITY_EDITOR
        Debug.Log($"🔑 Continue button clicked - saving {pendingKeyName} key now");
        #endif

        // Save the key
        ActuallySaveKey();

        // Hide the key unlocked canvas
        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);

        // Small delay to ensure event is processed before returning to lobby
        StartCoroutine(DelayedReturnToLobby());
    }

    private IEnumerator DelayedReturnToLobby()
    {
        // Wait a frame to ensure the KeyCollectionEvents are processed
        yield return null;
        ReturnToLobby();
    }

    // 🔑 Actually save the key to GameData and update UI
    private void ActuallySaveKey()
    {
        if (!pendingKeyUnlock || string.IsNullOrEmpty(pendingKeyName))
        {
            #if UNITY_EDITOR
            Debug.LogWarning("🔑 No pending key to save!");
            #endif
            return;
        }

        #if UNITY_EDITOR
        Debug.Log($"🔑 Saving {pendingKeyName} key to GameData...");
        #endif

        // Save the key based on the key name
        switch (pendingKeyName.ToLower())
        {
            case "sugaria":
                GameDataManager.Instance.CollectSugariaKey();
                break;
            case "preservia":
                GameDataManager.Instance.CollectPreserviaKey();
                break;
            case "allerthia":
                GameDataManager.Instance.CollectAllerthiaKey();
                break;
            case "ocr":
                GameDataManager.Instance.CollectOCRScannerKey();
                break;
            default:
                #if UNITY_EDITOR
                Debug.LogWarning($"🔑 Unknown key name: {pendingKeyName}");
                #endif
                break;
        }

        // Trigger the event for UI updates (this will update Global Map)
        KeyCollectionEvents.TriggerKeyCollected(pendingKeyName);

        #if UNITY_EDITOR
        Debug.Log($"🔑 Key '{pendingKeyName}' saved successfully! Global Map button should now be enabled.");
        #endif

        // Reset pending flag
        pendingKeyUnlock = false;
        pendingKeyName = "";
    }

    // 🔑 Return to lobby
    private void ReturnToLobby()
    {
        #if UNITY_EDITOR
        Debug.Log("Returning to lobby...");
        #endif

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

        #if UNITY_EDITOR
        Debug.Log("=== HOME BUTTON COMPLETE ===");
        #endif
    }

    // ========== RESTART BUTTON ==========
    private void OnRestartClicked()
    {
        if (!isCountingAnimationComplete) return;

        #if UNITY_EDITOR
        Debug.Log("=== RESTART BUTTON CLICKED ===");
        #endif

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();

        AddRewardsToGameData();

        PlayRestartMusic();
        ResetGameEndState();

        DisableObjectsOnHomeOrRestart();

        #if UNITY_EDITOR
        Debug.Log("STEP 1: Performing complete game reset...");
        #endif
        ResetMinigames();
        ResetAllContinueButtons();

        if (gameManager != null)
        {
            gameManager.FullGameReset();
            #if UNITY_EDITOR
            Debug.Log("GameManager fully reset");
            #endif
        }

        #if UNITY_EDITOR
        Debug.Log("STEP 2: Teleporting to lobby point...");
        #endif
        TeleportPlayerToLobbyPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        #if UNITY_EDITOR
        Debug.Log("STEP 3: Playing restart timeline...");
        #endif
        StartCoroutine(PlayRestartTimeline());

        StartCoroutine(RestoreCameraBlendAfterTeleport());

        ForceEnableBackgroundMusic();

        #if UNITY_EDITOR
        Debug.Log("=== RESTART BUTTON COMPLETE ===");
        Debug.Log($"Rewards added: +{totalCoins} Coins, +{totalExp} EXP");
        #endif
    }

    // Coroutine to play restart timeline
    private IEnumerator PlayRestartTimeline()
    {
        if (restartPlayableDirector == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("Cannot play restart timeline - Playable Director is not assigned!");
            #endif
            yield break;
        }

        if (restartPlayableAsset == null)
        {
            #if UNITY_EDITOR
            Debug.LogError("Cannot play restart timeline - Playable Asset is not assigned!");
            #endif
            yield break;
        }

        if (restartTimelineDelay > 0)
        {
            #if UNITY_EDITOR
            Debug.Log($"Waiting {restartTimelineDelay}s before playing restart timeline...");
            #endif
            yield return CoroutineYieldCache.WaitForSeconds(restartTimelineDelay);
        }

        #if UNITY_EDITOR
        Debug.Log("Playing restart timeline...");
        #endif

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

        #if UNITY_EDITOR
        Debug.Log("Restart timeline finished - Game is now ready to play");
        #endif

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private void OnNextClicked()
    {
        if (!isCountingAnimationComplete) return;

        #if UNITY_EDITOR
        Debug.Log("=== NEXT BUTTON CLICKED ===");
        #endif

        StopCountAudio();
        OnButtonClicked();

        ForceEnableBackgroundMusic();

        AddRewardsToGameData();

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
        #if UNITY_EDITOR
        Debug.Log("=== COMPLETE MINIGAMES RESET (HOME BUTTON - NO STARTING SEQUENCE) ===");
        #endif

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

        #if UNITY_EDITOR
        Debug.Log("=== MINIGAMES COMPLETELY RESET (HOME BUTTON) ===");
        #endif
    }

    public void ResetMinigames()
    {
        #if UNITY_EDITOR
        Debug.Log("=== COMPLETE MINIGAMES RESET (RESTART BUTTON) ===");
        #endif

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

            #if UNITY_EDITOR
            Debug.Log("StartingSequenceManager reset for restart");
            #endif
        }

        ResetTorchMinigame();

        AssessmentTrigger assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
        if (assessmentTrigger != null) assessmentTrigger.ForceResetForNewGame();

        EndGameTrigger endGameTrigger = FindObjectOfType<EndGameTrigger>();
        if (endGameTrigger != null) endGameTrigger.ResetEndTrigger();

        ResetObjectsToInitialState();
        ResetOneTimeAnimations();

        #if UNITY_EDITOR
        Debug.Log("=== MINIGAMES COMPLETELY RESET (RESTART BUTTON) ===");
        #endif
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
        if (keyUnlockedCanvas != null && keyUnlockedCanvas.activeSelf) keyUnlockedCanvas.SetActive(false);

        if (pointsText != null) pointsText.text = "0";
        if (timeText != null) timeText.text = "00:00";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";

        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (gameSummaryParent != null) gameSummaryParent.SetActive(false);

        // Reset pending key flags
        pendingKeyUnlock = false;
        pendingKeyName = "";
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
        // This method is now handled by the pending key system
        // Keeping for compatibility
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

    public int GetRequiredStarsForKey()
    {
        return starsRequiredForKey;
    }

    public void ForceStopCountAudio()
    {
        StopCountAudio();
    }

    // ========== IN-GAME SETTINGS BUTTON SUPPORT ==========
    // These public methods allow InGameSettingsButton to perform the same
    // restart/home actions that the game-end buttons do.

    /// <summary>
    /// Full in-game restart: resets everything and plays the farmer NPC cutscene.
    /// Call this from InGameSettingsButton instead of manually replicating logic.
    /// </summary>
    public void PerformInGameRestart()
    {
        #if UNITY_EDITOR
        Debug.Log("=== IN-GAME RESTART (via GameEndManager) ===");
        #endif

        ForceEnableBackgroundMusic();
        PlayRestartMusic();
        ResetGameEndState();
        DisableObjectsOnHomeOrRestart();

        ResetMinigames();
        ResetAllContinueButtons();

        if (gameManager != null)
        {
            gameManager.FullGameReset();
            #if UNITY_EDITOR
            Debug.Log("GameManager fully reset");
            #endif
        }

        TeleportPlayerToLobbyPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        // Play the restart timeline (farmer NPC cutscene)
        StartCoroutine(PlayRestartTimeline());

        StartCoroutine(RestoreCameraBlendAfterTeleport());
        ForceEnableBackgroundMusic();

        #if UNITY_EDITOR
        Debug.Log("=== IN-GAME RESTART COMPLETE ===");
        #endif
    }

    /// <summary>
    /// Full in-game home: stops the game, resets everything, returns to lobby.
    /// Call this from InGameSettingsButton instead of manually replicating logic.
    /// </summary>
    public void PerformInGameHome()
    {
        #if UNITY_EDITOR
        Debug.Log("=== IN-GAME HOME (via GameEndManager) ===");
        #endif

        ForceEnableBackgroundMusic();

        // End the active game first
        if (gameManager != null && gameManager.IsGameActive())
        {
            gameManager.EndGame();
        }

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

        if (playerController != null && !playerController.enabled)
            playerController.enabled = true;

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        if (gameManager != null)
        {
            gameManager.FullGameReset();
        }

        StartCoroutine(RestoreCameraBlendAfterTeleport());
        ForceEnableBackgroundMusic();

        #if UNITY_EDITOR
        Debug.Log("=== IN-GAME HOME COMPLETE ===");
        #endif
    }

    // ========== DEBUG METHODS ==========

    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        starsEarned = 3;
        HandleLevelComplete();
    }

    [ContextMenu("Test Win without Key")]
    public void TestWinWithoutKey()
    {
        starsEarned = 1;
        HandleLevelComplete();
    }

    [ContextMenu("Test Force Save Sugaria Key")]
    public void TestForceSaveSugariaKey()
    {
        starsEarned = 3;
    }
}
