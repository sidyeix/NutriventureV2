using UnityEngine;

public class BookInteractable : Interactable
{
    [Header("Book Settings")]
    public string bookId = "BookOfAllergens";
    public string bookName = "Book of Allergens";
    public Sprite bookIcon;
    
    public override void Pickup()
    {
        if (BookUIManager.Instance != null)
        {
            BookUIManager.Instance.AddBookToUI(bookId, bookName, bookIcon);
        }
        else
        {
            Debug.LogError("BookUIManager Instance not found!");
        }
        
        base.Pickup();
    }
}