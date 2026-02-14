using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class IngredientCollectionUI : MonoBehaviour
{
    public IngredientDatabase database;
    public Transform contentParent;
    public IngredientCardUI cardPrefab;
    public GameObject lockedCardPrefab;
    public KingdomFrameLibrary frameLibrary;
    
    [Header("Filter Buttons")]
    public Button allFilterButton;
    public Button nutriKingdomFilterButton;
    public Button alerthiaFilterButton;
    public Button sugariaFilterButton;
    public Button preserviaFilterButton;
    
    [Header("Filter Button Colors")]
    public Color selectedFilterColor = Color.white;
    public Color unselectedFilterColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private List<IngredientDatabase.IngredientInfo> allIngredients;
    private List<GameObject> currentCards = new List<GameObject>();
    private IngredientDatabase.KingdomOrigin? currentFilter = null;

    void Start()
    {
        ValidateReferences();
        
        if (database.ingredients == null || database.ingredients.Count == 0)
        {
            Debug.LogError("Database ingredients list is empty!");
            return;
        }

        // Store all ingredients
        allIngredients = new List<IngredientDatabase.IngredientInfo>(database.ingredients);
        
        Debug.Log($"Populating collection with {allIngredients.Count} ingredients");
        
        // Setup filter buttons
        SetupFilterButtons();
        
        // Initially show all ingredients
        ApplyFilter(null);
    }

    void ValidateReferences()
    {
        if (database == null)
        {
            Debug.LogError("Database is not assigned in IngredientCollectionUI!");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("Content Parent is not assigned in IngredientCollectionUI!");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab is not assigned in IngredientCollectionUI!");
            return;
        }

        if (frameLibrary == null)
        {
            Debug.LogError("Frame Library is not assigned in IngredientCollectionUI!");
            return;
        }
    }

    void SetupFilterButtons()
    {
        // Assign button listeners if buttons are assigned
        if (allFilterButton != null)
        {
            allFilterButton.onClick.RemoveAllListeners();
            allFilterButton.onClick.AddListener(() => ApplyFilter(null));
        }
        
        if (nutriKingdomFilterButton != null)
        {
            nutriKingdomFilterButton.onClick.RemoveAllListeners();
            nutriKingdomFilterButton.onClick.AddListener(() => ApplyFilter(IngredientDatabase.KingdomOrigin.NutriKingdom));
        }
        
        if (alerthiaFilterButton != null)
        {
            alerthiaFilterButton.onClick.RemoveAllListeners();
            alerthiaFilterButton.onClick.AddListener(() => ApplyFilter(IngredientDatabase.KingdomOrigin.Alerthia));
        }
        
        if (sugariaFilterButton != null)
        {
            sugariaFilterButton.onClick.RemoveAllListeners();
            sugariaFilterButton.onClick.AddListener(() => ApplyFilter(IngredientDatabase.KingdomOrigin.Sugaria));
        }
        
        if (preserviaFilterButton != null)
        {
            preserviaFilterButton.onClick.RemoveAllListeners();
            preserviaFilterButton.onClick.AddListener(() => ApplyFilter(IngredientDatabase.KingdomOrigin.Preservia));
        }
        
        // Initially highlight the All button
        UpdateFilterButtonHighlights(null);
    }

    public void ApplyFilter(IngredientDatabase.KingdomOrigin? kingdomFilter)
    {
        currentFilter = kingdomFilter;
        
        // Update button highlights
        UpdateFilterButtonHighlights(kingdomFilter);
        
        // Filter the ingredients
        List<IngredientDatabase.IngredientInfo> filteredList;
        
        if (kingdomFilter.HasValue)
        {
            filteredList = allIngredients
                .Where(i => i.kingdom == kingdomFilter.Value)
                .ToList();
            
            Debug.Log($"Filtering by {kingdomFilter.Value}: Found {filteredList.Count} ingredients");
        }
        else
        {
            filteredList = allIngredients;
            Debug.Log($"Showing all ingredients: {filteredList.Count}");
        }
        
        // Repopulate with filtered list
        Populate(filteredList);
    }

    void UpdateFilterButtonHighlights(IngredientDatabase.KingdomOrigin? activeFilter)
    {
        // Reset all buttons to unselected color
        SetButtonColor(allFilterButton, unselectedFilterColor);
        SetButtonColor(nutriKingdomFilterButton, unselectedFilterColor);
        SetButtonColor(alerthiaFilterButton, unselectedFilterColor);
        SetButtonColor(sugariaFilterButton, unselectedFilterColor);
        SetButtonColor(preserviaFilterButton, unselectedFilterColor);
        
        // Set active button to selected color
        if (!activeFilter.HasValue)
        {
            SetButtonColor(allFilterButton, selectedFilterColor);
        }
        else
        {
            switch (activeFilter.Value)
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
    }

    void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = color;
            button.colors = colors;
        }
    }

    public void Populate(List<IngredientDatabase.IngredientInfo> list)
    {
        if (list == null)
        {
            Debug.LogError("Ingredients list is null!");
            return;
        }

        // Clear existing cards
        ClearCurrentCards();

        int unlockedCount = 0;
        int lockedCount = 0;

        // Create new cards
        foreach (var ingredient in list)
        {
            if (ingredient == null) 
            {
                Debug.LogWarning("Found null ingredient in list");
                continue;
            }

            if (ingredient.isUnlocked)
            {
                // Spawn normal card
                var card = Instantiate(cardPrefab, contentParent);
                card.Setup(ingredient, database, frameLibrary);
                currentCards.Add(card.gameObject);
                unlockedCount++;
                Debug.Log($"Created unlocked card for: {ingredient.ingredientName} from {ingredient.kingdom}");
            }
            else
            {
                // Spawn locked prefab
                if (lockedCardPrefab != null)
                {
                    var lockedCard = Instantiate(lockedCardPrefab, contentParent);
                    currentCards.Add(lockedCard);
                    lockedCount++;
                }
            }
        }

        Debug.Log($"Population complete - Unlocked: {unlockedCount}, Locked: {lockedCount}, Total: {list.Count}");
    }

    void ClearCurrentCards()
    {
        foreach (var card in currentCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        currentCards.Clear();
    }

    // Public methods to trigger filters from other scripts if needed
    public void ShowAll() => ApplyFilter(null);
    public void ShowNutriKingdom() => ApplyFilter(IngredientDatabase.KingdomOrigin.NutriKingdom);
    public void ShowAlerthia() => ApplyFilter(IngredientDatabase.KingdomOrigin.Alerthia);
    public void ShowSugaria() => ApplyFilter(IngredientDatabase.KingdomOrigin.Sugaria);
    public void ShowPreservia() => ApplyFilter(IngredientDatabase.KingdomOrigin.Preservia);
}