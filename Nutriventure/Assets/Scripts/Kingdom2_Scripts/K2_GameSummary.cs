using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using Cinemachine;

public class K2_GameSummary : MonoBehaviour
{
    [Header("Game Summary Panel")]
    public GameObject gameSummaryPanel;
    public CanvasGroup panelCanvasGroup;

    [Header("Panel Sprites - Win/Lose")]
    public Image panelBackgroundImage;
    public Sprite winPanelSprite;
    public Sprite losePanelSprite;

    [Header("Summary Text Fields")]
    public TextMeshProUGUI timePlayedText;
    public TextMeshProUGUI gameScoreText;
    public TextMeshProUGUI coinsEarnedText;
    public TextMeshProUGUI keyStatusText;
    public TextMeshProUGUI starsEarnedText;

    [Header("Star Animations")]
    public Animator starAnimator;
    public string starParameterName = "star";
    private int currentStars = 0;

    [Header("Key Display")]
    public GameObject keyImageObject;

    [Header("Buttons")]
    public Button confirmButton;

    [Header("Panel Animation")]
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 0.5f;

    [Header("Audio Settings")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public float soundVolume = 0.7f;
    public AudioSource backgroundMusicSource;
    public float backgroundMusicVolumeDuringSummary = 0.2f;
    private float originalBackgroundMusicVolume = 1.0f;

    [Header("Key Status Colors")]
    public Color unlockedColor = Color.green;
    public Color lockedColor = Color.red;

    [Header("Coin Reward Settings")]
    public int coinsPerStar = 10;
    public int baseCoinsPerScore = 1;
    public float loseMultiplier = 0.5f;
    public float winMultiplier = 1.0f;

    [Header("Spawn Settings")]
    public Transform playerSpawnPoint;
    public ProductSpawner productSpawner;

    [Header("Camera References")]
    public CinemachineVirtualCamera summaryVirtualCamera;
    public CinemachineVirtualCamera playerFollowCamera;
    private CinemachineBrain cinemachineBrain;

    [Header("Character Animation")]
    public CharacterVisualSwapper characterVisualSwapper;
    public string lookAroundParameter = "LookAround";

    [Header("UI References")]
    public GameObject joystickCanvas; // Assign UI_Canvas_StarterAssetsInputs_Joysticks here
    public GameObject qa1Panel;
    public GameObject qa2Panel;

    [Header("QA2 Completion Settings")]
    public bool showSummaryOnQA2Completion = true;
    [Range(1, 5)] public int requiredQA2CorrectAnswers = 5;

    // Star animation states
    private string[] starStateNames = new string[] { "Empty", "Star1", "Star2", "Star3" };

    // Private references
    private SugariaPlayerStat playerHealth;
    private GameplayProgression gameplayProgression;
    private ProductInformationManager productManager;
    private SugariaScoringSystem scoringSystem;
    private MainMenu_Manager mainMenuManager;
    private GameObject playerObject;
    private CollectProducts collectProductsScript;
    private K2_QA2system qa2System;
    private K2_QA1system qa1System;
    private Animator playerAnimator;
    private AudioSource audioSource;

    // Game state
    private bool isGameOver = false;
    private bool isVictory = false;
    private bool waitingForLastQA2Panel = false;
    private bool isSummaryActive = false;
    private float originalTimeScale;
    private int calculatedCoinsEarned = 0;
    private bool coinsAddedToDatabase = false;
    private int healthBeforeDeath = 0;
    private bool isProcessingConfirm = false;
    private bool summaryLocked = false;

    void Awake()
    {
        // Singleton pattern
        var existingInstances = FindObjectsOfType<K2_GameSummary>();
        if (existingInstances.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        FindAllReferences();
        InitializeComponents();
    }

    void Update()
    {
        CheckGameConditions();
    }

    #region Initialization

    private void FindAllReferences()
    {
        playerHealth = FindObjectOfType<SugariaPlayerStat>();
        gameplayProgression = FindObjectOfType<GameplayProgression>();
        productManager = FindObjectOfType<ProductInformationManager>();
        scoringSystem = FindObjectOfType<SugariaScoringSystem>();
        mainMenuManager = FindObjectOfType<MainMenu_Manager>();
        playerObject = GameObject.FindGameObjectWithTag("Player");
        collectProductsScript = FindObjectOfType<CollectProducts>();
        qa2System = FindObjectOfType<K2_QA2system>();
        qa1System = FindObjectOfType<K2_QA1system>();

        if (characterVisualSwapper == null)
            characterVisualSwapper = FindObjectOfType<CharacterVisualSwapper>();

        if (playerAnimator == null && playerObject != null)
            playerAnimator = playerObject.GetComponentInChildren<Animator>();

        if (backgroundMusicSource == null)
            backgroundMusicSource = FindBackgroundMusicSource();

        if (audioSource == null)
            CreateAudioSource();

        if (playerObject == null)
            playerObject = GameObject.Find("PlayerArmature");

        // IMPORTANT: Try multiple ways to find ProductSpawner
        if (productSpawner == null)
        {
            productSpawner = FindObjectOfType<ProductSpawner>();
            if (productSpawner != null)
            {
                Debug.Log($"Found ProductSpawner: {productSpawner.gameObject.name}");
            }
            else
            {
                Debug.LogWarning("ProductSpawner not found! Products may not respawn.");
            }
        }

        if (cinemachineBrain == null)
            cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();
    }

    private AudioSource FindBackgroundMusicSource()
    {
        AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
        if (audioHandler != null)
            return audioHandler.GetComponent<AudioSource>();

        GameObject bgMusicObj = GameObject.FindGameObjectWithTag("BackgroundMusic");
        return bgMusicObj?.GetComponent<AudioSource>();
    }

    private void CreateAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void InitializeComponents()
    {
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);

        ResetStarAnimator();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        if (backgroundMusicSource != null)
            originalBackgroundMusicVolume = backgroundMusicSource.volume;

        Debug.Log($"GameSummary initialized - QA2 Completion Summary: {showSummaryOnQA2Completion}");
    }

    #endregion

    #region Game Condition Checks

    private void CheckGameConditions()
    {
        if (summaryLocked) return;
        // Check for lose condition (health reaches 0)
        if (!isGameOver && !isSummaryActive && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
        }

        // Check for win condition (QA2 completed)
        if (showSummaryOnQA2Completion && !isGameOver && !isSummaryActive && !waitingForLastQA2Panel && qa2System != null && IsQA2Completed())
        {
            isVictory = true;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    private bool IsQA2Completed()
    {
        if (qa2System == null) return false;

        int correctlyAnswered = qa2System.GetCorrectlyAnsweredCount();

        if (qa2System.IsPanelActive())
        {
            waitingForLastQA2Panel = true;
            StartCoroutine(WaitForLastQA2PanelToClose());
            return false;
        }

        return correctlyAnswered >= requiredQA2CorrectAnswers;
    }

    private IEnumerator WaitForLastQA2PanelToClose()
    {
        while (qa2System != null && qa2System.IsPanelActive())
            yield return null;

        waitingForLastQA2Panel = false;

        if (qa2System != null && !isGameOver && !isSummaryActive && showSummaryOnQA2Completion)
        {
            int correctlyAnswered = qa2System.GetCorrectlyAnsweredCount();
            if (correctlyAnswered >= requiredQA2CorrectAnswers)
            {
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
        }
    }

    #endregion

    #region Summary Panel

    private IEnumerator ShowSummaryPanel()
    {
        if (isGameOver || isSummaryActive)
        {
            Debug.LogWarning("Summary panel already shown!");
            yield break;
        }

        isGameOver = true;
        isSummaryActive = true;
        summaryLocked = true;

        Debug.Log($"Starting ShowSummaryPanel() - Victory: {isVictory}");

        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        PrepareGameForSummary();
        yield return null;

        yield return TriggerLookAroundAnimationDuringPause();
        PlayResultSound();

        CalculateCoinReward();
        UpdateSummaryData();

        ShowPanelWithAnimation();

        Debug.Log($"Game {(isVictory ? "won" : "lost")} - Summary panel shown");
        
        // Wait a moment then play star animation
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
    }

    private void PrepareGameForSummary()
    {
        DisableCinemachineBlending();
        MovePlayerToSpawnPoint();
        DisablePlayerInput();
        CloseAllQAPanels();
        LowerBackgroundMusicVolume();
        SwitchToSummaryCameraImmediate();
        HideJoystickCanvas();
    }

    private void ShowPanelWithAnimation()
    {
        if (gameSummaryPanel == null)
        {
            Debug.LogError("Game Summary Panel is not assigned!");
            return;
        }

        gameSummaryPanel.SetActive(true);
        UpdatePanelSprite();

        if (panelCanvasGroup != null)
            StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
    }

    private IEnumerator FadePanel(float startAlpha, float endAlpha, float duration)
    {
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

    private void UpdatePanelSprite()
    {
        if (panelBackgroundImage == null) return;

        if (isVictory && winPanelSprite != null)
            panelBackgroundImage.sprite = winPanelSprite;
        else if (!isVictory && losePanelSprite != null)
            panelBackgroundImage.sprite = losePanelSprite;
        else
            Debug.LogWarning("Missing panel sprite assignment!");
    }

    #endregion

    #region UI Management

    private void HideJoystickCanvas()
    {
        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(false);
            Debug.Log("Joystick canvas hidden");
        }
    }

    private void ShowJoystickCanvas()
    {
        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(true);
            Debug.Log("Joystick canvas shown");
        }
    }

    private void CloseAllQAPanels()
    {
        if (qa1Panel != null && qa1Panel.activeInHierarchy)
        {
            qa1Panel.SetActive(false);
            InvokeMethodIfExists(qa1System, "ClosePanel");
        }

        if (qa2Panel != null && qa2Panel.activeInHierarchy)
        {
            qa2Panel.SetActive(false);
            InvokeMethodIfExists(qa2System, "OnCloseButtonClicked");
        }

        // Close any other interfering UI
        CloseInterferingUI();
    }

    private void CloseInterferingUI()
    {
        GameObject[] allCanvases = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allCanvases)
        {
            if (obj.activeInHierarchy && obj != gameSummaryPanel &&
                (obj.name.Contains("Assessment") || obj.name.Contains("QA") ||
                 obj.name.Contains("Nutrition") || obj.name.Contains("Menu")))
            {
                obj.SetActive(false);
            }
        }
    }

    // Method to invoke a method by name on an object
    private void InvokeMethodIfExists(object target, string methodName)
    {
        if (target == null) return;

        var method = target.GetType().GetMethod(methodName);
        if (method != null)
            method.Invoke(target, null);
    }

    #endregion

    #region Player & Camera Control

    private void MovePlayerToSpawnPoint()
    {
        if (playerObject == null) return;

        if (playerSpawnPoint != null)
        {
            playerObject.transform.position = playerSpawnPoint.position;
            playerObject.transform.rotation = playerSpawnPoint.rotation;
        }

        CharacterController charController = playerObject.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
            charController.enabled = true;
        }
    }

    private void DisablePlayerInput()
    {
        ToggleComponent<InputManager>("DisablePlayerInput", false);
        ToggleComponent<ThirdPersonController>(enabled: false);
        ToggleComponent<StarterAssetsInputs>(enabled: false);
        HideJoystickCanvas();
        Debug.Log("Player input disabled");
    }

    private void EnablePlayerInput()
    {
        ToggleComponent<InputManager>("EnablePlayerInput", true);
        ToggleComponent<ThirdPersonController>(enabled: true);
        ToggleComponent<StarterAssetsInputs>(enabled: true);
        ShowJoystickCanvas();
        Debug.Log("Player input enabled");
    }

    private void ToggleComponent<T>(string methodName = null, bool enabled = false) where T : MonoBehaviour
    {
        T component = FindObjectOfType<T>();
        if (component != null)
        {
            if (!string.IsNullOrEmpty(methodName))
            {
                var method = component.GetType().GetMethod(methodName);
                if (method != null)
                    method.Invoke(component, null);
            }
            else
            {
                component.enabled = enabled;
            }
        }
    }

    #endregion

    #region Camera Control

    private void DisableCinemachineBlending()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
            cinemachineBrain.m_DefaultBlend.m_Time = 0f;
        }
    }

    private void EnableCinemachineBlending()
    {
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_DefaultBlend.m_Time = 0.5f;
        }
    }

    private void SwitchToSummaryCameraImmediate()
    {
        if (summaryVirtualCamera != null)
        {
            summaryVirtualCamera.Priority = 100;
            if (playerFollowCamera != null)
                playerFollowCamera.Priority = 0;

            if (cinemachineBrain != null)
                cinemachineBrain.ManualUpdate();
        }
    }

    private void SwitchToPlayerCameraWithBlend()
    {
        EnableCinemachineBlending();

        if (playerFollowCamera != null)
            playerFollowCamera.Priority = 100;

        if (summaryVirtualCamera != null)
            summaryVirtualCamera.Priority = 0;

        if (cinemachineBrain != null)
            cinemachineBrain.ManualUpdate();
    }

    #endregion

    #region Animation

    private IEnumerator TriggerLookAroundAnimationDuringPause()
    {
        if (playerAnimator != null)
        {
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

            if (!string.IsNullOrEmpty(lookAroundParameter))
                playerAnimator.SetBool(lookAroundParameter, true);

            if (characterVisualSwapper != null)
                characterVisualSwapper.TriggerLookAroundAnimation();

            playerAnimator.Update(0f);
        }

        yield return new WaitForSecondsRealtime(0.1f);
    }

    private void StopLookAroundAnimationDuringPause()
    {
        if (playerAnimator != null)
        {
            if (!string.IsNullOrEmpty(lookAroundParameter))
                playerAnimator.SetBool(lookAroundParameter, false);

            playerAnimator.Update(0f);
            playerAnimator.updateMode = AnimatorUpdateMode.Normal;
        }

        if (characterVisualSwapper != null)
            characterVisualSwapper.StopLookAroundAnimation();
    }

    #endregion

    #region Audio

    private void LowerBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
            backgroundMusicSource.volume = backgroundMusicVolumeDuringSummary;
    }

    private void RestoreBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = originalBackgroundMusicVolume;
    }

    private void PlayResultSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = isVictory ? winSound : loseSound;
        if (clipToPlay != null)
            audioSource.PlayOneShot(clipToPlay, soundVolume);
    }

    #endregion

    #region Summary Data & Calculations

    private void UpdateSummaryData()
    {
        currentStars = CalculateStars();
        UpdateKeyStatus(currentStars);
        UpdateTimePlayed();
        UpdateScore();
        UpdateCoinsEarned();
        UpdateStarsEarnedText();
        
        Debug.Log($"=== UPDATE SUMMARY DATA ===");
        Debug.Log($"Current stars calculated: {currentStars}");
        Debug.Log($"Stars earned text will show: {currentStars}/3");
    }

    private void UpdateStarsEarnedText()
    {
        if (starsEarnedText != null)
        {
            starsEarnedText.text = $"{currentStars}/3";
            Debug.Log($"Stars earned text updated: {currentStars}/3");
        }
    }

    private void UpdateTimePlayed()
    {
        if (timePlayedText != null && gameplayProgression != null)
        {
            float timePlayed = gameplayProgression.GetCurrentTime();
            int minutes = Mathf.FloorToInt(timePlayed / 60f);
            int seconds = Mathf.FloorToInt(timePlayed % 60f);
            timePlayedText.text = $"{minutes:00}:{seconds:00}";
        }
        else if (timePlayedText != null)
        {
            timePlayedText.text = "Time: --:--";
        }
    }

    private void UpdateScore()
    {
        if (gameScoreText != null && scoringSystem != null)
        {
            int score = scoringSystem.GetCurrentScore();
            gameScoreText.text = $"{score}";
        }
        else if (gameScoreText != null)
        {
            gameScoreText.text = "Score: 0";
        }
    }

    private void UpdateCoinsEarned()
    {
        if (coinsEarnedText != null)
            coinsEarnedText.text = $"{calculatedCoinsEarned}";
    }

    private int CalculateStars()
    {
        int health = 0;
        
        if (isVictory)
        {
            // Check if we won via key collection
            K2_CollectKey collectKey = FindObjectOfType<K2_CollectKey>();
            if (collectKey != null && collectKey.HasTriggeredSummary())
            {
                // Use health at key collection if available
                health = collectKey.GetHealthAtKeyCollection();
                Debug.Log($"Using health at key collection: {health}");
            }
            else
            {
                // Use current health for QA2 completion wins
                health = playerHealth?.currentHealth ?? 0;
                Debug.Log($"Using current health for QA2 win: {health}");
            }
        }
        else
        {
            // For lose condition
            health = Mathf.Max(0, healthBeforeDeath);
            Debug.Log($"Using health before death for lose: {health}");
        }
        
        int stars = 0;
        
        if (health >= 5) stars = 3;
        else if (health >= 3) stars = 2;
        else if (health >= 1) stars = 1;
        
        Debug.Log($"=== CALCULATE STARS ===");
        Debug.Log($"Health: {health}");
        Debug.Log($"Calculated stars: {stars}");
        Debug.Log($"Stars text will show: {stars}/3");
        
        return Mathf.Clamp(stars, 0, 3);
    }

    private void PlayStarAnimationDirect()
    {
        if (starAnimator != null)
        {
            Debug.Log($"=== PLAYING STAR ANIMATION DIRECT: {currentStars} stars ===");
            
            // Ensure GameObject is active
            if (!starAnimator.gameObject.activeSelf)
            {
                Debug.Log("Activating star animator GameObject");
                starAnimator.gameObject.SetActive(true);
            }
            
            // Ensure Animator is enabled
            if (!starAnimator.enabled)
            {
                Debug.Log("Enabling star animator component");
                starAnimator.enabled = true;
            }
            
            // Set to UnscaledTime since game is paused
            starAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            // FIRST: Reset to 0 to ensure clean transition
            starAnimator.SetInteger(starParameterName, 0);
            starAnimator.Update(0f);
            
            // Wait a tiny moment for the reset to take effect
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
        
        // Now set to the final value
        starAnimator.SetInteger(starParameterName, currentStars);
        starAnimator.Update(0f);
        
        // Double-check the value was set
        int currentValue = starAnimator.GetInteger(starParameterName);
        Debug.Log($"Star parameter set to: {currentValue} (requested: {currentStars})");
        
        // Get current state info
        AnimatorStateInfo stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current animation state: {stateInfo.fullPathHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Is in transition: {starAnimator.IsInTransition(0)}");
        
        // If still in default state, try to play the animation directly
        if (stateInfo.normalizedTime == 0 && currentStars > 0)
        {
            Debug.Log("Attempting to play animation directly...");
            ForcePlayStarAnimation(currentStars);
        }
        
        // Verify the animation is playing
        yield return new WaitForSecondsRealtime(0.1f);
        stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"After 0.1s - State normalized time: {stateInfo.normalizedTime}");
    }

    private void ForcePlayStarAnimation(int stars)
    {
        if (starAnimator != null && stars > 0 && stars <= 3)
        {
            string stateName = starStateNames[stars];
            Debug.Log($"Force playing animation state: {stateName}");
            
            // Play the state directly at the beginning
            starAnimator.Play(stateName, 0, 0f);
            
            // Force update
            starAnimator.Update(0f);
        }
    }

    private void ResetStarAnimator()
    {
        if (starAnimator != null)
        {
            Debug.Log("Resetting star animator...");
            
            // Reset to default state
            starAnimator.SetInteger(starParameterName, 0);
            
            // Set to normal update mode
            starAnimator.updateMode = AnimatorUpdateMode.Normal;
            
            // Force update
            starAnimator.Update(0f);
            
            Debug.Log("Star animator reset to default state");
        }
    }

    private void UpdateKeyStatus(int stars)
    {
        if (keyStatusText != null)
        {
            bool isUnlocked = (stars >= 2);
            keyStatusText.text = isUnlocked ? "KEY: UNLOCKED" : "KEY: LOCKED";
            keyStatusText.color = isUnlocked ? unlockedColor : lockedColor;
        }

        if (keyImageObject != null)
            keyImageObject.SetActive(stars >= 2);
    }

    private void CalculateCoinReward()
    {
        int stars = CalculateStars();
        int score = scoringSystem != null ? scoringSystem.GetCurrentScore() : 0;

        int starCoins = stars * coinsPerStar;
        int scoreCoins = Mathf.Max(0, (score / 300) * baseCoinsPerScore);
        int totalBaseCoins = starCoins + scoreCoins;

        float multiplier = isVictory ? winMultiplier : loseMultiplier;
        calculatedCoinsEarned = Mathf.Max(1, Mathf.RoundToInt(totalBaseCoins * multiplier));
        
        Debug.Log($"Coin calculation: Stars={stars}, Score={score}, StarCoins={starCoins}, ScoreCoins={scoreCoins}, Multiplier={multiplier}, Total={calculatedCoinsEarned}");
    }

    private void AddCoinsToDatabase()
    {
        if (coinsAddedToDatabase || GameDataManager.Instance == null) return;

        GameDataManager.Instance.CurrentGameData.nutriCoins += calculatedCoinsEarned;
        GameDataManager.Instance.SaveGameData();
        coinsAddedToDatabase = true;

        Debug.Log($"Added {calculatedCoinsEarned} coins to database");
    }

    #endregion

    #region Button Handlers

    public void OnConfirmButtonClicked()
    {
        if (!isSummaryActive || !isGameOver || isProcessingConfirm) return;
        
        isProcessingConfirm = true;

        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (confirmButton != null)
            confirmButton.interactable = false;

        StartCoroutine(HidePanelAndRestartGame());
    }

    private IEnumerator HidePanelAndRestartGame()
    {
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);

        StopLookAroundAnimationDuringPause();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        RestartGame();
        
        // Reset the processing flag
        isProcessingConfirm = false;

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void PlayButtonClickSound()
    {
        AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
        if (audioHandler != null)
            InvokeMethodIfExists(audioHandler, "PlayButtonClick");
    }

    #endregion

    #region Game Restart

    private void RestartGame()
    {
        Debug.Log("Restarting game...");

        SwitchToPlayerCameraWithBlend();
        ResetGameState();
        RespawnAllProducts();
        EnablePlayerInput();
        EnsureGameMode();
        ResetManager();

        summaryLocked = false;
        Debug.Log("Game restarted - Ready to play again!");
    }

    private void ResetGameState()
    {
        // Reset player systems
        if (playerHealth != null) playerHealth.ResetHealth();
        if (scoringSystem != null) scoringSystem.ResetSessionStats();
        if (productManager != null) productManager.ResetForNewSession();
        if (gameplayProgression != null) InvokeMethodIfExists(gameplayProgression, "ResetTimer");
        if (qa2System != null) InvokeMethodIfExists(qa2System, "ClearScannedProducts");

        // Reset monsters
        ResetAllMonsters();

        // Reset key system
        ResetKeySystem();

        // Reset collectibles
        if (collectProductsScript != null && collectProductsScript.HasCollectedDummyProduct())
            collectProductsScript.ResetDummyProductCollection();

        Debug.Log("Game state reset");
    }

    private void ResetAllMonsters()
    {
        MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
        foreach (MonsterObstacle monster in allMonsters)
        {
            if (monster != null)
            {
                monster.gameObject.SetActive(true);
                InvokeMethodIfExists(monster, "ResetMonster");
            }
        }
    }

    private void ResetKeySystem()
    {
        // Reset all key scripts
        K2_CollectKey[] allKeyScripts = FindObjectsOfType<K2_CollectKey>();
        foreach (K2_CollectKey keyScript in allKeyScripts)
        {
            if (keyScript != null)
            {
                InvokeMethodIfExists(keyScript, "ResetKey");
                InvokeMethodIfExists(keyScript, "ForceFullReset");
            }
        }

        // Destroy remaining key objects
        GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
        foreach (GameObject key in remainingKeys)
            Destroy(key);
    }

    private void RespawnAllProducts()
    {
        // Use the ProductSpawner script to respawn products
        if (productSpawner != null)
        {
            Debug.Log("Calling ProductSpawner to respawn products...");

            // Try to call RespawnProducts first
            System.Reflection.MethodInfo respawnMethod = productSpawner.GetType().GetMethod("RespawnProducts");
            if (respawnMethod != null)
            {
                respawnMethod.Invoke(productSpawner, null);
                Debug.Log("Called RespawnProducts() on ProductSpawner");
            }
            else
            {
                // Fall back to SpawnProducts
                System.Reflection.MethodInfo spawnMethod = productSpawner.GetType().GetMethod("SpawnProducts");
                if (spawnMethod != null)
                {
                    spawnMethod.Invoke(productSpawner, null);
                    Debug.Log("Called SpawnProducts() on ProductSpawner");
                }
                else
                {
                    // Direct call as last resort
                    productSpawner.SpawnProducts();
                    Debug.Log("Directly called SpawnProducts()");
                }
            }
        }
        else
        {
            Debug.LogWarning("ProductSpawner not assigned! Products will not respawn.");
        }
    }

    private void EnsureGameMode()
    {
        if (mainMenuManager == null) return;

        if (mainMenuManager.menuCanvas != null && mainMenuManager.menuCanvas.activeInHierarchy)
            mainMenuManager.menuCanvas.SetActive(false);

        ShowJoystickCanvas();
    }

    private void ResetManager()
    {
        isGameOver = false;
        isVictory = false;
        waitingForLastQA2Panel = false;
        isSummaryActive = false;
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        currentStars = 0;
        ResetStarAnimator();

        if (starsEarnedText != null)
            starsEarnedText.text = "0/3";

        Debug.Log("GameSummaryManager reset for new game");
    }

    #endregion

    #region Public Methods

    public void TriggerSummaryFromQA2()
    {
        if (!isGameOver && !isSummaryActive && showSummaryOnQA2Completion)
        {
            bool shouldTrigger = true;
            K2_CollectKey collectKey = FindObjectOfType<K2_CollectKey>();
            if (collectKey != null && collectKey.HasTriggeredSummary())
                shouldTrigger = false;

            if (shouldTrigger)
            {
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
        }
    }

    public bool IsQA2SummaryEnabled() => showSummaryOnQA2Completion;
    public void SetQA2SummaryEnabled(bool enabled) => showSummaryOnQA2Completion = enabled;
    public void SetRequiredQA2Answers(int requiredAnswers) => requiredQA2CorrectAnswers = Mathf.Clamp(requiredAnswers, 1, 5);

    #region Public Methods for External Triggers

    // Add this method to make it easier for key collection to trigger summary
    public void TriggerSummaryFromKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("TriggerSummaryFromKey called");
            isVictory = true;
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot trigger summary from key - already active or game over");
        }
    }

    // Method to show summary panel directly
    public IEnumerator ShowSummaryPanelDirectly(bool isWin)
    {
        if (isGameOver || isSummaryActive) 
        {
            Debug.LogWarning("Summary panel already active, cannot show directly");
            yield break;
        }
        
        summaryLocked = true;
        isGameOver = true;
        isSummaryActive = true;
        isVictory = isWin;
        
        Debug.Log($"Starting ShowSummaryPanelDirectly() - Victory: {isVictory}");
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        PrepareGameForSummary();
        yield return null;
        
        yield return TriggerLookAroundAnimationDuringPause();
        PlayResultSound();
        
        CalculateCoinReward();
        UpdateSummaryData();
        
        ShowPanelWithAnimation();
        
        Debug.Log($"Summary panel shown directly");
        
        // Wait a moment then play star animation
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
    }

    #endregion

    // Method to check if summary is already active
    public bool IsSummaryActive()
    {
        return isSummaryActive;
    }

    #endregion

    #region Debug & Testing

    [ContextMenu("Test Win")]
    public void TestWin()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            if (playerHealth != null) playerHealth.currentHealth = 6;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = false;
            healthBeforeDeath = 0;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    [ContextMenu("Test 3 Stars Direct")]
    public void Test3StarsDirect()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            if (playerHealth != null) playerHealth.currentHealth = 6;
            
            // Start the summary
            StartCoroutine(TestStarsDirectCoroutine());
        }
    }

    private IEnumerator TestStarsDirectCoroutine()
    {
        Debug.Log($"=== TESTING DIRECT STAR ANIMATION ===");
        
        isGameOver = true;
        isSummaryActive = true;
        summaryLocked = true;
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Calculate stars
        currentStars = CalculateStars();
        UpdateStarsEarnedText();
        
        Debug.Log($"Stars calculated: {currentStars}");
        
        // Play animation directly
        PlayStarAnimationDirect();
        
        yield return new WaitForSecondsRealtime(2f);
        
        Time.timeScale = originalTimeScale;
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
    }

    [ContextMenu("Test Star Animation")]
    public void TestStarAnimation()
    {
        if (starAnimator != null)
        {
            Debug.Log("Testing star animations...");
            
            // Make sure everything is set up
            if (!starAnimator.gameObject.activeSelf)
                starAnimator.gameObject.SetActive(true);
            
            if (!starAnimator.enabled)
                starAnimator.enabled = true;
            
            starAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            for (int i = 0; i <= 3; i++)
            {
                Debug.Log($"\n=== Testing star value: {i} ===");
                
                // Reset first
                starAnimator.SetInteger(starParameterName, 0);
                starAnimator.Update(0f);
                
                System.Threading.Thread.Sleep(100);
                
                // Set to value
                starAnimator.SetInteger(starParameterName, i);
                starAnimator.Update(0f);
                
                // Check current state
                AnimatorStateInfo stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"Current state: {stateInfo.fullPathHash}");
                Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
                Debug.Log($"Is in transition: {starAnimator.IsInTransition(0)}");
                
                // If not playing, try direct play
                if (stateInfo.normalizedTime == 0 && i > 0)
                {
                    Debug.Log("Animation not playing, trying direct play...");
                    ForcePlayStarAnimation(i);
                }
                
                // Wait to see animation
                System.Threading.Thread.Sleep(500);
            }
        }
        else
        {
            Debug.LogWarning("Star animator is null!");
        }
    }

    [ContextMenu("Debug Star Animator")]
    public void DebugStarAnimator()
    {
        if (starAnimator == null)
        {
            Debug.LogError("Star animator is null!");
            return;
        }
        
        Debug.Log("=== STAR ANIMATOR DEBUG ===");
        
        // Basic info
        Debug.Log($"GameObject: {starAnimator.gameObject.name}");
        Debug.Log($"GameObject active: {starAnimator.gameObject.activeSelf}");
        Debug.Log($"Animator enabled: {starAnimator.enabled}");
        Debug.Log($"Update mode: {starAnimator.updateMode}");
        
        // Controller info
        Debug.Log($"Controller: {starAnimator.runtimeAnimatorController?.name}");
        
        // Parameter info
        Debug.Log($"Current '{starParameterName}' value: {starAnimator.GetInteger(starParameterName)}");
        
        // List all parameters
        Debug.Log("All parameters:");
        foreach (var param in starAnimator.parameters)
        {
            string value = "";
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    value = starAnimator.GetFloat(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Int:
                    value = starAnimator.GetInteger(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Bool:
                    value = starAnimator.GetBool(param.name).ToString();
                    break;
                case AnimatorControllerParameterType.Trigger:
                    value = "Trigger";
                    break;
            }
            Debug.Log($"- {param.name} (Type: {param.type}, Value: {value})");
        }
        
        // Current state info
        AnimatorStateInfo stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current state hash: {stateInfo.fullPathHash}");
        Debug.Log($"State name hash: {stateInfo.shortNameHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Length: {stateInfo.length}");
        Debug.Log($"Is in transition: {starAnimator.IsInTransition(0)}");
        
        if (starAnimator.IsInTransition(0))
        {
            AnimatorTransitionInfo transInfo = starAnimator.GetAnimatorTransitionInfo(0);
            Debug.Log($"Transition duration: {transInfo.duration}");
            Debug.Log($"Transition normalized time: {transInfo.normalizedTime}");
        }
    }

    #endregion

    void OnDestroy()
    {
        if (isGameOver)
            Time.timeScale = originalTimeScale;

        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = originalBackgroundMusicVolume;

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
    }
}

// Add this extension class in the same file or a separate one
public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string paramName, AnimatorControllerParameterType paramType)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName && param.type == paramType)
                return true;
        }
        return false;
    }
}