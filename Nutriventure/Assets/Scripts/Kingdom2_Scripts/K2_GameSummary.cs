using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using Cinemachine;
using UnityEngine.SceneManagement;

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

    [Header("Counting Animation System")]
    [SerializeField] private float countAnimationDuration = 2f;
    [SerializeField] private AudioClip countTickSound;
    [SerializeField] private AudioClip countCompleteSound;
    [SerializeField] private AudioSource countAudioSource;
    private Coroutine countAnimationCoroutine;
    private bool isCountingAnimationComplete = false;

    [Header("Character Win/Lose Animation")]
    public Animator characterAnimator;
    public string danceParameter = "isDance";
    public string thinkParameter = "isThinking";
    private bool isCharacterVisualSwapperEnabledBeforeSummary = true;

    [Header("Key Image Display")]
    public GameObject KeyImageunlocking;
    
    [Header("Key Unlocked Animation")]
    public GameObject keyUnlockedAnimation; // The KeyUnlockedAnimation GameObject
    public Button continueKeyButton; // The ContinueKeyBTN inside the animation
    public KeyUnlockedCanvasController keyUnlockedController; // Reference to the controller

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
    public GameObject joystickCanvas;
    public GameObject qa1Panel;
    public GameObject qa2Panel;

    [Header("QA2 Completion Settings")]
    public bool showSummaryOnQA2Completion = true;
    [Range(1, 5)] public int requiredQA2CorrectAnswers = 5;

    [Header("Timeline Settings")]
    public GameObject timelineController;
    public string timelineObjectName = "K2_QueenACS2";

    [Header("Complete Restart Settings")]
    public bool completeRestartOnConfirm = true;
    public string sceneToReload = "";

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
    private K2_CollectKey collectKeyScript;

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

    // Key tracking
    private bool summaryTriggeredByKeyCollection = false;
    
    // Key Collection State
    private bool keyWasCollected = false; // Whether the key was collected in this session
    private bool keySavedToDatabase = false; // Whether we've already saved the key to GameData
    
    // Counting animation values
    private float currentTimePlayed = 0f;
    private int currentGameScore = 0;
    private int targetCoinsEarned = 0;
    private float elapsedAnimationTime = 0f;

    // Store original positions for reset
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

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
        
        // Debug input for testing counting animation
        #if UNITY_EDITOR
        //if (Input.GetKeyDown(KeyCode.F8))
        //{
        //    TestCountingAnimation();
        //}
        #endif
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
        collectKeyScript = FindObjectOfType<K2_CollectKey>();

        if (characterVisualSwapper == null)
            characterVisualSwapper = FindObjectOfType<CharacterVisualSwapper>();

        if (playerAnimator == null && playerObject != null)
            playerAnimator = playerObject.GetComponentInChildren<Animator>();

        // Set character animator if not assigned
        if (characterAnimator == null && playerAnimator != null)
        {
            characterAnimator = playerAnimator;
        }

        if (backgroundMusicSource == null)
            backgroundMusicSource = FindBackgroundMusicSource();

        if (audioSource == null)
            CreateAudioSource();
            
        // Setup counting audio source if not assigned
        if (countAudioSource == null)
        {
            countAudioSource = gameObject.AddComponent<AudioSource>();
            countAudioSource.playOnAwake = false;
            countAudioSource.spatialBlend = 0f;
            Debug.Log("Created counting audio source");
        }

        if (playerObject == null)
            playerObject = GameObject.Find("PlayerArmature");

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
            restartButton.onClick.AddListener(OnRestartButtonClicked);

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

        CheckAndDisableTimelineOnStart();

        Debug.Log($"GameSummary initialized - Complete Restart: {completeRestartOnConfirm}");
    }

    private void CheckAndDisableTimelineOnStart()
    {
        bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
        
        if (keyAlreadyInDatabase && !string.IsNullOrEmpty(timelineObjectName))
        {
            DisableTimelineIfExists();
            Debug.Log("Timeline disabled on start (key already in database)");
        }
    }

    #endregion

    #region Game Condition Checks

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

        if (showSummaryOnQA2Completion && !isGameOver && !isSummaryActive && !waitingForLastQA2Panel && qa2System != null && IsQA2Completed())
        {
            int currentHealth = playerHealth != null ? playerHealth.currentHealth : 0;
            
            // Check if key was collected in this session OR already in database
            bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                    GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
            
            bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
            
            Debug.Log($"QA2 Completed - Health: {currentHealth}, Key Collected This Session: {keyCollectedThisSession}, Key In Database: {keyAlreadyInDatabase}");
            
            if (currentHealth <= 0)
            {
                Debug.Log("QA2 completed but health is 0. This should be a lose.");
                isVictory = false;
                StartCoroutine(ShowSummaryPanel());
            }
            else if (currentHealth <= 2)
            {
                Debug.Log("QA2 completed with 1-2 hearts. Victory but NO KEY.");
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
            else if (currentHealth >= 3)
            {
                if (keyCollectedThisSession || keyAlreadyInDatabase)
                {
                    Debug.Log("QA2 completed AND key collected. Triggering victory summary.");
                    isVictory = true;
                    if (keyCollectedThisSession)
                    {
                        summaryTriggeredByKeyCollection = true;
                        keyWasCollected = true;
                    }
                    StartCoroutine(ShowSummaryPanel());
                }
                else
                {
                    Debug.Log("QA2 completed, key not collected yet. Timeline should trigger.");
                }
            }
        }

        CheckTimelineConditions();
    }

    private void CheckTimelineConditions()
    {
        if (playerHealth == null || isSummaryActive || isGameOver) return;

        int currentHealth = playerHealth.currentHealth;
        
        // Check if key was collected in this session OR already in database
        bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
        
        bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
        
        Debug.Log($"Health: {currentHealth}, Key Collected This Session: {keyCollectedThisSession}, Key In Database: {keyAlreadyInDatabase}");
        
        if (currentHealth <= 0)
        {
            Debug.Log($"Health ({currentHealth}) = 0. Triggering LOSE summary with 0 stars...");
            healthBeforeDeath = currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
            return;
        }
        
        if (currentHealth <= 2 && currentHealth > 0)
        {
            Debug.Log($"Health ({currentHealth}) = 1-2. Checking QA2 completion...");
            
            bool qa2Completed = qa2System != null && qa2System.GetCorrectlyAnsweredCount() >= requiredQA2CorrectAnswers;
            
            if (qa2Completed && !isGameOver && !isSummaryActive)
            {
                Debug.Log($"QA2 completed with {currentHealth} hearts. Victory but NO KEY.");
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
            else if (!qa2Completed)
            {
                Debug.Log($"Health 1-2 but QA2 not completed ({qa2System?.GetCorrectlyAnsweredCount() ?? 0}/{requiredQA2CorrectAnswers}). Player can continue playing.");
            }
        }
        else if (currentHealth >= 3)
        {
            Debug.Log($"Health ({currentHealth}) ≥ 3, checking timeline conditions...");
            
            bool qa2Completed = qa2System != null && qa2System.GetCorrectlyAnsweredCount() >= requiredQA2CorrectAnswers;
            
            if (!keyCollectedThisSession && !keyAlreadyInDatabase)
            {
                if (collectKeyScript != null && collectKeyScript.HasTriggeredSummary())
                {
                    Debug.Log("Key collection already triggered summary. Skipping timeline.");
                }
                else if (qa2Completed)
                {
                    Debug.Log("QA2 completed and key not collected. Timeline should play for key.");
                    TryActivateTimeline();
                }
                else
                {
                    Debug.Log("Key not collected and QA2 not completed. No timeline yet.");
                }
            }
            else
            {
                Debug.Log("Key already collected (session or database). Timeline will not play.");
                DisableTimelineIfExists();
                
                if (qa2Completed && !isGameOver && !isSummaryActive)
                {
                    Debug.Log("Key already collected AND QA2 completed. Triggering VICTORY summary.");
                    isVictory = true;
                    if (keyCollectedThisSession)
                    {
                        summaryTriggeredByKeyCollection = true;
                        keyWasCollected = true;
                    }
                    StartCoroutine(ShowSummaryPanel());
                }
            }
        }
    }

    private void DisableTimelineIfExists()
    {
        if (string.IsNullOrEmpty(timelineObjectName)) return;
        
        GameObject timelineObj = GameObject.Find(timelineObjectName);
        if (timelineObj != null && timelineObj.activeInHierarchy)
        {
            timelineObj.SetActive(false);
            Debug.Log($"Disabled timeline (key already collected): {timelineObjectName}");
            
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
        
        bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
        
        bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
        
        if (keyAlreadyInDatabase || keyCollectedThisSession)
        {
            Debug.Log("Key already collected. Timeline will not play.");
            DisableTimelineIfExists();
            return;
        }
        
        GameObject timelineObj = GameObject.Find(timelineObjectName);
        if (timelineObj != null)
        {
            if (!timelineObj.activeInHierarchy)
            {
                Debug.Log($"Activating timeline: {timelineObjectName}");
                timelineObj.SetActive(true);
                
                K2_QueenACS2 queenCutscene = timelineObj.GetComponent<K2_QueenACS2>();
                if (queenCutscene != null && !queenCutscene.enabled)
                {
                    queenCutscene.enabled = true;
                    Debug.Log("Enabled K2_QueenACS2 component for timeline");
                }
                
                if (timelineController != null)
                {
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
                bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                        GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
                
                bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
                
                if (keyCollectedThisSession || keyAlreadyInDatabase)
                {
                    Debug.Log("After QA2 panel closed: Key collected, triggering victory summary.");
                    isVictory = true;
                    if (keyCollectedThisSession)
                    {
                        summaryTriggeredByKeyCollection = true;
                        keyWasCollected = true;
                    }
                    StartCoroutine(ShowSummaryPanel());
                }
                else
                {
                    Debug.Log("After QA2 panel closed: Key not collected yet. Waiting for timeline.");
                }
            }
        }
    }

    #endregion

    #region Summary Panel with Counting Animation

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
        isCountingAnimationComplete = false;

        Debug.Log($"Starting ShowSummaryPanel() - Victory: {isVictory}, Key Collected: {keyWasCollected}");

        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        PrepareGameForSummary();
        yield return null;

        yield return TriggerCharacterAnimationDuringPause();
        PlayResultSound();

        CalculateCoinReward();
        UpdateSummaryData();

        ShowPanelWithAnimation();

        Debug.Log($"Game {(isVictory ? "won" : "lost")} - Summary panel shown");
        
        // Start the counting animation sequence
        yield return StartCoroutine(GameEndSequence());
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
        CloseAllQAPanels();
        LowerBackgroundMusicVolume();
        SwitchToSummaryCameraImmediate();
        HideJoystickCanvas();
        
        // Reset UI text to 0 for counting animation
        ResetUITextForCounting();
    }

    private void ResetUITextForCounting()
    {
        // Set all text fields to 0 for counting animation
        if (timePlayedText != null) timePlayedText.text = "00:00";
        if (gameScoreText != null) gameScoreText.text = "0";
        if (coinsEarnedText != null) coinsEarnedText.text = "0";
        if (starsEarnedText != null) starsEarnedText.text = "0/3";
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
            
        // NEW: Disable home button on lose
        if (!isVictory && homeButton != null)
        {
            homeButton.interactable = false;
            Debug.Log("Home button disabled on lose");
        }
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

    #region Character Animation System

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

    #endregion

    #region Counting Animation System

    private IEnumerator GameEndSequence()
    {
        // Wait for panel to fade in
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Play star animation first
        yield return StartCoroutine(PlayStarAnimationWithDelay());
        
        // Then start counting animation
        yield return StartCoroutine(AnimateCountingNumbers());
        
        // Mark counting as complete
        isCountingAnimationComplete = true;
        
        Debug.Log("Counting animation complete - buttons enabled");
    }

    private IEnumerator PlayStarAnimationWithDelay()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        PlayStarAnimationDirect();
        yield return new WaitForSecondsRealtime(1f); // Wait for star animation to complete
    }

    private IEnumerator AnimateCountingNumbers()
    {
        Debug.Log("Starting counting animation...");
        
        if (timePlayedText == null || gameScoreText == null || coinsEarnedText == null || starsEarnedText == null)
        {
            Debug.LogError("Text components not assigned!");
            Debug.Log($"timePlayedText: {timePlayedText != null}");
            Debug.Log($"gameScoreText: {gameScoreText != null}");
            Debug.Log($"coinsEarnedText: {coinsEarnedText != null}");
            Debug.Log($"starsEarnedText: {starsEarnedText != null}");
            yield break;
        }

        // Get target values
        float targetTimePlayed = gameplayProgression != null ? gameplayProgression.GetCurrentTime() : 0f;
        int targetGameScore = scoringSystem != null ? scoringSystem.GetCurrentScore() : 0;
        
        // Store these for the animation
        currentTimePlayed = targetTimePlayed;
        currentGameScore = targetGameScore;
        targetCoinsEarned = calculatedCoinsEarned;

        // Reset animation
        elapsedAnimationTime = 0f;
        int lastIntegerValue = 0;

        // Start with all zeros
        timePlayedText.text = "00:00";
        gameScoreText.text = "0";
        coinsEarnedText.text = "0";
        starsEarnedText.text = "0/3";

        yield return new WaitForSecondsRealtime(0.3f);

        // Calculate how many ticks we want
        int numberOfTicks = Mathf.Clamp(targetGameScore / 50, 10, 30);
        float tickInterval = countAnimationDuration / numberOfTicks;
        float nextTickTime = 0f;
        
        Debug.Log($"Audio: Will play {numberOfTicks} ticks every {tickInterval:F2} seconds");

        // Animate all values simultaneously
        while (elapsedAnimationTime < countAnimationDuration)
        {
            elapsedAnimationTime += Time.unscaledDeltaTime;
            float progress = elapsedAnimationTime / countAnimationDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Animate time
            float currentTime = Mathf.Lerp(0, targetTimePlayed, smoothProgress);
            timePlayedText.text = FormatTime(currentTime);

            // Animate score
            float currentScore = Mathf.Lerp(0, targetGameScore, smoothProgress);
            int currentInteger = Mathf.FloorToInt(currentScore);
            
            // Play tick sound at regular intervals
            if (elapsedAnimationTime >= nextTickTime)
            {
                if (countTickSound != null && countAudioSource != null)
                {
                    countAudioSource.Stop(); // Stop any previous sound
                    countAudioSource.PlayOneShot(countTickSound, 0.5f);
                    Debug.Log($"✓ Tick sound played at {elapsedAnimationTime:F2}s - Score: {currentInteger}");
                }
                else
                {
                    Debug.LogWarning("Count tick sound or audio source is null!");
                }
                
                nextTickTime += tickInterval;
            }
            
            gameScoreText.text = Mathf.FloorToInt(currentScore).ToString("N0");

            // Animate coins
            float currentCoins = Mathf.Lerp(0, targetCoinsEarned, smoothProgress);
            coinsEarnedText.text = Mathf.FloorToInt(currentCoins).ToString("N0");

            // Animate stars (as text)
            int currentStarsText = Mathf.FloorToInt(Mathf.Lerp(0, currentStars, smoothProgress));
            starsEarnedText.text = $"{currentStarsText}/3";

            yield return null;
        }

        // Set final values
        timePlayedText.text = FormatTime(targetTimePlayed);
        gameScoreText.text = targetGameScore.ToString("N0");
        coinsEarnedText.text = targetCoinsEarned.ToString("N0");
        starsEarnedText.text = $"{currentStars}/3";

        // Wait a moment for last tick to finish
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Play completion sound
        if (countCompleteSound != null && countAudioSource != null)
        {
            // Stop any ongoing tick sounds
            if (countAudioSource.isPlaying)
            {
                countAudioSource.Stop();
            }
            
            countAudioSource.PlayOneShot(countCompleteSound, 0.7f);
            Debug.Log("✓ Completion sound played");
        }
        else
        {
            Debug.LogWarning("Count complete sound or audio source is null!");
        }

        Debug.Log("Counting animation complete!");
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
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
                 obj.name.Contains("Timeline") || obj.name == timelineObjectName))
            {
                obj.SetActive(false);
                Debug.Log($"Closed interfering UI: {obj.name}");
            }
        }
    }

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
            Debug.Log($"Moved player to spawn point: {playerSpawnPoint.position}");
        }

        CharacterController charController = playerObject.GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
            charController.enabled = true;
        }
    }

    private void MovePlayerBackToOriginalPosition()
    {
        if (playerObject == null) return;
        
        Debug.Log($"Moving player back to original position: {originalPlayerPosition}");
        
        playerObject.transform.position = originalPlayerPosition;
        playerObject.transform.rotation = originalPlayerRotation;
        
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
        
        Debug.Log($"=== UPDATE SUMMARY DATA ===");
        Debug.Log($"Current stars calculated: {currentStars}");
        Debug.Log($"Stars earned text will show: {currentStars}/3");
        Debug.Log($"Summary triggered by key collection: {summaryTriggeredByKeyCollection}");
        Debug.Log($"Key was collected this session: {keyWasCollected}");
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

    // Save key to database when Continue button is clicked
    private void SaveKeyToDatabase()
    {
        if (keySavedToDatabase || GameDataManager.Instance == null) return;
        
        if (keyWasCollected)
        {
            GameDataManager.Instance.CurrentGameData.CollectPreserviaKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
            Debug.Log("PreserviaKey saved to GameData from Continue button");
        }
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

    public void OnRestartButtonClicked()
    {
        if (!isSummaryActive || !isGameOver || isProcessingConfirm) return;
        
        // Check if counting animation is complete
        if (!isCountingAnimationComplete)
        {
            Debug.Log("Cannot confirm - counting animation still in progress!");
            return;
        }
        
        isProcessingConfirm = true;

        PlayButtonClickSound();
        AddCoinsToDatabase();

        // Save key if it was collected (only if we're doing a restart that saves)
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
        if (!isSummaryActive || !isGameOver || isProcessingConfirm) return;
        
        // Check if counting animation is complete
        if (!isCountingAnimationComplete)
        {
            Debug.Log("Cannot confirm - counting animation still in progress!");
            return;
        }
        
        // NEW: Don't proceed if on lose screen (home button disabled)
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

        // BOTH key collected this session AND key already in database go to pre-summary state
        if (keyWasCollected || keySavedToDatabase)
        {
            // Key was collected this session OR already in database
            // Both go to pre-summary state
            Debug.Log($"Key state - Collected this session: {keyWasCollected}, Saved to database: {keySavedToDatabase}");
            
            if (keyWasCollected && !keySavedToDatabase)
            {
                // Key collected this session AND not saved yet - show animation
                Debug.Log("Key collected this session - showing KeyUnlockedAnimation");
                StartCoroutine(ReturnToPreSummaryStateAndShowAnimation());
            }
            else
            {
                // Key already in database - just return to pre-summary state without animation
                Debug.Log("Key already in database - returning to pre-summary state without animation");
                StartCoroutine(ReturnToPreSummaryStateOnly());
            }
        }
        else
        {
            // No key at all - return to original game state
            Debug.Log("No key - returning to game fully");
            StartCoroutine(ReturnToGameFully());
        }
    }

    // Return to pre-summary state AND show key animation
    private IEnumerator ReturnToPreSummaryStateAndShowAnimation()
    {
        Debug.Log("Returning to pre-summary state and showing key animation");
        
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
        
        // Respawn products
        RespawnAllProducts();
        
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

    // Return to pre-summary state ONLY (no animation)
    private IEnumerator ReturnToPreSummaryStateOnly()
    {
        Debug.Log("Returning to pre-summary state only (no animation)");
        
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
        
        // Respawn products
        RespawnAllProducts();
        
        // Reset game over flags
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
        
        Debug.Log($"Player at spawn position, input enabled: {playerObject.transform.position}");
        
        // Finish the sequence without showing animation
        FinishHomeButtonSequence();
        
        yield return null;
    }

    // Fully return to game (for no key)
    private IEnumerator ReturnToGameFully()
    {
        Debug.Log("Returning to game fully");
        
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
        
        // REMOVE THIS LINE - player stays at spawn point
        // MovePlayerBackToOriginalPosition();
        
        // Switch back to player camera
        SwitchToPlayerCameraWithBlend();
        
        // Enable player input
        EnablePlayerInput();
        
        // Reset game state
        ResetGameState();
        
        // Respawn products
        RespawnAllProducts();
        
        // Reset game over flags
        isGameOver = false;
        isSummaryActive = false;
        summaryLocked = false;
        
        // Finish up
        FinishHomeButtonSequence();
        
        yield return null;
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
        
        // If key was collected and saved, we keep keyWasCollected true but keySavedToDatabase will be true
        // They'll be fully reset when the game restarts properly
        
        Debug.Log("Home button sequence complete");
    }

    // Handle ContinueKeyButton click (direct button reference, separate from controller callback)
    public void OnContinueKeyButtonClicked()
    {
        Debug.Log("ContinueKeyButton clicked directly");
        
        // Save the key to database
        SaveKeyToDatabase();
        
        // Hide KeyUnlockedAnimation
        if (keyUnlockedController != null && keyUnlockedController.IsShowing())
        {
            // Controller will handle hiding through its own method
            // We just need to wait for it
        }
        else if (keyUnlockedAnimation != null)
        {
            keyUnlockedAnimation.SetActive(false);
        }
        
        // Finish the sequence
        FinishHomeButtonSequence();
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
        if (playerHealth != null) playerHealth.ResetHealth();
        if (scoringSystem != null) scoringSystem.ResetSessionStats();
        if (productManager != null) productManager.ResetForNewSession();
        if (gameplayProgression != null) InvokeMethodIfExists(gameplayProgression, "ResetTimer");
        if (qa2System != null) InvokeMethodIfExists(qa2System, "ClearScannedProducts");

        ResetAllMonsters();
        ResetKeySystem();

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
        K2_CollectKey[] allKeyScripts = FindObjectsOfType<K2_CollectKey>();
        foreach (K2_CollectKey keyScript in allKeyScripts)
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

    private void RespawnAllProducts()
    {
        if (productSpawner != null)
        {
            Debug.Log("Calling ProductSpawner to respawn products...");

            System.Reflection.MethodInfo respawnMethod = productSpawner.GetType().GetMethod("RespawnProducts");
            if (respawnMethod != null)
            {
                respawnMethod.Invoke(productSpawner, null);
                Debug.Log("Called RespawnProducts() on ProductSpawner");
            }
            else
            {
                System.Reflection.MethodInfo spawnMethod = productSpawner.GetType().GetMethod("SpawnProducts");
                if (spawnMethod != null)
                {
                    spawnMethod.Invoke(productSpawner, null);
                    Debug.Log("Called SpawnProducts() on ProductSpawner");
                }
                else
                {
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
        isCountingAnimationComplete = false;
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        currentStars = 0;
        summaryTriggeredByKeyCollection = false;
        
        // Reset key collection flags
        keyWasCollected = false;
        keySavedToDatabase = false;
        
        ResetStarAnimator();
        
        // Reset counting animation coroutine
        if (countAnimationCoroutine != null)
        {
            StopCoroutine(countAnimationCoroutine);
            countAnimationCoroutine = null;
        }

        // Reset character animation
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

        Debug.Log($"GameSummaryManager reset - keyWasCollected: {keyWasCollected}, keySavedToDatabase: {keySavedToDatabase}");
    }

    #endregion

    #region Complete Scene Reload

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
        
        K2_CollectKey.GlobalResetAllKeys();
        
        Debug.Log("Persistent data reset complete");
    }

    #endregion

    #region Public Methods

    public void TriggerQA2CompletionSummary()
    {
        if (!isGameOver && !isSummaryActive && showSummaryOnQA2Completion)
        {
            bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                    GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
            
            bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
            
            if (keyCollectedThisSession || keyAlreadyInDatabase)
            {
                Debug.Log("Manual QA2 completion summary trigger - Key collected.");
                isVictory = true;
                if (keyCollectedThisSession)
                {
                    summaryTriggeredByKeyCollection = true;
                    keyWasCollected = true;
                }
                StartCoroutine(ShowSummaryPanel());
            }
            else
            {
                Debug.Log("Manual QA2 completion summary trigger - Key not collected yet. Waiting for timeline.");
            }
        }
    }

    public void TriggerSummaryFromQA2()
    {
        if (!isGameOver && !isSummaryActive && showSummaryOnQA2Completion)
        {
            bool keyAlreadyInDatabase = GameDataManager.Instance != null && 
                                    GameDataManager.Instance.CurrentGameData.HasPreserviaKey();
            
            bool keyCollectedThisSession = collectKeyScript != null && collectKeyScript.HasKey();
            
            if (keyCollectedThisSession || keyAlreadyInDatabase)
            {
                Debug.Log("TriggerSummaryFromQA2 - Key collected, triggering victory.");
                isVictory = true;
                if (keyCollectedThisSession)
                {
                    summaryTriggeredByKeyCollection = true;
                    keyWasCollected = true;
                }
                StartCoroutine(ShowSummaryPanel());
            }
            else
            {
                Debug.Log("TriggerSummaryFromQA2 - Key not collected yet. Not triggering summary.");
            }
        }
    }

    public bool IsQA2SummaryEnabled() => showSummaryOnQA2Completion;
    public void SetQA2SummaryEnabled(bool enabled) => showSummaryOnQA2Completion = enabled;
    public void SetRequiredQA2Answers(int requiredAnswers) => requiredQA2CorrectAnswers = Mathf.Clamp(requiredAnswers, 1, 5);

    public void TriggerSummaryFromKey()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("TriggerSummaryFromKey called - marking summary as triggered by key collection");
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            keyWasCollected = true; // Mark that key was collected this session
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot trigger summary from key - already active or game over");
        }
    }

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
        summaryTriggeredByKeyCollection = false;
        keyWasCollected = false; // Reset this for direct shows
        
        Debug.Log($"Starting ShowSummaryPanelDirectly() - Victory: {isVictory}");
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        PrepareGameForSummary();
        yield return null;
        
        yield return TriggerCharacterAnimationDuringPause();
        PlayResultSound();
        
        CalculateCoinReward();
        UpdateSummaryData();
        
        ShowPanelWithAnimation();
        
        Debug.Log($"Summary panel shown directly");
        
        yield return new WaitForSecondsRealtime(0.5f);
        PlayStarAnimationDirect();
        
        yield return StartCoroutine(AnimateCountingNumbers());
        isCountingAnimationComplete = true;
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

    // Check if key was collected this session
    public bool WasKeyCollectedThisSession()
    {
        return keyWasCollected;
    }

    // Check if key is saved to database
    public bool IsKeySavedToDatabase()
    {
        return keySavedToDatabase;
    }

    #endregion

    #region Debug & Testing

    [ContextMenu("Test Counting Animation")]
    public void TestCountingAnimation()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log("Testing counting animation...");
            StartCoroutine(TestCountingAnimationCoroutine());
        }
    }

    private IEnumerator TestCountingAnimationCoroutine()
    {
        isGameOver = true;
        isSummaryActive = true;
        isVictory = true;
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Set some test values
        currentStars = 3;
        calculatedCoinsEarned = 1500;
        
        // Show panel
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(true);
        
        // Reset text to 0
        ResetUITextForCounting();
        
        yield return new WaitForSecondsRealtime(0.5f);
        
        // Play star animation
        PlayStarAnimationDirect();
        yield return new WaitForSecondsRealtime(1f);
        
        // Start counting animation
        yield return StartCoroutine(AnimateCountingNumbers());
        
        isCountingAnimationComplete = true;
        
        Debug.Log("Counting animation test complete!");
        
        // Clean up
        yield return new WaitForSecondsRealtime(2f);
        
        Time.timeScale = originalTimeScale;
        isGameOver = false;
        isSummaryActive = false;
        
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(false);
    }

    [ContextMenu("Test Key Collection Flow")]
    public void TestKeyCollectionFlow()
    {
        Debug.Log("=== TESTING KEY COLLECTION FLOW ===");
        keyWasCollected = true;
        keySavedToDatabase = false;
        Debug.Log($"Set keyWasCollected=true, keySavedToDatabase={keySavedToDatabase}");
    }

    [ContextMenu("Test Key Animation")]
    public void TestKeyAnimation()
    {
        if (!isGameOver && !isSummaryActive)
        {
            StartCoroutine(TestKeyAnimationCoroutine());
        }
    }

    private IEnumerator TestKeyAnimationCoroutine()
    {
        // Setup
        isGameOver = true;
        isSummaryActive = true;
        keyWasCollected = true;
        keySavedToDatabase = false;
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Show summary panel
        if (gameSummaryPanel != null)
            gameSummaryPanel.SetActive(true);
        
        // Fade in
        if (panelCanvasGroup != null)
            StartCoroutine(FadePanel(0f, 1f, fadeInDuration));
        
        yield return new WaitForSecondsRealtime(2f);
        
        // Simulate home button click
        OnHomeButtonClicked();
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

    [ContextMenu("Debug Star Animator")]
    public void DebugStarAnimator()
    {
        if (starAnimator == null)
        {
            Debug.LogError("Star animator is null!");
            return;
        }
        
        Debug.Log("=== STAR ANIMATOR DEBUG ===");
        
        Debug.Log($"GameObject: {starAnimator.gameObject.name}");
        Debug.Log($"GameObject active: {starAnimator.gameObject.activeSelf}");
        Debug.Log($"Animator enabled: {starAnimator.enabled}");
        Debug.Log($"Update mode: {starAnimator.updateMode}");
        
        Debug.Log($"Controller: {starAnimator.runtimeAnimatorController?.name}");
        
        Debug.Log($"Current '{starParameterName}' value: {starAnimator.GetInteger(starParameterName)}");
        
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

    [ContextMenu("Check PreserviaKey Status")]
    public void CheckPreserviaKeyStatus()
    {
        bool hasKey = HasPreserviaKey();
        Debug.Log($"PreserviaKey status: {(hasKey ? "COLLECTED" : "NOT COLLECTED")}");
        Debug.Log($"Key collected this session: {keyWasCollected}");
        Debug.Log($"Key saved to database: {keySavedToDatabase}");
    }

    [ContextMenu("Collect PreserviaKey (Test)")]
    public void TestCollectPreserviaKey()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.CollectPreserviaKey();
            GameDataManager.Instance.SaveGameData();
            keySavedToDatabase = true;
            Debug.Log("PreserviaKey collected and saved to GameData");
        }
    }

    [ContextMenu("Reset PreserviaKey (Test)")]
    public void TestResetPreserviaKey()
    {
        ResetPreserviaKey();
        keySavedToDatabase = false;
        keyWasCollected = false;
    }

    [ContextMenu("Test Complete Restart")]
    public void TestCompleteRestart()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            summaryTriggeredByKeyCollection = true;
            keyWasCollected = true;
            if (playerHealth != null) playerHealth.currentHealth = 6;
            StartCoroutine(ShowSummaryPanel());
            
            StartCoroutine(TestCompleteRestartCoroutine());
        }
    }

    private IEnumerator TestCompleteRestartCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        OnRestartButtonClicked();
    }

    #endregion

    void OnDestroy()
    {
        if (isGameOver)
            Time.timeScale = originalTimeScale;

        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = originalBackgroundMusicVolume;

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        
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