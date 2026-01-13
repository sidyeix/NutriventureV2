using UnityEngine;

public class K3_RocksEmerge : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject playerArmature;
    
    [Header("Trigger Objects")]
    public GameObject triggerSteps;
    
    [Header("Box Collider Settings")]
    public BoxCollider triggerCollider;
    
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
        }
        else
        {
            Debug.LogWarning($"Not the expected player. PlayerArmature: {playerArmature?.name}, Collider's parent: {other.transform.root?.name}");
        }
    }
    
    // Check if this GameObject is the player OR a child of the player
    private bool IsPlayerOrChild(GameObject obj)
    {
        // Method 1: Check if it's exactly the player
        if (obj == playerArmature)
            return true;
            
        // Method 2: Check if it's a child of the player
        if (obj.transform.IsChildOf(playerArmature.transform))
            return true;
            
        // Method 3: Check by tag (like DeathplaneFall does)
        if (obj.CompareTag("Player"))
            return true;
            
        return false;
    }
}