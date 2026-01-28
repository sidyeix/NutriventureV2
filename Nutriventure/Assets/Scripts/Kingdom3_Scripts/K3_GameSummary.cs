using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using Cinemachine;
using UnityEngine.SceneManagement;

public class K3_GameSummary : MonoBehaviour
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

    [Header("Key Image Display")]
    public GameObject KeyImageunlocking;
    [Header("Fail Game Objects (Disabled on Lose)")]
    public GameObject failGameObject1;
    public GameObject failGameObject2;
    public GameObject failGameObject3;
    
    [Header("Buttons")]
    public Button confirmButton;
    public Button restartButton;

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

    [Header("Camera References")]
    public CinemachineVirtualCamera summaryVirtualCamera;
    public CinemachineVirtualCamera playerFollowCamera;
    private CinemachineBrain cinemachineBrain;

    [Header("Character Animation")]
    public CharacterVisualSwapper characterVisualSwapper;
    public string lookAroundParameter = "LookAround";

    [Header("UI References")]
    public GameObject joystickCanvas;

    [Header("Complete Restart Settings")]
    public bool completeRestartOnConfirm = true;
    public string sceneToReload = "";

    private string[] starStateNames = new string[] { "Empty", "Star1", "Star2", "Star3" };

    // K3 SPECIFIC REFERENCES - CHANGED FROM SugariaPlayerStat TO PreserviaPlayerStat
    private PreserviaPlayerStat playerHealth;
    private K3_GameplayProgression gameplayProgression;
    private PreserviaScoringSystem scoringSystem;
    private MainMenu_Manager mainMenuManager;
    private GameObject playerObject;
    private Animator playerAnimator;
    private AudioSource audioSource;
    private K3_CollectKey collectKeyScript;

    private bool isGameOver = false;
    private bool isVictory = false;
    private bool isSummaryActive = false;
    private float originalTimeScale;
    private int calculatedCoinsEarned = 0;
    private bool coinsAddedToDatabase = false;
    private int healthBeforeDeath = 0;
    private bool isProcessingConfirm = false;
    private bool summaryLocked = false;
    private bool summaryTriggeredByKeyCollection = false;

    void Awake()
    {
        var existingInstances = FindObjectsOfType<K3_GameSummary>();
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

    private void FindAllReferences()
    {
        // K3 SPECIFIC: Changed to PreserviaPlayerStat
        playerHealth = FindObjectOfType<PreserviaPlayerStat>();
        gameplayProgression = FindObjectOfType<K3_GameplayProgression>();
        scoringSystem = FindObjectOfType<PreserviaScoringSystem>();
        mainMenuManager = FindObjectOfType<MainMenu_Manager>();
        playerObject = GameObject.FindGameObjectWithTag("Player");
        collectKeyScript = FindObjectOfType<K3_CollectKey>(); // K3 specific

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

        if (restartButton != null)
            restartButton.onClick.AddListener(OnConfirmButtonClicked);

        if (backgroundMusicSource != null)
            originalBackgroundMusicVolume = backgroundMusicSource.volume;

        if (KeyImageunlocking != null)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking initialized as DISABLED");
        }

        Debug.Log($"K3 GameSummary initialized - Complete Restart: {completeRestartOnConfirm}");
    }

    private void CheckGameConditions()
    {
        if (summaryLocked) return;
        
        // Check for lose condition (health reaches 0) - 0 STARS
        if (!isGameOver && !isSummaryActive && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
            return;
        }
        
        // Check for key collection trigger - USING K3_CollectKey
        if (collectKeyScript != null && collectKeyScript.HasTriggeredSummary() && !isGameOver && !isSummaryActive)
        {
            Debug.Log("K3 Key collection triggered summary");
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            StartCoroutine(ShowSummaryPanel());
        }
    }

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

        Debug.Log($"Starting K3 ShowSummaryPanel() - Victory: {isVictory}");

        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        PrepareGameForSummary();
        yield return null;

        yield return TriggerLookAroundAnimationDuringPause();
        PlayResultSound();

        CalculateCoinReward();
        UpdateSummaryData();

        ShowPanelWithAnimation();

        Debug.Log($"K3 Game {(isVictory ? "won" : "lost")} - Summary panel shown");
        
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
    }

    private void PrepareGameForSummary()
    {
        DisableCinemachineBlending();
        MovePlayerToSpawnPoint();
        DisablePlayerInput();
        LowerBackgroundMusicVolume();
        SwitchToSummaryCameraImmediate();
        HideJoystickCanvas();
    }

    private void ShowPanelWithAnimation()
    {
        if (gameSummaryPanel == null)
        {
            Debug.LogError("K3 Game Summary Panel is not assigned!");
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

    private void UpdateSummaryData()
    {
        currentStars = CalculateStars();
        UpdateKeyStatus(currentStars);
        UpdateTimePlayed();
        UpdateScore();
        UpdateCoinsEarned();
        UpdateStarsEarnedText();
        UpdateKeyImageDisplay();
        
        if (!isVictory)
        {
            if (failGameObject1 != null && failGameObject1.activeSelf)
                failGameObject1.SetActive(false);
            
            if (failGameObject2 != null && failGameObject2.activeSelf)
                failGameObject2.SetActive(false);
            
            if (failGameObject3 != null && failGameObject3.activeSelf)
                failGameObject3.SetActive(false);
        }
        
        Debug.Log($"=== K3 UPDATE SUMMARY DATA ===");
        Debug.Log($"Current stars calculated: {currentStars}");
        Debug.Log($"Stars earned text will show: {currentStars}/3");
        Debug.Log($"Summary triggered by key collection: {summaryTriggeredByKeyCollection}");
    }

    private void UpdateKeyImageDisplay()
    {
        if (KeyImageunlocking != null)
        {
            bool shouldShowKeyImage = isSummaryActive && 
                                     summaryTriggeredByKeyCollection && 
                                     currentStars >= 2;
            
            KeyImageunlocking.SetActive(shouldShowKeyImage);
            
            Debug.Log($"KeyImageunlocking: {(shouldShowKeyImage ? "SHOWN" : "HIDDEN")} " +
                     $"- SummaryActive: {isSummaryActive} " +
                     $"- TriggeredByKey: {summaryTriggeredByKeyCollection} " +
                     $"- Stars: {currentStars}");
        }
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
            health = playerHealth?.currentHealth ?? 0;
            Debug.Log($"Using current health for victory stars: {health}");
        }
        else
        {
            health = Mathf.Max(0, healthBeforeDeath);
            Debug.Log($"Using health before death for lose: {health}");
        }
        
        int stars = 0;
        
        if (health >= 5) stars = 3;
        else if (health >= 3) stars = 2;
        else if (health >= 1) stars = 1;
        
        Debug.Log($"=== K3 CALCULATE STARS ===");
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
            
            if (!starAnimator.gameObject.activeSelf)
            {
                Debug.Log("Activating star animator GameObject");
                starAnimator.gameObject.SetActive(true);
            }
            
            if (!starAnimator.enabled)
            {
                Debug.Log("Enabling star animator component");
                starAnimator.enabled = true;
            }
            
            starAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            starAnimator.SetInteger(starParameterName, 0);
            starAnimator.Update(0f);
            
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
        
        starAnimator.SetInteger(starParameterName, currentStars);
        starAnimator.Update(0f);
        
        int currentValue = starAnimator.GetInteger(starParameterName);
        Debug.Log($"Star parameter set to: {currentValue} (requested: {currentStars})");
        
        AnimatorStateInfo stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"Current animation state: {stateInfo.fullPathHash}");
        Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
        Debug.Log($"Is in transition: {starAnimator.IsInTransition(0)}");
        
        if (stateInfo.normalizedTime == 0 && currentStars > 0)
        {
            Debug.Log("Attempting to play animation directly...");
            ForcePlayStarAnimation(currentStars);
        }
        
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
            
            starAnimator.Play(stateName, 0, 0f);
            starAnimator.Update(0f);
        }
    }

    private void ResetStarAnimator()
    {
        if (starAnimator != null)
        {
            Debug.Log("Resetting star animator...");
            
            starAnimator.SetInteger(starParameterName, 0);
            starAnimator.updateMode = AnimatorUpdateMode.Normal;
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

    public void OnConfirmButtonClicked()
    {
        if (!isSummaryActive || !isGameOver || isProcessingConfirm) return;
        
        isProcessingConfirm = true;

        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (confirmButton != null)
            confirmButton.interactable = false;

        if (completeRestartOnConfirm)
        {
            Debug.Log("Complete restart requested - reloading scene");
            StartCoroutine(CompleteRestartGame());
        }
        else
        {
            StartCoroutine(HidePanelAndRestartGame());
        }
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
        
        isProcessingConfirm = false;

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private IEnumerator CompleteRestartGame()
    {
        Debug.Log("Starting complete game restart...");
        
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);
        
        Time.timeScale = originalTimeScale;
        
        yield return new WaitForSecondsRealtime(0.1f);
        
        ReloadCurrentScene();
    }

    private void PlayButtonClickSound()
    {
        AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
        if (audioHandler != null)
            InvokeMethodIfExists(audioHandler, "PlayButtonClick");
    }

    // Method to invoke a method by name on an object
    private void InvokeMethodIfExists(object target, string methodName)
    {
        if (target == null) return;

        var method = target.GetType().GetMethod(methodName);
        if (method != null)
            method.Invoke(target, null);
    }

    private void RestartGame()
    {
        Debug.Log("Restarting K3 game...");

        SwitchToPlayerCameraWithBlend();
        ResetGameState();
        EnablePlayerInput();
        EnsureGameMode();
        ResetManager();

        summaryLocked = false;
        Debug.Log("K3 Game restarted - Ready to play again!");
    }

    private void ResetGameState()
    {
        // Reset player systems - USING PreserviaPlayerStat
        if (playerHealth != null) playerHealth.ResetHealth();
        if (scoringSystem != null) scoringSystem.ResetSessionStats();
        if (gameplayProgression != null) InvokeMethodIfExists(gameplayProgression, "ResetTimer");

        ResetAllMonsters();
        ResetKeySystem();

        Debug.Log("K3 Game state reset");
    }

    private void ResetAllMonsters()
    {
        // Find any monsters in K3 scene
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
        // Reset all K3 key scripts
        K3_CollectKey[] allKeyScripts = FindObjectsOfType<K3_CollectKey>();
        foreach (K3_CollectKey keyScript in allKeyScripts)
        {
            if (keyScript != null)
            {
                InvokeMethodIfExists(keyScript, "ResetKey");
                InvokeMethodIfExists(keyScript, "ForceFullReset");
            }
        }

        // Destroy remaining K3 key objects
        GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
        foreach (GameObject key in remainingKeys)
            Destroy(key);
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
        isSummaryActive = false;
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        currentStars = 0;
        summaryTriggeredByKeyCollection = false;
        ResetStarAnimator();

        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking hidden during manager reset");
        }

        if (starsEarnedText != null)
            starsEarnedText.text = "0/3";

        Debug.Log("K3 GameSummaryManager reset for new game");
    }

    private void ReloadCurrentScene()
    {
        Debug.Log("Reloading scene for complete restart...");
        
        string sceneName = string.IsNullOrEmpty(sceneToReload) ? 
            SceneManager.GetActiveScene().name : sceneToReload;
        
        ResetPersistentData();
        SceneManager.LoadScene(sceneName);
    }

    private void ResetPersistentData()
    {
        Debug.Log("Resetting persistent data...");
        
        // Use K3_CollectKey for global reset
        K3_CollectKey.GlobalResetAllKeys();
        
        Debug.Log("Persistent data reset complete");
    }

    public void TriggerSummaryFromKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("K3 TriggerSummaryFromKey called - marking summary as triggered by key collection");
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot trigger K3 summary from key - already active or game over");
        }
    }

    public IEnumerator ShowSummaryPanelDirectly(bool isWin)
    {
        if (isGameOver || isSummaryActive) 
        {
            Debug.LogWarning("K3 Summary panel already active, cannot show directly");
            yield break;
        }
        
        summaryLocked = true;
        isGameOver = true;
        isSummaryActive = true;
        isVictory = isWin;
        summaryTriggeredByKeyCollection = false;
        
        Debug.Log($"Starting K3 ShowSummaryPanelDirectly() - Victory: {isVictory}, TriggeredByKey: {summaryTriggeredByKeyCollection}");
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        PrepareGameForSummary();
        yield return null;
        
        yield return TriggerLookAroundAnimationDuringPause();
        PlayResultSound();
        
        CalculateCoinReward();
        UpdateSummaryData();
        
        ShowPanelWithAnimation();
        
        Debug.Log($"K3 Summary panel shown directly");
        
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
    }

    public bool IsSummaryActive()
    {
        return isSummaryActive;
    }

    public bool HasPreserviaKey()
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
    }

    public void ResetPreserviaKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.ResetPreserviaKey();
            GameDataManager.Instance.SaveGameData();
            Debug.Log("PreserviaKey reset in GameData");
        }
    }

    public void SetCompleteRestart(bool enabled)
    {
        completeRestartOnConfirm = enabled;
        Debug.Log($"Complete restart on confirm: {enabled}");
    }

    public void SetSceneToReload(string sceneName)
    {
        sceneToReload = sceneName;
        Debug.Log($"Scene to reload set to: {sceneName}");
    }

    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            if (playerHealth != null) playerHealth.currentHealth = 6;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    [ContextMenu("Test Win without Key")]
    public void TestWinWithoutKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = false;
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
            summaryTriggeredByKeyCollection = false;
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
        
        currentStars = CalculateStars();
        UpdateStarsEarnedText();
        
        Debug.Log($"Stars calculated: {currentStars}");
        
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
            
            if (!starAnimator.gameObject.activeSelf)
                starAnimator.gameObject.SetActive(true);
            
            if (!starAnimator.enabled)
                starAnimator.enabled = true;
            
            starAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            for (int i = 0; i <= 3; i++)
            {
                Debug.Log($"\n=== Testing star value: {i} ===");
                
                starAnimator.SetInteger(starParameterName, 0);
                starAnimator.Update(0f);
                
                System.Threading.Thread.Sleep(100);
                
                starAnimator.SetInteger(starParameterName, i);
                starAnimator.Update(0f);
                
                AnimatorStateInfo stateInfo = starAnimator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"Current state: {stateInfo.fullPathHash}");
                Debug.Log($"Normalized time: {stateInfo.normalizedTime}");
                Debug.Log($"Is in transition: {starAnimator.IsInTransition(0)}");
                
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

    void OnDestroy()
    {
        if (isGameOver)
            Time.timeScale = originalTimeScale;

        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = originalBackgroundMusicVolume;

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnConfirmButtonClicked);
        
        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
        }
    }
        // Add this method to K3_GameSummary class:
    public void TriggerQA2CompletionSummary()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("K3: Triggering summary from QA/Assessment completion");
            isVictory = true;
            summaryTriggeredByKeyCollection = false; // Not triggered by key
            StartCoroutine(ShowSummaryPanel());
        }
    }
        // Add this method to K3_GameSummary class:
    public void TriggerAssessmentCompletionSummary()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("K3: Triggering summary from assessment completion (no key)");
            isVictory = true;
            summaryTriggeredByKeyCollection = false; // Not triggered by key
            StartCoroutine(ShowSummaryPanel());
        }
    }
}
