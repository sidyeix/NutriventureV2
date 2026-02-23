using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("References")]
    public IngredientDatabase ingredientDatabase;
    public GameDataManager gameDataManager;
    public Player_Data playerData;

    [Header("Check Interval")]
    public float checkInterval = 1f; // Check every second

    [Header("Reward Feedback UI - COINS")]
    [SerializeField] private GameObject coinRewardFeedbackPrefab;
    [SerializeField] private RectTransform coinRewardSpawnPoint;

    [Header("Reward Feedback UI - GEMS")]
    [SerializeField] private GameObject gemRewardFeedbackPrefab;
    [SerializeField] private RectTransform gemRewardSpawnPoint;

    [Header("Animation Settings")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private float feedbackSlideDuration = 0.5f;
    [SerializeField] private float feedbackFadeOutDuration = 0.3f;
    [SerializeField] private float feedbackSlideUpAmount = 50f;
    [SerializeField] private string feedbackPrefix = "+";
    [SerializeField] private string coinSuffix = "";
    [SerializeField] private string gemSuffix = "";

    [Header("Audio")]
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip gemSound;

    private Coroutine powerUpCheckCoroutine;
    private Dictionary<string, DateTime> lastTriggerTimes = new Dictionary<string, DateTime>();

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

        // Find the main canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }

        // Initialize power-ups from GameData on start
        InitializePowerUpsFromGameData();

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

    // Initialize power-ups based on what's already in GameData
    private void InitializePowerUpsFromGameData()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        // Get equipped pets from GameData
        string pet1 = gameDataManager.CurrentGameData.equippedPetSlot1;
        string pet2 = gameDataManager.CurrentGameData.equippedPetSlot2;

        if (!string.IsNullOrEmpty(pet1))
        {
            RegisterPetPowerUps(pet1);
        }

        if (!string.IsNullOrEmpty(pet2))
        {
            RegisterPetPowerUps(pet2);
        }

        Debug.Log($"Initialized power-ups from GameData: Pet1='{pet1}', Pet2='{pet2}'");
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

        // Get currently equipped pets from GameData
        string slot1Pet = gameDataManager.CurrentGameData.equippedPetSlot1;
        string slot2Pet = gameDataManager.CurrentGameData.equippedPetSlot2;

        // Check power-ups for each equipped pet
        CheckPetPowerUps(slot1Pet);
        CheckPetPowerUps(slot2Pet);
    }

    private void CheckPetPowerUps(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return;

        var ingredient = ingredientDatabase.GetIngredientInfo(petName);
        if (ingredient == null || ingredient.powerUps == null) return;

        // Check each power-up of this pet
        for (int i = 0; i < ingredient.powerUps.Count; i++)
        {
            var powerUp = ingredient.powerUps[i];

            // Only process Coins and Gems power-ups
            if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Coins ||
                powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Gems)
            {
                if (powerUp.cooldownMinutes > 0)
                {
                    CheckAndTriggerSinglePowerUp(petName, i, powerUp);
                }
            }
        }
    }

    private void CheckAndTriggerSinglePowerUp(string petName, int powerUpIndex, IngredientDatabase.PowerUpInfo powerUp)
    {
        string key = $"{petName}_{powerUpIndex}";

        DateTime lastTrigger;
        if (!lastTriggerTimes.TryGetValue(key, out lastTrigger))
        {
            // First time checking this power-up - initialize with current time minus cooldown to trigger immediately
            lastTrigger = DateTime.Now - TimeSpan.FromMinutes(powerUp.cooldownMinutes);
            lastTriggerTimes[key] = lastTrigger;
        }

        TimeSpan timeSinceLast = DateTime.Now - lastTrigger;
        if (timeSinceLast.TotalMinutes >= powerUp.cooldownMinutes)
        {
            // Trigger the power-up
            TriggerPowerUp(petName, powerUp);

            // Update last trigger time
            lastTriggerTimes[key] = DateTime.Now;
        }
    }

    private void TriggerPowerUp(string petName, IngredientDatabase.PowerUpInfo powerUp)
    {
        Debug.Log($"Triggering power-up for {petName}: {powerUp.powerUpType} +{powerUp.amount}");

        switch (powerUp.powerUpType)
        {
            case IngredientDatabase.PowerUpInfo.PowerUpType.Coins:
                StartCoroutine(AddCoinsWithFeedback(powerUp.amount));
                break;

            case IngredientDatabase.PowerUpInfo.PowerUpType.Gems:
                StartCoroutine(AddGemsWithFeedback(powerUp.amount));
                break;
        }
    }

    private IEnumerator AddCoinsWithFeedback(int amount)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) yield break;

        // Show feedback first
        ShowRewardFeedback(coinRewardFeedbackPrefab, coinRewardSpawnPoint, amount, coinSuffix);

        // Play coin sound
        PlaySound(coinSound);

        // Add coins after feedback starts
        gameDataManager.CurrentGameData.nutriCoins += amount;
        gameDataManager.SaveGameData();

        Debug.Log($"Power-up added {amount} coins. Total: {gameDataManager.CurrentGameData.nutriCoins}");

        // Update UI through Player_Data with animation
        if (playerData != null)
        {
            playerData.AddCoins(amount);
        }

        yield return null;
    }

    private IEnumerator AddGemsWithFeedback(int amount)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) yield break;

        // Show feedback first
        ShowRewardFeedback(gemRewardFeedbackPrefab, gemRewardSpawnPoint, amount, gemSuffix);

        // Play gem sound
        PlaySound(gemSound);

        // Add gems after feedback starts
        gameDataManager.CurrentGameData.nutriGems += amount;
        gameDataManager.SaveGameData();

        Debug.Log($"Power-up added {amount} gems. Total: {gameDataManager.CurrentGameData.nutriGems}");

        // Update UI through Player_Data with animation
        if (playerData != null)
        {
            playerData.AddGems(amount);
        }

        yield return null;
    }

    private void ShowRewardFeedback(GameObject prefab, RectTransform spawnPoint, int amount, string suffix)
    {
        if (prefab == null || spawnPoint == null || parentCanvas == null) return;

        GameObject feedbackObject = Instantiate(prefab, parentCanvas.transform);
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        // Position at spawn point
        rectTransform.position = spawnPoint.position;
        rectTransform.anchorMin = spawnPoint.anchorMin;
        rectTransform.anchorMax = spawnPoint.anchorMax;
        rectTransform.pivot = spawnPoint.pivot;

        // Set text
        TMP_Text feedbackText = feedbackObject.GetComponentInChildren<TMP_Text>();
        if (feedbackText != null)
        {
            feedbackText.text = $"{feedbackPrefix}{amount}{suffix}";
        }

        StartCoroutine(AnimateRewardFeedback(feedbackObject));
    }

    private IEnumerator AnimateRewardFeedback(GameObject feedbackObject)
    {
        if (feedbackObject == null) yield break;

        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = feedbackObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = feedbackObject.AddComponent<CanvasGroup>();
        }

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, feedbackSlideUpAmount);

        float elapsedTime = 0f;

        // Slide up
        while (elapsedTime < feedbackSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / feedbackSlideDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Fade out
        elapsedTime = 0f;
        while (elapsedTime < feedbackFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / feedbackFadeOutDuration);
            yield return null;
        }

        Destroy(feedbackObject);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(clip);
        }
    }

    // Called when a pet is equipped (from EnerlingSlotButton)
    public void RegisterPetPowerUps(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return;

        var ingredient = ingredientDatabase.GetIngredientInfo(petName);
        if (ingredient == null || ingredient.powerUps == null) return;

        Debug.Log($"Registering power-ups for pet: {petName}");

        // Register each power-up's last trigger time
        for (int i = 0; i < ingredient.powerUps.Count; i++)
        {
            var powerUp = ingredient.powerUps[i];

            // For active power-ups (Coins, Gems), initialize with current time
            if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Coins ||
                powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Gems)
            {
                if (powerUp.cooldownMinutes > 0)
                {
                    string key = $"{petName}_{i}";
                    if (!lastTriggerTimes.ContainsKey(key))
                    {
                        // Start with current time minus cooldown to trigger immediately
                        lastTriggerTimes[key] = DateTime.Now - TimeSpan.FromMinutes(powerUp.cooldownMinutes);
                    }
                    Debug.Log($"Registered active power-up for {petName}: +{powerUp.amount} {powerUp.powerUpType} every {powerUp.cooldownMinutes}min");
                }
            }
            // For passive power-ups (Heart, Time, Exp), update GameData
            else if (powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Heart ||
                     powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Time ||
                     powerUp.powerUpType == IngredientDatabase.PowerUpInfo.PowerUpType.Exp)
            {
                if (gameDataManager != null)
                {
                    gameDataManager.RegisterPassivePowerUp(petName, powerUp.powerUpType, powerUp.amount);
                }
                Debug.Log($"Registered passive power-up for {petName}: {powerUp.amount} {powerUp.powerUpType}");
            }
        }
    }

    // Called when a pet is unequipped
    public void UnregisterPetPowerUps(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return;

        // Remove all trigger times for this pet
        List<string> keysToRemove = new List<string>();
        foreach (var key in lastTriggerTimes.Keys)
        {
            if (key.StartsWith(petName + "_"))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            lastTriggerTimes.Remove(key);
        }

        // Remove passive power-ups from GameData
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.RemovePassivePowerUpsForPet(petName);
            gameDataManager.SaveGameData();
        }

        Debug.Log($"Unregistered all power-ups for {petName}");
    }

    // Get remaining time for a specific power-up
    public TimeSpan GetRemainingTime(string petName, int powerUpIndex)
    {
        string key = $"{petName}_{powerUpIndex}";

        if (lastTriggerTimes.TryGetValue(key, out DateTime lastTrigger))
        {
            var ingredient = ingredientDatabase.GetIngredientInfo(petName);
            if (ingredient != null && powerUpIndex < ingredient.powerUps.Count)
            {
                float cooldownMinutes = ingredient.powerUps[powerUpIndex].cooldownMinutes;
                TimeSpan timeSinceLast = DateTime.Now - lastTrigger;
                TimeSpan cooldown = TimeSpan.FromMinutes(cooldownMinutes);
                TimeSpan timeRemaining = cooldown - timeSinceLast;

                return timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero;
            }
        }

        return TimeSpan.Zero;
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

    // Get total heart bonus at game start
    public int GetStartGameHeartBonus()
    {
        return gameDataManager != null ? gameDataManager.GetTotalHeartBonus() : 0;
    }

    // Get total time reduction at game start (in seconds)
    public int GetStartGameTimeReduction()
    {
        return gameDataManager != null ? gameDataManager.GetTotalTimeReductionSeconds() : 0;
    }
}