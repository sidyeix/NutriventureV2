using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ChestDatabase", menuName = "Chest/Chest Database")]
public class ChestDatabase : ScriptableObject
{
    [System.Serializable]
    public class ChestConfig
    {
        public string chestName;
        public GameObject chestPrefab;
        public float rewardDelay = 2f;
        public VideoClip videoClip;
        public AudioClip backgroundMusic;
        public Color chestColor = Color.white;
        public string chestDescription;

        // ADD THESE: New image fields
        public Sprite chestRarityImage;      // Image for chest rarity panel
        public Sprite rewardItemBackground;  // Background image for reward items

        // Rewards for this chest type
        public List<ChestReward> chestRewards = new List<ChestReward>();
    }

    [System.Serializable]
    public class ChestReward
    {
        public string rewardName;      // Example: "Gold Coins", "Diamonds", "Magic Key"
        public string rewardType;      // Example: "Coin", "Gem", "Key", "PowerUp"
        public int amount;             // Example: 100, 50, 1
        public Sprite rewardIcon;      // Icon for this reward (from database)
    }

    public List<ChestConfig> chestConfigs = new List<ChestConfig>();

    // Helper method to get chest config by index
    public ChestConfig GetChestConfig(int index)
    {
        if (index >= 0 && index < chestConfigs.Count)
        {
            return chestConfigs[index];
        }
        return null;
    }

    // Helper method to get chest config by name
    public ChestConfig GetChestConfigByName(string name)
    {
        return chestConfigs.Find(c => c.chestName == name);
    }

    // Helper method to get total chest count
    public int GetChestCount()
    {
        return chestConfigs.Count;
    }
}