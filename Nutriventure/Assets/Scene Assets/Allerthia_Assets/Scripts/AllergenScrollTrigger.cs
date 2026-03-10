using UnityEngine;

/// <summary>
/// Place this on the collider zone near the Allerthia scroll.
/// When the player enters, it tells AllergenGameManager to show the grab canvas.
/// If the scroll was already grabbed, nothing happens.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AllergenScrollTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.ShowGrabCanvas();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.HideGrabCanvas();
    }
}
