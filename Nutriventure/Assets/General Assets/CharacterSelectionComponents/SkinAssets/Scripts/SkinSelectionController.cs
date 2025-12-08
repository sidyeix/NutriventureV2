using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkinSelectionController : MonoBehaviour
{
    [Header("References")]
    public CharacterSelectionController characterSelectionController;
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;
    public TimelineSignalBridge skinTimelineBridge;

    [Header("UI References")]
    public GridLayoutGroup skinGridLayout;
    public GameObject skinButtonPrefab;
    public GameObject noSkinsText;
    public GameObject skinSelectionPanel;
    public Image characterIconImage;
    public TMP_Text characterNameText;
    public TMP_Text characterDescriptionText;
    public TMP_Text skinNameText;

    [Header("Button States")]
    public Color lockedSkinColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color unlockedSkinColor = Color.white;

    private List<GameObject> skinButtons = new List<GameObject>();
    private CharacterDatabase.CharacterData currentCharacterData;
    private int selectedSkinID = -1;
    private bool isInSkinPreview = false;

    void Start()
    {
        // Hide skin selection panel initially
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(false);
        }

        if (noSkinsText != null)
        {
            noSkinsText.SetActive(false);
        }
    }

    // Call this when entering skin selection mode
    public void EnterSkinSelection(int characterID)
    {
        currentCharacterData = characterDatabase.GetCharacterByID(characterID);
        if (currentCharacterData == null)
        {
            Debug.LogError($"Character with ID {characterID} not found!");
            return;
        }

        // Update character info display
        UpdateCharacterInfoDisplay(currentCharacterData);

        // Populate skin buttons
        PopulateSkinGrid(currentCharacterData);

        // Show skin selection panel
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(true);
        }

        isInSkinPreview = true;

        // Load saved skin selection for this character
        if (GameDataManager.Instance != null)
        {
            selectedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            UpdateSkinNameDisplay();
        }
        else
        {
            selectedSkinID = -1;
            if (skinNameText != null)
            {
                skinNameText.text = "Default";
            }
        }
    }

    // Call this when exiting skin selection mode
    public void ExitSkinSelection()
    {
        // Clear all skin buttons
        ClearSkinGrid();

        // Hide skin selection panel
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(false);
        }

        isInSkinPreview = false;
    }

    private void UpdateCharacterInfoDisplay(CharacterDatabase.CharacterData characterData)
    {
        if (characterIconImage != null && characterData.characterIcon != null)
        {
            characterIconImage.sprite = characterData.characterIcon;
        }

        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }

        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = characterData.characterDescription;
        }

        UpdateSkinNameDisplay();
    }

    private void UpdateSkinNameDisplay()
    {
        if (skinNameText != null)
        {
            if (selectedSkinID == -1)
            {
                skinNameText.text = "Default";
            }
            else
            {
                var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
                if (skinData != null)
                {
                    skinNameText.text = skinData.skinName;
                }
                else
                {
                    skinNameText.text = "Default";
                }
            }
        }
    }

    private void PopulateSkinGrid(CharacterDatabase.CharacterData characterData)
    {
        // Clear existing buttons
        ClearSkinGrid();

        // Check if character has skins
        if (characterData.skins == null || characterData.skins.Count == 0)
        {
            // Show "no skins available" text
            if (noSkinsText != null)
            {
                noSkinsText.SetActive(true);
            }

            if (skinGridLayout != null)
            {
                skinGridLayout.gameObject.SetActive(false);
            }
            return;
        }

        // Hide "no skins" text
        if (noSkinsText != null)
        {
            noSkinsText.SetActive(false);
        }

        // Show grid
        if (skinGridLayout != null)
        {
            skinGridLayout.gameObject.SetActive(true);

            // Create buttons for each skin
            foreach (var skinData in characterData.skins)
            {
                CreateSkinButton(characterData, skinData);
            }
        }
    }

    private void CreateSkinButton(CharacterDatabase.CharacterData characterData, CharacterDatabase.SkinData skinData)
    {
        if (skinButtonPrefab == null || skinGridLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinGridLayout.transform);
        skinButtons.Add(buttonObj);

        // Check if skin is unlocked
        bool isUnlocked = IsSkinUnlocked(characterData.characterID, skinData.skinID);

        // Setup button components
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = isUnlocked;

            // Remove existing listeners and add new one
            button.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                button.onClick.AddListener(() => OnSkinButtonClicked(skinData));
            }
            else
            {
                button.onClick.AddListener(() => OnLockedSkinClicked(skinData));
            }
        }

        // Set skin icon
        Image iconImage = buttonObj.GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            if (skinData.skinIcon != null)
            {
                iconImage.sprite = skinData.skinIcon;
            }

            // Change color based on unlock status
            iconImage.color = isUnlocked ? unlockedSkinColor : lockedSkinColor;
        }

        // Set skin name on the button itself
        TMP_Text buttonNameText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonNameText != null)
        {
            buttonNameText.text = skinData.skinName;

            // Change text color based on unlock status
            buttonNameText.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        // Add lock icon for locked skins
        if (!isUnlocked)
        {
            // You can add a lock icon overlay here if you have one
            // GameObject lockIcon = Instantiate(lockIconPrefab, buttonObj.transform);
        }
    }

    private bool IsSkinUnlocked(int characterID, int skinID)
    {
        // Default skin (ID = -1) is always unlocked
        if (skinID == -1) return true;

        // Check GameData for unlock status
        if (GameDataManager.Instance != null)
        {
            // Check if explicitly unlocked in GameData
            bool isUnlockedInGameData = GameDataManager.Instance.CurrentGameData.IsSkinUnlocked(characterID, skinID);

            // If not unlocked in GameData, check if it's unlocked by default in database
            if (!isUnlockedInGameData)
            {
                var skinDataFromDB = characterDatabase.GetSkinByID(characterID, skinID);
                if (skinDataFromDB != null && skinDataFromDB.unlockedByDefault)
                {
                    // Auto-unlock default skins and save
                    GameDataManager.Instance.CurrentGameData.UnlockSkinForCharacter(characterID, skinID);
                    GameDataManager.Instance.SaveGameData();
                    Debug.Log($"Auto-unlocked default skin: {skinDataFromDB.skinName} for character ID: {characterID}");
                    return true;
                }
            }

            return isUnlockedInGameData;
        }

        // If no GameDataManager, check database for default unlocks
        var skinDataFromDatabase = characterDatabase.GetSkinByID(characterID, skinID);
        return skinDataFromDatabase != null && skinDataFromDatabase.unlockedByDefault;
    }

    private void OnSkinButtonClicked(CharacterDatabase.SkinData skinData)
    {
        Debug.Log($"=== SKIN BUTTON CLICKED ===");

        selectedSkinID = skinData.skinID;
        UpdateSkinNameDisplay();

        bool hasTimeline = skinData.skinTimeline != null;

        // ===== SINGLE CLICK LOGIC =====
        if (hasTimeline)
        {
            // SKIN WITH TIMELINE: Play timeline (timeline will handle switching to skin environment)
            if (skinTimelineBridge != null)
            {
                skinTimelineBridge.PlayTimelineForSkin(skinData.skinTimeline, skinData.skinID);
            }
        }
        else
        {
            // SKIN WITHOUT TIMELINE: Must show in MAIN environment only

            // IMPORTANT: Stop any currently playing timeline first!
            // This will automatically switch back to main environment
            if (skinTimelineBridge != null)
            {
                skinTimelineBridge.StopTimelineAndReturn();

                // Apply the skin prefab
                if (characterVisualSwapper != null)
                {
                    characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
                }

                Debug.Log("Applied skin without timeline - stopped any playing timeline, switched to main environment");
            }
            else if (characterVisualSwapper != null)
            {
                // Fallback if no bridge
                characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
            }
        }

        // ===== UPDATE & SAVE =====
        if (characterSelectionController != null)
        {
            characterSelectionController.UpdateSkinSelection(skinData.skinID);
        }

        SaveSkinSelection(currentCharacterData.characterID, skinData.skinID);

        // Play selection sound
        if (skinData.selectionSound != null)
        {
            AudioSource.PlayClipAtPoint(skinData.selectionSound, Camera.main.transform.position);
        }
    }

    // Helper method for normal skins
    private void ApplySkinImmediately(int skinID)
    {
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplySkinToCurrentCharacter(skinID);
        }
        else
        {
            Debug.LogError("CharacterVisualSwapper not found!");
        }
    }

    private void SaveSkinSelection(int characterID, int skinID)
    {
        // Save to GameDataManager
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(characterID, skinID);
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Saved skin {skinID} for character {characterID}");
        }
        else
        {
            Debug.LogWarning("GameDataManager.Instance is null, cannot save skin selection");
        }
    }

    private void OnLockedSkinClicked(CharacterDatabase.SkinData skinData)
    {
        Debug.Log($"Locked skin clicked: {skinData.skinName}");

        // Show unlock requirements or purchase dialog
        ShowUnlockRequirements(skinData);
    }

    private void ShowUnlockRequirements(CharacterDatabase.SkinData skinData)
    {
        // You can implement a UI popup here showing unlock requirements
        Debug.Log($"Unlock Requirements for {skinData.skinName}:");
        Debug.Log($"- Coins needed: {skinData.coinsToUnlock}");
        Debug.Log($"- Level requirement: {skinData.levelRequirement}");

        // Example: Show a popup with unlock options
        // UnlockPopup.Instance.Show(skinData);
    }

    private void ClearSkinGrid()
    {
        foreach (GameObject button in skinButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        skinButtons.Clear();
    }

    // Getter for selected skin ID
    public int GetSelectedSkinID()
    {
        return selectedSkinID;
    }

    // Check if we're currently in skin preview mode
    public bool IsInSkinPreview()
    {
        return isInSkinPreview;
    }

    // Unlock a skin (called from unlock/purchase system)
    public void UnlockSkin(int characterID, int skinID)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.UnlockSkinForCharacter(characterID, skinID);
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Unlocked skin {skinID} for character {characterID}");

            // Refresh skin grid if this is the current character
            if (currentCharacterData != null && currentCharacterData.characterID == characterID)
            {
                PopulateSkinGrid(currentCharacterData);
            }
        }
    }

    void OnDestroy()
    {
        ClearSkinGrid();
    }
}