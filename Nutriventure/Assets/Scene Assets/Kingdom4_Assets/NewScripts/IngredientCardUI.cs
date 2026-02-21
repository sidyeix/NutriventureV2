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
    
    [Header("Catch Progress")] // NEW
    public Slider catchProgressSlider; // Add this in Inspector
    public TextMeshProUGUI catchCountText; // Replace or keep progressText
    public Image sliderFillImage; // Optional: for color changes
    
    // Keep original progressText for backward compatibility
    public TextMeshProUGUI progressText;

    private IngredientDatabase database;
    private IngredientDatabase.IngredientInfo currentInfo;
    private KingdomFrameLibrary frameLibrary;
    private Enerling3DViewer viewer;
    private IngredientCollectionUI collection;

    private Button button;
    private bool isInitialized = false;

    // Colors for slider (optional)
    [Header("Slider Colors")]
    public Color lowProgressColor = new Color(1f, 0.2f, 0.2f); // Red
    public Color mediumProgressColor = new Color(1f, 0.8f, 0.2f); // Yellow
    public Color highProgressColor = new Color(0.2f, 1f, 0.2f); // Green
    public Color completedColor = new Color(0.2f, 0.5f, 1f); // Blue

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
            button = gameObject.AddComponent<Button>();
    }

    // =========================
    // SETUP (UPDATED)
    // =========================
    public void Setup(
        IngredientDatabase.IngredientInfo info,
        IngredientDatabase db,
        KingdomFrameLibrary library,
        Enerling3DViewer v,
        IngredientCollectionUI col)
    {
        currentInfo = info;
        database = db;
        frameLibrary = library;
        viewer = v;
        collection = col;
        isInitialized = true;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickCard);

        // FRAME
        if (frameImage != null)
            frameImage.sprite =
                library.GetFrame(info.kingdom);

        // BIG ICON
        if (enerlingImage != null)
        {
            Sprite customIcon =
                library.GetEnerlingIcon(
                    info.ingredientName);

            enerlingImage.sprite =
                customIcon != null
                ? customIcon
                : info.enerlingSprite;
        }

        // SMALL ICON
        if (smallIconImage != null)
        {
            smallIconImage.sprite =
                enerlingImage.sprite;
        }

        // RARITY
        if (rarityIcon != null)
        {
            Sprite customRarity =
                library.GetRarityIcon(
                    info.rarity);

            rarityIcon.sprite =
                customRarity != null
                ? customRarity
                : db.GetRarityIcon(info.rarity);
        }

        // ==========================================
        // Update visual states based on unlock status
        // ==========================================
        bool unlocked = info.isUnlocked;

        // Set lock icon visibility
        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        // Set button interactability
        if (button != null)
            button.interactable = unlocked;

        // Set image colors based on unlock status
        Color targetColor = unlocked ? Color.white : Color.black;
        
        if (enerlingImage != null)
            enerlingImage.color = targetColor;
        
        if (smallIconImage != null)
            smallIconImage.color = targetColor;

        // ==========================================
        // CATCH PROGRESS SETUP (NEW)
        // ==========================================
        if (unlocked)
        {
            SetupCatchProgress();
        }
        else
        {
            // For locked ingredients, show 0/max
            if (catchProgressSlider != null)
            {
                catchProgressSlider.value = 0;
                catchProgressSlider.maxValue = info.maxCatch;
                catchProgressSlider.interactable = false;
            }
            
            if (catchCountText != null)
            {
                catchCountText.text = $"0/{info.maxCatch}";
            }
            
            // Keep original progress text for backward compatibility
            if (progressText != null)
                progressText.text = "0/20";
        }
    }

    // =========================
    // CATCH PROGRESS SETUP (NEW)
    // =========================
    private void SetupCatchProgress()
    {
        // Setup slider
        if (catchProgressSlider != null)
        {
            catchProgressSlider.minValue = 0;
            catchProgressSlider.maxValue = currentInfo.maxCatch;
            catchProgressSlider.value = currentInfo.currentCatchCount;
            catchProgressSlider.interactable = false; // Read-only
            
            // Update slider fill color based on progress
            UpdateSliderColor();
        }
        
        // Update catch count text
        if (catchCountText != null)
        {
            catchCountText.text = $"{currentInfo.currentCatchCount}/{currentInfo.maxCatch}";
        }
        
        // Keep original progress text for backward compatibility
        if (progressText != null)
        {
            progressText.text = $"{currentInfo.currentCatchCount}/{currentInfo.maxCatch}";
        }
    }

    // =========================
    // UPDATE SLIDER COLOR (NEW)
    // =========================
    private void UpdateSliderColor()
    {
        if (sliderFillImage == null || catchProgressSlider == null)
            return;
            
        float progress = (float)currentInfo.currentCatchCount / currentInfo.maxCatch;
        
        if (currentInfo.currentCatchCount >= currentInfo.maxCatch)
        {
            sliderFillImage.color = completedColor;
        }
        else if (progress >= 0.66f)
        {
            sliderFillImage.color = highProgressColor;
        }
        else if (progress >= 0.33f)
        {
            sliderFillImage.color = mediumProgressColor;
        }
        else
        {
            sliderFillImage.color = lowProgressColor;
        }
    }

    // =========================
    // REFRESH CARD (NEW)
    // Call this when catch count changes
    // =========================
    public void RefreshCard()
    {
        if (currentInfo == null) return;
        
        // Update slider
        if (catchProgressSlider != null)
        {
            catchProgressSlider.value = currentInfo.currentCatchCount;
            UpdateSliderColor();
        }
        
        // Update texts
        if (catchCountText != null)
        {
            catchCountText.text = $"{currentInfo.currentCatchCount}/{currentInfo.maxCatch}";
        }
        
        if (progressText != null)
        {
            progressText.text = $"{currentInfo.currentCatchCount}/{currentInfo.maxCatch}";
        }
        
        // Update lock state (in case it was unlocked)
        if (lockIcon != null)
            lockIcon.SetActive(!currentInfo.isUnlocked);
        
        if (button != null)
            button.interactable = currentInfo.isUnlocked;
    }

    // =========================
    // CLICK
    // =========================
    public void OnClickCard()
    {
        if (!isInitialized) return;

        if (!currentInfo.isUnlocked)
            return;

        // OPEN DETAILS
        IngredientDetailsUI details =
            FindObjectOfType<IngredientDetailsUI>(true);

        if (details != null)
        {
            // Pass the current filtered list and index for navigation
            details.ShowDetails(
                currentInfo,
                database,
                collection != null ? collection.GetCurrentFilteredList() : null,
                collection != null ? collection.GetCurrentIndex(currentInfo) : -1);
        }

        // SHOW 3D MODEL
        if (viewer != null)
        {
            viewer.ShowEnerling(currentInfo);
        }
        else
        {
            Debug.LogWarning("Viewer not assigned!");
        }
    }
}