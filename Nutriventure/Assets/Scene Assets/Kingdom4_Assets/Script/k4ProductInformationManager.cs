using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class k4ProductInformationManager : MonoBehaviour
{   
    [Header("UI References")]
    public GameObject infoPanel; // Main popup panel
    public Transform productDisplaySpawnPoint; // Where to spawn product for showcase
    
    [Header("Text Fields - Popup Panel")]
    public TextMeshProUGUI productNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI labelTipText;
    public TextMeshProUGUI funFactText;
    public TextMeshProUGUI collectionCountText; // For panel: "X/8"
    
    [Header("Text Fields - In-Game Display")]
    public TextMeshProUGUI inGameCollectionText; // For in-game display: "Collected Product Count: X/8"
    public AllergenProductData allergenDatabase;
public TextMeshProUGUI allergenTypeText;
public TextMeshProUGUI allergenWarningText;

public Color safeColor = Color.green;
public Color dangerColor = Color.red;

    
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
    
    // Session-based collection tracking (only counts regular products)
    public List<string> collectedProductIDs = new List<string>();
    private GameObject currentDisplayedProduct;
    private AllergenProductData.ProductInfo currentProductInfo;

    private K2_DummypTimeline timelineController;
    
    // Track if this is a dummy product display
    private bool isDummyProductDisplay = false;
    
    void Start()
    {
        InitializeUI();
        
        // Hide panel at start
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Get reference to timeline controller
        timelineController = FindAnyObjectByType<K2_DummypTimeline>();
        if (timelineController == null)
        {
            Debug.LogWarning("K2_DummypTimeline controller not found!");
        }
        else
        {
            Debug.Log("Found K2_DummypTimeline controller");
        }
            
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
        if (allergenDatabase == null)
        {
            Debug.LogWarning("Product database not assigned! Looking for default...");
            allergenDatabase = Resources.Load<AllergenProductData>("Allergen_ProductData");
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
        
        string displayName = isDummyProductDisplay ? 
            $"{currentProductInfo.displayName} (Demo)" : 
            currentProductInfo.displayName;
        
        Debug.Log($"Showing product info for: {displayName}");
    }
    
    private void UpdateProductUI(AllergenProductData.ProductInfo productInfo, bool isDummy = false)
{
    // Product name
    if (productNameText != null)
    {
        productNameText.text = isDummy
            ? $"{productInfo.displayName} (Demo)"
            : productInfo.displayName;
    }

    // Allergen type display
    if (allergenTypeText != null)
    {
        if (productInfo.containsAllergen)
        {
            allergenTypeText.text = $"Contains: {productInfo.allergenType}";
            allergenTypeText.color = dangerColor;
        }
        else
        {
            allergenTypeText.text = "Allergen-Free";
            allergenTypeText.color = safeColor;
        }
    }

    // Allergen warning
    if (allergenWarningText != null)
    {
        allergenWarningText.text = productInfo.containsAllergen
            ? productInfo.allergenWarning
            : "This food does not contain any of the Big Nine Allergens.";
    }

    // Educational texts
    if (descriptionText != null)
        descriptionText.text = productInfo.description;

    if (labelTipText != null)
        labelTipText.text = productInfo.labelTip;

    if (funFactText != null)
        funFactText.text = productInfo.funFact;

    // Update collection count
    if (!isDummy)
    {
        UpdateAllCollectionDisplays();
    }
}

    
    private void SpawnProductForDisplay(GameObject productPrefab)
{
    if (currentDisplayedProduct != null)
        Destroy(currentDisplayedProduct);

    if (productDisplaySpawnPoint == null || productPrefab == null)
    {
        Debug.LogError("ProductSpawnPoint NOT assigned!");
        return;
    }

    // Spawn first (no parent yet)
    currentDisplayedProduct = Instantiate(productPrefab);

    // 🔒 FORCE parent to ProductSpawnPoint
    currentDisplayedProduct.transform.SetParent(productDisplaySpawnPoint, false);

    // Reset local transform
    // Default rotation for most products
Vector3 rotation = new Vector3(0, 180, 0);

// 🥛 Milk needs a different facing direction
if (currentProductInfo != null && currentProductInfo.productID == "milk")
{
    rotation = new Vector3(90, 0, 0);
}
// 🥛 Milk has an Animator that overrides rotation — disable it ONLY for Milk
if (currentProductInfo != null && currentProductInfo.productID == "milk")
{
    Animator animator = currentDisplayedProduct.GetComponent<Animator>();
    if (animator != null)
    {
        animator.enabled = false;
    }
}


currentDisplayedProduct.transform.localRotation = Quaternion.Euler(rotation);

    currentDisplayedProduct.transform.localScale = Vector3.one * 0.6f;

    // Disable physics
    Rigidbody rb = currentDisplayedProduct.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Disable colliders
    Collider col = currentDisplayedProduct.GetComponent<Collider>();
    if (col != null)
        col.enabled = false;

    // Rotate for showcase
    // Add rotator
if (currentProductInfo.productID.Equals("MILK", StringComparison.OrdinalIgnoreCase))
{
    // Rotate the mesh child instead of root
    Transform meshRoot = currentDisplayedProduct.transform.GetChild(0);
    meshRoot.gameObject.AddComponent<ProductDisplayRotator>();
}
else
{
    currentDisplayedProduct.AddComponent<ProductDisplayRotator>();
}

    Debug.Log("✅ Product FORCED under ProductSpawnPoint");
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
            
            // Check if this was a dummy product display
            if (isDummyProductDisplay && timelineController != null)
            {
                // Start the second timeline for dummy product
                timelineController.StartSecondCutscene();
            }
            
            OnPanelHidden();
        }
        
        // Clean up displayed product
        if (currentDisplayedProduct != null)
        {
            Destroy(currentDisplayedProduct);
            currentDisplayedProduct = null;
        }
        
        // Check if this was the last product collection (for third cutscene)
        if (!isDummyProductDisplay && allergenDatabase != null && collectedProductIDs.Count >= allergenDatabase.GetAllergenCount())
        {
            Debug.Log("=== LAST PRODUCT COLLECTED ===");
            Debug.Log($"Collection complete: {collectedProductIDs.Count}/{allergenDatabase.GetAllergenCount()}");
            
            if (timelineController != null)
            {
                Debug.Log("Notifying timeline controller about last product collection...");
                timelineController.OnLastProductCollected();
            }
            else
            {
                Debug.LogError("Timeline controller not found!");
            }
        }
        
        // Reset dummy product flag
        isDummyProductDisplay = false;
    }
    
    private IEnumerator HidePanelAfterAnimation()
    {
        // Wait for animation to complete
        yield return new WaitForSeconds(0.5f);
        
        if (infoPanel != null)
            infoPanel.SetActive(false);
        
        // Check if this was a dummy product display
        if (isDummyProductDisplay && timelineController != null)
        {
            // Start the second timeline for dummy product
            timelineController.StartSecondCutscene();
        }
        
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
        MonoBehaviour movementScript = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = false;
        
        // Also disable any input
        UnityEngine.InputSystem.PlayerInput playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = false;
    }
    
    private void EnablePlayerMovement()
    {
        // Re-enable player movement
        MonoBehaviour movementScript = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = true;
        
        // Re-enable input
        UnityEngine.InputSystem.PlayerInput playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
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
        if (collectionCountText != null && allergenDatabase != null)
        {
            int collected = collectedProductIDs.Count;
            int total = allergenDatabase.GetAllergenCount();
            collectionCountText.text = $"{collected}/{total}";
        }
    }
    
    private void UpdateInGameCollectionDisplay()
    {
        if (inGameCollectionText != null && showInGameCounter && allergenDatabase != null)
        {
            int collected = collectedProductIDs.Count;
            int total = allergenDatabase.GetAllergenCount();
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
        return allergenDatabase != null && 
               collectedProductIDs.Count >= allergenDatabase.GetAllergenCount();
    }
    
    public List<string> GetCollectedProductIDs()
    {
        return new List<string>(collectedProductIDs);
    }
    
    public bool IsProductCollected(string productID)
    {
        return collectedProductIDs.Contains(productID);
    }
    
    // Show product info for dummy products (doesn't add to collection)
    public void ShowProductInfoForDummy(string productID)
    {
        if (allergenDatabase == null)
        {
            Debug.LogError("No product database assigned!");
            return;
        }
        
        // Get product information
        currentProductInfo = allergenDatabase.GetProductInfo(productID);
        if (currentProductInfo == null)
        {
            // Try alternative ID if not found
            string alternativeID = productID.Replace("_DUMMY", "").Replace("DUMMY_", "");
            currentProductInfo = allergenDatabase.GetProductInfo(alternativeID);
            
            if (currentProductInfo == null)
            {
                Debug.LogError($"Product with ID '{productID}' not found in database!");
                return;
            }
        }
        
        // Set dummy product flag
        isDummyProductDisplay = true;
        
        // Update UI with product information (marked as dummy)
        UpdateProductUI(currentProductInfo, true);
        
        // Spawn product for display
        SpawnProductForDisplay(currentProductInfo.productPrefab);
        
        // Show the panel
        StartCoroutine(ShowPanelWithDelay());
        
        Debug.Log($"Showing dummy product info for: {productID} (not counted in collection)");
    }
    
    // Show product info for regular products (adds to collection)
    public void ShowProductInfo(string productID)
    {
        if (allergenDatabase == null)
        {
            Debug.LogError("No product database assigned!");
            return;
        }
        
        // Get product information
        currentProductInfo = allergenDatabase.GetProductInfo(productID);
        if (currentProductInfo == null)
        {
            Debug.LogError($"Product with ID '{productID}' not found in database!");
            return;
        }
        
        // Set dummy product flag to false
        isDummyProductDisplay = false;
        
        // Add to session collection if not already collected
        if (!collectedProductIDs.Contains(productID))
        {
            collectedProductIDs.Add(productID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added {productID} to session collection. Total: {collectedProductIDs.Count}");
            
            // Check if this was the last product
            if (IsAllCollected())
            {
                Debug.Log("=== ALL 8 PRODUCTS COLLECTED ===");
                Debug.Log("All products collected! This will trigger third cutscene after panel closes.");
                
                // Notify the timeline controller that all products are collected
                if (timelineController != null)
                {
                    timelineController.OnLastProductCollected();
                }
                else
                {
                    Debug.LogError("K2_DummypTimeline controller not found!");
                }
            }
        }
        else
        {
            Debug.Log($"Product {productID} already collected. Not adding to collection.");
        }
        
        // Update UI with product information
        UpdateProductUI(currentProductInfo, false);
        
        // Spawn product for display
        SpawnProductForDisplay(currentProductInfo.productPrefab);
        
        // Show the panel
        StartCoroutine(ShowPanelWithDelay());
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
    
    [ContextMenu("Test Show Dummy Soda")]
    public void TestShowDummySoda()
    {
        ShowProductInfoForDummy("SODA");
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
        Debug.Log($"Total Products in Database: {allergenDatabase?.GetAllergenCount()}");
        Debug.Log($"Products Collected This Session: {collectedProductIDs.Count}");
        Debug.Log($"Collected IDs: {string.Join(", ", collectedProductIDs)}");
        Debug.Log($"All Collected: {IsAllCollected()}");
        Debug.Log($"In-Game Counter Visible: {showInGameCounter}");
        Debug.Log($"In-Game Text Assigned: {inGameCollectionText != null}");
        
        // Check timeline controller
        if (timelineController == null)
        {
            timelineController = FindAnyObjectByType<K2_DummypTimeline>();
        }
        Debug.Log($"Timeline Controller Found: {timelineController != null}");
    }
    
    [ContextMenu("Test Add Collection")]
    public void TestAddCollection()
    {
        // Simulate collecting a product
        if (collectedProductIDs.Count < allergenDatabase.GetAllergenCount())
        {
            string testID = $"TEST_{collectedProductIDs.Count + 1}";
            collectedProductIDs.Add(testID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added test collection: {testID}");
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
    
}