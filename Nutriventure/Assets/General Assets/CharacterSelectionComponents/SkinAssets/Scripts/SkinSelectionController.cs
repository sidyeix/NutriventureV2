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
    private int selectedSkinID = -1;
    private int lastSavedSkinID = -1;
    private bool isInSkinPreview = false;
    private Coroutine errorCoroutine;
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
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            lastSavedSkinID = gameDataManager.CurrentGameData.GetSelectedSkinForCharacter(characterID);
            selectedSkinID = lastSavedSkinID;
            Debug.Log($"EnterSkinSelection: CharID={characterID}, SavedSkinID={lastSavedSkinID}");
        }
        else
        {
            lastSavedSkinID = -1;
            selectedSkinID = -1;
        }

        UpdateCharacterInfoDisplay(currentCharacterData);
        PopulateSkinButtons(currentCharacterData);

        isInSkinPreview = true;

        // Apply the character visuals with saved skin
        if (characterVisualSwapper != null)
        {
            if (selectedSkinID == -1)
            {
                characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);

                // NOTIFY ENVIRONMENT CONTROLLER - DEFAULT SKIN SELECTED
                if (skinEnvironmentController != null)
                {
                    skinEnvironmentController.OnDefaultSkinSelected(currentCharacterData.characterID);
                }
            }
            else
            {
                characterVisualSwapper.ApplySkinToCurrentCharacter(selectedSkinID);

                // Get skin data for environment notification
                var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
                if (skinData != null && skinEnvironmentController != null)
                {
                    skinEnvironmentController.OnSkinSelected(
                        currentCharacterData.characterID,
                        selectedSkinID,
                        skinData.skinName
                    );
                }
            }
        }

        UpdateSkinNameDisplay();
        UpdateActionButtons();
        UpdateCurrencyDisplays();

        if (skinSelectionPanel != null)
        {
            skinSelectionPanel.SetActive(true);
        }

        // Set skin camera priority to 30
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 30;
        }
    }

    public void ExitSkinSelection()
    {
        ClearSkinButtons();
        isInSkinPreview = false;

        // Reset camera priority to 0
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
            Debug.Log("Skin camera priority set to 0 on exit");
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

            // CREATE DEFAULT SKIN CARD
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

        Image skinIcon = FindSkinIcon(buttonObj.transform);
        if (skinIcon != null)
        {
            skinIcon.sprite = characterData.characterIcon ?? defaultSkinIcon;
            UpdateButtonIconColor(buttonObj, -1, isUnlocked);
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
        buttonData.isUnlocked = true;
        buttonData.isDefaultSkin = true;
        buttonData.skinIcon = skinIcon;
    }

    private void CreateSkinButton(CharacterDatabase.CharacterData characterData, CharacterDatabase.SkinData skinData)
    {
        if (skinButtonPrefab == null || skinHorizontalLayout == null) return;

        GameObject buttonObj = Instantiate(skinButtonPrefab, skinHorizontalLayout.transform);
        skinButtons.Add(buttonObj);

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
            UpdateButtonIconColor(buttonObj, skinData.skinID, isUnlocked);
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
        buttonData.isUnlocked = isUnlocked;
        buttonData.isDefaultSkin = false;
        buttonData.skinIcon = skinIcon;
    }

    private void UpdateButtonIconColor(GameObject buttonObj, int skinID, bool isUnlocked)
    {
        SkinButtonData buttonData = buttonObj.GetComponent<SkinButtonData>();
        if (buttonData == null || buttonData.skinIcon == null) return;

        bool isSelected = (selectedSkinID == skinID);

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

    private bool IsSkinUnlocked(int characterID, int skinID)
    {
        if (skinID == -1) return true;

        var skinData = characterDatabase.GetSkinByID(characterID, skinID);
        if (skinData != null && skinData.unlock) return true;

        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            return gameDataManager.CurrentGameData.IsSkinUnlocked(characterID, skinID);
        }

        return false;
    }

    private void OnDefaultSkinButtonClicked(CharacterDatabase.CharacterData characterData)
    {
        selectedSkinID = -1;

        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplyCharacterVisuals(currentCharacterData);
        }

        // NOTIFY ENVIRONMENT CONTROLLER - DEFAULT SKIN SELECTED
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
        selectedSkinID = skinData.skinID;

        // Preview the skin (even if locked)
        if (characterVisualSwapper != null)
        {
            characterVisualSwapper.ApplySkinToCurrentCharacter(skinData.skinID);
        }

        // NOTIFY ENVIRONMENT CONTROLLER - SKIN SELECTED
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
            if (selectButton != null) selectButton.SetActive(true);
            return;
        }

        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData == null) return;

        bool isUnlocked = IsSkinUnlocked(currentCharacterData.characterID, selectedSkinID);

        if (isUnlocked)
        {
            if (selectButton != null) selectButton.SetActive(true);
        }
        else
        {
            if (skinData.isSkinReward)
            {
                if (lockedButton != null)
                {
                    lockedButton.SetActive(true);
                    if (lockedButtonText != null)
                        lockedButtonText.text = skinData.taskToUnlock;
                }
            }
            else
            {
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

        // NOTIFY ENVIRONMENT CONTROLLER - EXITING (before saving)
        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnExitSkinSelection();
        }

        // Save the skin selection
        SaveSkinSelection(currentCharacterData.characterID, selectedSkinID);
        lastSavedSkinID = selectedSkinID;

        // Update character visual with the selected skin
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

        // Reset camera priority to 0
        if (skinSelectionCamera != null)
        {
            skinSelectionCamera.Priority = 0;
            Debug.Log("Skin camera priority set to 0 on select");
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

            if (playerNutriGems >= skinData.nutrigemsToUnlock)
            {
                ShowConfirmationDialog(skinData);
            }
            else
            {
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
        if (skinData == null || gameDataManager == null || gameDataManager.CurrentGameData == null) return;

        if (gameDataManager.CurrentGameData.nutriGems < skinData.nutrigemsToUnlock)
        {
            ShowCustomErrorMessage("Not enough NutriGems!\nCannot proceed with purchase.");
            return;
        }

        gameDataManager.CurrentGameData.nutriGems -= skinData.nutrigemsToUnlock;
        UnlockSkin(currentCharacterData.characterID, selectedSkinID);
        gameDataManager.SaveGameData();

        UpdateAllButtonColors();
        UpdateActionButtons();
        UpdateCurrencyDisplays();

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
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

        // NOTIFY ENVIRONMENT CONTROLLER - EXITING
        if (skinEnvironmentController != null)
        {
            skinEnvironmentController.OnExitSkinSelection();
        }

        // Reset camera priority to 0
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

    private void SaveSkinSelection(int characterID, int skinID)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.SetSelectedSkinForCharacter(characterID, skinID);
            gameDataManager.SaveGameData();
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
        var skinData = characterDatabase.GetSkinByID(currentCharacterData.characterID, selectedSkinID);
        if (skinData != null)
        {
            string message = $"{skinData.skinName} is locked!\n";
            message += skinData.isSkinReward ? $"Task: {skinData.taskToUnlock}" : $"Cost: {skinData.nutrigemsToUnlock} NutriGems";
            ShowCustomErrorMessage(message);
        }
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
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.UnlockSkinForCharacter(characterID, skinID);
            gameDataManager.SaveGameData();

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
    public bool isDefaultSkin;
    public Image skinIcon;
}