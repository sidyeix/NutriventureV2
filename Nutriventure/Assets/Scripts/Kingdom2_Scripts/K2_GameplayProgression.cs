using UnityEngine;
using TMPro;
using System.Collections;

public class GameplayProgression : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI levelText;   
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI timerText; // Assign your timer text UI here
    
    [Header("Update Settings")]
    public bool autoUpdate = true;
    public float updateInterval = 0.5f;
    
    [Header("Timer Settings")]
    public bool countUp = true; // Set to false for countdown timer
    public float initialTime = 0f; // For countdown, set this to starting time
    
    private float currentTime = 0f;
    private bool isTimerRunning = false;
    private bool gameStarted = false;
    
    // Reference to main menu manager to detect when game starts
    private MainMenu_Manager mainMenuManager;
    
    private void Start()
    {
        mainMenuManager = UnityEngine.Object.FindFirstObjectByType<MainMenu_Manager>();
        if (mainMenuManager == null)
        {
            Debug.LogWarning("MainMenu_Manager not found in scene!");
        }
        
        InitializeUI();
        
        if (autoUpdate)
        {
            // Start periodic updates
            InvokeRepeating(nameof(UpdateAllDisplays), 0f, updateInterval);
        }
        
        // Initialize timer but don't start it yet
        ResetTimer();
        UpdateTimerDisplay();
        
        // Listen for game start event
        SetupGameStartListener();
        
        // Subscribe to product panel events
        ProductInformationManager.OnProductPanelShown += PauseTimer;
        ProductInformationManager.OnProductPanelHidden += ResumeTimer;
    }
    
    private void SetupGameStartListener()
    {
        // We'll check for game start in Update instead of events
        // since MainMenu_Manager doesn't have events we can subscribe to
    }
    
    private void Update()
    {
        // Check if game should start (when menu is hidden and joystick is shown)
        if (!gameStarted && IsGameStarted())
        {
            StartGame();
        }
        
        if (isTimerRunning)
        {
            if (countUp)
            {
                // Count up timer
                currentTime += Time.deltaTime;
            }
            else
            {
                // Countdown timer
                currentTime -= Time.deltaTime;
                currentTime = Mathf.Max(0f, currentTime);
                
                // Check if countdown reached zero
                if (currentTime <= 0f)
                {
                    StopTimer();
                    OnTimerComplete();
                }
            }
            
            UpdateTimerDisplay();
        }
    }
    
    // Check if the game has started by looking at the UI state
    private bool IsGameStarted()
    {
        if (mainMenuManager != null)
        {
            // Check if menu is hidden (game has started)
            if (mainMenuManager.menuCanvas != null && !mainMenuManager.menuCanvas.activeInHierarchy)
            {
                return true;
            }
            
            // Alternative: Check if joystick is active
            if (mainMenuManager.joystickCanvas != null && mainMenuManager.joystickCanvas.activeInHierarchy)
            {
                return true;
            }
        }
        
        // Fallback: Check if there's a player with input enabled
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Check if player has active movement components
            MonoBehaviour[] controllers = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour controller in controllers)
            {
                if (controller != null && controller.enabled && 
                    (controller.GetType().Name.Contains("Controller") || 
                     controller.GetType().Name.Contains("Movement")))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private void InitializeUI()
    {
        UpdateCoinDisplay();
        UpdateLevelDisplay();
        UpdateXPDisplay();
        UpdateTimerDisplay();
    }
    
    // Automatically called when game starts
    public void StartGame()
    {
        if (!gameStarted)
        {
            gameStarted = true;
            StartTimer();
            
            Debug.Log("Game started! Timer running...");
            
            // You can add other game start logic here
            OnGameStart();
        }
    }
    
    public void StartTimer()
    {
        isTimerRunning = true;
        Debug.Log($"Timer started at: {FormatTime(currentTime)}");
    }
    
    public void StopTimer()
    {
        isTimerRunning = false;
        Debug.Log($"Timer stopped at: {FormatTime(currentTime)}");
    }
    
    public void ResetTimer()
    {
        currentTime = countUp ? 0f : initialTime;
        isTimerRunning = false;
        gameStarted = false;
        UpdateTimerDisplay();
        
        Debug.Log("Timer reset");
    }
    
    public void PauseTimer()
    {
        isTimerRunning = false;
        Debug.Log("Timer paused");
    }
    
    public void ResumeTimer()
    {
        // FIXED: Check if game is actually started before resuming
        if (gameStarted && !isTimerRunning)
        {
            isTimerRunning = true;
            Debug.Log("Timer resumed");
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
        }
        else
        {
            Debug.LogWarning("Timer Text not assigned in Inspector!");
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        
        return $"{minutes:00}:{seconds:00}";
    }
    
    public void UpdateCoinDisplay()
    {
        if (coinsText != null)
        {
            int currentCoins = CoinCollectionSystem.GetCurrentCoins();
            coinsText.text = $"Coins: {currentCoins}";
        }
    }
    
    public void UpdateLevelDisplay()
    {
        if (levelText != null && GameDataManager.Instance != null)
        {
            levelText.text = $"Level {GameDataManager.Instance.CurrentGameData.playerLevel}";
        }
    }
    
    public void UpdateXPDisplay()
    {
        if (xpText != null && GameDataManager.Instance != null)
        {
            float currentXP = GameDataManager.Instance.CurrentGameData.currentXP;
            float xpToNextLevel = GameDataManager.Instance.CurrentGameData.xpToNextLevel;
            xpText.text = $"{currentXP}/{xpToNextLevel} XP";
        }
    }
    
    public void UpdateAllDisplays()
    {
        UpdateCoinDisplay();
        UpdateLevelDisplay();
        UpdateXPDisplay();
        // Timer is updated in Update(), no need to call here
    }
    
    // Event handlers
    private void OnGameStart()
    {
        // Override this method to add custom game start logic
        Debug.Log("Game started event triggered");
        
        // Example: Enable player controls, spawn enemies, etc.
    }
    
    private void OnTimerComplete()
    {
        // This is called when countdown timer reaches zero
        Debug.Log("Timer completed!");
        
        // Example: End game, show results, etc.
        // GameManager.Instance.EndGame();
    }
    
    // Public methods to access timer state
    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }
    
    public bool IsGameStarted2()
    {
        return gameStarted;
    }
    
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    public string GetFormattedTime()
    {
        return FormatTime(currentTime);
    }
    
    // Set a specific time (useful for loading saved games)
    public void SetTime(float newTime)
    {
        currentTime = Mathf.Max(0f, newTime);
        UpdateTimerDisplay();
    }
    
    // Add bonus time (useful for power-ups)
    public void AddTime(float secondsToAdd)
    {
        currentTime += secondsToAdd;
        if (!countUp)
        {
            currentTime = Mathf.Max(0f, currentTime);
        }
        UpdateTimerDisplay();
    }
    
    // Called when coin is collected (can be called from other scripts)
    public void OnCoinCollected()
    {
        UpdateCoinDisplay();
    }
    
    // Called when level changes
    public void OnLevelUp()
    {
        UpdateLevelDisplay();
        UpdateXPDisplay();
    }
    
    private void OnEnable()
    {
        // Refresh UI when this component is enabled
        UpdateAllDisplays();
        UpdateTimerDisplay();
    }
    
    private void OnDestroy()
    {
        // Clean up repeating invokes
        if (autoUpdate)
        {
            CancelInvoke(nameof(UpdateAllDisplays));
        }
        
        // Unsubscribe from events
        ProductInformationManager.OnProductPanelShown -= PauseTimer;
        ProductInformationManager.OnProductPanelHidden -= ResumeTimer;
    }
    
    // Context menu methods for testing in editor
    [ContextMenu("Start Game")]
    public void DebugStartGame()
    {
        StartGame();
    }
    
    [ContextMenu("Reset Game")]
    public void DebugResetGame()
    {
        ResetTimer();
    }
    
    [ContextMenu("Add 30 Seconds")]
    public void DebugAddTime()
    {
        AddTime(30f);
    }
    
    [ContextMenu("Debug Timer State")]
    public void DebugTimerState()
    {
        Debug.Log($"Timer Running: {isTimerRunning}");
        Debug.Log($"Game Started: {gameStarted}");
        Debug.Log($"Current Time: {currentTime}");
        Debug.Log($"Formatted Time: {FormatTime(currentTime)}");
        Debug.Log($"Timer Text Assigned: {timerText != null}");
        Debug.Log($"Main Menu Manager Found: {mainMenuManager != null}");
        
        if (mainMenuManager != null)
        {
            Debug.Log($"Menu Canvas Active: {mainMenuManager.menuCanvas?.activeInHierarchy}");
            Debug.Log($"Joystick Canvas Active: {mainMenuManager.joystickCanvas?.activeInHierarchy}");
        }
    }
    
    // Manual start method that can be called from MainMenu_Manager if needed
    public void ManualGameStart()
    {
        StartGame();
    }
}