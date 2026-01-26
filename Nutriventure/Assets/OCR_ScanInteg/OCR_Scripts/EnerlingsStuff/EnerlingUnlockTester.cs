using UnityEngine;

public class EnerlingUnlockTester : MonoBehaviour
{
    public IngredientDatabase ingredientDatabase;

    [Header("Testing Options")]
    public bool unlockAllOnStart = false;
    public bool unlockRandomOnStart = false;
    public int randomUnlockCount = 3;
    public bool resetOnStart = false;

    void Start()
    {
        if (resetOnStart)
        {
            ResetAllUnlocks();
        }

        if (unlockAllOnStart)
        {
            UnlockAllEnerlings();
        }
        else if (unlockRandomOnStart)
        {
            UnlockRandomEnerlings(randomUnlockCount);
        }
    }

    [ContextMenu("Unlock All Enerlings")]
    public void UnlockAllEnerlings()
    {
        if (PersistentDataManager.Instance != null && ingredientDatabase != null)
        {
            foreach (var ingredient in ingredientDatabase.ingredients)
            {
                PersistentDataManager.Instance.UnlockEnerling(ingredient.ingredientName);
            }
            Debug.Log($"Unlocked all {ingredientDatabase.ingredients.Count} enerlings");

            // Refresh UI if needed
            var manager = FindObjectOfType<EnerlingSelectionManager>();
            if (manager != null)
            {
                manager.RefreshDisplay();
            }
        }
    }

    [ContextMenu("Unlock Random Enerlings")]
    public void UnlockRandomEnerlings(int count = 3)
    {
        if (PersistentDataManager.Instance != null && ingredientDatabase != null)
        {
            System.Random random = new System.Random();
            int unlocked = 0;

            while (unlocked < count && unlocked < ingredientDatabase.ingredients.Count)
            {
                int randomIndex = random.Next(ingredientDatabase.ingredients.Count);
                string enerlingName = ingredientDatabase.ingredients[randomIndex].ingredientName;

                if (!PersistentDataManager.Instance.IsEnerlingUnlocked(enerlingName))
                {
                    PersistentDataManager.Instance.UnlockEnerling(enerlingName);
                    unlocked++;
                    Debug.Log($"Unlocked: {enerlingName}");
                }
            }

            // Refresh UI if needed
            var manager = FindObjectOfType<EnerlingSelectionManager>();
            if (manager != null)
            {
                manager.RefreshDisplay();
            }
        }
    }

    [ContextMenu("Reset All Unlocks")]
    public void ResetAllUnlocks()
    {
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.ResetAllProgress();
            Debug.Log("Reset all unlocks");

            // Refresh UI if needed
            var manager = FindObjectOfType<EnerlingSelectionManager>();
            if (manager != null)
            {
                manager.RefreshDisplay();
            }
        }
    }

    [ContextMenu("Heal All Enerlings")]
    public void HealAllEnerlings()
    {
        if (PersistentDataManager.Instance != null && ingredientDatabase != null)
        {
            foreach (var ingredient in ingredientDatabase.ingredients)
            {
                if (ingredient.isUnlocked)
                {
                    ingredient.currentLife = ingredient.baseLife;
                    PersistentDataManager.Instance.SaveEnerlingCurrentLife(ingredient.ingredientName, ingredient.currentLife);
                }
            }
            Debug.Log("Healed all enerlings");
        }
    }
}