using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    private QuestManager questManager;

    private void Awake()
    {
        questManager = QuestManager.Instance;
        claimButton.onClick.AddListener(OnClaimButtonClicked);
    }

    public void SetupQuest(Quest quest)
    {
        currentQuest = quest;
        UpdateUI();
    }

    private void UpdateUI()
    {
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

        // Check if any task has required amount > 0
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
        if (currentQuest == null) return;

        Debug.Log($"Claiming quest: {currentQuest.questName}");

        // Call QuestManager to claim the quest
        if (questManager != null)
        {
            // You'll need to add a ClaimQuest method to QuestManager
            // For now, we'll just update the UI
            currentQuest.status = QuestStatus.Claimed;
            UpdateActionsUI();

            // Also update rewards UI to show they've been claimed
            foreach (var reward in currentQuest.rewards)
            {
                reward.GrantReward();
            }

            // Refresh the quest board
            QuestBoardUIController questBoard = FindObjectOfType<QuestBoardUIController>();
            if (questBoard != null)
            {
                questBoard.RefreshUI();
            }
        }
    }

    // Public method to refresh this item
    public void RefreshItem()
    {
        UpdateUI();
    }
}