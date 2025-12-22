using UnityEngine;
using TMPro;

public class QuestUIController : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI questNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statusText;

    [Header("Tasks")]
    public Transform taskContent;
    public GameObject taskItemPrefab;

    [Header("Rewards")]
    public Transform rewardContent;
    public GameObject rewardItemPrefab;

    private Quest displayedQuest;

    public void DisplayQuest(Quest quest)
    {
        displayedQuest = quest;

        questNameText.text = quest.questName;
        descriptionText.text = quest.description;
        statusText.text = quest.status.ToString();

        DisplayTasks();
        DisplayRewards();
    }

    private void DisplayTasks()
    {
        foreach (Transform child in taskContent)
            Destroy(child.gameObject);

        foreach (var task in displayedQuest.tasks)
        {
            GameObject go = Instantiate(taskItemPrefab, taskContent);
            TaskItemUI ui = go.GetComponent<TaskItemUI>();
            ui.Setup(task);
        }
    }

    private void DisplayRewards()
    {
        foreach (Transform child in rewardContent)
            Destroy(child.gameObject);

        foreach (var reward in displayedQuest.rewards)
        {
            GameObject go = Instantiate(rewardItemPrefab, rewardContent);
            RewardItemUI ui = go.GetComponent<RewardItemUI>();
            ui.Setup(reward);
        }
    }
}
