using UnityEngine;
using System.Collections;

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
    [SerializeField] private float cameraBlendTime = 1.5f;
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
            {
#if UNITY_EDITOR
                Debug.LogWarning("StartingSequenceManager not found! Camera transitions may not work.");
#endif
            }
        }

        if (assessmentManager == null)
        {
            assessmentManager = FindObjectOfType<GrowAssessmentManager>();
            if (assessmentManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("GrowAssessmentManager not found! Cannot check completion status.");
#endif
            }
        }

        // Auto-find all group managers if not assigned
        if (groupManagers == null || groupManagers.Length == 0)
        {
            groupManagers = FindObjectsOfType<ObjectGroupManager>();
#if UNITY_EDITOR
            Debug.Log($"Auto-found {groupManagers.Length} group managers");
#endif
        }

        // Find assessment trigger if not assigned
        if (assessmentTrigger == null)
        {
            assessmentTrigger = FindObjectOfType<AssessmentTrigger>();
            if (assessmentTrigger == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("AssessmentTrigger not found.");
#endif
            }
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
#if UNITY_EDITOR
            Debug.Log("Player reached end game trigger - Completing GROW ASSESSMENT ONLY");
#endif

            // Check if assessment manager exists
            if (assessmentManager == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Assessment Manager not found! Cannot check completion status.");
#endif
                return;
            }

            // Check if player has completed all questions AND is waiting for end trigger
            bool hasCompletedQuestions = assessmentManager.HasCompletedAllQuestions();
            bool isWaitingForEnd = assessmentManager.IsWaitingForEndTrigger();
            bool isAssessmentActive = assessmentManager.IsAssessmentActive();

#if UNITY_EDITOR
            Debug.Log($"EndGameTrigger Check - Completed: {hasCompletedQuestions}, Waiting: {isWaitingForEnd}, Active: {isAssessmentActive}");
#endif

            if (!hasCompletedQuestions)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Player hasn't completed all questions yet! Trigger ignored.");
#endif

                // Optional: Show message to player
                if (isAssessmentActive)
                {
#if UNITY_EDITOR
                    Debug.Log("Assessment is still active. Complete all questions first!");
#endif
                }
                return;
            }

            if (!isWaitingForEnd)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Assessment is not in 'waiting for end' state! Trigger ignored.");
#endif
                return;
            }

#if UNITY_EDITOR
            Debug.Log("Player has completed all questions and is waiting for end trigger. Completing assessment...");
#endif

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
#if UNITY_EDITOR
            Debug.Log("Grow Assessment ended");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Assessment Manager is null! Cannot end assessment.");
#endif
        }

        // Completely reset the assessment manager for the next playthrough
        if (assessmentManager != null)
        {
            // Call the complete reset method that clears everything
            assessmentManager.CompleteResetForNewGame();
#if UNITY_EDITOR
            Debug.Log("Assessment Manager completely reset for new game");
#endif
        }

        // 3. Reset all group managers
        if (groupManagers != null && groupManagers.Length > 0)
        {
            foreach (ObjectGroupManager group in groupManagers)
            {
                if (group != null)
                {
                    group.ResetGroupForNewGame();
#if UNITY_EDITOR
                    Debug.Log($"Reset group: {group.gameObject.name}");
#endif
                }
            }
#if UNITY_EDITOR
            Debug.Log($"Reset {groupManagers.Length} group managers");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("No group managers assigned or found. Skipping group reset.");
#endif
        }

        // 4. Wait a bit before camera transition (optional)
        if (cameraTransitionDelay > 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Waiting {cameraTransitionDelay}s before camera transition...");
#endif
            yield return CoroutineYieldCache.WaitForSeconds(cameraTransitionDelay);
        }

        // 5. Re-enable controls and UI (with smooth camera transition back to normal)
        if (sequenceManager != null)
        {
            if (useSmoothCameraTransition)
            {
                // Use the smooth transition method with specified blend time
                sequenceManager.EnableAllControlsAndUI(cameraBlendTime);
#if UNITY_EDITOR
                Debug.Log($"All controls and UI restored - SMOOTH camera transition started ({cameraBlendTime}s blend)");
#endif
            }
            else
            {
                // Force instant camera reset
                sequenceManager.ForceCameraReset(0f); // 0 second = instant
                sequenceManager.EnablePlayerInput();
#if UNITY_EDITOR
                Debug.Log("All controls and UI restored - Instant camera reset");
#endif
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("Starting Sequence Manager not found! Camera and controls may not reset properly.");
#endif
        }

        // 6. Reset assessment trigger for next run
        if (assessmentTrigger != null)
        {
            // Use the force reset method to ensure it's fully reset
            assessmentTrigger.ForceResetForNewGame();
#if UNITY_EDITOR
            Debug.Log("Assessment trigger force reset for next game");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Assessment Trigger not found. Skipping trigger reset.");
#endif
        }

        // 7. Reset timeline for next playthrough
        TimelineSequenceManager timelineManager = FindObjectOfType<TimelineSequenceManager>();
        if (timelineManager != null)
        {
            timelineManager.ResetTimeline();
#if UNITY_EDITOR
            Debug.Log("Timeline reset for next playthrough");
#endif
        }

        // 8. Play completion sound if available
        if (completionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(completionSound);
#if UNITY_EDITOR
            Debug.Log("Played completion sound");
#endif
        }

        // 9. Disable this trigger if set to
        if (disableAfterTrigger)
        {
            Collider thisCollider = GetComponent<Collider>();
            if (thisCollider != null)
            {
                thisCollider.enabled = false;
#if UNITY_EDITOR
                Debug.Log("End game trigger disabled");
#endif
            }
        }

        // Enable the EndGameTrigger collider for the next playthrough
        if (disableAfterTrigger)
        {
            // Wait a moment, then re-enable the collider for next playthrough
            yield return CoroutineYieldCache.WaitForSeconds(1f);
            ResetEndTrigger();
        }

#if UNITY_EDITOR
        Debug.Log("=== GROW ASSESSMENT PART COMPLETE ===");
        Debug.Log("Player can now continue to other parts of the game");
        Debug.Log("Everything reset - Ready for second playthrough!");
#endif
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
#if UNITY_EDITOR
                Debug.Log("Player armature scale reset to original");
#endif
            }
            else
            {
                // If no armature found, try common child names
                foreach (Transform child in player.transform)
                {
                    if (child.name.Contains("Armature") || child.name.Contains("Character") || child.name.Contains("Model"))
                    {
                        child.localScale = Vector3.one;
#if UNITY_EDITOR
                        Debug.Log($"Player {child.name} scale reset to original");
#endif
                        return;
                    }
                }

                // Last resort: reset the player's transform directly
                player.transform.localScale = Vector3.one;
#if UNITY_EDITOR
                Debug.Log("Player transform scale reset to original");
#endif
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning("Player not found! Cannot reset player scale.");
#endif
        }
    }

    // Public method to reset the trigger for next game
    public void ResetEndTrigger()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.enabled)
        {
            collider.enabled = true;
#if UNITY_EDITOR
            Debug.Log("End game trigger collider re-enabled");
#endif
        }
    }

    // Set custom camera blend time
    public void SetCameraBlendTime(float blendTime)
    {
        cameraBlendTime = Mathf.Max(0.1f, blendTime);
#if UNITY_EDITOR
        Debug.Log($"EndGameTrigger camera blend time set to {cameraBlendTime}s");
#endif
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
