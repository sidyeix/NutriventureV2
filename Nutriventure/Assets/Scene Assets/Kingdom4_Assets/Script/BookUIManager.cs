using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Playables;
using System.Collections;

public class BookUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform bookUIContainer;
    public GameObject bookEntryPrefab;
    public GameObject bookPanel;
    public GameObject ingredientPanel;
    public GameObject gateObject; // Drag your gate cube here
    
    [Header("Current Book UI")]
    public Transform ingredientsGrid;
    public GameObject ingredientSlotPrefab;
    
    [Header("Ingredient Info Panel")]
    public TextMeshProUGUI ingredientNameText;
    public TextMeshProUGUI ingredientDescriptionText;
    public Image ingredientIconImage;
    public Button closeIngredientButton;
    public Button closeBookButton;
    
    [Header("All Possible Ingredients")]
    public List<IngredientDefinition> allPossibleIngredients = new List<IngredientDefinition>();
    
    [Header("Timeline")]
    public PlayableDirector timelineDirector; // Drag your timeline here
    
    private BookInteractable mainBook;
    private GameObject currentBookUIEntry;
    private bool timelinePlayed = false;
    
    [System.Serializable]
    public class IngredientDefinition
    {
        public string ingredientId;
        public string ingredientName;
        [TextArea(3, 5)]
        public string ingredientDescription;
        public Sprite ingredientIcon;
        public Sprite silhouetteSprite;
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
    }
    
    public void SetMainBook(BookInteractable book)
    {
        mainBook = book;
        Debug.Log($"BookUIManager: Book set: {book.bookName}");
        
        // Check immediately if we already have 9 ingredients
        CheckForTimeline();
    }
    
    public void AddBookToUI(string bookId, string bookName, Sprite bookIcon)
    {
        if (bookEntryPrefab != null && bookUIContainer != null)
        {
            if (currentBookUIEntry == null)
            {
                currentBookUIEntry = Instantiate(bookEntryPrefab, bookUIContainer);
                BookUIEntry entry = currentBookUIEntry.GetComponent<BookUIEntry>();
                
                if (entry != null)
                {
                    entry.Initialize(bookId, bookName, bookIcon, this);
                }
            }
        }
    }
    
    public void OpenBook(string bookId, string bookName, Sprite bookIcon)
    {
        if (bookPanel != null)
        {
            bookPanel.SetActive(true);
            UpdateBookUI();
        }
    }
    
    public void CloseBook()
    {
        if (bookPanel != null)
        {
            bookPanel.SetActive(false);
        }
    }
    
    public void UpdateBookUI()
    {
        if (ingredientsGrid == null || mainBook == null) return;
        
        // Clear grid
        foreach (Transform child in ingredientsGrid)
        {
            Destroy(child.gameObject);
        }
        
        // Add all ingredients
        foreach (var ingredientDef in allPossibleIngredients)
        {
            if (ingredientSlotPrefab != null)
            {
                GameObject ingredientSlot = Instantiate(ingredientSlotPrefab, ingredientsGrid);
                IngredientSlot slot = ingredientSlot.GetComponent<IngredientSlot>();
                
                if (slot != null)
                {
                    bool isCollected = mainBook.IsIngredientCollected(ingredientDef.ingredientId);
                    
                    BookInteractable.IngredientData data = new BookInteractable.IngredientData
                    {
                        ingredientId = ingredientDef.ingredientId,
                        ingredientName = ingredientDef.ingredientName,
                        ingredientDescription = ingredientDef.ingredientDescription,
                        ingredientIcon = ingredientDef.ingredientIcon
                    };
                    
                    slot.Initialize(data, this, isCollected);
                    
                    // Set silhouette if available
                    if (ingredientDef.silhouetteSprite != null)
                    {
                        slot.SetSilhouetteSprite(ingredientDef.silhouetteSprite);
                    }
                }
            }
        }
        
        // Check for timeline
        CheckForTimeline();
    }
    
    public void OnIngredientCollected(string ingredientId)
    {
        Debug.Log($"BookUIManager: Ingredient collected: {ingredientId}");
        UpdateBookUI();
    }
    
    private void CheckForTimeline()
    {
        if (mainBook == null || timelinePlayed) return;
        
        int collectedCount = mainBook.GetCollectedCount();
        
        if (collectedCount >= 9)
        {
            timelinePlayed = true;
            
            // Play timeline
            PlayTimeline();
            
            // Disable the gate cube
            if (gateObject != null)
            {
                gateObject.SetActive(false);
                Debug.Log("✅ BookUIManager: Gate cube disabled!");
            }
            else
            {
                Debug.LogWarning("⚠️ BookUIManager: Gate object not assigned in inspector!");
            }
        }
    }
    
    private void PlayTimeline()
    {
        if (timelineDirector != null)
        {
            Debug.Log("🎬 BookUIManager: Playing timeline!");
            timelineDirector.Play();
            
            // Listen for timeline completion
            StartCoroutine(WaitForTimelineCompletion());
        }
        else
        {
            Debug.LogError("❌ BookUIManager: No Timeline Director assigned!");
        }
    }
    
    private IEnumerator WaitForTimelineCompletion()
    {
        if (timelineDirector == null) yield break;
        
        // Wait for timeline to start
        yield return null;
        
        // Wait while timeline is playing
        while (timelineDirector.state == PlayState.Playing)
        {
            yield return null;
        }
        
        Debug.Log("✅ BookUIManager: Timeline completed!");
        
        // You can add additional logic here after timeline finishes
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
                
            if (ingredientIconImage != null)
                ingredientIconImage.sprite = ingredient.ingredientIcon;
        }
    }
    
    public void CloseIngredientPanel()
    {
        if (ingredientPanel != null)
        {
            ingredientPanel.SetActive(false);
        }
    }
    
    // Test function - Shows narration and disables gate
    [ContextMenu("Test Gate Opening")]
    public void TestGateOpening()
    {
        if (mainBook != null)
        {
            // Clear and add 9 ingredients
            mainBook.ClearAllIngredients();
            
            for (int i = 0; i < 9; i++)
            {
                mainBook.AddTestIngredient();
            }
            
            UpdateBookUI();
            Debug.Log("Test: 9 ingredients added - timeline should trigger!");
        }
    }
    
    [ContextMenu("Test Timeline Only")]
    public void TestTimelineOnly()
    {
        PlayTimeline();
    }
    
    [ContextMenu("Reset Timeline Trigger")]
    public void ResetTimelineTrigger()
    {
        timelinePlayed = false;
        
        // Re-enable the gate when resetting
        if (gateObject != null)
        {
            gateObject.SetActive(true);
            Debug.Log("BookUIManager: Timeline trigger reset and gate re-enabled");
        }
        
        // Stop timeline if it's playing
        if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
        {
            timelineDirector.Stop();
        }
        
        Debug.Log("BookUIManager: Timeline trigger reset - will trigger again at 9 ingredients");
    }
}