using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class QuestTaskUpdater : MonoBehaviour
{
    [Header("Quest Selection")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Quest to Update")]
    [SerializeField] private string kingdomID;
    [SerializeField] private string questID;

    [Header("Task Update Settings")]
    [SerializeField] private int progressAmount = 1;
    [SerializeField] private bool updateOnClick = true;

    [Header("Optional UI Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Audio")]
    [SerializeField] private string clickSound = "ButtonClick";

    private Button button;
    private bool hasInitialized = false;

    private void Start()
    {
        button = GetComponent<Button>();

        if (button != null && updateOnClick)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // Validate quest reference
        ValidateQuestReference();
    }

    private void ValidateQuestReference()
    {
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase is not assigned on " + gameObject.name);
            return;
        }

        // Initialize database if needed
        questDatabase.InitializeDatabase();

        // Get quest to verify it exists
        Quest quest = questDatabase.GetQuest(questID);
        if (quest == null)
        {
            Debug.LogWarning($"Quest with ID {questID} not found in database. Will try to find by kingdom and quest name.");

            // Try to find by kingdom and quest name
            if (!string.IsNullOrEmpty(kingdomID))
            {
                var kingdomQuests = questDatabase.GetQuestsByKingdom(kingdomID);
                foreach (var q in kingdomQuests)
                {
                    if (q.questID == questID || q.questName == questID)
                    {
                        quest = q;
                        questID = q.questID; // Update to correct ID
                        Debug.Log($"Found quest: {q.questName} with ID: {q.questID}");
                        break;
                    }
                }
            }
        }

        hasInitialized = true;
    }

    public void OnButtonClick()
    {
        // Play click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        UpdateQuestProgress();
    }

    public void UpdateQuestProgress()
    {
        if (!hasInitialized)
        {
            ValidateQuestReference();
        }

        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase is null!");
            ShowFeedback("Error: Database missing", false);
            return;
        }

        // Get the quest
        Quest quest = questDatabase.GetQuest(questID);
        if (quest == null)
        {
            Debug.LogError($"Quest with ID {questID} not found!");
            ShowFeedback("Quest not found", false);
            return;
        }

        // Check if quest is in progress
        if (quest.status != QuestStatus.InProgress)
        {
            Debug.Log($"Quest {quest.questName} is not in progress. Current status: {quest.status}");

            if (quest.status == QuestStatus.Completed)
            {
                ShowFeedback("Quest already completed!", false);
            }
            else if (quest.status == QuestStatus.NotStarted)
            {
                ShowFeedback("Quest not started yet", false);
            }
            else if (quest.status == QuestStatus.Claimed)
            {
                ShowFeedback("Quest already claimed!", false);
            }

            return;
        }

        // Update the first task (as per requirements)
        if (quest.tasks != null && quest.tasks.Count > 0)
        {
            QuestTask task = quest.tasks[0];

            // Check if task is already completed
            if (task.isCompleted)
            {
                Debug.Log("Task is already completed!");
                ShowFeedback("Task already done!", false);
                return;
            }

            // Update progress
            int newAmount = task.currentAmount + progressAmount;

            // Don't exceed required amount
            if (newAmount > task.requiredAmount)
            {
                newAmount = task.requiredAmount;
            }

            task.currentAmount = newAmount;

            // Check if task is now completed
            if (task.currentAmount >= task.requiredAmount)
            {
                task.isCompleted = true;
                Debug.Log($"Task completed! Progress: {task.currentAmount}/{task.requiredAmount}");
                ShowFeedback("Task completed!", true);

                // Check if all tasks are completed to mark quest as completed
                if (quest.AllTasksComplete)
                {
                    quest.CompleteQuest();
                    Debug.Log($"Quest {quest.questName} completed!");
                    ShowFeedback("Quest completed! Talk to NPC to claim rewards.", true);
                }
            }
            else
            {
                Debug.Log($"Task progress updated: {task.currentAmount}/{task.requiredAmount}");
                ShowFeedback($"Progress: {task.currentAmount}/{task.requiredAmount}", true);
            }
        }
        else
        {
            Debug.LogError("Quest has no tasks!");
            ShowFeedback("No tasks found", false);
        }
    }

    private void ShowFeedback(string message, bool isSuccess)
    {
        if (feedbackText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowFeedbackCoroutine(message, isSuccess));
        }
    }

    private IEnumerator ShowFeedbackCoroutine(string message, bool isSuccess)
    {
        feedbackText.text = message;
        feedbackText.color = isSuccess ? Color.green : Color.red;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(feedbackDuration);

        feedbackText.gameObject.SetActive(false);
    }

    // Editor helper to populate dropdowns
#if UNITY_EDITOR
    [ContextMenu("Find Quest in Database")]
    private void EditorFindQuest()
    {
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase not assigned!");
            return;
        }

        questDatabase.InitializeDatabase();

        // Try to find by quest ID first
        Quest quest = questDatabase.GetQuest(questID);
        if (quest != null)
        {
            Kingdom kingdom = questDatabase.GetKingdomForQuest(questID);
            if (kingdom != null)
            {
                kingdomID = kingdom.kingdomID;
                Debug.Log($"Found quest: {quest.questName} in kingdom: {kingdom.kingdomName}");
            }
            return;
        }

        // If not found, list all available quests
        Debug.Log("=== AVAILABLE QUESTS ===");
        var kingdoms = questDatabase.GetAllKingdoms();
        foreach (var kingdom in kingdoms)
        {
            Debug.Log($"Kingdom: {kingdom.kingdomName} ({kingdom.kingdomID})");
            foreach (var q in kingdom.quests)
            {
                Debug.Log($"  - Quest: {q.questName} (ID: {q.questID})");
            }
        }
    }
#endif

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}