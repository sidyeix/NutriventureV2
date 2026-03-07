using UnityEngine;

public class K3_RocksEmerge : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject playerArmature;

    [Header("Trigger Objects")]
    public GameObject triggerSteps;

    [Header("Box Collider Settings")]
    public BoxCollider triggerCollider;

    [Header("Effects to Disable on Trigger")]
    [Tooltip("GameObjects (particles, VFX, indicators) that are visible before the player enters and get disabled once triggered.")]
    public GameObject[] effectsToDisableOnTrigger;

    private void Start()
    {
        // Auto-get BoxCollider if not assigned
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        // Ensure it's a trigger
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        // Start with triggerSteps disabled
        if (triggerSteps != null)
            triggerSteps.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Phase1 Trigger entered by: {other.gameObject.name} (Tag: {other.gameObject.tag})");

        // FIX 1: Check if it's the player OR any child of the player
        if (playerArmature != null && IsPlayerOrChild(other.gameObject))
        {
            Debug.Log($"SUCCESS: Player detected! Player: {playerArmature.name}, Collider: {other.gameObject.name}");

            if (triggerSteps != null)
            {
                triggerSteps.SetActive(true);
                Debug.Log($"TriggerSteps enabled! Active: {triggerSteps.activeSelf}");
            }

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
                Debug.Log($"Trigger collider disabled.");
            }

            // Disable any visual effects associated with the trigger
            if (effectsToDisableOnTrigger != null)
            {
                for (int i = 0; i < effectsToDisableOnTrigger.Length; i++)
                {
                    if (effectsToDisableOnTrigger[i] != null)
                        effectsToDisableOnTrigger[i].SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Not the expected player. PlayerArmature: {playerArmature?.name}, Collider's parent: {other.transform.root?.name}");
        }
    }

    // Check if this GameObject is the player OR a child of the player
    private bool IsPlayerOrChild(GameObject obj)
    {
        if (obj == playerArmature) return true;
        if (obj.transform.IsChildOf(playerArmature.transform)) return true;
        if (obj.CompareTag("Player")) return true;
        return false;
    }

    /// <summary>
    /// Resets the emerged rocks/steps back to initial state.
    /// </summary>
    public void ResetRocks()
    {
        if (triggerSteps != null)
            triggerSteps.SetActive(false);

        if (triggerCollider != null)
            triggerCollider.enabled = true;

        // Re-enable visual effects so they are visible again
        if (effectsToDisableOnTrigger != null)
        {
            for (int i = 0; i < effectsToDisableOnTrigger.Length; i++)
            {
                if (effectsToDisableOnTrigger[i] != null)
                    effectsToDisableOnTrigger[i].SetActive(true);
            }
        }
    }
}