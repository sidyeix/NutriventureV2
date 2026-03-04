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
    private float originalTimeScale;
    private Coroutine countdownCoroutine;
    private AudioSource audioSource; // For playing countdown sounds

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

        // Subscribe to ProfileSettings close event so we know when the panel closes
        // (covers the X/close button, save button, dialog yes/no — any close path)
        if (profileSettings != null)
        {
            profileSettings.OnClosed += OnProfileSettingsClosed;
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
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (isGameActive && gameManager != null)
            {
                gameManager.PauseGameTimer();
            }

            Debug.Log(isGameActive ? "Game paused (active)" : "Game paused (roaming)");
        }
    }

    /// <summary>
    /// Called automatically whenever ProfileSettings finishes closing (any close path).
    /// Re-shows the settings button and resumes the game if it was active.
    /// </summary>
    private void OnProfileSettingsClosed()
    {
        // Always re-show the settings button
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // If the game was active and we paused it, resume with countdown
        if (isGameActive && isPaused)
        {
            StartCountdownCoroutine();
        }
        else if (isPaused)
        {
            // Game wasn't active (just roaming) — resume immediately, no countdown
            Time.timeScale = originalTimeScale;
            isPaused = false;
            Debug.Log("Settings closed while roaming — resumed immediately.");
        }
    }

    private void OnResumeGameClicked()
    {
        PlayButtonSound();

        if (!isGameActive)
        {
            Debug.LogWarning("Cannot resume: Game is not active");
            return;
        }

        // Close the profile settings panel.
        // OnProfileSettingsClosed will fire automatically and handle
        // re-showing the button + starting the countdown.
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }
    }

    private void StartCountdownCoroutine()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

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
        Time.timeScale = originalTimeScale;
        isPaused = false;

        // Resume game manager timers
        if (gameManager != null)
        {
            gameManager.ResumeGameTimer();
        }

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        Debug.Log("Game resumed");
    }

    private void OnRestartGameClicked()
    {
        PlayButtonSound();

        if (!isGameActive)
        {
            Debug.LogWarning("Cannot restart: Game is not active");
            return;
        }

        Debug.Log("=== IN-GAME RESTART BUTTON CLICKED ===");

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

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Delegate the entire restart flow to GameEndManager
        // This properly resets minigames, teleports to lobby, and plays the farmer NPC cutscene
        if (gameEndManager != null)
        {
            gameEndManager.PerformInGameRestart();
        }
        else
        {
            Debug.LogWarning("GameEndManager not found, reloading scene as fallback");
            SceneManager.LoadScene(currentSceneName);
        }

        Debug.Log("=== IN-GAME RESTART BUTTON COMPLETE ===");
    }

    private void OnBackToHomeClicked()
    {
        PlayButtonSound();

        if (!isGameActive)
        {
            Debug.LogWarning("Cannot go to home: Game is not active");
            return;
        }

        Debug.Log("=== IN-GAME HOME BUTTON CLICKED ===");

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

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Delegate the entire home flow to GameEndManager
        // This properly ends the game, resets everything, disables the playable director,
        // and returns the player to the lobby
        if (gameEndManager != null)
        {
            gameEndManager.PerformInGameHome();
        }
        else
        {
            Debug.LogWarning("GameEndManager not found, using basic teleport fallback");
            TeleportPlayerToLobbyPoint();

            if (gameManager != null)
            {
                gameManager.FullGameReset();
            }
        }

        Debug.Log("=== IN-GAME HOME BUTTON COMPLETE ===");
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
                Debug.Log("BackgroundMusic GameObject ENABLED");
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
            Debug.LogError("Cannot teleport player - GameManager or Lobby Point is null!");
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

        Debug.Log($"Teleporting player to Lobby Point: {targetPosition}");

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

        Debug.Log("Player teleported to lobby point successfully");
    }

    private void ResetAllContinueButtons()
    {
        ContinueButton[] continueButtons = FindObjectsOfType<ContinueButton>(true);
        foreach (ContinueButton continueButton in continueButtons)
        {
            if (continueButton != null)
            {
                continueButton.ResetButton();
            }
        }
        Debug.Log("All Continue buttons reset");
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

    private void OnDisable()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
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