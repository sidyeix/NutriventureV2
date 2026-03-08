using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;

/// <summary>
/// Shows a "Do you want to resume?" panel when the player enters the 5_Kingdom3
/// scene and a saved game state exists on disk.
///
/// Setup:
///  1. Place this script on a Canvas in the 5_Kingdom3 scene.
///  2. Assign the panel, buttons, and text elements in the Inspector.
///  3. The canvas will call CheckForResumeData() automatically on Start().
///     - If a save exists AND game was ACTIVE → panel appears, player picks Resume or Restart.
///     - If a save exists AND game was NOT active (roaming) → silently restores position only.
///     - If no save exists → panel stays hidden and the scene runs normally.
/// </summary>
public class K3_ResumeGameCanvas : MonoBehaviour
{
  [Header("Canvas References")]
  [SerializeField] private GameObject resumeCanvas;
  [SerializeField] private GameObject resumePanel;

  [Header("Buttons")]
  [SerializeField] private Button resumeButton;
  [SerializeField] private Button restartButton;

  [Header("Text Elements")]
  [SerializeField] private TMP_Text titleText;
  [SerializeField] private TMP_Text saveInfoText;

  [Header("Animation")]
  [SerializeField] private float panelFadeInDuration = 0.5f;
  [SerializeField] private CanvasGroup canvasGroup;

  [Header("Scene Management")]
  [SerializeField] private string kingdomSceneName = "5_Kingdom3";

  [Header("Pause Gameplay While Showing")]
  [SerializeField] private bool freezeTimeWhileShowing = true;

  private K3_GameStateManager gameStateManager;
  private bool hasResumeData = false;
  private K3_NPCinstructions1[] cachedNPCs;

  // ============================================================
  //  LIFECYCLE
  // ============================================================

  private void Awake()
  {
    if (canvasGroup == null)
      canvasGroup = resumeCanvas != null
          ? resumeCanvas.GetComponent<CanvasGroup>()
          : GetComponent<CanvasGroup>();

    if (canvasGroup == null && resumeCanvas != null)
      canvasGroup = resumeCanvas.AddComponent<CanvasGroup>();

    // Immediately disable all NPC triggers so the intro cutscene
    // cannot fire before we finish the resume check.
    cachedNPCs = FindObjectsOfType<K3_NPCinstructions1>();
    SetNPCTriggersEnabled(false);
  }

  private void Start()
  {
    if (resumeButton != null)
      resumeButton.onClick.AddListener(OnResumeClicked);

    if (restartButton != null)
      restartButton.onClick.AddListener(OnRestartClicked);

    HideResumeCanvas();

    StartCoroutine(CheckAfterDelay());
  }

  private IEnumerator CheckAfterDelay()
  {
    // Wait 2 frames + a small delay to let K3_GameStateManager finish its Awake/Start
    yield return null;
    yield return null;
    yield return new WaitForSecondsRealtime(0.15f);

    gameStateManager = K3_GameStateManager.Instance;
    CheckForResumeData();
  }

  // ============================================================
  //  CHECK & SHOW
  // ============================================================

  public void CheckForResumeData()
  {
    if (gameStateManager == null)
      gameStateManager = K3_GameStateManager.Instance;

    if (gameStateManager == null)
    {
#if UNITY_EDITOR
            Debug.LogWarning("K3_ResumeGameCanvas: K3_GameStateManager not found – cannot check for resume data.");
#endif
      return;
    }

    // If K3_GameStateManager is already restoring, don't show the panel again
    if (gameStateManager.IsResumeInProgress)
    {
#if UNITY_EDITOR
            Debug.Log("K3_ResumeGameCanvas: Resume already in progress – skipping panel.");
#endif
      return;
    }

    hasResumeData = gameStateManager.HasSavedGameState();

    if (hasResumeData)
    {
      K3_GameStateSaveData saveData = gameStateManager.GetLastSavedState();

      if (saveData != null && !saveData.isGameActive)
      {
        // Game was NOT active (roaming) – just start at spawn point, no position restore.
#if UNITY_EDITOR
                Debug.Log("K3_ResumeGameCanvas: Game was NOT active – starting at spawn point.");
#endif
        gameStateManager.ClearSavedGameState();
        // Re-enable NPC triggers for normal roaming
        SetNPCTriggersEnabled(true);
        return;
      }

      // Game WAS active – show Resume / Restart panel
      // NPC triggers stay disabled until the player picks Resume or Restart.
#if UNITY_EDITOR
            Debug.Log("K3_ResumeGameCanvas: Game was ACTIVE – showing resume panel.");
#endif
      ShowResumeCanvas();
    }
    else
    {
      // No saved state – re-enable NPC triggers for normal play
      SetNPCTriggersEnabled(true);
    }
  }

  private void ShowResumeCanvas()
  {
    if (resumeCanvas == null) return;

    // Populate info text
    K3_GameStateSaveData saveData = gameStateManager.GetLastSavedState();
    if (saveInfoText != null && saveData != null)
    {
      int minutes = Mathf.FloorToInt(saveData.gameTimer / 60f);
      int seconds = Mathf.FloorToInt(saveData.gameTimer % 60f);

      saveInfoText.text =
          $"Score: {saveData.currentScore}  |  Hearts: {saveData.currentHealth}/{saveData.maxHealth}\n" +
          $"Time: {minutes:00}:{seconds:00}";
    }

    if (titleText != null)
      titleText.text = "Do you want to resume your game?";

    // Pause the game while the panel is up
    if (freezeTimeWhileShowing)
      Time.timeScale = 0f;

    DisablePlayerInput();

    resumeCanvas.SetActive(true);

    if (canvasGroup != null)
    {
      canvasGroup.alpha = 0f;
      StartCoroutine(FadeInCanvas());
    }
  }

  private IEnumerator FadeInCanvas()
  {
    float elapsed = 0f;
    while (elapsed < panelFadeInDuration)
    {
      elapsed += Time.unscaledDeltaTime;
      if (canvasGroup != null)
        canvasGroup.alpha = Mathf.Clamp01(elapsed / panelFadeInDuration);
      yield return null;
    }
    if (canvasGroup != null) canvasGroup.alpha = 1f;
  }

  private void HideResumeCanvas()
  {
    if (resumeCanvas != null)
      resumeCanvas.SetActive(false);

    if (freezeTimeWhileShowing)
      Time.timeScale = 1f;

    EnablePlayerInput();
  }

  // ============================================================
  //  BUTTON HANDLERS
  // ============================================================

  private void OnResumeClicked()
  {
#if UNITY_EDITOR
        Debug.Log("K3_ResumeGameCanvas: RESUME clicked – restoring saved game state.");
#endif

    HideResumeCanvas();

    // Mark all NPCs as already triggered so the intro cutscene is skipped.
    // Then re-enable their GameObjects (they just won't re-fire).
    MarkAllNPCsAsTriggered();
    SetNPCTriggersEnabled(true);

    if (gameStateManager != null)
    {
      gameStateManager.LoadAndResumeGame();
    }
  }

  private void OnRestartClicked()
  {
#if UNITY_EDITOR
        Debug.Log("K3_ResumeGameCanvas: RESTART clicked – clearing save and resetting.");
#endif

    HideResumeCanvas();

    // Re-enable NPC triggers so the intro cutscene can play again
    SetNPCTriggersEnabled(true);

    // Delete the saved state
    if (gameStateManager != null)
    {
      gameStateManager.ClearSavedGameState();
    }

    // Reset K3 game managers to initial state
    ResetKingdom3State();

    // If not in the kingdom scene, load it fresh
    if (gameStateManager != null && !gameStateManager.IsInKingdomScene())
    {
      UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
      return;
    }

    // Already in the kingdom scene – replay the intro cutscene only if the key hasn't been collected yet
    bool allerthiaKeyCollected = GameDataManager.Instance != null &&
                                 GameDataManager.Instance.CurrentGameData != null &&
                                 GameDataManager.Instance.CurrentGameData.allerthiaKeyCollected;
    if (!allerthiaKeyCollected)
    {
      K3_IntroCutscene introCutscene = FindObjectOfType<K3_IntroCutscene>();
      if (introCutscene != null)
        introCutscene.PlayCutscene();
    }

#if UNITY_EDITOR
        Debug.Log("K3_ResumeGameCanvas: Restart complete – player at lobby with fresh state.");
#endif
  }

  private void ResetKingdom3State()
  {
    // Reset timer
    K3_GameplayProgression gp = FindObjectOfType<K3_GameplayProgression>();
    if (gp != null)
      gp.ResetTimer();

    // Reset health
    PreserviaPlayerStat playerHealth = FindObjectOfType<PreserviaPlayerStat>();
    if (playerHealth != null)
      playerHealth.ResetHealth();

    // Reset scoring
    if (PreserviaScoringSystem.Instance != null)
      PreserviaScoringSystem.Instance.ResetSessionStats();

    // Reset NPC interactions so they can trigger again
    K3_NPCinstructions1[] allNPCs = FindObjectsOfType<K3_NPCinstructions1>();
    for (int i = 0; i < allNPCs.Length; i++)
    {
      if (allNPCs[i] != null)
        allNPCs[i].ResetInteraction();
    }

    // Reset 2D instruction triggers so they can fire again next game
    K2_Instructions2D[] allInstructions = FindObjectsOfType<K2_Instructions2D>();
    for (int i = 0; i < allInstructions.Length; i++)
    {
      if (allInstructions[i] != null)
        allInstructions[i].ResetTrigger();
    }

    // Reset monsters
    MonsterObstacle[] allMonsters = FindObjectsOfType<MonsterObstacle>();
    for (int i = 0; i < allMonsters.Length; i++)
    {
      if (allMonsters[i] != null)
      {
        allMonsters[i].gameObject.SetActive(true);
        allMonsters[i].ResetMonster();
      }
    }

    // Reset keys
    K3_CollectKey[] allKeyScripts = FindObjectsOfType<K3_CollectKey>();
    for (int i = 0; i < allKeyScripts.Length; i++)
    {
      if (allKeyScripts[i] != null)
        allKeyScripts[i].gameObject.SetActive(true);
    }

    // Destroy any remaining loose keys
    GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
    for (int i = 0; i < remainingKeys.Length; i++)
      Destroy(remainingKeys[i]);

    // --- Reset all additional K3 systems ---

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

    // Detach player from any moving platform
    K3_PlayerPlatformStick platformStick = FindObjectOfType<K3_PlayerPlatformStick>();
    if (platformStick != null)
      platformStick.ForceDetach();

    // Apply home button game object states (same objects the GameSummary toggles)
    K3_GameSummary gameSummary = FindObjectOfType<K3_GameSummary>();
    if (gameSummary != null)
      gameSummary.ApplyHomeButtonGameObjectStates();

    // Teleport player to lobby
    TeleportPlayerToLobby();
  }

  private void TeleportPlayerToLobby()
  {
    // Find the lobby point via K3_InGameSettingsButton (it already has the lobbyPoint reference)
    K3_InGameSettingsButton settings = FindObjectOfType<K3_InGameSettingsButton>();
    if (settings != null)
    {
      settings.TeleportPlayerToSpawnPointPublic();
      return;
    }

    // Fallback: try to find a spawn point tagged object
    GameObject spawnObj = GameObject.Find("LobbyPoint");
    if (spawnObj == null)
      spawnObj = GameObject.Find("SpawnPoint");

    if (spawnObj != null)
    {
      GameObject player = GameObject.FindGameObjectWithTag("Player");
      if (player == null)
        player = GameObject.Find("PlayerArmature");

      if (player != null)
      {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = spawnObj.transform.position;
        if (cc != null) cc.enabled = true;
      }
    }
  }

  // ============================================================
  //  PLAYER INPUT HELPERS
  // ============================================================

  // ============================================================
  //  NPC TRIGGER HELPERS
  // ============================================================

  /// <summary>
  /// Enable or disable all NPC trigger colliders so the intro cutscene
  /// cannot fire while we are showing the resume panel.
  /// </summary>
  private void SetNPCTriggersEnabled(bool enabled)
  {
    if (cachedNPCs == null) return;
    for (int i = 0; i < cachedNPCs.Length; i++)
    {
      if (cachedNPCs[i] != null)
      {
        Collider col = cachedNPCs[i].GetComponent<Collider>();
        if (col != null)
          col.enabled = enabled;
      }
    }
#if UNITY_EDITOR
        Debug.Log($"K3_ResumeGameCanvas: NPC triggers {(enabled ? "ENABLED" : "DISABLED")} ({(cachedNPCs != null ? cachedNPCs.Length : 0)} NPCs)");
#endif
  }

  /// <summary>
  /// Mark every NPC as already triggered so their cutscene will not replay
  /// when the player resumes into the middle of a game.
  /// </summary>
  private void MarkAllNPCsAsTriggered()
  {
    if (cachedNPCs == null) return;
    for (int i = 0; i < cachedNPCs.Length; i++)
    {
      if (cachedNPCs[i] != null)
      {
        // Set hasTriggered = true via the public API
        // TriggerCutscene checks hasTriggered and exits early
        cachedNPCs[i].MarkAsTriggered();
      }
    }
#if UNITY_EDITOR
        Debug.Log("K3_ResumeGameCanvas: All NPCs marked as already triggered – cutscene will be skipped.");
#endif
  }

  // ============================================================
  //  PLAYER INPUT HELPERS
  // ============================================================

  private void DisablePlayerInput()
  {
    ThirdPersonController tpc = FindObjectOfType<ThirdPersonController>();
    if (tpc != null) tpc.enabled = false;
  }

  private void EnablePlayerInput()
  {
    ThirdPersonController tpc = FindObjectOfType<ThirdPersonController>();
    if (tpc != null) tpc.enabled = true;
  }
}
