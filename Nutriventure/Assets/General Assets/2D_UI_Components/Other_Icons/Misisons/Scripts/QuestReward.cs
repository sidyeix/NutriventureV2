using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class QuestReward
{
    public string rewardName;
    public Sprite rewardIcon;
    public int amount = 1;
    public RewardType type = RewardType.Item;

    public enum RewardType
    {
        Item,
        Currency,
        Experience,
        Unlockable,
        Custom
    }

    public void GrantReward()
    {
        // Implement reward granting logic
        Debug.Log($"Granted {amount} {rewardName}");
    }
}

[System.Serializable]
public class QuestTask
{
    public string taskID;
    public string description;
    public TaskType type = TaskType.Collect;
    public string targetID; // Could be item ID, enemy ID, location ID, etc.
    public int requiredAmount = 1;
    public int currentAmount = 0;
    public bool isOptional = false;

    public enum TaskType
    {
        Collect,
        Kill,
        Talk,
        Reach,
        Interact,
        Custom
    }

    public bool IsComplete => currentAmount >= requiredAmount;

    public void UpdateProgress(int amount = 1)
    {
        currentAmount = Mathf.Min(currentAmount + amount, requiredAmount);
    }

    public void ResetProgress()
    {
        currentAmount = 0;
    }

    public float ProgressPercentage => (float)currentAmount / requiredAmount * 100f;
}

[System.Serializable]
public class Quest
{
    public string questID;
    public string questName;
    public string description;

    [TextArea(3, 5)]
    public string longDescription;

    public Kingdom kingdom;
    public QuestCategory category;

    [Header("Requirements")]
    public List<string> requiredQuestIDs = new List<string>(); // Quests that must be completed first
    public int requiredLevel = 1;

    [Header("Tasks")]
    public List<QuestTask> tasks = new List<QuestTask>();

    [Header("Rewards")]
    public List<QuestReward> rewards = new List<QuestReward>();

    [Header("Timeline")]
    public PlayableAsset timelineAsset; // Optional timeline for quest

    [Header("Status")]
    public QuestStatus status = QuestStatus.NotStarted;
    public DateTime startTime;
    public DateTime? completionTime;

    [Header("UI")]
    public Sprite questIcon;
    public Color questColor = Color.white;

    // Progress tracking
    public int CompletedTaskCount
    {
        get
        {
            int count = 0;
            foreach (var task in tasks)
            {
                if (task.IsComplete) count++;
            }
            return count;
        }
    }

    public int TotalTaskCount => tasks.Count;
    public bool AllTasksComplete => CompletedTaskCount >= TotalTaskCount;
    public float OverallProgress => (float)CompletedTaskCount / TotalTaskCount * 100f;

    public void StartQuest()
    {
        if (status == QuestStatus.NotStarted)
        {
            status = QuestStatus.InProgress;
            startTime = DateTime.Now;
        }
    }

    public void CompleteQuest()
    {
        if (status == QuestStatus.InProgress && AllTasksComplete)
        {
            status = QuestStatus.Completed;
            completionTime = DateTime.Now;

            // Grant rewards
            foreach (var reward in rewards)
            {
                reward.GrantReward();
            }
        }
    }

    public void AbandonQuest()
    {
        if (status == QuestStatus.InProgress)
        {
            status = QuestStatus.Abandoned;
            ResetAllTasks();
        }
    }

    public void ResetAllTasks()
    {
        foreach (var task in tasks)
        {
            task.ResetProgress();
        }
    }

    public QuestTask GetTask(string taskID)
    {
        return tasks.Find(t => t.taskID == taskID);
    }
}

[System.Serializable]
public class Kingdom
{
    public string kingdomID;
    public string kingdomName;
    public Sprite kingdomIcon;
    public Color kingdomColor = Color.white;
    public List<string> questIDs = new List<string>();
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Abandoned
}

public enum QuestCategory
{
    MainStory,
    SideQuest,
    Daily,
    Weekly,
    Event,
    Tutorial
}