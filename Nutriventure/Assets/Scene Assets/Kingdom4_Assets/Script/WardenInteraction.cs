// WardenInteraction.cs (UPDATED)
using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.UI;

public class WardenInteraction : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject talkButtonUI; // The actual GameObject
    [SerializeField] private CanvasGroup talkButtonCanvas;
    [SerializeField] private RectTransform talkButtonRect;
    [SerializeField] private Button talkButton;
    
    [Header("Timelines")]
    [SerializeField] private PlayableDirector firstArrivalTimeline;  // First time meeting
    [SerializeField] private PlayableDirector keyReturnTimeline;     // Returning with key
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float slideDistance = 50f;
    
    [Header("Quest Settings")]
    [SerializeField] private bool isKeyGiverNPC = true; // Is this the NPC that gives the key?
    [SerializeField] private bool showButtonOnlyOnce = false; // Only show button once per visit
    
    [Header("Game Start Settings")]
    [SerializeField] private bool startsGameTimer = true; // Should this NPC start the game timer?
    [SerializeField] private bool isFirstWardenInteraction = true; // Is this the initial game start?
    
    // Timer reference
    private GameTimer gameTimer;
    
    private bool isTimelinePlaying = false;
    private bool isPlayerInRange = false;
    private bool isUIVisible = false;
    private bool hasInteractedThisVisit = false;
    private Vector2 originalPosition;
    private Coroutine animationCoroutine;
    
    private void Start()
    {
        // Make sure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        
        // Deactivate UI completely at start
        if (talkButtonUI != null)
        {
            talkButtonUI.SetActive(false);
        }
        
        // Store original position for animation
        if (talkButtonRect != null)
        {
            originalPosition = talkButtonRect.anchoredPosition;
        }
        
        // Setup button click
        if (talkButton != null)
        {
            talkButton.onClick.AddListener(StartAppropriateTimeline);
        }
        
        // Find the game timer
        FindGameTimer();
        
        // Check if player already has key
        CheckIfKeyAlreadyCollected();
    }
    
    private void FindGameTimer()
    {
        // Find the GameTimer in the scene
        gameTimer = GameTimer.Instance;
        if (gameTimer == null)
        {
            Debug.LogWarning("GameTimer not found in scene! Timer won't start automatically.");
            
            // Try to find it manually
            gameTimer = FindObjectOfType<GameTimer>();
            if (gameTimer != null)
            {
                Debug.Log($"Found GameTimer: {gameTimer.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"Found GameTimer Instance: {gameTimer.gameObject.name}");
            
            // If this is the first warden and timer should start, ensure timer is reset
            if (isFirstWardenInteraction && startsGameTimer)
            {
                gameTimer.ResetTimer(false);
            }
        }
    }
    
    private void OnEnable()
    {
        // Reset interaction state when NPC is enabled
        hasInteractedThisVisit = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTimelinePlaying)
        {
            isPlayerInRange = true;
            
            // Check if we should show the button
            if (!showButtonOnlyOnce || !hasInteractedThisVisit)
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
        // This method can be used to adjust NPC behavior based on key status
        // For example, change dialogue or appearance
        bool hasKey = CheckPlayerHasKey();
        
        if (hasKey && isKeyGiverNPC)
        {
            Debug.Log("Player already has key. NPC behavior may change.");
            // You could change NPC animation, dialogue options, etc. here
        }
    }
    
    private bool CheckPlayerHasKey()
    {
        // Check multiple sources for key status
        if (AllerthriaGameManager.Instance != null)
        {
            return AllerthriaGameManager.Instance.hasKey;
        }
        
        // If you have a GameDataManager1, check there too
        /*
        if (GameDataManager1.Instance != null)
        {
            return GameDataManager1.Instance.currentGameData.hasKey;
        }
        */
        
        // Check PlayerPrefs as fallback
        string keyId = "castle_key"; // You might want to make this configurable
        return PlayerPrefs.HasKey($"KeyCollected_{keyId}");
    }
    
    private PlayableDirector GetAppropriateTimeline()
    {
        bool hasKey = CheckPlayerHasKey();
        
        if (hasKey && keyReturnTimeline != null)
        {
            Debug.Log("Playing key return timeline - player already has key");
            return keyReturnTimeline;
        }
        else if (firstArrivalTimeline != null)
        {
            Debug.Log("Playing first arrival timeline");
            return firstArrivalTimeline;
        }
        
        Debug.LogWarning("No appropriate timeline found!");
        return null;
    }
    
    private void ShowTalkButton()
    {
        if (isUIVisible) return;
        
        // Check if we should even show the button
        if (isKeyGiverNPC)
        {
            bool hasKey = CheckPlayerHasKey();
            
            // If this is the key-giving NPC and player already has key,
            // you might want to hide the button or show different text
            // For now, we'll still show it but with different behavior
            if (hasKey)
            {
                Debug.Log("Player already has key, but showing button for return cutscene");
            }
        }
        
        // Activate the UI GameObject first
        if (talkButtonUI != null)
        {
            talkButtonUI.SetActive(true);
        }
        
        // Set initial state (invisible and moved down)
        if (talkButtonCanvas != null)
        {
            talkButtonCanvas.alpha = 0f;
            talkButtonCanvas.blocksRaycasts = false;
            talkButtonCanvas.interactable = false;
        }
        
        if (talkButtonRect != null)
        {
            talkButtonRect.anchoredPosition = originalPosition + Vector2.down * slideDistance;
        }
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateButton(true));
    }
    
    private void HideTalkButton()
    {
        if (!isUIVisible) return;
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateButton(false));
    }
    
    private IEnumerator AnimateButton(bool show)
    {
        isUIVisible = show;
        
        // Enable/disable button interaction
        if (talkButtonCanvas != null)
        {
            talkButtonCanvas.blocksRaycasts = show;
            talkButtonCanvas.interactable = show;
        }
        
        float startAlpha = talkButtonCanvas != null ? talkButtonCanvas.alpha : 0f;
        float targetAlpha = show ? 1f : 0f;
        
        Vector2 startPos = talkButtonRect != null ? talkButtonRect.anchoredPosition : Vector2.zero;
        Vector2 targetPos = show ? originalPosition : 
            originalPosition + Vector2.down * slideDistance;
        
        float time = 0f;
        
        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            
            // Fade
            if (talkButtonCanvas != null)
                talkButtonCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            // Slide
            if (talkButtonRect != null)
                talkButtonRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            yield return null;
        }
        
        // Set final values
        if (talkButtonCanvas != null)
            talkButtonCanvas.alpha = targetAlpha;
        
        if (talkButtonRect != null)
            talkButtonRect.anchoredPosition = targetPos;
        
        // Deactivate GameObject when hidden
        if (!show && talkButtonUI != null)
        {
            talkButtonUI.SetActive(false);
        }
        
        animationCoroutine = null;
    }
    
    private void StartAppropriateTimeline()
    {
        PlayableDirector timelineToPlay = GetAppropriateTimeline();
        
        if (timelineToPlay != null)
        {
            isTimelinePlaying = true;
            hasInteractedThisVisit = true;

            // Force-hide UI immediately
            HideTalkButton();

            // NEW: Start the game timer if this is the first warden interaction
            StartGameTimerIfNeeded();

            timelineToPlay.Play();
            StartCoroutine(WaitForTimelineEnd(timelineToPlay));
            
            // Handle quest progression if this is key-giving NPC
            if (isKeyGiverNPC && !CheckPlayerHasKey())
            {
                // This is the first time getting the key
                // The key collection will be handled by CollectibleKey script
                Debug.Log("First time interacting with key-giving NPC");
            }
        }
        else
        {
            Debug.LogError("No Timeline Director assigned!");
        }
    }
    
    // Method to start the game timer
    private void StartGameTimerIfNeeded()
    {
        if (startsGameTimer && isFirstWardenInteraction && gameTimer != null)
        {
            if (gameTimer.CanStartTimer())
            {
                gameTimer.StartTimerFromInteraction();
                
                // Also trigger game start in other systems if needed
                TriggerGameStartEvents();
            }
            else
            {
                Debug.LogWarning("Timer cannot start (already running or not ready)");
            }
        }
        else if (startsGameTimer && gameTimer == null)
        {
            Debug.LogWarning("Game timer not found, but this NPC should start it!");
        }
    }
    
    // Trigger other game start events
    private void TriggerGameStartEvents()
    {
        // Notify AllerthriaGameManager that game has started
        if (AllerthriaGameManager.Instance != null)
        {
            // You might need to check the actual method name in your AllerthriaGameManager
            AllerthriaGameManager.Instance.OnGameTimerStarted();
            Debug.Log("Notified AllerthriaGameManager that game timer has started");
        }
        else
        {
            Debug.LogWarning("AllerthriaGameManager.Instance is null!");
        }
        
        // Notify Kingdom4ScoreManager that game has started
        if (Kingdom4ScoreManager.Instance != null)
        {
            // You might want to add a game start method to your score manager
            Kingdom4ScoreManager.Instance.OnGameStarted();
            Debug.Log("Notified Kingdom4ScoreManager that game timer has started");
        }
        else
        {
            Debug.LogWarning("Kingdom4ScoreManager.Instance is null!");
        }
        
        // Notify Kingdom4GameEndManager that game has started
        if (Kingdom4GameEndManager.Instance != null)
        {
            // You might want to add a game start method to your game end manager
            Kingdom4GameEndManager.Instance.OnGameStarted();
            Debug.Log("Notified Kingdom4GameEndManager that game timer has started");
        }
        else
        {
            Debug.LogWarning("Kingdom4GameEndManager.Instance is null!");
        }
        
        // You could also trigger UI elements, audio, etc.
        Debug.Log("GAME STARTED! Timer is now running.");
    }
    
    private IEnumerator WaitForTimelineEnd(PlayableDirector director)
    {
        yield return null;

        while (director != null)
        {
            // If timeline stopped → exit
            if (director.state == PlayState.Paused)
            {
                // Wait until resumed
                yield return new WaitUntil(() =>
                    director.state != PlayState.Paused);
            }

            if (director.state != PlayState.Playing)
                break;

            yield return null;
        }

        isTimelinePlaying = false;

        // Only show button again if player is still in range AND we allow multiple interactions
        if (isPlayerInRange && (!showButtonOnlyOnce || !hasInteractedThisVisit))
        {
            ShowTalkButton();
        }
    }
    
    // Public method to manually trigger the appropriate timeline
    public void TriggerNPCInteraction()
    {
        if (!isTimelinePlaying)
        {
            StartAppropriateTimeline();
        }
    }
    
    // Method to reset interaction state
    public void ResetInteraction()
    {
        hasInteractedThisVisit = false;
        isTimelinePlaying = false;
        
        if (isPlayerInRange)
        {
            ShowTalkButton();
        }
    }
    
    // Method to check which timeline would play
    public bool WouldPlayReturnCutscene()
    {
        return CheckPlayerHasKey() && keyReturnTimeline != null;
    }
    
    // Public method to check if this NPC starts the timer
    public bool DoesStartTimer()
    {
        return startsGameTimer;
    }
    
    // Public method to manually start timer (for debugging or other triggers)
    public void ManuallyStartGameTimer()
    {
        if (gameTimer != null && !gameTimer.IsActive)
        {
            gameTimer.StartTimerFromInteraction();
            Debug.Log("Game timer manually started by NPC");
        }
    }
    
    // For debugging in the Inspector
    [ContextMenu("Test Check Key Status")]
    public void TestCheckKeyStatus()
    {
        bool hasKey = CheckPlayerHasKey();
        Debug.Log($"Player has key: {hasKey}");
        Debug.Log($"Would play: {(hasKey ? "Return Cutscene" : "First Arrival Cutscene")}");
    }
    
    [ContextMenu("Force Play First Arrival")]
    public void ForcePlayFirstArrival()
    {
        if (firstArrivalTimeline != null)
        {
            isTimelinePlaying = true;
            firstArrivalTimeline.Play();
            
            // Also start timer if this is the first warden
            StartGameTimerIfNeeded();
            
            StartCoroutine(WaitForTimelineEnd(firstArrivalTimeline));
        }
    }
    
    [ContextMenu("Force Play Return Cutscene")]
    public void ForcePlayReturnCutscene()
    {
        if (keyReturnTimeline != null)
        {
            isTimelinePlaying = true;
            keyReturnTimeline.Play();
            StartCoroutine(WaitForTimelineEnd(keyReturnTimeline));
        }
    }
    
    [ContextMenu("Test Timer Start")]
    public void TestTimerStart()
    {
        if (gameTimer != null)
        {
            Debug.Log($"Timer state: Active={gameTimer.IsActive}, CanStart={gameTimer.CanStartTimer()}");
            
            if (gameTimer.CanStartTimer())
            {
                gameTimer.StartTimerFromInteraction();
                Debug.Log("Timer started via test");
            }
            else
            {
                Debug.Log("Timer cannot start (already running or not ready)");
            }
        }
        else
        {
            Debug.LogWarning("Game timer not found!");
        }
    }
}