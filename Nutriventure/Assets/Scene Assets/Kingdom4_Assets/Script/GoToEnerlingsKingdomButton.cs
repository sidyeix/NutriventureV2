using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GoToEnerlingsKingdomButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "EnerlingsKingdom";

    private Button attachedButton;

    private void Awake()
    {
        attachedButton = GetComponent<Button>();
        if (attachedButton != null)
        {
            attachedButton.onClick.AddListener(LoadTargetScene);
        }
    }

    private void OnDestroy()
    {
        if (attachedButton != null)
        {
            attachedButton.onClick.RemoveListener(LoadTargetScene);
        }
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("GoToEnerlingsKingdomButton: targetSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
