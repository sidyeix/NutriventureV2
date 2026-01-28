using UnityEngine;

public class AnimationTrigger : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator targetAnimator; // Assign the Animator you want to control
    public string parameterName = "isTrigger"; // Name of the parameter in Animator

    [Header("Optional: Force Set To False")]
    [Tooltip("When checked, will set the parameter to false instead of toggling")]
    public bool setToFalse = false; // Check this to force false state

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
            // Set the animator parameter
            if (targetAnimator != null)
            {
                // Make sure animator is enabled before triggering
                targetAnimator.enabled = true;

                // Check if we should force set to false
                if (setToFalse)
                {
                    // Try to set as bool if that's what you need
                    if (HasBoolParameter(parameterName))
                    {
                        targetAnimator.SetBool(parameterName, false);
                        Debug.Log($"Set '{parameterName}' to FALSE on {targetAnimator.name}");
                    }
                    else
                    {
                        // Fallback to trigger (less ideal but works)
                        targetAnimator.SetTrigger(parameterName);
                        Debug.Log($"Triggered '{parameterName}' (no bool found)");
                    }
                }
                else
                {
                    // Original behavior - use trigger parameter
                    targetAnimator.SetTrigger(parameterName);
                    Debug.Log($"Trigger '{parameterName}' set on {targetAnimator.name} by {other.name}");
                }

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

    // Helper method to check if parameter exists as a bool
    private bool HasBoolParameter(string paramName)
    {
        if (targetAnimator == null) return false;

        foreach (var param in targetAnimator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                return true;
        }
        return false;
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

            // Reset based on parameter type
            if (HasBoolParameter(parameterName) && setToFalse)
            {
                targetAnimator.SetBool(parameterName, false);
            }
            else
            {
                targetAnimator.ResetTrigger(parameterName);
            }

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