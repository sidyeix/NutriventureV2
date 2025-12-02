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
    
    [Header("UI References")]
    public Button pickupButton; // Assign your UI pickup button here
    
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
    private bool isButtonVisible = false;
    private AudioSource audioSource;
    
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
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 10f;
        }
        
        // Convert parameter name to hash for better performance
        pickupHash = Animator.StringToHash(pickupParameterName);
        
        // Set up the pickup button
        if (pickupButton != null)
        {
            pickupButton.onClick.AddListener(OnPickupButtonClicked);
            pickupButton.gameObject.SetActive(false); // Hide by default
            isButtonVisible = false;
            Debug.Log("Pickup button listener added - button hidden by default");
        }
        else
        {
            Debug.LogError("Pickup Button not assigned in Inspector!");
        }
        
        if (playerAnimator == null)
        {
            Debug.LogError("Player Animator not found! Please assign it in the inspector.");
        }
        else
        {
            Debug.Log($"Pickup animation controller initialized with parameter: {pickupParameterName}");
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
        
        GameObject closestProduct = null;
        float closestDistance = float.MaxValue;
        
        // Check NaturalSugar products
        foreach (GameObject product in naturalSugarProducts)
        {
            if (product == null) continue;
            
            float distance = Vector3.Distance(transform.position, product.transform.position);
            if (distance < pickupRange && distance < closestDistance)
            {
                closestProduct = product;
                closestDistance = distance;
            }
        }
        
        // Check AddedSugar products
        foreach (GameObject product in addedSugarProducts)
        {
            if (product == null) continue;
            
            float distance = Vector3.Distance(transform.position, product.transform.position);
            if (distance < pickupRange && distance < closestDistance)
            {
                closestProduct = product;
                closestDistance = distance;
            }
        }
        
        // Update button visibility based on proximity
        if (closestProduct != null && closestDistance <= pickupRange)
        {
            // Product is nearby
            if (currentNearbyProduct != closestProduct)
            {
                currentNearbyProduct = closestProduct;
                Debug.Log($"Nearby product detected: {currentNearbyProduct.name} (Distance: {closestDistance:F2})");
            }
            
            // Show button if not already visible
            if (!isButtonVisible)
            {
                ShowPickupButton();
            }
        }
        else
        {
            // No products nearby
            if (currentNearbyProduct != null)
            {
                Debug.Log("Moved away from product");
                currentNearbyProduct = null;
            }
            
            // Hide button if currently visible
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
        
        if (currentNearbyProduct == null)
        {
            Debug.LogWarning("No product nearby to pickup!");
            HidePickupButton();
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
        Debug.Log($"Starting pickup process for: {currentNearbyProduct.name}");
        
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
    
    // In your CollectProducts script, modify the CompletePickup method:
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
                    // Still destroy the product, but don't show info
                    Destroy(currentNearbyProduct);
                    currentNearbyProduct = null;
                    
                    // Re-enable movement immediately
                    if (playerMovementScript != null)
                        playerMovementScript.enabled = true;
                        
                    return;
                }
                
                // Show product information (first time collection)
                productInfoManager.ShowProductInfo(productID);
            }
            else
            {
                Debug.LogWarning("ProductInformationManager not found!");
            }
            
            // Play product disappearance effect if available
            PlayProductDisappearanceEffect();
            
            Destroy(currentNearbyProduct);
            
            // Invoke complete event
            OnPickupComplete?.Invoke(currentNearbyProduct);
            
            Debug.Log($"Successfully collected: {productName}");
            
            currentNearbyProduct = null;
        }
        
        // Note: Player movement will be re-enabled by product info manager when panel closes
        // Only re-enable here if we didn't show the info panel
        // if (playerMovementScript != null)
        // {
        //     playerMovementScript.enabled = true;
        // }
        
        Debug.Log("Pickup completed successfully");
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
                ShowPickupButton();
            }
            else
            {
                HidePickupButton();
            }
            
            Debug.Log("Pickup animation force stopped");
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
        
        return "Unknown";
    }
    
    // Check if button is currently visible
    public bool IsButtonVisible()
    {
        return isButtonVisible;
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
    }
    
    // Show or hide the pickup button manually
    public void SetPickupButtonVisible(bool visible)
    {
        if (visible)
        {
            ShowPickupButton();
        }
        else
        {
            HidePickupButton();
        }
    }
    
    // Clean up
    private void OnDestroy()
    {
        // Remove the button listener to prevent memory leaks
        if (pickupButton != null)
        {
            pickupButton.onClick.RemoveListener(OnPickupButtonClicked);
        }
    }
    
    // Visualize pickup range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        // Draw line to current nearby product if any
        if (currentNearbyProduct != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentNearbyProduct.transform.position);
        }
    }
    
    // Debug method to see nearby products in console
    [ContextMenu("Debug Nearby Products")]
    public void DebugNearbyProducts()
    {
        GameObject[] naturalSugarProducts = GameObject.FindGameObjectsWithTag("NaturalSugar");
        GameObject[] addedSugarProducts = GameObject.FindGameObjectsWithTag("AddedSugar");
        
        Debug.Log($"NaturalSugar products in scene: {naturalSugarProducts.Length}");
        Debug.Log($"AddedSugar products in scene: {addedSugarProducts.Length}");
        
        foreach (GameObject product in naturalSugarProducts)
        {
            if (product != null)
            {
                float distance = Vector3.Distance(transform.position, product.transform.position);
                Debug.Log($"NaturalSugar: {product.name} - Distance: {distance:F2} - In Range: {distance <= pickupRange}");
            }
        }
        
        foreach (GameObject product in addedSugarProducts)
        {
            if (product != null)
            {
                float distance = Vector3.Distance(transform.position, product.transform.position);
                Debug.Log($"AddedSugar: {product.name} - Distance: {distance:F2} - In Range: {distance <= pickupRange}");
            }
        }
        
        Debug.Log($"Current nearby product: {(currentNearbyProduct != null ? currentNearbyProduct.name : "None")}");
        Debug.Log($"Button visible: {isButtonVisible}");
        Debug.Log($"Is picking up: {isPickingUp}");
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