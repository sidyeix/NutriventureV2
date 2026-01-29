using UnityEngine;

public class AssessmentTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimelineSequenceManager timelineManager;
    [SerializeField] private StartingSequenceManager sequenceManager;
    [SerializeField] private GrowAssessmentManager assessmentManager;

    [Header("Settings")]
    [SerializeField] private bool disableAfterTrigger = false; // Keep as FALSE so it can be triggered again
    [SerializeField] private bool resetOnGameEnd = true;

    private bool hasBeenTriggered = false;
    private Collider triggerCollider;

    void Start()
    {
        // Get the collider component
        triggerCollider = GetComponent<Collider>();

        if (timelineManager == null)
            timelineManager = FindObjectOfType<TimelineSequenceManager>();

        if (sequenceManager == null)
            sequenceManager = FindObjectOfType<StartingSequenceManager>();

        if (assessmentManager == null)
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Remove the hasBeenTriggered check for the second playthrough
        // Just check if it's the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered assessment trigger - Starting new playthrough");

            // Store this as triggered
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

            // Set energy to 100 at start of assessment
            if (GoGrowGlowGameManager.Instance != null)
            {
                GoGrowGlowGameManager.Instance.SetEnergy(100f);
                Debug.Log("Energy set to 100 for assessment");
            }

            // NEW: Enable the collider just in case
            if (triggerCollider != null && !triggerCollider.enabled)
            {
                triggerCollider.enabled = true;
            }

            // Only disable if explicitly set to (set this to FALSE in inspector)
            if (disableAfterTrigger && triggerCollider != null)
            {
                triggerCollider.enabled = false;
                Debug.Log("Trigger collider disabled after use");
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

    // Reset trigger for next game
    public void ResetTrigger()
    {
        hasBeenTriggered = false;

        if (triggerCollider != null && !triggerCollider.enabled)
        {
            triggerCollider.enabled = true;
            Debug.Log($"AssessmentTrigger {gameObject.name} reset - collider re-enabled");
        }
        else
        {
            Debug.Log($"AssessmentTrigger {gameObject.name} reset");
        }
    }

    // Force reset for new game
    public void ForceResetForNewGame()
    {
        hasBeenTriggered = false;
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
        Debug.Log("Assessment trigger force reset for new game");
    }

    // Call this when assessment completes to prepare for next run
    public void OnAssessmentComplete()
    {
        if (resetOnGameEnd)
        {
            ResetTrigger();
        }
    }

    // Public getter
    public bool HasBeenTriggered() => hasBeenTriggered;
}