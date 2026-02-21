using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ProfileSettings : MonoBehaviour
{
    [Header("Main References")]
    public GameObject canvas;
    public Player_Data playerData;
    public GameDataManager gameDataManager;
    public ProfileIconDatabase iconDatabase;
    public FrameDatabase frameDatabase;
    public AchievementDatabase achievementDatabase;

    [Header("Profile Display")]
    public Image currentProfileIcon;
    public Image currentFrameImage;

    [Header("Name Editing")]
    public TMP_InputField nameInputField;
    public Button editNameButton;

    [Header("Save Button")]
    public Button saveButton;
    public float saveButtonFadeDuration = 0.3f;

    [Header("Level & XP")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;
    public Slider xpSlider;
    public string levelFormat = "Level: {0}";
    public string xpFormat = "{0}/{1}";

    [Header("Navigation")]
    public Button profileIconsButton;
    public Button framesButton;
    public Button achievementsButton;

    [Header("Navigation Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = new Color(0.48f, 0.34f, 0.34f, 1f);

    [Header("Icon Grid")]
    public Transform iconGridParent;
    public GameObject iconButtonPrefab;
    public Vector2 iconCellSize = new Vector2(370, 450);

    [Header("Frame Grid")]
    public GameObject frameButtonPrefab;
    public Vector2 frameCellSize = new Vector2(370, 380);

    [Header("Achievement Grid")]
    public GameObject achievementButtonPrefab;
    public Vector2 achievementCellSize = new Vector2(370, 450);

    [Header("Achievement Info Panel")]
    public GameObject achievementInfoPanel;
    public CanvasGroup achievementInfoCanvasGroup;
    public float panelFadeDuration = 0.25f;
    public TextMeshProUGUI achievementNameText;
    public TextMeshProUGUI achievementDescriptionText;
    public TextMeshProUGUI achievementStatusText;
    public TextMeshProUGUI achievementPrizeText;
    public Image achievementIconImage;
    public Button achievementInfoCloseButton;
    public Button achievementInfoClaimButton;

    [Header("Close Dialog")]
    public GameObject dialogPanel;
    public Button closeButton;
    public Button dialogYesButton;
    public Button dialogNoButton;

    // Private variables
    private string originalName;
    private string originalIconId;
    private string originalFrameId;
    private bool hasChanges = false;
    private bool hasInitializedDefaults = false;

    // Icon selection tracking
    private List<ProfileIconButton> iconButtons = new List<ProfileIconButton>();
    private ProfileIconButton currentlySelectedIconButton;

    // Frame selection tracking
    private List<FrameButton> frameButtons = new List<FrameButton>();
    private FrameButton currentlySelectedFrameButton;

    // Achievement tracking
    private List<AchievementButton> achievementButtons = new List<AchievementButton>();
    private AchievementDatabase.AchievementData currentlySelectedAchievement;

    // Current view state
    private enum ViewMode { Icons, Frames, Achievements }
    private ViewMode currentView = ViewMode.Icons;
    private GridLayoutGroup gridLayout;

    // Store original button colors
    private Color originalProfileIconColor;
    private Color originalFramesColor;
    private Color originalAchievementsColor;

    // Save button fade animation
    private CanvasGroup saveButtonCanvasGroup;
    private Coroutine saveButtonFadeCoroutine;

    // Panel fade animation
    private Coroutine panelFadeCoroutine;

    // Main canvas fade
    private CanvasGroup mainCanvasGroup;

    private void Awake()
    {
        // Initialize default icons and frames once when the game starts
        if (!hasInitializedDefaults && gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            InitializeDefaultItems();
            hasInitializedDefaults = true;
        }
    }

    private void Start()
    {
        // Initialize UI state
        nameInputField.interactable = false;
        dialogPanel.SetActive(false);

        // Setup main canvas group for fade transitions
        if (canvas != null)
        {
            mainCanvasGroup = canvas.GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = canvas.AddComponent<CanvasGroup>();
            }
            mainCanvasGroup.alpha = 0f;
        }

        // Setup achievement info panel
        if (achievementInfoPanel != null)
        {
            achievementInfoCanvasGroup = achievementInfoPanel.GetComponent<CanvasGroup>();
            if (achievementInfoCanvasGroup == null)
            {
                achievementInfoCanvasGroup = achievementInfoPanel.AddComponent<CanvasGroup>();
            }
            achievementInfoPanel.SetActive(false);
            achievementInfoCanvasGroup.alpha = 0f;
        }

        // Setup save button for fade transitions
        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(false);
            saveButtonCanvasGroup = saveButton.GetComponent<CanvasGroup>();
            if (saveButtonCanvasGroup == null)
            {
                saveButtonCanvasGroup = saveButton.gameObject.AddComponent<CanvasGroup>();
            }
            saveButtonCanvasGroup.alpha = 0f;

            // Add both listeners
            saveButton.onClick.AddListener(PlayButtonSound);
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        // Store original button colors
        if (profileIconsButton != null)
            originalProfileIconColor = profileIconsButton.GetComponent<Image>().color;
        if (framesButton != null)
            originalFramesColor = framesButton.GetComponent<Image>().color;
        if (achievementsButton != null)
            originalAchievementsColor = achievementsButton.GetComponent<Image>().color;

        // Get grid layout component
        if (iconGridParent != null)
        {
            gridLayout = iconGridParent.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = iconGridParent.gameObject.AddComponent<GridLayoutGroup>();
            }
        }

        // Setup listeners with click sounds
        if (editNameButton != null)
        {
            editNameButton.onClick.AddListener(OnEditNameClicked);
            editNameButton.onClick.AddListener(PlayButtonSound);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            closeButton.onClick.AddListener(PlayButtonSound);
        }

        if (dialogYesButton != null)
        {
            dialogYesButton.onClick.AddListener(OnDialogYes);
            dialogYesButton.onClick.AddListener(PlayButtonSound);
        }

        if (dialogNoButton != null)
        {
            dialogNoButton.onClick.AddListener(OnDialogNo);
            dialogNoButton.onClick.AddListener(PlayButtonSound);
        }

        // Navigation buttons with click sounds
        if (profileIconsButton != null)
        {
            profileIconsButton.onClick.AddListener(OnProfileIconsClicked);
            profileIconsButton.onClick.AddListener(PlayButtonSound);
        }

        if (framesButton != null)
        {
            framesButton.onClick.AddListener(OnFramesClicked);
            framesButton.onClick.AddListener(PlayButtonSound);
        }

        if (achievementsButton != null)
        {
            achievementsButton.onClick.AddListener(OnAchievementsClicked);
            achievementsButton.onClick.AddListener(PlayButtonSound);
        }

        // Achievement info panel buttons
        if (achievementInfoCloseButton != null)
        {
            achievementInfoCloseButton.onClick.AddListener(OnAchievementInfoCloseClicked);
            achievementInfoCloseButton.onClick.AddListener(PlayButtonSound);
        }

        if (achievementInfoClaimButton != null)
        {
            achievementInfoClaimButton.onClick.AddListener(OnAchievementInfoClaimClicked);
            achievementInfoClaimButton.onClick.AddListener(PlayButtonSound);
        }

        // Add listener for name input field changes
        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(OnNameChanged);
    }

    private void PlayButtonSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    private void InitializeDefaultItems()
    {
        if (gameDataManager?.CurrentGameData == null) return;

        Debug.Log("=== INITIALIZING DEFAULT ITEMS FROM PROFILE SETTINGS ===");

        if (iconDatabase != null)
        {
            List<string> defaultIconIds = new List<string>();
            foreach (var icon in iconDatabase.icons)
            {
                if (icon.unlockedByDefault)
                {
                    defaultIconIds.Add(icon.id);
                    Debug.Log($"Found default icon: {icon.id}");
                }
            }

            foreach (string iconId in defaultIconIds)
            {
                if (!gameDataManager.CurrentGameData.unlockedIconIds.Contains(iconId))
                {
                    gameDataManager.CurrentGameData.unlockedIconIds.Add(iconId);
                    Debug.Log($"Added default icon to GameData: {iconId}");
                }
            }

            if (string.IsNullOrEmpty(gameDataManager.CurrentGameData.equippedIconId) && defaultIconIds.Count > 0)
            {
                gameDataManager.CurrentGameData.equippedIconId = defaultIconIds[0];
                Debug.Log($"Set default equipped icon to: {defaultIconIds[0]}");
            }
        }

        if (frameDatabase != null)
        {
            List<string> defaultFrameIds = new List<string>();
            foreach (var frame in frameDatabase.frames)
            {
                if (frame.unlockedByDefault)
                {
                    defaultFrameIds.Add(frame.id);
                    Debug.Log($"Found default frame: {frame.id}");
                }
            }

            foreach (string frameId in defaultFrameIds)
            {
                if (!gameDataManager.CurrentGameData.unlockedFrameIds.Contains(frameId))
                {
                    gameDataManager.CurrentGameData.unlockedFrameIds.Add(frameId);
                    Debug.Log($"Added default frame to GameData: {frameId}");
                }
            }

            if (string.IsNullOrEmpty(gameDataManager.CurrentGameData.equippedFrameId) && defaultFrameIds.Count > 0)
            {
                gameDataManager.CurrentGameData.equippedFrameId = defaultFrameIds[0];
                Debug.Log($"Set default equipped frame to: {defaultFrameIds[0]}");
            }
        }

        gameDataManager.SaveGameData();
        Debug.Log("=== DEFAULT ITEMS INITIALIZATION COMPLETE ===");
    }

    public void OpenProfileSettings()
    {
        if (gameDataManager?.CurrentGameData == null) return;

        var gameData = gameDataManager.CurrentGameData;

        originalName = gameData.playerName;
        originalIconId = gameData.equippedIconId;
        originalFrameId = gameData.equippedFrameId;

        RefreshProfileDisplay();
        RefreshLevelAndXP();

        ShowIconsView();
        HighlightNavigationButton(ViewMode.Icons);

        hasChanges = false;
        nameInputField.interactable = false;
        dialogPanel.SetActive(false);

        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(false);
            if (saveButtonCanvasGroup != null)
                saveButtonCanvasGroup.alpha = 0f;
        }

        canvas.SetActive(true);
        FadeMainCanvas(1f, panelFadeDuration);

        PlayButtonSound();
    }

    private void CloseProfileSettings()
    {
        StartCoroutine(CloseProfileSettingsCoroutine());
    }

    private IEnumerator CloseProfileSettingsCoroutine()
    {
        yield return StartCoroutine(FadeCanvasGroup(mainCanvasGroup, 0f, panelFadeDuration));
        canvas.SetActive(false);
    }

    private void FadeMainCanvas(float targetAlpha, float duration)
    {
        if (mainCanvasGroup == null) return;

        if (targetAlpha > 0 && !canvas.activeSelf)
        {
            canvas.SetActive(true);
        }

        StartCoroutine(FadeCanvasGroup(mainCanvasGroup, targetAlpha, duration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void RefreshProfileDisplay()
    {
        var gameData = gameDataManager.CurrentGameData;

        nameInputField.text = gameData.playerName;

        Sprite iconSprite = iconDatabase.GetIconSprite(gameData.equippedIconId);
        if (iconSprite != null)
            currentProfileIcon.sprite = iconSprite;

        Sprite frameSprite = frameDatabase.GetFrameSprite(gameData.equippedFrameId);
        if (frameSprite != null)
            currentFrameImage.sprite = frameSprite;
    }

    private void RefreshLevelAndXP()
    {
        var gameData = gameDataManager.CurrentGameData;

        levelText.text = string.Format(levelFormat, gameData.playerLevel);
        xpText.text = string.Format(xpFormat, gameData.currentXP, gameData.xpToNextLevel);

        xpSlider.maxValue = gameData.xpToNextLevel;
        xpSlider.value = gameData.currentXP;
    }

    private void HighlightNavigationButton(ViewMode activeView)
    {
        if (profileIconsButton != null)
            profileIconsButton.GetComponent<Image>().color = normalButtonColor;
        if (framesButton != null)
            framesButton.GetComponent<Image>().color = normalButtonColor;
        if (achievementsButton != null)
            achievementsButton.GetComponent<Image>().color = normalButtonColor;

        switch (activeView)
        {
            case ViewMode.Icons:
                if (profileIconsButton != null)
                    profileIconsButton.GetComponent<Image>().color = selectedButtonColor;
                break;
            case ViewMode.Frames:
                if (framesButton != null)
                    framesButton.GetComponent<Image>().color = selectedButtonColor;
                break;
            case ViewMode.Achievements:
                if (achievementsButton != null)
                    achievementsButton.GetComponent<Image>().color = selectedButtonColor;
                break;
        }
    }

    private void OnProfileIconsClicked()
    {
        ShowIconsView();
        HighlightNavigationButton(ViewMode.Icons);
    }

    private void OnFramesClicked()
    {
        ShowFramesView();
        HighlightNavigationButton(ViewMode.Frames);
    }

    private void OnAchievementsClicked()
    {
        ShowAchievementsView();
        HighlightNavigationButton(ViewMode.Achievements);
    }

    private void ShowIconsView()
    {
        currentView = ViewMode.Icons;

        if (gridLayout != null)
        {
            gridLayout.cellSize = iconCellSize;
        }

        ClearGrid();
        RefreshIconGrid();
    }

    private void ShowFramesView()
    {
        currentView = ViewMode.Frames;

        if (gridLayout != null)
        {
            gridLayout.cellSize = frameCellSize;
        }

        ClearGrid();
        RefreshFrameGrid();
    }

    private void ShowAchievementsView()
    {
        currentView = ViewMode.Achievements;

        if (gridLayout != null)
        {
            gridLayout.cellSize = achievementCellSize;
        }

        ClearGrid();
        RefreshAchievementGrid();
    }

    private void ClearGrid()
    {
        foreach (Transform child in iconGridParent)
        {
            Destroy(child.gameObject);
        }
        iconButtons.Clear();
        frameButtons.Clear();
        achievementButtons.Clear();
    }

    private void RefreshIconGrid()
    {
        var gameData = gameDataManager.CurrentGameData;
        List<ProfileIconDatabase.ProfileIcon> allIcons = iconDatabase.icons;

        Debug.Log("=== REFRESHING ICON GRID ===");
        Debug.Log($"Player unlocked icons: {string.Join(", ", gameData.unlockedIconIds)}");

        foreach (var icon in allIcons)
        {
            // Check if icon is unlocked in GameData by ID OR by name
            bool isUnlockedById = gameData.unlockedIconIds.Contains(icon.id);
            bool isUnlockedByName = gameData.unlockedIconIds.Contains(icon.iconName);
            bool isUnlockedInGameData = isUnlockedById || isUnlockedByName;

            // Check if icon is unlocked by default in database
            bool isDefaultUnlock = icon.unlockedByDefault;

            // Combined unlock state
            bool isLocked = !(isUnlockedInGameData || isDefaultUnlock);
            bool isSelected = (!isLocked && icon.id == gameData.equippedIconId);

            Debug.Log($"Icon: {icon.iconName} (ID: {icon.id})");
            Debug.Log($"  - Default Unlock: {isDefaultUnlock}");
            Debug.Log($"  - In GameData by ID: {isUnlockedById}");
            Debug.Log($"  - In GameData by Name: {isUnlockedByName}");
            Debug.Log($"  - Final Locked State: {isLocked}");
            Debug.Log($"  - Selected: {isSelected}");

            GameObject buttonObj = Instantiate(iconButtonPrefab, iconGridParent);
            ProfileIconButton iconButton = buttonObj.GetComponent<ProfileIconButton>();

            iconButton.Initialize(icon, OnIconSelected, isLocked, isSelected);

            if (isSelected)
            {
                currentlySelectedIconButton = iconButton;
            }

            iconButtons.Add(iconButton);
        }
        Debug.Log("=== ICON GRID REFRESH COMPLETE ===");
    }

    private void RefreshFrameGrid()
    {
        var gameData = gameDataManager.CurrentGameData;
        List<FrameDatabase.FrameData> allFrames = frameDatabase.frames;

        Debug.Log("=== REFRESHING FRAME GRID ===");
        Debug.Log($"Player unlocked frames (IDs): {string.Join(", ", gameData.unlockedFrameIds)}");

        foreach (var frame in allFrames)
        {
            // Check if frame is unlocked in GameData by ID OR by name
            bool isUnlockedById = gameData.unlockedFrameIds.Contains(frame.id);
            bool isUnlockedByName = gameData.unlockedFrameIds.Contains(frame.frameName);
            bool isUnlockedInGameData = isUnlockedById || isUnlockedByName;

            // Check if frame is unlocked by default in database
            bool isDefaultUnlock = frame.unlockedByDefault;

            // Combined unlock state
            bool isLocked = !(isUnlockedInGameData || isDefaultUnlock);

            bool isSelected = (!isLocked && frame.id == gameData.equippedFrameId);

            Debug.Log($"Frame: {frame.frameName} (ID: {frame.id})");
            Debug.Log($"  - Default Unlock: {isDefaultUnlock}");
            Debug.Log($"  - In GameData by ID: {isUnlockedById}");
            Debug.Log($"  - In GameData by Name: {isUnlockedByName}");
            Debug.Log($"  - Final Locked State: {isLocked}");
            Debug.Log($"  - Selected: {isSelected}");

            GameObject buttonObj = Instantiate(frameButtonPrefab, iconGridParent);
            FrameButton frameButton = buttonObj.GetComponent<FrameButton>();

            frameButton.Initialize(frame, OnFrameSelected, isLocked, isSelected);

            if (isSelected)
            {
                currentlySelectedFrameButton = frameButton;
            }

            frameButtons.Add(frameButton);
        }
        Debug.Log("=== FRAME GRID REFRESH COMPLETE ===");
    }

    private void RefreshAchievementGrid()
    {
        if (achievementDatabase == null) return;

        var gameData = gameDataManager.CurrentGameData;
        List<AchievementDatabase.AchievementData> allAchievements = achievementDatabase.achievements;

        allAchievements.Sort((a, b) => {
            AchievementStatus statusA = gameDataManager.GetAchievementStatus(a.id);
            AchievementStatus statusB = gameDataManager.GetAchievementStatus(b.id);

            int priorityA = statusA == AchievementStatus.Completed ? 0 :
                           statusA == AchievementStatus.NotComplete ? 1 : 2;
            int priorityB = statusB == AchievementStatus.Completed ? 0 :
                           statusB == AchievementStatus.NotComplete ? 1 : 2;

            return priorityA.CompareTo(priorityB);
        });

        foreach (var achievement in allAchievements)
        {
            GameObject buttonObj = Instantiate(achievementButtonPrefab, iconGridParent);
            AchievementButton achievementButton = buttonObj.GetComponent<AchievementButton>();

            AchievementStatus status = gameDataManager.GetAchievementStatus(achievement.id);
            achievementButton.Initialize(achievement, status, OnAchievementClicked);

            achievementButtons.Add(achievementButton);
        }
    }

    private void OnAchievementClicked(AchievementDatabase.AchievementData achievement)
    {
        currentlySelectedAchievement = achievement;
        UpdateAchievementInfoPanel(achievement);
        ShowAchievementInfoPanel();
        PlayButtonSound();
    }

    private void UpdateAchievementInfoPanel(AchievementDatabase.AchievementData achievement)
    {
        if (achievement == null) return;

        AchievementStatus status = gameDataManager.GetAchievementStatus(achievement.id);

        if (achievementNameText != null)
            achievementNameText.text = achievement.achievementName;

        if (achievementDescriptionText != null)
            achievementDescriptionText.text = achievement.description;

        if (achievementPrizeText != null)
            achievementPrizeText.text = $"x{achievement.prizeGems}";

        if (achievementIconImage != null && achievement.achievementIcon != null)
            achievementIconImage.sprite = achievement.achievementIcon;

        if (achievementStatusText != null)
        {
            switch (status)
            {
                case AchievementStatus.NotComplete:
                    achievementStatusText.text = "Not Complete";
                    achievementStatusText.color = Color.gray;
                    break;
                case AchievementStatus.Completed:
                    achievementStatusText.text = "Completed";
                    achievementStatusText.color = Color.green;
                    break;
                case AchievementStatus.Claimed:
                    achievementStatusText.text = "Claimed";
                    achievementStatusText.color = Color.yellow;
                    break;
            }
        }

        if (achievementInfoClaimButton != null)
        {
            achievementInfoClaimButton.gameObject.SetActive(status == AchievementStatus.Completed);
            achievementInfoClaimButton.interactable = (status == AchievementStatus.Completed);
        }
    }

    private void ShowAchievementInfoPanel()
    {
        if (achievementInfoPanel == null || achievementInfoCanvasGroup == null) return;

        if (panelFadeCoroutine != null)
        {
            StopCoroutine(panelFadeCoroutine);
        }

        achievementInfoPanel.SetActive(true);
        panelFadeCoroutine = StartCoroutine(FadeCanvasGroup(achievementInfoCanvasGroup, 1f, panelFadeDuration));
    }

    private void HideAchievementInfoPanel()
    {
        if (achievementInfoPanel == null || achievementInfoCanvasGroup == null) return;

        if (panelFadeCoroutine != null)
        {
            StopCoroutine(panelFadeCoroutine);
        }

        panelFadeCoroutine = StartCoroutine(FadeCanvasGroup(achievementInfoCanvasGroup, 0f, panelFadeDuration));
    }

    private void OnAchievementInfoCloseClicked()
    {
        HideAchievementInfoPanel();
    }

    private void OnAchievementInfoClaimClicked()
    {
        if (currentlySelectedAchievement != null)
        {
            gameDataManager.ClaimAchievement(currentlySelectedAchievement.id, currentlySelectedAchievement.prizeGems);

            foreach (var button in achievementButtons)
            {
                if (button.GetAchievementId() == currentlySelectedAchievement.id)
                {
                    button.UpdateStatus(AchievementStatus.Claimed);
                    break;
                }
            }

            UpdateAchievementInfoPanel(currentlySelectedAchievement);

            if (playerData != null)
            {
                playerData.UpdateGemDisplayImmediate();
            }

            if (AudioHandler.Instance != null)
            {
                AudioHandler.Instance.PlayClaimSound();
            }

            Debug.Log($"Achievement {currentlySelectedAchievement.achievementName} claimed! +{currentlySelectedAchievement.prizeGems} gems");
        }
    }

    private void OnIconSelected(ProfileIconDatabase.ProfileIcon icon)
    {
        currentProfileIcon.sprite = icon.iconSprite;

        if (currentlySelectedIconButton != null)
        {
            currentlySelectedIconButton.SetSelected(false);
        }

        foreach (var button in iconButtons)
        {
            if (button.GetIconId() == icon.id)
            {
                button.SetSelected(true);
                currentlySelectedIconButton = button;
                break;
            }
        }

        CheckForChanges();
    }

    private void OnFrameSelected(FrameDatabase.FrameData frame)
    {
        currentFrameImage.sprite = frame.frameSprite;

        if (currentlySelectedFrameButton != null)
        {
            currentlySelectedFrameButton.SetSelected(false);
        }

        foreach (var button in frameButtons)
        {
            if (button.GetFrameId() == frame.id)
            {
                button.SetSelected(true);
                currentlySelectedFrameButton = button;
                break;
            }
        }

        CheckForChanges();
    }

    private void OnEditNameClicked()
    {
        nameInputField.interactable = !nameInputField.interactable;

        if (nameInputField.interactable)
        {
            nameInputField.Select();
        }

        CheckForChanges();
    }

    private void OnNameChanged(string newName)
    {
        CheckForChanges();
    }

    private void CheckForChanges()
    {
        bool nameChanged = nameInputField.text != originalName;
        bool iconChanged = false;
        bool frameChanged = false;

        if (currentlySelectedIconButton != null)
        {
            iconChanged = (currentlySelectedIconButton.GetIconId() != originalIconId);
        }

        if (currentlySelectedFrameButton != null)
        {
            frameChanged = (currentlySelectedFrameButton.GetFrameId() != originalFrameId);
        }

        bool newHasChanges = nameChanged || iconChanged || frameChanged;

        if (newHasChanges != hasChanges)
        {
            hasChanges = newHasChanges;

            if (saveButton != null && saveButtonCanvasGroup != null)
            {
                if (hasChanges)
                {
                    FadeSaveButton(1f, saveButtonFadeDuration);
                }
                else
                {
                    FadeSaveButton(0f, saveButtonFadeDuration);
                }
            }
        }
    }

    private void FadeSaveButton(float targetAlpha, float duration)
    {
        if (saveButtonFadeCoroutine != null)
        {
            StopCoroutine(saveButtonFadeCoroutine);
        }

        saveButtonFadeCoroutine = StartCoroutine(FadeSaveButtonCoroutine(targetAlpha, duration));
    }

    private IEnumerator FadeSaveButtonCoroutine(float targetAlpha, float duration)
    {
        if (saveButtonCanvasGroup == null)
            yield break;

        if (targetAlpha > 0 && !saveButton.gameObject.activeSelf)
        {
            saveButton.gameObject.SetActive(true);
            saveButtonCanvasGroup.alpha = 0f;
        }

        float startAlpha = saveButtonCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            saveButtonCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        saveButtonCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            saveButton.gameObject.SetActive(false);
        }

        saveButtonFadeCoroutine = null;
    }

    private void OnSaveClicked()
    {
        Debug.Log("Save button clicked - saving changes");
        SaveChanges();
        CloseProfileSettings();
        Debug.Log("Profile changes saved and canvas closed!");
    }

    private void OnCloseClicked()
    {
        if (hasChanges)
        {
            dialogPanel.SetActive(true);
        }
        else
        {
            CloseProfileSettings();
        }
    }

    private void OnDialogYes()
    {
        SaveChanges();
        dialogPanel.SetActive(false);
        CloseProfileSettings();
        Debug.Log("Profile changes saved via dialog!");
    }

    private void OnDialogNo()
    {
        RevertChanges();
        dialogPanel.SetActive(false);
        CloseProfileSettings();
        Debug.Log("Profile changes discarded!");
    }

    private void SaveChanges()
    {
        Debug.Log("=== SAVING CHANGES ===");
        var gameData = gameDataManager.CurrentGameData;

        if (nameInputField.text != originalName)
        {
            Debug.Log($"Saving name: {nameInputField.text} (was: {originalName})");
            gameData.playerName = nameInputField.text;

            if (playerData != null)
            {
                playerData.SetPlayerName(nameInputField.text);
            }
        }

        if (currentlySelectedIconButton != null && currentlySelectedIconButton.GetIconId() != originalIconId)
        {
            string newIconId = currentlySelectedIconButton.GetIconId();
            Debug.Log($"Saving icon: {newIconId} (was: {originalIconId})");
            gameData.equippedIconId = newIconId;

            if (playerData != null)
            {
                playerData.UpdateProfileIcon(currentProfileIcon.sprite);
            }
        }

        if (currentlySelectedFrameButton != null && currentlySelectedFrameButton.GetFrameId() != originalFrameId)
        {
            string newFrameId = currentlySelectedFrameButton.GetFrameId();
            Debug.Log($"Saving frame: {newFrameId} (was: {originalFrameId})");
            gameData.equippedFrameId = newFrameId;

            if (playerData != null)
            {
                playerData.UpdateFrame(currentFrameImage.sprite);
            }
        }

        gameDataManager.SaveGameData();
        Debug.Log("GameData saved to disk");

        originalName = nameInputField.text;
        if (currentlySelectedIconButton != null)
            originalIconId = currentlySelectedIconButton.GetIconId();
        if (currentlySelectedFrameButton != null)
            originalFrameId = currentlySelectedFrameButton.GetFrameId();

        hasChanges = false;

        if (saveButton != null && saveButtonCanvasGroup != null)
        {
            FadeSaveButton(0f, saveButtonFadeDuration);
        }

        Debug.Log("=== SAVE COMPLETE ===");
    }

    private void RevertChanges()
    {
        nameInputField.text = originalName;

        Sprite originalIconSprite = iconDatabase.GetIconSprite(originalIconId);
        if (originalIconSprite != null)
            currentProfileIcon.sprite = originalIconSprite;

        Sprite originalFrameSprite = frameDatabase.GetFrameSprite(originalFrameId);
        if (originalFrameSprite != null)
            currentFrameImage.sprite = originalFrameSprite;

        foreach (var button in iconButtons)
        {
            bool isSelected = (button.GetIconId() == originalIconId);
            button.SetSelected(isSelected);
            if (isSelected)
            {
                currentlySelectedIconButton = button;
            }
        }

        foreach (var button in frameButtons)
        {
            bool isSelected = (button.GetFrameId() == originalFrameId);
            button.SetSelected(isSelected);
            if (isSelected)
            {
                currentlySelectedFrameButton = button;
            }
        }

        nameInputField.interactable = false;
        hasChanges = false;

        if (saveButton != null && saveButtonCanvasGroup != null)
        {
            FadeSaveButton(0f, saveButtonFadeDuration);
        }
    }

    public void UnlockIcon(string iconId)
    {
        gameDataManager.UnlockIcon(iconId);

        if (canvas.activeSelf && currentView == ViewMode.Icons)
        {
            ShowIconsView();
        }
    }

    public void UnlockFrame(string frameId)
    {
        gameDataManager.UnlockFrame(frameId);

        if (canvas.activeSelf && currentView == ViewMode.Frames)
        {
            ShowFramesView();
        }
    }

    public void CompleteAchievement(string achievementId)
    {
        gameDataManager.CompleteAchievement(achievementId);

        if (canvas.activeSelf && currentView == ViewMode.Achievements)
        {
            ShowAchievementsView();

            if (currentlySelectedAchievement != null && currentlySelectedAchievement.id == achievementId)
            {
                UpdateAchievementInfoPanel(currentlySelectedAchievement);
            }
        }
    }
}