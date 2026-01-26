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
    public CanvasGroup characterSelectionCanvas; // Main character selection UI
    public CanvasGroup skinSelectionCanvas; // Skin selection UI (RENAMED from characterPreviewCanvas)
    public CanvasGroup characterControlsCanvas;

    [Header("Buttons")]
    public Button selectCharacterButton;
    public Button previewSelectButton;
    public Button skinButton;
    public Button backButton;
    public Button characterButton;

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

    private bool isInCharacterSelection = false;
    private bool isInSkinSelection = false;
    private int pendingCharacterSelection = -1;
    private int selectedSkinID = -1;
    private Coroutine exitCoroutine;

    private int lastSavedCharacterID = 0;
    private int lastSavedSkinID = -1;

    void Start()
    {
        // Initialize references
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

        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        LoadSavedData();
        SetupButtonListeners();

        StartCoroutine(InitializeEquippedCharacter());
    }

    IEnumerator InitializeEquippedCharacter()
    {
        yield return new WaitForSeconds(0.1f);

        // Check if we're in the main menu - if yes, skip loading character
        // because MainMenuController already loaded it
        MainMenuController mainMenuController = FindObjectOfType<MainMenuController>();
        if (mainMenuController != null)
        {
            Debug.Log("MainMenuController detected - skipping character load in CharacterSelectionController");
            yield break;
        }

        // Only load character if MainMenuController is not present (e.g., in game scene)
        if (GameDataManager.Instance != null && characterVisualSwapper != null)
        {
            int equippedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
            int equippedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(equippedCharacterID);

            characterVisualSwapper.LoadCharacterWithSavedSkinNoAnimation(equippedCharacterID);
        }
    }

    void LoadSavedData()
    {
        if (GameDataManager.Instance != null)
        {
            lastSavedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
            lastSavedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(lastSavedCharacterID);
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
    }

    // ============ PUBLIC METHODS ============

    public void ActivateCharacterSelection()
    {
        if (isInCharacterSelection) return;

        isInCharacterSelection = true;
        Debug.Log("CharacterSelectionController: Entering character selection mode");

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        if (pendingCharacterSelection == -1)
        {
            pendingCharacterSelection = lastSavedCharacterID;
        }

        // Load character with saved skin
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(lastSavedCharacterID);
        }

        UpdateCharacterHighlight(lastSavedCharacterID, true);

        // Make sure buttons are enabled
        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;
        if (skinButton != null) skinButton.interactable = true;

        Debug.Log("CharacterSelectionController: Ready for selection");
    }

    // ============ BUTTON HANDLERS ============

    public void OnFirstSelectButtonClicked()
    {
        Debug.Log("First select button clicked");
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnSecondSelectButtonClicked()
    {
        Debug.Log("Second select button clicked");
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnCharacterButtonClicked()
    {
        Debug.Log("Character button clicked");
        ResetToCharacterSelection();
    }

    public void OnSkinButtonClicked()
    {
        if (isInCharacterSelection && !isInSkinSelection)
        {
            int characterID = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
            Debug.Log($"Entering skin selection for character {characterID}");

            if (GameDataManager.Instance != null)
            {
                selectedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            }

            // CRITICAL FIX: Show the skin selection UI
            ShowSkinSelectionUI();

            if (skinSelectionController != null)
            {
                skinSelectionController.gameObject.SetActive(true);
                skinSelectionController.EnterSkinSelection(characterID);
            }

            EnterSkinSelection();
        }
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");

        if (isInSkinSelection)
        {
            Debug.Log("Character Controller: Exiting skin selection via back button");

            // Simply hide the skin selection UI and exit the state
            HideSkinSelectionUI();
            ExitSkinSelection();
        }
        else if (isInCharacterSelection)
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    // Add this method to handle skin selection closure
    public void OnSkinSelectionClosed()
    {
        Debug.Log("Skin selection closed notification received");

        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        ExitSkinSelection();
    }

    // ============ SKIN SELECTION UI CONTROL ============

    private void ShowSkinSelectionUI()
    {
        Debug.Log("Showing Skin Selection UI");

        // Hide character selection UI
        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, characterSelectionCanvas.alpha, 0f, fadeDuration));
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        // Show skin selection UI
        if (skinSelectionCanvas != null)
        {
            skinSelectionCanvas.gameObject.SetActive(true);
            StartCoroutine(FadeCanvasGroup(skinSelectionCanvas, 0f, 1f, fadeDuration));
            skinSelectionCanvas.interactable = true;
            skinSelectionCanvas.blocksRaycasts = true;
        }

        // Set skin camera priority higher
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 30;
            Debug.Log("Skin camera priority set to 30");
        }
    }

    private void HideSkinSelectionUI()
    {
        Debug.Log("Hiding Skin Selection UI");

        // Hide skin selection UI
        if (skinSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(skinSelectionCanvas, skinSelectionCanvas.alpha, 0f, fadeDuration));
            skinSelectionCanvas.interactable = false;
            skinSelectionCanvas.blocksRaycasts = false;
            StartCoroutine(DeactivateAfterDelay(skinSelectionCanvas.gameObject, fadeDuration));
        }

        // Show character selection UI
        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
            Debug.Log("Skin camera priority set to 10");
        }
    }

    private IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null) obj.SetActive(false);
    }

    // ============ SKIN SELECTION STATE ============

    private void EnterSkinSelection()
    {
        isInSkinSelection = true;
        Debug.Log("Entering Skin Selection State");
    }

    private void ExitSkinSelection()
    {
        isInSkinSelection = false;
        Debug.Log("Exiting Skin Selection State from Character Controller");

        // Make sure camera priority is reset
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
            Debug.Log("Character Controller: Skin camera priority set to 10");
        }
    }

    // ============ CHARACTER SELECTION CONFIRMATION ============

    public void OnSelectCharacterConfirmed(int characterID = -1)
    {
        Debug.Log($"=== CHARACTER SELECTION CONFIRMED ===");

        int characterToSave = characterID != -1 ? characterID :
                            (pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID);

        lastSavedCharacterID = characterToSave;

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

        // Reset camera priorities
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 10;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
        }

        if (playerArmatureAnimator != null)
        {
            playerArmatureAnimator.SetBool("LookAround", false);
        }

        // Hide skin selection UI if it's active
        if (isInSkinSelection && skinSelectionCanvas != null && skinSelectionCanvas.gameObject.activeSelf)
        {
            HideSkinSelectionUI();
        }

        // Notify platform trigger to exit
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
        // Hide skin selection UI if it's active
        if (isInSkinSelection && skinSelectionCanvas != null && skinSelectionCanvas.gameObject.activeSelf)
        {
            HideSkinSelectionUI();
        }

        // Notify platform trigger to exit
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

        Debug.Log("Character selection complete");
    }

    // ============ CHARACTER PREVIEW ============

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

    // ============ CHARACTER HIGHLIGHT MANAGEMENT ============

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

    // ============ DATA MANAGEMENT ============

    private void SaveCharacterSelection(int characterID)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = characterID;

            if (selectedSkinID != -1)
            {
                GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(characterID, selectedSkinID);
            }

            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Saved to GameData: Character={characterID}, Skin={selectedSkinID}");
        }
    }

    public void UpdateSkinSelection(int skinID)
    {
        selectedSkinID = skinID;
        Debug.Log($"Skin selection updated: {skinID}");

        if (pendingCharacterSelection != -1 && GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(pendingCharacterSelection, skinID);

            if (characterVisualSwapper != null)
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(skinID);
            }
        }
    }

    // ============ RESET TO CHARACTER SELECTION ============

    private void ResetToCharacterSelection()
    {
        Debug.Log("Resetting to character selection");

        // Hide skin selection UI
        if (isInSkinSelection && skinSelectionCanvas != null)
        {
            HideSkinSelectionUI();

            if (skinSelectionController != null)
            {
                skinSelectionController.ExitSkinSelection();
                skinSelectionController.gameObject.SetActive(false);
            }
        }

        isInSkinSelection = false;
        isInCharacterSelection = true;

        Debug.Log("Reset to character selection completed");
    }

    // ============ UTILITY METHODS ============

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

    // ============ PUBLIC GETTERS ============

    public bool IsInCharacterSelection() => isInCharacterSelection;
    public bool IsInSkinSelection() => isInSkinSelection;
    public int GetPendingCharacterSelection() => pendingCharacterSelection;
    public int GetSelectedSkinID() => selectedSkinID;
    public int GetLastSavedCharacterID() => lastSavedCharacterID;
    public int GetLastSavedSkinID() => lastSavedSkinID;

    // ============ CLEANUP ============

    void OnDestroy()
    {
        if (exitCoroutine != null) StopCoroutine(exitCoroutine);
    }
}