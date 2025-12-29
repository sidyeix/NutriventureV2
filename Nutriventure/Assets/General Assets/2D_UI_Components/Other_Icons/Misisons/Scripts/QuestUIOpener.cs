using UnityEngine;
using UnityEngine.UI;

public class QuestUIOpener : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject questCanvas; // Drag your QuestCanvas here
    [SerializeField] private Button openButton; // Optional: if you want to assign a button
    [SerializeField] private Button closeButton; // Optional: if you want to assign a button

    [Header("Settings")]
    [SerializeField] private bool refreshOnOpen = true;

    private void Start()
    {
        // Set up button listeners if buttons are assigned
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenQuestUI);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseQuestUI);
        }

        // Close UI on start (optional)
        CloseQuestUI();
    }

    public void OpenQuestUI()
    {
        if (questCanvas != null)
        {
            questCanvas.SetActive(true);

            // Refresh quest list if needed
            if (refreshOnOpen)
            {
                var questBoard = questCanvas.GetComponentInChildren<QuestBoardUIController>();
                if (questBoard != null)
                {
                    questBoard.RefreshUI();
                }
            }
        }
    }

    public void CloseQuestUI()
    {
        if (questCanvas != null)
        {
            questCanvas.SetActive(false);
        }
    }

    // Simple toggle method
    public void ToggleQuestUI()
    {
        if (questCanvas != null)
        {
            bool isActive = questCanvas.activeSelf;
            questCanvas.SetActive(!isActive);

            if (!isActive && refreshOnOpen) // If we're opening it
            {
                var questBoard = questCanvas.GetComponentInChildren<QuestBoardUIController>();
                if (questBoard != null)
                {
                    questBoard.RefreshUI();
                }
            }
        }
    }
}