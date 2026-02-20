using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RewardProcessor : MonoBehaviour
{
    [Header("Database References")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private ProfileIconDatabase iconDatabase;
    [SerializeField] private FrameDatabase frameDatabase;
    [SerializeField] private IngredientDatabase ingredientDatabase;

    [Header("Unlockable Canvas")]
    [SerializeField] private UnlockableCanvasController unlockableCanvas;

    [Header("Audio")]
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip gemSound;

    [Header("Reward Feedback")]
    [SerializeField] private GameObject coinRewardFeedbackPrefab;
    [SerializeField] private GameObject gemRewardFeedbackPrefab;
    [SerializeField] private RectTransform coinRewardSpawnPoint;
    [SerializeField] private RectTransform gemRewardSpawnPoint;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private float feedbackSlideDuration = 0.5f;
    [SerializeField] private float feedbackFadeOutDuration = 0.3f;
    [SerializeField] private float feedbackSlideUpAmount = 50f;
    [SerializeField] private string feedbackPrefix = "+";
    [SerializeField] private string coinSuffix = "";
    [SerializeField] private string gemSuffix = "";

    [Header("Reward Delay")]
    [SerializeField] private float rewardDelay = 1f;

    private Player_Data playerData;
    private GameDataManager gameDataManager;

    // Public properties to access private fields (for QuestBoard and NPC to copy settings)
    public GameObject CoinRewardFeedbackPrefab { get => coinRewardFeedbackPrefab; set => coinRewardFeedbackPrefab = value; }
    public GameObject GemRewardFeedbackPrefab { get => gemRewardFeedbackPrefab; set => gemRewardFeedbackPrefab = value; }
    public RectTransform CoinRewardSpawnPoint { get => coinRewardSpawnPoint; set => coinRewardSpawnPoint = value; }
    public RectTransform GemRewardSpawnPoint { get => gemRewardSpawnPoint; set => gemRewardSpawnPoint = value; }
    public Canvas ParentCanvas { get => parentCanvas; set => parentCanvas = value; }
    public float FeedbackSlideDuration { get => feedbackSlideDuration; set => feedbackSlideDuration = value; }
    public float FeedbackFadeOutDuration { get => feedbackFadeOutDuration; set => feedbackFadeOutDuration = value; }
    public float FeedbackSlideUpAmount { get => feedbackSlideUpAmount; set => feedbackSlideUpAmount = value; }
    public string FeedbackPrefix { get => feedbackPrefix; set => feedbackPrefix = value; }
    public string CoinSuffix { get => coinSuffix; set => coinSuffix = value; }
    public string GemSuffix { get => gemSuffix; set => gemSuffix = value; }
    public AudioClip CoinSound { get => coinSound; set => coinSound = value; }
    public float RewardDelay { get => rewardDelay; set => rewardDelay = value; }

    private void Start()
    {
        gameDataManager = GameDataManager.Instance;
        playerData = FindObjectOfType<Player_Data>();

        // Initialize unlockable canvas with database references
        if (unlockableCanvas != null)
        {
            unlockableCanvas.SetDatabases(characterDatabase, iconDatabase, frameDatabase, ingredientDatabase);
        }

        // Find main canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }
    }

    public IEnumerator ProcessRewards(List<QuestReward> rewards, System.Action onAllRewardsProcessed)
    {
        // First, check if there are any unlockable rewards (Character, Skin, Icon, Frame, Enerling)
        List<QuestReward> unlockableRewards = new List<QuestReward>();

        int totalCoins = 0;
        int totalGems = 0;

        // Separate rewards by type
        foreach (var reward in rewards)
        {
            switch (reward.type)
            {
                case QuestReward.RewardType.Character:
                case QuestReward.RewardType.Skin:
                case QuestReward.RewardType.Icon:
                case QuestReward.RewardType.Frame:
                case QuestReward.RewardType.Enerlings:
                    unlockableRewards.Add(reward);
                    break;

                case QuestReward.RewardType.NutriCoins:
                    totalCoins += reward.amount;
                    break;

                case QuestReward.RewardType.NutriGems:
                    totalGems += reward.amount;
                    break;

                case QuestReward.RewardType.Exp:
                    // Process EXP immediately
                    ProcessExpReward(reward);
                    break;

                default:
                    // Process other reward types
                    ProcessOtherReward(reward);
                    break;
            }
        }

        // Process each unlockable reward (check if already owned)
        foreach (var reward in unlockableRewards)
        {
            bool alreadyOwned = CheckIfAlreadyOwned(reward);

            if (!alreadyOwned)
            {
                // Unlock the item in GameData
                UnlockReward(reward);

                // Save game data
                if (gameDataManager != null)
                {
                    gameDataManager.SaveGameData();
                }

                // Show unlockable canvas
                if (unlockableCanvas != null)
                {
                    // Wait a tiny moment for the quest board to close
                    yield return new WaitForSeconds(0.2f);

                    bool canvasShown = false;
                    unlockableCanvas.ShowUnlockable(reward, () => canvasShown = true);

                    // Wait until canvas is closed
                    while (!canvasShown)
                    {
                        yield return null;
                    }
                }
            }
            else
            {
                // Already owned, give alternative currency reward
                Debug.Log($"{reward.rewardName} already owned. Giving alternative currency reward.");
                totalCoins += 500; // 500 coins as alternative
                totalGems += 50;    // 50 gems as alternative
            }
        }

        // Wait for delay before showing currency feedback
        yield return new WaitForSeconds(rewardDelay);

        // Play sound and show feedback for coins/gems
        if (totalCoins > 0 || totalGems > 0)
        {
            // Play appropriate sound
            if (totalCoins > 0 && totalGems > 0)
            {
                if (coinSound != null && AudioHandler.Instance != null)
                    AudioHandler.Instance.soundEffectsSource.PlayOneShot(coinSound);
            }
            else if (totalCoins > 0)
            {
                if (coinSound != null && AudioHandler.Instance != null)
                    AudioHandler.Instance.soundEffectsSource.PlayOneShot(coinSound);
            }
            else if (totalGems > 0)
            {
                if (gemSound != null && AudioHandler.Instance != null)
                    AudioHandler.Instance.soundEffectsSource.PlayOneShot(gemSound);
            }

            // Add currency to game data
            if (gameDataManager != null && gameDataManager.CurrentGameData != null)
            {
                if (totalCoins > 0)
                {
                    gameDataManager.CurrentGameData.nutriCoins += totalCoins;
                }
                if (totalGems > 0)
                {
                    gameDataManager.CurrentGameData.nutriGems += totalGems;
                }
            }

            // Show feedback
            if (totalCoins > 0)
            {
                ShowRewardFeedback(coinRewardFeedbackPrefab, coinRewardSpawnPoint, totalCoins, coinSuffix);
            }

            if (totalGems > 0)
            {
                ShowRewardFeedback(gemRewardFeedbackPrefab, gemRewardSpawnPoint, totalGems, gemSuffix);
            }

            // Update Player_Data UI
            if (playerData != null)
            {
                if (totalCoins > 0) playerData.NotifyCoinCollected(totalCoins);
                if (totalGems > 0) playerData.NotifyGemCollected(totalGems);
                playerData.ForceUpdateAllUI();
            }
        }

        // Save game data one more time
        if (gameDataManager != null)
        {
            gameDataManager.SaveGameData();
        }

        // Small delay before completing
        yield return new WaitForSeconds(0.2f);

        onAllRewardsProcessed?.Invoke();
    }

    private bool CheckIfAlreadyOwned(QuestReward reward)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return false;

        GameData gameData = gameDataManager.CurrentGameData;

        switch (reward.type)
        {
            case QuestReward.RewardType.Character:
                // Find character by name
                if (characterDatabase != null)
                {
                    var character = characterDatabase.GetCharacterByName(reward.rewardName);
                    if (character != null)
                    {
                        return gameData.unlockedCharacterIDs.Contains(character.characterID);
                    }
                }
                break;

            case QuestReward.RewardType.Skin:
                // Find skin by name across all characters
                if (characterDatabase != null)
                {
                    var skinInfo = characterDatabase.GetSkinByName(reward.rewardName);
                    if (skinInfo.HasValue)
                    {
                        return gameData.IsSkinUnlocked(skinInfo.Value.characterId, skinInfo.Value.skinId);
                    }
                }
                break;

            case QuestReward.RewardType.Icon:
                return gameData.unlockedIconIds.Contains(reward.rewardName);

            case QuestReward.RewardType.Frame:
                return gameData.unlockedFrameIds.Contains(reward.rewardName);

            case QuestReward.RewardType.Enerlings:
                return gameData.unlockedEnerlings.Contains(reward.rewardName);
        }

        return false;
    }

    private void UnlockReward(QuestReward reward)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        GameData gameData = gameDataManager.CurrentGameData;

        switch (reward.type)
        {
            case QuestReward.RewardType.Character:
                if (characterDatabase != null)
                {
                    var character = characterDatabase.GetCharacterByName(reward.rewardName);
                    if (character != null && !gameData.unlockedCharacterIDs.Contains(character.characterID))
                    {
                        gameData.unlockedCharacterIDs.Add(character.characterID);
                        Debug.Log($"Unlocked Character: {character.characterName} (ID: {character.characterID})");
                    }
                }
                break;

            case QuestReward.RewardType.Skin:
                if (characterDatabase != null)
                {
                    var skinInfo = characterDatabase.GetSkinByName(reward.rewardName);
                    if (skinInfo.HasValue)
                    {
                        gameData.UnlockSkinForCharacter(skinInfo.Value.characterId, skinInfo.Value.skinId);
                        Debug.Log($"Unlocked Skin: {reward.rewardName} for Character ID: {skinInfo.Value.characterId}");
                    }
                }
                break;

            case QuestReward.RewardType.Icon:
                if (!gameData.unlockedIconIds.Contains(reward.rewardName))
                {
                    gameData.unlockedIconIds.Add(reward.rewardName);
                    Debug.Log($"Unlocked Icon: {reward.rewardName}");
                }
                break;

            case QuestReward.RewardType.Frame:
                if (!gameData.unlockedFrameIds.Contains(reward.rewardName))
                {
                    gameData.unlockedFrameIds.Add(reward.rewardName);
                    Debug.Log($"Unlocked Frame: {reward.rewardName}");
                }
                break;

            case QuestReward.RewardType.Enerlings:
                if (!gameData.unlockedEnerlings.Contains(reward.rewardName))
                {
                    gameData.unlockedEnerlings.Add(reward.rewardName);
                    Debug.Log($"Unlocked Enerling: {reward.rewardName}");

                    // Also unlock in ingredient database if needed
                    if (ingredientDatabase != null)
                    {
                        ingredientDatabase.UnlockIngredient(reward.rewardName);
                    }
                }
                break;
        }
    }

    private void ProcessExpReward(QuestReward reward)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        GameData gameData = gameDataManager.CurrentGameData;
        gameData.currentXP += reward.amount;

        // Check for level up
        while (gameData.currentXP >= gameData.xpToNextLevel)
        {
            gameData.currentXP -= gameData.xpToNextLevel;
            gameData.playerLevel++;
            gameData.xpToNextLevel = CalculateNextLevelXP(gameData.playerLevel);
            Debug.Log($"Level up! New level: {gameData.playerLevel}");
        }

        Debug.Log($"Added {reward.amount} XP. Current XP: {gameData.currentXP}/{gameData.xpToNextLevel}");
    }

    private void ProcessOtherReward(QuestReward reward)
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
            return;

        GameData gameData = gameDataManager.CurrentGameData;

        switch (reward.type)
        {
            case QuestReward.RewardType.NutriCoins:
                gameData.nutriCoins += reward.amount;
                break;

            case QuestReward.RewardType.NutriGems:
                gameData.nutriGems += reward.amount;
                break;
        }
    }

    private float CalculateNextLevelXP(int level)
    {
        return 100 * level;
    }

    private void ShowRewardFeedback(GameObject prefab, RectTransform spawnPoint, int amount, string suffix)
    {
        if (prefab == null || spawnPoint == null || parentCanvas == null || amount <= 0) return;

        GameObject feedbackObject = Instantiate(prefab, parentCanvas.transform);
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        rectTransform.position = spawnPoint.position;
        rectTransform.anchorMin = spawnPoint.anchorMin;
        rectTransform.anchorMax = spawnPoint.anchorMax;
        rectTransform.pivot = spawnPoint.pivot;

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

    // Public method to copy settings from another RewardProcessor or from external sources
    public void CopySettingsFrom(RewardProcessor other)
    {
        if (other == null) return;

        this.coinRewardFeedbackPrefab = other.coinRewardFeedbackPrefab;
        this.gemRewardFeedbackPrefab = other.gemRewardFeedbackPrefab;
        this.coinRewardSpawnPoint = other.coinRewardSpawnPoint;
        this.gemRewardSpawnPoint = other.gemRewardSpawnPoint;
        this.parentCanvas = other.parentCanvas;
        this.feedbackSlideDuration = other.feedbackSlideDuration;
        this.feedbackFadeOutDuration = other.feedbackFadeOutDuration;
        this.feedbackSlideUpAmount = other.feedbackSlideUpAmount;
        this.feedbackPrefix = other.feedbackPrefix;
        this.coinSuffix = other.coinSuffix;
        this.gemSuffix = other.gemSuffix;
        this.coinSound = other.coinSound;
        this.rewardDelay = other.rewardDelay;
    }
}