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
            currentChest.ClaimChest();
            currentChest = null;
        }
    }

    public void SetCurrentChest(Chest chest)
    {
        currentChest = chest;
        ClearRewardsGrid();
        claimButton.gameObject.SetActive(false);
        UpdateUI();

        if (rewardRevealCoroutine != null)
            StopCoroutine(rewardRevealCoroutine);

        rewardRevealCoroutine = StartCoroutine(RevealRewardsOneByOne());
    }

    void UpdateUI()
    {
        if (currentChest != null)
        {
            chestNameText.text = "Claim " + currentChest.ChestName;
        }
    }

    IEnumerator RevealRewardsOneByOne()
    {
        int rewardCount = 4;
        List<Vector3> targetPositions = new List<Vector3>();

        // First, create all reward items but keep them hidden
        for (int i = 0; i < rewardCount; i++)
        {
            GameObject rewardItem = Instantiate(rewardItemPrefab, rewardsGridParent);
            rewardItems.Add(rewardItem);

            RewardItem rewardComponent = rewardItem.GetComponent<RewardItem>();
            if (rewardComponent != null)
            {
                rewardComponent.SetReward("Reward " + (i + 1), GetRandomRewardType());
            }

            // Store target position for gold ball
            targetPositions.Add(rewardComponent.GetWorldPosition());

            // Initially hide the reward
            rewardItem.SetActive(false);
        }

        // Animate gold balls flying to each reward position
        for (int i = 0; i < rewardCount; i++)
        {
            if (goldBallPrefab != null && currentChest != null)
            {
                Vector3 startPos = GetChestSpawnPosition();
                Vector3 targetPos = targetPositions[i];

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

    private string GetRandomRewardType()
    {
        string[] rewardTypes = { "Coin", "Gem", "Key", "PowerUp" };
        return rewardTypes[Random.Range(0, rewardTypes.Length)];
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
    }
}