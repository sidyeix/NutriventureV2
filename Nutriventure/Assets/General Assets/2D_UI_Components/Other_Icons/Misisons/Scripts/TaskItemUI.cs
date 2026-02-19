using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskItemUI : MonoBehaviour
{
    [Header("Task Item References")]
    [SerializeField] private Toggle checkBox;
    [SerializeField] private TextMeshProUGUI taskDescriptionText;
    [SerializeField] private TextMeshProUGUI progressText;

    private void Awake()
    {
        // Auto-find references if not assigned in inspector
        if (checkBox == null)
            checkBox = GetComponentInChildren<Toggle>();

        if (taskDescriptionText == null)
        {
            Transform taskTextContainer = transform.Find("TaskTextContainer");
            if (taskTextContainer != null)
                taskDescriptionText = taskTextContainer.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (progressText == null)
        {
            Transform progressTextTransform = transform.Find("ProgressText");
            if (progressTextTransform != null)
                progressText = progressTextTransform.GetComponent<TextMeshProUGUI>();
        }
    }

    public void Setup(QuestTask task)
    {
        // Validate references
        if (checkBox == null)
        {
            Debug.LogError("CheckBox (Toggle) is not assigned in TaskItemUI!", gameObject);
            return;
        }

        if (taskDescriptionText == null)
        {
            Debug.LogError("TaskDescriptionText is not assigned in TaskItemUI!", gameObject);
            return;
        }

        // Set task description
        taskDescriptionText.text = task.description;

        // Set checkbox state
        checkBox.isOn = task.isCompleted;
        checkBox.interactable = false;

        // Handle progress text
        if (progressText != null)
        {
            if (task.requiredAmount > 0)
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"({task.currentAmount}/{task.requiredAmount})";
            }
            else
            {
                progressText.gameObject.SetActive(false);
            }
        }
    }

    // Helper method to manually set references in inspector
    [ContextMenu("Find References in Children")]
    private void FindReferencesInChildren()
    {
        if (checkBox == null)
            checkBox = GetComponentInChildren<Toggle>();

        if (taskDescriptionText == null)
        {
            Transform taskTextContainer = transform.Find("TaskTextContainer");
            if (taskTextContainer != null)
                taskDescriptionText = taskTextContainer.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (progressText == null)
        {
            Transform progressTextTransform = transform.Find("ProgressText");
            if (progressTextTransform != null)
                progressText = progressTextTransform.GetComponent<TextMeshProUGUI>();
        }

        Debug.Log("References refreshed!", gameObject);
    }
}