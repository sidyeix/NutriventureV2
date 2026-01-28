using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class k4ProductInformationManager : MonoBehaviour
{   
    [Header("UI References")]
    public GameObject infoPanel;
    public Transform productDisplaySpawnPoint;
    
    [Header("Text Fields")]
    public TextMeshProUGUI productNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI labelTipText;
    public TextMeshProUGUI funFactText;
    public TextMeshProUGUI collectionCountText;
    public TextMeshProUGUI inGameCollectionText;
    public TextMeshProUGUI allergenTypeText;
    public TextMeshProUGUI allergenWarningText;
    
    [Header("Data")]
    public AllergenProductData allergenDatabase;
    
    [Header("Colors")]
    public Color safeColor = Color.green;
    public Color dangerColor = Color.red;
    
    [Header("Buttons")]
    public Button confirmButton;
    
    [Header("Animation")]
    public Animator panelAnimator;
    public string showAnimationTrigger = "Show";
    public string hideAnimationTrigger = "Hide";
    public float panelShowDelay = 0.5f;
    
    [Header("Settings")]
    public bool showInGameCounter = true;
    public bool autoUpdateInGameCounter = true;
    public string inGameCounterPrefix = "Collected: ";
    
    // Events
    public static event Action OnProductPanelShown;
    public static event Action OnProductPanelHidden;
    
    // Collection tracking
    public List<string> collectedProductIDs = new List<string>();
    private GameObject currentDisplayedProduct;
    private AllergenProductData.ProductInfo currentProductInfo;
    private bool isDummyProductDisplay = false;
    
    void Start()
    {
        InitializeUI();
        
        if (infoPanel != null)
            infoPanel.SetActive(false);

        ResetSessionCollection();
    }
    
    private void InitializeUI()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HideProductInfo);
        }
        
        if (allergenDatabase == null)
        {
            Debug.LogWarning("Product database not assigned!");
            allergenDatabase = Resources.Load<AllergenProductData>("Allergen_ProductData");
        }
        
        UpdateInGameCollectionDisplay();
    }
    
    public void ResetSessionCollection()
    {
        collectedProductIDs.Clear();
        UpdateAllCollectionDisplays();
        Debug.Log("Collection reset");
    }
    
    private IEnumerator ShowPanelWithDelay()
    {
        yield return new WaitForSeconds(panelShowDelay);
        
        OnProductPanelShown?.Invoke();
        
        if (infoPanel != null)
            infoPanel.SetActive(true);
        
        if (panelAnimator != null)
            panelAnimator.SetTrigger(showAnimationTrigger);
        
        DisablePlayerMovement();
        
        Debug.Log($"Showing product info for: {currentProductInfo?.displayName}");
    }
    
    private void UpdateProductUI(AllergenProductData.ProductInfo productInfo, bool isDummy = false)
    {
        if (productNameText != null)
        {
            productNameText.text = isDummy ? $"{productInfo.displayName} (Demo)" : productInfo.displayName;
        }

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

        if (allergenWarningText != null)
        {
            allergenWarningText.text = productInfo.containsAllergen
                ? productInfo.allergenWarning
                : "This food does not contain any of the Big Nine Allergens.";
        }

        if (descriptionText != null) descriptionText.text = productInfo.description;
        if (labelTipText != null) labelTipText.text = productInfo.labelTip;
        if (funFactText != null) funFactText.text = productInfo.funFact;

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

        currentDisplayedProduct = Instantiate(productPrefab);
        currentDisplayedProduct.transform.SetParent(productDisplaySpawnPoint, false);

        Vector3 rotation = new Vector3(0, 180, 0);
        if (currentProductInfo != null && currentProductInfo.productID == "milk")
        {
            rotation = new Vector3(90, 0, 0);
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

        Collider col = currentDisplayedProduct.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Add rotator
        if (currentProductInfo != null && currentProductInfo.productID.Equals("MILK", StringComparison.OrdinalIgnoreCase))
        {
            Transform meshRoot = currentDisplayedProduct.transform.GetChild(0);
            meshRoot.gameObject.AddComponent<ProductDisplayRotator>();
        }
        else
        {
            currentDisplayedProduct.AddComponent<ProductDisplayRotator>();
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
        
        if (currentDisplayedProduct != null)
        {
            Destroy(currentDisplayedProduct);
            currentDisplayedProduct = null;
        }
        
        // Check if last product collected
        if (!isDummyProductDisplay && allergenDatabase != null && 
            collectedProductIDs.Count >= allergenDatabase.GetAllergenCount())
        {
            Debug.Log("=== ALL PRODUCTS COLLECTED ===");
            // You can trigger your playable director here if you want
            // Example: FindObjectOfType<PlayableDirector>()?.Play();
        }
        
        isDummyProductDisplay = false;
    }
    
    private IEnumerator HidePanelAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (infoPanel != null)
            infoPanel.SetActive(false);
        
        OnPanelHidden();
    }
    
    private void OnPanelHidden()
    {
        OnProductPanelHidden?.Invoke();
        EnablePlayerMovement();
        Debug.Log("Product info panel hidden");
    }
    
    private void DisablePlayerMovement()
    {
        MonoBehaviour movementScript = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
        if (movementScript != null) movementScript.enabled = false;
        
        UnityEngine.InputSystem.PlayerInput playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
    
    private void EnablePlayerMovement()
    {
        MonoBehaviour movementScript = FindAnyObjectByType<StarterAssets.ThirdPersonController>();
        if (movementScript != null) movementScript.enabled = true;
        
        UnityEngine.InputSystem.PlayerInput playerInput = FindAnyObjectByType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = true;
    }
    
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
    
    // Public methods
    public bool IsPanelVisible() => infoPanel != null && infoPanel.activeInHierarchy;
    public int GetCollectedCount() => collectedProductIDs.Count;
    public bool IsAllCollected() => allergenDatabase != null && collectedProductIDs.Count >= allergenDatabase.GetAllergenCount();
    public List<string> GetCollectedProductIDs() => new List<string>(collectedProductIDs);
    public bool IsProductCollected(string productID) => collectedProductIDs.Contains(productID);
    
    public void ShowProductInfoForDummy(string productID)
    {
        if (allergenDatabase == null)
        {
            Debug.LogError("No product database assigned!");
            return;
        }
        
        currentProductInfo = allergenDatabase.GetProductInfo(productID);
        if (currentProductInfo == null)
        {
            string alternativeID = productID.Replace("_DUMMY", "").Replace("DUMMY_", "");
            currentProductInfo = allergenDatabase.GetProductInfo(alternativeID);
            
            if (currentProductInfo == null)
            {
                Debug.LogError($"Product with ID '{productID}' not found!");
                return;
            }
        }
        
        isDummyProductDisplay = true;
        UpdateProductUI(currentProductInfo, true);
        SpawnProductForDisplay(currentProductInfo.productPrefab);
        StartCoroutine(ShowPanelWithDelay());
        
        Debug.Log($"Showing dummy product: {productID}");
    }
    
    public void ShowProductInfo(string productID)
    {
        if (allergenDatabase == null)
        {
            Debug.LogError("No product database assigned!");
            return;
        }
        
        currentProductInfo = allergenDatabase.GetProductInfo(productID);
        if (currentProductInfo == null)
        {
            Debug.LogError($"Product with ID '{productID}' not found!");
            return;
        }
        
        isDummyProductDisplay = false;
        
        if (!collectedProductIDs.Contains(productID))
        {
            collectedProductIDs.Add(productID);
            UpdateAllCollectionDisplays();
            Debug.Log($"Added {productID}. Total: {collectedProductIDs.Count}");
            
            if (IsAllCollected())
            {
                Debug.Log("=== ALL PRODUCTS COLLECTED ===");
                // Trigger your playable director here when last allergen is collected
                // Example: PlayableDirector director = FindObjectOfType<PlayableDirector>();
                // if (director != null) director.Play();
            }
        }
        else
        {
            Debug.Log($"Product {productID} already collected.");
        }
        
        UpdateProductUI(currentProductInfo, false);
        SpawnProductForDisplay(currentProductInfo.productPrefab);
        StartCoroutine(ShowPanelWithDelay());
    }
    
    public void ResetForNewSession() => ResetSessionCollection();
    public void ManualUpdateInGameCounter() => UpdateInGameCollectionDisplay();
    
    public void SetInGameCounterVisible(bool visible)
    {
        showInGameCounter = visible;
        if (inGameCollectionText != null) inGameCollectionText.gameObject.SetActive(visible);
        UpdateInGameCollectionDisplay();
    }
    
    public void SetCounterPrefix(string newPrefix)
    {
        inGameCounterPrefix = newPrefix;
        UpdateInGameCollectionDisplay();
    }
    
    public void ShowInGameCounter() => SetInGameCounterVisible(true);
    public void HideInGameCounter() => SetInGameCounterVisible(false);
    
    // Debug/testing
    [ContextMenu("Test Show Banana Info")]
    public void TestShowBananaInfo() => ShowProductInfo("BANANA");
    
    [ContextMenu("Test Show Cookies Info")]
    public void TestShowCookiesInfo() => ShowProductInfo("COOKIES");
    
    [ContextMenu("Test Show Dummy Soda")]
    public void TestShowDummySoda() => ShowProductInfoForDummy("SODA");
    
    [ContextMenu("Reset Session Collection")]
    public void ResetCurrentSession() => ResetSessionCollection();
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== COLLECTION STATUS ===");
        Debug.Log($"Total Products: {allergenDatabase?.GetAllergenCount()}");
        Debug.Log($"Collected: {collectedProductIDs.Count}");
        Debug.Log($"All Collected: {IsAllCollected()}");
    }
    
    // Simple rotator script
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