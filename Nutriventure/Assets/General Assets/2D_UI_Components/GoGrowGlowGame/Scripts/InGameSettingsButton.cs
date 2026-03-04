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

        // Pause the game first
        if (isGameActive)
        {
            PauseGame();
        }

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

            if (gameManager != null)
            {
                gameManager.PauseGameTimer();
            }

            Debug.Log("Game paused");
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

        // Close the profile settings panel first (hide it)
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }

        // Start countdown before resuming
        StartCountdownCoroutine();
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

        // Use GameEndManager logic for restart - EXACTLY like OnRestartClicked in GameEndManager
        if (gameEndManager != null)
        {
            // Stop count audio (like GameEndManager)
            gameEndManager.ForceStopCountAudio();

            // Force enable background music (like GameEndManager)
            ForceEnableBackgroundMusic();

            // Add rewards to game data (like GameEndManager)
            // Note: This requires public methods in GameEndManager to access rewards
            // For now, we'll rely on GameEndManager's internal logic

            // Play restart music (like GameEndManager)
            // This would need a public method in GameEndManager

            // Reset game end state (like GameEndManager)
            gameEndManager.ResetGameEndState();

            // Disable objects on home/restart (like GameEndManager)
            // This would need a public method in GameEndManager

            Debug.Log("STEP 1: Performing complete game reset...");

            // Reset minigames (like GameEndManager)
            gameEndManager.ResetMinigames();

            // Reset all continue buttons (like GameEndManager)
            ResetAllContinueButtons();

            // Full game reset (like GameEndManager)
            if (gameManager != null)
            {
                gameManager.FullGameReset();
                Debug.Log("GameManager fully reset");
            }

            Debug.Log("STEP 2: Teleporting to lobby point...");

            // Teleport to lobby point (like GameEndManager)
            TeleportPlayerToLobbyPoint();

            // Note: The restart timeline in GameEndManager is specific to that context
            // We don't need to play it here as this is an in-game restart

            Debug.Log("=== IN-GAME RESTART BUTTON COMPLETE ===");
        }
        else
        {
            // Fallback: reload current scene
            Debug.LogWarning("GameEndManager not found, reloading scene");
            SceneManager.LoadScene(currentSceneName);
        }
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

        // Use GameEndManager logic for home button - EXACTLY like OnHomeClicked in GameEndManager
        if (gameEndManager != null)
        {
            // Stop count audio (like GameEndManager)
            gameEndManager.ForceStopCountAudio();

            // Force enable background music (like GameEndManager)
            ForceEnableBackgroundMusic();

            // Add rewards to game data (like GameEndManager)
            // Note: This requires public methods in GameEndManager to access rewards

            // Check for pending key unlock (like GameEndManager)
            // This would need public methods in GameEndManager to check key state

            // Play lobby music (like GameEndManager)
            // This would need a public method in GameEndManager

            // Reset game end state (like GameEndManager)
            gameEndManager.ResetGameEndState();

            // Reset minigames for home button (like GameEndManager)
            gameEndManager.ResetMinigamesForHomeButton();

            // Reset all continue buttons (like GameEndManager)
            ResetAllContinueButtons();

            // Disable objects on home/restart (like GameEndManager)
            // This would need a public method in GameEndManager

            // Switch to player camera (handled by GameEndManager in ResetGameEndState)

            // Teleport to lobby point (like GameEndManager)
            TeleportPlayerToLobbyPoint();

            // Enable objects on home button (handled by GameEndManager in ResetGameEndState)

            Debug.Log("=== IN-GAME HOME BUTTON COMPLETE ===");
        }
        else
        {
            // Fallback: just teleport
            Debug.LogWarning("GameEndManager not found, using basic teleport");
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
    }
}