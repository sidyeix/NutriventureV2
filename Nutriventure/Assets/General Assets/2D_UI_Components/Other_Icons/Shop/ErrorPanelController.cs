using UnityEngine;
using UnityEngine.UI;

public class ErrorPanelController : MonoBehaviour
{
    [Header("Drag Objects Here")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private Button closeButton;

    void Start()
    {
        // Make sure we have a button
        if (closeButton != null)
        {
            // Connect the button click to close the panel
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            Debug.LogWarning("No close button assigned to ErrorPanelController!");
        }
    }

    void ClosePanel()
    {
        // Hide the panel when button is clicked
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No error panel assigned to ErrorPanelController!");
        }
    }

    void OnDestroy()
    {
        // Clean up to prevent memory leaks
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}