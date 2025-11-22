using UnityEngine;
using System.IO;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;
    
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
        
        // Update energy based on time passed
        UpdateEnergyBasedOnTime();
    }
    
    private void CreateNewGameData()
    {
        CurrentGameData = new GameData();
        SaveGameData();
    }
    
    public void ResetGameData()
    {
        CreateNewGameData();
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