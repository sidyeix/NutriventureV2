using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("Kingdoms")]
    public List<Kingdom> kingdoms = new List<Kingdom>();

    private Dictionary<string, Quest> questDictionary = new Dictionary<string, Quest>();
    private Dictionary<string, Kingdom> kingdomDictionary = new Dictionary<string, Kingdom>();

    #region Initialization

    public void InitializeDatabase()
    {
        BuildDictionaries();
        ValidateAllQuests();
    }

    private void BuildDictionaries()
    {
        questDictionary.Clear();
        kingdomDictionary.Clear();

        foreach (var kingdom in kingdoms)
        {
            if (!string.IsNullOrEmpty(kingdom.kingdomID))
            {
                kingdomDictionary[kingdom.kingdomID] = kingdom;

                foreach (var quest in kingdom.quests)
                {
                    if (!string.IsNullOrEmpty(quest.questID))
                    {
                        questDictionary[quest.questID] = quest;
                    }
                }
            }
        }
    }

    private void ValidateAllQuests()
    {
        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                ValidateQuest(quest);
            }
        }
    }

    private void ValidateQuest(Quest quest)
    {
        // Validate required quests exist
        foreach (var requiredID in quest.requiredQuestIDs)
        {
            if (!questDictionary.ContainsKey(requiredID))
            {
                Debug.LogWarning($"Quest {quest.questID} requires non-existent quest: {requiredID}");
            }
        }

        // Ensure all tasks have IDs
        for (int i = 0; i < quest.tasks.Count; i++)
        {
            if (string.IsNullOrEmpty(quest.tasks[i].taskID))
            {
                quest.tasks[i].taskID = $"{quest.questID}_task_{i}";
            }
        }
    }

    #endregion

    #region Quest Access

    public Quest GetQuest(string questID)
    {
        questDictionary.TryGetValue(questID, out Quest quest);
        if (quest == null)
            Debug.LogWarning($"Quest not found: {questID}");
        return quest;
    }

    public List<Quest> GetQuestsByKingdom(string kingdomID)
    {
        if (kingdomDictionary.TryGetValue(kingdomID, out Kingdom kingdom))
            return new List<Quest>(kingdom.quests);

        Debug.LogWarning($"Kingdom not found: {kingdomID}");
        return new List<Quest>();
    }

    public List<Quest> GetQuestsByStatus(QuestStatus status)
    {
        List<Quest> filteredQuests = new List<Quest>();

        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.status == status)
                {
                    filteredQuests.Add(quest);
                }
            }
        }

        return filteredQuests;
    }

    public List<Quest> GetAvailableQuests(int playerLevel)
    {
        List<Quest> availableQuests = new List<Quest>();

        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.status == QuestStatus.NotStarted &&
                    quest.requiredLevel <= playerLevel &&
                    ArePrerequisitesMet(quest))
                {
                    availableQuests.Add(quest);
                }
            }
        }

        return availableQuests;
    }

    private bool ArePrerequisitesMet(Quest quest)
    {
        foreach (var requiredID in quest.requiredQuestIDs)
        {
            Quest requiredQuest = GetQuest(requiredID);
            if (requiredQuest == null || requiredQuest.status != QuestStatus.Completed)
                return false;
        }
        return true;
    }

    // NEW: Get quests by category
    public List<Quest> GetQuestsByCategory(QuestCategory category)
    {
        List<Quest> filteredQuests = new List<Quest>();

        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.category == category)
                {
                    filteredQuests.Add(quest);
                }
            }
        }

        return filteredQuests;
    }

    // NEW: Get quests by category and status
    public List<Quest> GetQuestsByCategoryAndStatus(QuestCategory category, QuestStatus status)
    {
        List<Quest> filteredQuests = new List<Quest>();

        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.category == category && quest.status == status)
                {
                    filteredQuests.Add(quest);
                }
            }
        }

        return filteredQuests;
    }

    #endregion

    #region Task Management

    public bool UpdateTaskProgress(string questID, string taskID, int amount = 1)
    {
        Quest quest = GetQuest(questID);
        if (quest == null || quest.status != QuestStatus.InProgress)
            return false;

        QuestTask task = quest.GetTask(taskID);
        if (task == null)
            return false;

        task.UpdateProgress(amount);

        // Check if quest is complete
        if (quest.AllTasksComplete)
        {
            quest.CompleteQuest();
        }

        return true;
    }

    public bool CompleteTask(string questID, string taskID)
    {
        Quest quest = GetQuest(questID);
        if (quest == null)
            return false;

        QuestTask task = quest.GetTask(taskID);
        if (task == null)
            return false;

        task.MarkAsComplete(); // Use the new method

        if (quest.AllTasksComplete)
        {
            quest.CompleteQuest();
        }

        return true;
    }

    #endregion

    #region Kingdom Access

    public Kingdom GetKingdom(string kingdomID)
    {
        kingdomDictionary.TryGetValue(kingdomID, out Kingdom kingdom);
        if (kingdom == null)
            Debug.LogWarning($"Kingdom not found: {kingdomID}");
        return kingdom;
    }

    public List<Kingdom> GetAllKingdoms()
    {
        return new List<Kingdom>(kingdoms);
    }

    public Kingdom GetKingdomForQuest(string questID)
    {
        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.questID == questID)
                {
                    return kingdom;
                }
            }
        }
        return null;
    }

    #endregion

    #region Save/Load System

    [System.Serializable]
    public class QuestSaveData
    {
        public string questID;
        public QuestStatus status;
        public string startTime;
        public string completionTime;
        public List<TaskSaveData> tasks = new List<TaskSaveData>();
    }

    [System.Serializable]
    public class TaskSaveData
    {
        public string taskID;
        public int currentAmount;
        public bool isCompleted;
    }

    public List<QuestSaveData> GetSaveData()
    {
        List<QuestSaveData> saveData = new List<QuestSaveData>();

        foreach (var kingdom in kingdoms)
        {
            foreach (var quest in kingdom.quests)
            {
                var questData = new QuestSaveData
                {
                    questID = quest.questID,
                    status = quest.status,
                    startTime = quest.startTime.ToString("o"),
                    completionTime = quest.completionTime?.ToString("o")
                };

                foreach (var task in quest.tasks)
                {
                    questData.tasks.Add(new TaskSaveData
                    {
                        taskID = task.taskID,
                        currentAmount = task.currentAmount,
                        isCompleted = task.isCompleted
                    });
                }

                saveData.Add(questData);
            }
        }

        return saveData;
    }

    public void LoadSaveData(List<QuestSaveData> saveData)
    {
        foreach (var questData in saveData)
        {
            Quest quest = GetQuest(questData.questID);
            if (quest != null)
            {
                quest.status = questData.status;

                if (DateTime.TryParse(questData.startTime, out DateTime startTime))
                    quest.startTime = startTime;

                if (!string.IsNullOrEmpty(questData.completionTime) &&
                    DateTime.TryParse(questData.completionTime, out DateTime completionTime))
                    quest.completionTime = completionTime;

                foreach (var taskData in questData.tasks)
                {
                    QuestTask task = quest.GetTask(taskData.taskID);
                    if (task != null)
                    {
                        task.currentAmount = taskData.currentAmount;
                        task.isCompleted = taskData.isCompleted;

                        // Ensure consistency
                        if (task.currentAmount >= task.requiredAmount && !task.isCompleted)
                        {
                            task.isCompleted = true;
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    public void AddNewKingdom()
    {
        Kingdom newKingdom = new Kingdom
        {
            kingdomID = $"kingdom_{kingdoms.Count + 1}",
            kingdomName = "New Kingdom",
            quests = new List<Quest>()
        };

        kingdoms.Add(newKingdom);
        BuildDictionaries();
        EditorUtility.SetDirty(this);
    }

    public void AddNewQuestToKingdom(string kingdomID)
    {
        if (kingdomDictionary.TryGetValue(kingdomID, out Kingdom kingdom))
        {
            Quest newQuest = new Quest
            {
                questID = $"{kingdomID}_quest_{kingdom.quests.Count + 1}",
                questName = "New Quest",
                description = "Quest description here",
                questIcon = null,
                category = QuestCategory.MainStory,
                tasks = new List<QuestTask>()
            };

            kingdom.quests.Add(newQuest);
            questDictionary[newQuest.questID] = newQuest;
            EditorUtility.SetDirty(this);
        }
    }

    public void AddNewTaskToQuest(string questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null)
        {
            QuestTask newTask = new QuestTask
            {
                taskID = $"{questID}_task_{quest.tasks.Count + 1}",
                description = "New Task",
                requiredAmount = 1,
                isCompleted = false
            };

            quest.tasks.Add(newTask);
            EditorUtility.SetDirty(this);
        }
    }

    public void OrganizeByKingdoms()
    {
        // Sort kingdoms by name
        kingdoms.Sort((a, b) => a.kingdomName.CompareTo(b.kingdomName));

        // Sort quests within each kingdom
        foreach (var kingdom in kingdoms)
        {
            kingdom.quests.Sort((a, b) => a.questName.CompareTo(b.questName));
        }

        BuildDictionaries();
        EditorUtility.SetDirty(this);
    }

    // NEW: Add General Quests Kingdom
    [ContextMenu("Add General Quests Kingdom")]
    public void AddGeneralQuestsKingdom()
    {
        // Check if General Quests kingdom already exists
        foreach (var kingdom in kingdoms)
        {
            if (kingdom.kingdomID == "general_quests")
            {
                Debug.Log("General Quests kingdom already exists!");
                return;
            }
        }

        Kingdom generalKingdom = new Kingdom
        {
            kingdomID = "general_quests",
            kingdomName = "General Quests",
            kingdomIcon = null,
            quests = new List<Quest>()
        };

        kingdoms.Add(generalKingdom);
        BuildDictionaries();
        EditorUtility.SetDirty(this);
        Debug.Log("General Quests kingdom added!");
    }

    // NEW: Add example General Quest
    [ContextMenu("Add Example General Quest")]
    public void AddExampleGeneralQuest()
    {
        Kingdom generalKingdom = GetKingdom("general_quests");
        if (generalKingdom == null)
        {
            Debug.LogWarning("General Quests kingdom not found. Run 'Add General Quests Kingdom' first.");
            return;
        }

        Quest newQuest = new Quest
        {
            questID = $"general_quest_{generalKingdom.quests.Count + 1}",
            questName = "Example General Quest",
            description = "Complete this example general quest",
            longDescription = "This is a longer description for the example general quest.",
            category = QuestCategory.GeneralQuest,
            questIcon = null,
            requiredLevel = 1,
            status = QuestStatus.NotStarted,
            rewards = new List<QuestReward>()
        };

        // Add a task
        newQuest.tasks.Add(new QuestTask
        {
            taskID = $"{newQuest.questID}_task_1",
            description = "Example task",
            requiredAmount = 5,
            currentAmount = 0
        });

        // Add a reward
        newQuest.rewards.Add(new QuestReward
        {
            rewardName = "Coins",
            rewardIcon = null,
            amount = 100
        });

        generalKingdom.quests.Add(newQuest);
        questDictionary[newQuest.questID] = newQuest;
        EditorUtility.SetDirty(this);
        Debug.Log("Example General Quest added!");
    }

    // NEW: Add example Kingdom Quest
    [ContextMenu("Add Example Kingdom Quest")]
    public void AddExampleKingdomQuest()
    {
        if (kingdoms.Count == 0)
        {
            Debug.LogWarning("No kingdoms exist. Add a kingdom first.");
            return;
        }

        // Use the first kingdom
        Kingdom firstKingdom = kingdoms[0];

        Quest newQuest = new Quest
        {
            questID = $"{firstKingdom.kingdomID}_quest_{firstKingdom.quests.Count + 1}",
            questName = "Example Kingdom Quest",
            description = "Complete this example kingdom quest",
            longDescription = "This is a longer description for the example kingdom quest.",
            category = QuestCategory.MainStory,
            questIcon = null,
            requiredLevel = 1,
            status = QuestStatus.InProgress,
            rewards = new List<QuestReward>()
        };

        // Add a task
        newQuest.tasks.Add(new QuestTask
        {
            taskID = $"{newQuest.questID}_task_1",
            description = "Example task",
            requiredAmount = 3,
            currentAmount = 1
        });

        // Add a reward
        newQuest.rewards.Add(new QuestReward
        {
            rewardName = "Experience",
            rewardIcon = null,
            amount = 50
        });

        firstKingdom.quests.Add(newQuest);
        questDictionary[newQuest.questID] = newQuest;
        EditorUtility.SetDirty(this);
        Debug.Log("Example Kingdom Quest added!");
    }
#endif

    #endregion
}