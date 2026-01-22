using UnityEngine;

public class IngredientInteractable : Interactable
{
    [Header("Ingredient Settings")]
    public string ingredientId;
    public string ingredientName;
    [TextArea(3, 5)]
    public string ingredientDescription;
    public Sprite ingredientIcon;

    [Header("Sound FX")]
    public AudioClip pickupSFX;
    [Range(0f, 1f)] public float pickupVolume = 1f;

    public override void Pickup()
    {
        // 🔊 Play pickup sound
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                pickupSFX,
                transform.position,
                pickupVolume
            );
        }

        // Add ingredient to book
        if (BookInteractable.Instance != null)
        {
            BookInteractable.Instance.AddIngredient(
                ingredientId,
                ingredientName,
                ingredientDescription,
                ingredientIcon
            );
            Debug.Log($"Added {ingredientName} to book");
        }
        else
        {
            Debug.LogWarning("No book collected yet! Collect the book first.");
        }

        // Notify spawn manager
        AllergenSpawnManager spawnManager = FindAnyObjectByType<AllergenSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.OnAllergenCollected(gameObject);
        }

        base.Pickup();
    }
}
