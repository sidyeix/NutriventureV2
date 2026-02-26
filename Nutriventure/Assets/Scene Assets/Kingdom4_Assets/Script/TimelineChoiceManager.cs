using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class TimelineChoiceManager : MonoBehaviour
{
    public static TimelineChoiceManager Instance;
    
    [Header("Timeline References")]
    [SerializeField] private PlayableDirector mainTimeline;      // For HasOCRKey = false (has pause)
    [SerializeField] private PlayableDirector acceptTimeline;    // Plays when ACCEPT is clicked
    
    [Header("Button References")]
    [SerializeField] private GameObject choiceButtonsPanel;      // Panel containing Accept/Decline buttons
    
    [Header("Warden Interaction Reference")]
    [SerializeField] private WardenInteraction wardenInteraction;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Track state
    private bool hasMadeChoice = false;
    private bool isWaitingForTimeline = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Hide choice buttons initially
        if (choiceButtonsPanel != null)
            choiceButtonsPanel.SetActive(false);
            
        // Find warden interaction if not assigned
        if (wardenInteraction == null)
            wardenInteraction = FindObjectOfType<WardenInteraction>();
    }
    
    // Called by WardenInteraction to set which timelines to use
    public void SetTimelines(PlayableDirector main, PlayableDirector accept)
    {
        mainTimeline = main;
        acceptTimeline = accept;
        
        DebugLog($"Timelines set - Main: {(main != null ? main.name : "null")}, Accept: {(accept != null ? accept.name : "null")}");
    }
    
    // Called by WardenInteraction to start the appropriate cutscene
    public void StartInitialCutscene()
    {
        if (GameDataManager.Instance == null)
        {
            DebugLogError("GameDataManager.Instance is null!");
            return;
        }
        
        // Check if player has OCR Scanner Key
        bool hasOCRKey = GameDataManager.Instance.HasOCRScannerKey();
        
        DebugLog($"OCR Scanner Key status: {hasOCRKey}");
        
        if (hasOCRKey)
        {
            // If player has OCR key, this should have been handled by WardenInteraction directly
            DebugLogWarning("StartInitialCutscene called when player has OCR key - this should not happen!");
        }
        else
        {
            // If player doesn't have OCR key, play the main timeline (which will pause)
            PlayMainTimeline();
        }
    }
    
    void PlayMainTimeline()
    {
        if (mainTimeline == null)
        {
            DebugLogError("Main timeline is not assigned!");
            return;
        }
        
        DebugLog("Playing MAIN timeline (will pause for choice)");
        
        // Ensure timeline is reset to beginning
        mainTimeline.time = 0;
        
        // Play the main timeline
        mainTimeline.Play();
        
        // Reset choice state
        hasMadeChoice = false;
        isWaitingForTimeline = true;
    }
    
    void PlayAcceptTimeline()
    {
        if (acceptTimeline == null)
        {
            DebugLogError("Accept timeline is not assigned!");
            return;
        }
        
        DebugLog("Playing ACCEPT timeline");
        
        // COMPLETELY STOP the main timeline - not just pause
        if (mainTimeline != null)
        {
            if (mainTimeline.state == PlayState.Playing)
            {
                mainTimeline.Stop(); // This completely ends the timeline
                DebugLog("Main timeline STOPPED completely");
            }
            
            // Also force it to not resume by clearing any pending signals
            mainTimeline.time = 0;
            mainTimeline.Evaluate(); // Force evaluate to clear any pending signals
        }
        
        // Also notify TimelinePauseManager that we're done with the main timeline
        if (TimelinePauseManager.Instance != null)
        {
            // We don't want the pause manager to think it's still paused
            // Since we're not modifying TimelinePauseManager, we'll just log this
            DebugLog("Main timeline stopped - pause manager will need to be reset for next interaction");
        }
        
        // Ensure accept timeline is reset to beginning
        acceptTimeline.time = 0;
        
        // Play the accept timeline
        acceptTimeline.Play();
        
        // Notify warden interaction that accept timeline is playing
        if (wardenInteraction != null)
        {
            // We'll need to know when this timeline ends
            StartCoroutine(WaitForAcceptTimelineEnd());
        }
    }
    
    IEnumerator WaitForAcceptTimelineEnd()
    {
        DebugLog("Waiting for accept timeline to end...");
        
        while (acceptTimeline != null && acceptTimeline.state == PlayState.Playing)
        {
            yield return null;
        }
            
        DebugLog("Accept timeline finished");
        
        // Notify warden interaction
        if (wardenInteraction != null)
            wardenInteraction.OnAcceptTimelineEnded();
            
        isWaitingForTimeline = false;
    }
    
    // Called by TimelinePauseManager when timeline is paused
    public void OnTimelinePaused()
    {
        DebugLog("Timeline paused - showing choice buttons");
        
        // Show choice buttons
        if (choiceButtonsPanel != null)
            choiceButtonsPanel.SetActive(true);
    }
    
    // Method for ACCEPT button
    public void OnAcceptButtonClicked()
    {
        if (hasMadeChoice)
        {
            DebugLog("Choice already made, ignoring button click");
            return;
        }
        
        DebugLog("ACCEPT button clicked - Playing accept timeline");
        
        // Hide choice buttons
        HideChoiceButtons();
        
        // Notify warden interaction
        if (wardenInteraction != null)
            wardenInteraction.OnQuestAccepted();
        
        // Play the accept timeline (this will STOP the main timeline)
        PlayAcceptTimeline();
        
        // Mark that choice has been made
        hasMadeChoice = true;
    }
    
    // Method for DECLINE button
    public void OnDeclineButtonClicked()
    {
        if (hasMadeChoice)
        {
            DebugLog("Choice already made, ignoring button click");
            return;
        }
        
        DebugLog("DECLINE button clicked - Continuing main timeline");
        
        // Hide choice buttons
        HideChoiceButtons();
        
        // Notify warden interaction
        if (wardenInteraction != null)
            wardenInteraction.OnQuestRejected();
        
        // Resume the paused main timeline (KEEP it alive)
        if (TimelinePauseManager.Instance != null)
        {
            TimelinePauseManager.Instance.ResumeTimeline();
            
            // Mark that choice has been made
            hasMadeChoice = true;
            
            DebugLog("Main timeline resumed");
        }
        else
        {
            DebugLogError("TimelinePauseManager.Instance is null! Cannot resume timeline.");
            
            // Fallback - try to resume directly
            if (mainTimeline != null)
            {
                mainTimeline.Resume();
                hasMadeChoice = true;
                DebugLog("Resumed main timeline directly");
            }
        }
    }
    
    // Public method to hide choice buttons (called from WardenInteraction)
    public void HideChoiceButtons()
    {
        if (choiceButtonsPanel != null)
        {
            choiceButtonsPanel.SetActive(false);
            DebugLog("Choice buttons hidden");
        }
    }
    
    // Optional: Method to reset for next interaction
    public void ResetChoiceSystem()
    {
        hasMadeChoice = false;
        isWaitingForTimeline = false;
        HideChoiceButtons();
        DebugLog("Choice system reset");
    }
    
    // Force reset if timeline gets stuck
    public void ForceReset()
    {
        DebugLog("FORCE RESETTING CHOICE MANAGER");
        
        hasMadeChoice = false;
        isWaitingForTimeline = false;
        HideChoiceButtons();
        
        // Stop all timelines completely
        if (mainTimeline != null)
        {
            if (mainTimeline.state == PlayState.Playing)
                mainTimeline.Stop();
            mainTimeline.time = 0;
        }
            
        if (acceptTimeline != null)
        {
            if (acceptTimeline.state == PlayState.Playing)
                acceptTimeline.Stop();
            acceptTimeline.time = 0;
        }
            
        DebugLog("Choice manager force reset complete");
    }
    
    [ContextMenu("Debug Timeline State")]
    public void DebugTimelineState()
    {
        Debug.Log("=== TIMELINE CHOICE MANAGER STATE ===");
        Debug.Log($"hasMadeChoice: {hasMadeChoice}");
        Debug.Log($"isWaitingForTimeline: {isWaitingForTimeline}");
        
        if (mainTimeline != null)
        {
            Debug.Log($"Main Timeline: {mainTimeline.name}");
            Debug.Log($"- State: {mainTimeline.state}");
            Debug.Log($"- Time: {mainTimeline.time}");
            Debug.Log($"- Enabled: {mainTimeline.enabled}");
            Debug.Log($"- GameObject Active: {mainTimeline.gameObject.activeSelf}");
        }
        
        if (acceptTimeline != null)
        {
            Debug.Log($"Accept Timeline: {acceptTimeline.name}");
            Debug.Log($"- State: {acceptTimeline.state}");
            Debug.Log($"- Time: {acceptTimeline.time}");
            Debug.Log($"- Enabled: {acceptTimeline.enabled}");
            Debug.Log($"- GameObject Active: {acceptTimeline.gameObject.activeSelf}");
        }
        
        // Check camera state
        if (Kingdom4GameEndManager.Instance != null)
        {
            Kingdom4GameEndManager.Instance.DebugCameraState();
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TimelineChoiceManager] {message}");
        }
    }
    
    private void DebugLogError(string message)
    {
        Debug.LogError($"[TimelineChoiceManager] {message}");
    }
    
    private void DebugLogWarning(string message)
    {
        Debug.LogWarning($"[TimelineChoiceManager] {message}");
    }
}