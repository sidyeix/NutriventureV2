using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BookUIManager : MonoBehaviour
{
    public static BookUIManager Instance { get; private set; }
    
    [Header("UI References")]
    public Transform bookUIContainer;
    public GameObject bookEntryPrefab;
    public GameObject bookPanel;
    public GameObject ingredientPanel;
    
    [Header("Current Book UI")]
    public Image currentBookIcon;
    public Text currentBookTitle;
    public Transform ingredientsGrid;
    public GameObject ingredientSlotPrefab;
    
    [Header("Ingredient Info Panel")]
    public Text ingredientNameText;
    public Text ingredientDescriptionText;
    public Image ingredientIconImage;
    public Button closeIngredientButton;
    public Button closeBookButton;
    
    private Dictionary<string, GameObject> bookUIEntries = new Dictionary<string, GameObject>();
    private Dictionary<string, IngredientData> collectedIngredients = new Dictionary<string, IngredientData>();
    private string currentOpenBookId;
    
    [System.Serializable]
    public class IngredientData
    {
        public string id;
        public string name;
        public string description;
        public Sprite icon;
        public bool isCollected;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeUI();
    }
    
    void InitializeUI()
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
    }
    
    public void AddBookToUI(string bookId, string bookName, Sprite bookIcon)
    {
        if (!bookUIEntries.ContainsKey(bookId))
        {
            if (bookEntryPrefab != null && bookUIContainer != null)
            {
                GameObject bookUI = Instantiate(bookEntryPrefab, bookUIContainer);
                BookUIEntry entry = bookUI.GetComponent<BookUIEntry>();
                
                if (entry != null)
                {
                    entry.Initialize(bookId, bookName, bookIcon, this);
                }
                
                bookUIEntries[bookId] = bookUI;
                Debug.Log($"Book {bookId} added to UI");
            }
        }
    }
    
    public void AddIngredientToBook(string ingredientId, string name, string description, Sprite icon)
    {
        if (!collectedIngredients.ContainsKey(ingredientId))
        {
            IngredientData newIngredient = new IngredientData
            {
                id = ingredientId,
                name = name,
                description = description,
                icon = icon,
                isCollected = true
            };
            
            collectedIngredients[ingredientId] = newIngredient;
            Debug.Log($"Ingredient {name} added to book");
            
            if (currentOpenBookId != null)
            {
                UpdateBookUI(currentOpenBookId);
            }
        }
    }
    
    public void OpenBook(string bookId, string bookName, Sprite bookIcon)
    {
        currentOpenBookId = bookId;
        
        if (bookPanel != null)
        {
            bookPanel.SetActive(true);
            
            if (currentBookTitle != null)
                currentBookTitle.text = bookName;
                
            if (currentBookIcon != null && bookIcon != null)
                currentBookIcon.sprite = bookIcon;
                
            UpdateBookUI(bookId);
        }
    }
    
    private void UpdateBookUI(string bookId)
    {
        if (ingredientsGrid == null) return;
        
        foreach (Transform child in ingredientsGrid)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var ingredient in collectedIngredients.Values)
        {
            if (ingredientSlotPrefab != null)
            {
                GameObject ingredientSlot = Instantiate(ingredientSlotPrefab, ingredientsGrid);
                IngredientSlot slot = ingredientSlot.GetComponent<IngredientSlot>();
                
                if (slot != null)
                {
                    slot.Initialize(ingredient, this);
                }
            }
        }
    }
    
    public void ShowIngredientInfo(IngredientData ingredient)
    {
        if (ingredientPanel != null)
        {
            ingredientPanel.SetActive(true);
            
            if (ingredientNameText != null)
                ingredientNameText.text = ingredient.name;
                
            if (ingredientDescriptionText != null)
                ingredientDescriptionText.text = ingredient.description;
                
            if (ingredientIconImage != null && ingredient.icon != null)
                ingredientIconImage.sprite = ingredient.icon;
        }
    }
    
    public void CloseBook()
    {
        if (bookPanel != null) bookPanel.SetActive(false);
        currentOpenBookId = null;
    }
    
    public void CloseIngredientPanel()
    {
        if (ingredientPanel != null) ingredientPanel.SetActive(false);
    }
}