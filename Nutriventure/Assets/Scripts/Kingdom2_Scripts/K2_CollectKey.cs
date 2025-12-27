using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class K2_CollectKey : MonoBehaviour
{
    [Header("Key Settings")]
    public float pickupRange = 3f;
    public string keyTag = "NutriKey";
    
    [Header("UI References")]
    public Button pickupButton; // UI button for key pickup
    public GameObject keyIndicator; // Optional UI indicator showing key is collected
    
    [Header("Animation Settings")]
    public Animator playerAnimator;
    public string pickupAnimationParameter = "IsPickingUp";
    public float pickupAnimationDuration = 0.5f;
    
    [Header("Player Movement")]
    public MonoBehaviour playerMovementScript;
    
    [Header("Audio Settings")]
    public AudioClip pickupSound;
    public float pickupSoundVolume = 0.7f;
    public float soundPlayDelay = 0.2f;
    
    [Header("Game Summary Reference")]
    public K2_GameSummary gameSummaryManager; // Reference to GameSummary script
    
    [Header("QA2 System Reference")]
    public K2_QA2system qa2System; // Reference to QA2 system to check completion
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onKeyCollected; // Event triggered when key is collected
    
    // Key collection state
    private bool hasKey = false;
    private bool isPickingUp = false;
    private bool hasTriggeredSummary = false;
    private float pickupTimer = 0f;
    private GameObject currentNearbyKey = null;
    private AudioSource audioSource;
    private int pickupHash;
    
    // Static flag for global reset
    private static bool globalResetFlag = false;
    
    void Start()
    {
        // Check global reset flag first
        if (globalResetFlag)
        {
            ResetKey();
            globalResetFlag = false;
            Debug.Log("Global reset flag detected - resetting key state");
        }
        
        // Get animator if not assigned
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }
        
        // Get player movement script if not assigned
        if (playerMovementScript == null)
        {
            playerMovementScript = GetComponent<StarterAssets.ThirdPersonController>();
        }
        
        // Set up AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Convert animation parameter to hash
        pickupHash = Animator.StringToHash(pickupAnimationParameter);
        
        // Set up pickup button
        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(OnPickupButtonClicked);
            pickupButton.gameObject.SetActive(false);
        }
        
        // Initialize key indicator
        if (keyIndicator != null)
        {
            keyIndicator.SetActive(false);
        }
        
        // Find QA2 system if not assigned
        if (qa2System == null)
        {
            qa2System = FindObjectOfType<K2_QA2system>();
        }
        
        // Find GameSummary if not assigned
        if (gameSummaryManager == null)
        {
            gameSummaryManager = FindObjectOfType<K2_GameSummary>();
            if (gameSummaryManager != null)
            {
                Debug.Log("Found GameSummary manager: " + gameSummaryManager.gameObject.name);
            }
            else
            {
                Debug.LogError("GameSummary manager not found! Make sure it exists in the scene.");
            }
        }
        
        Debug.Log("K2_CollectKey initialized. Has key: " + hasKey + ", Has triggered summary: " + hasTriggeredSummary);
    }
    
    void Update()
    {
        // Handle pickup animation timer
        if (isPickingUp)
        {
            pickupTimer += Time.deltaTime;
            
            // End animation after duration
            if (pickupTimer >= pickupAnimationDuration)
            {
                EndPickupAnimation();
            }
        }
        else if (!hasKey && !hasTriggeredSummary && !isPickingUp) // Only check for keys if we don't already have one AND haven't triggered summary
        {
            CheckForNearbyKeys();
        }
    }
    
    void CheckForNearbyKeys()
    {
        // If we've already triggered summary, don't look for keys
        if (hasTriggeredSummary || isPickingUp) return;
        
        // Find all objects with the key tag
        GameObject[] keys = GameObject.FindGameObjectsWithTag(keyTag);
        
        GameObject closestKey = null;
        float closestDistance = float.MaxValue;
        
        // Find the closest key within pickup range
        foreach (GameObject key in keys)
        {
            if (key == null) continue;
            
            float distance = Vector3.Distance(transform.position, key.transform.position);
            if (distance < pickupRange && distance < closestDistance)
            {
                closestKey = key;
                closestDistance = distance;
            }
        }
        
        // Update current nearby key
        if (closestKey != null && closestDistance <= pickupRange)
        {
            currentNearbyKey = closestKey;
            ShowPickupButton();
        }
        else if (currentNearbyKey != null)
        {
            currentNearbyKey = null;
            HidePickupButton();
        }
    }
    
    void OnPickupButtonClicked()
    {
        if (isPickingUp || currentNearbyKey == null || hasKey || hasTriggeredSummary) return;
        
        StartCoroutine(PickupKey());
    }
    
    private IEnumerator PickupKey()
    {
        Debug.Log($"Starting key pickup: {currentNearbyKey.name}");
        
        // Disable player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }
        
        // Hide pickup button immediately
        HidePickupButton();
        
        // Start pickup animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(pickupHash, true);
            isPickingUp = true;
            pickupTimer = 0f;
        }
        
        // Play pickup sound with delay
        if (audioSource != null && pickupSound != null)
        {
            StartCoroutine(PlayPickupSoundWithDelay());
        }
        
        // Wait for animation to complete
        yield return new WaitForSeconds(pickupAnimationDuration);
        
        // Complete the key pickup
        CompleteKeyPickup();
    }
    
    private IEnumerator PlayPickupSoundWithDelay()
    {
        yield return new WaitForSeconds(soundPlayDelay);
        audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
    }
    
    private void CompleteKeyPickup()
    {
        // End animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(pickupHash, false);
        }
        
        isPickingUp = false;
        pickupTimer = 0f;
        
        // Mark key as collected
        hasKey = true;
        
        // Show key indicator in UI
        if (keyIndicator != null)
        {
            keyIndicator.SetActive(true);
        }
        
        // Trigger collection event
        onKeyCollected?.Invoke();
        
        // Destroy the key object
        if (currentNearbyKey != null)
        {
            Debug.Log($"Key collected: {currentNearbyKey.name}");
            Destroy(currentNearbyKey);
            currentNearbyKey = null;
        }
        
        // DEBUG: Check if summary manager is available
        if (gameSummaryManager == null)
        {
            Debug.LogError("GameSummary manager is null! Cannot trigger summary.");
            // Re-enable player movement as fallback
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
            return;
        }
        
        // Trigger Game Summary when key is collected
        TriggerGameSummaryIfNeeded();
    }
    
    private void EndPickupAnimation()
    {
        CompleteKeyPickup();
    }
    
    void ShowPickupButton()
    {
        if (pickupButton != null && !pickupButton.gameObject.activeSelf && !hasTriggeredSummary && !isPickingUp)
        {
            pickupButton.gameObject.SetActive(true);
        }
    }
    
    void HidePickupButton()
    {
        if (pickupButton != null && pickupButton.gameObject.activeSelf)
        {
            pickupButton.gameObject.SetActive(false);
        }
    }
    
    // Trigger summary when key is collected
    private void TriggerGameSummaryIfNeeded()
    {
        Debug.Log("TriggerGameSummaryIfNeeded() called from key collection");
        Debug.Log($"Current state - hasTriggeredSummary: {hasTriggeredSummary}, gameSummaryManager: {gameSummaryManager != null}");
        
        // Always trigger summary when key is collected (key is an alternate win condition)
        if (gameSummaryManager != null && !hasTriggeredSummary)
        {
            Debug.Log("Key collected! Triggering game summary via TestWin()...");
            
            // Mark that we've triggered the summary
            hasTriggeredSummary = true;
            
            // IMPORTANT: Set the victory state BEFORE calling TestWin
            // Use reflection to set isVictory to true
            System.Reflection.FieldInfo victoryField = gameSummaryManager.GetType().GetField("isVictory", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (victoryField != null)
            {
                victoryField.SetValue(gameSummaryManager, true);
                Debug.Log("Set isVictory to true for key collection win");
            }
            
            // Call TestWin method - this will show the summary panel
            gameSummaryManager.TestWin();
            
            // DO NOT re-enable player movement - GameSummary will handle disabling/enabling
        }
        else if (hasTriggeredSummary)
        {
            Debug.Log("Summary already triggered by this key collection. Skipping.");
        }
        else if (gameSummaryManager == null)
        {
            Debug.LogError("GameSummary manager is null! Cannot trigger summary.");
            
            // Re-enable player movement as fallback
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
        }
    }
    
    // Public methods to interact with the key system
    
    public bool HasKey()
    {
        return hasKey;
    }
    
    public void UseKey()
    {
        if (hasKey)
        {
            hasKey = false;
            if (keyIndicator != null)
            {
                keyIndicator.SetActive(false);
            }
            Debug.Log("Key used");
        }
    }
    
    // ResetKey method - properly reset all states
    public void ResetKey()
    {
        hasKey = false;
        hasTriggeredSummary = false; // Reset this flag
        isPickingUp = false;
        pickupTimer = 0f;
        currentNearbyKey = null;
        
        // Reset animator if needed
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(pickupHash, false);
        }
        
        // Hide key indicator
        if (keyIndicator != null)
        {
            keyIndicator.SetActive(false);
        }
        
        // Hide pickup button
        HidePickupButton();
        
        // Re-enable movement if it was disabled
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }
        
        Debug.Log("Key state FULLY reset. hasKey: " + hasKey + ", hasTriggeredSummary: " + hasTriggeredSummary);
    }
    
    public void ForceStopPickup()
    {
        if (isPickingUp)
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetBool(pickupHash, false);
            }
            
            isPickingUp = false;
            pickupTimer = 0f;
            
            // Re-enable movement
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
            
            // Show button again if there's still a key nearby
            if (currentNearbyKey != null && !hasTriggeredSummary)
            {
                ShowPickupButton();
            }
        }
    }
    
    // Public method to check if summary was triggered
    public bool HasTriggeredSummary()
    {
        return hasTriggeredSummary;
    }
    
    // Method to check if pickup is in progress
    public bool IsPickingUp()
    {
        return isPickingUp;
    }
    
    // Force reset all states (for GameSummary to call)
    public void ForceFullReset()
    {
        ResetKey();
        Debug.Log("ForceFullReset called on K2_CollectKey");
    }
    
    // Static method for global reset
    public static void GlobalResetAllKeys()
    {
        globalResetFlag = true;
        Debug.Log("Global key reset flag set");
    }
    
    // OnDestroy to clean up
    void OnDestroy()
    {
        // Remove button listener
        if (pickupButton != null)
        {
            pickupButton.onClick.RemoveListener(OnPickupButtonClicked);
        }
    }
}