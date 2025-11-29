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
        if (BookUIManager.Instance != null)
        {
            BookUIManager.Instance.AddIngredientToBook(ingredientId, ingredientName, ingredientDescription, ingredientIcon);
        }
        else
        {
            Debug.LogError("BookUIManager Instance not found!");
        }
        
        base.Pickup();
    }
}