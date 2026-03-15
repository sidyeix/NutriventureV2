using UnityEngine;
using System.Collections.Generic;
using System;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    [Header("References")]
    public IngredientDatabase ingredientDatabase;

    // Regen durations (0→max) per rarity in minutes
    private const float COMMON_FULL_REGEN_MINUTES = 10f;
    private const float RARE_FULL_REGEN_MINUTES = 15f;
    private const float ULTRA_RARE_FULL_REGEN_MINUTES = 20f;

    // Saved data
    private string selectedEnerlingName = "";
    private string opponentEnerlingName = "";
    private Dictionary<string, int> enerlingCurrentLife = new Dictionary<string, int>();
    private HashSet<string> unlockedEnerlings = new HashSet<string>();

    // Enerlings marked as unlocked by default in the ScriptableObject (e.g. Stevia Extract)
    // Captured once at startup so we can re-apply them after a reset.
    private HashSet<string> defaultUnlockedEnerlings = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load saved data first
            LoadAllData();

            // Process offline health regen before anything reads life values
            ProcessAllEnerlingHealthRegen();

            // Then apply unlocks to database
            ApplyUnlocksToDatabase();

            // Sync catch counts from PlayerPrefs (primary) or GameData (fallback)
            SyncCatchCountsFromGameData();

            // Keep GameData.unlockedEnerlings in sync as backup
            SyncUnlocksToGameData();
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

        // Also sync to the IngredientInfo on the ScriptableObject
        if (ingredientDatabase != null)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(enerlingName);
            if (ingredient != null)
            {
                ingredient.currentLife = currentLife;
            }
        }

        PlayerPrefs.SetInt(enerlingName + "_CurrentLife", currentLife);
        PlayerPrefs.Save();

        // Auto-start regen if below max and not already regenerating
        if (ingredientDatabase != null)
        {
            var info = ingredientDatabase.GetIngredientInfo(enerlingName);
            if (info != null)
            {
                if (currentLife < info.baseLife)
                {
                    // Only begin regen if not already running
                    string regenKey = enerlingName + "_RegenStartTime";
                    if (!PlayerPrefs.HasKey(regenKey))
                    {
                        BeginEnerlingRegen(enerlingName, currentLife);
                    }
                }
                else
                {
                    ClearEnerlingHealthRegen(enerlingName);
                    // Also clear snapshot
                    PlayerPrefs.DeleteKey(enerlingName + "_RegenStartLife");
                    PlayerPrefs.Save();
                }
            }
        }
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

            // Initialize catch count in PlayerPrefs if not already set
            if (!PlayerPrefs.HasKey(enerlingName + "_CatchCount"))
            {
                PlayerPrefs.SetInt(enerlingName + "_CatchCount", 0);
            }

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

            // Sync to GameData as backup
            SyncUnlocksToGameData();

            Debug.Log($"Unlocked enerling: {enerlingName} (Total unlocked: {unlockedEnerlings.Count})");
        }
    }

    /// <summary>
    /// Increment the catch count for a specific enerling in the database, PlayerPrefs, and GameData.
    /// </summary>
    public void IncrementCatchCount(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        int newCount = 0;

        // Update runtime database
        if (ingredientDatabase != null)
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(enerlingName);
            if (ingredient != null)
            {
                if (ingredient.currentCatchCount < ingredient.maxCatch)
                {
                    ingredient.currentCatchCount++;
                    newCount = ingredient.currentCatchCount;
                    Debug.Log($"Catch count for {enerlingName}: {ingredient.currentCatchCount}/{ingredient.maxCatch}");
                }
                else
                {
                    Debug.Log($"{enerlingName} already at max catch ({ingredient.maxCatch})");
                    return;
                }
            }
        }

        // Save to PlayerPrefs (primary persistence)
        PlayerPrefs.SetInt(enerlingName + "_CatchCount", newCount);
        PlayerPrefs.Save();

        // Also save to GameDataManager for backup persistence
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.IncrementEnerlingCatchCount(enerlingName);
        }
    }

    /// <summary>
    /// Get the catch count for a specific enerling from PlayerPrefs.
    /// Returns 0 if not found or if the enerling is not unlocked.
    /// </summary>
    public int GetCatchCount(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return 0;
        if (!IsEnerlingUnlocked(enerlingName)) return 0;
        return PlayerPrefs.GetInt(enerlingName + "_CatchCount", 0);
    }

    /// <summary>
    /// Syncs catch counts from PlayerPrefs (primary) or GameData (fallback) into the runtime IngredientDatabase.
    /// Also ensures locked enerlings always have a catch count of 0.
    /// Call this on startup / scene load.
    /// </summary>
    public void SyncCatchCountsFromGameData()
    {
        if (ingredientDatabase == null) return;

        foreach (var ingredient in ingredientDatabase.ingredients)
        {
            // Locked enerlings always have 0 catch count
            if (!IsEnerlingUnlocked(ingredient.ingredientName))
            {
                ingredient.currentCatchCount = 0;
                continue;
            }

            // Try PlayerPrefs first (primary source of truth)
            string prefsKey = ingredient.ingredientName + "_CatchCount";
            if (PlayerPrefs.HasKey(prefsKey))
            {
                int savedCount = PlayerPrefs.GetInt(prefsKey, 0);
                ingredient.currentCatchCount = Mathf.Min(savedCount, ingredient.maxCatch);
            }
            else if (GameDataManager.Instance != null)
            {
                // Fallback to GameData JSON
                int savedCount = GameDataManager.Instance.GetEnerlingCatchCount(ingredient.ingredientName);
                ingredient.currentCatchCount = Mathf.Min(savedCount, ingredient.maxCatch);

                // Migrate to PlayerPrefs for future loads
                if (savedCount > 0)
                {
                    PlayerPrefs.SetInt(prefsKey, ingredient.currentCatchCount);
                }
            }
            else
            {
                ingredient.currentCatchCount = 0;
            }
        }

        PlayerPrefs.Save();
        Debug.Log("Synced catch counts from PlayerPrefs/GameData to IngredientDatabase");
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

    /// <summary>
    /// Get the total number of unlocked enerlings.
    /// </summary>
    public int GetTotalUnlockedCount()
    {
        return unlockedEnerlings.Count;
    }

    /// <summary>
    /// Get the total number of enerlings in the database.
    /// </summary>
    public int GetTotalEnerlingCount()
    {
        if (ingredientDatabase == null) return 0;
        return ingredientDatabase.ingredients.Count;
    }

    /// <summary>
    /// Syncs the unlock list from PlayerPrefs into GameData.unlockedEnerlings
    /// so the JSON save file stays consistent as a backup.
    /// </summary>
    public void SyncUnlocksToGameData()
    {
        if (GameDataManager.Instance == null) return;

        var gameData = GameDataManager.Instance.CurrentGameData;
        if (gameData == null) return;

        if (gameData.unlockedEnerlings == null)
            gameData.unlockedEnerlings = new List<string>();

        gameData.unlockedEnerlings.Clear();
        gameData.unlockedEnerlings.AddRange(unlockedEnerlings);

        GameDataManager.Instance.SaveGameData();
        Debug.Log($"Synced {unlockedEnerlings.Count} unlocked enerlings to GameData");
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

                    // Sync saved life back to the IngredientInfo on the ScriptableObject
                    ingredient.currentLife = life;
                }
            }
        }

        // --- Default unlock initialization ---
        // Check the ScriptableObject's editor defaults BEFORE ApplyUnlocksToDatabase overwrites them.
        // Any enerling marked isUnlocked = true in the SO is a "starter" enerling (e.g. Stevia Extract).
        // If this is the first time the game runs (no PlayerPrefs key for that enerling), auto-unlock it.
        defaultUnlockedEnerlings.Clear();
        foreach (var ingredient in ingredientDatabase.ingredients)
        {
            if (ingredient.isUnlocked)
            {
                // Remember SO defaults for use in ResetAllProgress
                defaultUnlockedEnerlings.Add(ingredient.ingredientName);

                string key = ingredient.ingredientName + "_Unlocked";
                if (!PlayerPrefs.HasKey(key))
                {
                    // First-time player: no saved state for this enerling, use SO default
                    unlockedEnerlings.Add(ingredient.ingredientName);
                    PlayerPrefs.SetInt(key, 1);

                    // Initialize catch count
                    if (!PlayerPrefs.HasKey(ingredient.ingredientName + "_CatchCount"))
                    {
                        PlayerPrefs.SetInt(ingredient.ingredientName + "_CatchCount", 0);
                    }

                    Debug.Log($"Default unlock from database: {ingredient.ingredientName}");
                }
            }
        }
        PlayerPrefs.Save();

        Debug.Log($"Loaded {unlockedEnerlings.Count} unlocked enerlings (including {defaultUnlockedEnerlings.Count} SO defaults)");
    }

    // ==================== ENERLING HEALTH REGEN ====================

    /// <summary>
    /// Returns full regen duration (0→max) in minutes for the given rarity.
    /// </summary>
    public static float GetFullRegenMinutes(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common: return COMMON_FULL_REGEN_MINUTES;
            case IngredientDatabase.Rarity.Rare: return RARE_FULL_REGEN_MINUTES;
            case IngredientDatabase.Rarity.UltraRare: return ULTRA_RARE_FULL_REGEN_MINUTES;
            default: return COMMON_FULL_REGEN_MINUTES;
        }
    }

    /// <summary>
    /// Start health regen timer for an enerling. Call after battle damage.
    /// The timestamp records when regen started; the life at that moment is already saved.
    /// </summary>
    public void StartEnerlingHealthRegen(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName) || ingredientDatabase == null) return;

        var info = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (info == null) return;

        // Only start regen if not already at full health
        if (info.currentLife >= info.baseLife) return;

        string key = enerlingName + "_RegenStartTime";
        // Don't overwrite an existing regen timestamp — regen is already running
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetString(key, DateTime.Now.ToString("o"));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Clear the regen timer (enerling reached full health).
    /// </summary>
    public void ClearEnerlingHealthRegen(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;
        string key = enerlingName + "_RegenStartTime";
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Check if an enerling is currently regenerating health.
    /// </summary>
    public bool IsEnerlingRegenerating(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName) || ingredientDatabase == null) return false;

        var info = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (info == null) return false;

        // Use the authoritative dictionary value instead of the SO field
        // (SO field may not be synced yet on restart)
        int life = GetEnerlingCurrentLife(enerlingName);
        if (life < 0) life = info.currentLife; // fallback
        if (life >= info.baseLife) return false;

        string key = enerlingName + "_RegenStartTime";
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// Get remaining seconds until this enerling is fully healed.
    /// Returns 0 if not regenerating or already full.
    /// </summary>
    public float GetEnerlingRegenRemainingSeconds(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName) || ingredientDatabase == null) return 0f;

        var info = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (info == null || info.currentLife >= info.baseLife) return 0f;

        string key = enerlingName + "_RegenStartTime";
        if (!PlayerPrefs.HasKey(key)) return 0f;

        string iso = PlayerPrefs.GetString(key, "");
        if (!DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime startTime))
            return 0f;

        // Calculate total regen time based on how much life was missing when regen started
        int lifeAtStart = GetLifeAtRegenStart(enerlingName, info);
        int missingAtStart = info.baseLife - lifeAtStart;
        if (missingAtStart <= 0) return 0f;

        float fullRegenSec = GetFullRegenMinutes(info.rarity) * 60f;
        float totalRegenSec = (fullRegenSec * missingAtStart) / info.baseLife;

        double elapsed = (DateTime.Now - startTime).TotalSeconds;
        float remaining = totalRegenSec - (float)elapsed;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// Process health regen for all enerlings based on elapsed real time.
    /// Call on startup to handle offline regen, and periodically during gameplay.
    /// </summary>
    public void ProcessAllEnerlingHealthRegen()
    {
        if (ingredientDatabase == null) return;

        foreach (var info in ingredientDatabase.ingredients)
        {
            ProcessSingleEnerlingRegen(info);
        }
    }

    private void ProcessSingleEnerlingRegen(IngredientDatabase.IngredientInfo info)
    {
        if (info == null) return;

        string key = info.ingredientName + "_RegenStartTime";

        // Auto-start regen if life < baseLife but no regen timestamp exists
        if (!PlayerPrefs.HasKey(key))
        {
            int life = GetEnerlingCurrentLife(info.ingredientName);
            if (life < 0) life = info.currentLife;
            if (life > 0 && life < info.baseLife)
            {
                Debug.Log($"[Regen] Auto-starting regen for {info.ingredientName}: {life}/{info.baseLife}");
                BeginEnerlingRegen(info.ingredientName, life);
            }
            return;
        }

        string iso = PlayerPrefs.GetString(key, "");
        if (!DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime startTime))
            return;

        // Life when regen started
        int lifeAtStart = GetLifeAtRegenStart(info.ingredientName, info);
        int missingAtStart = info.baseLife - lifeAtStart;
        if (missingAtStart <= 0)
        {
            ClearEnerlingHealthRegen(info.ingredientName);
            return;
        }

        // Full regen (0→max) duration in seconds
        float fullRegenSec = GetFullRegenMinutes(info.rarity) * 60f;
        // Proportional regen time for the missing amount
        float totalRegenSec = (fullRegenSec * missingAtStart) / info.baseLife;
        // HP per second
        float hpPerSecond = (float)missingAtStart / totalRegenSec;

        double elapsed = (DateTime.Now - startTime).TotalSeconds;
        int hpRecovered = Mathf.FloorToInt((float)(elapsed * hpPerSecond));

        int newLife = Mathf.Min(lifeAtStart + hpRecovered, info.baseLife);

        if (newLife != info.currentLife)
        {
            Debug.Log($"[Regen] {info.ingredientName}: {info.currentLife} → {newLife} (start={lifeAtStart}, elapsed={elapsed:F1}s, +{hpRecovered}HP)");
            info.currentLife = newLife;
            SaveEnerlingCurrentLife(info.ingredientName, newLife);
        }

        if (info.currentLife >= info.baseLife)
        {
            Debug.Log($"[Regen] {info.ingredientName}: Fully healed!");
            info.currentLife = info.baseLife;
            SaveEnerlingCurrentLife(info.ingredientName, info.baseLife);
            ClearEnerlingHealthRegen(info.ingredientName);
        }
    }

    /// <summary>
    /// Save the life snapshot when regen starts, so we can calculate HP recovered.
    /// </summary>
    private int GetLifeAtRegenStart(string enerlingName, IngredientDatabase.IngredientInfo info)
    {
        string lifeKey = enerlingName + "_RegenStartLife";
        if (PlayerPrefs.HasKey(lifeKey))
            return PlayerPrefs.GetInt(lifeKey, info.currentLife);
        return info.currentLife;
    }

    /// <summary>
    /// Call this after saving life from battle damage to begin regen.
    /// Saves the snapshot life and starts the regen timer.
    /// </summary>
    public void BeginEnerlingRegen(string enerlingName, int currentLife)
    {
        if (string.IsNullOrEmpty(enerlingName) || ingredientDatabase == null) return;

        var info = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (info == null || currentLife >= info.baseLife) return;

        // Save snapshot of life when regen starts
        PlayerPrefs.SetInt(enerlingName + "_RegenStartLife", currentLife);
        // Save the regen start timestamp
        PlayerPrefs.SetString(enerlingName + "_RegenStartTime", DateTime.Now.ToString("o"));
        PlayerPrefs.Save();

        float regenMinutes = GetFullRegenMinutes(info.rarity);
        int missing = info.baseLife - currentLife;
        float totalSec = (regenMinutes * 60f * missing) / info.baseLife;
        Debug.Log($"[Regen] Started for {enerlingName}: {currentLife}/{info.baseLife}, full heal in {totalSec:F0}s");
    }

    /// <summary>
    /// Pause regen for an enerling entering battle.
    /// Applies any HP recovered so far, saves the updated life, then clears the timer.
    /// Regen will resume automatically via SaveEnerlingCurrentLife when the battle ends.
    /// </summary>
    public void PauseEnerlingRegen(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName) || ingredientDatabase == null) return;

        var info = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (info == null) return;

        string key = enerlingName + "_RegenStartTime";
        if (!PlayerPrefs.HasKey(key)) return;

        // Process any HP recovered up to this moment
        ProcessSingleEnerlingRegen(info);

        // Now clear the regen timer so it stops during battle
        ClearEnerlingHealthRegen(enerlingName);
        PlayerPrefs.DeleteKey(enerlingName + "_RegenStartLife");
        PlayerPrefs.Save();

        Debug.Log($"[Regen] Paused regen for {enerlingName} (entering battle) — life now {info.currentLife}/{info.baseLife}");
    }

    /// <summary>
    /// Ensure every damaged enerling has a regen timer running.
    /// Call at the start of key scenes (BattlePlay, EnerlingSelection, etc.).
    /// </summary>
    public void EnsureAllDamagedEnerlingsRegenerating()
    {
        if (ingredientDatabase == null) return;

        foreach (var info in ingredientDatabase.ingredients)
        {
            if (info == null) continue;
            int life = GetEnerlingCurrentLife(info.ingredientName);
            if (life < 0) life = info.currentLife;
            if (life > 0 && life < info.baseLife)
            {
                string key = info.ingredientName + "_RegenStartTime";
                if (!PlayerPrefs.HasKey(key))
                {
                    Debug.Log($"[Regen] EnsureRegen: starting for {info.ingredientName} ({life}/{info.baseLife})");
                    BeginEnerlingRegen(info.ingredientName, life);
                }
            }
        }
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
                ingredient.currentCatchCount = 0;
                ClearEnerlingHealthRegen(ingredient.ingredientName);
            }
        }

        // Re-apply default unlocks from the ScriptableObject (e.g. Stevia Extract)
        // so the player has their starter enerling(s) immediately after reset.
        foreach (string defaultName in defaultUnlockedEnerlings)
        {
            unlockedEnerlings.Add(defaultName);
            PlayerPrefs.SetInt(defaultName + "_Unlocked", 1);
            PlayerPrefs.SetInt(defaultName + "_CatchCount", 0);

            if (ingredientDatabase != null)
            {
                var ingredient = ingredientDatabase.GetIngredientInfo(defaultName);
                if (ingredient != null)
                {
                    ingredient.isUnlocked = true;
                }
            }

            Debug.Log($"Re-applied default unlock after reset: {defaultName}");
        }
        PlayerPrefs.Save();

        // Sync defaults to GameData backup
        SyncUnlocksToGameData();

        Debug.Log($"All enerling progress reset! ({defaultUnlockedEnerlings.Count} default enerlings re-applied)");
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}