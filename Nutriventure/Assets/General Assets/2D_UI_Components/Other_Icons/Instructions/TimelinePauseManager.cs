// TimelinePauseManager.cs
using UnityEngine;
using UnityEngine.Playables;

public class TimelinePauseManager : MonoBehaviour
{
    // Singleton for easy access
    public static TimelinePauseManager Instance;

    [Header("Main Timeline")]
    public PlayableDirector timeline;

    // State
    private bool isPaused = false;

    // Skip protection: any PauseTimeline call while timeline.time <= this value
    // is silently ignored.  Set by TimelineSkipButton, auto-cleared in Update()
    // once the director advances past the threshold.
    private double ignoreSignalsUpToTime = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Auto-clear the ignore threshold once the timeline has moved past it.
        // The 0.05 buffer accounts for floating-point jitter.
        if (ignoreSignalsUpToTime >= 0 && timeline != null &&
            timeline.state == PlayState.Playing &&
            timeline.time > ignoreSignalsUpToTime + 0.05)
        {
            Debug.Log($"Skip protection cleared — timeline advanced to {timeline.time:F2} (threshold was {ignoreSignalsUpToTime:F2})");
            ignoreSignalsUpToTime = -1;
        }
    }

    // **CALL THIS FROM TIMELINE SIGNAL** 
    public void PauseTimeline()
    {
        if (timeline == null)
        {
            Debug.LogError("No timeline assigned to TimelinePauseManager!");
            return;
        }

        // --- Skip protection ---
        // If we recently skipped, ignore any pause signal that fires while the
        // timeline hasn't yet advanced past the skip destination.
        if (ignoreSignalsUpToTime >= 0)
        {
            if (timeline.time <= ignoreSignalsUpToTime + 0.05)
            {
                Debug.Log($"PauseTimeline IGNORED (skip protection active) at: {timeline.time:F2}  threshold: {ignoreSignalsUpToTime:F2}");
                return;
            }
            else
            {
                // Timeline has moved past the skip point — clear protection
                // and fall through to pause normally.
                ignoreSignalsUpToTime = -1;
            }
        }

        timeline.Pause();
        isPaused = true;

        Debug.Log("Timeline paused at: " + timeline.time);
    }

    /// <summary>
    /// Called by TimelineSkipButton.  Any PauseTimeline() call whose
    /// timeline.time &lt;= <paramref name="time"/> + tiny buffer will be ignored.
    /// Automatically cleared in Update() once the director advances past it.
    /// </summary>
    public void SetIgnoreSignalsUpToTime(double time)
    {
        ignoreSignalsUpToTime = time;
        Debug.Log($"Skip protection SET — ignore pause signals up to {time:F2}s");
    }

    /// <summary>
    /// Force-clear the skip protection (safety net).
    /// </summary>
    public void ClearSkipProtection()
    {
        ignoreSignalsUpToTime = -1;
    }

    // **CALL THIS TO CONTINUE (from button or trigger)**
    public void ResumeTimeline()
    {
        if (!isPaused || timeline == null)
        {
            Debug.LogWarning("Cannot resume: Timeline not paused or null!");
            return;
        }

        timeline.Resume();
        isPaused = false;

        Debug.Log("Timeline resumed");
    }

    // **For UI Button - attach this method to button's OnClick**
    public void OnContinueButtonClicked()
    {
        ResumeTimeline();
    }

    // Helper method to check if timeline is paused
    public bool IsTimelinePaused()
    {
        return isPaused;
    }

}