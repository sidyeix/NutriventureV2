using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleSettingsAndResumeController : MonoBehaviour
{
  [Header("Battle References")]
  [SerializeField] private BattlePlayManager battlePlayManager;
  [SerializeField] private BattleEnerlingManager battleEnerlingManager;
  [SerializeField] private AIEnerlingManager aiEnerlingManager;
  [SerializeField] private TurnSystem turnSystem;

  [Header("Settings UI")]
  [SerializeField] private GameObject settingsCanvas;
  [SerializeField] private Button openSettingsButton;
  [SerializeField] private Button resumeGameButton;
  [SerializeField] private Button closeSettingsButton;
  [SerializeField] private Button backToScanButton;
  [SerializeField] private Button homeButton;
  [SerializeField] private Button exitButton;

  [Header("Countdown Settings")]
  [SerializeField] private int settingsCloseCountdownSeconds = 3;

  [Header("Settings Controls")]
  [SerializeField] private Slider musicSlider;
  [SerializeField] private Slider sfxSlider;
  [SerializeField] private Slider sensitivitySlider;
  [SerializeField] private List<AudioSource> backgroundMusicSources = new List<AudioSource>();
  [SerializeField] private List<AudioSource> soundEffectSources = new List<AudioSource>();

  [Header("Mute Buttons")]
  [SerializeField] private Button muteBGButton;
  [SerializeField] private Button muteSFXButton;
  [SerializeField] private Button muteAllButton;
  [SerializeField] private TextMeshProUGUI muteBGButtonText;
  [SerializeField] private TextMeshProUGUI muteSFXButtonText;
  [SerializeField] private TextMeshProUGUI muteAllButtonText;

  [Header("Resume Prompt UI")]
  [SerializeField] private GameObject resumeCanvas;
  [SerializeField] private Button resumeYesButton;
  [SerializeField] private Button resumeNoButton;
  [SerializeField] private int resumeYesCountdownSeconds = 3;

  [Header("Shared Countdown (K1 Style)")]
  [SerializeField] private GameObject resumeCountdownCanvas;
  [SerializeField] private GameObject resumeCountdownPanel;
  [SerializeField] private TextMeshProUGUI resumeCountdownText;
  [SerializeField] private float goDisplaySeconds = 0.5f;
  [SerializeField] private AudioSource countdownAudioSource;
  [SerializeField] private AudioClip countdownSFX;

  [Header("Confirmation Dialog")]
  [SerializeField] private GameObject confirmationPanel;
  [SerializeField] private TextMeshProUGUI confirmationText;
  [SerializeField] private Button confirmationYesButton;
  [SerializeField] private Button confirmationNoButton;
  [SerializeField] private string confirmBackToScanMessage = "Are you sure you want to return to ScanOCR?";
  [SerializeField] private string confirmHomeMessage = "Are you sure you want to go Home?";
  [SerializeField] private string confirmExitMessage = "Are you sure you want to exit the game?";

  [Header("Scene Names")]
  [SerializeField] private string scanSceneName = "ScanOCR";
  [SerializeField] private string previousScenePrefsKey = "ScanOCR_PreviousScene";

  private bool isPaused;
  private float previousTimeScale = 1f;
  private bool isBGMusicMuted;
  private bool isSFXMuted;
  private bool isAllMuted;
  private float lastMusicVolume = 1f;
  private float lastSFXVolume = 1f;
  private Coroutine activeTransitionCoroutine;
  private PendingConfirmationAction pendingConfirmationAction = PendingConfirmationAction.None;

  private enum PendingConfirmationAction
  {
    None,
    BackToScan,
    Home,
    Exit
  }

  private void Awake()
  {
    BattleRuntimeStateStore.PreloadFromPrefs();
  }

  private void Start()
  {
    ResolveReferences();
    SetupSettingsListeners();
    SetupResumePromptListeners();
    SetupConfirmationDialog();
    InitializeSettingsUI();

    if (settingsCanvas != null)
      settingsCanvas.SetActive(false);

    if (BattleRuntimeStateStore.ShouldDeferBattleInitialization)
    {
      ShowResumePrompt();
    }
    else if (battlePlayManager != null)
    {
      battlePlayManager.StartFreshBattleInitialization();
    }
  }

  private void ResolveReferences()
  {
    if (battlePlayManager == null)
      battlePlayManager = FindObjectOfType<BattlePlayManager>();

    if (battleEnerlingManager == null)
      battleEnerlingManager = FindObjectOfType<BattleEnerlingManager>();

    if (aiEnerlingManager == null)
      aiEnerlingManager = FindObjectOfType<AIEnerlingManager>();

    if (turnSystem == null)
      turnSystem = FindObjectOfType<TurnSystem>();

    if (AudioHandler.Instance != null)
    {
      if (AudioHandler.Instance.musicSource != null && !backgroundMusicSources.Contains(AudioHandler.Instance.musicSource))
        backgroundMusicSources.Add(AudioHandler.Instance.musicSource);

      if (AudioHandler.Instance.soundEffectsSource != null && !soundEffectSources.Contains(AudioHandler.Instance.soundEffectsSource))
        soundEffectSources.Add(AudioHandler.Instance.soundEffectsSource);
    }
  }

  private void SetupSettingsListeners()
  {
    BindButton(openSettingsButton, OpenSettings);
    BindButton(resumeGameButton, RequestCloseSettingsWithCountdown);
    BindButton(closeSettingsButton, RequestCloseSettingsWithCountdown);
    BindButton(backToScanButton, OnBackToScanClicked);
    BindButton(homeButton, OnHomeClicked);
    BindButton(exitButton, OnExitClicked);

    BindButton(muteBGButton, OnMuteBGButtonClicked);
    BindButton(muteSFXButton, OnMuteSFXButtonClicked);
    BindButton(muteAllButton, OnMuteAllButtonClicked);

    if (musicSlider != null)
    {
      musicSlider.onValueChanged.RemoveAllListeners();
      musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
    }

    if (sfxSlider != null)
    {
      sfxSlider.onValueChanged.RemoveAllListeners();
      sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
    }

    if (sensitivitySlider != null)
    {
      sensitivitySlider.onValueChanged.RemoveAllListeners();
      sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }
  }

  private void SetupConfirmationDialog()
  {
    BindButton(confirmationYesButton, OnConfirmationYesClicked);
    BindButton(confirmationNoButton, OnConfirmationNoClicked);

    if (confirmationPanel != null)
      confirmationPanel.SetActive(false);
  }

  private void SetupResumePromptListeners()
  {
    BindButton(resumeYesButton, OnResumePromptYes);
    BindButton(resumeNoButton, OnResumePromptNo);

    if (resumeCanvas != null)
      resumeCanvas.SetActive(false);
  }

  private void InitializeSettingsUI()
  {
    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      float musicVolume = Mathf.Clamp01(GameDataManager.Instance.CurrentGameData.musicVolume);
      float sfxVolume = Mathf.Clamp01(GameDataManager.Instance.CurrentGameData.soundVolume);

      lastMusicVolume = musicVolume;
      lastSFXVolume = sfxVolume;

      if (musicSlider != null)
        musicSlider.value = musicVolume * 100f;

      if (sfxSlider != null)
        sfxSlider.value = sfxVolume * 100f;

      if (sensitivitySlider != null)
      {
        sensitivitySlider.value = GameDataManager.Instance.CurrentGameData.lookSensitivity;
        sensitivitySlider.interactable = false;
      }
    }
    else if (sensitivitySlider != null)
    {
      sensitivitySlider.interactable = false;
    }

    isBGMusicMuted = false;
    isSFXMuted = false;
    isAllMuted = false;
    UpdateMuteButtonTexts();
  }

  private void OpenSettings()
  {
    PlayButtonClick();
    PauseGameTime();

    if (settingsCanvas != null)
      settingsCanvas.SetActive(true);

    InitializeSettingsUI();
  }

  private void RequestCloseSettingsWithCountdown()
  {
    PlayButtonClick();

    if (activeTransitionCoroutine != null)
      return;

    activeTransitionCoroutine = StartCoroutine(CloseSettingsAfterCountdownRoutine());
  }

  private IEnumerator CloseSettingsAfterCountdownRoutine()
  {
    if (settingsCanvas != null)
      settingsCanvas.SetActive(false);

    yield return StartCoroutine(PlayCountdownRoutine(settingsCloseCountdownSeconds));

    ResumeGameTime();
    activeTransitionCoroutine = null;
  }

  private void PauseGameTime()
  {
    if (isPaused)
      return;

    previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
    Time.timeScale = 0f;
    isPaused = true;
  }

  private void ResumeGameTime()
  {
    if (!isPaused)
      return;

    Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    isPaused = false;
  }

  private void OnBackToScanClicked()
  {
    PlayButtonClick();
    ShowConfirmationDialog(PendingConfirmationAction.BackToScan, confirmBackToScanMessage);
  }

  private void OnHomeClicked()
  {
    PlayButtonClick();
    ShowConfirmationDialog(PendingConfirmationAction.Home, confirmHomeMessage);
  }

  private void OnExitClicked()
  {
    PlayButtonClick();
    ShowConfirmationDialog(PendingConfirmationAction.Exit, confirmExitMessage);
  }

  private void ShowResumePrompt()
  {
    if (resumeCanvas == null)
    {
      if (battlePlayManager != null)
        battlePlayManager.StartFreshBattleInitialization();
      return;
    }

    Time.timeScale = 0f;
    resumeCanvas.SetActive(true);
  }

  private void OnResumePromptYes()
  {
    PlayButtonClick();

    if (activeTransitionCoroutine != null)
      return;

    BattleRuntimeState state = BattleRuntimeStateStore.GetPendingState();

    activeTransitionCoroutine = StartCoroutine(ResumeBattleAfterCountdownRoutine(state));
  }

  private IEnumerator ResumeBattleAfterCountdownRoutine(BattleRuntimeState state)
  {
    yield return StartCoroutine(PlayCountdownRoutine(resumeYesCountdownSeconds));

    if (resumeCanvas != null)
      resumeCanvas.SetActive(false);

    Time.timeScale = 1f;

    if (battlePlayManager != null)
      battlePlayManager.StartResumedBattle(state);

    activeTransitionCoroutine = null;
  }

  private IEnumerator PlayCountdownRoutine(int seconds)
  {
    int countdownSeconds = Mathf.Max(0, seconds);

    if (resumeCountdownCanvas != null)
      resumeCountdownCanvas.SetActive(countdownSeconds > 0);
    if (resumeCountdownPanel != null)
      resumeCountdownPanel.SetActive(countdownSeconds > 0);

    if (countdownSeconds > 0)
    {
      for (int remaining = countdownSeconds; remaining > 0; remaining--)
      {
        if (resumeCountdownText != null)
          resumeCountdownText.text = remaining.ToString();

        PlayCountdownSFX();

        yield return new WaitForSecondsRealtime(1f);
      }

      if (resumeCountdownText != null)
        resumeCountdownText.text = "GO!";

      PlayCountdownSFX();

      yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, goDisplaySeconds));
    }

    if (resumeCountdownCanvas != null)
      resumeCountdownCanvas.SetActive(false);
    if (resumeCountdownPanel != null)
      resumeCountdownPanel.SetActive(false);

    if (resumeCountdownCanvas == null && resumeCountdownPanel == null && resumeCountdownText == null)
    {
      Debug.LogWarning("BattleSettingsAndResumeController: Shared countdown UI is not assigned.");
    }
  }

  private void PlayCountdownSFX()
  {
    if (countdownSFX == null)
      return;

    if (countdownAudioSource != null)
    {
      countdownAudioSource.PlayOneShot(countdownSFX);
      return;
    }

    if (AudioHandler.Instance != null && AudioHandler.Instance.soundEffectsSource != null)
    {
      AudioHandler.Instance.soundEffectsSource.PlayOneShot(countdownSFX);
    }
  }

  private void OnResumePromptNo()
  {
    PlayButtonClick();
    BattleRuntimeStateStore.ClearState();

    if (resumeCanvas != null)
      resumeCanvas.SetActive(false);

    Time.timeScale = 1f;
    SceneManager.LoadScene(scanSceneName);
  }

  private void ShowConfirmationDialog(PendingConfirmationAction action, string message)
  {
    pendingConfirmationAction = action;

    if (confirmationText != null)
      confirmationText.text = message;

    if (confirmationPanel != null)
      confirmationPanel.SetActive(true);
  }

  private void OnConfirmationYesClicked()
  {
    PlayButtonClick();

    if (confirmationPanel != null)
      confirmationPanel.SetActive(false);

    Time.timeScale = 1f;

    switch (pendingConfirmationAction)
    {
      case PendingConfirmationAction.BackToScan:
        BattleRuntimeStateStore.ClearState();
        SceneManager.LoadScene(scanSceneName);
        break;

      case PendingConfirmationAction.Home:
        BattleRuntimeStateStore.ClearState();
        string previousScene = PlayerPrefs.GetString(previousScenePrefsKey, scanSceneName);
        if (string.IsNullOrWhiteSpace(previousScene))
          previousScene = scanSceneName;
        SceneManager.LoadScene(previousScene);
        break;

      case PendingConfirmationAction.Exit:
        SaveBattleSnapshotIfInProgress();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        break;
    }

    pendingConfirmationAction = PendingConfirmationAction.None;
  }

  private void OnConfirmationNoClicked()
  {
    PlayButtonClick();

    if (confirmationPanel != null)
      confirmationPanel.SetActive(false);

    pendingConfirmationAction = PendingConfirmationAction.None;
  }

  private void OnMusicSliderChanged(float value)
  {
    float volume = Mathf.Clamp01(value / 100f);

    foreach (AudioSource source in backgroundMusicSources)
    {
      if (source != null)
        source.volume = volume;
    }

    if (!isBGMusicMuted && !isAllMuted)
      lastMusicVolume = volume;

    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      GameDataManager.Instance.CurrentGameData.musicVolume = volume;
      GameDataManager.Instance.SaveGameData();
    }
  }

  private void OnSFXSliderChanged(float value)
  {
    float volume = Mathf.Clamp01(value / 100f);

    foreach (AudioSource source in soundEffectSources)
    {
      if (source != null)
        source.volume = volume;
    }

    if (!isSFXMuted && !isAllMuted)
      lastSFXVolume = volume;

    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      GameDataManager.Instance.CurrentGameData.soundVolume = volume;
      GameDataManager.Instance.SaveGameData();
    }
  }

  private void OnSensitivityChanged(float value)
  {
    foreach (UI_SwipeLook swipeLook in FindObjectsByType<UI_SwipeLook>(FindObjectsSortMode.None))
    {
      if (swipeLook != null)
        swipeLook.SetSensitivity(value);
    }

    if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
    {
      GameDataManager.Instance.CurrentGameData.lookSensitivity = value;
      GameDataManager.Instance.SaveGameData();
    }
  }

  private void OnMuteBGButtonClicked()
  {
    PlayButtonClick();

    if (!isBGMusicMuted && !isAllMuted)
    {
      isBGMusicMuted = true;
      if (musicSlider != null)
        musicSlider.interactable = false;

      foreach (AudioSource source in backgroundMusicSources)
      {
        if (source != null)
          source.volume = 0f;
      }
    }
    else if (isBGMusicMuted)
    {
      isBGMusicMuted = false;
      if (musicSlider != null)
        musicSlider.interactable = true;

      foreach (AudioSource source in backgroundMusicSources)
      {
        if (source != null)
          source.volume = lastMusicVolume;
      }
    }

    UpdateMuteAllState();
    UpdateMuteButtonTexts();
  }

  private void OnMuteSFXButtonClicked()
  {
    PlayButtonClick();

    if (!isSFXMuted && !isAllMuted)
    {
      isSFXMuted = true;
      if (sfxSlider != null)
        sfxSlider.interactable = false;

      foreach (AudioSource source in soundEffectSources)
      {
        if (source != null)
          source.volume = 0f;
      }
    }
    else if (isSFXMuted)
    {
      isSFXMuted = false;
      if (sfxSlider != null)
        sfxSlider.interactable = true;

      foreach (AudioSource source in soundEffectSources)
      {
        if (source != null)
          source.volume = lastSFXVolume;
      }
    }

    UpdateMuteAllState();
    UpdateMuteButtonTexts();
  }

  private void OnMuteAllButtonClicked()
  {
    PlayButtonClick();

    if (!isAllMuted)
    {
      isAllMuted = true;
      isBGMusicMuted = true;
      isSFXMuted = true;

      if (musicSlider != null)
        musicSlider.interactable = false;
      if (sfxSlider != null)
        sfxSlider.interactable = false;

      foreach (AudioSource source in backgroundMusicSources)
      {
        if (source != null)
          source.volume = 0f;
      }

      foreach (AudioSource source in soundEffectSources)
      {
        if (source != null)
          source.volume = 0f;
      }
    }
    else
    {
      isAllMuted = false;
      isBGMusicMuted = false;
      isSFXMuted = false;

      if (musicSlider != null)
        musicSlider.interactable = true;
      if (sfxSlider != null)
        sfxSlider.interactable = true;

      foreach (AudioSource source in backgroundMusicSources)
      {
        if (source != null)
          source.volume = lastMusicVolume;
      }

      foreach (AudioSource source in soundEffectSources)
      {
        if (source != null)
          source.volume = lastSFXVolume;
      }
    }

    UpdateMuteButtonTexts();
  }

  private void UpdateMuteAllState()
  {
    isAllMuted = isBGMusicMuted && isSFXMuted;
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

  private void BindButton(Button button, UnityEngine.Events.UnityAction action)
  {
    if (button == null)
      return;

    button.onClick.RemoveAllListeners();
    button.onClick.AddListener(action);
  }

  private void PlayButtonClick()
  {
    if (AudioHandler.Instance != null)
      AudioHandler.Instance.PlayButtonClick();
  }

  private bool IsBattleInProgress()
  {
    if (battleEnerlingManager == null || aiEnerlingManager == null || turnSystem == null)
      return false;

    IngredientDatabase.IngredientInfo player = battleEnerlingManager.GetBattleEnerling();
    IngredientDatabase.IngredientInfo ai = aiEnerlingManager.GetAIEnerling();

    return player != null && ai != null && !battleEnerlingManager.IsPlayerDefeated() && !aiEnerlingManager.IsAIDefeated();
  }

  private void SaveBattleSnapshotIfInProgress()
  {
    if (!IsBattleInProgress())
      return;

    IngredientDatabase.IngredientInfo player = battleEnerlingManager.GetBattleEnerling();
    IngredientDatabase.IngredientInfo ai = aiEnerlingManager.GetAIEnerling();

    if (player == null || ai == null)
      return;

    BattleRuntimeState snapshot = new BattleRuntimeState
    {
      hasActiveBattle = true,
      playerEnerlingName = player.ingredientName,
      opponentEnerlingName = ai.ingredientName,
      playerState = battleEnerlingManager.CaptureRuntimeState(),
      aiState = aiEnerlingManager.CaptureRuntimeState(),
      turnState = turnSystem.CaptureRuntimeState()
    };

    BattleRuntimeStateStore.SaveState(snapshot);
  }

  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus)
      SaveBattleSnapshotIfInProgress();
  }

  private void OnApplicationQuit()
  {
    SaveBattleSnapshotIfInProgress();
  }
}
