using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class GameDataManager1 : MonoBehaviour
{
    public static GameDataManager1 Instance { get; private set; }
    
    [System.Serializable]
    public class GameData
    {
        public bool hasScroll = false;
        public string[] collectedAllergens = new string[0];
        public bool hasKey = false;
        public int score = 0;
        public float playTime = 0f;
        public string lastPlayedDate;
        
        // Kingdom 4 specific data
        public bool kingdom4Completed = false;
        public int kingdom4Score = 0;
        public int allergensFound = 0;
        public int wagonHits = 0;
        public int maxCombo = 0;
    }
    
    public GameData currentGameData = new GameData();
    
    private string savePath;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Initialize()
    {
        savePath = Application.persistentDataPath + "/gameData.save";
        Debug.Log("Save path: " + savePath);
    }
    
    public void SaveGameProgress()
    {
        try
        {
            // Update data before saving
            UpdateCurrentData();
            
            // Create formatter
            BinaryFormatter formatter = new BinaryFormatter();
            
            // Create file stream
            FileStream stream = new FileStream(savePath, FileMode.Create);
            
            // Serialize data
            formatter.Serialize(stream, currentGameData);
            
            // Close stream
            stream.Close();
            
            Debug.Log("Game saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
        }
    }
    
    public void LoadGameProgress()
    {
        if (File.Exists(savePath))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(savePath, FileMode.Open);
                
                currentGameData = formatter.Deserialize(stream) as GameData;
                stream.Close();
                
                Debug.Log("Game loaded successfully!");
                ApplyLoadedData();
            }
            catch (Exception e)
            {
                Debug.LogError("Load failed: " + e.Message);
                currentGameData = new GameData(); // Reset to default
            }
        }
        else
        {
            Debug.Log("No save file found. Starting new game.");
            currentGameData = new GameData();
        }
    }
    
    private void UpdateCurrentData()
    {
        // Get data from game managers
        if (AllerthriaGameManager.Instance != null)
        {
            currentGameData.hasScroll = AllerthriaGameManager.Instance.hasScroll;
            currentGameData.hasKey = AllerthriaGameManager.Instance.hasKey;
            currentGameData.collectedAllergens = AllerthriaGameManager.Instance.collectedAllergens.ToArray();
        }
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            currentGameData.kingdom4Score = Kingdom4ScoreManager.Instance.GetFinalScore();
            currentGameData.allergensFound = Kingdom4ScoreManager.Instance.allergensFound;
            currentGameData.wagonHits = Kingdom4ScoreManager.Instance.totalWagonHits;
            
            // Mark kingdom as completed if player has key
            if (AllerthriaGameManager.Instance != null && AllerthriaGameManager.Instance.hasKey)
            {
                currentGameData.kingdom4Completed = true;
            }
        }
        
        // Update timestamp
        currentGameData.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    private void ApplyLoadedData()
    {
        // Apply loaded data to game managers
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.hasScroll = currentGameData.hasScroll;
            AllerthriaGameManager.Instance.hasKey = currentGameData.hasKey;
            AllerthriaGameManager.Instance.collectedAllergens = new System.Collections.Generic.List<string>(currentGameData.collectedAllergens);
            
            // Update game phase based on loaded data
            if (currentGameData.hasKey)
            {
                AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
            }
            else if (currentGameData.hasScroll)
            {
                AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.AllergenHunt);
            }
        }
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            // Kingdom4ScoreManager will update its own values when needed
        }
    }
    
    public void SaveKingdom4Progress()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            currentGameData.kingdom4Score = Kingdom4ScoreManager.Instance.GetFinalScore();
            currentGameData.allergensFound = Kingdom4ScoreManager.Instance.allergensFound;
            currentGameData.wagonHits = Kingdom4ScoreManager.Instance.totalWagonHits;
            
            SaveGameProgress();
            Debug.Log("Kingdom 4 progress saved!");
        }
    }
    
    public void SaveAllerthriaProgress()
    {
        if (AllerthriaGameManager.Instance != null)
        {
            UpdateCurrentData();
            SaveGameProgress();
            Debug.Log("Allerthria progress saved!");
        }
    }
    
    public void ResetGameData()
    {
        currentGameData = new GameData();
        
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        
        Debug.Log("Game data reset!");
    }
    
    public void DeleteSaveFile()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            currentGameData = new GameData();
            Debug.Log("Save file deleted!");
        }
    }
    
    // For testing in editor
    [ContextMenu("Save Game")]
    public void TestSave()
    {
        SaveGameProgress();
    }
    
    [ContextMenu("Load Game")]
    public void TestLoad()
    {
        LoadGameProgress();
    }
    
    [ContextMenu("Reset Save")]
    public void TestReset()
    {
        ResetGameData();
    }
    
    [ContextMenu("Print Save Info")]
    public void PrintSaveInfo()
    {
        Debug.Log("=== SAVE DATA INFO ===");
        Debug.Log("Has Scroll: " + currentGameData.hasScroll);
        Debug.Log("Has Key: " + currentGameData.hasKey);
        Debug.Log("Allergens Collected: " + currentGameData.collectedAllergens.Length);
        Debug.Log("Kingdom 4 Score: " + currentGameData.kingdom4Score);
        Debug.Log("Kingdom 4 Completed: " + currentGameData.kingdom4Completed);
        Debug.Log("Save Path: " + savePath);
        Debug.Log("File Exists: " + File.Exists(savePath));
    }
}