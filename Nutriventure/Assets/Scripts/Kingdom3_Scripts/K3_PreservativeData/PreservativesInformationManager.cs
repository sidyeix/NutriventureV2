using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class PreservativesInformationManager : MonoBehaviour
{
    [Header("Data Reference")]
    public K3_PreservativeData preservativeDatabase;
    
    [Header("UI References")]
    public GameObject infoPanel; // Main popup panel
    public Transform preservativeDisplaySpawnPoint; // Where to spawn preservative for showcase
    public Image preservativeIconDisplay; // Display for the preservative icon
    
    [Header("Text Fields - Popup Panel")]
    public TextMeshProUGUI preservativeNameText;
    public TextMeshProUGUI preservativeIDText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI strengthsLimitsText;
    public TextMeshProUGUI foundInText;
    public TextMeshProUGUI funFactText;
    public TextMeshProUGUI collectionCountText; // For panel: "X/Y"
    
    [Header("Text Fields - In-Game Display")]
    public TextMeshProUGUI inGameCollectionText; // For in-game display: "Preservatives Collected: X/Y"
    
    [Header("Buttons")]
    public Button confirmButton;
    
    [Header("Animation")]
    public Animator panelAnimator;
    public string showAnimationTrigger = "Show";
    public string hideAnimationTrigger = "Hide";
    public float panelShowDelay = 0.5f;
    
    [Header("In-Game Display Settings")]
    public bool showInGameCounter = true;
    public bool autoUpdateInGameCounter = true;
    public string inGameCounterPrefix = "Preservatives Collected: ";

    [Header("Display Settings")]
    public Vector3 displayScale = Vector3.one * 1.5f; // Scale for displayed preservative
    public bool autoRotateDisplay = true;
    public float rotationSpeed = 30f;
    
    // Events for pausing game systems
    public static event Action OnPreservativePanelShown;
    public static event Action OnPreservativePanelHidden;
    
    // Session-based collection tracking
    private List<string> collectedPreservativeIDs = new List<string>();
    private GameObject currentDisplayedPreservative;
    private K3_PreservativeData.PreservativeInfo currentPreservativeInfo;

    void Start()
    {
        InitializeUI();
        
        // Hide panel at start
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Reset collection at start of each session
        ResetSessionCollection();
    }
    
    private void InitializeUI()
    {
        // Set up button listeners
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HidePreservativeInfo);
        }
        else
        {
            Debug.LogWarning("Confirm button not assigned in inspector!");
        }
        
        // Initialize preservative database if needed
        if (preservativeDatabase == null)
        {
            Debug.LogError("Preservative database not assigned! Please assign in inspector.");
            preservativeDatabase = Resources.Load<K3_PreservativeData>("K3_PreservativeData");
        }
        
        // Initialize in-game counter display
        UpdateInGameCollectionDisplay();
    }
    
    // Reset collection for new session
    public void ResetSessionCollection()
    {
        collectedPreservativeIDs.Clear();
        UpdateAllCollectionDisplays();
        Debug.Log("Preservative collection reset. Starting fresh.");
    }
    
    private IEnumerator ShowPanelWithDelay()
    {
        yield return new WaitForSeconds(panelShowDelay);
        
        // Trigger panel shown event
        OnPreservativePanelShown?.Invoke();
        
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            Debug.Log("Info panel activated");
        }
        else
        {
            Debug.LogError("Info panel not assigned!");
        }
        
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger(showAnimationTrigger);
        }
        
        // Disable player movement
        DisablePlayerMovement();
        
        Debug.Log($"Showing preservative info for: {currentPreservativeInfo.displayName}");
    }
    
    private void UpdatePreservativeUI(K3_PreservativeData.PreservativeInfo preservativeInfo)
    {
        // Basic information
        if (preservativeNameText != null)
        {
            preservativeNameText.text = preservativeInfo.displayName;
        }
        else
        {
            Debug.LogWarning("Preservative name text not assigned!");
        }
        
        if (preservativeIDText != null)
        {
            preservativeIDText.text = $"ID: {preservativeInfo.preservativeID}";
        }
        
        // Detailed information
        if (descriptionText != null)
            descriptionText.text = preservativeInfo.preservDesc;
        
        if (strengthsLimitsText != null)
            strengthsLimitsText.text = preservativeInfo.strengthsLimits;
        
        if (foundInText != null)
            foundInText.text = preservativeInfo.foundIn;
        
        if (funFactText != null)
            funFactText.text = preservativeInfo.funFact;
        
        // Update preservative icon
        if (preservativeIconDisplay != null)
        {
            if (preservativeInfo.preservativeIcon != null)
            {
                preservativeIconDisplay.sprite = preservativeInfo.preservativeIcon;
                preservativeIconDisplay.gameObject.SetActive(true);
            }
            else
            {
                preservativeIconDisplay.gameObject.SetActive(false);
                Debug.LogWarning($"No icon for preservative: {preservativeInfo.displayName}");
            }
        }
        
        // Update collection count
        UpdateAllCollectionDisplays();
    }
    
    private void SpawnPreservativeForDisplay(GameObject preservativePrefab)
    {
        // Clean up previously displayed preservative
        if (currentDisplayedPreservative != null)
            Destroy(currentDisplayedPreservative);
        
        if (preservativeDisplaySpawnPoint != null && preservativePrefab != null)
        {
            // Spawn the preservative at the display location
            currentDisplayedPreservative = Instantiate(
                preservativePrefab, 
                preservativeDisplaySpawnPoint.position, 
                preservativeDisplaySpawnPoint.rotation
            );
            
            // Adjust scale
            currentDisplayedPreservative.transform.localScale = displayScale;
            
            // Add rotation script for visual appeal
            if (autoRotateDisplay)
            {
                PreservativeDisplayRotator rotator = currentDisplayedPreservative.AddComponent<PreservativeDisplayRotator>();
                rotator.rotationSpeed = rotationSpeed;
            }
            
            Debug.Log($"Spawned {preservativePrefab.name} for display");
        }
        else
        {
            Debug.LogWarning("Cannot spawn preservative for display: spawn point or prefab is null");
        }
    }
    
    public void HidePreservativeInfo()
    {
        Debug.Log("Hiding preservative info panel");
        
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger(hideAnimationTrigger);
            StartCoroutine(HidePanelAfterAnimation());
        }
        else
        {
            if (infoPanel != null)
                infoPanel.SetActive(false);
            
            OnPanelHidden();
        }
        
        // Clean up displayed preservative
        if (currentDisplayedPreservative != null)
        {
            Destroy(currentDisplayedPreservative);
            currentDisplayedPreservative = null;
        }
    }
    
    private IEnumerator HidePanelAfterAnimation()
    {
        // Wait for animation to complete
        yield return new WaitForSeconds(0.5f);
        
        if (infoPanel != null)
            infoPanel.SetActive(false);
        
        OnPanelHidden();
    }
    
    private void OnPanelHidden()
    {
        // Trigger panel hidden event - this will re-enable player movement
        OnPreservativePanelHidden?.Invoke();
        
        // Re-enable player movement
        EnablePlayerMovement();
        
        Debug.Log("Preservative info panel hidden");
    }
    
    private void DisablePlayerMovement()
    {
        Debug.Log("Disabling player movement");
        
        // Find and disable player movement
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
        {
            movementScript.enabled = false;
            Debug.Log("Player movement disabled");
        }
        else
        {
            Debug.LogWarning("Could not find ThirdPersonController to disable!");
        }
        
        // Also disable any input
        UnityEngine.InputSystem.PlayerInput playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log("Player input disabled");
        }
    }
    
    private void EnablePlayerMovement()
    {
        Debug.Log("Enabling player movement");
        
        // Re-enable player movement
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
        {
            movementScript.enabled = true;
            Debug.Log("Player movement enabled");
        }
        
        // Re-enable input
        UnityEngine.InputSystem.PlayerInput playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = true;
    }
    
    // Update all collection displays
    private void UpdateAllCollectionDisplays()
    {
        UpdatePanelCollectionDisplay();
        UpdateInGameCollectionDisplay();
    }
    
    private void UpdatePanelCollectionDisplay()
    {
        if (collectionCountText != null && preservativeDatabase != null)
        {
            int collected = collectedPreservativeIDs.Count;
            int total = preservativeDatabase.GetTotalCount();
            collectionCountText.text = $"{collected}/{total}";
        }
    }
    
    private void UpdateInGameCollectionDisplay()
    {
        if (inGameCollectionText != null && showInGameCounter && preservativeDatabase != null)
        {
            int collected = collectedPreservativeIDs.Count;
            int total = preservativeDatabase.GetTotalCount();
            inGameCollectionText.text = $"{inGameCounterPrefix}{collected}/{total}";
        }
    }
    
    // Public methods for external access
    public bool IsPanelVisible()
    {
        return infoPanel != null && infoPanel.activeInHierarchy;
    }
    
    public int GetCollectedCount()
    {
        return collectedPreservativeIDs.Count;
    }
    
    public bool IsAllCollected()
    {
        return preservativeDatabase != null && 
               collectedPreservativeIDs.Count >= preservativeDatabase.GetTotalCount();
    }
    
    public List<string> GetCollectedPreservativeIDs()
    {
        return new List<string>(collectedPreservativeIDs);
    }
    
    public bool IsPreservativeCollected(string preservativeID)
    {
        return collectedPreservativeIDs.Contains(preservativeID);
    }
    
    // Show preservative info when collected - CALLED BY COLLECTION SCRIPT
    public void ShowPreservativeInfo(string preservativeID)
    {
        Debug.Log($"ShowPreservativeInfo called with ID: {preservativeID}");
        
        if (preservativeDatabase == null)
        {
            Debug.LogError("No preservative database assigned!");
            // Still trigger hidden event to re-enable movement
            OnPreservativePanelHidden?.Invoke();
            return;
        }
        
        // Get preservative information
        currentPreservativeInfo = preservativeDatabase.GetPreservativeInfo(preservativeID);
        
        // Try to find the preservative if not found with exact ID
        if (currentPreservativeInfo == null)
        {
            Debug.LogWarning($"Preservative '{preservativeID}' not found. Searching...");
            
            // Try to find by partial match
            foreach (var preservative in preservativeDatabase.allPreservatives)
            {
                if (preservative == null) continue;
                
                // Check if the ID contains part of what we're looking for
                if (preservative.preservativeID.ToUpper().Contains(preservativeID.ToUpper()) || 
                    preservative.displayName.ToUpper().Contains(preservativeID.ToUpper()))
                {
                    currentPreservativeInfo = preservative;
                    Debug.Log($"Found match: {preservative.preservativeID} for search: {preservativeID}");
                    break;
                }
            }
        }
        
        if (currentPreservativeInfo == null)
        {
            Debug.LogError($"Preservative with ID '{preservativeID}' not found in database!");
            // Trigger panel hidden to re-enable movement
            OnPreservativePanelHidden?.Invoke();
            return;
        }
        
        // Add to session collection if not already collected
        string actualID = currentPreservativeInfo.preservativeID;
        if (!collectedPreservativeIDs.Contains(actualID))
        {
            collectedPreservativeIDs.Add(actualID);
            Debug.Log($"Added {actualID} to session collection. Total: {collectedPreservativeIDs.Count}");
            
            // Check if this was the last preservative
            if (IsAllCollected())
            {
                Debug.Log($"=== ALL {preservativeDatabase.GetTotalCount()} PRESERVATIVES COLLECTED ===");
                Debug.Log("All preservatives collected!");
            }
        }
        else
        {
            Debug.Log($"Preservative {actualID} already collected. Not adding to collection.");
        }
        
        // Update UI with preservative information
        UpdatePreservativeUI(currentPreservativeInfo);
        
        // Spawn preservative for display
        if (currentPreservativeInfo.preservativePrefab != null)
        {
            SpawnPreservativeForDisplay(currentPreservativeInfo.preservativePrefab);
        }
        else
        {
            Debug.LogWarning($"No prefab assigned for preservative: {currentPreservativeInfo.displayName}");
        }
        
        // Show the panel
        StartCoroutine(ShowPanelWithDelay());
    }
    
    // Show preservative info without adding to collection (for preview/UI)
    public void PreviewPreservativeInfo(string preservativeID)
    {
        if (preservativeDatabase == null)
        {
            Debug.LogError("No preservative database assigned!");
            return;
        }
        
        // Get preservative information
        currentPreservativeInfo = preservativeDatabase.GetPreservativeInfo(preservativeID);
        if (currentPreservativeInfo == null)
        {
            Debug.LogError($"Preservative with ID '{preservativeID}' not found in database!");
            return;
        }
        
        // Update UI with preservative information
        UpdatePreservativeUI(currentPreservativeInfo);
        
        // Spawn preservative for display
        SpawnPreservativeForDisplay(currentPreservativeInfo.preservativePrefab);
        
        // Show the panel
        StartCoroutine(ShowPanelWithDelay());
    }
    
    // Reset for new game session
    public void ResetForNewSession()
    {
        ResetSessionCollection();
        Debug.Log("Preservative collection reset for new session");
    }
    
    // Manually update the in-game counter (call this if auto-update is disabled)
    public void ManualUpdateInGameCounter()
    {
        UpdateInGameCollectionDisplay();
    }
    
    // Set the in-game counter visibility
    public void SetInGameCounterVisible(bool visible)
    {
        showInGameCounter = visible;
        if (inGameCollectionText != null)
        {
            inGameCollectionText.gameObject.SetActive(visible);
        }
        UpdateInGameCollectionDisplay();
    }
    
    // Change the counter prefix text
    public void SetCounterPrefix(string newPrefix)
    {
        inGameCounterPrefix = newPrefix;
        UpdateInGameCollectionDisplay();
    }
    
    // Show/Hide just the in-game counter (keeping panel counter visible)
    public void ShowInGameCounter()
    {
        SetInGameCounterVisible(true);
    }
    
    public void HideInGameCounter()
    {
        SetInGameCounterVisible(false);
    }
    
    // Get preservative info by ID
    public K3_PreservativeData.PreservativeInfo GetPreservativeInfo(string preservativeID)
    {
        if (preservativeDatabase != null)
        {
            return preservativeDatabase.GetPreservativeInfo(preservativeID);
        }
        return null;
    }
    
    // Get total count from database
    public int GetTotalPreservativeCount()
    {
        if (preservativeDatabase != null)
        {
            return preservativeDatabase.GetTotalCount();
        }
        return 0;
    }
    
    // Context menu for testing
    [ContextMenu("Test Show First Preservative")]
    public void TestShowFirstPreservative()
    {
        if (preservativeDatabase != null && preservativeDatabase.allPreservatives.Length > 0)
        {
            ShowPreservativeInfo(preservativeDatabase.allPreservatives[0].preservativeID);
        }
        else
        {
            Debug.LogError("Cannot test: database is null or empty!");
        }
    }
    
    [ContextMenu("Test Preview First Preservative")]
    public void TestPreviewFirstPreservative()
    {
        if (preservativeDatabase != null && preservativeDatabase.allPreservatives.Length > 0)
        {
            PreviewPreservativeInfo(preservativeDatabase.allPreservatives[0].preservativeID);
        }
    }
    
    [ContextMenu("Reset Session Collection")]
    public void ResetCurrentSession()
    {
        ResetSessionCollection();
    }
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== PRESERVATIVE COLLECTION STATUS ===");
        Debug.Log($"Database: {(preservativeDatabase != null ? preservativeDatabase.name : "NULL")}");
        Debug.Log($"Total Preservatives in Database: {preservativeDatabase?.GetTotalCount() ?? 0}");
        Debug.Log($"Preservatives Collected This Session: {collectedPreservativeIDs.Count}");
        Debug.Log($"Collected IDs: {string.Join(", ", collectedPreservativeIDs)}");
        Debug.Log($"All Collected: {IsAllCollected()}");
        Debug.Log($"Info Panel: {(infoPanel != null ? "Assigned" : "NULL")}");
        Debug.Log($"Panel Active: {IsPanelVisible()}");
        Debug.Log($"In-Game Counter Visible: {showInGameCounter}");
        Debug.Log($"In-Game Text Assigned: {inGameCollectionText != null}");
    }
    
    [ContextMenu("Test Add Collection")]
    public void TestAddCollection()
    {
        // Simulate collecting a preservative
        if (preservativeDatabase != null && collectedPreservativeIDs.Count < preservativeDatabase.GetTotalCount())
        {
            string testID = preservativeDatabase.allPreservatives[collectedPreservativeIDs.Count].preservativeID;
            collectedPreservativeIDs.Add(testID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added test collection: {testID}");
        }
    }
    
    [ContextMenu("Collect All Preservatives")]
    public void CollectAllPreservatives()
    {
        if (preservativeDatabase != null)
        {
            collectedPreservativeIDs.Clear();
            foreach (var preservative in preservativeDatabase.allPreservatives)
            {
                if (preservative != null)
                {
                    collectedPreservativeIDs.Add(preservative.preservativeID);
                }
            }
            UpdateAllCollectionDisplays();
            Debug.Log($"Collected all {collectedPreservativeIDs.Count} preservatives");
        }
    }
    
    [ContextMenu("Check UI References")]
    public void CheckUIReferences()
    {
        Debug.Log($"=== UI REFERENCES CHECK ===");
        Debug.Log($"Info Panel: {(infoPanel != null ? "✓" : "✗")}");
        Debug.Log($"Preservative Name Text: {(preservativeNameText != null ? "✓" : "✗")}");
        Debug.Log($"Description Text: {(descriptionText != null ? "✓" : "✗")}");
        Debug.Log($"Confirm Button: {(confirmButton != null ? "✓" : "✗")}");
        Debug.Log($"Panel Animator: {(panelAnimator != null ? "✓" : "✗")}");
    }
    
    // Simple rotator script for displayed preservatives
    public class PreservativeDisplayRotator : MonoBehaviour
    {
        public float rotationSpeed = 30f;
        public Vector3 rotationAxis = Vector3.up;
        
        void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}