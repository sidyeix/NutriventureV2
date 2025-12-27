using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using Cinemachine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class K2_GameSummary : MonoBehaviour
{
    [Header("Game Summary Panel")]
    public GameObject gameSummaryPanel;
    public CanvasGroup panelCanvasGroup;
    
    [Header("Summary Text Fields")]
    public TextMeshProUGUI starsCountText;
    public TextMeshProUGUI timePlayedText;
    public TextMeshProUGUI productsCollectedText;
    public TextMeshProUGUI gameScoreText;
    public TextMeshProUGUI coinsEarnedText;
    public TextMeshProUGUI resultText; // "You Win!" or "You Lose!"
    public TextMeshProUGUI keyStatusText; // "KEY: UNLOCKED" or "KEY: LOCKED"
    
    [Header("Buttons")]
    public Button confirmButton;
    
    [Header("Panel Animation")]
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 0.5f;
    
    [Header("Audio Settings")]
    public AudioClip winSound;
    public AudioClip loseSound;
    public float soundVolume = 0.7f;
    public AudioSource backgroundMusicSource; // Direct reference to background music AudioSource
    public float backgroundMusicVolumeDuringSummary = 0.2f; // Lower volume during summary
    private float originalBackgroundMusicVolume = 1.0f;
    
    [Header("Key Status Colors")]
    public Color unlockedColor = Color.green;
    public Color lockedColor = Color.red;
    
    [Header("Coin Reward Settings")]
    public int coinsPerStar = 10;
    public int baseCoinsPerScore = 1; // Coins per 100 score points
    public float loseMultiplier = 0.5f; // Get 50% of normal coin reward when losing
    public float winMultiplier = 1.0f; // Get 100% of normal coin reward when winning
    
    [Header("Spawn Settings")]
    public Transform playerSpawnPoint; // Assign your spawn point here
    public ProductSpawner productSpawner; // Assign your ProductSpawner script here
    
    [Header("Camera References")]
    public CinemachineVirtualCamera summaryVirtualCamera; // Dedicated camera for summary
    public CinemachineVirtualCamera playerFollowCamera;
    private CinemachineBrain cinemachineBrain; // Reference to the CinemachineBrain
    
    [Header("Character Animation")]
    public CharacterVisualSwapper characterVisualSwapper; // Reference to CharacterVisualSwapper
    public string lookAroundParameter = "LookAround"; // Animation parameter name
    
    [Header("QA Panel References")]
    public GameObject qa1Panel; // Drag QA1 assessment panel here
    public GameObject qa2Panel; // Drag QA2 assessment panel here
    
    [Header("Timeline References")]
    public K2_DummypTimeline timelineManager; // Reference to the timeline manager
    public PlayableDirector cutscene2Timeline; // Reference to second cutscene timeline
    public PlayableDirector cutscene3Timeline; // Reference to third cutscene timeline (Queen timeline)
    public GameObject cutscene2ParentObject; // "Cutscene2Things" parent object
    public GameObject cutscene3ParentObject; // "Cutscene3" parent object (Queen timeline)
    
    [Header("QA2 Completion Settings")]
    [Tooltip("Enable summary panel when QA2 is completed (all 5 products answered correctly)")]
    public bool showSummaryOnQA2Completion = true;
    [Tooltip("Required correct answers in QA2 to trigger summary")]
    [Range(1, 5)] public int requiredQA2CorrectAnswers = 5;
    
    [Header("References - Auto Found")]
    private SugariaPlayerStat playerHealth;
    private GameplayProgression gameplayProgression;
    private ProductInformationManager productManager;
    private SugariaScoringSystem scoringSystem;
    private MainMenu_Manager mainMenuManager;
    private GameObject playerObject;
    private CollectProducts collectProductsScript;
    private K2_QA2system qa2System;
    private K2_QA1system qa1System; // Added reference to QA1 system
    private Animator playerAnimator; // Reference to player's animator
    
    // Store original timeline object positions
    private Dictionary<Transform, TransformData> originalTimelineObjectPositions = new Dictionary<Transform, TransformData>();
    
    private bool isGameOver = false;
    private bool isVictory = false;
    private bool waitingForLastQA2Panel = false;
    private float originalTimeScale;
    private int calculatedCoinsEarned = 0;
    private bool coinsAddedToDatabase = false;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;
    private AudioSource audioSource;
    private int healthBeforeDeath = 0; // Store health before death for star calculation
    
    // NEW: Flag to prevent multiple summary triggers
    private bool isSummaryActive = false;
    
    // Helper class to store transform data
    [System.Serializable]
    public class TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public bool isActive;
        
        public TransformData(Vector3 pos, Quaternion rot, Vector3 scl, bool active)
        {
            position = pos;
            rotation = rot;
            scale = scl;
            isActive = active;
        }
    }
    
    void Awake()
    {
        // Ensure only one instance exists
        var existingInstances = FindObjectsOfType<K2_GameSummary>();
        if (existingInstances.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Find all necessary references
        FindAllReferences();
        
        // Store player's original position and rotation
        StorePlayerOriginalTransform();
        
        // Store original timeline object positions
        StoreTimelineObjectPositions();
        
        // Hide panel at start
        if (gameSummaryPanel != null)
        {
            gameSummaryPanel.SetActive(false);
        }
        
        // Set up button listener
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        
        // Find CinemachineBrain on main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
            if (cinemachineBrain != null)
            {
                Debug.Log($"Found CinemachineBrain: {cinemachineBrain.gameObject.name}");
            }
        }
        
        // Store original background music volume
        if (backgroundMusicSource != null)
        {
            originalBackgroundMusicVolume = backgroundMusicSource.volume;
            Debug.Log($"Original background music volume: {originalBackgroundMusicVolume}");
        }
        
        Debug.Log($"GameSummaryManager initialized - QA2 Completion Summary: {showSummaryOnQA2Completion}");
    }
    
    void Update()
    {
        // Check for game over condition (lose) - health reaches 0
        if (!isGameOver && !isSummaryActive && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            // Store actual health value before death
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
        }
        
        // Check for victory condition (QA2 completed) - ONLY if enabled
        if (showSummaryOnQA2Completion && !isGameOver && !isSummaryActive && !waitingForLastQA2Panel && qa2System != null && IsQA2Completed())
        {
            isVictory = true;
            StartCoroutine(ShowSummaryPanel());
        }
    }
    
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
        
        // Find timeline manager
        if (timelineManager == null)
        {
            timelineManager = FindObjectOfType<K2_DummypTimeline>();
            if (timelineManager != null)
            {
                Debug.Log($"Found timeline manager: {timelineManager.gameObject.name}");
            }
        }
        
        // Find CharacterVisualSwapper
        if (characterVisualSwapper == null)
        {
            characterVisualSwapper = FindObjectOfType<CharacterVisualSwapper>();
            if (characterVisualSwapper != null)
            {
                Debug.Log($"Found CharacterVisualSwapper: {characterVisualSwapper.gameObject.name}");
                // Get the animator from the swapper
                playerAnimator = characterVisualSwapper.playerAnimator;
            }
        }
        
        // Try to find animator on player object if not found via swapper
        if (playerAnimator == null && playerObject != null)
        {
            playerAnimator = playerObject.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                Debug.Log($"Found animator on player: {playerAnimator.gameObject.name}");
            }
        }
        
        // Find background music AudioSource if not assigned
        if (backgroundMusicSource == null)
        {
            AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
            if (audioHandler != null)
            {
                backgroundMusicSource = audioHandler.GetComponent<AudioSource>();
                if (backgroundMusicSource != null)
                {
                    Debug.Log($"Found background music AudioSource on AudioHandler: {audioHandler.gameObject.name}");
                }
            }
            
            if (backgroundMusicSource == null)
            {
                GameObject bgMusicObj = GameObject.FindGameObjectWithTag("BackgroundMusic");
                if (bgMusicObj != null)
                {
                    backgroundMusicSource = bgMusicObj.GetComponent<AudioSource>();
                }
            }
        }
        
        // Try to find QA panels if not assigned
        if (qa1Panel == null && qa1System != null)
        {
            if (qa1System.assessmentCanvas != null)
            {
                qa1Panel = qa1System.assessmentCanvas;
                Debug.Log($"Found QA1 panel: {qa1Panel.name}");
            }
        }
        
        if (qa2Panel == null && qa2System != null)
        {
            if (qa2System.assessmentCanvas != null)
            {
                qa2Panel = qa2System.assessmentCanvas;
                Debug.Log($"Found QA2 panel: {qa2Panel.name}");
            }
        }
        
        // Create audio source for win/lose sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        if (playerObject == null)
        {
            playerObject = GameObject.Find("PlayerArmature");
        }
        
        // Find ProductSpawner if not assigned
        if (productSpawner == null)
        {
            productSpawner = FindObjectOfType<ProductSpawner>();
            if (productSpawner != null)
            {
                Debug.Log($"Found ProductSpawner: {productSpawner.gameObject.name}");
            }
        }
        
        // Find cameras if not assigned
        if (summaryVirtualCamera == null || playerFollowCamera == null)
        {
            FindCameraReferences();
        }
        
        // Find spawn point if not assigned
        if (playerSpawnPoint == null)
        {
            FindSpawnPoint();
        }
        
        // Find timeline cutscene objects if not assigned
        if (cutscene2ParentObject == null)
        {
            cutscene2ParentObject = GameObject.Find("Cutscene2Things");
            if (cutscene2ParentObject != null)
            {
                Debug.Log($"Found Cutscene2Things: {cutscene2ParentObject.name}");
            }
        }
        
        if (cutscene3ParentObject == null)
        {
            cutscene3ParentObject = GameObject.Find("Cutscene3");
            if (cutscene3ParentObject != null)
            {
                Debug.Log($"Found Cutscene3: {cutscene3ParentObject.name}");
            }
        }
        
        // Find timeline directors if not assigned
        if (cutscene2Timeline == null)
        {
            cutscene2Timeline = FindTimelineDirector("NPC_Cutscene2");
        }
        
        if (cutscene3Timeline == null)
        {
            cutscene3Timeline = FindTimelineDirector("NPC_Timeline3");
        }
        
        Debug.Log($"References found - Player: {playerObject != null}, Animator: {playerAnimator != null}, " +
                 $"CharacterSwapper: {characterVisualSwapper != null}, Spawn: {playerSpawnPoint != null}, " +
                 $"SummaryCamera: {summaryVirtualCamera != null}, TimelineManager: {timelineManager != null}, " +
                 $"QA2 Summary Enabled: {showSummaryOnQA2Completion}");
    }
    
    private PlayableDirector FindTimelineDirector(string name)
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        foreach (PlayableDirector director in allDirectors)
        {
            if (director.name.Contains(name))
            {
                Debug.Log($"Found timeline director: {director.name}");
                return director;
            }
        }
        return null;
    }
    
    // Store original positions of timeline objects
    private void StoreTimelineObjectPositions()
    {
        originalTimelineObjectPositions.Clear();
        
        // Store positions for Cutscene2 objects
        if (cutscene2ParentObject != null)
        {
            StoreTransformAndChildren(cutscene2ParentObject.transform);
        }
        
        // Store positions for Cutscene3 objects
        if (cutscene3ParentObject != null)
        {
            StoreTransformAndChildren(cutscene3ParentObject.transform);
        }
        
        Debug.Log($"Stored original positions for {originalTimelineObjectPositions.Count} timeline objects");
    }
    
    private void StoreTransformAndChildren(Transform parent)
    {
        if (parent == null) return;
        
        // Store this transform
        if (!originalTimelineObjectPositions.ContainsKey(parent))
        {
            originalTimelineObjectPositions[parent] = new TransformData(
                parent.position,
                parent.rotation,
                parent.localScale,
                parent.gameObject.activeSelf
            );
        }
        
        // Store all children
        foreach (Transform child in parent)
        {
            if (!originalTimelineObjectPositions.ContainsKey(child))
            {
                originalTimelineObjectPositions[child] = new TransformData(
                    child.position,
                    child.rotation,
                    child.localScale,
                    child.gameObject.activeSelf
                );
            }
            
            // Recursively store grandchildren
            if (child.childCount > 0)
            {
                StoreTransformAndChildren(child);
            }
        }
    }
    
    // Restore timeline object positions
    private void RestoreTimelineObjectPositions()
    {
        Debug.Log($"Restoring timeline object positions...");
        
        // FIRST: Stop and reset all timeline directors
        ResetAllTimelineDirectors();
        
        // SECOND: Force evaluate timelines at time 0 to clear any animation state
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        foreach (PlayableDirector director in allDirectors)
        {
            if (director != null)
            {
                director.time = 0;
                director.Evaluate(); // This applies the "time 0" state
            }
        }
        
        // THIRD: Restore original positions from our stored data
        foreach (var kvp in originalTimelineObjectPositions)
        {
            if (kvp.Key != null)
            {
                TransformData data = kvp.Value;
                
                // IMPORTANT: Disable animator components before restoring
                Animator animator = kvp.Key.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false; // Disable to prevent override
                }
                
                // Also check for Animation components
                Animation animation = kvp.Key.GetComponent<Animation>();
                if (animation != null)
                {
                    animation.Stop();
                    animation.enabled = false;
                }
                
                // Restore transform
                kvp.Key.position = data.position;
                kvp.Key.rotation = data.rotation;
                kvp.Key.localScale = data.scale;
                kvp.Key.gameObject.SetActive(data.isActive);
                
                // Re-enable animator after restoring
                if (animator != null)
                {
                    animator.enabled = true;
                    animator.Rebind(); // Reset to initial state
                    animator.Update(0f); // Force update
                }
                
                // Re-enable animation if exists
                if (animation != null)
                {
                    animation.enabled = true;
                }
            }
        }
        
        // FOURTH: Rebind all animators in the timeline objects
        Animator[] allAnimators = FindObjectsOfType<Animator>();
        foreach (Animator animator in allAnimators)
        {
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
        
        // FIFTH: Ensure timeline parent objects are disabled
        if (cutscene2ParentObject != null)
        {
            cutscene2ParentObject.SetActive(false);
        }
        
        if (cutscene3ParentObject != null)
        {
            cutscene3ParentObject.SetActive(false);
        }
        
        // SIXTH: Clear any remaining animation state
        StartCoroutine(ClearAnimationStateNextFrame());
        
        Debug.Log($"Restored {originalTimelineObjectPositions.Count} timeline object positions");
    }
    
    private IEnumerator ClearAnimationStateNextFrame()
    {
        yield return null; // Wait one frame
        
        // Force another evaluation to ensure timeline state is cleared
        PlayableDirector[] directors = FindObjectsOfType<PlayableDirector>();
        foreach (PlayableDirector director in directors)
        {
            if (director != null)
            {
                director.Evaluate();
            }
        }
    }
    
    // Call this AFTER timeline finishes playing to update stored positions
    public void UpdateTimelinePositionsAfterChanges()
    {
        // Clear and re-store positions to capture current state
        originalTimelineObjectPositions.Clear();
        
        // Store current positions (which might have been changed by timeline)
        if (cutscene2ParentObject != null)
        {
            StoreTransformAndChildren(cutscene2ParentObject.transform);
        }
        
        if (cutscene3ParentObject != null)
        {
            StoreTransformAndChildren(cutscene3ParentObject.transform);
        }
        
        Debug.Log($"Updated timeline positions after changes. Stored {originalTimelineObjectPositions.Count} positions.");
    }
    
    private bool IsQA2Completed()
    {
        if (qa2System == null) return false;
        
        int correctlyAnswered = qa2System.GetCorrectlyAnsweredCount();
        
        // Check if QA2 panel is currently active - if so, wait for it to close
        if (qa2System.IsPanelActive())
        {
            waitingForLastQA2Panel = true;
            StartCoroutine(WaitForLastQA2PanelToClose());
            return false;
        }
        
        // Use the inspector setting for required correct answers
        return correctlyAnswered >= requiredQA2CorrectAnswers;
    }
    
    private IEnumerator WaitForLastQA2PanelToClose()
    {
        Debug.Log("Waiting for last QA2 panel to close before showing summary...");
        
        while (qa2System != null && qa2System.IsPanelActive())
        {
            yield return null;
        }
        
        Debug.Log("QA2 panel closed, checking for completion...");
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
    
    private bool IsSummaryAlreadyActive()
    {
        return isGameOver || isSummaryActive;
    }

    // Update the TriggerSummaryFromQA2 method:
    public void TriggerSummaryFromQA2()
    {
        if (!isGameOver && !isSummaryActive && showSummaryOnQA2Completion)
        {
            // Check if any other system has already triggered summary (like key collection)
            bool shouldTrigger = true;
            
            // Check K2_CollectKey if it exists
            K2_CollectKey collectKey = FindObjectOfType<K2_CollectKey>();
            if (collectKey != null && collectKey.HasTriggeredSummary())
            {
                Debug.Log("Key collection already triggered summary - skipping QA2 trigger to avoid double summary.");
                shouldTrigger = false;
            }
            
            if (shouldTrigger)
            {
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
        }
    }
    
    // Public method to check if QA2 summary is enabled
    public bool IsQA2SummaryEnabled()
    {
        return showSummaryOnQA2Completion;
    }
    
    // Public method to enable/disable QA2 summary at runtime
    public void SetQA2SummaryEnabled(bool enabled)
    {
        showSummaryOnQA2Completion = enabled;
        Debug.Log($"QA2 Summary Trigger {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    // Public method to set required correct answers
    public void SetRequiredQA2Answers(int requiredAnswers)
    {
        requiredQA2CorrectAnswers = Mathf.Clamp(requiredAnswers, 1, 5);
        Debug.Log($"Required QA2 answers set to: {requiredQA2CorrectAnswers}");
    }
    
    private void StorePlayerOriginalTransform()
    {
        if (playerObject != null)
        {
            originalPlayerPosition = playerObject.transform.position;
            originalPlayerRotation = playerObject.transform.rotation;
            Debug.Log($"Stored player position: {originalPlayerPosition}, rotation: {originalPlayerRotation}");
        }
    }
    
    private void FindCameraReferences()
    {
        CinemachineVirtualCamera[] allCams = FindObjectsOfType<CinemachineVirtualCamera>();
        
        foreach (var cam in allCams)
        {
            // Look for summary camera
            if (cam.name.Contains("Summary", System.StringComparison.OrdinalIgnoreCase) || 
                cam.name.Contains("Result", System.StringComparison.OrdinalIgnoreCase))
            {
                summaryVirtualCamera = cam;
                Debug.Log($"Found summary camera: {cam.name}");
            }
            else if (cam.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase) || 
                    cam.name.Contains("Follow", System.StringComparison.OrdinalIgnoreCase))
            {
                playerFollowCamera = cam;
                Debug.Log($"Found player camera: {cam.name}");
            }
        }
        
        // If no summary camera found, create one or use menu camera
        if (summaryVirtualCamera == null)
        {
            Debug.LogWarning("No summary camera found! Please assign a dedicated camera for the summary view.");
        }
    }
    
    private void FindSpawnPoint()
    {
        GameObject spawnObj = GameObject.FindGameObjectWithTag("SpawnPoint");
        if (spawnObj == null)
        {
            spawnObj = GameObject.Find("SpawnPoint");
        }
        if (spawnObj == null)
        {
            spawnObj = GameObject.Find("PlayerSpawn");
        }
        
        if (spawnObj != null)
        {
            playerSpawnPoint = spawnObj.transform;
            Debug.Log($"Found spawn point: {spawnObj.name}");
        }
        else
        {
            Debug.LogWarning("No spawn point found! Will use original player position.");
        }
    }
    
    private IEnumerator ShowSummaryPanel()
    {
        if (isGameOver || isSummaryActive) 
        {
            Debug.LogWarning("Summary panel already shown or in progress! Skipping.");
            yield break;
        }
        
        isGameOver = true;
        isSummaryActive = true;
        
        Debug.Log($"Starting ShowSummaryPanel() - Victory: {isVictory}, Triggered by: {(isVictory ? "Key collection or QA2" : "Health depletion")}");
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Disable CinemachineBrain blending to avoid camera movement
        DisableCinemachineBlending();
        
        // Move player to spawn point BEFORE showing summary
        MovePlayerToSpawnPoint();
        
        // Disable player input BEFORE showing summary
        DisablePlayerInput();
        
        // Close all QA panels
        CloseAllQAPanels();
        
        // Lower background music volume instead of stopping it
        LowerBackgroundMusicVolume();
        
        // Switch to summary camera IMMEDIATELY with no blend
        SwitchToSummaryCameraImmediate();
        
        // Wait for one frame to ensure camera is positioned
        yield return null;
        
        // Trigger LookAround animation - FIXED to work during pause
        yield return StartCoroutine(TriggerLookAroundAnimationDuringPause());
        
        // Play appropriate sound
        PlayResultSound();
        
        // Calculate coin reward BEFORE showing panel
        CalculateCoinReward();
        
        // Collect all summary data
        UpdateSummaryData();
        
        // Show the panel
        if (gameSummaryPanel != null)
        {
            gameSummaryPanel.SetActive(true);
            
            // Set result text
            if (resultText != null)
            {
                resultText.text = isVictory ? "YOU WIN!" : "YOU LOSE!";
                resultText.color = isVictory ? Color.green : Color.red;
            }
            
            // Fade in panel if CanvasGroup exists
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                
                float elapsedTime = 0f;
                while (elapsedTime < fadeInDuration)
                {
                    elapsedTime += Time.unscaledDeltaTime;
                    panelCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
                    yield return null;
                }
                panelCanvasGroup.alpha = 1f;
            }
        }
        else
        {
            Debug.LogError("Game Summary Panel is not assigned in the inspector!");
        }
        
        Debug.Log($"Game {(isVictory ? "won" : "lost")} - Summary panel shown, player at spawn point. Triggered by: {(isVictory ? "Key collection or QA2 Completion" : "Health Depletion")}");
    }
    
    // Trigger LookAround animation during pause
    private IEnumerator TriggerLookAroundAnimationDuringPause()
    {
        if (playerAnimator != null)
        {
            // Force animator to use unscaled time so it works during pause
            playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            
            // Set the look around parameter to true
            if (!string.IsNullOrEmpty(lookAroundParameter))
            {
                playerAnimator.SetBool(lookAroundParameter, true);
            }
            
            // Also use CharacterVisualSwapper if available
            if (characterVisualSwapper != null)
            {
                characterVisualSwapper.TriggerLookAroundAnimation();
            }
            
            // Force an immediate update
            playerAnimator.Update(0f);
            
            Debug.Log("LookAround animation triggered during pause (using UnscaledTime)");
        }
        
        // Small delay to ensure animation starts
        yield return new WaitForSecondsRealtime(0.1f);
    }
    
    // Stop LookAround animation properly when panel closes
    private void StopLookAroundAnimationDuringPause()
    {
        if (playerAnimator != null)
        {
            // Set the look around parameter to false
            if (!string.IsNullOrEmpty(lookAroundParameter))
            {
                playerAnimator.SetBool(lookAroundParameter, false);
            }
            
            // Force an immediate update
            playerAnimator.Update(0f);
            
            // Restore animator update mode to normal
            playerAnimator.updateMode = AnimatorUpdateMode.Normal;
            
            Debug.Log("LookAround animation stopped and animator restored to Normal mode");
        }
        
        // Also use CharacterVisualSwapper if available
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.StopLookAroundAnimation();
        }
    }
    
    // Lower background music volume
    private void LowerBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.volume = backgroundMusicVolumeDuringSummary;
            Debug.Log($"Background music volume lowered to: {backgroundMusicSource.volume}");
        }
    }
    
    // Restore background music volume
    private void RestoreBackgroundMusicVolume()
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = originalBackgroundMusicVolume;
            Debug.Log($"Background music volume restored to: {backgroundMusicSource.volume}");
        }
    }
    
    // Disable Cinemachine blending to avoid camera movement
    private void DisableCinemachineBlending()
    {
        if (cinemachineBrain != null)
        {
            // Store original blend style and time
            cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
            cinemachineBrain.m_DefaultBlend.m_Time = 0f;
            Debug.Log("Disabled Cinemachine blending - using instant cut");
        }
    }
    
    // Enable Cinemachine blending for normal gameplay
    private void EnableCinemachineBlending()
    {
        if (cinemachineBrain != null)
        {
            // Restore smooth blending
            cinemachineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            cinemachineBrain.m_DefaultBlend.m_Time = 0.5f; // Adjust as needed
            Debug.Log("Enabled Cinemachine blending");
        }
    }
    
    // Immediate camera switch with no blending
    private void SwitchToSummaryCameraImmediate()
    {
        if (summaryVirtualCamera != null)
        {
            // Set summary camera to highest priority
            summaryVirtualCamera.Priority = 100;
            
            // Ensure player camera has lower priority
            if (playerFollowCamera != null)
            {
                playerFollowCamera.Priority = 0;
            }
            
            // Force the CinemachineBrain to update immediately
            if (cinemachineBrain != null)
            {
                cinemachineBrain.ManualUpdate();
            }
            
            Debug.Log("Switched to Summary Camera (immediate)");
        }
        else
        {
            Debug.LogWarning("Summary Virtual Camera not assigned!");
        }
    }
    
    // Switch back to player camera with blending enabled
    private void SwitchToPlayerCameraWithBlend()
    {
        // Re-enable blending first
        EnableCinemachineBlending();
        
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 100;
            Debug.Log("Switched to Player Follow Camera");
        }

        if (summaryVirtualCamera != null)
        {
            summaryVirtualCamera.Priority = 0;
        }
        
        // Force a manual update to ensure the switch happens
        if (cinemachineBrain != null)
        {
            cinemachineBrain.ManualUpdate();
        }
    }
    
    private void MovePlayerToSpawnPoint()
    {
        if (playerObject != null)
        {
            // Use spawn point if available, otherwise use original position
            if (playerSpawnPoint != null)
            {
                playerObject.transform.position = playerSpawnPoint.position;
                playerObject.transform.rotation = playerSpawnPoint.rotation;
                Debug.Log($"Player moved to spawn point: {playerSpawnPoint.position}");
            }
            else
            {
                playerObject.transform.position = originalPlayerPosition;
                playerObject.transform.rotation = originalPlayerRotation;
                Debug.Log($"Player moved to original position: {originalPlayerPosition}");
            }
            
            // Reset character controller if it exists
            CharacterController charController = playerObject.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                charController.enabled = true;
            }
        }
    }
    
    private void CloseAllQAPanels()
    {
        Debug.Log("Closing all QA panels before showing summary...");
        
        // Close QA1 panel if it exists and is active
        if (qa1Panel != null && qa1Panel.activeInHierarchy)
        {
            Debug.Log($"Closing QA1 panel: {qa1Panel.name}");
            qa1Panel.SetActive(false);
            
            if (qa1System != null)
            {
                System.Reflection.MethodInfo closeMethod = qa1System.GetType().GetMethod("ClosePanel");
                if (closeMethod != null)
                {
                    closeMethod.Invoke(qa1System, null);
                    Debug.Log("Called QA1 ClosePanel method");
                }
            }
        }
        
        // Close QA2 panel if it exists and is active
        if (qa2Panel != null && qa2Panel.activeInHierarchy)
        {
            Debug.Log($"Closing QA2 panel: {qa2Panel.name}");
            qa2Panel.SetActive(false);
            
            if (qa2System != null)
            {
                System.Reflection.MethodInfo closeMethod = qa2System.GetType().GetMethod("OnCloseButtonClicked");
                if (closeMethod != null)
                {
                    closeMethod.Invoke(qa2System, null);
                    Debug.Log("Called QA2 OnCloseButtonClicked method");
                }
            }
        }
        
        // Also close any other active UI panels that might interfere
        GameObject[] allCanvases = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allCanvases)
        {
            if (obj.activeInHierarchy && obj != gameSummaryPanel && 
                (obj.name.Contains("Assessment") || obj.name.Contains("QA") || 
                 obj.name.Contains("Nutrition") || obj.name.Contains("Menu")))
            {
                Debug.Log($"Found and closing interfering panel: {obj.name}");
                obj.SetActive(false);
            }
        }
        
        Debug.Log("All QA panels and interfering UI closed");
    }
    
    private void PlayResultSound()
    {
        if (audioSource == null) return;
        
        AudioClip clipToPlay = isVictory ? winSound : loseSound;
        
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, soundVolume);
            Debug.Log($"Playing {(isVictory ? "win" : "lose")} sound");
        }
        else
        {
            Debug.LogWarning($"{(isVictory ? "Win" : "Lose")} sound not assigned!");
        }
    }
    
    private void UpdateSummaryData()
    {
        // Calculate stars based on health or victory
        int stars = CalculateStars();
        if (starsCountText != null)
        {
            starsCountText.text = $"Stars: {stars}/3";
        }
        
        // Update key status based on stars (2-3 stars = unlocked, 0-1 stars = locked)
        UpdateKeyStatus(stars);
        
        // Get time played
        if (timePlayedText != null && gameplayProgression != null)
        {
            float timePlayed = gameplayProgression.GetCurrentTime();
            int minutes = Mathf.FloorToInt(timePlayed / 60f);
            int seconds = Mathf.FloorToInt(timePlayed % 60f);
            timePlayedText.text = $"Time: {minutes:00}:{seconds:00}";
        }
        else if (timePlayedText != null)
        {
            timePlayedText.text = "Time: --:--";
        }
        
        // Get products collected (from product manager)
        if (productsCollectedText != null && productManager != null)
        {
            int collected = productManager.GetCollectedCount();
            int total = 8; // Assuming 8 products total
            productsCollectedText.text = $"Products Collected: {collected}/{total}";
        }
        else if (productsCollectedText != null)
        {
            productsCollectedText.text = "Products Collected: ?/8";
        }
        
        // Get game score
        if (gameScoreText != null && scoringSystem != null)
        {
            int score = scoringSystem.GetCurrentScore();
            gameScoreText.text = $"Score: {score}";
        }
        else if (gameScoreText != null)
        {
            gameScoreText.text = "Score: 0";
        }
        
        // Show coins earned
        if (coinsEarnedText != null)
        {
            coinsEarnedText.text = $"Coins Earned: {calculatedCoinsEarned}";
        }
        
        Debug.Log($"Summary updated - Victory: {isVictory}, Stars: {stars}/3, Coins: {calculatedCoinsEarned}, Key: {(stars >= 2 ? "UNLOCKED" : "LOCKED")}");
    }
    
    private void UpdateKeyStatus(int stars)
    {
        if (keyStatusText != null)
        {
            bool isUnlocked = (stars == 2 || stars == 3);
            
            keyStatusText.text = isUnlocked ? "KEY: UNLOCKED" : "KEY: LOCKED";
            keyStatusText.color = isUnlocked ? unlockedColor : lockedColor;
        }
    }
    
    private int CalculateStars()
    {
        int stars = 0;
        
        if (isVictory)
        {
            // Victory: Get stars based on health at win
            if (playerHealth != null)
            {
                int health = playerHealth.currentHealth;

                if (health >= 5) stars = 3;
                else if (health >= 3) stars = 2;
                else if (health >= 1) stars = 1;
                else stars = 0;
                
                Debug.Log($"Victory stars calculation - Health: {health}, Stars: {stars}");
            }
        }
        else
        {
            int health = healthBeforeDeath > 0 ? healthBeforeDeath : 0;
            
            if (health >= 5) stars = 3;
            else if (health >= 3) stars = 2;
            else if (health >= 1) stars = 1;
            else stars = 0;
            
            Debug.Log($"Loss stars calculation - Health before death: {health}, Stars: {stars}");
        }
        
        // Minimum stars is 0, maximum is 3
        stars = Mathf.Clamp(stars, 0, 3);
        
        return stars;
    }
    
    private void CalculateCoinReward()
    {
        int stars = CalculateStars();
        int score = scoringSystem != null ? scoringSystem.GetCurrentScore() : 0;
        
        // Calculate base coin reward
        int starCoins = stars * coinsPerStar;
        int scoreCoins = Mathf.Max(0, (score / 300) * baseCoinsPerScore); // 1 coin per 300 score points
        
        // Apply multiplier based on win/lose
        int totalBaseCoins = starCoins + scoreCoins;
        float multiplier = isVictory ? winMultiplier : loseMultiplier;
        calculatedCoinsEarned = Mathf.RoundToInt(totalBaseCoins * multiplier);
        
        // Minimum of 1 coin
        calculatedCoinsEarned = Mathf.Max(1, calculatedCoinsEarned);
        
        Debug.Log($"Coin calculation - Victory: {isVictory}, Stars: {stars} ({starCoins} coins), " +
                 $"Score: {score} ({scoreCoins} coins), Base: {totalBaseCoins}, " +
                 $"Multiplier: {multiplier}, Final: {calculatedCoinsEarned}");
    }
    
    private void AddCoinsToDatabase()
    {
        if (coinsAddedToDatabase) return;
        
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.nutriCoins += calculatedCoinsEarned;
            GameDataManager.Instance.SaveGameData();
            
            coinsAddedToDatabase = true;
            
            Debug.Log($"Added {calculatedCoinsEarned} coins to database. New total: {GameDataManager.Instance.CurrentGameData.nutriCoins}");
        }
        else
        {
            Debug.LogWarning("GameDataManager not found! Coins not saved.");
        }
    }
    
    private void DisablePlayerInput()
    {
        // Find and disable player input
        InputManager inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            inputManager.DisablePlayerInput();
        }
        
        // Also disable ThirdPersonController directly as backup
        ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        // Disable StarterAssetsInputs
        StarterAssetsInputs inputs = FindObjectOfType<StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.enabled = false;
        }
        
        // Hide joystick UI
        if (mainMenuManager != null && mainMenuManager.joystickCanvas != null)
        {
            mainMenuManager.joystickCanvas.SetActive(false);
        }
        
        Debug.Log("Player input disabled for summary");
    }
    
    private void EnablePlayerInput()
    {
        // Find and enable player input
        InputManager inputManager = FindObjectOfType<InputManager>();
        if (inputManager != null)
        {
            inputManager.EnablePlayerInput();
        }
        
        // Also enable ThirdPersonController directly as backup
        ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
        
        // Enable StarterAssetsInputs
        StarterAssetsInputs inputs = FindObjectOfType<StarterAssetsInputs>();
        if (inputs != null)
        {
            inputs.enabled = true;
        }
        
        // Show joystick UI
        if (mainMenuManager != null && mainMenuManager.joystickCanvas != null)
        {
            mainMenuManager.joystickCanvas.SetActive(true);
        }
        
        Debug.Log("Player input enabled for gameplay");
    }
    
    public void OnConfirmButtonClicked()
    {
        Debug.Log("Confirm button clicked. Checking if we can proceed...");
        
        // Prevent multiple clicks
        if (!isSummaryActive || !isGameOver)
        {
            Debug.LogWarning("Confirm button clicked but summary is not active. Ignoring.");
            return;
        }
        
        // Play button sound if available
        AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
        if (audioHandler != null)
        {
            System.Reflection.MethodInfo clickMethod = audioHandler.GetType().GetMethod("PlayButtonClick");
            if (clickMethod != null)
            {
                clickMethod.Invoke(audioHandler, null);
                Debug.Log("Button click sound played");
            }
        }
        
        // ADD COINS TO DATABASE BEFORE RESTARTING
        AddCoinsToDatabase();
        
        // Disable the confirm button to prevent multiple clicks
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
        
        // Start fade out and restart game
        StartCoroutine(HidePanelAndRestartGame());
    }
    
    private IEnumerator HidePanelAndRestartGame()
    {
        Debug.Log("Starting HidePanelAndRestartGame()...");
        
        // Fade out panel if CanvasGroup exists
        if (panelCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / fadeOutDuration));
                yield return null;
            }
            panelCanvasGroup.alpha = 0f;
        }
        
        // Hide panel
        if (gameSummaryPanel != null)
        {
            gameSummaryPanel.SetActive(false);
        }
        
        // Stop LookAround animation before restarting
        StopLookAroundAnimationDuringPause();
        
        // Restore background music volume
        RestoreBackgroundMusicVolume();
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Restart the game
        RestartGame();
        
        // Re-enable confirm button for next time
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        
        yield return null;
    }
    
    private void RestartGame()
    {
        Debug.Log("Restarting game...");
        
        // Switch back to player camera with blending enabled
        SwitchToPlayerCameraWithBlend();
        
        // Reset all game systems
        ResetGameState();
        
        // Player is already at spawn point (moved there before summary)
        // Just ensure any necessary position corrections
        if (playerObject != null && playerSpawnPoint != null)
        {
            playerObject.transform.position = playerSpawnPoint.position;
            playerObject.transform.rotation = playerSpawnPoint.rotation;
        }
        
        // Restore timeline object positions
        RestoreTimelineObjectPositions();
        
        // Respawn all products
        RespawnAllProducts();
        
        // Re-enable player input for gameplay
        EnablePlayerInput();
        
        // Show joystick UI
        if (mainMenuManager != null && mainMenuManager.joystickCanvas != null)
        {
            mainMenuManager.joystickCanvas.SetActive(true);
        }
        
        // Ensure game stays in game mode (not menu mode)
        EnsureGameMode();
        
        // Reset this manager for the new game
        ResetManager();
        
        Debug.Log("Game restarted - Ready to play again!");
    }
    
    private void RespawnAllProducts()
    {
        // Use the ProductSpawner script to respawn products
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
        // Make sure we're in game mode, not menu mode
        if (mainMenuManager != null)
        {
            // Hide menu canvas if it's visible
            if (mainMenuManager.menuCanvas != null && mainMenuManager.menuCanvas.activeInHierarchy)
            {
                mainMenuManager.menuCanvas.SetActive(false);
                Debug.Log("Menu canvas hidden (was accidentally visible)");
            }
            
            // Ensure joystick is visible
            if (mainMenuManager.joystickCanvas != null && !mainMenuManager.joystickCanvas.activeInHierarchy)
            {
                mainMenuManager.joystickCanvas.SetActive(true);
                Debug.Log("Joystick canvas re-enabled");
            }
        }
    }
    
    private void ResetGameState()
    {
        Debug.Log("Resetting game state...");
        
        // Reset player health
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
            Debug.Log("Player health reset");
        }
        
        // Reset scoring system
        if (scoringSystem != null)
        {
            scoringSystem.ResetSessionStats();
            Debug.Log("Scoring system reset");
        }
        
        // Reset product collection
        if (productManager != null)
        {
            productManager.ResetForNewSession();
            Debug.Log("Product collection reset");
        }
        
        // Reset timer
        if (gameplayProgression != null)
        {
            System.Reflection.MethodInfo resetMethod = gameplayProgression.GetType().GetMethod("ResetTimer");
            if (resetMethod != null)
            {
                resetMethod.Invoke(gameplayProgression, null);
                Debug.Log("Game timer reset");
            }
            else
            {
                gameplayProgression.ManualGameStart();
                Debug.Log("Game timer manually reset");
            }
        }
        
        // Reset QA2 system if it exists
        if (qa2System != null)
        {
            System.Reflection.MethodInfo qa2ResetMethod = qa2System.GetType().GetMethod("ClearScannedProducts");
            if (qa2ResetMethod != null)
            {
                qa2ResetMethod.Invoke(qa2System, null);
                Debug.Log("QA2 scanned products cleared");
            }
        }
        
        // Reset timeline manager if it exists
        if (timelineManager != null)
        {
            timelineManager.ResetAllCutscenes();
            Debug.Log("Timeline manager reset");
        }
        
        // Reset all timeline directors
        ResetAllTimelineDirectors();
        
        // Find and reset all monsters
        ResetAllMonsters();
        
        // Reset dummy product collection if applicable
        if (collectProductsScript != null && collectProductsScript.HasCollectedDummyProduct())
        {
            collectProductsScript.ResetDummyProductCollection();
            Debug.Log("Dummy product collection reset");
        }
        
        // FIXED: PROPERLY RESET KEY SYSTEM
        ResetKeySystem();
        
        // Call global reset for keys
        System.Reflection.MethodInfo globalResetMethod = typeof(K2_CollectKey).GetMethod("GlobalResetAllKeys", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (globalResetMethod != null)
        {
            globalResetMethod.Invoke(null, null);
            Debug.Log("Called global key reset");
        }
        
        Debug.Log("Game state fully reset");
    }
    
    // NEW: Method to properly reset the key system
    private void ResetKeySystem()
    {
        Debug.Log("Resetting key system...");
        
        // Find and reset all K2_CollectKey scripts
        K2_CollectKey[] allKeyScripts = FindObjectsOfType<K2_CollectKey>();
        foreach (K2_CollectKey keyScript in allKeyScripts)
        {
            if (keyScript != null)
            {
                // Call the ResetKey method
                keyScript.ResetKey();
                Debug.Log($"Reset key script on {keyScript.gameObject.name}");
                
                // Also try to call ForceFullReset if it exists
                System.Reflection.MethodInfo forceResetMethod = keyScript.GetType().GetMethod("ForceFullReset");
                if (forceResetMethod != null)
                {
                    forceResetMethod.Invoke(keyScript, null);
                    Debug.Log($"Called ForceFullReset on {keyScript.gameObject.name}");
                }
            }
        }
        
        // Also try to find and destroy any remaining key objects
        GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
        foreach (GameObject key in remainingKeys)
        {
            if (key != null)
            {
                Destroy(key);
                Debug.Log($"Destroyed remaining key object: {key.name}");
            }
        }
        
        Debug.Log($"Key system reset: {allKeyScripts.Length} key scripts reset, {remainingKeys.Length} key objects destroyed");
    }
    
    // Reset all timeline directors with proper cleanup
    private void ResetAllTimelineDirectors()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        foreach (PlayableDirector director in allDirectors)
        {
            if (director != null)
            {
                // Stop and reset
                director.Stop();
                director.time = 0;
                director.Evaluate(); // Force evaluation at time 0
                
                // Get all animators bound to this timeline and reset them
                var bindings = director.playableAsset.outputs;
                foreach (var binding in bindings)
                {
                    var boundObject = director.GetGenericBinding(binding.sourceObject);
                    if (boundObject is Animator animator)
                    {
                        // IMPORTANT: Stop any playing animation
                        animator.enabled = false;
                        animator.Rebind(); // Reset to initial state
                        animator.enabled = true;
                        animator.Update(0f);
                    }
                    
                    // Also handle GameObject activations
                    if (boundObject is GameObject gameObj)
                    {
                        // Check if this GameObject has an animator
                        Animator objAnimator = gameObj.GetComponent<Animator>();
                        if (objAnimator != null)
                        {
                            objAnimator.enabled = false;
                            objAnimator.Rebind();
                            objAnimator.enabled = true;
                            objAnimator.Update(0f);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Reset {allDirectors.Length} timeline directors");
    }
    
    private void ResetAllMonsters()
    {
        // Find all monsters and reset them
        MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
        
        foreach (MonsterObstacle monster in allMonsters)
        {
            if (monster != null)
            {
                // Reset monster to starting state
                monster.gameObject.SetActive(true);
                
                System.Reflection.MethodInfo resetMethod = monster.GetType().GetMethod("ResetMonster");
                if (resetMethod != null)
                {
                    resetMethod.Invoke(monster, null);
                }
            }
        }
        
        Debug.Log($"Reset {allMonsters.Length} monsters");
    }
    
    private void ResetManager()
    {
        isGameOver = false;
        isVictory = false;
        waitingForLastQA2Panel = false;
        isSummaryActive = false; // NEW: Reset this flag
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        
        Debug.Log("GameSummaryManager reset for new game - isGameOver: " + isGameOver + ", isVictory: " + isVictory + ", isSummaryActive: " + isSummaryActive);
    }
    
    // Public method to manually trigger win (for testing) - UPDATED
    [ContextMenu("Test Win")]
    public void TestWin()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = true;
            // Set health for testing star calculation
            if (playerHealth != null)
            {
                playerHealth.currentHealth = 6; // 3 stars
            }
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot test win - summary already active!");
        }
    }
    
    // Public method to manually trigger lose (for testing) - UPDATED
    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        if (!isGameOver && !isSummaryActive)
        {
            isVictory = false;
            // Set health before death for testing star calculation
            healthBeforeDeath = 0; // 0 stars (to test the fix)
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot test lose - summary already active!");
        }
    }
    
    // Test adding coins
    [ContextMenu("Test Coin Calculation")]
    public void TestCoinCalculation()
    {
        CalculateCoinReward();
        Debug.Log($"Test coin calculation: {calculatedCoinsEarned} coins");
    }
    
    // Test QA2 summary trigger
    [ContextMenu("Test QA2 Summary Trigger")]
    public void TestQA2Summary()
    {
        if (!isGameOver && !isSummaryActive)
        {
            Debug.Log($"Testing QA2 Summary Trigger (Enabled: {showSummaryOnQA2Completion})");
            isVictory = true;
            StartCoroutine(ShowSummaryPanel());
        }
        else
        {
            Debug.LogWarning("Cannot test QA2 summary - summary already active!");
        }
    }
    
    // Toggle QA2 summary at runtime
    [ContextMenu("Toggle QA2 Summary")]
    public void ToggleQA2Summary()
    {
        showSummaryOnQA2Completion = !showSummaryOnQA2Completion;
        Debug.Log($"QA2 Summary Trigger {(showSummaryOnQA2Completion ? "ENABLED" : "DISABLED")}");
    }
    
    // Debug info
    [ContextMenu("Debug Summary Info")]
    public void DebugSummaryInfo()
    {
        Debug.Log("=== GAME SUMMARY MANAGER DEBUG ===");
        Debug.Log($"Game Over State: {isGameOver}");
        Debug.Log($"Victory State: {isVictory}");
        Debug.Log($"Summary Active: {isSummaryActive}");
        Debug.Log($"Player Object: {playerObject != null}");
        Debug.Log($"Player at Spawn: {(playerObject != null && playerSpawnPoint != null ? (Vector3.Distance(playerObject.transform.position, playerSpawnPoint.position) < 0.1f).ToString() : "N/A")}");
        Debug.Log($"Character Visual Swapper: {characterVisualSwapper != null}");
        Debug.Log($"Player Animator: {playerAnimator != null}");
        Debug.Log($"LookAround Parameter: {lookAroundParameter}");
        Debug.Log($"Summary Camera: {summaryVirtualCamera != null}");
        Debug.Log($"Spawn Point: {playerSpawnPoint != null}");
        Debug.Log($"Spawn Position: {(playerSpawnPoint != null ? playerSpawnPoint.position.ToString() : "N/A")}");
        Debug.Log($"QA2 Summary Enabled: {showSummaryOnQA2Completion}");
        Debug.Log($"Required QA2 Answers: {requiredQA2CorrectAnswers}");
        
        if (playerAnimator != null && !string.IsNullOrEmpty(lookAroundParameter))
        {
            Debug.Log($"LookAround bool value: {playerAnimator.GetBool(lookAroundParameter)}");
        }
        
        Debug.Log("=== END DEBUG ===");
    }
    
    [ContextMenu("Debug Timeline Positions")]
    public void DebugTimelinePositions()
    {
        Debug.Log("=== TIMELINE POSITIONS DEBUG ===");
        Debug.Log($"Stored positions: {originalTimelineObjectPositions.Count}");
        
        int i = 0;
        foreach (var kvp in originalTimelineObjectPositions)
        {
            if (kvp.Key != null)
            {
                Debug.Log($"{i}: {kvp.Key.name} - Active: {kvp.Value.isActive}, Position: {kvp.Value.position}");
                i++;
            }
        }
        
        Debug.Log("=== END TIMELINE DEBUG ===");
    }
    
    void OnDestroy()
    {
        // Clean up - restore time scale if destroyed while paused
        if (isGameOver)
        {
            Time.timeScale = originalTimeScale;
        }
        
        // Restore background music volume
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = originalBackgroundMusicVolume;
        }
        
        // Remove button listener
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
}