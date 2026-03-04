using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResumeGameCanvas : MonoBehaviour
{
    [Header("Canvas References")]
    [SerializeField] private GameObject resumeCanvas;
    [SerializeField] private GameObject resumePanel;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button closeButton;

    [Header("Text Elements")]
    [SerializeField] private TMP_Text saveInfoText;
    [SerializeField] private TMP_Text warningText;

    [Header("Animation")]
    [SerializeField] private float panelFadeInDuration = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Scene Management")]
    [SerializeField] private string kingdomSceneName = "3_Kingdom1";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private GameStateManager gameStateManager;
    private bool hasResumeData = false;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null && resumeCanvas != null)
            canvasGroup = resumeCanvas.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        gameStateManager = GameStateManager.Instance;

        // Set up button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HideResumeCanvas);

        // Initially hide the canvas
        HideResumeCanvas();
    }

    // Call this when the game starts (from main menu or scene load)
    public void CheckForResumeData()
    {
        if (gameStateManager == null)
            gameStateManager = GameStateManager.Instance;

        if (gameStateManager == null)
        {
            Debug.LogError("GameStateManager not found!");
            return;
        }

        hasResumeData = gameStateManager.HasSavedGameState(kingdomSceneName);

        if (hasResumeData)
        {
            ShowResumeCanvas();
        }
        else
        {
            Debug.Log("No resume data found - starting new game");
            // Optionally auto-start the game without showing canvas
        }
    }

    private void ShowResumeCanvas()
    {
        if (resumeCanvas == null) return;

        GameStateSaveData saveData = gameStateManager.GetLastSavedState();

        // Update save info text
        if (saveInfoText != null && saveData != null)
        {
            saveInfoText.text = $"Last played: {saveData.GetFormattedSaveTime()}\n" +
                               $"Progress: {saveData.litTorchesCount}/8 Torches, " +
                               $"{saveData.growCorrectAnswers}/8 Assessments, " +
                               $"{saveData.litTowersCount}/3 Towers";
        }

        // Show canvas with fade animation
        resumeCanvas.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeInCanvas());
        }
    }

    private IEnumerator FadeInCanvas()
    {
        float elapsedTime = 0f;
        while (elapsedTime < panelFadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / panelFadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private void HideResumeCanvas()
    {
        if (resumeCanvas != null)
            resumeCanvas.SetActive(false);
    }

    private void OnResumeClicked()
    {
        Debug.Log("Resume button clicked - loading saved game state");

        if (gameStateManager != null)
        {
            // Load the saved state and resume
            gameStateManager.LoadSavedGameState();

            // Hide the canvas
            HideResumeCanvas();
        }
    }

    private void OnRestartClicked()
    {
        Debug.Log("Restart button clicked - starting fresh game");

        if (gameStateManager != null)
        {
            // Clear the saved state and start fresh
            gameStateManager.ClearSavedGameState(kingdomSceneName);

            // Start the scene fresh (reload)
            UnityEngine.SceneManagement.SceneManager.LoadScene(kingdomSceneName);

            // Hide the canvas
            HideResumeCanvas();
        }
    }

    // Called when starting the game from main menu
    public void OnStartJourneyClicked()
    {
        // Check if we have resume data and show canvas if needed
        CheckForResumeData();
    }
}