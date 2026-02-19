using UnityEngine;
using System.Collections.Generic;

// Achievement Status Enum
public enum AchievementStatus
{
    NotComplete,  // Not yet completed
    Completed,    // Completed but not claimed
    Claimed       // Completed and claimed
}

[CreateAssetMenu(fileName = "AchievementDatabase", menuName = "Game/Achievement Database")]
public class AchievementDatabase : ScriptableObject
{
    public List<AchievementData> achievements = new List<AchievementData>();

    [System.Serializable]
    public class AchievementData
    {
        public string id;
        public string achievementName;
        public Sprite achievementIcon;
        public int prizeGems;
        [TextArea(2, 3)]
        public string description;
    }

    public AchievementData GetAchievement(string id)
    {
        return achievements.Find(a => a.id == id);
    }

    public Sprite GetAchievementIcon(string id)
    {
        var achievement = GetAchievement(id);
        return achievement != null ? achievement.achievementIcon : null;
    }

    public int GetPrizeGems(string id)
    {
        var achievement = GetAchievement(id);
        return achievement != null ? achievement.prizeGems : 0;
    }
}