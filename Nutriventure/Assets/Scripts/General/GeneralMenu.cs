using UnityEngine;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Character System")]
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;

    [Header("Player Settings")]
    public bool enablePlayerControlInMenu = false;
    public bool stopLookAroundAnimation = true;

    [Header("Resume Game Canvas")]
    [SerializeField] private ResumeGameCanvas resumeGameCanvas;
    [SerializeField] private string kingdomSceneName = "3_Kingdom1";

    void Start()
    {
        StartCoroutine(InitializeMainMenu());
    }

    IEnumerator InitializeMainMenu()
    {
        Debug.Log("=== MAIN MENU STARTING ===");

        yield return new WaitForSeconds(0.1f);
        LoadSavedCharacterNoAnimation();
        yield return new WaitForSeconds(0.2f);

        if (stopLookAroundAnimation && characterVisualSwapper != null)
        {
            characterVisualSwapper.StopLookAroundAnimation();
        }

        if (enablePlayerControlInMenu)
        {
            EnableStarterAssetsControl();
        }
        else
        {
            DisableStarterAssetsControl();
        }

        Debug.Log("=== MAIN MENU READY ===");
    }

    void LoadSavedCharacterNoAnimation()
    {
        if (characterVisualSwapper == null || characterDatabase == null)
        {
            Debug.LogError("Character system not set up!");
            return;
        }

        int savedCharacterID = 0;

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            savedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
        }
        else
        {
            Debug.LogWarning("No save data found, using default character (ID: 0)");
        }

        if (characterVisualSwapper != null)
        {
            var methodInfo = characterVisualSwapper.GetType().GetMethod("LoadCharacterWithSavedSkinNoAnimation");
            if (methodInfo != null)
            {
                methodInfo.Invoke(characterVisualSwapper, new object[] { savedCharacterID });
                Debug.Log($"Character loaded with ID {savedCharacterID}");
            }
            else
            {
                characterVisualSwapper.LoadCharacterWithSavedSkin(savedCharacterID);
                characterVisualSwapper.StopLookAroundAnimation();
            }
        }
    }

    // Call this when "Start Journey" button is clicked
    public void OnStartJourneyClicked()
    {
        Debug.Log("Start Journey clicked - checking for resume data");

        if (resumeGameCanvas != null)
        {
            // Show the resume canvas which will check for save data
            resumeGameCanvas.OnStartJourneyClicked();
        }
        else
        {
            // No resume canvas, just load the scene directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
        }
    }

    // Call this when "New Game" button is clicked (if you have one)
    public void OnNewGameClicked()
    {
        Debug.Log("New Game clicked - clearing save data");

        // Clear any saved state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ClearSavedGameState(kingdomSceneName);
        }

        // Load the scene fresh
        UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
    }

    void EnableStarterAssetsControl()
    {
        GameObject player = FindPlayer();
        if (player == null) return;

        MonoBehaviour[] allComponents = player.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var component in allComponents)
        {
            if (component == null) continue;

            string typeName = component.GetType().Name;

            if (typeName.Contains("ThirdPersonController") ||
                typeName.Contains("StarterAssetsInputs") ||
                typeName == "CharacterController")
            {
                component.enabled = true;
            }
        }
    }

    void DisableStarterAssetsControl()
    {
        GameObject player = FindPlayer();
        if (player == null) return;

        MonoBehaviour[] allComponents = player.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var component in allComponents)
        {
            if (component == null) continue;

            string typeName = component.GetType().Name;

            if (typeName.Contains("ThirdPersonController") ||
                typeName.Contains("StarterAssetsInputs") ||
                typeName.Contains("Movement") ||
                typeName.Contains("Controller") && !typeName.Contains("CharacterSelection"))
            {
                component.enabled = false;
            }
        }
    }

    GameObject FindPlayer()
    {
        GameObject player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        return player;
    }

    public void RefreshCharacter()
    {
        LoadSavedCharacterNoAnimation();
    }

    public void OnEnterCharacterSelection()
    {
        Debug.Log("Entering character selection mode");
    }

    public void OnExitCharacterSelection()
    {
        Debug.Log("Exiting character selection mode");
        LoadSavedCharacterNoAnimation();
    }
}