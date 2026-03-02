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
    [SerializeField] private PlayableDirector firstCutscene;  // For HasOCRKey = false
    [SerializeField] private PlayableDirector secondCutscene; // For HasOCRKey = true
    
    [Header("Quest Settings")]
    [SerializeField] private bool isKeyGiverNPC = true;
    
    [Header("Game Start Settings")]
    [SerializeField] private bool startsGameTimer = true;
    [SerializeField] private bool isFirstWardenInteraction = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private GameTimer gameTimer;
    private bool cutscenePlayed = false; // Track if cutscene has been played
    private bool isCutscenePlaying = false;
    private bool isPlayerInRange = false;
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
        
        // Register for timeline events
        RegisterTimelineEvents();
    }
    
    private void RegisterTimelineEvents()
    {
        if (firstCutscene != null)
        {
            firstCutscene.stopped += OnTimelineStopped;
        }
        
        if (secondCutscene != null)
        {
            secondCutscene.stopped += OnTimelineStopped;
        }
    }
    
    private void OnDestroy()
    {
        // Unregister events
        if (firstCutscene != null)
        {
            firstCutscene.stopped -= OnTimelineStopped;
        }
        
        if (secondCutscene != null)
        {
            secondCutscene.stopped -= OnTimelineStopped;
        }
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        DebugLog($"Timeline stopped: {director.name}");
        
        if (director == firstCutscene)
        {
            OnFirstCutsceneEnded();
        }
        else if (director == secondCutscene)
        {
            OnSecondCutsceneEnded();
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
        cutscenePlayed = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCutscenePlaying)
        {
            isPlayerInRange = true;
            if (!cutscenePlayed)
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
        if (cutscenePlayed) return;
        
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
        if (cutscenePlayed) return;
        
        // Check if player has OCR key
        bool hasOCRKey = CheckPlayerHasKey();
        DebugLog($"Player has OCR key: {hasOCRKey}");
        
        // Mark that cutscene has been played
        cutscenePlayed = true;
        isCutscenePlaying = true;
        HideTalkButton();
        
        if (hasOCRKey && secondCutscene != null)
        {
            // Player has OCR key - play second cutscene
            DebugLog("Player has OCR key, playing second cutscene");
            
            // Activate game UI for second cutscene (this will disable profile panel)
            ActivateAllGameUI();
            
            // Stop any currently playing timeline
            StopAllTimelines();
            
            // Play second cutscene
            currentPlayingTimeline = secondCutscene;
            secondCutscene.Play();
        }
        else if (!hasOCRKey && firstCutscene != null)
        {
            // Player does NOT have OCR key - play first cutscene
            DebugLog("Player does NOT have OCR key, playing first cutscene");
            
            // Deactivate game UI during first cutscene (this will enable profile panel)
            DeactivateAllGameUI();
            
            // Stop any currently playing timeline
            StopAllTimelines();
            
            // Play first cutscene
            currentPlayingTimeline = firstCutscene;
            firstCutscene.Play();
        }
        else
        {
            isCutscenePlaying = false;
            cutscenePlayed = false; // Reset so player can try again
            if (isPlayerInRange) ShowTalkButton();
        }
    }
    
    private void StopAllTimelines()
    {
        if (firstCutscene != null && firstCutscene.state == PlayState.Playing)
        {
            firstCutscene.Stop();
            DebugLog("Stopped firstCutscene");
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
        if (firstCutscene != null)
            firstCutscene.time = 0;
            
        if (secondCutscene != null)
            secondCutscene.time = 0;
        
        isCutscenePlaying = false;
        cutscenePlayed = false;
        
        // Show talk button if player in range
        if (isPlayerInRange)
            ShowTalkButton();
            
        DebugLog("Timeline reset complete");
    }
    
    // Called when the first cutscene finishes playing
    private void OnFirstCutsceneEnded()
    {
        DebugLog("First cutscene ended");
        
        // For first cutscene, we don't start the game - just reset state
        if (isPlayerInRange && !cutscenePlayed)
        {
            // If cutscenePlayed was somehow reset, show talk button
            ShowTalkButton();
        }
        
        isCutscenePlaying = false;
        currentPlayingTimeline = null;
        
        // Reactivate profile panel since game UI is off
        DeactivateAllGameUI();
    }
    
    // Called when the second cutscene finishes (for players who already have key)
    private void OnSecondCutsceneEnded()
    {
        DebugLog("Second cutscene ended - starting the game");
        
        // Start the game
        StartGameTimerNow();
        ActivateAllGameUI();
        
        isCutscenePlaying = false;
        currentPlayingTimeline = null;
        
        // FIXED: Notify the GameEndManager that we're starting the game
        // This ensures camera system is ready for when game ends
        if (Kingdom4GameEndManager.Instance != null)
        {
            Kingdom4GameEndManager.Instance.OnAcceptTimelineEndedAndGameStarting();
        }
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
    
    // Public method to reset interaction (can be called from game manager)
    public void ResetInteraction()
    {
        DebugLog("Resetting interaction");
        cutscenePlayed = false;
        isCutscenePlaying = false;
        
        if (isPlayerInRange)
            ShowTalkButton();
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
        Debug.Log($"cutscenePlayed: {cutscenePlayed}");
        Debug.Log($"isCutscenePlaying: {isCutscenePlaying}");
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