using UnityEngine;

public class SlideTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Which slide to show when player enters this trigger (0-based index)")]
    public int slideNumberToShow = 0;

    [Header("Options")]
    public bool triggerOnce = true;
    public bool showDebugMessages = true;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;

            MechanicsBoardManager manager = FindObjectOfType<MechanicsBoardManager>();

            if (manager != null)
            {
                manager.SetTargetSlide(slideNumberToShow);

                if (showDebugMessages)
                {
                    Debug.Log($"SlideTrigger: Player entered. Set slide to: {slideNumberToShow}");
                }

                hasTriggered = true;
            }
            else
            {
                Debug.LogWarning("SlideTrigger: Could not find MechanicsBoardManager in scene!");
            }
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    public void TriggerManually()
    {
        MechanicsBoardManager manager = FindObjectOfType<MechanicsBoardManager>();

        if (manager != null)
        {
            manager.SetTargetSlide(slideNumberToShow);
            Debug.Log($"SlideTrigger: Manually triggered. Set slide to: {slideNumberToShow}");
        }
    }
}