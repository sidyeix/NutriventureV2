using StarterAssets;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class K2_InGameSettingsButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProfileSettings profileSettings;
    [SerializeField] private Button settingsButton;

    [Header("Game State References")]
    [SerializeField] private GameplayProgression gameplayProgression;
    [SerializeField] private K2_GameSummary gameSummary;
    [SerializeField] private K2_GameSessionManager gameSessionManager;

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

    [Header("Scene Names")]
    [SerializeField] private string currentSceneName;

    // Private variables
    private bool isGameActive = false;
    private bool isPaused = false;
    private bool wasGameActiveWhenPaused = false;
    private float originalTimeScale;
    private Coroutine countdownCoroutine;
    private AudioSource audioSource;

    private void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;

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
                Debug.LogError("K2_InGameSettingsButton: ProfileSettings not found in scene! Please assign in inspector.");
        }

        if (gameplayProgression == null)
            gameplayProgression = FindObjectOfType<GameplayProgression>();

        if (gameSummary == null)
            gameSummary = FindObjectOfType<K2_GameSummary>();

        if (gameSessionManager == null)
            gameSessionManager = K2_GameSessionManager.Instance;
    }

    private void OnEnable()
    {
        // Re-subscribe in case OnDisable unsubscribed (e.g. parent GO was toggled)
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

        // Subscribe to ProfileSettings close event
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
        if (gameplayProgression != null)
        {
            bool gameWasActive = isGameActive;
            isGameActive = gameplayProgression.IsGameStarted2();

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

    // ============================================================
    //  OPEN / PAUSE / CLOSE
    // ============================================================

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
            // Guard against saving a 0 timeScale (e.g. another system already froze time)
            originalTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            if (wasGameActiveWhenPaused && gameplayProgression != null)
            {
                gameplayProgression.PauseTimer();
            }

#if UNITY_EDITOR
            Debug.Log(wasGameActiveWhenPaused ? "K2: Game paused (active)" : "K2: Game paused (roaming)");
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
        Debug.Log($"K2: OnProfileSettingsClosed — isPaused={isPaused}, wasGameActive={wasGameActiveWhenPaused}, originalTimeScale={originalTimeScale}");
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
            Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
            isPaused = false;
            wasGameActiveWhenPaused = false;
#if UNITY_EDITOR
            Debug.Log("K2: Settings closed while roaming — resumed immediately.");
#endif
        }
        else
        {
            // Safety: if somehow isPaused is false but timeScale is 0, force restore
            if (Time.timeScale == 0f)
            {
#if UNITY_EDITOR
                Debug.LogWarning("K2: OnProfileSettingsClosed — isPaused was false but timeScale is 0! Force restoring.");
#endif
                Time.timeScale = 1f;
            }
        }
    }

    // ============================================================
    //  RESUME
    // ============================================================

    private void OnResumeGameClicked()
    {
        PlayButtonSound();

        // Close the profile settings panel.
        // OnProfileSettingsClosed will fire automatically and handle
        // re-showing the button + starting the countdown.
        if (profileSettings != null)
        {
            profileSettings.CloseProfileSettingsDirect();
        }
        else
        {
            // Fallback: if profileSettings ref is somehow lost, resume directly
#if UNITY_EDITOR
            Debug.LogWarning("K2: OnResumeGameClicked — profileSettings is null, forcing resume.");
#endif
            ForceResume();
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
        Time.timeScale = originalTimeScale;
        isPaused = false;
        wasGameActiveWhenPaused = false;

        // Resume game timer
        if (gameplayProgression != null)
        {
            gameplayProgression.ResumeTimer();
        }

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

#if UNITY_EDITOR
        Debug.Log("K2: Game resumed");
#endif
    }

    // ============================================================
    //  RESTART
    // ============================================================

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
        Debug.Log("=== K2 IN-GAME RESTART CONFIRMED ===");
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

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Reset game session (products, player health)
        if (gameSessionManager != null)
        {
            gameSessionManager.RestartGame();
        }

        // Reset timer and restart it
        if (gameplayProgression != null)
        {
            gameplayProgression.ResetTimer();
            gameplayProgression.StartGame();
        }

        // Reset monsters and key system
        ResetMonstersAndKeys();

        // Teleport player to spawn/lobby point
        TeleportPlayerToSpawnPoint();

        // Clear any saved game state so we don't resume into old data
        if (K2_GameStateManager.Instance != null)
        {
            K2_GameStateManager.Instance.ClearSavedGameState();
        }

#if UNITY_EDITOR
        Debug.Log("=== K2 IN-GAME RESTART COMPLETE ===");
#endif
    }

    // ============================================================
    //  HOME / BACK TO MENU
    // ============================================================

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
        Debug.Log("=== K2 IN-GAME HOME CONFIRMED ===");
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

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // End the current game session
        if (gameSessionManager != null)
        {
            gameSessionManager.EndCurrentSession();
        }

        // Stop and reset the timer
        if (gameplayProgression != null)
        {
            gameplayProgression.ResetTimer();
        }

        // Apply home button game object state changes (same as GameSummary home button)
        if (gameSummary != null)
        {
            gameSummary.ApplyHomeButtonGameObjectStates();
        }

        // Reset monsters and key system
        ResetMonstersAndKeys();

        // Teleport player to spawn point
        TeleportPlayerToSpawnPoint();

        // Enable background music
        ForceEnableBackgroundMusic();

        // Clear saved game state
        if (K2_GameStateManager.Instance != null)
        {
            K2_GameStateManager.Instance.ClearSavedGameState();
        }

#if UNITY_EDITOR
        Debug.Log("=== K2 IN-GAME HOME COMPLETE ===");
#endif
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    private void ResetMonstersAndKeys()
    {
        // Reset all monsters
        MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
        for (int i = 0; i < allMonsters.Length; i++)
        {
            if (allMonsters[i] != null)
            {
                allMonsters[i].gameObject.SetActive(true);
            }
        }

        // Reset key system
        K2_CollectKey[] allKeyScripts = FindObjectsOfType<K2_CollectKey>();
        for (int i = 0; i < allKeyScripts.Length; i++)
        {
            if (allKeyScripts[i] != null)
            {
                allKeyScripts[i].gameObject.SetActive(true);
            }
        }

        // Destroy any remaining loose keys
        GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
        for (int i = 0; i < remainingKeys.Length; i++)
            Destroy(remainingKeys[i]);
    }

    /// <summary>
    /// Public accessor so other scripts (e.g. K2_ResumeGameCanvas) can trigger the teleport.
    /// </summary>
    public void TeleportPlayerToSpawnPointPublic() => TeleportPlayerToSpawnPoint();

    private void TeleportPlayerToSpawnPoint()
    {
        if (lobbyPoint == null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.Find("PlayerArmature");

        if (player == null)
        {
            return;
        }

        // Disable CharacterController before moving to avoid physics conflicts
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.position = lobbyPoint.position;
        player.transform.rotation = lobbyPoint.rotation;

        if (cc != null) cc.enabled = true;

        // Reset animator states
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

#if UNITY_EDITOR
        Debug.Log($"K2: Player teleported to {lobbyPoint.position}");
#endif
    }

    private void ForceEnableBackgroundMusic()
    {
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
                    bgSource.enabled = true;

                if (!bgSource.isPlaying && bgSource.clip != null)
                    bgSource.Play();
            }
        }
    }

    // ============================================================
    //  PUBLIC API
    // ============================================================

    /// <summary>
    /// Called externally (e.g. by K2_GameStateManager) to show the 3-2-1 countdown
    /// before the player regains control after resuming from a saved state.
    /// </summary>
    public void ShowResumeCountdown()
    {
#if UNITY_EDITOR
        Debug.Log("K2: ShowResumeCountdown called — starting countdown for save resume");
#endif

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

    /// <summary>
    /// Emergency resume — restores time and state regardless of current flags.
    /// Used as a fallback when the normal close → OnClosed → resume flow fails.
    /// </summary>
    private void ForceResume()
    {
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        Time.timeScale = 1f;

        if (isPaused && wasGameActiveWhenPaused && gameplayProgression != null)
        {
            gameplayProgression.ResumeTimer();
        }

        isPaused = false;
        wasGameActiveWhenPaused = false;

#if UNITY_EDITOR
        Debug.Log("K2: ForceResume executed — timeScale restored to 1.");
#endif
    }

    public bool IsGamePaused()
    {
        return isPaused;
    }

    public void OpenSettings()
    {
        OpenSettingsPanel();
    }

    // ============================================================
    //  AUDIO HELPERS
    // ============================================================

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

    // ============================================================
    //  CLEANUP
    // ============================================================

    private void OnDisable()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
            wasGameActiveWhenPaused = false;
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
