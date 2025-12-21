// ContinueButton.cs
using UnityEngine;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour
{
    void Start()
    {
        // Get button and add listener
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogWarning("ContinueButton script needs a Button component!");
        }
    }

    void OnButtonClick()
    {
        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.OnContinueButtonClicked();
        }
        else
        {
            Debug.LogError("No TimelinePauseManager found in scene!");
        }
    }
}