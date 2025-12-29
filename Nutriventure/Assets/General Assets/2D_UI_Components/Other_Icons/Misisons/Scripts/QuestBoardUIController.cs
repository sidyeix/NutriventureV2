using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private TextMeshProUGUI questTrackerText; // Single tracker text for both categories

    [Header("Settings")]
    [SerializeField] private Color selectedButtonColor = new Color(1f, 1f, 1f, 1f); // Alpha 255
    [SerializeField] private Color deselectedButtonColor = new Color(1f, 1f, 1f, 0f); // Alpha 0

    private QuestManager questManager;
    private QuestCategory currentCategory = QuestCategory.GeneralQuest; // Default to General
    private List<QuestUIItem> currentQuestItems = new List<QuestUIItem>();

    private void Awake()
    {
        questManager = QuestManager.Instance;

        if (questManager == null)
        {
            Debug.LogError("QuestManager.Instance is null! Make sure QuestManager is in the scene and has been initialized.");

            // Try to find it manually
            questManager = FindObjectOfType<QuestManager>();

            if (questManager == null)
            {
                Debug.LogError("No QuestManager found in scene! The quest system won't work.");
                return;
            }
        }

        // Button listeners
        generalButton.onClick.AddListener(() => OnCategoryButtonClicked(QuestCategory.GeneralQuest));
        kingdomsButton.onClick.AddListener(() => OnCategoryButtonClicked(QuestCategory.MainStory));

        // Initial setup
        if (questManager != null)
        {
            OnCategoryButtonClicked(QuestCategory.GeneralQuest);
        }
    }

    private void OnEnable()
    {
        RefreshQuestList();
    }

    private void OnCategoryButtonClicked(QuestCategory category)
    {
        currentCategory = category;
        UpdateButtonStates();
        RefreshQuestList();
        UpdateQuestTracker(); // Update tracker when category changes
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
        generalButton.interactable = !isGeneralSelected;
        kingdomsButton.interactable = !isKingdomsSelected;
    }

    private void RefreshQuestList()
    {
        ClearQuestList();

        List<Quest> filteredQuests = GetFilteredQuests();

        if (filteredQuests.Count == 0)
        {
            ShowNoQuestsMessage();
            UpdateQuestTracker(); // Still update tracker even if no quests
            return;
        }

        ShowQuestList();

        foreach (var quest in filteredQuests)
        {
            GameObject questObject = Instantiate(questContainerPrefab, contentContainer);
            QuestUIItem questUI = questObject.GetComponent<QuestUIItem>();

            if (questUI != null)
            {
                questUI.SetupQuest(quest);
                currentQuestItems.Add(questUI);
            }
        }

        UpdateQuestTracker(); // Update tracker after refreshing list
    }

    private List<Quest> GetFilteredQuests()
    {
        List<Quest> filteredQuests = new List<Quest>();

        // Check if questManager exists
        if (questManager == null)
        {
            Debug.LogError("QuestManager is null! Cannot get quests.");
            return filteredQuests;
        }

        var allKingdoms = questManager.GetAllKingdoms();

        // Check if allKingdoms is null
        if (allKingdoms == null)
        {
            Debug.LogError("GetAllKingdoms() returned null!");
            return filteredQuests;
        }

        foreach (var kingdom in allKingdoms)
        {
            if (kingdom == null) continue;

            foreach (var quest in kingdom.quests)
            {
                if (quest == null) continue;

                if (currentCategory == QuestCategory.GeneralQuest) // General Quests
                {
                    if (quest.category == QuestCategory.GeneralQuest)
                    {
                        filteredQuests.Add(quest);
                    }
                }
                else if (currentCategory == QuestCategory.MainStory) // Kingdom Quests
                {
                    if (quest.category == QuestCategory.MainStory &&
                        quest.status == QuestStatus.InProgress)
                    {
                        filteredQuests.Add(quest);
                    }
                }
            }
        }

        // Sort quests: InProgress first, then Completed, then NotStarted
        filteredQuests = filteredQuests.OrderBy(q =>
        {
            return q.status switch
            {
                QuestStatus.InProgress => 0,
                QuestStatus.Completed => 1,
                QuestStatus.NotStarted => 2,
                _ => 3
            };
        }).ThenBy(q => q.questName).ToList();

        return filteredQuests;
    }

    private void UpdateQuestTracker()
    {
        if (questTrackerText == null || questManager == null) return;

        int totalQuests = 0;
        int completedQuests = 0;

        var allKingdoms = questManager.GetAllKingdoms();
        if (allKingdoms == null) return;

        foreach (var kingdom in allKingdoms)
        {
            if (kingdom == null || kingdom.quests == null) continue;

            foreach (var quest in kingdom.quests)
            {
                if (quest == null) continue;

                // Count based on current category
                if (currentCategory == QuestCategory.GeneralQuest &&
                    quest.category != QuestCategory.GeneralQuest) continue;

                if (currentCategory == QuestCategory.MainStory &&
                    quest.category != QuestCategory.MainStory) continue;

                totalQuests++;

                // Count as completed if status is Completed OR Claimed
                if (quest.status == QuestStatus.Completed || quest.status == QuestStatus.Claimed)
                {
                    completedQuests++;
                }
            }
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
                    ? "No active kingdom quests available. Complete previous quests to unlock more!"
                    : "No general quests available at the moment. Check back later!";

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

    // Call this when quests are updated
    public void OnQuestsUpdated()
    {
        RefreshQuestList();
        UpdateQuestTracker(); // Also update tracker
    }

    // Public method to manually refresh from other scripts
    public void RefreshUI()
    {
        RefreshQuestList();
    }

    // Public method to switch to specific category
    public void SwitchToCategory(QuestCategory category)
    {
        OnCategoryButtonClicked(category);
    }

    // Public method to get current category
    public QuestCategory GetCurrentCategory()
    {
        return currentCategory;
    }

    // Public method to show/hide the entire UI
    public void SetUIVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (visible)
        {
            RefreshQuestList();
        }
    }
}