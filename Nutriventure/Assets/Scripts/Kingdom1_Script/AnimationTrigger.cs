using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator targetAnimator; // Assign the Animator you want to control
    public string triggerParameter = "isTrigger"; // Name of the trigger parameter in Animator

    [Header("Trigger Settings")]
    public string playerTag = "Player"; // Tag to detect
    public bool triggerOnce = true; // If true, will only trigger once

    private bool hasBeenTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if we should trigger and if it's the player
        if ((!triggerOnce || !hasBeenTriggered) && other.CompareTag(playerTag))
        {
            // Set the animator trigger
            if (targetAnimator != null)
            {
                targetAnimator.SetTrigger(triggerParameter);
                Debug.Log($"Trigger '{triggerParameter}' set on {targetAnimator.name} by {other.name}");
            }
            else
            {
                Debug.LogWarning("Target Animator not assigned!");
            }

            hasBeenTriggered = true;
        }
    }

    // Optional: Reset the trigger state
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        if (targetAnimator != null)
        {
            targetAnimator.ResetTrigger(triggerParameter);
        }
    }
}