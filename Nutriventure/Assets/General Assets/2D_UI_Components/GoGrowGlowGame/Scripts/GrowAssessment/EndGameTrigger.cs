using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StartingSequenceManager sequenceManager;
    [SerializeField] private GrowAssessmentManager assessmentManager;
    [SerializeField] private ObjectGroupManager[] groupManagers;
    [SerializeField] private AssessmentTrigger assessmentTrigger;

    [Header("End Sequence Settings")]
    [SerializeField] private bool useSmoothCameraTransition = true;
    [SerializeField] private float cameraTransitionDelay = 0.5f;
    [SerializeField] private bool disableAfterTrigger = true;

    [Header("Audio")]
    [SerializeField] private AudioClip completionSound;
    private AudioSource audioSource;

    void Start()
    {
        // Find references if not assigned
        if (sequenceManager == null)
        {
            sequenceManager = FindObjectOfType<StartingSequenceManager>();
            if (sequenceManager == null)
                Debug.LogWarning("StartingSequenceManager not found! Camera transitions may not work.");
        }

        if (assessmentManager == null)
        {
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();
            if (assessmentManager == null)
                Debug.LogError("GrowAssessmentManager not found! Cannot check completion status.");
        }

        // Auto-find all group managers if not assigned
        if (groupManagers == null || groupManagers.Length == 0)
        {
            groupManagers = FindObjectsOfType<ObjectGroupManager>();
            Debug.Log($"Auto-found {groupManagers.Length} group managers");
        }

        // Find assessment trigger if not assigned
        if (assessmentTrigger == null)
        {
            assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
            if (assessmentTrigger == null)
                Debug.LogWarning("AssessmentTrigger not found.");
        }

        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player reached end game trigger - Completing GROW ASSESSMENT ONLY");

            // Check if assessment manager exists
            if (assessmentManager == null)
            {
                Debug.LogError("Assessment Manager not found! Cannot check completion status.");
                return;
            }

            // Check if player has completed all questions AND is waiting for end trigger
            bool hasCompletedQuestions = assessmentManager.HasCompletedAllQuestions();
            bool isWaitingForEnd = assessmentManager.IsWaitingForEndTrigger();
            bool isAssessmentActive = assessmentManager.IsAssessmentActive();

            Debug.Log($"EndGameTrigger Check - Completed: {hasCompletedQuestions}, Waiting: {isWaitingForEnd}, Active: {isAssessmentActive}");

            if (!hasCompletedQuestions)
            {
                Debug.LogWarning("Player hasn't completed all questions yet! Trigger ignored.");

                // Optional: Show message to player
                if (isAssessmentActive)
                {
                    Debug.Log("Assessment is still active. Complete all questions first!");
                }
                return;
            }

            if (!isWaitingForEnd)
            {
                Debug.LogWarning("Assessment is not in 'waiting for end' state! Trigger ignored.");
                return;
            }

            Debug.Log("Player has completed all questions and is waiting for end trigger. Completing assessment...");

            // Start the end sequence
            StartCoroutine(EndGrowAssessmentSequence());
        }
    }

    private System.Collections.IEnumerator EndGrowAssessmentSequence()
    {
        // 1. Reset player scale
        ResetPlayerScale();

        // 2. End grow assessment (this will hide UI and deactivate groups)
        if (assessmentManager != null)
        {
            assessmentManager.EndGrowAssessment();
            Debug.Log("Grow Assessment ended");
        }
        else
        {
            Debug.LogError("Assessment Manager is null! Cannot end assessment.");
        }

        // NEW: ADD THIS CRITICAL RESET FOR SECOND PLAYTHROUGH
        // Completely reset the assessment manager for the next playthrough
        if (assessmentManager != null)
        {
            // Call the complete reset method that clears everything
            assessmentManager.CompleteResetForNewGame();
            Debug.Log("Assessment Manager completely reset for new game");
        }

        // 3. Reset all group managers
        if (groupManagers != null && groupManagers.Length > 0)
        {
            foreach (ObjectGroupManager group in groupManagers)
            {
                if (group != null)
                {
                    group.ResetGroupForNewGame();
                    Debug.Log($"Reset group: {group.gameObject.name}");
                }
            }
            Debug.Log($"Reset {groupManagers.Length} group managers");
        }
        else
        {
            Debug.LogWarning("No group managers assigned or found. Skipping group reset.");
        }

        // 4. Wait a bit before camera transition (optional)
        if (cameraTransitionDelay > 0)
        {
            Debug.Log($"Waiting {cameraTransitionDelay}s before camera transition...");
            yield return new WaitForSeconds(cameraTransitionDelay);
        }

        // 5. Re-enable controls and UI (with smooth camera transition back to normal)
        if (sequenceManager != null)
        {
            if (useSmoothCameraTransition)
            {
                // Use the smooth transition method
                sequenceManager.EnableAllControlsAndUI();
                Debug.Log("All controls and UI restored - Smooth camera transition started");
            }
            else
            {
                // Force instant camera reset
                sequenceManager.ForceCameraReset();
                sequenceManager.EnablePlayerInput();
                Debug.Log("All controls and UI restored - Instant camera reset");
            }
        }
        else
        {
            Debug.LogError("Starting Sequence Manager not found! Camera and controls may not reset properly.");
        }

        // 6. Reset assessment trigger for next run
        if (assessmentTrigger != null)
        {
            // Use the force reset method to ensure it's fully reset
            assessmentTrigger.ForceResetForNewGame();
            Debug.Log("Assessment trigger force reset for next game");
        }
        else
        {
            Debug.LogWarning("Assessment Trigger not found. Skipping trigger reset.");
        }

        // 7. Reset timeline for next playthrough
        TimelineSequenceManager timelineManager = FindObjectOfType<TimelineSequenceManager>();
        if (timelineManager != null)
        {
            timelineManager.ResetTimeline();
            Debug.Log("Timeline reset for next playthrough");
        }

        // 8. Play completion sound if available
        if (completionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(completionSound);
            Debug.Log("Played completion sound");
        }

        // 9. Disable this trigger if set to
        if (disableAfterTrigger)
        {
            Collider thisCollider = GetComponent<Collider>();
            if (thisCollider != null)
            {
                thisCollider.enabled = false;
                Debug.Log("End game trigger disabled");
            }
        }

        // NEW: Enable the EndGameTrigger collider for the next playthrough
        // This ensures it can be triggered again
        if (disableAfterTrigger)
        {
            // Wait a moment, then re-enable the collider for next playthrough
            yield return new WaitForSeconds(1f);
            ResetEndTrigger();
        }

        Debug.Log("=== GROW ASSESSMENT PART COMPLETE ===");
        Debug.Log("Player can now continue to other parts of the game");
        Debug.Log("Everything reset - Ready for second playthrough!");
    }

    private void ResetPlayerScale()
    {
        // Find the player's armature and reset to original scale
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Try to find armature first
            Transform playerArmature = player.transform.Find("Armature");
            if (playerArmature != null)
            {
                playerArmature.localScale = Vector3.one;
                Debug.Log("Player armature scale reset to original");
            }
            else
            {
                // If no armature found, try common child names
                foreach (Transform child in player.transform)
                {
                    if (child.name.Contains("Armature") || child.name.Contains("Character") || child.name.Contains("Model"))
                    {
                        child.localScale = Vector3.one;
                        Debug.Log($"Player {child.name} scale reset to original");
                        return;
                    }
                }

                // Last resort: reset the player's transform directly
                player.transform.localScale = Vector3.one;
                Debug.Log("Player transform scale reset to original");
            }
        }
        else
        {
            Debug.LogWarning("Player not found! Cannot reset player scale.");
        }
    }

    // Public method to reset the trigger for next game
    public void ResetEndTrigger()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.enabled)
        {
            collider.enabled = true;
            Debug.Log("End game trigger collider re-enabled");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.5f); // Orange transparent
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.DrawCube(transform.position + boxCollider.center, boxCollider.size);
        }
        else
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.DrawSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.DrawWireCube(transform.position + boxCollider.center, boxCollider.size);
        }
        else
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
            }
        }
    }
}