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
    [SerializeField] private bool updateOnTrigger = true;
    [SerializeField] private bool destroyOnTrigger = false;
    [SerializeField] private string playerTag = "Player";

    [Header("Trigger Cooldown")]
    [SerializeField] private bool useCooldown = false;
    [SerializeField] private float cooldownDuration = 2f;
    private bool isOnCooldown = false;

    [Header("Optional UI Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Audio")]
    [SerializeField] private string clickSound = "ButtonClick";

    private Button button;
    private Collider triggerCollider;
    private bool hasInitialized = false;

    private void Start()
    {
        // Check for Button component
        button = GetComponent<Button>();
        if (button != null && updateOnClick)
        {
            button.onClick.AddListener(OnButtonClick);
            Debug.Log($"QuestTaskUpdater: Button mode enabled on {gameObject.name}");
        }

        // Check for trigger collider
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                Debug.Log($"QuestTaskUpdater: Trigger mode enabled on {gameObject.name}");
                break;
            }
        }

        // If no trigger found on this object, check children
        if (triggerCollider == null)
        {
            triggerCollider = GetComponentInChildren<Collider>();
            if (triggerCollider != null && !triggerCollider.isTrigger)
            {
                triggerCollider = null;
            }
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

    private void OnTriggerEnter(Collider other)
    {
        // Check if trigger mode is enabled
        if (!updateOnTrigger) return;

        // Check if the entering object has the correct tag
        if (!other.CompareTag(playerTag)) return;

        // Check cooldown
        if (useCooldown && isOnCooldown) return;

        Debug.Log($"QuestTaskUpdater: Player entered trigger on {gameObject.name}");

        // Update quest progress
        UpdateQuestProgress();

        // Start cooldown if enabled
        if (useCooldown)
        {
            StartCoroutine(CooldownCoroutine());
        }

        // Destroy object if enabled
        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Optional: Implement stay logic if needed
        // For now, we'll leave this empty
    }

    private void OnTriggerExit(Collider other)
    {
        // Optional: Implement exit logic if needed
    }

    private IEnumerator CooldownCoroutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
    }

    public void OnButtonClick()
    {
        // Check if button mode is enabled
        if (!updateOnClick) return;

        // Play click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        Debug.Log($"QuestTaskUpdater: Button clicked on {gameObject.name}");
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

        Debug.Log($"QuestTaskUpdater: Updating quest - {quest.questName}, Current Status: {quest.status}");

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

            Debug.Log($"QuestTaskUpdater: Current task progress - {task.currentAmount}/{task.requiredAmount}");

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
            Debug.Log($"QuestTaskUpdater: Task progress updated to {task.currentAmount}/{task.requiredAmount}");

            // Mark the database as dirty in editor
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(questDatabase);
#endif

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

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(questDatabase);
#endif
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

    // Public method to manually trigger progress update from other scripts
    public void ManualTriggerUpdate()
    {
        UpdateQuestProgress();
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

    [ContextMenu("Detect Interaction Type")]
    private void DetectInteractionType()
    {
        Button btn = GetComponent<Button>();
        Collider[] colliders = GetComponents<Collider>();
        bool hasTrigger = false;

        foreach (Collider col in colliders)
        {
            if (col.isTrigger)
            {
                hasTrigger = true;
                break;
            }
        }

        if (btn != null)
        {
            Debug.Log($"GameObject {gameObject.name} has a Button component - will use click interaction");
        }

        if (hasTrigger)
        {
            Debug.Log($"GameObject {gameObject.name} has a Trigger Collider - will use trigger interaction");
        }

        if (btn == null && !hasTrigger)
        {
            Debug.LogWarning($"GameObject {gameObject.name} has neither Button nor Trigger Collider! No interaction will work.");
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

    private void OnDrawGizmosSelected()
    {
        // Visualize trigger area in editor
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider col in colliders)
        {
            if (col != null && col.isTrigger)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);

                if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
                else if (col is CapsuleCollider capsule)
                {
                    // Simple visualization for capsule
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireSphere(capsule.center, capsule.radius);
                }
                break; // Only draw the first trigger
            }
        }
    }
}