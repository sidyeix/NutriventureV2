// WardenInteraction.cs (COMPLETE - Works with AllerthriaGameManager)
using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.UI;

public class WardenInteraction : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button talkButton;
    [SerializeField] private GameObject talkButtonObject; // Drag the actual button GameObject here
    
    [Header("Game UI References")]
    [SerializeField] private GameObject gamePanel; // Main game panel that contains timer, hearts, points
    [SerializeField] private GameObject heartsContainer;
    [SerializeField] private GameObject pointsPanel;
    [SerializeField] private GameObject timerPanel;
    
    [Header("Timelines")]
    [SerializeField] private PlayableDirector firstArrivalTimeline;  // First time meeting
    [SerializeField] private PlayableDirector keyReturnTimeline;     // Returning with key
    
    [Header("Quest Settings")]
    [SerializeField] private bool isKeyGiverNPC = true; // Is this the NPC that gives the key?
    
    [Header("Game Start Settings")]
    [SerializeField] private bool startsGameTimer = true; // Should this NPC start the game timer?
    [SerializeField] private bool isFirstWardenInteraction = true; // Is this the initial game start?
    
    // Timer reference
    private GameTimer gameTimer;
    
    // Quest acceptance flag
    private bool questAccepted = false;
    private bool isTimelinePlaying = false;
    private bool isPlayerInRange = false;
    
    private void Start()
    {
        Debug.Log($"[WardenInteraction] Start called on {gameObject.name}");
        
        // Make sure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Hide talk button at start
        HideTalkButton();
        
        // Deactivate all game UI at start
        DeactivateAllGameUI();
        
        // Setup button click
        if (talkButton != null)
        {
            talkButton.onClick.RemoveAllListeners();
            talkButton.onClick.AddListener(StartIntroTimeline);
        }
        
        // Find the game timer
        FindGameTimer();
        
        // Check if player already has key
        CheckIfKeyAlreadyCollected();
    }
    
    private void DeactivateAllGameUI()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (pointsPanel != null) pointsPanel.SetActive(false);
        if (timerPanel != null) timerPanel.SetActive(false);
        
        Debug.Log("All game UI deactivated");
    }
    
    private void FindGameTimer()
    {
        gameTimer = GameTimer.Instance;
        if (gameTimer == null)
        {
            gameTimer = FindObjectOfType<GameTimer>();
        }
        
        if (isFirstWardenInteraction && startsGameTimer && gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }
    }
    
    private void OnEnable()
    {
        // Reset interaction state when NPC is enabled
        questAccepted = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTimelinePlaying)
        {
            isPlayerInRange = true;
            
            // Only show if quest hasn't been accepted yet
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
    
    // Check if player already has key (uses AllerthriaGameManager)
    private void CheckIfKeyAlreadyCollected()
    {
        bool hasKey = CheckPlayerHasKey();
        
        if (hasKey && isKeyGiverNPC)
        {
            Debug.Log("Player already has key. Will play return cutscene.");
        }
    }
    
    // Check player has key from AllerthriaGameManager
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
        {
            talkButtonObject.SetActive(true);
        }
        else if (talkButton != null)
        {
            talkButton.gameObject.SetActive(true);
        }
    }
    
    private void HideTalkButton()
    {
        Debug.Log("Hiding talk button");
        
        if (talkButtonObject != null)
        {
            talkButtonObject.SetActive(false);
        }
        else if (talkButton != null)
        {
            talkButton.gameObject.SetActive(false);
        }
    }
    
    private void StartIntroTimeline()
    {
        if (questAccepted)
        {
            Debug.Log("Quest already accepted, cannot start intro again");
            return;
        }
        
        // Check if we should play return cutscene instead
        if (CheckPlayerHasKey() && keyReturnTimeline != null)
        {
            Debug.Log("Player has key, playing return cutscene");
            isTimelinePlaying = true;
            HideTalkButton();
            keyReturnTimeline.Play();
            StartCoroutine(WaitForTimelineEnd(keyReturnTimeline));
            return;
        }
        
        // Play intro timeline
        if (firstArrivalTimeline != null)
        {
            isTimelinePlaying = true;
            
            // Hide talk button immediately
            HideTalkButton();

            // Play the intro timeline
            firstArrivalTimeline.Play();
            
            StartCoroutine(WaitForTimelineEnd(firstArrivalTimeline));
            
            Debug.Log("Intro timeline started - waiting for quest acceptance");
        }
        else
        {
            Debug.LogError("No First Arrival Timeline assigned!");
        }
    }
    
    // Method to be called when player accepts the quest
    public void OnQuestAccepted()
    {
        if (questAccepted) return;
        
        questAccepted = true;
        Debug.Log("QUEST ACCEPTED! Starting game timer and activating UI...");
        
        // Stop the intro timeline if it's still playing
        if (firstArrivalTimeline != null && firstArrivalTimeline.state == PlayState.Playing)
        {
            firstArrivalTimeline.Stop();
        }
        
        // Play the accept timeline if available
        if (TimelineChoiceManager.Instance != null && TimelineChoiceManager.Instance.acceptTimeline != null)
        {
            TimelineChoiceManager.Instance.AcceptQuest();
        }
        
        // Start the timer - This will call AllerthriaGameManager.OnGameTimerStarted()
        StartGameTimerNow();
        
        // Activate all game UI
        ActivateAllGameUI();
        
        // Hide talk button permanently
        HideTalkButton();
    }
    
    // Method to be called when player rejects the quest
    public void OnQuestRejected()
{
    Debug.Log("QUEST REJECTED");

    if (firstArrivalTimeline != null)
    {
        firstArrivalTimeline.Stop();
        firstArrivalTimeline.time = 0;
        firstArrivalTimeline.Evaluate();
    }

    // 🔥 FORCE RESET
    isTimelinePlaying = false;

    if (isPlayerInRange)
        ShowTalkButton();
}

    
    // Start the game timer
    private void StartGameTimerNow()
    {
        if (startsGameTimer && isFirstWardenInteraction && gameTimer != null)
        {
            if (gameTimer.CanStartTimer())
            {
                gameTimer.StartTimerFromInteraction();
                Debug.Log("GAME TIMER STARTED!");
                
                // This will trigger AllerthriaGameManager.OnGameTimerStarted()
                // through the GameTimer's events
            }
        }
    }
    
    // Activate all game UI elements
    private void ActivateAllGameUI()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (heartsContainer != null) heartsContainer.SetActive(true);
        if (pointsPanel != null) pointsPanel.SetActive(true);
        if (timerPanel != null) timerPanel.SetActive(true);
        
        Debug.Log("Game UI activated - Hearts, Points, and Timer panels should now be visible");
    }
    
    private IEnumerator WaitForTimelineEnd(PlayableDirector director)
{
    while (director != null && director.state == PlayState.Playing)
        yield return null;

    isTimelinePlaying = false;

    Debug.Log("Timeline ended → flag reset");

    if (isPlayerInRange && !questAccepted)
        ShowTalkButton();
}

    
    // Public method to check if this NPC starts the timer
    public bool DoesStartTimer()
    {
        return startsGameTimer;
    }
    
    // For debugging
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
        Debug.Log("====================");
    }
}