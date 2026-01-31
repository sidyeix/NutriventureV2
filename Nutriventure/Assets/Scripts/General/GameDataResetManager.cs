using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class GameDataResetManager : MonoBehaviour
{
    [Header("UI References")]
    public Button resetSugariaButton;
    public Button resetPreserviaButton;
    public Button resetAllKeysButton;
    public Button resetAllGameDataButton;
    public Button viewSaveFileButton;
    
    [Header("Confirmation UI")]
    public GameObject confirmationPanel;
    public TextMeshProUGUI confirmationText;
    public Button confirmButton;
    public Button cancelButton;
    
    [Header("Save File Info")]
    public TextMeshProUGUI saveFileInfoText;
    public bool autoRefreshSaveInfo = true;
    public float saveInfoRefreshRate = 5f;
    
    [Header("Notification UI")]
    public GameObject notificationPanel;
    public TextMeshProUGUI notificationTitle;
    public TextMeshProUGUI notificationMessage;
    public float notificationDuration = 3f;
    public enum ResetType
    {
        SugariaKey,
        PreserviaKey,
        AllKeys,
        AllGameData
    }
    
    private ResetType pendingResetType;
    private string saveFilePath;
    private Coroutine refreshCoroutine;
    
    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "nutriventure_save.json");
        Debug.Log($"Save file path: {saveFilePath}");
        
        InitializeButtons();
        InitializeConfirmationPanel();
        InitializeNotificationPanel();
        
        if (autoRefreshSaveInfo)
        {
            refreshCoroutine = StartCoroutine(RefreshSaveInfo());
        }
        
        UpdateSaveFileInfo();
        Debug.Log("GameDataResetManager initialized");
    }
    
    void InitializeButtons()
    {
        // Safely add listeners with null checks
        if (resetSugariaButton != null)
        {
            resetSugariaButton.onClick.RemoveAllListeners();
            resetSugariaButton.onClick.AddListener(() => ShowConfirmation(ResetType.SugariaKey));
        }
        
        if (resetPreserviaButton != null)
        {
            resetPreserviaButton.onClick.RemoveAllListeners();
            resetPreserviaButton.onClick.AddListener(() => ShowConfirmation(ResetType.PreserviaKey));
        }
        
        if (resetAllKeysButton != null)
        {
            resetAllKeysButton.onClick.RemoveAllListeners();
            resetAllKeysButton.onClick.AddListener(() => ShowConfirmation(ResetType.AllKeys));
        }
        
        if (resetAllGameDataButton != null)
        {
            resetAllGameDataButton.onClick.RemoveAllListeners();
            resetAllGameDataButton.onClick.AddListener(() => ShowConfirmation(ResetType.AllGameData));
        }
        
        if (viewSaveFileButton != null)
        {
            viewSaveFileButton.onClick.RemoveAllListeners();
            viewSaveFileButton.onClick.AddListener(ViewSaveFileContents);
        }
    }
    
    void InitializeConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
            
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmReset);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(CancelReset);
            }
        }
    }
    
    void InitializeNotificationPanel()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    System.Collections.IEnumerator RefreshSaveInfo()
    {
        while (true)
        {
            yield return new WaitForSeconds(saveInfoRefreshRate);
            UpdateSaveFileInfo();
        }
    }
    
    void ShowConfirmation(ResetType resetType)
    {
        pendingResetType = resetType;
        
        if (confirmationPanel == null)
        {
            Debug.LogWarning("No confirmation panel found. Performing reset immediately.");
            PerformReset(resetType);
            return;
        }
        
        // Set confirmation message
        string message = GetConfirmationMessage(resetType);
        if (confirmationText != null)
            confirmationText.text = message;
        
        confirmationPanel.SetActive(true);
    }
    
    string GetConfirmationMessage(ResetType resetType)
    {
        return resetType switch
        {
            ResetType.SugariaKey => "Are you sure you want to reset the Sugaria Key?\n\nThis will allow you to collect it again in Kingdom 2.\n\nNote: This resets the PERSISTENT save file.",
            ResetType.PreserviaKey => "Are you sure you want to reset the Preservia Key?\n\nThis will allow you to collect it again in Kingdom 3.\n\nNote: This resets the PERSISTENT save file.",
            ResetType.AllKeys => "Are you sure you want to reset ALL Kingdom Keys?\n\nThis will reset both Sugaria and Preservia keys in the persistent save file.",
            ResetType.AllGameData => "⚠️ WARNING: Are you sure you want to reset ALL game data?\n\nThis will delete the entire save file including progress, coins, characters, and settings!\n\nThis action cannot be undone.",
            _ => "Are you sure?"
        };
    }
    
    void ConfirmReset()
    {
        PerformReset(pendingResetType);
        HideConfirmation();
    }
    
    void CancelReset()
    {
        HideConfirmation();
    }
    
    void HideConfirmation()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
    
    void PerformReset(ResetType resetType)
    {
        Debug.Log($"Attempting to perform reset: {resetType}");
        
        try
        {
            switch (resetType)
            {
                case ResetType.SugariaKey:
                    ForceResetSugariaKey();
                    break;
                    
                case ResetType.PreserviaKey:
                    ForceResetPreserviaKey();
                    break;
                    
                case ResetType.AllKeys:
                    ForceResetAllKeys();
                    break;
                    
                case ResetType.AllGameData:
                    ForceResetAllGameData();
                    break;
            }
            
            Debug.Log($"Reset completed: {resetType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during reset: {e.Message}");
            ShowNotification("Reset Failed", $"Error: {e.Message}");
        }
    }
    
    #region Force Reset Methods
    
    void ForceResetSugariaKey()
    {
        Debug.Log("=== FORCE RESET SUGARIA KEY ===");
        
        bool success = false;
        string message = "";
        
        // Method 1: Direct file manipulation (MOST RELIABLE)
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                
                // Direct string manipulation to set sugariaKeyCollected to false
                if (jsonData.Contains("\"sugariaKeyCollected\":true"))
                {
                    jsonData = jsonData.Replace("\"sugariaKeyCollected\":true", "\"sugariaKeyCollected\":false");
                    File.WriteAllText(saveFilePath, jsonData);
                    success = true;
                    message = "✓ Sugaria Key reset in save file!";
                    Debug.Log(message);
                }
                else if (jsonData.Contains("\"sugariaKeyCollected\":false"))
                {
                    success = true;
                    message = "✓ Sugaria Key already false in save file";
                    Debug.Log(message);
                }
                else
                {
                    message = "⚠ Could not find sugariaKeyCollected in save file";
                    Debug.Log(message);
                }
            }
            catch (System.Exception e)
            {
                message = $"✗ Error reading/writing save file: {e.Message}";
                Debug.LogError(message);
            }
        }
        else
        {
            message = "✗ Save file does not exist";
            Debug.Log(message);
        }
        
        // Method 2: Try through GameDataManager (for current session)
        try
        {
            GameDataManager manager = FindObjectOfType<GameDataManager>();
            if (manager != null && manager.CurrentGameData != null)
            {
                manager.CurrentGameData.ResetSugariaKey();
                manager.SaveGameData();
                Debug.Log("✓ Sugaria Key also reset via GameDataManager!");
                success = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not reset via GameDataManager: {e.Message}");
        }
        
        // Reset static flags
        K2_CollectKey.GlobalResetAllKeys();
        Debug.Log("✓ Static flags reset");
        
        UpdateSaveFileInfo();
        CheckKeyStatus();
        
        if (success)
        {
            ShowNotification("Sugaria Key Reset", "Sugaria Key has been reset in persistent save file.\n\nYou can now collect it again in Kingdom 2.\n\nNote: You may need to restart the scene.");
        }
        else
        {
            ShowNotification("Reset Failed", message);
        }
    }
    
    void ForceResetPreserviaKey()
    {
        Debug.Log("=== FORCE RESET PRESERVIA KEY ===");
        
        bool success = false;
        string message = "";
        
        // Method 1: Direct file manipulation
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                
                // Direct string manipulation to set preserviaKeyCollected to false
                if (jsonData.Contains("\"preserviaKeyCollected\":true"))
                {
                    jsonData = jsonData.Replace("\"preserviaKeyCollected\":true", "\"preserviaKeyCollected\":false");
                    File.WriteAllText(saveFilePath, jsonData);
                    success = true;
                    message = "✓ Preservia Key reset in save file!";
                    Debug.Log(message);
                }
                else if (jsonData.Contains("\"preserviaKeyCollected\":false"))
                {
                    success = true;
                    message = "✓ Preservia Key already false in save file";
                    Debug.Log(message);
                }
                else
                {
                    message = "⚠ Could not find preserviaKeyCollected in save file";
                    Debug.Log(message);
                    
                    // Try to add it if it doesn't exist
                    if (jsonData.Contains("\"sugariaKeyCollected\":"))
                    {
                        // Insert after sugariaKeyCollected
                        int insertPos = jsonData.IndexOf("\"sugariaKeyCollected\":") + 1;
                        jsonData = jsonData.Insert(jsonData.IndexOf(",", insertPos), ",\"preserviaKeyCollected\":false");
                        File.WriteAllText(saveFilePath, jsonData);
                        message = "✓ Added preserviaKeyCollected field to save file";
                        success = true;
                        Debug.Log(message);
                    }
                }
            }
            catch (System.Exception e)
            {
                message = $"✗ Error reading/writing save file: {e.Message}";
                Debug.LogError(message);
            }
        }
        else
        {
            message = "✗ Save file does not exist";
            Debug.Log(message);
        }
        
        // Method 2: Try through GameDataManager
        try
        {
            GameDataManager manager = FindObjectOfType<GameDataManager>();
            if (manager != null && manager.CurrentGameData != null)
            {
                manager.CurrentGameData.ResetPreserviaKey();
                manager.SaveGameData();
                Debug.Log("✓ Preservia Key also reset via GameDataManager!");
                success = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not reset via GameDataManager: {e.Message}");
        }
        
        // Reset static flags
        K3_CollectKey.GlobalResetAllKeys();
        Debug.Log("✓ Static flags reset");
        
        UpdateSaveFileInfo();
        CheckKeyStatus();
        
        if (success)
        {
            ShowNotification("Preservia Key Reset", "Preservia Key has been reset in persistent save file.\n\nYou can now collect it again in Kingdom 3.\n\nNote: You may need to restart the scene.");
        }
        else
        {
            ShowNotification("Reset Failed", message);
        }
    }
    
    void ForceResetAllKeys()
    {
        Debug.Log("=== FORCE RESET ALL KEYS ===");
        
        bool success = false;
        string message = "";
        
        // Method 1: Direct file manipulation
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                bool changed = false;
                
                // Reset Sugaria Key
                if (jsonData.Contains("\"sugariaKeyCollected\":true"))
                {
                    jsonData = jsonData.Replace("\"sugariaKeyCollected\":true", "\"sugariaKeyCollected\":false");
                    changed = true;
                    Debug.Log("Reset sugariaKeyCollected to false");
                }
                
                // Reset Preservia Key
                if (jsonData.Contains("\"preserviaKeyCollected\":true"))
                {
                    jsonData = jsonData.Replace("\"preserviaKeyCollected\":true", "\"preserviaKeyCollected\":false");
                    changed = true;
                    Debug.Log("Reset preserviaKeyCollected to false");
                }
                
                // If preserviaKeyCollected doesn't exist, add it
                if (!jsonData.Contains("\"preserviaKeyCollected\":"))
                {
                    if (jsonData.Contains("\"sugariaKeyCollected\":"))
                    {
                        int insertPos = jsonData.IndexOf("\"sugariaKeyCollected\":") + 1;
                        jsonData = jsonData.Insert(jsonData.IndexOf(",", insertPos), ",\"preserviaKeyCollected\":false");
                        changed = true;
                        Debug.Log("Added preserviaKeyCollected field");
                    }
                }
                
                if (changed)
                {
                    File.WriteAllText(saveFilePath, jsonData);
                    success = true;
                    message = "✓ All keys reset in save file!";
                    Debug.Log(message);
                }
                else
                {
                    success = true;
                    message = "✓ All keys already false in save file";
                    Debug.Log(message);
                }
            }
            catch (System.Exception e)
            {
                message = $"✗ Error reading/writing save file: {e.Message}";
                Debug.LogError(message);
            }
        }
        else
        {
            message = "✗ Save file does not exist";
            Debug.Log(message);
        }
        
        // Method 2: Try through GameDataManager
        try
        {
            GameDataManager manager = FindObjectOfType<GameDataManager>();
            if (manager != null && manager.CurrentGameData != null)
            {
                manager.CurrentGameData.ResetSugariaKey();
                manager.CurrentGameData.ResetPreserviaKey();
                manager.SaveGameData();
                Debug.Log("✓ All keys also reset via GameDataManager!");
                success = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not reset via GameDataManager: {e.Message}");
        }
        
        // Reset static flags
        K2_CollectKey.GlobalResetAllKeys();
        K3_CollectKey.GlobalResetAllKeys();
        Debug.Log("✓ Static flags reset");
        
        UpdateSaveFileInfo();
        CheckKeyStatus();
        
        if (success)
        {
            ShowNotification("All Keys Reset", "All kingdom keys have been reset in persistent save file.\n\nYou can now collect them again.\n\nNote: You may need to restart the scenes.");
        }
        else
        {
            ShowNotification("Reset Failed", message);
        }
    }
    
    void ForceResetAllGameData()
    {
        Debug.Log("=== FORCE RESET ALL GAME DATA ===");
        
        bool success = false;
        string message = "";
        
        // Method 1: Delete the save file
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                Debug.Log($"✓ Save file deleted: {saveFilePath}");
                
                // Also delete meta file if it exists
                string metaPath = saveFilePath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                    Debug.Log($"✓ Meta file deleted: {metaPath}");
                }
                
                success = true;
                message = "✓ Save file deleted! Game will create new save on next load.";
            }
            catch (System.Exception e)
            {
                message = $"✗ Error deleting save file: {e.Message}";
                Debug.LogError(message);
            }
        }
        else
        {
            message = "✗ Save file does not exist";
            Debug.Log(message);
        }
        
        // Method 2: Create GameDataManager and reset
        try
        {
            GameDataManager manager = FindObjectOfType<GameDataManager>();
            if (manager == null)
            {
                GameObject go = new GameObject("TempGameDataManager");
                manager = go.AddComponent<GameDataManager>();
                Debug.Log("Created temporary GameDataManager");
            }
            
            manager.ResetGameData();
            manager.SaveGameData();
            Debug.Log("✓ New save file created with default values");
            success = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not create new GameDataManager: {e.Message}");
        }
        
        // Reset static flags
        K2_CollectKey.GlobalResetAllKeys();
        K3_CollectKey.GlobalResetAllKeys();
        Debug.Log("✓ Static flags reset");
        
        UpdateSaveFileInfo();
        
        if (success)
        {
            ShowNotification("All Data Reset", "All game data has been reset to defaults.\n\nA new save file will be created with default values.\n\nYou need to restart the game for changes to fully take effect.");
        }
        else
        {
            ShowNotification("Reset Failed", message);
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    void UpdateSaveFileInfo()
    {
        if (saveFileInfoText == null) return;
        
        try
        {
            if (File.Exists(saveFilePath))
            {
                FileInfo fileInfo = new FileInfo(saveFilePath);
                string jsonData = File.ReadAllText(saveFilePath);
                
                bool hasSugaria = jsonData.Contains("\"sugariaKeyCollected\":true");
                bool hasPreservia = jsonData.Contains("\"preserviaKeyCollected\":true");
                
                string info = $"Save File: EXISTS\n";
                info += $"Size: {fileInfo.Length} bytes\n";
                info += $"Modified: {fileInfo.LastWriteTime:g}\n";
                info += $"Sugaria Key: {(hasSugaria ? "COLLECTED" : "Available")}\n";
                info += $"Preservia Key: {(hasPreservia ? "COLLECTED" : "Available")}";
                
                saveFileInfoText.text = info;
            }
            else
            {
                saveFileInfoText.text = "Save File: DOES NOT EXIST\nNo persistent data found";
            }
        }
        catch (Exception e)
        {
            saveFileInfoText.text = $"Error reading save file: {e.Message}";
        }
    }
    
    void CheckKeyStatus()
    {
        Debug.Log("=== CURRENT KEY STATUS ===");
        
        // Check save file first
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                bool hasSugaria = jsonData.Contains("\"sugariaKeyCollected\":true");
                bool hasPreservia = jsonData.Contains("\"preserviaKeyCollected\":true");
                
                Debug.Log($"Save File - Sugaria Key: {hasSugaria}");
                Debug.Log($"Save File - Preservia Key: {hasPreservia}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading save file: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Save file does not exist");
        }
        
        // Check GameDataManager
        GameDataManager manager = FindObjectOfType<GameDataManager>();
        if (manager != null)
        {
            if (manager.CurrentGameData != null)
            {
                Debug.Log($"GameDataManager - Sugaria Key: {manager.CurrentGameData.HasSugariaKey()}");
                Debug.Log($"GameDataManager - Preservia Key: {manager.CurrentGameData.HasPreserviaKey()}");
            }
            else
            {
                Debug.Log("CurrentGameData is null");
            }
        }
        else
        {
            Debug.Log("GameDataManager not found in scene");
        }
        
        Debug.Log("==========================");
    }
    
    void ViewSaveFileContents()
    {
        Debug.Log("=== VIEW SAVE FILE CONTENTS ===");
        
        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                Debug.Log("Save File Contents:");
                Debug.Log(jsonData);
                
                // Try to format it for better readability
                try
                {
                    var parsed = JsonUtility.FromJson<GameData>(jsonData);
                    Debug.Log("Parsed GameData successfully");
                    Debug.Log($"SugariaKey: {parsed.sugariaKeyCollected}");
                    Debug.Log($"PreserviaKey: {parsed.preserviaKeyCollected}");
                }
                catch
                {
                    Debug.Log("Could not parse as GameData, showing raw JSON");
                }
                
                ShowNotification("Save File", "Save file contents logged to Console.\nCheck Console for details.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading save file: {e.Message}");
                ShowNotification("Error", $"Could not read save file: {e.Message}");
            }
        }
        else
        {
            Debug.Log("Save file does not exist");
            ShowNotification("No Save File", "Save file does not exist yet.");
        }
    }
    
    void ShowNotification(string title, string message)
    {
        if (notificationPanel == null)
        {
            Debug.Log($"[{title}] {message}");
            return;
        }
        
        if (notificationTitle != null)
            notificationTitle.text = title;
        
        if (notificationMessage != null)
            notificationMessage.text = message;
        
        notificationPanel.SetActive(true);
        
        // Auto-hide after duration
        StartCoroutine(HideNotificationAfterDelay(notificationDuration));
    }
    
    System.Collections.IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    #endregion
    
    #region Context Menu Methods (for easy access in Editor)
    
    [ContextMenu("Reset Sugaria Key")]
    public void ResetSugariaKeyFromMenu()
    {
        ForceResetSugariaKey();
    }
    
    [ContextMenu("Reset Preservia Key")]
    public void ResetPreserviaKeyFromMenu()
    {
        ForceResetPreserviaKey();
    }
    
    [ContextMenu("Reset All Keys")]
    public void ResetAllKeysFromMenu()
    {
        ForceResetAllKeys();
    }
    
    [ContextMenu("Reset All Game Data")]
    public void ResetAllGameDataFromMenu()
    {
        ForceResetAllGameData();
    }
    
    [ContextMenu("Check Key Status")]
    public void CheckKeyStatusFromMenu()
    {
        CheckKeyStatus();
    }
    
    [ContextMenu("View Save File")]
    public void ViewSaveFileFromMenu()
    {
        ViewSaveFileContents();
    }
    
    [ContextMenu("Print Save Location")]
    public void PrintSaveLocationFromMenu()
    {
        Debug.Log($"Save file location: {saveFilePath}");
        Debug.Log($"Persistent data path: {Application.persistentDataPath}");
        
        #if UNITY_EDITOR
        GUIUtility.systemCopyBuffer = saveFilePath;
        Debug.Log("Path copied to clipboard!");
        #endif
    }
    
    [ContextMenu("Open Save Folder")]
    public void OpenSaveFolder()
    {
        string folderPath = Application.persistentDataPath;
        
        if (Directory.Exists(folderPath))
        {
            Debug.Log($"Opening folder: {folderPath}");
            
            #if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath.Replace("/", "\\"));
            #elif UNITY_EDITOR_OSX
            System.Diagnostics.Process.Start("open", folderPath);
            #elif UNITY_EDITOR_LINUX
            System.Diagnostics.Process.Start("xdg-open", folderPath);
            #else
            Debug.Log($"Save folder: {folderPath}");
            #endif
        }
        else
        {
            Debug.Log($"Folder does not exist: {folderPath}");
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }
        
        // Remove button listeners
        if (resetSugariaButton != null)
            resetSugariaButton.onClick.RemoveAllListeners();
        
        if (resetPreserviaButton != null)
            resetPreserviaButton.onClick.RemoveAllListeners();
        
        if (resetAllKeysButton != null)
            resetAllKeysButton.onClick.RemoveAllListeners();
        
        if (resetAllGameDataButton != null)
            resetAllGameDataButton.onClick.RemoveAllListeners();
        
        if (viewSaveFileButton != null)
            viewSaveFileButton.onClick.RemoveAllListeners();
        
        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();
        
        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
    }
}

// Add this simple class to help parse the save file
[System.Serializable]
public class SimpleGameData
{
    public string playerName;
    public int playerLevel;
    public float currentXP;
    public float xpToNextLevel;
    public int nutriCoins;
    public int nutriGems;
    public int currentEnergy;
    public bool sugariaKeyCollected = false;
    public bool preserviaKeyCollected = false;
    // Add other fields as needed
}