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
    public IngredientDatabase ingredientDatabase;

    [Header("Debug Options")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoSaveOnQuit = true;

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
            
            if (enableDebugLogs)
            {
                Debug.Log("=== GAME DATA SAVED ===");
                Debug.Log($"Selected Character: {CurrentGameData.selectedCharacterID}");

                if (CurrentGameData.skinData != null)
                {
                    Debug.Log($"SkinData has {CurrentGameData.skinData.Count} entries");
                }

                if (CurrentGameData.activePowerUps != null && CurrentGameData.activePowerUps.Count > 0)
                {
                    Debug.Log($"Active Power-ups: {CurrentGameData.activePowerUps.Count}");
                    foreach (var powerUp in CurrentGameData.activePowerUps)
                    {
                        Debug.Log($"Pet: {powerUp.petName}, Type: {powerUp.powerUpType}, Last Trigger: {powerUp.lastTriggerTime}, Cooldown: {powerUp.cooldownMinutes}min");
                    }
                }

                if (CurrentGameData.passivePowerUps != null && CurrentGameData.passivePowerUps.Count > 0)
                {
                    Debug.Log($"Passive Power-ups: {CurrentGameData.passivePowerUps.Count}");
                    foreach (var powerUp in CurrentGameData.passivePowerUps)
                    {
                        Debug.Log($"Pet: {powerUp.petName}, Type: {powerUp.powerUpType}, Amount: {powerUp.amount}");
                    }
                    Debug.Log($"Total Heart Bonus: {CurrentGameData.GetTotalHeartBonus()}");
                    Debug.Log($"Total Time Reduction: {CurrentGameData.GetTotalTimeReductionFormatted()}");
                }

                Debug.Log("=== END SAVED DATA ===");
            }
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

                if (enableDebugLogs)
                {
                    Debug.Log("=== GAME DATA LOADED ===");
                    Debug.Log($"Selected Character ID: {CurrentGameData.selectedCharacterID}");
                }

                if (CurrentGameData.skinData == null)
                    CurrentGameData.skinData = new List<GameData.SkinSaveData>();

                if (CurrentGameData.activePowerUps == null)
                    CurrentGameData.activePowerUps = new List<GameData.PowerUpSaveData>();

                if (CurrentGameData.passivePowerUps == null)
                    CurrentGameData.passivePowerUps = new List<GameData.PassivePowerUpData>();

                if (enableDebugLogs)
                {
                    Debug.Log($"Equipped Pet Slot 1: {CurrentGameData.equippedPetSlot1}");
                    Debug.Log($"Equipped Pet Slot 2: {CurrentGameData.equippedPetSlot2}");
                    Debug.Log($"Active Power-ups: {CurrentGameData.activePowerUps.Count}");
                    Debug.Log($"Passive Power-ups: {CurrentGameData.passivePowerUps.Count}");
                    Debug.Log($"Total Heart Bonus: {CurrentGameData.GetTotalHeartBonus()}");
                    Debug.Log($"Total Time Reduction: {CurrentGameData.GetTotalTimeReductionFormatted()}");
                    Debug.Log($"=== END LOAD ===");
                }
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

        InitializeDefaultIconsAndFrames();
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
            if (enableDebugLogs)
                Debug.Log("Default icons and frames already initialized, skipping...");
            return;
        }

        if (enableDebugLogs)
            Debug.Log("=== INITIALIZING DEFAULT ICONS AND FRAMES FROM DATABASES ===");

        bool changesMade = false;

        if (iconDatabase != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Icon database found with {iconDatabase.icons.Count} icons");

            if (CurrentGameData.unlockedIconIds == null)
                CurrentGameData.unlockedIconIds = new List<string>();

            foreach (var icon in iconDatabase.icons)
            {
                if (icon.unlockedByDefault && !CurrentGameData.unlockedIconIds.Contains(icon.id))
                {
                    CurrentGameData.unlockedIconIds.Add(icon.id);
                    if (enableDebugLogs)
                        Debug.Log($"Added default icon: {icon.id} - {icon.iconName}");
                    changesMade = true;
                }
            }

            if (enableDebugLogs)
                Debug.Log($"Unlocked icons after initialization: {string.Join(", ", CurrentGameData.unlockedIconIds)}");
        }
        else
        {
            Debug.LogError("iconDatabase is not assigned! Default icons will not be initialized.");
        }

        if (frameDatabase != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Frame database found with {frameDatabase.frames.Count} frames");

            if (CurrentGameData.unlockedFrameIds == null)
                CurrentGameData.unlockedFrameIds = new List<string>();

            foreach (var frame in frameDatabase.frames)
            {
                if (frame.unlockedByDefault && !CurrentGameData.unlockedFrameIds.Contains(frame.id))
                {
                    CurrentGameData.unlockedFrameIds.Add(frame.id);
                    if (enableDebugLogs)
                        Debug.Log($"Added default frame: {frame.id} - {frame.frameName}");
                    changesMade = true;
                }
            }

            if (enableDebugLogs)
                Debug.Log($"Unlocked frames after initialization: {string.Join(", ", CurrentGameData.unlockedFrameIds)}");
        }
        else
        {
            Debug.LogError("frameDatabase is not assigned! Default frames will not be initialized.");
        }

        if (string.IsNullOrEmpty(CurrentGameData.equippedIconId) && CurrentGameData.unlockedIconIds?.Count > 0)
        {
            CurrentGameData.equippedIconId = CurrentGameData.unlockedIconIds[0];
            if (enableDebugLogs)
                Debug.Log($"Set default equipped icon to: {CurrentGameData.equippedIconId}");
            changesMade = true;
        }

        if (string.IsNullOrEmpty(CurrentGameData.equippedFrameId) && CurrentGameData.unlockedFrameIds?.Count > 0)
        {
            CurrentGameData.equippedFrameId = CurrentGameData.unlockedFrameIds[0];
            if (enableDebugLogs)
                Debug.Log($"Set default equipped frame to: {CurrentGameData.equippedFrameId}");
            changesMade = true;
        }

        if (changesMade)
        {
            SaveGameData();
            if (enableDebugLogs)
                Debug.Log("Saved default icons and frames to GameData");
        }

        hasInitializedDefaults = true;
        if (enableDebugLogs)
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

        if (enableDebugLogs)
            Debug.Log($"Before adding defaults - Selected Character: {CurrentGameData.selectedCharacterID}");

        foreach (var character in characterDatabase.characters)
        {
            if (character.unlockedByDefault)
            {
                if (!CurrentGameData.unlockedCharacterIDs.Contains(character.characterID))
                {
                    CurrentGameData.unlockedCharacterIDs.Add(character.characterID);
                    if (enableDebugLogs)
                        Debug.Log($"Added default character {character.characterID} ({character.characterName}) to unlocked list");
                }
            }
        }

        if (enableDebugLogs)
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
        if (enableDebugLogs)
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
            if (enableDebugLogs)
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
        if (enableDebugLogs)
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
            if (enableDebugLogs)
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
            if (enableDebugLogs)
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
            if (enableDebugLogs)
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

                if (enableDebugLogs)
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
            if (enableDebugLogs)
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
        if (enableDebugLogs)
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
            if (enableDebugLogs)
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

    #region Skin System Methods

    public void UnlockSkin(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot unlock skin: CurrentGameData is null");
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"===== UNLOCK SKIN CALLED =====");
            Debug.Log($"Character ID: {characterID}, Skin ID: {skinID}");
        }

        bool beforeUnlock = CurrentGameData.IsSkinUnlocked(characterID, skinID);
        if (enableDebugLogs)
            Debug.Log($"Before unlock - Is skin unlocked? {beforeUnlock}");

        CurrentGameData.UnlockSkinForCharacter(characterID, skinID);
        SaveGameData();

        bool afterUnlock = CurrentGameData.IsSkinUnlocked(characterID, skinID);
        if (enableDebugLogs)
        {
            Debug.Log($"After unlock - Is skin unlocked? {afterUnlock}");

            var unlockedSkins = CurrentGameData.GetUnlockedSkinsForCharacter(characterID);
            Debug.Log($"All unlocked skins for character {characterID}: {string.Join(", ", unlockedSkins)}");

            Debug.Log($"===== UNLOCK SKIN COMPLETE =====");
        }
    }

    public bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot check skin unlock: CurrentGameData is null");
            return false;
        }

        return CurrentGameData.IsSkinUnlocked(characterID, skinID);
    }

    public List<int> GetUnlockedSkins(int characterID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot get unlocked skins: CurrentGameData is null");
            return new List<int>();
        }

        return CurrentGameData.GetUnlockedSkinsForCharacter(characterID);
    }

    public void SetSelectedSkin(int characterID, int skinID)
    {
        if (CurrentGameData == null)
        {
            Debug.LogError("Cannot set selected skin: CurrentGameData is null");
            return;
        }

        if (enableDebugLogs)
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

    #region Enerling Pet System Methods

    public void EquipPetToSlot(int slotIndex, string petName)
    {
        if (CurrentGameData == null) return;

        string previousPet = "";
        if (slotIndex == 1)
        {
            previousPet = CurrentGameData.equippedPetSlot1;
            CurrentGameData.equippedPetSlot1 = petName;
        }
        else if (slotIndex == 2)
        {
            previousPet = CurrentGameData.equippedPetSlot2;
            CurrentGameData.equippedPetSlot2 = petName;
        }

        if (!string.IsNullOrEmpty(previousPet) && previousPet != petName)
        {
            bool stillEquipped = (slotIndex == 1 && CurrentGameData.equippedPetSlot2 == previousPet) ||
                                 (slotIndex == 2 && CurrentGameData.equippedPetSlot1 == previousPet);

            if (!stillEquipped)
            {
                CurrentGameData.RemovePowerUpsForPet(previousPet);
                CurrentGameData.RemovePassivePowerUpsForPet(previousPet);
                if (enableDebugLogs)
                    Debug.Log($"Pet {previousPet} no longer equipped - power-ups removed");
            }
        }

        SaveGameData();
        if (enableDebugLogs)
            Debug.Log($"Equipped {petName} to slot {slotIndex}");
    }

    public void RemovePetFromSlot(int slotIndex)
    {
        if (CurrentGameData == null) return;

        string removedPet = "";
        if (slotIndex == 1)
        {
            removedPet = CurrentGameData.equippedPetSlot1;
            CurrentGameData.equippedPetSlot1 = "";
        }
        else if (slotIndex == 2)
        {
            removedPet = CurrentGameData.equippedPetSlot2;
            CurrentGameData.equippedPetSlot2 = "";
        }

        if (!string.IsNullOrEmpty(removedPet))
        {
            bool stillEquipped = (slotIndex == 1 && CurrentGameData.equippedPetSlot2 == removedPet) ||
                                 (slotIndex == 2 && CurrentGameData.equippedPetSlot1 == removedPet);

            if (!stillEquipped)
            {
                CurrentGameData.RemovePowerUpsForPet(removedPet);
                CurrentGameData.RemovePassivePowerUpsForPet(removedPet);
                if (enableDebugLogs)
                    Debug.Log($"Pet {removedPet} removed - power-ups removed");
            }
        }

        SaveGameData();
        if (enableDebugLogs)
            Debug.Log($"Removed pet from slot {slotIndex}");
    }

    public string GetEquippedPet(int slotIndex)
    {
        if (CurrentGameData == null) return "";

        if (slotIndex == 1)
            return CurrentGameData.equippedPetSlot1;
        else if (slotIndex == 2)
            return CurrentGameData.equippedPetSlot2;

        return "";
    }

    public List<string> GetAllEquippedPets()
    {
        List<string> pets = new List<string>();

        if (CurrentGameData == null) return pets;

        if (!string.IsNullOrEmpty(CurrentGameData.equippedPetSlot1))
            pets.Add(CurrentGameData.equippedPetSlot1);

        if (!string.IsNullOrEmpty(CurrentGameData.equippedPetSlot2))
            pets.Add(CurrentGameData.equippedPetSlot2);

        return pets;
    }

    #endregion

    #region Power-Up Tracking Methods

    public void RegisterPowerUp(string petName, int powerUpIndex, IngredientDatabase.PowerUpInfo.PowerUpType type, float cooldownMinutes, int amount)
    {
        if (CurrentGameData == null) return;

        CurrentGameData.AddPowerUp(petName, powerUpIndex, type, cooldownMinutes, amount);
        SaveGameData();
    }

    public void UpdatePowerUpTriggerTime(string petName, int powerUpIndex)
    {
        if (CurrentGameData == null) return;

        CurrentGameData.UpdatePowerUpLastTriggerTime(petName, powerUpIndex);
        SaveGameData();
    }

    public TimeSpan GetPowerUpTimeRemaining(string petName, int powerUpIndex)
    {
        if (CurrentGameData == null) return TimeSpan.Zero;

        return CurrentGameData.GetTimeUntilNextPowerUp(petName, powerUpIndex);
    }

    public List<GameData.PowerUpSaveData> GetAllActivePowerUps()
    {
        if (CurrentGameData == null) return new List<GameData.PowerUpSaveData>();

        return CurrentGameData.GetAllActivePowerUps();
    }

    public void RegisterPassivePowerUp(string petName, IngredientDatabase.PowerUpInfo.PowerUpType type, int amount)
    {
        if (CurrentGameData == null) return;

        CurrentGameData.AddPassivePowerUp(petName, type, amount);
        SaveGameData();
    }

    public int GetTotalHeartBonus()
    {
        if (CurrentGameData == null) return 0;

        return CurrentGameData.GetTotalHeartBonus();
    }

    public int GetTotalTimeReductionSeconds()
    {
        if (CurrentGameData == null) return 0;

        return CurrentGameData.GetTotalTimeReductionSeconds();
    }

    public string GetTotalTimeReductionFormatted()
    {
        if (CurrentGameData == null) return "0s";

        return CurrentGameData.GetTotalTimeReductionFormatted();
    }

    #endregion

    #region Kingdom Key Methods

    public bool HasSugariaKey() => CurrentGameData?.HasSugariaKey() ?? false;
    public bool HasPreserviaKey() => CurrentGameData?.HasPreserviaKey() ?? false;
    public bool HasNutriKingdomKey() => CurrentGameData?.HasNutriKingdomKey() ?? false;
    public bool HasAllerthiaKey() => CurrentGameData?.HasAllerthiaKey() ?? false;
    public bool HasOCRScannerKey() => CurrentGameData?.HasOCRScannerKey() ?? false;

    public void CollectSugariaKey() 
    { 
        CurrentGameData?.CollectSugariaKey(); 
        SaveGameData();
        if (enableDebugLogs) Debug.Log("Sugaria Key collected!");
    }
    
    public void CollectPreserviaKey() 
    { 
        CurrentGameData?.CollectPreserviaKey(); 
        SaveGameData();
        if (enableDebugLogs) Debug.Log("Preservia Key collected!");
    }
    
    public void CollectNutriKingdomKey() 
    { 
        CurrentGameData?.CollectNutriKingdomKey(); 
        SaveGameData();
        if (enableDebugLogs) Debug.Log("Nutri Kingdom Key collected!");
    }
    
    public void CollectAllerthiaKey() 
    { 
        CurrentGameData?.CollectAllerthiaKey(); 
        SaveGameData();
        if (enableDebugLogs) Debug.Log("Allerthia Key collected!");
    }

    public void CollectOCRScannerKey() 
    { 
        CurrentGameData?.CollectOCRScannerKey(); 
        SaveGameData();
        if (enableDebugLogs) Debug.Log("OCR Scanner Key collected!");
    }

    public void ResetOCRScannerKey()
    {
        if (CurrentGameData != null)
        {
            CurrentGameData.ResetOCRScannerKey();
            SaveGameData();
            if (enableDebugLogs) Debug.Log("OCR Scanner Key reset to default (false)");
        }
    }

    public bool HasKingdomKey(string kingdomName)
    {
        return CurrentGameData?.HasKingdomKey(kingdomName) ?? false;
    }

    public void CollectKingdomKey(string kingdomName)
    {
        CurrentGameData?.CollectKingdomKey(kingdomName);
        SaveGameData();
        if (enableDebugLogs) Debug.Log($"{kingdomName} Key collected!");
    }

    #endregion

    #region Resource Methods

    public void AddNutriGems(int amount)
    {
        if (CurrentGameData == null) return;
        CurrentGameData.AddNutriGems(amount);
        SaveGameData();
        if (enableDebugLogs) Debug.Log($"Added {amount} gems. Total: {CurrentGameData.nutriGems}");
    }

    public bool SpendNutriGems(int amount)
    {
        if (CurrentGameData == null) return false;
        
        bool success = CurrentGameData.SpendNutriGems(amount);
        if (success)
        {
            SaveGameData();
            if (enableDebugLogs) Debug.Log($"Spent {amount} gems. Remaining: {CurrentGameData.nutriGems}");
        }
        return success;
    }

    public int GetNutriGems() => CurrentGameData?.GetNutriGems() ?? 0;

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
        CurrentGameData.equippedPetSlot1 = "";
        CurrentGameData.equippedPetSlot2 = "";
        CurrentGameData.activePowerUps = new List<GameData.PowerUpSaveData>();
        CurrentGameData.passivePowerUps = new List<GameData.PassivePowerUpData>();

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

    public void ResetKingdomKeys()
    {
        if (CurrentGameData == null) return;

        Debug.LogWarning("=== RESETTING KINGDOM KEYS ===");

        CurrentGameData.ResetSugariaKey();
        CurrentGameData.ResetPreserviaKey();
        CurrentGameData.ResetNutriKingdomKey();
        CurrentGameData.ResetAllerthiaKey();
        CurrentGameData.ResetOCRScannerKey();

        SaveGameData();
        Debug.LogWarning("=== KINGDOM KEYS RESET COMPLETE ===");
    }

    #endregion

    #region Debug Options

    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
        Debug.Log($"Debug logs {(enabled ? "enabled" : "disabled")}");
    }

    public void SetAutoSaveOnQuit(bool enabled)
    {
        autoSaveOnQuit = enabled;
        Debug.Log($"Auto save on quit {(enabled ? "enabled" : "disabled")}");
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
        Debug.Log($"Equipped Pets: Slot1='{CurrentGameData.equippedPetSlot1}', Slot2='{CurrentGameData.equippedPetSlot2}'");
        Debug.Log($"Active Power-ups: {CurrentGameData.activePowerUps?.Count ?? 0}");
        Debug.Log($"Passive Power-ups (Heart/Time): {CurrentGameData.passivePowerUps?.Count ?? 0}");
        Debug.Log($"Total Heart Bonus: {CurrentGameData.GetTotalHeartBonus()}");
        Debug.Log($"Total Time Reduction: {CurrentGameData.GetTotalTimeReductionFormatted()}");
        
        Debug.Log($"Kingdom Keys - Sugaria: {CurrentGameData.HasSugariaKey()}, Preservia: {CurrentGameData.HasPreserviaKey()}, Nutri: {CurrentGameData.HasNutriKingdomKey()}, Allerthia: {CurrentGameData.HasAllerthiaKey()}, OCR Scanner: {CurrentGameData.HasOCRScannerKey()}");

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

    [ContextMenu("Debug/Add 1000 Gems")]
    private void DebugAddGems()
    {
        AddNutriGems(1000);
        Debug.Log("Added 1000 gems for debugging");
    }

    [ContextMenu("Debug/Add 1000 Coins")]
    private void DebugAddCoins()
    {
        if (CurrentGameData != null)
        {
            CurrentGameData.nutriCoins += 1000;
            SaveGameData();
            Debug.Log("Added 1000 coins for debugging");
        }
    }

    [ContextMenu("Debug/Reset Energy to Max")]
    private void DebugResetEnergy()
    {
        if (CurrentGameData != null)
        {
            CurrentGameData.currentEnergy = 10;
            CurrentGameData.lastEnergyUpdateTime = DateTime.Now;
            SaveGameData();
            Debug.Log("Energy reset to maximum (10)");
        }
    }

    [ContextMenu("Debug/Make Chest Available")]
    private void DebugMakeChestAvailable()
    {
        if (CurrentGameData != null)
        {
            CurrentGameData.isChestAvailable = true;
            SaveGameData();
            Debug.Log("Chest is now available");
        }
    }

    [ContextMenu("Debug/Collect All Kingdom Keys")]
    private void DebugCollectAllKeys()
    {
        CollectSugariaKey();
        CollectPreserviaKey();
        CollectNutriKingdomKey();
        CollectAllerthiaKey();
        CollectOCRScannerKey();
        Debug.Log("All Kingdom Keys collected!");
    }

    [ContextMenu("Debug/Reset OCR Scanner Key")]
    private void DebugResetOCRScannerKey()
    {
        ResetOCRScannerKey();
        Debug.Log("OCR Scanner Key reset to default (false)");
    }

    [ContextMenu("Debug/Toggle OCR Scanner Key")]
    private void DebugToggleOCRScannerKey()
    {
        if (CurrentGameData != null)
        {
            bool currentState = CurrentGameData.HasOCRScannerKey();
            if (currentState)
            {
                ResetOCRScannerKey();
                Debug.Log("OCR Scanner Key set to false");
            }
            else
            {
                CollectOCRScannerKey();
                Debug.Log("OCR Scanner Key set to true");
            }
        }
    }

    // ========== COMPREHENSIVE RESET METHODS ==========

    [ContextMenu("Debug/Reset EVERYTHING to Default")]
    private void DebugResetEverything()
    {
        Debug.LogWarning("========== RESETTING EVERYTHING TO DEFAULT ==========");
        
        ResetGameData();
        
        Debug.LogWarning("========== EVERYTHING RESET COMPLETE ==========");
    }

    [ContextMenu("Debug/Reset ALL Systems Individually")]
    private void DebugResetAllSystems()
    {
        Debug.LogWarning("========== RESETTING ALL SYSTEMS INDIVIDUALLY ==========");
        
        ResetIconsAndFrames();
        ResetAchievements();
        ResetCharactersAndSkins();
        ResetEnerlings();
        ResetResources();
        ResetKingdomKeys();
        
        Debug.LogWarning("========== ALL SYSTEMS RESET COMPLETE ==========");
    }

    [ContextMenu("Debug/Reset to New Game State")]
    private void DebugResetToNewGame()
    {
        Debug.LogWarning("========== RESETTING TO NEW GAME STATE ==========");
        
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"Deleted save file: {saveFilePath}");
        }
        
        CurrentGameData = new GameData();
        hasInitializedDefaults = false;
        
        InitializeDefaultIconsAndFrames();
        InitializeDefaultCharacters();
        InitializeDefaultSkins();
        
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.SetMusicVolume(CurrentGameData.musicVolume);
            AudioHandler.Instance.SetSoundVolume(CurrentGameData.soundVolume);
        }
        
        SaveGameData();
        
        Debug.LogWarning("========== NEW GAME STATE CREATED ==========");
        Debug.Log("Player has: 0 coins, 0 gems, 10 energy, Character 0 unlocked, Default icons/frames");
    }

    [ContextMenu("Debug/Reset Progress Only (Keep Resources)")]
    private void DebugResetProgressOnly()
    {
        Debug.LogWarning("========== RESETTING PROGRESS ONLY ==========");
        
        int currentCoins = CurrentGameData?.nutriCoins ?? 0;
        int currentGems = CurrentGameData?.nutriGems ?? 0;
        int currentEnergy = CurrentGameData?.currentEnergy ?? 10;
        
        ResetIconsAndFrames();
        ResetAchievements();
        ResetCharactersAndSkins();
        ResetEnerlings();
        ResetKingdomKeys();
        
        if (CurrentGameData != null)
        {
            CurrentGameData.nutriCoins = currentCoins;
            CurrentGameData.nutriGems = currentGems;
            CurrentGameData.currentEnergy = currentEnergy;
            CurrentGameData.lastEnergyUpdateTime = DateTime.Now;
        }
        
        SaveGameData();
        
        Debug.LogWarning("========== PROGRESS RESET COMPLETE ==========");
        Debug.Log($"Resources preserved: {currentCoins} coins, {currentGems} gems, {currentEnergy} energy");
    }

    [ContextMenu("Debug/Reset Collections Only")]
    private void DebugResetCollectionsOnly()
    {
        Debug.LogWarning("========== RESETTING COLLECTIONS ONLY ==========");
        
        if (CurrentGameData == null) return;
        
        CurrentGameData.unlockedIconIds = new List<string>();
        CurrentGameData.unlockedFrameIds = new List<string>();
        CurrentGameData.equippedIconId = "";
        CurrentGameData.equippedFrameId = "";
        
        CurrentGameData.completedAchievementIds = new List<string>();
        CurrentGameData.claimedAchievementIds = new List<string>();
        
        CurrentGameData.unlockedCharacterIDs = new List<int>() { 0 };
        CurrentGameData.selectedCharacterID = 0;
        CurrentGameData.skinData = new List<GameData.SkinSaveData>();
        
        CurrentGameData.unlockedEnerlings = new List<string>();
        CurrentGameData.equippedPetSlot1 = "";
        CurrentGameData.equippedPetSlot2 = "";
        CurrentGameData.activePowerUps = new List<GameData.PowerUpSaveData>();
        CurrentGameData.passivePowerUps = new List<GameData.PassivePowerUpData>();
        
        CurrentGameData.ResetSugariaKey();
        CurrentGameData.ResetPreserviaKey();
        CurrentGameData.ResetAllerthiaKey();
        CurrentGameData.ResetOCRScannerKey();
        
        hasInitializedDefaults = false;
        InitializeDefaultIconsAndFrames();
        InitializeDefaultCharacters();
        InitializeDefaultSkins();
        
        SaveGameData();
        
        Debug.LogWarning("========== COLLECTIONS RESET COMPLETE ==========");
        Debug.Log("All collection items reset to defaults");
    }

    [ContextMenu("Debug/Print Reset Options Help")]
    private void DebugPrintResetHelp()
    {
        Debug.Log("=== RESET OPTIONS AVAILABLE ===");
        Debug.Log("1. Debug/Reset EVERYTHING to Default - Complete wipe and restart");
        Debug.Log("2. Debug/Reset ALL Systems Individually - Calls each system's reset method");
        Debug.Log("3. Debug/Reset to New Game State - Creates brand new save file");
        Debug.Log("4. Debug/Reset Progress Only (Keep Resources) - Resets unlocks but keeps coins/gems");
        Debug.Log("5. Debug/Reset Collections Only - Resets only unlocked items, keeps progress");
        Debug.Log("6. Debug/Reset Icons and Frames - Resets only profile customizations");
        Debug.Log("7. Debug/Reset Achievements - Resets only achievements");
        Debug.Log("8. Debug/Reset Characters and Skins - Resets only characters and skins");
        Debug.Log("9. Debug/Reset Enerlings - Resets only pets and power-ups");
        Debug.Log("10. Debug/Reset Resources - Resets only coins, gems, energy");
        Debug.Log("11. Debug/Reset Kingdom Keys - Resets only kingdom keys");
        Debug.Log("=== END RESET OPTIONS ===");
    }

    [ContextMenu("Debug/Reset Icons and Frames")]
    private void DebugResetIconsAndFramesMenu() => ResetIconsAndFrames();

    [ContextMenu("Debug/Reset Achievements")]
    private void DebugResetAchievementsMenu() => ResetAchievements();

    [ContextMenu("Debug/Reset Characters and Skins")]
    private void DebugResetCharactersAndSkinsMenu() => ResetCharactersAndSkins();

    [ContextMenu("Debug/Reset Enerlings")]
    private void DebugResetEnerlingsMenu() => ResetEnerlings();

    [ContextMenu("Debug/Reset Resources")]
    private void DebugResetResourcesMenu() => ResetResources();

    [ContextMenu("Debug/Reset Kingdom Keys")]
    private void DebugResetKingdomKeysMenu() => ResetKingdomKeys();

    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGameData();
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
            SaveGameData();
    }
}