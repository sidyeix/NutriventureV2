// ContinueTrigger.cs
using UnityEngine;

public class ContinueTrigger : MonoBehaviour
{
    [Header("Settings")]
    public string playerTag = "Player";
    public bool disableAfterUse = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (TimelinePauseManager.Instance != null)
            {
                TimelinePauseManager.Instance.ResumeTimeline();

                if (disableAfterUse)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    // For 2D games
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (TimelinePauseManager.Instance != null)
            {
                TimelinePauseManager.Instance.ResumeTimeline();

                if (disableAfterUse)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}