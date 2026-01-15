// TimelinePauseManager.cs
using UnityEngine;
using UnityEngine.Playables;

public class TimelinePauseManager1 : MonoBehaviour
{
    // Singleton for easy access
    public static TimelinePauseManager1 Instance;

    [Header("Main Timeline")]
    public PlayableDirector timeline;

    // State
    private bool isPaused = false;

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
        // Mobile touch input option (if you want touch anywhere to continue)
        // if (isPaused && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        // {
        //     ResumeTimeline();
        // }
    }

    // **CALL THIS FROM TIMELINE SIGNAL** 
    public void PauseTimeline()
    {
        if (timeline == null)
        {
            Debug.LogError("No timeline assigned to TimelinePauseManager!");
            return;
        }

        timeline.Pause();
        isPaused = true;

        Debug.Log("Timeline paused at: " + timeline.time);
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