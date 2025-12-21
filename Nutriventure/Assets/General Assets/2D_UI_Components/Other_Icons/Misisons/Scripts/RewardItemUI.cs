using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    public Image rewardIcon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI rewardNameText;

    public void Setup(QuestReward reward)
    {
        rewardIcon.sprite = reward.rewardIcon;
        amountText.text = $"+{reward.amount}";
        rewardNameText.text = reward.rewardName;
    }
}
