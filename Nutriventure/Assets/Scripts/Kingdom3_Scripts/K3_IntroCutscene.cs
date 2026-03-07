using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class K3_IntroCutscene : MonoBehaviour
{
    [Header("Timeline References")]
    [SerializeField] private GameObject timelineParentObject;
    [SerializeField] private PlayableDirector timelineDirector;

    [Header("UI References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button skipButton;

    [Header("UI Canvas")]
    [SerializeField] private GameObject gameUICanvas; // This will be disabled during cutscene

    [Header("Dialogue Canvas - Subtitle System")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private bool enableDialogueCanvas = true;
    [SerializeField] private TMP_Text titleText;

    [Header("Voice Title Display")]
    [SerializeField] private GameObject voiceTitleObject; // NEW: Will be enabled only during timeline playback
    [SerializeField] private TMP_Text voiceTitleText; // NEW: Text component on voiceTitleObject

    [Header("Audio Handler")]
    [SerializeField] private GameObject audioHandler;

    [Header("Objects To Disable During Cutscene")]
    [SerializeField] private GameObject[] objectsToDisableDuringCutscene;

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipDelay = 1.0f;

    [Header("Input Actions")]
    [SerializeField] private InputAction skipAction;

    private bool isCutscenePlaying = false;
    private bool dialogueCanvasWasActive = false;
    private bool titleTextWasActive = false;
    private bool gameUICanvasWasActive = false; // Track GameUI Canvas state

    void Start()
    {
        InitializeAllComponents();

        // Add click listener to buttons
        if (startButton != null)
        {
            startButton.onClick.AddListener(PlayCutscene);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCutscene);
        }

        // Setup input action for skipping
        if (allowSkip)
        {
            skipAction = new InputAction("SkipCutscene");
            skipAction.AddBinding("<Keyboard>/space");
            skipAction.AddBinding("<Keyboard>/escape");
            skipAction.AddBinding("<Keyboard>/enter");
            skipAction.AddBinding("<Gamepad>/buttonSouth");
            skipAction.performed += ctx => SkipCutscene();
        }

        // Auto-play the cutscene on scene enter if the Allerthia key has NOT been collected yet
        bool allerthiaKeyCollected = GameDataManager.Instance != null &&
                                     GameDataManager.Instance.CurrentGameData != null &&
                                     GameDataManager.Instance.CurrentGameData.allerthiaKeyCollected;

        // Skip auto-play if there is a saved game state (the resume canvas handles that flow)
        bool hasSavedState = K3_GameStateManager.Instance != null &&
                             K3_GameStateManager.Instance.HasSavedGameState();

        if (!allerthiaKeyCollected && !hasSavedState)
        {
            PlayCutscene();
        }
    }

    private void InitializeAllComponents()
    {
        // Ensure timeline parent is disabled initially
        if (timelineParentObject != null)
        {
            timelineParentObject.SetActive(false);
        }

        // Ensure PlayableDirector is stopped
        if (timelineDirector != null)
        {
            timelineDirector.Stop();
            timelineDirector.stopped += OnTimelineFinished;
        }

        // Initialize GameUI Canvas state
        if (gameUICanvas != null)
        {
            // Store whether it was active before initialization
            gameUICanvasWasActive = gameUICanvas.activeSelf;
            Debug.Log($"GameUI Canvas initialized - Was active: {gameUICanvasWasActive}");
        }

        // Initialize dialogue canvas
        if (dialogueCanvas != null)
        {
            dialogueCanvasWasActive = dialogueCanvas.activeSelf;
            dialogueCanvas.SetActive(false);
        }

        // Initialize title text
        if (titleText != null)
        {
            titleTextWasActive = titleText.gameObject.activeSelf;
            titleText.gameObject.SetActive(false);
            Debug.Log($"Title text initialized: {titleText.name}, was active: {titleTextWasActive}");
        }

        // Initialize voice title object - ALWAYS disabled initially
        if (voiceTitleObject != null)
        {
            voiceTitleObject.SetActive(false);
            Debug.Log($"Voice title object initialized: {voiceTitleObject.name}, set to inactive");

            // Initialize voice title text if assigned
            if (voiceTitleText != null)
            {
                voiceTitleText.gameObject.SetActive(false);
            }
        }

        // Ensure skip button is disabled initially
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // Ensure audio handler is enabled initially
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }

        Debug.Log("Cutscene Manager initialized");
    }

    void OnEnable()
    {
        if (allowSkip)
        {
            skipAction.Enable();
        }
    }

    void OnDisable()
    {
        if (allowSkip)
        {
            skipAction.Disable();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (timelineDirector != null)
        {
            timelineDirector.stopped -= OnTimelineFinished;
        }

        if (allowSkip)
        {
            skipAction.Dispose();
        }
    }

    public void PlayCutscene()
    {
        PlayCutsceneWithTitle(null);
    }

    public void PlayCutscene(string customTitleText = null)
    {
        PlayCutsceneWithTitle(customTitleText);
    }

    private void PlayCutsceneWithTitle(string customTitleText = null)
    {
        if (timelineParentObject != null && timelineDirector != null)
        {
            isCutscenePlaying = true;


            // Enable the timeline parent object — the timeline itself manages
            // enabling the subtitle/dialogue canvas via its own Activation tracks
            timelineParentObject.SetActive(true);

            // ENABLE voice title object when timeline starts playing
            if (voiceTitleObject != null)
            {
                voiceTitleObject.SetActive(true);
                Debug.Log("Voice title object enabled for cutscene");

                // Also enable the text component if assigned
                if (voiceTitleText != null && !voiceTitleText.gameObject.activeSelf)
                {
                    voiceTitleText.gameObject.SetActive(true);
                }
            }

            // DISABLE GameUI Canvas when cutscene starts
            if (gameUICanvas != null)
            {
                gameUICanvas.SetActive(false);
                Debug.Log("GameUI Canvas disabled for cutscene");
            }

            // Play the timeline
            timelineDirector.Play();

            // Disable the start button
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            // Disable audio handler during cutscene
            if (audioHandler != null)
            {
                audioHandler.SetActive(false);
            }

            // Show skip button after delay (if allowed)
            if (allowSkip && skipButton != null)
            {
                StartCoroutine(ShowSkipButtonAfterDelay());
            }

            Debug.Log("Cutscene started - Voice title enabled, GameUI Canvas disabled");

            // Disable extra objects during cutscene
            if (objectsToDisableDuringCutscene != null)
            {
                foreach (var obj in objectsToDisableDuringCutscene)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("Timeline references are not set!");
        }
    }

    private IEnumerator ShowSkipButtonAfterDelay()
    {
        yield return new WaitForSeconds(skipDelay);

        if (skipButton != null && isCutscenePlaying)
        {
            skipButton.gameObject.SetActive(true);
        }
    }

    public void SkipCutscene()
    {
        if (!isCutscenePlaying || !allowSkip) return;

        Debug.Log("Cutscene skipped");

        // Stop the timeline
        if (timelineDirector != null && timelineDirector.state == PlayState.Playing)
        {
            timelineDirector.Stop();
        }

        // Manually call the finish method
        FinishCutscene();
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        // This method is called when the timeline finishes playing normally
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        isCutscenePlaying = false;

        // Disable the timeline parent object
        if (timelineParentObject != null)
        {
            timelineParentObject.SetActive(false);
        }

        // Disable dialogue canvas after cutscene
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(false);
            Debug.Log("Dialogue canvas disabled after cutscene");
        }

        // Disable title text after cutscene
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            Debug.Log("Title text disabled after cutscene");
        }

        // DISABLE voice title object when timeline ends
        if (voiceTitleObject != null)
        {
            voiceTitleObject.SetActive(false);
            Debug.Log("Voice title object disabled after cutscene");

            // Also disable the text component if assigned
            if (voiceTitleText != null)
            {
                voiceTitleText.gameObject.SetActive(false);
            }
        }


        // ENABLE GameUI Canvas when cutscene ends
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("GameUI Canvas re-enabled after cutscene");
        }

        // Re-enable audio handler after cutscene
        if (audioHandler != null)
        {
            audioHandler.SetActive(true);
        }

        // Hide skip button
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // Re-enable the start button
        if (startButton != null)
        {
            startButton.interactable = true;
        }

        Debug.Log("Cutscene finished - Voice title disabled, GameUI Canvas re-enabled");

        // Re-enable extra objects after cutscene
        if (objectsToDisableDuringCutscene != null)
        {
            foreach (var obj in objectsToDisableDuringCutscene)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
    }

    // NEW: Method to set voice title text
    public void SetVoiceTitleText(string text)
    {
        if (voiceTitleText != null)
        {
            voiceTitleText.text = text;
            Debug.Log($"Voice title text set to: '{text}'");
        }
        else if (voiceTitleObject != null)
        {
            // Try to find TMP_Text component if not assigned
            var tmpText = voiceTitleObject.GetComponentInChildren<TMP_Text>();
            if (tmpText != null)
            {
                tmpText.text = text;
                Debug.Log($"Voice title text found and set to: '{text}'");
            }
        }
    }

    // NEW: Method to manually control Voice Title object
    public void SetVoiceTitleActive(bool active)
    {
        if (voiceTitleObject != null)
        {
            voiceTitleObject.SetActive(active);
            Debug.Log($"Voice title object manually {(active ? "enabled" : "disabled")}");
        }
    }

    // NEW: Method to update voice title text directly
    public void UpdateVoiceTitleText(string newText)
    {
        SetVoiceTitleText(newText);
    }

    // NEW: Method to check if voice title is active
    public bool IsVoiceTitleActive()
    {
        return voiceTitleObject != null && voiceTitleObject.activeSelf;
    }

    // NEW: Method to get current voice title text
    public string GetCurrentVoiceTitleText()
    {
        if (voiceTitleText != null)
        {
            return voiceTitleText.text;
        }
        else if (voiceTitleObject != null)
        {
            var tmpText = voiceTitleObject.GetComponentInChildren<TMP_Text>();
            return tmpText != null ? tmpText.text : "";
        }
        return "";
    }

    // NEW: Method to manually control GameUI Canvas
    public void SetGameUICanvasActive(bool active)
    {
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(active);
            Debug.Log($"GameUI Canvas manually {(active ? "enabled" : "disabled")}");
        }
    }

    // NEW: Method to check GameUI Canvas state
    public bool IsGameUICanvasActive()
    {
        return gameUICanvas != null && gameUICanvas.activeSelf;
    }


    public void UpdateTitleText(string newText)
    {
        if (titleText != null && isCutscenePlaying)
        {
            titleText.text = newText;
            Debug.Log($"Title text updated to: '{newText}'");
        }
    }

    public void SetTitleTextActive(bool active)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(active && isCutscenePlaying);
            Debug.Log($"Title text {(active ? "shown" : "hidden")}");
        }
    }

    public void SetDialogueCanvasEnabled(bool enabled)
    {
        enableDialogueCanvas = enabled;

        if (dialogueCanvas != null)
        {
            if (enabled && isCutscenePlaying)
            {
                dialogueCanvas.SetActive(true);
            }
            else if (!enabled)
            {
                dialogueCanvas.SetActive(false);
            }
        }

        Debug.Log($"Dialogue canvas control {(enabled ? "ENABLED" : "DISABLED")}");
    }

    public void SetTitleText(TMP_Text newTitleText)
    {
        if (titleText != null && titleText.gameObject.activeSelf)
        {
            titleText.gameObject.SetActive(false);
        }

        titleText = newTitleText;

        if (titleText != null)
        {
            titleText.gameObject.SetActive(isCutscenePlaying);
            Debug.Log($"Title text assigned: {titleText.name}");
        }
    }

    public string GetCurrentTitleText()
    {
        return titleText != null ? titleText.text : "";
    }

    public bool IsCutscenePlaying()
    {
        return isCutscenePlaying;
    }

    public bool IsDialogueCanvasActive()
    {
        return dialogueCanvas != null && dialogueCanvas.activeSelf;
    }

    public bool IsTitleTextActive()
    {
        return titleText != null && titleText.gameObject.activeSelf;
    }

    [ContextMenu("Test Play Cutscene")]
    public void TestPlayCutscene()
    {
        PlayCutscene();
    }

    [ContextMenu("Test Play Cutscene with Custom Title")]
    public void TestPlayCutsceneWithCustomTitle()
    {
        PlayCutscene("CUSTOM TITLE - TEST");
    }

    [ContextMenu("Test Skip Cutscene")]
    public void TestSkipCutscene()
    {
        SkipCutscene();
    }

    [ContextMenu("Toggle Dialogue Canvas Control")]
    public void ToggleDialogueCanvasControl()
    {
        SetDialogueCanvasEnabled(!enableDialogueCanvas);
    }

    [ContextMenu("Toggle GameUI Canvas")]
    public void ToggleGameUICanvas()
    {
        SetGameUICanvasActive(!IsGameUICanvasActive());
    }

    [ContextMenu("Toggle Voice Title")]
    public void ToggleVoiceTitle()
    {
        SetVoiceTitleActive(!IsVoiceTitleActive());
    }

    [ContextMenu("Update Title to 'TEST TITLE'")]
    public void TestUpdateTitle()
    {
        UpdateTitleText("TEST TITLE - UPDATED");
    }

    [ContextMenu("Update Voice Title to 'VOICE TEST'")]
    public void TestUpdateVoiceTitle()
    {
        UpdateVoiceTitleText("VOICE TITLE - TEST");
    }

    [ContextMenu("Toggle Title Visibility")]
    public void TestToggleTitle()
    {
        SetTitleTextActive(!IsTitleTextActive());
    }

    /// <summary>
    /// Fully resets the cutscene to its initial state so it can be replayed.
    /// Call this before PlayCutscene() when restarting the game.
    /// </summary>
    public void ResetCutsceneState()
    {
        StopAllCoroutines();

        // Stop and rewind the timeline
        if (timelineDirector != null)
        {
            timelineDirector.Stop();
            timelineDirector.time = 0;
        }

        isCutscenePlaying = false;

        // Disable timeline parent
        if (timelineParentObject != null)
            timelineParentObject.SetActive(false);

        // Hide skip button
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // Hide voice title
        if (voiceTitleObject != null)
        {
            voiceTitleObject.SetActive(false);
            if (voiceTitleText != null)
                voiceTitleText.gameObject.SetActive(false);
        }

        // Hide dialogue canvas and title
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);
        if (titleText != null)
            titleText.gameObject.SetActive(false);

        // Re-enable GameUI canvas
        if (gameUICanvas != null)
            gameUICanvas.SetActive(true);

        // Re-enable audio handler
        if (audioHandler != null)
            audioHandler.SetActive(true);

        // Re-enable objects that get disabled during cutscene
        if (objectsToDisableDuringCutscene != null)
        {
            foreach (var obj in objectsToDisableDuringCutscene)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Re-enable start button
        if (startButton != null)
            startButton.interactable = true;

        Debug.Log("K3_IntroCutscene: Cutscene state fully reset.");
    }

    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== CUTSCENE MANAGER DEBUG ===");
        Debug.Log($"Is Cutscene Playing: {isCutscenePlaying}");
        Debug.Log($"GameUI Canvas Active: {IsGameUICanvasActive()}");
        Debug.Log($"Voice Title Active: {IsVoiceTitleActive()}");
        Debug.Log($"Voice Title Text: '{GetCurrentVoiceTitleText()}'");
        Debug.Log($"Dialogue Canvas Control Enabled: {enableDialogueCanvas}");
        Debug.Log($"Dialogue Canvas Active: {IsDialogueCanvasActive()}");
        Debug.Log($"Title Text Active: {IsTitleTextActive()}");
        Debug.Log($"Current Title: {(titleText != null ? $"'{titleText.text}'" : "N/A")}");
        Debug.Log($"Timeline Director State: {(timelineDirector != null ? timelineDirector.state.ToString() : "NULL")}");
        Debug.Log($"Timeline Parent Active: {(timelineParentObject != null ? timelineParentObject.activeSelf : "NULL")}");
        Debug.Log($"=== END DEBUG ===");
    }
}