using UnityEngine;

public class NPCTalkTrigger : MonoBehaviour
{
    [Header("References")]
    public Animator npcAnimator;

    [Header("Trigger Settings")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            npcAnimator.ResetTrigger("isIdle");
            npcAnimator.SetTrigger("isTalking");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            npcAnimator.ResetTrigger("isTalking");
            npcAnimator.SetTrigger("isIdle");
        }
    }
}
