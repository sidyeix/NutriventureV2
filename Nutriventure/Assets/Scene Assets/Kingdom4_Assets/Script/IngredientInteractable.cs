using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // Makes sure there's a collider for tapping
public class IngredientInteractable : Interactable
{
    [Header("Pickup Distance")]
[SerializeField] private float maxPickupDistance = 8f;
private Transform playerTransform;

private bool hasRequestedTouch = false;

    protected virtual void OnCollected() { }

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
    private Camera mainCamera;

private bool isCollected = false;

private bool IsPlayerInRangeByDistance()
{
    if (playerTransform == null)
        return false;

    return Vector3.Distance(transform.position, playerTransform.position) <= maxPickupDistance;
}


    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
if (player != null)
{
    playerTransform = player.transform;
}

        mainCamera = Camera.main;

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
    bool canInteract = IsPlayerInRangeByDistance();

if (canInteract && !hasRequestedTouch)
{
    LookUIRaycastController.Instance?.RequestWorldTouch(this);
    hasRequestedTouch = true;
}
else if (!canInteract && hasRequestedTouch)
{
    LookUIRaycastController.Instance?.ReleaseWorldTouch(this);
    hasRequestedTouch = false;
}



    // PC keyboard
    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
    {
        Pickup();
    }

    // Mobile tap
    CheckMobileTap();

    // Gamepad
    if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
    {
        Pickup();
    }
}



   private void CheckMobileTap()
{
    if (Touchscreen.current == null || mainCamera == null) return;

    var touch = Touchscreen.current.primaryTouch;
    if (!touch.press.wasPressedThisFrame) return;

    Vector2 touchPos = touch.position.ReadValue();

    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        return;

    Ray ray = mainCamera.ScreenPointToRay(touchPos);
    RaycastHit hit;

    // 1️⃣ Direct tap
    if (Physics.Raycast(ray, out hit, 1000f))
    {
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            Pickup();
            return;
        }
    }

    // 2️⃣ Fallback tap near ingredient
    Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
    float dist = Vector2.Distance(touchPos, screenPos);

    if (dist < 120f)
    {
        Pickup();
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
   if (productInfo == null || !IsPlayerInRangeByDistance())
    return;

    if (isCollected) return;
    isCollected = true;

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
        OnCollected();

        if (LookUIRaycastController.Instance != null)
{
    LookUIRaycastController.Instance?.ReleaseWorldTouch(this);
hasRequestedTouch = false;

}

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