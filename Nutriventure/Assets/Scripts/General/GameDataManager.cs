using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
                Debug.Log($"Unlocked Icons: {string.Join(", ", CurrentGameData.unlockedIconIds ?? new List<string>())}");
                Debug.Log($"Equipped Frame ID: {CurrentGameData.equippedFrameId}");
                Debug.Log($"Unlocked Frames count: {CurrentGameData.unlockedFrameIds?.Count ?? 0}");
                Debug.Log($"Unlocked Frames: {string.Join(", ", CurrentGameData.unlockedFrameIds ?? new List<string>())}");
                Debug.Log($"Completed Achievements count: {CurrentGameData.completedAchievementIds?.Count ?? 0}");
                Debug.Log($"Claimed Achievements count: {CurrentGameData.claimedAchievementIds?.Count ?? 0}");
                Debug.Log($"Unlocked Enerlings count: {CurrentGameData.unlockedEnerlings?.Count ?? 0}");
                Debug.Log($"Unlocked Enerlings: {string.Join(", ", CurrentGameData.unlockedEnerlings ?? new List<string>())}");
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
                        Debug.Log($"Added default icon: {icon.id} - {icon.iconName}");
                        changesMade = true;
                    }
                }
            }

            // Log all unlocked icons after initialization
            Debug.Log($"Unlocked icons after initialization: {string.Join(", ", CurrentGameData.unlockedIconIds)}");
        }
        else
        {
            Debug.LogError("iconDatabase is not assigned! Default icons will not be initialized.");
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
                        Debug.Log($"Added default frame: {frame.id} - {frame.frameName}");
                        changesMade = true;
                    }
                }
            }

            // Log all unlocked frames after initialization
            Debug.Log($"Unlocked frames after initialization: {string.Join(", ", CurrentGameData.unlockedFrameIds)}");
        }
        else
        {
            Debug.LogError("frameDatabase is not assigned! Default frames will not be initialized.");
        }

        // ===== SET DEFAULT EQUIPPED ITEMS =====

        // Set default equipped icon if none is set
        if (string.IsNullOrEmpty(CurrentGameData.equippedIconId) && CurrentGameData.unlockedIconIds?.Count > 0)
        {
            CurrentGameData.equippedIconId = CurrentGameData.unlockedIconIds[0];
            Debug.Log($"Set default equipped icon to: {CurrentGameData.equippedIconId}");
            changesMade = true;
        }

        // Set default equipped frame if none is set
        if (string.IsNullOrEmpty(CurrentGameData.equippedFrameId) && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            CurrentGameData.equippedFrameId = CurrentGameData.unlockedFrameIds[0];
            Debug.Log($"Set default equipped frame to: {CurrentGameData.equippedFrameId}");
            changesMade = true;
        }

        // Save changes if any were made
        if (changesMade)
        {
            SaveGameData();
            Debug.Log("Saved default icons and frames to GameData");
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

    // ========== RESET METHODS ==========

    /// <summary>
    /// Completely resets all game data to default values
    /// </summary>
    public void ResetGameData()
    {
        Debug.LogWarning("=== RESETTING ALL GAME DATA ===");

        // Delete the save file
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"Deleted save file: {saveFilePath}");
        }

        // Create fresh game data
        CurrentGameData = new GameData();

        // Reset flag so defaults are re-initialized
        hasInitializedDefaults = false;

        // Re-initialize defaults
        InitializeDefaultIconsAndFrames();
        InitializeDefaultSkins();

        // Reset audio settings
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }

        // Save the fresh data
        SaveGameData();

        Debug.LogWarning("=== GAME DATA RESET COMPLETE ===");
    }

    /// <summary>
    /// Reset only icons and frames, keep other data
    /// </summary>
    public void ResetIconsAndFrames()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ICONS AND FRAMES ===");

        // Clear icon and frame lists
        CurrentGameData.unlockedIconIds = new List<string>();
        CurrentGameData.unlockedFrameIds = new List<string>();

        // Reset equipped items
        CurrentGameData.equippedIconId = "";
        CurrentGameData.equippedFrameId = "";

        // Re-initialize defaults
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();

        SaveGameData();
        Debug.LogWarning("=== ICONS AND FRAMES RESET COMPLETE ===");
    }

    /// <summary>
    /// Reset only achievements
    /// </summary>
    public void ResetAchievements()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ACHIEVEMENTS ===");

        CurrentGameData.completedAchievementIds = new List<string>();
        CurrentGameData.claimedAchievementIds = new List<string>();

        SaveGameData();
        Debug.LogWarning("=== ACHIEVEMENTS RESET COMPLETE ===");
    }

    /// <summary>
    /// Reset only characters and skins
    /// </summary>
    public void ResetCharactersAndSkins()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING CHARACTERS AND SKINS ===");

        CurrentGameData.unlockedCharacterIDs = new List<int>() { 0 }; // Keep default character
        CurrentGameData.selectedCharacterID = 0;
        CurrentGameData.selectedSkinForCharacter = new GameData.SkinDictionary();
        CurrentGameData.unlockedSkinsForCharacter = new GameData.UnlockedSkinsDictionary();

        // Re-initialize skins
        InitializeDefaultSkins();

        SaveGameData();
        Debug.LogWarning("=== CHARACTERS AND SKINS RESET COMPLETE ===");
    }

    /// <summary>
    /// Reset only enerlings collection
    /// </summary>
    public void ResetEnerlings()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ENERLINGS ===");

        CurrentGameData.unlockedEnerlings = new List<string>();

        SaveGameData();
        Debug.LogWarning("=== ENERLINGS RESET COMPLETE ===");
    }

    /// <summary>
    /// Reset only resources (coins, gems, energy)
    /// </summary>
    public void ResetResources()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING RESOURCES ===");

        CurrentGameData.nutriCoins = 0;
        CurrentGameData.nutriGems = 0;
        CurrentGameData.currentEnergy = 10;
        CurrentGameData.lastEnergyUpdateTime = DateTime.Now;

        SaveGameData();
        Debug.LogWarning("=== RESOURCES RESET COMPLETE ===");
    }

    // ========== DEBUG METHODS (Only visible in Inspector Context Menu) ==========

    #region Debug Methods

    [ContextMenu("Debug/Reset All Game Data")]
    private void DebugResetAllGameData()
    {
        ResetGameData();
    }

    [ContextMenu("Debug/Reset Icons and Frames Only")]
    private void DebugResetIconsAndFrames()
    {
        ResetIconsAndFrames();
    }

    [ContextMenu("Debug/Reset Achievements Only")]
    private void DebugResetAchievements()
    {
        ResetAchievements();
    }

    [ContextMenu("Debug/Reset Characters and Skins Only")]
    private void DebugResetCharactersAndSkins()
    {
        ResetCharactersAndSkins();
    }

    [ContextMenu("Debug/Reset Enerlings Only")]
    private void DebugResetEnerlings()
    {
        ResetEnerlings();
    }

    [ContextMenu("Debug/Reset Resources Only")]
    private void DebugResetResources()
    {
        ResetResources();
    }

    [ContextMenu("Debug/Print Current Game Data")]
    private void DebugPrintGameData()
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("No game data loaded!");
            return;
        }

        Debug.Log("=== CURRENT GAME DATA ===");
        Debug.Log($"Player: {CurrentGameData.playerName} (Level {CurrentGameData.playerLevel})");
        Debug.Log($"Resources: {CurrentGameData.nutriCoins} Coins, {CurrentGameData.nutriGems} Gems, {CurrentGameData.currentEnergy} Energy");
        Debug.Log($"Unlocked Icons ({CurrentGameData.unlockedIconIds?.Count ?? 0}): {string.Join(", ", CurrentGameData.unlockedIconIds ?? new List<string>())}");
        Debug.Log($"Unlocked Frames ({CurrentGameData.unlockedFrameIds?.Count ?? 0}): {string.Join(", ", CurrentGameData.unlockedFrameIds ?? new List<string>())}");
        Debug.Log($"Unlocked Characters ({CurrentGameData.unlockedCharacterIDs?.Count ?? 0}): {string.Join(", ", CurrentGameData.unlockedCharacterIDs ?? new List<int>())}");
        Debug.Log($"Unlocked Enerlings ({CurrentGameData.unlockedEnerlings?.Count ?? 0}): {string.Join(", ", CurrentGameData.unlockedEnerlings ?? new List<string>())}");
        Debug.Log($"Completed Achievements ({CurrentGameData.completedAchievementIds?.Count ?? 0})");
        Debug.Log($"Claimed Achievements ({CurrentGameData.claimedAchievementIds?.Count ?? 0})");
        Debug.Log("=== END GAME DATA ===");
    }

    #endregion

    // ... (rest of your existing methods remain the same)
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

        // First check if it's in the unlocked list by ID
        bool inUnlockedList = CurrentGameData.unlockedIconIds != null &&
                              CurrentGameData.unlockedIconIds.Contains(iconId);

        // Then check database for unlockedByDefault (for safety)
        bool isDefault = false;
        if (iconDatabase != null)
        {
            var icon = iconDatabase.GetIcon(iconId);
            if (icon != null)
            {
                isDefault = icon.unlockedByDefault;

                // Also check if the icon name is in the unlocked list
                if (!inUnlockedList && CurrentGameData.unlockedIconIds != null)
                {
                    inUnlockedList = CurrentGameData.unlockedIconIds.Contains(icon.iconName);
                }
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

        // First check if it's in the unlocked list by ID
        bool inUnlockedList = CurrentGameData.unlockedFrameIds != null &&
                              CurrentGameData.unlockedFrameIds.Contains(frameId);

        // Then check database for unlockedByDefault (for safety)
        bool isDefault = false;
        if (frameDatabase != null)
        {
            var frame = frameDatabase.GetFrame(frameId);
            if (frame != null)
            {
                isDefault = frame.unlockedByDefault;

                // Also check if the frame name is in the unlocked list
                if (!inUnlockedList && CurrentGameData.unlockedFrameIds != null)
                {
                    inUnlockedList = CurrentGameData.unlockedFrameIds.Contains(frame.frameName);
                }

                Debug.Log($"Frame {frameId} - Default unlock: {isDefault}, In unlocked list: {inUnlockedList}");
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