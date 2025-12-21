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
    public CinemachineVirtualCamera menuVirtualCamera;
    public CinemachineVirtualCamera playerFollowCamera;
    
    [Header("References - Auto Found")]
    private SugariaPlayerStat playerHealth;
    private GameplayProgression gameplayProgression;
    private ProductInformationManager productManager;
    private SugariaScoringSystem scoringSystem;
    private MainMenu_Manager mainMenuManager;
    private GameObject playerObject;
    private CollectProducts collectProductsScript;
    private K2_QA2system qa2System;
    
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
        
        Debug.Log("GameSummaryManager initialized");
    }
    
    void Update()
    {
        // Check for game over condition (lose) - health reaches 0
        if (!isGameOver && playerHealth != null && playerHealth.currentHealth <= 0)
        {
            // Store actual health value before death (FIXED: was always setting to 1)
            healthBeforeDeath = playerHealth.currentHealth;
            isVictory = false;
            StartCoroutine(ShowSummaryPanel());
        }
        
        // Check for victory condition (QA2 completed)
        if (!isGameOver && !waitingForLastQA2Panel && qa2System != null && IsQA2Completed())
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
        
        // Find background music AudioSource if not assigned
        if (backgroundMusicSource == null)
        {
            // Look for AudioHandler first
            AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
            if (audioHandler != null)
            {
                // Try to get AudioSource from AudioHandler
                backgroundMusicSource = audioHandler.GetComponent<AudioSource>();
                if (backgroundMusicSource != null)
                {
                    Debug.Log($"Found background music AudioSource on AudioHandler: {audioHandler.gameObject.name}");
                }
                else
                {
                    // If no AudioSource on AudioHandler, look for any AudioSource playing music
                    AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
                    foreach (AudioSource source in allAudioSources)
                    {
                        if (source.isPlaying && source != audioSource)
                        {
                            backgroundMusicSource = source;
                            Debug.Log($"Found playing AudioSource: {source.gameObject.name}");
                            break;
                        }
                    }
                }
            }
            
            // If still not found, look for any AudioSource tagged as background music
            if (backgroundMusicSource == null)
            {
                GameObject bgMusicObj = GameObject.FindGameObjectWithTag("BackgroundMusic");
                if (bgMusicObj != null)
                {
                    backgroundMusicSource = bgMusicObj.GetComponent<AudioSource>();
                    if (backgroundMusicSource != null)
                    {
                        Debug.Log($"Found AudioSource on BackgroundMusic tagged object: {bgMusicObj.name}");
                    }
                }
            }
            
            if (backgroundMusicSource == null)
            {
                Debug.LogWarning("Background music AudioSource not found! Music will not stop/start with panel.");
            }
            else
            {
                Debug.Log($"Background music source found: {backgroundMusicSource.gameObject.name}, playing: {backgroundMusicSource.isPlaying}");
            }
        }
        
        // Create audio source for win/lose sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        if (playerObject == null)
        {
            // Try to find player by name
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
            else
            {
                Debug.LogWarning("ProductSpawner not found! Products may not respawn.");
            }
        }
        
        // Find cameras if not assigned
        if (menuVirtualCamera == null || playerFollowCamera == null)
        {
            FindCameraReferences();
        }
        
        // Find spawn point if not assigned
        if (playerSpawnPoint == null)
        {
            FindSpawnPoint();
        }
        
        Debug.Log($"References found - Player: {playerObject != null}, Health: {playerHealth != null}, " +
                 $"Spawn: {playerSpawnPoint != null}, ProductSpawner: {productSpawner != null}, " +
                 $"QA2: {qa2System != null}, BGM Source: {backgroundMusicSource != null}");
    }
    
    private bool IsQA2Completed()
    {
        if (qa2System == null) return false;
        
        // Check if all 5 products have been correctly answered in QA2
        int correctlyAnswered = qa2System.GetCorrectlyAnsweredCount();
        
        // Check if QA2 panel is currently active - if so, wait for it to close
        if (qa2System.IsPanelActive())
        {
            waitingForLastQA2Panel = true;
            StartCoroutine(WaitForLastQA2PanelToClose());
            return false;
        }
        
        return correctlyAnswered >= 5; // All 5 products completed
    }
    
    private IEnumerator WaitForLastQA2PanelToClose()
    {
        Debug.Log("Waiting for last QA2 panel to close before showing summary...");
        
        // Wait for the panel to close
        while (qa2System != null && qa2System.IsPanelActive())
        {
            yield return null;
        }
        
        Debug.Log("QA2 panel closed, checking for completion...");
        waitingForLastQA2Panel = false;
        
        // Check again if QA2 is completed now that panel is closed
        if (qa2System != null && !isGameOver)
        {
            int correctlyAnswered = qa2System.GetCorrectlyAnsweredCount();
            if (correctlyAnswered >= 5)
            {
                isVictory = true;
                StartCoroutine(ShowSummaryPanel());
            }
        }
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
        // Find all Cinemachine virtual cameras
        CinemachineVirtualCamera[] allCams = FindObjectsOfType<CinemachineVirtualCamera>();
        
        foreach (var cam in allCams)
        {
            if (cam.name.Contains("Menu", System.StringComparison.OrdinalIgnoreCase) || 
                cam.name.Contains("UI", System.StringComparison.OrdinalIgnoreCase))
            {
                menuVirtualCamera = cam;
            }
            else if (cam.name.Contains("Player", System.StringComparison.OrdinalIgnoreCase) || 
                    cam.name.Contains("Follow", System.StringComparison.OrdinalIgnoreCase))
            {
                playerFollowCamera = cam;
            }
        }
    }
    
    private void FindSpawnPoint()
    {
        // Look for spawn point by tag or name
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
        if (isGameOver) yield break;
        
        isGameOver = true;
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Stop background music when panel activates (SIMPLE STOP)
        StopBackgroundMusic();
        
        // Switch to menu camera for summary view
        SwitchToMenuCamera();
        
        // Wait for one frame to ensure everything is processed
        yield return null;
        
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
        
        Debug.Log($"Game {(isVictory ? "won" : "lost")} - Summary panel shown");
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
            
            // Optional: Add visual effects
            if (isUnlocked)
            {
                // You could add animation or particle effects here
                // For example: StartCoroutine(PulseTextEffect(keyStatusText));
            }
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
        
        Debug.Log("Player input enabled for gameplay");
    }
    
    private void SwitchToMenuCamera()
    {
        if (menuVirtualCamera != null)
        {
            menuVirtualCamera.Priority = 10;
            Debug.Log("Switched to Menu Camera for summary");
        }
        else
        {
            Debug.LogWarning("Menu Virtual Camera not assigned!");
        }

        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
        }
    }
    
    private void SwitchToPlayerCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 10;
            Debug.Log("Switched to Player Follow Camera");
        }
        else
        {
            Debug.LogWarning("Player Follow Camera not assigned!");
        }

        if (menuVirtualCamera != null)
        {
            menuVirtualCamera.Priority = 0;
        }
    }
    
    // SIMPLE Audio Control Methods - Just Stop and Start
    private void StopBackgroundMusic()
    {
        if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Stop();
            Debug.Log("Background music STOPPED");
        }
        else if (backgroundMusicSource != null)
        {
            Debug.Log("Background music source exists but isn't playing");
        }
    }
    
    private void StartBackgroundMusic()
    {
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.Play();
            Debug.Log("Background music STARTED");
        }
    }
    
    public void OnConfirmButtonClicked()
    {
        // Play button sound if available
        AudioHandler audioHandler = FindObjectOfType<AudioHandler>();
        if (audioHandler != null)
        {
            // Try to play button click sound
            System.Reflection.MethodInfo clickMethod = audioHandler.GetType().GetMethod("PlayButtonClick");
            if (clickMethod != null)
            {
                clickMethod.Invoke(audioHandler, null);
                Debug.Log("Button click sound played");
            }
        }
        
        // Start background music when confirm is pressed (SIMPLE START)
        StartBackgroundMusic();
        
        // Add coins to database before restarting
        AddCoinsToDatabase();
        
        // Start fade out and restart game
        StartCoroutine(HidePanelAndRestartGame());
    }
    
    private IEnumerator HidePanelAndRestartGame()
    {
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
        
        // Restore time scale
        Time.timeScale = originalTimeScale;
        
        // Restart the game
        RestartGame();
        
        yield return null;
    }
    
    private void RestartGame()
    {
        Debug.Log("Restarting game...");
        
        // Switch back to player camera
        SwitchToPlayerCamera();
        
        // Reset all game systems
        ResetGameState();
        
        // Reset player position to spawn point
        ResetPlayerPosition();
        
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
    
    private void ResetPlayerPosition()
    {
        if (playerObject != null)
        {
            // Use spawn point if available, otherwise use original position
            if (playerSpawnPoint != null)
            {
                playerObject.transform.position = playerSpawnPoint.position;
                playerObject.transform.rotation = playerSpawnPoint.rotation;
                Debug.Log($"Player reset to spawn point: {playerSpawnPoint.position}");
            }
            else
            {
                playerObject.transform.position = originalPlayerPosition;
                playerObject.transform.rotation = originalPlayerRotation;
                Debug.Log($"Player reset to original position: {originalPlayerPosition}");
            }
            
            // Also reset character controller if it exists
            CharacterController charController = playerObject.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
                charController.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("Player object not found! Cannot reset position.");
        }
    }
    
    private void RespawnAllProducts()
    {
        // Use the ProductSpawner script to respawn products
        if (productSpawner != null)
        {
            Debug.Log("Calling ProductSpawner to respawn products...");
            
            // Try different methods that might exist
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
                    // Try to call directly if it's a public method
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
            // We need to access the ResetTimer method if it exists
            System.Reflection.MethodInfo resetMethod = gameplayProgression.GetType().GetMethod("ResetTimer");
            if (resetMethod != null)
            {
                resetMethod.Invoke(gameplayProgression, null);
                Debug.Log("Game timer reset");
            }
            else
            {
                // Try manual reset
                gameplayProgression.ManualGameStart();
                Debug.Log("Game timer manually reset");
            }
        }
        
        // Reset QA2 system if it exists
        if (qa2System != null)
        {
            // Try to call a reset method if it exists
            System.Reflection.MethodInfo qa2ResetMethod = qa2System.GetType().GetMethod("ClearScannedProducts");
            if (qa2ResetMethod != null)
            {
                qa2ResetMethod.Invoke(qa2System, null);
                Debug.Log("QA2 scanned products cleared");
            }
        }
        
        // Find and reset all monsters
        ResetAllMonsters();
        
        // Reset dummy product collection if applicable
        if (collectProductsScript != null && collectProductsScript.HasCollectedDummyProduct())
        {
            collectProductsScript.ResetDummyProductCollection();
            Debug.Log("Dummy product collection reset");
        }
        
        Debug.Log("Game state fully reset");
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
                
                // Call reset method if it exists
                System.Reflection.MethodInfo resetMethod = monster.GetType().GetMethod("ResetMonster");
                if (resetMethod != null)
                {
                    resetMethod.Invoke(monster, null);
                }
                
                // Also try Reset method
                System.Reflection.MethodInfo simpleReset = monster.GetType().GetMethod("Reset");
                if (simpleReset != null)
                {
                    simpleReset.Invoke(monster, null);
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
        coinsAddedToDatabase = false;
        calculatedCoinsEarned = 0;
        healthBeforeDeath = 0;
        
        Debug.Log("GameSummaryManager reset for new game");
    }
    
    // Public method to manually trigger win (for testing)
    [ContextMenu("Test Win")]
    public void TestWin()
    {
        if (!isGameOver)
        {
            isVictory = true;
            // Set health for testing star calculation
            if (playerHealth != null)
            {
                playerHealth.currentHealth = 6; // 3 stars
            }
            StartCoroutine(ShowSummaryPanel());
        }
    }
    
    // Public method to manually trigger lose (for testing)
    [ContextMenu("Test Lose")]
    public void TestLose()
    {
        if (!isGameOver)
        {
            isVictory = false;
            // Set health before death for testing star calculation
            healthBeforeDeath = 0; // 0 stars (to test the fix)
            StartCoroutine(ShowSummaryPanel());
        }
    }
    
    // Test adding coins
    [ContextMenu("Test Coin Calculation")]
    public void TestCoinCalculation()
    {
        CalculateCoinReward();
        Debug.Log($"Test coin calculation: {calculatedCoinsEarned} coins");
    }
    
    // Debug info
    [ContextMenu("Debug Summary Info")]
    public void DebugSummaryInfo()
    {
        Debug.Log("=== GAME SUMMARY MANAGER DEBUG ===");
        Debug.Log($"Game Over State: {isGameOver}");
        Debug.Log($"Victory State: {isVictory}");
        Debug.Log($"Waiting for QA2 Panel: {waitingForLastQA2Panel}");
        Debug.Log($"Player Object: {playerObject != null}");
        Debug.Log($"Player Health: {(playerHealth != null ? playerHealth.currentHealth.ToString() : "N/A")}");
        Debug.Log($"Max Health: {(playerHealth != null ? playerHealth.maxHealth.ToString() : "N/A")}");
        Debug.Log($"Health Before Death: {healthBeforeDeath}");
        Debug.Log($"Spawn Point: {playerSpawnPoint != null}");
        Debug.Log($"Spawn Position: {(playerSpawnPoint != null ? playerSpawnPoint.position.ToString() : "N/A")}");
        Debug.Log($"ProductSpawner: {productSpawner != null}");
        Debug.Log($"QA2 System: {qa2System != null}");
        Debug.Log($"Background Music Source: {backgroundMusicSource != null}");
        if (backgroundMusicSource != null)
        {
            Debug.Log($"BGM Playing: {backgroundMusicSource.isPlaying}");
            Debug.Log($"BGM Game Object: {backgroundMusicSource.gameObject.name}");
        }
        Debug.Log($"Key Status Text: {keyStatusText != null}");
        
        if (qa2System != null)
        {
            Debug.Log($"QA2 Correctly Answered: {qa2System.GetCorrectlyAnsweredCount()}/5");
            Debug.Log($"QA2 Panel Active: {qa2System.IsPanelActive()}");
            Debug.Log($"QA2 Completed: {IsQA2Completed()}");
        }
        
        if (productSpawner != null)
        {
            Debug.Log($"ProductSpawner initialized: {productSpawner.IsGameInitialized()}");
            Debug.Log($"ProductSpawner spawned count: {productSpawner.GetSpawnedProductCount()}");
        }
        
        if (gameplayProgression != null)
        {
            Debug.Log($"Time Played: {gameplayProgression.GetCurrentTime():F1}s");
        }
        
        if (productManager != null)
        {
            Debug.Log($"Products Collected: {productManager.GetCollectedCount()}/8");
        }
        
        int stars = CalculateStars();
        Debug.Log($"Stars Calculation: {stars}/3");
        Debug.Log($"Key Status: {(stars >= 2 ? "UNLOCKED" : "LOCKED")}");
        Debug.Log($"Calculated Coins: {calculatedCoinsEarned}");
    }
    
    void OnDestroy()
    {
        // Clean up - restore time scale if destroyed while paused
        if (isGameOver)
        {
            Time.timeScale = originalTimeScale;
        }
        
        // Remove button listener
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
    
    // Optional: Text pulse animation for unlocked key status
    private IEnumerator PulseTextEffect(TextMeshProUGUI text)
    {
        if (text == null) yield break;
        
        float pulseDuration = 0.5f;
        float elapsedTime = 0f;
        Color originalColor = text.color;
        
        while (elapsedTime < pulseDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float alpha = Mathf.PingPong(elapsedTime / pulseDuration, 0.3f) + 0.7f;
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        text.color = originalColor;
    }
}