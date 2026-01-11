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

        Debug.Log("Starting timeline sequence...");

        // DO NOT pause energy - it continues to decrease
        // Set energy to 100 at start
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.SetEnergy(100f);
            Debug.Log("Energy set to 100 for timeline sequence");
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
            Debug.Log("Timeline playing...");
        }
        else
        {
            Debug.LogWarning("No timeline assigned, proceeding immediately");
            OnTimelineFinished(null);
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        timelineCompleted = true;
        isTimelinePlaying = false;

        Debug.Log("Timeline finished. Starting Grow Assessment sequence...");

        // Enable Grow Assessment system
        if (growAssessmentManager != null)
        {
            growAssessmentManager.StartGrowAssessment();
        }

        // Enable the assessment trigger for player interaction
        if (assessmentTrigger != null)
        {
            assessmentTrigger.SetActive(true);
            Debug.Log("Grow Assessment trigger enabled");
        }
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