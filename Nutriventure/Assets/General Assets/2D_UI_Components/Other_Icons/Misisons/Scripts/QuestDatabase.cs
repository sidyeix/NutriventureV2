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

    [Header("All Quests")]
    public List<Quest> allQuests = new List<Quest>();

    private Dictionary<string, Quest> questDictionary = new Dictionary<string, Quest>();
    private Dictionary<string, Kingdom> kingdomDictionary = new Dictionary<string, Kingdom>();

    #region Initialization

    public void InitializeDatabase()
    {
        BuildDictionaries();

        // Validate quest references
        foreach (var quest in allQuests)
        {
            ValidateQuest(quest);
        }
    }

    private void BuildDictionaries()
    {
        questDictionary.Clear();
        kingdomDictionary.Clear();

        foreach (var quest in allQuests)
        {
            if (!string.IsNullOrEmpty(quest.questID))
            {
                questDictionary[quest.questID] = quest;
            }
        }

        foreach (var kingdom in kingdoms)
        {
            if (!string.IsNullOrEmpty(kingdom.kingdomID))
            {
                kingdomDictionary[kingdom.kingdomID] = kingdom;
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
        if (questDictionary.ContainsKey(questID))
            return questDictionary[questID];

        Debug.LogWarning($"Quest not found: {questID}");
        return null;
    }

    public List<Quest> GetQuestsByKingdom(string kingdomID)
    {
        List<Quest> kingdomQuests = new List<Quest>();

        foreach (var quest in allQuests)
        {
            if (quest.kingdom != null && quest.kingdom.kingdomID == kingdomID)
            {
                kingdomQuests.Add(quest);
            }
        }

        return kingdomQuests;
    }

    public List<Quest> GetQuestsByStatus(QuestStatus status)
    {
        List<Quest> filteredQuests = new List<Quest>();

        foreach (var quest in allQuests)
        {
            if (quest.status == status)
            {
                filteredQuests.Add(quest);
            }
        }

        return filteredQuests;
    }

    public List<Quest> GetAvailableQuests(int playerLevel)
    {
        List<Quest> availableQuests = new List<Quest>();

        foreach (var quest in allQuests)
        {
            if (quest.status == QuestStatus.NotStarted &&
                quest.requiredLevel <= playerLevel &&
                ArePrerequisitesMet(quest))
            {
                availableQuests.Add(quest);
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

        task.currentAmount = task.requiredAmount;

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
        if (kingdomDictionary.ContainsKey(kingdomID))
            return kingdomDictionary[kingdomID];

        Debug.LogWarning($"Kingdom not found: {kingdomID}");
        return null;
    }

    public List<Kingdom> GetAllKingdoms()
    {
        return new List<Kingdom>(kingdoms);
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
    }

    public List<QuestSaveData> GetSaveData()
    {
        List<QuestSaveData> saveData = new List<QuestSaveData>();

        foreach (var quest in allQuests)
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
                    currentAmount = task.currentAmount
                });
            }

            saveData.Add(questData);
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
                    }
                }
            }
        }
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    public void AddNewQuest()
    {
        Quest newQuest = new Quest
        {
            questID = $"quest_{allQuests.Count + 1}",
            questName = "New Quest",
            description = "Quest description here"
        };

        allQuests.Add(newQuest);
        EditorUtility.SetDirty(this);
    }

    public void AddNewKingdom()
    {
        Kingdom newKingdom = new Kingdom
        {
            kingdomID = $"kingdom_{kingdoms.Count + 1}",
            kingdomName = "New Kingdom"
        };

        kingdoms.Add(newKingdom);
        EditorUtility.SetDirty(this);
    }

    public void SortQuestsByKingdom()
    {
        allQuests.Sort((a, b) =>
        {
            if (a.kingdom == null && b.kingdom == null) return 0;
            if (a.kingdom == null) return 1;
            if (b.kingdom == null) return -1;
            return a.kingdom.kingdomName.CompareTo(b.kingdom.kingdomName);
        });

        EditorUtility.SetDirty(this);
    }
#endif

    #endregion
}