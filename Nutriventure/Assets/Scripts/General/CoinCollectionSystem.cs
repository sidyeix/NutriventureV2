using UnityEngine;

public class CoinCollectionSystem : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;
    public AudioClip coinCollectSound;
    
    [Header("Collider Settings")]
    public float collectionRadius = 0.5f;
    public bool useMeshBounds = true;
    public Vector3 colliderCenterOffset = Vector3.zero;
    
    [Header("Player Detection")]
    public float playerHeightOffset = 1f; // Height from player's feet to consider for collection
    public bool useSimpleDetection = true; // Simple trigger vs precise distance check
    
    [Header("Visual Feedback")]
    public ParticleSystem collectParticles;
    
    private SphereCollider triggerCollider;
    private bool hasBeenCollected = false;
    
    private void Start()
    {
        SetupCollider();
    }
    
    private void SetupCollider()
    {
        // Remove any existing colliders first
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            Destroy(col);
        }

        // Calculate appropriate collider size based on mesh bounds
        float finalRadius = collectionRadius;
        Vector3 finalCenter = colliderCenterOffset;
        
        if (useMeshBounds)
        {
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                float meshSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                finalRadius = meshSize * 0.8f; // 80% of mesh size for comfortable collection
                
                // Calculate center offset based on mesh bounds
                Vector3 meshCenter = bounds.center;
                Vector3 pivotPosition = transform.position;
                finalCenter = transform.InverseTransformPoint(meshCenter);
                
                Debug.Log($"Mesh bounds: {bounds.size}, Center: {meshCenter}, Pivot: {pivotPosition}, Local Center: {finalCenter}");
            }
        }

        // Add trigger collider with calculated size and center
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = finalRadius;
        triggerCollider.center = finalCenter;
        
        Debug.Log($"Coin collider setup - Radius: {finalRadius}, Center: {finalCenter}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider is the player
        if (other.CompareTag("Player") && !hasBeenCollected)
        {
            Debug.Log($"Player entered coin trigger. Player: {other.gameObject.name}");
            
            if (useSimpleDetection)
            {
                // Simple detection - just collect when trigger is entered
                CollectCoin();
            }
            else
            {
                // Precise detection with distance check
                if (IsPlayerCloseEnough(other.transform))
                {
                    CollectCoin();
                }
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Also check if player stays in trigger (in case they entered too fast)
        if (other.CompareTag("Player") && !hasBeenCollected && !useSimpleDetection)
        {
            if (IsPlayerCloseEnough(other.transform))
            {
                CollectCoin();
            }
        }
    }
    
    private bool IsPlayerCloseEnough(Transform playerTransform)
    {
        if (triggerCollider == null) return true; // Fallback if no collider
        
        // Get the world position of the collider center
        Vector3 colliderWorldCenter = transform.TransformPoint(triggerCollider.center);
        
        // Get player position at a reasonable height (not at feet)
        Vector3 playerPosition = playerTransform.position;
        Vector3 playerCheckPosition = playerPosition + Vector3.up * playerHeightOffset;
        
        // Calculate horizontal distance only (ignore height differences)
        Vector3 colliderHorizontal = new Vector3(colliderWorldCenter.x, 0, colliderWorldCenter.z);
        Vector3 playerHorizontal = new Vector3(playerCheckPosition.x, 0, playerCheckPosition.z);
        
        float horizontalDistance = Vector3.Distance(colliderHorizontal, playerHorizontal);
        float colliderRadius = triggerCollider.radius;
        
        // Player should be within the collider radius horizontally
        bool isCloseEnough = horizontalDistance <= colliderRadius;
        
        Debug.Log($"Horizontal distance: {horizontalDistance}, Collider radius: {colliderRadius}, Close enough: {isCloseEnough}");
        Debug.Log($"Collider: {colliderWorldCenter}, Player: {playerCheckPosition}");
        
        return isCloseEnough;
    }
    
    public void CollectCoin()
    {
        // Prevent multiple collections
        if (hasBeenCollected || !gameObject.activeInHierarchy) return;
        
        hasBeenCollected = true;
        
        // Add coins to GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.nutriCoins += coinValue;
            GameDataManager.Instance.SaveGameData();
            
            Debug.Log($"Coin collected! +{coinValue} coins. Total: {GameDataManager.Instance.CurrentGameData.nutriCoins}");
            
            // Play collection effects
            PlayCollectionEffects();
            
            // Notify gameplay progression UI
            GameplayProgression gameplayUI = FindAnyObjectByType<GameplayProgression>();
            if (gameplayUI != null)
            {
                gameplayUI.UpdateCoinDisplay();
            }
            
            // Disable immediately and schedule destruction
            gameObject.SetActive(false);
            Destroy(gameObject, 0.1f);
        }
        else
        {
            Debug.LogWarning("GameDataManager not available - coin not collected");
        }
    }
    
    private void PlayCollectionEffects()
    {
        // Play sound
        if (coinCollectSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(coinCollectSound);
        }
        
        // Play particles at the collider center position
        Vector3 effectPosition = triggerCollider != null ? transform.TransformPoint(triggerCollider.center) : transform.position;
        
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, effectPosition, Quaternion.identity);
            particles.Play();
            
            // Destroy particles after they finish
            Destroy(particles.gameObject, particles.main.duration);
        }
    }
    
    // Method to manually collect coins from other systems (chest, rewards, etc.)
    public static void AddCoins(int amount)
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.nutriCoins += amount;
            GameDataManager.Instance.SaveGameData();
            
            Debug.Log($"Added {amount} coins. Total: {GameDataManager.Instance.CurrentGameData.nutriCoins}");
            
            // Update UI if available
            GameplayProgression gameplayUI = FindAnyObjectByType<GameplayProgression>();
            if (gameplayUI != null)
            {
                gameplayUI.UpdateCoinDisplay();
            }
        }
    }
    
    // Method to get current coin count
    public static int GetCurrentCoins()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            return GameDataManager.Instance.CurrentGameData.nutriCoins;
        }
        return 0;
    }
    
    // Method to check if player can afford something
    public static bool CanAfford(int cost)
    {
        return GetCurrentCoins() >= cost;
    }
    
    // Method to spend coins
    public static bool SpendCoins(int amount)
    {
        if (CanAfford(amount))
        {
            if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
            {
                GameDataManager.Instance.CurrentGameData.nutriCoins -= amount;
                GameDataManager.Instance.SaveGameData();
                
                Debug.Log($"Spent {amount} coins. Remaining: {GameDataManager.Instance.CurrentGameData.nutriCoins}");
                
                // Update UI
                GameplayProgression gameplayUI = FindAnyObjectByType<GameplayProgression>();
                if (gameplayUI != null)
                {
                    gameplayUI.UpdateCoinDisplay();
                }
                
                return true;
            }
        }
        return false;
    }
    
    // Visual debugging in editor
    private void OnDrawGizmosSelected()
    {
        if (triggerCollider != null)
        {
            // Draw collider sphere
            Gizmos.color = Color.green;
            Vector3 worldCenter = transform.TransformPoint(triggerCollider.center);
            Gizmos.DrawWireSphere(worldCenter, triggerCollider.radius);
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(worldCenter, triggerCollider.radius);
            
            // Draw line from pivot to collider center
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, worldCenter);
            
            // Draw horizontal detection area
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(worldCenter.x, 0, worldCenter.z), triggerCollider.radius);
        }
        else
        {
            // Draw preview in editor when not playing
            Gizmos.color = Color.green;
            Vector3 center = transform.TransformPoint(colliderCenterOffset);
            float radius = collectionRadius;
            
            if (useMeshBounds)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    Bounds bounds = renderer.bounds;
                    float meshSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                    radius = meshSize * 0.8f;
                    
                    // Calculate center for preview
                    Vector3 meshCenter = bounds.center;
                    center = meshCenter;
                }
            }
            
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(center, radius);
        }
    }
}