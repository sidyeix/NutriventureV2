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
    }
}