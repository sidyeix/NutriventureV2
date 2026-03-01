using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineVirtualCamera characterChangeCamera;
    public CinemachineVirtualCamera skinSelectionCamera;

    [Header("UI References")]
    public CanvasGroup characterSelectionCanvas;
    public CanvasGroup skinSelectionCanvas;
    public CanvasGroup characterControlsCanvas;

    [Header("Buttons")]
    public Button selectCharacterButton;
    public Button previewSelectButton;
    public Button skinButton;
    public Button backButton;
    public Button characterButton;
    public Button exitButton; // Exit button to return to character selection without saving

    [Header("Panel References")]
    public CharacterSelectionPanel characterSelectionPanel;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    public float elementFadeDelay = 0.1f;

    [Header("Character Rotation")]
    public CharacterRotationController characterRotationController;

    [Header("Character Visual Management")]
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;

    [Header("Skin Selection")]
    public SkinSelectionController skinSelectionController;

    [Header("Platform Integration")]
    public SimpleCharacterPlatformTrigger platformTrigger;

    [Header("Player Armature Animator")]
    public Animator playerArmatureAnimator;

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip buttonClickSound;

    private bool isInCharacterSelection = false;
    private bool isInSkinSelection = false;
    private int pendingCharacterSelection = -1;
    private int selectedSkinID = -1;
    private Coroutine exitCoroutine;

    private int lastSavedCharacterID = 0;
    private int lastSavedSkinID = -1;
    private GameDataManager gameDataManager;
    private int previousCharacterID = -1; // Track previous character for reverting

    void Start()
    {
        gameDataManager = GameDataManager.Instance;

        // Initialize camera priorities to 0
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 0;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        if (skinSelectionCanvas != null)
        {
            skinSelectionCanvas.gameObject.SetActive(false);
            skinSelectionCanvas.alpha = 0f;
        }

        if (characterControlsCanvas != null)
        {
            characterControlsCanvas.alpha = 1f;
            characterControlsCanvas.interactable = true;
            characterControlsCanvas.blocksRaycasts = true;
        }

        LoadSavedData();
        SetupButtonListeners();

        StartCoroutine(InitializeEquippedCharacter());
    }

    IEnumerator InitializeEquippedCharacter()
    {
        yield return new WaitForSeconds(0.1f);

        MainMenuController mainMenuController = FindObjectOfType<MainMenuController>();
        if (mainMenuController != null)
        {
            Debug.Log("MainMenuController detected - skipping character load in CharacterSelectionController");
            yield break;
        }

        if (gameDataManager != null && gameDataManager.CurrentGameData != null && characterVisualSwapper != null)
        {
            int equippedCharacterID = gameDataManager.CurrentGameData.selectedCharacterID;
            int equippedSkinID = gameDataManager.CurrentGameData.GetSelectedSkinForCharacter(equippedCharacterID);

            characterVisualSwapper.LoadCharacterWithSavedSkinNoAnimation(equippedCharacterID);
        }
    }

    void LoadSavedData()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            lastSavedCharacterID = gameDataManager.CurrentGameData.selectedCharacterID;
            lastSavedSkinID = gameDataManager.CurrentGameData.GetSelectedSkinForCharacter(lastSavedCharacterID);
            pendingCharacterSelection = lastSavedCharacterID;
            selectedSkinID = lastSavedSkinID;
        }
    }

    void SetupButtonListeners()
    {
        if (selectCharacterButton != null)
        {
            selectCharacterButton.onClick.RemoveAllListeners();
            selectCharacterButton.onClick.AddListener(OnFirstSelectButtonClicked);
        }

        if (previewSelectButton != null)
        {
            previewSelectButton.onClick.RemoveAllListeners();
            previewSelectButton.onClick.AddListener(OnSecondSelectButtonClicked);
        }

        if (skinButton != null)
        {
            skinButton.onClick.RemoveAllListeners();
            skinButton.onClick.AddListener(OnSkinButtonClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (characterButton != null)
        {
            characterButton.onClick.RemoveAllListeners();
            characterButton.onClick.AddListener(OnCharacterButtonClicked);
        }

        // Setup exit button
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    public void ActivateCharacterSelection()
    {
        if (isInCharacterSelection) return;

        isInCharacterSelection = true;
        Debug.Log("CharacterSelectionController: Entering character selection mode");

        // Refresh the character panel data
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.RefreshPanel();
        }

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        if (pendingCharacterSelection == -1)
        {
            pendingCharacterSelection = lastSavedCharacterID;
        }

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(lastSavedCharacterID);
        }

        UpdateCharacterHighlight(lastSavedCharacterID, true);

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;
        if (skinButton != null) skinButton.interactable = true;

        Debug.Log("CharacterSelectionController: Ready for selection");
    }

    public void OnFirstSelectButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("First select button clicked");
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnSecondSelectButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Second select button clicked");
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnCharacterButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Character button clicked");

        // NOTIFY ENVIRONMENT CONTROLLER - EXITING SKIN SELECTION
        if (skinSelectionController != null && skinSelectionController.skinEnvironmentController != null)
        {
            skinSelectionController.skinEnvironmentController.OnExitSkinSelection();
        }

        ResetToCharacterSelection();
    }

    // Exit button handler - exits without saving any changes
    public void OnExitButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Exit button clicked - Exiting to character selection without saving");

        // NOTIFY ENVIRONMENT CONTROLLER - EXITING SKIN SELECTION if in skin mode
        if (isInSkinSelection && skinSelectionController != null && skinSelectionController.skinEnvironmentController != null)
        {
            skinSelectionController.skinEnvironmentController.OnExitSkinSelection();
        }

        // Revert to saved character/skin (discard any pending changes)
        RevertToSavedCharacter();

        // Exit without saving
        StartCoroutine(ExitWithoutSaving());
    }

    // New coroutine for exit without saving
    private IEnumerator ExitWithoutSaving()
    {
        Debug.Log("Exiting without saving changes...");

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        // Reset camera priorities to 0
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 0;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        // Small delay to ensure camera switches
        yield return null;

        if (playerArmatureAnimator != null)
        {
            playerArmatureAnimator.SetBool("LookAround", false);
        }

        // Hide skin selection UI if active
        if (isInSkinSelection && skinSelectionCanvas != null && skinSelectionCanvas.gameObject.activeSelf)
        {
            HideSkinSelectionUI();
        }

        // Hide character selection UI
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, characterSelectionCanvas.alpha, 0f, fadeDuration));
            characterSelectionCanvas.gameObject.SetActive(false);
        }

        // Notify platform trigger to exit
        if (platformTrigger != null)
        {
            yield return platformTrigger.ExitCharacterSelection();
        }

        CompleteExitProcess();
    }

    public void OnSkinButtonClicked()
    {
        PlayButtonClickSound();

        if (isInCharacterSelection && !isInSkinSelection)
        {
            int characterID = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
            Debug.Log($"Entering skin selection for character {characterID}");

            // Store the current character ID to revert if needed
            previousCharacterID = characterID;

            if (gameDataManager != null && gameDataManager.CurrentGameData != null)
            {
                selectedSkinID = gameDataManager.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            }

            // Show skin selection UI
            ShowSkinSelectionUI();

            if (skinSelectionController != null)
            {
                // Make sure the skin selection controller is active
                skinSelectionController.gameObject.SetActive(true);

                // Tell the skin controller we're entering
                skinSelectionController.EnterSkinSelection(characterID);

                // CRITICAL: Ensure we start with default skin/environment
                skinSelectionController.ResetToDefaultSkin();
            }

            EnterSkinSelection();
        }
    }

    public void OnBackButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Back button clicked");

        if (isInSkinSelection)
        {
            Debug.Log("Character Controller: Exiting skin selection via back button");

            // NOTIFY ENVIRONMENT CONTROLLER - EXITING SKIN SELECTION
            if (skinSelectionController != null && skinSelectionController.skinEnvironmentController != null)
            {
                skinSelectionController.skinEnvironmentController.OnExitSkinSelection();
            }

            // REVERT to saved character/skin before exiting
            RevertToSavedCharacter();

            // Exit skin selection
            HideSkinSelectionUI();
            ExitSkinSelection();
        }
        else if (isInCharacterSelection)
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    public void OnSkinSelectionClosed()
    {
        Debug.Log("Skin selection closed notification received");

        // NOTIFY ENVIRONMENT CONTROLLER - EXITING SKIN SELECTION
        if (skinSelectionController != null && skinSelectionController.skinEnvironmentController != null)
        {
            skinSelectionController.skinEnvironmentController.OnExitSkinSelection();
        }

        // REVERT to saved character/skin before exiting
        RevertToSavedCharacter();

        if (skinSelectionController != null)
        {
            // We don't disable it here, just hide the panel
            // skinSelectionController.gameObject.SetActive(false);
        }

        ExitSkinSelection();
    }

    // Method to revert to saved character/skin
    private void RevertToSavedCharacter()
    {
        Debug.Log($"Reverting to saved character: {lastSavedCharacterID} with skin: {lastSavedSkinID}");

        if (characterVisualSwapper != null)
        {
            if (lastSavedSkinID == -1)
            {
                var characterData = characterDatabase.GetCharacterByID(lastSavedCharacterID);
                if (characterData != null)
                {
                    characterVisualSwapper.ApplyCharacterVisuals(characterData);
                }
            }
            else
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(lastSavedSkinID);
            }
        }

        // Reset pending selection to saved character
        pendingCharacterSelection = lastSavedCharacterID;
        selectedSkinID = lastSavedSkinID;
    }

    private void ShowSkinSelectionUI()
    {
        Debug.Log("Showing Skin Selection UI");

        // Set skin camera priority to 30
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 30;
            Debug.Log("Skin camera priority set to 30");
        }

        // Lower character camera priority
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 10;
        }

        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, characterSelectionCanvas.alpha, 0f, fadeDuration));
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        if (skinSelectionCanvas != null)
        {
            skinSelectionCanvas.gameObject.SetActive(true);
            StartCoroutine(FadeCanvasGroup(skinSelectionCanvas, 0f, 1f, fadeDuration));
            skinSelectionCanvas.interactable = true;
            skinSelectionCanvas.blocksRaycasts = true;
        }
    }

    private void HideSkinSelectionUI()
    {
        Debug.Log("Hiding Skin Selection UI");

        // Reset camera priorities
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
            Debug.Log("Skin camera priority set to 0");
        }

        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 30;
        }

        if (skinSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(skinSelectionCanvas, skinSelectionCanvas.alpha, 0f, fadeDuration));
            skinSelectionCanvas.interactable = false;
            skinSelectionCanvas.blocksRaycasts = false;
            StartCoroutine(DeactivateAfterDelay(skinSelectionCanvas.gameObject, fadeDuration));
        }

        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }
    }

    private IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    private void EnterSkinSelection()
    {
        isInSkinSelection = true;
        Debug.Log("Entering Skin Selection State");
    }

    private void ExitSkinSelection()
    {
        isInSkinSelection = false;
        Debug.Log("Exiting Skin Selection State from Character Controller");
    }

    public void OnSelectCharacterConfirmed(int characterID = -1)
    {
        Debug.Log($"=== CHARACTER SELECTION CONFIRMED ===");

        int characterToSave = characterID != -1 ? characterID :
                            (pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID);

        lastSavedCharacterID = characterToSave;
        lastSavedSkinID = selectedSkinID;

        SaveCharacterSelection(characterToSave);
        UpdateCharacterHighlight(characterToSave, true);

        Debug.Log($"Character {characterToSave} confirmed with skin {selectedSkinID}");

        if (exitCoroutine != null)
            StopCoroutine(exitCoroutine);
        exitCoroutine = StartCoroutine(SimpleExitSequence());
    }

    private IEnumerator SimpleExitSequence()
    {
        Debug.Log("Starting exit sequence...");

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        // Reset camera priorities to 0
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 0;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        // Small delay to ensure camera switches
        yield return null;

        if (playerArmatureAnimator != null)
        {
            playerArmatureAnimator.SetBool("LookAround", false);
        }

        if (isInSkinSelection && skinSelectionCanvas != null && skinSelectionCanvas.gameObject.activeSelf)
        {
            HideSkinSelectionUI();
        }

        if (platformTrigger != null)
        {
            yield return platformTrigger.ExitCharacterSelection();
        }

        CompleteExitProcess();
        Debug.Log("Exit sequence completed");
    }

    public void ExitCharacterSelectionWithoutSaving()
    {
        StartCoroutine(ExitCharacterSelectionRoutine());
    }

    private IEnumerator ExitCharacterSelectionRoutine()
    {
        if (isInSkinSelection && skinSelectionCanvas != null && skinSelectionCanvas.gameObject.activeSelf)
        {
            HideSkinSelectionUI();
        }

        if (platformTrigger != null)
        {
            yield return platformTrigger.ExitCharacterSelection();
        }

        CompleteExitProcess();
    }

    private void CompleteExitProcess()
    {
        isInCharacterSelection = false;
        isInSkinSelection = false;

        // Ensure all cameras are set to 0
        if (characterChangeCamera != null)
            characterChangeCamera.Priority = 0;
        if (skinSelectionCamera != null)
            skinSelectionCamera.Priority = 0;

        Debug.Log("Character selection complete");
    }

    public void OnCharacterPreviewSelected(int characterID)
    {
        pendingCharacterSelection = characterID;
        Debug.Log($"Character {characterID} selected for preview");

        UpdateCharacterHighlight(characterID, false);

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(characterID);
        }
    }

    private void UpdateCharacterHighlight(int characterID, bool isEquipped)
    {
        if (characterSelectionPanel != null)
        {
            GameObject characterButton = characterSelectionPanel.GetCharacterButton(characterID);
            if (characterButton != null)
            {
                CharacterButtonData buttonData = characterButton.GetComponent<CharacterButtonData>();
                if (buttonData != null && buttonData.selectedHighlight != null)
                {
                    bool shouldHighlight = isEquipped || (characterID == pendingCharacterSelection);
                    buttonData.selectedHighlight.enabled = shouldHighlight;
                    buttonData.isEquipped = isEquipped;
                }
            }
        }

        if (isEquipped && characterSelectionPanel != null)
        {
            characterSelectionPanel.SetCharacterEquipped(characterID, true);
        }
    }

    private void SaveCharacterSelection(int characterID)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.selectedCharacterID = characterID;

            if (selectedSkinID != -1)
            {
                gameDataManager.CurrentGameData.SetSelectedSkinForCharacter(characterID, selectedSkinID);
            }

            gameDataManager.SaveGameData();
            Debug.Log($"Saved to GameData: Character={characterID}, Skin={selectedSkinID}");
        }
    }

    public void UpdateSkinSelection(int skinID)
    {
        selectedSkinID = skinID;
        Debug.Log($"Skin selection updated: {skinID}");

        if (pendingCharacterSelection != -1 && gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.SetSelectedSkinForCharacter(pendingCharacterSelection, skinID);

            if (characterVisualSwapper != null)
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(skinID);
            }
        }
    }

    private void ResetToCharacterSelection()
    {
        Debug.Log("Resetting to character selection");

        if (isInSkinSelection && skinSelectionCanvas != null)
        {
            HideSkinSelectionUI();

            if (skinSelectionController != null)
            {
                skinSelectionController.ExitSkinSelection();
                // We don't disable the GameObject, just hide its panel
                // skinSelectionController.gameObject.SetActive(false);
            }
        }

        isInSkinSelection = false;
        isInCharacterSelection = true;

        Debug.Log("Reset to character selection completed");
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    // Helper method to play button click sound
    private void PlayButtonClickSound()
    {
        if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound);
        }
        else if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    public bool IsInCharacterSelection() => isInCharacterSelection;
    public bool IsInSkinSelection() => isInSkinSelection;
    public int GetPendingCharacterSelection() => pendingCharacterSelection;
    public int GetSelectedSkinID() => selectedSkinID;
    public int GetLastSavedCharacterID() => lastSavedCharacterID;
    public int GetLastSavedSkinID() => lastSavedSkinID;

    void OnDestroy()
    {
        if (exitCoroutine != null) StopCoroutine(exitCoroutine);
    }
}