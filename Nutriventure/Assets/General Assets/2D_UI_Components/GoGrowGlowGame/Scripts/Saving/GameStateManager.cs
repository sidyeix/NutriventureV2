using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

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

    // References to managers
    private GameDataManager gameDataManager;
    private GoGrowGlowGameManager gameManager;
    private TorchMinigameManager torchManager;
    private GrowAssessmentManager growManager;
    private GlowPartManager glowManager;

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
        gameDataManager = GameDataManager.Instance;

        // Subscribe to scene loaded events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When scene loads, find references to managers
        FindManagerReferences();

        // If we're restoring state and this is the kingdom scene, apply the saved state
        if (isRestoringState && scene.name == kingdomSceneName)
        {
            StartCoroutine(ApplySavedStateAfterLoad());
        }
    }

    private void FindManagerReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GoGrowGlowGameManager>();

        if (torchManager == null)
            torchManager = FindObjectOfType<TorchMinigameManager>();

        if (growManager == null)
            growManager = FindObjectOfType<GrowAssessmentManager>();

        if (glowManager == null)
            glowManager = FindObjectOfType<GlowPartManager>();
    }

    private System.Collections.IEnumerator ApplySavedStateAfterLoad()
    {
        // Wait a frame for everything to initialize
        yield return null;
        yield return new WaitForSeconds(0.2f);

        if (currentGameState != null && currentGameState.hasSavedGameState)
        {
            Debug.Log("=== APPLYING SAVED GAME STATE ===");
            RestoreGameState();
        }

        isRestoringState = false;
    }

    // Call this when the game is being quit or paused
    public void SaveCurrentGameState()
    {
        if (!IsInKingdomScene())
        {
            if (enableDebugLogs)
                Debug.Log("Not in kingdom scene - skipping save");
            return;
        }

        // Find fresh references
        FindManagerReferences();

        // Create new save data
        GameStateSaveData saveData = new GameStateSaveData();
        saveData.currentSceneName = SceneManager.GetActiveScene().name;
        saveData.lastSavedScene = saveData.currentSceneName;
        saveData.saveTime = DateTime.Now;

        // Save player position
        SavePlayerPosition(saveData);

        // Save GoGrowGlow game state
        SaveGameManagerState(saveData);

        // Save torch minigame progress
        SaveTorchProgress(saveData);

        // Save grow assessment progress
        SaveGrowProgress(saveData);

        // Save glow part progress
        SaveGlowProgress(saveData);

        // Save checkpoint
        SaveCheckpointInfo(saveData);

        // Save kingdom keys (from GameDataManager)
        SaveKingdomKeys(saveData);

        // Save to file
        saveData.hasSavedGameState = true;
        currentGameState = saveData;

        SaveToFile(saveData);

        if (enableDebugLogs)
        {
            Debug.Log($"=== GAME STATE SAVED ===");
            Debug.Log($"Scene: {saveData.currentSceneName}");
            Debug.Log($"Player Position: {saveData.playerPosition}");
            Debug.Log($"Energy: {saveData.currentEnergy}, Score: {saveData.currentScore}");
            Debug.Log($"Lives: {saveData.currentLifeAmount}");
            Debug.Log($"Torches: {saveData.litTorchesCount}/8");
            Debug.Log($"Grow: {saveData.growCorrectAnswers}/8");
            Debug.Log($"Towers: {saveData.litTowersCount}/3");
            Debug.Log($"Save Time: {saveData.GetFormattedSaveTime()}");
            Debug.Log($"=== SAVE COMPLETE ===");
        }
    }

    private void SavePlayerPosition(GameStateSaveData saveData)
    {
        if (gameManager != null && gameManager.playerTransform != null)
        {
            saveData.playerPosition = gameManager.playerTransform.position;
            saveData.playerRotation = gameManager.playerTransform.rotation;
        }
        else
        {
            // Try to find player by tag
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
        if (gameManager != null)
        {
            saveData.currentEnergy = gameManager.GetCurrentEnergy();
            saveData.currentScore = gameManager.GetCurrentScore();
            saveData.currentLifeAmount = gameManager.GetCurrentLifeAmount();
            saveData.currentLives = gameManager.GetCurrentLives();
            saveData.gameTimer = gameManager.GetGameTimer();
            saveData.isGameActive = gameManager.IsGameActive();
            saveData.currentFoodZone = gameManager.GetCurrentFoodZone();
        }
    }

    private void SaveTorchProgress(GameStateSaveData saveData)
    {
        if (torchManager != null)
        {
            saveData.litTorchesCount = torchManager.GetLitTorchesCount();
            saveData.litTorchIDs = torchManager.GetLitTorchIDs();
            saveData.torchMinigameCompleted = torchManager.HasCompleted();
        }
    }

    private void SaveGrowProgress(GameStateSaveData saveData)
    {
        if (growManager != null)
        {
            saveData.growCorrectAnswers = growManager.GetCorrectAnswersCount();
            saveData.growAssessmentCompleted = growManager.HasCompletedAllQuestions();
            saveData.isWaitingForEndTrigger = growManager.IsWaitingForEndTrigger();
        }
    }

    private void SaveGlowProgress(GameStateSaveData saveData)
    {
        if (glowManager != null)
        {
            saveData.litTowersCount = glowManager.GetLitTowersCount();

            // We'll need to implement a method to get lit tower names
            // saveData.litTowerNames = glowManager.GetLitTowerNames();
            saveData.glowPartCompleted = (glowManager.GetLitTowersCount() >= glowManager.GetTotalTowers());
        }
    }

    private void SaveCheckpointInfo(GameStateSaveData saveData)
    {
        // Find active checkpoint
        Checkpoint activeCheckpoint = FindObjectOfType<Checkpoint>();
        if (activeCheckpoint != null && activeCheckpoint.IsActivated())
        {
            saveData.currentCheckpointName = activeCheckpoint.gameObject.name;
            saveData.hasCheckpoint = true;
        }
        else
        {
            saveData.hasCheckpoint = false;
        }
    }

    private void SaveKingdomKeys(GameStateSaveData saveData)
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            saveData.sugariaKeyCollected = gameDataManager.HasSugariaKey();
            saveData.preserviaKeyCollected = gameDataManager.HasPreserviaKey();
            saveData.nutriKingdomKeyCollected = gameDataManager.HasNutriKingdomKey();
            saveData.allerthiaKeyCollected = gameDataManager.HasAllerthiaKey();
            saveData.ocrScannerKeyCollected = gameDataManager.HasOCRScannerKey();
        }
    }

    private void SaveToFile(GameStateSaveData saveData)
    {
        try
        {
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game state: {e.Message}");
        }
    }

    // Call this when loading the game to restore state
    public void LoadSavedGameState()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No saved game state found");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(saveFilePath);
            currentGameState = JsonUtility.FromJson<GameStateSaveData>(jsonData);

            if (currentGameState != null && currentGameState.hasSavedGameState)
            {
                Debug.Log($"=== LOADING SAVED GAME STATE ===");
                Debug.Log($"Scene: {currentGameState.currentSceneName}");

                // Set flag to restore after scene loads
                isRestoringState = true;

                // Load the scene
                SceneManager.LoadScene(currentGameState.currentSceneName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game state: {e.Message}");
        }
    }

    private void RestoreGameState()
    {
        if (currentGameState == null) return;

        Debug.Log("=== RESTORING GAME STATE ===");

        // Find fresh references
        FindManagerReferences();

        // Restore player position
        RestorePlayerPosition();

        // Restore game manager state
        RestoreGameManagerState();

        // Restore torch progress
        RestoreTorchProgress();

        // Restore grow progress
        RestoreGrowProgress();

        // Restore glow progress
        RestoreGlowProgress();

        // Restore checkpoint
        RestoreCheckpoint();

        // Restore UI and game active state
        RestoreUI();

        Debug.Log("=== GAME STATE RESTORED ===");
    }

    private void RestorePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = currentGameState.playerPosition;
                player.transform.rotation = currentGameState.playerRotation;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = currentGameState.playerPosition;
                player.transform.rotation = currentGameState.playerRotation;
            }

            Debug.Log($"Player position restored to: {currentGameState.playerPosition}");
        }
    }

    private void RestoreGameManagerState()
    {
        if (gameManager != null && currentGameState.hasSavedGameState)
        {
            // Set energy, score, lives
            gameManager.SetEnergy(currentGameState.currentEnergy);

            // Add points to reach saved score
            int currentScore = gameManager.GetCurrentScore();
            int scoreDiff = currentGameState.currentScore - currentScore;
            if (scoreDiff > 0)
                gameManager.AddPoints(scoreDiff);

            // Restore lives (we may need a method for this)
            // gameManager.SetLives(currentGameState.currentLifeAmount);

            // Set game timer
            // We'll need a method to set the timer

            // Set food zone
            // gameManager.SetCurrentFoodZone(currentGameState.currentFoodZone);

            Debug.Log($"GameManager state restored - Energy: {currentGameState.currentEnergy}, Score: {currentGameState.currentScore}");
        }
    }

    private void RestoreTorchProgress()
    {
        if (torchManager != null)
        {
            // Restore lit torch states
            if (currentGameState.litTorchIDs != null && currentGameState.litTorchIDs.Count > 0)
            {
                torchManager.RestoreTorchStates(currentGameState.litTorchIDs);
                Debug.Log($"Restored {currentGameState.litTorchIDs.Count} lit torches");
            }
        }
    }

    private void RestoreGrowProgress()
    {
        // We'll need methods in GrowAssessmentManager to restore progress
        // This is placeholder logic
        Debug.Log($"Grow progress would be restored: {currentGameState.growCorrectAnswers}/8 correct");
    }

    private void RestoreGlowProgress()
    {
        // We'll need methods in GlowPartManager to restore progress
        Debug.Log($"Glow progress would be restored: {currentGameState.litTowersCount}/3 towers lit");
    }

    private void RestoreCheckpoint()
    {
        if (currentGameState.hasCheckpoint && !string.IsNullOrEmpty(currentGameState.currentCheckpointName))
        {
            Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
            foreach (Checkpoint checkpoint in checkpoints)
            {
                if (checkpoint.gameObject.name == currentGameState.currentCheckpointName)
                {
                    checkpoint.Activate();
                    if (gameManager != null)
                        gameManager.SetCurrentCheckpoint(checkpoint);
                    Debug.Log($"Restored checkpoint: {currentGameState.currentCheckpointName}");
                    break;
                }
            }
        }
    }

    private void RestoreUI()
    {
        // Ensure the game is in the correct active state
        if (gameManager != null)
        {
            // If game was active, make sure it's active
            if (currentGameState.isGameActive && !gameManager.IsGameActive())
            {
                // We may need to restart the game or set active state
                // gameManager.SetGameActive(true);
            }
        }
    }

    // Check if there's a saved state for a specific scene
    public bool HasSavedGameState(string sceneName)
    {
        if (!File.Exists(saveFilePath))
            return false;

        try
        {
            string jsonData = File.ReadAllText(saveFilePath);
            GameStateSaveData tempData = JsonUtility.FromJson<GameStateSaveData>(jsonData);

            return tempData != null &&
                   tempData.hasSavedGameState &&
                   tempData.currentSceneName == sceneName;
        }
        catch
        {
            return false;
        }
    }

    // Get the last saved state
    public GameStateSaveData GetLastSavedState()
    {
        if (!File.Exists(saveFilePath))
            return null;

        try
        {
            string jsonData = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<GameStateSaveData>(jsonData);
        }
        catch
        {
            return null;
        }
    }

    // Clear saved state for a scene
    public void ClearSavedGameState(string sceneName)
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                // Option 1: Delete the file
                File.Delete(saveFilePath);
                Debug.Log($"Saved game state cleared for {sceneName}");

                // Option 2: Keep file but mark as invalid
                // currentGameState = new GameStateSaveData();
                // SaveToFile(currentGameState);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to clear saved state: {e.Message}");
            }
        }
    }

    private bool IsInKingdomScene()
    {
        return SceneManager.GetActiveScene().name == kingdomSceneName;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Game paused - saving state");
            SaveCurrentGameState();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Game quitting - saving state");
        SaveCurrentGameState();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}