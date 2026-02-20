using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class QuestBoardUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button generalButton;
    [SerializeField] private Button kingdomsButton;
    [SerializeField] private Image generalButtonImage;
    [SerializeField] private Image kingdomsButtonImage;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject questContainerPrefab;
    [SerializeField] private GameObject questListPanels;
    [SerializeField] private GameObject noQuestAvailableText;
    [SerializeField] private TextMeshProUGUI noQuestText;

    [Header("Quest Tracker")]
    [SerializeField] private TextMeshProUGUI questTrackerText;

    [Header("Canvas References")]
    [SerializeField] private CanvasGroup questBoardCanvasGroup;
    [SerializeField] private GameObject questBoardObject;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Settings")]
    [SerializeField] private Color selectedButtonColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color deselectedButtonColor = new Color(1f, 1f, 1f, 0f);

    [Header("Kingdom Configuration")]
    [SerializeField] private string generalQuestKingdomID = "general_quests";
    [SerializeField] private string kingdomQuestKingdomID = "sugaria";

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip coinSound;

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

    [Header("Reward Delay Settings")]
    [SerializeField] private float rewardDelay = 1f;

    [Header("Database Reference")]
    [SerializeField] private QuestDatabase questDatabase; // Direct reference to database

    private QuestManager questManager;
    private QuestCategory currentCategory = QuestCategory.GeneralQuest;
    private List<QuestUIItem> currentQuestItems = new List<QuestUIItem>();
    private bool isOpening = false;
    private bool isClosing = false;
    private Player_Data playerData;

    private void Awake()
    {
        questManager = QuestManager.Instance;

        if (questManager == null)
        {
            Debug.LogError("QuestManager.Instance is null! Make sure QuestManager is in the scene and has been initialized.");
            questManager = FindObjectOfType<QuestManager>();
        }

        // Find Player_Data
        playerData = FindObjectOfType<Player_Data>();
        if (playerData == null)
        {
            Debug.LogWarning("Player_Data not found in scene! UI will not update automatically.");
        }

        // Find the main canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }

        // Button listeners
        if (generalButton != null)
            generalButton.onClick.AddListener(() => OnCategoryButtonClicked(QuestCategory.GeneralQuest));

        if (kingdomsButton != null)
            kingdomsButton.onClick.AddListener(() => OnCategoryButtonClicked(QuestCategory.MainStory));

        // Ensure canvas group exists
        if (questBoardCanvasGroup == null && questBoardObject != null)
        {
            questBoardCanvasGroup = questBoardObject.GetComponent<CanvasGroup>();
            if (questBoardCanvasGroup == null)
            {
                questBoardCanvasGroup = questBoardObject.AddComponent<CanvasGroup>();
            }
        }

        // Initialize database if needed
        if (questDatabase != null)
        {
            questDatabase.InitializeDatabase();
        }

        // Initial setup
        if (questManager != null)
        {
            OnCategoryButtonClicked(QuestCategory.GeneralQuest);
        }

        // Ensure board starts hidden
        if (questBoardObject != null)
        {
            questBoardObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (questManager != null)
        {
            RefreshQuestList();
        }
    }

    public void OpenQuestBoard()
    {
        if (isOpening || isClosing) return;
        StartCoroutine(OpenBoardCoroutine());
    }

    public void CloseQuestBoard()
    {
        if (isOpening || isClosing) return;
        StartCoroutine(CloseBoardCoroutine());
    }

    private IEnumerator OpenBoardCoroutine()
    {
        isOpening = true;
        Debug.Log("Opening Quest Board...");

        // Show the board
        if (questBoardObject != null)
        {
            questBoardObject.SetActive(true);
        }

        // Fade in
        if (questBoardCanvasGroup != null)
        {
            questBoardCanvasGroup.alpha = 0f;
            float timer = 0f;

            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                questBoardCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                yield return null;
            }

            questBoardCanvasGroup.alpha = 1f;
        }

        // Refresh quest list
        RefreshQuestList();

        isOpening = false;
    }

    private IEnumerator CloseBoardCoroutine()
    {
        isClosing = true;
        Debug.Log("Closing Quest Board...");

        // Play click sound
        PlayButtonClickSound();

        // Fade out
        if (questBoardCanvasGroup != null)
        {
            float timer = 0f;
            float startAlpha = questBoardCanvasGroup.alpha;

            while (timer < fadeOutDuration)
            {
                timer += Time.deltaTime;
                questBoardCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeOutDuration);
                yield return null;
            }

            questBoardCanvasGroup.alpha = 0f;
        }

        // Hide the board
        if (questBoardObject != null)
        {
            questBoardObject.SetActive(false);
        }

        isClosing = false;
    }

    private void OnCategoryButtonClicked(QuestCategory category)
    {
        PlayButtonClickSound();
        currentCategory = category;
        UpdateButtonStates();
        RefreshQuestList();
        UpdateQuestTracker();
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(buttonClickSound);
        }
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    private void UpdateButtonStates()
    {
        bool isGeneralSelected = currentCategory == QuestCategory.GeneralQuest;
        bool isKingdomsSelected = currentCategory == QuestCategory.MainStory;

        // Update button colors
        if (generalButtonImage != null)
        {
            generalButtonImage.color = isGeneralSelected ? selectedButtonColor : deselectedButtonColor;
        }

        if (kingdomsButtonImage != null)
        {
            kingdomsButtonImage.color = isKingdomsSelected ? selectedButtonColor : deselectedButtonColor;
        }

        // Update button interactable state
        if (generalButton != null)
            generalButton.interactable = !isGeneralSelected;

        if (kingdomsButton != null)
            kingdomsButton.interactable = !isKingdomsSelected;
    }

    private void RefreshQuestList()
    {
        ClearQuestList();

        List<Quest> filteredQuests = GetFilteredQuests();

        if (filteredQuests.Count == 0)
        {
            ShowNoQuestsMessage();
            UpdateQuestTracker();
            return;
        }

        ShowQuestList();

        foreach (var quest in filteredQuests)
        {
            GameObject questObject = Instantiate(questContainerPrefab, contentContainer);
            QuestUIItem questUI = questObject.GetComponent<QuestUIItem>();

            if (questUI != null)
            {
                // Pass references to the QuestUIItem
                questUI.Initialize(
                    this,
                    questDatabase, // Pass the database reference
                    coinRewardFeedbackPrefab,
                    gemRewardFeedbackPrefab,
                    coinRewardSpawnPoint,
                    gemRewardSpawnPoint,
                    parentCanvas,
                    feedbackSlideDuration,
                    feedbackFadeOutDuration,
                    feedbackSlideUpAmount,
                    feedbackPrefix,
                    coinSuffix,
                    gemSuffix,
                    coinSound,
                    rewardDelay
                );

                questUI.SetupQuest(quest);
                currentQuestItems.Add(questUI);
            }
        }

        UpdateQuestTracker();
    }

    private List<Quest> GetFilteredQuests()
    {
        List<Quest> filteredQuests = new List<Quest>();

        if (questManager == null)
        {
            Debug.LogError("QuestManager is null! Cannot get quests.");
            return filteredQuests;
        }

        var allKingdoms = questManager.GetAllKingdoms();

        if (allKingdoms == null)
        {
            Debug.LogError("GetAllKingdoms() returned null!");
            return filteredQuests;
        }

        if (currentCategory == QuestCategory.GeneralQuest)
        {
            // Find the General Quests kingdom using the configurable ID
            Kingdom generalKingdom = allKingdoms.FirstOrDefault(k => k.kingdomID == generalQuestKingdomID);

            if (generalKingdom != null && generalKingdom.quests != null)
            {
                // Add all general quests in database order
                filteredQuests.AddRange(generalKingdom.quests);
                Debug.Log($"Found {generalKingdom.quests.Count} quests in general kingdom: {generalQuestKingdomID}");
            }
            else
            {
                Debug.LogWarning($"General quests kingdom with ID '{generalQuestKingdomID}' not found in database!");
            }
        }
        else if (currentCategory == QuestCategory.MainStory)
        {
            // Find the specific kingdom by ID for kingdom quests
            Kingdom targetKingdom = allKingdoms.FirstOrDefault(k => k.kingdomID == kingdomQuestKingdomID);

            if (targetKingdom != null && targetKingdom.quests != null)
            {
                // Add all kingdom quests in database order
                filteredQuests.AddRange(targetKingdom.quests);
                Debug.Log($"Found {targetKingdom.quests.Count} quests in kingdom: {kingdomQuestKingdomID}");
            }
            else
            {
                Debug.LogWarning($"Kingdom with ID '{kingdomQuestKingdomID}' not found in database!");
            }
        }

        // Sort quests: Database order first, then by status priority
        // We keep database order within each status group
        List<Quest> orderedQuests = new List<Quest>();

        // First add Completed quests in database order
        orderedQuests.AddRange(filteredQuests.Where(q => q.status == QuestStatus.Completed));

        // Then add InProgress quests in database order
        orderedQuests.AddRange(filteredQuests.Where(q => q.status == QuestStatus.InProgress));

        // Then add NotStarted quests in database order
        orderedQuests.AddRange(filteredQuests.Where(q => q.status == QuestStatus.NotStarted));

        // Finally add any other statuses (Claimed, Failed, Abandoned) in database order
        orderedQuests.AddRange(filteredQuests.Where(q => q.status != QuestStatus.Completed &&
                                                          q.status != QuestStatus.InProgress &&
                                                          q.status != QuestStatus.NotStarted));

        return orderedQuests;
    }

    private void UpdateQuestTracker()
    {
        if (questTrackerText == null || questManager == null) return;

        int totalQuests = 0;
        int completedQuests = 0;

        var allKingdoms = questManager.GetAllKingdoms();
        if (allKingdoms == null) return;

        // Determine which kingdom to track based on current category
        string targetKingdomID = currentCategory == QuestCategory.GeneralQuest ?
            generalQuestKingdomID : kingdomQuestKingdomID;

        Kingdom targetKingdom = allKingdoms.FirstOrDefault(k => k.kingdomID == targetKingdomID);

        if (targetKingdom != null && targetKingdom.quests != null)
        {
            totalQuests = targetKingdom.quests.Count;
            completedQuests = targetKingdom.CompletedQuestCount + targetKingdom.ClaimedQuestCount;
        }

        // Show/hide based on total count
        if (totalQuests == 0)
        {
            questTrackerText.gameObject.SetActive(false);
        }
        else
        {
            questTrackerText.gameObject.SetActive(true);
            questTrackerText.text = $"{completedQuests}/{totalQuests}";
        }
    }

    private void ClearQuestList()
    {
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        currentQuestItems.Clear();
    }

    private void ShowNoQuestsMessage()
    {
        if (questListPanels != null)
        {
            questListPanels.SetActive(false);
        }

        if (noQuestAvailableText != null)
        {
            noQuestAvailableText.SetActive(true);

            if (noQuestText != null)
            {
                string message = currentCategory == QuestCategory.MainStory
                    ? $"No quests available in this kingdom."
                    : "No general quests available at the moment.";

                noQuestText.text = message;
            }
        }
    }

    private void ShowQuestList()
    {
        if (questListPanels != null)
        {
            questListPanels.SetActive(true);
        }

        if (noQuestAvailableText != null)
        {
            noQuestAvailableText.SetActive(false);
        }
    }

    // Called when quests are updated externally
    public void OnQuestsUpdated()
    {
        RefreshQuestList();
        UpdateQuestTracker();
    }

    public void RefreshUI()
    {
        RefreshQuestList();
    }

    public void SwitchToCategory(QuestCategory category)
    {
        OnCategoryButtonClicked(category);
    }

    public QuestCategory GetCurrentCategory()
    {
        return currentCategory;
    }

    public void SetUIVisible(bool visible)
    {
        if (visible)
        {
            OpenQuestBoard();
        }
        else
        {
            CloseQuestBoard();
        }
    }

    // Method to handle reward claiming from QuestUIItem - NOW USING DIRECT DATABASE REFERENCE
    public IEnumerator ProcessQuestRewards(Quest quest, System.Action onRewardsProcessed)
    {
        // Store the quest ID for later reference
        string questID = quest.questID;

        // DIRECTLY update the quest status in the database
        if (questDatabase != null)
        {
            Quest databaseQuest = questDatabase.GetQuest(questID);
            if (databaseQuest != null)
            {
                databaseQuest.ClaimQuest();
                Debug.Log($"Quest {questID} status updated to {databaseQuest.status} in database");

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(questDatabase);
#endif
            }
        }

        // Also try through QuestManager as backup
        if (questManager != null)
        {
            questManager.ClaimQuest(questID);
        }

        // Use the RewardProcessor to handle all rewards
        RewardProcessor rewardProcessor = FindObjectOfType<RewardProcessor>();
        if (rewardProcessor == null)
        {
            // Create one if it doesn't exist
            GameObject processorObj = new GameObject("RewardProcessor");
            rewardProcessor = processorObj.AddComponent<RewardProcessor>();

            // Copy settings from this component using properties
            rewardProcessor.CoinRewardFeedbackPrefab = coinRewardFeedbackPrefab;
            rewardProcessor.GemRewardFeedbackPrefab = gemRewardFeedbackPrefab;
            rewardProcessor.CoinRewardSpawnPoint = coinRewardSpawnPoint;
            rewardProcessor.GemRewardSpawnPoint = gemRewardSpawnPoint;
            rewardProcessor.ParentCanvas = parentCanvas;
            rewardProcessor.FeedbackSlideDuration = feedbackSlideDuration;
            rewardProcessor.FeedbackFadeOutDuration = feedbackFadeOutDuration;
            rewardProcessor.FeedbackSlideUpAmount = feedbackSlideUpAmount;
            rewardProcessor.FeedbackPrefix = feedbackPrefix;
            rewardProcessor.CoinSuffix = coinSuffix;
            rewardProcessor.GemSuffix = gemSuffix;
            rewardProcessor.CoinSound = coinSound;
            rewardProcessor.RewardDelay = rewardDelay;
        }

        // Process all rewards through the unified system
        yield return StartCoroutine(rewardProcessor.ProcessRewards(quest.rewards, () => {
            Debug.Log("All rewards processed");
        }));

        // Refresh the quest list
        RefreshQuestList();

        onRewardsProcessed?.Invoke();
    }

    private void ProcessReward(QuestReward reward)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogError("GameDataManager or CurrentGameData is null!");
            return;
        }

        GameData gameData = GameDataManager.Instance.CurrentGameData;

        switch (reward.type)
        {
            case QuestReward.RewardType.NutriCoins:
                gameData.nutriCoins += reward.amount;
                Debug.Log($"Added {reward.amount} NutriCoins. Total: {gameData.nutriCoins}");
                break;

            case QuestReward.RewardType.Exp:
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
                break;

            case QuestReward.RewardType.NutriGems:
                gameData.nutriGems += reward.amount;
                Debug.Log($"Added {reward.amount} NutriGems. Total: {gameData.nutriGems}");
                break;

            case QuestReward.RewardType.Enerlings:
                if (!string.IsNullOrEmpty(reward.rewardID) && !gameData.unlockedEnerlings.Contains(reward.rewardID))
                {
                    gameData.unlockedEnerlings.Add(reward.rewardID);
                    Debug.Log($"Unlocked Enerling: {reward.rewardName} (ID: {reward.rewardID})");
                }
                break;

            case QuestReward.RewardType.Character:
                if (!string.IsNullOrEmpty(reward.rewardID))
                {
                    if (int.TryParse(reward.rewardID, out int characterID))
                    {
                        if (!gameData.unlockedCharacterIDs.Contains(characterID))
                        {
                            gameData.unlockedCharacterIDs.Add(characterID);
                            Debug.Log($"Unlocked Character: {reward.rewardName} (ID: {characterID})");
                        }
                    }
                }
                break;

            case QuestReward.RewardType.Frame:
                if (!string.IsNullOrEmpty(reward.rewardID) && !gameData.unlockedFrameIds.Contains(reward.rewardID))
                {
                    gameData.unlockedFrameIds.Add(reward.rewardID);
                    Debug.Log($"Unlocked Frame: {reward.rewardName} (ID: {reward.rewardID})");
                }
                break;

            case QuestReward.RewardType.Icon:
                if (!string.IsNullOrEmpty(reward.rewardID) && !gameData.unlockedIconIds.Contains(reward.rewardID))
                {
                    gameData.unlockedIconIds.Add(reward.rewardID);
                    Debug.Log($"Unlocked Icon: {reward.rewardName} (ID: {reward.rewardID})");
                }
                break;

            default:
                Debug.LogWarning($"Unknown reward type: {reward.type}");
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
}