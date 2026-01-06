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
        [TextArea(3, 5)]
        public string ingredientDescription;
        public Sprite ingredientIcon;
    }
    
    private BookUIManager bookManager;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep across scenes
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
            Debug.LogError("BookUIManager not found!");
        }
    }
    
    public override void Pickup()
    {
        if (bookManager != null)
        {
            bookManager.SetMainBook(this);
            bookManager.AddBookToUI(bookId, bookName, bookIcon);
        }
        
        Debug.Log($"📖 Book collected: {bookName}");
        gameObject.SetActive(false);
    }
    
    public void AddIngredient(string ingredientId, string name, string description, Sprite icon)
    {
        // Check if already collected
        if (IsIngredientCollected(ingredientId))
        {
            Debug.Log($"{name} already collected");
            return;
        }
        
        // Add to collection
        IngredientData newIngredient = new IngredientData
        {
            ingredientId = ingredientId,
            ingredientName = name,
            ingredientDescription = description,
            ingredientIcon = icon
        };
        
        collectedIngredients.Add(newIngredient);
        
        int count = collectedIngredients.Count;
        Debug.Log($"➕ Added ingredient: {name} (Total: {count}/9)");
        
        // Update UI
        if (bookManager != null)
        {
            bookManager.OnIngredientCollected(ingredientId);
        }
    }
    
    public bool IsIngredientCollected(string ingredientId)
    {
        foreach (var ingredient in collectedIngredients)
        {
            if (ingredient.ingredientId == ingredientId)
                return true;
        }
        return false;
    }
    
    public List<IngredientData> GetCollectedIngredients()
    {
        return collectedIngredients;
    }
    
    public int GetCollectedCount()
    {
        return collectedIngredients.Count;
    }
    
    [ContextMenu("Add Test Ingredient")]
    public void AddTestIngredient()
    {
        string testId = $"allergen_{collectedIngredients.Count + 1}";
        string testName = $"Allergy Source {collectedIngredients.Count + 1}";
        string testDesc = "A substance that causes allergic reactions in Allerthria.";
        
        AddIngredient(testId, testName, testDesc, null);
    }
    
    [ContextMenu("Clear All Ingredients")]
    public void ClearAllIngredients()
    {
        collectedIngredients.Clear();
        Debug.Log("🧹 All ingredients cleared");
        
        if (bookManager != null)
        {
            bookManager.ResetGateNarration();
            bookManager.UpdateBookUI();
        }
    }
    
    [ContextMenu("Fill All 9 Ingredients")]
    public void FillAllNineIngredients()
    {
        ClearAllIngredients();
        
        Debug.Log("Adding 9 allergy ingredients...");
        for (int i = 0; i < 9; i++)
        {
            AddTestIngredient();
        }
        
        Debug.Log($"✅ 9/9 ingredients collected! The gate to the wagon should open!");
    }
    
    [ContextMenu("Check Progress")]
    public void CheckProgress()
    {
        int count = GetCollectedCount();
        Debug.Log($"📊 Progress: {count}/9 allergies collected");
        
        if (count >= 9)
        {
            Debug.Log("🎉 READY: Gate to wagon should be open!");
        }
    }
}