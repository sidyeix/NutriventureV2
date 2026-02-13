using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image frameImage;
    public Image enerlingImage;
    public Image rarityIcon;
    public Image smallIconImage;
    public GameObject lockIcon;
    public TextMeshProUGUI progressText;

    private IngredientDatabase database;
    private IngredientDatabase.IngredientInfo currentInfo;
    private KingdomFrameLibrary frameLibrary;
    private Button button;
    private bool isInitialized = false; // Add initialization flag

    private void Awake()
    {
        // Get or add button component
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        // Don't add listener here - we'll add it after Setup
    }

    private void OnEnable()
    {
        // Only add listener if we're initialized
        if (button != null && isInitialized)
        {
            button.onClick.RemoveListener(OnClickCard); // Remove first to avoid duplicates
            button.onClick.AddListener(OnClickCard);
        }
    }

    private void OnDisable()
    {
        // Remove listener when disabled
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickCard);
        }
    }

    public void Setup(
        IngredientDatabase.IngredientInfo info,
        IngredientDatabase db,
        KingdomFrameLibrary library)
    {
        if (info == null)
        {
            Debug.LogError("IngredientInfo is null in Setup");
            return;
        }
        
        if (db == null)
        {
            Debug.LogError("Database is null in Setup");
            return;
        }
        
        if (library == null)
        {
            Debug.LogError("FrameLibrary is null in Setup");
            return;
        }

        // Store references
        currentInfo = info;
        database = db;
        frameLibrary = library;
        isInitialized = true;

        // Add button listener now that we're initialized
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickCard); // Remove any existing listeners
            button.onClick.AddListener(OnClickCard);
        }

        // Log to confirm setup
        Debug.Log($"Card setup for: {info.ingredientName}, Unlocked: {info.isUnlocked}");

        // =========================
        // FRAME BY KINGDOM
        // =========================
        if (frameImage != null)
            frameImage.sprite = library.GetFrame(info.kingdom);

        // =========================
        // BIG ICON
        // =========================
        if (enerlingImage != null)
        {
            Sprite customIcon = library.GetEnerlingIcon(info.ingredientName);
            enerlingImage.sprite = customIcon != null ? customIcon : info.enerlingSprite;
            
            if (enerlingImage.sprite == null)
                Debug.LogWarning($"No sprite found for {info.ingredientName}");
        }

        // =========================
        // SMALL ICON
        // =========================
        if (smallIconImage != null && enerlingImage != null)
        {
            smallIconImage.sprite = enerlingImage.sprite;
            smallIconImage.gameObject.SetActive(smallIconImage.sprite != null);
        }

        // =========================
        // RARITY ICON
        // =========================
        if (rarityIcon != null)
        {
            Sprite customRarity = library.GetRarityIcon(info.rarity);
            rarityIcon.sprite = customRarity != null ? customRarity : db.GetRarityIcon(info.rarity);
        }

        // =========================
        // LOCK STATE
        // =========================
        bool unlocked = info.isUnlocked;
        
        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);
        
        if (enerlingImage != null)
            enerlingImage.color = unlocked ? Color.white : Color.black;

        if (smallIconImage != null)
            smallIconImage.color = unlocked ? Color.white : Color.black;

        if (progressText != null)
            progressText.text = unlocked ? "1/20" : "0/20";
    }

    // =========================
    // CLICK → OPEN DETAILS
    // =========================
    public void OnClickCard()
    {
        Debug.Log("CLICK WORKING");

        // Double-check initialization
        if (!isInitialized)
        {
            Debug.LogError("Card not initialized! Setup() must be called before clicking.");
            return;
        }

        if (currentInfo == null) 
        {
            Debug.LogError("CurrentInfo is null in IngredientCardUI even though isInitialized is true");
            return;
        }

        if (!currentInfo.isUnlocked)
        {
            Debug.Log(currentInfo.ingredientName + " is locked.");
            return;
        }

        if (database == null)
        {
            Debug.LogError("Database is null in IngredientCardUI");
            return;
        }

        Debug.Log($"Clicking on {currentInfo.ingredientName} - Opening details...");

        // Find the details panel in the scene
        IngredientDetailsUI detailsPanel = FindObjectOfType<IngredientDetailsUI>(true);
        
        if (detailsPanel != null)
        {
            detailsPanel.ShowDetails(currentInfo, database);
        }
        else
        {
            Debug.LogError("IngredientDetailsUI not found in the scene! Make sure it exists in the hierarchy.");
        }
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (button != null)
        {
            button.onClick.RemoveListener(OnClickCard);
        }
    }
}