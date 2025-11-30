using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;

public class CharacterSelectionController : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineVirtualCamera menuCamera;
    public CinemachineVirtualCamera characterSelectionCamera;

    [Header("UI References")]
    public CanvasGroup characterSelectionCanvas;
    public GameObject characterSelectionPanel;
    public GameObject mainMenuCanvasesParent;
    public Button selectCharacterButton;

    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float slideDuration = 0.3f;
    public float menuShowDelay = 1.5f;

    [Header("Panel Positions")]
    public Vector3 panelEntryPosition = new Vector3(-466.331299f, -72.6670303f, 1.23346881e-05f);
    public Vector3 panelExitPosition = new Vector3(-2000f, -72.6670303f, 1.23346881e-05f);

    [Header("Animator Reference")]
    public Animator characterSelectionAnimator;

    [Header("Character Rotation")]
    public CharacterRotationController characterRotationController;

    [Header("Player Armature Reference")]
    public GameObject playerArmature;

    private bool isInCharacterSelection = false;
    private RectTransform panelRectTransform;
    private int pendingCharacterSelection = -1;
    private Coroutine exitCoroutine;

    void Start()
    {
        // Get the panel's RectTransform
        if (characterSelectionPanel != null)
        {
            panelRectTransform = characterSelectionPanel.GetComponent<RectTransform>();
        }

        // Setup select character button
        if (selectCharacterButton != null)
        {
            selectCharacterButton.onClick.RemoveAllListeners();
            selectCharacterButton.onClick.AddListener(OnSelectCharacterConfirmed);
            selectCharacterButton.interactable = false;
        }

        // Ensure character selection is hidden at start
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 0f;
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
        }

        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition3D = panelExitPosition;
            characterSelectionPanel.SetActive(false);
        }

        // Ensure main menu canvases are visible at start
        if (mainMenuCanvasesParent != null)
        {
            mainMenuCanvasesParent.SetActive(true);
        }

        // Set initial camera priorities
        SetMenuCameraActive();

        // Ensure animator starts in correct state
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", false);
            characterSelectionAnimator.SetBool("isSelected", false);
        }
    }

    // This method should be called when clicking the PlayerArmature
    public void OnPlayerArmatureClicked()
    {
        if (!isInCharacterSelection)
        {
            EnterCharacterSelection();
        }
        else
        {
            ExitCharacterSelectionWithoutSaving();
        }
    }

    private void EnterCharacterSelection()
    {
        isInCharacterSelection = true;

        // Switch to character selection camera
        SetCharacterSelectionCameraActive();

        // Trigger changing animation on CharacterSelection animator
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", true);
            characterSelectionAnimator.SetBool("isSelected", false);
            Debug.Log("Set isChanging = true, isSelected = false on CharacterSelection animator");
        }
        else
        {
            Debug.LogWarning("CharacterSelection Animator not assigned!");
        }

        // Enable character rotation
        if (characterRotationController != null)
        {
            // Reset rotation when entering character selection
            characterRotationController.ResetRotation();
            Debug.Log("Character rotation enabled for selection");
        }

        // Hide main menu canvases
        HideMainMenuCanvases();

        // Show character selection UI with effects
        ShowCharacterSelectionUI();

        // Reset pending selection
        pendingCharacterSelection = -1;
        UpdateSelectButtonState();

        Debug.Log("Entered Character Selection Mode");
    }

    // Called when user confirms character selection with the button
    public void OnSelectCharacterConfirmed()
    {
        if (pendingCharacterSelection != -1)
        {
            // Save the character selection
            SaveCharacterSelection(pendingCharacterSelection);

            // Start the synchronized exit sequence
            if (exitCoroutine != null)
                StopCoroutine(exitCoroutine);
            exitCoroutine = StartCoroutine(SynchronizedExitSequence());
        }
        else
        {
            Debug.LogWarning("No character selected to confirm!");
        }
    }

    // Called when user wants to exit without saving (like a back button)
    public void ExitCharacterSelectionWithoutSaving()
    {
        // Exit immediately without animation synchronization
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelected", false);
            Debug.Log("Set isSelected = false - Exited without selection");
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
            Debug.Log("Set isSelected = true - Starting selection animation");
        }

        // Reset character rotation when confirming selection
        if (characterRotationController != null)
        {
            characterRotationController.OnCharacterSelected();
            Debug.Log("Character rotation reset for selection confirmation");
        }

        // Hide character selection UI immediately
        HideCharacterSelectionUI();

        // PHASE 2: Switch camera priority IMMEDIATELY with the animation
        SetMenuCameraActive();
        Debug.Log("Camera priority switched to menu camera - synchronized with selection animation");

        // PHASE 3: Wait for the selection animation to complete
        yield return new WaitForSeconds(menuShowDelay);

        // PHASE 4: Complete the exit process
        CompleteExitProcess();

        Debug.Log("Synchronized exit sequence completed");
    }

    private void ExitCharacterSelection()
    {
        // Hide character selection UI immediately
        HideCharacterSelectionUI();

        // Switch camera immediately
        SetMenuCameraActive();

        // Complete exit process
        CompleteExitProcess();
    }

    private void CompleteExitProcess()
    {
        isInCharacterSelection = false;

        // Stop changing animation on CharacterSelection animator
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isChanging", false);
            Debug.Log("Set isChanging = false on CharacterSelection animator");
        }

        // Show main menu canvases
        ShowMainMenuCanvases();

        // Reset pending selection
        pendingCharacterSelection = -1;
        UpdateSelectButtonState();

        Debug.Log("Character Selection Mode Exited");
    }

    // Called when a character button is clicked in the panel
    public void OnCharacterPreviewSelected(int characterID)
    {
        // Store the character ID as pending selection
        pendingCharacterSelection = characterID;

        // Reset character rotation when previewing new character
        if (characterRotationController != null)
        {
            characterRotationController.OnCharacterSelected();
            Debug.Log("Character rotation reset for new preview");
        }

        // Enable the select button since we have a character selected
        UpdateSelectButtonState();

        Debug.Log($"Character {characterID} selected for preview");
    }

    private void SaveCharacterSelection(int characterID)
    {
        // Save to GameDataManager
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.selectedCharacterID = characterID;
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Character ID {characterID} saved to GameDataManager");
        }

        // If MainMenu_Manager exists, sync with it
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

        Debug.Log($"Character selection confirmed: {characterID}");
    }

    private void UpdateSelectButtonState()
    {
        if (selectCharacterButton != null)
        {
            selectCharacterButton.interactable = (pendingCharacterSelection != -1);

            Text buttonText = selectCharacterButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = (pendingCharacterSelection != -1) ? "SELECT CHARACTER" : "SELECT A CHARACTER";
            }
        }
    }

    private void HideMainMenuCanvases()
    {
        if (mainMenuCanvasesParent != null)
        {
            mainMenuCanvasesParent.SetActive(false);
        }
    }

    private void ShowMainMenuCanvases()
    {
        if (mainMenuCanvasesParent != null)
        {
            mainMenuCanvasesParent.SetActive(true);
        }
    }

    private void SetMenuCameraActive()
    {
        if (menuCamera != null) menuCamera.Priority = 20;
        if (characterSelectionCamera != null) characterSelectionCamera.Priority = 0;
    }

    private void SetCharacterSelectionCameraActive()
    {
        if (characterSelectionCamera != null) characterSelectionCamera.Priority = 20;
        if (menuCamera != null) menuCamera.Priority = 0;
    }

    private void ShowCharacterSelectionUI()
    {
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
            StartCoroutine(FadeCanvas(characterSelectionCanvas, 0f, 1f, fadeDuration));
        }

        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            characterSelectionPanel.SetActive(true);
            StartCoroutine(SlidePanel(characterSelectionPanel, true));
        }
    }

    private void HideCharacterSelectionUI()
    {
        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.interactable = false;
            characterSelectionCanvas.blocksRaycasts = false;
            StartCoroutine(FadeCanvas(characterSelectionCanvas, 1f, 0f, fadeDuration));
        }

        if (characterSelectionPanel != null && panelRectTransform != null)
        {
            StartCoroutine(SlidePanel(characterSelectionPanel, false));
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

        if (endAlpha == 0f && characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(false);
        }
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

        if (!slideIn)
        {
            panel.SetActive(false);
        }
    }

    // Public method to manually reset selection state if needed
    public void ResetSelectionState()
    {
        if (characterSelectionAnimator != null)
        {
            characterSelectionAnimator.SetBool("isSelected", false);
            Debug.Log("Selection state reset: isSelected = false");
        }
    }

    public bool IsInCharacterSelection()
    {
        return isInCharacterSelection;
    }

    public int GetPendingCharacterSelection()
    {
        return pendingCharacterSelection;
    }

    void OnDestroy()
    {
        if (exitCoroutine != null)
            StopCoroutine(exitCoroutine);
    }
}