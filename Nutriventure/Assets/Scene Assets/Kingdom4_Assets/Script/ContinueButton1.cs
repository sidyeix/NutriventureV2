// ContinueButton.cs
using UnityEngine;
using UnityEngine.UI;

public class ContinueButton1 : MonoBehaviour
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
        if (TimelinePauseManager1.Instance != null)
        {
            TimelinePauseManager1.Instance.OnContinueButtonClicked();
        }
        else
        {
            Debug.LogError("No TimelinePauseManager found in scene!");
        }
    }
}