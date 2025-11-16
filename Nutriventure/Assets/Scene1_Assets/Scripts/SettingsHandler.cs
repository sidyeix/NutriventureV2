using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
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
