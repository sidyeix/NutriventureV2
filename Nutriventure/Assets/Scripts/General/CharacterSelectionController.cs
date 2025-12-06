using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineVirtualCamera menuCamera;
    public CinemachineVirtualCamera characterSelectionCamera;
    public CinemachineVirtualCamera skinSelectionCamera;

    [Header("UI References")]
    public CanvasGroup characterSelectionCanvas;
    public CanvasGroup characterPreviewCanvas;
    public GameObject characterSelectionPanel;
    public GameObject mainMenuCanvasesParent;
    public Button selectCharacterButton;
    public Button previewSelectButton;
    public Button skinButton;
    public Button backButton;
    public Button characterButton;

    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float slideDuration = 0.3f;
    public float menuShowDelay = 1.5f;
    public float canvasShowDelay = 1.0f;

    [Header("Panel Positions")]
    public Vector3 panelEntryPosition = new Vector3(-466.331299f, -72.6670303f, 1.23346881e-05f);
    public Vector3 panelExitPosition = new Vector3(-2000f, -72.6670303f, 1.23346881e-05f);

    [Header("Animator Reference")]
    public Animator characterSelectionAnimator;

    [Header("Character Rotation")]
    public CharacterRotationController characterRotationController;

    [Header("Player Armature Reference")]
    public GameObject playerArmature;

    [Header("UI Color Objects")]
    public List<Image> colorizableImages = new List<Image>();
    public List<Text> colorizableTexts = new List<Text>();

    [Header("Skin Selection")]
    public SkinSelectionController skinSelectionController;

    [Header("Character Visual Management")]
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;

    private bool isInCharacterSelection = false;
    private bool isInSkinSelection = false;
    private RectTransform panelRectTransform;
    private int pendingCharacterSelection = -1;
    private int selectedSkinID = -1;
    private Coroutine exitCoroutine;
    private Coroutine canvasShowCoroutine;

    // NEW: Track the last saved character and skin
    private int lastSavedCharacterID = 0;
    private int lastSavedSkinID = -1;

    void Start()
    {
        if (characterSelectionPanel != null)
        {
            panelRectTransform = characterSelectionPanel.GetComponent<RectTransform>();
        }

        // Setup buttons
        if (selectCharacterButton != null)
        {
            selectCharacterButton.onClick.RemoveAllListeners();
            selectCharacterButton.onClick.AddListener(OnFirstSelectButtonClicked);
            selectCharacterButton.interactable = true;
        }

        if (previewSelectButton != null)
        {
            previewSelectButton.onClick.RemoveAllListeners();
            previewSelectButton.onClick.AddListener(OnSecondSelectButtonClicked);
            previewSelectButton.interactable = true;
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

        // Initialize UI states
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        if (characterPreviewCanvas != null)
        {
            characterPreviewCanvas.alpha = 0f;
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
        }

        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition3D = panelExitPosition;
            characterSelectionPanel.SetActive(false);
        }

        if (mainMenuCanvasesParent != null)
        {
            mainMenuCanvasesParent.SetActive(true);
        }

        SetMenuCameraActive();

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", false);
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        // NEW: Load last saved character and skin
        if (GameDataManager.Instance != null)
        {
            lastSavedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
            lastSavedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(lastSavedCharacterID);
            Debug.Log($"Loaded saved character: {lastSavedCharacterID}, skin: {lastSavedSkinID}");
        }
    }

    public void OnFirstSelectButtonClicked()
    {
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnSecondSelectButtonClicked()
    {
        int characterToSelect = pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID;
        OnSelectCharacterConfirmed(characterToSelect);
    }

    public void OnCharacterButtonClicked()
    {
        Debug.Log("Character button clicked - returning to character selection");

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        ShowCharacterSelectionPanel();
        ResetToCharacterSelection();
    }

    private void ShowCharacterSelectionPanel()
    {
        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            characterSelectionPanel.SetActive(true);
            StartCoroutine(SlidePanel(characterSelectionPanel, true));
        }
    }

    private void HideCharacterSelectionPanel()
    {
        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            StartCoroutine(SlidePanel(characterSelectionPanel, false));
        }
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

            HideCharacterSelectionPanel();
            EnterSkinSelection();
        }
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        if (isInSkinSelection)
        {
            // IMPORTANT: DO NOT APPLY OR SAVE ANY SKINS HERE
            // Skin was already applied when skin button was clicked
            Debug.Log("Exiting skin selection - NO skin changes made");

            // Exit skin selection
            if (skinSelectionController != null)
            {
                skinSelectionController.ExitSkinSelection();
                skinSelectionController.gameObject.SetActive(false);
            }

            ShowCharacterSelectionPanel();
            ExitSkinSelection();
        }
        else if (isInCharacterSelection)
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    public void OnPlayerArmatureClicked()
    {
        if (!isInCharacterSelection && !isInSkinSelection)
        {
            EnterCharacterSelection();
        }
        else if (isInCharacterSelection && !isInSkinSelection)
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    private void EnterSkinSelection()
    {
        isInSkinSelection = true;

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelectingSkin", true);
            characterSelectionAnimator.SetBool("isChanging", false);
            characterSelectionAnimator.SetBool("isSelected", false);
        }

        SetSkinSelectionCameraActive();

        if (characterSelectionCanvas != null)
        {
            StartCoroutine(FadeCanvas(characterSelectionCanvas, characterSelectionCanvas.alpha, 0f, fadeDuration));
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        ShowCharacterPreviewCanvasWithDelay();
    }

    private void ExitSkinSelection()
    {
        isInSkinSelection = false;

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelected", false);
        }

        SetCharacterSelectionCameraActive();

        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvas(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
        }

        ShowCharacterSelectionCanvasWithDelay();
    }

    private void EnterCharacterSelection()
    {
        isInCharacterSelection = true;

        SetCharacterSelectionCameraActive();

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
            Debug.Log("Set isChanging = true, isSelected = false, isSelectingSkin = false on CharacterSelection animator");
        }

        if (characterRotationController != null)
        {
            // Reset rotation when entering character selection
            characterRotationController.ResetRotation();
            Debug.Log("Character rotation enabled for selection");
        }

        HideMainMenuCanvases();
        ShowCharacterSelectionUI();

        pendingCharacterSelection = -1;
        // DO NOT reset selectedSkinID here - preserve current skin

        // CHANGED: Do NOT load any character when entering character selection
        // The character should stay exactly as it was
        Debug.Log("Entered Character Selection Mode - Character remains unchanged");

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;

        Debug.Log("Entered Character Selection Mode");
    }

    // CHANGED: Modified to accept characterID parameter
    public void OnSelectCharacterConfirmed(int characterID = -1)
    {
        int characterToSave = characterID != -1 ? characterID : (pendingCharacterSelection != -1 ? pendingCharacterSelection : lastSavedCharacterID);

        // Update last saved character
        lastSavedCharacterID = characterToSave;

        // Save the character selection
        SaveCharacterSelection(characterToSave);

        // CHANGED: Do NOT reload the character - it's already showing from the preview
        // The character visuals are already applied, we just need to save the selection
        Debug.Log($"Character {characterToSave} confirmed - No reload needed, character already displayed");

        // Start the synchronized exit sequence
        if (exitCoroutine != null)
            StopCoroutine(exitCoroutine);
        exitCoroutine = StartCoroutine(SynchronizedExitSequence());
    }

    public void ExitCharacterSelectionWithoutSaving()
    {
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        ExitCharacterSelection();
    }

    private IEnumerator SynchronizedExitSequence()
    {
        Debug.Log("Starting synchronized exit sequence...");

        // PHASE 1: Trigger selection animation and hide UI immediately
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelected", true);
            characterSelectionAnimator.SetBool("isChanging", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
            Debug.Log("Set isSelected = true, isChanging = false, isSelectingSkin = false - Starting selection animation");
        }

        // Reset character rotation when confirming selection
        if (characterRotationController != null)
        {
            characterRotationController.OnCharacterSelected();
            Debug.Log("Character rotation reset for selection confirmation");
        }

        // Hide character selection UI immediately (including panel)
        HideCharacterSelectionUI();

        // Hide character preview canvas immediately
        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvas(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
        }

        // Switch to menu camera immediately
        SetMenuCameraActive();

        // PHASE 2: Wait for the selection animation to complete
        yield return new WaitForSeconds(menuShowDelay);

        // PHASE 3: Complete the exit process
        CompleteExitProcess();

        Debug.Log("Synchronized exit sequence completed");
    }

    private void ExitCharacterSelection()
    {
        HideCharacterSelectionUI();
        HideCharacterPreviewUI();

        if (skinSelectionController != null)
        {
            skinSelectionController.gameObject.SetActive(false);
        }

        SetMenuCameraActive();
        CompleteExitProcess();
    }

    private void CompleteExitProcess()
    {
        isInCharacterSelection = false;
        isInSkinSelection = false;

        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", false);
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        ShowMainMenuCanvases();

        pendingCharacterSelection = -1;
        // DO NOT reset selectedSkinID here - preserve current skin

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;
    }

    public void OnCharacterPreviewSelected(int characterID)
    {
        pendingCharacterSelection = characterID;

        if (characterRotationController != null)
        {
            characterRotationController.OnCharacterSelected();
        }

        // Apply character preview visuals WITH SAVED SKIN
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(characterID);
        }

        Debug.Log($"Character {characterID} selected for preview");
    }

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

        MainMenu_Manager mainMenuManager = FindObjectOfType<MainMenu_Manager>();
        if (mainMenuManager != null)
        {
            var field = typeof(MainMenu_Manager).GetField("currentSelectedCharacterID",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(mainMenuManager, characterID);
            }
        }
    }

    // In CharacterSelectionController.cs, update the UpdateSkinSelection method:
    public void UpdateSkinSelection(int skinID)
    {
        // This method is called by SkinSelectionController when a skin button is clicked
        // We just update the selectedSkinID variable
        selectedSkinID = skinID;

        // The skin is already applied by SkinSelectionController, we don't need to do anything here
        Debug.Log($"Skin selection updated in CharacterSelectionController: {skinID}");

        // Save to GameData if we have a character selected
        if (pendingCharacterSelection != -1 && GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(pendingCharacterSelection, skinID);
            Debug.Log($"Skin {skinID} saved for character {pendingCharacterSelection}");
        }
    }

    // Helper methods
    private void ShowCharacterSelectionCanvasWithDelay()
    {
        if (canvasShowCoroutine != null) StopCoroutine(canvasShowCoroutine);
        canvasShowCoroutine = StartCoroutine(ShowCharacterSelectionCanvasDelayed());
    }

    private void ShowCharacterPreviewCanvasWithDelay()
    {
        if (canvasShowCoroutine != null) StopCoroutine(canvasShowCoroutine);
        canvasShowCoroutine = StartCoroutine(ShowCharacterPreviewCanvasDelayed());
    }

    private IEnumerator ShowCharacterSelectionCanvasDelayed()
    {
        yield return new WaitForSeconds(canvasShowDelay);
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            StartCoroutine(FadeCanvas(characterSelectionCanvas, 0f, 1f, fadeDuration));
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }
    }

    private IEnumerator ShowCharacterPreviewCanvasDelayed()
    {
        yield return new WaitForSeconds(canvasShowDelay);
        if (characterPreviewCanvas != null)
        {
            characterPreviewCanvas.alpha = 0f;
            StartCoroutine(FadeCanvas(characterPreviewCanvas, 0f, 1f, fadeDuration));
            characterPreviewCanvas.interactable = true;
            characterPreviewCanvas.blocksRaycasts = true;
        }
    }

    private void ResetToCharacterSelection()
    {
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelected", false);
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelectingSkin", false);
        }

        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvas(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
        }

        if (skinSelectionController != null && skinSelectionController.IsInSkinPreview())
        {
            skinSelectionController.ExitSkinSelection();
            skinSelectionController.gameObject.SetActive(false);
        }

        ShowCharacterSelectionCanvasWithDelay();
        SetCharacterSelectionCameraActive();

        isInSkinSelection = false;
        isInCharacterSelection = true;
        pendingCharacterSelection = -1;
        // DO NOT reset selectedSkinID here - preserve current skin

        if (selectCharacterButton != null) selectCharacterButton.interactable = true;
        if (previewSelectButton != null) previewSelectButton.interactable = true;

        Debug.Log($"Reset to character selection - preserving character: {lastSavedCharacterID}, skin: {selectedSkinID}");
    }

    private void HideMainMenuCanvases()
    {
        if (mainMenuCanvasesParent != null) mainMenuCanvasesParent.SetActive(false);
    }

    private void ShowMainMenuCanvases()
    {
        if (mainMenuCanvasesParent != null) mainMenuCanvasesParent.SetActive(true);
    }

    private void SetMenuCameraActive()
    {
        if (menuCamera != null) menuCamera.Priority = 20;
        if (characterSelectionCamera != null) characterSelectionCamera.Priority = 0;
        if (skinSelectionCamera != null) skinSelectionCamera.Priority = 0;
    }

    private void SetCharacterSelectionCameraActive()
    {
        if (characterSelectionCamera != null) characterSelectionCamera.Priority = 20;
        if (menuCamera != null) menuCamera.Priority = 0;
        if (skinSelectionCamera != null) skinSelectionCamera.Priority = 0;
    }

    private void SetSkinSelectionCameraActive()
    {
        if (skinSelectionCamera != null) skinSelectionCamera.Priority = 20;
        if (characterSelectionCamera != null) characterSelectionCamera.Priority = 10;
        if (menuCamera != null) menuCamera.Priority = 0;
    }

    private void ShowCharacterSelectionUI()
    {
        ShowCharacterSelectionCanvasWithDelay();
        ShowCharacterSelectionPanel();
    }

    private void HideCharacterSelectionUI()
    {
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
            StartCoroutine(FadeCanvas(characterSelectionCanvas, 1f, 0f, fadeDuration));
        }
        HideCharacterSelectionPanel();
    }

    private void HideCharacterPreviewUI()
    {
        if (characterPreviewCanvas != null)
        {
            StartCoroutine(FadeCanvas(characterPreviewCanvas, characterPreviewCanvas.alpha, 0f, fadeDuration));
            characterPreviewCanvas.interactable = false;
            characterPreviewCanvas.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = startAlpha;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            canvasGroup.alpha = currentAlpha;
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    private IEnumerator SlidePanel(GameObject panel, bool slideIn)
    {
        if (panelRectTransform == null) yield break;
        Vector3 startPosition = slideIn ? panelExitPosition : panelRectTransform.anchoredPosition3D;
        Vector3 targetPosition = slideIn ? panelEntryPosition : panelExitPosition;
        panelRectTransform.anchoredPosition3D = startPosition;
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / slideDuration);
            panelRectTransform.anchoredPosition3D = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        panelRectTransform.anchoredPosition3D = targetPosition;
        if (!slideIn) panel.SetActive(false);
        else panel.SetActive(true);
    }

    public bool IsInCharacterSelection() => isInCharacterSelection;
    public bool IsInSkinSelection() => isInSkinSelection;
    public int GetPendingCharacterSelection() => pendingCharacterSelection;
    public int GetSelectedSkinID() => selectedSkinID;

    void OnDestroy()
    {
        if (exitCoroutine != null) StopCoroutine(exitCoroutine);
        if (canvasShowCoroutine != null) StopCoroutine(canvasShowCoroutine);
    }
}