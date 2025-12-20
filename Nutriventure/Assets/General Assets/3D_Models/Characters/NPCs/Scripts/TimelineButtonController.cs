using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineButtonController : MonoBehaviour
{
    [System.Serializable]
    public class TimelineButton
    {
        [Tooltip("Button that triggers the timeline")]
        public Button button;

        [Tooltip("Playable Director to control")]
        public PlayableDirector director;

        [Tooltip("Optional: Timeline to play (if different from director's default)")]
        public PlayableAsset timelineAsset;

        [Tooltip("Restart timeline from beginning each time")]
        public bool restartFromBeginning = true;

        [Tooltip("Stop previous timeline before playing this one")]
        public bool stopPreviousTimeline = true;

        [Tooltip("Disable button while timeline is playing")]
        public bool disableDuringPlayback = true;

        [Tooltip("Optional: GameObject to activate when timeline plays")]
        public GameObject activateOnPlay;

        [Tooltip("Optional: GameObject to deactivate when timeline ends")]
        public GameObject deactivateOnEnd;

        [Tooltip("Optional: Button to skip this timeline")]
        public Button skipButton;

        [Tooltip("Signal name to look for in timeline to skip to")]
        public string skipSignalName = "SkipPoint";

        [Tooltip("If no signal found, skip to this time (seconds)")]
        public float fallbackSkipTime = 0f;

        [Tooltip("Skip immediately without confirmation")]
        public bool skipImmediately = true;
    }

    [Header("Timeline Controls")]
    [SerializeField] private TimelineButton[] timelineButtons;

    [Header("Global Settings")]
    [SerializeField] private bool stopAllOnPlay = false;
    [SerializeField] private float activationDelay = 0f;

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipHoldDuration = 1.5f;
    [SerializeField] private bool showSkipPrompt = true;
    [SerializeField] private bool skipToEndIfNoSignal = true;

    [Header("UI References")]
    [SerializeField] private CanvasGroup skipPromptGroup;
    [SerializeField] private Image skipProgressFill;
    [SerializeField] private Button globalSkipButton;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onAnyTimelineStart;
    public UnityEngine.Events.UnityEvent onAnyTimelineEnd;
    public UnityEngine.Events.UnityEvent onTimelineSkipped;

    private PlayableDirector currentlyPlaying = null;
    private TimelineButton currentTimelineButton = null;
    private bool isPlaying = false;
    private float skipHoldTimer = 0f;
    private bool isHoldingSkip = false;

    void Start()
    {
        InitializeButtons();
        InitializeSkipButtons();

        // Set all directors to not play on awake
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director != null)
            {
                tButton.director.playOnAwake = false;
                tButton.director.stopped += OnTimelineStopped;
            }
        }

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
        foreach (var tButton in timelineButtons)
        {
            if (tButton.button != null && tButton.director != null)
            {
                // Store reference to avoid closure issues
                PlayableDirector directorRef = tButton.director;
                TimelineButton tButtonRef = tButton;

                tButton.button.onClick.AddListener(() =>
                {
                    PlayTimeline(directorRef, tButtonRef);
                });
            }
            else
            {
                Debug.LogWarning("TimelineButtonController: Missing button or director reference!", this);
            }
        }
    }

    void InitializeSkipButtons()
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.skipButton != null)
            {
                // IMPORTANT: Clear existing listeners first
                tButton.skipButton.onClick.RemoveAllListeners();

                TimelineButton tButtonRef = tButton;

                // Add new listener
                tButton.skipButton.onClick.AddListener(() =>
                {
                    Debug.Log($"Skip button clicked for timeline: {tButtonRef.director?.name}");
                    Debug.Log($"Allow skip: {allowSkip}, Is playing: {isPlaying}, Current director: {currentlyPlaying?.name}");

                    if (allowSkip && isPlaying)
                    {
                        // Check if this skip button belongs to the currently playing timeline
                        bool isCurrentTimeline = (currentlyPlaying == tButtonRef.director);
                        Debug.Log($"Is current timeline: {isCurrentTimeline}");

                        if (isCurrentTimeline)
                        {
                            SkipCurrentTimeline();
                        }
                        else
                        {
                            Debug.LogWarning("Skip button clicked but it's not for the currently playing timeline!");
                        }
                    }
                    else
                    {
                        if (!allowSkip) Debug.LogWarning("Skipping is not allowed!");
                        if (!isPlaying) Debug.LogWarning("No timeline is currently playing!");
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

    void OnSkipButtonPressed(TimelineButton tButton)
    {
        if (!allowSkip || !isPlaying || currentlyPlaying != tButton.director) return;

        isHoldingSkip = true;
        skipHoldTimer = 0f;

        // Show skip prompt
        if (showSkipPrompt && skipPromptGroup != null)
        {
            skipPromptGroup.alpha = 1f;
            skipPromptGroup.blocksRaycasts = true;
        }
    }

    void OnSkipButtonReleased()
    {
        if (!isHoldingSkip) return;

        ResetSkipProgress();
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
        if (!allowSkip || !isPlaying || currentlyPlaying == null) return;

        // Skip immediately without hold requirement
        SkipCurrentTimeline();
    }

    public void PlayTimeline(PlayableDirector director, TimelineButton tButton = null)
    {
        if (director == null) return;

        // Find the timeline button if not provided
        if (tButton == null)
        {
            foreach (var tb in timelineButtons)
            {
                if (tb.director == director)
                {
                    tButton = tb;
                    break;
                }
            }

            if (tButton == null)
            {
                Debug.LogWarning("No TimelineButton found for director: " + director.name);
                return;
            }
        }

        // Set current timeline button reference
        currentTimelineButton = tButton;

        // Start playback with optional delay
        if (activationDelay > 0)
        {
            StartCoroutine(PlayTimelineDelayed(director, tButton));
        }
        else
        {
            StartTimelinePlayback(director, tButton);
        }
    }

    private System.Collections.IEnumerator PlayTimelineDelayed(PlayableDirector director, TimelineButton tButton)
    {
        yield return new WaitForSeconds(activationDelay);
        StartTimelinePlayback(director, tButton);
    }

    private void StartTimelinePlayback(PlayableDirector director, TimelineButton tButton)
    {
        // Stop all timelines if configured
        if (stopAllOnPlay)
        {
            StopAllTimelines();
        }
        else if (tButton.stopPreviousTimeline && currentlyPlaying != null)
        {
            currentlyPlaying.Stop();
        }

        // Set the timeline asset if specified
        if (tButton.timelineAsset != null)
        {
            director.playableAsset = tButton.timelineAsset;
        }

        // Restart from beginning if configured
        if (tButton.restartFromBeginning)
        {
            director.time = 0;
            director.Evaluate();
        }

        // Activate associated GameObject
        if (tButton.activateOnPlay != null && !tButton.activateOnPlay.activeSelf)
        {
            tButton.activateOnPlay.SetActive(true);
        }

        // Disable button during playback if configured
        if (tButton.disableDuringPlayback && tButton.button != null)
        {
            tButton.button.interactable = false;
        }

        // Enable skip button if assigned
        if (tButton.skipButton != null)
        {
            tButton.skipButton.gameObject.SetActive(true);
            tButton.skipButton.interactable = true;
            Debug.Log($"Enabled skip button for timeline: {director.name}");
        }

        // Play the timeline
        director.Play();
        currentlyPlaying = director;
        isPlaying = true;

        Debug.Log($"Started playing timeline: {director.name}");
        Debug.Log($"Current timeline button skip signal: {tButton.skipSignalName}");

        // Invoke start event
        onAnyTimelineStart?.Invoke();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Only process if this is the currently playing timeline
        if (director != currentlyPlaying) return;

        Debug.Log($"Timeline stopped: {director.name}");

        isPlaying = false;
        currentlyPlaying = null;
        currentTimelineButton = null;

        // Find the corresponding TimelineButton
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director == director)
            {
                // Re-enable button if it was disabled
                if (tButton.disableDuringPlayback && tButton.button != null)
                {
                    tButton.button.interactable = true;
                }

                // Disable skip button
                if (tButton.skipButton != null)
                {
                    tButton.skipButton.gameObject.SetActive(false);
                    Debug.Log($"Disabled skip button for timeline: {director.name}");
                }

                // Deactivate associated GameObject
                if (tButton.deactivateOnEnd != null && tButton.deactivateOnEnd.activeSelf)
                {
                    tButton.deactivateOnEnd.SetActive(false);
                }

                break;
            }
        }

        // Reset skip UI
        ResetSkipProgress();

        // Invoke end event
        onAnyTimelineEnd?.Invoke();
    }

    public void SkipCurrentTimeline()
    {
        if (!isPlaying || currentlyPlaying == null || currentTimelineButton == null)
        {
            Debug.LogWarning($"Cannot skip: isPlaying={isPlaying}, currentlyPlaying={currentlyPlaying?.name}, currentTimelineButton={currentTimelineButton?.director?.name}");
            return;
        }

        var tButton = currentTimelineButton;
        Debug.Log($"Attempting to skip timeline: {currentlyPlaying.name}");
        Debug.Log($"Looking for signal: {tButton.skipSignalName}");

        bool skipped = false;

        // Try to find and jump to the signal in the timeline
        if (!string.IsNullOrEmpty(tButton.skipSignalName))
        {
            Debug.Log($"Searching for signal: {tButton.skipSignalName}");
            skipped = JumpToSignalInTimeline(tButton.skipSignalName);
        }

        // If no signal found, use fallback
        if (!skipped)
        {
            Debug.Log($"Signal not found, using fallback");
            // Use fallback skip time
            if (tButton.fallbackSkipTime > 0)
            {
                Debug.Log($"Skipping to fallback time: {tButton.fallbackSkipTime}");
                currentlyPlaying.time = tButton.fallbackSkipTime;
                currentlyPlaying.Evaluate();
                skipped = true;
            }
            // Or skip to end if configured
            else if (skipToEndIfNoSignal)
            {
                Debug.Log($"Skipping to end: {currentlyPlaying.duration}");
                currentlyPlaying.time = currentlyPlaying.duration;
                currentlyPlaying.Evaluate();
                // Stop immediately since we're at the end
                currentlyPlaying.Stop();
                skipped = true;
            }
        }

        if (skipped)
        {
            Debug.Log($"Successfully skipped timeline!");
            // Invoke skip event
            onTimelineSkipped?.Invoke();

            // Reset skip progress
            ResetSkipProgress();
        }
        else
        {
            Debug.LogWarning("Failed to skip timeline!");
        }
    }

    private bool JumpToSignalInTimeline(string signalName)
    {
        if (currentlyPlaying == null || currentlyPlaying.playableAsset == null)
        {
            Debug.LogWarning("Cannot jump to signal: No currently playing timeline or asset");
            return false;
        }

        // Try to get the timeline asset
        var timelineAsset = currentlyPlaying.playableAsset as TimelineAsset;
        if (timelineAsset == null)
        {
            Debug.LogWarning("Cannot jump to signal: Playable asset is not a TimelineAsset");
            return false;
        }

        Debug.Log($"Searching in timeline: {timelineAsset.name}");

        // Search through all tracks for signal tracks
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            var signalTrack = track as SignalTrack;
            if (signalTrack != null)
            {
                Debug.Log($"Found signal track: {signalTrack.name}");
                // Get all markers on this track
                var markers = signalTrack.GetMarkers();
                foreach (var marker in markers)
                {
                    var signalMarker = marker as SignalEmitter;
                    if (signalMarker != null)
                    {
                        Debug.Log($"Found signal marker: {signalMarker.name} at time: {marker.time}");
                        if (signalMarker.name == signalName)
                        {
                            // Found our signal! Jump to this time
                            Debug.Log($"Found matching signal! Jumping to time: {marker.time}");
                            currentlyPlaying.time = marker.time;
                            currentlyPlaying.Evaluate();
                            return true;
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"Signal '{signalName}' not found in any track of timeline");
        return false;
    }

    public bool HasSkipSignal(string timelineName, string signalName = "SkipPoint")
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director != null && tButton.director.name == timelineName)
            {
                if (tButton.director.playableAsset is TimelineAsset timelineAsset)
                {
                    return FindSignalInTimeline(timelineAsset, signalName);
                }
            }
        }
        return false;
    }

    private bool FindSignalInTimeline(TimelineAsset timelineAsset, string signalName)
    {
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            var signalTrack = track as SignalTrack;
            if (signalTrack != null)
            {
                foreach (var marker in signalTrack.GetMarkers())
                {
                    var signalMarker = marker as SignalEmitter;
                    if (signalMarker != null && signalMarker.name == signalName)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void StopAllTimelines()
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director != null && tButton.director.state == PlayState.Playing)
            {
                tButton.director.Stop();

                // Re-enable any disabled buttons
                if (tButton.button != null)
                {
                    tButton.button.interactable = true;
                }

                // Disable skip buttons
                if (tButton.skipButton != null)
                {
                    tButton.skipButton.gameObject.SetActive(false);
                }
            }
        }

        currentlyPlaying = null;
        currentTimelineButton = null;
        isPlaying = false;

        // Reset skip UI
        ResetSkipProgress();
    }

    public void PauseCurrentTimeline()
    {
        if (currentlyPlaying != null)
        {
            currentlyPlaying.Pause();
        }
    }

    public void ResumeCurrentTimeline()
    {
        if (currentlyPlaying != null && currentlyPlaying.state == PlayState.Paused)
        {
            currentlyPlaying.Resume();
        }
    }

    public void SkipToTime(float timeInSeconds)
    {
        if (currentlyPlaying != null)
        {
            currentlyPlaying.time = timeInSeconds;
            currentlyPlaying.Evaluate();
        }
    }

    public void SkipToPercentage(float percentage)
    {
        if (currentlyPlaying != null && currentlyPlaying.playableAsset != null)
        {
            float totalTime = (float)currentlyPlaying.duration;
            float targetTime = totalTime * Mathf.Clamp01(percentage);
            currentlyPlaying.time = targetTime;
            currentlyPlaying.Evaluate();
        }
    }

    public void SkipToSignal(string signalName)
    {
        if (currentlyPlaying != null)
        {
            JumpToSignalInTimeline(signalName);
        }
    }

    public void PlayTimelineByName(string timelineName)
    {
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director != null && tButton.director.name == timelineName)
            {
                PlayTimeline(tButton.director, tButton);
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
            if (tButton.director != null)
            {
                PlayTimeline(tButton.director, tButton);
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

    public PlayableDirector GetCurrentlyPlaying()
    {
        return currentlyPlaying;
    }

    public TimelineButton GetCurrentTimelineButton()
    {
        return currentTimelineButton;
    }

    void OnDestroy()
    {
        // Clean up event listeners
        foreach (var tButton in timelineButtons)
        {
            if (tButton.director != null)
            {
                tButton.director.stopped -= OnTimelineStopped;
            }

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
        // Auto-populate director if button has one in children
        foreach (var tButton in timelineButtons)
        {
            if (tButton.button != null && tButton.director == null)
            {
                tButton.director = tButton.button.GetComponentInParent<PlayableDirector>();
            }
        }
    }
#endif
}