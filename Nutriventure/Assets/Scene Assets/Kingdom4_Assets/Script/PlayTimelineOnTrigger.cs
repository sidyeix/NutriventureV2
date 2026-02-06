using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class PlayTimelineOnTrigger : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector playableDirector;
    
    [Header("Settings")]
    public bool playOnlyOnce = true;
    public bool skipToSummaryIfHasKey = true;
    public float timelineDelay = 0.5f;
    
    [Header("Phase Settings")]
    public bool triggerOnCastlePhase = true;
    public bool triggerOnEndGame = true;
    public bool triggerOnPlatformPhase = true;
    public bool reachQueen = true;
    public bool completeGame = true;
    
    private bool hasPlayed = false;
    
    void Start()
    {
        // Ensure playableDirector is found if not assigned
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();
        }
        
        // Initialize key check after a short delay
        Invoke("InitializeKeyCheck", 0.5f);
    }
    
    void InitializeKeyCheck()
    {
        // This ensures managers are loaded before we check
        bool hasKey = CheckIfPlayerHasKey();
        Debug.Log($"PlayTimelineOnTrigger initialized. Has Key: {hasKey}");
        
        // If player already has key, make sure game manager is in EndGame phase
        if (hasKey && AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.hasKey = true;
            AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
            Debug.Log("Player already has key - set phase to EndGame");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered())
        {
            Debug.Log($"Player entered trigger. Checking phase...");
            
            // Check current phase
            if (AllerthriaGameManager.Instance == null)
            {
                Debug.LogError("AllerthriaGameManager.Instance is null!");
                return;
            }
            
            Debug.Log($"Current Phase: {AllerthriaGameManager.Instance.currentPhase}");
            
            // Check if player already has key (returning player)
            bool hasKeyAlready = CheckIfPlayerHasKey();
            Debug.Log($"Player has key already: {hasKeyAlready}");
            
            // Handle Castle Phase OR Platform Phase (first arrival OR returning without key collected yet)
            if ((triggerOnCastlePhase && 
                 AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.CastlePhase) ||
                (triggerOnPlatformPhase && 
                 AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase))
            {
                Debug.Log("Trigger: Platform or Castle Phase - Player reached queen area");
                HandleCastlePhase(hasKeyAlready);
            }
            // Handle End Game Phase (returning with key)
            else if (triggerOnEndGame && 
                     AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.EndGame)
            {
                Debug.Log("Trigger: End Game - Player returned with key");
                HandleEndGamePhase(hasKeyAlready);
            }
            // Handle Key Phase (player has key but game manager might not be in EndGame yet)
            else if (hasKeyAlready && AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.KeyPhase)
            {
                Debug.Log("Trigger: Player has key but still in KeyPhase - transitioning to EndGame");
                AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
                HandleEndGamePhase(true);
            }
        }
    }
    
    private void HandleCastlePhase(bool hasKeyAlready)
    {
        // If player already has key AND we should skip to summary
        if (hasKeyAlready && skipToSummaryIfHasKey)
        {
            Debug.Log("Player already has key in Castle Phase - going straight to summary");
            
            // Trigger castle phase actions
            if (reachQueen)
            {
                AllerthriaGameManager.Instance.ReachQueen();
            }
            
            // If we're in Platform Phase, transition to Castle Phase first
            if (AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase)
            {
                AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.CastlePhase);
            }
            
            // Go straight to game summary
            TriggerGameSummary();
            
            MarkAsPlayed();
            return;
        }
        
        // If we're in Platform Phase, transition to Castle Phase first
        if (AllerthriaGameManager.Instance.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase)
        {
            Debug.Log("Transitioning from Platform Phase to Castle Phase");
            AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.CastlePhase);
        }
        
        // Normal flow (first time or if we're not skipping)
        if (reachQueen)
        {
            AllerthriaGameManager.Instance.ReachQueen();
        }
        
        // Play timeline if assigned (only for first time)
        if (playableDirector != null && !hasKeyAlready)
        {
            StartCoroutine(PlayTimelineWithDelay());
        }
        else if (hasKeyAlready)
        {
            Debug.Log("Player has key but timeline not played (skipToSummaryIfHasKey is false)");
        }
        
        MarkAsPlayed();
    }
    
    private void HandleEndGamePhase(bool hasKeyAlready)
    {
        // Player should definitely have key if they're in EndGame phase
        if (!hasKeyAlready)
        {
            Debug.LogWarning("Player in EndGame phase but doesn't have key! Checking saved data...");
            hasKeyAlready = CheckIfPlayerHasKey(); // Re-check
        }
        
        if (hasKeyAlready)
        {
            Debug.Log("Player in EndGame phase with key - showing summary");
            
            // Trigger end game actions
            if (completeGame)
            {
                AllerthriaGameManager.Instance.CompleteGame();
            }
            
            // Trigger game summary immediately (no timeline)
            TriggerGameSummary();
        }
        else
        {
            Debug.LogError("Player in EndGame phase but no key found!");
        }
        
        MarkAsPlayed();
    }
    
    private bool CheckIfPlayerHasKey()
    {
        try
        {
            // Method 1: Check AllerthriaGameManager (current session)
            if (AllerthriaGameManager.Instance != null && AllerthriaGameManager.Instance.hasKey)
            {
                Debug.Log("Key found in AllerthriaGameManager");
                return true;
            }
            
            // Method 2: Check GameDataManager (saved data)
            if (GameDataManager1.Instance != null && GameDataManager1.Instance.currentGameData.hasKey)
            {
                Debug.Log("Key found in GameDataManager");
                // Sync with AllerthriaGameManager
                if (AllerthriaGameManager.Instance != null)
                {
                    AllerthriaGameManager.Instance.hasKey = true;
                    // Only set to EndGame if we're not in the middle of getting the key
                    if (AllerthriaGameManager.Instance.currentPhase != AllerthriaGameManager.GamePhase.KeyPhase)
                    {
                        AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
                    }
                    Debug.Log("Synced key to AllerthriaGameManager");
                }
                return true;
            }
            
            // Method 3: Check PlayerPrefs as fallback
            if (PlayerPrefs.GetInt("KeyCollected_castle_key", 0) == 1)
            {
                Debug.Log("Key found in PlayerPrefs");
                // Sync with both managers
                if (AllerthriaGameManager.Instance != null)
                {
                    AllerthriaGameManager.Instance.hasKey = true;
                    // Only set to EndGame if we're not in the middle of getting the key
                    if (AllerthriaGameManager.Instance.currentPhase != AllerthriaGameManager.GamePhase.KeyPhase)
                    {
                        AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
                    }
                }
                if (GameDataManager1.Instance != null)
                {
                    GameDataManager1.Instance.currentGameData.hasKey = true;
                    GameDataManager1.Instance.SaveGameProgress();
                }
                return true;
            }
            
            Debug.Log("Key not found in any system");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking key: {e.Message}");
            return false;
        }
    }
    
    private void TriggerGameSummary()
    {
        Debug.Log("Triggering game summary...");
        
        // Try K4GameSummary first
        K4GameSummary gameSummary = FindObjectOfType<K4GameSummary>();
        if (gameSummary != null)
        {
            gameSummary.TriggerSummaryFromKey();
            Debug.Log("Triggered game summary via K4GameSummary");
            return;
        }
        
        // Fallback to Kingdom4GameEndManager
        Kingdom4GameEndManager gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4Complete();
            Debug.Log("Triggered game summary via Kingdom4GameEndManager");
            return;
        }
        
        // Fallback to AllerthriaGameManager's CompleteGame
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.CompleteGame();
            Debug.Log("Triggered game completion via AllerthriaGameManager");
            return;
        }
        
        Debug.LogWarning("No game summary manager found!");
    }
    
    private IEnumerator PlayTimelineWithDelay()
    {
        yield return new WaitForSeconds(timelineDelay);
        
        if (playableDirector != null)
        {
            Debug.Log($"Playing timeline: {playableDirector.name}");
            playableDirector.Play();
        }
    }
    
    private bool hasTriggered()
    {
        return playOnlyOnce && hasPlayed;
    }
    
    private void MarkAsPlayed()
    {
        if (playOnlyOnce)
        {
            hasPlayed = true;
            Debug.Log($"Trigger marked as played. Will not trigger again.");
            
            // Optional: Disable the trigger collider
            Collider collider = GetComponent<Collider>();
            if (collider != null && collider.isTrigger)
            {
                collider.enabled = false;
            }
        }
    }
    
    // For debugging
    [ContextMenu("Test Check Key Status")]
    public void TestCheckKeyStatus()
    {
        bool hasKey = CheckIfPlayerHasKey();
        Debug.Log($"=== KEY STATUS TEST ===");
        Debug.Log($"AllerthriaGameManager.Instance: {AllerthriaGameManager.Instance}");
        Debug.Log($"AllerthriaGameManager.hasKey: {(AllerthriaGameManager.Instance != null ? AllerthriaGameManager.Instance.hasKey.ToString() : "N/A")}");
        Debug.Log($"AllerthriaGameManager.Phase: {(AllerthriaGameManager.Instance != null ? AllerthriaGameManager.Instance.currentPhase.ToString() : "N/A")}");
        Debug.Log($"GameDataManager1.Instance: {GameDataManager1.Instance}");
        Debug.Log($"GameDataManager1.hasKey: {(GameDataManager1.Instance != null ? GameDataManager1.Instance.currentGameData.hasKey.ToString() : "N/A")}");
        Debug.Log($"PlayerPrefs Key: {PlayerPrefs.GetInt("KeyCollected_castle_key", 0)}");
        Debug.Log($"Player has key: {hasKey}");
        Debug.Log($"========================");
    }
    
    [ContextMenu("Force Set Has Key")]
    public void ForceSetHasKey()
    {
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.hasKey = true;
            AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
        }
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.currentGameData.hasKey = true;
            GameDataManager1.Instance.SaveGameProgress();
        }
        PlayerPrefs.SetInt("KeyCollected_castle_key", 1);
        PlayerPrefs.Save();
        Debug.Log("Key force-set for testing! Phase set to EndGame.");
    }
    
    [ContextMenu("Reset Key")]
    public void ResetKey()
    {
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.hasKey = false;
            AllerthriaGameManager.Instance.StartPhase(AllerthriaGameManager.GamePhase.ScrollQuest);
        }
        if (GameDataManager1.Instance != null)
        {
            GameDataManager1.Instance.currentGameData.hasKey = false;
            GameDataManager1.Instance.SaveGameProgress();
        }
        PlayerPrefs.DeleteKey("KeyCollected_castle_key");
        Debug.Log("Key reset! Phase set to ScrollQuest.");
    }
}