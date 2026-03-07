using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class AllerthriaGameManager : MonoBehaviour
{
    public static AllerthriaGameManager Instance { get; private set; }
    
    public enum GamePhase
    {
        ScrollQuest,
        AllergenHunt,
        WagonPhase,
        PlatformPhase,
        CastlePhase,
        KeyPhase,
        EndGame
    }
    
    [Header("Game Flow")]
    public GamePhase currentPhase = GamePhase.ScrollQuest;
    
    [Header("Game State")]
    public bool isGameStarted = false; // Track if game has officially started
    public bool isTimerRunning = false; // Track if timer is running
    public bool isGameComplete = false; // Track if game is completed
    
    [Header("Quest Items")]
    public bool hasScroll = false;
    public List<string> collectedAllergens = new List<string>();
    public bool hasKey = false;
    
    [Header("References")]
    public GameObject scroll;
    public GameObject wagon;
    public GameObject movingPlatform;
    
    [Header("UI")]
    public TextMeshProUGUI questText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI allergenCountText;
    public TextMeshProUGUI wagonHitsText;
    public GameObject gameCompletePanel;
    public TextMeshProUGUI finalScoreText;
    
    [Header("Timer Integration")]
    [SerializeField] private GameTimer gameTimer; // Reference to timer
    [SerializeField] private Kingdom4GameEndManager gameEndManager; // Reference to game end manager
    
    // Events
    public event Action<GamePhase> OnPhaseChanged;
    public event Action OnGameStarted;
    public event Action OnGameCompleted;
    public event Action OnGameOver;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        StartPhase(GamePhase.ScrollQuest);
        
        UpdateQuestText("Find the scroll");
        UpdateScoreDisplay();
        
        // Initialize references if not set
        if (gameTimer == null)
            gameTimer = GameTimer.Instance;
        
        if (gameTimer == null)
            gameTimer = FindObjectOfType<GameTimer>();
        
        if (gameEndManager == null)
            gameEndManager = Kingdom4GameEndManager.Instance;
        
        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
        
        // Subscribe to score manager events
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.OnScoreChanged.AddListener(OnScoreChanged);
            Kingdom4ScoreManager.Instance.OnMultiplierChanged.AddListener(OnMultiplierChanged);
        }
        
        // Subscribe to timer events if available
        if (gameTimer != null)
        {
            // Optional: Listen for timer events
            Debug.Log("Connected to GameTimer");
        }
        
        Debug.Log("AllerthriaGameManager initialized");
    }
    
    void Update()
    {
        UpdatePhaseSpecificDisplay();
        
        // Check for time up game over
        if (isGameStarted && !isGameComplete && gameTimer != null && gameTimer.IsOverMaxTime)
        {
            TriggerGameOverByTime();
        }
        
        // Optional: Check for health-based game over
        CheckHealthBasedGameOver();
    }
    
    #region Game State Management
    
    // Called when timer starts (from WardenInteraction)
    public void OnGameTimerStarted()
    {
        isGameStarted = true;
        isTimerRunning = true;
        Debug.Log("Game officially started - timer is running");
        
        UpdateQuestText("Game started! Timer is running.");
        OnGameStarted?.Invoke();
        
        // Notify WardenInteraction if needed
        var warden = FindObjectOfType<WardenInteraction>();
        if (warden != null && warden.DoesStartTimer())
        {
            Debug.Log("First warden interaction completed, game timer started");
        }
    }
    
    // Called when timer stops
    public void OnGameTimerStopped()
    {
        isTimerRunning = false;
        Debug.Log("Game timer stopped");
    }
    
    // Get elapsed time for scoring
    public float GetElapsedTime()
    {
        if (gameTimer != null)
        {
            return gameTimer.ElapsedTime;
        }
        return 0f;
    }
    
    // Get remaining time
    public float GetRemainingTime()
    {
        if (gameTimer != null)
        {
            return Mathf.Max(0f, gameTimer.MaxGameTime - gameTimer.ElapsedTime);
        }
        return 0f;
    }
    
    // Check if all phases completed
    public bool AllPhasesCompleted()
    {
        return currentPhase == GamePhase.EndGame;
    }
    
    // Get current life amount (for compatibility with health system)
    public float GetCurrentLifeAmount()
    {
        if (PlayerHealthManager.Instance != null)
        {
            return PlayerHealthManager.Instance.currentHealth;
        }
        return 3f; // Default fallback
    }
    
    // Get current star rating based on time
    public int GetCurrentStarRating()
    {
        if (gameTimer != null)
        {
            return gameTimer.CurrentStarRating;
        }
        return 0;
    }
    
    #endregion
    
    #region UI Management
    private void UpdatePhaseSpecificDisplay()
    {
        switch (currentPhase)
        {
            case GamePhase.AllergenHunt:
                if (allergenCountText != null && Kingdom4ScoreManager.Instance != null)
                {
                    allergenCountText.text = $"Allergens: {collectedAllergens.Count}/9";
                }
                break;
                
            case GamePhase.WagonPhase:
                if (wagonHitsText != null && Kingdom4ScoreManager.Instance != null)
                {
                    wagonHitsText.text = $"Wagon Hits: {Kingdom4ScoreManager.Instance.totalWagonHits}";
                }
                break;
        }
        
        // Update timer UI if available
        if (gameTimer != null && questText != null)
        {
            // Add timer info to quest text during gameplay
            if (isGameStarted && !isGameComplete && currentPhase != GamePhase.ScrollQuest)
            {
                string timeInfo = $" | Time: {gameTimer.GetElapsedTimeFormatted()}";
                if (!questText.text.Contains("Time:"))
                {
                    questText.text += timeInfo;
                }
            }
        }
    }
    
    private void OnScoreChanged(int newScore)
    {
        UpdateScoreDisplay();
    }
    
    private void OnMultiplierChanged(int newMultiplier)
    {
        UpdateMultiplierDisplay();
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null && Kingdom4ScoreManager.Instance != null)
        {
            scoreText.text = $"SCORE: {Kingdom4ScoreManager.Instance.GetFinalScore()}";
        }
    }

    public bool CanAccessCastle()
    {
        // Allow castle access during PlatformPhase OR CastlePhase
        return currentPhase == GamePhase.PlatformPhase || currentPhase == GamePhase.CastlePhase;
    }
    
    private void UpdateMultiplierDisplay()
    {
        if (multiplierText != null && Kingdom4ScoreManager.Instance != null)
        {
            multiplierText.text = $"x{Kingdom4ScoreManager.Instance.comboMultiplier}";
            multiplierText.gameObject.SetActive(currentPhase == GamePhase.PlatformPhase);
        }
    }
    
    public void StartPhase(GamePhase phase)
    {
        currentPhase = phase;
        Debug.Log($"Starting phase: {phase}");
        
        UpdateUIVisibility();
        OnPhaseChanged?.Invoke(phase);
        
        // Log phase transition for debugging
        LogPhaseTransition(phase);
        
        switch (phase)
        {
            case GamePhase.ScrollQuest:
                StartScrollQuest();
                break;
            case GamePhase.AllergenHunt:
                StartAllergenHunt();
                break;
            case GamePhase.WagonPhase:
                StartWagonPhase();
                break;
            case GamePhase.PlatformPhase:
                StartPlatformPhase();
                break;
            case GamePhase.CastlePhase:
                StartCastlePhase();
                break;
            case GamePhase.KeyPhase:
                StartKeyPhase();
                break;
            case GamePhase.EndGame:
                StartEndGame();
                break;
        }
    }
    
    private void LogPhaseTransition(GamePhase newPhase)
    {
        string phaseName = newPhase.ToString();
        float elapsedTime = GetElapsedTime();
        int starRating = GetCurrentStarRating();
        
        Debug.Log($"Phase Transition: {currentPhase} -> {newPhase} at {elapsedTime:F1}s (Star Rating: {starRating})");
        
        // Save phase completion time for analytics
        PlayerPrefs.SetString($"Phase_{phaseName}_CompletionTime", elapsedTime.ToString("F1"));
        PlayerPrefs.Save();
    }
    
    private void UpdateUIVisibility()
    {
        if (allergenCountText != null)
        {
            allergenCountText.gameObject.SetActive(currentPhase == GamePhase.AllergenHunt);
        }
        
        if (wagonHitsText != null)
        {
            wagonHitsText.gameObject.SetActive(currentPhase == GamePhase.WagonPhase);
        }
        
        UpdateMultiplierDisplay();
    }
    
    private void UpdateQuestText(string text)
    {
        Debug.Log($"[QUEST] {text}");
        
        if (questText != null)
        {
            questText.text = text;
            
            // If timer is running, append time info
            if (isTimerRunning && gameTimer != null && currentPhase != GamePhase.ScrollQuest)
            {
                questText.text += $" | Time: {gameTimer.GetElapsedTimeFormatted()}";
            }
        }
    }
    
    #endregion
    
    #region Phase Implementations
    
    private void StartScrollQuest()
    {
        UpdateQuestText("Find the scroll");
        if (scroll != null)
            scroll.SetActive(true);
    }
    
    public void CollectScroll()
    {
        hasScroll = true;
        StartPhase(GamePhase.AllergenHunt);
    }
    
    private void StartAllergenHunt()
    {
        UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9");
        
        AllergenSpawnManager spawner = FindObjectOfType<AllergenSpawnManager>();
        if (spawner != null)
            spawner.SpawnNow();
    }
    
    public void CollectAllergen(string allergenId)
    {
        if (!collectedAllergens.Contains(allergenId))
        {
            collectedAllergens.Add(allergenId);
            
            if (Kingdom4ScoreManager.Instance != null)
            {
                Kingdom4ScoreManager.Instance.AddAllergenFound();
            }
            
            UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9");
            
            if (collectedAllergens.Count >= 9)
            {
                StartPhase(GamePhase.WagonPhase);
            }
            
            // Update timer UI with allergen count
            if (gameTimer != null && questText != null)
            {
                questText.text = $"Allergens: {collectedAllergens.Count}/9 | Time: {gameTimer.GetElapsedTimeFormatted()}";
            }
        }
    }
    
    private void StartWagonPhase()
    {
        UpdateQuestText("Drive the wagon to the platform");
        if (wagon != null)
            wagon.SetActive(true);
    }
    
    public void CompleteWagonPhase()
    {
        StartPhase(GamePhase.PlatformPhase);
    }
    
    /// <summary>
    /// Called by <see cref="Phase3ChallengeController"/> when all 5 big-rock allergen
    /// challenges have been completed.  Advances the game to the next appropriate phase.
    /// </summary>
    public void CompleteAllergenChallenge()
    {
        Debug.Log("Phase 3 allergen challenge completed!");
        
        int correct   = Phase3ChallengeController.Instance != null
            ? Phase3ChallengeController.Instance.GetCorrectCount() : 0;
        int total     = Phase3ChallengeController.Instance != null
            ? Phase3ChallengeController.Instance.GetCompletedCount() : 5;

        UpdateQuestText($"Allergen challenge done! {correct}/{total} correct. Head to the castle!");
        StartPhase(GamePhase.CastlePhase);
    }
    
    public void WagonHitAllergen()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.WagonHitAllergen();
        }
    }
    
    private void StartPlatformPhase()
    {
        UpdateQuestText("Land on healthy foods to build combo!");
        if (movingPlatform != null)
            movingPlatform.SetActive(true);
    }
    
    public void CompletePlatformPhase()
    {
        Debug.Log("Platform phase completed!");
        StartPhase(GamePhase.CastlePhase);
    }
    
    public void HitHealthyFood()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.HitHealthyFood();
        }
    }
    
    public void HitAllergenInPhase3()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.HitAllergenInPhase3();
        }
    }
    
    private void StartCastlePhase()
    {
        UpdateQuestText("Go to the castle and meet the queen");
    }
    
    public void ReachQueen()
    {
        Debug.Log("Reached the queen!");
        StartPhase(GamePhase.KeyPhase);
    }
    
    private void StartKeyPhase()
    {
        UpdateQuestText("Get the key from the queen");
    }
    
    public void ReceiveKey()
    {
        hasKey = true;
        StartPhase(GamePhase.EndGame);
    }
    
    private void StartEndGame()
    {
        UpdateQuestText("Return to the entrance with the key");
        
        // Notify that all phases are complete
        completedAllPhases = true;
    }
    
    #endregion
    
    #region Game Completion
    
    private bool completedAllPhases = false;
    
    public void CompleteGame()
    {
        if (isGameComplete) return;
        
        isGameComplete = true;
        UpdateQuestText("Mission Complete!");
        Debug.Log("Game Complete!");
        
        // Stop the timer
        if (gameTimer != null)
        {
            gameTimer.StopTimer();
            OnGameTimerStopped();
        }
        
        // Trigger game end manager for win
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4Complete();
        }
        else
        {
            Debug.LogWarning("GameEndManager not found, showing fallback score screen");
            ShowFinalScore();
        }
        
        OnGameCompleted?.Invoke();
        
        // Save completion stats
        SaveCompletionStats();
    }
    
    // Trigger game over (lose condition)
    public void TriggerGameOver()
    {
        if (isGameComplete) return;
        
        Debug.Log("Game Over triggered");
        
        // Stop the timer
        if (gameTimer != null)
        {
            gameTimer.StopTimer();
            OnGameTimerStopped();
        }
        
        // Trigger game end manager for lose
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4GameOver();
        }
        
        OnGameOver?.Invoke();
        
        // Save game over stats
        SaveGameOverStats();
    }
    
    // Trigger game over by time
    private void TriggerGameOverByTime()
    {
        if (isGameComplete) return;
        
        Debug.Log("Game Over - Time's up!");
        UpdateQuestText("Time's up! Game Over.");
        
        TriggerGameOver();
    }
    
    // Check for health-based game over
    private void CheckHealthBasedGameOver()
    {
        if (!isGameStarted || isGameComplete) return;
        
        if (PlayerHealthManager.Instance != null && PlayerHealthManager.Instance.currentHealth <= 0)
        {
            Debug.Log("Game Over - No health remaining!");
            UpdateQuestText("No health remaining! Game Over.");
            TriggerGameOver();
        }
    }
    
    private void SaveCompletionStats()
    {
        float completionTime = GetElapsedTime();
        int allergens = collectedAllergens.Count;
        int finalScore = Kingdom4ScoreManager.Instance != null ? Kingdom4ScoreManager.Instance.GetFinalScore() : 0;
        
        PlayerPrefs.SetFloat("LastCompletionTime", completionTime);
        PlayerPrefs.SetInt("LastAllergensCollected", allergens);
        PlayerPrefs.SetInt("LastFinalScore", finalScore);
        PlayerPrefs.SetInt("GamesCompleted", PlayerPrefs.GetInt("GamesCompleted", 0) + 1);
        PlayerPrefs.Save();
        
        Debug.Log($"Game stats saved: Time={completionTime:F1}s, Allergens={allergens}, Score={finalScore}");
    }
    
    private void SaveGameOverStats()
    {
        float elapsedTime = GetElapsedTime();
        int allergens = collectedAllergens.Count;
        int finalScore = Kingdom4ScoreManager.Instance != null ? Kingdom4ScoreManager.Instance.GetFinalScore() : 0;
        
        PlayerPrefs.SetFloat("LastGameOverTime", elapsedTime);
        PlayerPrefs.SetInt("LastGameOverAllergens", allergens);
        PlayerPrefs.SetInt("LastGameOverScore", finalScore);
        PlayerPrefs.SetInt("GamesFailed", PlayerPrefs.GetInt("GamesFailed", 0) + 1);
        PlayerPrefs.Save();
        
        Debug.Log($"Game over stats saved: Time={elapsedTime:F1}s, Allergens={allergens}, Score={finalScore}");
    }
    
    private void ShowFinalScore()
    {
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
            
            if (finalScoreText != null && Kingdom4ScoreManager.Instance != null)
            {
                int finalScore = Kingdom4ScoreManager.Instance.GetFinalScore();
                float elapsedTime = GetElapsedTime();
                int starRating = GetCurrentStarRating();
                
                finalScoreText.text = $"FINAL SCORE: {finalScore}\n" +
                                      $"Time: {elapsedTime:F1}s\n" +
                                      $"Star Rating: {starRating}/3\n" +
                                      $"Allergens: {collectedAllergens.Count}/9";
                
                Debug.Log($"Final Score Breakdown:");
                Debug.Log($"- Allergens Found: {Kingdom4ScoreManager.Instance.allergensFound}");
                Debug.Log($"- Wagon Hits: {Kingdom4ScoreManager.Instance.totalWagonHits}");
                Debug.Log($"- Max Combo: {Kingdom4ScoreManager.Instance.maxComboAchieved}");
                Debug.Log($"- Time: {elapsedTime:F1}s");
                Debug.Log($"- Star Rating: {starRating}/3");
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    public bool IsCurrentPhase(GamePhase phase)
    {
        return currentPhase == phase;
    }
    
    public void ResetGame()
    {
        hasScroll = false;
        collectedAllergens.Clear();
        hasKey = false;
        isGameStarted = false;
        isTimerRunning = false;
        isGameComplete = false;
        completedAllPhases = false;
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.ResetScore();
        }
        
        // Reset timer if exists
        if (gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }
        
        // Reset health if exists
        if (PlayerHealthManager.Instance != null)
        {
            PlayerHealthManager.Instance.ResetHealth();
        }
        
        UpdateScoreDisplay();
        StartPhase(GamePhase.ScrollQuest);
        
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(false);
        }
        
        Debug.Log("Game reset to initial state");
    }
    
    // Pause game (for pause menu)
    public void PauseGame()
    {
        Time.timeScale = 0f;
        if (gameTimer != null)
        {
            gameTimer.PauseTimer();
        }
        Debug.Log("Game paused");
    }
    
    // Resume game
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (gameTimer != null && isTimerRunning)
        {
            gameTimer.ResumeTimer();
        }
        Debug.Log("Game resumed");
    }
    
    // Get game stats for display
    public string GetGameStats()
    {
        float elapsedTime = GetElapsedTime();
        int allergens = collectedAllergens.Count;
        int score = Kingdom4ScoreManager.Instance != null ? Kingdom4ScoreManager.Instance.GetFinalScore() : 0;
        int starRating = GetCurrentStarRating();
        
        return $"Time: {FormatTime(elapsedTime)}\n" +
               $"Allergens: {allergens}/9\n" +
               $"Score: {score}\n" +
               $"Star Rating: {starRating}/3\n" +
               $"Phase: {currentPhase}";
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes:00}:{seconds:00}";
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.OnScoreChanged.RemoveListener(OnScoreChanged);
            Kingdom4ScoreManager.Instance.OnMultiplierChanged.RemoveListener(OnMultiplierChanged);
        }
    }
    
    #region Editor Helper Methods
    
    [ContextMenu("Test Game Start")]
    public void TestGameStart()
    {
        OnGameTimerStarted();
    }
    
    [ContextMenu("Test Game Complete")]
    public void TestGameComplete()
    {
        CompleteGame();
    }
    
    [ContextMenu("Test Game Over")]
    public void TestGameOver()
    {
        TriggerGameOver();
    }
    
    [ContextMenu("Check Game State")]
    public void CheckGameState()
    {
        Debug.Log($"Game State: Started={isGameStarted}, TimerRunning={isTimerRunning}, Complete={isGameComplete}, Phase={currentPhase}");
        if (gameTimer != null)
        {
            Debug.Log($"Timer: Elapsed={gameTimer.ElapsedTime:F1}s, Max={gameTimer.MaxGameTime:F1}s, OverMax={gameTimer.IsOverMaxTime}");
            Debug.Log($"Star Rating: {gameTimer.CurrentStarRatingText}");
        }
        Debug.Log($"Allergens: {collectedAllergens.Count}/9, HasScroll={hasScroll}, HasKey={hasKey}");
    }
    
    [ContextMenu("Force All Phases")]
    public void ForceAllPhases()
    {
        hasScroll = true;
        for (int i = 1; i <= 9; i++)
        {
            collectedAllergens.Add($"allergen_{i}");
        }
        hasKey = true;
        StartPhase(GamePhase.EndGame);
        Debug.Log("Forced all phases to complete");
    }
    
    [ContextMenu("Reset Game State")]
    public void ResetGameState()
    {
        ResetGame();
    }
    
    [ContextMenu("Manually Start Timer")]
    public void ManuallyStartTimer()
    {
        if (gameTimer != null && !gameTimer.IsActive)
        {
            gameTimer.StartTimer();
            OnGameTimerStarted();
        }
    }
    
    #endregion
}