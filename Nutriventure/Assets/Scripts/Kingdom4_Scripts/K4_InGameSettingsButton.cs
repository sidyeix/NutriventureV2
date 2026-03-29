using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class K4_InGameSettingsButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProfileSettings profileSettings;
    [SerializeField] private Button settingsButton;

    [Header("Game State References")]
    [SerializeField] private Kingdom4GameEndManager gameEndManager;

    [Header("Lobby/Spawn Point")]
    [SerializeField] private Transform lobbyPoint;

    [Header("UI Elements")]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown Settings")]
    [SerializeField] private AudioClip countdownTickSound;
    [SerializeField] private AudioClip countdownGoSound;

    [Header("Button States")]
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button backToHomeButton;
    [SerializeField] private Button resumeGameButton;

    [Header("Audio Source for Countdown")]
    [SerializeField] private AudioSource countdownAudioSource;

    // Private variables
    private bool isGameActive = false;
    private bool isPaused = false;
    private bool wasGameActiveWhenPaused = false;
    private float originalTimeScale;
    private Coroutine countdownCoroutine;
    private AudioSource audioSource;
    private bool isCountdownRunning = false;
    private float frozenSafetyTimer = 0f;
    private const float FROZEN_SAFETY_TIMEOUT = 6f;

    private void Start()
    {
        if (countdownAudioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        else
            audioSource = countdownAudioSource;

        ValidateReferences();
        SetupButtonListeners();
        InitializeUI();
    }

    private void ValidateReferences()
    {
        if (settingsButton == null)
            settingsButton = GetComponent<Button>();

        if (profileSettings == null)
        {
            profileSettings = FindObjectOfType<ProfileSettings>();
            if (profileSettings == null)
                Debug.LogError("K4_InGameSettingsButton: ProfileSettings not found!");
        }

        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
    }

    private void OnEnable()
    {
        if (profileSettings != null)
        {
            profileSettings.OnClosed -= OnProfileSettingsClosed;
            profileSettings.OnClosed += OnProfileSettingsClosed;
        }
    }

    private void SetupButtonListeners()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettingsPanel);
        }

        if (restartGameButton != null)
        {
            restartGameButton.onClick.RemoveAllListeners();
            restartGameButton.onClick.AddListener(OnRestartGameClicked);
        }

        if (backToHomeButton != null)
        {
            backToHomeButton.onClick.RemoveAllListeners();
            backToHomeButton.onClick.AddListener(OnBackToHomeClicked);
        }

        if (resumeGameButton != null)
        {
            resumeGameButton.onClick.RemoveAllListeners();
            resumeGameButton.onClick.AddListener(OnResumeGameClicked);
        }
    }

    private void InitializeUI()
    {
        SetGameButtonsInteractable(false);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }

    private void Update()
    {
        CheckGameState();
        CheckFrozenSafety();
    }

    private void CheckGameState()
    {
        if (AllergenGameManager.Instance != null)
        {
            bool gameWasActive = isGameActive;
            isGameActive = AllergenGameManager.Instance.IsGameActive;

            if (gameWasActive != isGameActive)
                SetGameButtonsInteractable(isGameActive);
        }
    }

    private void SetGameButtonsInteractable(bool interactable)
    {
        if (restartGameButton != null)
            restartGameButton.interactable = interactable;

        if (backToHomeButton != null)
            backToHomeButton.interactable = interactable;

        if (resumeGameButton != null)
            resumeGameButton.interactable = interactable;
    }

    // ============================================================
    //  OPEN / PAUSE / CLOSE
    // ============================================================

    private void OpenSettingsPanel()
    {
        PlayButtonSound();
        PauseGame();

        if (profileSettings != null)
            profileSettings.OpenSettingsView();

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(false);
    }

    private void PauseGame()
    {
        if (!isPaused)
        {
            isPaused = true;
            wasGameActiveWhenPaused = isGameActive;
            originalTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            if (wasGameActiveWhenPaused && AllergenGameManager.Instance != null)
                AllergenGameManager.Instance.PauseTimer();
        }
    }

    private void OnProfileSettingsClosed()
    {
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        if (wasGameActiveWhenPaused && isPaused)
        {
            StartCountdownCoroutine();
        }
        else if (isPaused)
        {
            ResumeImmediately();
        }
        else
        {
            EnsureTimeScaleRestored();
        }
    }

    private void ResumeImmediately()
    {
        Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;
    }

    private void EnsureTimeScaleRestored()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            isPaused = false;
            wasGameActiveWhenPaused = false;
            isCountdownRunning = false;
            frozenSafetyTimer = 0f;
        }
    }

    private void CheckFrozenSafety()
    {
        if (Time.timeScale != 0f)
        {
            frozenSafetyTimer = 0f;
            return;
        }

        if (isCountdownRunning) return;
        if (profileSettings != null && profileSettings.IsProfileSettingsOpen()) return;

        frozenSafetyTimer += Time.unscaledDeltaTime;

        if (frozenSafetyTimer >= FROZEN_SAFETY_TIMEOUT)
        {
            Debug.LogWarning("K4: Frozen safety triggered — forcing resume.");
            ForceResume();
            frozenSafetyTimer = 0f;
        }
    }

    // ============================================================
    //  RESUME
    // ============================================================

    private void OnResumeGameClicked()
    {
        PlayButtonSound();

        if (profileSettings != null)
            profileSettings.CloseProfileSettingsDirect();
        else
            ForceResume();
    }

    private void StartCountdownCoroutine()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        isCountdownRunning = true;
        frozenSafetyTimer = 0f;
        countdownCoroutine = StartCoroutine(CountdownBeforeResume());
    }

    private IEnumerator CountdownBeforeResume()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
        {
            countdownText.text = "3";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "2";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "1";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);

            countdownText.text = "GO!";
            PlaySound(countdownGoSound);
            yield return new WaitForSecondsRealtime(0.5f);

            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;
        countdownCoroutine = null;

        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.ResumeTimer();

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);
    }

    // ============================================================
    //  RESTART
    // ============================================================

    private void OnRestartGameClicked()
    {
        PlayButtonSound();

        if (!isGameActive) return;

        if (profileSettings != null)
        {
            profileSettings.ShowWarningDialog(
                "Are you sure you want to restart the game?",
                onYes: () => PerformRestart(),
                onNo: null
            );
        }
    }

    private void PerformRestart()
    {
        if (profileSettings != null)
            profileSettings.CloseProfileSettingsDirect();

        CleanupPauseState();

        // Reset all K4 game systems via the existing manager
        if (gameEndManager != null)
        {
            gameEndManager.ResetKingdom4Game();
            gameEndManager.TeleportPlayerToStartingPoint();
        }
        else if (AllergenGameManager.Instance != null)
        {
            AllergenGameManager.Instance.RestartK4Game();
        }

        // Reset ending trigger
        K4_EndingTrigger endingTrigger = FindObjectOfType<K4_EndingTrigger>();
        if (endingTrigger != null)
            endingTrigger.ResetTrigger();

        // Reset warden NPC so cutscene can play again
        WardenInteraction warden = FindObjectOfType<WardenInteraction>();
        if (warden != null)
            warden.ResetInteraction();
    }

    // ============================================================
    //  HOME / BACK TO LOBBY
    // ============================================================

    private void OnBackToHomeClicked()
    {
        PlayButtonSound();

        if (!isGameActive) return;

        if (profileSettings != null)
        {
            profileSettings.ShowWarningDialog(
                "Are you sure you want to go back to the lobby?",
                onYes: () => PerformGoHome(),
                onNo: null
            );
        }
    }

    private void PerformGoHome()
    {
        if (profileSettings != null)
            profileSettings.CloseProfileSettingsDirect();

        CleanupPauseState();

        // Reset all K4 game systems
        if (gameEndManager != null)
        {
            gameEndManager.ResetKingdom4Game();
        }
        else if (AllergenGameManager.Instance != null)
        {
            AllergenGameManager.Instance.RestartK4Game();
        }

        // Reset ending trigger
        K4_EndingTrigger endingTrigger = FindObjectOfType<K4_EndingTrigger>();
        if (endingTrigger != null)
            endingTrigger.ResetTrigger();

        // Reset warden NPC
        WardenInteraction warden = FindObjectOfType<WardenInteraction>();
        if (warden != null)
            warden.ResetInteraction();

        // Teleport player to lobby
        TeleportPlayerToLobby();

        // Enable background music
        ForceEnableBackgroundMusic();
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    private void CleanupPauseState()
    {
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        Time.timeScale = 1f;
        isPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);
    }

    private void TeleportPlayerToLobby()
    {
        if (lobbyPoint == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.Find("PlayerArmature");
        if (player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = lobbyPoint.position;
        player.transform.rotation = lobbyPoint.rotation;

        if (cc != null) cc.enabled = true;

        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool("FreeFall", false);
            animator.SetBool("Grounded", true);
            animator.SetBool("Jump", false);
            animator.ResetTrigger("Jump");
            animator.ResetTrigger("jump");
            animator.Update(0f);
        }
    }

    private void ForceEnableBackgroundMusic()
    {
        GameObject bgObj = GameObject.Find("BackgroundMusic");
        if (bgObj == null) bgObj = GameObject.Find("BGM");
        if (bgObj == null) bgObj = GameObject.Find("Music");
        if (bgObj == null) return;

        if (!bgObj.activeSelf)
            bgObj.SetActive(true);

        AudioSource bgSource = bgObj.GetComponent<AudioSource>();
        if (bgSource != null)
        {
            if (!bgSource.enabled) bgSource.enabled = true;
            if (!bgSource.isPlaying && bgSource.clip != null) bgSource.Play();
        }
    }

    // ============================================================
    //  PUBLIC API
    // ============================================================

    public void ShowResumeCountdown()
    {
        originalTimeScale = 1f;
        Time.timeScale = 0f;
        isPaused = true;
        wasGameActiveWhenPaused = true;

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(false);

        StartCountdownCoroutine();
    }

    private void ForceResume()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        Time.timeScale = 1f;

        if (isPaused && wasGameActiveWhenPaused && AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.ResumeTimer();

        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;
    }

    public bool IsGamePaused() => isPaused;

    public void OpenSettings() => OpenSettingsPanel();

    // ============================================================
    //  AUDIO HELPERS
    // ============================================================

    private void PlayButtonSound()
    {
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ============================================================
    //  CLEANUP
    // ============================================================

    private void OnDisable()
    {
        if (isPaused || Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
            isPaused = false;
            wasGameActiveWhenPaused = false;
            isCountdownRunning = false;
        }

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (profileSettings != null)
            profileSettings.OnClosed -= OnProfileSettingsClosed;
    }
}
