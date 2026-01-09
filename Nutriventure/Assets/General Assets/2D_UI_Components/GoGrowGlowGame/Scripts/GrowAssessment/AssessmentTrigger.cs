using UnityEngine;

public class AssessmentTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimelineSequenceManager timelineManager;
    [SerializeField] private StartingSequenceManager sequenceManager;
    [SerializeField] private GrowAssessmentManager assessmentManager;

    [Header("Settings")]
    [SerializeField] private bool disableAfterTrigger = true;

    private bool hasBeenTriggered = false;

    void Start()
    {
        if (timelineManager == null)
            timelineManager = FindObjectOfType<TimelineSequenceManager>();

        if (sequenceManager == null)
            sequenceManager = FindObjectOfType<StartingSequenceManager>();

        if (assessmentManager == null)
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTriggered && other.CompareTag("Player"))
        {
            Debug.Log("Player entered assessment trigger");

            hasBeenTriggered = true;

            // Start the timeline sequence
            if (timelineManager != null)
            {
                timelineManager.PlayTimelineSequence();
            }
            else
            {
                Debug.LogWarning("No TimelineSequenceManager found!");
                // If no timeline, start assessment directly
                if (assessmentManager != null)
                {
                    assessmentManager.StartGrowAssessment();
                }
            }

            // Start sequence manager if available
            if (sequenceManager != null)
            {
                sequenceManager.StartSequence();
            }

            // DO NOT pause energy - it continues to decrease
            // Set energy to 100 at start of assessment
            if (GoGrowGlowGameManager.Instance != null)
            {
                GoGrowGlowGameManager.Instance.SetEnergy(100f);
                Debug.Log("Energy set to 100 for assessment");
            }

            // Disable trigger if configured
            if (disableAfterTrigger)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f); // Cyan transparent
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
    }
}