using UnityEngine;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private bool disableAfterUse = false; // Checkbox in Inspector

    private Button button;
    private bool isButtonEnabled = true;

    void Start()
    {
        // Get button and add listener
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            // RESTORED: Warning, not error (matches original)
            Debug.LogWarning($"ContinueButton script on {gameObject.name} needs a Button component!");
        }
    }

    void OnButtonClick()
    {
        if (!isButtonEnabled) return;

        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.OnContinueButtonClicked();

            // Handle disable behavior based on checkbox
            if (disableAfterUse)
            {
                DisableButton();
            }
        }
        else
        {
            Debug.LogError("No TimelinePauseManager found in scene!");
        }
    }

    // Method to manually disable the button
    public void DisableButton()
    {
        if (button != null)
        {
            isButtonEnabled = false;
            button.interactable = false;
        }
        else
        {
            isButtonEnabled = false;
        }

        Debug.Log($"ContinueButton on {gameObject.name} has been disabled");
    }

    // Method to manually enable the button (for reuse)
    public void EnableButton()
    {
        if (button != null)
        {
            isButtonEnabled = true;
            button.interactable = true;
        }
        else
        {
            isButtonEnabled = true;
        }

        Debug.Log($"ContinueButton on {gameObject.name} has been enabled");
    }

    // Method to reset the button for a new game/restart
    public void ResetButton()
    {
        EnableButton();

        // Re-add listener
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        Debug.Log($"ContinueButton on {gameObject.name} has been reset");
    }

    // Called when the GameObject is enabled (for restart scenarios)
    void OnEnable()
    {
        // If the button was disabled, re-enable it based on disableAfterUse setting
        if (!disableAfterUse)
        {
            EnableButton();
        }
    }

    // Clean up listeners when destroyed
    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }

    // Public method to check if button is currently enabled
    public bool IsButtonEnabled()
    {
        return isButtonEnabled;
    }
}