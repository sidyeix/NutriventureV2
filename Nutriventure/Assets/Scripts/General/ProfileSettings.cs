using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;

public class ProfileSettings : MonoBehaviour
{
    /// <summary>
    /// Fired after the ProfileSettings canvas has fully closed (fade complete).
    /// InGameSettingsButton listens to this to re-show itself and resume the game.
    /// </summary>
    public event System.Action OnClosed;
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

    [Header("Stats Counters")]
    public TextMeshProUGUI achievementsCountText;
    public TextMeshProUGUI kingdomsCountText;
    public TextMeshProUGUI enerlingsCountText;
    public string achievementsFormat = "Achievements: {0}/{1}";
    public string kingdomsFormat = "Kingdoms: {0}";
    public string enerlingsFormat = "Enerlings: {0}";

    [Header("Navigation")]
    public Button profileIconsButton;
    public Button framesButton;
    public Button achievementsButton;
    public Button settingsButton;

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

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider sensitivitySlider;
    public List<AudioSource> backgroundMusicSources = new List<AudioSource>();
    public List<AudioSource> soundEffectSources = new List<AudioSource>();

    [Header("Settings - Mute Buttons")]
    public Button muteBGButton;
    public Button muteSFXButton;
    public Button muteAllButton;
    public TextMeshProUGUI muteBGButtonText;
    public TextMeshProUGUI muteSFXButtonText;
    public TextMeshProUGUI muteAllButtonText;

    [Header("Settings - Action Buttons")]
    public Button exitGameButton;
    public Button resetPlayerDataButton;

    [Header("Warning Dialog (Unified)")]
    public GameObject warningDialogPanel;
    public TextMeshProUGUI warningText;
    public Button warningYesButton;
    public Button warningNoButton;
    public Button closeButton;

    [Header("Reset Data Dialog")]
    public GameObject resetDataDialog;
    public Button confirmResetButton;
    public Button cancelResetButton;

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

    // Settings tracking
    private bool isBGMusicMuted = false;
    private bool isSFXMuted = false;
    private bool isAllMuted = false;
    private float lastMusicVolume;
    private float lastSFXVolume;

    // Current view state
    private enum ViewMode { Icons, Frames, Achievements, Settings }
    private ViewMode currentView = ViewMode.Icons;
    private GridLayoutGroup gridLayout;

    // Store original button colors
    private Color originalProfileIconColor;
    private Color originalFramesColor;
    private Color originalAchievementsColor;
    private Color originalSettingsColor;

    // Save button fade animation
    private CanvasGroup saveButtonCanvasGroup;
    private Coroutine saveButtonFadeCoroutine;

    // Panel fade animation
    private Coroutine panelFadeCoroutine;

    // Main canvas fade
    private CanvasGroup mainCanvasGroup;

    // Warning dialog dynamic callbacks
    private System.Action warningYesAction;
    private System.Action warningNoAction;

    private void Awake()
    {
        if (!hasInitializedDefaults && gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            InitializeDefaultItems();
            hasInitializedDefaults = true;
        }
    }

    private void Start()
    {
        nameInputField.interactable = false;
        if (warningDialogPanel != null)
            warningDialogPanel.SetActive(false);

        InitializeSettings();
        SetupCanvas();
        SetupAchievementPanel();
        SetupSaveButton();
        StoreOriginalColors();
        SetupGridLayout();
        SetupButtonListeners();

        UpdateAllCounters();
    }

    private void SetupCanvas()
    {
        if (canvas != null)
        {
            mainCanvasGroup = canvas.GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = canvas.AddComponent<CanvasGroup>();
            }
            mainCanvasGroup.alpha = 0f;
        }
    }

    private void SetupAchievementPanel()
    {
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
    }

    private void SetupSaveButton()
    {
        if (saveButton != null)
        {
            saveButton.gameObject.SetActive(false);
            saveButtonCanvasGroup = saveButton.GetComponent<CanvasGroup>();
            if (saveButtonCanvasGroup == null)
            {
                saveButtonCanvasGroup = saveButton.gameObject.AddComponent<CanvasGroup>();
            }
            saveButtonCanvasGroup.alpha = 0f;

            saveButton.onClick.AddListener(PlayButtonSound);
            saveButton.onClick.AddListener(OnSaveClicked);
        }
    }

    private void StoreOriginalColors()
    {
        if (profileIconsButton != null)
            originalProfileIconColor = profileIconsButton.GetComponent<Image>().color;
        if (framesButton != null)
            originalFramesColor = framesButton.GetComponent<Image>().color;
        if (achievementsButton != null)
            originalAchievementsColor = achievementsButton.GetComponent<Image>().color;
        if (settingsButton != null)
            originalSettingsColor = settingsButton.GetComponent<Image>().color;
    }

    private void SetupGridLayout()
    {
        if (iconGridParent != null)
        {
            gridLayout = iconGridParent.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = iconGridParent.gameObject.AddComponent<GridLayoutGroup>();
            }
        }
    }

    private void SetupButtonListeners()
    {
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

        // Unified warning dialog buttons
        if (warningYesButton != null)
        {
            warningYesButton.onClick.AddListener(OnWarningYesClicked);
            warningYesButton.onClick.AddListener(PlayButtonSound);
        }

        if (warningNoButton != null)
        {
            warningNoButton.onClick.AddListener(OnWarningNoClicked);
            warningNoButton.onClick.AddListener(PlayButtonSound);
        }

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

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            settingsButton.onClick.AddListener(PlayButtonSound);
        }

        if (muteBGButton != null)
            muteBGButton.onClick.AddListener(OnMuteBGButtonClicked);

        if (muteSFXButton != null)
            muteSFXButton.onClick.AddListener(OnMuteSFXButtonClicked);

        if (muteAllButton != null)
            muteAllButton.onClick.AddListener(OnMuteAllButtonClicked);

        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(OnExitGameButtonClicked);

        if (resetPlayerDataButton != null)
            resetPlayerDataButton.onClick.AddListener(OnResetPlayerDataClicked);

        if (confirmResetButton != null)
            confirmResetButton.onClick.AddListener(OnConfirmResetClicked);

        if (cancelResetButton != null)
            cancelResetButton.onClick.AddListener(OnCancelResetClicked);

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

        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(OnNameChanged);
    }

    private void UpdateAllCounters()
    {
        UpdateAchievementsCounter();
        UpdateKingdomsCounter();
        UpdateEnerlingsCounter();
    }

    private void UpdateAchievementsCounter()
    {
        if (achievementsCountText == null || gameDataManager?.CurrentGameData == null || achievementDatabase == null)
            return;

        int unlockedCount = 0;
        int totalCount = achievementDatabase.achievements.Count;

        foreach (var achievement in achievementDatabase.achievements)
        {
            AchievementStatus status = gameDataManager.GetAchievementStatus(achievement.id);
            if (status == AchievementStatus.Completed || status == AchievementStatus.Claimed)
            {
                unlockedCount++;
            }
        }

        achievementsCountText.text = string.Format(achievementsFormat, unlockedCount, totalCount);
    }

    private void UpdateKingdomsCounter()
    {
        if (kingdomsCountText == null || gameDataManager?.CurrentGameData == null)
            return;

        var gameData = gameDataManager.CurrentGameData;
        int unlockedCount = 0;

        if (gameData.HasSugariaKey()) unlockedCount++;
        if (gameData.HasPreserviaKey()) unlockedCount++;
        if (gameData.HasNutriKingdomKey()) unlockedCount++;
        if (gameData.HasAllerthiaKey()) unlockedCount++;
        if (gameData.HasOCRScannerKey()) unlockedCount++;

        kingdomsCountText.text = string.Format(kingdomsFormat, unlockedCount);
    }

    private void UpdateEnerlingsCounter()
    {
        if (enerlingsCountText == null)
            return;

        // Use PersistentDataManager (PlayerPrefs) as the primary source of truth
        int unlockedCount = 0;
        if (PersistentDataManager.Instance != null)
        {
            unlockedCount = PersistentDataManager.Instance.GetTotalUnlockedCount();
        }
        else if (gameDataManager?.CurrentGameData != null)
        {
            // Fallback to GameData if PersistentDataManager not loaded
            unlockedCount = gameDataManager.CurrentGameData.unlockedEnerlings?.Count ?? 0;
        }

        enerlingsCountText.text = string.Format(enerlingsFormat, unlockedCount);
    }

    private void InitializeSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (GameDataManager.Instance != null)
        {
            var gameData = GameDataManager.Instance.CurrentGameData;

            musicSlider.minValue = 0;
            musicSlider.maxValue = 100;
            sfxSlider.minValue = 0;
            sfxSlider.maxValue = 100;

            musicSlider.value = gameData.musicVolume * 100;
            sfxSlider.value = gameData.soundVolume * 100;

            lastMusicVolume = gameData.musicVolume;
            lastSFXVolume = gameData.soundVolume;

            // Sensitivity slider: range 0.1 to 2.0, stored as raw float
            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.1f;
                sensitivitySlider.maxValue = 2f;
                sensitivitySlider.value = gameData.lookSensitivity;
            }
        }

        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        UpdateMuteButtonTexts();

        if (resetDataDialog != null)
            resetDataDialog.SetActive(false);
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

        var gameData = gameDataManager.CurrentGameData;

        if (iconDatabase != null && iconDatabase.icons.Count > 0)
        {
            // Unlock all unlockedByDefault icons
            foreach (var icon in iconDatabase.icons)
            {
                if (icon.unlockedByDefault && !gameData.unlockedIconIds.Contains(icon.id))
                {
                    gameData.unlockedIconIds.Add(icon.id);
                }
            }

            // Always ensure the first icon in the database is unlocked
            string firstIconId = iconDatabase.icons[0].id;
            if (!gameData.unlockedIconIds.Contains(firstIconId))
            {
                gameData.unlockedIconIds.Add(firstIconId);
            }

            // Equip the first icon in the database for new players
            if (string.IsNullOrEmpty(gameData.equippedIconId))
            {
                gameData.equippedIconId = firstIconId;
            }
        }

        if (frameDatabase != null && frameDatabase.frames.Count > 0)
        {
            // Unlock all unlockedByDefault frames
            foreach (var frame in frameDatabase.frames)
            {
                if (frame.unlockedByDefault && !gameData.unlockedFrameIds.Contains(frame.id))
                {
                    gameData.unlockedFrameIds.Add(frame.id);
                }
            }

            // Always ensure the first frame in the database is unlocked
            string firstFrameId = frameDatabase.frames[0].id;
            if (!gameData.unlockedFrameIds.Contains(firstFrameId))
            {
                gameData.unlockedFrameIds.Add(firstFrameId);
            }

            // Equip the first frame in the database for new players
            if (string.IsNullOrEmpty(gameData.equippedFrameId))
            {
                gameData.equippedFrameId = firstFrameId;
            }
        }

        gameDataManager.SaveGameData();
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
        UpdateAllCounters();

        ShowIconsView();
        HighlightNavigationButton(ViewMode.Icons);

        hasChanges = false;
        nameInputField.interactable = false;
        if (warningDialogPanel != null)
            warningDialogPanel.SetActive(false);

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

    public void OpenSettingsView()
    {
        if (gameDataManager?.CurrentGameData == null) return;

        var gameData = gameDataManager.CurrentGameData;

        originalName = gameData.playerName;
        originalIconId = gameData.equippedIconId;
        originalFrameId = gameData.equippedFrameId;

        RefreshProfileDisplay();
        RefreshLevelAndXP();
        UpdateAllCounters();

        ShowSettingsView();
        HighlightNavigationButton(ViewMode.Settings);

        hasChanges = false;
        nameInputField.interactable = false;
        if (warningDialogPanel != null)
            warningDialogPanel.SetActive(false);

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

    public void CloseProfileSettingsDirect()
    {
        if (hasChanges)
        {
            RevertChanges();
        }

        CloseProfileSettings();
    }

    public bool IsProfileSettingsOpen()
    {
        return canvas != null && canvas.activeSelf;
    }

    public bool IsSettingsViewActive()
    {
        return currentView == ViewMode.Settings;
    }

    public void RefreshCounters()
    {
        UpdateAllCounters();
    }

    private void CloseProfileSettings()
    {
        StartCoroutine(CloseProfileSettingsCoroutine());
    }

    private IEnumerator CloseProfileSettingsCoroutine()
    {
        // Fade to 0 but DON'T let FadeCanvasGroup deactivate the object,
        // because that would kill this coroutine before OnClosed fires.
        yield return StartCoroutine(FadeCanvasGroupNoDeactivate(mainCanvasGroup, 0f, panelFadeDuration));

        // Notify listeners BEFORE deactivating the canvas
        OnClosed?.Invoke();

        canvas.SetActive(false);
    }

    private IEnumerator FadeCanvasGroupNoDeactivate(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
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

    // UPDATED: Use unscaledDeltaTime for all fades
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
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

    // UPDATED: Use unscaledDeltaTime for save button fade
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
            elapsedTime += Time.unscaledDeltaTime;
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
        if (settingsButton != null)
            settingsButton.GetComponent<Image>().color = normalButtonColor;

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
            case ViewMode.Settings:
                if (settingsButton != null)
                    settingsButton.GetComponent<Image>().color = selectedButtonColor;
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

    private void OnSettingsClicked()
    {
        // Refresh all profile data (name, icon, frame, XP, counters) just like opening the profile tab
        RefreshProfileDisplay();
        RefreshLevelAndXP();
        UpdateAllCounters();

        ShowSettingsView();
        HighlightNavigationButton(ViewMode.Settings);
    }

    private void ShowIconsView()
    {
        currentView = ViewMode.Icons;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (iconGridParent != null)
            iconGridParent.gameObject.SetActive(true);

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

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (iconGridParent != null)
            iconGridParent.gameObject.SetActive(true);

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

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (iconGridParent != null)
            iconGridParent.gameObject.SetActive(true);

        if (gridLayout != null)
        {
            gridLayout.cellSize = achievementCellSize;
        }

        ClearGrid();
        RefreshAchievementGrid();
    }

    private void ShowSettingsView()
    {
        currentView = ViewMode.Settings;

        if (iconGridParent != null)
            iconGridParent.gameObject.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        RefreshSettingsValues();
    }

    private void RefreshSettingsValues()
    {
        if (GameDataManager.Instance != null)
        {
            var gameData = GameDataManager.Instance.CurrentGameData;

            musicSlider.value = gameData.musicVolume * 100;
            sfxSlider.value = gameData.soundVolume * 100;

            lastMusicVolume = gameData.musicVolume;
            lastSFXVolume = gameData.soundVolume;

            if (sensitivitySlider != null)
                sensitivitySlider.value = gameData.lookSensitivity;
        }

        isBGMusicMuted = false;
        isSFXMuted = false;
        isAllMuted = false;
        musicSlider.interactable = true;
        sfxSlider.interactable = true;
        UpdateMuteButtonTexts();
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

        foreach (var icon in allIcons)
        {
            bool isUnlockedById = gameData.unlockedIconIds.Contains(icon.id);
            bool isUnlockedByName = gameData.unlockedIconIds.Contains(icon.iconName);
            bool isUnlockedInGameData = isUnlockedById || isUnlockedByName;
            bool isDefaultUnlock = icon.unlockedByDefault;
            bool isLocked = !(isUnlockedInGameData || isDefaultUnlock);
            bool isSelected = (!isLocked && icon.id == gameData.equippedIconId);

            GameObject buttonObj = Instantiate(iconButtonPrefab, iconGridParent);
            ProfileIconButton iconButton = buttonObj.GetComponent<ProfileIconButton>();

            iconButton.Initialize(icon, OnIconSelected, isLocked, isSelected);

            if (isSelected)
            {
                currentlySelectedIconButton = iconButton;
            }

            iconButtons.Add(iconButton);
        }
    }

    private void RefreshFrameGrid()
    {
        var gameData = gameDataManager.CurrentGameData;
        List<FrameDatabase.FrameData> allFrames = frameDatabase.frames;

        foreach (var frame in allFrames)
        {
            bool isUnlockedById = gameData.unlockedFrameIds.Contains(frame.id);
            bool isUnlockedByName = gameData.unlockedFrameIds.Contains(frame.frameName);
            bool isUnlockedInGameData = isUnlockedById || isUnlockedByName;
            bool isDefaultUnlock = frame.unlockedByDefault;
            bool isLocked = !(isUnlockedInGameData || isDefaultUnlock);
            bool isSelected = (!isLocked && frame.id == gameData.equippedFrameId);

            GameObject buttonObj = Instantiate(frameButtonPrefab, iconGridParent);
            FrameButton frameButton = buttonObj.GetComponent<FrameButton>();

            frameButton.Initialize(frame, OnFrameSelected, isLocked, isSelected);

            if (isSelected)
            {
                currentlySelectedFrameButton = frameButton;
            }

            frameButtons.Add(frameButton);
        }
    }

    private void RefreshAchievementGrid()
    {
        if (achievementDatabase == null) return;

        var gameData = gameDataManager.CurrentGameData;
        List<AchievementDatabase.AchievementData> allAchievements = achievementDatabase.achievements;

        allAchievements.Sort((a, b) =>
        {
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

            UpdateAchievementsCounter();
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

    private void OnSaveClicked()
    {
        SaveChanges();
        CloseProfileSettings();
    }

    private void OnCloseClicked()
    {
        if (hasChanges)
        {
            ShowWarningDialog(
                "You have unsaved changes.\nDo you want to save before closing?",
                onYes: () =>
                {
                    SaveChanges();
                    CloseProfileSettings();
                },
                onNo: () =>
                {
                    RevertChanges();
                    CloseProfileSettings();
                }
            );
        }
        else
        {
            CloseProfileSettings();
        }
    }

    /// <summary>
    /// Shows the unified warning dialog with dynamic Yes/No actions.
    /// Use this for save confirmation, exit, restart, home, etc.
    /// </summary>
    public void ShowWarningDialog(string message, System.Action onYes, System.Action onNo = null)
    {
        if (warningDialogPanel == null) return;

        if (warningText != null)
            warningText.text = message;

        warningYesAction = onYes;
        warningNoAction = onNo;

        warningDialogPanel.SetActive(true);
    }

    private void HideWarningDialog()
    {
        if (warningDialogPanel != null)
            warningDialogPanel.SetActive(false);

        warningYesAction = null;
        warningNoAction = null;
    }

    private void OnWarningYesClicked()
    {
        System.Action action = warningYesAction;
        HideWarningDialog();
        action?.Invoke();
    }

    private void OnWarningNoClicked()
    {
        System.Action action = warningNoAction;
        HideWarningDialog();
        action?.Invoke();
    }

    private void SaveChanges()
    {
        var gameData = gameDataManager.CurrentGameData;

        if (nameInputField.text != originalName)
        {
            gameData.playerName = nameInputField.text;

            if (playerData != null)
            {
                playerData.SetPlayerName(nameInputField.text);
            }
        }

        if (currentlySelectedIconButton != null && currentlySelectedIconButton.GetIconId() != originalIconId)
        {
            string newIconId = currentlySelectedIconButton.GetIconId();
            gameData.equippedIconId = newIconId;

            if (playerData != null)
            {
                playerData.UpdateProfileIcon(currentProfileIcon.sprite);
            }
        }

        if (currentlySelectedFrameButton != null && currentlySelectedFrameButton.GetFrameId() != originalFrameId)
        {
            string newFrameId = currentlySelectedFrameButton.GetFrameId();
            gameData.equippedFrameId = newFrameId;

            if (playerData != null)
            {
                playerData.UpdateFrame(currentFrameImage.sprite);
            }
        }

        gameDataManager.SaveGameData();

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

        UpdateAchievementsCounter();
    }

    #region Settings Methods

    private void OnMusicSliderChanged(float value)
    {
        float volume = value / 100f;

        foreach (var source in backgroundMusicSources)
        {
            if (source != null)
                source.volume = volume;
        }

        if (!isBGMusicMuted && !isAllMuted)
        {
            lastMusicVolume = volume;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.musicVolume = volume;
            GameDataManager.Instance.SaveGameData();
        }
    }

    private void OnSFXSliderChanged(float value)
    {
        float volume = value / 100f;

        foreach (var source in soundEffectSources)
        {
            if (source != null)
                source.volume = volume;
        }

        if (!isSFXMuted && !isAllMuted)
        {
            lastSFXVolume = volume;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.soundVolume = volume;
            GameDataManager.Instance.SaveGameData();
        }
    }

    private void OnSensitivitySliderChanged(float value)
    {
        // Update all active UI_SwipeLook instances in the scene
        foreach (var swipeLook in FindObjectsByType<UI_SwipeLook>(FindObjectsSortMode.None))
        {
            swipeLook.SetSensitivity(value);
        }

        // Save to GameData
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.lookSensitivity = value;
            GameDataManager.Instance.SaveGameData();
        }
    }

    private void OnMuteBGButtonClicked()
    {
        PlayButtonSound();

        if (!isBGMusicMuted && !isAllMuted)
        {
            isBGMusicMuted = true;
            musicSlider.interactable = false;

            foreach (var source in backgroundMusicSources)
            {
                if (source != null)
                    source.volume = 0;
            }
        }
        else if (isBGMusicMuted)
        {
            isBGMusicMuted = false;
            musicSlider.interactable = true;

            float volume = lastMusicVolume;

            foreach (var source in backgroundMusicSources)
            {
                if (source != null)
                    source.volume = volume;
            }
        }

        UpdateMuteAllState();
        UpdateMuteButtonTexts();
    }

    private void OnMuteSFXButtonClicked()
    {
        PlayButtonSound();

        if (!isSFXMuted && !isAllMuted)
        {
            isSFXMuted = true;
            sfxSlider.interactable = false;

            foreach (var source in soundEffectSources)
            {
                if (source != null)
                    source.volume = 0;
            }
        }
        else if (isSFXMuted)
        {
            isSFXMuted = false;
            sfxSlider.interactable = true;

            float volume = lastSFXVolume;

            foreach (var source in soundEffectSources)
            {
                if (source != null)
                    source.volume = volume;
            }
        }

        UpdateMuteAllState();
        UpdateMuteButtonTexts();
    }

    private void OnMuteAllButtonClicked()
    {
        PlayButtonSound();

        if (!isAllMuted)
        {
            isAllMuted = true;
            musicSlider.interactable = false;
            sfxSlider.interactable = false;

            foreach (var source in backgroundMusicSources)
            {
                if (source != null)
                    source.volume = 0;
            }

            foreach (var source in soundEffectSources)
            {
                if (source != null)
                    source.volume = 0;
            }

            isBGMusicMuted = true;
            isSFXMuted = true;
        }
        else
        {
            isAllMuted = false;
            musicSlider.interactable = true;
            sfxSlider.interactable = true;

            float musicVol = lastMusicVolume;
            float sfxVol = lastSFXVolume;

            foreach (var source in backgroundMusicSources)
            {
                if (source != null)
                    source.volume = musicVol;
            }

            foreach (var source in soundEffectSources)
            {
                if (source != null)
                    source.volume = sfxVol;
            }

            isBGMusicMuted = false;
            isSFXMuted = false;
        }

        UpdateMuteButtonTexts();
    }

    private void UpdateMuteAllState()
    {
        isAllMuted = (isBGMusicMuted && isSFXMuted);
    }

    private void UpdateMuteButtonTexts()
    {
        if (muteBGButtonText != null)
            muteBGButtonText.text = isBGMusicMuted ? "Unmute BG Music" : "Mute BG Music";

        if (muteSFXButtonText != null)
            muteSFXButtonText.text = isSFXMuted ? "Unmute SFX" : "Mute SFX";

        if (muteAllButtonText != null)
            muteAllButtonText.text = isAllMuted ? "Unmute All" : "Mute All";
    }

    private void OnExitGameButtonClicked()
    {
        PlayButtonSound();

        ShowWarningDialog(
            "Are you sure you want to exit the game?",
            onYes: () =>
            {
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.SaveGameData();
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        );
    }

    private void OnResetPlayerDataClicked()
    {
        PlayButtonSound();

        if (resetDataDialog != null)
            resetDataDialog.SetActive(true);
    }

    private void OnConfirmResetClicked()
    {
        PlayButtonSound();

        // 1. Reset the main game data file (nutriventure_save.json)
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetGameData();
        }

        // 2. Clear Kingdom 1 game-state save file (game_state.json)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ClearSavedGameState();
        }

        // 3. Clear Kingdom 2 game-state save file (k2_game_state.json)
        if (K2_GameStateManager.Instance != null)
        {
            K2_GameStateManager.Instance.ClearSavedGameState();
        }
        else
        {
            DeletePersistentFile("k2_game_state.json");
        }

        // 4. Clear Kingdom 3 game-state save file (k3_game_state.json)
        if (K3_GameStateManager.Instance != null)
        {
            K3_GameStateManager.Instance.ClearSavedGameState();
        }
        else
        {
            DeletePersistentFile("k3_game_state.json");
        }

        // 5. Clear Kingdom 4 (Allerthia) save file (gameData.save)
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.DeleteSaveFile();
        }
        else
        {
            DeletePersistentFile("gameData.save");
        }

        // 6. Reset Enerling / OCR data (PersistentDataManager)
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.ResetAllProgress();
        }

        // 7. Clear ALL PlayerPrefs (covers every key across the whole game)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("ProfileSettings: Full data reset complete — returning to LogoScreen as new player.");

        if (resetDataDialog != null)
            resetDataDialog.SetActive(false);

        SceneManager.LoadScene("LogoScreen");
    }

    /// <summary>
    /// Safely deletes a file in Application.persistentDataPath if it exists.
    /// Used as a fallback when a manager singleton is not loaded.
    /// </summary>
    private void DeletePersistentFile(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log($"ProfileSettings: Deleted persistent file: {fileName}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"ProfileSettings: Failed to delete {fileName} — {e.Message}");
            }
        }
    }

    private void OnCancelResetClicked()
    {
        PlayButtonSound();

        if (resetDataDialog != null)
            resetDataDialog.SetActive(false);
    }

    #endregion
}