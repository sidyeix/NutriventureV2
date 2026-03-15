using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class IngredientCollectionUI : MonoBehaviour
{
    // =========================
    // DATABASE
    // =========================
    [Header("Database")]
    public IngredientDatabase database;

    // =========================
    // UI REFERENCES
    // =========================
    [Header("UI References")]
    public Transform contentParent;
    public IngredientCardUI cardPrefab;
    public KingdomFrameLibrary frameLibrary;
    [Header("3D Viewer")]
    public Enerling3DViewer viewer;

    // =========================
    // KINGDOM FILTER BUTTONS
    // =========================
    [Header("Kingdom Filter Buttons")]
    public Button allFilterButton;
    public Button nutriKingdomFilterButton;
    public Button alerthiaFilterButton;
    public Button sugariaFilterButton;
    public Button preserviaFilterButton;

    [Header("Filter Button Colors")]
    public Color selectedFilterColor = Color.white;
    public Color unselectedFilterColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    // =========================
    // RARITY DROPDOWN
    // =========================
    [Header("Rarity Dropdown")]
    public TMP_Dropdown rarityDropdown;

    [Header("Rarity Icons")]
    public Sprite allRarityIcon;
    public Sprite commonRarityIcon;
    public Sprite rareRarityIcon;
    public Sprite ultraRareRarityIcon;

    // =========================
    // INTERNAL DATA
    // =========================
    private List<IngredientDatabase.IngredientInfo> allIngredients;
    private List<GameObject> currentCards = new();

    // Separate lists for display and navigation
    private List<IngredientDatabase.IngredientInfo> currentDisplayList; // All items (including locked)
    private List<IngredientDatabase.IngredientInfo> currentNavigationList; // Only unlocked items

    private IngredientDatabase.KingdomOrigin? currentKingdomFilter = null;
    private IngredientDatabase.Rarity? currentRarityFilter = null;

    // =========================
    // START
    // =========================
    void Start()
    {
        ValidateReferences();

        allIngredients =
            new List<IngredientDatabase.IngredientInfo>(
                database.ingredients);

        SetupFilterButtons();
        SetupRarityDropdown();

        ApplyCombinedFilter();
    }

    // =========================
    // ON ENABLE — Refresh grid with latest unlock/catch data from PlayerPrefs
    // =========================
    void OnEnable()
    {
        if (allIngredients != null && allIngredients.Count > 0)
        {
            // Re-sync catch counts and unlock states from PlayerPrefs
            if (PersistentDataManager.Instance != null)
            {
                PersistentDataManager.Instance.SyncCatchCountsFromGameData();
            }

            ApplyCombinedFilter();
        }
    }

    // =========================
    // VALIDATION
    // =========================
    void ValidateReferences()
    {
        if (database == null)
            Debug.LogError("Database missing!");

        if (contentParent == null)
            Debug.LogError("Content parent missing!");

        if (cardPrefab == null)
            Debug.LogError("Card prefab missing!");

        if (frameLibrary == null)
            Debug.LogError("Frame library missing!");
    }

    // =========================
    // RARITY DROPDOWN SETUP
    // =========================
    void SetupRarityDropdown()
    {
        if (rarityDropdown == null)
        {
            Debug.LogWarning("Rarity Dropdown not assigned!");
            return;
        }

        rarityDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options =
            new List<TMP_Dropdown.OptionData>();

        options.Add(new TMP_Dropdown.OptionData("", allRarityIcon, Color.white));
        options.Add(new TMP_Dropdown.OptionData("", commonRarityIcon, Color.white));
        options.Add(new TMP_Dropdown.OptionData("", rareRarityIcon, Color.white));
        options.Add(new TMP_Dropdown.OptionData("", ultraRareRarityIcon, Color.white));

        rarityDropdown.AddOptions(options);

        // Hide caption text
        if (rarityDropdown.captionText != null)
        {
            rarityDropdown.captionText.text = "";
            rarityDropdown.captionText.color =
                new Color(0, 0, 0, 0);
        }

        rarityDropdown.onValueChanged.RemoveAllListeners();
        rarityDropdown.onValueChanged.AddListener(OnRarityChanged);

        rarityDropdown.value = 0;
        rarityDropdown.RefreshShownValue();
    }

    // =========================
    // RARITY CHANGE
    // =========================
    void OnRarityChanged(int index)
    {
        switch (index)
        {
            case 0:
                currentRarityFilter = null;
                break;

            case 1:
                currentRarityFilter =
                    IngredientDatabase.Rarity.Common;
                break;

            case 2:
                currentRarityFilter =
                    IngredientDatabase.Rarity.Rare;
                break;

            case 3:
                currentRarityFilter =
                    IngredientDatabase.Rarity.UltraRare;
                break;
        }

        ApplyCombinedFilter();
    }

    // =========================
    // KINGDOM FILTER
    // =========================
    void SetupFilterButtons()
    {
        if (allFilterButton != null)
            allFilterButton.onClick.AddListener(
                () => ApplyKingdomFilter(null));

        if (nutriKingdomFilterButton != null)
            nutriKingdomFilterButton.onClick.AddListener(
                () => ApplyKingdomFilter(
                    IngredientDatabase.KingdomOrigin.NutriKingdom));

        if (alerthiaFilterButton != null)
            alerthiaFilterButton.onClick.AddListener(
                () => ApplyKingdomFilter(
                    IngredientDatabase.KingdomOrigin.Alerthia));

        if (sugariaFilterButton != null)
            sugariaFilterButton.onClick.AddListener(
                () => ApplyKingdomFilter(
                    IngredientDatabase.KingdomOrigin.Sugaria));

        if (preserviaFilterButton != null)
            preserviaFilterButton.onClick.AddListener(
                () => ApplyKingdomFilter(
                    IngredientDatabase.KingdomOrigin.Preservia));
    }

    public void ApplyKingdomFilter(
        IngredientDatabase.KingdomOrigin? kingdom)
    {
        currentKingdomFilter = kingdom;

        UpdateFilterButtonHighlights(kingdom);
        ApplyCombinedFilter();
    }

    // =========================
    // COMBINED FILTER (FIXED)
    // =========================
    void ApplyCombinedFilter()
    {
        List<IngredientDatabase.IngredientInfo> filtered =
            new(allIngredients);

        // Apply kingdom filter
        if (currentKingdomFilter.HasValue)
        {
            filtered = filtered
                .Where(i =>
                    i.kingdom ==
                    currentKingdomFilter.Value)
                .ToList();
        }

        // Apply rarity filter
        if (currentRarityFilter.HasValue)
        {
            filtered = filtered
                .Where(i =>
                    i.rarity ==
                    currentRarityFilter.Value)
                .ToList();
        }

        // Store ALL filtered items for display (including locked)
        currentDisplayList = filtered;

        // Store ONLY unlocked items for navigation
        currentNavigationList = filtered
            .Where(i => i.isUnlocked)
            .ToList();

        Debug.Log($"Display: {currentDisplayList.Count} items (including locked), Navigation: {currentNavigationList.Count} unlocked items");

        // Populate the grid with ALL items (locked and unlocked)
        Populate(currentDisplayList);
    }

    // =========================
    // POPULATE UI
    // =========================
    void Populate(
        List<IngredientDatabase.IngredientInfo> list)
    {
        ClearCards();

        foreach (var ingredient in list)
        {
            var card = Instantiate(cardPrefab, contentParent);
            card.Setup(ingredient, database, frameLibrary, viewer, this);
            currentCards.Add(card.gameObject);
        }
    }

    void ClearCards()
    {
        foreach (var card in currentCards)
        {
            if (card != null)
                Destroy(card);
        }

        currentCards.Clear();
    }

    // =========================
    // BUTTON COLORS
    // =========================
    void UpdateFilterButtonHighlights(
        IngredientDatabase.KingdomOrigin? active)
    {
        SetButtonColor(allFilterButton, unselectedFilterColor);
        SetButtonColor(nutriKingdomFilterButton, unselectedFilterColor);
        SetButtonColor(alerthiaFilterButton, unselectedFilterColor);
        SetButtonColor(sugariaFilterButton, unselectedFilterColor);
        SetButtonColor(preserviaFilterButton, unselectedFilterColor);

        if (!active.HasValue)
        {
            SetButtonColor(allFilterButton, selectedFilterColor);
            return;
        }

        switch (active.Value)
        {
            case IngredientDatabase.KingdomOrigin.NutriKingdom:
                SetButtonColor(nutriKingdomFilterButton, selectedFilterColor);
                break;

            case IngredientDatabase.KingdomOrigin.Alerthia:
                SetButtonColor(alerthiaFilterButton, selectedFilterColor);
                break;

            case IngredientDatabase.KingdomOrigin.Sugaria:
                SetButtonColor(sugariaFilterButton, selectedFilterColor);
                break;

            case IngredientDatabase.KingdomOrigin.Preservia:
                SetButtonColor(preserviaFilterButton, selectedFilterColor);
                break;
        }
    }

    void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;

        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color;
        cb.selectedColor = color;
        btn.colors = cb;
    }

    // =========================
    // NAVIGATION HELPERS (UPDATED)
    // =========================
    public List<IngredientDatabase.IngredientInfo> GetCurrentFilteredList()
    {
        // Return ONLY unlocked items for navigation
        return currentNavigationList;
    }

    public int GetCurrentIndex(IngredientDatabase.IngredientInfo info)
    {
        // Find index in the navigation list (unlocked items only)
        if (currentNavigationList == null) return -1;
        return currentNavigationList.FindIndex(i => i.ingredientName == info.ingredientName);
    }
}