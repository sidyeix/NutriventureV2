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
    public string rewardID; // For Enerlings, Character, Frame, Icon

    public enum RewardType
    {
        Item,
        Currency,
        Experience,
        Unlockable,
        Custom,
        // NEW: Specific reward types for your game
        NutriCoins,
        Exp,
        NutriGems,
        Enerlings,
        Character,
        Frame,
        Icon
    }

    public void GrantReward()
    {
        // The actual granting logic is handled in NPCQuestInteraction
        Debug.Log($"Granted {amount} {rewardName}");
    }
}

[System.Serializable]
public class QuestTask
{
    public string taskID;
    public string description;
    public TaskType type = TaskType.Collect;
    public string targetID;
    public int requiredAmount = 1;
    public int currentAmount = 0;
    public bool isOptional = false;
    public bool isCompleted = false;

    public enum TaskType
    {
        Collect,
        Kill,
        Talk,
        Reach,
        Interact,
        Custom
    }

    public bool IsComplete => isCompleted;

    public void UpdateProgress(int amount = 1)
    {
        currentAmount = Mathf.Min(currentAmount + amount, requiredAmount);

        if (currentAmount >= requiredAmount && !isCompleted)
        {
            isCompleted = true;
        }
    }

    public void ResetProgress()
    {
        currentAmount = 0;
        isCompleted = false;
    }

    public void MarkAsComplete()
    {
        currentAmount = requiredAmount;
        isCompleted = true;
    }

    public void MarkAsIncomplete()
    {
        currentAmount = Mathf.Min(currentAmount, requiredAmount - 1);
        isCompleted = false;
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

    public QuestCategory category;
    public Sprite questIcon;

    [Header("Requirements")]
    public List<string> requiredQuestIDs = new List<string>();
    public int requiredLevel = 1;

    [Header("Tasks")]
    public List<QuestTask> tasks = new List<QuestTask>();

    [Header("Rewards")]
    public List<QuestReward> rewards = new List<QuestReward>();

    [Header("Timeline")]
    public PlayableAsset timelineAsset;

    [Header("Status")]
    public QuestStatus status = QuestStatus.NotStarted;
    public DateTime startTime;
    public DateTime? completionTime;

    [Header("UI")]
    public Color questColor = Color.white;

    // Progress tracking
    public int CompletedTaskCount
    {
        get
        {
            int count = 0;
            foreach (var task in tasks)
            {
                if (task.isCompleted) count++;
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
        }
    }

    public bool ClaimQuest()
    {
        if (status == QuestStatus.Completed)
        {
            status = QuestStatus.Claimed;
            return true;
        }
        return false;
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

    public void MarkAllTasksComplete()
    {
        foreach (var task in tasks)
        {
            task.MarkAsComplete();
        }

        if (AllTasksComplete)
        {
            CompleteQuest();
        }
    }
}

[System.Serializable]
public class Kingdom
{
    public string kingdomID;
    public string kingdomName;
    public Sprite kingdomIcon;
    public Color kingdomColor = Color.white;

    public List<Quest> quests = new List<Quest>();

    public int TotalQuestCount => quests.Count;
    public int CompletedQuestCount
    {
        get
        {
            int count = 0;
            foreach (var quest in quests)
            {
                if (quest.status == QuestStatus.Completed) count++;
            }
            return count;
        }
    }

    public int ClaimedQuestCount
    {
        get
        {
            int count = 0;
            foreach (var quest in quests)
            {
                if (quest.status == QuestStatus.Claimed) count++;
            }
            return count;
        }
    }
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Claimed,
    Failed,
    Abandoned
}

public enum QuestCategory
{
    MainStory,
    GeneralQuest,
    SideQuest,
    Daily,
    Weekly,
    Event,
    Tutorial
}