using UnityEngine;
using System.Collections.Generic;

public class BookInteractable : Interactable
{
    public static BookInteractable Instance { get; private set; }
    
    [Header("Book Settings")]
    public string bookId = "BookOfAllergens";
    public string bookName = "Book of Allergens";
    public Sprite bookIcon;
    
    [Header("Collected Ingredients")]
    public List<IngredientData> collectedIngredients = new List<IngredientData>();
    
    [System.Serializable]
    public class IngredientData
    {
        public string ingredientId;
        public string ingredientName;
        public string ingredientDescription;
        public Sprite ingredientIcon;
    }
    
    private BookUIManager bookManager;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        bookManager = FindAnyObjectByType<BookUIManager>();
        if (bookManager == null)
        {
            Debug.LogError("BookUIManager not found in scene!");
        }
    }
    
    public override void Pickup()
    {
        if (bookManager != null)
        {
            // Register this book as the main book
            bookManager.SetMainBook(this);
            bookManager.AddBookToUI(bookId, bookName, bookIcon);
        }
        
        // Don't destroy the book GameObject, just disable it
        gameObject.SetActive(false);
        Debug.Log("Book collected and stored (GameObject disabled)");
    }
    
    public void AddIngredient(string ingredientId, string name, string description, Sprite icon)
    {
        // Check if ingredient already exists
        foreach (var ingredient in collectedIngredients)
        {
            if (ingredient.ingredientId == ingredientId)
                return;
        }
        
        // Add new ingredient
        IngredientData newIngredient = new IngredientData
        {
            ingredientId = ingredientId,
            ingredientName = name,
            ingredientDescription = description,
            ingredientIcon = icon
        };
        
        collectedIngredients.Add(newIngredient);
        Debug.Log($"Added ingredient to book: {name}");
        
        // Update UI if book is open
        if (bookManager != null)
        {
            bookManager.UpdateBookUI();
        }
    }
    
    public List<IngredientData> GetCollectedIngredients()
    {
        return collectedIngredients;
    }
    
    // New method for silhouette system
    public bool IsIngredientCollected(string ingredientId)
    {
        foreach (var ingredient in collectedIngredients)
        {
            if (ingredient.ingredientId == ingredientId)
                return true;
        }
        return false;
    }
}