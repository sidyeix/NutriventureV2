using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Cinemachine;

public class K4GameEndManager : MonoBehaviour
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

    [Header("UI Controls")]
    [SerializeField] private GameObject uiControlsCanvas;
    [SerializeField] private MechanicsBoardManager mechanicsBoardManager;

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

        if (mechanicsBoardManager != null)
            mechanicsBoardManager = FindObjectOfType<MechanicsBoardManager>();

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

        // Make sure mechanics board is hidden initially
        if (mechanicsBoardManager != null && mechanicsBoardManager.mechanicsBoard != null)
        {
            mechanicsBoardManager.mechanicsBoard.SetActive(false);
        }

        if (storeInitialPositionsOnStart)
        {
            StoreInitialTransforms();
        }

        // IMPORTANT: Don't auto-search for background music
        // It should be assigned in the Inspector
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is not assigned in the Inspector!");
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

    public void ShowGameEndScreen(bool playerWon)
    {
        Debug.Log($"=== SHOWING GAME END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");

        // Hide stars when showing summary
        HideStarsWhenShowingSummary();

        // Disable objects that should be hidden when game ends
        DisableObjectsOnGameEnd();

        // Switch to game end camera with CUT blend
        SwitchToGameEndCameraWithCut();

        // Teleport player to result spawn point
        TeleportPlayerToResultPoint();

        // Collect game data
        completionTime = gameManager.GetGameTimer();
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance?.CalculateTimeBonus(completionTime);
            playerPoints = Kingdom4ScoreManager.Instance.GetFinalScore();
        }
        else
        {
            playerPoints = 0;
        }



        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        // Calculate star rating
        starsEarned = CalculateStarRating(remainingHearts, completionTime);

        // Calculate rewards
        CalculateRewards();

        // Set up UI - Set correct background based on win/lose
        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
        }

        // Handle character animation based on stars
        HandleCharacterAnimation(playerWon, starsEarned);

        // Handle background music - ONLY if we have a valid source
        HandleBackgroundMusic(playerWon && starsEarned > 0);

        // Handle win/lose specific logic
        if (!playerWon)
        {
            HandleLose();
        }
        else
        {
            HandleWin();
        }

        // Handle key unlocked object based on quest status AND stars
        HandleKeyUnlockedObject(playerWon);

        // Show the game summary
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(true);

        // Reset counting animation flag
        isCountingAnimationComplete = false;

        // Start animations
        StartCoroutine(GameEndSequence());
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
                }
            }
        }

        keyUnlockedObject.SetActive(shouldShowKey);
    }

    private void HandleCharacterAnimation(bool playerWon, int stars)
    {
        if (characterAnimator == null) return;

        // Reset all animation parameters first
        characterAnimator.SetBool(danceParameter, false);
        characterAnimator.SetBool(thinkParameter, false);

        // Set animation based on stars
        if (stars == 0)
        {
            characterAnimator.SetBool(thinkParameter, true);
        }
        else if (playerWon)
        {
            characterAnimator.SetBool(danceParameter, true);
        }
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
            // CRITICAL: Check if this is our background music source
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

        // Also disable mechanics board if it's open
        if (mechanicsBoardManager != null && mechanicsBoardManager.mechanicsBoard != null &&
            mechanicsBoardManager.mechanicsBoard.activeSelf)
        {
            mechanicsBoardManager.CloseMechanicsBoard();
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
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        if (playerController != null && resultCharacterSpawnPoint != null)
        {
            playerController.transform.position = resultCharacterSpawnPoint.position;
            playerController.transform.rotation = resultCharacterSpawnPoint.rotation;
        }
    }

    private void TeleportPlayerToStartingPoint()
    {
        if (playerController != null && startingPoint != null)
        {
            playerController.transform.position = startingPoint.position;
            playerController.transform.rotation = startingPoint.rotation;
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

        pointsText.text = "0";
        timeText.text = "00:00";
        coinsText.text = "0";
        expText.text = "0";

        yield return new WaitForSeconds(0.3f);

        float elapsedTime = 0f;
        int lastIntegerValue = 0;

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
            if (currentInteger > lastIntegerValue && countTickSound != null && countAudioSource != null)
            {
                countAudioSource.PlayOneShot(countTickSound);
                lastIntegerValue = currentInteger;
            }

            pointsText.text = Mathf.FloorToInt(currentPoints).ToString("N0");
            timeText.text = FormatTime(currentTime);
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");

            yield return null;
        }

        pointsText.text = playerPoints.ToString("N0");
        timeText.text = FormatTime(completionTime);
        coinsText.text = totalCoins.ToString("N0");
        expText.text = totalExp.ToString("N0");

        if (countCompleteSound != null && countAudioSource != null)
        {
            countAudioSource.PlayOneShot(countCompleteSound);
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
            case 3:
                baseCoins = 1000; baseExp = 500; break;
            case 2:
                baseCoins = 700; baseExp = 300; break;
            case 1:
                baseCoins = 400; baseExp = 150; break;
            default:
                baseCoins = 100; baseExp = 50; break;
        }

        totalExp = baseExp;
        totalCoins = baseCoins;
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
        if (timeText != null) timeText.text = "00:00";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";

        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (gameSummaryParent != null) gameSummaryParent.SetActive(false);
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
        if (playerController != null) playerController.enabled = true;
    }

    public void ResetMinigames()
    {
        if (growAssessmentManager != null) growAssessmentManager.EndGrowAssessment();
        ResetGlowTowers();
        ResetDayNightTransition();

        if (startingSequenceManager != null) startingSequenceManager.EnableAllControlsAndUI();
        ResetTorchMinigame();

        AssessmentTrigger assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
        if (assessmentTrigger != null) assessmentTrigger.ForceResetForNewGame();

        EndGameTrigger endGameTrigger = FindObjectOfType<EndGameTrigger>();
        if (endGameTrigger != null) endGameTrigger.ResetEndTrigger();

        ResetObjectsToInitialState();
        ResetOneTimeAnimations();
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

    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete) return;

        OnButtonClicked();
        PlayLobbyMusic();
        ResetGameEndState();
        ResetMinigames();

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
            }
        }

        StartCoroutine(RestoreCameraBlendAfterTeleport());
    }

    private IEnumerator RestoreCameraBlendAfterTeleport()
    {
        yield return null;
        RestoreOriginalCameraBlend();
    }

    // FIXED: This method now ONLY changes the music clip, doesn't disable/create AudioSources
    private void PlayLobbyMusic()
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play lobby music.");
            return;
        }

        if (lobbyMusicClip != null)
        {
            // Just change the clip, don't disable/enable the GameObject
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
        if (!isCountingAnimationComplete) return;

        OnButtonClicked();
        ResetGameEndState();
        ResetMinigames();
        TeleportPlayerToStartingPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        if (gameManager != null)
            gameManager.FullGameReset();

        if (mechanicsBoardManager != null)
            mechanicsBoardManager.OpenMechanicsBoard();

        PlayRestartMusic();
        StartCoroutine(RestoreCameraBlendAfterTeleport());
    }

    // FIXED: This method now ONLY changes the music clip
    private void PlayRestartMusic()
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("BackgroundMusicSource is null. Cannot play restart music.");
            return;
        }

        if (restartMusicClip != null)
        {
            // Just change the clip, don't disable/enable the GameObject
            backgroundMusicSource.Stop();
            backgroundMusicSource.clip = restartMusicClip;
            backgroundMusicSource.loop = false;
            backgroundMusicSource.Play();

            Debug.Log($"Changed to restart music: {restartMusicClip.name}");
        }
    }

    private void OnNextClicked()
    {
        if (!isCountingAnimationComplete) return;

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

        if (playerController != null && lobbyPoint != null)
        {
            playerController.transform.position = lobbyPoint.position;
            playerController.transform.rotation = lobbyPoint.rotation;
        }

        StartCoroutine(RestoreCameraBlendAfterTeleport());
    }

    public void HandleGameOver()
    {
        if (gameManager != null && gameManager.IsGameActive())
            gameManager.EndGame();

        completionTime = gameManager.GetGameTimer();
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance?.CalculateTimeBonus(completionTime);
            playerPoints = Kingdom4ScoreManager.Instance.GetFinalScore();
        }
        else
        {
            playerPoints = 0;
        }

        remainingHearts = 0;
        starsEarned = 0;
        CalculateRewards();
        ShowGameEndScreen(false);
    }

    public void HandleLevelComplete()
    {
        if (gameManager != null && gameManager.IsGameActive())
            gameManager.EndGame();

        completionTime = gameManager.GetGameTimer();
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance?.CalculateTimeBonus(completionTime);
            playerPoints = Kingdom4ScoreManager.Instance.GetFinalScore();
        }
        else
        {
            playerPoints = 0;
        }

        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());
        starsEarned = CalculateStarRating(remainingHearts, completionTime);
        ShowGameEndScreen(true);
    }

    public void TriggerLevelComplete() => HandleLevelComplete();
    public void TriggerGameOver() => HandleGameOver();

    // FIXED: This method now ONLY changes the music clip
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
            // Just change the clip, don't disable/enable the GameObject
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

    public void ShowGameEndAfterKingTimeline()
    {
        HideStarsWhenShowingSummary();
        DisableObjectsOnGameEnd();
        SwitchToGameEndCameraWithCut();
        TeleportPlayerToResultPoint();

        if (GoGrowGlowGameManager.Instance != null)
        {
            completionTime = GoGrowGlowGameManager.Instance.GetGameTimer();
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance?.CalculateTimeBonus(completionTime);
                playerPoints = Kingdom4ScoreManager.Instance.GetFinalScore();
            }
            else
            {
                playerPoints = 0;
            }

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
}