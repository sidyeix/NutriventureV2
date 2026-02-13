using UnityEngine;
using System.Collections.Generic;

public class IngredientCollectionUI : MonoBehaviour
{
    public IngredientDatabase database;
    public Transform contentParent;
    public IngredientCardUI cardPrefab;
    public GameObject lockedCardPrefab;
    public KingdomFrameLibrary frameLibrary;

    void Start()
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

        if (database.ingredients == null || database.ingredients.Count == 0)
        {
            Debug.LogError("Database ingredients list is empty!");
            return;
        }

        Debug.Log($"Populating collection with {database.ingredients.Count} ingredients");
        Populate(database.ingredients);
    }

    public void Populate(List<IngredientDatabase.IngredientInfo> list)
    {
        if (list == null)
        {
            Debug.LogError("Ingredients list is null!");
            return;
        }

        // Clear existing cards
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

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
                unlockedCount++;
                Debug.Log($"Created unlocked card for: {ingredient.ingredientName}");
            }
            else
            {
                // Spawn locked prefab
                if (lockedCardPrefab != null)
                {
                    Instantiate(lockedCardPrefab, contentParent);
                    lockedCount++;
                }
            }
        }

        Debug.Log($"Population complete - Unlocked: {unlockedCount}, Locked: {lockedCount}");
    }
}