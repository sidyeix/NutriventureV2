using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using Cinemachine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class NPCQuestInteraction : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("NPC Configuration")]
    [SerializeField] private string kingdomID;

    [Tooltip("Quest categories this NPC can give")]
    [SerializeField]
    private List<QuestCategory> allowedCategories = new List<QuestCategory>
    {
        QuestCategory.Tutorial,
        QuestCategory.MainStory,
        QuestCategory.GeneralQuest
    };

    [SerializeField] private GameObject questButton;
    [SerializeField] private GameObject missionCanvas;
    [SerializeField] private GameObject uiControllerCanvas;

    [Header("Cinemachine Virtual Camera")]
    [SerializeField] private CinemachineVirtualCamera npcVirtualCamera;
    private int originalCameraPriority = 10;

    [Header("Quest Display References")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text questStatusText;
    [SerializeField] private Transform tasksContainer;
    [SerializeField] private Transform rewardsContainer;

    [Header("UI Buttons")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button okayButton;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button backButton;

    [Header("Prefabs")]
    [SerializeField] private GameObject taskItemPrefab;
    [SerializeField] private GameObject rewardItemPrefab;

    [Header("Director")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDelay = 1f;
    [SerializeField] private CanvasGroup missionCanvasGroup;

    [Header("Audio")]
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
    [SerializeField] private float rewardDelay = 1f; // 1 second delay before showing rewards

    private Quest currentQuest;
    private bool playerInRange = false;
    private bool isPlayingTimeline = false;
    private bool isExiting = false;
    private bool hasPlayedTimelineForCurrentQuest = false;
    private List<TaskItemUI> activeTaskItems = new List<TaskItemUI>();
    private Player_Data playerData;

    private void Start()
    {
        if (questButton != null)
            questButton.SetActive(false);

        if (missionCanvas != null)
            missionCanvas.SetActive(false);

        if (npcVirtualCamera != null)
            originalCameraPriority = npcVirtualCamera.Priority;

        if (questDatabase != null)
        {
            questDatabase.InitializeDatabase();
            Debug.Log($"QuestDatabase initialized with {questDatabase.kingdoms.Count} kingdoms");
        }
        else
        {
            Debug.LogError("QuestDatabase is not assigned in the inspector!");
        }

        SetupButtonListeners();

        if (missionCanvasGroup == null && missionCanvas != null)
        {
            missionCanvasGroup = missionCanvas.GetComponent<CanvasGroup>();
        }

        // Find the main canvas if not assigned
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogWarning("No Canvas found in scene! Reward feedback will not display correctly.");
            }
        }

        // Find Player_Data
        playerData = FindObjectOfType<Player_Data>();
        if (playerData == null)
        {
            Debug.LogWarning("Player_Data not found in scene! UI will not update automatically.");
        }

        // Validate task item prefab
        if (taskItemPrefab != null)
        {
            // Ensure the prefab has TaskItemUI component
            if (taskItemPrefab.GetComponent<TaskItemUI>() == null)
            {
                Debug.LogWarning("TaskItemPrefab is missing TaskItemUI component. Adding one...");
                taskItemPrefab.AddComponent<TaskItemUI>();
            }
        }
    }

    private void SetupButtonListeners()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(OnAcceptButtonClicked);
            acceptButton.gameObject.SetActive(false);
        }

        if (okayButton != null)
        {
            okayButton.onClick.RemoveAllListeners();
            okayButton.onClick.AddListener(OnOkayButtonClicked);
            okayButton.gameObject.SetActive(false);
        }

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimButtonClicked);
            claimButton.gameObject.SetActive(false);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayButtonClicked);
            replayButton.gameObject.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            CheckForAvailableQuest();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HideQuestButton();

            // Ensure camera priority is set to 0 when player leaves
            if (npcVirtualCamera != null)
            {
                npcVirtualCamera.Priority = 0;
                Debug.Log($"NPC virtual camera priority set to 0 on trigger exit");
            }
        }
    }

    private void CheckForAvailableQuest()
    {
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase is null!");
            HideQuestButton();
            return;
        }

        Debug.Log($"Checking for quests in kingdom: {kingdomID}");
        Debug.Log($"Allowed categories: {string.Join(", ", allowedCategories)}");

        var kingdomQuests = questDatabase.GetQuestsByKingdom(kingdomID);

        if (kingdomQuests == null || kingdomQuests.Count == 0)
        {
            Debug.Log($"No quests found in kingdom: {kingdomID}");
            HideQuestButton();
            return;
        }

        Debug.Log($"Found {kingdomQuests.Count} quests in kingdom");

        // Find the first quest that matches criteria (NotStarted, InProgress, or Completed)
        foreach (var quest in kingdomQuests)
        {
            Debug.Log($"Checking quest: {quest.questName}, Status: {quest.status}, Category: {quest.category}");

            bool hasValidStatus = quest.status == QuestStatus.NotStarted ||
                                  quest.status == QuestStatus.InProgress ||
                                  quest.status == QuestStatus.Completed;

            bool hasAllowedCategory = allowedCategories.Contains(quest.category);

            if (hasValidStatus && hasAllowedCategory)
            {
                currentQuest = quest;
                if (hasPlayedTimelineForCurrentQuest && quest.questID != currentQuest?.questID)
                {
                    hasPlayedTimelineForCurrentQuest = false;
                }
                Debug.Log($"Found available quest: {quest.questName}, Status: {quest.status}, Category: {quest.category}");
                ShowQuestButton();
                return;
            }
        }

        Debug.Log($"No available quests matching criteria in kingdom: {kingdomID}");
        HideQuestButton();
    }

    private void ShowQuestButton()
    {
        if (questButton != null)
        {
            questButton.SetActive(true);
            Debug.Log("Quest button shown");
        }
    }

    private void HideQuestButton()
    {
        if (questButton != null)
        {
            questButton.SetActive(false);
            Debug.Log("Quest button hidden");
        }
    }

    public void OnQuestButtonClicked()
    {
        Debug.Log("===== QUEST BUTTON CLICKED =====");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        if (currentQuest == null)
        {
            Debug.LogError("Current quest is null!");
            return;
        }

        Debug.Log($"Processing quest: {currentQuest.questName}");
        Debug.Log($"Quest status: {currentQuest.status}");
        Debug.Log($"Has timeline asset: {currentQuest.timelineAsset != null}");

        HideQuestButton();

        // Set camera priority to 30 when opening quest
        if (npcVirtualCamera != null)
        {
            npcVirtualCamera.Priority = 30;
            Debug.Log($"Camera priority set to: {npcVirtualCamera.Priority}");
        }

        if (uiControllerCanvas != null)
        {
            uiControllerCanvas.SetActive(false);
            Debug.Log("UIControllerCanvas disabled");
        }

        bool shouldPlayTimelineAutomatically =
            currentQuest.timelineAsset != null &&
            playableDirector != null &&
            currentQuest.status == QuestStatus.NotStarted &&
            !hasPlayedTimelineForCurrentQuest;

        if (shouldPlayTimelineAutomatically)
        {
            Debug.Log("Playing timeline automatically for new quest...");
            PlayTimeline();
            hasPlayedTimelineForCurrentQuest = true;
        }
        else
        {
            Debug.Log("Showing mission canvas immediately...");
            ShowMissionCanvas();
        }
    }

    private void PlayTimeline()
    {
        if (playableDirector == null || currentQuest.timelineAsset == null)
        {
            Debug.LogError("Cannot play timeline - missing director or timeline asset");
            ShowMissionCanvas();
            return;
        }

        Debug.Log($"Setting timeline asset: {currentQuest.timelineAsset.name}");

        playableDirector.playableAsset = currentQuest.timelineAsset;
        playableDirector.stopped += OnTimelineStopped;
        playableDirector.Play();

        isPlayingTimeline = true;
        Debug.Log("Timeline started playing");
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        Debug.Log("Timeline stopped!");
        director.stopped -= OnTimelineStopped;
        ShowMissionCanvas();
        isPlayingTimeline = false;
    }

    private void ShowMissionCanvas()
    {
        Debug.Log("Showing mission canvas...");

        UpdateQuestUI();
        UpdateButtonVisibility();
        StartCoroutine(FadeInMissionCanvas());
    }

    private void UpdateButtonVisibility()
    {
        UpdateReplayButtonVisibility();

        if (currentQuest != null)
        {
            if (acceptButton != null) acceptButton.gameObject.SetActive(false);
            if (okayButton != null) okayButton.gameObject.SetActive(false);
            if (claimButton != null) claimButton.gameObject.SetActive(false);

            switch (currentQuest.status)
            {
                case QuestStatus.NotStarted:
                    if (acceptButton != null)
                    {
                        acceptButton.gameObject.SetActive(true);
                        Debug.Log("Accept button shown (quest is NotStarted)");
                    }
                    break;

                case QuestStatus.InProgress:
                    if (okayButton != null)
                    {
                        okayButton.gameObject.SetActive(true);
                        Debug.Log("Okay button shown (quest is InProgress)");
                    }
                    break;

                case QuestStatus.Completed:
                    if (claimButton != null)
                    {
                        claimButton.gameObject.SetActive(true);
                        Debug.Log("Claim button shown (quest is Completed)");
                    }
                    break;
            }
        }
    }

    private void UpdateReplayButtonVisibility()
    {
        if (replayButton != null)
        {
            bool hasTimelineAndDirector = currentQuest.timelineAsset != null && playableDirector != null;
            replayButton.gameObject.SetActive(hasTimelineAndDirector);

            if (hasTimelineAndDirector)
            {
                Debug.Log($"Replay button shown - Quest has timeline: {currentQuest.timelineAsset.name}");
            }
            else
            {
                Debug.Log("Replay button hidden - No timeline or director available");
            }
        }
    }

    private IEnumerator FadeInMissionCanvas()
    {
        isExiting = true;
        Debug.Log("isExit set to TRUE - Starting fade sequence");

        yield return new WaitForSeconds(fadeDelay);

        isExiting = false;
        Debug.Log("isExit set to FALSE - Showing canvas");

        if (missionCanvas != null)
        {
            missionCanvas.SetActive(true);

            if (missionCanvasGroup != null)
            {
                missionCanvasGroup.alpha = 0f;
                float timer = 0f;
                float fadeDuration = 0.5f;

                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    missionCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                    yield return null;
                }
                missionCanvasGroup.alpha = 1f;
            }

            Debug.Log("Mission canvas is now active");
        }
        else
        {
            Debug.LogError("Mission canvas reference is null!");
        }
    }

    private void UpdateQuestUI()
    {
        if (currentQuest == null)
        {
            Debug.LogError("Cannot update quest UI - current quest is null!");
            return;
        }

        Debug.Log($"Updating UI for quest: {currentQuest.questName}");

        if (questNameText != null)
        {
            questNameText.text = currentQuest.questName;
        }

        if (questDescriptionText != null)
        {
            questDescriptionText.text = currentQuest.description;
        }

        if (questStatusText != null)
        {
            questStatusText.text = currentQuest.status.ToString();
        }

        ClearContainers();

        // Display tasks using TaskItemUI
        if (tasksContainer != null && taskItemPrefab != null)
        {
            Debug.Log($"Displaying {currentQuest.tasks.Count} tasks");
            activeTaskItems.Clear();

            foreach (var task in currentQuest.tasks)
            {
                GameObject taskObj = Instantiate(taskItemPrefab, tasksContainer);
                TaskItemUI taskItem = taskObj.GetComponent<TaskItemUI>();

                if (taskItem != null)
                {
                    taskItem.Setup(task);
                    activeTaskItems.Add(taskItem);
                    Debug.Log($"Task item created and setup for: {task.description}");
                }
                else
                {
                    Debug.LogError("TaskItemPrefab is missing TaskItemUI component!");
                    // Try to add it dynamically
                    taskItem = taskObj.AddComponent<TaskItemUI>();
                    taskItem.Setup(task);
                    activeTaskItems.Add(taskItem);
                }
            }
        }

        // Display rewards
        if (rewardsContainer != null && rewardItemPrefab != null)
        {
            Debug.Log($"Displaying {currentQuest.rewards.Count} rewards");
            foreach (var reward in currentQuest.rewards)
            {
                GameObject rewardObj = Instantiate(rewardItemPrefab, rewardsContainer);
                RewardItemUI rewardItem = rewardObj.GetComponent<RewardItemUI>();
                if (rewardItem != null)
                {
                    rewardItem.Setup(reward);
                }
            }
        }
    }

    private void ClearContainers()
    {
        if (tasksContainer != null)
        {
            int childCount = tasksContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(tasksContainer.GetChild(i).gameObject);
            }
            activeTaskItems.Clear();
        }

        if (rewardsContainer != null)
        {
            int childCount = rewardsContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Destroy(rewardsContainer.GetChild(i).gameObject);
            }
        }
    }

    private void OnReplayButtonClicked()
    {
        Debug.Log("Replay button clicked!");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        if (currentQuest != null && currentQuest.timelineAsset != null && playableDirector != null)
        {
            StartCoroutine(FadeOutAndReplay());
        }
        else
        {
            Debug.LogError("Cannot replay - missing timeline asset or playable director!");
        }
    }

    private IEnumerator FadeOutAndReplay()
    {
        if (missionCanvasGroup != null && missionCanvas != null && missionCanvas.activeSelf)
        {
            float timer = 0f;
            float fadeDuration = 0.3f;
            float startAlpha = missionCanvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                missionCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / fadeDuration);
                yield return null;
            }
        }

        if (missionCanvas != null)
            missionCanvas.SetActive(false);

        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (okayButton != null) okayButton.gameObject.SetActive(false);
        if (claimButton != null) claimButton.gameObject.SetActive(false);

        PlayTimeline();
        hasPlayedTimelineForCurrentQuest = true;
    }

    private void OnAcceptButtonClicked()
    {
        Debug.Log("Accept button clicked!");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        if (currentQuest != null && questDatabase != null)
        {
            if (currentQuest.status == QuestStatus.NotStarted)
            {
                currentQuest.StartQuest();
                Debug.Log($"Quest accepted and status updated to: {currentQuest.status}");
            }

            // IMMEDIATE reset for accept button
            ImmediateReset();
            CheckForAvailableQuest();
        }
        else
        {
            Debug.LogError("Cannot accept quest - currentQuest or questDatabase is null!");
        }
    }

    private void OnOkayButtonClicked()
    {
        Debug.Log("Okay button clicked! (Quest is already InProgress)");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        // IMMEDIATE reset for okay button
        ImmediateReset();

        if (playerInRange)
        {
            Debug.Log("Player still in range - checking for available quests");
            CheckForAvailableQuest();
        }
    }

    private void OnClaimButtonClicked()
    {
        Debug.Log("Claim button clicked!");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        if (currentQuest != null && GameDataManager.Instance != null)
        {
            // Start the immediate reset and delayed rewards process
            StartCoroutine(ImmediateResetAndDelayedRewards());
        }
        else
        {
            Debug.LogError("Cannot claim rewards - currentQuest or GameDataManager is null!");
        }
    }

    private IEnumerator ImmediateResetAndDelayedRewards()
    {
        // STEP 1: IMMEDIATELY reset camera and show UI controller
        Debug.Log("STEP 1: Immediately resetting camera and UI...");
        ImmediateReset();

        // Mark quest as claimed
        currentQuest.ClaimQuest();

        // Save game data
        GameDataManager.Instance.SaveGameData();

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
        yield return StartCoroutine(rewardProcessor.ProcessRewards(currentQuest.rewards, () => {
            Debug.Log("All rewards processed");
        }));

        // Check for next available quest
        yield return new WaitForSeconds(0.2f);
        CheckForAvailableQuest();
    }

    private void ImmediateReset()
    {
        Debug.Log("Immediate reset - camera to 0, UI controller enabled");

        // Set camera priority to 0 immediately
        if (npcVirtualCamera != null)
        {
            npcVirtualCamera.Priority = 0;
            Debug.Log($"NPC virtual camera priority set to 0 immediately");
        }

        // Hide mission canvas immediately
        if (missionCanvas != null)
        {
            missionCanvas.SetActive(false);
            Debug.Log("Mission canvas hidden immediately");
        }

        // Hide all buttons immediately
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (acceptButton != null) acceptButton.gameObject.SetActive(false);
        if (okayButton != null) okayButton.gameObject.SetActive(false);
        if (claimButton != null) claimButton.gameObject.SetActive(false);

        // Re-enable main UI controller canvas immediately
        if (uiControllerCanvas != null)
        {
            uiControllerCanvas.SetActive(true);
            Debug.Log("UIControllerCanvas enabled immediately");
        }

        // Stop any playing timeline
        if (playableDirector != null && playableDirector.state == PlayState.Playing)
        {
            playableDirector.Stop();
            Debug.Log("Timeline stopped");
        }
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

    // ========== REWARD FEEDBACK METHODS ==========

    private void ShowCoinRewardFeedback(int amount)
    {
        if (coinRewardFeedbackPrefab == null)
        {
            Debug.LogWarning("Coin Reward Feedback Prefab is not assigned!");
            return;
        }

        if (coinRewardSpawnPoint == null)
        {
            Debug.LogWarning("Coin Reward Spawn Point is not assigned!");
            return;
        }

        ShowRewardFeedback(coinRewardFeedbackPrefab, coinRewardSpawnPoint, amount, coinSuffix);
    }

    private void ShowGemRewardFeedback(int amount)
    {
        if (gemRewardFeedbackPrefab == null)
        {
            Debug.LogWarning("Gem Reward Feedback Prefab is not assigned!");
            return;
        }

        if (gemRewardSpawnPoint == null)
        {
            Debug.LogWarning("Gem Reward Spawn Point is not assigned!");
            return;
        }

        ShowRewardFeedback(gemRewardFeedbackPrefab, gemRewardSpawnPoint, amount, gemSuffix);
    }

    private void ShowRewardFeedback(GameObject prefab, RectTransform spawnPoint, int amount, string suffix)
    {
        if (parentCanvas == null)
        {
            Debug.LogWarning("Parent Canvas is not assigned! Cannot show reward feedback.");
            return;
        }

        if (amount <= 0) return; // Don't show feedback for zero amount

        // Spawn the feedback object as a child of the canvas
        GameObject feedbackObject = Instantiate(prefab, parentCanvas.transform);

        // Get the RectTransform component
        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();

        // Position it at the spawn point
        if (spawnPoint != null)
        {
            // Copy the position from the spawn point
            rectTransform.position = spawnPoint.position;

            // Optional: Copy anchor settings from spawn point
            rectTransform.anchorMin = spawnPoint.anchorMin;
            rectTransform.anchorMax = spawnPoint.anchorMax;
            rectTransform.pivot = spawnPoint.pivot;
        }

        // Get the text component
        TMP_Text feedbackText = feedbackObject.GetComponentInChildren<TMP_Text>();
        if (feedbackText != null)
        {
            feedbackText.text = $"{feedbackPrefix}{amount}{suffix}";
        }

        // Start the animation coroutine
        StartCoroutine(AnimateRewardFeedback(feedbackObject));
    }

    private IEnumerator AnimateRewardFeedback(GameObject feedbackObject)
    {
        if (feedbackObject == null) yield break;

        RectTransform rectTransform = feedbackObject.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = feedbackObject.GetComponent<CanvasGroup>();

        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
        {
            canvasGroup = feedbackObject.AddComponent<CanvasGroup>();
        }

        // Store the starting anchored position (for UI elements)
        Vector2 startAnchoredPosition = rectTransform.anchoredPosition;
        Vector2 endAnchoredPosition = startAnchoredPosition + new Vector2(0, feedbackSlideUpAmount);

        float elapsedTime = 0f;

        // Slide up animation
        while (elapsedTime < feedbackSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / feedbackSlideDuration;

            // Smooth step for easing
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Move upward using anchoredPosition (better for UI)
            rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPosition, endAnchoredPosition, smoothT);

            yield return null;
        }

        // Fade out animation
        elapsedTime = 0f;
        while (elapsedTime < feedbackFadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / feedbackFadeOutDuration;

            canvasGroup.alpha = Mathf.Lerp(1, 0, t);

            yield return null;
        }

        // Destroy the feedback object
        Destroy(feedbackObject);
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked!");

        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        // IMMEDIATE reset for back button
        ImmediateReset();

        if (playerInRange)
        {
            Debug.Log("Player still in range - checking for available quests");
            CheckForAvailableQuest();
        }
    }

    private void Update()
    {
        if (playerInRange && currentQuest != null)
        {
            if (currentQuest.status == QuestStatus.Completed)
            {
                CheckForAvailableQuest();
            }
        }
    }

    public bool IsExiting => isExiting;

    public void ForceCheckForQuests()
    {
        CheckForAvailableQuest();
    }
}