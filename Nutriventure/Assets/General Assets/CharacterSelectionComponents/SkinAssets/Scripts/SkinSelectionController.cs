using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Cinemachine;
using System.Collections;

public class SkinSelectionController : MonoBehaviour
{
    [Header("References")]
    public CharacterSelectionController characterSelectionController;
    public CharacterVisualSwapper characterVisualSwapper;
    public CharacterDatabase characterDatabase;
    public Player_Data playerData; // Reference to update UI displays

    [Header("UI References")]
    public HorizontalLayoutGroup skinHorizontalLayout;
    public GameObject skinButtonPrefab;
    public GameObject noSkinsText;
    public GameObject skinSelectionPanel;
    public Image characterLogoImage;
    public TMP_Text characterNameText;
    public TMP_Text characterTaglineText;
    public TMP_Text characterDescriptionText;
    public TMP_Text skinNameText;
    public Button backButton;

    [Header("Action Buttons")]
    public GameObject selectButton;
    public GameObject buyButton;
    public GameObject lockedButton;
    public TMP_Text buyButtonText;
    public TMP_Text lockedButtonText; // Remove this if you have icon embedded
    public Button previewSelectButton; // Keep for compatibility

    [Header("Confirmation Dialog")]
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Currency Displays")]
    public TMP_Text coinsText; // For NutriCoins
    public TMP_Text gemsText; // NEW: For NutriGems

    [Header("Skin Card Colors")]
    public Color selectedColor = Color.white; // FFFFFF - For selected/previewed skin
    public Color deselectedColor = new Color(0.588f, 0.588f, 0.588f, 1f); // 969696
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Default Skin Card (Character)")]
    public Sprite defaultSkinIcon; // If you want a special icon for the default skin

    [Header("Error Handling")]
    public CanvasGroup skinErrorCanvasGroup;
    public TMP_Text skinErrorMessageText;
    public float errorFadeInDuration = 0.3f;
    public float errorFadeOutDuration = 0.5f;
    public float errorDisplayDuration = 2f;

    [Header("Camera Settings")]
    public CinemachineVirtualCamera characterSelectionCamera;
    public CinemachineVirtualCamera skinSelectionCamera;

    [Header("Canvas Settings")]
    public CanvasGroup characterSelectionCanvas;
    public CanvasGroup skinSelectionCanvas;
    public CanvasGroup characterControlsCanvas;

    private List<GameObject> skinButtons = new List<GameObject>();
    private CharacterDatabase.CharacterData currentCharacterData;
    private int selectedSkinID = -1; // The skin that is currently SELECTED in UI
    private bool isInSkinPreview = false;
    private Coroutine errorCoroutine;

    void Start()
    {
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(false);
        }

        if (noSkinsText != null)
        {
            noSkinsText.SetActive(false);
        }

        // Keep existing button for compatibility
        if (previewSelectButton != null)
        {
            previewSelectButton.onClick.RemoveAllListeners();
            previewSelectButton.onClick.AddListener(OnSelectButtonClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // Setup new action buttons
        if (selectButton != null)
        {
            Button selectBtn = selectButton.GetComponent<Button>();
            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(OnSelectButtonClicked);
            }
        }

        if (buyButton != null)
        {
            Button buyBtn = buyButton.GetComponent<Button>();
            if (buyBtn != null)
            {
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(OnBuyButtonClicked);
            }
        }

        if (lockedButton != null)
        {
            Button lockedBtn = lockedButton.GetComponent<Button>();
            if (lockedBtn != null)
            {
                lockedBtn.onClick.RemoveAllListeners();
                lockedBtn.onClick.AddListener(OnLockedButtonClicked);
            }
        }

        // Setup confirmation dialog
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(OnConfirmPurchaseYes);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(OnConfirmPurchaseNo);
            }
        }

        // Initialize error handling
        if (skinErrorCanvasGroup != null)
        {
            skinErrorCanvasGroup.alpha = 0f;
            skinErrorCanvasGroup.gameObject.SetActive(false);
        }

        // Hide all action buttons initially
        HideAllActionButtons();
    }

    public void EnterSkinSelection(int characterID)
    {
        currentCharacterData = characterDatabase.GetCharacterByID(characterID);
        if (currentCharacterData == null)
        {
            Debug.LogError($"Character with ID {characterID} not found!");
            return;
        }

        // Load saved skin for this character
        if (GameDataManager.Instance != null)
        {
            int savedSkinID = GameDataManager.Instance.CurrentGameData.GetSelectedSkinForCharacter(characterID);

            // Always start with default skin (-1) selected
            selectedSkinID = -1;
            Debug.Log($"EnterSkinSelection: CharID={characterID}, SavedSkinID={savedSkinID}, InitiallySelected=-1 (Default)");
        }
        else
        {
            selectedSkinID = -1; // Default to default skin
        }

        UpdateCharacterInfoDisplay(currentCharacterData);
        PopulateSkinButtons(currentCharacterData);

        isInSkinPreview = true;

        // Apply the default character visuals (since default skin is initially selected)
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
        }

        UpdateSkinNameDisplay();
        UpdateActionButtons();
        UpdateCurrencyDisplays(); // NEW: Update both currency displays

        // Make sure the panel is active
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(true);
        }
    }

    public void ExitSkinSelection()
    {
        ClearSkinButtons();
        isInSkinPreview = false;

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
            Debug.Log("Skin camera priority set to 10 on exit");
        }

        // Clear references to prevent memory leaks
        currentCharacterData = null;
        HideAllActionButtons();
    }

    // NEW: Update both currency displays
    private void UpdateCurrencyDisplays()
    {
        if (GameDataManager.Instance == null) return;

        // Update coins display
        if (coinsText != null)
        {
            coinsText.text = GameDataManager.Instance.CurrentGameData.nutriCoins.ToString();
        }

        // Update gems display
        if (gemsText != null)
        {
            gemsText.text = GameDataManager.Instance.CurrentGameData.nutriGems.ToString();
        }

        // Also update Player_Data if available
        if (playerData != null)
        {
            playerData.UpdateCoinDisplayImmediate();
        }
    }

    private void HideSkinSelectionUI()
    {
        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(false);
        }

        if (characterSelectionCamera != null)
        {
            characterSelectionCamera.Priority = 30;
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
        }

        if (characterSelectionCanvas != null)
        {
            characterSelectionCanvas.alpha = 1f;
            characterSelectionCanvas.interactable = true;
            characterSelectionCanvas.blocksRaycasts = true;
        }

        if (skinSelectionCanvas != null)
        {
            skinSelectionCanvas.alpha = 0f;
            skinSelectionCanvas.interactable = false;
            skinSelectionCanvas.blocksRaycasts = false;
        }
    }

    private void UpdateCharacterInfoDisplay(CharacterDatabase.CharacterData characterData)
    {
        if (characterLogoImage != null && characterData.characterLogo != null)
        {
            characterLogoImage.sprite = characterData.characterLogo;
            characterLogoImage.gameObject.SetActive(true);
        }

        if (characterNameText != null)
        {
            characterNameText.text = characterData.characterName;
        }

        if (characterTaglineText != null)
        {
            characterTaglineText.text = characterData.characterTagline;
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
            if (selectedSkinID == -1) // Show selected skin name
            {
                skinNameText.text = currentCharacterData?.characterName ?? "Default";
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
                    skinNameText.text = currentCharacterData?.characterName ?? "Default";
                }
            }
        }
    }

    private void PopulateSkinButtons(CharacterDatabase.CharacterData characterData)
    {
        ClearSkinButtons();

        if (noSkinsText != null) noSkinsText.SetActive(false);
        if (skinHorizontalLayout != null)
        {
            skinHorizontalLayout.gameObject.SetActive(true);

            // CREATE DEFAULT SKIN CARD (Character itself) - Always first
            CreateDefaultSkinButton(characterData);

            // CREATE ACTUAL SKIN CARDS
            if (characterData.skins != null && characterData.skins.Count > 0)
            {
                foreach (var skinData in characterData.skins)
                {
                    CreateSkinButton(characterData, skinData);
                }
            }
        }
    }

    private void CreateDefaultSkinButton(CharacterDatabase.CharacterData characterData)
    {
        if (skinButtonPrefab == null || skinHorizontalLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinHorizontalLayout.transform);
        skinButtons.Add(buttonObj);

        // Default skin (character itself) is always unlocked
        bool isUnlocked = true;

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                OnDefaultSkinButtonClicked(characterData);
            });
        }

        // Find the SkinIcon in the prefab hierarchy
        Image skinIcon = FindSkinIcon(buttonObj.transform);
        if (skinIcon != null)
        {
            // Use character icon for default skin
            if (characterData.characterIcon != null)
            {
                skinIcon.sprite = characterData.characterIcon;
            }

            // Apply initial color
            UpdateButtonIconColor(buttonObj, -1, isUnlocked);
        }

        // Find the SkinName text
        TMP_Text skinNameText = FindSkinNameText(buttonObj.transform);
        if (skinNameText != null)
        {
            skinNameText.text = characterData.characterName;
            skinNameText.color = Color.white;
        }

        // Store skin data for reference
        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null)
            buttonData = buttonObj.AddComponent<SkinButtonData>();

        buttonData.characterID = characterData.characterID;
        buttonData.skinID = -1; // -1 indicates default skin (character itself)
        buttonData.isUnlocked = true; // Always unlocked
        buttonData.isDefaultSkin = true; // Mark as default skin
        buttonData.skinIcon = skinIcon;

        // The first card (default skin) will automatically show as selected because selectedSkinID = -1
    }

    private void CreateSkinButton(CharacterDatabase.CharacterData characterData, CharacterDatabase.SkinData skinData)
    {
        if (skinButtonPrefab == null || skinHorizontalLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinHorizontalLayout.transform);
        skinButtons.Add(buttonObj);

        // Check if skin is unlocked in database OR in game data
        bool isUnlocked = IsSkinUnlocked(characterData.characterID, skinData.skinID);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true; // Always interactable for preview

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                OnSkinButtonClicked(skinData);
            });
        }

        // Find the SkinIcon in the prefab hierarchy
        Image skinIcon = FindSkinIcon(buttonObj.transform);
        if (skinIcon != null)
        {
            if (skinData.skinIcon != null)
            {
                skinIcon.sprite = skinData.skinIcon;
            }

            // Apply initial color
            UpdateButtonIconColor(buttonObj, skinData.skinID, isUnlocked);
        }

        // Find the SkinName text
        TMP_Text skinNameText = FindSkinNameText(buttonObj.transform);
        if (skinNameText != null)
        {
            skinNameText.text = skinData.skinName;
            skinNameText.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        // Store skin data for reference
        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null)
            buttonData = buttonObj.AddComponent<SkinButtonData>();

        buttonData.characterID = characterData.characterID;
        buttonData.skinID = skinData.skinID;
        buttonData.isUnlocked = isUnlocked;
        buttonData.isDefaultSkin = false; // Not default skin
        buttonData.skinIcon = skinIcon;
    }

    private void UpdateButtonIconColor(GameObject buttonObj, int skinID, bool isUnlocked)
    {
        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null || buttonData.skinIcon == null) return;

        bool isSelected = (selectedSkinID == skinID);

        // SIMPLE LOGIC: Only ONE skin can be selected at a time
        if (isSelected)
        {
            // SELECTED skin shows as white
            buttonData.skinIcon.color = selectedColor;
        }
        else if (!isUnlocked)
        {
            // LOCKED skin shows as dimmed
            buttonData.skinIcon.color = lockedColor;
        }
        else
        {
            // DESELECTED skin shows as gray
            buttonData.skinIcon.color = deselectedColor;
        }
    }

    private Image FindSkinIcon(Transform parent)
    {
        // Look for SkinIcon in the hierarchy based on your image
        foreach (Transform child in parent)
        {
            if (child.name == "SkinIcon")
            {
                return child.GetComponent<Image>();
            }

            Image found = FindSkinIcon(child);
            if (found != null) return found;
        }
        return null;
    }

    private TMP_Text FindSkinNameText(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name == "SkinName")
            {
                return child.GetComponent<TMP_Text>();
            }

            TMP_Text found = FindSkinNameText(child);
            if (found != null) return found;
        }
        return null;
    }

    private bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (skinID == -1) return true; // Default skin is always unlocked

        // First check if skin is marked as unlock=true in database
        var skinData = characterDatabase.GetSkinByID(characterID, skinID);
        if (skinData != null && skinData.unlock) return true;

        // Then check if unlocked in game data
        if (GameDataManager.Instance != null)
        {
            return GameDataManager.Instance.CurrentGameData.IsSkinUnlocked(characterID, skinID);
        }

        return false;
    }

    private void OnDefaultSkinButtonClicked(CharacterDatabase.CharacterData characterData)
    {
        selectedSkinID = -1; // -1 indicates default skin

        // Preview the default character (no skin)
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
        }

        // Update ALL button colors
        UpdateAllButtonColors();

        // Update skin name display
        UpdateSkinNameDisplay();

        // Update action buttons
        UpdateActionButtons();

        // Play sound if character has one
        if (characterData.selectionSound != null)
        {
            AudioSource.PlayClipAtPoint(characterData.selectionSound, Camera.main.transform.position);
        }
    }

    private void OnSkinButtonClicked(CharacterDatabase.SkinData skinData)
    {
        selectedSkinID = skinData.skinID;

        // Check if skin is unlocked before applying preview
        bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, skinData.skinID);

        if (isUnlocked)
        {
            // Preview the skin
            if (characterVisualSwapper != null)
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
            }
        }
        else
        {
            // Show locked skin preview (visual only, don't save)
            if (characterVisualSwapper != null)
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
            }
        }

        // Update ALL button colors (this is key - update everything)
        UpdateAllButtonColors();

        // Update skin name display
        UpdateSkinNameDisplay();

        // Update action buttons
        UpdateActionButtons();

        if (skinData.selectionSound != null)
        {
            AudioSource.PlayClipAtPoint(skinData.selectionSound, Camera.main.transform.position);
        }
    }

    private void UpdateActionButtons()
    {
        HideAllActionButtons();

        if (selectedSkinID == -1)
        {
            // Default skin - always unlocked, show select button
            if (selectButton != null) selectButton.SetActive(true);
            return;
        }

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null) return;

        bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);

        if (isUnlocked)
        {
            // Already unlocked - show select button
            if (selectButton != null) selectButton.SetActive(true);
        }
        else
        {
            // Not unlocked yet
            if (skinData.isSkinReward)
            {
                // Skin reward - show locked button (icon is embedded in UI)
                if (lockedButton != null)
                {
                    lockedButton.SetActive(true);
                }
            }
            else
            {
                // Purchasable skin - show buy button with gem price
                if (buyButton != null)
                {
                    buyButton.SetActive(true);
                    if (buyButtonText != null)
                        buyButtonText.text = $"{skinData.nutrigemsToUnlock}";
                }
            }
        }
    }

    private void HideAllActionButtons()
    {
        if (selectButton != null) selectButton.SetActive(false);
        if (buyButton != null) buyButton.SetActive(false);
        if (lockedButton != null) lockedButton.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        // Keep old button hidden too
        if (previewSelectButton != null && previewSelectButton.gameObject != selectButton)
            previewSelectButton.gameObject.SetActive(false);
    }

    private void OnSelectButtonClicked()
    {
        PlayButtonClickSound();

        Debug.Log($"Select button clicked. Selected Skin: {selectedSkinID}");

        // Check if the selected skin is unlocked (skip for default skin which is always unlocked)
        if (selectedSkinID != -1) // Only check for actual skins, not default skin
        {
            bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);

            if (!isUnlocked)
            {
                // Show error message - skin is locked
                Debug.Log($"Skin {selectedSkinID} is locked!");
                ShowLockedSkinMessage();
                return;
            }
        }

        // Save the skin selection
        SaveSkinSelection(currentCharacterData.characterID, selectedSkinID);

        // Update character visual with the selected skin
        if (characterVisualSwapper != null)
        {
            if (selectedSkinID == -1)
            {
                // Apply default character
                characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
            }
            else
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(selectedSkinID);
            }
        }

        // Exit skin selection
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 10;
            Debug.Log("Skin camera priority set to 10 on select");
        }

        if (characterControlsCanvas != null)
        {
            characterControlsCanvas.alpha = 0f;
            characterControlsCanvas.gameObject.SetActive(true);
            characterControlsCanvas.interactable = true;
            characterControlsCanvas.blocksRaycasts = true;
        }

        if (characterSelectionController != null)
        {
            characterSelectionController.OnSelectCharacterConfirmed(currentCharacterData.characterID);
        }
    }

    private void OnBuyButtonClicked()
    {
        PlayButtonClickSound();

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null) return;

        // Check if player has enough NutriGems
        if (GameDataManager.Instance != null)
        {
            int playerNutriGems = GameDataManager.Instance.CurrentGameData.nutriGems; // CHANGED: Use nutriGems not nutriCoins

            if (playerNutriGems >= skinData.nutrigemsToUnlock)
            {
                // Show confirmation dialog
                ShowConfirmationDialog(skinData);
            }
            else
            {
                // Not enough gems - show error message
                ShowCustomErrorMessage("Not enough NutriGems!\nCannot proceed with purchase.");
            }
        }
    }

    private void OnLockedButtonClicked()
    {
        PlayButtonClickSound();

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData != null)
        {
            // Show task to unlock message
            ShowCustomErrorMessage(skinData.taskToUnlock);
        }
    }

    private void ShowConfirmationDialog(CharacterDatabase.SkinData skinData)
    {
        if (confirmationPanel != null && confirmationText != null)
        {
            confirmationText.text = $"Are you sure you want to buy {skinData.skinName} for {skinData.nutrigemsToUnlock} NutriGems?";
            confirmationPanel.SetActive(true);
        }
    }

    private void OnConfirmPurchaseYes()
    {
        PlayButtonClickSound();

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null || GameDataManager.Instance == null) return;

        // Check again if player still has enough NutriGems (in case something changed)
        if (GameDataManager.Instance.CurrentGameData.nutriGems < skinData.nutrigemsToUnlock) // CHANGED: Use nutriGems
        {
            ShowCustomErrorMessage("Not enough NutriGems!\nCannot proceed with purchase.");
            return;
        }

        // Deduct NutriGems (not NutriCoins)
        GameDataManager.Instance.CurrentGameData.nutriGems -= skinData.nutrigemsToUnlock; // CHANGED: Use nutriGems

        // Unlock the skin in game data - THIS SAVES IT TO PLAYER'S UNLOCKED SKINS
        UnlockSkin(currentCharacterData.characterID, selectedSkinID);

        // Save game data
        GameDataManager.Instance.SaveGameData();

        // Update UI
        UpdateAllButtonColors();
        UpdateActionButtons();
        UpdateCurrencyDisplays(); // NEW: Update currency displays after purchase

        // Hide confirmation dialog
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        // Show success message
        ShowCustomErrorMessage($"{skinData.skinName} unlocked!");
    }

    private void OnConfirmPurchaseNo()
    {
        PlayButtonClickSound();
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    private void OnBackButtonClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Skin Selection Back button clicked");

        // Exit skin selection
        ExitSkinSelection();

        // Hide the UI
        HideSkinSelectionUI();

        // Notify character controller
        if (characterSelectionController != null)
        {
            characterSelectionController.OnSkinSelectionClosed();
        }
    }

    private void SaveSkinSelection(int characterID, int skinID)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.SetSelectedSkinForCharacter(characterID, skinID);
            GameDataManager.Instance.SaveGameData();
            Debug.Log($"Saved skin {skinID} for character {characterID}");
        }
    }

    private void UpdateAllButtonColors()
    {
        foreach (GameObject button in skinButtons)
        {
            SkinButtonData buttonData = button.GetComponent<SkinButtonData>();
            if (buttonData == null) continue;

            UpdateButtonIconColor(button, buttonData.skinID, buttonData.isUnlocked);
        }
    }

    private void ShowLockedSkinMessage()
    {
        if (skinErrorCanvasGroup == null || skinErrorMessageText == null) return;

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData != null)
        {
            string message = $"{skinData.skinName} is locked!\n";

            if (skinData.isSkinReward)
            {
                message += $"Task: {skinData.taskToUnlock}";
            }
            else
            {
                message += $"Cost: {skinData.nutrigemsToUnlock} NutriGems"; // UPDATED: Say NutriGems
            }

            skinErrorMessageText.text = message;
        }

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }

        errorCoroutine = StartCoroutine(ShowSkinErrorCoroutine());
    }

    private void ShowCustomErrorMessage(string message)
    {
        if (skinErrorCanvasGroup == null || skinErrorMessageText == null) return;

        skinErrorMessageText.text = message;

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }

        errorCoroutine = StartCoroutine(ShowSkinErrorCoroutine());
    }

    private IEnumerator ShowSkinErrorCoroutine()
    {
        if (skinErrorCanvasGroup != null)
        {
            skinErrorCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(skinErrorCanvasGroup, 0f, 1f, errorFadeInDuration));
        }

        yield return new WaitForSeconds(errorDisplayDuration);

        if (skinErrorCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(skinErrorCanvasGroup, 1f, 0f, errorFadeOutDuration));
            skinErrorCanvasGroup.gameObject.SetActive(false);
        }

        errorCoroutine = null;
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

    private void ClearSkinButtons()
    {
        foreach (GameObject button in skinButtons)
        {
            if (button != null) Destroy(button);
        }
        skinButtons.Clear();

        // Force garbage collection to prevent memory leaks
        System.GC.Collect();
    }

    private void PlayButtonClickSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    public int GetSelectedSkinID()
    {
        return selectedSkinID;
    }

    public bool IsInSkinPreview()
    {
        return isInSkinPreview;
    }

    public void UnlockSkin(int characterID, int skinID)
    {
        if (GameDataManager.Instance != null)
        {
            // This calls GameData.UnlockSkinForCharacter which saves to unlockedSkinsForCharacter
            GameDataManager.Instance.CurrentGameData.UnlockSkinForCharacter(characterID, skinID);
            GameDataManager.Instance.SaveGameData();

            // Refresh the skin buttons to show the newly unlocked skin
            if (currentCharacterData != null && currentCharacterData.characterID == characterID)
            {
                PopulateSkinButtons(currentCharacterData);
            }
        }
    }
}

// Helper class to store skin button data
public class SkinButtonData : MonoBehaviour
{
    public int characterID;
    public int skinID;
    public bool isUnlocked;
    public bool isDefaultSkin; // Added to distinguish default skin card
    public Image skinIcon; // Added to store reference to the icon for color updates
}