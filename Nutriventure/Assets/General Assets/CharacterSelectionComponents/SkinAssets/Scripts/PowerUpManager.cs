using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("References")]
    public IngredientDatabase ingredientDatabase;
    public GameDataManager gameDataManager;
    public Player_Data playerData;

    [Header("Check Interval")]
    public float checkInterval = 1f; // Check every second

    private Coroutine powerUpCheckCoroutine;
    private Dictionary<string, List<ActivePowerUp>> activePowerUps = new Dictionary<string, List<ActivePowerUp>>();

    // Internal class for runtime tracking
    private class ActivePowerUp
    {
        public string petName;
        public int powerUpIndex;
        public IngredientDatabase.PowerUpInfo.PowerUpType type;
        public float cooldownMinutes;
        public int amount;
        public DateTime lastTriggerTime;

        public bool IsReady()
        {
            if (cooldownMinutes <= 0) return false; // No cooldown means one-time or instant
            TimeSpan timeSinceLast = DateTime.Now - lastTriggerTime;
            return timeSinceLast.TotalMinutes >= cooldownMinutes;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (gameDataManager == null)
            gameDataManager = GameDataManager.Instance;

        if (playerData == null)
            playerData = FindObjectOfType<Player_Data>();

        StartPowerUpChecking();
    }

    void OnEnable()
    {
        StartPowerUpChecking();
    }

    void OnDisable()
    {
        StopPowerUpChecking();
    }

    public void StartPowerUpChecking()
    {
        if (powerUpCheckCoroutine == null)
        {
            powerUpCheckCoroutine = StartCoroutine(CheckPowerUpsRoutine());
        }
    }

    public void StopPowerUpChecking()
    {
        if (powerUpCheckCoroutine != null)
        {
            StopCoroutine(powerUpCheckCoroutine);
            powerUpCheckCoroutine = null;
        }
    }

    private IEnumerator CheckPowerUpsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckAndTriggerPowerUps();
        }
    }

    private void CheckAndTriggerPowerUps()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        List<GameData.PowerUpSaveData> activePowerUps = gameDataManager.GetAllActivePowerUps();

        foreach (var powerUpData in activePowerUps)
        {
            // Check if the pet is still equipped
            string slot1Pet = gameDataManager.GetEquippedPet(1);
            string slot2Pet = gameDataManager.GetEquippedPet(2);

            bool isEquipped = (powerUpData.petName == slot1Pet || powerUpData.petName == slot2Pet);

            if (!isEquipped)
            {
                // Pet no longer equipped - remove its power-ups
                gameDataManager.CurrentGameData.RemovePowerUpsForPet(powerUpData.petName);
                continue;
            }

            // Check if enough time has passed
            TimeSpan timeSinceLast = DateTime.Now - powerUpData.lastTriggerTime;
            if (timeSinceLast.TotalMinutes >= powerUpData.cooldownMinutes)
            {
                // Trigger the power-up
                TriggerPowerUp(powerUpData);

                // Update last trigger time
                gameDataManager.UpdatePowerUpTriggerTime(powerUpData.petName, powerUpData.powerUpIndex);
            }
        }
    }

    private void TriggerPowerUp(GameData.PowerUpSaveData powerUpData)
    {
        Debug.Log($"Triggering power-up: {powerUpData.petName} - Type: {powerUpData.powerUpType}, Amount: {powerUpData.amount}");

        switch (powerUpData.powerUpType)
        {
            case IngredientDatabase.PowerUpInfo.PowerUpType.Coins:
                AddCoins(powerUpData.amount);
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Gems:
                AddGems(powerUpData.amount);
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Exp:
                AddExp(powerUpData.amount);
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Time:
                // Time-based power-ups (like slowing time) handled elsewhere
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Heart:
                // Heart/health power-ups handled elsewhere
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Speed:
                // Speed power-ups handled elsewhere
                break;
        }

        // Update UI
        if (playerData != null)
        {
            playerData.ForceUpdateAllUI();
        }
    }

    private void AddCoins(int amount)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        gameDataManager.CurrentGameData.nutriCoins += amount;
        gameDataManager.SaveGameData();

        Debug.Log($"Power-up added {amount} coins. Total: {gameDataManager.CurrentGameData.nutriCoins}");

        // Show floating text or effect
        ShowPowerUpEffect($"+{amount} Coins", Color.yellow);
    }

    private void AddGems(int amount)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        gameDataManager.CurrentGameData.nutriGems += amount;
        gameDataManager.SaveGameData();

        Debug.Log($"Power-up added {amount} gems. Total: {gameDataManager.CurrentGameData.nutriGems}");

        // Show floating text or effect
        ShowPowerUpEffect($"+{amount} Gems", Color.cyan);
    }

    private void AddExp(int amount)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        gameDataManager.CurrentGameData.currentXP += amount;

        // Check for level up
        while (gameDataManager.CurrentGameData.currentXP >= gameDataManager.CurrentGameData.xpToNextLevel)
        {
            gameDataManager.CurrentGameData.currentXP -= gameDataManager.CurrentGameData.xpToNextLevel;
            gameDataManager.CurrentGameData.playerLevel++;
            gameDataManager.CurrentGameData.xpToNextLevel *= 1.5f; // Increase next level requirement

            Debug.Log($"Level up! New level: {gameDataManager.CurrentGameData.playerLevel}");
            ShowPowerUpEffect($"LEVEL UP!", Color.green);
        }

        gameDataManager.SaveGameData();

        Debug.Log($"Power-up added {amount} XP. Current XP: {gameDataManager.CurrentGameData.currentXP}");

        // Show floating text or effect
        ShowPowerUpEffect($"+{amount} XP", Color.magenta);
    }

    private void ShowPowerUpEffect(string message, Color color)
    {
        // You can implement a floating text effect here
        // For now, just log to console
        Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
    }

    // Called when a pet is equipped
    public void RegisterPetPowerUps(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return;

        var ingredient = ingredientDatabase.GetIngredientInfo(petName);
        if (ingredient == null || ingredient.powerUps == null) return;

        // Register each power-up
        for (int i = 0; i < ingredient.powerUps.Count; i++)
        {
            var powerUp = ingredient.powerUps[i];

            // Only register cooldown-based power-ups (Coins, Gems, Exp)
            if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Coins ||
                powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Gems ||
                powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Exp)
            {
                if (powerUp.cooldownMinutes > 0)
                {
                    gameDataManager.RegisterPowerUp(petName, i, powerUp.powerUpType, powerUp.cooldownMinutes, powerUp.amount);
                    Debug.Log($"Registered power-up for {petName}: +{powerUp.amount} {powerUp.powerUpType} every {powerUp.cooldownMinutes}min");
                }
            }
        }
    }

    // Called when a pet is unequipped
    public void UnregisterPetPowerUps(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return;

        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.RemovePowerUpsForPet(petName);
            gameDataManager.SaveGameData();
            Debug.Log($"Unregistered all power-ups for {petName}");
        }
    }

    // Get remaining time for a specific power-up
    public TimeSpan GetRemainingTime(string petName, int powerUpIndex)
    {
        if (gameDataManager == null)
            return TimeSpan.Zero;

        return gameDataManager.GetPowerUpTimeRemaining(petName, powerUpIndex);
    }

    // Get formatted remaining time string
    public string GetRemainingTimeString(string petName, int powerUpIndex)
    {
        TimeSpan remaining = GetRemainingTime(petName, powerUpIndex);

        if (remaining.TotalSeconds <= 0)
            return "Ready";

        if (remaining.TotalHours >= 1)
            return $"{remaining.Hours}h {remaining.Minutes}m";
        else if (remaining.TotalMinutes >= 1)
            return $"{remaining.Minutes}m {remaining.Seconds}s";
        else
            return $"{remaining.Seconds}s";
    }
}