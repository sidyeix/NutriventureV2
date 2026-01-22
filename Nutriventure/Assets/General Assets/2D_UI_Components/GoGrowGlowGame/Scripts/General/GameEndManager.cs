using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using Cinemachine;

public class GameEndManager : MonoBehaviour
{
    [Header("Star Rating System")]
    [SerializeField] private GameObject starsContainer;
    [SerializeField] private Animator starsAnimator; // Single animator for all stars
    [SerializeField] private string starParameter = "Stars"; // Parameter name in animator

    [Header("Game Summary UI")]
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private GameObject buttonContainer;
    [SerializeField] private float countAnimationDuration = 2f;

    [Header("Result Background")]
    [SerializeField] private Image resultBackground;
    [SerializeField] private Sprite winBackground;
    [SerializeField] private Sprite loseBackground;

    [Header("Game Objects Management")]
    [SerializeField] private GameObject gameSummaryParent;
    [SerializeField] private List<GameObject> objectsToEnableOnLose = new List<GameObject>();
    [SerializeField] private List<GameObject> objectsToEnableOnWin = new List<GameObject>();
    [SerializeField] private List<GameObject> objectsToDisableOnGameEnd = new List<GameObject>(); // These remain disabled
    [SerializeField] private List<GameObject> objectsToEnableOnHomeButton = new List<GameObject>(); // Objects to enable when Home is clicked
    [SerializeField] private GameObject keyUnlockedObject;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera gameEndVirtualCamera;
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    [SerializeField] private int gameEndCameraPriority = 100;
    [SerializeField] private int playerCameraPriority = 10;

    [Header("Spawn Points")]
    [SerializeField] private Transform resultCharacterSpawnPoint;
    [SerializeField] private Transform lobbyPoint;
    [SerializeField] private Transform startingPoint; // NEW: Starting point for restart

    [Header("Quest System")]
    [SerializeField] private string kingdomID = "general_quests";
    [SerializeField] private string questID = "0001";

    [Header("Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioClip winMusicClip;        // For 1-3 stars
    [SerializeField] private AudioClip loseMusicClip;       // For 0 stars
    [SerializeField] private AudioClip restartMusicClip;    // When restarting
    [SerializeField] private AudioClip lobbyMusicClip;      // When going to lobby/home

    [Header("Object Reset System")]
    [SerializeField] private List<GameObject> objectsToReset = new List<GameObject>();
    [SerializeField] private bool storeInitialPositionsOnStart = true;

    [Header("Animator Reset System")]
    [SerializeField] private List<Animator> animatorsToReset = new List<Animator>();
    [SerializeField] private string defaultStateName = "Default";

    [Header("Character Animation")]
    [SerializeField] private Animator characterAnimator; // Reference to player's animator
    [SerializeField] private string danceParameter = "isDancing"; // Parameter for dancing animation
    [SerializeField] private string thinkParameter = "isThinking"; // Parameter for thinking animation

    [Header("UI Controls")]
    [SerializeField] private GameObject uiControlsCanvas; // Reference to UI Controls Canvas
    [SerializeField] private MechanicsBoardManager mechanicsBoardManager; // Reference to Game Mechanics Board Manager

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

    private Dictionary<GameObject, TransformData> initialTransformData = new Dictionary<GameObject, TransformData>();

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

        if (mechanicsBoardManager == null)
            mechanicsBoardManager = FindObjectOfType<MechanicsBoardManager>();

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
                Debug.Log("Found StartingPoint: " + startingPoint.name);
            }
            else
            {
                Debug.LogWarning("StartingPoint not found and not assigned! Player will not respawn at correct position on restart.");
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

        // Make sure UI controls are enabled initially (if assigned)
        if (uiControlsCanvas != null)
            uiControlsCanvas.SetActive(true);

        // Make sure mechanics board is hidden initially (controlled by MechanicsBoardManager)
        if (mechanicsBoardManager != null && mechanicsBoardManager.mechanicsBoard != null)
        {
            mechanicsBoardManager.mechanicsBoard.SetActive(false);
        }

        if (storeInitialPositionsOnStart)
        {
            StoreInitialTransforms();
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

        Debug.Log($"Stored initial transforms for {initialTransformData.Count} objects");
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

        Debug.Log($"Reset {initialTransformData.Count} objects to initial state");
    }

    public void ShowGameEndScreen(bool playerWon)
    {
        Debug.Log($"=== SHOWING GAME END SCREEN - {(playerWon ? "WIN" : "LOSE")} ===");

        // Disable objects that should be hidden when game ends
        DisableObjectsOnGameEnd();

        // Switch to game end camera instantly
        SwitchToGameEndCamera();

        // Teleport player to result spawn point
        TeleportPlayerToResultPoint();

        // Collect game data
        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        Debug.Log($"Game End Data - Time: {completionTime}, Points: {playerPoints}, Hearts: {remainingHearts}");

        // Calculate star rating
        starsEarned = CalculateStarRating(remainingHearts, completionTime);
        Debug.Log($"Stars Earned: {starsEarned}");

        // Calculate rewards
        CalculateRewards();

        // Set up UI - Set correct background based on win/lose
        if (resultBackground != null)
        {
            resultBackground.sprite = playerWon ? winBackground : loseBackground;
            Debug.Log($"Set result background to {(playerWon ? "Win" : "Lose")} background");
        }

        // Handle character animation based on stars
        HandleCharacterAnimation(playerWon, starsEarned);

        // Handle background music
        HandleBackgroundMusic(playerWon && starsEarned > 0); // Only play win music if at least 1 star

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

        // Start animations
        StartCoroutine(GameEndSequence());
    }

    private void HandleKeyUnlockedObject(bool playerWon)
    {
        if (keyUnlockedObject == null) return;

        // Reset key unlocked object first
        keyUnlockedObject.SetActive(false);

        // Check if we should show the key unlocked object
        bool shouldShowKey = false;

        if (questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                Debug.Log($"Quest found: {quest.questID}, Status: {quest.status}, Stars: {starsEarned}");

                // Only show key if:
                // 1. Quest is NotStarted or InProgress
                // 2. Player got 2-3 stars
                // 3. Player won the game
                if ((quest.status == QuestStatus.NotStarted || quest.status == QuestStatus.InProgress) &&
                    starsEarned >= 2 &&
                    playerWon)
                {
                    shouldShowKey = true;
                    isFirstTimeCompletion = true;
                    Debug.Log("Key unlocked: Quest is NotStarted/InProgress AND player got 2-3 stars AND won the game");
                }
                else
                {
                    Debug.Log($"Key not unlocked - Conditions not met: Status={quest.status}, Stars={starsEarned}, Won={playerWon}");
                }
            }
            else
            {
                Debug.LogWarning($"Quest not found: {questID}");
            }
        }
        else
        {
            Debug.LogWarning("QuestManager not found");
        }

        // Activate or deactivate the key object
        keyUnlockedObject.SetActive(shouldShowKey);
        Debug.Log($"Key Unlocked Object {(shouldShowKey ? "activated" : "deactivated")}");
    }

    private void HandleCharacterAnimation(bool playerWon, int stars)
    {
        if (characterAnimator == null)
        {
            Debug.LogWarning("Character animator not assigned!");
            return;
        }

        // Reset all animation parameters first
        characterAnimator.SetBool(danceParameter, false);
        characterAnimator.SetBool(thinkParameter, false);

        // Set animation based on stars
        if (stars == 0)
        {
            // 0 stars - thinking animation
            characterAnimator.SetBool(thinkParameter, true);
            Debug.Log("Set character to thinking animation (0 stars)");
        }
        else if (playerWon)
        {
            // Win with stars - dancing animation
            characterAnimator.SetBool(danceParameter, true);
            Debug.Log("Set character to dancing animation (win with stars)");
        }
        // If lose with stars (unlikely), no special animation
    }

    private void ResetCharacterAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(danceParameter, false);
            characterAnimator.SetBool(thinkParameter, false);
            Debug.Log("Reset character animation parameters");
        }
    }

    private void DisableObjectsOnGameEnd()
    {
        Debug.Log($"Disabling {objectsToDisableOnGameEnd.Count} objects on game end");

        foreach (GameObject obj in objectsToDisableOnGameEnd)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled object: {obj.name}");
            }
        }

        // Also disable UI controls canvas if assigned
        if (uiControlsCanvas != null && uiControlsCanvas.activeSelf)
        {
            uiControlsCanvas.SetActive(false);
            Debug.Log("Disabled UI Controls Canvas");
        }

        // Also disable mechanics board if it's open
        if (mechanicsBoardManager != null && mechanicsBoardManager.mechanicsBoard != null &&
            mechanicsBoardManager.mechanicsBoard.activeSelf)
        {
            mechanicsBoardManager.CloseMechanicsBoard();
            Debug.Log("Closed Mechanics Board");
        }
    }

    private void SwitchToGameEndCamera()
    {
        if (gameEndVirtualCamera != null)
        {
            // Disable player camera
            if (playerFollowCamera != null)
            {
                playerFollowCamera.Priority = 0;
                playerFollowCamera.gameObject.SetActive(false);
            }

            // Enable game end camera with high priority
            gameEndVirtualCamera.gameObject.SetActive(true);
            gameEndVirtualCamera.Priority = gameEndCameraPriority;

            Debug.Log("Switched to game end camera instantly");
        }
        else
        {
            Debug.LogWarning("Game end virtual camera not assigned!");
        }
    }

    private void SwitchToPlayerCameraInstantly()
    {
        if (playerFollowCamera != null)
        {
            // Disable game end camera
            if (gameEndVirtualCamera != null)
            {
                gameEndVirtualCamera.Priority = 0;
                gameEndVirtualCamera.gameObject.SetActive(false);
            }

            // Enable player camera instantly
            playerFollowCamera.gameObject.SetActive(true);
            playerFollowCamera.Priority = playerCameraPriority;

            // Force camera to update immediately
            CinemachineBrain brain = FindObjectOfType<CinemachineBrain>();
            if (brain != null)
            {
                brain.ManualUpdate();
            }

            Debug.Log("Switched back to player camera INSTANTLY");
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        if (playerController != null && resultCharacterSpawnPoint != null)
        {
            playerController.transform.position = resultCharacterSpawnPoint.position;
            playerController.transform.rotation = resultCharacterSpawnPoint.rotation;
            Debug.Log($"Player teleported to result spawn point");
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
        else if (playerController != null)
        {
            Debug.LogWarning("StartingPoint not assigned! Player will remain at current position.");
        }
    }

    private void HandleLose()
    {
        Debug.Log("Handling lose state...");

        // Enable objects for losing state
        foreach (GameObject obj in objectsToEnableOnLose)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
                Debug.Log($"Enabled object on lose: {obj.name}");
            }
        }
    }

    private void HandleWin()
    {
        Debug.Log("Handling win state...");

        // Enable objects for winning state
        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
                Debug.Log($"Enabled object on win: {obj.name}");
            }
        }
    }

    // NOTE: We DON'T re-enable objects that were disabled on game end
    // These remain disabled permanently after game ends

    private void EnableObjectsOnHomeButton()
    {
        Debug.Log($"Enabling {objectsToEnableOnHomeButton.Count} objects on Home button click");

        foreach (GameObject obj in objectsToEnableOnHomeButton)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
                Debug.Log($"Enabled object on Home button: {obj.name}");
            }
        }
    }

    private IEnumerator GameEndSequence()
    {
        // Wait a moment before starting
        yield return new WaitForSeconds(0.5f);

        // Animate stars (single animator version)
        yield return StartCoroutine(AnimateStars());

        // Animate counting numbers
        yield return StartCoroutine(AnimateCountingNumbers());

        // Show buttons (except next button which is disabled)
        if (buttonContainer != null)
            buttonContainer.SetActive(true);
    }

    private IEnumerator AnimateStars()
    {
        if (starsContainer == null || starsAnimator == null)
            yield break;

        // Activate stars container
        starsContainer.SetActive(true);

        // Wait a moment
        yield return new WaitForSeconds(0.3f);

        // Reset to default state first
        starsAnimator.SetInteger(starParameter, 0);
        starsAnimator.Play("Default", -1, 0f);

        // Wait one frame
        yield return null;

        // Set the star parameter (0-4 where 0=0 stars, 1=1 star, 2=2 stars, 3=3 stars)
        starsAnimator.SetInteger(starParameter, starsEarned);

        // Force animation update
        starsAnimator.Update(0f);

        // If you have specific animation clips for each star count, trigger them
        // For example, if you have triggers named "Show1Star", "Show2Stars", etc.
        if (starsEarned > 0)
        {
            string triggerName = $"Show{starsEarned}Star" + (starsEarned > 1 ? "s" : "");
            starsAnimator.SetTrigger(triggerName);
        }

        // Wait for animation to play
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator AnimateCountingNumbers()
    {
        if (pointsText == null || timeText == null || coinsText == null || expText == null)
            yield break;

        // Reset all values to 0
        pointsText.text = "0";
        timeText.text = "00:00";
        coinsText.text = "0";
        expText.text = "0";

        // Wait a moment
        yield return new WaitForSeconds(0.3f);

        float elapsedTime = 0f;

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / countAnimationDuration;

            // Smooth progress curve
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Update values with easing
            float currentPoints = Mathf.Lerp(0, playerPoints, smoothProgress);
            float currentTime = Mathf.Lerp(0, completionTime, smoothProgress);
            float currentCoins = Mathf.Lerp(0, totalCoins, smoothProgress);
            float currentExp = Mathf.Lerp(0, totalExp, smoothProgress);

            // Update UI
            pointsText.text = Mathf.FloorToInt(currentPoints).ToString("N0");
            timeText.text = FormatTime(currentTime);
            coinsText.text = Mathf.FloorToInt(currentCoins).ToString("N0");
            expText.text = Mathf.FloorToInt(currentExp).ToString("N0");

            yield return null;
        }

        // Set final values
        pointsText.text = playerPoints.ToString("N0");
        timeText.text = FormatTime(completionTime);
        coinsText.text = totalCoins.ToString("N0");
        expText.text = totalExp.ToString("N0");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private int CalculateStarRating(int hearts, float time)
    {
        // Debug log for understanding calculations
        Debug.Log($"Calculating stars: Hearts={hearts}, Time={time}s ({FormatTime(time)})");

        // Apply priority order (time in seconds)
        if (hearts == 0)
        {
            Debug.Log("0 stars: Hearts == 0");
            return 0; // 0 stars for failure
        }

        if (hearts >= 3 && time <= 15 * 60) // 15 minutes in seconds = 900
        {
            Debug.Log("3 stars: Hearts >= 3 AND Time <= 15 min (900s)");
            return 3;
        }

        if (hearts == 2 && time <= 17 * 60) // 17 minutes in seconds = 1020
        {
            Debug.Log("2 stars: Hearts == 2 AND Time <= 17 min (1020s)");
            return 2;
        }

        if (hearts == 1 && time >= 20 * 60) // 20 minutes in seconds = 1200
        {
            Debug.Log("1 star: Hearts == 1 AND Time >= 20 min (1200s)");
            return 1;
        }

        // Default: 1 star if conditions not met but player completed with at least 1 heart
        Debug.Log("1 star: Default (hearts > 0 but other conditions not met)");
        return 1;
    }

    private void CalculateRewards()
    {
        // Base rewards based on stars
        switch (starsEarned)
        {
            case 3:
                baseCoins = 1000;
                baseExp = 1000;
                Debug.Log("Base: 1000 coins, 1000 EXP");
                break;
            case 2:
                baseCoins = 500;
                baseExp = 500;
                Debug.Log("Base: 500 coins, 500 EXP");
                break;
            case 1:
                baseCoins = 100;
                baseExp = 100;
                Debug.Log("Base: 100 coins, 100 EXP");
                break;
            default:
                baseCoins = 0;
                baseExp = 0;
                Debug.Log("Base: 0 coins, 0 EXP");
                break;
        }

        // Bonus from points
        int bonusExpFromPoints = Mathf.FloorToInt(playerPoints / 7f);
        int bonusCoinsFromPoints = Mathf.FloorToInt(playerPoints / 10f);
        Debug.Log($"Points Bonus: {bonusCoinsFromPoints} coins, {bonusExpFromPoints} EXP (from {playerPoints} points)");

        // Life bonus (coins only)
        int lifeBonusCoins = 0;
        if (remainingHearts >= 5)
            lifeBonusCoins = 300;
        else if (remainingHearts == 4)
            lifeBonusCoins = 200;
        else if (remainingHearts == 3)
            lifeBonusCoins = 100;
        else if (remainingHearts == 2)
            lifeBonusCoins = 50;
        else if (remainingHearts == 1)
            lifeBonusCoins = 0;

        Debug.Log($"Life Bonus: {lifeBonusCoins} coins for {remainingHearts} hearts");

        // Total rewards
        totalExp = baseExp + bonusExpFromPoints;
        totalCoins = baseCoins + bonusCoinsFromPoints + lifeBonusCoins;

        Debug.Log($"Total: {totalCoins} coins, {totalExp} EXP");
    }

    // NEW: Common button click handler to disable win/lose objects and enable player control
    private void OnButtonClicked()
    {
        Debug.Log("Button clicked - disabling win/lose objects and enabling player control");

        // Disable win/lose objects
        DisableWinLoseObjects();

        // Enable ThirdPersonController for player movement
        EnablePlayerControl();
    }

    private void ResetGameEndState()
    {
        Debug.Log("Resetting game end state...");

        // Reset character animation first
        ResetCharacterAnimation();

        // Switch back to player camera INSTANTLY (no transition)
        SwitchToPlayerCameraInstantly();

        // NOTE: We DON'T re-enable objects that were disabled on game end
        // objectsToDisableOnGameEnd remain disabled permanently

        // Disable win/lose specific objects
        DisableWinLoseObjects();

        // Reset star animation
        if (starsAnimator != null)
        {
            starsAnimator.SetInteger(starParameter, 0);
        }

        // Hide stars container
        if (starsContainer != null)
            starsContainer.SetActive(false);

        // Reset key unlocked object
        if (keyUnlockedObject != null && keyUnlockedObject.activeSelf)
        {
            keyUnlockedObject.SetActive(false);
            Debug.Log("KeyUnlocked object deactivated");
        }

        // Reset text values
        if (pointsText != null) pointsText.text = "0";
        if (timeText != null) timeText.text = "00:00";
        if (coinsText != null) coinsText.text = "0";
        if (expText != null) expText.text = "0";

        // Hide buttons
        if (buttonContainer != null)
            buttonContainer.SetActive(false);

        // Hide game summary
        if (gameSummaryParent != null)
            gameSummaryParent.SetActive(false);
    }

    private void DisableWinLoseObjects()
    {
        // Disable win objects
        foreach (GameObject obj in objectsToEnableOnWin)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled win object: {obj.name}");
            }
        }

        // Disable lose objects
        foreach (GameObject obj in objectsToEnableOnLose)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                Debug.Log($"Disabled lose object: {obj.name}");
            }
        }
    }

    // NEW: Method to enable player control
    private void EnablePlayerControl()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("ThirdPersonController enabled - Player can now move");
        }
        else
        {
            Debug.LogWarning("PlayerController not found!");
        }
    }

    private void ResetMinigames()
    {
        Debug.Log("=== RESETTING ALL MINIGAMES ===");

        // Reset Grow Assessment
        if (growAssessmentManager != null)
        {
            growAssessmentManager.EndGrowAssessment();
            Debug.Log("Grow Assessment reset");
        }

        // Reset Glow Part and Towers
        ResetGlowTowers();

        // Reset Starting Sequence
        if (startingSequenceManager != null)
        {
            startingSequenceManager.EnableAllControlsAndUI();
            Debug.Log("Starting Sequence reset");
        }

        // Reset Torch Minigame
        ResetTorchMinigame();

        // Reset objects to initial positions
        ResetObjectsToInitialState();

        // Reset animators with one-time animations
        ResetOneTimeAnimations();

        Debug.Log("=== ALL MINIGAMES RESET ===");
    }

    // NEW: Method to reset Glow Towers with proper reset logic
    private void ResetGlowTowers()
    {
        Debug.Log("=== RESETTING GLOW TOWERS ===");

        // First, try to reset through the GlowPartManager
        if (glowPartManager != null)
        {
            try
            {
                // Method 1: Try CompleteReset first
                glowPartManager.CompleteReset();
                Debug.Log("Glow Part Manager reset using CompleteReset method");
            }
            catch (System.Exception e1)
            {
                Debug.LogWarning($"CompleteReset failed: {e1.Message}. Trying ResetAllTowers...");

                try
                {
                    // Method 2: Try ResetAllTowers
                    glowPartManager.ResetAllTowers();
                    Debug.Log("Glow Part Manager reset using ResetAllTowers method");
                }
                catch (System.Exception e2)
                {
                    Debug.LogWarning($"ResetAllTowers failed: {e2.Message}. Trying EndGlowPart...");

                    try
                    {
                        // Method 3: Fallback to EndGlowPart
                        glowPartManager.EndGlowPart();
                        Debug.Log("Glow Part Manager reset using EndGlowPart");
                    }
                    catch (System.Exception e3)
                    {
                        Debug.LogError($"All reset methods failed: {e3.Message}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("GlowPartManager not found in scene! Will reset towers individually.");
        }

        // Reset individual towers as backup
        GlowTower[] allTowers = FindObjectsOfType<GlowTower>();
        Debug.Log($"Found {allTowers.Length} glow towers to reset");

        foreach (GlowTower tower in allTowers)
        {
            if (tower != null)
            {
                try
                {
                    // Try ForceReset first
                    tower.ForceReset();
                    Debug.Log($"Tower {tower.gameObject.name} reset using ForceReset");
                }
                catch (System.Exception e1)
                {
                    Debug.LogWarning($"ForceReset failed for {tower.gameObject.name}: {e1.Message}. Trying ResetTower...");

                    try
                    {
                        // Fallback to ResetTower
                        tower.ResetTower();
                        Debug.Log($"Tower {tower.gameObject.name} reset using ResetTower");
                    }
                    catch (System.Exception e2)
                    {
                        Debug.LogWarning($"ResetTower failed for {tower.gameObject.name}: {e2.Message}. Basic reset...");

                        // Ultimate fallback: basic reset
                        tower.SetEnergy(0f);
                        tower.DeactivateTower();
                        Debug.Log($"Tower {tower.gameObject.name} basic reset complete");
                    }
                }
            }
        }

        Debug.Log($"Reset {allTowers.Length} glow towers");
    }

    // NEW: Method to reset animators with one-time animations
    private void ResetOneTimeAnimations()
    {
        foreach (Animator animator in animatorsToReset)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                // Reset to default state
                animator.Play(defaultStateName, -1, 0f);
                animator.Update(0f);

                // Reset all parameters
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

                Debug.Log($"Reset animator: {animator.gameObject.name}");
            }
        }
    }

    // NEW: Simplified method to reset Torch Minigame
    private void ResetTorchMinigame()
    {
        TorchMinigameManager torchManager = FindObjectOfType<TorchMinigameManager>();
        if (torchManager != null)
        {
            // Check if TorchMinigameManager has CompleteMinigameReset method
            System.Type torchManagerType = typeof(TorchMinigameManager);
            var resetMethod = torchManagerType.GetMethod("CompleteMinigameReset");

            if (resetMethod != null)
            {
                resetMethod.Invoke(torchManager, null);
                Debug.Log("Torch Minigame reset using CompleteMinigameReset method");
            }
            else
            {
                // Fallback: use ResetMinigame method
                var fallbackMethod = torchManagerType.GetMethod("ResetMinigame");
                if (fallbackMethod != null)
                {
                    fallbackMethod.Invoke(torchManager, null);
                    Debug.Log("Torch Minigame reset using ResetMinigame method");
                }
                else
                {
                    Debug.LogWarning("TorchMinigameManager doesn't have a reset method!");
                }
            }
        }
        else
        {
            Debug.Log("No TorchMinigameManager found in scene.");
        }
    }

    private void OnHomeClicked()
    {
        Debug.Log("=== HOME BUTTON CLICKED ===");

        // Call common button handler first to disable win/lose objects and enable player control
        OnButtonClicked();

        // Play lobby music
        PlayLobbyMusic();

        // Reset game end state (this includes switching camera instantly)
        ResetGameEndState();

        // Reset minigames
        ResetMinigames();

        // Teleport to lobby
        if (playerController != null && lobbyPoint != null)
        {
            playerController.transform.position = lobbyPoint.position;
            playerController.transform.rotation = lobbyPoint.rotation;
            Debug.Log("Player teleported to lobby");
        }

        // Enable UI Controls Canvas
        if (uiControlsCanvas != null && !uiControlsCanvas.activeSelf)
        {
            uiControlsCanvas.SetActive(true);
            Debug.Log("Enabled UI Controls Canvas");
        }

        // Enable specific objects on Home button click
        EnableObjectsOnHomeButton();

        // Update quest status if first time completion
        if (isFirstTimeCompletion && questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                Debug.Log($"Completing quest: {questID}");
                // Mark quest as completed
                questManager.CompleteTask(questID, $"{questID}_task_1");
                questManager.ClaimQuest(questID); // Also claim the quest rewards
            }
        }

        Debug.Log("=== HOME BUTTON PROCESS COMPLETE ===");
    }

    private void OnRestartClicked()
    {
        Debug.Log("=== RESTART BUTTON CLICKED ===");

        // Call common button handler first to disable win/lose objects and enable player control
        OnButtonClicked();

        // Play restart music
        PlayRestartMusic();

        // Reset game end state (this includes switching camera instantly)
        ResetGameEndState();

        // Reset minigames
        ResetMinigames();

        // TELEPORT PLAYER TO STARTING POINT
        TeleportPlayerToStartingPoint();

        // Show Game Mechanics Board using the MechanicsBoardManager
        if (mechanicsBoardManager != null)
        {
            mechanicsBoardManager.OpenMechanicsBoard();
            Debug.Log("Opened Game Mechanics Board via MechanicsBoardManager");
        }
        else
        {
            Debug.LogWarning("MechanicsBoardManager not found!");
        }

        // Reset game manager (but don't start game yet - wait for Start button)
        if (gameManager != null)
        {
            gameManager.EndGame(); // Clean up current game
            Debug.Log("Game cleaned up, ready for restart via Start button");
        }

        Debug.Log("=== RESTART BUTTON PROCESS COMPLETE ===");
    }

    private void OnNextClicked()
    {
        Debug.Log("=== NEXT BUTTON CLICKED ===");

        // Call common button handler first to disable win/lose objects and enable player control
        OnButtonClicked();

        // Play lobby music
        PlayLobbyMusic();

        // Reset game end state
        ResetGameEndState();

        // Reset minigames
        ResetMinigames();

        // Update quest status if first time completion
        if (isFirstTimeCompletion && questManager != null)
        {
            Quest quest = questManager.GetQuest(questID);
            if (quest != null)
            {
                Debug.Log($"Completing quest: {questID}");
                // Mark quest as completed
                questManager.CompleteTask(questID, $"{questID}_task_1");
                questManager.ClaimQuest(questID); // Also claim the quest rewards
            }
        }

        // For now, just go back to lobby (you can change this to load next level)
        if (playerController != null && lobbyPoint != null)
        {
            playerController.transform.position = lobbyPoint.position;
            playerController.transform.rotation = lobbyPoint.rotation;
            Debug.Log("Player teleported to lobby");
        }

        Debug.Log("=== NEXT BUTTON PROCESS COMPLETE ===");
    }

    // Call this when player loses all hearts during gameplay
    public void HandleGameOver()
    {
        Debug.Log("=== HANDLING GAME OVER ===");

        // Stop the game first
        if (gameManager != null && gameManager.IsGameActive())
        {
            gameManager.EndGame();
        }

        // Get current game state
        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = 0; // Player lost all hearts

        Debug.Log($"Game Over - Time: {completionTime}, Points: {playerPoints}, Hearts: {remainingHearts}");

        // Always 0 stars for game over
        starsEarned = 0;

        // Calculate rewards (0 stars = 0 rewards)
        CalculateRewards();

        // Show game end screen with losing background
        ShowGameEndScreen(false);
    }

    // Call this when player completes the level
    public void HandleLevelComplete()
    {
        Debug.Log("=== HANDLING LEVEL COMPLETE ===");

        // Stop the game first
        if (gameManager != null && gameManager.IsGameActive())
        {
            gameManager.EndGame();
        }

        // Get current game state
        completionTime = gameManager.GetGameTimer();
        playerPoints = gameManager.GetCurrentScore();
        remainingHearts = Mathf.CeilToInt(gameManager.GetCurrentLifeAmount());

        Debug.Log($"Level Complete - Time: {completionTime}, Points: {playerPoints}, Hearts: {remainingHearts}");

        // Calculate star rating
        starsEarned = CalculateStarRating(remainingHearts, completionTime);

        // Show game end screen with winning background
        ShowGameEndScreen(true);
    }

    // Helper methods to call from GoGrowGlowGameManager
    public void TriggerLevelComplete()
    {
        HandleLevelComplete();
    }

    public void TriggerGameOver()
    {
        HandleGameOver();
    }

    private void HandleBackgroundMusic(bool isWin)
    {
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("Background Music Source not assigned!");
            return;
        }

        AudioClip musicToPlay = isWin ? winMusicClip : loseMusicClip;

        if (musicToPlay != null)
        {
            backgroundMusicSource.clip = musicToPlay;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
            Debug.Log($"Playing {(isWin ? "win" : "lose")} music: {musicToPlay.name}");
        }
        else
        {
            Debug.LogWarning($"{(isWin ? "Win" : "Lose")} music clip not assigned!");
        }
    }

    private void PlayRestartMusic()
    {
        if (backgroundMusicSource != null && restartMusicClip != null)
        {
            backgroundMusicSource.clip = restartMusicClip;
            backgroundMusicSource.loop = false; // Usually restart music doesn't loop
            backgroundMusicSource.Play();
            Debug.Log("Playing restart music");
        }
    }

    private void PlayLobbyMusic()
    {
        if (backgroundMusicSource != null && lobbyMusicClip != null)
        {
            backgroundMusicSource.clip = lobbyMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
            Debug.Log("Playing lobby music");
        }
    }

    // Public getters for external access
    public int GetStarsEarned() => starsEarned;
    public int GetTotalCoins() => totalCoins;
    public int GetTotalExp() => totalExp;
    public float GetCompletionTime() => completionTime;
    public int GetPlayerPoints() => playerPoints;
    public bool IsFirstTimeCompletion() => isFirstTimeCompletion;

    // Helper method to test from inspector
    [ContextMenu("Test Win")]
    public void TestWin()
    {
        // Simulate win conditions
        if (gameManager != null)
        {
            // Set test values
            gameManager.AddPoints(1000);
            gameManager.SetEnergy(100);

            // Show win screen
            HandleLevelComplete();
        }
    }

    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        // Simulate lose conditions
        if (gameManager != null)
        {
            // Set test values
            gameManager.AddPoints(500);
            gameManager.SetEnergy(0);

            // Show lose screen
            HandleGameOver();
        }
    }
}