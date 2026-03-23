using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    private const string PreviousSceneKey = "ScanOCR_PreviousScene";

    [Header("Scene Names")]
    [SerializeField] private string scanSceneName = "ScanOCR";
    [SerializeField] private string nutriKingdomSceneName = "3_Kingdom1";
    [SerializeField] private string sugariaSceneName = "4_Kingdom 2";
    [SerializeField] private string preserviaSceneName = "5_Kingdom3";
    [SerializeField] private string allerthiaSceneName = "6_Kingdom4_R";

    public void ChangeToOCRScene()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        string sceneToPersist = ResolvePreviousKingdomScene(activeSceneName);

        // Save the kingdom scene so ScanOCR and BattlePlay can route back correctly.
        PlayerPrefs.SetString(PreviousSceneKey, sceneToPersist);
        PlayerPrefs.Save();

        if (!string.IsNullOrWhiteSpace(scanSceneName))
        {
            SceneManager.LoadScene(scanSceneName);
        }
        else
        {
            Debug.LogError("SceneChanger: Scan scene name is empty.");
        }
    }

    private string ResolvePreviousKingdomScene(string activeSceneName)
    {
        if (activeSceneName == nutriKingdomSceneName)
            return nutriKingdomSceneName;

        if (activeSceneName == sugariaSceneName)
            return sugariaSceneName;

        if (activeSceneName == preserviaSceneName)
            return preserviaSceneName;

        if (activeSceneName == allerthiaSceneName)
            return allerthiaSceneName;

        Debug.LogWarning($"SceneChanger: Active scene '{activeSceneName}' is not a configured kingdom scene. Saving current scene name as fallback.");
        return activeSceneName;
    }
}