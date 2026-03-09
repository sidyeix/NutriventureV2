using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour, IPointerDownHandler
{
    [Header("Button Settings")]
    [SerializeField] private bool disableAfterUse = false; // Checkbox in Inspector

    [Header("Audio Settings")]
    [SerializeField] private bool playClickSound = true; // Option to enable/disable click sound

    private Button button;
    private bool isButtonEnabled = true;

    void Start()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogWarning($"ContinueButton script on {gameObject.name} needs a Button component!");
        }
    }

    // Fires IMMEDIATELY on touch/press — no waiting for release
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isButtonEnabled) return;
        if (button != null && !button.interactable) return;

        // Play click sound effect
        if (playClickSound && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

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

    void OnDestroy() { }

    // Public method to check if button is currently enabled
    public bool IsButtonEnabled()
    {
        return isButtonEnabled;
    }
}