using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class ProductInformationManager : MonoBehaviour
{
    [Header("Data Reference")]
    public ProductData productDatabase;
    
    [Header("UI References")]
    public GameObject infoPanel; // Main popup panel
    public Transform productDisplaySpawnPoint; // Where to spawn product for showcase
    
    [Header("Text Fields - Popup Panel")]
    public TextMeshProUGUI productNameText;
    public TextMeshProUGUI sugarTypeText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI labelTipText;
    public TextMeshProUGUI funFactText;
    public TextMeshProUGUI collectionCountText; // For panel: "X/8"
    
    [Header("Text Fields - In-Game Display")]
    public TextMeshProUGUI inGameCollectionText; // NEW: For in-game display: "Collected Product Count: X/8"
    
    [Header("Colors")]
    public Color naturalSugarColor = Color.green;
    public Color addedSugarColor = Color.red;
    
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
    public string inGameCounterPrefix = "Collected Product: ";
    
    // Events for pausing game systems
    public static event Action OnProductPanelShown;
    public static event Action OnProductPanelHidden;
    
    // Session-based collection tracking
    private List<string> collectedProductIDs = new List<string>();
    private GameObject currentDisplayedProduct;
    private ProductData.ProductInfo currentProductInfo;
    
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
            confirmButton.onClick.AddListener(HideProductInfo);
        }
        
        // Initialize product database if needed
        if (productDatabase == null)
        {
            Debug.LogWarning("Product database not assigned! Looking for default...");
            productDatabase = Resources.Load<ProductData>("ProductData");
        }
        
        // Initialize in-game counter display
        UpdateInGameCollectionDisplay();
    }
    
    // Reset collection for new session
    public void ResetSessionCollection()
    {
        collectedProductIDs.Clear();
        UpdateAllCollectionDisplays();
        Debug.Log("Session collection reset. Starting fresh.");
    }
    
    private IEnumerator ShowPanelWithDelay()
    {
        yield return new WaitForSeconds(panelShowDelay);
        
        // Trigger panel shown event - this will pause monsters and timer
        OnProductPanelShown?.Invoke();
        
        if (infoPanel != null)
            infoPanel.SetActive(true);
        
        if (panelAnimator != null)
            panelAnimator.SetTrigger(showAnimationTrigger);
        
        // Disable player movement
        DisablePlayerMovement();
        
        Debug.Log($"Showing product info for: {currentProductInfo.displayName}");
    }
    
    private void UpdateProductUI(ProductData.ProductInfo productInfo)
    {
        // Basic information
        if (productNameText != null)
            productNameText.text = productInfo.displayName;
        
        if (sugarTypeText != null)
        {
            sugarTypeText.text = productInfo.productType == ProductData.ProductType.NaturalSugar ? 
                "Natural Sugar" : "Added Sugar";
            
            // Set color based on sugar type
            sugarTypeText.color = productInfo.productType == ProductData.ProductType.NaturalSugar ? 
                naturalSugarColor : addedSugarColor;
        }
        
        // Detailed information
        if (descriptionText != null)
            descriptionText.text = productInfo.description;
        
        if (labelTipText != null)
            labelTipText.text = productInfo.labelTip;
        
        if (funFactText != null)
            funFactText.text = productInfo.funFact;
        
        // Update collection count
        UpdateAllCollectionDisplays();
    }
    
    private void SpawnProductForDisplay(GameObject productPrefab)
    {
        // Clean up previously displayed product
        if (currentDisplayedProduct != null)
            Destroy(currentDisplayedProduct);
        
        if (productDisplaySpawnPoint != null && productPrefab != null)
        {
            // Spawn the product at the display location
            currentDisplayedProduct = Instantiate(
                productPrefab, 
                productDisplaySpawnPoint.position, 
                productDisplaySpawnPoint.rotation
            );
            
            // Optional: Adjust scale if needed
            currentDisplayedProduct.transform.localScale = Vector3.one;
            
            // Add rotation script for visual appeal
            ProductDisplayRotator rotator = currentDisplayedProduct.AddComponent<ProductDisplayRotator>();
            rotator.rotationSpeed = 30f;
            
            Debug.Log($"Spawned {productPrefab.name} for display");
        }
    }
    
    public void HideProductInfo()
    {
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
        
        // Clean up displayed product
        if (currentDisplayedProduct != null)
        {
            Destroy(currentDisplayedProduct);
            currentDisplayedProduct = null;
        }
    }
    
    private IEnumerator HidePanelAfterAnimation()
    {
        // Wait for animation to complete (adjust time based on your animation)
        yield return new WaitForSeconds(0.5f);
        
        if (infoPanel != null)
            infoPanel.SetActive(false);
        
        OnPanelHidden();
    }
    
    private void OnPanelHidden()
    {
        // Trigger panel hidden event - this will resume monsters and timer
        OnProductPanelHidden?.Invoke();
        
        // Re-enable player movement
        EnablePlayerMovement();
        
        Debug.Log("Product info panel hidden");
    }
    
    private void DisablePlayerMovement()
    {
        // Find and disable player movement
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = false;
        
        // Also disable any input
        UnityEngine.InputSystem.PlayerInput playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = false;
    }
    
    private void EnablePlayerMovement()
    {
        // Re-enable player movement
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = true;
        
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
        if (collectionCountText != null && productDatabase != null)
        {
            int collected = collectedProductIDs.Count;
            int total = productDatabase.GetTotalCount();
            collectionCountText.text = $"{collected}/{total}";
        }
    }
    
    private void UpdateInGameCollectionDisplay()
    {
        if (inGameCollectionText != null && showInGameCounter && productDatabase != null)
        {
            int collected = collectedProductIDs.Count;
            int total = productDatabase.GetTotalCount();
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
        return collectedProductIDs.Count;
    }
    
    public bool IsAllCollected()
    {
        return productDatabase != null && 
               collectedProductIDs.Count >= productDatabase.GetTotalCount();
    }
    
    public List<string> GetCollectedProductIDs()
    {
        return new List<string>(collectedProductIDs);
    }
    
    public bool IsProductCollected(string productID)
    {
        return collectedProductIDs.Contains(productID);
    }
    
    // Reset for new game session
    public void ResetForNewSession()
    {
        ResetSessionCollection();
        Debug.Log("Product collection reset for new session");
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
    
    // Context menu for testing
    [ContextMenu("Test Show Banana Info")]
    public void TestShowBananaInfo()
    {
        ShowProductInfo("BANANA");
    }
    
    [ContextMenu("Test Show Cookies Info")]
    public void TestShowCookiesInfo()
    {
        ShowProductInfo("COOKIES");
    }
    
    [ContextMenu("Reset Session Collection")]
    public void ResetCurrentSession()
    {
        ResetSessionCollection();
    }
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== COLLECTION STATUS ===");
        Debug.Log($"Total Products in Database: {productDatabase?.GetTotalCount()}");
        Debug.Log($"Products Collected This Session: {collectedProductIDs.Count}");
        Debug.Log($"Collected IDs: {string.Join(", ", collectedProductIDs)}");
        Debug.Log($"All Collected: {IsAllCollected()}");
        Debug.Log($"In-Game Counter Visible: {showInGameCounter}");
        Debug.Log($"In-Game Text Assigned: {inGameCollectionText != null}");
    }
    
    [ContextMenu("Test Add Collection")]
    public void TestAddCollection()
    {
        // Simulate collecting a product
        if (collectedProductIDs.Count < productDatabase.GetTotalCount())
        {
            string testID = $"TEST_{collectedProductIDs.Count + 1}";
            collectedProductIDs.Add(testID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added test collection: {testID}");
        }
    }

    // Add this method to the ProductInformationManager class:

  public void ForceAllMonstersToReturnToPatrol()
  {
      // Find all monsters in the scene
      MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
      
      foreach (MonsterObstacle monster in allMonsters)
      {
          if (monster != null && !monster.IsPaused())
          {
              monster.ForceReturnToPatrol();
          }
      }
      
      Debug.Log($"Forced {allMonsters.Length} monsters to return to patrol");
  }

    // Modify the ShowProductInfo method to call this:
    public void ShowProductInfo(string productID)
    {
        if (productDatabase == null)
        {
            Debug.LogError("No product database assigned!");
            return;
        }
        
        // Get product information
        currentProductInfo = productDatabase.GetProductInfo(productID);
        if (currentProductInfo == null)
        {
            Debug.LogError($"Product with ID '{productID}' not found in database!");
            return;
        }
        
        // Add to session collection if not already collected
        if (!collectedProductIDs.Contains(productID))
        {
            collectedProductIDs.Add(productID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added {productID} to session collection. Total: {collectedProductIDs.Count}");
        }
        
        // Update UI with product information
        UpdateProductUI(currentProductInfo);
        
        // Spawn product for display
        SpawnProductForDisplay(currentProductInfo.productPrefab);
        
        // Force all nearby monsters to return to patrol before showing panel
        ForceAllMonstersToReturnToPatrol();
        
        // Show the panel
        StartCoroutine(ShowPanelWithDelay());
    }

}

// Simple rotator script for displayed products
public class ProductDisplayRotator : MonoBehaviour
{
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;
    
    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}

