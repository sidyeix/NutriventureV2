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

    // Player armature rotation (separate from player root)
    public Quaternion playerArmatureRotation;
    public Vector3 playerArmatureScale = Vector3.one;

    // Game state for 3_Kingdom1 (GoGrowGlow game)
    public bool hasSavedGameState = false;

    // GoGrowGlowGameManager state
    public float currentEnergy;
    public float targetEnergy;
    public int currentScore;
    public float currentLifeAmount;
    public int currentLives;
    public float gameTimer;
    public bool isGameActive;
    public GoGrowGlowGameManager.FoodType currentFoodZone;
    public bool isEnergyDecreasePaused;
    public bool isGameTimerPaused;

    // Boost states
    public bool isSpeedBoosted;
    public float speedBoostTimer;
    public bool isSizeBoosted;
    public float sizeBoostTimer;

    // Player speed/size
    public float playerSpeed;
    public float playerSize;

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

    // Per-tower energy levels (parallel lists since JsonUtility doesn't support Dictionary)
    public List<string> towerEnergyNames = new List<string>();
    public List<float> towerEnergyValues = new List<float>();

    // Checkpoint system
    public string currentCheckpointName;
    public bool hasCheckpoint;

    // Activated checkpoints (so we don't lose checkpoint progress)
    public List<string> activatedCheckpointNames = new List<string>();

    // Timestamp for save (stored as ticks for JSON serialization reliability)
    public long saveTimeTicks;

    [System.NonSerialized]
    private DateTime? _saveTime;

    public DateTime saveTime
    {
        get
        {
            if (_saveTime == null)
                _saveTime = new DateTime(saveTimeTicks);
            return _saveTime.Value;
        }
        set
        {
            _saveTime = value;
            saveTimeTicks = value.Ticks;
        }
    }

    public GameStateSaveData()
    {
        litTorchIDs = new List<string>();
        litTowerNames = new List<string>();
        towerEnergyNames = new List<string>();
        towerEnergyValues = new List<float>();
        activatedCheckpointNames = new List<string>();
        saveTime = DateTime.Now;
        playerArmatureScale = Vector3.one;
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