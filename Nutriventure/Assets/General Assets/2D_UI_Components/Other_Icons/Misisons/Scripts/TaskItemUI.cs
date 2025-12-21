using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskItemUI : MonoBehaviour
{
    public Toggle checkBox;
    public TextMeshProUGUI taskDescriptionText;
    public TextMeshProUGUI progressText;

    public void Setup(QuestTask task)
    {
        taskDescriptionText.text = task.description;
        checkBox.isOn = task.isCompleted;
        checkBox.interactable = false;

        if (task.requiredAmount <= 0)
        {
            progressText.gameObject.SetActive(false);
        }
        else
        {
            progressText.gameObject.SetActive(true);
            progressText.text = $"({task.currentAmount}/{task.requiredAmount})";
        }
    }
}
