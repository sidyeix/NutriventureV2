using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu_Manager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingsPanel;
    public GameObject ResetConfirmationPanel;
    public GameObject menuCanvas; // Assign your main menu UI Canvas here
    public GameObject joystickCanvas; // Assign your joystick UI Canvas here

    [Header("Settings UI Elements")]
    public Slider musicVolumeSlider;
    public Slider soundVolumeSlider;
    public Button resetDataButton;
    public Button confirmResetButton;
    public Button cancelResetButton;

    [Header("Character Selection References")]
    public CharacterDatabase characterDatabase;
    public Transform characterSpawnPoint;
    public List<Button> characterButtons;

    [Header("Character Visual Swapper")]
    public CharacterVisualSwapper characterVisualSwapper; // Assign this in inspector

    [Header("Character Selection Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Input Management")]
    public InputManager inputManager; // Assign this in inspector

    // Local character data - temporary until saved
    private GameObject currentSpawnedCharacter;
    private int currentSelectedCharacterID = 0;

    void Start()
    {
        SettingsPanel.SetActive(false);
        ResetConfirmationPanel.SetActive(false);

        // Ensure joystick canvas is disabled at start
        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(false);
        }

        // Ensure menu canvas is enabled at start
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }

        // Disable player input immediately
        if (inputManager != null)
        {
            inputManager.DisablePlayerInput();
        }
        else
        {
            Debug.LogWarning("InputManager not assigned - player input might interfere with UI");
        }

        // Wait for GameDataManager to be ready, then initialize
        StartCoroutine(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        // Wait for GameDataManager to initialize
        yield return null;

        // If GameDataManager exists, load the saved character, otherwise use default
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            currentSelectedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
        }

        InitializeCharacterSystem();
        InitializeSettingsUI();
    }

    // START BUTTON - This is where we enable input and switch to game mode
    public void OnStartButtonClicked()
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        Debug.Log($"Start button clicked - Saving character ID {currentSelectedCharacterID} to GameDataManager");

        // Save the currently selected character to GameDataManager
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = currentSelectedCharacterID;
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Character ID {currentSelectedCharacterID} saved to GameDataManager");
        }
        else
        {
            Debug.LogWarning("GameDataManager not found - character selection won't be persisted");
        }

        // Switch to game mode
        SwitchToGameMode();
    }

    private void SwitchToGameMode()
    {
        // ENABLE player input
        if (inputManager != null)
        {
            inputManager.EnablePlayerInput();
            Debug.Log("Player input enabled");
        }

        // Show joystick UI
        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(true);
            Debug.Log("Joystick UI enabled");
        }
        else
        {
            Debug.LogWarning("Joystick Canvas not assigned!");
        }

        // Hide menu UI
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
            Debug.Log("Menu UI disabled");
        }
        else
        {
            Debug.LogWarning("Menu Canvas not assigned!");
        }

        // Optional: Hide any spawned preview character if using fallback system
        if (currentSpawnedCharacter != null)
        {
            currentSpawnedCharacter.SetActive(false);
        }

        Debug.Log("Switched to game mode - Player can now move with joystick controls");
    }

    // Method to return to menu (in case you need it later)
    public void ReturnToMenu()
    {
        // Disable player input
        if (inputManager != null)
        {
            inputManager.DisablePlayerInput();
            Debug.Log("Player input disabled");
        }

        // Hide joystick UI
        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(false);
            Debug.Log("Joystick UI disabled");
        }

        // Show menu UI
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
            Debug.Log("Menu UI enabled");
        }

        // Show preview character if using fallback system
        if (currentSpawnedCharacter != null)
        {
            currentSpawnedCharacter.SetActive(true);
        }

        Debug.Log("Returned to menu mode");
    }

    // Generic button click method for other buttons
    public void OnButtonClick()
    {
        AudioHandler.Instance.PlayButtonClick();
    }

    public void ToggleSettings()
    {
        bool newState = !SettingsPanel.activeSelf;
        SettingsPanel.SetActive(newState);

        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // If opening settings, update sliders
        if (newState)
        {
            UpdateSettingsSliders();
        }

        // Ensure input stays DISABLED when settings are open/closed
        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }
    }

    // Settings Panel Methods
    private void InitializeSettingsUI()
    {
        // Setup slider listeners
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        // Setup reset button
        if (resetDataButton != null)
        {
            resetDataButton.onClick.AddListener(OnResetDataClicked);
        }

        // Setup confirmation panel buttons
        if (confirmResetButton != null)
        {
            confirmResetButton.onClick.AddListener(OnConfirmResetClicked);
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.onClick.AddListener(OnCancelResetClicked);
        }
    }

    private void UpdateSettingsSliders()
    {
        if (AudioHandler.Instance != null)
        {
            musicVolumeSlider.value = AudioHandler.Instance.GetMusicVolume();
            soundVolumeSlider.value = AudioHandler.Instance.GetSoundVolume();
        }
    }

    private void OnMusicVolumeChanged(float volume)
    {
        AudioHandler.Instance.SetMusicVolume(volume);
    }

    private void OnSoundVolumeChanged(float volume)
    {
        AudioHandler.Instance.SetSoundVolume(volume);
    }

    private void OnResetDataClicked()
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // Show confirmation panel, hide settings panel
        SettingsPanel.SetActive(false);
        ResetConfirmationPanel.SetActive(true);
    }

    private void OnConfirmResetClicked()
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // Reset game data
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetGameData();

            // Update character selection to default
            currentSelectedCharacterID = 0;

            // Use visual swapper if available, otherwise fallback
            if (characterVisualSwapper != null)
            {
                CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(currentSelectedCharacterID);
                if (characterData != null)
                {
                    characterVisualSwapper.ApplyCharacterVisuals(characterData);
                }
            }
            else
            {
                SpawnCharacter(currentSelectedCharacterID);
            }

            UpdateButtonAppearance();

            // Update audio settings
            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.SetMusicVolume(GameDataManager.Instance.CurrentGameData.musicVolume);
                AudioHandler.Instance.SetSoundVolume(GameDataManager.Instance.CurrentGameData.soundVolume);
                UpdateSettingsSliders();
            }
        }

        // Close confirmation panel, show settings panel
        ResetConfirmationPanel.SetActive(false);
        SettingsPanel.SetActive(true);

        Debug.Log("Game data reset successfully!");

        // Ensure input stays DISABLED after reset
        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }
    }

    private void OnCancelResetClicked()
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // Close confirmation panel, show settings panel
        ResetConfirmationPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    // Character Selection Methods - UPDATED
    private void InitializeCharacterSystem()
    {
        // Use visual swapper if available, otherwise use old system
        if (characterVisualSwapper != null)
        {
            CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(currentSelectedCharacterID);
            if (characterData != null)
            {
                characterVisualSwapper.ApplyCharacterVisuals(characterData);
                Debug.Log($"Applied character visuals for: {characterData.characterName}");
            }
        }
        else
        {
            // Fallback to old system
            SpawnCharacter(currentSelectedCharacterID);
            Debug.Log("Using fallback character spawning system");
        }

        UpdateButtonAppearance();

        // Clear existing listeners first to prevent duplicates
        foreach (Button button in characterButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        // Set up button listeners
        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => OnCharacterSelected(index));
        }

        Debug.Log($"Character system initialized with character ID: {currentSelectedCharacterID}");
    }

    private void OnCharacterSelected(int buttonIndex)
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // Get character data from database using button index
        if (buttonIndex >= characterDatabase.characters.Count)
        {
            Debug.LogError($"Button index {buttonIndex} exceeds character database count");
            return;
        }

        CharacterDatabase.CharacterData selectedCharacter = characterDatabase.characters[buttonIndex];
        int characterID = selectedCharacter.characterID;

        Debug.Log($"Character {selectedCharacter.characterName} (ID: {characterID}) selected");

        if (characterID == currentSelectedCharacterID)
        {
            Debug.Log("Same character selected, ignoring");
            return;
        }

        // Check if character is unlocked using database method
        if (!characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData))
        {
            Debug.Log($"Character {selectedCharacter.characterName} is locked!");
            return;
        }

        // Update LOCAL data only - not saved to GameDataManager yet
        currentSelectedCharacterID = characterID;

        // Use visual swapper if available, otherwise use old system
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(selectedCharacter);
            Debug.Log($"Applied character visuals for: {selectedCharacter.characterName}");
        }
        else
        {
            // Fallback to old system
            SpawnCharacter(characterID);
        }

        UpdateButtonAppearance();

        // Play character selection sound if available
        if (selectedCharacter.selectionSound != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(selectedCharacter.selectionSound);
        }

        Debug.Log($"Character preview changed to: {selectedCharacter.characterName} (ID: {characterID}) - Not saved yet");

        // Ensure input stays DISABLED during character selection
        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }
    }

    // Keep this as fallback method
    private void SpawnCharacter(int characterID)
    {
        CharacterDatabase.CharacterData characterData = characterDatabase.GetCharacterByID(characterID);
        if (characterData == null || characterData.characterPrefab == null)
        {
            Debug.LogError($"Character data or prefab not found for ID: {characterID}");
            return;
        }

        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }

        currentSpawnedCharacter = Instantiate(
            characterData.characterPrefab,
            characterSpawnPoint.position,
            characterSpawnPoint.rotation
        );

        // Disable any components in preview character
        DisablePreviewCharacterComponents(currentSpawnedCharacter);

        Debug.Log($"Spawned character preview: {characterData.characterName}");
    }

    private void DisablePreviewCharacterComponents(GameObject previewCharacter)
    {
        // Disable animator in preview
        Animator animator = previewCharacter.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

        // Disable any controller scripts
        MonoBehaviour[] scripts = previewCharacter.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && script.enabled)
            {
                if (script.GetType().Name.Contains("Controller") ||
                    script.GetType().Name.Contains("Movement") ||
                    script.GetType().Name.Contains("Input"))
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void UpdateButtonAppearance()
    {
        for (int i = 0; i < characterButtons.Count; i++)
        {
            if (i >= characterDatabase.characters.Count) continue;

            Button button = characterButtons[i];
            Image buttonImage = button.GetComponent<Image>();
            CharacterDatabase.CharacterData characterData = characterDatabase.characters[i];

            if (buttonImage != null)
            {
                bool isSelected = (characterData.characterID == currentSelectedCharacterID);
                bool isUnlocked = characterDatabase.IsCharacterUnlocked(characterData.characterID, GameDataManager.Instance.CurrentGameData);

                buttonImage.color = isSelected ? selectedColor : normalColor;
                button.interactable = isUnlocked;

                if (!isUnlocked)
                {
                    buttonImage.color = Color.gray;
                }
            }
        }
    }

    // Method to get the selected character data
    public CharacterDatabase.CharacterData GetCurrentCharacterData()
    {
        return characterDatabase.GetCharacterByID(currentSelectedCharacterID);
    }

    public int GetCurrentCharacterID()
    {
        return currentSelectedCharacterID;
    }

    // Helper methods to manage unlocked characters
    public void UnlockCharacter(int characterID)
    {
        if (!GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Contains(characterID))
        {
            GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs.Add(characterID);
            GameDataManager.Instance.SaveGameData();
            UpdateButtonAppearance();
        }
    }

    // Fallback method to disable StarterAssets directly if InputManager is not available
    private void DisableStarterAssetsDirectly()
    {
        // Find the player controller
        GameObject player = GameObject.Find("PlayerArmature");
        if (player != null)
        {
            // Disable ThirdPersonController
            MonoBehaviour controller = player.GetComponent<MonoBehaviour>();
            if (controller != null && controller.GetType().Name.Contains("ThirdPersonController"))
            {
                controller.enabled = false;
                Debug.Log("ThirdPersonController disabled directly");
            }

            // Disable StarterAssetsInputs
            MonoBehaviour inputs = player.GetComponent<MonoBehaviour>();
            if (inputs != null && inputs.GetType().Name.Contains("StarterAssetsInputs"))
            {
                inputs.enabled = false;
                Debug.Log("StarterAssetsInputs disabled directly");
            }
        }
    }

    // Called when the menu is destroyed or scene changes
    void OnDestroy()
    {
        // Clean up any spawned characters (only for fallback system)
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
    }
}