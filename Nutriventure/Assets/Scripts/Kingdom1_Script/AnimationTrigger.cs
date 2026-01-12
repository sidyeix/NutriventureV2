using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator targetAnimator; // Assign the Animator you want to control
    public string triggerParameter = "isTrigger"; // Name of the trigger parameter in Animator

    [Header("Trigger Settings")]
    public string playerTag = "Player"; // Tag to detect
    public bool triggerOnce = true; // If true, will only trigger once

    [Header("Disable Animator After")]
    public bool disableAnimatorAfterAnimation = false; // Checkbox to disable animator after animation
    public float disableAfterSeconds = 1.0f; // Time in seconds before disabling animator

    private bool hasBeenTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if we should trigger and if it's the player
        if ((!triggerOnce || !hasBeenTriggered) && other.CompareTag(playerTag))
        {
            // Set the animator trigger
            if (targetAnimator != null)
            {
                // Make sure animator is enabled before triggering
                targetAnimator.enabled = true;

                // Set the trigger
                targetAnimator.SetTrigger(triggerParameter);
                Debug.Log($"Trigger '{triggerParameter}' set on {targetAnimator.name} by {other.name}");

                // If we need to disable animator after animation
                if (disableAnimatorAfterAnimation)
                {
                    // Schedule to disable animator after specified seconds
                    Invoke(nameof(DisableAnimator), disableAfterSeconds);
                }
            }
            else
            {
                Debug.LogWarning("Target Animator not assigned!");
            }

            hasBeenTriggered = true;
        }
    }

    void DisableAnimator()
    {
        if (targetAnimator != null && targetAnimator.enabled)
        {
            targetAnimator.enabled = false;
            Debug.Log($"Disabled animator on {targetAnimator.name} after {disableAfterSeconds} seconds");
        }
    }

    // Optional: Reset the trigger state and re-enable animator
    public void ResetTrigger()
    {
        hasBeenTriggered = false;

        if (targetAnimator != null)
        {
            // Re-enable animator first
            targetAnimator.enabled = true;
            targetAnimator.ResetTrigger(triggerParameter);
            Debug.Log($"Reset trigger and re-enabled animator on {targetAnimator.name}");
        }

        CancelInvoke(nameof(DisableAnimator)); // Cancel any pending disable
    }

    // Clean up when object is destroyed
    void OnDestroy()
    {
        CancelInvoke(nameof(DisableAnimator));
    }
}