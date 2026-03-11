using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationRespawnTrigger : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator targetAnimator; // Assign the Animator you want to control
    public string triggerParameter = "isTrigger"; // Name of the trigger parameter in Animator

    [Header("Respawn Settings")]
    public List<GameObject> objectsToRespawn = new List<GameObject>();
    public float disableTime = 2f;

    [Header("Animation Reset Settings")]
    public bool resetAnimatorOnRespawn = true; // Reset animator when object re-enables
    public string defaultStateName = "Idle"; // Name of default animation state
    public bool replayAnimationOnEnable = false; // Replay animation when object re-enables

    [Header("Trigger Settings")]
    public string playerTag = "Player"; // Tag to detect
    public bool triggerOnce = false; // If true, will only trigger once

    [Header("Disable Animator After")]
    public bool disableAnimatorAfterAnimation = false; // Checkbox to disable animator after animation
    public float disableAfterSeconds = 1.0f; // Time in seconds before disabling animator

    private bool hasBeenTriggered = false;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> originalScales = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();
    private Dictionary<Animator, AnimatorStateInfo> animatorStates = new Dictionary<Animator, AnimatorStateInfo>();

    void Start()
    {
        // Store original transforms for all objects
        StoreOriginalTransforms();

        // Store original animator states
        StoreAnimatorStates();
    }

    void StoreOriginalTransforms()
    {
        foreach (var obj in objectsToRespawn)
        {
            if (obj != null)
            {
                originalPositions[obj] = obj.transform.localPosition;
                originalScales[obj] = obj.transform.localScale;
                originalRotations[obj] = obj.transform.localRotation;
            }
        }
    }

    void StoreAnimatorStates()
    {
        foreach (var obj in objectsToRespawn)
        {
            if (obj != null)
            {
                var animator = obj.GetComponent<Animator>();
                if (animator != null)
                {
                    animatorStates[animator] = animator.GetCurrentAnimatorStateInfo(0);
                }
            }
        }

        if (targetAnimator != null && !animatorStates.ContainsKey(targetAnimator))
        {
            animatorStates[targetAnimator] = targetAnimator.GetCurrentAnimatorStateInfo(0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we should trigger and if it's the player
        if ((!triggerOnce || !hasBeenTriggered) && other.CompareTag(playerTag))
        {
            // Trigger the animation
            TriggerAnimation();

            // Start the respawn process
            StartCoroutine(RespawnObjects());

            hasBeenTriggered = true;
        }
    }

    void TriggerAnimation()
    {
        // Set the animator trigger
        if (targetAnimator != null)
        {
            // Make sure animator is enabled before triggering
            targetAnimator.enabled = true;

            // Set the trigger
            targetAnimator.SetTrigger(triggerParameter);
            #if UNITY_EDITOR
            Debug.Log($"Trigger '{triggerParameter}' set on {targetAnimator.name}");
            #endif

            // If we need to disable animator after animation
            if (disableAnimatorAfterAnimation)
            {
                // Schedule to disable animator after specified seconds
                Invoke(nameof(DisableTargetAnimator), disableAfterSeconds);
            }
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Target Animator not assigned!");
            #endif
        }
    }

    private IEnumerator RespawnObjects()
    {
        // Store current states before disabling
        StoreAnimatorStates();

        // Disable all objects
        foreach (var obj in objectsToRespawn)
        {
            if (obj != null && obj.activeSelf)
            {
                // Store the current state of animator if it has one
                var animator = obj.GetComponent<Animator>();
                if (animator != null)
                {
                    animatorStates[animator] = animator.GetCurrentAnimatorStateInfo(0);
                }

                obj.SetActive(false);
                #if UNITY_EDITOR
                Debug.Log($"Disabled: {obj.name}");
                #endif
            }
        }

        // Wait
        yield return CoroutineYieldCache.WaitForSeconds(disableTime);

        // Re-enable all objects
        foreach (var obj in objectsToRespawn)
        {
            if (obj != null && !obj.activeSelf)
            {
                // Reset transform before enabling
                ResetObjectTransform(obj);

                // Enable the object
                obj.SetActive(true);

                // Reset animator if needed
                ResetObjectAnimator(obj);

                #if UNITY_EDITOR
                Debug.Log($"Enabled: {obj.name}");
                #endif
            }
        }
    }

    void ResetObjectTransform(GameObject obj)
    {
        if (originalPositions.ContainsKey(obj))
        {
            obj.transform.localPosition = originalPositions[obj];
            obj.transform.localScale = originalScales[obj];
            obj.transform.localRotation = originalRotations[obj];
            #if UNITY_EDITOR
            Debug.Log($"Reset transform for {obj.name}");
            #endif
        }
    }

    void ResetObjectAnimator(GameObject obj)
    {
        if (!resetAnimatorOnRespawn) return;

        var animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            // Enable animator
            animator.enabled = true;

            // Method 1: Rebind to reset to initial state
            animator.Rebind();
            animator.Update(0f);

            // Method 2: Play default state
            if (!string.IsNullOrEmpty(defaultStateName))
            {
                animator.Play(defaultStateName, 0, 0f);
            }

            // Method 3: Reset all parameters
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.ResetTrigger(param.name);
                }
                else if (param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(param.name, false);
                }
                else if (param.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(param.name, 0f);
                }
                else if (param.type == AnimatorControllerParameterType.Int)
                {
                    animator.SetInteger(param.name, 0);
                }
            }

            #if UNITY_EDITOR
            Debug.Log($"Reset animator for {obj.name}");
            #endif

            // Re-trigger animation if needed
            if (replayAnimationOnEnable && obj == targetAnimator?.gameObject)
            {
                Invoke(nameof(RetriggerAnimation), 0.1f);
            }
        }
    }

    void RetriggerAnimation()
    {
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(triggerParameter);
            #if UNITY_EDITOR
            Debug.Log($"Re-triggered animation for {targetAnimator.name}");
            #endif
        }
    }

    void DisableTargetAnimator()
    {
        if (targetAnimator != null && targetAnimator.enabled)
        {
            targetAnimator.enabled = false;
            #if UNITY_EDITOR
            Debug.Log($"Disabled animator on {targetAnimator.name} after {disableAfterSeconds} seconds");
            #endif
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

            // Reset to default state
            if (!string.IsNullOrEmpty(defaultStateName))
            {
                targetAnimator.Play(defaultStateName, 0, 0f);
            }

            #if UNITY_EDITOR
            Debug.Log($"Reset trigger and re-enabled animator on {targetAnimator.name}");
            #endif
        }

        CancelInvoke(nameof(DisableTargetAnimator)); // Cancel any pending disable
    }

    // Public methods for manual control
    public void AddObjectToRespawn(GameObject obj)
    {
        if (obj != null && !objectsToRespawn.Contains(obj))
        {
            objectsToRespawn.Add(obj);
            StoreOriginalTransform(obj);
            #if UNITY_EDITOR
            Debug.Log($"Added {obj.name} to respawn list");
            #endif
        }
    }

    void StoreOriginalTransform(GameObject obj)
    {
        if (obj != null)
        {
            originalPositions[obj] = obj.transform.localPosition;
            originalScales[obj] = obj.transform.localScale;
            originalRotations[obj] = obj.transform.localRotation;
        }
    }

    public void RemoveObjectFromRespawn(GameObject obj)
    {
        if (objectsToRespawn.Contains(obj))
        {
            objectsToRespawn.Remove(obj);
            originalPositions.Remove(obj);
            originalScales.Remove(obj);
            originalRotations.Remove(obj);
            #if UNITY_EDITOR
            Debug.Log($"Removed {obj.name} from respawn list");
            #endif
        }
    }

    public void TriggerRespawnManually()
    {
        if (!hasBeenTriggered || !triggerOnce)
        {
            TriggerAnimation();
            StartCoroutine(RespawnObjects());
        }
    }

    public void ForceResetAllObjects()
    {
        StopAllCoroutines();

        foreach (var obj in objectsToRespawn)
        {
            if (obj != null)
            {
                ResetObjectTransform(obj);

                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                }

                ResetObjectAnimator(obj);
            }
        }

        if (targetAnimator != null)
        {
            ResetTrigger();
        }

        hasBeenTriggered = false;
    }

    // Clean up when object is destroyed
    void OnDestroy()
    {
        CancelInvoke(nameof(DisableTargetAnimator));
        CancelInvoke(nameof(RetriggerAnimation));
    }

    void OnValidate()
    {
        // Ensure sensible values
        if (disableTime < 0) disableTime = 0;
        if (disableAfterSeconds < 0) disableAfterSeconds = 0;
    }

    void OnDrawGizmos()
    {
        // Draw wireframe to show trigger area
        if (GetComponent<Collider>() != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider>().bounds.size);
        }

        // Draw lines to connected objects
        Gizmos.color = Color.yellow;
        foreach (var obj in objectsToRespawn)
        {
            if (obj != null)
            {
                Gizmos.DrawLine(transform.position, obj.transform.position);
            }
        }

        // Draw line to target animator
        if (targetAnimator != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetAnimator.transform.position);
        }
    }
}
