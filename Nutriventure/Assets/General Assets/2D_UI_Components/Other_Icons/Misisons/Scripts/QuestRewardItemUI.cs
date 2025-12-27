using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestRewardItemUI : MonoBehaviour
{
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TextMeshProUGUI rewardText;

    public void SetupReward(QuestReward reward)
    {
        if (rewardIcon != null)
        {
            rewardIcon.sprite = reward.rewardIcon;
            rewardIcon.color = reward.rewardIcon != null ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        if (rewardText != null)
        {
            rewardText.text = $"+{reward.amount}";
        }
    }
} 