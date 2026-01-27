using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using Cinemachine;
using UnityEngine.SceneManagement; // Add this for scene reloading

public class K4GameSummary : MonoBehaviour
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
    private bool timelineTriggered = false;


    [Header("Star Animations")]
    public Animator starAnimator;
    public string starParameterName = "star";
    private int currentStars = 0;

    [Header("Key Image Display")]
    public GameObject KeyImageunlocking; // Game object that shows key image (initially disabled)
    [Header("Fail Game Objects (Disabled on Lose)")]
    public GameObject failGameObject1;
    public GameObject failGameObject2;
    public GameObject failGameObject3;
    
    [Header("Buttons")]
    public Button confirmButton;
    public Button restartButton; // Add a dedicated restart button if needed

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

    [Header("Timeline Settings")]
    public GameObject timelineController; // Reference to timeline controller GameObject
public string timelineObjectName = "K4_KeyTimeline";

    [Header("Complete Restart Settings")]
    public bool completeRestartOnConfirm = true; // NEW: Toggle for complete restart
    public string sceneToReload = ""; // Leave empty to reload current scene

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
    private Animator playerAnimator;
    private AudioSource audioSource;
    private K2_CollectKey collectKeyScript;

    // Game state
    private bool isGameOver = false;
    private bool isVictory = false;
    private bool isSummaryActive = false;
    private float originalTimeScale;
    private int calculatedCoinsEarned = 0;
    private bool coinsAddedToDatabase = false;
    private int healthBeforeDeath = 0;
    private bool isProcessingConfirm = false;
    private bool summaryLocked = false;

    // Key tracking
    private bool summaryTriggeredByKeyCollection = false;

    void Awake()
    {
        // Singleton pattern
        var existingInstances = FindObjectsOfType<K4GameSummary>();
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
        collectKeyScript = FindObjectOfType<K2_CollectKey>();

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

        // Add restart button listener if exists
        if (restartButton != null)
            restartButton.onClick.AddListener(OnConfirmButtonClicked);

        if (backgroundMusicSource != null)
            originalBackgroundMusicVolume = backgroundMusicSource.volume;

        // Initialize KeyImageunlocking - disable by default
        if (KeyImageunlocking != null)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking initialized as DISABLED");
        }

        // NEW: Check and disable timeline if key is already collected
        CheckAndDisableTimelineOnStart();

        Debug.Log($"GameSummary initialized - Complete Restart: {completeRestartOnConfirm}");
    }

    private void CheckAndDisableTimelineOnStart()
    {
        bool keyAlreadyCollected = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasSugariaKey();
        
        if (keyAlreadyCollected && !string.IsNullOrEmpty(timelineObjectName))
        {
            DisableTimelineIfExists();
            Debug.Log("Timeline disabled on start (key already collected)");
        }
    }

    #endregion

    #region Game Condition Checks

    private void CheckGameConditions()
    {
        if (summaryLocked) return;
        
        // Check for lose condition (health reaches 0) - 0 STARS
        if (!isGameOver && !isSummaryActive && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false; // This is a LOSE
            StartCoroutine(ShowSummaryPanel());
            return; // Exit early after triggering lose
        }

        // Check for timeline conditions
        CheckTimelineConditions();
    }

    private void CheckTimelineConditions()
    {
        if (playerHealth == null || isSummaryActive || isGameOver) return;

        int currentHealth = playerHealth.currentHealth;
        bool keyAlreadyCollected = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasSugariaKey();
        
        // NEW: Also check if key was just collected in this session
        if (!keyAlreadyCollected && collectKeyScript != null)
        {
            keyAlreadyCollected = collectKeyScript.HasTriggeredSummary();
        }
        
        Debug.Log($"Health: {currentHealth}, Key Collected: {keyAlreadyCollected}");
        
        // Heart = 0: Lose Summary (only at 0 hearts) - 0 STARS
        if (currentHealth <= 0)
        {
            Debug.Log($"Health ({currentHealth}) = 0. Triggering LOSE summary with 0 stars...");
            healthBeforeDeath = currentHealth;
            isVictory = false; // This is a LOSE
            StartCoroutine(ShowSummaryPanel());
            return;
        }
        // Heart 1–2: Player continues playing, no summary, no timeline
if (currentHealth > 0 && currentHealth < 3)
{
    return;
}// Heart ≥ 3: Allow timeline for key collection
if (currentHealth >= 3 && !keyAlreadyCollected && !timelineTriggered)
{
    timelineTriggered = true;
    TryActivateTimeline();
}



    }
    

    // Add this new method to disable timeline:
    private void DisableTimelineIfExists()
    {
        if (string.IsNullOrEmpty(timelineObjectName)) return;
        
        GameObject timelineObj = GameObject.Find(timelineObjectName);
        if (timelineObj != null && timelineObj.activeInHierarchy)
        {
            timelineObj.SetActive(false);
            Debug.Log($"Disabled timeline (key already collected): {timelineObjectName}");
            
            // Also disable K2_QueenACS2 component
            K2_QueenACS2 queenCutscene = timelineObj.GetComponent<K2_QueenACS2>();
            if (queenCutscene != null)
            {
                queenCutscene.enabled = false;
                Debug.Log("Disabled K2_QueenACS2 component");
            }
        }
    }

    private void TryActivateTimeline()
    {
        if (string.IsNullOrEmpty(timelineObjectName)) return;
        
        // Check if key is already collected
        bool keyAlreadyCollected = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasSugariaKey();
        
        if (keyAlreadyCollected)
        {
            Debug.Log("Key already collected. Timeline will not play.");
            DisableTimelineIfExists();
            return;
        }
        
        GameObject timelineObj = GameObject.Find(timelineObjectName);
        if (timelineObj != null)
        {
            // Check if timeline has already been played or is active
            if (!timelineObj.activeInHierarchy)
            {
                Debug.Log($"Activating timeline: {timelineObjectName}");
                timelineObj.SetActive(true);
                
                // Make sure K2_QueenACS2 is enabled
                K2_QueenACS2 queenCutscene = timelineObj.GetComponent<K2_QueenACS2>();
                if (queenCutscene != null && !queenCutscene.enabled)
                {
                    queenCutscene.enabled = true;
                    Debug.Log("Enabled K2_QueenACS2 component for timeline");
                }
                
                // Get timeline controller component if exists
                if (timelineController != null)
                {
                    // Try to play timeline
                    System.Reflection.MethodInfo playMethod = timelineController.GetType().GetMethod("PlayTimeline");
                    if (playMethod != null)
                    {
                        playMethod.Invoke(timelineController, null);
                    }
                }
            }
            else
            {
                Debug.Log($"Timeline {timelineObjectName} is already active.");
            }
        }
        else
        {
            Debug.LogWarning($"Timeline object '{timelineObjectName}' not found in scene.");
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
                 obj.name.Contains("Nutrition") || obj.name.Contains("Menu") ||
                 (obj.name.Contains("Timeline") && obj.name != timelineObjectName)))
            {
                obj.SetActive(false);
                Debug.Log($"Closed interfering UI: {obj.name}");
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
        UpdateKeyImageDisplay();
        
        // NEW: Disable fail game objects only when losing
        if (!isVictory)
        {
            // This is a lose summary, disable the fail game objects
            if (failGameObject1 != null && failGameObject1.activeSelf)
                failGameObject1.SetActive(false);
            
            if (failGameObject2 != null && failGameObject2.activeSelf)
                failGameObject2.SetActive(false);
            
            if (failGameObject3 != null && failGameObject3.activeSelf)
                failGameObject3.SetActive(false);
        }
        
        Debug.Log($"=== UPDATE SUMMARY DATA ===");
        Debug.Log($"Current stars calculated: {currentStars}");
        Debug.Log($"Stars earned text will show: {currentStars}/3");
        Debug.Log($"Summary triggered by key collection: {summaryTriggeredByKeyCollection}");
    }

    // NEW METHOD: Update the KeyImageunlocking display
    private void UpdateKeyImageDisplay()
    {
        if (KeyImageunlocking != null)
        {
            // Key image should ONLY be shown when:
            // 1. Summary is active
            // 2. Summary was triggered by key collection (not by other means like QA2 completion or losing)
            // 3. AND player has at least 2 stars (3+ hearts)
            
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
            // For victory, use current health
            health = playerHealth?.currentHealth ?? 0;
            Debug.Log($"Using current health for victory stars: {health}");
        }
        else
        {
            // For lose condition (only at 0 hearts)
            health = Mathf.Max(0, healthBeforeDeath);
            Debug.Log($"Using health before death for lose: {health}");
        }
        
        int stars = 0;
        
        if (health >= 5) stars = 3;
        else if (health >= 3) stars = 2;
        else if (health >= 1) stars = 1;
        // 0 hearts = 0 stars (already 0)
        
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
            // Key is unlocked only if player has 2+ stars (3+ hearts)
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

        // NEW: Option to completely restart the game
        if (completeRestartOnConfirm)
        {
            Debug.Log("Complete restart requested - reloading scene");
            StartCoroutine(CompleteRestartGame());
        }
        else
        {
            // Original behavior - soft reset
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
        
        // Reset the processing flag
        isProcessingConfirm = false;

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    // NEW: Complete restart method
    private IEnumerator CompleteRestartGame()
    {
        Debug.Log("Starting complete game restart...");
        
        // Fade out panel if available
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        // Hide the summary panel
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);
        
        // Reset time scale
        Time.timeScale = originalTimeScale;
        
        // Add a small delay to ensure UI is hidden
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Reload the scene
        ReloadCurrentScene();
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

        // Reset monsters
        ResetAllMonsters();

        // Reset key system (BUT NOT THE PERSISTENT SUGARIAKEY)
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
        // Reset all key scripts (session-specific only)
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
        timelineTriggered = false;
        isGameOver = false;
        isVictory = false;
        isSummaryActive = false;
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        currentStars = 0;
        summaryTriggeredByKeyCollection = false; // Reset this flag
        ResetStarAnimator();

        // Ensure KeyImageunlocking is hidden when resetting
        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking hidden during manager reset");
        }

        if (starsEarnedText != null)
            starsEarnedText.text = "0/3";

        Debug.Log("GameSummaryManager reset for new game");
    }

    #endregion

    #region Complete Scene Reload

    // NEW: Method to reload the current scene
    private void ReloadCurrentScene()
    {
        Debug.Log("Reloading scene for complete restart...");
        
        // Get the current scene name
        string sceneName = string.IsNullOrEmpty(sceneToReload) ? 
            SceneManager.GetActiveScene().name : sceneToReload;
        
        // Reset all static flags and persistent data if needed
        ResetPersistentData();
        
        // Load the scene
        SceneManager.LoadScene(sceneName);
    }

    // NEW: Reset any persistent data that should be cleared on restart
    private void ResetPersistentData()
    {
        Debug.Log("Resetting persistent data...");
        
        // Reset global key flags
        K2_CollectKey.GlobalResetAllKeys();
        
        // Optionally reset SugariaKey if you want fresh start
        // Uncomment the next line if you want to reset the key on complete restart
        // if (GameDataManager.Instance != null) ResetSugariaKey();
        
        // Clear any static variables or flags
        // Add any other static resets here
        
        Debug.Log("Persistent data reset complete");
    }

    #endregion

    #region Public Methods

    // Add this method to manually trigger QA2 completion summary
    #region Public Methods for External Triggers

    // Add this method to make it easier for key collection to trigger summary
    public void TriggerSummaryFromKey()
{
    if (!isGameOver && !isSummaryActive)
    {
        Debug.Log("Key collected → Triggering Game Summary");
        isVictory = true;
        summaryTriggeredByKeyCollection = true;
        StartCoroutine(ShowSummaryPanel());
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
        
        // If calling directly, assume not triggered by key collection unless specified
        summaryTriggeredByKeyCollection = false;
        
        Debug.Log($"Starting ShowSummaryPanelDirectly() - Victory: {isVictory}, TriggeredByKey: {summaryTriggeredByKeyCollection}");
        
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

    // Method to check if SugariaKey is collected (persistent)
    public bool HasSugariaKey()
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData.HasSugariaKey();
    }

    // Method to reset SugariaKey (for testing or new game)
    public void ResetSugariaKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.ResetSugariaKey();
            GameDataManager.Instance.SaveGameData();
            Debug.Log("SugariaKey reset in GameData");
        }
    }

    // NEW: Toggle complete restart
    public void SetCompleteRestart(bool enabled)
    {
        completeRestartOnConfirm = enabled;
        Debug.Log($"Complete restart on confirm: {enabled}");
    }

    // NEW: Set scene to reload
    public void SetSceneToReload(string sceneName)
    {
        sceneToReload = sceneName;
        Debug.Log($"Scene to reload set to: {sceneName}");
    }

    #endregion

    #region Debug & Testing

    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = true; // Simulate key collection trigger
            if (playerHealth != null) playerHealth.currentHealth = 6;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    public void TestWinWithoutKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = false; // Not triggered by key
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
            summaryTriggeredByKeyCollection = false; // Lose is never triggered by key
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

    [ContextMenu("Check SugariaKey Status")]
    public void CheckSugariaKeyStatus()
    {
        bool hasKey = HasSugariaKey();
        Debug.Log($"SugariaKey status: {(hasKey ? "COLLECTED" : "NOT COLLECTED")}");
    }

    [ContextMenu("Collect SugariaKey (Test)")]
    public void TestCollectSugariaKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.CollectSugariaKey();
            GameDataManager.Instance.SaveGameData();
            Debug.Log("SugariaKey collected and saved to GameData");
        }
    }

    [ContextMenu("Reset SugariaKey (Test)")]
    public void TestResetSugariaKey()
    {
        ResetSugariaKey();
    }

    [ContextMenu("Test Complete Restart")]
    public void TestCompleteRestart()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            if (playerHealth != null) playerHealth.currentHealth = 6;
            StartCoroutine(ShowSummaryPanel());
            
            // After showing summary, trigger complete restart
            StartCoroutine(TestCompleteRestartCoroutine());
        }
    }

    private IEnumerator TestCompleteRestartCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        OnConfirmButtonClicked();
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
        
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnConfirmButtonClicked);
        
        // Ensure KeyImageunlocking is not left active
        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
        }
    }
}