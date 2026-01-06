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
    public AudioSource audioSource; 
    public K2_SubtitleController subtitleController; // Reference to your subtitle controller
    
    [Header("Dialogue Canvas (for subtitle display)")]
    public GameObject dialogueCanvas; // The canvas that contains subtitle UI - like NPCGuardController
    
    [Header("Subtitle Timing")]
    public float narrationDisplayTime = 6f; 
    public float typingSpeed = 0.05f; // Speed for subtitle typing
    
    private BookInteractable mainBook;
    private GameObject currentBookUIEntry;
    private bool gateNarrationPlayed = false;
    private Coroutine currentNarration;
    
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
        
        // Set up audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Initialize dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("BookUIManager: Dialogue canvas initialized (set inactive)");
        }
        else
        {
            Debug.LogWarning("BookUIManager: No dialogue canvas assigned! Subtitle display will fail.");
        }
        
        // Check for subtitle controller
        if (subtitleController == null)
        {
            Debug.LogWarning("BookUIManager: No subtitle controller assigned! Assign K2_SubtitleController in Inspector.");
        }
        else
        {
            // Make sure subtitle controller is initially disabled if it's on the dialogue canvas
            if (subtitleController.gameObject == dialogueCanvas)
            {
                subtitleController.enabled = false;
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
        
        if (currentNarration != null)
        {
            StopCoroutine(currentNarration);
        }
        
        // Start new narration using subtitle controller
        currentNarration = StartCoroutine(ShowSubtitleNarration(message, gateOpenSound));
        
        Debug.Log("🎉 BookUIManager: NARRATION TRIGGERED!");
    }
    
    private IEnumerator ShowSubtitleNarration(string message, AudioClip soundClip = null)
    {
        // Check if we have required components
        if (subtitleController == null)
        {
            Debug.LogError("BookUIManager: Subtitle controller is null! Cannot show narration.");
            currentNarration = null;
            yield break;
        }
        
        if (dialogueCanvas == null)
        {
            Debug.LogError("BookUIManager: Dialogue canvas is null! Cannot show subtitle UI.");
            currentNarration = null;
            yield break;
        }
        
        // ACTIVATE THE DIALOGUE CANVAS FIRST
        if (!dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(true);
            Debug.Log("BookUIManager: Activated dialogue canvas for narration");
            
            // Wait for the canvas to be fully activated
            yield return null;
        }
        
        // Ensure the subtitle controller script is enabled
        if (!subtitleController.enabled)
        {
            subtitleController.enabled = true;
            Debug.Log("BookUIManager: Enabled subtitle controller script");
        }
        
        // Check if subtitleTextUI is properly set up
        if (subtitleController.subtitleTextUI == null)
        {
            Debug.LogError("BookUIManager: Subtitle Text UI is null! Check K2_SubtitleController setup.");
            currentNarration = null;
            yield break;
        }
        
        // Ensure the Text UI is active
        if (!subtitleController.subtitleTextUI.gameObject.activeInHierarchy)
        {
            subtitleController.subtitleTextUI.gameObject.SetActive(true);
            Debug.Log("BookUIManager: Activated subtitle text UI");
        }
        
        // Now show the subtitle
        Debug.Log($"BookUIManager: Showing subtitle: {message}");
        subtitleController.ShowSubtitle(message, typingSpeed);
        
        // Play audio if provided
        if (soundClip != null && audioSource != null)
        {
            Debug.Log($"BookUIManager: Playing audio clip: {soundClip.name}");
            audioSource.PlayOneShot(soundClip);
            float clipDuration = soundClip.length;
            float waitTime = Mathf.Max(clipDuration, narrationDisplayTime);
            Debug.Log($"BookUIManager: Waiting for {waitTime} seconds");
            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            Debug.Log($"BookUIManager: No audio, waiting {narrationDisplayTime} seconds");
            yield return new WaitForSeconds(narrationDisplayTime);
        }
        
        // Clear subtitle
        subtitleController.ClearSubtitle();
        Debug.Log("BookUIManager: Cleared subtitle");
        
        // Deactivate the dialogue canvas after narration is complete
        // (This matches what NPCGuardController does)
        if (dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("BookUIManager: Deactivated dialogue canvas after narration");
        }
        
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
        
        // Clear subtitle and deactivate canvas if needed
        if (subtitleController != null)
        {
            subtitleController.ClearSubtitle();
        }
        
        if (dialogueCanvas != null && dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(false);
        }
        
        Debug.Log("BookUIManager: Gate narration reset - will trigger again at 9 ingredients");
    }
    
    // Clean up when this object is destroyed
    void OnDestroy()
    {
        if (currentNarration != null)
        {
            StopCoroutine(currentNarration);
        }
        
        // Ensure dialogue canvas is deactivated
        if (dialogueCanvas != null && dialogueCanvas.activeInHierarchy)
        {
            dialogueCanvas.SetActive(false);
        }
    }
}