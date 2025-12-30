using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
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
    
    [Header("Narration Settings")]
    public AudioClip gateOpenSound; // Audio for when gate opens
    
    // ASSIGN THESE MANUALLY IN INSPECTOR
    public Canvas dialogueCanvas; 
    public TextMeshProUGUI dialogueText; 
    public AudioSource audioSource; 
    
    [Header("Narration Timing")]
    public float narrationDisplayTime = 6f; 
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.5f;
    
    private BookInteractable mainBook;
    private GameObject currentBookUIEntry;
    private bool gateNarrationPlayed = false;
    private Coroutine currentNarration;
    private CanvasGroup dialogueCanvasGroup;
    
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
        
        // Set up dialogue canvas (MANUAL ASSIGNMENT REQUIRED)
        if (dialogueCanvas != null)
        {
            dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialogueCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            dialogueCanvasGroup.alpha = 0f;
        }
        else
        {
            Debug.LogWarning("BookUIManager: No dialogue canvas assigned! Assign in Inspector.");
        }
        
        // Set up audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
     public void SetMainBook(BookInteractable book)
    {
        mainBook = book;
        Debug.Log($"BookUIManager: Book set: {book.bookName}");
        
        // Check immediately if we already have 9 ingredients
        CheckForGateNarration();
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
        
        // Check for gate narration
        CheckForGateNarration();
    }
    
    public void OnIngredientCollected(string ingredientId)
    {
        Debug.Log($"BookUIManager: Ingredient collected: {ingredientId}");
        UpdateBookUI();
    }
    
    private void CheckForGateNarration()
    {
        if (mainBook == null || gateNarrationPlayed) return;
        
        int collectedCount = mainBook.GetCollectedCount();
        
        if (collectedCount >= 9)
        {
            gateNarrationPlayed = true;
            
            // Show narration message
            ShowNarrationMessage();
            
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
    
    private void ShowNarrationMessage()
{
    string message = "Congratulations on finding all 9 allergies! The gate to the wagon is now open. Proceed with it to heal the kingdom of Allerthria.";
    
    // Use CanvasCoordinator if available
    if (CanvasCoordinator.Instance != null)
    {
        CanvasCoordinator.Instance.ShowBookNarration(message, gateOpenSound);
    }
    else
    {
        // Fallback to original method
        Debug.Log("CanvasCoordinator not found, using fallback narration");
        
        // Stop any existing narration
        if (currentNarration != null)
        {
            StopCoroutine(currentNarration);
        }
        
        // Start new narration
        currentNarration = StartCoroutine(ShowDialogueCoroutine(message, gateOpenSound));
    }
    
    Debug.Log("🎉 BookUIManager: NARRATION TRIGGERED!");
}
    
    private IEnumerator ShowDialogueCoroutine(string message, AudioClip soundClip = null)
    {
        // If no canvas or text, skip
        if (dialogueCanvas == null || dialogueText == null)
        {
            Debug.LogError("BookUIManager: Canvas or Text is null! Cannot show narration.");
            currentNarration = null;
            yield break;
        }
        
        // Make sure canvas is active
        if (!dialogueCanvas.gameObject.activeSelf)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }
        
        // Make sure CanvasGroup exists
        if (dialogueCanvasGroup == null)
        {
            dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = dialogueCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Set the text
        dialogueText.text = message;
        
        // Fade in
        float timer = 0f;
        while (timer < fadeInTime)
        {
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInTime);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f;
        }
        
        // Play audio if provided
        if (soundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundClip);
            yield return new WaitForSeconds(Mathf.Max(soundClip.length, narrationDisplayTime));
        }
        else
        {
            yield return new WaitForSeconds(narrationDisplayTime);
        }
        
        // Fade out
        timer = 0f;
        while (timer < fadeOutTime)
        {
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutTime);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
        }
        
        // Clear text
        dialogueText.text = "";
        
        currentNarration = null;
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
            Debug.Log("Test: 9 ingredients added - gate narration should trigger!");
        }
    }
    
    [ContextMenu("Test Narration Only")]
    public void TestNarrationOnly()
    {
        ShowNarrationMessage();
    }
    
    [ContextMenu("Reset Gate Narration")]
    public void ResetGateNarration()
    {
        gateNarrationPlayed = false;
        
        // Re-enable the gate when resetting
        if (gateObject != null)
        {
            gateObject.SetActive(true);
            Debug.Log("BookUIManager: Gate narration reset and gate re-enabled");
        }
        
        // Clear any current narration
        if (currentNarration != null)
        {
            StopCoroutine(currentNarration);
        }
        
        // Hide dialogue
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        
        Debug.Log("BookUIManager: Gate narration reset - will trigger again at 9 ingredients");
    }
    
}