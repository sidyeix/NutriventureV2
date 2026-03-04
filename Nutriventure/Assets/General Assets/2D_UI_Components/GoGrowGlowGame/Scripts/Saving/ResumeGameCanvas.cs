using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using StarterAssets;

/// <summary>
/// Shows a "Do you want to resume?" panel when the player enters the 3_Kingdom1
/// scene and a saved game state exists on disk.
///
/// Setup:
///  1. Place this script on a Canvas in the 3_Kingdom1 scene.
///  2. Assign the panel, buttons, and text elements in the Inspector.
///  3. The canvas will call CheckForResumeData() automatically on Start().
///     - If a save exists → panel appears, player picks Resume or Restart.
///     - If no save exists → panel stays hidden and the scene runs normally.
///
/// Resume  → Restores position, energy, score, lives, timer, checkpoints, etc.
/// Restart → Clears saved data and teleports the player to the lobby point, 
///           resetting all game state as if the game hasn't been played yet.
/// </summary>
public class ResumeGameCanvas : MonoBehaviour
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
    [SerializeField] private string kingdomSceneName = "3_Kingdom1";

    [Header("Pause Gameplay While Showing")]
    [SerializeField] private bool freezeTimeWhileShowing = true;

    private GameStateManager gameStateManager;
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
        // Wire up button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        // Hide immediately
        HideResumeCanvas();

        // Wait one frame so all managers finish their Start() calls, then check
        StartCoroutine(CheckAfterDelay());
    }

    private IEnumerator CheckAfterDelay()
    {
        // Wait 2 frames + a small delay to let GameStateManager finish its Awake/Start
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(0.15f);

        gameStateManager = GameStateManager.Instance;
        CheckForResumeData();
    }

    // ============================================================
    //  CHECK & SHOW
    // ============================================================

    /// <summary>
    /// Reads the save file and shows the resume panel if a valid save exists.
    /// </summary>
    public void CheckForResumeData()
    {
        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;

        if (gameStateManager == null)
        {
            Debug.LogWarning("ResumeGameCanvas: GameStateManager not found – cannot check for resume data.");
            return;
        }

        // If GameStateManager is already restoring (user clicked Resume from main menu),
        // don't show the panel again – the state is being applied automatically.
        if (gameStateManager.IsResumeInProgress)
        {
            Debug.Log("ResumeGameCanvas: Resume already in progress – skipping panel.");
            return;
        }

        hasResumeData = gameStateManager.HasSavedGameState(kingdomSceneName);

        if (hasResumeData)
        {
            // Check if the game was actually active when the player left
            GameStateSaveData saveData = gameStateManager.GetLastSavedState();

            if (saveData != null && !saveData.isGameActive)
            {
                // Player was just roaming the kingdom (game not active).
                // Silently restore position – no panel needed.
                Debug.Log("ResumeGameCanvas: Game was NOT active – silently restoring position.");
                gameStateManager.SilentRestorePositionOnly();
                return;
            }

            // Game WAS active – show Resume / Restart panel
            Debug.Log("ResumeGameCanvas: Game was ACTIVE – showing resume panel.");
            ShowResumeCanvas();
        }
        else
        {
            Debug.Log("ResumeGameCanvas: No saved game state – starting fresh.");
        }
    }

    private void ShowResumeCanvas()
    {
        if (resumeCanvas == null) return;

        // Populate info text
        GameStateSaveData saveData = gameStateManager.GetLastSavedState();
        if (saveInfoText != null && saveData != null)
        {
            int minutes = Mathf.FloorToInt(saveData.gameTimer / 60f);
            int seconds = Mathf.FloorToInt(saveData.gameTimer % 60f);

            saveInfoText.text =
                $"Last played: {saveData.GetFormattedSaveTime()}\n" +
                $"Score: {saveData.currentScore}  |  Hearts: {saveData.currentLifeAmount:F0}\n" +
                $"Time: {minutes:00}:{seconds:00}  |  Zone: {saveData.currentFoodZone}\n" +
                $"Torches: {saveData.litTorchesCount}  |  Assessments: {saveData.growCorrectAnswers}  |  Towers: {saveData.litTowersCount}";
        }

        if (titleText != null)
            titleText.text = "Do you want to resume your game?";

        // Pause the game while the panel is up so nothing moves
        if (freezeTimeWhileShowing)
            Time.timeScale = 0f;

        // Disable player input while panel is showing
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
            // Use unscaledDeltaTime because Time.timeScale may be 0
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

        // Restore time scale
        if (freezeTimeWhileShowing)
            Time.timeScale = 1f;

        // Re-enable player input
        EnablePlayerInput();
    }

    // ============================================================
    //  BUTTON HANDLERS
    // ============================================================

    private void OnResumeClicked()
    {
        Debug.Log("ResumeGameCanvas: RESUME clicked – restoring saved game state.");

        HideResumeCanvas();

        if (gameStateManager != null)
        {
            // We are already in the kingdom scene, so LoadAndResumeGame will detect that
            // and apply the saved state directly.
            gameStateManager.LoadAndResumeGame();
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("ResumeGameCanvas: RESTART clicked – clearing save and resetting.");

        HideResumeCanvas();

        if (gameStateManager != null)
        {
            // Delete the saved state
            gameStateManager.ClearSavedGameState(kingdomSceneName);
        }

        // Reset the game manager to its initial state (lobby point, no progress)
        GoGrowGlowGameManager gm = GoGrowGlowGameManager.Instance;
        if (gm != null)
        {
            gm.FullGameReset();
        }

        // Also reset sub-game managers
        TorchMinigameManager torchMgr = FindObjectOfType<TorchMinigameManager>();
        if (torchMgr != null) torchMgr.CompleteMinigameReset();

        GrowAssessmentManager growMgr = FindObjectOfType<GrowAssessmentManager>();
        if (growMgr != null) growMgr.ResetForNewAssessmentWithoutMovingPanel();

        GlowPartManager glowMgr = FindObjectOfType<GlowPartManager>();
        if (glowMgr != null) glowMgr.ResetAllTowers();

        // Teleport player to lobby (GameEndManager handles this)
        GameEndManager gameEndMgr = FindObjectOfType<GameEndManager>();
        if (gameEndMgr != null)
        {
            gameEndMgr.ResetGameEndState();
        }

        // If we're on the main menu (not in the kingdom scene), load the scene fresh
        if (gameStateManager != null && !gameStateManager.IsInKingdomScene())
        {
            Debug.Log("ResumeGameCanvas: Restart from main menu – loading kingdom scene fresh.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
            return;
        }

        Debug.Log("ResumeGameCanvas: Restart complete – player at lobby with fresh state.");
    }

    // ============================================================
    //  CALLED FROM MAIN MENU (GeneralMenu / MainMenuController)
    // ============================================================

    /// <summary>
    /// Called by the main menu when the player taps "Start Journey".
    /// If a save exists → show the resume/restart panel.
    /// If no save exists → load the kingdom scene directly.
    /// </summary>
    public void OnStartJourneyClicked()
    {
        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;

        if (gameStateManager != null && gameStateManager.HasSavedGameState(kingdomSceneName))
        {
            // Check if the game was actually active when the player left
            GameStateSaveData saveData = gameStateManager.GetLastSavedState();

            if (saveData != null && !saveData.isGameActive)
            {
                // Player was just roaming – silently load scene and restore position
                Debug.Log("ResumeGameCanvas: Game was NOT active – loading scene and restoring position silently.");
                gameStateManager.SilentRestorePositionOnly();
                return;
            }

            // Game WAS active – show Resume/Restart panel
            Debug.Log("ResumeGameCanvas: Game was ACTIVE – showing resume panel from main menu.");
            ShowResumeCanvas();
        }
        else
        {
            Debug.Log("ResumeGameCanvas: No save data – loading kingdom scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);
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