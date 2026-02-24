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
    [SerializeField] private GameObject profilePanel; // Add this - reference to profile panel
    
    [Header("Timelines")]
    [SerializeField] private PlayableDirector firstArrivalTimeline;
    [SerializeField] private PlayableDirector keyReturnTimeline;
    
    [Header("Quest Settings")]
    [SerializeField] private bool isKeyGiverNPC = true;
    
    [Header("Game Start Settings")]
    [SerializeField] private bool startsGameTimer = true;
    [SerializeField] private bool isFirstWardenInteraction = true;
    
    private GameTimer gameTimer;
    private bool questAccepted = false;
    private bool isTimelinePlaying = false;
    private bool isPlayerInRange = false;
    private Coroutine timelineWaitCoroutine;
    
    private void Start()
    {
        Debug.Log($"[WardenInteraction] Start called on {gameObject.name}");
        
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
        CheckIfKeyAlreadyCollected();
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
            Debug.Log("Profile panel activated (game UI off)");
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
            Debug.Log("Profile panel disabled (game UI on)");
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
    
    private void CheckIfKeyAlreadyCollected()
    {
        bool hasKey = CheckPlayerHasKey();
        if (hasKey && isKeyGiverNPC)
        {
            Debug.Log("Player already has key. Will play return cutscene when talking.");
        }
    }
    
    private bool CheckPlayerHasKey()
    {
        // Check AllerthriaGameManager first
        if (AllerthriaGameManager.Instance != null)
        {
            return AllerthriaGameManager.Instance.hasKey;
        }
        
        // Check PlayerPrefs as fallback
        string keyId = "castle_key";
        return PlayerPrefs.HasKey($"KeyCollected_{keyId}");
    }
    
    private void ShowTalkButton()
    {
        if (questAccepted) return;
        
        Debug.Log("Showing talk button");
        
        if (talkButtonObject != null)
            talkButtonObject.SetActive(true);
        else if (talkButton != null)
            talkButton.gameObject.SetActive(true);
    }
    
    private void HideTalkButton()
    {
        Debug.Log("Hiding talk button");
        
        if (talkButtonObject != null)
            talkButtonObject.SetActive(false);
        else if (talkButton != null)
            talkButton.gameObject.SetActive(false);
    }
    
    private void StartInteraction()
    {
        if (questAccepted) return;
        
        // Check if player has key - play return cutscene
        if (CheckPlayerHasKey() && keyReturnTimeline != null)
        {
            Debug.Log("Player has key, playing return cutscene");
            isTimelinePlaying = true;
            HideTalkButton();
            
            // IMPORTANT: Activate game UI for second cutscene (this will disable profile panel)
            ActivateAllGameUI();
            
            keyReturnTimeline.Play();
            
            if (timelineWaitCoroutine != null)
                StopCoroutine(timelineWaitCoroutine);
            timelineWaitCoroutine = StartCoroutine(WaitForTimelineEnd(keyReturnTimeline, true));
            return;
        }
        
        // Play intro timeline (first meeting)
        if (firstArrivalTimeline != null)
        {
            Debug.Log("Playing intro timeline");
            isTimelinePlaying = true;
            HideTalkButton();
            
            // Deactivate game UI during intro (this will enable profile panel)
            DeactivateAllGameUI();
            
            firstArrivalTimeline.Play();
            
            if (timelineWaitCoroutine != null)
                StopCoroutine(timelineWaitCoroutine);
            timelineWaitCoroutine = StartCoroutine(WaitForTimelineEnd(firstArrivalTimeline, false));
        }
    }
    
    public void OnQuestAccepted()
    {
        if (questAccepted) return;
        
        questAccepted = true;
        Debug.Log("QUEST ACCEPTED!");
        
        // Stop current timeline if still playing
        if (firstArrivalTimeline != null && firstArrivalTimeline.state == PlayState.Playing)
        {
            firstArrivalTimeline.Stop();
        }
        
        // Hide choice buttons
        if (TimelineChoiceManager.Instance != null)
        {
            TimelineChoiceManager.Instance.HideChoiceButtons();
        }
        
        // Start the game - this will activate game UI and disable profile panel
        StartGameTimerNow();
        ActivateAllGameUI();
        HideTalkButton();
        
        // Reset timeline playing flag
        isTimelinePlaying = false;
    }
    
    public void OnQuestRejected()
    {
        Debug.Log("QUEST REJECTED");
        
        // IMPORTANT: Resume the timeline instead of stopping it
        if (firstArrivalTimeline != null)
        {
            firstArrivalTimeline.Play(); // Resume playback
        }
        
        // Hide choice buttons
        if (TimelineChoiceManager.Instance != null)
        {
            TimelineChoiceManager.Instance.HideChoiceButtons();
        }
        
        // Don't reset isTimelinePlaying - let the timeline continue
    }
    
    private void StartGameTimerNow()
    {
        if (startsGameTimer && isFirstWardenInteraction && gameTimer != null)
        {
            if (gameTimer.CanStartTimer())
            {
                gameTimer.StartTimerFromInteraction();
                Debug.Log("GAME TIMER STARTED!");
            }
        }
    }
    
    private IEnumerator WaitForTimelineEnd(PlayableDirector director, bool isReturnCutscene)
    {
        while (director != null && director.state == PlayState.Playing)
            yield return null;
        
        isTimelinePlaying = false;
        Debug.Log($"Timeline ended: {(isReturnCutscene ? "Return" : "Intro")} cutscene");
        
        // For intro cutscene without quest acceptance, show talk button again
        if (!isReturnCutscene && !questAccepted && isPlayerInRange)
        {
            ShowTalkButton();
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
            Debug.Log($"Profile panel manually set to: {active}");
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
    
    [ContextMenu("Check Player Has Key")]
    public void TestCheckKey()
    {
        bool hasKey = CheckPlayerHasKey();
        Debug.Log($"Player has key: {hasKey}");
        
        if (AllerthriaGameManager.Instance != null)
        {
            Debug.Log($"AllerthriaGameManager.Instance.hasKey = {AllerthriaGameManager.Instance.hasKey}");
        }
        else
        {
            Debug.Log("AllerthriaGameManager.Instance is null!");
        }
    }
    
    [ContextMenu("Show Current State")]
    public void ShowCurrentState()
    {
        Debug.Log("=== CURRENT STATE ===");
        Debug.Log($"isPlayerInRange: {isPlayerInRange}");
        Debug.Log($"questAccepted: {questAccepted}");
        Debug.Log($"isTimelinePlaying: {isTimelinePlaying}");
        
        bool buttonActive = false;
        if (talkButtonObject != null) buttonActive = talkButtonObject.activeSelf;
        else if (talkButton != null) buttonActive = talkButton.gameObject.activeSelf;
        
        Debug.Log($"Talk Button Active: {buttonActive}");
        Debug.Log($"Has Key: {CheckPlayerHasKey()}");
        Debug.Log($"Profile Panel Active: {(profilePanel != null ? profilePanel.activeSelf.ToString() : "Not Assigned")}");
        Debug.Log("====================");
    }
}