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
    public Player_Data playerData;

    [Header("Environment Control")]
    public SkinEnvironmentController skinEnvironmentController;

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
    public TMP_Text lockedButtonText;
    public Button previewSelectButton;

    [Header("Confirmation Dialog")]
    public GameObject confirmationPanel;
    public TMP_Text confirmationText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Currency Displays")]
    public TMP_Text coinsText;
    public TMP_Text gemsText;

    [Header("Skin Card Colors")]
    public Color selectedColor = Color.white;
    public Color deselectedColor = new Color(0.588f, 0.588f, 0.588f, 1f);
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Default Skin Card")]
    public Sprite defaultSkinIcon;

    [Header("Feedback Panels")]
    public CanvasGroup errorFeedbackCanvasGroup;
    public TMP_Text errorFeedbackText;
    public CanvasGroup successFeedbackCanvasGroup;
    public TMP_Text successFeedbackText;
    public float feedbackFadeInDuration = 0.3f;
    public float feedbackDisplayDuration = 2f;
    public float feedbackFadeOutDuration = 0.5f;

    [Header("Power-Up Panels")]
    public GameObject powerUpPanel1;  // First power-up panel
    public GameObject powerUpPanel2;  // Second power-up panel
    public Image powerUpIcon1;
    public Image powerUpIcon2;
    public TMP_Text powerUpAmount1;
    public TMP_Text powerUpAmount2;

    [Header("Audio")]
    public AudioSource sfxAudioSource;
    public AudioClip successSound;
    public AudioClip errorSound;
    public AudioClip buttonClickSound;

    [Header("Camera Settings")]
    public CinemachineVirtualCamera characterSelectionCamera;
    public CinemachineVirtualCamera skinSelectionCamera;

    [Header("Canvas Settings")]
    public CanvasGroup characterSelectionCanvas;
    public CanvasGroup skinSelectionCanvas;
    public CanvasGroup characterControlsCanvas;

    [Header("Database References")]
    public IngredientDatabase ingredientDatabase; // Added for power-ups

    private List<GameObject> skinButtons = new List<GameObject>();
    private CharacterDatabase.CharacterData currentCharacterData;
    private int selectedSkinID = -1;
    private int lastSavedSkinID = -1;
    private bool isInSkinPreview = false;
    private Coroutine errorCoroutine;
    private Coroutine successCoroutine;
    private GameDataManager gameDataManager;

    void Start()
    {
        gameDataManager = GameDataManager.Instance;

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(false);
        }

        if (noSkinsText != null)
        {
            noSkinsText.SetActive(false);
        }

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

        if (errorFeedbackCanvasGroup != null)
        {
            errorFeedbackCanvasGroup.alpha = 0f;
            errorFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        if (successFeedbackCanvasGroup != null)
        {
            successFeedbackCanvasGroup.alpha = 0f;
            successFeedbackCanvasGroup.gameObject.SetActive(false);
        }

        // Hide power-up panels initially
        if (powerUpPanel1 != null) powerUpPanel1.SetActive(false);
        if (powerUpPanel2 != null) powerUpPanel2.SetActive(false);

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

        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            lastSavedSkinID = gameDataManager.GetSelectedSkin(characterID);

            // IMPORTANT: Always start with DEFAULT skin (-1)
            selectedSkinID = -1;

            Debug.Log($"=== ENTERING SKIN SELECTION ===");
            Debug.Log($"Character: {currentCharacterData.characterName} (ID: {characterID})");
            Debug.Log($"Last Saved Skin: {lastSavedSkinID}");

            var unlockedSkins = gameDataManager.GetUnlockedSkins(characterID);
            Debug.Log($"Unlocked Skins: {string.Join(", ", unlockedSkins)}");
        }
        else
        {
            Debug.LogError("GameDataManager or CurrentGameData is NULL!");
            lastSavedSkinID = -1;
            selectedSkinID = -1;
        }

        UpdateCharacterInfoDisplay(currentCharacterData);
        PopulateSkinButtons(currentCharacterData);

        isInSkinPreview = true;

        // Apply DEFAULT character visuals (NOT saved skin)
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);

            // Reset to default environment
            if (skinEnvironmentController != null)
            {
                skinEnvironmentController.OnDefaultSkinSelected(currentCharacterData.characterID);
            }
        }

        UpdateSkinNameDisplay();
        UpdateActionButtons();
        UpdateCurrencyDisplays();
        UpdatePowerUpPanels(); // Update power-up panels

        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(true);
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 30;
        }
    }

    // Update power-up panels based on equipped pets
    private void UpdatePowerUpPanels()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null || ingredientDatabase == null)
            return;

        // Get equipped pets
        string pet1 = gameDataManager.GetEquippedPet(1);
        string pet2 = gameDataManager.GetEquippedPet(2);

        int activePanels = 0;

        // Update first panel
        if (!string.IsNullOrEmpty(pet1))
        {
            UpdatePowerUpPanel(1, pet1);
            activePanels++;
        }

        // Update second panel
        if (!string.IsNullOrEmpty(pet2))
        {
            UpdatePowerUpPanel(2, pet2);
            activePanels++;
        }

        // Show/hide panels based on how many pets are equipped
        if (powerUpPanel1 != null)
            powerUpPanel1.SetActive(activePanels >= 1);

        if (powerUpPanel2 != null)
            powerUpPanel2.SetActive(activePanels >= 2);
    }

    // Update a specific power-up panel
    private void UpdatePowerUpPanel(int panelIndex, string petName)
    {
        if (string.IsNullOrEmpty(petName) || ingredientDatabase == null)
            return;

        var ingredient = ingredientDatabase.GetIngredientInfo(petName);
        if (ingredient == null || ingredient.powerUps == null || ingredient.powerUps.Count == 0)
            return;

        var powerUp = ingredient.powerUps[0]; // First power-up only

        Image iconImage = panelIndex == 1 ? powerUpIcon1 : powerUpIcon2;
        TMP_Text amountText = panelIndex == 1 ? powerUpAmount1 : powerUpAmount2;

        if (iconImage != null && powerUp.powerUpIcon != null)
            iconImage.sprite = powerUp.powerUpIcon;

        if (amountText != null)
        {
            string prefix = GetPowerUpPrefix(powerUp.powerUpType);
            amountText.text = $"{prefix}{powerUp.amount}";
        }
    }

    // Helper method to get the correct prefix based on power-up type
    private string GetPowerUpPrefix(IngredientDatabase.PowerUpInfo.PowerUpType type)
    {
        switch (type)
        {
            case IngredientDatabase.PowerUpInfo.PowerUpType.Time:
                return "-"; // Time is deducted/reduced
            case IngredientDatabase.PowerUpInfo.PowerUpType.Heart:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Speed:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Coins:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Exp:
            case IngredientDatabase.PowerUpInfo.PowerUpType.Gems:
            default:
                return "+"; // All others are added/increased
        }
    }

    public void ResetToDefaultSkin()
    {
        Debug.Log("SkinSelectionController: Resetting to default skin");

        selectedSkinID = -1;

        if (characterVisualSwapper != null && currentCharacterData != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
        }

        if (skinEnvironmentController != null && currentCharacterData != null)
        {
            skinEnvironmentController.OnDefaultSkinSelected(currentCharacterData.characterID);
        }

        UpdateAllButtonColors();
        UpdateSkinNameDisplay();
        UpdateActionButtons();
    }

    public void ExitSkinSelection()
    {
        ClearSkinButtons();
        isInSkinPreview = false;

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        currentCharacterData = null;
        HideAllActionButtons();
    }

    private void UpdateCurrencyDisplays()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        if (coinsText != null)
        {
            coinsText.text = gameDataManager.CurrentGameData.nutriCoins.ToString();
        }

        if (gemsText != null)
        {
            gemsText.text = gameDataManager.CurrentGameData.nutriGems.ToString();
        }

        if (playerData != null)
        {
            playerData.UpdateCoinDisplayImmediate();
            playerData.UpdateGemDisplayImmediate();
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
            skinSelectionCamera.Priority = 0;
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
            if (selectedSkinID == -1)
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

            // Create default skin button
            CreateDefaultSkinButton(characterData);

            // Create skin buttons for each skin
            if (characterData.skins != null && characterData.skins.Count > 0)
            {
                foreach (var skinData in characterData.skins)
                {
                    CreateSkinButton(characterData, skinData);
                }
            }
        }

        // IMPORTANT: Set default skin as selected and update colors
        selectedSkinID = -1;
        UpdateAllButtonColors();
    }

    private void CreateDefaultSkinButton(CharacterDatabase.CharacterData characterData)
    {
        if (skinButtonPrefab == null || skinHorizontalLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinHorizontalLayout.transform);
        skinButtons.Add(buttonObj);

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

        Image skinIcon = FindSkinIcon(buttonObj.transform);
        if (skinIcon != null)
        {
            skinIcon.sprite = characterData.characterIcon ?? defaultSkinIcon;
        }

        TMP_Text skinNameText = FindSkinNameText(buttonObj.transform);
        if (skinNameText != null)
        {
            skinNameText.text = characterData.characterName;
            skinNameText.color = Color.white;
        }

        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null)
            buttonData = buttonObj.AddComponent<SkinButtonData>();

        buttonData.characterID = characterData.characterID;
        buttonData.skinID = -1;
        buttonData.isDefaultSkin = true;
        buttonData.skinIcon = skinIcon;
    }

    private void CreateSkinButton(CharacterDatabase.CharacterData characterData, CharacterDatabase.SkinData skinData)
    {
        if (skinButtonPrefab == null || skinHorizontalLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinHorizontalLayout.transform);
        skinButtons.Add(buttonObj);

        // IMPORTANT: Check unlock status EVERY TIME we create the button
        bool isUnlocked = IsSkinUnlocked(characterData.characterID, skinData.skinID);

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                PlayButtonClickSound();
                OnSkinButtonClicked(skinData);
            });
        }

        Image skinIcon = FindSkinIcon(buttonObj.transform);
        if (skinIcon != null)
        {
            skinIcon.sprite = skinData.skinIcon ?? defaultSkinIcon;
        }

        TMP_Text skinNameText = FindSkinNameText(buttonObj.transform);
        if (skinNameText != null)
        {
            skinNameText.text = skinData.skinName;
            skinNameText.color = isUnlocked ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null)
            buttonData = buttonObj.AddComponent<SkinButtonData>();

        buttonData.characterID = characterData.characterID;
        buttonData.skinID = skinData.skinID;
        buttonData.isDefaultSkin = false;
        buttonData.skinIcon = skinIcon;

        Debug.Log($"Created skin button for {skinData.skinName} (ID: {skinData.skinID}) - Unlocked: {isUnlocked}");
    }

    private void UpdateAllButtonColors()
    {
        foreach (var buttonObj in skinButtons)
        {
            if (buttonObj == null) continue;

            SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
            if (buttonData == null || buttonData.skinIcon == null) continue;

            bool isSelected = (selectedSkinID == buttonData.skinID);

            // IMPORTANT: Check unlock status LIVE for non-default skins
            bool isUnlocked = buttonData.isDefaultSkin ? true : IsSkinUnlocked(buttonData.characterID, buttonData.skinID);

            if (isSelected)
            {
                buttonData.skinIcon.color = selectedColor;
            }
            else if (!isUnlocked)
            {
                buttonData.skinIcon.color = lockedColor;
            }
            else
            {
                buttonData.skinIcon.color = deselectedColor;
            }
        }
    }

    private Image FindSkinIcon(Transform parent)
    {
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

    // CRITICAL: This method MUST check GameData ONLY, not the database unlock field
    private bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (skinID == -1) return true;

        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
        {
            Debug.LogError("GameDataManager or CurrentGameData is NULL!");
            return false;
        }

        // ONLY check GameData, NEVER check the database unlock field
        bool isUnlockedInGameData = gameDataManager.IsSkinUnlocked(characterID, skinID);

        Debug.Log($"Checking if skin {skinID} for character {characterID} is unlocked: {isUnlockedInGameData}");

        return isUnlockedInGameData;
    }

    private void OnDefaultSkinButtonClicked(CharacterDatabase.CharacterData characterData)
    {
        Debug.Log("Default skin button clicked");
        selectedSkinID = -1;

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
        }

        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnDefaultSkinSelected(currentCharacterData.characterID);
        }

        UpdateAllButtonColors();
        UpdateSkinNameDisplay();
        UpdateActionButtons();

        if (characterData.selectionSound != null)
        {
            AudioSource.PlayClipAtPoint(characterData.selectionSound, Camera.main.transform.position);
        }
    }

    private void OnSkinButtonClicked(CharacterDatabase.SkinData skinData)
    {
        Debug.Log($"Skin button clicked: {skinData.skinName} (ID: {skinData.skinID})");
        selectedSkinID = skinData.skinID;

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
        }

        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnSkinSelected(
                currentCharacterData.characterID,
                skinData.skinID,
                skinData.skinName
            );
        }

        UpdateAllButtonColors();
        UpdateSkinNameDisplay();
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
            // Default skin always shows select button
            if (selectButton != null) selectButton.SetActive(true);
            Debug.Log("Default skin - showing SELECT button");
            return;
        }

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null) return;

        // IMPORTANT: Check unlock status LIVE
        bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);
        Debug.Log($"Updating action buttons for skin {selectedSkinID}: isUnlocked = {isUnlocked}");

        if (isUnlocked)
        {
            // Skin is unlocked - show SELECT button
            if (selectButton != null)
            {
                selectButton.SetActive(true);
                Debug.Log("Showing SELECT button (skin unlocked)");
            }
        }
        else
        {
            // Skin is locked
            if (skinData.isSkinReward)
            {
                // Reward skin - show locked button with task
                if (lockedButton != null)
                {
                    lockedButton.SetActive(true);
                    if (lockedButtonText != null)
                        lockedButtonText.text = skinData.taskToUnlock;
                    Debug.Log("Showing LOCKED button (reward skin)");
                }
            }
            else
            {
                // Purchasable skin - show buy button with price
                if (buyButton != null)
                {
                    buyButton.SetActive(true);
                    if (buyButtonText != null)
                        buyButtonText.text = $"{skinData.nutrigemsToUnlock}";
                    Debug.Log("Showing BUY button (purchasable skin)");
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
        if (previewSelectButton != null && previewSelectButton.gameObject != selectButton)
            previewSelectButton.gameObject.SetActive(false);
    }

    private void OnSelectButtonClicked()
    {
        PlayButtonClickSound();

        Debug.Log($"Select button clicked. Selected Skin: {selectedSkinID}");

        if (selectedSkinID != -1)
        {
            bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);
            if (!isUnlocked)
            {
                ShowLockedSkinMessage();
                return;
            }
        }

        HideAllFeedback();

        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnExitSkinSelection();
        }

        // Save the skin selection
        gameDataManager.SetSelectedSkin(currentCharacterData.characterID, selectedSkinID);
        lastSavedSkinID = selectedSkinID;

        if (characterVisualSwapper != null)
        {
            if (selectedSkinID == -1)
            {
                characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
            }
            else
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(selectedSkinID);
            }
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
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

        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            int playerNutriGems = gameDataManager.CurrentGameData.nutriGems;
            Debug.Log($"Player has {playerNutriGems} gems, skin costs {skinData.nutrigemsToUnlock}");

            if (playerNutriGems >= skinData.nutrigemsToUnlock)
            {
                ShowConfirmationDialog(skinData);
            }
            else
            {
                ShowErrorMessage($"Not enough NutriGems!\nYou need {skinData.nutrigemsToUnlock} NutriGems to unlock this skin.");
                // Play error sound
                PlayErrorSound();
            }
        }
    }

    private void OnLockedButtonClicked()
    {
        PlayButtonClickSound();

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData != null)
        {
            ShowErrorMessage(skinData.taskToUnlock);
            // Play error sound
            PlayErrorSound();
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

    // FIXED: This method now properly saves to GameData and updates UI
    private void OnConfirmPurchaseYes()
    {
        PlayButtonClickSound();

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null || gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        if (gameDataManager.CurrentGameData.nutriGems < skinData.nutrigemsToUnlock)
        {
            ShowErrorMessage("Not enough NutriGems!\nCannot proceed with purchase.");
            PlayErrorSound();
            return;
        }

        // Deduct gems
        gameDataManager.CurrentGameData.nutriGems -= skinData.nutrigemsToUnlock;
        Debug.Log($"Deducted {skinData.nutrigemsToUnlock} gems. New balance: {gameDataManager.CurrentGameData.nutriGems}");

        // CRITICAL: Unlock the skin in GameData
        gameDataManager.UnlockSkin(currentCharacterData.characterID, selectedSkinID);

        // Save immediately
        gameDataManager.SaveGameData();

        Debug.Log($"GameData saved. Skin {selectedSkinID} should now be unlocked");

        // FIX: Update the colors of existing buttons instead of repopulating
        UpdateAllButtonColors();

        // Update the action buttons to show SELECT
        UpdateActionButtons();

        // Update currency display
        UpdateCurrencyDisplays();

        // Verify unlock
        bool isNowUnlocked = gameDataManager.IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);
        Debug.Log($"Verification - Skin {selectedSkinID} is now unlocked: {isNowUnlocked}");

        ShowSuccessMessage($"{skinData.skinName} unlocked successfully!");
        // Play success sound
        PlaySuccessSound();

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
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

        HideAllFeedback();

        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnExitSkinSelection();
        }

        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
        }

        ExitSkinSelection();
        HideSkinSelectionUI();

        if (characterSelectionController != null)
        {
            characterSelectionController.OnSkinSelectionClosed();
        }
    }

    private void ShowLockedSkinMessage()
    {
        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData != null)
        {
            string message = $"{skinData.skinName} is locked!\n";
            message += skinData.isSkinReward ? $"Task: {skinData.taskToUnlock}" : $"Cost: {skinData.nutrigemsToUnlock} NutriGems";
            ShowErrorMessage(message);
            // Play error sound
            PlayErrorSound();
        }
    }

    private void ShowErrorMessage(string message)
    {
        if (errorFeedbackCanvasGroup == null || errorFeedbackText == null) return;

        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
        }
        if (successCoroutine != null)
        {
            StopCoroutine(successCoroutine);
            HideFeedbackImmediate(successFeedbackCanvasGroup);
        }

        errorFeedbackText.text = message;
        errorCoroutine = StartCoroutine(ShowFeedbackCoroutine(errorFeedbackCanvasGroup, feedbackDisplayDuration));
    }

    private void ShowSuccessMessage(string message)
    {
        if (successFeedbackCanvasGroup == null || successFeedbackText == null) return;

        if (successCoroutine != null)
        {
            StopCoroutine(successCoroutine);
        }
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            HideFeedbackImmediate(errorFeedbackCanvasGroup);
        }

        successFeedbackText.text = message;
        successCoroutine = StartCoroutine(ShowFeedbackCoroutine(successFeedbackCanvasGroup, feedbackDisplayDuration));
    }

    private IEnumerator ShowFeedbackCoroutine(CanvasGroup canvasGroup, float displayDuration)
    {
        canvasGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, feedbackFadeInDuration));
        yield return new WaitForSeconds(displayDuration);
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, feedbackFadeOutDuration));
        canvasGroup.gameObject.SetActive(false);
    }

    private void HideFeedbackImmediate(CanvasGroup canvasGroup)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void HideAllFeedback()
    {
        if (errorCoroutine != null)
        {
            StopCoroutine(errorCoroutine);
            errorCoroutine = null;
        }
        if (successCoroutine != null)
        {
            StopCoroutine(successCoroutine);
            successCoroutine = null;
        }
        HideFeedbackImmediate(errorFeedbackCanvasGroup);
        HideFeedbackImmediate(successFeedbackCanvasGroup);
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
    }

    private void PlayButtonClickSound()
    {
        if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    // Method to play error sound
    private void PlayErrorSound()
    {
        if (sfxAudioSource != null && errorSound != null)
        {
            sfxAudioSource.PlayOneShot(errorSound);
        }
    }

    // Method to play success sound
    private void PlaySuccessSound()
    {
        if (sfxAudioSource != null && successSound != null)
        {
            sfxAudioSource.PlayOneShot(successSound);
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
}

public class SkinButtonData : MonoBehaviour
{
    public int characterID;
    public int skinID;
    public bool isDefaultSkin;
    public Image skinIcon;
}