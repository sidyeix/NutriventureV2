using UnityEngine;
using System.IO;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;
    public CharacterDatabase characterDatabase; // Keep only character database for skin system

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

                Debug.Log($"=== GAME DATA LOADED ===");
                Debug.Log($"Selected Character ID from save file: {CurrentGameData.selectedCharacterID}");
                Debug.Log($"Equipped Icon ID: {CurrentGameData.equippedIconId}");
                Debug.Log($"Unlocked Icons count: {CurrentGameData.unlockedIconIds?.Count ?? 0}");
                Debug.Log($"Equipped Frame ID: {CurrentGameData.equippedFrameId}");
                Debug.Log($"Unlocked Frames count: {CurrentGameData.unlockedFrameIds?.Count ?? 0}");
                Debug.Log($"Completed Achievements count: {CurrentGameData.completedAchievementIds?.Count ?? 0}");
                Debug.Log($"Claimed Achievements count: {CurrentGameData.claimedAchievementIds?.Count ?? 0}");
                Debug.Log($"=== END LOAD ===");
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

        // Initialize all systems
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

    public void ResetGameData()
    {
        CurrentGameData = new GameData();

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }

        Debug.Log("Game data reset to default!");
    }

    // Call this from ProfileSettings to initialize default icons
    public void InitializeDefaultIcons(string[] defaultIconIds)
    {
        if (CurrentGameData == null) return;

        foreach (string iconId in defaultIconIds)
        {
            if (!CurrentGameData.unlockedIconIds.Contains(iconId))
            {
                CurrentGameData.unlockedIconIds.Add(iconId);
                Debug.Log($"Added default icon: {iconId}");
            }
        }

        // Set default equipped icon if none
        if (string.IsNullOrEmpty(CurrentGameData.equippedIconId) && CurrentGameData.unlockedIconIds.Count > 0)
        {
            CurrentGameData.equippedIconId = CurrentGameData.unlockedIconIds[0];
            Debug.Log($"Set default equipped icon to: {CurrentGameData.equippedIconId}");
        }

        SaveGameData();
    }

    // Call this from ProfileSettings to initialize default frames
    public void InitializeDefaultFrames(string[] defaultFrameIds)
    {
        if (CurrentGameData == null) return;

        foreach (string frameId in defaultFrameIds)
        {
            if (!CurrentGameData.unlockedFrameIds.Contains(frameId))
            {
                CurrentGameData.unlockedFrameIds.Add(frameId);
                Debug.Log($"Added default frame: {frameId}");
            }
        }

        // Set default equipped frame if none
        if (string.IsNullOrEmpty(CurrentGameData.equippedFrameId) && CurrentGameData.unlockedFrameIds.Count > 0)
        {
            CurrentGameData.equippedFrameId = CurrentGameData.unlockedFrameIds[0];
            Debug.Log($"Set default equipped frame to: {CurrentGameData.equippedFrameId}");
        }

        SaveGameData();
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

        Debug.Log($"Before adding defaults - Selected Character: {CurrentGameData.selectedCharacterID}");

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

        Debug.Log($"After adding defaults - Selected Character: {CurrentGameData.selectedCharacterID}");

        SaveGameData();
    }

    private void InitializeDefaultSkins()
    {
        if (characterDatabase == null)
        {
            Debug.LogWarning("CharacterDatabase not assigned in GameDataManager!");
            return;
        }

        CurrentGameData.InitializeAllCharactersSkins(characterDatabase);
        SaveGameData();
        Debug.Log("Skin system initialized!");
    }

    #region Profile Icon Methods

    public void UnlockIcon(string iconId)
    {
        if (CurrentGameData == null) return;

        if (!CurrentGameData.unlockedIconIds.Contains(iconId))
        {
            CurrentGameData.unlockedIconIds.Add(iconId);
            SaveGameData();
            Debug.Log($"Icon {iconId} unlocked!");
        }
    }

    public bool IsIconUnlocked(string iconId)
    {
        if (CurrentGameData == null) return false;
        return CurrentGameData.unlockedIconIds != null && CurrentGameData.unlockedIconIds.Contains(iconId);
    }

    public void EquipIcon(string iconId)
    {
        if (CurrentGameData == null) return;

        if (IsIconUnlocked(iconId))
        {
            CurrentGameData.equippedIconId = iconId;
            SaveGameData();
            Debug.Log($"Icon {iconId} equipped!");
        }
    }

    #endregion

    #region Frame Methods

    public void UnlockFrame(string frameId)
    {
        if (CurrentGameData == null) return;

        if (!CurrentGameData.unlockedFrameIds.Contains(frameId))
        {
            CurrentGameData.unlockedFrameIds.Add(frameId);
            SaveGameData();
            Debug.Log($"Frame {frameId} unlocked!");
        }
    }

    public bool IsFrameUnlocked(string frameId)
    {
        if (CurrentGameData == null) return false;
        return CurrentGameData.unlockedFrameIds != null && CurrentGameData.unlockedFrameIds.Contains(frameId);
    }

    public void EquipFrame(string frameId)
    {
        if (CurrentGameData == null) return;

        if (IsFrameUnlocked(frameId))
        {
            CurrentGameData.equippedFrameId = frameId;
            SaveGameData();
            Debug.Log($"Frame {frameId} equipped!");
        }
    }

    #endregion

    #region Achievement Methods

    public void CompleteAchievement(string achievementId)
    {
        if (CurrentGameData == null) return;

        CurrentGameData.CompleteAchievement(achievementId);
        SaveGameData();
        Debug.Log($"Achievement {achievementId} completed!");
    }

    public void ClaimAchievement(string achievementId)
    {
        if (CurrentGameData == null) return;

        if (CurrentGameData.IsAchievementCompleted(achievementId))
        {
            // We need the prize gems - this should come from the achievement database in ProfileSettings
            // For now, we'll use a default value or pass it as a parameter
            int prizeGems = 10; // This should be passed from ProfileSettings
            CurrentGameData.AddNutriGems(prizeGems);
            CurrentGameData.ClaimAchievement(achievementId);

            SaveGameData();
            Debug.Log($"Achievement {achievementId} claimed! +{prizeGems} gems");
        }
    }

    // Overloaded method that accepts prize gems
    public void ClaimAchievement(string achievementId, int prizeGems)
    {
        if (CurrentGameData == null) return;

        if (CurrentGameData.IsAchievementCompleted(achievementId))
        {
            CurrentGameData.AddNutriGems(prizeGems);
            CurrentGameData.ClaimAchievement(achievementId);

            SaveGameData();
            Debug.Log($"Achievement {achievementId} claimed! +{prizeGems} gems");
        }
    }

    public AchievementStatus GetAchievementStatus(string achievementId)
    {
        if (CurrentGameData == null)
            return AchievementStatus.NotComplete;

        return CurrentGameData.GetAchievementStatus(achievementId);
    }

    public bool IsAchievementCompleted(string achievementId)
    {
        if (CurrentGameData == null) return false;
        return CurrentGameData.IsAchievementCompleted(achievementId);
    }

    public bool IsAchievementClaimed(string achievementId)
    {
        if (CurrentGameData == null) return false;
        return CurrentGameData.IsAchievementClaimed(achievementId);
    }

    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    void OnApplicationQuit()
    {
        SaveGameData();
    }
}