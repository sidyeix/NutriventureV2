using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // Makes sure there's a collider for tapping
public class IngredientInteractable : Interactable, IPointerClickHandler
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

    [Header("Tap Settings")]
    public float maxTapDistance = 3f; // How close player needs to be to tap
    public GameObject tapHighlight; // Visual feedback when tappable
    
    private AllergenProductData.ProductInfo productInfo;
    private Transform playerTransform;
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

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Make sure we have a collider for tapping
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }

        // Add EventTrigger if using Input System (for mobile touch)
        SetupEventSystemSupport();
    }

    private void SetupEventSystemSupport()
    {
        // This makes sure the ingredient can receive pointer events
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void Update()
    {
        // Check if player is close enough to tap
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = distance <= maxTapDistance;
            
            // Show/hide visual feedback
            if (tapHighlight != null)
            {
                tapHighlight.SetActive(isPlayerInRange);
            }
        }
    }

    // This is called when the ingredient is clicked/tapped (WORKS ON MOBILE!)
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only allow left click or touch
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        
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

    // Optional: Visual feedback when hovered (for PC)
    private void OnMouseEnter()
    {
        if (isPlayerInRange)
        {
            // Add any hover effect you want
        }
    }

    private void OnMouseExit()
    {
        // Remove hover effect
    }

    // Visualize the tap range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxTapDistance);
    }

    public override void Pickup()
    {
        if (productInfo == null)
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
            AllerthriaGameManager.Instance.CollectAllergen(ingredientId);
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
    }
}