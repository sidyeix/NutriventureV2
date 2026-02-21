using UnityEngine;
using UnityEngine.Playables;

public class TimelinePauseManager1 : MonoBehaviour
{
    public static TimelinePauseManager1 Instance;

    private PlayableDirector currentTimeline;
    private bool isPaused = false;

    void Awake()
    {
        Instance = this;
    }

    // Called from Timeline Signal
    public void PauseTimeline(PlayableDirector director)
    {
        if (director == null)
        {
            Debug.LogError("PauseTimeline called with NULL director!");
            return;
        }

        currentTimeline = director;
        currentTimeline.Pause();
        isPaused = true;

        Debug.Log("⏸️ Paused timeline: " + director.name);
    }

    public void ResumeTimeline()
    {
        if (currentTimeline == null)
        {
            Debug.LogError("No timeline stored to resume!");
            return;
        }

        Debug.Log("▶️ Resuming timeline: " + currentTimeline.name);

        currentTimeline.Play(); // safer than Resume
        isPaused = false;
    }

    public void OnContinueButtonClicked()
    {
        ResumeTimeline();
    }
}
