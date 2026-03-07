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
    public bool isGameStarted = false;
    public bool isTimerRunning = false;
    public bool isGameComplete = false;
    
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
    [SerializeField] private GameTimer gameTimer;
    [SerializeField] private Kingdom4GameEndManager gameEndManager;
    
    [Header("Rock System")]
    [SerializeField] private int rocksCompleted = 0;
    [SerializeField] private bool[] rocksStatus = new bool[5];
    [SerializeField] private TextMeshProUGUI warningMessageText;
    [SerializeField] private float messageDisplayTime = 2f;
    [SerializeField] private AudioClip rockCompleteSound;
    [SerializeField] private AudioClip wrongPathSound;
    [SerializeField] private AudioClip successSound;
    
    // Events
    public event Action<GamePhase> OnPhaseChanged;
    public event Action OnGameStarted;
    public event Action OnGameCompleted;
    public event Action OnGameOver;
    
    private bool completedAllPhases = false;
    private AudioSource audioSource;
    
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
        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
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
        
        Debug.Log("AllerthriaGameManager initialized");
    }
    
    void Update()
    {
        UpdatePhaseSpecificDisplay();
        
        if (isGameStarted && !isGameComplete && gameTimer != null && gameTimer.IsOverMaxTime)
        {
            TriggerGameOverByTime();
        }
        
        CheckHealthBasedGameOver();
    }
    
    #region Rock System Methods
    
    public void OnRockActivated(int rockID, string allergen)
    {
        Debug.Log($"Rock {rockID} activated with allergen: {allergen}");
        ShowMessage($"Warning: {allergen} detected in this area!");
    }
    
    public bool IsRockCompleted(int rockID)
    {
        if (rockID >= 1 && rockID <= 5)
        {
            return rocksStatus[rockID - 1];
        }
        return false;
    }
    
    public void MarkRockCompleted(int rockID)
    {
        if (rockID >= 1 && rockID <= 5 && !rocksStatus[rockID - 1])
        {
            rocksStatus[rockID - 1] = true;
            rocksCompleted++;
            
            if (rockCompleteSound != null && audioSource != null)
                audioSource.PlayOneShot(rockCompleteSound);
            
            Debug.Log($"Rock {rockID} completed. Total: {rocksCompleted}/5");
            
            if (allergenCountText != null)
            {
                allergenCountText.text = $"Allergens: {collectedAllergens.Count}/9";
            }
            
            if (rocksCompleted >= 5)
            {
                ShowMessage("All rocks navigated! Find 4 more allergens to continue!");
            }
            
            if (collectedAllergens.Count >= 9)
            {
                StartPhase(GamePhase.WagonPhase);
            }
        }
    }
    
    public void ShowMessage(string message)
    {
        if (warningMessageText != null)
        {
            StopAllCoroutines();
            StartCoroutine(DisplayMessage(message, Color.white));
        }
    }
    
    public void ShowWarningMessage(string message)
    {
        if (warningMessageText != null)
        {
            if (wrongPathSound != null && audioSource != null)
                audioSource.PlayOneShot(wrongPathSound);
                
            StopAllCoroutines();
            StartCoroutine(DisplayMessage(message, Color.red));
        }
    }
    
    // ADD THIS MISSING METHOD
    public void ShowSuccessMessage(string message)
    {
        if (warningMessageText != null)
        {
            if (successSound != null && audioSource != null)
                audioSource.PlayOneShot(successSound);
                
            StopAllCoroutines();
            StartCoroutine(DisplayMessage(message, Color.green));
        }
    }
    
    private System.Collections.IEnumerator DisplayMessage(string message, Color color)
    {
        warningMessageText.color = color;
        warningMessageText.text = message;
        warningMessageText.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(messageDisplayTime);
        
        warningMessageText.gameObject.SetActive(false);
    }
    
    #endregion
    
    #region Game State Management
    
    public void OnGameTimerStarted()
    {
        isGameStarted = true;
        isTimerRunning = true;
        Debug.Log("Game officially started - timer is running");
        
        UpdateQuestText("Game started! Timer is running.");
        OnGameStarted?.Invoke();
        
        var warden = FindObjectOfType<WardenInteraction>();
        if (warden != null && warden.DoesStartTimer())
        {
            Debug.Log("First warden interaction completed, game timer started");
        }
    }
    
    public void OnGameTimerStopped()
    {
        isTimerRunning = false;
        Debug.Log("Game timer stopped");
    }
    
    public float GetElapsedTime()
    {
        if (gameTimer != null)
        {
            return gameTimer.ElapsedTime;
        }
        return 0f;
    }
    
    public float GetRemainingTime()
    {
        if (gameTimer != null)
        {
            return Mathf.Max(0f, gameTimer.MaxGameTime - gameTimer.ElapsedTime);
        }
        return 0f;
    }
    
    public bool AllPhasesCompleted()
    {
        return currentPhase == GamePhase.EndGame;
    }
    
    public float GetCurrentLifeAmount()
    {
        if (PlayerHealthManager.Instance != null)
        {
            return PlayerHealthManager.Instance.currentHealth;
        }
        return 3f;
    }
    
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
                if (allergenCountText != null)
                {
                    allergenCountText.text = $"Allergens: {collectedAllergens.Count}/9 | Rocks: {rocksCompleted}/5";
                }
                break;
                
            case GamePhase.WagonPhase:
                if (wagonHitsText != null && Kingdom4ScoreManager.Instance != null)
                {
                    wagonHitsText.text = $"Wagon Hits: {Kingdom4ScoreManager.Instance.totalWagonHits}";
                }
                break;
        }
        
        if (gameTimer != null && questText != null)
        {
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
        UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9 | Rocks: {rocksCompleted}/5");
        
        AllergenSpawnManager spawner = FindObjectOfType<AllergenSpawnManager>();
        if (spawner != null)
        {
            spawner.SpawnNow();
        }
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
            
            ShowSuccessMessage($"Collected: {allergenId}!");
            UpdateQuestText($"Find allergens: {collectedAllergens.Count}/9 | Rocks: {rocksCompleted}/5");
            
            if (collectedAllergens.Count >= 9)
            {
                StartPhase(GamePhase.WagonPhase);
            }
            
            if (gameTimer != null && questText != null)
            {
                questText.text = $"Allergens: {collectedAllergens.Count}/9 | Rocks: {rocksCompleted}/5 | Time: {gameTimer.GetElapsedTimeFormatted()}";
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
        completedAllPhases = true;
    }
    
    #endregion
    
    #region Game Completion
    
    public void CompleteGame()
    {
        if (isGameComplete) return;
        
        isGameComplete = true;
        UpdateQuestText("Mission Complete!");
        Debug.Log("Game Complete!");
        
        if (gameTimer != null)
        {
            gameTimer.StopTimer();
            OnGameTimerStopped();
        }
        
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
        SaveCompletionStats();
    }
    
    public void TriggerGameOver()
    {
        if (isGameComplete) return;
        
        Debug.Log("Game Over triggered");
        
        if (gameTimer != null)
        {
            gameTimer.StopTimer();
            OnGameTimerStopped();
        }
        
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4GameOver();
        }
        
        OnGameOver?.Invoke();
        SaveGameOverStats();
    }
    
    private void TriggerGameOverByTime()
    {
        if (isGameComplete) return;
        
        Debug.Log("Game Over - Time's up!");
        UpdateQuestText("Time's up! Game Over.");
        TriggerGameOver();
    }
    
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
                                      $"Allergens: {collectedAllergens.Count}/9\n" +
                                      $"Rocks Completed: {rocksCompleted}/5";
                
                Debug.Log($"Final Score Breakdown:");
                Debug.Log($"- Allergens Found: {Kingdom4ScoreManager.Instance.allergensFound}");
                Debug.Log($"- Rocks Completed: {rocksCompleted}/5");
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
        rocksCompleted = 0;
        
        for (int i = 0; i < rocksStatus.Length; i++)
        {
            rocksStatus[i] = false;
        }
        
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.ResetScore();
        }
        
        if (gameTimer != null)
        {
            gameTimer.ResetTimer(false);
        }
        
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
    
    public void PauseGame()
    {
        Time.timeScale = 0f;
        if (gameTimer != null)
        {
            gameTimer.PauseTimer();
        }
        Debug.Log("Game paused");
    }
    
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (gameTimer != null && isTimerRunning)
        {
            gameTimer.ResumeTimer();
        }
        Debug.Log("Game resumed");
    }
    
    public string GetGameStats()
    {
        float elapsedTime = GetElapsedTime();
        int allergens = collectedAllergens.Count;
        int score = Kingdom4ScoreManager.Instance != null ? Kingdom4ScoreManager.Instance.GetFinalScore() : 0;
        int starRating = GetCurrentStarRating();
        
        return $"Time: {FormatTime(elapsedTime)}\n" +
               $"Allergens: {allergens}/9\n" +
               $"Rocks: {rocksCompleted}/5\n" +
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
        Debug.Log($"Allergens: {collectedAllergens.Count}/9, Rocks: {rocksCompleted}/5, HasScroll={hasScroll}, HasKey={hasKey}");
    }
    
    [ContextMenu("Force All Phases")]
    public void ForceAllPhases()
    {
        hasScroll = true;
        for (int i = 1; i <= 9; i++)
        {
            collectedAllergens.Add($"allergen_{i}");
        }
        rocksCompleted = 5;
        for (int i = 0; i < rocksStatus.Length; i++)
        {
            rocksStatus[i] = true;
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
    
    [ContextMenu("Complete 1 Rock")]
    public void TestCompleteOneRock()
    {
        for (int i = 1; i <= 5; i++)
        {
            if (!IsRockCompleted(i))
            {
                MarkRockCompleted(i);
                return;
            }
        }
        Debug.Log("All rocks already completed!");
    }
    
    #endregion
}