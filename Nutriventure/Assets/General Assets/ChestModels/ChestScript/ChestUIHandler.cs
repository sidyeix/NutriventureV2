using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ChestUIHandler : MonoBehaviour
{
    [Header("UI Elements")]
    public Button claimButton;
    public TextMeshProUGUI chestNameText;
    public Transform rewardsGridParent;
    public GameObject rewardItemPrefab;

    // ADD THIS: Reference to chest rarity image panel
    public Image chestRarityImagePanel;

    [Header("Animation Settings")]
    public float rewardSpawnDelay = 1f;
    public GameObject goldBallPrefab;

    private Chest currentChest;
    private List<GameObject> rewardItems = new List<GameObject>();
    private Coroutine rewardRevealCoroutine;

    void Start()
    {
        claimButton.onClick.AddListener(OnClaimButtonClicked);
        gameObject.SetActive(false);
        claimButton.gameObject.SetActive(false);
    }

    void OnClaimButtonClicked()
    {
        if (currentChest != null)
        {
            // Play claim sound using AudioHandler
            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayClaimSound();
            }

            currentChest.ClaimChest();
            currentChest = null;
        }
    }

    public void SetCurrentChest(Chest chest)
    {
        // Store chest reference
        currentChest = chest;
        ClearRewardsGrid();
        claimButton.gameObject.SetActive(false);

        // Play chest open sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayChestOpen();
        }

        // Clear any existing coroutine
        if (rewardRevealCoroutine != null)
        {
            StopCoroutine(rewardRevealCoroutine);
            rewardRevealCoroutine = null;
        }

        // Show opening message
        if (currentChest != null)
        {
            chestNameText.text = "Opening " + currentChest.ChestName + "...";
        }

        // UPDATE CHEST RARITY IMAGE
        UpdateChestRarityImage();
    }

    // New method to update chest rarity image
    private void UpdateChestRarityImage()
    {
        if (chestRarityImagePanel == null || currentChest == null || ChestManager.Instance == null)
            return;

        var chestConfig = ChestManager.Instance.GetChestConfig(currentChest.chestOrder);
        if (chestConfig != null && chestConfig.chestRarityImage != null)
        {
            chestRarityImagePanel.sprite = chestConfig.chestRarityImage;
            chestRarityImagePanel.gameObject.SetActive(true);
            Debug.Log($"Set chest rarity image: {chestConfig.chestRarityImage.name}");
        }
        else
        {
            chestRarityImagePanel.gameObject.SetActive(false);
            Debug.LogWarning("No chest rarity image found in database");
        }
    }

    public void StartRevealingRewards(Chest chest)
    {
        // Update chest reference
        currentChest = chest;

        if (rewardRevealCoroutine != null)
            StopCoroutine(rewardRevealCoroutine);

        rewardRevealCoroutine = StartCoroutine(RevealRewardsOneByOne());
    }

    IEnumerator RevealRewardsOneByOne()
    {
        // Update UI to show we're revealing rewards
        if (currentChest != null)
        {
            chestNameText.text = "Claim " + currentChest.ChestName;
        }

        // Get rewards from database
        List<ChestDatabase.ChestReward> chestRewards = GetRewardsFromDatabase();

        // If no rewards in database, use fallback
        if (chestRewards.Count == 0)
        {
            Debug.LogWarning("No rewards found in database for chest. Using fallback rewards.");
            chestRewards = GetFallbackRewards();
        }

        List<Vector3> targetPositions = new List<Vector3>();

        // Get reward item background from database
        Sprite rewardItemBackground = GetRewardItemBackground();

        // First, create all reward items but keep them hidden
        for (int i = 0; i < chestRewards.Count; i++)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsGridParent);
            rewardItems.Add(rewardItem);

            RewardItem rewardComponent = rewardItem.GetComponent<RewardItem>();
            if (rewardComponent != null)
            {
                var reward = chestRewards[i];
                rewardComponent.SetReward(
                    reward.rewardName,      // Name from database
                    reward.rewardType,      // Type from database
                    reward.amount,          // Amount from database
                    reward.rewardIcon,      // Icon from database
                    rewardItemBackground    // Background from database
                );
            }

            // Store target position for gold ball
            targetPositions.Add(rewardComponent.GetWorldPosition());

            // Initially hide the reward
            rewardItem.SetActive(false);
        }

        // Animate gold balls flying to each reward position
        for (int i = 0; i < chestRewards.Count; i++)
        {
            if (goldBallPrefab != null && currentChest != null)
            {
                Vector3 startPos = GetChestSpawnPosition();
                Vector3 targetPos = targetPositions[i];

                // Play gold ball fly sound using AudioHandler
                if (AudioHandler.Instance != null)
                {
                    AudioHandler.Instance.PlayGoldBallFly();
                }

                GameObject goldBall = Instantiate(goldBallPrefab);
                GoldBallController ballController = goldBall.GetComponent<GoldBallController>();

                if (ballController != null)
                {
                    ballController.Initialize(startPos, targetPos);
                }
            }

            // Wait for gold ball to reach target, then show reward
            yield return new WaitForSeconds(rewardSpawnDelay - 0.3f);

            // Show the reward with pop effect
            if (i < rewardItems.Count)
            {
                rewardItems[i].SetActive(true);
                RewardItem rewardComponent = rewardItems[i].GetComponent<RewardItem>();
                if (rewardComponent != null)
                {
                    rewardComponent.ShowReward();
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        // All rewards revealed, show claim button
        claimButton.gameObject.SetActive(true);
    }

    // Get rewards from database based on current chest
    private List<ChestDatabase.ChestReward> GetRewardsFromDatabase()
    {
        List<ChestDatabase.ChestReward> rewards = new List<ChestDatabase.ChestReward>();

        if (currentChest != null && ChestManager.Instance != null)
        {
            var chestConfig = ChestManager.Instance.GetChestConfig(currentChest.chestOrder);
            if (chestConfig != null && chestConfig.chestRewards != null)
            {
                rewards = chestConfig.chestRewards;
            }
        }

        return rewards;
    }

    // Get reward item background from database
    private Sprite GetRewardItemBackground()
    {
        if (currentChest != null && ChestManager.Instance != null)
        {
            var chestConfig = ChestManager.Instance.GetChestConfig(currentChest.chestOrder);
            if (chestConfig != null && chestConfig.rewardItemBackground != null)
            {
                return chestConfig.rewardItemBackground;
            }
        }
        return null; // Return null if no background found
    }

    // Fallback rewards if database doesn't have rewards
    private List<ChestDatabase.ChestReward> GetFallbackRewards()
    {
        // Create fallback rewards (you can customize these)
        List<ChestDatabase.ChestReward> fallbackRewards = new List<ChestDatabase.ChestReward>();

        string[] rewardTypes = { "Coin", "Gem", "Key", "PowerUp" };
        string[] rewardNames = { "Gold Coins", "Magic Gems", "Ancient Key", "Power Crystal" };
        int[] amounts = { 100, 50, 1, 1 };

        for (int i = 0; i < 4; i++)
        {
            ChestDatabase.ChestReward reward = new ChestDatabase.ChestReward
            {
                rewardName = rewardNames[i],
                rewardType = rewardTypes[i],
                amount = amounts[i],
                rewardIcon = null // No icon in fallback
            };
            fallbackRewards.Add(reward);
        }

        return fallbackRewards;
    }

    private Vector3 GetChestSpawnPosition()
    {
        if (currentChest == null) return Vector3.zero;

        // Look for specific spawn point child in the chest
        Transform spawnPoint = currentChest.transform.Find("GoldSpawnPoint");
        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        // Fallback: use chest position with offset
        Debug.LogWarning("GoldSpawnPoint not found in chest. Using chest position with offset.");
        return currentChest.transform.position + new Vector3(0, 1f, 0);
    }

    private void ClearRewardsGrid()
    {
        foreach (GameObject reward in rewardItems)
        {
            if (reward != null)
            {
                Destroy(reward);
            }
        }
        rewardItems.Clear();
    }

    public void OnChestUIClosed()
    {
        if (rewardRevealCoroutine != null)
        {
            StopCoroutine(rewardRevealCoroutine);
            rewardRevealCoroutine = null;
        }
        ClearRewardsGrid();
        claimButton.gameObject.SetActive(false);

        // Reset chest rarity image
        if (chestRarityImagePanel != null)
        {
            chestRarityImagePanel.gameObject.SetActive(false);
        }

        currentChest = null;
    }
}