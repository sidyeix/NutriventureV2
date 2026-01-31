using UnityEngine;
using System.IO;
using System.Reflection;

public class GameDataResetter : MonoBehaviour
{
    private string saveFilePath;
    
    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "nutriventure_save.json");
    }
    
    [ContextMenu("Reset Game Data")]
    public void ResetGameData()
    {
        Debug.Log("=== RESETTING GAME DATA ===");
        
        // Method 1: Delete the save file
        DeleteSaveFile();
        
        // Method 2: Call GameDataManager's ResetGameData method
        ResetGameDataManager();
        
        // Method 3: Clear PlayerPrefs
        ClearPlayerPrefs();
        
        Debug.Log("=== GAME DATA RESET COMPLETE ===");
        Debug.Log("Restart the game or reload scene to see fresh data.");
    }
    
    private void DeleteSaveFile()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
                Debug.Log($"✅ Save file deleted: {saveFilePath}");
                
                // Delete backup file if exists
                string backupFile = saveFilePath + ".bak";
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                    Debug.Log($"✅ Backup file deleted: {backupFile}");
                }
            }
            else
            {
                Debug.Log($"📭 No save file found at: {saveFilePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to delete save file: {e.Message}");
        }
    }
    
    private void ResetGameDataManager()
    {
        if (GameDataManager.Instance != null)
        {
            Debug.Log("🔄 Calling GameDataManager.ResetGameData()...");
            
            // Use the public ResetGameData method that already exists
            GameDataManager.Instance.ResetGameData();
            
            // Force save to create fresh save file
            GameDataManager.Instance.SaveGameData();
            
            Debug.Log("✅ GameDataManager reset and saved");
        }
        else
        {
            Debug.LogWarning("⚠️ GameDataManager.Instance is null");
            
            // Try to find it in the scene
            GameDataManager manager = FindObjectOfType<GameDataManager>();
            if (manager != null)
            {
                Debug.Log("🔍 Found GameDataManager in scene, resetting...");
                manager.ResetGameData();
                manager.SaveGameData();
                Debug.Log("✅ Found and reset GameDataManager");
            }
            else
            {
                Debug.LogError("❌ Could not find GameDataManager in the scene!");
            }
        }
    }
    
    private void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ PlayerPrefs cleared");
    }
    
    [ContextMenu("Show Current Game Data")]
    public void ShowCurrentGameData()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameData data = GameDataManager.Instance.CurrentGameData;
            
            Debug.Log("=== CURRENT GAME DATA IN MEMORY ===");
            Debug.Log($"Player: {data.playerName} (Level {data.playerLevel})");
            Debug.Log($"Coins: {data.nutriCoins}, Gems: {data.nutriGems}");
            Debug.Log($"Energy: {data.currentEnergy}/10");
            Debug.Log($"Selected Character: {data.selectedCharacterID}");
            Debug.Log($"Unlocked Characters: {data.unlockedCharacterIDs?.Count ?? 0}");
            
            // Check save file
            if (File.Exists(saveFilePath))
            {
                FileInfo info = new FileInfo(saveFilePath);
                Debug.Log($"Save File: {info.Length} bytes, modified {info.LastWriteTime}");
            }
            else
            {
                Debug.Log("Save File: Does not exist");
            }
            Debug.Log("=====================================");
        }
        else
        {
            Debug.Log("GameDataManager.Instance or CurrentGameData is null");
        }
    }
    
    [ContextMenu("Delete Save File Only")]
    public void DeleteSaveFileOnly()
    {
        DeleteSaveFile();
        Debug.Log("Only the save file was deleted. GameDataManager data is still in memory.");
    }
    
    [ContextMenu("Reset GameDataManager Only")]
    public void ResetGameDataManagerOnly()
    {
        ResetGameDataManager();
        Debug.Log("Only GameDataManager was reset. Save file may still exist.");
    }
    
    [ContextMenu("Nuclear: Delete & Reload Scene")]
    public void NuclearResetWithSceneReload()
    {
        Debug.Log("⚠️ NUCLEAR RESET - Deleting save and forcing scene reload");
        
        // Delete save file
        DeleteSaveFile();
        
        // Clear PlayerPrefs
        ClearPlayerPrefs();
        
        // Reset GameDataManager if exists
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetGameData();
        }
        
        Debug.Log("✅ Nuclear reset complete!");
        Debug.Log("MANUALLY reload the scene to see fresh data.");
    }
    
    [ContextMenu("Create Fresh Save File")]
    public void CreateFreshSaveFile()
    {
        try
        {
            Debug.Log("Creating fresh save file...");
            
            // Option 1: Use reflection to create new GameData and force save
            if (GameDataManager.Instance != null)
            {
                // Reset the GameDataManager first
                GameDataManager.Instance.ResetGameData();
                
                // Save it
                GameDataManager.Instance.SaveGameData();
                
                Debug.Log("✅ Fresh save created via GameDataManager");
                ShowCurrentGameData();
            }
            else
            {
                // Option 2: Create save file directly
                GameData freshData = new GameData();
                string jsonData = JsonUtility.ToJson(freshData, true);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(saveFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write to file
                File.WriteAllText(saveFilePath, jsonData);
                
                Debug.Log($"✅ Fresh save file created at: {saveFilePath}");
                Debug.Log($"Default player: {freshData.playerName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create fresh save: {e.Message}");
        }
    }
}