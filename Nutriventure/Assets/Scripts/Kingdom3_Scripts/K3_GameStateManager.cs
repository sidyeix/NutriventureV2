using UnityEngine;
using System;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages saving and restoring the game state for Kingdom 3 (Preservia).
/// Saves automatically on application quit / pause.
/// Works with K3_InGameSettingsButton for resume-with-countdown flow.
/// </summary>
public class K3_GameStateManager : MonoBehaviour
{
  public static K3_GameStateManager Instance { get; private set; }

  [Header("Settings")]
  [SerializeField] private bool enableDebugLogs = true;
  [SerializeField] private string kingdomSceneName = "5_Kingdom3";
  [SerializeField] private string saveFileName = "k3_game_state.json";

  private K3_GameStateSaveData currentGameState;
  private string saveFilePath;
  private bool pendingResumeAfterSceneLoad = false;
  private bool pendingSilentRestore = false;

  /// <summary>True while a resume is actively being processed.</summary>
  public bool IsResumeInProgress { get; private set; } = false;

  // Cached references – refreshed every scene load
  private K3_GameplayProgression gameplayProgression;
  private PreserviaPlayerStat playerHealth;
  private PreserviaScoringSystem scoringSystem;
  private K3_InGameSettingsButton inGameSettings;
  private K3_NPCinstructions1 npcInstructions;
  private Transform _cachedPlayer;
  private CharacterController _cachedPlayerCC;

  // ============================================================
  //  LIFECYCLE
  // ============================================================

  private void Awake()
  {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject);
      Initialize();
    }
    else
    {
      Destroy(gameObject);
    }
  }

  private void Initialize()
  {
    saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  private static readonly string[] transientScenes = { "LogoScreen", "LoadingScreen", "PlayerProfile" };

  private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    // Track last meaningful scene
    if (!Array.Exists(transientScenes, s => s == scene.name))
    {
      PlayerPrefs.SetString("LastScene", scene.name);
      PlayerPrefs.Save();
    }

    // Refresh references when entering the kingdom scene
    if (scene.name == kingdomSceneName)
    {
      StartCoroutine(RefreshReferencesNextFrame());
    }

    // If we were asked to restore, do it after the scene is ready
    if (pendingResumeAfterSceneLoad && scene.name == kingdomSceneName)
    {
      StartCoroutine(ApplySavedStateAfterLoad());
    }
    else if (pendingSilentRestore && scene.name == kingdomSceneName)
    {
      StartCoroutine(ApplySilentRestoreAfterLoad());
    }
    else if (scene.name == kingdomSceneName)
    {
      // No pending resume — clear any stale non-active save immediately
      // so no other system can restore the player position before the
      // ResumeGameCanvas decides what to do.
      K3_GameStateSaveData stale = LoadFromFile();
      if (stale != null && stale.hasSavedGameState && !stale.isGameActive)
      {
        ClearSavedGameState();
#if UNITY_EDITOR
        if (enableDebugLogs) Debug.Log("K3_GameStateManager: Cleared stale non-active save on scene load.");
#endif
      }
    }
  }

  private IEnumerator RefreshReferencesNextFrame()
  {
    yield return null;
    FindManagerReferences();
  }

  private void FindManagerReferences()
  {
    gameplayProgression = FindObjectOfType<K3_GameplayProgression>();
    playerHealth = FindObjectOfType<PreserviaPlayerStat>();
    scoringSystem = PreserviaScoringSystem.Instance;
    inGameSettings = FindObjectOfType<K3_InGameSettingsButton>();
    npcInstructions = FindObjectOfType<K3_NPCinstructions1>();

    // Cache player reference to avoid repeated Find calls
    if (_cachedPlayer == null)
    {
      GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj == null)
        playerObj = GameObject.Find("PlayerArmature");
      if (playerObj != null)
      {
        _cachedPlayer = playerObj.transform;
        _cachedPlayerCC = playerObj.GetComponent<CharacterController>();
      }
    }
  }

  // ============================================================
  //  SAVE
  // ============================================================

  public void SaveCurrentGameState()
  {
    if (!IsInKingdomScene())
    {
#if UNITY_EDITOR
            if (enableDebugLogs) Debug.Log("K3_GameStateManager: Not in kingdom scene – skipping save.");
#endif
      return;
    }

    FindManagerReferences();

    K3_GameStateSaveData saveData = new K3_GameStateSaveData();
    saveData.currentSceneName = SceneManager.GetActiveScene().name;
    saveData.saveTime = DateTime.Now.ToString("o");

    // Player position
    SavePlayerPosition(saveData);

    // Game state
    saveData.isGameActive = gameplayProgression != null && gameplayProgression.IsGameStarted2();
    saveData.gameTimer = gameplayProgression != null ? gameplayProgression.GetCurrentTime() : 0f;
    saveData.currentHealth = playerHealth != null ? playerHealth.currentHealth : 0;
    saveData.maxHealth = playerHealth != null ? playerHealth.maxHealth : 5;
    saveData.currentScore = scoringSystem != null ? scoringSystem.GetCurrentScore() : 0;

    // Game mode state
    saveData.isInGameMode = saveData.isGameActive;

    saveData.hasSavedGameState = true;
    currentGameState = saveData;
    SaveToFile(saveData);

#if UNITY_EDITOR
        if (enableDebugLogs)
        {
            Debug.Log("=== K3 GAME STATE SAVED ===");
            Debug.Log($"Scene: {saveData.currentSceneName}, Active: {saveData.isGameActive}");
            Debug.Log($"Position: {saveData.playerPosition}, Timer: {saveData.gameTimer}s");
            Debug.Log($"Health: {saveData.currentHealth}/{saveData.maxHealth}, Score: {saveData.currentScore}");
        }
#endif
  }

  private void SavePlayerPosition(K3_GameStateSaveData saveData)
  {
    if (_cachedPlayer != null)
    {
      saveData.playerPosition = _cachedPlayer.position;
      saveData.playerRotation = _cachedPlayer.rotation;
    }
  }

  // ============================================================
  //  LOAD / RESTORE
  // ============================================================

  public void LoadAndResumeGame()
  {
    K3_GameStateSaveData loaded = LoadFromFile();
    if (loaded == null || !loaded.hasSavedGameState)
    {
      Debug.LogWarning("K3_GameStateManager: No valid saved game state to load.");
      return;
    }

    currentGameState = loaded;
    IsResumeInProgress = true;

    if (IsInKingdomScene())
    {
      StartCoroutine(ApplySavedStateAfterLoad());
    }
    else
    {
      pendingResumeAfterSceneLoad = true;
      SceneManager.LoadScene(currentGameState.currentSceneName);
    }
  }

  private IEnumerator ApplySavedStateAfterLoad()
  {
    yield return null;
    yield return CoroutineYieldCache.WaitForSeconds(0.3f);

    FindManagerReferences();

    if (currentGameState != null && currentGameState.hasSavedGameState)
    {
#if UNITY_EDITOR
            if (enableDebugLogs) Debug.Log("=== APPLYING K3 SAVED GAME STATE ===");
#endif
      RestoreFullGameState();

      // Show 3-2-1 countdown before the player can move
      if (inGameSettings != null)
      {
        inGameSettings.ShowResumeCountdown();
      }
      else
      {
#if UNITY_EDITOR
                if (enableDebugLogs) Debug.LogWarning("K3_InGameSettingsButton not found — skipping resume countdown");
#endif
      }
    }

    pendingResumeAfterSceneLoad = false;
    IsResumeInProgress = false;
  }

  private void RestoreFullGameState()
  {
    if (currentGameState == null) return;

    // 1. Restore player position
    RestorePlayerPosition();

    // 2. Restore timer
    if (gameplayProgression != null && currentGameState.isGameActive)
    {
      gameplayProgression.SetTime(currentGameState.gameTimer);
      gameplayProgression.StartGame();
      gameplayProgression.PauseTimer(); // Will be resumed by countdown
    }

    // 3. Restore health
    if (playerHealth != null)
    {
      playerHealth.ResetHealth();
      int damageTaken = playerHealth.maxHealth - currentGameState.currentHealth;
      if (damageTaken > 0 && currentGameState.currentHealth > 0)
      {
        playerHealth.currentHealth = currentGameState.currentHealth;
        playerHealth.ForceRefreshHearts();
      }
    }

    // 4. Restore game-active UI (timer, hearts, score panel, etc.)
    if (currentGameState.isGameActive && npcInstructions != null)
    {
      // Mark NPC as already triggered so the intro cutscene is skipped
      npcInstructions.MarkAsTriggered();
      npcInstructions.HandlePostCutscene2DynamicUI();
    }

#if UNITY_EDITOR
        if (enableDebugLogs) Debug.Log("=== K3 GAME STATE FULLY RESTORED ===");
#endif
  }

  private void RestorePlayerPosition()
  {
    if (_cachedPlayer == null) return;

    if (_cachedPlayerCC != null) _cachedPlayerCC.enabled = false;

    _cachedPlayer.position = currentGameState.playerPosition;
    _cachedPlayer.rotation = currentGameState.playerRotation;

    if (_cachedPlayerCC != null) _cachedPlayerCC.enabled = true;

#if UNITY_EDITOR
        if (enableDebugLogs) Debug.Log($"K3: Player position restored to {currentGameState.playerPosition}");
#endif
  }

  /// <summary>
  /// Silently restores only the player position.
  /// Used when the player was just roaming (game was NOT active).
  /// </summary>
  public void SilentRestorePositionOnly()
  {
    K3_GameStateSaveData loaded = LoadFromFile();
    if (loaded == null || !loaded.hasSavedGameState)
    {
      if (enableDebugLogs) Debug.Log("K3_GameStateManager: No save data for silent restore.");
      return;
    }

    currentGameState = loaded;
    IsResumeInProgress = true;

    if (IsInKingdomScene())
    {
      StartCoroutine(ApplySilentRestoreAfterLoad());
    }
    else
    {
      pendingResumeAfterSceneLoad = false;
      pendingSilentRestore = true;
      SceneManager.LoadScene(currentGameState.currentSceneName);
    }
  }

  private IEnumerator ApplySilentRestoreAfterLoad()
  {
    yield return null;
    yield return CoroutineYieldCache.WaitForSeconds(0.3f);

    FindManagerReferences();

    if (currentGameState != null && currentGameState.hasSavedGameState)
    {
      RestorePlayerPosition();

#if UNITY_EDITOR
            if (enableDebugLogs)
                Debug.Log($"K3: Silent restore complete – player at {currentGameState.playerPosition}. Game was not active.");
#endif
    }

    pendingSilentRestore = false;
    IsResumeInProgress = false;
  }

  // ============================================================
  //  FILE I/O
  // ============================================================

  private void SaveToFile(K3_GameStateSaveData saveData)
  {
    try
    {
      string json = JsonUtility.ToJson(saveData, true);
      File.WriteAllText(saveFilePath, json);
    }
    catch (Exception e)
    {
      Debug.LogError($"K3_GameStateManager: Save failed – {e.Message}");
    }
  }

  private K3_GameStateSaveData LoadFromFile()
  {
    if (!File.Exists(saveFilePath)) return null;
    try
    {
      string json = File.ReadAllText(saveFilePath);
      return JsonUtility.FromJson<K3_GameStateSaveData>(json);
    }
    catch (Exception e)
    {
      Debug.LogError($"K3_GameStateManager: Load failed – {e.Message}");
      return null;
    }
  }

  // ============================================================
  //  PUBLIC QUERIES
  // ============================================================

  public bool HasSavedGameState()
  {
    K3_GameStateSaveData data = LoadFromFile();
    return data != null && data.hasSavedGameState;
  }

  public K3_GameStateSaveData GetLastSavedState()
  {
    return LoadFromFile();
  }

  public void ClearSavedGameState()
  {
    if (File.Exists(saveFilePath))
    {
      try
      {
        File.Delete(saveFilePath);
        currentGameState = null;
        if (enableDebugLogs) Debug.Log("K3_GameStateManager: Saved state cleared.");
      }
      catch (Exception e)
      {
        Debug.LogError($"K3_GameStateManager: Failed to clear saved state – {e.Message}");
      }
    }
  }

  public bool IsInKingdomScene()
  {
    return SceneManager.GetActiveScene().name == kingdomSceneName;
  }

  public string GetKingdomSceneName() => kingdomSceneName;

  // ============================================================
  //  AUTO-SAVE HOOKS
  // ============================================================

  private void OnApplicationPause(bool pauseStatus)
  {
    if (pauseStatus)
    {
#if UNITY_EDITOR
            if (enableDebugLogs) Debug.Log("K3_GameStateManager: App paused – saving state.");
#endif
      SaveCurrentGameState();
    }
  }

  private void OnApplicationQuit()
  {
#if UNITY_EDITOR
        if (enableDebugLogs) Debug.Log("K3_GameStateManager: App quitting – saving state.");
#endif
    SaveCurrentGameState();
  }

  private void OnDestroy()
  {
    SceneManager.sceneLoaded -= OnSceneLoaded;
  }
}

/// <summary>
/// Serializable data class for Kingdom 3 game state.
/// </summary>
[Serializable]
public class K3_GameStateSaveData
{
  public bool hasSavedGameState = false;
  public string currentSceneName;
  public string saveTime;

  // Player transform
  public Vector3 playerPosition;
  public Quaternion playerRotation;

  // Game state
  public bool isGameActive;
  public bool isInGameMode;
  public float gameTimer;
  public int currentHealth;
  public int maxHealth;
  public int currentScore;
}
