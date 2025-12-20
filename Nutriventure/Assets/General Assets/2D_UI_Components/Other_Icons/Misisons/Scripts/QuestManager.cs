using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private QuestDatabase questDatabase;

    [Header("Events")]
    public UnityEvent<Quest> onQuestStarted;
    public UnityEvent<Quest> onQuestProgressUpdated;
    public UnityEvent<Quest> onQuestCompleted;
    public UnityEvent<QuestTask> onTaskProgressUpdated;
    public UnityEvent<string> onKingdomQuestsUpdated; // Kingdom ID

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
                }

                // Notify kingdom update
                if (quest.kingdom != null)
                {
                    onKingdomQuestsUpdated?.Invoke(quest.kingdom.kingdomID);
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

        return true;
    }

    #endregion

    #region Getters

    public List<Quest> GetQuestsByKingdom(string kingdomID)
    {
        return questDatabase.GetQuestsByKingdom(kingdomID);
    }

    public List<Quest> GetAvailableQuests(int playerLevel)
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
    }

    public void LoadQuests(string saveKey = "quest_save_data")
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            string json = PlayerPrefs.GetString(saveKey);
            var wrapper = JsonUtility.FromJson<Wrapper<List<QuestDatabase.QuestSaveData>>>(json);
            questDatabase.LoadSaveData(wrapper.items);
        }
    }

    // Wrapper class for JSON serialization of lists
    [System.Serializable]
    private class Wrapper<T>
    {
        public T items;
        public Wrapper(T items) { this.items = items; }
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug: Print All Quests")]
    public void DebugPrintAllQuests()
    {
        foreach (var kingdom in GetAllKingdoms())
        {
            Debug.Log($"=== {kingdom.kingdomName} ===");
            var kingdomQuests = GetQuestsByKingdom(kingdom.kingdomID);

            foreach (var quest in kingdomQuests)
            {
                Debug.Log($"[{quest.status}] {quest.questName}: {quest.CompletedTaskCount}/{quest.TotalTaskCount} tasks");
            }
        }
    }

    #endregion
}