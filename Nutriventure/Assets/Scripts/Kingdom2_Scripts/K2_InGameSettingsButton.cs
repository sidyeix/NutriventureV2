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

    [Header("Restart Point")]
    [Tooltip("Where the player is teleported on Restart. If unset, falls back to lobbyPoint.")]
    [SerializeField] private Transform restartPoint;

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

    [Header("Sugardino NPC")]
    [Tooltip("Drag the Sugardino NPC trigger GameObject here. It will be re-enabled and reset on Restart / Home.")]
    [SerializeField] private GameObject sugardinoNPC;

    [Header("Scene Names")]
    [SerializeField] private string currentSceneName;

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

        // Subscription is now handled by OnEnable/OnDisable to survive GO toggles
    }

    private void InitializeUI()
    {
        SetGameButtonsInteractable(false);

#if UNITY_EDITOR
        if (restartGameButton == null) Debug.LogWarning("K2_InGameSettingsButton: restartGameButton is NOT assigned!");
        if (backToHomeButton == null) Debug.LogWarning("K2_InGameSettingsButton: backToHomeButton is NOT assigned!");
        if (resumeGameButton == null) Debug.LogWarning("K2_InGameSettingsButton: resumeGameButton is NOT assigned!");
#endif

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

    private bool lastGameActiveState = false;

    private void CheckGameState()
    {
        if (gameplayProgression != null)
        {
            bool newState = gameplayProgression.IsGameStarted2();
            if (newState != lastGameActiveState)
            {
                lastGameActiveState = newState;
                isGameActive = newState;
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

        // Force-refresh button interactability so restart/home/resume
        // are disabled when the player is just roaming (not in-game).
        SetGameButtonsInteractable(isGameActive);

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
            ResumeImmediately();
#if UNITY_EDITOR
            Debug.Log("K2: Settings closed while roaming — resumed immediately.");
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
            Debug.LogWarning("K2: EnsureTimeScaleRestored — timeScale was 0 when not paused! Forcing to 1.");
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
            Debug.LogWarning("K2: Frozen safety triggered — timeScale stuck at 0 for too long. Forcing resume.");
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
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // Reset game session (products, player health)
        if (gameSessionManager != null)
        {
            gameSessionManager.RestartGame();
        }

        // Respawn products so they appear in the world again
        RespawnAllProducts();

        // Reset the timer and mark the game as not in-progress.
        // The game will start again when the player approaches
        // the Sugardino NPC and confirms the instruction panel.
        if (gameplayProgression != null)
        {
            gameplayProgression.SetGameInProgress(false);
            gameplayProgression.ResetTimer();
        }

        // Reset monsters and key system
        ResetMonstersAndKeys();

        // Reset NPC interactions, instructions, DummypTimeline cutscenes, DYK, and scoring
        ResetNPCAndInstructionState();

        // Teleport player to the restart point (not the lobby)
        TeleportPlayerToRestartPoint();

        // Clear any saved game state so we don't resume into old data
        if (K2_GameStateManager.Instance != null)
        {
            K2_GameStateManager.Instance.ClearSavedGameState();
        }

        // Auto-trigger the first NPC cutscene so the game restarts from the beginning
        TriggerSugardinoNPCCutscene();

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
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        // Show settings button again
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(true);

        // End the current game session
        if (gameSessionManager != null)
        {
            gameSessionManager.EndCurrentSession();
        }

        // Stop the game and reset the timer
        if (gameplayProgression != null)
        {
            gameplayProgression.SetGameInProgress(false);
            gameplayProgression.ResetTimer();
        }

        // Apply home button game object state changes (same as GameSummary home button)
        if (gameSummary != null)
        {
            gameSummary.ApplyHomeButtonGameObjectStates();
        }

        // Reset monsters and key system
        ResetMonstersAndKeys();

        // Reset NPC interactions, instructions, DummypTimeline cutscenes, DYK, and scoring
        ResetNPCAndInstructionState();

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
                allMonsters[i].ResetMonster();
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
    /// Resets all NPC interactions, instruction triggers, DummypTimeline cutscenes,
    /// DYK popups, and scoring so the game returns to its initial "lobby" state.
    /// </summary>
    private void ResetNPCAndInstructionState()
    {
        // Re-enable the Sugardino NPC GameObject (may have been disabled
        // after its cutscene played or in the inspector). Must happen
        // BEFORE FindObjectsOfType since disabled objects aren't found.
        if (sugardinoNPC != null)
        {
            sugardinoNPC.SetActive(true);

            // Also reset its K2_NPCtrigInstructs directly since
            // the FindObjectsOfType below might not pick it up
            // if the GO was just re-enabled this frame.
            K2_NPCtrigInstructs npcTrig = sugardinoNPC.GetComponent<K2_NPCtrigInstructs>();
            if (npcTrig != null)
                npcTrig.ResetInteraction();
        }

        // Reset NPC trigger interactions (arrow indicator, hasTriggered, etc.)
        K2_NPCtrigInstructs[] allNPCs = FindObjectsOfType<K2_NPCtrigInstructs>();
        for (int i = 0; i < allNPCs.Length; i++)
        {
            if (allNPCs[i] != null)
                allNPCs[i].ResetInteraction();
        }

        // Reset 2D instruction triggers
        K2_Instructions2D[] allInstructions = FindObjectsOfType<K2_Instructions2D>();
        for (int i = 0; i < allInstructions.Length; i++)
        {
            if (allInstructions[i] != null)
                allInstructions[i].ResetTrigger();
        }

        // Reset DummypTimeline cutscene state (dialogue, subtitle, dynamic UI)
        K2_DummypTimeline dummyTimeline = FindObjectOfType<K2_DummypTimeline>();
        if (dummyTimeline != null)
            dummyTimeline.ResetAllCutscenes();

        // Reset DYK popup system
        K2_Dyk dyk = FindObjectOfType<K2_Dyk>();
        if (dyk != null)
            dyk.ResetPopupSystem();

        // Reset scoring
        SugariaScoringSystem scoring = FindObjectOfType<SugariaScoringSystem>();
        if (scoring != null)
            scoring.ResetSessionStats();

        // Reset QA1 assessment fully (trigger, spawned products, UI)
        K2_QA1system qa1 = FindObjectOfType<K2_QA1system>();
        if (qa1 != null)
            qa1.ResetForNewGame();

        // Reset QA2 assessment
        K2_QA2system qa2 = FindObjectOfType<K2_QA2system>();
        if (qa2 != null)
            qa2.ClearScannedProducts();

        // Respawn the dummy product (hidden, not destroyed, on collection)
        CollectProducts collectProducts = FindObjectOfType<CollectProducts>();
        if (collectProducts != null)
            collectProducts.RespawnDummyProduct();
    }

    /// <summary>
    /// Public accessor — teleports to the lobby (home button).
    /// </summary>
    public void TeleportPlayerToSpawnPointPublic() => TeleportPlayerToSpawnPoint();

    /// <summary>
    /// Public accessor — teleports to the restart point (restart button).
    /// </summary>
    public void TeleportPlayerToRestartPointPublic() => TeleportPlayerToRestartPoint();

    /// <summary>
    /// Teleports the player to restartPoint. Falls back to lobbyPoint if restartPoint is unset.
    /// </summary>
    private void TeleportPlayerToRestartPoint()
    {
        Transform target = restartPoint != null ? restartPoint : lobbyPoint;
        TeleportPlayerTo(target);
    }

    /// <summary>
    /// Triggers the Sugardino NPC cutscene so the game replays from the very
    /// beginning after a restart. Mirrors K3's TriggerNPCInstructionCutscene().
    /// </summary>
    public void TriggerSugardinoNPCCutscene()
    {
        // Prefer the explicit inspector reference
        if (sugardinoNPC != null)
        {
            K2_NPCtrigInstructs npcTrig = sugardinoNPC.GetComponent<K2_NPCtrigInstructs>();
            if (npcTrig != null)
            {
                npcTrig.TriggerCutscene();
#if UNITY_EDITOR
                Debug.Log("K2: Auto-triggered Sugardino NPC cutscene via inspector ref");
#endif
                return;
            }
        }

        // Fallback: find the first K2_NPCtrigInstructs in the scene
        K2_NPCtrigInstructs[] allNPCs = FindObjectsOfType<K2_NPCtrigInstructs>();
        for (int i = 0; i < allNPCs.Length; i++)
        {
            if (allNPCs[i] != null)
            {
                allNPCs[i].TriggerCutscene();
#if UNITY_EDITOR
                Debug.Log($"K2: Auto-triggered NPC cutscene on {allNPCs[i].gameObject.name}");
#endif
                break;
            }
        }
    }

    /// <summary>
    /// Respawns all products so they appear in the world again.
    /// </summary>
    private void RespawnAllProducts()
    {
        ProductSpawner spawner = FindObjectOfType<ProductSpawner>();
        if (spawner != null)
        {
            System.Reflection.MethodInfo respawnMethod = spawner.GetType().GetMethod("RespawnProducts");
            if (respawnMethod != null)
            {
                respawnMethod.Invoke(spawner, null);
            }
            else
            {
                spawner.SpawnProducts();
            }
        }
    }

    private void TeleportPlayerToSpawnPoint()
    {
        TeleportPlayerTo(lobbyPoint);
    }

    private void TeleportPlayerTo(Transform target)
    {
        if (target == null)
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

        player.transform.position = target.position;
        player.transform.rotation = target.rotation;

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
        Debug.Log($"K2: Player teleported to {target.position}");
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

        if (isPaused && wasGameActiveWhenPaused && gameplayProgression != null)
        {
            gameplayProgression.ResumeTimer();
        }

        isPaused = false;
        wasGameActiveWhenPaused = false;
        isCountdownRunning = false;
        frozenSafetyTimer = 0f;

        Debug.Log("K2: ForceResume executed — timeScale restored to 1.");
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
