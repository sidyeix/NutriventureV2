using UnityEngine;

public class NPCInteractionTrigger : MonoBehaviour
{
    [SerializeField] private NPCGuardController cutsceneController;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            cutsceneController.OnPlayerEnter();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            cutsceneController.OnPlayerExit();
    }
}
