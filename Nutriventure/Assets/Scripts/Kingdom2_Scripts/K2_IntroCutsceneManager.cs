using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // Add this namespace for TextMeshPro
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
    [SerializeField] private GameObject timelineCamera;
    
    [Header("UI Canvas")]
    [SerializeField] private GameObject gameUICanvas;
    
    [Header("Dialogue Canvas - Subtitle System")]
    [SerializeField] private GameObject dialogueCanvas; // Add this field for subtitle canvas
    [SerializeField] private bool enableDialogueCanvas = true; // Toggle to enable/disable dialogue canvas
    [SerializeField] private TMP_Text titleText; // NEW: TextMeshPro title to activate
    
    [Header("Audio Handler")]
    [SerializeField] private GameObject audioHandler;
    
    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipDelay = 1.0f;
    
    [Header("Input Actions")]
    [SerializeField] private InputAction skipAction;
    
    private bool isCutscenePlaying = false;
    private bool dialogueCanvasWasActive = false; // Track previous state
    private bool titleTextWasActive = false; // Track title's previous state
    
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
        
        // Ensure game UI canvas is disabled initially
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(false);
        }
        
        // Initialize dialogue canvas
        if (dialogueCanvas != null)
        {
            // Store whether it was active before initialization
            dialogueCanvasWasActive = dialogueCanvas.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            dialogueCanvas.SetActive(false);
        }
        
        // Initialize title text
        if (titleText != null)
        {
            // Store whether it was active before initialization
            titleTextWasActive = titleText.gameObject.activeSelf;
            // Disable it initially (will be enabled when cutscene plays)
            titleText.gameObject.SetActive(false);
            Debug.Log($"Title text initialized: {titleText.name}, was active: {titleTextWasActive}");
        }
        else
        {
            Debug.Log("No title text assigned - skipping title display");
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
        
        Debug.Log("Cutscene Manager initialized - Dialogue Canvas control: " + (enableDialogueCanvas ? "ENABLED" : "DISABLED"));
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
        PlayCutsceneWithTitle(null); // Default call without custom title
    }
    
    // NEW: Overload method to play cutscene with custom title text
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
                else
                {
                    Debug.Log($"Title text activated: {titleText.name}");
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
            
            Debug.Log("Cutscene started - Audio handler disabled, Dialogue canvas: " + 
                     (enableDialogueCanvas && dialogueCanvas != null && dialogueCanvas.activeSelf ? "ON" : "OFF") +
                     ", Title text: " + (titleText != null && titleText.gameObject.activeSelf ? "ON" : "OFF"));
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
        
        // Enable the player follow camera
        if (playerFollowCamera != null)
        {
            playerFollowCamera.SetActive(true);
        }
        
        // Enable the game UI canvas
        if (gameUICanvas != null)
        {
            gameUICanvas.SetActive(true);
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
        
        Debug.Log("Cutscene finished/skipped - UI enabled, audio handler re-enabled, dialogue canvas and title disabled");
    }
    
    // NEW: Method to update title text during cutscene
    public void UpdateTitleText(string newText)
    {
        if (titleText != null && isCutscenePlaying)
        {
            titleText.text = newText;
            Debug.Log($"Title text updated to: '{newText}'");
        }
        else if (titleText == null)
        {
            Debug.LogWarning("Cannot update title text - no title text assigned!");
        }
        else if (!isCutscenePlaying)
        {
            Debug.LogWarning("Cannot update title text - cutscene is not playing!");
        }
    }
    
    // NEW: Method to show/hide title text during cutscene
    public void SetTitleTextActive(bool active)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(active && isCutscenePlaying);
            Debug.Log($"Title text {(active ? "shown" : "hidden")}");
        }
    }
    
    // Public method to manually enable/disable dialogue canvas
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
    
    // NEW: Method to assign title text at runtime
    public void SetTitleText(TMP_Text newTitleText)
    {
        // Disable old title if exists
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
    
    // NEW: Method to get current title text
    public string GetCurrentTitleText()
    {
        return titleText != null ? titleText.text : "";
    }
    
    // Method to check if cutscene is playing
    public bool IsCutscenePlaying()
    {
        return isCutscenePlaying;
    }
    
    // Method to get dialogue canvas state
    public bool IsDialogueCanvasActive()
    {
        return dialogueCanvas != null && dialogueCanvas.activeSelf;
    }
    
    // NEW: Method to get title text state
    public bool IsTitleTextActive()
    {
        return titleText != null && titleText.gameObject.activeSelf;
    }
    
    // Context menu for testing
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
        Debug.Log($"Dialogue Canvas Control Enabled: {enableDialogueCanvas}");
        Debug.Log($"Dialogue Canvas Active: {IsDialogueCanvasActive()}");
        Debug.Log($"Title Text Assigned: {titleText != null}");
        Debug.Log($"Title Text Active: {IsTitleTextActive()}");
        Debug.Log($"Current Title: {(titleText != null ? $"'{titleText.text}'" : "N/A")}");
        Debug.Log($"Timeline Director State: {(timelineDirector != null ? timelineDirector.state.ToString() : "NULL")}");
        Debug.Log($"Timeline Parent Active: {(timelineParentObject != null ? timelineParentObject.activeSelf : "NULL")}");
        Debug.Log($"Game UI Canvas Active: {(gameUICanvas != null ? gameUICanvas.activeSelf : "NULL")}");
        Debug.Log($"Player Camera Active: {(playerFollowCamera != null ? playerFollowCamera.activeSelf : "NULL")}");
        Debug.Log($"=== END DEBUG ===");
    }
}