using UnityEngine;
using UnityEngine.UI;

public class ContinueButton2 : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private bool disableAfterUse = false;

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnContinueClicked);
        }
    }

    void OnContinueClicked()
    {
        // Use your existing TimelinePauseManager
        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.OnContinueButtonClicked();

            if (disableAfterUse)
            {
                button.interactable = false;
            }
        }
    }

    public void EnableButton()
    {
        if (button != null)
        {
            button.interactable = true;
        }
    }

    public void DisableButton()
    {
        if (button != null)
        {
            button.interactable = false;
        }
    }
}