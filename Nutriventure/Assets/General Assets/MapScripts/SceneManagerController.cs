using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerController : MonoBehaviour
{
    // Load scene by name with simple error handling
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty!");
            return;
        }

        try
        {
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{sceneName}': {e.Message}");
        }
    }

    // Convenience methods for each kingdom - UPDATED BASED ON YOUR SCENE LIST
    public void LoadNutriKingdom() => LoadScene("Scenes/3_Kingdom1");
    public void LoadSugaria() => LoadScene("Scenes/4_Kingdom 2");
    public void LoadAlerthia() => LoadScene("Scenes/6_Kingdom4_R");
    public void LoadPreservia() => LoadScene("Scenes/5_Kingdom3");
    // Optional: You can also load by build index
    public void LoadSceneByIndex(int index)
    {
        if (index >= 0 && index < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(index);
        }
        else
        {
            Debug.LogError($"Invalid scene index: {index}");
        }
    }
}