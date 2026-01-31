using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void ChangeToOCRScene()
    {
        // Replace "YourOCRSceneName" with the actual name of your scene
        SceneManager.LoadScene("ScanOCR");
    }
}