using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows a "Do you want to resume?" panel when the player enters the 4_Kingdom 2
/// scene and a saved game state exists on disk.
///
/// Setup:
///  1. Place this script on a Canvas in the 4_Kingdom 2 scene.
///  2. Assign the panel, buttons, and text elements in the Inspector.
///  3. The canvas will call CheckForResumeData() automatically on Start().
///     - If a save exists → panel appears, player picks Resume or Restart.
///     - If no save exists → panel stays hidden and the scene runs normally.
///
/// Resume  → Restores position, health, score, timer, etc.
/// Restart → Clears saved data and teleports the player to the lobby point,
///           resetting all game state as if the game hasn't been played yet.
/// </summary>
public class K2_ResumeGameCanvas : MonoBehaviour
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
    [SerializeField] private string kingdomSceneName = "4_Kingdom 2";

    [Header("Pause Gameplay While Showing")]
    [SerializeField] private bool freezeTimeWhileShowing = true;

    [Header("Sugardino NPC")]
    [Tooltip("Drag the Sugardino NPC trigger GameObject here so it can be re-enabled on Restart.")]
    [SerializeField] private GameObject sugardinoNPC;

    private K2_GameStateManager gameStateManager;
    private bool hasResumeData = false;
    private K2_Instructions2D[] cachedInstructions;
    private K2_NPCtrigInstructs[] cachedNPCs;

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

        // Cache NPC / instruction triggers so we can disable them
        // while the resume panel is showing (prevents cutscene from firing).
        cachedInstructions = FindObjectsOfType<K2_Instructions2D>();
        cachedNPCs = FindObjectsOfType<K2_NPCtrigInstructs>();
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
        // Wait 2 frames + a small delay to let K2_GameStateManager finish its Awake/Start
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.15f);

        gameStateManager = K2_GameStateManager.Instance;
        CheckForResumeData();
    }

    // ============================================================
    //  CHECK & SHOW
    // ============================================================

    public void CheckForResumeData()
    {
        if (gameStateManager == null)
            gameStateManager = K2_GameStateManager.Instance;

        if (gameStateManager == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("K2_ResumeGameCanvas: K2_GameStateManager not found – cannot check for resume data.");
#endif
            SetNPCTriggersEnabled(true);
            return;
        }

        // If K2_GameStateManager is already restoring, don't show the panel again
        if (gameStateManager.IsResumeInProgress)
        {
#if UNITY_EDITOR
            Debug.Log("K2_ResumeGameCanvas: Resume already in progress – skipping panel.");
#endif
            return;
        }

        // Check whether the Preservia key has already been collected
        bool preserviaKeyCollected = GameDataManager.Instance != null &&
                                     GameDataManager.Instance.CurrentGameData != null &&
                                     GameDataManager.Instance.CurrentGameData.preserviaKeyCollected;

        hasResumeData = gameStateManager.HasSavedGameState();

        if (hasResumeData)
        {
            K2_GameStateSaveData saveData = gameStateManager.GetLastSavedState();

            if (saveData != null && saveData.isGameActive)
            {
                // Game WAS active – player has real progress.
                // Show Resume / Restart panel regardless of key state.
                // NPC triggers stay disabled until the player picks Resume or Restart.
#if UNITY_EDITOR
                Debug.Log("K2_ResumeGameCanvas: Game was ACTIVE – showing resume panel.");
#endif
                ShowResumeCanvas();
            }
            else
            {
                // Game was NOT active (roaming) – just start at spawn point, no position restore.
#if UNITY_EDITOR
                Debug.Log("K2_ResumeGameCanvas: Game was NOT active – starting at spawn point.");
#endif
                gameStateManager.ClearSavedGameState();
                SetNPCTriggersEnabled(true);

                // Play intro cutscene if key not yet collected
                if (!preserviaKeyCollected)
                {
                    TriggerIntroCutscene();
                }
            }
        }
        else
        {
            // No saved state at all.
            SetNPCTriggersEnabled(true);

            if (!preserviaKeyCollected)
            {
                // First-time entry with key not collected – play intro cutscene.
#if UNITY_EDITOR
                Debug.Log("K2_ResumeGameCanvas: No save & key NOT collected – playing intro cutscene.");
#endif
                TriggerIntroCutscene();
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log("K2_ResumeGameCanvas: No save & key already collected – proceeding normally.");
            }
#endif
        }
    }

    /// <summary>
    /// Finds the K2_IntroCutsceneManager in the scene and plays it.
    /// </summary>
    private void TriggerIntroCutscene()
    {
        K2_IntroCutsceneManager introCutscene = FindObjectOfType<K2_IntroCutsceneManager>();
        if (introCutscene != null)
        {
            introCutscene.PlayCutscene();
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("K2_ResumeGameCanvas: K2_IntroCutsceneManager not found in scene!");
        }
#endif
    }

    private void ShowResumeCanvas()
    {
        if (resumeCanvas == null) return;

        // Populate info text
        K2_GameStateSaveData saveData = gameStateManager.GetLastSavedState();
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
        Debug.Log("K2_ResumeGameCanvas: RESUME clicked – restoring saved game state (no cutscene).");
#endif

        HideResumeCanvas();

        // Mark all NPC / instruction triggers as already triggered so the
        // intro cutscene and instruction panels won't replay on resume.
        MarkAllTriggersAsTriggered();
        SetNPCTriggersEnabled(true);

        if (gameStateManager != null)
        {
            gameStateManager.LoadAndResumeGame();
        }
    }

    private void OnRestartClicked()
    {
#if UNITY_EDITOR
        Debug.Log("K2_ResumeGameCanvas: RESTART clicked – clearing save and resetting.");
#endif

        HideResumeCanvas();

        // Delete the saved state
        if (gameStateManager != null)
        {
            gameStateManager.ClearSavedGameState();
        }

        // Reset K2 game managers to initial state (NPCs, timer, health, etc.)
        ResetKingdom2State();

        // Re-enable NPC triggers AFTER the reset to prevent premature firing
        SetNPCTriggersEnabled(true);

        // If not in the kingdom scene, load it fresh
        if (gameStateManager != null && !gameStateManager.IsInKingdomScene())
        {
            SceneManager.LoadScene(kingdomSceneName);
            return;
        }

        // Already in the kingdom scene.
        // Play the intro cutscene ONLY if the Preservia key has NOT been collected.
        // If the key is already collected the player just restarts at the lobby.
        bool preserviaKeyCollected = GameDataManager.Instance != null &&
                                     GameDataManager.Instance.CurrentGameData != null &&
                                     GameDataManager.Instance.CurrentGameData.preserviaKeyCollected;

        if (!preserviaKeyCollected)
        {
            TriggerIntroCutscene();
        }

#if UNITY_EDITOR
        Debug.Log($"K2_ResumeGameCanvas: Restart complete – key collected: {preserviaKeyCollected}");
#endif
    }

    private void ResetKingdom2State()
    {
        // Stop the game and reset timer
        GameplayProgression gp = FindObjectOfType<GameplayProgression>();
        if (gp != null)
        {
            gp.SetGameInProgress(false);
            gp.ResetTimer();
        }

        // Reset health
        SugariaPlayerStat playerHealth = FindObjectOfType<SugariaPlayerStat>();
        if (playerHealth != null)
            playerHealth.ResetHealth();

        // Reset scoring
        SugariaScoringSystem scoring = FindObjectOfType<SugariaScoringSystem>();
        if (scoring != null)
            scoring.ResetSessionStats();

        // End the current game session
        K2_GameSessionManager sessionMgr = K2_GameSessionManager.Instance;
        if (sessionMgr != null)
            sessionMgr.EndCurrentSession();

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
        K2_CollectKey[] allKeyScripts = FindObjectsOfType<K2_CollectKey>();
        for (int i = 0; i < allKeyScripts.Length; i++)
        {
            if (allKeyScripts[i] != null)
                allKeyScripts[i].gameObject.SetActive(true);
        }

        // Destroy any remaining loose keys
        GameObject[] remainingKeys = GameObject.FindGameObjectsWithTag("NutriKey");
        for (int i = 0; i < remainingKeys.Length; i++)
            Destroy(remainingKeys[i]);

        // Re-enable the Sugardino NPC so the player can trigger it again
        if (sugardinoNPC != null)
        {
            sugardinoNPC.SetActive(true);
            K2_NPCtrigInstructs npcTrig = sugardinoNPC.GetComponent<K2_NPCtrigInstructs>();
            if (npcTrig != null)
                npcTrig.ResetInteraction();
        }

        // Reset NPC interactions so they can trigger again
        K2_NPCtrigInstructs[] allNPCs = FindObjectsOfType<K2_NPCtrigInstructs>();
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

        // Reset DummypTimeline cutscene state
        K2_DummypTimeline dummyTimeline = FindObjectOfType<K2_DummypTimeline>();
        if (dummyTimeline != null)
            dummyTimeline.ResetAllCutscenes();

        // Reset DYK popup system
        K2_Dyk dyk = FindObjectOfType<K2_Dyk>();
        if (dyk != null)
            dyk.ResetPopupSystem();

        // Apply home button game object states (same objects the GameSummary toggles)
        K2_GameSummary gameSummary = FindObjectOfType<K2_GameSummary>();
        if (gameSummary != null)
            gameSummary.ApplyHomeButtonGameObjectStates();

        // Respawn the dummy product (hidden on collection, not destroyed)
        CollectProducts collectProducts = FindObjectOfType<CollectProducts>();
        if (collectProducts != null)
            collectProducts.RespawnDummyProduct();

        // Teleport player to lobby
        TeleportPlayerToLobby();
    }

    private void TeleportPlayerToLobby()
    {
        // Find the lobby point via K2_InGameSettingsButton (it already has the lobbyPoint reference)
        K2_InGameSettingsButton settings = FindObjectOfType<K2_InGameSettingsButton>();
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
    //  NPC / INSTRUCTION TRIGGER HELPERS
    // ============================================================

    /// <summary>
    /// Enable or disable all NPC and instruction trigger colliders so the intro
    /// cutscene cannot fire while we are showing the resume panel.
    /// </summary>
    private void SetNPCTriggersEnabled(bool enabled)
    {
        if (cachedNPCs != null)
        {
            for (int i = 0; i < cachedNPCs.Length; i++)
            {
                if (cachedNPCs[i] != null)
                {
                    Collider col = cachedNPCs[i].GetComponent<Collider>();
                    if (col != null) col.enabled = enabled;
                }
            }
        }

        if (cachedInstructions != null)
        {
            for (int i = 0; i < cachedInstructions.Length; i++)
            {
                if (cachedInstructions[i] != null)
                {
                    Collider col = cachedInstructions[i].GetComponent<Collider>();
                    if (col != null) col.enabled = enabled;
                }
            }
        }

#if UNITY_EDITOR
        Debug.Log($"K2_ResumeGameCanvas: Triggers {(enabled ? "ENABLED" : "DISABLED")}");
#endif
    }

    /// <summary>
    /// Mark every NPC and instruction trigger as already triggered so their
    /// cutscene / panel will not replay when the player resumes.
    /// </summary>
    private void MarkAllTriggersAsTriggered()
    {
        if (cachedNPCs != null)
        {
            for (int i = 0; i < cachedNPCs.Length; i++)
            {
                if (cachedNPCs[i] != null)
                    cachedNPCs[i].MarkAsTriggered();
            }
        }

#if UNITY_EDITOR
        Debug.Log("K2_ResumeGameCanvas: All NPC triggers marked as already triggered.");
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
