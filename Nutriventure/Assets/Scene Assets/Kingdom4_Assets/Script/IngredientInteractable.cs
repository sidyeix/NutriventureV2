using UnityEngine;

public class IngredientInteractable : Interactable
{
    [Header("Ingredient Settings")]
    public string ingredientId;
    public string ingredientName;
    [TextArea(3, 5)]
    public string ingredientDescription;
    public Sprite ingredientIcon;
    
    public override void Pickup()
    {
        // Use the singleton instance instead of finding through BookUIManager
        if (BookInteractable.Instance != null)
        {
            // Add ingredient directly to the book
            BookInteractable.Instance.AddIngredient(ingredientId, ingredientName, ingredientDescription, ingredientIcon);
            Debug.Log($"Added {ingredientName} to book");
        }
        else
        {
            Debug.LogWarning("No book collected yet! Collect the book first.");
            // Option 1: Still destroy the ingredient
            // Option 2: Don't destroy it (comment out base.Pickup())
        }
        
        base.Pickup();
    }
}