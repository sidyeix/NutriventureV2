using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CharacterButtonData : MonoBehaviour
{
    public int characterIndex;
    public int characterID;
    public Image lockIcon;
    public Image selectedHighlight;
    public Image characterIcon;
    public GameObject lockedOverlay;
    public bool isEquipped = false;
}

public class CharacterSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterSelectionPanel;
    public Transform characterButtonContainer;
    public GameObject characterButtonPrefab;
    public ScrollRect scrollRect;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterTaglineText;
    public Image characterLogoImage;

    [Header("Locked Character Feedback")]
    public CanvasGroup lockedFeedbackCanvasGroup;
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 2f;
    public TMPro.TextMeshProUGUI lockedMessageText;

    [Header("Layout Settings")]
    public int maxColumns = 3;
    public float buttonSpacing = 20f;
    public Vector2 buttonSize = new Vector2(350f, 450f);

    [Header("Character Icon Colors")]
    public Color selectedIconColor = Color.white;
    public Color deselectedIconColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color lockedIconColor = new Color(0.192f, 0.192f, 0.192f, 1f);

    [Header("Character System References")]
    public CharacterDatabase characterDatabase;
    public CharacterVisualSwapper characterVisualSwapper;
    public InputManager inputManager;
    public CharacterSelectionController characterSelectionController;

    private List<GameObject> characterButtons = new List<GameObject>();
    private int currentSelectedCharacterID = -1;
    private Coroutine lockedFeedbackCoroutine;
    private bool isShowingLockedFeedback = false;
    private int equippedCharacterID = -1;
    private GameDataManager gameDataManager;

    void Start()
    {
        gameDataManager = GameDataManager.Instance;

        if (characterSelectionController == null)
        {
            characterSelectionController = FindObjectOfType<CharacterSelectionController>();
        }

        // Get equipped character from GameData
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            equippedCharacterID = gameDataManager.CurrentGameData.selectedCharacterID;
            currentSelectedCharacterID = equippedCharacterID;
        }

        // Initialize locked feedback
        if (lockedFeedbackCanvasGroup != null)
        {
            lockedFeedbackCanvasGroup.alpha = 0f;
            lockedFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        InitializeCharacterPanel();
        UpdateAllButtonHighlights();
    }

    public void InitializeCharacterPanel()
    {
        // Clear existing buttons
        foreach (var button in characterButtons)
        {
            if (button != null) Destroy(button);
        }
        characterButtons.Clear();

        // Create buttons for each character
        for (int i = 0; i < characterDatabase.characters.Count; i++)
        {
            CreateCharacterButton(i);
        }

        SetupGridLayout();
        UpdateAllButtonAppearances();

        // Select the equipped character initially
        if (equippedCharacterID != -1)
        {
            SelectCharacterByID(equippedCharacterID);
        }
    }

    private void CreateCharacterButton(int characterIndex)
    {
        if (characterButtonPrefab == null || characterButtonContainer == null)
        {
            Debug.LogError("Character button prefab or container not assigned!");
            return;
        }

        GameObject buttonGO = Instantiate(characterButtonPrefab, characterButtonContainer);
        characterButtons.Add(buttonGO);

        CharacterDatabase.CharacterData characterData = characterDatabase.characters[characterIndex];

        Button button = buttonGO.GetComponent<Button>();
        CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
        if (buttonData == null)
            buttonData = buttonGO.AddComponent<CharacterButtonData>();

        buttonData.characterIndex = characterIndex;
        buttonData.characterID = characterData.characterID;

        // Check if this character is equipped
        buttonData.isEquipped = (characterData.characterID == equippedCharacterID);

        SetupButtonUIElements(buttonGO, characterData, buttonData);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCharacterButtonClicked(characterIndex));

        UpdateButtonAppearance(buttonGO);
    }

    private void SetupButtonUIElements(GameObject buttonGO, CharacterDatabase.CharacterData characterData, CharacterButtonData buttonData)
    {
        // Find CharacterIcon
        Transform characterIconTransform = FindDeepChild(buttonGO.transform, "CharacterIcon");
        if (characterIconTransform != null)
        {
            Image characterIcon = characterIconTransform.GetComponent<Image>();
            if (characterIcon != null)
            {
                buttonData.characterIcon = characterIcon;
                if (characterData.characterIcon != null)
                {
                    characterIcon.sprite = characterData.characterIcon;
                    characterIcon.preserveAspect = true;
                    characterIcon.gameObject.SetActive(true);
                }
                else
                {
                    characterIcon.gameObject.SetActive(false);
                }
            }
        }

        // Find LockIcon
        Transform lockIconTransform = FindDeepChild(buttonGO.transform, "LockIcon");
        if (lockIconTransform != null)
        {
            Image lockIcon = lockIconTransform.GetComponent<Image>();
            if (lockIcon != null)
            {
                buttonData.lockIcon = lockIcon;
            }
        }

        // Find SelectedHighlight
        Transform selectedHighlightTransform = FindDeepChild(buttonGO.transform, "SelectedHighlight");
        if (selectedHighlightTransform != null)
        {
            Image selectedHighlight = selectedHighlightTransform.GetComponent<Image>();
            if (selectedHighlight != null)
            {
                buttonData.selectedHighlight = selectedHighlight;
                selectedHighlight.enabled = false;
            }
        }

        // Find LockedOverlay
        Transform lockedOverlayTransform = FindDeepChild(buttonGO.transform, "LockedOverlay");
        if (lockedOverlayTransform != null)
        {
            buttonData.lockedOverlay = lockedOverlayTransform.gameObject;
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }

    private void OnCharacterButtonClicked(int characterIndex)
    {
        // Play button click sound
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }

        CharacterDatabase.CharacterData selectedCharacter = characterDatabase.characters[characterIndex];
        int characterID = selectedCharacter.characterID;

        Debug.Log($"=== Character Button Clicked ===");
        Debug.Log($"Character: {selectedCharacter.characterName} (ID: {characterID})");

        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
        {
            Debug.LogError("GameDataManager or GameData is null!");
            return;
        }

        GameData gameData = gameDataManager.CurrentGameData;

        // Check unlock status using GameData
        bool isUnlockedByDefault = selectedCharacter.unlockedByDefault;
        bool isUnlockedInGameData = gameData.unlockedCharacterIDs.Contains(characterID);
        bool isUnlocked = isUnlockedByDefault || isUnlockedInGameData;

        Debug.Log($"Character unlocked: {isUnlocked} (Default: {isUnlockedByDefault}, InGameData: {isUnlockedInGameData})");

        if (!isUnlocked)
        {
            Debug.Log($"Character {selectedCharacter.characterName} is locked!");
            ShowLockedCharacterFeedback(selectedCharacter);
            return;
        }

        // Don't do anything if same character is selected
        if (characterID == currentSelectedCharacterID)
        {
            Debug.Log("Same character selected, ignoring");
            return;
        }

        // Update UI
        UpdateCharacterInfoDisplay(selectedCharacter);
        currentSelectedCharacterID = characterID;

        // Apply character visuals with saved skin
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.LoadCharacterWithSavedSkin(characterID);
        }

        // Reset rotation
        if (characterSelectionController != null && characterSelectionController.characterRotationController != null)
        {
            characterSelectionController.characterRotationController.OnCharacterSelected();
        }

        // Play selection sound
        if (selectedCharacter.selectionSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(selectedCharacter.selectionSound);
        }

        // Update highlights
        UpdateAllButtonHighlights();

        // Notify character selection controller
        if (characterSelectionController != null)
        {
            characterSelectionController.OnCharacterPreviewSelected(characterID);
        }

        // Ensure input stays disabled
        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }

        Debug.Log($"Character preview changed to: {selectedCharacter.characterName} (ID: {characterID})");
    }

    public void UpdateCharacterInfoDisplay(CharacterDatabase.CharacterData characterData)
    {
        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }

        if (characterTaglineText != null)
        {
            characterTaglineText.text = characterData.characterTagline;
        }

        if (characterLogoImage != null)
        {
            if (characterData.characterLogo != null)
            {
                characterLogoImage.sprite = characterData.characterLogo;
                characterLogoImage.gameObject.SetActive(true);
                characterLogoImage.preserveAspect = true;
            }
            else
            {
                characterLogoImage.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateAllButtonHighlights()
    {
        foreach (GameObject buttonGO in characterButtons)
        {
            if (buttonGO != null)
            {
                UpdateButtonHighlight(buttonGO);
            }
        }
    }

    private void UpdateButtonHighlight(GameObject buttonGO)
    {
        CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
        if (buttonData == null || buttonData.selectedHighlight == null) return;

        // Check if this character is equipped
        bool isEquipped = (buttonData.characterID == equippedCharacterID);

        // Check if this character is currently selected
        bool isSelected = (buttonData.characterID == currentSelectedCharacterID);

        // Enable highlight if equipped OR selected
        bool shouldHighlight = isEquipped || isSelected;

        buttonData.selectedHighlight.enabled = shouldHighlight;
        buttonData.isEquipped = isEquipped;

        // Update icon color based on selection state
        if (buttonData.characterIcon != null)
        {
            buttonData.characterIcon.color = shouldHighlight ?
                new Color(1f, 1f, 1f, 1f) : // White when selected
                new Color(0.588f, 0.588f, 0.588f, 1f); // Gray when not selected
        }
    }

    public void SetCharacterEquipped(int characterID, bool equipped)
    {
        equippedCharacterID = equipped ? characterID : -1;
        UpdateAllButtonHighlights();
    }

    public void ShowLockedCharacterFeedback(CharacterDatabase.CharacterData lockedCharacter)
    {
        if (lockedFeedbackCoroutine != null)
        {
            StopCoroutine(lockedFeedbackCoroutine);
        }

        lockedFeedbackCoroutine = StartCoroutine(ShowLockedFeedbackCoroutine(lockedCharacter));
    }

    private IEnumerator ShowLockedFeedbackCoroutine(CharacterDatabase.CharacterData lockedCharacter)
    {
        isShowingLockedFeedback = true;

        if (lockedMessageText != null)
        {
            lockedMessageText.text = $"{lockedCharacter.characterName} is locked!\nPurchase it from Sir Fuego's Wagon";
        }

        if (lockedFeedbackCanvasGroup != null)
        {
            lockedFeedbackCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(lockedFeedbackCanvasGroup, 0f, 1f, fadeInDuration));
        }

        yield return new WaitForSeconds(displayDuration);

        if (lockedFeedbackCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(lockedFeedbackCanvasGroup, 1f, 0f, fadeOutDuration));
            lockedFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        isShowingLockedFeedback = false;
        lockedFeedbackCoroutine = null;
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

    private void SetupGridLayout()
    {
        GridLayoutGroup gridLayout = characterButtonContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = characterButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = maxColumns;
        gridLayout.cellSize = buttonSize;
        gridLayout.spacing = new Vector2(buttonSpacing, buttonSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter sizeFitter = characterButtonContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = characterButtonContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 25f;
        }
    }

    private void UpdateAllButtonAppearances()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        foreach (GameObject buttonGO in characterButtons)
        {
            if (buttonGO != null)
            {
                UpdateButtonAppearance(buttonGO);
            }
        }
    }

    private void UpdateButtonAppearance(GameObject buttonGO)
    {
        CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
        if (buttonData == null) return;

        CharacterDatabase.CharacterData characterData = characterDatabase.characters[buttonData.characterIndex];

        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        GameData gameData = gameDataManager.CurrentGameData;

        bool isUnlockedByDefault = characterData.unlockedByDefault;
        bool isUnlockedInGameData = gameData.unlockedCharacterIDs.Contains(characterData.characterID);
        bool finalIsUnlocked = isUnlockedByDefault || isUnlockedInGameData;

        // Set lock icon and overlay
        if (buttonData.lockIcon != null)
        {
            buttonData.lockIcon.gameObject.SetActive(!finalIsUnlocked);
        }

        if (buttonData.lockedOverlay != null)
        {
            buttonData.lockedOverlay.SetActive(!finalIsUnlocked);
        }

        // Update icon color
        if (buttonData.characterIcon != null)
        {
            if (!finalIsUnlocked)
            {
                buttonData.characterIcon.color = lockedIconColor;
            }
        }
    }

    public GameObject GetCharacterButton(int characterID)
    {
        foreach (GameObject buttonGO in characterButtons)
        {
            CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
            if (buttonData != null && buttonData.characterID == characterID)
            {
                return buttonGO;
            }
        }
        return null;
    }

    public void SelectCharacterByID(int characterID)
    {
        for (int i = 0; i < characterDatabase.characters.Count; i++)
        {
            if (characterDatabase.characters[i].characterID == characterID)
            {
                OnCharacterButtonClicked(i);
                return;
            }
        }
        Debug.LogWarning($"Character with ID {characterID} not found!");
    }

    public void RefreshPanel()
    {
        InitializeCharacterPanel();
    }

    public int GetCurrentCharacterID()
    {
        return currentSelectedCharacterID;
    }

    public void HideLockedFeedbackImmediately()
    {
        if (lockedFeedbackCoroutine != null)
        {
            StopCoroutine(lockedFeedbackCoroutine);
            lockedFeedbackCoroutine = null;
        }

        if (lockedFeedbackCanvasGroup != null)
        {
            lockedFeedbackCanvasGroup.alpha = 0f;
            lockedFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        isShowingLockedFeedback = false;
    }
}