using UnityEngine;

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

    private AllergenProductData.ProductInfo productInfo;

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
