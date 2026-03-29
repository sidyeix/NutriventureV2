using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    // Player Profile
    public string playerName;
    public int playerLevel;
    public float currentXP;
    public float xpToNextLevel;

    // Resources
    public int nutriCoins;
    public int nutriGems;
    public int currentEnergy;
    public DateTime lastEnergyUpdateTime;

    // Key Kingdom Collections
    public bool sugariaKeyCollected = false;
    public bool preserviaKeyCollected = false;
    public bool nutriKingdomKeyCollected = true;
    public bool allerthiaKeyCollected = false;
    public bool ocrScannerKeyCollected = false;

    // Allerthia Game State
    public bool allerthiaScrollGrabbed = false;
    public List<string> collectedAllerthiaProducts = new List<string>();

    // Character System
    public int selectedCharacterID = 0;
    public List<int> unlockedCharacterIDs = new List<int>();

    // SKIN SYSTEM
    [System.Serializable]
    public class SkinSaveData
    {
        public int characterID;
        public int selectedSkinID = -1;
        public List<int> unlockedSkinIDs = new List<int>();
    }

    public List<SkinSaveData> skinData = new List<SkinSaveData>();

    // ENERLING PET SYSTEM
    public string equippedPetSlot1 = "";
    public string equippedPetSlot2 = "";

    // POWER-UP TRACKING SYSTEM
    [System.Serializable]
    public class PowerUpSaveData
    {
        public string petName;              // Name of the pet this power-up belongs to
        public int powerUpIndex;             // Index in the pet's power-ups list
        public IngredientDatabase.PowerUpInfo.PowerUpType powerUpType; // Type of power-up
        public DateTime lastTriggerTime;     // When the power-up was last triggered
        public float cooldownMinutes;         // Cooldown in minutes
        public int amount;                    // Amount to add when triggered
    }

    public List<PowerUpSaveData> activePowerUps = new List<PowerUpSaveData>();

    // HEART AND TIME POWER-UP TRACKING
    [System.Serializable]
    public class PassivePowerUpData
    {
        public string petName;                // Name of the pet
        public IngredientDatabase.PowerUpInfo.PowerUpType powerUpType; // Heart or Time
        public int amount;                     // Amount of hearts to add or time to deduct (in seconds/minutes)
    }

    public List<PassivePowerUpData> passivePowerUps = new List<PassivePowerUpData>();

    // Chest System
    public DateTime lastChestClaimTime;
    public bool isChestAvailable = true;

    // Progress Tracking
    public List<bool> unlockedKingdoms = new List<bool>() { true, false, false, false };
    [System.Serializable]
    public class StringBoolDictionary : SerializableDictionary<string, bool> { }
    public StringBoolDictionary completedMinigames = new StringBoolDictionary();

    [System.Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int> { }
    public StringIntDictionary minigameStars = new StringIntDictionary();

    // Collection System
    public List<string> unlockedEnerlings = new List<string>();
    [System.Serializable]
    public class StringBoolDictionary2 : SerializableDictionary<string, bool> { }
    public StringBoolDictionary2 scannedIngredients = new StringBoolDictionary2();

    // Settings
    public float musicVolume = 1f;
    public float soundVolume = 1f;
    public float lookSensitivity = 0.2f;
    public string language = "English";

    // Profile Icon System
    public string equippedIconId = "icon1";
    public List<string> unlockedIconIds = new List<string>();

    // Frame System
    public string equippedFrameId = "frame_default";
    public List<string> unlockedFrameIds = new List<string>();

    // Achievement System
    public List<string> completedAchievementIds = new List<string>();
    public List<string> claimedAchievementIds = new List<string>();

    // ===== OCR BATTLE LIFE & ENERGY SYSTEM =====
    public int ocrBattleLives = 5;              // Current lives (max 5)
    public int ocrBattleMaxLives = 5;
    public int ocrBattleEnergy = 15;             // Current energy (max 15)
    public int ocrBattleMaxEnergy = 15;
    public string ocrLastLifeLossTime = "";      // ISO 8601 timestamp when the most recent life was lost
    public string ocrLastEnergyUseTime = "";     // ISO 8601 timestamp when the most recent energy was used
    public int ocrLivesRegening = 0;             // How many lives are currently regenerating
    public int ocrEnergyRegening = 0;            // How many energy units are currently regenerating

    // Kingdom Instruction Tracking (first-visit timeline played)
    public bool hasPlayedK1Instruction = false;
    public bool hasPlayedK4CompletionCutscene = false;

    // Enerling catch counts (enerlingName -> catchCount)
    [System.Serializable]
    public class StringIntDictionary3 : SerializableDictionary<string, int> { }
    public StringIntDictionary3 enerlingCatchCounts = new StringIntDictionary3();

    public GameData()
    {
        // Initialize default values
        playerName = "Adventurer";
        playerLevel = 1;
        currentXP = 0;
        xpToNextLevel = 100;
        nutriCoins = 0;
        nutriGems = 0;
        currentEnergy = 10;
        lastEnergyUpdateTime = DateTime.Now;
        lastChestClaimTime = DateTime.MinValue;
        isChestAvailable = true;
        selectedCharacterID = 0;

        // Kingdom Keys
        sugariaKeyCollected = false;
        preserviaKeyCollected = false;
        nutriKingdomKeyCollected = true;
        allerthiaKeyCollected = false;
        ocrScannerKeyCollected = false;

        // Allerthia products
        allerthiaScrollGrabbed = false;
        collectedAllerthiaProducts = new List<string>();

        // Initialize lists properly
        if (unlockedCharacterIDs == null)
            unlockedCharacterIDs = new List<int>() { 0 };
        else
            unlockedCharacterIDs.Clear();
        unlockedCharacterIDs.Add(0);

        // Initialize icon and frame lists
        if (unlockedIconIds == null)
            unlockedIconIds = new List<string>();

        if (unlockedFrameIds == null)
            unlockedFrameIds = new List<string>();

        if (completedAchievementIds == null)
            completedAchievementIds = new List<string>();

        if (claimedAchievementIds == null)
            claimedAchievementIds = new List<string>();

        // Initialize skin data
        if (skinData == null)
            skinData = new List<GameData.SkinSaveData>();

        // Initialize power-up tracking
        if (activePowerUps == null)
            activePowerUps = new List<PowerUpSaveData>();

        // Initialize passive power-ups (Heart & Time)
        if (passivePowerUps == null)
            passivePowerUps = new List<PassivePowerUpData>();

        // Initialize progress tracking
        if (unlockedKingdoms == null)
        {
            unlockedKingdoms = new List<bool>() { true, false, false, false };
        }

        if (completedMinigames == null)
            completedMinigames = new StringBoolDictionary();

        if (minigameStars == null)
            minigameStars = new StringIntDictionary();

        if (unlockedEnerlings == null)
            unlockedEnerlings = new List<string>();

        if (scannedIngredients == null)
            scannedIngredients = new StringBoolDictionary2();

        // Initialize pet slots
        equippedPetSlot1 = "";
        equippedPetSlot2 = "";

        // Initialize OCR battle life & energy
        ocrBattleLives = 5;
        ocrBattleMaxLives = 5;
        ocrBattleEnergy = 15;
        ocrBattleMaxEnergy = 15;
        ocrLastLifeLossTime = "";
        ocrLastEnergyUseTime = "";
        ocrLivesRegening = 0;
        ocrEnergyRegening = 0;

        if (enerlingCatchCounts == null)
            enerlingCatchCounts = new StringIntDictionary3();
    }

    // Helper method to get or create skin data for a character
    private SkinSaveData GetOrCreateSkinData(int characterID)
    {
        // First try to find existing data
        foreach (var data in skinData)
        {
            if (data.characterID == characterID)
                return data;
        }

        // Create new if not found
        SkinSaveData newData = new SkinSaveData { characterID = characterID };
        skinData.Add(newData);
        return newData;
    }

    public int GetSelectedSkinForCharacter(int characterID)
    {
        foreach (var data in skinData)
        {
            if (data.characterID == characterID)
                return data.selectedSkinID;
        }
        return -1;
    }

    public void SetSelectedSkinForCharacter(int characterID, int skinID)
    {
        var data = GetOrCreateSkinData(characterID);
        data.selectedSkinID = skinID;
        Debug.Log($"Set selected skin {skinID} for character {characterID}");
    }

    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (skinID == -1) return true;

        foreach (var data in skinData)
        {
            if (data.characterID == characterID && data.unlockedSkinIDs != null)
            {
                return data.unlockedSkinIDs.Contains(skinID);
            }
        }
        return false;
    }

    public void UnlockSkinForCharacter(int characterID, int skinID)
    {
        if (skinID == -1) return;

        var data = GetOrCreateSkinData(characterID);
        if (data.unlockedSkinIDs == null)
            data.unlockedSkinIDs = new List<int>();

        if (!data.unlockedSkinIDs.Contains(skinID))
        {
            data.unlockedSkinIDs.Add(skinID);
            Debug.Log($"Unlocked skin {skinID} for character {characterID}");
        }
        else
        {
            Debug.Log($"Skin {skinID} for character {characterID} was already unlocked");
        }
    }

    public List<int> GetUnlockedSkinsForCharacter(int characterID)
    {
        foreach (var data in skinData)
        {
            if (data.characterID == characterID && data.unlockedSkinIDs != null)
                return new List<int>(data.unlockedSkinIDs);
        }
        return new List<int>();
    }

    public bool HasUnlockedSkins(int characterID)
    {
        foreach (var data in skinData)
        {
            if (data.characterID == characterID && data.unlockedSkinIDs != null)
                return data.unlockedSkinIDs.Count > 0;
        }
        return false;
    }

    public void InitializeAllCharactersSkins(CharacterDatabase characterDatabase)
    {
        if (characterDatabase == null) return;

        // Ensure each character has an entry
        foreach (var character in characterDatabase.characters)
        {
            GetOrCreateSkinData(character.characterID);
        }

        Debug.Log("Skin data initialized with list approach");
    }

    public void DebugPrintSkinData()
    {
        Debug.Log("=== SKIN DATA DEBUG ===");
        Debug.Log($"Selected Character ID: {selectedCharacterID}");
        Debug.Log($"Total skin data entries: {skinData.Count}");

        foreach (var data in skinData)
        {
            Debug.Log($"Character {data.characterID}: Selected={data.selectedSkinID}, Unlocked={string.Join(", ", data.unlockedSkinIDs ?? new List<int>())}");
        }
        Debug.Log("=== END SKIN DATA ===");
    }

    // POWER-UP TRACKING METHODS

    public void AddPowerUp(string petName, int powerUpIndex, IngredientDatabase.PowerUpInfo.PowerUpType type, float cooldownMinutes, int amount)
    {
        if (activePowerUps == null)
            activePowerUps = new List<PowerUpSaveData>();

        // Check if this power-up already exists
        foreach (var powerUp in activePowerUps)
        {
            if (powerUp.petName == petName && powerUp.powerUpIndex == powerUpIndex)
            {
                // Update existing
                powerUp.lastTriggerTime = DateTime.Now;
                powerUp.cooldownMinutes = cooldownMinutes;
                powerUp.amount = amount;
                return;
            }
        }

        // Create new
        PowerUpSaveData newPowerUp = new PowerUpSaveData
        {
            petName = petName,
            powerUpIndex = powerUpIndex,
            powerUpType = type,
            lastTriggerTime = DateTime.Now,
            cooldownMinutes = cooldownMinutes,
            amount = amount
        };

        activePowerUps.Add(newPowerUp);
        Debug.Log($"Added power-up tracking for {petName} - Type: {type}, Cooldown: {cooldownMinutes}min");
    }

    public void RemovePowerUpsForPet(string petName)
    {
        if (activePowerUps == null) return;

        activePowerUps.RemoveAll(p => p.petName == petName);
        Debug.Log($"Removed all power-up tracking for {petName}");
    }

    public void UpdatePowerUpLastTriggerTime(string petName, int powerUpIndex)
    {
        if (activePowerUps == null) return;

        foreach (var powerUp in activePowerUps)
        {
            if (powerUp.petName == petName && powerUp.powerUpIndex == powerUpIndex)
            {
                powerUp.lastTriggerTime = DateTime.Now;
                break;
            }
        }
    }

    public TimeSpan GetTimeUntilNextPowerUp(string petName, int powerUpIndex)
    {
        if (activePowerUps == null)
            return TimeSpan.Zero;

        foreach (var powerUp in activePowerUps)
        {
            if (powerUp.petName == petName && powerUp.powerUpIndex == powerUpIndex)
            {
                TimeSpan timeSinceLast = DateTime.Now - powerUp.lastTriggerTime;
                TimeSpan cooldown = TimeSpan.FromMinutes(powerUp.cooldownMinutes);
                TimeSpan timeRemaining = cooldown - timeSinceLast;

                return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
            }
        }

        // No record found - power-up is ready
        return TimeSpan.Zero;
    }

    public List<PowerUpSaveData> GetAllActivePowerUps()
    {
        if (activePowerUps == null)
            return new List<PowerUpSaveData>();

        return activePowerUps;
    }

    // PASSIVE POWER-UP METHODS (HEART & TIME)

    public void AddPassivePowerUp(string petName, IngredientDatabase.PowerUpInfo.PowerUpType type, int amount)
    {
        if (passivePowerUps == null)
            passivePowerUps = new List<PassivePowerUpData>();

        // Check if this passive power-up already exists
        foreach (var powerUp in passivePowerUps)
        {
            if (powerUp.petName == petName && powerUp.powerUpType == type)
            {
                // Update existing
                powerUp.amount = amount;
                return;
            }
        }

        // Create new
        PassivePowerUpData newPowerUp = new PassivePowerUpData
        {
            petName = petName,
            powerUpType = type,
            amount = amount
        };

        passivePowerUps.Add(newPowerUp);
        Debug.Log($"Added passive power-up for {petName} - Type: {type}, Amount: {amount}");
    }

    public void RemovePassivePowerUpsForPet(string petName)
    {
        if (passivePowerUps == null) return;

        passivePowerUps.RemoveAll(p => p.petName == petName);
        Debug.Log($"Removed all passive power-ups for {petName}");
    }

    // Get total hearts from all equipped pets
    public int GetTotalHeartBonus()
    {
        if (passivePowerUps == null) return 0;

        int totalHearts = 0;
        foreach (var powerUp in passivePowerUps)
        {
            if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Heart)
            {
                totalHearts += powerUp.amount;
            }
        }
        return totalHearts;
    }

    // Get total time reduction from all equipped pets (in seconds)
    public int GetTotalTimeReductionSeconds()
    {
        if (passivePowerUps == null) return 0;

        int totalSeconds = 0;
        foreach (var powerUp in passivePowerUps)
        {
            if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Time)
            {
                totalSeconds += powerUp.amount;
            }
        }
        return totalSeconds;
    }

    // Get total time reduction formatted as minutes:seconds
    public string GetTotalTimeReductionFormatted()
    {
        int totalSeconds = GetTotalTimeReductionSeconds();

        if (totalSeconds <= 0)
            return "0s";

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (minutes > 0)
            return $"{minutes}m {seconds}s";
        else
            return $"{seconds}s";
    }

    // Clear all power-ups
    public void ClearAllPowerUps()
    {
        if (activePowerUps != null)
            activePowerUps.Clear();

        if (passivePowerUps != null)
            passivePowerUps.Clear();

        Debug.Log("All power-up tracking cleared");
    }

    // Sugaria Key Methods
    public bool HasSugariaKey() => sugariaKeyCollected;
    public void CollectSugariaKey() => sugariaKeyCollected = true;
    public void ResetSugariaKey() => sugariaKeyCollected = false;

    // Preservia Key Methods
    public bool HasPreserviaKey() => preserviaKeyCollected;
    public void CollectPreserviaKey() => preserviaKeyCollected = true;
    public void ResetPreserviaKey() => preserviaKeyCollected = false;

    // Nutri Kingdom Key Methods
    public bool HasNutriKingdomKey() => nutriKingdomKeyCollected;
    public void CollectNutriKingdomKey() => nutriKingdomKeyCollected = true;
    public void ResetNutriKingdomKey() => nutriKingdomKeyCollected = false;

    // Allerthia Key Methods
    public bool HasAllerthiaKey() => allerthiaKeyCollected;
    public void CollectAllerthiaKey() => allerthiaKeyCollected = true;
    public void ResetAllerthiaKey() => allerthiaKeyCollected = false;

    // Allerthia Product Collection Methods
    public bool IsAllerthiaProductCollected(string productID) => collectedAllerthiaProducts.Contains(productID);
    public void CollectAllerthiaProduct(string productID)
    {
        if (!collectedAllerthiaProducts.Contains(productID))
            collectedAllerthiaProducts.Add(productID);
    }
    public int GetCollectedAllerthiaProductCount() => collectedAllerthiaProducts.Count;
    public void ResetAllerthiaProducts() => collectedAllerthiaProducts.Clear();

    // OCR SCANNER KEY METHODS
    public bool HasOCRScannerKey() => ocrScannerKeyCollected;
    public void CollectOCRScannerKey() => ocrScannerKeyCollected = true;
    public void ResetOCRScannerKey() => ocrScannerKeyCollected = false;

    public bool HasKingdomKey(string kingdomName)
    {
        switch (kingdomName.ToLower())
        {
            case "sugaria": return HasSugariaKey();
            case "preservia": return HasPreserviaKey();
            case "nutri":
            case "nutrikingdom": return HasNutriKingdomKey();
            case "allerthia":
            case "allerthiakingdom": return HasAllerthiaKey();
            case "ocr":
            case "ocrscanner":
            case "ocrscannerkey": return HasOCRScannerKey();
            default:
                Debug.LogWarning($"Unknown kingdom name: {kingdomName}");
                return false;
        }
    }

    public void CollectKingdomKey(string kingdomName)
    {
        switch (kingdomName.ToLower())
        {
            case "sugaria": CollectSugariaKey(); break;
            case "preservia": CollectPreserviaKey(); break;
            case "nutri":
            case "nutrikingdom": CollectNutriKingdomKey(); break;
            case "allerthia":
            case "allerthiakingdom": CollectAllerthiaKey(); break;
            case "ocr":
            case "ocrscanner":
            case "ocrscannerkey": CollectOCRScannerKey(); break;
            default: Debug.LogWarning($"Unknown kingdom name: {kingdomName}"); break;
        }
    }

    public void AddNutriGems(int amount) => nutriGems += amount;
    public bool SpendNutriGems(int amount)
    {
        if (nutriGems >= amount)
        {
            nutriGems -= amount;
            return true;
        }
        return false;
    }
    public int GetNutriGems() => nutriGems;

    public void InitializeDefaultIcons(ProfileIconDatabase database)
    {
        if (database == null) return;

        foreach (var icon in database.icons)
        {
            if (icon.unlockedByDefault && !unlockedIconIds.Contains(icon.id))
            {
                unlockedIconIds.Add(icon.id);
            }
        }

        if (string.IsNullOrEmpty(equippedIconId) && unlockedIconIds.Count > 0)
        {
            equippedIconId = unlockedIconIds[0];
        }
    }

    #region Achievement Methods

    public AchievementStatus GetAchievementStatus(string achievementId)
    {
        if (claimedAchievementIds != null && claimedAchievementIds.Contains(achievementId))
            return AchievementStatus.Claimed;

        if (completedAchievementIds != null && completedAchievementIds.Contains(achievementId))
            return AchievementStatus.Completed;

        return AchievementStatus.NotComplete;
    }

    public void CompleteAchievement(string achievementId)
    {
        if (completedAchievementIds == null)
            completedAchievementIds = new List<string>();

        if (!completedAchievementIds.Contains(achievementId) &&
            (claimedAchievementIds == null || !claimedAchievementIds.Contains(achievementId)))
        {
            completedAchievementIds.Add(achievementId);
        }
    }

    public void ClaimAchievement(string achievementId)
    {
        if (claimedAchievementIds == null)
            claimedAchievementIds = new List<string>();

        if (completedAchievementIds != null && completedAchievementIds.Contains(achievementId))
        {
            completedAchievementIds.Remove(achievementId);
        }

        if (!claimedAchievementIds.Contains(achievementId))
        {
            claimedAchievementIds.Add(achievementId);
        }
    }

    public bool IsAchievementCompleted(string achievementId) => completedAchievementIds != null && completedAchievementIds.Contains(achievementId);
    public bool IsAchievementClaimed(string achievementId) => claimedAchievementIds != null && claimedAchievementIds.Contains(achievementId);

    #endregion
}

// Keep your SerializableDictionary for other dictionaries
[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        foreach (var pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys == null || values == null)
        {
            keys = new List<TKey>();
            values = new List<TValue>();
            return;
        }

        int count = Math.Min(keys.Count, values.Count);

        for (int i = 0; i < count; i++)
        {
            try
            {
                if (keys[i] == null) continue;
                this[keys[i]] = values[i];
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding key-value pair: {e.Message}");
            }
        }
    }
}