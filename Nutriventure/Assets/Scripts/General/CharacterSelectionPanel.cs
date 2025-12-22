using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

        Debug.Log($"Character {selectedCharacter.characterName} (ID: {characterID}) selected for preview");

        // Check if character is unlocked using database method (checks unlockedByDefault)
        bool isUnlocked = characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData);
        if (!isUnlocked)
        {
            Debug.Log($"Character {selectedCharacter.characterName} is locked!");
            return;
        }

        // Don't do anything if same character is selected
        if (characterID == currentSelectedCharacterID)
        {
            Debug.Log("Same character selected, ignoring");
            return;
        }

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

        // DEBUG THE IDS
        Debug.Log($"=== Character Check ===");
        Debug.Log($"Button characterIndex: {buttonData.characterIndex}");
        Debug.Log($"Button characterID: {buttonData.characterID}");
        Debug.Log($"Database character ID: {characterData.characterID}");
        Debug.Log($"Character Name: {characterData.characterName}");
        Debug.Log($"unlockedByDefault: {characterData.unlockedByDefault}");

        // Are we getting the right character from the database?
        CharacterDatabase.CharacterData charFromID = characterDatabase.GetCharacterByID(buttonData.characterID);
        Debug.Log($"GetCharacterByID({buttonData.characterID}) found: {charFromID?.characterName ?? "NULL"}");

        // Check unlock status using database method (checks unlockedByDefault)
        bool isUnlocked = characterDatabase.IsCharacterUnlocked(characterData.characterID, GameDataManager.Instance.CurrentGameData);
        bool isSelected = (currentSelectedCharacterID == characterData.characterID);

        Button button = buttonGO.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        // Update lock icon (show when locked)
        if (buttonData.lockIcon != null)
        {
            buttonData.lockIcon.gameObject.SetActive(!isUnlocked);
        }

        // Update locked overlay (show when locked)
        if (buttonData.lockedOverlay != null)
        {
            buttonData.lockedOverlay.SetActive(!isUnlocked);
        }

        // Update selection highlight
        if (buttonData.selectedHighlight != null)
        {
            buttonData.selectedHighlight.gameObject.SetActive(isSelected);
        }

        // Update character icon color based on selection and lock state
        if (buttonData.characterIcon != null)
        {
            if (!isUnlocked)
            {
                // Character is locked - use locked color (#313131)
                buttonData.characterIcon.color = lockedIconColor;
            }
            else if (isSelected)
            {
                // Character is selected and unlocked - use selected color (white)
                buttonData.characterIcon.color = selectedIconColor;
            }
            else
            {
                // Character is unlocked but not selected - use deselected color (#9A9A9A)
                buttonData.characterIcon.color = deselectedIconColor;
            }
        }

        // Debug log for unlock status
        if (characterData.unlockedByDefault)
        {
            Debug.Log($"Character {characterData.characterName} is unlocked by default in database");
        }
        else if (isUnlocked)
        {
            Debug.Log($"Character {characterData.characterName} is unlocked via GameData");
        }
        else
        {
            Debug.Log($"Character {characterData.characterName} is LOCKED");
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
        return characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData);
    }
}