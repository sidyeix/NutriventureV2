using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineVirtualCamera characterChangeCamera; // Character change camera

    [Header("UI References")]
    public CanvasGroup characterSelectionCanvas;
    public CanvasGroup characterPreviewCanvas;
    public Button selectCharacterButton;
    public Button previewSelectButton;
    public Button skinButton;
    public Button backButton;
    public Button characterButton;

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
    public Animator playerArmatureAnimator; // To control LookAround animation

    private bool isInCharacterSelection = false;
    private bool isInSkinSelection = false;
    private int pendingCharacterSelection = -1;
    private int selectedSkinID = -1;
    private Coroutine exitCoroutine;

    // Track the last saved character and skin
    private int lastSavedCharacterID = 0;
    private int lastSavedSkinID = -1;

    // UI element lists for fade effects
    private List<CanvasGroup> uiElements = new List<CanvasGroup>();

    void Start()
    {
        InitializeUIElements();
        SetupButtonListeners();
        InitializeUIStates();

        // Initialize skin selection
        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        // Load last saved character and skin
        if (GameDataManager.Instance != null)
        {
            lastSavedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
            lastSavedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(lastSavedCharacterID);
            Debug.Log($"Loaded saved character: {lastSavedCharacterID}, skin: {lastSavedSkinID}");
        }

        // Initialize with the saved character as pending selection
        pendingCharacterSelection = lastSavedCharacterID;
    }

    void InitializeUIElements()
    {
        // Add main canvas group
        if (characterSelectionCanvas != null && !uiElements.Contains(characterSelectionCanvas))
        {
            uiElements.Add(characterSelectionCanvas);
        }

        // Add character preview canvas
        if (characterPreviewCanvas != null && !uiElements.Contains(characterPreviewCanvas))
        {
            uiElements.Add(characterPreviewCanvas);
        }

        // Find all canvas groups in children
        if (characterSelectionCanvas != null)
        {
            CanvasGroup[] childGroups = characterSelectionCanvas.GetComponentsInChildren<CanvasGroup>(true);
            foreach (var group in childGroups)
            {
                if (!uiElements.Contains(group) && group.gameObject != characterSelectionCanvas.gameObject)
                {
                    uiElements.Add(group);
                }
            }
        }
    }

    void SetupButtonListeners()
    {
        // First select button
        if (selectCharacterButton != null)
        {
            selectCharacterButton.onClick.RemoveAllListeners();
            selectCharacterButton.onClick.AddListener(OnFirstSelectButtonClicked);
            selectCharacterButton.interactable = true;
        }

        // Preview select button
        if (previewSelectButton != null)
        {
            previewSelectButton.onClick.RemoveAllListeners();
            previewSelectButton.onClick.AddListener(OnSecondSelectButtonClicked);
            previewSelectButton.interactable = true;
        }

        // Skin button
        if (skinButton != null)
        {
            skinButton.onClick.RemoveAllListeners();
            skinButton.onClick.AddListener(OnSkinButtonClicked);
        }

        // Back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Character button
        if (characterButton != null)
        {
            characterButton.onClick.RemoveAllListeners();
            characterButton.onClick.AddListener(OnCharacterButtonClicked);
        }
    }

    void InitializeUIStates()
    {
        // Hide all UI with alpha 0
        SetAllUIAlpha(0f);

        // Set canvas states
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.gameObject.SetActive(false);
        }

        if (characterPreviewCanvas != null)
        {
            characterPreviewCanvas.gameObject.SetActive(false);
        }
    }

    void SetAllUIAlpha(float alpha)
    {
        foreach (var element in uiElements)
        {
            if (element != null)
                element.alpha = alpha;
        }
    }

    // ============ PUBLIC METHODS FOR PLATFORM TRIGGER ============

    public void ActivateCharacterSelection()
    {
        if (isInCharacterSelection) return;
        EnterCharacterSelection();
    }

    // ============ CHARACTER SELECTION FLOW ============

    private void EnterCharacterSelection()
    {
        isInCharacterSelection = true;

        // Show character selection canvas
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.gameObject.SetActive(true);
        }

        // Start fade in animation
        StartCoroutine(FadeInUI());

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        // IMPORTANT: Set pending selection to the last saved character if not already set
        if (pendingCharacterSelection == -1)
        {
            pendingCharacterSelection = lastSavedCharacterID;
        }

        // Load current character
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(lastSavedCharacterID);
        }

        // ENABLE THE BUTTONS - FIX
        if (selectCharacterButton != null)
        {
            selectCharacterButton.interactable = true;
            Debug.Log("Select button enabled on enter");
        }

        if (previewSelectButton != null)
        {
            previewSelectButton.interactable = true;
            Debug.Log("Preview select button enabled on enter");
        }

        Debug.Log("Entered Character Selection Mode");
    }

    private IEnumerator FadeInUI()
    {
        // Fade in character selection canvas
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }

        // Staggered fade for other UI elements
        foreach (var element in uiElements)
        {
            if (element != null && element != characterSelectionCanvas)
            {
                element.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(element, 0f, 1f, fadeDuration * 0.7f));
                yield return new WaitForSeconds(elementFadeDelay);
            }
        }
    }

    // ============ BUTTON HANDLERS ============

    public void OnFirstSelectButtonClicked()
    {
        Debug.Log("First select button clicked");
        Debug.Log($"Pending character selection: {pendingCharacterSelection}");
        Debug.Log($"Last saved character ID: {lastSavedCharacterID}");

        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        Debug.Log($"Confirming character: {characterToSelect}");
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnSecondSelectButtonClicked()
    {
        Debug.Log("Second select button clicked");
        Debug.Log($"Pending character selection: {pendingCharacterSelection}");
        Debug.Log($"Last saved character ID: {lastSavedCharacterID}");

        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        Debug.Log($"Confirming character: {characterToSelect}");
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnCharacterButtonClicked()
    {
        Debug.Log("Character button clicked - returning to character selection");

        if (skinSelectionController != null && skinSelectionController.skinTimelineBridge != null)
        {
            skinSelectionController.skinTimelineBridge.StopTimelineAndReturn();
        }

        ResetToCharacterSelection();
    }

    public void OnSkinButtonClicked()
    {
        if (isInCharacterSelection && !isInSkinSelection)
        {
            int characterID = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;

            // Load saved skin for this character
            if (GameDataManager.Instance != null)
            {
                selectedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            }

            // Enter skin selection
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

        if (skinSelectionController != null && skinSelectionController.skinTimelineBridge != null)
        {
            skinSelectionController.skinTimelineBridge.StopTimelineAndReturn();
        }

        if (isInSkinSelection)
        {
            Debug.Log("Exiting skin selection - NO skin changes made");

            // Exit skin selection
            if (skinSelectionController != null)
            {
                skinSelectionController.ExitSkinSelection();
                skinSelectionController.gameObject.SetActive(false);
            }

            ExitSkinSelection();
        }
        else if (isInCharacterSelection)
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    // ============ SKIN SELECTION ============

    private void EnterSkinSelection()
    {
        isInSkinSelection = true;

        // Hide character selection UI
        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, characterSelectionCanvas.alpha, 0f, fadeDuration));
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        // Show character preview canvas
        if (characterPreviewCanvas != null)
        {
            characterPreviewCanvas.gameObject.SetActive(true);
            characterPreviewCanvas.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(characterPreviewCanvas, 0f, 1f, fadeDuration));
            characterPreviewCanvas.interactable = true;
            characterPreviewCanvas.blocksRaycasts = true;
        }
    }

    private void ExitSkinSelection()
    {
        isInSkinSelection = false;

        // Hide character preview canvas
        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
            characterPreviewCanvas.gameObject.SetActive(false);
        }

        // Show character selection canvas
        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }
    }

    // ============ CHARACTER SELECTION CONFIRMATION ============

    public void OnSelectCharacterConfirmed(int characterID = -1)
    {
        Debug.Log($"=== OnSelectCharacterConfirmed called ===");
        Debug.Log($"Character ID parameter: {characterID}");
        Debug.Log($"Pending character selection: {pendingCharacterSelection}");
        Debug.Log($"Last saved character ID: {lastSavedCharacterID}");

        if (skinSelectionController != null && skinSelectionController.skinTimelineBridge != null)
        {
            skinSelectionController.skinTimelineBridge.StopTimelineAndReturn();
        }

        // Determine which character to save
        int characterToSave;

        if (characterID != -1)
        {
            // Use the parameter if provided
            characterToSave = characterID;
            Debug.Log($"Using parameter character ID: {characterToSave}");
        }
        else if (pendingCharacterSelection != -1)
        {
            // Use pending selection if set
            characterToSave = pendingCharacterSelection;
            Debug.Log($"Using pending character selection: {characterToSave}");
        }
        else
        {
            // Fall back to last saved character
            characterToSave = lastSavedCharacterID;
            Debug.Log($"Using last saved character ID: {characterToSave}");
        }

        // Update last saved character
        lastSavedCharacterID = characterToSave;

        // Save the character selection
        SaveCharacterSelection(characterToSave);

        Debug.Log($"Character {characterToSave} confirmed");

        // Start the exit sequence
        if (exitCoroutine != null)
            StopCoroutine(exitCoroutine);
        exitCoroutine = StartCoroutine(SimpleExitSequence());
    }

    public void ExitCharacterSelectionWithoutSaving()
    {
        StartCoroutine(ExitCharacterSelectionRoutine());
    }

    private IEnumerator SimpleExitSequence()
    {
        Debug.Log("Starting exit sequence...");

        // Reset character rotation
        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        // Fade out all UI
        yield return StartCoroutine(FadeOutUI());

        // 1. Set character change camera priority to 10 (lower priority)
        if (characterChangeCamera != null)
        {
            characterChangeCamera.Priority = 10;
            Debug.Log("Set character change camera priority to 10");
        }

        // 2. Stop LookAround animation
        if (playerArmatureAnimator != null)
        {
            playerArmatureAnimator.SetBool("LookAround", false);
            Debug.Log("Stopped LookAround animation");
        }

        // 3. Notify platform trigger to exit (will enable StarterAssetsInputs Canvas)
        if (platformTrigger != null)
        {
            StartCoroutine(platformTrigger.ExitCharacterSelection());
        }

        // Complete the exit process
        CompleteExitProcess();

        Debug.Log("Exit sequence completed");
    }

    private IEnumerator ExitCharacterSelectionRoutine()
    {
        // Fade out UI
        yield return StartCoroutine(FadeOutUI());

        // Hide character preview UI
        if (characterPreviewCanvas != null)
        {
            characterPreviewCanvas.gameObject.SetActive(false);
        }

        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        CompleteExitProcess();
    }

    private IEnumerator FadeOutUI()
    {
        // Fade out UI elements in reverse order
        for (int i = uiElements.Count - 1; i >= 0; i--)
        {
            if (uiElements[i] != null && uiElements[i] != characterSelectionCanvas)
            {
                StartCoroutine(FadeCanvasGroup(uiElements[i], uiElements[i].alpha, 0f, fadeDuration * 0.5f));
            }
            yield return new WaitForSeconds(elementFadeDelay * 0.5f);
        }

        // Fade out main canvas
        if (characterSelectionCanvas != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 1f, 0f, fadeDuration));
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }
    }

    private void CompleteExitProcess()
    {
        isInCharacterSelection = false;
        isInSkinSelection = false;
        // Don't reset pendingCharacterSelection here - keep it for next time
        // pendingCharacterSelection = -1; // REMOVED THIS LINE

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;

        Debug.Log("Character selection complete");
    }

    // ============ CHARACTER PREVIEW ============

    public void OnCharacterPreviewSelected(int characterID)
    {
        // CRITICAL FIX: Set the pendingCharacterSelection when a character is previewed
        pendingCharacterSelection = characterID;
        Debug.Log($"Character {characterID} selected for preview. Pending selection updated.");

        if (characterRotationController != null)
        {
            characterRotationController.ResetRotation();
        }

        // Apply character preview visuals WITH SAVED SKIN
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(characterID);
        }

        Debug.Log($"Character {characterID} selected for preview");
    }

    // ============ DATA MANAGEMENT ============

    private void SaveCharacterSelection(int characterID)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = characterID;

            // Save the skin if we have one selected
            if (selectedSkinID != -1)
            {
                GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(characterID, selectedSkinID);
            }

            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Saved character: {characterID}, skin: {selectedSkinID}");
        }
    }

    public void UpdateSkinSelection(int skinID)
    {
        selectedSkinID = skinID;
        Debug.Log($"Skin selection updated in CharacterSelectionController: {skinID}");

        // Save to GameData if we have a character selected
        if (pendingCharacterSelection != -1 && GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(pendingCharacterSelection, skinID);
            Debug.Log($"Skin {skinID} saved for character {pendingCharacterSelection}");
        }
    }

    // ============ RESET TO CHARACTER SELECTION ============

    private void ResetToCharacterSelection()
    {
        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvasGroup(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
            characterPreviewCanvas.gameObject.SetActive(false);
        }

        if (skinSelectionController != null)
        {
            skinSelectionController.ExitSkinSelection();
            skinSelectionController.gameObject.SetActive(false);
        }

        // Show character selection canvas
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            StartCoroutine(FadeCanvasGroup(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }

        isInSkinSelection = false;
        isInCharacterSelection = true;
        // Don't reset pendingCharacterSelection here - keep the current selection
        // pendingCharacterSelection = -1; // REMOVED THIS LINE

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;

        Debug.Log($"Reset to character selection");
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

    // ============ CLEANUP ============

    void OnDestroy()
    {
        if (exitCoroutine != null) StopCoroutine(exitCoroutine);
    }
}