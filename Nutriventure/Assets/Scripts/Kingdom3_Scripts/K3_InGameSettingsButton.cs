using StarterAssets;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class K3_InGameSettingsButton : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private ProfileSettings profileSettings;
  [SerializeField] private Button settingsButton;

  [Header("Game State References")]
  [SerializeField] private K3_GameplayProgression gameplayProgression;
  [SerializeField] private K3_GameSummary gameSummary;

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
        Debug.LogError("K3_InGameSettingsButton: ProfileSettings not found in scene! Please assign in inspector.");
    }

    if (gameplayProgression == null)
      gameplayProgression = FindObjectOfType<K3_GameplayProgression>();

    if (gameSummary == null)
      gameSummary = FindObjectOfType<K3_GameSummary>();
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

    PauseGame();

    if (profileSettings != null)
    {
      profileSettings.OpenSettingsView();
    }

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

      if (wasGameActiveWhenPaused && gameplayProgression != null)
      {
        gameplayProgression.PauseTimer();
      }

#if UNITY_EDITOR
            Debug.Log(wasGameActiveWhenPaused ? "K3: Game paused (active)" : "K3: Game paused (roaming)");
#endif
    }
  }

  private void OnProfileSettingsClosed()
  {
#if UNITY_EDITOR
        Debug.Log($"K3: OnProfileSettingsClosed — isPaused={isPaused}, wasGameActive={wasGameActiveWhenPaused}, originalTimeScale={originalTimeScale}");
#endif

    if (settingsButton != null)
      settingsButton.gameObject.SetActive(true);

    if (wasGameActiveWhenPaused && isPaused)
    {
      StartCountdownCoroutine();
    }
    else if (isPaused)
    {
      Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
      isPaused = false;
      wasGameActiveWhenPaused = false;
#if UNITY_EDITOR
            Debug.Log("K3: Settings closed while roaming — resumed immediately.");
#endif
    }
    else
    {
      if (Time.timeScale == 0f)
      {
#if UNITY_EDITOR
                Debug.LogWarning("K3: OnProfileSettingsClosed — isPaused was false but timeScale is 0! Force restoring.");
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

    if (profileSettings != null)
    {
      profileSettings.CloseProfileSettingsDirect();
    }
    else
    {
#if UNITY_EDITOR
            Debug.LogWarning("K3: OnResumeGameClicked — profileSettings is null, forcing resume.");
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
    if (audioSource != null && audioSource.isPlaying)
    {
      audioSource.Stop();
    }

    if (countdownCanvas != null)
      countdownCanvas.SetActive(true);

    if (countdownPanel != null)
      countdownPanel.SetActive(true);

    if (countdownText != null)
    {
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

      if (audioSource != null && audioSource.isPlaying)
      {
        audioSource.Stop();
      }
    }

    if (countdownCanvas != null)
      countdownCanvas.SetActive(false);

    if (countdownPanel != null)
      countdownPanel.SetActive(false);

    Time.timeScale = originalTimeScale;
    isPaused = false;
    wasGameActiveWhenPaused = false;

    if (gameplayProgression != null)
    {
      gameplayProgression.ResumeTimer();
    }

    if (settingsButton != null)
      settingsButton.gameObject.SetActive(true);

#if UNITY_EDITOR
        Debug.Log("K3: Game resumed");
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
        Debug.Log("=== K3 IN-GAME RESTART CONFIRMED ===");
#endif

    if (profileSettings != null)
    {
      profileSettings.CloseProfileSettingsDirect();
    }

    if (countdownCanvas != null)
      countdownCanvas.SetActive(false);

    if (countdownPanel != null)
      countdownPanel.SetActive(false);

    if (audioSource != null && audioSource.isPlaying)
    {
      audioSource.Stop();
    }

    Time.timeScale = 1f;
    isPaused = false;

    if (settingsButton != null)
      settingsButton.gameObject.SetActive(true);

    // Reset scoring system
    if (PreserviaScoringSystem.Instance != null)
    {
      PreserviaScoringSystem.Instance.ResetSessionStats();
    }

    // Reset timer (do NOT call StartGame — the NPC instruction
    // cutscene flow will restart the game once the player triggers it)
    if (gameplayProgression != null)
    {
      gameplayProgression.SetGameInProgress(false);
      gameplayProgression.ResetTimer();
    }

    // Reset health
    PreserviaPlayerStat playerHealth = FindObjectOfType<PreserviaPlayerStat>();
    if (playerHealth != null)
    {
      playerHealth.ResetHealth();
    }

    // Reset NPC interactions so they can trigger again
    ResetAllNPCInteractions();

    // Reset 2D instruction triggers so they can fire again next game
    ResetAllInstruction2DTriggers();

    ResetMonstersAndKeys();

    // --- Reset all additional K3 systems ---
    ResetAllK3Systems();

    // Teleport player to the restart point (via GameSummary)
    TeleportPlayerToRestartPoint();

    // Detach player from any moving platform
    K3_PlayerPlatformStick platformStick = FindObjectOfType<K3_PlayerPlatformStick>();
    if (platformStick != null)
      platformStick.ForceDetach();

    // Clear saved game state
    if (K3_GameStateManager.Instance != null)
    {
      K3_GameStateManager.Instance.ClearSavedGameState();
    }

    // Trigger the NPC instruction cutscene so it plays on restart
    TriggerNPCInstructionCutscene();

#if UNITY_EDITOR
        Debug.Log("=== K3 IN-GAME RESTART COMPLETE ===");
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
        Debug.Log("=== K3 IN-GAME HOME CONFIRMED ===");
#endif

    if (profileSettings != null)
    {
      profileSettings.CloseProfileSettingsDirect();
    }

    if (countdownCanvas != null)
      countdownCanvas.SetActive(false);

    if (countdownPanel != null)
      countdownPanel.SetActive(false);

    if (audioSource != null && audioSource.isPlaying)
    {
      audioSource.Stop();
    }

    Time.timeScale = 1f;
    isPaused = false;

    if (settingsButton != null)
      settingsButton.gameObject.SetActive(true);

    // Stop and reset the timer
    if (gameplayProgression != null)
    {
      gameplayProgression.SetGameInProgress(false);
      gameplayProgression.ResetTimer();
    }

    // Reset NPC interactions FIRST (before ApplyHomeButtonGameObjectStates
    // which may disable NPC GameObjects, making FindObjectsOfType miss them)
    ResetAllNPCInteractions();

    // Reset 2D instruction triggers so they can fire again next game
    ResetAllInstruction2DTriggers();

    // Apply home button game object states (same as GameSummary home button)
    if (gameSummary != null)
    {
      gameSummary.ApplyHomeButtonGameObjectStates();
    }

    // Reset monsters and key system
    ResetMonstersAndKeys();

    // --- Reset all additional K3 systems ---
    ResetAllK3Systems();

    // Detach player from any moving platform
    K3_PlayerPlatformStick platformStick = FindObjectOfType<K3_PlayerPlatformStick>();
    if (platformStick != null)
      platformStick.ForceDetach();

    // Teleport player to spawn point
    TeleportPlayerToSpawnPoint();

    // Enable background music
    ForceEnableBackgroundMusic();

    // Clear saved game state
    if (K3_GameStateManager.Instance != null)
    {
      K3_GameStateManager.Instance.ClearSavedGameState();
    }

#if UNITY_EDITOR
        Debug.Log("=== K3 IN-GAME HOME COMPLETE ===");
#endif
  }

  // ============================================================
  //  HELPERS
  // ============================================================

  private void ResetAllNPCInteractions()
  {
    K3_NPCinstructions1[] allNPCs = FindObjectsOfType<K3_NPCinstructions1>();
    for (int i = 0; i < allNPCs.Length; i++)
    {
      if (allNPCs[i] != null)
      {
        allNPCs[i].ResetInteraction();
      }
    }
  }

  private void ResetAllInstruction2DTriggers()
  {
    K2_Instructions2D[] allInstructions = FindObjectsOfType<K2_Instructions2D>();
    for (int i = 0; i < allInstructions.Length; i++)
    {
      if (allInstructions[i] != null)
      {
        allInstructions[i].ResetTrigger();
      }
    }
  }

  private void ResetMonstersAndKeys()
  {
    MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
    for (int i = 0; i < allMonsters.Length; i++)
    {
      if (allMonsters[i] != null)
      {
        allMonsters[i].gameObject.SetActive(true);
        allMonsters[i].ResetMonster();
      }
    }

    K3_CollectKey[] allKeyScripts = FindObjectsOfType<K3_CollectKey>();
    for (int i = 0; i < allKeyScripts.Length; i++)
    {
      if (allKeyScripts[i] != null)
      {
        allKeyScripts[i].gameObject.SetActive(true);
      }
    }

    GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
    for (int i = 0; i < remainingKeys.Length; i++)
      Destroy(remainingKeys[i]);
  }

  /// <summary>
  /// Resets all additional K3 systems (doors, rocks, GEMs, DYK popups,
  /// intro cutscene, death plane) so the game is in a fresh state.
  /// </summary>
  private void ResetAllK3Systems()
  {
    // Reset doors
    K3_DoorClose[] allDoors = FindObjectsOfType<K3_DoorClose>();
    for (int i = 0; i < allDoors.Length; i++)
    {
      if (allDoors[i] != null)
        allDoors[i].ResetDoor();
    }

    // Reset rocks
    K3_RocksEmerge[] allRocks = FindObjectsOfType<K3_RocksEmerge>();
    for (int i = 0; i < allRocks.Length; i++)
    {
      if (allRocks[i] != null)
        allRocks[i].ResetRocks();
    }

    // Reset Phase1 GEM system
    K3_Phase1Functions phase1 = FindObjectOfType<K3_Phase1Functions>();
    if (phase1 != null)
      phase1.ResetAllSystems();

    // Reset DYK popup system
    K3_Dyk dyk = FindObjectOfType<K3_Dyk>();
    if (dyk != null)
      dyk.ResetDyk();

    // Reset intro cutscene
    K3_IntroCutscene introCutscene = FindObjectOfType<K3_IntroCutscene>();
    if (introCutscene != null)
      introCutscene.ResetCutsceneState();

    // Reset death plane
    K3_DeathplaneFall deathPlane = FindObjectOfType<K3_DeathplaneFall>();
    if (deathPlane != null)
      deathPlane.ResetDeathPlane();
  }

  /// <summary>
  /// Triggers the NPC instruction cutscene so it plays on restart,
  /// replicating the same flow as when the player first triggers the NPC.
  /// </summary>
  private void TriggerNPCInstructionCutscene()
  {
    K3_NPCinstructions1[] allNPCs = FindObjectsOfType<K3_NPCinstructions1>();
    for (int i = 0; i < allNPCs.Length; i++)
    {
      if (allNPCs[i] != null)
      {
        allNPCs[i].TriggerCutscene();
#if UNITY_EDITOR
                Debug.Log($"K3 Restart: Triggered NPC cutscene on {allNPCs[i].gameObject.name}");
#endif
        break; // Only trigger the first NPC
      }
    }
  }

  /// <summary>
  /// Public accessor so other scripts (e.g. K3_ResumeGameCanvas) can trigger the teleport.
  /// </summary>
  public void TeleportPlayerToSpawnPointPublic() => TeleportPlayerToSpawnPoint();

  /// <summary>
  /// Teleports the player to the restart point defined in K3_GameSummary.
  /// Falls back to lobbyPoint if GameSummary or its restartPoint is not available.
  /// </summary>
  private void TeleportPlayerToRestartPoint()
  {
    K3_GameSummary summary = gameSummary != null ? gameSummary : FindObjectOfType<K3_GameSummary>();
    if (summary != null && summary.restartPoint != null)
    {
      summary.TeleportPlayerToRestartPointPublic();
      return;
    }

    // Fallback to lobby point
    TeleportPlayerToSpawnPoint();
  }

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

#if UNITY_EDITOR
        Debug.Log($"K3: Player teleported to {lobbyPoint.position}");
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
  /// Called externally (e.g. by K3_GameStateManager) to show the 3-2-1 countdown
  /// before the player regains control after resuming from a saved state.
  /// </summary>
  public void ShowResumeCountdown()
  {
#if UNITY_EDITOR
        Debug.Log("K3: ShowResumeCountdown called — starting countdown for save resume");
#endif

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
        Debug.Log("K3: ForceResume executed — timeScale restored to 1.");
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

    if (profileSettings != null)
    {
      profileSettings.OnClosed -= OnProfileSettingsClosed;
    }
  }
}
