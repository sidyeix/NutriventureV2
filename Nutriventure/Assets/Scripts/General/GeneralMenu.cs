using UnityEngine;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("Character System")]
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;

    [Header("Player Settings")]
    public bool enablePlayerControlInMenu = false; // Usually false in menu
    public bool stopLookAroundAnimation = true;

    void Start()
    {
        StartCoroutine(InitializeMainMenu());
    }

    IEnumerator InitializeMainMenu()
    {
        Debug.Log("=== MAIN MENU STARTING ===");

        // 1. Load the saved character appearance WITHOUT animation
        LoadSavedCharacterNoAnimation();

        // 2. Wait for initialization
        yield return new WaitForSeconds(0.2f);

        // 3. Make sure LookAround animation is OFF
        if (stopLookAroundAnimation && characterVisualSwapper != null)
        {
            characterVisualSwapper.StopLookAroundAnimation();
            Debug.Log("Ensuring LookAround animation is stopped in main menu");
        }

        // 4. Control player movement in menu
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

        // Get saved character ID
        int savedCharacterID = 0;

        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            savedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
            Debug.Log($"Loading saved character ID: {savedCharacterID} (no animation - main menu)");
        }
        else
        {
            Debug.LogWarning("No save data found, using default character (ID: 0)");
        }

        // Load the character WITHOUT triggering animation (for main menu)
        if (characterVisualSwapper != null)
        {
            // Check if the new method exists, otherwise fall back
            var methodInfo = characterVisualSwapper.GetType().GetMethod("LoadCharacterWithSavedSkinNoAnimation");
            if (methodInfo != null)
            {
                methodInfo.Invoke(characterVisualSwapper, new object[] { savedCharacterID });
            }
            else
            {
                // Fallback to regular method and then stop animation
                characterVisualSwapper.LoadCharacterWithSavedSkin(savedCharacterID);
                characterVisualSwapper.StopLookAroundAnimation();
            }
        }
    }

    void EnableStarterAssetsControl()
    {
        GameObject player = FindPlayer();
        if (player == null) return;

        // Enable all Starter Assets components
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
                Debug.Log($"Enabled: {typeName}");
            }
        }
    }

    void DisableStarterAssetsControl()
    {
        GameObject player = FindPlayer();
        if (player == null) return;

        // Disable all Starter Assets components (in menu we usually don't want movement)
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
                Debug.Log($"Disabled (for menu): {typeName}");
            }
        }
    }

    GameObject FindPlayer()
    {
        // Find the player GameObject
        GameObject player = GameObject.Find("PlayerArmature");
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Could not find Player GameObject!");
                return null;
            }
        }

        Debug.Log($"Found player: {player.name}");
        return player;
    }

    // Optional: Force refresh character
    public void RefreshCharacter()
    {
        LoadSavedCharacterNoAnimation();
        Debug.Log("Character refreshed (no animation)");
    }

    // Called when entering character selection
    public void OnEnterCharacterSelection()
    {
        Debug.Log("Entering character selection mode");
        // You might want to disable player movement here if not already
    }

    // Called when exiting character selection
    public void OnExitCharacterSelection()
    {
        Debug.Log("Exiting character selection mode");
        // Refresh character without animation
        LoadSavedCharacterNoAnimation();
    }
}