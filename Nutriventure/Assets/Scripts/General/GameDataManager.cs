using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    [Header("Databases")]
    public CharacterDatabase characterDatabase;
    public ProfileIconDatabase iconDatabase;
    public FrameDatabase frameDatabase;
    public AchievementDatabase achievementDatabase;

    public GameData CurrentGameData { get; private set; }

    private string saveFilePath;
    private bool hasInitializedDefaults = false;

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
                Debug.Log($"Selected Character ID: {CurrentGameData.selectedCharacterID}");
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

        // Initialize default icons and frames from databases
        InitializeDefaultIconsAndFrames();

        // Initialize all systems
        InitializeDefaultCharacters();
        InitializeDefaultSkins();
        UpdateEnergyBasedOnTime();
        UpdateChestAvailability();
    }

    /// <summary>
    /// Initializes default icons and frames based on the databases
    /// This runs whenever game data is loaded or created
    /// </summary>
    private void InitializeDefaultIconsAndFrames()
    {
        if (CurrentGameData == null) return;

        // Prevent multiple initializations if already done
        if (hasInitializedDefaults && CurrentGameData.unlockedIconIds?.Count > 0 && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            Debug.Log("Default icons and frames already initialized, skipping...");
            return;
        }

        Debug.Log("=== INITIALIZING DEFAULT ICONS AND FRAMES FROM DATABASES ===");

        bool changesMade = false;

        // ===== INITIALIZE ICONS =====
        if (iconDatabase != null)
        {
            Debug.Log($"Icon database found with {iconDatabase.icons.Count} icons");

            // Ensure the list exists
            if (CurrentGameData.unlockedIconIds == null)
            {
                CurrentGameData.unlockedIconIds = new List<string>();
            }

            // Loop through all icons in the database
            foreach (var icon in iconDatabase.icons)
            {
                // Check if this icon should be unlocked by default
                if (icon.unlockedByDefault)
                {
                    // If not already in unlocked list, add it
                    if (!CurrentGameData.unlockedIconIds.Contains(icon.id))
                    {
                        CurrentGameData.unlockedIconIds.Add(icon.id);
                        Debug.Log($"? Added default icon: {icon.id} - {icon.iconName}");
                        changesMade = true;
                    }
                }
                else
                {
                    Debug.Log($"?? Icon not unlocked by default: {icon.id} - {icon.iconName}");
                }
            }

            // Log all unlocked icons after initialization
            Debug.Log($"Unlocked icons after initialization: {string.Join(", ", CurrentGameData.unlockedIconIds)}");
        }
        else
        {
            Debug.LogError("? iconDatabase is not assigned! Default icons will not be initialized.");
        }

        // ===== INITIALIZE FRAMES =====
        if (frameDatabase != null)
        {
            Debug.Log($"Frame database found with {frameDatabase.frames.Count} frames");

            // Ensure the list exists
            if (CurrentGameData.unlockedFrameIds == null)
            {
                CurrentGameData.unlockedFrameIds = new List<string>();
            }

            // Loop through all frames in the database
            foreach (var frame in frameDatabase.frames)
            {
                // Check if this frame should be unlocked by default
                if (frame.unlockedByDefault)
                {
                    // If not already in unlocked list, add it
                    if (!CurrentGameData.unlockedFrameIds.Contains(frame.id))
                    {
                        CurrentGameData.unlockedFrameIds.Add(frame.id);
                        Debug.Log($"? Added default frame: {frame.id} - {frame.frameName}");
                        changesMade = true;
                    }
                }
                else
                {
                    Debug.Log($"?? Frame not unlocked by default: {frame.id} - {frame.frameName}");
                }
            }

            // Log all unlocked frames after initialization
            Debug.Log($"Unlocked frames after initialization: {string.Join(", ", CurrentGameData.unlockedFrameIds)}");
        }
        else
        {
            Debug.LogError("? frameDatabase is not assigned! Default frames will not be initialized.");
        }

        // ===== SET DEFAULT EQUIPPED ITEMS =====

        // Set default equipped icon if none is set
        if (string.IsNullOrEmpty(CurrentGameData.equippedIconId) && CurrentGameData.unlockedIconIds?.Count > 0)
        {
            CurrentGameData.equippedIconId = CurrentGameData.unlockedIconIds[0];
            Debug.Log($"?? Set default equipped icon to: {CurrentGameData.equippedIconId}");
            changesMade = true;
        }

        // Set default equipped frame if none is set
        if (string.IsNullOrEmpty(CurrentGameData.equippedFrameId) && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            CurrentGameData.equippedFrameId = CurrentGameData.unlockedFrameIds[0];
            Debug.Log($"?? Set default equipped frame to: {CurrentGameData.equippedFrameId}");
            changesMade = true;
        }

        // Save changes if any were made
        if (changesMade)
        {
            SaveGameData();
            Debug.Log("?? Saved default icons and frames to GameData");
        }

        hasInitializedDefaults = true;
        Debug.Log("=== DEFAULT ICONS AND FRAMES INITIALIZATION COMPLETE ===");
    }

    private void CreateNewGameData()
    {
        CurrentGameData = new GameData();

        // Initialize default icons and frames for new game
        // Reset the flag so initialization runs
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();

        SaveGameData();
    }

    public void ResetGameData()
    {
        CurrentGameData = new GameData();

        // Reset flag and initialize defaults
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();
        InitializeDefaultSkins();

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }

        Debug.Log("Game data reset to default!");
    }

    // Public method to force re-initialization (useful for debugging)
    public void ForceReinitializeDefaults()
    {
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();
    }

    private void UpdateEnergyBasedOnTime()
    {
        if (CurrentGameData == null) return;

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
        if (CurrentGameData == null) return;

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
        return CurrentGameData != null && CurrentGameData.isChestAvailable;
    }

    public TimeSpan GetTimeUntilNextChest()
    {
        if (CurrentGameData == null) return TimeSpan.Zero;

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
        if (CurrentGameData == null || !CurrentGameData.isChestAvailable) return;

        CurrentGameData.nutriCoins += 50;
        CurrentGameData.isChestAvailable = false;
        CurrentGameData.lastChestClaimTime = DateTime.Now;

        SaveGameData();
        Debug.Log($"Chest claimed! Received 50 coins. Total coins: {CurrentGameData.nutriCoins}");
    }

    private void InitializeDefaultCharacters()
    {
        if (CurrentGameData == null) return;

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
        if (CurrentGameData == null) return;

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

        // First check if it's in the unlocked list
        bool inUnlockedList = CurrentGameData.unlockedIconIds != null && CurrentGameData.unlockedIconIds.Contains(iconId);

        // Then check database for unlockedByDefault (for safety)
        bool isDefault = false;
        if (iconDatabase != null)
        {
            var icon = iconDatabase.GetIcon(iconId);
            if (icon != null)
            {
                isDefault = icon.unlockedByDefault;
            }
        }

        return inUnlockedList || isDefault;
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

        // First check if it's in the unlocked list
        bool inUnlockedList = CurrentGameData.unlockedFrameIds != null && CurrentGameData.unlockedFrameIds.Contains(frameId);

        // Then check database for unlockedByDefault (for safety)
        bool isDefault = false;
        if (frameDatabase != null)
        {
            var frame = frameDatabase.GetFrame(frameId);
            if (frame != null)
            {
                isDefault = frame.unlockedByDefault;
            }
        }

        return inUnlockedList || isDefault;
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