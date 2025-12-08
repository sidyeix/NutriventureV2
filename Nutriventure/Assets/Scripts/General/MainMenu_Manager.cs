using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cinemachine;

public class MainMenu_Manager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingsPanel;
    public GameObject ResetConfirmationPanel;
    public GameObject menuCanvas;
    public GameObject joystickCanvas;

    [Header("Camera References")]
    public CinemachineVirtualCamera menuVirtualCamera;
    public CinemachineVirtualCamera playerFollowCamera;

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
    public CharacterVisualSwapper characterVisualSwapper;

    [Header("Character Selection Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Input Management")]
    public InputManager inputManager;

    [Header("Character Selection Animator")]
    public Animator characterSelectionAnimator;

    [Header("Character Rotation Controller")]
    public CharacterRotationController characterRotationController;

    // Local character data - temporary until saved
    private GameObject currentSpawnedCharacter;
    private int currentSelectedCharacterID = 0;

    void Start()
    {
        SettingsPanel.SetActive(false);
        ResetConfirmationPanel.SetActive(false);

        if (joystickCanvas != null)
        {
            joystickCanvas.SetActive(false);
        }

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }

        SetupCameras();

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.EnsureAnimatorEnabled();
        }

        if (inputManager != null)
        {
            inputManager.DisablePlayerInput();
        }
        else
        {
            Debug.LogWarning("InputManager not assigned - player input might interfere with UI");
        }

        StartCoroutine(InitializeAfterFrame());
    }

    private void SetupCameras()
    {
        if (menuVirtualCamera != null)
        {
            menuVirtualCamera.Priority = 10;
        }
        else
        {
            Debug.LogWarning("Menu Virtual Camera not assigned!");
        }

        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
        }
        else
        {
            Debug.LogWarning("Player Follow Camera not assigned!");
        }

        Debug.Log("Cameras setup: Menu camera active, Player camera inactive");
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return null;

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
        AudioHandler.Instance.PlayButtonClick();

        Debug.Log($"Start button clicked - Saving character ID {currentSelectedCharacterID} to GameDataManager");

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
        // Switch to player follow camera
        SwitchToPlayerCamera();

        // STOP LookAround animation and set parameter to false
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.StopLookAroundAnimation();
            Debug.Log("LookAround animation stopped for gameplay");
        }

        // Disable CharacterSelection animator when starting the game
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.enabled = false;
            Debug.Log("CharacterSelection animator disabled for gameplay");
        }

        // DISABLE CharacterRotationController to give control back to Starter Assets
        if (characterRotationController != null)
        {
            characterRotationController.enabled = false;
            Debug.Log("CharacterRotationController disabled - Starter Assets now controls rotation");
        }

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
        // Switch back to menu camera
        SwitchToMenuCamera();

        // Re-enable CharacterSelection animator when returning to menu
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.enabled = true;
            Debug.Log("CharacterSelection animator re-enabled for menu");
        }

        // RE-ENABLE CharacterRotationController for menu character rotation
        if (characterRotationController != null)
        {
            characterRotationController.enabled = true;
            characterRotationController.ResetRotation();
            Debug.Log("CharacterRotationController re-enabled for menu rotation");
        }

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

    private void SwitchToPlayerCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 10;
            Debug.Log("Switched to Player Follow Camera");
        }
        else
        {
            Debug.LogWarning("Player Follow Camera not assigned - cannot switch to player view");
        }

        if (menuVirtualCamera != null)
        {
            menuVirtualCamera.Priority = 0;
        }
    }

    private void SwitchToMenuCamera()
    {
        if (menuVirtualCamera != null)
        {
            menuVirtualCamera.Priority = 10;
            Debug.Log("Switched to Menu Camera");
        }
        else
        {
            Debug.LogWarning("Menu Virtual Camera not assigned - cannot switch to menu view");
        }

        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
        }
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

        AudioHandler.Instance.PlayButtonClick();

        if (newState)
        {
            UpdateSettingsSliders();
        }

        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }
    }

    // Settings Panel Methods
    private void InitializeSettingsUI()
    {
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        if (resetDataButton != null)
        {
            resetDataButton.onClick.AddListener(OnResetDataClicked);
        }

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
        AudioHandler.Instance.PlayButtonClick();

        SettingsPanel.SetActive(false);
        ResetConfirmationPanel.SetActive(true);
    }

    private void OnConfirmResetClicked()
    {
        AudioHandler.Instance.PlayButtonClick();

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetGameData();

            currentSelectedCharacterID = 0;

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

            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.SetMusicVolume(GameDataManager.Instance.CurrentGameData.musicVolume);
                AudioHandler.Instance.SetSoundVolume(GameDataManager.Instance.CurrentGameData.soundVolume);
                UpdateSettingsSliders();
            }
        }

        ResetConfirmationPanel.SetActive(false);
        SettingsPanel.SetActive(true);

        Debug.Log("Game data reset successfully!");

        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }
    }

    private void OnCancelResetClicked()
    {
        AudioHandler.Instance.PlayButtonClick();

        ResetConfirmationPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    // Character Selection Methods
    private void InitializeCharacterSystem()
    {
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
            SpawnCharacter(currentSelectedCharacterID);
            Debug.Log("Using fallback character spawning system");
        }

        UpdateButtonAppearance();

        foreach (Button button in characterButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => OnCharacterSelected(index));
        }

        Debug.Log($"Character system initialized with character ID: {currentSelectedCharacterID}");
    }

    private void OnCharacterSelected(int buttonIndex)
    {
        AudioHandler.Instance.PlayButtonClick();

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

        if (!characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData))
        {
            Debug.Log($"Character {selectedCharacter.characterName} is locked!");
            return;
        }

        currentSelectedCharacterID = characterID;

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(selectedCharacter);
            Debug.Log($"Applied character visuals for: {selectedCharacter.characterName}");
        }
        else
        {
            SpawnCharacter(characterID);
        }

        UpdateButtonAppearance();

        if (selectedCharacter.selectionSound != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(selectedCharacter.selectionSound);
        }

        Debug.Log($"Character preview changed to: {selectedCharacter.characterName} (ID: {characterID}) - Not saved yet");

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

        DisablePreviewCharacterComponents(currentSpawnedCharacter);

        Debug.Log($"Spawned character preview: {characterData.characterName}");
    }

    private void DisablePreviewCharacterComponents(GameObject previewCharacter)
    {
        Animator animator = previewCharacter.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }

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
        GameObject player = GameObject.Find("PlayerArmature");
        if (player != null)
        {
            MonoBehaviour controller = player.GetComponent<MonoBehaviour>();
            if (controller != null && controller.GetType().Name.Contains("ThirdPersonController"))
            {
                controller.enabled = false;
                Debug.Log("ThirdPersonController disabled directly");
            }

            MonoBehaviour inputs = player.GetComponent<MonoBehaviour>();
            if (inputs != null && inputs.GetType().Name.Contains("StarterAssetsInputs"))
            {
                inputs.enabled = false;
                Debug.Log("StarterAssetsInputs disabled directly");
            }
        }
    }

    void OnDestroy()
    {
        if (currentSpawnedCharacter != null)
        {
            Destroy(currentSpawnedCharacter);
        }
    }
}