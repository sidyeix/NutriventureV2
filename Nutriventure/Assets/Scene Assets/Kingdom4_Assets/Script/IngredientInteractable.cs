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

        // Initialize visual feedback
        if (highlightObject != null) highlightObject.SetActive(false);
        if (pickupPrompt != null) pickupPrompt.SetActive(false);
    }

   private void Update()
{
    CheckMobileTap();
}




  private void CheckMobileTap()
{
    if (Touchscreen.current == null || mainCamera == null)
        return;

    var touch = Touchscreen.current.primaryTouch;

    if (!touch.press.wasPressedThisFrame)
        return;

    Vector2 touchPos = touch.position.ReadValue();

    Ray ray = mainCamera.ScreenPointToRay(touchPos);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, 1000f))
    {
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            Pickup();
        }
    }
}


    public override void Pickup()
    {
   if (productInfo == null)
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