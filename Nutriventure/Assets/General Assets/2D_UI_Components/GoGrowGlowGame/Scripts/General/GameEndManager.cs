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

    [Header("PLAYABLE DIRECTOR OBJECT CONTROL")]
    [SerializeField] private GameObject playableDirectorObject; // The entire GameObject containing the PlayableDirector
    [SerializeField] private float homeButtonReactivateDelay = 2f; // Time to wait before reactivating on Home button

    [Header("References")]
    [SerializeField] private GoGrowGlowGameManager gameManager;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private GrowAssessmentManager growAssessmentManager;
    [SerializeField] private GlowPartManager glowPartManager;
    [SerializeField] private StartingSequenceManager startingSequenceManager;

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
    private bool isPlayableDirectorReactivating = false; // Track if we're waiting to reactivate

    // Audio control variables
    private bool isCountAudioPlaying = false;
    private float lastTickTime = 0f;

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

        // Warn if player armature is not assigned
        if (playerArmature == null)
        {
            Debug.LogWarning("Player Armature is not assigned in the Inspector! Please drag the PlayerArmature GameObject to the field.");
        }

        // Warn if playable director object is not assigned
        if (playableDirectorObject == null)
        {
            Debug.LogWarning("Playable Director Object is not assigned in the Inspector! Timeline control will not work.");
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

        // IMPORTANT: Don't auto-search for background music
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
        }
    }

    // ========== PLAYABLE DIRECTOR OBJECT CONTROL ==========

    /// <summary>
    /// Disables the Playable Director GameObject immediately
    /// Called when game ends or player reaches result point
    /// </summary>
    private void DisablePlayableDirectorObject()
    {
        if (playableDirectorObject != null)
        {
            playableDirectorObject.SetActive(false);
            Debug.Log("?? Playable Director Object DISABLED - Home button clicked");
        }
    }

    /// <summary>
    /// Enables the Playable Director GameObject immediately
    /// Called when restart button is clicked
    /// </summary>
    private void EnablePlayableDirectorObject()
    {
        if (playableDirectorObject != null)
        {
            playableDirectorObject.SetActive(true);
            Debug.Log("?? Playable Director Object ENABLED - Restart button clicked");
        }
    }

    /// <summary>
    /// Disables the Playable Director GameObject and re-enables it after a delay
    /// Called when home button is clicked
    /// </summary>
    private void DisableAndReenablePlayableDirectorObject()
    {
        if (playableDirectorObject != null)
        {
            // Stop any existing reactivation coroutine
            if (isPlayableDirectorReactivating)
            {
                StopCoroutine(ReenablePlayableDirectorObjectAfterDelay(homeButtonReactivateDelay));
                isPlayableDirectorReactivating = false;
            }

            // Disable immediately
            playableDirectorObject.SetActive(false);
            Debug.Log("?? Playable Director Object DISABLED - Home button clicked");

            // Start coroutine to re-enable after delay
            StartCoroutine(ReenablePlayableDirectorObjectAfterDelay(homeButtonReactivateDelay));
        }
    }

    private IEnumerator ReenablePlayableDirectorObjectAfterDelay(float delay)
    {
        isPlayableDirectorReactivating = true;
        Debug.Log($"?? Waiting {delay} seconds before re-enabling Playable Director Object...");

        yield return new WaitForSeconds(delay);

        if (playableDirectorObject != null)
        {
            playableDirectorObject.SetActive(true);
            Debug.Log("?? Playable Director Object RE-ENABLED after delay");
        }

        isPlayableDirectorReactivating = false;
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

    // ========== GAME END SCREEN ==========

    public void ShowGameEndScreen(bool playerWon)
    {
        Debug.Log($"=== SHOWING GAME END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");

        // CRITICAL: Disable Playable Director Object when game ends / result point reached
        DisablePlayableDirectorObject();

        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();

        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        starsEarned = CalculateStarRating(remainingHearts, completionTime);
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

        if (questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                if ((quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress) &&
                    starsEarned >= 2 && playerWon)
                {
                    shouldShowKey = true;
                    isFirstTimeCompletion = true;
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
    // Disable Playable Director Object -> Teleport -> NO reactivation
    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== HOME BUTTON CLICKED ===");

        StopCountAudio();
        OnButtonClicked();
        PlayLobbyMusic();
        ResetGameEndState();
        ResetMinigames(); // This now ONLY resets minigames, no PlayableDirector control
        ResetAllContinueButtons();

        // DISABLE Playable Director Object - stays disabled until Restart
        DisablePlayableDirectorObject();

        // IMMEDIATELY switch to player camera
        SwitchToPlayerCameraWithCut();

        // Teleport to LOBBY POINT
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

        Debug.Log("=== HOME BUTTON COMPLETE ===");
        Debug.Log("Playable Director Object DISABLED - Will remain disabled until Restart button");
    }


    // ========== RESTART BUTTON ==========
    // ENABLE Playable Director Object FIRST, then do everything else
    private void OnRestartClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== RESTART BUTTON CLICKED ===");

        // CRITICAL: Enable Playable Director Object IMMEDIATELY before any other logic
        EnablePlayableDirectorObject();

        StopCountAudio();
        OnButtonClicked();
        PlayRestartMusic();
        ResetGameEndState();

        Debug.Log("STEP 1: Performing complete game reset...");
        ResetMinigames();
        ResetAllContinueButtons();

        if (gameManager != null)
        {
            gameManager.FullGameReset(); // This no longer calls ResetMinigames() again
            Debug.Log("GameManager fully reset");
        }

        Debug.Log("STEP 2: Teleporting to lobby point...");
        TeleportPlayerToLobbyPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
            uiControlsCanvas.SetActive(true);

        EnableObjectsOnHomeButton();

        StartCoroutine(RestoreCameraBlendAfterTeleport());

        Debug.Log("=== RESTART BUTTON COMPLETE ===");
        Debug.Log("Playable Director Object ENABLED - Ready for timeline");
    }


    private void OnNextClicked()
    {
        if (!isCountingAnimationComplete) return;

        Debug.Log("=== NEXT BUTTON CLICKED ===");

        StopCountAudio();
        OnButtonClicked();
        PlayLobbyMusic();
        ResetGameEndState();
        ResetMinigames();

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
    }

    // ========== RESET METHODS ==========

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

    public void ResetMinigames()
    {
        Debug.Log("=== COMPLETE MINIGAMES RESET ===");

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
        }

        ResetTorchMinigame();

        AssessmentTrigger assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
        if (assessmentTrigger != null) assessmentTrigger.ForceResetForNewGame();

        EndGameTrigger endGameTrigger = FindObjectOfType<EndGameTrigger>();
        if (endGameTrigger != null) endGameTrigger.ResetEndTrigger();

        ResetObjectsToInitialState();
        ResetOneTimeAnimations();

        Debug.Log("=== MINIGAMES COMPLETELY RESET ===");
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
        // CRITICAL: Disable Playable Director Object when game ends
        DisablePlayableDirectorObject();

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

    public int GetStarsEarned() => starsEarned;
    public int GetTotalCoins() => totalCoins;
    public int GetTotalExp() => totalExp;
    public float GetCompletionTime() => completionTime;
    public int GetPlayerPoints() => playerPoints;
    public bool IsFirstTimeCompletion() => isFirstTimeCompletion;

    public void ForceStopCountAudio()
    {
        StopCountAudio();
    }
}