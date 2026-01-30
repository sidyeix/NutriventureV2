using UnityEngine;
using System.Collections.Generic;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    [Header("References")]
    public IngredientDatabase ingredientDatabase;

    // Saved data
    private string selectedEnerlingName = "";
    private string opponentEnerlingName = "";
    private Dictionary<string, int> enerlingCurrentLife = new Dictionary<string, int>();
    private HashSet<string> unlockedEnerlings = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved data first
            LoadAllData();

            // Then apply unlocks to database
            ApplyUnlocksToDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Apply unlocked status to the database
    private void ApplyUnlocksToDatabase()
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase not assigned to PersistentDataManager!");
            return;
        }

        Debug.Log($"Applying {unlockedEnerlings.Count} unlocks to database...");

        // Reset all to locked first (in case of reload)
        foreach (var ingredient in ingredientDatabase.ingredients)
        {
            ingredient.isUnlocked = false;
        }

        // Then unlock based on saved data
        foreach (var enerlingName in unlockedEnerlings)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(enerlingName);
            if (ingredient != null)
            {
                ingredient.isUnlocked = true;
                Debug.Log($"Applied unlock to database: {enerlingName}");
            }
            else
            {
                Debug.LogWarning($"Could not find ingredient in database: {enerlingName}");
            }
        }
    }

    // Save selected enerling (player's enerling)
    public void SaveSelectedEnerling(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        selectedEnerlingName = enerlingName;
        PlayerPrefs.SetString("SelectedEnerling", enerlingName);
        PlayerPrefs.Save();
        Debug.Log($"Saved selected enerling: {enerlingName}");
    }

    // Get selected enerling (player's enerling)
    public string GetSelectedEnerlingName()
    {
        return selectedEnerlingName;
    }

    // Save opponent enerling (AI opponent to fight against)
    public void SaveOpponentEnerling(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        opponentEnerlingName = enerlingName;
        PlayerPrefs.SetString("OpponentEnerling", enerlingName);
        PlayerPrefs.Save();
        Debug.Log($"Saved opponent enerling: {enerlingName}");
    }

    // Get opponent enerling (AI opponent to fight against)
    public string GetOpponentEnerlingName()
    {
        return opponentEnerlingName;
    }

    // Clear opponent data (useful when restarting)
    public void ClearOpponentData()
    {
        opponentEnerlingName = "";
        PlayerPrefs.DeleteKey("OpponentEnerling");
        PlayerPrefs.Save();
        Debug.Log("Cleared opponent data");
    }

    // Save enerling current life
    public void SaveEnerlingCurrentLife(string enerlingName, int currentLife)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        if (enerlingCurrentLife.ContainsKey(enerlingName))
        {
            enerlingCurrentLife[enerlingName] = currentLife;
        }
        else
        {
            enerlingCurrentLife.Add(enerlingName, currentLife);
        }

        PlayerPrefs.SetInt(enerlingName + "_CurrentLife", currentLife);
        PlayerPrefs.Save();
    }

    // Get enerling current life
    public int GetEnerlingCurrentLife(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return -1;

        if (enerlingCurrentLife.ContainsKey(enerlingName))
        {
            return enerlingCurrentLife[enerlingName];
        }

        // Try to load from PlayerPrefs
        if (PlayerPrefs.HasKey(enerlingName + "_CurrentLife"))
        {
            return PlayerPrefs.GetInt(enerlingName + "_CurrentLife", -1);
        }

        return -1; // Not found
    }

    // Unlock an enerling
    public void UnlockEnerling(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        if (!unlockedEnerlings.Contains(enerlingName))
        {
            unlockedEnerlings.Add(enerlingName);
            PlayerPrefs.SetInt(enerlingName + "_Unlocked", 1);
            PlayerPrefs.Save();

            // Also update the database immediately
            if (ingredientDatabase != null)
            {
                var ingredient = ingredientDatabase.GetIngredientInfo(enerlingName);
                if (ingredient != null)
                {
                    ingredient.isUnlocked = true;
                }
            }

            Debug.Log($"Unlocked enerling: {enerlingName}");
        }
    }

    // Check if enerling is unlocked
    public bool IsEnerlingUnlocked(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return false;
        return unlockedEnerlings.Contains(enerlingName);
    }

    // Get all unlocked enerlings
    public List<string> GetAllUnlockedEnerlings()
    {
        return new List<string>(unlockedEnerlings);
    }

    // Load all data
    private void LoadAllData()
    {
        Debug.Log("Loading all saved data...");

        // Load selected enerling
        selectedEnerlingName = PlayerPrefs.GetString("SelectedEnerling", "");
        Debug.Log($"Loaded selected enerling: {selectedEnerlingName}");

        // Load opponent enerling
        opponentEnerlingName = PlayerPrefs.GetString("OpponentEnerling", "");
        Debug.Log($"Loaded opponent enerling: {opponentEnerlingName}");

        // Load unlocked enerlings
        unlockedEnerlings.Clear();
        if (ingredientDatabase != null)
        {
            foreach (var ingredient in ingredientDatabase.ingredients)
            {
                string key = ingredient.ingredientName + "_Unlocked";
                if (PlayerPrefs.HasKey(key) && PlayerPrefs.GetInt(key) == 1)
                {
                    unlockedEnerlings.Add(ingredient.ingredientName);
                    Debug.Log($"Loaded unlocked enerling: {ingredient.ingredientName}");
                }

                // Load current life
                string lifeKey = ingredient.ingredientName + "_CurrentLife";
                if (PlayerPrefs.HasKey(lifeKey))
                {
                    int life = PlayerPrefs.GetInt(lifeKey, ingredient.baseLife);

                    if (!enerlingCurrentLife.ContainsKey(ingredient.ingredientName))
                    {
                        enerlingCurrentLife.Add(ingredient.ingredientName, life);
                    }
                    else
                    {
                        enerlingCurrentLife[ingredient.ingredientName] = life;
                    }
                }
            }
        }

        Debug.Log($"Loaded {unlockedEnerlings.Count} unlocked enerlings");
    }

    // Reset all progress (for testing)
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteAll();
        selectedEnerlingName = "";
        opponentEnerlingName = "";
        enerlingCurrentLife.Clear();
        unlockedEnerlings.Clear();

        if (ingredientDatabase != null)
        {
            foreach (var ingredient in ingredientDatabase.ingredients)
            {
                ingredient.isUnlocked = false;
                ingredient.currentLife = ingredient.baseLife;
            }
        }

        Debug.Log("All progress reset!");
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}