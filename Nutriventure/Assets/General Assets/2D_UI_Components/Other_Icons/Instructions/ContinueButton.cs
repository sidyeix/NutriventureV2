using UnityEngine;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private bool disableAfterUse = false; // <-- CHECKBOX IN INSPECTOR
    [SerializeField] private bool logDebugMessages = true;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private Button button;
    private bool isButtonEnabled = true;

    void Start()
    {
        // Get button component
        button = GetComponent<Button>();

        if (button != null)
        {
            // Remove all existing listeners first to prevent duplicates
            button.onClick.RemoveAllListeners();

            // Add the listener
            button.onClick.AddListener(OnButtonClick);

            if (logDebugMessages)
                Debug.Log($"ContinueButton initialized on {gameObject.name}. Disable after use: {disableAfterUse}");
        }
        else
        {
            Debug.LogError($"ContinueButton script on {gameObject.name} needs a Button component!");
        }
    }

    void OnButtonClick()
    {
        // Prevent multiple clicks if button is disabled
        if (!isButtonEnabled)
        {
            if (logDebugMessages)
                Debug.Log($"ContinueButton on {gameObject.name} is disabled, ignoring click");
            return;
        }

        if (logDebugMessages)
            Debug.Log($"ContinueButton on {gameObject.name} clicked!");

        // Play click sound if assigned
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Resume the timeline
        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.OnContinueButtonClicked();

            if (logDebugMessages)
                Debug.Log("Timeline resumed via ContinueButton");
        }
        else
        {
            Debug.LogError("No TimelinePauseManager found in scene!");
            return;
        }

        // Handle disable behavior based on checkbox
        if (disableAfterUse)
        {
            DisableButton();
        }
    }

    // Method to manually disable the button
    public void DisableButton()
    {
        if (button != null)
        {
            isButtonEnabled = false;
            button.interactable = false;

            if (logDebugMessages)
                Debug.Log($"ContinueButton on {gameObject.name} has been disabled");
        }
    }

    // Method to manually enable the button (for reuse)
    public void EnableButton()
    {
        if (button != null)
        {
            isButtonEnabled = true;
            button.interactable = true;

            if (logDebugMessages)
                Debug.Log($"ContinueButton on {gameObject.name} has been enabled");
        }
    }

    // Method to reset the button for a new game/restart
    public void ResetButton()
    {
        EnableButton();

        // Re-add listener if it was removed
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClick);
        }

        if (logDebugMessages)
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

    // Public method to change disable behavior at runtime
    public void SetDisableAfterUse(bool shouldDisable)
    {
        disableAfterUse = shouldDisable;

        if (logDebugMessages)
            Debug.Log($"ContinueButton on {gameObject.name} disableAfterUse set to: {shouldDisable}");
    }

    // Public method to check if button is currently enabled
    public bool IsButtonEnabled()
    {
        return isButtonEnabled && (button != null && button.interactable);
    }
}