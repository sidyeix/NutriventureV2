using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.Collections.Generic;

public class TimelineButtonController : MonoBehaviour
{
    [System.Serializable]
    public class TimelineButton
    {
        [Tooltip("Button that triggers the timeline")]
        public Button button;

        [Tooltip("Timeline to play")]
        public PlayableAsset timelineAsset;

        [Tooltip("Restart timeline from beginning each time")]
        public bool restartFromBeginning = true;

        [Tooltip("Optional: GameObject to activate when timeline plays")]
        public GameObject activateOnPlay;

        [Tooltip("Optional: GameObject to deactivate when timeline ends")]
        public GameObject deactivateOnEnd;

        [Tooltip("Optional: Button to skip this timeline")]
        public Button skipButton;

        [Tooltip("Time (in seconds) to jump to when skipping")]
        public float skipTime = 0f;

        [Tooltip("Skip immediately without confirmation")]
        public bool skipImmediately = true;

        [Tooltip("If skipTime is 0, skip to end instead")]
        public bool skipToEndIfTimeZero = true;
    }

    [Header("Global Playable Director")]
    [SerializeField] private PlayableDirector globalDirector;

    [Header("Timeline Controls")]
    [SerializeField] private TimelineButton[] timelineButtons;

    [Header("Global Settings")]
    [SerializeField] private float activationDelay = 0f;

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipHoldDuration = 1.5f;
    [SerializeField] private bool showSkipPrompt = true;

    [Header("UI References")]
    [SerializeField] private CanvasGroup skipPromptGroup;
    [SerializeField] private Image skipProgressFill;
    [SerializeField] private Button globalSkipButton;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onAnyTimelineStart;
    public UnityEngine.Events.UnityEvent onAnyTimelineEnd;
    public UnityEngine.Events.UnityEvent onTimelineSkipped;

    private TimelineButton currentTimelineButton = null;
    private PlayableAsset currentTimelineAsset = null;
    private bool isPlaying = false;
    private float skipHoldTimer = 0f;
    private bool isHoldingSkip = false;
    private Dictionary<Button, TimelineButton> buttonToTimelineMap = new Dictionary<Button, TimelineButton>();

    void Start()
    {
        if (globalDirector == null)
        {
            Debug.LogError("Global PlayableDirector is not assigned!", this);
            return;
        }

        InitializeButtons();
        InitializeSkipButtons();

        // Set director to not play on awake
        globalDirector.playOnAwake = false;
        globalDirector.stopped += OnTimelineStopped;

        // Initialize skip UI
        if (skipPromptGroup != null)
        {
            skipPromptGroup.alpha = 0f;
            skipPromptGroup.interactable = false;
            skipPromptGroup.blocksRaycasts = false;
        }

        if (skipProgressFill != null)
        {
            skipProgressFill.fillAmount = 0f;
        }

        // Setup global skip button if assigned
        if (globalSkipButton != null)
        {
            globalSkipButton.onClick.AddListener(OnGlobalSkipPressed);
        }
    }

    void InitializeButtons()
    {
        buttonToTimelineMap.Clear();

        foreach (var tButton in timelineButtons)
        {
            if (tButton.button != null && tButton.timelineAsset != null)
            {
                // Map button to timeline
                buttonToTimelineMap[tButton.button] = tButton;

                // Add click listener
                tButton.button.onClick.AddListener(() =>
                {
                    PlayTimeline(tButton);
                });
            }
            else
            {
                Debug.LogWarning("TimelineButtonController: Missing button or timeline asset reference!", this);
            }
        }
    }

    void InitializeSkipButtons()
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.skipButton != null)
            {
                // Clear existing listeners first
                tButton.skipButton.onClick.RemoveAllListeners();

                // Add new listener
                tButton.skipButton.onClick.AddListener(() =>
                {
                    if (allowSkip && isPlaying && currentTimelineButton == tButton)
                    {
                        SkipCurrentTimeline();
                    }
                });
            }
        }
    }

    void Update()
    {
        // Handle skip button hold progress (if using hold-to-skip)
        if (isHoldingSkip && isPlaying && allowSkip)
        {
            skipHoldTimer += Time.deltaTime;

            // Update progress fill
            if (skipProgressFill != null)
            {
                skipProgressFill.fillAmount = skipHoldTimer / skipHoldDuration;
            }

            // Check if hold duration completed
            if (skipHoldTimer >= skipHoldDuration)
            {
                SkipCurrentTimeline();
                ResetSkipProgress();
            }
        }
    }

    void ResetSkipProgress()
    {
        isHoldingSkip = false;
        skipHoldTimer = 0f;

        // Hide skip prompt
        if (skipPromptGroup != null)
        {
            skipPromptGroup.alpha = 0f;
            skipPromptGroup.blocksRaycasts = false;
        }

        // Reset progress fill
        if (skipProgressFill != null)
        {
            skipProgressFill.fillAmount = 0f;
        }
    }

    void OnGlobalSkipPressed()
    {
        if (!allowSkip || !isPlaying || currentTimelineAsset == null) return;

        // Skip immediately without hold requirement
        SkipCurrentTimeline();
    }

    public void PlayTimeline(TimelineButton tButton)
    {
        if (tButton == null || tButton.timelineAsset == null || globalDirector == null) return;

        // Set current timeline button reference
        currentTimelineButton = tButton;
        currentTimelineAsset = tButton.timelineAsset;

        // Start playback with optional delay
        if (activationDelay > 0)
        {
            StartCoroutine(PlayTimelineDelayed(tButton));
        }
        else
        {
            StartTimelinePlayback(tButton);
        }
    }

    private System.Collections.IEnumerator PlayTimelineDelayed(TimelineButton tButton)
    {
        yield return new WaitForSeconds(activationDelay);
        StartTimelinePlayback(tButton);
    }

    private void StartTimelinePlayback(TimelineButton tButton)
    {
        // Stop any currently playing timeline
        if (isPlaying && globalDirector.state == PlayState.Playing)
        {
            globalDirector.Stop();
        }

        // Set the timeline asset
        globalDirector.playableAsset = tButton.timelineAsset;

        // Restart from beginning if configured
        if (tButton.restartFromBeginning)
        {
            globalDirector.time = 0;
            globalDirector.Evaluate();
        }

        // Activate associated GameObject
        if (tButton.activateOnPlay != null && !tButton.activateOnPlay.activeSelf)
        {
            tButton.activateOnPlay.SetActive(true);
        }

        // Disable button during playback
        if (tButton.button != null)
        {
            tButton.button.interactable = false;
        }

        // Disable all other timeline buttons to prevent overlapping
        foreach (var otherButton in timelineButtons)
        {
            if (otherButton.button != null && otherButton.button != tButton.button)
            {
                otherButton.button.interactable = false;
            }
        }

        // Enable skip button if assigned
        if (tButton.skipButton != null)
        {
            tButton.skipButton.gameObject.SetActive(true);
            tButton.skipButton.interactable = true;
            Debug.Log($"Enabled skip button for timeline: {tButton.timelineAsset.name}");
        }

        // Disable all other skip buttons
        foreach (var otherButton in timelineButtons)
        {
            if (otherButton.skipButton != null && otherButton.skipButton != tButton.skipButton)
            {
                otherButton.skipButton.gameObject.SetActive(false);
            }
        }

        // Play the timeline
        globalDirector.Play();
        isPlaying = true;

        Debug.Log($"Started playing timeline: {tButton.timelineAsset.name}");
        Debug.Log($"Skip time set to: {tButton.skipTime} seconds");

        // Invoke start event
        onAnyTimelineStart?.Invoke();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Only process if this is our global director
        if (director != globalDirector) return;

        Debug.Log($"Timeline stopped: {currentTimelineAsset?.name}");

        if (currentTimelineButton != null)
        {
            // Re-enable the button for this timeline
            if (currentTimelineButton.button != null)
            {
                currentTimelineButton.button.interactable = true;
            }

            // Disable skip button
            if (currentTimelineButton.skipButton != null)
            {
                currentTimelineButton.skipButton.gameObject.SetActive(false);
                Debug.Log($"Disabled skip button for timeline: {currentTimelineAsset?.name}");
            }

            // Deactivate associated GameObject
            if (currentTimelineButton.deactivateOnEnd != null && currentTimelineButton.deactivateOnEnd.activeSelf)
            {
                currentTimelineButton.deactivateOnEnd.SetActive(false);
            }
        }

        // Re-enable all timeline buttons
        foreach (var tButton in timelineButtons)
        {
            if (tButton.button != null)
            {
                tButton.button.interactable = true;
            }
        }

        // Reset state
        isPlaying = false;
        currentTimelineButton = null;
        currentTimelineAsset = null;

        // Reset skip UI
        ResetSkipProgress();

        // Invoke end event
        onAnyTimelineEnd?.Invoke();
    }

    public void SkipCurrentTimeline()
    {
        if (!isPlaying || globalDirector == null || currentTimelineButton == null)
        {
            Debug.LogWarning($"Cannot skip: No timeline is playing");
            return;
        }

        var tButton = currentTimelineButton;
        Debug.Log($"Attempting to skip timeline: {currentTimelineAsset?.name}");

        // Get the skip time
        float skipToTime = tButton.skipTime;

        // If skip time is 0 and we should skip to end
        if (skipToTime <= 0 && tButton.skipToEndIfTimeZero)
        {
            skipToTime = (float)globalDirector.duration;
            Debug.Log($"Skipping to end: {skipToTime} seconds");
        }
        else if (skipToTime > 0)
        {
            Debug.Log($"Skipping to time: {skipToTime} seconds");
        }
        else
        {
            Debug.LogWarning($"Invalid skip time: {skipToTime}. Cannot skip.");
            return;
        }

        // Jump to the skip time
        globalDirector.time = skipToTime;
        globalDirector.Evaluate();

        Debug.Log($"Successfully skipped timeline to {skipToTime} seconds!");

        // Invoke skip event
        onTimelineSkipped?.Invoke();

        // Reset skip progress
        ResetSkipProgress();
    }

    // Helper method to convert frames to time
    public float FramesToSeconds(int frames, float frameRate = 60f)
    {
        return frames / frameRate;
    }

    // Helper method to set skip time in frames
    public void SetSkipTimeInFrames(TimelineButton tButton, int frames, float frameRate = 60f)
    {
        tButton.skipTime = FramesToSeconds(frames, frameRate);
    }

    public void StopCurrentTimeline()
    {
        if (globalDirector != null)
        {
            globalDirector.Stop();
        }
    }

    public void PauseCurrentTimeline()
    {
        if (globalDirector != null && isPlaying)
        {
            globalDirector.Pause();
        }
    }

    public void ResumeCurrentTimeline()
    {
        if (globalDirector != null && globalDirector.state == PlayState.Paused)
        {
            globalDirector.Resume();
        }
    }

    public void SkipToTime(float timeInSeconds)
    {
        if (globalDirector != null && isPlaying)
        {
            globalDirector.time = timeInSeconds;
            globalDirector.Evaluate();
        }
    }

    public void SkipToPercentage(float percentage)
    {
        if (globalDirector != null && isPlaying && currentTimelineAsset != null)
        {
            float totalTime = (float)globalDirector.duration;
            float targetTime = totalTime * Mathf.Clamp01(percentage);
            globalDirector.time = targetTime;
            globalDirector.Evaluate();
        }
    }

    public void PlayTimelineByName(string timelineName)
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.timelineAsset != null && tButton.timelineAsset.name == timelineName)
            {
                PlayTimeline(tButton);
                return;
            }
        }

        Debug.LogWarning("No timeline found with name: " + timelineName);
    }

    public void PlayTimelineByIndex(int index)
    {
        if (index >= 0 && index < timelineButtons.Length)
        {
            var tButton = timelineButtons[index];
            if (tButton.timelineAsset != null)
            {
                PlayTimeline(tButton);
            }
        }
        else
        {
            Debug.LogWarning("Invalid timeline index: " + index);
        }
    }

    public bool IsAnyTimelinePlaying()
    {
        return isPlaying;
    }

    public PlayableAsset GetCurrentTimelineAsset()
    {
        return currentTimelineAsset;
    }

    public TimelineButton GetCurrentTimelineButton()
    {
        return currentTimelineButton;
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (globalDirector != null)
        {
            globalDirector.stopped -= OnTimelineStopped;
        }

        foreach (var tButton in timelineButtons)
        {
            if (tButton.button != null)
            {
                tButton.button.onClick.RemoveAllListeners();
            }

            if (tButton.skipButton != null)
            {
                tButton.skipButton.onClick.RemoveAllListeners();
            }
        }

        if (globalSkipButton != null)
        {
            globalSkipButton.onClick.RemoveAllListeners();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-populate global director if not set
        if (globalDirector == null)
        {
            globalDirector = GetComponent<PlayableDirector>();
            if (globalDirector == null)
            {
                globalDirector = FindObjectOfType<PlayableDirector>();
            }
        }
    }
#endif
}