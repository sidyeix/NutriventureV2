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

            // 1. RESET PLAYER SCALE TO (1,1,1)
            ResetPlayerScale();

            // 2. End the grow assessment
            if (assessmentManager != null)
            {
                assessmentManager.EndGrowAssessment();
                Debug.Log("Grow Assessment ended");
            }

            // 3. Deactivate all group managers
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

            // 4. Re-enable all controls, UI, and reset camera priority to 10
            if (sequenceManager != null)
            {
                sequenceManager.EnableAllControlsAndUI();
                Debug.Log("All controls and UI restored, camera priority set to 10");
            }

            // 5. Optional: Add some victory/complete effects here
            PlayCompletionEffects();

            // 6. Disable trigger after use
            if (disableAfterTrigger)
            {
                GetComponent<Collider>().enabled = false;
                Debug.Log("End game trigger disabled");
            }
        }
    }

    private void ResetPlayerScale()
    {
        // Get the GameManager instance
        if (GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.playerArmature != null)
        {
            // Reset scale to (1,1,1)
            GoGrowGlowGameManager.Instance.playerArmature.localScale = Vector3.one;

            // Also reset the targetSize to 1 so it doesn't try to scale back
            // You might need to add a public method in GameManager for this
            // For now, we'll just set it directly if we can access it
            Debug.Log("Player scale reset to (1,1,1)");
        }
        else
        {
            // Alternative: Try to find the player armature directly
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Try common armature names
                Transform armature = player.transform.Find("Armature");
                if (armature == null) armature = player.transform.Find("Character");
                if (armature == null) armature = player.transform; // Use player transform as fallback

                armature.localScale = Vector3.one;
                Debug.Log("Player scale reset to (1,1,1) via direct search");
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

        Debug.Log("Assessment completed successfully! Player scale reset to (1,1,1).");

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