using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void ChangeToOCRScene()
    {
        // Save the current scene so ScanOCR can return here on exit
        PlayerPrefs.SetString("ScanOCR_PreviousScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        SceneManager.LoadScene("ScanOCR");
    }
}