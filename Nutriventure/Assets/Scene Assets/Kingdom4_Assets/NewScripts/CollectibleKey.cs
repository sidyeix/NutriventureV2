using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CollectibleKey : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyId = "castle_key";
    
    [Header("Debug Settings")]
    public bool debugMode = true;
    
    [Header("Activation Settings")]
    public bool activateWhenNoKey = true; // Active when player doesn't have key
    public bool deactivateWhenHasKey = true; // Inactive when player has key
    
    [Header("Visual Settings")]
    public float rotationSpeed = 90f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    
    [Header("Sound FX")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)] public float pickupVolume = 1f;
    
    private Camera mainCamera;
    private Vector3 startPosition;
    private bool isCollected = false;
    private bool shouldBeActive = true;
    
    void Start()
    {
        InitializeKey();
    }
    
    void InitializeKey()
    {
        startPosition = transform.position;
        mainCamera = Camera.main;
        
        // Start checking saved state after a short delay
        StartCoroutine(CheckSavedStateAfterDelay());
    }
    
    private IEnumerator CheckSavedStateAfterDelay()
    {
        // Wait for one frame to ensure all managers are initialized
        yield return null;
        
        // Check if player already has the key
        bool playerHasKey = CheckIfPlayerHasKey();
        
        // Set activation based on player's key status
        if (activateWhenNoKey && deactivateWhenHasKey)
        {
            // Show key only if player doesn't have it
            shouldBeActive = !playerHasKey;
        }
        else if (activateWhenNoKey)
        {
            // Always show if activateWhenNoKey is true
            shouldBeActive = true;
        }
        else if (deactivateWhenHasKey)
        {
            // Hide only if player has key
            shouldBeActive = !playerHasKey;
        }
        
        // Apply activation state
        if (shouldBeActive)
        {
            // Key should be visible and collectible
            isCollected = false;
            gameObject.SetActive(true);
            
            if (debugMode) Debug.Log($"Key '{keyId}' is ACTIVE (player doesn't have key)");
        }
        else
        {
            // Player already has key, hide it
            isCollected = true;
            gameObject.SetActive(false);
            
            if (debugMode) Debug.Log($"Key '{keyId}' is INACTIVE (player already has key)");
        }
        
        #if UNITY_EDITOR
        // In Editor debug mode, we might want to override
        if (debugMode && PlayerPrefs.HasKey($"KeyCollected_{keyId}"))
        {
            Debug.LogWarning($"Key '{keyId}' was previously collected but is active for testing.");
            // Don't auto-disable in Editor for testing if we want to test collection again
            // isCollected = false;
            // gameObject.SetActive(true);
        }
        #endif
    }
    
    private bool CheckIfPlayerHasKey()
    {
        try
        {
            // Check AllerthriaGameManager (current session)
            if (AllerthriaGameManager.Instance != null && AllerthriaGameManager.Instance.hasKey)
            {
                return true;
            }
            
            // Check GameDataManager (saved data)
            if (GameDataManager1.Instance != null && GameDataManager1.Instance.currentGameData.hasKey)
            {
                return true;
            }
            
            // Check PlayerPrefs as fallback
            if (PlayerPrefs.GetInt($"KeyCollected_{keyId}", 0) == 1)
            {
                return true;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking key: {e.Message}");
            return false;
        }
    }
    
    void Update()
    {
        if (!isCollected && gameObject.activeSelf && shouldBeActive)
        {
            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Rotation animation
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            
            // Check for mobile tap
            CheckMobileTap();
        }
    }
    
    void OnMouseDown()
    {
        if (!isCollected && shouldBeActive)
        {
            if (debugMode) Debug.Log("Key clicked with mouse!");
            CollectKey();
        }
    }
    
    void CheckMobileTap()
    {
        if (Touchscreen.current == null || mainCamera == null)
            return;
        
        var touch = Touchscreen.current.primaryTouch;
        
        if (!touch.press.wasPressedThisFrame)
            return;
        
        Vector2 touchPos = touch.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(touchPos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (!isCollected && shouldBeActive)
                {
                    if (debugMode) Debug.Log("Key tapped on mobile!");
                    CollectKey();
                }
            }
        }
    }
    
    public void CollectKey()
    {
        if (isCollected || !shouldBeActive) return;
        
        isCollected = true;
        
        if (debugMode) Debug.Log($"Key '{keyId}' collected!");
        
        // Play collect sound
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position, pickupVolume);
        }
        
        // Notify Game Manager
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ReceiveKey();
        }
        
        // Save to ALL systems
        SaveKeyState();
        
        // Save to GameDataManager
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.currentGameData.hasKey = true;
            GameDataManager1.Instance.currentGameData.kingdom4Completed = true;
            GameDataManager1.Instance.SaveGameProgress();
            if (debugMode) Debug.Log("Key saved to GameDataManager!");
        }
        
        // Trigger the game summary
        TriggerGameSummary();
        
        // Disable the key
        gameObject.SetActive(false);
    }
    
    void TriggerGameSummary()
    {
        // Try K4GameSummary first
        K4GameSummary gameSummary = FindObjectOfType<K4GameSummary>();
        if (gameSummary != null)
        {
            gameSummary.TriggerSummaryFromKey();
            if (debugMode) Debug.Log("Triggered game summary via K4GameSummary");
            return;
        }
        
        // Fallback to Kingdom4GameEndManager
        Kingdom4GameEndManager gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4Complete();
            if (debugMode) Debug.Log("Triggered game summary via Kingdom4GameEndManager");
            return;
        }
        
        Debug.LogWarning("No game summary manager found!");
    }
    
    void SaveKeyState()
    {
        PlayerPrefs.SetInt($"KeyCollected_{keyId}", 1);
        PlayerPrefs.Save();
        
        if (debugMode) Debug.Log($"Saved key state: {keyId} = collected");
    }
    
    [ContextMenu("Check Key Status")]
    public void CheckKeyStatus()
    {
        bool playerHasKey = CheckIfPlayerHasKey();
        Debug.Log($"=== KEY STATUS ===");
        Debug.Log($"Player has key: {playerHasKey}");
        Debug.Log($"Key should be active: {!playerHasKey}");
        Debug.Log($"Key object active: {gameObject.activeSelf}");
        Debug.Log($"Key isCollected: {isCollected}");
        Debug.Log($"==================");
    }
    
    [ContextMenu("Test Collect Key")]
    public void TestCollectKey()
    {
        if (!isCollected && shouldBeActive)
        {
            CollectKey();
        }
        else
        {
            Debug.Log($"Cannot collect key: isCollected={isCollected}, shouldBeActive={shouldBeActive}");
        }
    }
    
    [ContextMenu("Reset Key")]
    public void ResetKey()
    {
        isCollected = false;
        shouldBeActive = true;
        gameObject.SetActive(true);
        
        PlayerPrefs.DeleteKey($"KeyCollected_{keyId}");
        
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.currentGameData.hasKey = false;
            GameDataManager1.Instance.currentGameData.kingdom4Completed = false;
            GameDataManager1.Instance.SaveGameProgress();
        }
        
        if (debugMode) Debug.Log($"Key '{keyId}' reset and activated");
    }
    
    [ContextMenu("Force Activate Key")]
    public void ForceActivateKey()
    {
        // Force activate key regardless of saved state
        isCollected = false;
        shouldBeActive = true;
        gameObject.SetActive(true);
        PlayerPrefs.DeleteKey($"KeyCollected_{keyId}");
        
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.currentGameData.hasKey = false;
            GameDataManager1.Instance.currentGameData.kingdom4Completed = false;
        }
        
        if (debugMode) Debug.Log($"Key '{keyId}' force activated");
    }
}