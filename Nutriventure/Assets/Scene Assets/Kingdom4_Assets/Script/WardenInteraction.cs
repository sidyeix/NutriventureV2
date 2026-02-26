using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.UI;

public class WardenInteraction : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button talkButton;
    [SerializeField] private GameObject talkButtonObject;
    
    [Header("Game UI References")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject heartsContainer;
    [SerializeField] private GameObject pointsPanel;
    [SerializeField] private GameObject timerPanel;
    [SerializeField] private GameObject profilePanel; // Reference to profile panel
    
    [Header("Timelines")]
    [SerializeField] private PlayableDirector firstArrivalTimeline;  // For HasOCRKey = false (with pause)
    [SerializeField] private PlayableDirector acceptTimeline;        // Plays when ACCEPT is clicked
    [SerializeField] private PlayableDirector secondCutscene;        // For HasOCRKey = true (plays directly)
    
    [Header("Quest Settings")]
    [SerializeField] private bool isKeyGiverNPC = true;
    
    [Header("Game Start Settings")]
    [SerializeField] private bool startsGameTimer = true;
    [SerializeField] private bool isFirstWardenInteraction = true;
    
    [Header("Choice Manager Reference")]
    [SerializeField] private TimelineChoiceManager choiceManager; // Reference to the choice manager
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private GameTimer gameTimer;
    private bool questAccepted = false;
    private bool isTimelinePlaying = false;
    private bool isPlayerInRange = false;
    private Coroutine timelineWaitCoroutine;
    private PlayableDirector currentPlayingTimeline;
    
    private void Start()
    {
        DebugLog($"[WardenInteraction] Start called on {gameObject.name}");
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
        HideTalkButton();
        DeactivateAllGameUI();
        
        if (talkButton != null)
        {
            talkButton.onClick.RemoveAllListeners();
            talkButton.onClick.AddListener(StartInteraction);
        }
        
        FindGameTimer();
        
        // Find choice manager if not assigned
        if (choiceManager == null)
        {
            choiceManager = FindObjectOfType<TimelineChoiceManager>();
            if (choiceManager == null)
            {
                Debug.LogError("TimelineChoiceManager not found in scene! Please assign it in the inspector.");
            }
        }
        
        // Register for timeline events
        RegisterTimelineEvents();
    }
    
    private void RegisterTimelineEvents()
    {
        if (firstArrivalTimeline != null)
        {
            firstArrivalTimeline.stopped += OnTimelineStopped;
            firstArrivalTimeline.paused += OnTimelinePaused;
        }
        
        if (acceptTimeline != null)
        {
            acceptTimeline.stopped += OnTimelineStopped;
            acceptTimeline.paused += OnTimelinePaused;
        }
        
        if (secondCutscene != null)
        {
            secondCutscene.stopped += OnTimelineStopped;
            secondCutscene.paused += OnTimelinePaused;
        }
    }
    
    private void OnDestroy()
    {
        // Unregister events
        if (firstArrivalTimeline != null)
        {
            firstArrivalTimeline.stopped -= OnTimelineStopped;
            firstArrivalTimeline.paused -= OnTimelinePaused;
        }
        
        if (acceptTimeline != null)
        {
            acceptTimeline.stopped -= OnTimelineStopped;
            acceptTimeline.paused -= OnTimelinePaused;
        }
        
        if (secondCutscene != null)
        {
            secondCutscene.stopped -= OnTimelineStopped;
            secondCutscene.paused -= OnTimelinePaused;
        }
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        DebugLog($"Timeline stopped: {director.name}");
        
        if (director == firstArrivalTimeline)
        {
            OnMainTimelineEnded();
        }
        else if (director == acceptTimeline)
        {
            OnAcceptTimelineEnded();
        }
        else if (director == secondCutscene)
        {
            OnSecondCutsceneEnded();
        }
    }
    
    private void OnTimelinePaused(PlayableDirector director)
    {
        DebugLog($"Timeline paused: {director.name}");
        
        if (director == firstArrivalTimeline)
        {
            // Notify choice manager that timeline is paused
            if (choiceManager != null)
            {
                choiceManager.OnTimelinePaused();
            }
        }
    }
    
    private void DeactivateAllGameUI()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (pointsPanel != null) pointsPanel.SetActive(false);
        if (timerPanel != null) timerPanel.SetActive(false);
        
        // Make sure profile panel is visible when game UI is off
        if (profilePanel != null)
        {
            profilePanel.SetActive(true);
            DebugLog("Profile panel activated (game UI off)");
        }
    }
    
    private void ActivateAllGameUI()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (heartsContainer != null) heartsContainer.SetActive(true);
        if (pointsPanel != null) pointsPanel.SetActive(true);
        if (timerPanel != null) timerPanel.SetActive(true);
        
        // IMPORTANT: Disable profile panel when game UI is on
        if (profilePanel != null)
        {
            profilePanel.SetActive(false);
            DebugLog("Profile panel disabled (game UI on)");
        }
    }
    
    private void FindGameTimer()
    {
        gameTimer = GameTimer.Instance;
        if (gameTimer == null) gameTimer = FindObjectOfType<GameTimer>();
        
        if (isFirstWardenInteraction && startsGameTimer && gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }
    }
    
    private void OnEnable()
    {
        questAccepted = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTimelinePlaying)
        {
            isPlayerInRange = true;
            if (!questAccepted)
            {
                ShowTalkButton();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            HideTalkButton();
        }
    }
    
    private bool CheckPlayerHasKey()
    {
        // Use GameDataManager to check OCR Scanner Key
        if (GameDataManager.Instance != null)
        {
            return GameDataManager.Instance.HasOCRScannerKey();
        }
        
        // Fallback to AllerthriaGameManager
        if (AllerthriaGameManager.Instance != null)
        {
            return AllerthriaGameManager.Instance.hasKey;
        }
        
        // Check PlayerPrefs as last fallback
        string keyId = "ocr_scanner_key";
        return PlayerPrefs.HasKey($"KeyCollected_{keyId}");
    }
    
    private void ShowTalkButton()
    {
        if (questAccepted) return;
        
        DebugLog("Showing talk button");
        
        if (talkButtonObject != null)
            talkButtonObject.SetActive(true);
        else if (talkButton != null)
            talkButton.gameObject.SetActive(true);
    }
    
    private void HideTalkButton()
    {
        DebugLog("Hiding talk button");
        
        if (talkButtonObject != null)
            talkButtonObject.SetActive(false);
        else if (talkButton != null)
            talkButton.gameObject.SetActive(false);
    }
    
    private void StartInteraction()
    {
        if (questAccepted) return;
        
        // Check if player has OCR key
        bool hasOCRKey = CheckPlayerHasKey();
        DebugLog($"Player has OCR key: {hasOCRKey}");
        
        if (hasOCRKey && secondCutscene != null)
        {
            // Player has OCR key - play second cutscene directly (no pause/buttons)
            DebugLog("Player has OCR key, playing second cutscene directly");
            isTimelinePlaying = true;
            HideTalkButton();
            
            // Activate game UI for second cutscene (this will disable profile panel)
            ActivateAllGameUI();
            
            // Stop any currently playing timeline
            StopAllTimelines();
            
            // Play second cutscene
            currentPlayingTimeline = secondCutscene;
            secondCutscene.Play();
            
            return;
        }
        
        // Player does NOT have OCR key - play intro timeline with pause/buttons
        if (firstArrivalTimeline != null)
        {
            DebugLog("Player does NOT have OCR key, playing intro timeline (will pause for choice)");
            isTimelinePlaying = true;
            HideTalkButton();
            
            // Deactivate game UI during intro (this will enable profile panel)
            DeactivateAllGameUI();
            
            // Stop any currently playing timeline
            StopAllTimelines();
            
            // Tell choice manager which timelines to use for this interaction
            if (choiceManager != null)
            {
                choiceManager.SetTimelines(firstArrivalTimeline, acceptTimeline);
                // Start the initial cutscene through choice manager
                currentPlayingTimeline = firstArrivalTimeline;
                choiceManager.StartInitialCutscene();
            }
            else
            {
                // Fallback if no choice manager
                currentPlayingTimeline = firstArrivalTimeline;
                firstArrivalTimeline.Play();
            }
        }
    }
    
    private void StopAllTimelines()
    {
        if (firstArrivalTimeline != null && firstArrivalTimeline.state == PlayState.Playing)
        {
            firstArrivalTimeline.Stop();
            DebugLog("Stopped firstArrivalTimeline");
        }
        
        if (acceptTimeline != null && acceptTimeline.state == PlayState.Playing)
        {
            acceptTimeline.Stop();
            DebugLog("Stopped acceptTimeline");
        }
        
        if (secondCutscene != null && secondCutscene.state == PlayState.Playing)
        {
            secondCutscene.Stop();
            DebugLog("Stopped secondCutscene");
        }
        
        currentPlayingTimeline = null;
    }
    
    // FORCE RESET METHOD - Call this if timeline gets stuck
    public void ForceResetTimeline()
    {
        DebugLog("FORCE RESETTING TIMELINE");
        
        StopAllTimelines();
        
        // Reset timeline time
        if (firstArrivalTimeline != null)
            firstArrivalTimeline.time = 0;
            
        if (acceptTimeline != null)
            acceptTimeline.time = 0;
            
        if (secondCutscene != null)
            secondCutscene.time = 0;
        
        isTimelinePlaying = false;
        questAccepted = false;
        
        // Hide choice buttons
        if (choiceManager != null)
            choiceManager.HideChoiceButtons();
        
        // Show talk button if player in range
        if (isPlayerInRange)
            ShowTalkButton();
            
        DebugLog("Timeline reset complete");
    }
    
    public void OnQuestAccepted()
    {
        if (questAccepted) return;
        
        questAccepted = true;
        DebugLog("QUEST ACCEPTED via TimelineChoiceManager!");
        
        // Hide talk button
        HideTalkButton();
        
        // Note: The timeline switching is handled by TimelineChoiceManager
        // We just need to prepare the game for when the accept timeline ends
    }
    
    public void OnQuestRejected()
    {
        DebugLog("QUEST REJECTED via TimelineChoiceManager");
        
        // Note: The timeline resuming is handled by TimelineChoiceManager
        // We just need to prepare for the main timeline to continue
    }
    
    // Called when the accept timeline finishes playing
    public void OnAcceptTimelineEnded()
    {
        DebugLog("Accept timeline ended - starting the game");
        
        // Start the game - this will activate game UI and disable profile panel
        StartGameTimerNow();
        ActivateAllGameUI();
        
        // Reset timeline playing flag
        isTimelinePlaying = false;
        currentPlayingTimeline = null;
        
        // FIXED: Notify the GameEndManager that we're starting the game
        // This ensures camera system is ready for when game ends
        if (Kingdom4GameEndManager.Instance != null)
        {
            // Tell GameEndManager that the game is starting from accept timeline
            Kingdom4GameEndManager.Instance.OnAcceptTimelineEndedAndGameStarting();
        }
    }
    
    // Called when the main timeline finishes playing (after rejection or natural end)
    public void OnMainTimelineEnded()
    {
        DebugLog("Main timeline ended");
        
        if (!questAccepted && isPlayerInRange)
        {
            // If quest wasn't accepted and player is still in range, show talk button again
            ShowTalkButton();
        }
        
        isTimelinePlaying = false;
        currentPlayingTimeline = null;
    }
    
    // Called when the second cutscene finishes (for players who already have key)
    public void OnSecondCutsceneEnded()
    {
        DebugLog("Second cutscene ended - starting the game");
        
        // Start the game
        StartGameTimerNow();
        ActivateAllGameUI();
        
        isTimelinePlaying = false;
        currentPlayingTimeline = null;
    }
    
    private void StartGameTimerNow()
    {
        if (startsGameTimer && isFirstWardenInteraction && gameTimer != null)
        {
            if (gameTimer.CanStartTimer())
            {
                gameTimer.StartTimerFromInteraction();
                DebugLog("GAME TIMER STARTED!");
            }
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WardenInteraction] {message}");
        }
    }
    
    public bool DoesStartTimer()
    {
        return startsGameTimer;
    }
    
    // Public method to manually toggle profile panel if needed elsewhere
    public void SetProfilePanelActive(bool active)
    {
        if (profilePanel != null)
        {
            profilePanel.SetActive(active);
            DebugLog($"Profile panel manually set to: {active}");
        }
    }
    
    // Debug methods
    [ContextMenu("Test Show Talk Button")]
    public void TestShowButton()
    {
        ShowTalkButton();
    }
    
    [ContextMenu("Test Hide Talk Button")]
    public void TestHideButton()
    {
        HideTalkButton();
    }
    
    [ContextMenu("Test Quest Acceptance")]
    public void TestQuestAcceptance()
    {
        OnQuestAccepted();
    }
    
    [ContextMenu("Test Quest Rejection")]
    public void TestQuestRejection()
    {
        OnQuestRejected();
    }
    
    [ContextMenu("Test Activate Game UI")]
    public void TestActivateGameUI()
    {
        ActivateAllGameUI();
    }
    
    [ContextMenu("Test Deactivate Game UI")]
    public void TestDeactivateGameUI()
    {
        DeactivateAllGameUI();
    }
    
    [ContextMenu("Check Player Has OCR Key")]
    public void TestCheckKey()
    {
        bool hasKey = CheckPlayerHasKey();
        DebugLog($"Player has OCR key: {hasKey}");
        
        if (GameDataManager.Instance != null)
        {
            DebugLog($"GameDataManager.Instance.HasOCRScannerKey() = {GameDataManager.Instance.HasOCRScannerKey()}");
        }
        else
        {
            DebugLog("GameDataManager.Instance is null!");
        }
    }
    
    [ContextMenu("Force Reset Timeline")]
    public void TestForceReset()
    {
        ForceResetTimeline();
    }
    
    [ContextMenu("Show Current State")]
    public void ShowCurrentState()
    {
        Debug.Log("=== CURRENT STATE ===");
        Debug.Log($"isPlayerInRange: {isPlayerInRange}");
        Debug.Log($"questAccepted: {questAccepted}");
        Debug.Log($"isTimelinePlaying: {isTimelinePlaying}");
        Debug.Log($"currentPlayingTimeline: {(currentPlayingTimeline != null ? currentPlayingTimeline.name : "None")}");
        
        if (currentPlayingTimeline != null)
        {
            Debug.Log($"Timeline state: {currentPlayingTimeline.state}");
            Debug.Log($"Timeline time: {currentPlayingTimeline.time}");
        }
        
        bool buttonActive = false;
        if (talkButtonObject != null) buttonActive = talkButtonObject.activeSelf;
        else if (talkButton != null) buttonActive = talkButton.gameObject.activeSelf;
        
        Debug.Log($"Talk Button Active: {buttonActive}");
        Debug.Log($"Has OCR Key: {CheckPlayerHasKey()}");
        Debug.Log($"Profile Panel Active: {(profilePanel != null ? profilePanel.activeSelf.ToString() : "Not Assigned")}");
        Debug.Log("====================");
    }
}