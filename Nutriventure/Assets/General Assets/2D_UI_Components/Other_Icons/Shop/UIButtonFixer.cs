using UnityEngine;
using UnityEngine.UI;

public class UIButtonFixer : MonoBehaviour
{
    [SerializeField] private bool fixOnStart = true;

    void Start()
    {
        if (fixOnStart)
        {
            FixAllButtons();
        }
    }

    [ContextMenu("Fix All Buttons")]
    public void FixAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        Debug.Log($"Found {allButtons.Length} buttons to fix");

        foreach (Button button in allButtons)
        {
            try
            {
                // Fix navigation
                Navigation nav = button.navigation;
                nav.mode = Navigation.Mode.None;
                button.navigation = nav;

                // Fix colors if they're causing issues
                ColorBlock colors = button.colors;
                if (colors.highlightedColor.a == 0)
                {
                    colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                    button.colors = colors;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to fix button {button.name}: {e.Message}");
            }
        }

        Debug.Log("All UI buttons fixed");
    }
}