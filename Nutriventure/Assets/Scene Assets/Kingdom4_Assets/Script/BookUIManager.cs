using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class BookUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform bookUIContainer;
    public GameObject bookEntryPrefab;
    public GameObject bookPanel;
    public GameObject ingredientPanel;
    
    [Header("UI Elements to Hide When Book Opens")]
    public GameObject[] uiElementsToHide; // Assign other UI panels here
    
    [Header("Current Book UI")]
    public Transform ingredientsGrid;
    public GameObject ingredientSlotPrefab;
    
    [Header("Ingredient Info Panel")]
    public TextMeshProUGUI ingredientNameText;
    public TextMeshProUGUI ingredientDescriptionText;
    public Image ingredientIconImage;
    public Button closeIngredientButton;
    public Button closeBookButton;
    
    [Header("Silhouette Settings")]
    public List<Sprite> silhouetteSprites = new List<Sprite>(); // Assign your 9 silhouette sprites here
    
    [Header("All Possible Ingredients")]
    public List<IngredientDefinition> allPossibleIngredients = new List<IngredientDefinition>();
    
    private BookInteractable mainBook;
    private GameObject currentBookUIEntry;
    private List<GameObject> hiddenUIElements = new List<GameObject>();
    private Dictionary<string, Sprite> ingredientSilhouetteMap = new Dictionary<string, Sprite>();
    
    [System.Serializable]
    public class IngredientDefinition
    {
        public string ingredientId;
        public string ingredientName;
        public string ingredientDescription;
        public Sprite ingredientIcon;
        public int silhouetteIndex = -1; // Optional: assign specific silhouette index
    }
    
    void Start()
    {
        if (bookPanel != null) bookPanel.SetActive(false);
        if (ingredientPanel != null) ingredientPanel.SetActive(false);
        
        if (closeIngredientButton != null)
        {
            closeIngredientButton.onClick.AddListener(CloseIngredientPanel);
        }
        
        if (closeBookButton != null)
        {
            closeBookButton.onClick.AddListener(CloseBook);
        }
        
        // Initialize silhouette mapping
        InitializeSilhouetteMapping();
    }
    
    private void InitializeSilhouetteMapping()
    {
        // Clear existing mapping
        ingredientSilhouetteMap.Clear();
        
        // Assign silhouette sprites to ingredients
        for (int i = 0; i < allPossibleIngredients.Count; i++)
        {
            var ingredient = allPossibleIngredients[i];
            Sprite silhouette = GetSilhouetteForIngredient(ingredient, i);
            ingredientSilhouetteMap[ingredient.ingredientId] = silhouette;
        }
    }
    
    private Sprite GetSilhouetteForIngredient(IngredientDefinition ingredient, int index)
    {
        // If ingredient has a specific silhouette index assigned, use it
        if (ingredient.silhouetteIndex >= 0 && ingredient.silhouetteIndex < silhouetteSprites.Count)
        {
            return silhouetteSprites[ingredient.silhouetteIndex];
        }
        
        // Otherwise, assign based on index (cycle through available silhouettes)
        if (silhouetteSprites.Count > 0)
        {
            return silhouetteSprites[index % silhouetteSprites.Count];
        }
        
        Debug.LogWarning("No silhouette sprites assigned! Please assign silhouette sprites in the inspector.");
        return null;
    }
    
    public Sprite GetSilhouetteForIngredientId(string ingredientId)
    {
        if (ingredientSilhouetteMap.TryGetValue(ingredientId, out Sprite silhouette))
        {
            return silhouette;
        }
        
        // Fallback: return first silhouette or null
        if (silhouetteSprites.Count > 0)
        {
            return silhouetteSprites[0];
        }
        
        return null;
    }
    
    public void SetMainBook(BookInteractable book)
    {
        mainBook = book;
        Debug.Log($"Main book set: {book.bookName}");
    }
    
    public BookInteractable GetMainBook()
    {
        return BookInteractable.Instance;
    }
    
    public void AddBookToUI(string bookId, string bookName, Sprite bookIcon)
    {
        if (bookEntryPrefab != null && bookUIContainer != null)
        {
            // Only create one book entry
            if (currentBookUIEntry == null)
            {
                currentBookUIEntry = Instantiate(bookEntryPrefab, bookUIContainer);
                BookUIEntry entry = currentBookUIEntry.GetComponent<BookUIEntry>();
                
                if (entry != null)
                {
                    entry.Initialize(bookId, bookName, bookIcon, this);
                }
                
                Debug.Log($"Book {bookId} added to UI");
            }
        }
        else
        {
            Debug.LogError("Book entry prefab or container not assigned!");
        }
    }
    
    public void OpenBook(string bookId, string bookName, Sprite bookIcon)
    {
        if (bookPanel != null)
        {
            // Hide other UI elements
            HideOtherUI();
            
            // Show book panel
            bookPanel.SetActive(true);
                
            UpdateBookUI();
        }
    }
    
    public void CloseBook()
    {
        if (bookPanel != null) 
        {
            bookPanel.SetActive(false);
            
            // Restore hidden UI elements
            RestoreHiddenUI();
        }
    }
    
    private void HideOtherUI()
    {
        hiddenUIElements.Clear();
        
        if (uiElementsToHide != null)
        {
            foreach (GameObject uiElement in uiElementsToHide)
            {
                if (uiElement != null && uiElement.activeInHierarchy)
                {
                    hiddenUIElements.Add(uiElement);
                    uiElement.SetActive(false);
                }
            }
        }
        
        // Also hide the book collection panel itself
        if (bookUIContainer != null && bookUIContainer.gameObject.activeInHierarchy)
        {
            hiddenUIElements.Add(bookUIContainer.gameObject);
            bookUIContainer.gameObject.SetActive(false);
        }
    }
    
    private void RestoreHiddenUI()
    {
        foreach (GameObject uiElement in hiddenUIElements)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
            }
        }
        hiddenUIElements.Clear();
    }
    
    public void UpdateBookUI()
    {
        if (ingredientsGrid == null) return;
        
        // Clear current ingredients grid
        foreach (Transform child in ingredientsGrid)
        {
            Destroy(child.gameObject);
        }
        
        // Show ALL possible ingredients (both collected and uncollected)
        foreach (var ingredientDef in allPossibleIngredients)
        {
            if (ingredientSlotPrefab != null)
            {
                GameObject ingredientSlot = Instantiate(ingredientSlotPrefab, ingredientsGrid);
                IngredientSlot slot = ingredientSlot.GetComponent<IngredientSlot>();
                
                if (slot != null)
                {
                    // Check if this ingredient has been collected
                    bool isCollected = mainBook != null && mainBook.IsIngredientCollected(ingredientDef.ingredientId);
                    
                    // Create display data
                    BookInteractable.IngredientData displayData = new BookInteractable.IngredientData
                    {
                        ingredientId = ingredientDef.ingredientId,
                        ingredientName = ingredientDef.ingredientName,
                        ingredientDescription = ingredientDef.ingredientDescription,
                        ingredientIcon = ingredientDef.ingredientIcon
                    };
                    
                    // Get the specific silhouette for this ingredient
                    Sprite silhouette = GetSilhouetteForIngredientId(ingredientDef.ingredientId);
                    slot.silhouetteSprite = silhouette;
                    
                    slot.Initialize(displayData, this, isCollected);
                }
            }
        }
        
        int collectedCount = mainBook != null ? mainBook.GetCollectedIngredients().Count : 0;
        Debug.Log($"Updated book UI: {collectedCount}/{allPossibleIngredients.Count} ingredients collected");
    }
    
    public void ShowIngredientInfo(BookInteractable.IngredientData ingredient)
    {
        if (ingredientPanel != null)
        {
            ingredientPanel.SetActive(true);
            
            if (ingredientNameText != null)
                ingredientNameText.text = ingredient.ingredientName;
                
            if (ingredientDescriptionText != null)
                ingredientDescriptionText.text = ingredient.ingredientDescription;
                
            if (ingredientIconImage != null && ingredient.ingredientIcon != null)
                ingredientIconImage.sprite = ingredient.ingredientIcon;
        }
    }
    
    public void CloseIngredientPanel()
    {
        if (ingredientPanel != null) ingredientPanel.SetActive(false);
    }
    
    // Method to add ingredients dynamically with silhouette support
    public void AddPossibleIngredient(string id, string name, string description, Sprite icon, int silhouetteIndex = -1)
    {
        IngredientDefinition newIngredient = new IngredientDefinition
        {
            ingredientId = id,
            ingredientName = name,
            ingredientDescription = description,
            ingredientIcon = icon,
            silhouetteIndex = silhouetteIndex
        };
        
        allPossibleIngredients.Add(newIngredient);
        
        // Add to silhouette mapping
        Sprite silhouette = GetSilhouetteForIngredient(newIngredient, allPossibleIngredients.Count - 1);
        ingredientSilhouetteMap[id] = silhouette;
    }
    
    // Method to refresh the UI
    public void RefreshUI()
    {
        UpdateBookUI();
    }
    
    // Method to manually assign a specific silhouette to an ingredient
    public void AssignSilhouetteToIngredient(string ingredientId, int silhouetteIndex)
    {
        if (silhouetteIndex >= 0 && silhouetteIndex < silhouetteSprites.Count)
        {
            ingredientSilhouetteMap[ingredientId] = silhouetteSprites[silhouetteIndex];
        }
        else
        {
            Debug.LogWarning($"Invalid silhouette index: {silhouetteIndex}. Must be between 0 and {silhouetteSprites.Count - 1}");
        }
    }
}