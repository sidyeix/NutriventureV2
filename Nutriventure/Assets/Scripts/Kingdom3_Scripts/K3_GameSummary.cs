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
    
    [Header("Key Unlocked Animation")]
    public GameObject keyUnlockedAnimation;
    public Button continueKeyButton;
    public KeyUnlockedCanvasController keyUnlockedController;
    
    [Header("Fail Game Objects (Disabled on Lose)")]
    public GameObject failGameObject1;
    public GameObject failGameObject2;
    public GameObject failGameObject3;
    
    [Header("Buttons")]
    public Button restartButton;
    public Button homeButton;

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

    [Header("Count Animation Settings")]
    public float countAnimationDuration = 2f;
    public AudioClip countTickSound;
    public AudioClip countCompleteSound;
    [SerializeField] private AudioSource countAudioSource;

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
    
    [Header("Character Win/Lose Animation")]
    public Animator characterAnimator;
    public string danceParameter = "isDance";
    public string thinkParameter = "isThinking";

    [Header("UI References")]
    public GameObject joystickCanvas;

    [Header("Complete Restart Settings")]
    public bool completeRestartOnConfirm = true;
    public string sceneToReload = "";

    private string[] starStateNames = new string[] { "Empty", "Star1", "Star2", "Star3" };

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
    
    private Coroutine countAnimationCoroutine;
    private bool isCountingAnimationComplete = false;
    private bool isCharacterVisualSwapperEnabledBeforeSummary = true;

    // Key Collection State
    private bool keyWasCollected = false;
    private bool keySavedToDatabase = false;

    // Store original positions for reset
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

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
        playerHealth = FindObjectOfType<PreserviaPlayerStat>();
        gameplayProgression = FindObjectOfType<K3_GameplayProgression>();
        scoringSystem = FindObjectOfType<PreserviaScoringSystem>();
        mainMenuManager = FindObjectOfType<MainMenu_Manager>();
        playerObject = GameObject.FindGameObjectWithTag("Player");
        collectKeyScript = FindObjectOfType<K3_CollectKey>();

        if (characterVisualSwapper == null)
            characterVisualSwapper = FindObjectOfType<CharacterVisualSwapper>();

        if (playerAnimator == null && playerObject != null)
            playerAnimator = playerObject.GetComponentInChildren<Animator>();

        if (characterAnimator == null && playerAnimator != null)
        {
            characterAnimator = playerAnimator;
        }

        if (backgroundMusicSource == null)
            backgroundMusicSource = FindBackgroundMusicSource();

        if (audioSource == null)
            CreateAudioSource();

        if (countAudioSource == null)
            countAudioSource = gameObject.AddComponent<AudioSource>();
        
        if (playerObject == null)
            playerObject = GameObject.Find("PlayerArmature");

        if (cinemachineBrain == null)
            cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();
            
        // Store original player position
        if (playerObject != null)
        {
            originalPlayerPosition = playerObject.transform.position;
            originalPlayerRotation = playerObject.transform.rotation;
        }
        
        // Find KeyUnlockedController if not assigned
        if (keyUnlockedController == null && keyUnlockedAnimation != null)
        {
            keyUnlockedController = keyUnlockedAnimation.GetComponent<KeyUnlockedCanvasController>();
        }
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

        // Initialize KeyUnlockedAnimation
        if (keyUnlockedAnimation != null)
            keyUnlockedAnimation.SetActive(false);
            
        if (continueKeyButton != null)
        {
            continueKeyButton.onClick.AddListener(OnContinueKeyButtonClicked);
            Debug.Log("ContinueKeyButton listener added");
        }

        ResetStarAnimator();

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRetryButtonClicked);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnHomeButtonClicked);

        if (backgroundMusicSource != null)
            originalBackgroundMusicVolume = backgroundMusicSource.volume;

        if (KeyImageunlocking != null)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking initialized as DISABLED");
        }

        isCountingAnimationComplete = false;
        keyWasCollected = false;
        keySavedToDatabase = false;
        
        Debug.Log($"K3 GameSummary initialized - Complete Restart: {completeRestartOnConfirm}");
    }

    private void CheckGameConditions()
    {
        if (summaryLocked) return;
        
        if (!isGameOver && !isSummaryActive && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
            return;
        }
        
        if (collectKeyScript != null && collectKeyScript.HasTriggeredSummary() && !isGameOver && !isSummaryActive)
        {
            Debug.Log("K3 Key collection triggered summary");
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            keyWasCollected = true;
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

        yield return TriggerCharacterAnimationDuringPause();
        PlayResultSound();

        CalculateCoinReward();
        UpdateSummaryData();

        ShowPanelWithAnimation();

        Debug.Log($"K3 Game {(isVictory ? "won" : "lost")} - Summary panel shown");
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        PlayStarAnimationDirect();
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        if (countAnimationCoroutine != null)
            StopCoroutine(countAnimationCoroutine);
        
        countAnimationCoroutine = StartCoroutine(AnimateCountingNumbers());
    }

    private void PrepareGameForSummary()
    {
        // Store current position before moving to spawn point
        if (playerObject != null)
        {
            originalPlayerPosition = playerObject.transform.position;
            originalPlayerRotation = playerObject.transform.rotation;
            Debug.Log($"Stored original player position: {originalPlayerPosition}");
        }
        
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
        
        // Disable home button on lose
        if (!isVictory && homeButton != null)
        {
            homeButton.interactable = false;
            Debug.Log("Home button disabled on lose");
        }
        
        ResetAllTextToZero();
    }

    private void ResetAllTextToZero()
    {
        if (timePlayedText != null)
            timePlayedText.text = "00:00";
        
        if (gameScoreText != null)
            gameScoreText.text = "0";
        
        if (coinsEarnedText != null)
            coinsEarnedText.text = "0";
        
        if (starsEarnedText != null)
            starsEarnedText.text = "0/3";
        
        Debug.Log("All summary text reset to 0 for animation");
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

    private IEnumerator TriggerCharacterAnimationDuringPause()
    {
        if (characterAnimator != null)
        {
            characterAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            Debug.Log($"=== Setting character animation ===");
            Debug.Log($"isVictory: {isVictory}");
            Debug.Log($"Dance parameter: {danceParameter}");
            Debug.Log($"LookAround parameter: {lookAroundParameter}");
            
            // Store CharacterVisualSwapper state before modifying
            if (characterVisualSwapper != null)
            {
                isCharacterVisualSwapperEnabledBeforeSummary = characterVisualSwapper.enabled;
                Debug.Log($"Stored CharacterVisualSwapper enabled state: {isCharacterVisualSwapperEnabledBeforeSummary}");
            }
            
            // WIN: Set dance animation
            if (isVictory)
            {
                Debug.Log("WIN - Setting dance animation");
                
                // Disable CharacterVisualSwapper for win to prevent interference
                if (characterVisualSwapper != null)
                {
                    characterVisualSwapper.enabled = false;
                    Debug.Log("Disabled CharacterVisualSwapper for win animation");
                }
                
                // Reset other animations first
                if (!string.IsNullOrEmpty(lookAroundParameter))
                {
                    characterAnimator.SetBool(lookAroundParameter, false);
                    Debug.Log($"Set {lookAroundParameter} = false");
                }
                
                if (!string.IsNullOrEmpty(thinkParameter))
                {
                    characterAnimator.SetBool(thinkParameter, false);
                    Debug.Log($"Set {thinkParameter} = false");
                }
                
                // Turn Dance ON
                if (!string.IsNullOrEmpty(danceParameter))
                {
                    characterAnimator.SetBool(danceParameter, true);
                    Debug.Log($"Set {danceParameter} = true");
                }
            }
            // LOSE: Set look around animation
            else
            {
                Debug.Log("LOSE - Setting look around animation");
                
                // Enable CharacterVisualSwapper for lose
                if (characterVisualSwapper != null && !characterVisualSwapper.enabled)
                {
                    characterVisualSwapper.enabled = true;
                    Debug.Log("Enabled CharacterVisualSwapper for lose animation");
                }
                
                // Reset other animations first
                if (!string.IsNullOrEmpty(danceParameter))
                {
                    characterAnimator.SetBool(danceParameter, false);
                    Debug.Log($"Set {danceParameter} = false");
                }
                
                if (!string.IsNullOrEmpty(thinkParameter))
                {
                    characterAnimator.SetBool(thinkParameter, false);
                    Debug.Log($"Set {thinkParameter} = false");
                }
                
                // Turn LookAround ON
                if (!string.IsNullOrEmpty(lookAroundParameter))
                {
                    characterAnimator.SetBool(lookAroundParameter, true);
                    Debug.Log($"Set {lookAroundParameter} = true");
                }
                
                // Trigger CharacterVisualSwapper for lose
                if (characterVisualSwapper != null)
                {
                    characterVisualSwapper.TriggerLookAroundAnimation();
                    Debug.Log("Triggered CharacterVisualSwapper LookAround animation");
                }
            }
            
            // Force update immediately
            characterAnimator.Update(0f);
            
            // DEBUG: Check the actual values
            bool danceValue = !string.IsNullOrEmpty(danceParameter) ? characterAnimator.GetBool(danceParameter) : false;
            bool lookAroundValue = !string.IsNullOrEmpty(lookAroundParameter) ? characterAnimator.GetBool(lookAroundParameter) : false;
            Debug.Log($"After setting - Dance: {danceValue}, LookAround: {lookAroundValue}");
        }

        yield return new WaitForSecondsRealtime(0.1f);
    }

    private void StopCharacterAnimationDuringPause()
    {
        if (characterAnimator != null)
        {
            // Reset all animation parameters
            if (!string.IsNullOrEmpty(danceParameter))
                characterAnimator.SetBool(danceParameter, false);
            
            if (!string.IsNullOrEmpty(lookAroundParameter))
                characterAnimator.SetBool(lookAroundParameter, false);
            
            if (!string.IsNullOrEmpty(thinkParameter))
                characterAnimator.SetBool(thinkParameter, false);
            
            characterAnimator.Update(0f);
            characterAnimator.updateMode = AnimatorUpdateMode.Normal;
        }

        // Restore CharacterVisualSwapper to its original state
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.enabled = isCharacterVisualSwapperEnabledBeforeSummary;
            Debug.Log($"Restored CharacterVisualSwapper enabled to: {isCharacterVisualSwapperEnabledBeforeSummary}");
            
            if (characterVisualSwapper.enabled)
            {
                characterVisualSwapper.StopLookAroundAnimation();
            }
        }
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

    private void UpdateKeyStatus(int stars)
    {
        if (keyStatusText != null)
        {
            bool isUnlocked = (stars >= 2);
            keyStatusText.text = isUnlocked ? "KEY: UNLOCKED" : "KEY: LOCKED";
            keyStatusText.color = isUnlocked ? unlockedColor : lockedColor;
        }
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

    private IEnumerator AnimateCountingNumbers()
    {
        // Fixed null check
        if (timePlayedText == null || gameScoreText == null || coinsEarnedText == null)
        {
            Debug.LogError("One or more text fields for counting animation are null!");
            yield break;
        }

        // Get final values
        float finalTimePlayed = gameplayProgression != null ? gameplayProgression.GetCurrentTime() : 0f;
        int finalScore = scoringSystem != null ? scoringSystem.GetCurrentScore() : 0;
        int finalCoins = calculatedCoinsEarned;

        Debug.Log($"Starting counting animation - Final values: Time={finalTimePlayed}, Score={finalScore}, Coins={finalCoins}");

        // RESET: Ensure all values start at 0
        timePlayedText.text = "00:00";
        gameScoreText.text = "0";
        coinsEarnedText.text = "0";

        yield return new WaitForSecondsRealtime(0.3f);

        float elapsedTime = 0f;
        int lastPlayedTickScore = 0;
        
        // Calculate how many ticks we want (more ticks for larger scores)
        int numberOfTicks = Mathf.Clamp(finalScore / 50, 10, 30); // At least 10 ticks, max 30
        float tickInterval = countAnimationDuration / numberOfTicks;
        float nextTickTime = 0f;
        
        Debug.Log($"Audio: Will play {numberOfTicks} ticks every {tickInterval:F2} seconds");

        while (elapsedTime < countAnimationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = elapsedTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Calculate current animated values
            float currentScore = Mathf.Lerp(0, finalScore, smoothProgress);
            float currentTime = Mathf.Lerp(0, finalTimePlayed, smoothProgress);
            float currentCoins = Mathf.Lerp(0, finalCoins, smoothProgress);

            int currentIntScore = Mathf.FloorToInt(currentScore);

            // Play tick sound at regular intervals
            if (elapsedTime >= nextTickTime)
            {
                // Only play if we have audio assets
                if (countTickSound != null && countAudioSource != null)
                {
                    // Don't stop previous sound - let it play out
                    // Just play the new one
                    countAudioSource.PlayOneShot(countTickSound, 0.5f); // 50% volume
                    Debug.Log($"✓ Tick sound played at {elapsedTime:F2}s - Score: {currentIntScore}");
                }
                else
                {
                    Debug.LogWarning("Count tick sound or audio source is null!");
                }
                
                nextTickTime += tickInterval;
            }

            // Update UI with animated values
            gameScoreText.text = currentIntScore.ToString("N0");
            timePlayedText.text = FormatTime(currentTime);
            coinsEarnedText.text = Mathf.FloorToInt(currentCoins).ToString("N0");

            yield return null;
        }

        // Set final values at the end
        gameScoreText.text = finalScore.ToString("N0");
        timePlayedText.text = FormatTime(finalTimePlayed);
        coinsEarnedText.text = finalCoins.ToString("N0");

        // Play completion sound (wait a moment for last tick to finish)
        yield return new WaitForSecondsRealtime(0.1f);
        
        if (countCompleteSound != null && countAudioSource != null)
        {
            // Stop any ongoing tick sounds
            if (countAudioSource.isPlaying)
            {
                countAudioSource.Stop();
            }
            
            countAudioSource.PlayOneShot(countCompleteSound, 0.7f); // 70% volume
            Debug.Log("✓ Completion sound played");
        }
        else
        {
            Debug.LogWarning("Count complete sound or audio source is null!");
        }

        // Mark animation as complete
        isCountingAnimationComplete = true;
        
        Debug.Log("Counting animation complete!");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void AddCoinsToDatabase()
    {
        if (coinsAddedToDatabase || GameDataManager.Instance == null) return;

        GameDataManager.Instance.CurrentGameData.nutriCoins += calculatedCoinsEarned;
        GameDataManager.Instance.SaveGameData();
        coinsAddedToDatabase = true;

        Debug.Log($"Added {calculatedCoinsEarned} coins to database");
    }

    // ========== KEY COLLECTION METHODS WITH EVENT TRIGGER ==========
    
    // Save key to database when Continue button is clicked
    private void SaveKeyToDatabase()
    {
        if (keySavedToDatabase || GameDataManager.Instance == null) return;
        
        if (keyWasCollected)
        {
            // Save to GameData
            GameDataManager.Instance.CurrentGameData.CollectAllerthiaKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
            Debug.Log("AllerthiaKey saved to GameData from Continue button");
            
            // 🔥 TRIGGER THE KEY COLLECTION EVENT - THIS UPDATES THE GLOBAL MAP
            KeyCollectionEvents.TriggerKeyCollected("Allerthia");
            Debug.Log("🔥 Key Collection Event Triggered: Allerthia");
        }
    }

    public void OnRetryButtonClicked()
    {
        if (!isSummaryActive || !isGameOver || isProcessingConfirm || !isCountingAnimationComplete) 
        {
            Debug.Log("Confirm button blocked - counting animation not complete");
            return;
        }
        
        isProcessingConfirm = true;

        PlayButtonClickSound();
        AddCoinsToDatabase();

        // Save key if it was collected
        if (keyWasCollected && !keySavedToDatabase)
        {
            SaveKeyToDatabase();
        }

        if (restartButton != null)
            restartButton.interactable = false;

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

    public void OnHomeButtonClicked()
    {
        if (!isSummaryActive || !isGameOver || isProcessingConfirm || !isCountingAnimationComplete) 
        {
            Debug.Log("Home button blocked - counting animation not complete");
            return;
        }
        
        // Don't proceed if on lose screen (home button disabled)
        if (!isVictory)
        {
            Debug.Log("Home button is disabled on lose screen");
            return;
        }
        
        isProcessingConfirm = true;

        PlayButtonClickSound();
        AddCoinsToDatabase();

        if (homeButton != null)
            homeButton.interactable = false;

        // BOTH key collected this session AND key already in database go to spawn point
        if (keyWasCollected || keySavedToDatabase)
        {
            Debug.Log($"Key state - Collected this session: {keyWasCollected}, Saved to database: {keySavedToDatabase}");
            
            if (keyWasCollected && !keySavedToDatabase)
            {
                // Key collected this session AND not saved yet - show animation
                Debug.Log("Key collected this session - showing KeyUnlockedAnimation");
                StartCoroutine(ReturnToSpawnPointAndShowAnimation());
            }
            else
            {
                // Key already in database - just return to spawn point without animation
                Debug.Log("Key already in database - returning to spawn point without animation");
                StartCoroutine(ReturnToSpawnPointOnly());
            }
        }
        else
        {
            // No key at all - return to spawn point with input enabled
            Debug.Log("No key - returning to spawn point");
            StartCoroutine(ReturnToSpawnPointOnly());
        }
    }

    // Return to spawn point with input enabled (for no key or key already in database)
    private IEnumerator ReturnToSpawnPointOnly()
    {
        Debug.Log("Returning to spawn point");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);
        
        // Stop character animation
        StopCharacterAnimationDuringPause();
        
        // Restore background music
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Switch back to player camera
        SwitchToPlayerCameraWithBlend();
        
        // Enable player input
        EnablePlayerInput();
        
        // Reset game state
        ResetGameState();
        
        // Reset game over flags
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
        
        // Finish up
        FinishHomeButtonSequence();
        
        yield return null;
    }

    // Return to spawn point AND show key animation (for newly collected key)
    private IEnumerator ReturnToSpawnPointAndShowAnimation()
    {
        Debug.Log("Returning to spawn point and showing key animation");
        
        // Fade out summary panel
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);
        
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);
        
        // Stop character animation
        StopCharacterAnimationDuringPause();
        
        // Restore background music
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Switch back to player camera
        SwitchToPlayerCameraWithBlend();
        
        // Enable player input
        EnablePlayerInput();
        
        // Reset game state
        ResetGameState();
        
        // Reset game over flags
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
        
        Debug.Log($"Player at spawn position, input enabled: {playerObject.transform.position}");
        
        // Small delay before showing animation
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Now show KeyUnlockedAnimation
        if (keyUnlockedController != null)
        {
            Debug.Log("Showing KeyUnlockedAnimation via controller");
            keyUnlockedController.ShowKeyUnlockedCanvas(OnKeyAnimationContinue);
        }
        else if (keyUnlockedAnimation != null)
        {
            Debug.LogWarning("KeyUnlockedController not found, activating GameObject directly");
            keyUnlockedAnimation.SetActive(true);
        }
        else
        {
            Debug.LogError("KeyUnlockedAnimation GameObject is not assigned!");
            FinishHomeButtonSequence();
        }
    }

    // Callback for when Continue button in key animation is clicked
    private void OnKeyAnimationContinue()
    {
        Debug.Log("Key animation continue callback received");
        
        // Save the key to database
        SaveKeyToDatabase();
        
        // Finish the home button sequence
        FinishHomeButtonSequence();
    }

    // Handle ContinueKeyButton click
    public void OnContinueKeyButtonClicked()
    {
        Debug.Log("ContinueKeyButton clicked directly");
        
        // Save the key to database
        SaveKeyToDatabase();
        
        // Hide KeyUnlockedAnimation
        if (keyUnlockedController != null && keyUnlockedController.IsShowing())
        {
            // Controller will handle hiding
        }
        else if (keyUnlockedAnimation != null)
        {
            keyUnlockedAnimation.SetActive(false);
        }
        
        // Finish the sequence
        FinishHomeButtonSequence();
    }

    // Common cleanup for home button sequence
    private void FinishHomeButtonSequence()
    {
        // Reset flags
        isProcessingConfirm = false;
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
        
        if (homeButton != null)
            homeButton.interactable = true;
            
        if (restartButton != null)
            restartButton.interactable = true;
        
        Debug.Log("Home button sequence complete");
    }

    private IEnumerator HidePanelAndRestartGame()
    {
        if (panelCanvasGroup != null)
            yield return FadePanel(1f, 0f, fadeOutDuration);

        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);

        StopCharacterAnimationDuringPause();
        RestoreBackgroundMusicVolume();
        Time.timeScale = originalTimeScale;

        RestartGame();
        
        isProcessingConfirm = false;

        if (restartButton != null)
            restartButton.interactable = true;
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
        if (playerHealth != null) playerHealth.ResetHealth();
        if (scoringSystem != null) scoringSystem.ResetSessionStats();
        if (gameplayProgression != null) InvokeMethodIfExists(gameplayProgression, "ResetTimer");

        ResetAllMonsters();
        ResetKeySystem();

        Debug.Log("K3 Game state reset");
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
        K3_CollectKey[] allKeyScripts = FindObjectsOfType<K3_CollectKey>();
        foreach (K3_CollectKey keyScript in allKeyScripts)
        {
            if (keyScript != null)
            {
                InvokeMethodIfExists(keyScript, "ResetKey");
                InvokeMethodIfExists(keyScript, "ForceFullReset");
            }
        }

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
        
        // Reset key collection flags
        keyWasCollected = false;
        keySavedToDatabase = false;
        
        ResetStarAnimator();
        
        isCountingAnimationComplete = false;
        if (countAnimationCoroutine != null)
        {
            StopCoroutine(countAnimationCoroutine);
            countAnimationCoroutine = null;
        }

        StopCharacterAnimationDuringPause();

        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
            Debug.Log("KeyImageunlocking hidden during manager reset");
        }
        
        if (keyUnlockedAnimation != null && keyUnlockedAnimation.activeSelf)
        {
            keyUnlockedAnimation.SetActive(false);
            Debug.Log("KeyUnlockedAnimation hidden during manager reset");
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
            keyWasCollected = true;
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
        
        yield return TriggerCharacterAnimationDuringPause();
        PlayResultSound();
        
        CalculateCoinReward();
        UpdateSummaryData();
        
        ShowPanelWithAnimation();
        
        Debug.Log($"K3 Summary panel shown directly");
        
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        if (countAnimationCoroutine != null)
            StopCoroutine(countAnimationCoroutine);
        
        countAnimationCoroutine = StartCoroutine(AnimateCountingNumbers());
    }

    public bool IsSummaryActive()
    {
        return isSummaryActive;
    }

    public bool HasAllerthiaKey()
    {
        return GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData.HasAllerthiaKey();
    }

    public void ResetAllerthiaKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.ResetAllerthiaKey();
            GameDataManager.Instance.SaveGameData();
            Debug.Log("AllerthiaKey reset in GameData");
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

    public void TriggerQA2CompletionSummary()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("K3: Triggering summary from QA/Assessment completion");
            isVictory = true;
            summaryTriggeredByKeyCollection = false;
            StartCoroutine(ShowSummaryPanel());
        }
    }
    
    public void TriggerAssessmentCompletionSummary()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("K3: Triggering summary from assessment completion (no key)");
            isVictory = true;
            summaryTriggeredByKeyCollection = false;
            StartCoroutine(ShowSummaryPanel());
        }
    }

    [ContextMenu("Test Win with Key")]
    public void TestWinWithKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            keyWasCollected = true;
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
            keyWasCollected = false;
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
            keyWasCollected = false;
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
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        if (countAnimationCoroutine != null)
            StopCoroutine(countAnimationCoroutine);
        
        countAnimationCoroutine = StartCoroutine(AnimateCountingNumbers());
        
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

    [ContextMenu("Debug Animator Parameters")]
    public void DebugAnimatorParameters()
    {
        if (characterAnimator == null)
        {
            Debug.LogError("CharacterAnimator is null!");
            return;
        }
        
        Debug.Log("=== CURRENT ANIMATOR PARAMETERS ===");
        Debug.Log($"isVictory: {isVictory}");
        Debug.Log($"isSummaryActive: {isSummaryActive}");
        
        foreach (AnimatorControllerParameter param in characterAnimator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Bool)
            {
                Debug.Log($"{param.name}: {characterAnimator.GetBool(param.name)}");
            }
        }
        
        if (characterVisualSwapper != null)
        {
            Debug.Log($"CharacterVisualSwapper enabled: {characterVisualSwapper.enabled}");
        }
    }

    void OnDestroy()
    {
        if (isGameOver)
            Time.timeScale = originalTimeScale;

        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = originalBackgroundMusicVolume;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRetryButtonClicked);
        
        if (homeButton != null)
            homeButton.onClick.RemoveListener(OnHomeButtonClicked);
        
        if (continueKeyButton != null)
            continueKeyButton.onClick.RemoveListener(OnContinueKeyButtonClicked);
        
        if (KeyImageunlocking != null && KeyImageunlocking.activeSelf)
        {
            KeyImageunlocking.SetActive(false);
        }
        
        if (keyUnlockedAnimation != null && keyUnlockedAnimation.activeSelf)
        {
            keyUnlockedAnimation.SetActive(false);
        }
    }
}