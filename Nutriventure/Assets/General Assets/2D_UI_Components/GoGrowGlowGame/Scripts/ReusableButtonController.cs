using UnityEngine;

public class ReusableButtonController : MonoBehaviour
{
    [Header("Button Animation")]
    public Animator buttonAnimator;
    public string buttonPressParameter = "isPressed";

    [Header("Targets to Control")]
    public Animator[] targetAnimators; // Can control multiple animators
    public string[] targetParameters; // Parameter names for each animator

    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public string pushableTag = "Pushable";

    private int objectsOnButton = 0;

    void Start()
    {
        // Make sure button starts not pressed
        if (buttonAnimator != null)
        {
            buttonAnimator.SetBool(buttonPressParameter, false);
        }

        // Make sure all targets start in their "off" state
        SetAllTargets(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's player or pushable object
        if (other.CompareTag(playerTag) || other.CompareTag(pushableTag))
        {
            objectsOnButton++;

            // If this is the first object to enter, press the button
            if (objectsOnButton == 1)
            {
                PressButton();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's player or pushable object
        if (other.CompareTag(playerTag) || other.CompareTag(pushableTag))
        {
            objectsOnButton--;

            // If no objects left on button, release it
            if (objectsOnButton == 0)
            {
                ReleaseButton();
            }
        }
    }

    void PressButton()
    {
        // Set button animation to pressed
        if (buttonAnimator != null)
        {
            buttonAnimator.SetBool(buttonPressParameter, true);
        }

        // Activate all targets
        SetAllTargets(true);
    }

    void ReleaseButton()
    {
        // Set button animation to not pressed
        if (buttonAnimator != null)
        {
            buttonAnimator.SetBool(buttonPressParameter, false);
        }

        // Deactivate all targets
        SetAllTargets(false);
    }

    void SetAllTargets(bool activate)
    {
        // Set the parameter for each target animator
        for (int i = 0; i < targetAnimators.Length; i++)
        {
            if (targetAnimators[i] != null && i < targetParameters.Length)
            {
                targetAnimators[i].SetBool(targetParameters[i], activate);
            }
        }
    }

    // For debugging - see how many objects are on button
    void OnDrawGizmos()
    {
        if (objectsOnButton > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
        }
    }
}