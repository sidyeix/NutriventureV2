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
    public Image characterLogoImage;  // ADDED FOR CHARACTER LOGO

    [Header("Locked Character Feedback")]
    public CanvasGroup lockedFeedbackCanvasGroup; // Drag your CanvasGroup here
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;
    public float displayDuration = 2f;
    public TMPro.TextMeshProUGUI lockedMessageText; // Optional: For showing "Locked!" message

    [Header("Layout Settings")]
    public int maxColumns = 3;
    public float buttonSpacing = 20f;
    public Vector2 buttonSize = new Vector2(350f, 450f);

    [Header("Character Icon Colors")]
    [Tooltip("Color of character icon when selected")]
    public Color selectedIconColor = Color.white;
    [Tooltip("Color of character icon when not selected")]
    public Color deselectedIconColor = new Color(0.6f, 0.6f, 0.6f, 1f); // #9A9A9A
    [Tooltip("Color of character icon when locked - #313131")]
    public Color lockedIconColor = new Color(0.192f, 0.192f, 0.192f, 1f); // #313131

    [Header("Character System References")]
    public CharacterDatabase characterDatabase;
    public CharacterVisualSwapper characterVisualSwapper;
    public InputManager inputManager;
    public CharacterSelectionController characterSelectionController;

    private List<GameObject> characterButtons = new List<GameObject>();
    private int currentSelectedCharacterID = -1;
    private Coroutine lockedFeedbackCoroutine;
    private bool isShowingLockedFeedback = false;

    void Start()
    {
        // Get reference to CharacterSelectionController
        if (characterSelectionController == null)
        {
            characterSelectionController = FindObjectOfType<CharacterSelectionController>();
        }

        // Initialize current selection from GameData
        if (GameDataManager.Instance != null)
        {
            currentSelectedCharacterID = GameDataManager.Instance.CurrentGameData.selectedCharacterID;
        }

        // Initialize the locked feedback canvas group
        if (lockedFeedbackCanvasGroup != null)
        {
            lockedFeedbackCanvasGroup.alpha = 0f;
            lockedFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        // Initialize the panel
        InitializeCharacterPanel();

        // Debug check
        DebugCharacterIcons();
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

        // Setup grid layout
        SetupGridLayout();

        // Update button appearances
        UpdateAllButtonAppearances();
    }

    private void CreateCharacterButton(int characterIndex)
    {
        if (characterButtonPrefab == null || characterButtonContainer == null)
        {
            Debug.LogError("Character button prefab or container not assigned!");
            return;
        }

        // Instantiate button
        GameObject buttonGO = Instantiate(characterButtonPrefab, characterButtonContainer);
        characterButtons.Add(buttonGO);

        // Get character data
        CharacterDatabase.CharacterData characterData = characterDatabase.characters[characterIndex];

        // Setup button components
        Button button = buttonGO.GetComponent<Button>();

        // Store character data in button for easy access
        CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
        if (buttonData == null)
            buttonData = buttonGO.AddComponent<CharacterButtonData>();

        buttonData.characterIndex = characterIndex;
        buttonData.characterID = characterData.characterID;

        // Setup UI elements (including icon)
        SetupButtonUIElements(buttonGO, characterData, buttonData);

        // Set button click listener
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCharacterButtonClicked(characterIndex));

        // Initial appearance setup
        UpdateButtonAppearance(buttonGO);
    }

    private void SetupButtonUIElements(GameObject buttonGO, CharacterDatabase.CharacterData characterData, CharacterButtonData buttonData)
    {
        // 1. CHARACTER ICON (Main addition)
        Transform characterIconTransform = FindDeepChild(buttonGO.transform, "CharacterIcon");
        if (characterIconTransform == null)
        {
            characterIconTransform = buttonGO.transform.Find("CharacterIcon");
        }

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
                    Debug.LogWarning($"No icon assigned for character: {characterData.characterName}");
                    characterIcon.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError($"CharacterIcon GameObject found but no Image component attached!");
            }
        }
        else
        {
            Debug.LogError($"CharacterIcon not found in button prefab hierarchy! Check button prefab structure.");
        }

        // 2. LOCK ICON
        Transform lockIconTransform = FindDeepChild(buttonGO.transform, "LockIcon");
        if (lockIconTransform == null)
        {
            lockIconTransform = buttonGO.transform.Find("LockIcon");
        }

        if (lockIconTransform != null)
        {
            Image lockIcon = lockIconTransform.GetComponent<Image>();
            if (lockIcon != null)
            {
                buttonData.lockIcon = lockIcon;
            }
        }

        // 3. SELECTED HIGHLIGHT
        Transform selectedHighlightTransform = FindDeepChild(buttonGO.transform, "SelectedHighlight");
        if (selectedHighlightTransform == null)
        {
            selectedHighlightTransform = buttonGO.transform.Find("SelectedHighlight");
        }

        if (selectedHighlightTransform != null)
        {
            Image selectedHighlight = selectedHighlightTransform.GetComponent<Image>();
            if (selectedHighlight != null)
            {
                buttonData.selectedHighlight = selectedHighlight;
            }
        }

        // 4. LOCKED OVERLAY - NEW
        Transform lockedOverlayTransform = FindDeepChild(buttonGO.transform, "LockedOverlay");
        if (lockedOverlayTransform == null)
        {
            lockedOverlayTransform = buttonGO.transform.Find("LockedOverlay");
        }

        if (lockedOverlayTransform != null)
        {
            buttonData.lockedOverlay = lockedOverlayTransform.gameObject;
        }
        else
        {
            Debug.LogWarning($"LockedOverlay not found in button prefab hierarchy!");
        }
    }

    // Helper method to find child recursively
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

        // Get character data
        CharacterDatabase.CharacterData selectedCharacter = characterDatabase.characters[characterIndex];
        int characterID = selectedCharacter.characterID;

        Debug.Log($"=== Character Button Clicked ===");
        Debug.Log($"Character: {selectedCharacter.characterName} (ID: {characterID})");

        // Get GameData
        GameData gameData = GameDataManager.Instance?.CurrentGameData;

        if (gameData == null)
        {
            Debug.LogError("GameData is null! Cannot check unlock status.");
            return;
        }

        // Debug: Show all unlocked IDs
        string unlockedIDs = "Currently unlocked IDs: ";
        foreach (int id in gameData.unlockedCharacterIDs)
        {
            unlockedIDs += id + ", ";
        }
        Debug.Log(unlockedIDs);

        // Check unlock status - FIXED LOGIC
        bool isUnlockedByDefault = selectedCharacter.unlockedByDefault;
        bool isUnlockedInGameData = gameData.unlockedCharacterIDs.Contains(characterID);
        bool isUnlocked = isUnlockedByDefault || isUnlockedInGameData;

        Debug.Log($"unlockedByDefault: {isUnlockedByDefault}");
        Debug.Log($"in unlockedCharacterIDs: {isUnlockedInGameData}");
        Debug.Log($"Final isUnlocked: {isUnlocked}");

        if (!isUnlocked)
        {
            Debug.Log($"Character {selectedCharacter.characterName} is locked! Showing feedback...");

            // Show locked feedback instead of swapping character
            ShowLockedCharacterFeedback(selectedCharacter);
            return; // Don't proceed with character swap
        }

        // Only proceed if character is unlocked
        Debug.Log($"Character {selectedCharacter.characterName} is unlocked, proceeding...");

        // Don't do anything if same character is selected
        if (characterID == currentSelectedCharacterID)
        {
            Debug.Log("Same character selected, ignoring");
            return;
        }

        // UPDATE CHARACTER INFO DISPLAY
        UpdateCharacterInfoDisplay(selectedCharacter);

        // Update local selection
        currentSelectedCharacterID = characterID;

        // Apply character visuals using CharacterVisualSwapper
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(selectedCharacter);
            Debug.Log($"Applied character visuals for: {selectedCharacter.characterName}");
        }
        else
        {
            Debug.LogError("CharacterVisualSwapper not assigned!");
        }

        // Reset character rotation when selecting new character
        if (characterSelectionController != null && characterSelectionController.characterRotationController != null)
        {
            characterSelectionController.characterRotationController.OnCharacterSelected();
            Debug.Log("Character rotation reset for new character preview");
        }

        // Play character selection sound if available
        if (selectedCharacter.selectionSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(selectedCharacter.selectionSound);
        }

        // Update all button appearances
        UpdateAllButtonAppearances();

        // Notify character selection controller about the preview selection
        if (characterSelectionController != null)
        {
            characterSelectionController.OnCharacterPreviewSelected(characterID);
        }

        // Ensure input stays disabled during character selection
        if (inputManager != null && inputManager.IsInputEnabled())
        {
            inputManager.DisablePlayerInput();
        }

        Debug.Log($"Character preview changed to: {selectedCharacter.characterName} (ID: {characterID})");
    }

    // UPDATED METHOD: Now also displays character logo
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

        if (characterLogoImage != null)  // ADDED LOGO DISPLAY
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
                Debug.LogWarning($"No logo assigned for character: {characterData.characterName}");
            }
        }
    }

    // New method: Show locked character feedback
    public void ShowLockedCharacterFeedback(CharacterDatabase.CharacterData lockedCharacter)
    {
        // Stop any existing feedback coroutine
        if (lockedFeedbackCoroutine != null)
        {
            StopCoroutine(lockedFeedbackCoroutine);
        }

        // Start new feedback coroutine
        lockedFeedbackCoroutine = StartCoroutine(ShowLockedFeedbackCoroutine(lockedCharacter));
    }

    private IEnumerator ShowLockedFeedbackCoroutine(CharacterDatabase.CharacterData lockedCharacter)
    {
        isShowingLockedFeedback = true;

        // Set message text if available
        if (lockedMessageText != null)
        {
            lockedMessageText.text = $"{lockedCharacter.characterName} is locked!\nPurchase it from Sir Fuego's Wagon";
        }

        // Fade in
        if (lockedFeedbackCanvasGroup != null)
        {
            lockedFeedbackCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(lockedFeedbackCanvasGroup, 0f, 1f, fadeInDuration));
        }

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (lockedFeedbackCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(lockedFeedbackCanvasGroup, 1f, 0f, fadeOutDuration));
            lockedFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        isShowingLockedFeedback = false;
        lockedFeedbackCoroutine = null;
    }

    // Helper method for fading CanvasGroup
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
        // Add or get GridLayoutGroup
        GridLayoutGroup gridLayout = characterButtonContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = characterButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configure grid layout for 3 columns
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = maxColumns;
        gridLayout.cellSize = buttonSize;
        gridLayout.spacing = new Vector2(buttonSpacing, buttonSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        // Add ContentSizeFitter for dynamic height
        ContentSizeFitter sizeFitter = characterButtonContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = characterButtonContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Configure scroll rect for smooth scrolling
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

        // Get current GameData
        GameData gameData = GameDataManager.Instance?.CurrentGameData;

        if (gameData == null)
        {
            Debug.LogError("GameData is null! Using fallback logic.");

            // Fallback: Just use unlockedByDefault
            bool isUnlocked = characterData.unlockedByDefault;

            if (buttonData.lockIcon != null)
                buttonData.lockIcon.gameObject.SetActive(!isUnlocked);
            if (buttonData.lockedOverlay != null)
                buttonData.lockedOverlay.SetActive(!isUnlocked);

            Debug.Log($"{characterData.characterName}: GameData null, using fallback - unlocked: {isUnlocked}");
            return;
        }

        // Debug: Print all unlocked character IDs
        string unlockedIDs = "Unlocked IDs: ";
        foreach (int id in gameData.unlockedCharacterIDs)
        {
            unlockedIDs += id + ", ";
        }
        Debug.Log(unlockedIDs);

        // Check both conditions
        bool isUnlockedByDefault = characterData.unlockedByDefault;
        bool isUnlockedInGameData = gameData.unlockedCharacterIDs.Contains(characterData.characterID);
        bool finalIsUnlocked = isUnlockedByDefault || isUnlockedInGameData;

        Debug.Log($"{characterData.characterName} (ID: {characterData.characterID}): " +
                  $"unlockedByDefault={isUnlockedByDefault}, " +
                  $"inGameData={isUnlockedInGameData}, " +
                  $"FINAL={finalIsUnlocked}");

        // Set UI elements
        if (buttonData.lockIcon != null)
        {
            buttonData.lockIcon.gameObject.SetActive(!finalIsUnlocked);
            Debug.Log($"LockIcon active: {buttonData.lockIcon.gameObject.activeSelf}");
        }

        if (buttonData.lockedOverlay != null)
        {
            buttonData.lockedOverlay.SetActive(!finalIsUnlocked);
            Debug.Log($"LockedOverlay active: {buttonData.lockedOverlay.activeSelf}");
        }
    }

    // Debug method to check icon setup
    private void DebugCharacterIcons()
    {
        Debug.Log($"=== Character Icon Debug ===");
        Debug.Log($"Total characters: {characterDatabase.characters.Count}");
        Debug.Log($"Total buttons created: {characterButtons.Count}");

        for (int i = 0; i < characterDatabase.characters.Count; i++)
        {
            var charData = characterDatabase.characters[i];
            Debug.Log($"Character {i}: {charData.characterName} - " +
                     $"Icon: {charData.characterIcon?.name ?? "NULL"}, " +
                     $"Logo: {charData.characterLogo?.name ?? "NULL"}, " +  // ADDED LOGO DEBUG
                     $"UnlockedByDefault: {charData.unlockedByDefault}");
        }

        foreach (GameObject buttonGO in characterButtons)
        {
            CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
            if (buttonData != null)
            {
                CharacterDatabase.CharacterData charData = characterDatabase.characters[buttonData.characterIndex];

                Image icon = buttonGO.transform.Find("CharacterIcon")?.GetComponent<Image>();
                if (icon == null)
                {
                    Transform iconTransform = FindDeepChild(buttonGO.transform, "CharacterIcon");
                    icon = iconTransform?.GetComponent<Image>();
                }

                Debug.Log($"Button {buttonData.characterIndex} - " +
                         $"Icon Found: {icon != null}, " +
                         $"Sprite: {icon?.sprite?.name ?? "NULL"}, " +
                         $"Active: {icon?.gameObject.activeSelf}");
            }
        }
    }

    // Force refresh icons (useful for testing)
    public void ForceRefreshIcons()
    {
        foreach (GameObject buttonGO in characterButtons)
        {
            CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
            if (buttonData != null && buttonData.characterIcon != null)
            {
                CharacterDatabase.CharacterData charData = characterDatabase.characters[buttonData.characterIndex];
                if (charData.characterIcon != null)
                {
                    buttonData.characterIcon.sprite = charData.characterIcon;
                    buttonData.characterIcon.SetAllDirty(); // Force UI update

                    // Also update color and overlay
                    UpdateButtonAppearance(buttonGO);
                }
            }
        }
    }

    // Call this when characters are unlocked to refresh the panel
    public void RefreshPanel()
    {
        InitializeCharacterPanel();
    }

    // Get currently selected character ID
    public int GetCurrentCharacterID()
    {
        return currentSelectedCharacterID;
    }

    // Get character button by ID
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

    // Select a specific character programmatically
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

    // Method to manually update icon colors (can be called externally)
    public void UpdateIconColors(Color newSelectedColor, Color newDeselectedColor, Color newLockedColor)
    {
        selectedIconColor = newSelectedColor;
        deselectedIconColor = newDeselectedColor;
        lockedIconColor = newLockedColor;
        UpdateAllButtonAppearances();
    }

    // Method to check if a specific character is unlocked
    public bool CheckCharacterUnlocked(int characterID)
    {
        // Use the database's method which should handle both conditions
        return characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData);
    }

    // Optional: Add a method to manually hide the feedback
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

    [ContextMenu("Debug: Reset Unlocked Characters")]
    public void DebugResetUnlockedCharacters()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            Debug.Log("BEFORE Reset: " + string.Join(", ", GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs));

            // Keep only truly default characters
            List<int> newUnlockedList = new List<int>();
            foreach (var character in characterDatabase.characters)
            {
                if (character.unlockedByDefault)
                {
                    newUnlockedList.Add(character.characterID);
                }
            }

            GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs = newUnlockedList;
            GameDataManager.Instance.SaveGameData();

            Debug.Log("AFTER Reset: " + string.Join(", ", GameDataManager.Instance.CurrentGameData.unlockedCharacterIDs));
            Debug.Log("Only truly default characters kept!");

            // Refresh the panel
            UpdateAllButtonAppearances();
        }
    }
}