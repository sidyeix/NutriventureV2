using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StartingSequenceManager sequenceManager;
    [SerializeField] private GrowAssessmentManager assessmentManager;
    [SerializeField] private ObjectGroupManager[] groupManagers; // All group managers to deactivate

    [Header("Settings")]
    [SerializeField] private bool disableAfterTrigger = true;

    void Start()
    {
        if (sequenceManager == null)
            sequenceManager = FindObjectOfType<StartingSequenceManager>();

        if (assessmentManager == null)
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();

        // Auto-find all group managers
        if (groupManagers == null || groupManagers.Length == 0)
        {
            groupManagers = FindObjectsOfType<ObjectGroupManager>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached end game trigger - Completing assessment");

            // 1. End the grow assessment
            if (assessmentManager != null)
            {
                assessmentManager.EndGrowAssessment();
                Debug.Log("Grow Assessment ended");
            }

            // 2. Deactivate all group managers
            if (groupManagers != null)
            {
                foreach (ObjectGroupManager group in groupManagers)
                {
                    if (group != null)
                    {
                        group.DeactivateGroup();
                        Debug.Log($"Deactivated group: {group.gameObject.name}");
                    }
                }
            }

            // 3. Re-enable all controls, UI, and reset camera priority to 10
            if (sequenceManager != null)
            {
                sequenceManager.EnableAllControlsAndUI();
                Debug.Log("All controls and UI restored, camera priority set to 10");
            }

            // 4. Optional: Add some victory/complete effects here
            PlayCompletionEffects();

            // 5. Disable trigger after use
            if (disableAfterTrigger)
            {
                GetComponent<Collider>().enabled = false;
                Debug.Log("End game trigger disabled");
            }
        }
    }

    private void PlayCompletionEffects()
    {
        // You can add completion effects here:
        // - Play victory sound
        // - Show "Assessment Complete!" text
        // - Trigger fireworks/particles
        // - Update score/achievements

        Debug.Log("Assessment completed successfully!");

        // Example: Play sound if available
        // AudioSource.PlayClipAtPoint(completionSound, transform.position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f); // Orange semi-transparent
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
    }
}