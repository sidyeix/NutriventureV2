using UnityEngine;

public class SettingsHandler : MonoBehaviour
{
    public GameObject SettingsPanel;
    void Start()
    {
        SettingsPanel.SetActive(false);   
    }
    public void ToggleSettings()
    {
        SettingsPanel.SetActive(!SettingsPanel.activeSelf);
    }
}
