using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;

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

    private K2_GameStateManager gameStateManager;
    private bool hasResumeData = false;

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

        hasResumeData = gameStateManager.HasSavedGameState();

        if (hasResumeData)
        {
            K2_GameStateSaveData saveData = gameStateManager.GetLastSavedState();

            if (saveData != null && !saveData.isGameActive)
            {
                // Player was just roaming (game not active) – silently restore position
#if UNITY_EDITOR
                Debug.Log("K2_ResumeGameCanvas: Game was NOT active – silently restoring position.");
#endif
                gameStateManager.SilentRestorePositionOnly();
                return;
            }

            // Game WAS active – show Resume / Restart panel
#if UNITY_EDITOR
            Debug.Log("K2_ResumeGameCanvas: Game was ACTIVE – showing resume panel.");
#endif
            ShowResumeCanvas();
        }
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
        Debug.Log("K2_ResumeGameCanvas: RESUME clicked – restoring saved game state.");
#endif

        HideResumeCanvas();

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

        // Reset K2 game managers to initial state
        ResetKingdom2State();

        // If not in the kingdom scene, load it fresh
        if (gameStateManager != null && !gameStateManager.IsInKingdomScene())
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
            return;
        }

#if UNITY_EDITOR
        Debug.Log("K2_ResumeGameCanvas: Restart complete – player at lobby with fresh state.");
#endif
    }

    private void ResetKingdom2State()
    {
        // Reset timer
        GameplayProgression gp = FindObjectOfType<GameplayProgression>();
        if (gp != null)
            gp.ResetTimer();

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
                allMonsters[i].gameObject.SetActive(true);
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

        // Apply home button game object states (same objects the GameSummary toggles)
        K2_GameSummary gameSummary = FindObjectOfType<K2_GameSummary>();
        if (gameSummary != null)
            gameSummary.ApplyHomeButtonGameObjectStates();

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
