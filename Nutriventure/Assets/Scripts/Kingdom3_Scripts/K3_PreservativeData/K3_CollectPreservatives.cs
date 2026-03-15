using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class K3_CollectPreservatives : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator playerAnimator;
    public string pickupParameterName = "IsPickingUp";
    
    [Header("Pickup Settings")]
    public float pickupAnimationDuration = 0.5f;
    public float pickupRange = 3f; // How close player needs to be to show button
    
    [Header("UI References")]
    public Button pickupButton; // Assign your UI pickup button here
    
    [Header("Player Movement")]
    public MonoBehaviour playerMovementScript; // Assign your ThirdPersonController here
    
    [Header("Audio Settings")]
    public AudioClip pickupSound; // Assign your pickup sound here
    public float pickupSoundVolume = 0.7f;
    public float soundPlayDelay = 0.2f; // Delay to align with animation
    public bool playSoundOnPickup = true;
    
    [Header("Preservative System")]
    [Tooltip("Reference to the preservative spawner for tracking")]
    public K3_PreservativeSpawner preservativeSpawner;
    
    [Header("Information Manager")]
    [Tooltip("Reference to the preservative information manager")]
    public PreservativesInformationManager infoManager; // CRITICAL: ADDED THIS
    
    [Header("Collection Feedback")]
    [Tooltip("UI Text to show collection feedback")]
    public Text collectionFeedbackText;
    [Tooltip("Duration to show collection feedback")]
    public float feedbackDuration = 2f;
    [Tooltip("Particle effect when collecting")]
    public ParticleSystem collectionParticleEffect;
    
    private int pickupHash;
    private bool isPickingUp = false;
    private float pickupTimer = 0f;
    private GameObject currentNearbyPotion = null;
    private bool isButtonVisible = false;
    // REMOVED: private AudioSource audioSource; - NO LOCAL AUDIO SOURCE
    
    // Events for other systems
    public System.Action<GameObject, string> OnPotionCollected; // GameObject, PreservativeID
    public System.Action OnPickupStart;
    public System.Action OnPickupComplete;
    
    // Collection tracking
    private List<string> collectedPreservativeIDs = new List<string>();
    
    void Start()
    {
        // Get the animator if not assigned
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }
        
        // Get the player movement script if not assigned
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<StarterAssets.ThirdPersonController>();
        }
        
        // REMOVED: AudioSource setup - NO LOCAL AUDIO SOURCE
        
        // Convert parameter name to hash for better performance
        pickupHash = Animator.StringToHash(pickupParameterName);
        
        // Set up the pickup button
        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(OnPickupButtonClicked);
            pickupButton.gameObject.SetActive(false); // Hide by default
            isButtonVisible = false;
            Debug.Log("Pickup button listener added");
        }
        else
        {
            Debug.LogError("Pickup Button not assigned in Inspector!");
        }
        
        // Initialize collection feedback text
        if (collectionFeedbackText != null)
        {
            collectionFeedbackText.gameObject.SetActive(false);
        }
        
        if (playerAnimator == null)
        {
            Debug.LogError("Player Animator not found! Please assign it in the inspector.");
        }
        else
        {
            Debug.Log($"Pickup animation controller initialized with parameter: {pickupParameterName}");
        }
        
        // Try to find preservative spawner if not assigned
        if (preservativeSpawner == null)
        {
            preservativeSpawner = FindObjectOfType<K3_PreservativeSpawner>();
            if (preservativeSpawner != null)
            {
                Debug.Log($"Found preservative spawner: {preservativeSpawner.name}");
            }
        }
        
        // CRITICAL: Try to find info manager if not assigned
        if (infoManager == null)
        {
            infoManager = FindObjectOfType<PreservativesInformationManager>();
            if (infoManager != null)
            {
                Debug.Log($"Found PreservativesInformationManager: {infoManager.name}");
            }
            else
            {
                Debug.LogError("PreservativesInformationManager not found! Please assign it in the inspector.");
            }
        }
        
        // Check AudioHandler exists
        if (AudioHandler.Instance == null)
        {
            Debug.LogWarning("AudioHandler.Instance not found! Make sure AudioHandler is in the scene.");
        }
    }
    
    void Update()
    {
        // Handle pickup animation timer
        if (isPickingUp)
        {
            pickupTimer += Time.deltaTime;
            
            // Automatically end pickup animation after duration
            if (pickupTimer >= pickupAnimationDuration)
            {
                EndPickupAnimation();
            }
        }
        else
        {
            // Only check for nearby potions when not picking up
            CheckForNearbyPotions();
        }
    }
    
    void CheckForNearbyPotions()
    {
        // Find all potions with the specified tag
        GameObject[] preservativePotions = GameObject.FindGameObjectsWithTag("K3_PreservativePotion");
        
        GameObject closestPotion = null;
        float closestDistance = float.MaxValue;
        
        // Check all potions
        foreach (GameObject potion in preservativePotions)
        {
            if (potion == null) continue;
            
            float distance = Vector3.Distance(transform.position, potion.transform.position);
            if (distance < pickupRange && distance < closestDistance)
            {
                closestPotion = potion;
                closestDistance = distance;
            }
        }
        
        // Update button visibility based on proximity
        if (closestPotion != null && closestDistance <= pickupRange)
        {
            currentNearbyPotion = closestPotion;
            
            // Show button if not already visible
            if (!isButtonVisible)
            {
                ShowPickupButton();
            }
        }
        else
        {
            // No potions nearby
            if (currentNearbyPotion != null)
            {
                currentNearbyPotion = null;
            }
            
            // Hide button if visible
            if (isButtonVisible)
            {
                HidePickupButton();
            }
        }
    }
    
    // This method is called when the pickup button is clicked
    public void OnPickupButtonClicked()
    {
        if (isPickingUp)
        {
            Debug.Log("Already picking up!");
            return;
        }
        
        if (currentNearbyPotion == null)
        {
            Debug.LogWarning("No preservative potion nearby to pickup!");
            HidePickupButton();
            return;
        }
        
        // Verify we're picking up a preservative potion
        if (!currentNearbyPotion.CompareTag("K3_PreservativePotion"))
        {
            Debug.LogWarning($"Wrong tag! Expected K3_PreservativePotion, got {currentNearbyPotion.tag}");
            return;
        }
        
        if (playerAnimator != null)
        {
            StartCoroutine(PickupPotion());
        }
        else
        {
            Debug.LogError("Cannot trigger pickup - animator not found!");
        }
    }
    
    private IEnumerator PickupPotion()
    {
        Debug.Log($"Starting pickup process for: {currentNearbyPotion.name}");
        
        // Disable player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            Debug.Log("Player movement disabled");
        }
        
        // Hide pickup button immediately
        HidePickupButton();
        
        // Trigger the pickup animation
        playerAnimator.SetBool(pickupHash, true);
        isPickingUp = true;
        pickupTimer = 0f;
        
        // Invoke start event
        OnPickupStart?.Invoke();
        
        Debug.Log($"Pickup animation started for: {currentNearbyPotion.name}");
        
        // CHANGED: Play pickup sound with delay using AudioHandler
        if (playSoundOnPickup && pickupSound != null && AudioHandler.Instance != null)
        {
            StartCoroutine(PlayPickupSoundWithDelay());
        }
        
        // Wait for animation to complete
        yield return new WaitForSeconds(pickupAnimationDuration);
        
        // Complete the pickup
        CompletePickup();
    }
    
    // CHANGED: Using AudioHandler instead of local AudioSource
    private IEnumerator PlayPickupSoundWithDelay()
    {
        yield return new WaitForSeconds(soundPlayDelay);
        
        if (pickupSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(pickupSound);
            Debug.Log($"Pickup sound played through AudioHandler: {pickupSound.name}");
        }
    }
    
    private void CompletePickup()
    {
        Debug.Log("Completing pickup process");
        
        // End the pickup animation
        playerAnimator.SetBool(pickupHash, false);
        isPickingUp = false;
        pickupTimer = 0f;
        
        // Get preservative information
        if (currentNearbyPotion != null)
        {
            string potionName = currentNearbyPotion.name;
            
            Debug.Log($"Collecting preservative potion: {potionName}");
            
            // Get the preservative ID from the spawned instance
            string preservativeID = GetPreservativeIDFromPotion(currentNearbyPotion);
            
            if (!string.IsNullOrEmpty(preservativeID))
            {
                // Add to collected list if not already collected
                if (!collectedPreservativeIDs.Contains(preservativeID))
                {
                    collectedPreservativeIDs.Add(preservativeID);
                    Debug.Log($"Added preservative to collection: {preservativeID}");
                }
                
                // Show collection feedback
                ShowCollectionFeedback(preservativeID);
                
                // Play particle effect
                PlayCollectionParticleEffect();
                
                // Invoke collection event
                OnPotionCollected?.Invoke(currentNearbyPotion, preservativeID);
                
                // CRITICAL: CALL THE INFORMATION MANAGER TO SHOW THE PANEL
                if (infoManager != null)
                {
                    Debug.Log($"Calling infoManager.ShowPreservativeInfo for: {preservativeID}");
                    infoManager.ShowPreservativeInfo(preservativeID);
                }
                else
                {
                    Debug.LogError("InfoManager is null! Panel won't show.");
                    // Fallback: re-enable movement since no panel will show
                    if (playerMovementScript != null)
                    {
                        playerMovementScript.enabled = true;
                    }
                }
                
                // Notify spawner to remove this preservative
                if (preservativeSpawner != null)
                {
                    preservativeSpawner.RemovePreservative(currentNearbyPotion);
                }
                else
                {
                    // Fallback: just destroy the object
                    Destroy(currentNearbyPotion);
                }
            }
            else
            {
                Debug.LogWarning($"Could not determine preservative ID for: {potionName}");
                Destroy(currentNearbyPotion);
                // Re-enable movement since we're not showing a panel
                if (playerMovementScript != null)
                {
                    playerMovementScript.enabled = true;
                }
            }
            
            currentNearbyPotion = null;
        }
        else
        {
            // Re-enable movement if no potion was found
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
        }
        
        // Invoke complete event
        OnPickupComplete?.Invoke();
        
        Debug.Log("Potion pickup completed successfully");
    }
    
    private string GetPreservativeIDFromPotion(GameObject potion)
    {
        // Method 1: Try to get PreservativeInstance component
        PreservativeInstance preservativeInstance = potion.GetComponent<PreservativeInstance>();
        if (preservativeInstance != null && !string.IsNullOrEmpty(preservativeInstance.preservativeID))
        {
            return preservativeInstance.preservativeID;
        }
        
        // Method 2: Try to get info from spawner
        if (preservativeSpawner != null)
        {
            var preservativeInfo = preservativeSpawner.GetPreservativeInfo(potion);
            if (preservativeInfo != null)
            {
                return preservativeInfo.preservativeID;
            }
        }
        
        // Method 3: Extract from name (fallback)
        string cleanName = potion.name.Replace("_Spawned", "")
                                     .Replace("(Clone)", "")
                                     .Replace("_Preservative", "")
                                     .Trim();
        
        return cleanName;
    }
    
    private void ShowCollectionFeedback(string preservativeID)
    {
        if (collectionFeedbackText == null) return;
        
        // Get display name from database if available
        string displayName = preservativeID;
        if (preservativeSpawner != null && preservativeSpawner.preservativeDatabase != null)
        {
            var info = preservativeSpawner.preservativeDatabase.GetPreservativeInfo(preservativeID);
            if (info != null && !string.IsNullOrEmpty(info.displayName))
            {
                displayName = info.displayName;
            }
        }
        
        collectionFeedbackText.text = $"Collected: {displayName}!";
        collectionFeedbackText.gameObject.SetActive(true);
        
        StartCoroutine(HideFeedbackAfterDelay());
    }
    
    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        
        if (collectionFeedbackText != null)
        {
            collectionFeedbackText.gameObject.SetActive(false);
        }
    }
    
    private void PlayCollectionParticleEffect()
    {
        if (collectionParticleEffect != null)
        {
            ParticleSystem effect = Instantiate(collectionParticleEffect, 
                transform.position + Vector3.up, 
                Quaternion.identity);
            effect.Play();
            
            // Destroy after particle lifetime
            Destroy(effect.gameObject, effect.main.duration);
        }
    }
    
    private void ShowPickupButton()
    {
        if (pickupButton != null && !isButtonVisible)
        {
            pickupButton.gameObject.SetActive(true);
            pickupButton.interactable = true;
            isButtonVisible = true;
            Debug.Log("Pickup button shown");
        }
    }
    
    private void HidePickupButton()
    {
        if (pickupButton != null && isButtonVisible)
        {
            pickupButton.gameObject.SetActive(false);
            isButtonVisible = false;
            Debug.Log("Pickup button hidden");
        }
    }
    
    // Call this to manually end the pickup animation
    public void EndPickupAnimation()
    {
        if (playerAnimator != null && isPickingUp)
        {
            Debug.Log("Manually ending pickup animation");
            CompletePickup();
        }
    }
    
    // Call this to force stop pickup animation (emergency stop)
    public void ForceStopPickup()
    {
        if (isPickingUp)
        {
            Debug.Log("Force stopping pickup animation");
            
            playerAnimator.SetBool(pickupHash, false);
            isPickingUp = false;
            pickupTimer = 0f;
            
            // REMOVED: Audio stopping code - AudioHandler handles this globally
            
            // Re-enable movement
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
            
            // Show button again if there's a nearby potion
            if (currentNearbyPotion != null)
            {
                ShowPickupButton();
            }
            
            Debug.Log("Pickup animation force stopped");
        }
    }
    
    // Check if currently picking up
    public bool IsPickingUp()
    {
        return isPickingUp;
    }
    
    // Get the current nearby potion
    public GameObject GetCurrentNearbyPotion()
    {
        return currentNearbyPotion;
    }
    
    // Check if button is currently visible
    public bool IsButtonVisible()
    {
        return isButtonVisible;
    }
    
    // Get collection count
    public int GetCollectedCount()
    {
        return collectedPreservativeIDs.Count;
    }
    
    // Get all collected preservative IDs
    public List<string> GetCollectedPreservativeIDs()
    {
        return new List<string>(collectedPreservativeIDs);
    }
    
    // Check if specific preservative has been collected
    public bool HasCollectedPreservative(string preservativeID)
    {
        return collectedPreservativeIDs.Contains(preservativeID);
    }
    
    // Reset all collections
    public void ResetCollection()
    {
        collectedPreservativeIDs.Clear();
        Debug.Log("Preservative collection reset");
    }
    
    // Clean up
    private void OnDestroy()
    {
        // Remove the button listener to prevent memory leaks
        if (pickupButton != null)
        {
            pickupButton.onClick.RemoveAllListeners();
        }
    }
    
    // Visualize pickup range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // Draw line to current nearby potion if any
        if (currentNearbyPotion != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentNearbyPotion.transform.position);
        }
    }
    
    // Debug method
    [ContextMenu("Debug Nearby Potions")]
    public void DebugNearbyPotions()
    {
        GameObject[] preservativePotions = GameObject.FindGameObjectsWithTag("K3_PreservativePotion");
        
        Debug.Log($"=== K3 PRESERVATIVE POTIONS DEBUG ===");
        Debug.Log($"Potions in scene: {preservativePotions.Length}");
        Debug.Log($"Current nearby potion: {(currentNearbyPotion != null ? currentNearbyPotion.name : "None")}");
        Debug.Log($"Button visible: {isButtonVisible}");
        Debug.Log($"Is picking up: {isPickingUp}");
        Debug.Log($"Collected count: {collectedPreservativeIDs.Count}");
        Debug.Log($"Info Manager: {(infoManager != null ? "Assigned" : "NULL")}");
        
        foreach (string id in collectedPreservativeIDs)
        {
            Debug.Log($"  - Collected: {id}");
        }
    }
    
    // CHANGED: Test sound using AudioHandler
    [ContextMenu("Test Pickup Sound")]
    public void TestPickupSound()
    {
        if (pickupSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(pickupSound);
            Debug.Log("Test pickup sound played through AudioHandler");
        }
        else
        {
            Debug.LogWarning($"Cannot test pickup sound - PickupSound: {pickupSound != null}, AudioHandler.Instance: {AudioHandler.Instance != null}");
        }
    }
    
    // Auto-find preservative spawner
    [ContextMenu("Find Preservative Spawner")]
    public void FindPreservativeSpawner()
    {
        preservativeSpawner = FindObjectOfType<K3_PreservativeSpawner>();
        if (preservativeSpawner != null)
        {
            Debug.Log($"Found preservative spawner: {preservativeSpawner.name}");
        }
        else
        {
            Debug.LogWarning("No K3_PreservativeSpawner found in scene!");
        }
    }
    
    // Auto-find info manager
    [ContextMenu("Find Information Manager")]
    public void FindInformationManager()
    {
        infoManager = FindObjectOfType<PreservativesInformationManager>();
        if (infoManager != null)
        {
            Debug.Log($"Found PreservativesInformationManager: {infoManager.name}");
        }
        else
        {
            Debug.LogError("No PreservativesInformationManager found in scene!");
        }
    }
}