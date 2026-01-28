using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Cinemachine;

public class Kingdom4GameEndManager : MonoBehaviour
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
    [SerializeField] private TMP_Text collectedText;
    [SerializeField] private TMP_Text wagonHitsText;
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

    // Game end calculations
    private int starsEarned = 0;
    private int baseCoins = 0;
    private int baseExp = 0;
    private int totalCoins = 0;
    private int totalExp = 0;
    private float completionTime = 0f;
    private int playerScore = 0;
    private int allergensCollected = 0;
    private int wagonHits = 0;

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
            gameManager = FindObjectOfType<AllerthriaGameManager>();

        if (playerController == null)
            playerController = FindObjectOfType<ThirdPersonController>();

        if (questManager == null)
            questManager = QuestManager.Instance;

        if (scoreManager == null)
            scoreManager = FindObjectOfType<Kingdom4ScoreManager>();

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

        if (storeInitialPositionsOnStart)
        {
            StoreInitialTransforms();
        }

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
        Debug.Log($"=== SHOWING KINGDOM 4 END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");

        // Hide stars when showing summary
        HideStarsWhenShowingSummary();

        // Disable objects that should be hidden when game ends
        DisableObjectsOnGameEnd();

        // Switch to game end camera with CUT blend
        SwitchToGameEndCameraWithCut();

        // Teleport player to result spawn point
        TeleportPlayerToResultPoint();

        // Collect game data from Kingdom4ScoreManager
        if (scoreManager != null)
        {
            playerScore = scoreManager.GetFinalScore();
        }

        // Get data from AllerthriaGameManager
        if (gameManager != null)
        {
            allergensCollected = gameManager.collectedAllergens.Count;
        }

        // Get wagon hits from Kingdom4ScoreManager
        if (scoreManager != null)
        {
            wagonHits = scoreManager.totalWagonHits;
        }

        // Calculate star rating for Kingdom 4
        starsEarned = CalculateKingdom4StarRating(playerWon);

        // Calculate rewards
        CalculateKingdom4Rewards();

        // Set up UI - Set correct background based on win/lose
        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
        }

        // Handle character animation based on stars
        HandleCharacterAnimation(playerWon, starsEarned);

        // Handle background music
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

        // Handle key unlocked object based on quest status
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
        if (pointsText == null || timeText == null || collectedText == null || wagonHitsText == null)
            yield break;

        pointsText.text = "0";
        timeText.text = "00:00";
        collectedText.text = "0/9";
        wagonHitsText.text = "0";

        yield return new WaitForSeconds(0.3f);

        float elapsedTime = 0f;
        int lastIntegerValue = 0;

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            float currentScore = Mathf.Lerp(0, playerScore, smoothProgress);
            float currentCollected = Mathf.Lerp(0, allergensCollected, smoothProgress);
            float currentWagonHits = Mathf.Lerp(0, wagonHits, smoothProgress);

            int currentInteger = Mathf.FloorToInt(currentScore);
            if (currentInteger > lastIntegerValue && countTickSound != null && countAudioSource != null)
            {
                countAudioSource.PlayOneShot(countTickSound);
                lastIntegerValue = currentInteger;
            }

            pointsText.text = Mathf.FloorToInt(currentScore).ToString("N0");
            collectedText.text = $"{Mathf.FloorToInt(currentCollected)}/9";
            wagonHitsText.text = Mathf.FloorToInt(currentWagonHits).ToString("N0");

            yield return null;
        }

        pointsText.text = playerScore.ToString("N0");
        collectedText.text = $"{allergensCollected}/9";
        wagonHitsText.text = wagonHits.ToString("N0");

        if (countCompleteSound != null && countAudioSource != null)
        {
            countAudioSource.PlayOneShot(countCompleteSound);
        }
    }

    private int CalculateKingdom4StarRating(bool playerWon)
    {
        if (!playerWon) return 0;

        // Kingdom 4 star calculation based on performance
        if (allergensCollected == 9 && wagonHits == 0) return 3; // Perfect
        if (allergensCollected >= 7 && wagonHits <= 1) return 2; // Good
        if (allergensCollected >= 5 && wagonHits <= 3) return 1; // OK
        return 0; // Poor or failed
    }

    private void CalculateKingdom4Rewards()
    {
        switch (starsEarned)
        {
            case 3:
                baseCoins = 1500; baseExp = 1500; break; // Perfect
            case 2:
                baseCoins = 800; baseExp = 800; break;   // Good
            case 1:
                baseCoins = 300; baseExp = 300; break;   // OK
            default:
                baseCoins = 100; baseExp = 100; break;   // Participation
        }

        // Bonus for collecting all allergens
        if (allergensCollected == 9)
        {
            baseCoins += 500;
            baseExp += 500;
        }

        // Penalty for wagon hits
        int wagonPenalty = wagonHits * 50;
        baseCoins = Mathf.Max(0, baseCoins - wagonPenalty);
        baseExp = Mathf.Max(0, baseExp - wagonPenalty);

        // Score bonus
        int scoreBonus = Mathf.FloorToInt(playerScore / 10);
        
        totalCoins = baseCoins + scoreBonus;
        totalExp = baseExp + scoreBonus;
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
        if (collectedText != null) collectedText.text = "0/9";
        if (wagonHitsText != null) wagonHitsText.text = "0";

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

    public void ResetKingdom4Game()
    {
        ResetObjectsToInitialState();
        
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
    }

    private void OnHomeClicked()
    {
        if (!isCountingAnimationComplete) return;

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
            }
        }

        StartCoroutine(RestoreCameraBlendAfterTeleport());
    }

    private IEnumerator RestoreCameraBlendAfterTeleport()
    {
        yield return null;
        RestoreOriginalCameraBlend();
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
        if (!isCountingAnimationComplete) return;

        OnButtonClicked();
        ResetGameEndState();
        ResetKingdom4Game();
        TeleportPlayerToStartingPoint();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = playerCameraPriority;

        PlayRestartMusic();
        StartCoroutine(RestoreCameraBlendAfterTeleport());
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

    public void HandleKingdom4GameOver()
    {
        completionTime = 0f; // Kingdom 4 doesn't track time, but you could add it
        playerScore = scoreManager != null ? scoreManager.GetFinalScore() : 0;
        allergensCollected = gameManager != null ? gameManager.collectedAllergens.Count : 0;
        wagonHits = scoreManager != null ? scoreManager.totalWagonHits : 0;
        starsEarned = 0;
        CalculateKingdom4Rewards();
        ShowGameEndScreen(false);
    }

    public void HandleKingdom4Complete()
    {
        completionTime = 0f;
        playerScore = scoreManager != null ? scoreManager.GetFinalScore() : 0;
        allergensCollected = gameManager != null ? gameManager.collectedAllergens.Count : 0;
        wagonHits = scoreManager != null ? scoreManager.totalWagonHits : 0;
        starsEarned = CalculateKingdom4StarRating(true);
        CalculateKingdom4Rewards();
        ShowGameEndScreen(true);
    }

    public void TriggerKingdom4Complete() => HandleKingdom4Complete();
    public void TriggerKingdom4GameOver() => HandleKingdom4GameOver();

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

    public int GetStarsEarned() => starsEarned;
    public int GetTotalCoins() => totalCoins;
    public int GetTotalExp() => totalExp;
    public int GetPlayerScore() => playerScore;
    public bool IsFirstTimeCompletion() => isFirstTimeCompletion;
    public int GetAllergensCollected() => allergensCollected;
    public int GetWagonHits() => wagonHits;
}