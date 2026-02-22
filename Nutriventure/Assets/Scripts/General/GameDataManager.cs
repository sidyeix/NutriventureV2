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
            Debug.Log("=== GAME DATA SAVED ===");
            Debug.Log($"Selected Character: {CurrentGameData.selectedCharacterID}");

            // Print skin data using the new List approach
            if (CurrentGameData.skinData != null)
            {
                Debug.Log($"SkinData has {CurrentGameData.skinData.Count} entries");
                foreach (var data in CurrentGameData.skinData)
                {
                    Debug.Log($"Character {data.characterID}: Selected={data.selectedSkinID}, Unlocked={string.Join(", ", data.unlockedSkinIDs)}");
                }
            }
            else
            {
                Debug.LogError("skinData is NULL during save!");
            }

            Debug.Log("=== END SAVED DATA ===");
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

                Debug.Log("=== GAME DATA LOADED ===");
                Debug.Log($"Selected Character ID: {CurrentGameData.selectedCharacterID}");

                // Check if skin data was loaded
                if (CurrentGameData.skinData != null)
                {
                    Debug.Log($"SkinData loaded with {CurrentGameData.skinData.Count} entries");
                    foreach (var data in CurrentGameData.skinData)
                    {
                        Debug.Log($"Character {data.characterID}: Selected={data.selectedSkinID}, Unlocked={string.Join(", ", data.unlockedSkinIDs)}");
                    }
                }
                else
                {
                    Debug.LogWarning("skinData is NULL after loading! Creating new one.");
                    CurrentGameData.skinData = new List<GameData.SkinSaveData>();
                }

                Debug.Log($"Equipped Icon ID: {CurrentGameData.equippedIconId}");
                Debug.Log($"Unlocked Icons count: {CurrentGameData.unlockedIconIds?.Count ?? 0}");
                Debug.Log($"Unlocked Icons: {string.Join(", ", CurrentGameData.unlockedIconIds ?? new List<string>())}");
                Debug.Log($"Equipped Frame ID: {CurrentGameData.equippedFrameId}");
                Debug.Log($"Unlocked Frames count: {CurrentGameData.unlockedFrameIds?.Count ?? 0}");
                Debug.Log($"Unlocked Frames: {string.Join(", ", CurrentGameData.unlockedFrameIds ?? new List<string>())}");
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

    private void CreateNewGameData()
    {
        CurrentGameData = new GameData();
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();
        SaveGameData();
        Debug.Log("New GameData created");
    }

    private void InitializeDefaultIconsAndFrames()
    {
        if (CurrentGameData == null) return;

        if (hasInitializedDefaults && CurrentGameData.unlockedIconIds?.Count > 0 && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            Debug.Log("Default icons and frames already initialized, skipping...");
            return;
        }

        Debug.Log("=== INITIALIZING DEFAULT ICONS AND FRAMES FROM DATABASES ===");

        bool changesMade = false;

        // Initialize Icons
        if (iconDatabase != null)
        {
            Debug.Log($"Icon database found with {iconDatabase.icons.Count} icons");

            if (CurrentGameData.unlockedIconIds == null)
            {
                CurrentGameData.unlockedIconIds = new List<string>();
            }

            foreach (var icon in iconDatabase.icons)
            {
                if (icon.unlockedByDefault && !CurrentGameData.unlockedIconIds.Contains(icon.id))
                {
                    CurrentGameData.unlockedIconIds.Add(icon.id);
                    Debug.Log($"Added default icon: {icon.id} - {icon.iconName}");
                    changesMade = true;
                }
            }

            Debug.Log($"Unlocked icons after initialization: {string.Join(", ", CurrentGameData.unlockedIconIds)}");
        }
        else
        {
            Debug.LogError("iconDatabase is not assigned! Default icons will not be initialized.");
        }

        // Initialize Frames
        if (frameDatabase != null)
        {
            Debug.Log($"Frame database found with {frameDatabase.frames.Count} frames");

            if (CurrentGameData.unlockedFrameIds == null)
            {
                CurrentGameData.unlockedFrameIds = new List<string>();
            }

            foreach (var frame in frameDatabase.frames)
            {
                if (frame.unlockedByDefault && !CurrentGameData.unlockedFrameIds.Contains(frame.id))
                {
                    CurrentGameData.unlockedFrameIds.Add(frame.id);
                    Debug.Log($"Added default frame: {frame.id} - {frame.frameName}");
                    changesMade = true;
                }
            }

            Debug.Log($"Unlocked frames after initialization: {string.Join(", ", CurrentGameData.unlockedFrameIds)}");
        }
        else
        {
            Debug.LogError("frameDatabase is not assigned! Default frames will not be initialized.");
        }

        // Set default equipped items
        if (string.IsNullOrEmpty(CurrentGameData.equippedIconId) && CurrentGameData.unlockedIconIds?.Count > 0)
        {
            CurrentGameData.equippedIconId = CurrentGameData.unlockedIconIds[0];
            Debug.Log($"Set default equipped icon to: {CurrentGameData.equippedIconId}");
            changesMade = true;
        }

        if (string.IsNullOrEmpty(CurrentGameData.equippedFrameId) && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            CurrentGameData.equippedFrameId = CurrentGameData.unlockedFrameIds[0];
            Debug.Log($"Set default equipped frame to: {CurrentGameData.equippedFrameId}");
            changesMade = true;
        }

        if (changesMade)
        {
            SaveGameData();
            Debug.Log("Saved default icons and frames to GameData");
        }

        hasInitializedDefaults = true;
        Debug.Log("=== DEFAULT ICONS AND FRAMES INITIALIZATION COMPLETE ===");
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
        Debug.Log("Skin system initialized with List approach!");
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

        bool inUnlockedList = CurrentGameData.unlockedIconIds != null &&
                              CurrentGameData.unlockedIconIds.Contains(iconId);

        bool isDefault = false;
        if (iconDatabase != null)
        {
            var icon = iconDatabase.GetIcon(iconId);
            if (icon != null)
            {
                isDefault = icon.unlockedByDefault;

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

        bool inUnlockedList = CurrentGameData.unlockedFrameIds != null &&
                              CurrentGameData.unlockedFrameIds.Contains(frameId);

        bool isDefault = false;
        if (frameDatabase != null)
        {
            var frame = frameDatabase.GetFrame(frameId);
            if (frame != null)
            {
                isDefault = frame.unlockedByDefault;

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

    #region Skin System Methods - UPDATED FOR List APPROACH

    public void UnlockSkin(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot unlock skin: CurrentGameData is null");
            return;
        }

        Debug.Log($"===== UNLOCK SKIN CALLED =====");
        Debug.Log($"Character ID: {characterID}, Skin ID: {skinID}");

        // Log before unlock
        bool beforeUnlock = CurrentGameData.IsSkinUnlocked(characterID, skinID);
        Debug.Log($"Before unlock - Is skin unlocked? {beforeUnlock}");

        // Unlock the skin
        CurrentGameData.UnlockSkinForCharacter(characterID, skinID);

        // Save immediately
        SaveGameData();

        // Verify after unlock
        bool afterUnlock = CurrentGameData.IsSkinUnlocked(characterID, skinID);
        Debug.Log($"After unlock - Is skin unlocked? {afterUnlock}");

        // Print all unlocked skins for this character
        var unlockedSkins = CurrentGameData.GetUnlockedSkinsForCharacter(characterID);
        Debug.Log($"All unlocked skins for character {characterID}: {string.Join(", ", unlockedSkins)}");

        Debug.Log($"===== UNLOCK SKIN COMPLETE =====");
    }

    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot check skin unlock: CurrentGameData is null");
            return false;
        }

        bool isUnlocked = CurrentGameData.IsSkinUnlocked(characterID, skinID);
        Debug.Log($"IsSkinUnlocked - Character {characterID}, Skin {skinID}: {isUnlocked}");
        return isUnlocked;
    }

    public List<int> GetUnlockedSkins(int characterID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot get unlocked skins: CurrentGameData is null");
            return new List<int>();
        }

        var unlockedSkins = CurrentGameData.GetUnlockedSkinsForCharacter(characterID);
        Debug.Log($"GetUnlockedSkins - Character {characterID}: {string.Join(", ", unlockedSkins)}");
        return unlockedSkins;
    }

    public void SetSelectedSkin(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot set selected skin: CurrentGameData is null");
            return;
        }

        Debug.Log($"Setting selected skin {skinID} for character {characterID}");
        CurrentGameData.SetSelectedSkinForCharacter(characterID, skinID);
        SaveGameData();
    }

    public int GetSelectedSkin(int characterID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot get selected skin: CurrentGameData is null");
            return -1;
        }

        return CurrentGameData.GetSelectedSkinForCharacter(characterID);
    }

    #endregion

    #region Reset Methods

    public void ResetGameData()
    {
        Debug.LogWarning("=== RESETTING ALL GAME DATA ===");

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"Deleted save file: {saveFilePath}");
        }

        CurrentGameData = new GameData();
        hasInitializedDefaults = false;

        InitializeDefaultIconsAndFrames();
        InitializeDefaultSkins();

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }

        SaveGameData();
        Debug.LogWarning("=== GAME DATA RESET COMPLETE ===");
    }

    public void ResetIconsAndFrames()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ICONS AND FRAMES ===");

        CurrentGameData.unlockedIconIds = new List<string>();
        CurrentGameData.unlockedFrameIds = new List<string>();
        CurrentGameData.equippedIconId = "";
        CurrentGameData.equippedFrameId = "";

        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();

        SaveGameData();
        Debug.LogWarning("=== ICONS AND FRAMES RESET COMPLETE ===");
    }

    public void ResetAchievements()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ACHIEVEMENTS ===");

        CurrentGameData.completedAchievementIds = new List<string>();
        CurrentGameData.claimedAchievementIds = new List<string>();

        SaveGameData();
        Debug.LogWarning("=== ACHIEVEMENTS RESET COMPLETE ===");
    }

    public void ResetCharactersAndSkins()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING CHARACTERS AND SKINS ===");

        CurrentGameData.unlockedCharacterIDs = new List<int>() { 0 };
        CurrentGameData.selectedCharacterID = 0;
        CurrentGameData.skinData = new List<GameData.SkinSaveData>();

        InitializeDefaultSkins();

        SaveGameData();
        Debug.LogWarning("=== CHARACTERS AND SKINS RESET COMPLETE ===");
    }

    public void ResetEnerlings()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING ENERLINGS ===");

        CurrentGameData.unlockedEnerlings = new List<string>();

        SaveGameData();
        Debug.LogWarning("=== ENERLINGS RESET COMPLETE ===");
    }

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

    #endregion

    #region Debug Methods

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

        // Print skin data using new List approach
        if (CurrentGameData.skinData != null)
        {
            Debug.Log($"SkinData has {CurrentGameData.skinData.Count} entries");
            foreach (var data in CurrentGameData.skinData)
            {
                Debug.Log($"Character {data.characterID}: Selected={data.selectedSkinID}, Unlocked={string.Join(", ", data.unlockedSkinIDs)}");
            }
        }

        Debug.Log("=== END GAME DATA ===");
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