using UnityEngine;
using UnityEngine.Playables;

public class TimelineSequenceManager : MonoBehaviour
{
    [Header("Timeline Settings")]
    [SerializeField] private PlayableDirector timeline;
    [SerializeField] private float timelineStartDelay = 0.5f;

    [Header("References")]
    [SerializeField] private GrowAssessmentManager growAssessmentManager;
    [SerializeField] private GameObject assessmentTrigger;
    [SerializeField] private StartingSequenceManager sequenceManager;

    private bool timelineCompleted = false;
    private bool isTimelinePlaying = false;

    void Start()
    {
        if (timeline == null)
            timeline = GetComponent<PlayableDirector>();

        if (timeline != null)
        {
            timeline.stopped += OnTimelineFinished;
        }
    }

    public void PlayTimelineSequence()
    {
        if (isTimelinePlaying || timelineCompleted) return;

        #if UNITY_EDITOR
        Debug.Log("Starting timeline sequence...");
        #endif

        // DO NOT pause energy - it continues to decrease
        // Set energy to 100 at start
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.SetEnergy(100f);
            #if UNITY_EDITOR
            Debug.Log("Energy set to 100 for timeline sequence");
            #endif
        }

        // Start timeline after delay
        Invoke(nameof(StartTimeline), timelineStartDelay);

        isTimelinePlaying = true;
    }

    private void StartTimeline()
    {
        if (timeline != null)
        {
            timeline.Play();
            #if UNITY_EDITOR
            Debug.Log("Timeline playing...");
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning("No timeline assigned, proceeding immediately");
            #endif
            OnTimelineFinished(null);
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        timelineCompleted = true;
        isTimelinePlaying = false;

        #if UNITY_EDITOR
        Debug.Log("Timeline finished. Starting Grow Assessment sequence...");
        #endif

        // Enable Grow Assessment system
        if (growAssessmentManager != null)
        {
            growAssessmentManager.StartGrowAssessment();
        }

        // Enable the assessment trigger for player interaction
        if (assessmentTrigger != null)
        {
            assessmentTrigger.SetActive(true);
            #if UNITY_EDITOR
            Debug.Log("Grow Assessment trigger enabled");
            #endif
        }
    }

    // NEW: Reset timeline for next playthrough
    public void ResetTimeline()
    {
        #if UNITY_EDITOR
        Debug.Log("Resetting timeline for new game...");
        #endif

        // Stop timeline if playing
        if (isTimelinePlaying && timeline != null)
        {
            timeline.Stop();
        }

        // Reset timeline to start
        if (timeline != null)
        {
            timeline.time = 0;
            timeline.Evaluate();
        }

        // Reset flags
        timelineCompleted = false;
        isTimelinePlaying = false;

        #if UNITY_EDITOR
        Debug.Log("Timeline reset complete");
        #endif
    }

    public bool IsTimelineComplete()
    {
        return timelineCompleted;
    }

    public bool IsTimelinePlaying()
    {
        return isTimelinePlaying;
    }
}
