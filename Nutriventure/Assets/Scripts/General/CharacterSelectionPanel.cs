using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Helper component to store button data - MOVED TO TOP
public class CharacterButtonData : MonoBehaviour
{
    public int characterIndex;
    public int characterID;
    public Image lockIcon;
    public Image selectedHighlight;
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

    [Header("Character System References")]
    public CharacterDatabase characterDatabase;
    public CharacterVisualSwapper characterVisualSwapper;
    public InputManager inputManager;
    public CharacterSelectionController characterSelectionController;

    private List<GameObject> characterButtons = new List<GameObject>();
    private MainMenu_Manager mainMenuManager;
    private int currentSelectedCharacterID = -1;

    void Start()
    {
        // Get reference to MainMenu_Manager
        mainMenuManager = FindObjectOfType<MainMenu_Manager>();

        if (mainMenuManager == null)
        {
            Debug.LogError("MainMenu_Manager not found in scene!");
        }

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

        // Find UI elements
        SetupButtonUIElements(buttonGO, characterData);

        // Set button click listener
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnCharacterButtonClicked(characterIndex));

        // Store character data in button for easy access
        CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
        if (buttonData == null)
            buttonData = buttonGO.AddComponent<CharacterButtonData>();

        buttonData.characterIndex = characterIndex;
        buttonData.characterID = characterData.characterID;

        // Initial appearance setup
        UpdateButtonAppearance(buttonGO);
    }

    private void SetupButtonUIElements(GameObject buttonGO, CharacterDatabase.CharacterData characterData)
    {
        // Character Icon
        Image characterIcon = buttonGO.transform.Find("CharacterIcon")?.GetComponent<Image>();
        if (characterIcon != null && characterData.characterIcon != null)
            characterIcon.sprite = characterData.characterIcon;

        // Lock Icon
        Image lockIcon = buttonGO.transform.Find("LockIcon")?.GetComponent<Image>();
        if (lockIcon != null)
        {
            CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
            if (buttonData != null) buttonData.lockIcon = lockIcon;
        }

        // Selected Highlight
        Image selectedHighlight = buttonGO.transform.Find("SelectedHighlight")?.GetComponent<Image>();
        if (selectedHighlight != null)
        {
            CharacterButtonData buttonData = buttonGO.GetComponent<CharacterButtonData>();
            if (buttonData != null) buttonData.selectedHighlight = selectedHighlight;
        }
    }

    private void OnCharacterButtonClicked(int characterIndex)
    {
        // Play button click sound
        AudioHandler.Instance.PlayButtonClick();

        // Get character data
        CharacterDatabase.CharacterData selectedCharacter = characterDatabase.characters[characterIndex];
        int characterID = selectedCharacter.characterID;

        Debug.Log($"Character {selectedCharacter.characterName} (ID: {characterID}) selected for preview");

        // Check if character is unlocked
        if (!characterDatabase.IsCharacterUnlocked(characterID, GameDataManager.Instance.CurrentGameData))
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
        if (selectedCharacter.selectionSound != null)
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

        Debug.Log($"Character preview changed to: {selectedCharacter.characterName} (ID: {characterID}) - Click 'Select Character' to confirm");
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
        bool isUnlocked = characterDatabase.IsCharacterUnlocked(characterData.characterID, GameDataManager.Instance.CurrentGameData);
        bool isSelected = (currentSelectedCharacterID == characterData.characterID);

        Button button = buttonGO.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = isUnlocked;
        }

        // Update lock icon
        if (buttonData.lockIcon != null)
        {
            buttonData.lockIcon.gameObject.SetActive(!isUnlocked);
        }

        // Update selection highlight
        if (buttonData.selectedHighlight != null)
        {
            buttonData.selectedHighlight.gameObject.SetActive(isSelected);
        }

        // Update button color based on selection and lock state
        Image buttonImage = buttonGO.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (isSelected)
            {
                buttonImage.color = new Color(1f, 1f, 0.5f, 1f); // Light yellow for selection
            }
            else if (!isUnlocked)
            {
                buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray for locked
            }
            else
            {
                buttonImage.color = Color.white; // Normal color
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
}