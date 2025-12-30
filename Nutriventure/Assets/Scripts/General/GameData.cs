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

    // Resources - ADDED NUTRIGEMS
    public int nutriCoins;
    public int nutriGems; // NEW: Added NutriGems
    public int currentEnergy;
    public DateTime lastEnergyUpdateTime;
    
    // Key Kingdom Collections
    public bool sugariaKeyCollected = false;

    // Character System
    public int selectedCharacterID = 0;
    public List<int> unlockedCharacterIDs = new List<int>();

    // SKIN SYSTEM - Using SerializableDictionary for proper Unity serialization
    [System.Serializable]
    public class SkinDictionary : SerializableDictionary<int, int> { }

    [System.Serializable]
    public class UnlockedSkinsDictionary : SerializableDictionary<int, List<int>> { }

    public SkinDictionary selectedSkinForCharacter = new SkinDictionary(); // characterID -> skinID
    public UnlockedSkinsDictionary unlockedSkinsForCharacter = new UnlockedSkinsDictionary(); // characterID -> List<skinID>

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
    public string language = "English";

    public GameData()
    {
        // Initialize default values
        playerName = "Adventurer";
        playerLevel = 1;
        currentXP = 0;
        xpToNextLevel = 100;
        nutriCoins = 0;
        nutriGems = 0; // NEW: Initialize NutriGems
        currentEnergy = 10;
        lastEnergyUpdateTime = DateTime.Now;
        lastChestClaimTime = DateTime.MinValue;
        isChestAvailable = true;
        selectedCharacterID = 0;

        // Kingdom Keys
        sugariaKeyCollected = false;

        // Initialize lists properly
        if (unlockedCharacterIDs == null)
            unlockedCharacterIDs = new List<int>() { 0 };
        else
            unlockedCharacterIDs.Clear();
        unlockedCharacterIDs.Add(0);

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

    // NEW: Add NutriGems methods
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
            selectedSkinForCharacter[characterID] = -1; // -1 means default skin
        }

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        if (!unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            unlockedSkinsForCharacter[characterID] = new List<int>();
            // Default skin (-1) is always considered unlocked
        }
    }

    // Get the selected skin for a character
    public int GetSelectedSkinForCharacter(int characterID)
    {
        if (selectedSkinForCharacter == null)
            selectedSkinForCharacter = new SkinDictionary();

        if (selectedSkinForCharacter.ContainsKey(characterID))
        {
            return selectedSkinForCharacter[characterID];
        }

        // Initialize if not exists
        InitializeDefaultSkinForCharacter(characterID);
        return selectedSkinForCharacter[characterID];
    }

    // Set the selected skin for a character
    public void SetSelectedSkinForCharacter(int characterID, int skinID)
    {
        if (skinID < -1) return; // Invalid skin ID

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

        // Ensure the skin is marked as unlocked
        if (skinID != -1)
        {
            UnlockSkinForCharacter(characterID, skinID);
        }

        Debug.Log($"Set skin {skinID} for character {characterID}");
    }

    // Check if a skin is unlocked for a character
    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        // Default skin (character's original) is always unlocked
        if (skinID == -1) return true;

        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        // Check if character has unlocked skins list
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

    // Unlock a skin for a character
    public void UnlockSkinForCharacter(int characterID, int skinID)
    {
        if (skinID == -1) return; // Don't add default skin to unlocked list

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

    // Get all unlocked skins for a character
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

    // Check if character has any unlocked skins
    public bool HasUnlockedSkins(int characterID)
    {
        if (unlockedSkinsForCharacter == null)
            unlockedSkinsForCharacter = new UnlockedSkinsDictionary();

        return unlockedSkinsForCharacter.ContainsKey(characterID) &&
               unlockedSkinsForCharacter[characterID] != null &&
               unlockedSkinsForCharacter[characterID].Count > 0;
    }

    // Initialize all characters with their default skins
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

            // Initialize selected skin
            if (!selectedSkinForCharacter.ContainsKey(characterID))
            {
                selectedSkinForCharacter[characterID] = -1; // Default skin
            }

            // Initialize unlocked skins list
            if (!unlockedSkinsForCharacter.ContainsKey(characterID))
            {
                unlockedSkinsForCharacter[characterID] = new List<int>();
            }
        }
    }
}

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    // Save the dictionary to lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        // IMPORTANT: Use ToArray() to avoid modification issues
        var items = new KeyValuePair<TKey, TValue>[this.Count];
        CopyTo(items, 0);

        foreach (var pair in items)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // Load the dictionary from lists
    public void OnAfterDeserialize()
    {
        this.Clear();

        // FIX: Handle null lists gracefully
        if (keys == null || values == null)
        {
            Debug.LogWarning("SerializableDictionary: Keys or values list is null. Creating empty dictionary.");
            keys = new List<TKey>();
            values = new List<TValue>();
            return;
        }

        // FIX: Handle mismatch by using minimum count
        int count = Math.Min(keys.Count, values.Count);

        if (keys.Count != values.Count)
        {
            Debug.LogWarning($"SerializableDictionary: Key count ({keys.Count}) doesn't match value count ({values.Count}). Using minimum count ({count}).");
        }

        for (int i = 0; i < count; i++)
        {
            try
            {
                // Skip if key is null
                if (keys[i] == null) continue;

                // Add to dictionary
                this[keys[i]] = values[i];
            }
            catch (Exception e)
            {
                Debug.LogError($"Error adding key-value pair at index {i}: {e.Message}");
            }
        }
    }

    // Helper method to safely copy dictionary items
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