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
    public bool nutriKingdomKeyCollected = true; // Changed to true for default unlock
    public bool allerthiaKeyCollected = false;
    public bool ocrScannerKeyCollected = false; // ADD THIS LINE - OCR Scanner Key

    // Character System
    public int selectedCharacterID = 0;
    public List<int> unlockedCharacterIDs = new List<int>();

    // SKIN SYSTEM
    [System.Serializable]
    public class SkinDictionary : SerializableDictionary<int, int> { }

    [System.Serializable]
    public class UnlockedSkinsDictionary : SerializableDictionary<int, List<int>> { }

    public SkinDictionary selectedSkinForCharacter = new SkinDictionary();
    public UnlockedSkinsDictionary unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

    // Chest System
    public DateTime lastChestClaimTime;
    public bool isChestAvailable = true;

    // Progress Tracking
    public List<bool> unlockedKingdoms = new List<bool>() { true, false, false, false }; // Nutri, Sugaria, Preservia, Allerthia
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
        nutriKingdomKeyCollected = true; // CHANGED TO true (unlocked by default)
        allerthiaKeyCollected = false;
        ocrScannerKeyCollected = false; // ADD THIS LINE

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

        // Initialize skin dictionaries
        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        // Initialize default skin for character 0
        InitializeDefaultSkinForCharacter(0);

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
    }

    // Sugaria Key Methods
    public bool HasSugariaKey()
    {
        return sugariaKeyCollected;
    }

    public void CollectSugariaKey()
    {
        sugariaKeyCollected = true;
    }

    public void ResetSugariaKey()
    {
        sugariaKeyCollected = false;
    }

    // Preservia Key Methods
    public bool HasPreserviaKey()
    {
        return preserviaKeyCollected;
    }

    public void CollectPreserviaKey()
    {
        preserviaKeyCollected = true;
    }

    public void ResetPreserviaKey()
    {
        preserviaKeyCollected = false;
    }

    // Nutri Kingdom Key Methods
    public bool HasNutriKingdomKey()
    {
        return nutriKingdomKeyCollected;
    }

    public void CollectNutriKingdomKey()
    {
        nutriKingdomKeyCollected = true;
    }

    public void ResetNutriKingdomKey()
    {
        nutriKingdomKeyCollected = false;
    }

    // Allerthia Key Methods
    public bool HasAllerthiaKey()
    {
        return allerthiaKeyCollected;
    }

    public void CollectAllerthiaKey()
    {
        allerthiaKeyCollected = true;
    }

    public void ResetAllerthiaKey()
    {
        allerthiaKeyCollected = false;
    }

    // OCR SCANNER KEY METHODS - ADD THESE
    public bool HasOCRScannerKey()
    {
        return ocrScannerKeyCollected;
    }

    public void CollectOCRScannerKey()
    {
        ocrScannerKeyCollected = true;
    }

    public void ResetOCRScannerKey()
    {
        ocrScannerKeyCollected = false;
    }

    public bool HasKingdomKey(string kingdomName)
    {
        switch (kingdomName.ToLower())
        {
            case "sugaria":
                return HasSugariaKey();
            case "preservia":
                return HasPreserviaKey();
            case "nutri":
            case "nutrikingdom":
                return HasNutriKingdomKey();
            case "allerthia":
            case "allerthiakingdom":
                return HasAllerthiaKey();
            case "ocr":
            case "ocrscanner":
            case "ocrscannerkey":
                return HasOCRScannerKey();
            default:
                Debug.LogWarning($"Unknown kingdom name: {kingdomName}");
                return false;
        }
    }

    public void CollectKingdomKey(string kingdomName)
    {
        switch (kingdomName.ToLower())
        {
            case "sugaria":
                CollectSugariaKey();
                break;
            case "preservia":
                CollectPreserviaKey();
                break;
            case "nutri":
            case "nutrikingdom":
                CollectNutriKingdomKey();
                break;
            case "allerthia":
            case "allerthiakingdom":
                CollectAllerthiaKey();
                break;
            case "ocr":
            case "ocrscanner":
            case "ocrscannerkey":
                CollectOCRScannerKey();
                break;
            default:
                Debug.LogWarning($"Unknown kingdom name: {kingdomName}");
                break;
        }
    }

    public void AddNutriGems(int amount)
    {
        nutriGems += amount;
    }

    public bool SpendNutriGems(int amount)
    {
        if (nutriGems >= amount)
        {
            nutriGems -= amount;
            return true;
        }
        return false;
    }

    public int GetNutriGems()
    {
        return nutriGems;
    }

    // Initialize default skin for a character
    private void InitializeDefaultSkinForCharacter(int characterID)
    {
        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (!selectedSkinForCharacter.ContainsKey(characterID))
        {
            selectedSkinForCharacter[characterID] = -1;
        }

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        if (!unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            unlockedSkinsForCharacter[characterID] = new List<int>();
        }
    }

    public int GetSelectedSkinForCharacter(int characterID)
    {
        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (selectedSkinForCharacter.ContainsKey(characterID))
        {
            return selectedSkinForCharacter[characterID];
        }

        InitializeDefaultSkinForCharacter(characterID);
        return selectedSkinForCharacter[characterID];
    }

    public void SetSelectedSkinForCharacter(int characterID, int skinID)
    {
        if (skinID < -1) return;

        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (!selectedSkinForCharacter.ContainsKey(characterID))
        {
            selectedSkinForCharacter[characterID] = skinID;
        }
        else
        {
            selectedSkinForCharacter[characterID] = skinID;
        }

        if (skinID != -1)
        {
            UnlockSkinForCharacter(characterID, skinID);
        }

        Debug.Log($"Set skin {skinID} for character {characterID}");
    }

    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (skinID == -1) return true;

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        if (unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            var skinsList = unlockedSkinsForCharacter[characterID];
            if (skinsList != null)
            {
                return skinsList.Contains(skinID);
            }
        }

        return false;
    }

    public void UnlockSkinForCharacter(int characterID, int skinID)
    {
        if (skinID == -1) return;

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        if (!unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            unlockedSkinsForCharacter[characterID] = new List<int>();
        }

        var skinsList = unlockedSkinsForCharacter[characterID];
        if (skinsList == null)
        {
            skinsList = new List<int>();
            unlockedSkinsForCharacter[characterID] = skinsList;
        }

        if (!skinsList.Contains(skinID))
        {
            skinsList.Add(skinID);
            Debug.Log($"Unlocked skin {skinID} for character {characterID}");
        }
    }

    public List<int> GetUnlockedSkinsForCharacter(int characterID)
    {
        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        if (unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            var skinsList = unlockedSkinsForCharacter[characterID];
            if (skinsList != null)
            {
                return skinsList;
            }
        }

        return new List<int>();
    }

    public bool HasUnlockedSkins(int characterID)
    {
        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        return unlockedSkinsForCharacter.ContainsKey(characterID) &&
               unlockedSkinsForCharacter[characterID] != null &&
               unlockedSkinsForCharacter[characterID].Count > 0;
    }

    public void InitializeAllCharactersSkins(CharacterDatabase characterDatabase)
    {
        if (characterDatabase == null) return;

        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        foreach (var character in characterDatabase.characters)
        {
            int characterID = character.characterID;

            if (!selectedSkinForCharacter.ContainsKey(characterID))
            {
                selectedSkinForCharacter[characterID] = -1;
            }

            if (!unlockedSkinsForCharacter.ContainsKey(characterID))
            {
                unlockedSkinsForCharacter[characterID] = new List<int>();
            }
        }
    }

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

    public bool IsAchievementCompleted(string achievementId)
    {
        return completedAchievementIds != null && completedAchievementIds.Contains(achievementId);
    }

    public bool IsAchievementClaimed(string achievementId)
    {
        return claimedAchievementIds != null && claimedAchievementIds.Contains(achievementId);
    }

    #endregion
}

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

        var items = new KeyValuePair<TKey, TValue>[this.Count];
        CopyTo(items, 0);

        foreach (var pair in items)
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
            Debug.LogWarning("SerializableDictionary: Keys or values list is null. Creating empty dictionary.");
            keys = new List<TKey>();
            values = new List<TValue>();
            return;
        }

        int count = Math.Min(keys.Count, values.Count);

        if (keys.Count != values.Count)
        {
            Debug.LogWarning($"SerializableDictionary: Key count ({keys.Count}) doesn't match value count ({values.Count}). Using minimum count ({count}).");
        }

        for (int i = 0; i < count; i++)
        {
            try
            {
                if (keys[i] == null) continue;
                this[keys[i]] = values[i];
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding key-value pair at index {i}: {e.Message}");
            }
        }
    }

    private void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        int i = arrayIndex;
        foreach (var pair in this)
        {
            if (i >= array.Length) break;
            array[i] = pair;
            i++;
        }
    }
}