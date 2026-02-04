using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // Makes sure there's a collider for tapping
public class IngredientInteractable : Interactable
{
    [Header("UI Manager")]
    public k4ProductInformationManager productInfoManager;

    [Header("Allergen Data Source")]
    public AllergenProductData allergenDatabase;

    [Header("Ingredient Settings")]
    public string ingredientId; // MUST match productID in ScriptableObject

    [Header("Sound FX")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    [Header("Visual Feedback")]
    public GameObject highlightObject; // Visual feedback when player is in range
    public GameObject pickupPrompt; // Optional: "Press E to pickup" text/image

    private AllergenProductData.ProductInfo productInfo;
    private bool isPlayerInRange = false;

    private void Awake()
    {
        if (allergenDatabase == null)
        {
            Debug.LogError("Allergen database not assigned!");
            return;
        }

        productInfo = allergenDatabase.GetProductInfo(ingredientId);

        if (productInfo == null)
        {
            Debug.LogError($"No product found with ID: {ingredientId}");
        }

        // 🔍 Auto-find Product Info Manager if not assigned
        if (productInfoManager == null)
        {
            productInfoManager = FindAnyObjectByType<k4ProductInformationManager>();
        }

        // Configure collider as trigger
        Collider collider = GetComponent<Collider>();
        collider.isTrigger = true;

        // Initialize visual feedback
        if (highlightObject != null) highlightObject.SetActive(false);
        if (pickupPrompt != null) pickupPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered trigger
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            
            // Show visual feedback
            if (highlightObject != null) highlightObject.SetActive(true);
            if (pickupPrompt != null) pickupPrompt.SetActive(true);
            
            Debug.Log("Player entered ingredient range");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if player exited trigger
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            
            // Hide visual feedback
            if (highlightObject != null) highlightObject.SetActive(false);
            if (pickupPrompt != null) pickupPrompt.SetActive(false);
            
            Debug.Log("Player exited ingredient range");
        }
    }

    private void Update()
    {
        // Check for input when player is in range
        if (isPlayerInRange)
        {
            // Check for keyboard input (PC)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Pickup();
            }
            
            // Check for touch input (Mobile)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                // For mobile, you might want to check touch position or use a UI button instead
                Pickup();
            }
            
            // Check for gamepad input
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                Pickup();
            }
        }
    }

    // Alternative: Simple automatic pickup when player enters trigger
    public void AutoPickupOnEnter()
    {
        if (isPlayerInRange)
        {
            Pickup();
        }
    }

    // Optional: Call this from a UI button for mobile
    public void PickupFromUIButton()
    {
        if (isPlayerInRange)
        {
            Pickup();
        }
        else
        {
            Debug.Log("Too far to collect! Move closer.");
            // Optional: Show a message to the player
        }
    }

    public override void Pickup()
    {
        if (productInfo == null || !isPlayerInRange)
            return;

        // 🔊 Play pickup sound
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                pickupSFX,
                transform.position,
                pickupVolume
            );
        }
        
        // Notify game manager about allergen collection
        if (!string.IsNullOrEmpty(ingredientId))
        {
            AllerthriaGameManager.Instance?.CollectAllergen(ingredientId);
        }

        // 📖 Add ingredient to book
        if (BookInteractable.Instance != null)
        {
            BookInteractable.Instance.AddIngredient(
                productInfo.productID,
                productInfo.displayName,
                productInfo.description,
                productInfo.productIcon
            );
        }

        // 📢 SHOW PRODUCT INFO POPUP
        if (productInfoManager != null)
        {
            productInfoManager.ShowProductInfo(productInfo.productID);
        }
        else
        {
            Debug.LogWarning("ProductInformationManager not found!");
        }

        // 🌿 Notify spawn manager
        AllergenSpawnManager spawnManager = FindAnyObjectByType<AllergenSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.OnAllergenCollected(gameObject);
        }

        // ❗ Destroy / disable ingredient
        base.Pickup();
        
        // Disable the trigger after pickup
        GetComponent<Collider>().enabled = false;
    }

    // Visualize the trigger area in the editor
    private void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = Color.green;
            
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.DrawWireCube(
                    transform.position + boxCollider.center,
                    boxCollider.size
                );
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(
                    transform.position + sphereCollider.center,
                    sphereCollider.radius
                );
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                // Draw capsule outline
                Vector3 top = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
                Vector3 bottom = transform.position + capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
                
                Gizmos.DrawWireSphere(top, capsuleCollider.radius);
                Gizmos.DrawWireSphere(bottom, capsuleCollider.radius);
                
                // Draw connecting lines
                Gizmos.DrawLine(top + Vector3.right * capsuleCollider.radius, bottom + Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.right * capsuleCollider.radius, bottom - Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top + Vector3.forward * capsuleCollider.radius, bottom + Vector3.forward * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.forward * capsuleCollider.radius, bottom - Vector3.forward * capsuleCollider.radius);
            }
        }
    }
}