using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class PlayTimelineOnTrigger : MonoBehaviour
{
    [Header("References")]
    public PlayableDirector playableDirector;
    public Kingdom4GameEndManager gameEndManager; // Add reference to GameEndManager
    
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
    private AllerthriaGameManager gameManager;
    
    void Start()
    {
        // Get references
        gameManager = AllerthriaGameManager.Instance;
        
        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
        
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
        bool hasKey = CheckIfPlayerHasOCRScannerKey();
        Debug.Log($"PlayTimelineOnTrigger initialized. Has OCR Scanner Key: {hasKey}");
        
        // If player already has key, make sure game manager is in EndGame phase
        if (hasKey && gameManager != null)
        {
            gameManager.hasKey = true;
            gameManager.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
            Debug.Log("Player already has OCR Scanner Key - set phase to EndGame");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered())
        {
            Debug.Log($"Player entered trigger. Checking phase...");

            if (gameEndManager == null)
                gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
            
            // Check current phase
            if (gameManager == null)
            {
                Debug.LogWarning("AllerthriaGameManager.Instance is null. Using direct summary fallback.");
                if (reachQueen || completeGame)
                {
                    TriggerGameSummary();
                    MarkAsPlayed();
                }
                return;
            }
            
            Debug.Log($"Current Phase: {gameManager.currentPhase}");
            
            // Check if player already has OCR Scanner Key (returning player)
            bool hasKeyAlready = CheckIfPlayerHasOCRScannerKey();
            Debug.Log($"Player has OCR Scanner Key already: {hasKeyAlready}");
            
            // Handle different phases
            if ((triggerOnCastlePhase && gameManager.currentPhase == AllerthriaGameManager.GamePhase.CastlePhase) ||
                (triggerOnPlatformPhase && gameManager.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase))
            {
                Debug.Log("Trigger: Platform or Castle Phase - Player reached queen area");
                HandleCastlePhase(hasKeyAlready);
            }
            else if (triggerOnEndGame && gameManager.currentPhase == AllerthriaGameManager.GamePhase.EndGame)
            {
                Debug.Log("Trigger: End Game - Player returned with key");
                HandleEndGamePhase(hasKeyAlready);
            }
            else if (hasKeyAlready && gameManager.currentPhase == AllerthriaGameManager.GamePhase.KeyPhase)
            {
                Debug.Log("Trigger: Player has key but still in KeyPhase - transitioning to EndGame");
                gameManager.StartPhase(AllerthriaGameManager.GamePhase.EndGame);
                HandleEndGamePhase(true);
            }
            else if (reachQueen || completeGame)
            {
                // Fallback path: queen/end trigger should always be able to open summary.
                Debug.Log("Phase did not match expected branches. Forcing summary fallback.");
                if (reachQueen)
                {
                    gameManager.ReachQueen();
                }
                if (completeGame)
                {
                    gameManager.CompleteGame();
                }
                TriggerGameSummary();
                MarkAsPlayed();
            }
        }
    }
    
    private void HandleCastlePhase(bool hasKeyAlready)
    {
        // Always proceed straight to summary from queen trigger.
        // Timeline playback is intentionally bypassed.
        if (gameManager.currentPhase == AllerthriaGameManager.GamePhase.PlatformPhase)
        {
            Debug.Log("Transitioning from Platform Phase to Castle Phase");
            gameManager.StartPhase(AllerthriaGameManager.GamePhase.CastlePhase);
        }

        if (reachQueen)
        {
            gameManager.ReachQueen();
        }

        if (completeGame)
        {
            gameManager.CompleteGame();
        }

        TriggerGameSummary();
        
        MarkAsPlayed();
    }
    
    private void HandleEndGamePhase(bool hasKeyAlready)
    {
        // Player should definitely have key if they're in EndGame phase
        if (!hasKeyAlready)
        {
            Debug.LogWarning("Player in EndGame phase but doesn't have OCR Scanner Key! Checking saved data...");
            hasKeyAlready = CheckIfPlayerHasOCRScannerKey(); // Re-check
        }
        
        if (hasKeyAlready)
        {
            Debug.Log("Player in EndGame phase with OCR Scanner Key - showing summary");
            
            // Trigger end game actions
            if (completeGame)
            {
                gameManager.CompleteGame();
            }
            
            // Trigger game summary immediately (no timeline)
            TriggerGameSummary();
        }
        else
        {
            Debug.LogError("Player in EndGame phase but no OCR Scanner Key found!");
        }
        
        MarkAsPlayed();
    }
    
    private bool CheckIfPlayerHasOCRScannerKey()
    {
        try
        {
            // Method 1: Check GameDataManager (saved data) - This is the primary source
            if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
            {
                bool hasKey = GameDataManager.Instance.CurrentGameData.HasOCRScannerKey();
                Debug.Log($"OCR Scanner Key status from GameData: {hasKey}");
                
                // Sync with AllerthriaGameManager if needed
                if (hasKey && gameManager != null)
                {
                    gameManager.hasKey = true;
                    Debug.Log("Synced OCR Scanner Key to AllerthriaGameManager");
                }
                return hasKey;
            }
            
            // Method 2: Check AllerthriaGameManager (current session) as fallback
            if (gameManager != null && gameManager.hasKey)
            {
                Debug.Log("OCR Scanner Key found in AllerthriaGameManager");
                return true;
            }
            
            Debug.Log("OCR Scanner Key not found in any system");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking OCR Scanner Key: {e.Message}");
            return false;
        }
    }
    
    private void TriggerGameSummary()
    {
        Debug.Log("Triggering game summary via Kingdom4GameEndManager...");
        
        if (gameEndManager != null)
        {
            // This will call ShowGameEndScreen(true) which handles the key check internally
            gameEndManager.HandleKingdom4Complete();
            Debug.Log("Game summary triggered successfully");
        }
        else
            Debug.LogError("Kingdom4GameEndManager reference is null! Cannot trigger summary.");
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
    [ContextMenu("Test Check OCR Scanner Key Status")]
    public void TestCheckOCRScannerKeyStatus()
    {
        bool hasKey = CheckIfPlayerHasOCRScannerKey();
        Debug.Log($"=== OCR SCANNER KEY STATUS TEST ===");
        Debug.Log($"GameDataManager.Instance: {GameDataManager.Instance}");
        Debug.Log($"GameData Current: {(GameDataManager.Instance != null ? GameDataManager.Instance.CurrentGameData != null ? "Loaded" : "Not Loaded" : "N/A")}");
        Debug.Log($"GameData.HasOCRScannerKey(): {(GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null ? GameDataManager.Instance.CurrentGameData.HasOCRScannerKey().ToString() : "N/A")}");
        Debug.Log($"AllerthriaGameManager.hasKey: {(gameManager != null ? gameManager.hasKey.ToString() : "N/A")}");
        Debug.Log($"Player has OCR Scanner Key: {hasKey}");
        Debug.Log($"=====================================");
    }
}