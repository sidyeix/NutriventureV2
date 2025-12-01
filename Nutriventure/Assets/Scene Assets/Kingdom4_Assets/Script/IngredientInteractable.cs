using UnityEngine;

public class IngredientInteractable : Interactable
{
    [Header("Ingredient Settings")]
    public string ingredientId;
    public string ingredientName;
    [TextArea(3, 5)]
    public string ingredientDescription;
    public Sprite ingredientIcon;
    
    private BookUIManager bookManager;
    
    void Start()
    {
        bookManager = FindAnyObjectByType<BookUIManager>();
    }
    
    public override void Pickup()
    {
        if (bookManager != null && bookManager.GetMainBook() != null)
        {
            // Add ingredient directly to the main book
            bookManager.GetMainBook().AddIngredient(ingredientId, ingredientName, ingredientDescription, ingredientIcon);
        }
        else
        {
            Debug.LogWarning("No book collected yet! Collect the book first.");
            // Optional: You can still destroy the ingredient or keep it for later
        }
        
        base.Pickup();
    }
}