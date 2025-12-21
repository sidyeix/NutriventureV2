using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Player")]
    [SerializeField] private int playerLevel = 1;

    [Header("Events")]
    public UnityEvent<Quest> onQuestStarted;
    public UnityEvent<Quest> onQuestProgressUpdated;
    public UnityEvent<Quest> onQuestCompleted;
    public UnityEvent<QuestTask> onTaskProgressUpdated;
    public UnityEvent<string> onKingdomQuestsUpdated; // Kingdom ID
    public UnityEvent<List<Kingdom>> onKingdomsUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (questDatabase != null)
        {
            questDatabase.InitializeDatabase();
            onKingdomsUpdated?.Invoke(questDatabase.GetAllKingdoms());
        }
        else
        {
            Debug.LogError("Quest Database is not assigned!");
        }
    }

    #region Quest Management

    public bool StartQuest(string questID)
    {
        Quest quest = questDatabase.GetQuest(questID);
        if (quest == null) return false;

        quest.StartQuest();
        onQuestStarted?.Invoke(quest);
        onQuestProgressUpdated?.Invoke(quest);

        Kingdom questKingdom = questDatabase.GetKingdomForQuest(questID);
        if (questKingdom != null)
        {
            onKingdomQuestsUpdated?.Invoke(questKingdom.kingdomID);
        }

        return true;
    }

    public bool UpdateTaskProgress(string questID, string taskID, int amount = 1)
    {
        bool success = questDatabase.UpdateTaskProgress(questID, taskID, amount);

        if (success)
        {
            Quest quest = questDatabase.GetQuest(questID);
            QuestTask task = quest?.GetTask(taskID);

            if (task != null)
            {
                onTaskProgressUpdated?.Invoke(task);
            }

            if (quest != null)
            {
                onQuestProgressUpdated?.Invoke(quest);

                if (quest.status == QuestStatus.Completed)
                {
                    onQuestCompleted?.Invoke(quest);

                    Kingdom questKingdom = questDatabase.GetKingdomForQuest(questID);
                    if (questKingdom != null)
                    {
                        onKingdomQuestsUpdated?.Invoke(questKingdom.kingdomID);
                        onKingdomsUpdated?.Invoke(questDatabase.GetAllKingdoms());
                    }
                }
            }
        }

        return success;
    }

    public bool CompleteTask(string questID, string taskID)
    {
        Quest quest = questDatabase.GetQuest(questID);
        if (quest == null) return false;

        QuestTask task = quest.GetTask(taskID);
        if (task == null) return false;

        return UpdateTaskProgress(questID, taskID, task.requiredAmount - task.currentAmount);
    }

    public bool AbandonQuest(string questID)
    {
        Quest quest = questDatabase.GetQuest(questID);
        if (quest == null || quest.status != QuestStatus.InProgress) return false;

        quest.AbandonQuest();
        onQuestProgressUpdated?.Invoke(quest);

        Kingdom questKingdom = questDatabase.GetKingdomForQuest(questID);
        if (questKingdom != null)
        {
            onKingdomQuestsUpdated?.Invoke(questKingdom.kingdomID);
            onKingdomsUpdated?.Invoke(questDatabase.GetAllKingdoms());
        }

        return true;
    }

    #endregion

    #region Getters

    public List<Quest> GetQuestsByKingdom(string kingdomID)
    {
        return questDatabase.GetQuestsByKingdom(kingdomID);
    }

    public List<Quest> GetAvailableQuests()
    {
        return questDatabase.GetAvailableQuests(playerLevel);
    }

    public List<Quest> GetActiveQuests()
    {
        return questDatabase.GetQuestsByStatus(QuestStatus.InProgress);
    }

    public List<Quest> GetCompletedQuests()
    {
        return questDatabase.GetQuestsByStatus(QuestStatus.Completed);
    }

    public Quest GetQuest(string questID)
    {
        return questDatabase.GetQuest(questID);
    }

    public Kingdom GetKingdom(string kingdomID)
    {
        return questDatabase.GetKingdom(kingdomID);
    }

    public List<Kingdom> GetAllKingdoms()
    {
        return questDatabase.GetAllKingdoms();
    }

    public Kingdom GetKingdomForQuest(string questID)
    {
        return questDatabase.GetKingdomForQuest(questID);
    }

    public List<Kingdom> GetKingdomsWithAvailableQuests()
    {
        var kingdomsWithQuests = new List<Kingdom>();

        foreach (var kingdom in GetAllKingdoms())
        {
            foreach (var quest in kingdom.quests)
            {
                if (quest.status == QuestStatus.NotStarted &&
                    quest.requiredLevel <= playerLevel &&
                    ArePrerequisitesMet(quest))
                {
                    kingdomsWithQuests.Add(kingdom);
                    break; // Found at least one available quest
                }
            }
        }

        return kingdomsWithQuests;
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

    public int GetPlayerLevel() => playerLevel;
    public void SetPlayerLevel(int level) => playerLevel = level;

    #endregion

    #region Timeline Integration

    public void PlayQuestTimeline(string questID)
    {
        Quest quest = GetQuest(questID);
        if (quest == null || quest.timelineAsset == null) return;

        // Find a PlayableDirector in the scene
        var director = FindObjectOfType<PlayableDirector>();
        if (director != null)
        {
            director.playableAsset = quest.timelineAsset;
            director.Play();
        }
        else
        {
            Debug.LogWarning("No PlayableDirector found in scene to play quest timeline");
        }
    }

    #endregion

    #region Save/Load

    public void SaveQuests(string saveKey = "quest_save_data")
    {
        var saveData = questDatabase.GetSaveData();
        string json = JsonUtility.ToJson(new Wrapper<List<QuestDatabase.QuestSaveData>>(saveData));
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        Debug.Log("Quests saved!");
    }

    public void LoadQuests(string saveKey = "quest_save_data")
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            var wrapper = JsonUtility.FromJson<Wrapper<List<QuestDatabase.QuestSaveData>>>(json);
            questDatabase.LoadSaveData(wrapper.items);
            onKingdomsUpdated?.Invoke(questDatabase.GetAllKingdoms());
            Debug.Log("Quests loaded!");
        }
        else
        {
            Debug.Log("No saved quest data found.");
        }
    }

    public void ResetAllQuests()
    {
        foreach (var kingdom in GetAllKingdoms())
        {
            foreach (var quest in kingdom.quests)
            {
                quest.status = QuestStatus.NotStarted;
                quest.startTime = DateTime.MinValue;
                quest.completionTime = null;
                quest.ResetAllTasks();
            }
        }
        onKingdomsUpdated?.Invoke(questDatabase.GetAllKingdoms());
        Debug.Log("All quests reset!");
    }

    // Wrapper class for JSON serialization of lists
    [System.Serializable]
    private class Wrapper<T>
    {
        public T items;
        public Wrapper(T items) { this.items = items; }
    }

    #endregion

}