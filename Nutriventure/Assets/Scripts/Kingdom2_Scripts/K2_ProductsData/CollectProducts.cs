using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CollectProducts : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator playerAnimator;
    public string pickupParameterName = "IsPickingUp";
    
    [Header("Pickup Settings")]
    public float pickupAnimationDuration = 0.5f;
    public float pickupRange = 3f; // How close player needs to be to show button
    
    [Header("UI References - Regular Products")]
    public Button pickupButton; // Assign your UI pickup button here
    
    [Header("UI References - Dummy Products")]
    public Button dummyPickupButton; // Separate button for dummy products
    
    [Header("Instruction UI for Dummy Products")]
    public RawImage instructionImage1; // Instruction when near DummyProduct
    public RawImage instructionImage2; // Instruction after collecting DummyProduct and info panel shows
    
    [Header("Player Movement")]
    public MonoBehaviour playerMovementScript; // Assign your ThirdPersonController here
    
    [Header("Audio Settings")]
    public AudioClip pickupSound; // Assign your pickup sound here
    public float pickupSoundVolume = 0.7f;
    public float soundPlayDelay = 0.2f; // Delay to align with animation
    public bool playSoundOnPickup = true;
    
    private int pickupHash;
    private bool isPickingUp = false;
    private float pickupTimer = 0f;
    private GameObject currentNearbyProduct = null;
    private bool isRegularButtonVisible = false;
    private bool isDummyButtonVisible = false;
    private AudioSource audioSource;
    private bool isNearDummyProduct = false;
    private bool hasCollectedDummyProduct = false;
    
    // Track product type
    public enum ProductType { Regular, Dummy }
    private ProductType currentProductType = ProductType.Regular;
    
    // Events for other systems
    public System.Action<GameObject> OnPickupStart;
    public System.Action<GameObject> OnPickupComplete;
    
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
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 10f;
        }
        
        // Convert parameter name to hash for better performance
        pickupHash = Animator.StringToHash(pickupParameterName);
        
        // Set up the regular pickup button
        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(() => OnPickupButtonClicked(ProductType.Regular));
            pickupButton.gameObject.SetActive(false); // Hide by default
            isRegularButtonVisible = false;
            Debug.Log("Regular pickup button listener added - button hidden by default");
        }
        else
        {
            Debug.LogError("Regular Pickup Button not assigned in Inspector!");
        }
        
        // Set up the dummy pickup button
        if (dummyPickupButton != null)
        {
            dummyPickupButton.onClick.AddListener(() => OnPickupButtonClicked(ProductType.Dummy));
            dummyPickupButton.gameObject.SetActive(false); // Hide by default
            isDummyButtonVisible = false;
            Debug.Log("Dummy pickup button listener added - button hidden by default");
        }
        else
        {
            Debug.LogError("Dummy Pickup Button not assigned in Inspector!");
        }
        
        // Initialize instruction images
        if (instructionImage1 != null)
        {
            instructionImage1.gameObject.SetActive(false);
            Debug.Log("Instruction Image 1 initialized and hidden");
        }
        
        if (instructionImage2 != null)
        {
            instructionImage2.gameObject.SetActive(false);
            Debug.Log("Instruction Image 2 initialized and hidden");
        }
        
        if (playerAnimator == null)
        {
            Debug.LogError("Player Animator not found! Please assign it in the inspector.");
        }
        else
        {
            Debug.Log($"Pickup animation controller initialized with parameter: {pickupParameterName}");
        }
        
        // Subscribe to panel events
        SubscribeToPanelEvents();
    }
    
    void SubscribeToPanelEvents()
    {
        ProductInformationManager.OnProductPanelShown += OnProductPanelShown;
        ProductInformationManager.OnProductPanelHidden += OnProductPanelHidden;
    }
    
    void UnsubscribeFromPanelEvents()
    {
        ProductInformationManager.OnProductPanelShown -= OnProductPanelShown;
        ProductInformationManager.OnProductPanelHidden -= OnProductPanelHidden;
    }
    
    void OnProductPanelShown()
    {
        Debug.Log("Product panel shown event received");
        
        // Show instruction image 2 ONLY if we just collected a dummy product
        if (currentProductType == ProductType.Dummy && instructionImage2 != null)
        {
            instructionImage2.gameObject.SetActive(true);
            Debug.Log("Showing instruction image 2 - Dummy product panel is showing");
        }
    }
    
    void OnProductPanelHidden()
    {
        Debug.Log("Product panel hidden event received");
        
        // ALWAYS hide instruction image 2 when panel is closed
        if (instructionImage2 != null && instructionImage2.gameObject.activeSelf)
        {
            instructionImage2.gameObject.SetActive(false);
            Debug.Log("Hiding instruction image 2 - Panel closed");
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
            // Only check for nearby products when not picking up
            CheckForNearbyProducts();
        }
    }
    
    void CheckForNearbyProducts()
    {
        // Find all products with the specified tags
        GameObject[] naturalSugarProducts = GameObject.FindGameObjectsWithTag("NaturalSugar");
        GameObject[] addedSugarProducts = GameObject.FindGameObjectsWithTag("AddedSugar");
        GameObject[] dummyProducts = GameObject.FindGameObjectsWithTag("DummyProduct");
        
        GameObject closestRegularProduct = null;
        GameObject closestDummyProduct = null;
        float closestRegularDistance = float.MaxValue;
        float closestDummyDistance = float.MaxValue;
        
        // Check NaturalSugar products
        foreach (GameObject product in naturalSugarProducts)
        {
            if (product == null) continue;
            
            float distance = Vector3.Distance(transform.position, product.transform.position);
            if (distance < pickupRange && distance < closestRegularDistance)
            {
                closestRegularProduct = product;
                closestRegularDistance = distance;
            }
        }
        
        // Check AddedSugar products
        foreach (GameObject product in addedSugarProducts)
        {
            if (product == null) continue;
            
            float distance = Vector3.Distance(transform.position, product.transform.position);
            if (distance < pickupRange && distance < closestRegularDistance)
            {
                closestRegularProduct = product;
                closestRegularDistance = distance;
            }
        }
        
        // Check DummyProduct products (only if not already collected)
        if (!hasCollectedDummyProduct)
        {
            foreach (GameObject product in dummyProducts)
            {
                if (product == null) continue;
                
                float distance = Vector3.Distance(transform.position, product.transform.position);
                if (distance < pickupRange && distance < closestDummyDistance)
                {
                    closestDummyProduct = product;
                    closestDummyDistance = distance;
                }
            }
        }
        
        // Determine which product to show button for
        bool showDummyButton = false;
        bool showRegularButton = false;
        
        if (closestDummyProduct != null && closestDummyDistance <= pickupRange)
        {
            // Dummy product is closer or only dummy available
            if (closestRegularProduct == null || closestDummyDistance <= closestRegularDistance)
            {
                currentNearbyProduct = closestDummyProduct;
                currentProductType = ProductType.Dummy;
                showDummyButton = true;
                showRegularButton = false;
            }
            else
            {
                currentNearbyProduct = closestRegularProduct;
                currentProductType = ProductType.Regular;
                showDummyButton = false;
                showRegularButton = true;
            }
        }
        else if (closestRegularProduct != null && closestRegularDistance <= pickupRange)
        {
            currentNearbyProduct = closestRegularProduct;
            currentProductType = ProductType.Regular;
            showDummyButton = false;
            showRegularButton = true;
        }
        else
        {
            // No products nearby
            if (currentNearbyProduct != null)
            {
                Debug.Log("Moved away from product");
                currentNearbyProduct = null;
            }
        }
        
        // Update dummy product instruction visibility
        UpdateDummyProductInstructions(showDummyButton);
        
        // Update button visibility
        if (showDummyButton && !isDummyButtonVisible)
        {
            ShowDummyPickupButton();
            HideRegularPickupButton();
        }
        else if (showRegularButton && !isRegularButtonVisible)
        {
            ShowRegularPickupButton();
            HideDummyPickupButton();
        }
        else if (!showDummyButton && !showRegularButton)
        {
            HideAllPickupButtons();
        }
    }
    
    void UpdateDummyProductInstructions(bool showDummyButton)
    {
        if (showDummyButton != isNearDummyProduct)
        {
            isNearDummyProduct = showDummyButton;
            
            // Show/hide instruction image 1 based on proximity to dummy product
            if (instructionImage1 != null)
            {
                instructionImage1.gameObject.SetActive(showDummyButton && !hasCollectedDummyProduct);
                if (showDummyButton && !hasCollectedDummyProduct)
                {
                    Debug.Log("Showing instruction image 1 - Near DummyProduct");
                }
                else if (instructionImage1.gameObject.activeSelf)
                {
                    Debug.Log("Hiding instruction image 1");
                }
            }
        }
    }
    
    // This method is called when either pickup button is clicked
    public void OnPickupButtonClicked(ProductType productType)
    {
        if (isPickingUp)
        {
            Debug.Log("Already picking up!");
            return;
        }
        
        if (currentNearbyProduct == null)
        {
            Debug.LogWarning("No product nearby to pickup!");
            HideAllPickupButtons();
            return;
        }
        
        // Verify we're picking up the right type
        if ((productType == ProductType.Dummy && !currentNearbyProduct.CompareTag("DummyProduct")) ||
            (productType == ProductType.Regular && currentNearbyProduct.CompareTag("DummyProduct")))
        {
            Debug.LogWarning($"Button type mismatch! Button: {productType}, Product: {currentNearbyProduct.tag}");
            return;
        }
        
        if (playerAnimator != null)
        {
            StartCoroutine(PickupProduct());
        }
        else
        {
            Debug.LogError("Cannot trigger pickup - animator not found!");
        }
    }
    
    private IEnumerator PickupProduct()
    {
        Debug.Log($"Starting pickup process for: {currentNearbyProduct.name} (Type: {currentProductType})");
        
        // Disable player movement
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            Debug.Log("Player movement disabled");
        }
        
        // Hide pickup buttons immediately
        HideAllPickupButtons();
        
        // Hide instruction image 1 if it's a dummy product
        if (currentProductType == ProductType.Dummy && instructionImage1 != null)
        {
            instructionImage1.gameObject.SetActive(false);
        }
        
        // Trigger the pickup animation
        playerAnimator.SetBool(pickupHash, true);
        isPickingUp = true;
        pickupTimer = 0f;
        
        // Invoke start event
        OnPickupStart?.Invoke(currentNearbyProduct);
        
        Debug.Log($"Pickup animation started for: {currentNearbyProduct.name}");
        
        // Play pickup sound with delay to align with animation
        if (playSoundOnPickup && pickupSound != null)
        {
            StartCoroutine(PlayPickupSoundWithDelay());
        }
        
        // Wait for animation to complete
        yield return new WaitForSeconds(pickupAnimationDuration);
        
        // Complete the pickup
        CompletePickup();
    }
    
    private IEnumerator PlayPickupSoundWithDelay()
    {
        yield return new WaitForSeconds(soundPlayDelay);
        
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
            Debug.Log("Pickup sound played");
        }
    }
    
    private void CompletePickup()
    {
        Debug.Log("Completing pickup process");
        
        // End the pickup animation
        playerAnimator.SetBool(pickupHash, false);
        isPickingUp = false;
        pickupTimer = 0f;
        
        // Remove the collected product
        if (currentNearbyProduct != null)
        {
            string productName = currentNearbyProduct.name;
            string productTag = currentNearbyProduct.tag;
            
            Debug.Log($"Collecting product: {productName} ({productTag})");
            
            // Check if it's a dummy product
            if (currentProductType == ProductType.Dummy)
            {
                // Handle dummy product collection (shows panel but doesn't count)
                HandleDummyProductCollection(productName);
            }
            else
            {
                // Handle regular product collection
                HandleRegularProductCollection(productName, productTag);
            }
            
            // Play product disappearance effect if available
            PlayProductDisappearanceEffect();
            
            Destroy(currentNearbyProduct);
            
            // Invoke complete event
            OnPickupComplete?.Invoke(currentNearbyProduct);
            
            Debug.Log($"Successfully collected: {productName}");
            
            currentNearbyProduct = null;
        }
        
        Debug.Log("Pickup completed successfully");
    }
    
    private void HandleDummyProductCollection(string productName)
    {
        Debug.Log($"Collected dummy product: {productName}");
        hasCollectedDummyProduct = true;
        
        // Extract product ID from dummy product name (assuming it's named like "Soda_Dummy")
        string productID = ExtractProductID(productName.Replace("_Dummy", "").Replace("Dummy_", ""));
        
        // Get product information manager
        ProductInformationManager productInfoManager = FindObjectOfType<ProductInformationManager>();
        if (productInfoManager != null)
        {
            // Show product information WITHOUT adding to collection
            productInfoManager.ShowProductInfoForDummy(productID);
            
            // Instruction image 2 will be shown by OnProductPanelShown event
            // when the panel actually appears
        }
        else
        {
            Debug.LogWarning("ProductInformationManager not found!");
            // Re-enable movement if no manager found
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
        }
    }
    
    private void HandleRegularProductCollection(string productName, string productTag)
    {
        // Get product information manager
        ProductInformationManager productInfoManager = FindObjectOfType<ProductInformationManager>();
        if (productInfoManager != null)
        {
            // Extract product ID from name
            string productID = ExtractProductID(productName);
            
            // Check if already collected in this session
            if (productInfoManager.IsProductCollected(productID))
            {
                Debug.Log($"Product {productID} already collected this session! Skipping info panel.");
                // Re-enable movement immediately
                if (playerMovementScript != null)
                    playerMovementScript.enabled = true;
                    
                return;
            }
            
            // Show product information (first time collection)
            productInfoManager.ShowProductInfo(productID);
            
            // If this is a soda product and we've collected a dummy product before,
            // make sure instruction image 2 is hidden
            if ((productID.Contains("SODA") || productID.Contains("SODA_")) && hasCollectedDummyProduct)
            {
                if (instructionImage2 != null && instructionImage2.gameObject.activeSelf)
                {
                    instructionImage2.gameObject.SetActive(false);
                    Debug.Log("Hiding instruction image 2 - Real soda collected");
                }
            }
        }
        else
        {
            Debug.LogWarning("ProductInformationManager not found!");
            // Re-enable movement if no manager found
            if (playerMovementScript != null)
                playerMovementScript.enabled = true;
        }
    }
    
    private string ExtractProductID(string productName)
    {
        // Extract product ID from the name
        // Example: "Banana_Instance" -> "BANANA"
        string cleanName = productName.ToUpper();
        
        // Remove common suffixes
        if (cleanName.Contains("_"))
            cleanName = cleanName.Split('_')[0];
        
        if (cleanName.Contains("(CLONE)"))
            cleanName = cleanName.Replace("(CLONE)", "").Trim();
        
        return cleanName;
    }
    
    private void PlayProductDisappearanceEffect()
    {
        // You can add visual effects here like:
        // - Particle system
        // - Fade out animation
        // - Scale down effect
        
        // Example: If you want to add a simple particle effect
        /*
        ParticleSystem ps = currentNearbyProduct.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        */
        
        Debug.Log("Product disappearance effect triggered");
    }
    
    // Button visibility methods
    private void ShowRegularPickupButton()
    {
        if (pickupButton != null && !isRegularButtonVisible)
        {
            pickupButton.gameObject.SetActive(true);
            pickupButton.interactable = true;
            isRegularButtonVisible = true;
            Debug.Log("Regular pickup button shown");
        }
    }
    
    private void HideRegularPickupButton()
    {
        if (pickupButton != null && isRegularButtonVisible)
        {
            pickupButton.gameObject.SetActive(false);
            isRegularButtonVisible = false;
            Debug.Log("Regular pickup button hidden");
        }
    }
    
    private void ShowDummyPickupButton()
    {
        if (dummyPickupButton != null && !isDummyButtonVisible)
        {
            dummyPickupButton.gameObject.SetActive(true);
            dummyPickupButton.interactable = true;
            isDummyButtonVisible = true;
            Debug.Log("Dummy pickup button shown");
        }
    }
    
    private void HideDummyPickupButton()
    {
        if (dummyPickupButton != null && isDummyButtonVisible)
        {
            dummyPickupButton.gameObject.SetActive(false);
            isDummyButtonVisible = false;
            Debug.Log("Dummy pickup button hidden");
        }
    }
    
    private void HideAllPickupButtons()
    {
        HideRegularPickupButton();
        HideDummyPickupButton();
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
            
            // Stop any playing sounds
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Re-enable movement
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = true;
            }
            
            // Reset button state
            if (currentNearbyProduct != null)
            {
                if (currentProductType == ProductType.Dummy)
                    ShowDummyPickupButton();
                else
                    ShowRegularPickupButton();
            }
            else
            {
                HideAllPickupButtons();
            }
            
            // Reset instruction images
            if (instructionImage1 != null && instructionImage1.gameObject.activeSelf)
            {
                instructionImage1.gameObject.SetActive(false);
            }
            if (instructionImage2 != null && instructionImage2.gameObject.activeSelf)
            {
                instructionImage2.gameObject.SetActive(false);
            }
            
            Debug.Log("Pickup animation force stopped");
        }
    }
    
    // Audio control methods
    public void SetPickupSound(AudioClip newSound)
    {
        pickupSound = newSound;
    }
    
    public void SetPickupSoundVolume(float volume)
    {
        pickupSoundVolume = Mathf.Clamp01(volume);
    }
    
    public void SetSoundPlayDelay(float delay)
    {
        soundPlayDelay = Mathf.Max(0f, delay);
    }
    
    public void EnablePickupSound(bool enable)
    {
        playSoundOnPickup = enable;
    }
    
    // Check if currently picking up
    public bool IsPickingUp()
    {
        return isPickingUp;
    }
    
    // Get remaining pickup time
    public float GetRemainingPickupTime()
    {
        return Mathf.Max(0f, pickupAnimationDuration - pickupTimer);
    }
    
    // For external systems to check if pickup can be triggered
    public bool CanPickup()
    {
        return !isPickingUp && playerAnimator != null && currentNearbyProduct != null;
    }
    
    // Get the current nearby product
    public GameObject GetCurrentNearbyProduct()
    {
        return currentNearbyProduct;
    }
    
    // Get the current nearby product type
    public string GetCurrentProductType()
    {
        if (currentNearbyProduct == null) return "";
        
        if (currentNearbyProduct.CompareTag("NaturalSugar")) return "NaturalSugar";
        if (currentNearbyProduct.CompareTag("AddedSugar")) return "AddedSugar";
        if (currentNearbyProduct.CompareTag("DummyProduct")) return "DummyProduct";
        
        return "Unknown";
    }
    
    // Check if button is currently visible
    public bool IsButtonVisible()
    {
        return isRegularButtonVisible || isDummyButtonVisible;
    }
    
    // Get distance to current nearby product
    public float GetDistanceToProduct()
    {
        if (currentNearbyProduct == null) return float.MaxValue;
        
        return Vector3.Distance(transform.position, currentNearbyProduct.transform.position);
    }
    
    // Enable or disable the pickup button
    public void SetPickupButtonEnabled(bool enabled)
    {
        if (pickupButton != null)
        {
            pickupButton.interactable = enabled;
        }
        if (dummyPickupButton != null)
        {
            dummyPickupButton.interactable = enabled;
        }
    }
    
    // Show or hide the pickup button manually
    public void SetPickupButtonVisible(bool visible)
    {
        if (visible)
        {
            if (currentProductType == ProductType.Dummy)
                ShowDummyPickupButton();
            else
                ShowRegularPickupButton();
        }
        else
        {
            HideAllPickupButtons();
        }
    }
    
    // Check if dummy product has been collected
    public bool HasCollectedDummyProduct()
    {
        return hasCollectedDummyProduct;
    }
    
    // Reset dummy product collection state
    public void ResetDummyProductCollection()
    {
        hasCollectedDummyProduct = false;
        if (instructionImage2 != null)
        {
            instructionImage2.gameObject.SetActive(false);
        }
        Debug.Log("Dummy product collection state reset");
    }
    
    // Clean up
    private void OnDestroy()
    {
        // Remove the button listeners to prevent memory leaks
        if (pickupButton != null)
        {
            pickupButton.onClick.RemoveAllListeners();
        }
        if (dummyPickupButton != null)
        {
            dummyPickupButton.onClick.RemoveAllListeners();
        }
        
        // Unsubscribe from events
        UnsubscribeFromPanelEvents();
    }
    
    // Visualize pickup range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // Draw line to current nearby product if any
        if (currentNearbyProduct != null)
        {
            Gizmos.color = currentProductType == ProductType.Dummy ? Color.blue : Color.green;
            Gizmos.DrawLine(transform.position, currentNearbyProduct.transform.position);
        }
    }
    
    // Debug method to see nearby products in console
    [ContextMenu("Debug Nearby Products")]
    public void DebugNearbyProducts()
    {
        GameObject[] naturalSugarProducts = GameObject.FindGameObjectsWithTag("NaturalSugar");
        GameObject[] addedSugarProducts = GameObject.FindGameObjectsWithTag("AddedSugar");
        GameObject[] dummyProducts = GameObject.FindGameObjectsWithTag("DummyProduct");
        
        Debug.Log($"=== NEARBY PRODUCTS DEBUG ===");
        Debug.Log($"NaturalSugar products in scene: {naturalSugarProducts.Length}");
        Debug.Log($"AddedSugar products in scene: {addedSugarProducts.Length}");
        Debug.Log($"DummyProduct products in scene: {dummyProducts.Length}");
        
        Debug.Log($"Current nearby product: {(currentNearbyProduct != null ? currentNearbyProduct.name + " (" + currentNearbyProduct.tag + ")" : "None")}");
        Debug.Log($"Product type: {currentProductType}");
        Debug.Log($"Regular button visible: {isRegularButtonVisible}");
        Debug.Log($"Dummy button visible: {isDummyButtonVisible}");
        Debug.Log($"Is picking up: {isPickingUp}");
        Debug.Log($"Has collected dummy product: {hasCollectedDummyProduct}");
        Debug.Log($"Instruction Image 2 active: {(instructionImage2 != null ? instructionImage2.gameObject.activeSelf.ToString() : "null")}");
    }
    
    // Test sound method
    [ContextMenu("Test Pickup Sound")]
    public void TestPickupSound()
    {
        if (audioSource != null && pickupSound != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
            Debug.Log("Test pickup sound played");
        }
        else
        {
            Debug.LogWarning("Cannot test pickup sound - audio source or pickup sound not set");
        }
    }
}