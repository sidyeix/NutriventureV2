using UnityEngine;
using System.Collections.Generic;

public class IngredientCollectionUI : MonoBehaviour
{
    public IngredientDatabase database;

    public Transform contentParent;

    public IngredientCardUI cardPrefab;     // UNLOCKED prefab
    public GameObject lockedCardPrefab;     // 🔒 LOCKED prefab

    public KingdomFrameLibrary frameLibrary;

    void Start()
    {
        Populate(database.ingredients);
    }

    public void Populate(List<IngredientDatabase.IngredientInfo> list)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var ingredient in list)
        {
            // 🔒 CHECK IF LOCKED
            if (ingredient.isUnlocked)
            {
                // Spawn normal card
                var card = Instantiate(cardPrefab, contentParent);
                card.Setup(ingredient, database, frameLibrary);
            }
            else
            {
                // Spawn locked prefab instead
                Instantiate(lockedCardPrefab, contentParent);
            }
        }
    }
}
