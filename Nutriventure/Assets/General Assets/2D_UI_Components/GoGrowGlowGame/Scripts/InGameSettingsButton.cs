using StarterAssets;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameSettingsButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProfileSettings profileSettings;
    [SerializeField] private Button settingsButton;

    [Header("Game State References")]
    [SerializeField] private GoGrowGlowGameManager gameManager;
    [SerializeField] private GameEndManager gameEndManager;

    [Header("Lobby Point")]
    [SerializeField] private Transform lobbyPoint;

    [Header("UI Elements")]
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown Settings")]
    [SerializeField] private float countdownStartDelay = 3f;
    [SerializeField] private AudioClip countdownTickSound;
    [SerializeField] private AudioClip countdownGoSound;

    [Header("Button States")]
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button backToHomeButton;
    [SerializeField] private Button resumeGameButton;

    [Header("Audio Source for Countdown")]
    [SerializeField] private AudioSource countdownAudioSource;

    [Header("Scene Names")]
    [SerializeField] private string logoScreenSceneName = "LogoScreen";
    [SerializeField] private string currentSceneName; // Will be set automatically

    // Private variables
    private bool isGameActive = false;
    private bool isPaused = false;
    private bool wasGameActiveWhenPaused = false;
    private float originalTimeScale;
    private Coroutine countdownCoroutine;
    private AudioSource audioSource; // For playing countdown sounds
    private bool isCountdownRunning = false;
    private float frozenSafetyTimer = 0f;
    private const float FROZEN_SAFETY_TIMEOUT = 6f;

    private void Start()
    {
        // Get current scene name
        currentSceneName = SceneManager.GetActiveScene().name;

        // Setup AudioSource for countdown sounds
        if (countdownAudioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = countdownAudioSource;
        }

        ValidateReferences();
        SetupButtonListeners();
        InitializeUI();
    }

    private void OnEnable()
    {
        if (profileSettings != null)
        {
            profileSettings.OnClosed -= OnProfileSettingsClosed;
            profileSettings.OnClosed += OnProfileSettingsClosed;
        }
    }

    private void ValidateReferences()
    {
        if (settingsButton == null)
            settingsButton = GetComponent<Button>();

        if (profileSettings == null)
            Debug.LogError("ProfileSettings reference is missing! Please assign in inspector.");

        if (gameManager == null)
            gameManager = FindObjectOfType<GoGrowGlowGameManager>();

        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<GameEndManager>();
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

        // Subscription is now handled by OnEnable/OnDisable to survive GO toggles
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
        if (gameManager != null)
        {
            bool gameWasActive = isGameActive;
            isGameActive = gameManager.IsGameActive();

            if (gameWasActive != isGameActive)
            {
                SetGameButtonsInteractable(isGameActive);
            }
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

    private void OpenSettingsPanel()
    {
        PlayButtonSound();

        // Pause the game (whether game is active or just roaming)
        PauseGame();

        // Open the profile settings and switch to settings view
        if (profileSettings != null)
        {
            profileSettings.OpenSettingsView();
        }

        // Hide settings button
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

            if (wasGameActiveWhenPaused && gameManager != null)
            {
                gameManager.PauseGameTimer();
            }

#if UNITY_EDITOR
            Debug.Log(wasGameActiveWhenPaused ? "Game paused (active)" : "Game paused (roaming)");
#endif
        }
    }

    /// <summary>
    /// Called automatically whenever ProfileSettings finishes closing (any close path).
    /// Re-shows the settings button and resumes the game if it was active.
    /// </summary>
    private void OnProfileSettingsClosed()
    {
#if UNITY_EDITOR
        Debug.Log($"K1: OnProfileSettingsClosed — isPaused={isPaused}, wasGameActive={wasGameActiveWhenPaused}, originalTimeScale={originalTimeScale}");
#endif

        // Always re-show the settings button
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // If the game was active when we paused, resume with countdown
        if (wasGameActiveWhenPaused && isPaused)
        {
            StartCountdownCoroutine();
        }
        else if (isPaused)
        {
            // Game wasn't active (just roaming) — resume immediately, no countdown
            ResumeImmediately();
#if UNITY_EDITOR
            Debug.Log("K1: Settings closed while roaming — resumed immediately.");
#endif
        }
        else
        {
            // Safety: force-restore timeScale if it's stuck at 0
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
            Debug.LogWarning("K1: EnsureTimeScaleRestored — timeScale was 0 when not paused! Forcing to 1.");
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
            Debug.LogWarning("K1: Frozen safety triggered — timeScale stuck at 0 for too long. Forcing resume.");
            ForceResume();
            frozenSafetyTimer = 0f;
        }
    }

    private void OnResumeGameClicked()
    {
        PlayButtonSound();

        if (!wasGameActiveWhenPaused)
        {
            return;
        }

        // Close the profile settings panel.
        // OnProfileSettingsClosed will fire automatically and handle
        // re-showing the button + starting the countdown.
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }
        else
        {
            ForceResume();
        }
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
        // Stop any existing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Show countdown UI
        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
        {
            // Countdown from 3
            countdownText.text = "3";
            PlaySound(countdownTickSound);
            yield return CoroutineYieldCache.WaitForSecondsRealtime(1f);

            countdownText.text = "2";
            PlaySound(countdownTickSound);
            yield return CoroutineYieldCache.WaitForSecondsRealtime(1f);

            countdownText.text = "1";
            PlaySound(countdownTickSound);
            yield return CoroutineYieldCache.WaitForSecondsRealtime(1f);

            countdownText.text = "GO!";
            PlaySound(countdownGoSound);
            yield return CoroutineYieldCache.WaitForSecondsRealtime(0.5f);

            // Stop any playing audio
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        // Hide countdown UI
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Resume game
        Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;
        countdownCoroutine = null;

        // Resume game manager timers
        if (gameManager != null)
        {
            gameManager.ResumeGameTimer();
        }

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

#if UNITY_EDITOR
        Debug.Log("K1: Game resumed");
#endif
    }

    private void OnRestartGameClicked()
    {
        PlayButtonSound();

        if (!isGameActive)
        {
            return;
        }

        // Show warning dialog before restarting
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
#if UNITY_EDITOR
        Debug.Log("=== IN-GAME RESTART CONFIRMED ===");
#endif

        // Close settings panel
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }

        // Hide countdown UI elements
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Resume time immediately before restarting
        Time.timeScale = 1f;
        isPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Delegate the entire restart flow to GameEndManager
        if (gameEndManager != null)
        {
            gameEndManager.PerformInGameRestart();
        }
        else
        {
            SceneManager.LoadScene(currentSceneName);
        }
    }

    private void OnBackToHomeClicked()
    {
        PlayButtonSound();

        if (!isGameActive)
        {
            return;
        }

        // Show warning dialog before going home
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
#if UNITY_EDITOR
        Debug.Log("=== IN-GAME HOME CONFIRMED ===");
#endif

        // Close settings panel
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }

        // Hide countdown UI elements
        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Resume time
        Time.timeScale = 1f;
        isPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Delegate the entire home flow to GameEndManager
        if (gameEndManager != null)
        {
            gameEndManager.PerformInGameHome();
        }
        else
        {
            TeleportPlayerToLobbyPoint();

            if (gameManager != null)
            {
                gameManager.FullGameReset();
            }
        }
    }

    private void ForceEnableBackgroundMusic()
    {
        // Find background music object and ensure it's enabled
        // This mirrors the ForceEnableBackgroundMusic method in GameEndManager
        GameObject backgroundMusicObject = GameObject.Find("BackgroundMusic");
        if (backgroundMusicObject == null)
            backgroundMusicObject = GameObject.Find("BGM");
        if (backgroundMusicObject == null)
            backgroundMusicObject = GameObject.Find("Music");

        if (backgroundMusicObject != null)
        {
            if (!backgroundMusicObject.activeSelf)
            {
                backgroundMusicObject.SetActive(true);
            }

            AudioSource bgSource = backgroundMusicObject.GetComponent<AudioSource>();
            if (bgSource != null)
            {
                if (!bgSource.enabled)
                {
                    bgSource.enabled = true;
                }

                if (!bgSource.isPlaying && bgSource.clip != null)
                {
                    bgSource.Play();
                }
            }
        }
    }

    private void TeleportPlayerToLobbyPoint()
    {
        if (gameManager == null || lobbyPoint == null)
        {
            return;
        }

        Transform playerTransform = gameManager.playerTransform;
        Transform playerArmature = gameManager.playerArmature;
        ThirdPersonController playerController = gameManager.playerController;

        if (playerTransform == null)
        {
            Debug.LogError("Player transform is null in GameManager!");
            return;
        }

        Vector3 targetPosition = lobbyPoint.position;
        Quaternion targetRotation = lobbyPoint.rotation;

#if UNITY_EDITOR
        Debug.Log($"Teleporting player to Lobby Point: {targetPosition}");
#endif

        // Reset animator before teleport (like GameEndManager)
        if (gameManager.characterAnimator != null)
        {
            gameManager.characterAnimator.SetBool("FreeFall", false);
            gameManager.characterAnimator.SetBool("Grounded", true);
            gameManager.characterAnimator.SetBool("Jump", false);
            gameManager.characterAnimator.ResetTrigger("Jump");
            gameManager.characterAnimator.ResetTrigger("jump");
        }

        // Handle armature parenting (like GameEndManager)
        if (playerArmature != null && playerTransform != null)
        {
            if (playerArmature.parent != playerTransform)
            {
                playerArmature.SetParent(playerTransform);
            }
            playerArmature.localPosition = Vector3.zero;
            playerArmature.localRotation = Quaternion.identity;
        }

        // Teleport the player
        playerTransform.position = targetPosition;
        playerTransform.rotation = targetRotation;

        // Update animator
        if (gameManager.characterAnimator != null)
        {
            gameManager.characterAnimator.Update(0f);
        }

        // Ensure player controller is enabled
        if (playerController != null && !playerController.enabled)
        {
            playerController.enabled = true;
        }
    }

    /// <summary>
    /// Public accessor for teleporting the player to the lobby point.
    /// Used by ResumeGameCanvas when the player clicks "No" (Restart).
    /// </summary>
    public void TeleportPlayerToLobbyPointPublic()
    {
        TeleportPlayerToLobbyPoint();
    }

    private void ResetAllContinueButtons()
    {
        ContinueButton[] continueButtons = FindObjectsOfType<ContinueButton>(true);
        for (int i = 0; i < continueButtons.Length; i++)
        {
            if (continueButtons[i] != null)
            {
                continueButtons[i].ResetButton();
            }
        }
    }

    private void PlayButtonSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    public void OpenSettings()
    {
        OpenSettingsPanel();
    }

    /// <summary>
    /// Called externally (e.g. by GameStateManager) to show the 3-2-1 countdown
    /// before the player regains control after resuming from a saved state.
    /// </summary>
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

        if (isPaused && wasGameActiveWhenPaused && gameManager != null)
        {
            gameManager.ResumeGameTimer();
        }

        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        Debug.Log("K1: ForceResume executed — timeScale restored to 1.");
    }

    public void ShowResumeCountdown()
    {
        Debug.Log("K1: ShowResumeCountdown called — starting countdown for save resume");

        // Freeze time so the game waits during countdown
        originalTimeScale = 1f;
        Time.timeScale = 0f;
        isPaused = true;
        wasGameActiveWhenPaused = true;

        // Hide the settings button during countdown
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(false);

        StartCountdownCoroutine();
    }

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

        // Unsubscribe from the close event to avoid leaks
        if (profileSettings != null)
        {
            profileSettings.OnClosed -= OnProfileSettingsClosed;
        }
    }
}