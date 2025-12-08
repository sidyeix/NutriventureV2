using System;
using System.Collections.Generic;

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
    public int currentEnergy;
    public DateTime lastEnergyUpdateTime;

    // Character System
    public int selectedCharacterID = 0; // Default to first character ID
    public List<int> unlockedCharacterIDs = new List<int>() { 0 };

    // SKIN SYSTEM - New additions
    public Dictionary<int, int> selectedSkinForCharacter = new Dictionary<int, int>(); // characterID -> skinID
    public Dictionary<int, List<int>> unlockedSkinsForCharacter = new Dictionary<int, List<int>>(); // characterID -> List<skinID>

    // Chest System
    public DateTime lastChestClaimTime;
    public bool isChestAvailable = true;

    // Progress Tracking
    public List<bool> unlockedKingdoms = new List<bool>() { true, false, false, false }; // First kingdom unlocked
    public Dictionary<string, bool> completedMinigames = new Dictionary<string, bool>();
    public Dictionary<string, int> minigameStars = new Dictionary<string, int>();

    // Collection System
    public List<string> unlockedEnerlings = new List<string>();
    public Dictionary<string, bool> scannedIngredients = new Dictionary<string, bool>();

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
        currentEnergy = 10;
        lastEnergyUpdateTime = DateTime.Now;
        lastChestClaimTime = DateTime.MinValue; // Never claimed
        isChestAvailable = true;
        selectedCharacterID = 0;
        unlockedCharacterIDs = new List<int>() { 0 };

        // Initialize skin dictionaries
        selectedSkinForCharacter = new Dictionary<int, int>();
        unlockedSkinsForCharacter = new Dictionary<int, List<int>>();
    }

    // SKIN SYSTEM METHODS

    // Get the selected skin for a character
    public int GetSelectedSkinForCharacter(int characterID)
    {
        if (selectedSkinForCharacter.ContainsKey(characterID))
        {
            return selectedSkinForCharacter[characterID];
        }
        return -1; // -1 means default/character's original skin
    }

    // Set the selected skin for a character
    public void SetSelectedSkinForCharacter(int characterID, int skinID)
    {
        if (selectedSkinForCharacter.ContainsKey(characterID))
        {
            selectedSkinForCharacter[characterID] = skinID;
        }
        else
        {
            selectedSkinForCharacter.Add(characterID, skinID);
        }
    }

    // Check if a skin is unlocked for a character
    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        // Default skin (character's original) is always unlocked
        if (skinID == -1) return true;

        // Check if character has unlocked skins list
        if (unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            return unlockedSkinsForCharacter[characterID].Contains(skinID);
        }

        // Check if skin is unlocked by default in the database
        // You'll need to check CharacterDatabase for this
        return false;
    }

    // Unlock a skin for a character
    public void UnlockSkinForCharacter(int characterID, int skinID)
    {
        if (!unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            unlockedSkinsForCharacter[characterID] = new List<int>();
        }

        if (!unlockedSkinsForCharacter[characterID].Contains(skinID))
        {
            unlockedSkinsForCharacter[characterID].Add(skinID);
        }
    }

    // Get all unlocked skins for a character
    public List<int> GetUnlockedSkinsForCharacter(int characterID)
    {
        if (unlockedSkinsForCharacter.ContainsKey(characterID))
        {
            return unlockedSkinsForCharacter[characterID];
        }
        return new List<int>();
    }

    // Check if character has any unlocked skins
    public bool HasUnlockedSkins(int characterID)
    {
        return unlockedSkinsForCharacter.ContainsKey(characterID) &&
               unlockedSkinsForCharacter[characterID].Count > 0;
    }
}