using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using StarterAssets;

/// <summary>
/// Manages saving and restoring the full game state for 3_Kingdom1.
/// Attach to a persistent GameObject (DontDestroyOnLoad).
/// Saves automatically on application quit / pause.
/// Works with ResumeGameCanvas to let the player choose Resume or Restart.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private string kingdomSceneName = "3_Kingdom1";
    [SerializeField] private string saveFileName = "game_state.json";

    private GameStateSaveData currentGameState;
    private string saveFilePath;
    private bool isRestoringState = false;
    private bool pendingResumeAfterSceneLoad = false;
    private bool pendingSilentRestore = false;

    /// <summary>True while a resume is actively being processed (scene loading + state restore).
    /// ResumeGameCanvas checks this to avoid showing the panel a second time.</summary>
    public bool IsResumeInProgress { get; private set; } = false;

    // Cached references – refreshed every scene load
    private GoGrowGlowGameManager gameManager;
    private TorchMinigameManager torchManager;
    private GrowAssessmentManager growManager;
    private GlowPartManager glowManager;
    private GameEndManager gameEndManager;
    private InGameSettingsButton inGameSettings;

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

    // Scenes that should NOT be stored as "LastScene" (transitional/utility scenes)
    private static readonly string[] transientScenes = { "LogoScreen", "LoadingScreen", "PlayerProfile" };

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Track the last meaningful scene the player was in.
        // LogoManager reads this on next launch to return the player here.
        if (!System.Array.Exists(transientScenes, s => s == scene.name))
        {
            PlayerPrefs.SetString("LastScene", scene.name);
            PlayerPrefs.Save();
            if (enableDebugLogs) Debug.Log($"GameStateManager: LastScene set to '{scene.name}'");
        }

        // Always refresh references when entering the kingdom scene
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
            GameStateSaveData stale = LoadFromFile();
            if (stale != null && stale.hasSavedGameState && !stale.isGameActive)
            {
                ClearSavedGameState();
                if (enableDebugLogs) Debug.Log("GameStateManager: Cleared stale non-active save on scene load.");
            }
        }
    }

    private IEnumerator RefreshReferencesNextFrame()
    {
        yield return null; // wait one frame for all Awake/Start calls
        FindManagerReferences();
    }

    private void FindManagerReferences()
    {
        gameManager = FindObjectOfType<GoGrowGlowGameManager>();
        torchManager = FindObjectOfType<TorchMinigameManager>();
        growManager = FindObjectOfType<GrowAssessmentManager>();
        glowManager = FindObjectOfType<GlowPartManager>();
        gameEndManager = FindObjectOfType<GameEndManager>();
        inGameSettings = FindObjectOfType<InGameSettingsButton>();
    }

    // ============================================================
    //  SAVE
    // ============================================================

    /// <summary>
    /// Captures the full game state and writes it to disk.
    /// Only saves when the current scene is the kingdom scene.
    /// </summary>
    public void SaveCurrentGameState()
    {
        if (!IsInKingdomScene())
        {
            if (enableDebugLogs) Debug.Log("GameStateManager: Not in kingdom scene – skipping save.");
            return;
        }

        FindManagerReferences();

        GameStateSaveData saveData = new GameStateSaveData();
        saveData.currentSceneName = SceneManager.GetActiveScene().name;
        saveData.lastSavedScene = saveData.currentSceneName;
        saveData.saveTime = DateTime.Now;

        // --- Player transform ---
        SavePlayerPosition(saveData);

        // --- GoGrowGlowGameManager ---
        SaveGameManagerState(saveData);

        // --- Torch minigame ---
        SaveTorchProgress(saveData);

        // --- Grow assessment ---
        SaveGrowProgress(saveData);

        // --- Glow towers ---
        SaveGlowProgress(saveData);

        // --- Checkpoints ---
        SaveCheckpointInfo(saveData);

        // --- Kingdom keys ---
        SaveKingdomKeys(saveData);

        // Finalize
        saveData.hasSavedGameState = true;
        currentGameState = saveData;
        SaveToFile(saveData);

        if (enableDebugLogs)
        {
#if UNITY_EDITOR
            Debug.Log("=== GAME STATE SAVED ===");
            Debug.Log($"Scene: {saveData.currentSceneName}");
            Debug.Log($"Position: {saveData.playerPosition}");
            Debug.Log($"Energy: {saveData.currentEnergy}, Score: {saveData.currentScore}, Lives: {saveData.currentLifeAmount}");
            Debug.Log($"Timer: {saveData.gameTimer}s, Zone: {saveData.currentFoodZone}, Active: {saveData.isGameActive}");
            Debug.Log($"Torches: {saveData.litTorchesCount}, Grow: {saveData.growCorrectAnswers}, Towers: {saveData.litTowersCount} (partial: {saveData.towerEnergyNames.Count})");
            Debug.Log($"Checkpoints activated: {saveData.activatedCheckpointNames.Count}");
            Debug.Log($"Save Time: {saveData.GetFormattedSaveTime()}");
#endif
        }
    }

    // ------ individual save helpers ------

    private void SavePlayerPosition(GameStateSaveData saveData)
    {
        if (gameManager != null && gameManager.playerTransform != null)
        {
            saveData.playerPosition = gameManager.playerTransform.position;
            saveData.playerRotation = gameManager.playerTransform.rotation;

            if (gameManager.playerArmature != null)
            {
                saveData.playerArmatureRotation = gameManager.playerArmature.rotation;
                saveData.playerArmatureScale = gameManager.playerArmature.localScale;
            }
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                saveData.playerPosition = player.transform.position;
                saveData.playerRotation = player.transform.rotation;
            }
        }
    }

    private void SaveGameManagerState(GameStateSaveData saveData)
    {
        if (gameManager == null) return;

        saveData.currentEnergy = gameManager.GetCurrentEnergy();
        saveData.targetEnergy = gameManager.GetCurrentEnergy(); // use same value
        saveData.currentScore = gameManager.GetCurrentScore();
        saveData.currentLifeAmount = gameManager.GetCurrentLifeAmount();
        saveData.currentLives = gameManager.GetCurrentLives();
        saveData.gameTimer = gameManager.GetGameTimer();
        saveData.isGameActive = gameManager.IsGameActive();
        saveData.currentFoodZone = gameManager.GetCurrentFoodZone();
        saveData.isEnergyDecreasePaused = gameManager.IsEnergyDecreasePaused();
        saveData.isGameTimerPaused = gameManager.IsGameTimerPaused();
        saveData.isSpeedBoosted = gameManager.IsSpeedBoosted();
        saveData.isSizeBoosted = gameManager.IsSizeBoosted();

        // Player speed/size from controller & armature
        if (gameManager.playerController != null)
            saveData.playerSpeed = gameManager.playerController.MoveSpeed;
        if (gameManager.playerArmature != null)
            saveData.playerSize = gameManager.playerArmature.localScale.x;
    }

    private void SaveTorchProgress(GameStateSaveData saveData)
    {
        if (torchManager == null) return;
        saveData.litTorchesCount = torchManager.GetLitTorchesCount();
        saveData.litTorchIDs = torchManager.GetLitTorchIDs();
        saveData.torchMinigameCompleted = torchManager.HasCompleted();
    }

    private void SaveGrowProgress(GameStateSaveData saveData)
    {
        if (growManager == null) return;
        saveData.growCorrectAnswers = growManager.GetCorrectAnswersCount();
        saveData.growAssessmentCompleted = growManager.HasCompletedAllQuestions();
        saveData.isWaitingForEndTrigger = growManager.IsWaitingForEndTrigger();
    }

    private void SaveGlowProgress(GameStateSaveData saveData)
    {
        if (glowManager == null) return;
        saveData.litTowersCount = glowManager.GetLitTowersCount();
        saveData.litTowerNames = glowManager.GetLitTowerNames();
        saveData.glowPartCompleted = (glowManager.GetLitTowersCount() >= glowManager.GetTotalTowers());

        // Save partial tower energy for towers that are mid-transfer
        glowManager.GetTowerEnergyLevels(out List<string> energyNames, out List<float> energyValues);
        saveData.towerEnergyNames = energyNames;
        saveData.towerEnergyValues = energyValues;
    }

    private void SaveCheckpointInfo(GameStateSaveData saveData)
    {
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        saveData.activatedCheckpointNames = new List<string>();
        saveData.hasCheckpoint = false;

        foreach (Checkpoint cp in allCheckpoints)
        {
            if (cp != null && cp.IsActivated())
            {
                saveData.activatedCheckpointNames.Add(cp.gameObject.name);

                // The "current" checkpoint is the last activated one registered with the game manager
                // We'll store it; on restore we set the last one
                saveData.currentCheckpointName = cp.gameObject.name;
                saveData.hasCheckpoint = true;
            }
        }
    }

    private void SaveKingdomKeys(GameStateSaveData saveData)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null) return;
        saveData.sugariaKeyCollected = GameDataManager.Instance.HasSugariaKey();
        saveData.preserviaKeyCollected = GameDataManager.Instance.HasPreserviaKey();
        saveData.nutriKingdomKeyCollected = GameDataManager.Instance.HasNutriKingdomKey();
        saveData.allerthiaKeyCollected = GameDataManager.Instance.HasAllerthiaKey();
        saveData.ocrScannerKeyCollected = GameDataManager.Instance.HasOCRScannerKey();
    }

    // ============================================================
    //  LOAD / RESTORE
    // ============================================================

    /// <summary>
    /// Reads saved state from disk. If we are already in the kingdom scene,
    /// applies immediately; otherwise loads the scene first and applies after.
    /// </summary>
    public void LoadAndResumeGame()
    {
        GameStateSaveData loaded = LoadFromFile();
        if (loaded == null || !loaded.hasSavedGameState)
        {
#if UNITY_EDITOR
            Debug.LogWarning("GameStateManager: No valid saved game state to load.");
#endif
            return;
        }

        currentGameState = loaded;
        IsResumeInProgress = true;

        if (IsInKingdomScene())
        {
            // Already in the right scene – apply directly
            StartCoroutine(ApplySavedStateAfterLoad());
        }
        else
        {
            // Need to load scene first
            pendingResumeAfterSceneLoad = true;
            SceneManager.LoadScene(currentGameState.currentSceneName);
        }
    }

    private IEnumerator ApplySavedStateAfterLoad()
    {
        // Wait for scene objects to initialize
        yield return null;
        yield return CoroutineYieldCache.WaitForSeconds(0.3f);

        FindManagerReferences();

        if (currentGameState != null && currentGameState.hasSavedGameState)
        {
            if (enableDebugLogs) Debug.Log("=== APPLYING SAVED GAME STATE ===");
            RestoreFullGameState();

            // Show 3-2-1 countdown before the player can move
            if (inGameSettings != null)
            {
                inGameSettings.ShowResumeCountdown();
            }
            else
            {
                if (enableDebugLogs) Debug.LogWarning("InGameSettingsButton not found — skipping resume countdown");
            }
        }

        pendingResumeAfterSceneLoad = false;
        isRestoringState = false;
        IsResumeInProgress = false;
    }

    private void RestoreFullGameState()
    {
        if (currentGameState == null) return;

        // 1. Restore player position first (needs CharacterController disable/enable)
        RestorePlayerPosition();

        // 2. Restore checkpoints BEFORE game manager so SetCurrentCheckpoint works
        RestoreCheckpoints();

        // 3. Restore the main game manager state
        RestoreGameManagerState();

        // 4. Restore sub-game progress
        RestoreTorchProgress();
        RestoreGrowProgress();
        RestoreGlowProgress();

        if (enableDebugLogs) Debug.Log("=== GAME STATE FULLY RESTORED ===");
    }

    // ------ individual restore helpers ------

    private void RestorePlayerPosition()
    {
        if (gameManager == null) return;

        Transform playerTransform = gameManager.playerTransform;
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        if (playerTransform == null) return;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        playerTransform.position = currentGameState.playerPosition;
        playerTransform.rotation = currentGameState.playerRotation;

        if (gameManager.playerArmature != null)
        {
            gameManager.playerArmature.rotation = currentGameState.playerArmatureRotation;
            gameManager.playerArmature.localScale = currentGameState.playerArmatureScale;
        }

        if (cc != null) cc.enabled = true;

        if (enableDebugLogs) Debug.Log($"Player position restored to {currentGameState.playerPosition}");
    }

    private void RestoreCheckpoints()
    {
        Checkpoint[] allCheckpoints = FindObjectsOfType<Checkpoint>();
        Checkpoint lastCheckpoint = null;

        // Activate all previously activated checkpoints
        foreach (Checkpoint cp in allCheckpoints)
        {
            if (cp == null) continue;

            if (currentGameState.activatedCheckpointNames.Contains(cp.gameObject.name))
            {
                cp.Activate();
                lastCheckpoint = cp;
            }
        }

        // Set the current checkpoint to the one we saved
        if (currentGameState.hasCheckpoint && !string.IsNullOrEmpty(currentGameState.currentCheckpointName))
        {
            foreach (Checkpoint cp in allCheckpoints)
            {
                if (cp != null && cp.gameObject.name == currentGameState.currentCheckpointName)
                {
                    lastCheckpoint = cp;
                    break;
                }
            }
        }

        if (lastCheckpoint != null && gameManager != null)
        {
            gameManager.SetCurrentCheckpoint(lastCheckpoint);
            if (enableDebugLogs) Debug.Log($"Checkpoint restored: {lastCheckpoint.gameObject.name}");
        }
    }

    private void RestoreGameManagerState()
    {
        if (gameManager == null || !currentGameState.hasSavedGameState) return;

        // We call the dedicated resume method on the game manager.
        // This starts the game in a "resumed" state without the normal start sequence.
        gameManager.ResumeFromSavedState(currentGameState);

#if UNITY_EDITOR
        if (enableDebugLogs)
            Debug.Log($"GameManager restored \u2013 Energy:{currentGameState.currentEnergy} Score:{currentGameState.currentScore} " +
                      $"Lives:{currentGameState.currentLifeAmount} Timer:{currentGameState.gameTimer}s Zone:{currentGameState.currentFoodZone}");
#endif
    }

    private void RestoreTorchProgress()
    {
        if (torchManager == null) return;

        if (currentGameState.litTorchIDs != null && currentGameState.litTorchIDs.Count > 0)
        {
            torchManager.RestoreTorchStates(currentGameState.litTorchIDs);
            if (enableDebugLogs) Debug.Log($"Torches restored: {currentGameState.litTorchIDs.Count} lit");
        }
    }

    private void RestoreGrowProgress()
    {
        if (growManager == null) return;

        if (currentGameState.growCorrectAnswers > 0 || currentGameState.growAssessmentCompleted)
        {
            growManager.RestoreProgress(
                currentGameState.growCorrectAnswers,
                currentGameState.growAssessmentCompleted,
                currentGameState.isWaitingForEndTrigger
            );
            if (enableDebugLogs) Debug.Log($"Grow assessment restored: {currentGameState.growCorrectAnswers} correct");
        }
    }

    private void RestoreGlowProgress()
    {
        if (glowManager == null) return;

        if (currentGameState.litTowerNames != null && currentGameState.litTowerNames.Count > 0)
        {
            glowManager.RestoreTowerStates(currentGameState.litTowerNames);
            if (enableDebugLogs) Debug.Log($"Glow towers restored: {currentGameState.litTowerNames.Count} lit");
        }

        // Restore partial tower energy for towers that were mid-transfer
        if (currentGameState.towerEnergyNames != null && currentGameState.towerEnergyNames.Count > 0)
        {
            glowManager.RestoreTowerEnergyLevels(currentGameState.towerEnergyNames, currentGameState.towerEnergyValues);
            if (enableDebugLogs) Debug.Log($"Partial tower energy restored for {currentGameState.towerEnergyNames.Count} towers");
        }
    }

    // ============================================================
    //  FILE I/O
    // ============================================================

    private void SaveToFile(GameStateSaveData saveData)
    {
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"GameStateManager: Save failed – {e.Message}");
#endif
        }
    }

    private GameStateSaveData LoadFromFile()
    {
        if (!File.Exists(saveFilePath)) return null;
        try
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<GameStateSaveData>(json);
        }
        catch (Exception e)
        {
#if UNITY_EDITOR
            Debug.LogError($"GameStateManager: Load failed – {e.Message}");
#endif
            return null;
        }
    }

    // ============================================================
    //  PUBLIC QUERIES
    // ============================================================

    /// <summary>Returns true if there is a valid saved game state for the kingdom scene.</summary>
    public bool HasSavedGameState()
    {
        return HasSavedGameState(kingdomSceneName);
    }

    public bool HasSavedGameState(string sceneName)
    {
        GameStateSaveData data = LoadFromFile();
        return data != null && data.hasSavedGameState && data.currentSceneName == sceneName;
    }

    public GameStateSaveData GetLastSavedState()
    {
        return LoadFromFile();
    }

    /// <summary>
    /// Silently restores only the player position and checkpoints.
    /// Used when the player was just roaming the kingdom (game was NOT active).
    /// No panel is shown – the player continues where they left off.
    /// </summary>
    public void SilentRestorePositionOnly()
    {
        GameStateSaveData loaded = LoadFromFile();
        if (loaded == null || !loaded.hasSavedGameState)
        {
            if (enableDebugLogs) Debug.Log("GameStateManager: No save data for silent restore.");
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
            pendingResumeAfterSceneLoad = false; // don't trigger full restore
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
            // Only restore position, checkpoints, and sub-game progress (torches, grow, glow)
            // Do NOT call ResumeFromSavedState on the game manager (game was not active)
            RestorePlayerPosition();
            RestoreCheckpoints();
            RestoreTorchProgress();
            RestoreGrowProgress();
            RestoreGlowProgress();

#if UNITY_EDITOR
            if (enableDebugLogs)
                Debug.Log($"Silent restore complete \u2013 player at {currentGameState.playerPosition}. Game was not active, just roaming.");
#endif
        }

        pendingSilentRestore = false;
        IsResumeInProgress = false;
    }

    /// <summary>Deletes the saved state file so the next launch starts fresh.</summary>
    public void ClearSavedGameState()
    {
        ClearSavedGameState(kingdomSceneName);
    }

    public void ClearSavedGameState(string sceneName)
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                File.Delete(saveFilePath);
                currentGameState = null;
                if (enableDebugLogs) Debug.Log($"GameStateManager: Saved state cleared for {sceneName}.");
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"GameStateManager: Failed to clear saved state – {e.Message}");
#endif
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
            if (enableDebugLogs) Debug.Log("GameStateManager: App paused – saving state.");
            SaveCurrentGameState();
        }
    }

    private void OnApplicationQuit()
    {
        if (enableDebugLogs) Debug.Log("GameStateManager: App quitting – saving state.");
        SaveCurrentGameState();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
