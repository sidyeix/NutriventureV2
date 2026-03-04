using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameStateSaveData
{
    // Scene identification
    public string currentSceneName;
    public string lastSavedScene = "MainMenu";

    // Player position and rotation
    public Vector3 playerPosition;
    public Quaternion playerRotation;

    // Game state for 3_Kingdom1 (GoGrowGlow game)
    public bool hasSavedGameState = false;

    // GoGrowGlowGameManager state
    public float currentEnergy;
    public int currentScore;
    public float currentLifeAmount;
    public int currentLives;
    public float gameTimer;
    public bool isGameActive;
    public GoGrowGlowGameManager.FoodType currentFoodZone;

    // Kingdom keys and progress
    public bool sugariaKeyCollected;
    public bool preserviaKeyCollected;
    public bool nutriKingdomKeyCollected;
    public bool allerthiaKeyCollected;
    public bool ocrScannerKeyCollected;

    // Torch Minigame progress
    public int litTorchesCount;
    public List<string> litTorchIDs = new List<string>();
    public bool torchMinigameCompleted;

    // Grow Assessment progress
    public int growCorrectAnswers;
    public bool growAssessmentCompleted;
    public bool isWaitingForEndTrigger;

    // Glow Part progress
    public int litTowersCount;
    public List<string> litTowerNames = new List<string>();
    public bool glowPartCompleted;

    // Checkpoint system
    public string currentCheckpointName;
    public bool hasCheckpoint;

    // Timestamp for save
    public DateTime saveTime;

    public GameStateSaveData()
    {
        litTorchIDs = new List<string>();
        litTowerNames = new List<string>();
        saveTime = DateTime.Now;
    }

    // Helper to check if save is from today
    public bool IsTodaysSave()
    {
        return saveTime.Date == DateTime.Now.Date;
    }

    // Get formatted save time
    public string GetFormattedSaveTime()
    {
        if (saveTime.Date == DateTime.Now.Date)
            return $"Today at {saveTime:hh:mm tt}";
        else if (saveTime.Date == DateTime.Now.AddDays(-1).Date)
            return $"Yesterday at {saveTime:hh:mm tt}";
        else
            return saveTime.ToString("MMM dd, yyyy hh:mm tt");
    }
}