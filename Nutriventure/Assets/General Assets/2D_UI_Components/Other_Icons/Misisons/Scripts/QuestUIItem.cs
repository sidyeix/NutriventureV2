using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class QuestUIItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image questIconImage;
    [SerializeField] private TextMeshProUGUI questNameText;
    [SerializeField] private TextMeshProUGUI questProgressText;
    [SerializeField] private GameObject questProgressContainer;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private GameObject questRewardItemPrefab;
    [SerializeField] private GameObject actionsContainer;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimedText;

    private Quest currentQuest;
    private string currentQuestID;
    private QuestManager questManager;
    private QuestBoardUIController questBoardController;
    private QuestDatabase questDatabase; // Add database reference

    // Reward feedback references
    private GameObject coinRewardPrefab;
    private GameObject gemRewardPrefab;
    private RectTransform coinSpawnPoint;
    private RectTransform gemSpawnPoint;
    private Canvas parentCanvas;
    private float slideDuration;
    private float fadeDuration;
    private float slideUpAmount;
    private string prefix;
    private string coinSuffix;
    private string gemSuffix;
    private AudioClip coinSound;
    private float rewardDelay;

    private void Awake()
    {
        questManager = QuestManager.Instance;

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
    }

    public void Initialize(QuestBoardUIController controller,
        QuestDatabase database, // Add database parameter
        GameObject coinPrefab, GameObject gemPrefab,
        RectTransform coinSpawn, RectTransform gemSpawn,
        Canvas canvas, float slideDur, float fadeDur, float slideAmount,
        string pre, string cSuffix, string gSuffix,
        AudioClip coinClip, float delay)
    {
        questBoardController = controller;
        questDatabase = database; // Store database reference
        coinRewardPrefab = coinPrefab;
        gemRewardPrefab = gemPrefab;
        coinSpawnPoint = coinSpawn;
        gemSpawnPoint = gemSpawn;
        parentCanvas = canvas;
        slideDuration = slideDur;
        fadeDuration = fadeDur;
        slideUpAmount = slideAmount;
        prefix = pre;
        coinSuffix = cSuffix;
        gemSuffix = gSuffix;
        coinSound = coinClip;
        rewardDelay = delay;
    }

    public void SetupQuest(Quest quest)
    {
        currentQuest = quest;
        currentQuestID = quest.questID;
        UpdateUI();
    }

    private void RefreshQuestData()
    {
        if (questDatabase != null && !string.IsNullOrEmpty(currentQuestID))
        {
            // Get fresh quest directly from database (like NPCQuestInteraction)
            Quest freshQuest = questDatabase.GetQuest(currentQuestID);
            if (freshQuest != null)
            {
                currentQuest = freshQuest;
                Debug.Log($"Refreshed quest data from database: {currentQuestID} = {currentQuest.status}");
            }
        }
        else if (questManager != null && !string.IsNullOrEmpty(currentQuestID))
        {
            // Fallback to QuestManager
            Quest freshQuest = questManager.GetQuest(currentQuestID);
            if (freshQuest != null)
            {
                currentQuest = freshQuest;
            }
        }
    }

    private void UpdateUI()
    {
        // Always get fresh data from database before updating UI
        RefreshQuestData();

        if (currentQuest == null) return;

        // Quest Icon
        if (questIconImage != null)
        {
            questIconImage.sprite = currentQuest.questIcon;
            questIconImage.color = currentQuest.questIcon != null ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        // Quest Name
        if (questNameText != null)
        {
            questNameText.text = currentQuest.questName;
        }

        // Quest Progress
        UpdateProgressUI();

        // Quest Description
        if (questDescriptionText != null)
        {
            questDescriptionText.text = currentQuest.description;
        }

        // Rewards
        UpdateRewardsUI();

        // Actions (Claim Button / Claimed Text)
        UpdateActionsUI();
    }

    private void UpdateProgressUI()
    {
        if (questProgressContainer == null || questProgressText == null) return;

        bool hasProgress = false;
        string progressText = "";

        foreach (var task in currentQuest.tasks)
        {
            if (task.requiredAmount > 0)
            {
                hasProgress = true;
                progressText += $"({task.currentAmount}/{task.requiredAmount}) ";
            }
        }

        questProgressContainer.SetActive(hasProgress);

        if (hasProgress)
        {
            questProgressText.text = progressText.Trim();
        }
    }

    private void UpdateRewardsUI()
    {
        ClearRewards();

        if (rewardsContainer == null || questRewardItemPrefab == null) return;

        foreach (var reward in currentQuest.rewards)
        {
            GameObject rewardItem = Instantiate(questRewardItemPrefab, rewardsContainer);
            QuestRewardItemUI rewardUI = rewardItem.GetComponent<QuestRewardItemUI>();

            if (rewardUI != null)
            {
                rewardUI.SetupReward(reward);
            }
        }
    }

    private void ClearRewards()
    {
        if (rewardsContainer == null) return;

        foreach (Transform child in rewardsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void UpdateActionsUI()
    {
        if (actionsContainer == null) return;

        bool showActions = false;
        bool showClaimButton = false;
        bool showClaimedText = false;

        if (currentQuest != null)
        {
            switch (currentQuest.status)
            {
                case QuestStatus.Completed:
                    showActions = true;
                    showClaimButton = true;
                    break;

                case QuestStatus.Claimed:
                    showActions = true;
                    showClaimedText = true;
                    break;

                default:
                    showActions = false;
                    break;
            }
        }

        actionsContainer.SetActive(showActions);

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(showClaimButton);
            claimButton.interactable = showClaimButton;
        }

        if (claimedText != null)
        {
            claimedText.SetActive(showClaimedText);
        }
    }

    private void OnClaimButtonClicked()
    {
        if (currentQuest == null || questBoardController == null) return;

        Debug.Log($"Claiming quest: {currentQuest.questName} (Current Status: {currentQuest.status})");

        // Play click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        // Disable the claim button to prevent double-clicking
        if (claimButton != null)
        {
            claimButton.interactable = false;
        }

        // Start the reward process through the board controller
        StartCoroutine(questBoardController.ProcessQuestRewards(currentQuest, OnRewardsProcessed));
    }

    private void OnRewardsProcessed()
    {
        Debug.Log($"Rewards processed for quest: {currentQuest?.questName}");

        // Refresh data from database and update UI
        RefreshQuestData();
        UpdateUI();

        // Re-enable the button if needed (though it should be hidden now)
        if (claimButton != null)
        {
            claimButton.interactable = true;
        }
    }

    public void RefreshItem()
    {
        RefreshQuestData();
        UpdateUI();
    }
}