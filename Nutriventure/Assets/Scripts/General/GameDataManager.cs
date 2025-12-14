using UnityEngine;
using System.IO;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;
    public CharacterDatabase characterDatabase; // Add this reference

    public GameData CurrentGameData { get; private set; }

    private string saveFilePath;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeData()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "nutriventure_save.json");
        LoadGameData();
    }

    public void SaveGameData()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(CurrentGameData, true);
            File.WriteAllText(saveFilePath, jsonData);
            Debug.Log("Game data saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }

    public void LoadGameData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                CurrentGameData = JsonUtility.FromJson<GameData>(jsonData);
                Debug.Log("Game data loaded successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError("Load failed: " + e.Message);
                CreateNewGameData();
            }
        }
        else
        {
            CreateNewGameData();
        }

        // Initialize default skins for new saves
        InitializeDefaultSkins();

        // Update energy based on time passed
        UpdateEnergyBasedOnTime();

        // Update chest availability based on time passed
        UpdateChestAvailability();
    }

    private void CreateNewGameData()
    {
        CurrentGameData = new GameData();
        SaveGameData();
    }

    private void InitializeDefaultSkins()
    {
        if (characterDatabase == null)
        {
            Debug.LogWarning("CharacterDatabase not assigned in GameDataManager!");
            return;
        }

        // If CharacterDatabase has a public list/array of characters
        // You might need to check the actual field name in your CharacterDatabase class

        // Option A: If it's a List<CharacterData>
        if (characterDatabase.characters != null)
        {
            foreach (var character in characterDatabase.characters)
            {
                if (character.skins != null)
                {
                    foreach (var skin in character.skins)
                    {
                        if (skin.unlockedByDefault)
                        {
                            CurrentGameData.UnlockSkinForCharacter(character.characterID, skin.skinID);
                        }
                    }
                }
            }
        }
        // Option B: If characters are stored differently
        else
        {
            // Try to get characters by ID (assuming IDs start from 0)
            for (int i = 0; i < 10; i++) // Adjust the limit as needed
            {
                var character = characterDatabase.GetCharacterByID(i);
                if (character != null && character.skins != null)
                {
                    foreach (var skin in character.skins)
                    {
                        if (skin.unlockedByDefault)
                        {
                            CurrentGameData.UnlockSkinForCharacter(character.characterID, skin.skinID);
                        }
                    }
                }
            }
        }

        SaveGameData();
        Debug.Log("Default skins initialized!");
    }

    public void ResetGameData()
    {
        // Create completely fresh game data
        CurrentGameData = new GameData();
        InitializeDefaultSkins(); // Re-initialize default skins

        // Update AudioHandler with new volume settings
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }

        Debug.Log("Game data reset to default!");
    }

    private void UpdateEnergyBasedOnTime()
    {
        TimeSpan timeSinceLastUpdate = DateTime.Now - CurrentGameData.lastEnergyUpdateTime;
        int energyToAdd = (int)(timeSinceLastUpdate.TotalMinutes / 30); // 1 energy every 30 minutes

        if (energyToAdd > 0)
        {
            CurrentGameData.currentEnergy = Mathf.Min(10, CurrentGameData.currentEnergy + energyToAdd);
            CurrentGameData.lastEnergyUpdateTime = DateTime.Now;
            SaveGameData();
        }
    }

    private void UpdateChestAvailability()
    {
        // If chest was already available, no need to check
        if (CurrentGameData.isChestAvailable) return;

        // Check if 3 hours have passed since last claim
        TimeSpan timeSinceLastClaim = DateTime.Now - CurrentGameData.lastChestClaimTime;
        if (timeSinceLastClaim.TotalHours >= 3)
        {
            CurrentGameData.isChestAvailable = true;
            SaveGameData();
            Debug.Log("Chest is now available!");
        }
    }

    public bool CanClaimChest()
    {
        return CurrentGameData.isChestAvailable;
    }

    public TimeSpan GetTimeUntilNextChest()
    {
        if (CurrentGameData.isChestAvailable)
        {
            return TimeSpan.Zero;
        }

        TimeSpan timeSinceLastClaim = DateTime.Now - CurrentGameData.lastChestClaimTime;
        TimeSpan timeRemaining = TimeSpan.FromHours(3) - timeSinceLastClaim;

        // Ensure we don't return negative time
        return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
    }

    public void ClaimChestReward()
    {
        if (!CurrentGameData.isChestAvailable) return;

        // Add 50 coins
        CurrentGameData.nutriCoins += 50;

        // Update chest state
        CurrentGameData.isChestAvailable = false;
        CurrentGameData.lastChestClaimTime = DateTime.Now;

        SaveGameData();

        Debug.Log($"Chest claimed! Received 50 coins. Total coins: {CurrentGameData.nutriCoins}");
    }

    // Auto-save when app closes or pauses
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    void OnApplicationQuit()
    {
        SaveGameData();
    }
}