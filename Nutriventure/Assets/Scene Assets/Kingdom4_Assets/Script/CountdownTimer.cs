// GameTimer.cs (UPDATED)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }
    
    [Header("Timer Settings")]
    [SerializeField] private float maxGameTime = 1200f; // 20 minutes max (for 1-star cutoff)
    [SerializeField] private bool startOnAwake = false;
    [SerializeField] private bool connectToGameEndManager = true;
    
    [Header("UI References")]
    [SerializeField] private Image timerIcon; // Optional: visual timer sprite
    [SerializeField] private TextMeshProUGUI timerText; // Optional: elapsed time text
    [SerializeField] private GameObject timerVisuals; // Optional: parent object for all timer visuals
    
    [Header("Timer Events")]
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimerTick; // Called every second
    public UnityEvent OnTimerComplete;
    public UnityEvent OnTimerReset;
    public UnityEvent<float> OnTimeUpdated; // Passes elapsed time
    
    [Header("Star Rating Time Thresholds")]
    [SerializeField] private float threeStarThreshold = 600f;    // 10 minutes = 3 stars
    [SerializeField] private float twoStarThreshold = 900f;      // 15 minutes = 2 stars
    [SerializeField] private float oneStarThreshold = 1200f;     // 20 minutes = 1 star
    
    [Header("Visual Settings")]
    [SerializeField] private Color threeStarColor = Color.green;    // Under 10 min
    [SerializeField] private Color twoStarColor = Color.yellow;     // 10-15 min
    [SerializeField] private Color oneStarColor = Color.red;        // 15-20 min
    [SerializeField] private Color failedColor = Color.gray;        // Over 20 min
    [SerializeField] private bool showStarColors = true;            // Change color based on star rating
    
    [Header("Game Integration")]
    [SerializeField] private bool autoStartOnGameStart = false;
    [SerializeField] private bool pauseOnGamePause = true;
    [SerializeField] private bool stopOnGameEnd = true;
    
    // Timer state
    private float elapsedTime = 0f;
    private bool isTimerActive = false;
    private int lastWholeSecond = -1;
    
    // References
    private Kingdom4GameEndManager gameEndManager;
    private AllerthriaGameManager gameManager;
    
    #region Properties
    public float ElapsedTime => elapsedTime;
    public bool IsActive => isTimerActive;
    public float MaxGameTime => maxGameTime;
    
    // Star rating time checks
    public bool IsUnderThreeStarTime => elapsedTime <= threeStarThreshold;
    public bool IsUnderTwoStarTime => elapsedTime <= twoStarThreshold;
    public bool IsUnderOneStarTime => elapsedTime <= oneStarThreshold;
    public bool IsOverMaxTime => elapsedTime >= maxGameTime;
    
    // Current star rating based on elapsed time
    public int CurrentStarRating
    {
        get
        {
            if (elapsedTime <= threeStarThreshold) return 3;    // Under 10 min
            else if (elapsedTime <= twoStarThreshold) return 2; // 10-15 min
            else if (elapsedTime <= oneStarThreshold) return 1; // 15-20 min
            else return 0;                                      // Over 20 min
        }
    }
    
    public string CurrentStarRatingText
    {
        get
        {
            if (elapsedTime <= threeStarThreshold) return "★★★ (Under 10 min)";
            else if (elapsedTime <= twoStarThreshold) return "★★ (10-15 min)";
            else if (elapsedTime <= oneStarThreshold) return "★ (15-20 min)";
            else return "Time's up! (Over 20 min)";
        }
    }
    #endregion
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        InitializeReferences();
        
        if (startOnAwake)
        {
            StartTimer();
        }
        else
        {
            ResetTimer(false);
        }
    }
    
    void Start()
    {
        if (autoStartOnGameStart && !startOnAwake)
        {
            StartTimer();
        }
    }
    
    void Update()
    {
        if (!isTimerActive) return;
        
        // Count UP (elapsed time)
        elapsedTime += Time.deltaTime;
        
        // Check if a whole second has passed
        int currentWholeSecond = Mathf.FloorToInt(elapsedTime);
        if (currentWholeSecond != lastWholeSecond)
        {
            lastWholeSecond = currentWholeSecond;
            OnTimerTick?.Invoke();
        }
        
        UpdateUI();
        
        // Auto game over if over 20 minutes
        if (elapsedTime >= maxGameTime && connectToGameEndManager && gameEndManager != null)
        {
            TriggerGameOverByTime();
        }
    }
    
    #region Initialization
    private void InitializeReferences()
    {
        // Find GameEndManager for backend connection
        if (connectToGameEndManager)
        {
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
            if (gameEndManager == null)
            {
                Debug.LogWarning("Kingdom4GameEndManager not found! Timer won't connect to backend.");
            }
            else
            {
                Debug.Log("Timer connected to Kingdom4GameEndManager");
            }
        }
        
        // Find GameManager for game state integration
        gameManager = FindObjectOfType<AllerthriaGameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("AllerthriaGameManager not found!");
        }
    }
    #endregion
    
    #region Public Timer Controls
    public void StartTimer()
    {
        if (isTimerActive) return;
        
        isTimerActive = true;
        elapsedTime = 0f; // Start counting from 0
        lastWholeSecond = Mathf.FloorToInt(elapsedTime);
        
        UpdateUI();
        ShowVisuals(true);
        OnTimerStart?.Invoke();
        
        Debug.Log($"Timer started - counting up from 0");
    }
    
    // Start timer from external trigger (like WardenInteraction)
    public void StartTimerFromInteraction()
    {
        if (!isTimerActive)
        {
            StartTimer();
            Debug.Log("Timer started from NPC interaction");
        }
    }
    
    public void PauseTimer()
    {
        if (!isTimerActive) return;
        
        isTimerActive = false;
        Debug.Log("Timer paused");
    }
    
    public void ResumeTimer()
    {
        if (isTimerActive) return;
        
        isTimerActive = true;
        Debug.Log("Timer resumed");
    }
    
    public void StopTimer()
    {
        isTimerActive = false;
        ShowVisuals(false);
        Debug.Log($"Timer stopped at {FormatTime(elapsedTime)}");
    }
    
    public void ResetTimer(bool restart = false)
    {
        elapsedTime = 0f;
        lastWholeSecond = Mathf.FloorToInt(elapsedTime);
        isTimerActive = false;
        
        UpdateUI();
        ShowVisuals(false);
        OnTimerReset?.Invoke();
        
        if (restart)
        {
            StartTimer();
        }
        
        Debug.Log("Timer reset to 0");
    }
    
    public void SetTime(float seconds)
    {
        elapsedTime = Mathf.Max(0f, seconds);
        UpdateUI();
        Debug.Log($"Timer set to {seconds} seconds");
    }
    #endregion
    
    #region Game Integration Methods
    // Check if timer is ready to start
    public bool CanStartTimer()
    {
        return !isTimerActive && elapsedTime == 0f;
    }
    
    // Get formatted elapsed time for display
    public string GetElapsedTimeFormatted()
    {
        return FormatTime(elapsedTime);
    }
    
    // Get star rating based on current time
    public int GetStarRatingForCurrentTime()
    {
        return CurrentStarRating;
    }
    
    // Get star rating for a specific time (for predictions)
    public int GetStarRatingForTime(float timeInSeconds)
    {
        if (timeInSeconds <= threeStarThreshold) return 3;
        else if (timeInSeconds <= twoStarThreshold) return 2;
        else if (timeInSeconds <= oneStarThreshold) return 1;
        else return 0;
    }
    
    // Get time remaining until next star threshold
    public float GetTimeUntilNextThreshold()
    {
        if (elapsedTime <= threeStarThreshold)
            return threeStarThreshold - elapsedTime;
        else if (elapsedTime <= twoStarThreshold)
            return twoStarThreshold - elapsedTime;
        else if (elapsedTime <= oneStarThreshold)
            return oneStarThreshold - elapsedTime;
        else
            return 0f;
    }
    
    // Get which star threshold you're currently in
    public string GetCurrentTimeRange()
    {
        if (elapsedTime <= threeStarThreshold)
            return "Under 10 minutes (3★)";
        else if (elapsedTime <= twoStarThreshold)
            return "10-15 minutes (2★)";
        else if (elapsedTime <= oneStarThreshold)
            return "15-20 minutes (1★)";
        else
            return "Over 20 minutes (0★)";
    }
    #endregion
    
    #region Private Methods
    private void TriggerGameOverByTime()
    {
        if (!connectToGameEndManager || gameEndManager == null) return;
        
        Debug.Log($"Time's up! {FormatTime(elapsedTime)} elapsed - Triggering game over");
        gameEndManager.HandleKingdom4GameOver();
        StopTimer();
    }
    
    private void UpdateUI()
    {
        // Update timer text
        if (timerText != null)
        {
            timerText.text = FormatTime(elapsedTime);
            
            // Change text color based on star rating time
            if (showStarColors)
            {
                if (elapsedTime <= threeStarThreshold)
                {
                    timerText.color = threeStarColor;
                }
                else if (elapsedTime <= twoStarThreshold)
                {
                    timerText.color = twoStarColor;
                }
                else if (elapsedTime <= oneStarThreshold)
                {
                    timerText.color = oneStarColor;
                }
                else
                {
                    timerText.color = failedColor;
                }
            }
        }
        
        // Update timer icon fill (if using filled image)
        if (timerIcon != null && timerIcon.type == Image.Type.Filled)
        {
            // Fill shows progress through 20 minutes
            timerIcon.fillAmount = Mathf.Clamp01(elapsedTime / oneStarThreshold);
            
            // Change icon color based on star rating
            if (showStarColors)
            {
                if (elapsedTime <= threeStarThreshold)
                {
                    timerIcon.color = threeStarColor;
                }
                else if (elapsedTime <= twoStarThreshold)
                {
                    timerIcon.color = twoStarColor;
                }
                else if (elapsedTime <= oneStarThreshold)
                {
                    timerIcon.color = oneStarColor;
                }
                else
                {
                    timerIcon.color = failedColor;
                }
            }
        }
        
        // Fire time updated event for other systems
        OnTimeUpdated?.Invoke(elapsedTime);
    }
    
    private void ShowVisuals(bool show)
    {
        if (timerVisuals != null)
        {
            timerVisuals.SetActive(show);
        }
        else
        {
            // If no parent object, show/hide individual components
            if (timerIcon != null) timerIcon.gameObject.SetActive(show);
            if (timerText != null) timerText.gameObject.SetActive(show);
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    #endregion
    
    #region Utility Methods
    public string GetFormattedTime()
    {
        return FormatTime(elapsedTime);
    }
    
    public TimerData GetTimerData()
    {
        return new TimerData
        {
            elapsedTime = elapsedTime,
            isActive = isTimerActive,
            currentStarRating = CurrentStarRating,
            starRatingText = CurrentStarRatingText
        };
    }
    
    [System.Serializable]
    public struct TimerData
    {
        public float elapsedTime;
        public bool isActive;
        public int currentStarRating;
        public string starRatingText;
        
        public override string ToString()
        {
            return $"Time: {elapsedTime:F1}s, Active: {isActive}, Stars: {currentStarRating} ({starRatingText})";
        }
    }
    #endregion
    
    #region Editor Helper Methods
    [ContextMenu("Start Timer")]
    private void EditorStartTimer()
    {
        StartTimer();
    }
    
    [ContextMenu("Reset Timer")]
    private void EditorResetTimer()
    {
        ResetTimer();
    }
    
    [ContextMenu("Test 3-Star Time (9 min)")]
    private void TestThreeStarTime()
    {
        SetTime(540f); // 9 minutes
        Debug.Log($"Set to 9:00 - Star Rating: {CurrentStarRatingText}");
    }
    
    [ContextMenu("Test 2-Star Time (12 min)")]
    private void TestTwoStarTime()
    {
        SetTime(720f); // 12 minutes
        Debug.Log($"Set to 12:00 - Star Rating: {CurrentStarRatingText}");
    }
    
    [ContextMenu("Test 1-Star Time (18 min)")]
    private void TestOneStarTime()
    {
        SetTime(1080f); // 18 minutes
        Debug.Log($"Set to 18:00 - Star Rating: {CurrentStarRatingText}");
    }
    
    [ContextMenu("Test Game Over Time (21 min)")]
    private void TestGameOverTime()
    {
        SetTime(1260f); // 21 minutes
        Debug.Log($"Set to 21:00 - Star Rating: {CurrentStarRatingText}");
    }
    
    [ContextMenu("Check Timer Status")]
    private void CheckTimerStatus()
    {
        Debug.Log($"Timer Status: Active={IsActive}, Time={GetFormattedTime()}, Stars={CurrentStarRatingText}");
        Debug.Log($"Star Thresholds: 3★ ≤ {threeStarThreshold/60}min, 2★ ≤ {twoStarThreshold/60}min, 1★ ≤ {oneStarThreshold/60}min");
    }
    #endregion
}