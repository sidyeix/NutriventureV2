using UnityEngine;
using System.IO;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;
    public CharacterDatabase characterDatabase;

    public GameData CurrentGameData { get; private set; }

    private string saveFilePath;

    void Awake()
    {
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

        InitializeDefaultCharacters();
        InitializeDefaultSkins();
        UpdateEnergyBasedOnTime();
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

        // Initialize all characters with their skin dictionaries
        CurrentGameData.InitializeAllCharactersSkins(characterDatabase);

        // DO NOT auto-unlock skins based on unlockedByDefault anymore
        // Only unlock skins that are already marked as unlocked in GameData

        SaveGameData();
        Debug.Log("Skin system initialized!");
    }

    public void ResetGameData()
    {
        CurrentGameData = new GameData();
        InitializeDefaultSkins();

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
        int energyToAdd = (int)(timeSinceLastUpdate.TotalMinutes / 30);

        if (energyToAdd > 0)
        {
            CurrentGameData.currentEnergy = Mathf.Min(10, CurrentGameData.currentEnergy + energyToAdd);
            CurrentGameData.lastEnergyUpdateTime = DateTime.Now;
            SaveGameData();
        }
    }

    private void UpdateChestAvailability()
    {
        if (CurrentGameData.isChestAvailable) return;

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

        return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
    }

    public void ClaimChestReward()
    {
        if (!CurrentGameData.isChestAvailable) return;

        CurrentGameData.nutriCoins += 50;
        CurrentGameData.isChestAvailable = false;
        CurrentGameData.lastChestClaimTime = DateTime.Now;

        SaveGameData();
        Debug.Log($"Chest claimed! Received 50 coins. Total coins: {CurrentGameData.nutriCoins}");
    }

    private void InitializeDefaultCharacters()
    {
        if (characterDatabase == null)
        {
            Debug.LogWarning("CharacterDatabase not assigned in GameDataManager!");
            return;
        }

        foreach (var character in characterDatabase.characters)
        {
            if (character.unlockedByDefault)
            {
                if (!CurrentGameData.unlockedCharacterIDs.Contains(character.characterID))
                {
                    CurrentGameData.unlockedCharacterIDs.Add(character.characterID);
                    Debug.Log($"Added default character {character.characterID} ({character.characterName}) to unlocked list");
                }
            }
        }

        SaveGameData();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    void OnApplicationQuit()
    {
        SaveGameData();
    }
}