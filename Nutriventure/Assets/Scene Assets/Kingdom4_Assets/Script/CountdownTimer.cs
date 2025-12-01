using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float duration = 120f; // Default: 2 minutes
    [SerializeField] private bool startOnAwake = false;
    [SerializeField] private bool loopTimer = false;
    
    [Header("UI References")]
    [SerializeField] private Image timerIcon; // Optional: visual timer sprite
    [SerializeField] private TextMeshProUGUI timerText; // Optional: countdown text
    [SerializeField] private GameObject timerVisuals; // Optional: parent object for all timer visuals
    
    [Header("Timer Events")]
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimerTick; // Called every second
    public UnityEvent OnTimerComplete;
    public UnityEvent OnTimerReset;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow; // For last 30 seconds
    [SerializeField] private Color criticalColor = Color.red; // For last 10 seconds
    [SerializeField] private float warningThreshold = 30f; // Switch to warning color at 30s
    [SerializeField] private float criticalThreshold = 10f; // Switch to critical color at 10s
    
    private float currentTime = 0f;
    private bool isTimerActive = false;
    private int lastWholeSecond = -1;
    
    #region Properties
    public float CurrentTime => currentTime;
    public bool IsActive => isTimerActive;
    public float Progress => Mathf.Clamp01(1f - (currentTime / duration));
    public bool IsComplete => currentTime <= 0f && isTimerActive;
    
    public float Duration
    {
        get => duration;
        set
        {
            duration = Mathf.Max(0f, value);
            if (isTimerActive)
            {
                currentTime = Mathf.Min(currentTime, duration);
            }
        }
    }
    #endregion
    
    void Awake()
    {
        if (startOnAwake)
        {
            StartTimer();
        }
        else
        {
            ResetTimer(false);
        }
    }
    
    void Update()
    {
        if (!isTimerActive) return;
        
        currentTime -= Time.deltaTime;
        
        // Check if a whole second has passed
        int currentWholeSecond = Mathf.CeilToInt(currentTime);
        if (currentWholeSecond != lastWholeSecond)
        {
            lastWholeSecond = currentWholeSecond;
            OnTimerTick?.Invoke();
        }
        
        UpdateUI();
        
        if (currentTime <= 0f)
        {
            CompleteTimer();
        }
    }
    
    #region Public Timer Controls
    public void StartTimer()
    {
        if (isTimerActive) return;
        
        isTimerActive = true;
        currentTime = duration;
        lastWholeSecond = Mathf.CeilToInt(currentTime);
        
        UpdateUI();
        ShowVisuals(true);
        OnTimerStart?.Invoke();
    }
    
    public void StartTimer(float customDuration)
    {
        duration = Mathf.Max(0f, customDuration);
        StartTimer();
    }
    
    public void PauseTimer()
    {
        isTimerActive = false;
    }
    
    public void ResumeTimer()
    {
        if (currentTime > 0f)
        {
            isTimerActive = true;
        }
    }
    
    public void StopTimer()
    {
        isTimerActive = false;
        ShowVisuals(false);
    }
    
    public void ResetTimer(bool restart = false)
    {
        currentTime = duration;
        lastWholeSecond = Mathf.CeilToInt(currentTime);
        isTimerActive = false;
        
        UpdateUI();
        ShowVisuals(false);
        OnTimerReset?.Invoke();
        
        if (restart)
        {
            StartTimer();
        }
    }
    
    public void AddTime(float seconds)
    {
        currentTime += seconds;
        UpdateUI();
    }
    
    public void SubtractTime(float seconds)
    {
        currentTime = Mathf.Max(0f, currentTime - seconds);
        UpdateUI();
    }
    
    public void SetTime(float seconds)
    {
        currentTime = Mathf.Clamp(seconds, 0f, duration);
        UpdateUI();
    }
    #endregion
    
    #region Private Methods
    private void CompleteTimer()
    {
        currentTime = 0f;
        isTimerActive = false;
        
        // Update UI one last time
        if (timerText != null)
        {
            timerText.text = "00:00";
        }
        
        // Set icon to complete state if using filled image
        if (timerIcon != null && timerIcon.type == Image.Type.Filled)
        {
            timerIcon.fillAmount = 1f;
        }
        
        OnTimerComplete?.Invoke();
        
        if (loopTimer)
        {
            ResetTimer(true);
        }
        else
        {
            ShowVisuals(false);
        }
    }
    
    private void UpdateUI()
    {
        // Update timer text
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTime);
            
            // Change text color based on time remaining
            if (currentTime <= criticalThreshold)
            {
                timerText.color = criticalColor;
            }
            else if (currentTime <= warningThreshold)
            {
                timerText.color = warningColor;
            }
            else
            {
                timerText.color = normalColor;
            }
        }
        
        // Update timer icon fill (if using filled image)
        if (timerIcon != null && timerIcon.type == Image.Type.Filled)
        {
            timerIcon.fillAmount = Progress;
            
            // Change icon color based on time remaining
            if (currentTime <= criticalThreshold)
            {
                timerIcon.color = criticalColor;
            }
            else if (currentTime <= warningThreshold)
            {
                timerIcon.color = warningColor;
            }
        }
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
    #endregion
    
    #region Utility Methods
    public string GetFormattedTime()
    {
        return FormatTime(currentTime);
    }
    
    public string GetFormattedTimeRemaining()
    {
        return FormatTime(Mathf.Max(0f, currentTime));
    }
    
    private string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0f) return "00:00";
        
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    public string GetFormattedTimeDetailed()
    {
        if (currentTime <= 0f) return "00:00.0";
        
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        int milliseconds = Mathf.FloorToInt((currentTime % 1) * 10);
        return string.Format("{0:00}:{1:00}.{2:0}", minutes, seconds, milliseconds);
    }
    #endregion
    
    #region Editor Helper Methods
    // Called from editor buttons if needed
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
    
    [ContextMenu("Complete Timer")]
    private void EditorCompleteTimer()
    {
        currentTime = 0.1f; // Small value to trigger completion
    }
    #endregion
}