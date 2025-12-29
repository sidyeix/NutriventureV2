using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class K2_IntroCutsceneManager : MonoBehaviour
{
    [Header("Timeline References")]
    [SerializeField] private GameObject timelineParentObject;
    [SerializeField] private PlayableDirector timelineDirector;
    
    [Header("UI References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button skipButton;
    
    [Header("Camera References")]
    [SerializeField] private GameObject playerFollowCamera;
    
    [Header("UI Canvas")]
    [SerializeField] private GameObject gameUICanvas; // This will be disabled during cutscene
    
    [Header("Dialogue Canvas - Subtitle System")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private bool enableDialogueCanvas = true;
    [SerializeField] private TMP_Text titleText;
    
    [Header("Audio Handler")]
    [SerializeField] private GameObject audioHandler;
    
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
        
        // Ensure player follow camera is disabled initially
        if (playerFollowCamera != null)
        {
            playerFollowCamera.SetActive(false);
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
            
            // Enable the timeline parent object
            timelineParentObject.SetActive(true);
            
            // DISABLE GameUI Canvas when cutscene starts
            if (gameUICanvas != null)
            {
                gameUICanvas.SetActive(false);
                Debug.Log("GameUI Canvas disabled for cutscene");
            }
            
            // Enable dialogue canvas if configured
            if (enableDialogueCanvas && dialogueCanvas != null)
            {
                dialogueCanvas.SetActive(true);
                Debug.Log("Dialogue canvas enabled for cutscene");
            }
            
            // Enable and set title text if assigned
            if (titleText != null)
            {
                titleText.gameObject.SetActive(true);
                
                // Set custom title text if provided
                if (!string.IsNullOrEmpty(customTitleText))
                {
                    titleText.text = customTitleText;
                    Debug.Log($"Title text set to: '{customTitleText}'");
                }
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
            
            Debug.Log("Cutscene started - GameUI Canvas disabled");
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
        
        // ENABLE GameUI Canvas when cutscene ends
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
            Debug.Log("GameUI Canvas re-enabled after cutscene");
        }
        
        // Enable the player follow camera
        if (playerFollowCamera != null)
        {
            playerFollowCamera.SetActive(true);
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
        
        Debug.Log("Cutscene finished - GameUI Canvas re-enabled");
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
    
    [ContextMenu("Update Title to 'TEST TITLE'")]
    public void TestUpdateTitle()
    {
        UpdateTitleText("TEST TITLE - UPDATED");
    }
    
    [ContextMenu("Toggle Title Visibility")]
    public void TestToggleTitle()
    {
        SetTitleTextActive(!IsTitleTextActive());
    }
    
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== CUTSCENE MANAGER DEBUG ===");
        Debug.Log($"Is Cutscene Playing: {isCutscenePlaying}");
        Debug.Log($"GameUI Canvas Active: {IsGameUICanvasActive()}");
        Debug.Log($"Dialogue Canvas Control Enabled: {enableDialogueCanvas}");
        Debug.Log($"Dialogue Canvas Active: {IsDialogueCanvasActive()}");
        Debug.Log($"Title Text Active: {IsTitleTextActive()}");
        Debug.Log($"Current Title: {(titleText != null ? $"'{titleText.text}'" : "N/A")}");
        Debug.Log($"Timeline Director State: {(timelineDirector != null ? timelineDirector.state.ToString() : "NULL")}");
        Debug.Log($"Timeline Parent Active: {(timelineParentObject != null ? timelineParentObject.activeSelf : "NULL")}");
        Debug.Log($"Player Camera Active: {(playerFollowCamera != null ? playerFollowCamera.activeSelf : "NULL")}");
        Debug.Log($"=== END DEBUG ===");
    }
}