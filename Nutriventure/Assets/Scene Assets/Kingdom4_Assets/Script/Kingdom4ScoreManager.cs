using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;

public class Kingdom4ScoreManager : MonoBehaviour
{
    // UnityEvents for Inspector assignment
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnMultiplierChanged;
    public UnityEvent<int> OnMaxComboChanged;
    public UnityEvent<int> OnAllergensFoundChanged;
    public UnityEvent<int> OnWagonHitsChanged;
    public UnityEvent OnGameStartedEvent; // Renamed to avoid conflict

    // Regular C# events for code subscriptions
    public event Action<int, int, int> OnScoreUpdated; // score, allergens, wagonHits

    public static Kingdom4ScoreManager Instance;

    [Header("Phase 1: Allergen Hunt")]
    [Tooltip("Points for collecting each allergen")]
    public int pointsPerAllergen = 200;
    
    [Tooltip("Bonus for collecting all 9 allergens")]
    public int allAllergensBonus = 500;

    [Header("Phase 2: Wagon Phase")]
    [Tooltip("Penalty for each allergen hit with the wagon")]
    public int wagonHitPenalty = 75;
    
    [Tooltip("Bonus for no wagon hits")]
    public int noWagonHitsBonus = 300;

    [Header("Phase 3: Platform Phase - Combo System")]
    [Tooltip("Base points for landing on healthy food")]
    public int healthyFoodBasePoints = 100;
    
    [Tooltip("Maximum combo multiplier")]
    public int maxCombo = 8;
    
    [Tooltip("Bonus for max combo achievement")]
    public int maxComboBonus = 400;

    [Header("Time-Based Scoring")]
    [Tooltip("Maximum time allowed (20 minutes)")]
    public float maxGameTime = 1200f; // 20 minutes
    
    [Tooltip("Time thresholds for star ratings (in seconds)")]
    public float threeStarTime = 600f;    // 10 minutes
    public float twoStarTime = 900f;      // 15 minutes
    public float oneStarTime = 1200f;     // 20 minutes
    
    [Tooltip("Time bonuses based on completion time")]
    public int threeStarTimeBonus = 1000;  // Under 10 min
    public int twoStarTimeBonus = 600;     // 10-15 min
    public int oneStarTimeBonus = 300;     // 15-20 min
    public int noTimeBonus = 100;          // Over 20 min

    [Header("UI Display References")]
    [Tooltip("Drag your total score TextMeshPro UI here")]
    public TMP_Text scoreText;
    
    [Tooltip("Drag your combo multiplier TextMeshPro UI here")]
    public TMP_Text multiplierText;
    
    [Tooltip("Drag your allergen count TextMeshPro UI here")]
    public TMP_Text allergenCountText;
    
    [Tooltip("Drag your wagon hits TextMeshPro UI here")]
    public TMP_Text wagonHitsText;
    
    [Tooltip("Drag your time display TextMeshPro UI here (optional)")]
    public TMP_Text timeText;
    
    [Tooltip("Drag your star rating display TextMeshPro UI here (optional)")]
    public TMP_Text starRatingText;

    // Score tracking
    public int allergensFound = 0;
    public int totalWagonHits = 0;
    public int comboMultiplier = 1;
    public int maxComboAchieved = 1; // Added for star rating system
    public int starRating = 0; // Current star rating (0-3)
    
    private int totalScore = 0;
    public float timeBonus = 0f;
    private bool timeBonusApplied = false;
    private bool allAllergensBonusApplied = false;
    private bool noWagonHitsBonusApplied = false;
    private bool maxComboBonusApplied = false;
    
    // Timer reference
    private GameTimer gameTimer;
    private float gameStartTime;
    private bool isGameStarted = false;

    private void Awake()
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
        // Find timer reference
        gameTimer = GameTimer.Instance;
        if (gameTimer == null)
        {
            gameTimer = FindObjectOfType<GameTimer>();
        }
        
        // Subscribe to timer events if available
        if (gameTimer != null)
        {
            gameTimer.OnTimeUpdated.AddListener(OnTimeUpdated);
            Debug.Log("Connected to GameTimer for scoring");
        }
        
        ResetScore();
        UpdateAllUI();
    }
    
    void Update()
    {
        // Update time display if available
        if (timeText != null && gameTimer != null && gameTimer.IsActive)
        {
            UpdateTimeDisplay();
        }
        
        // Update star rating display
        if (starRatingText != null && gameTimer != null)
        {
            UpdateStarRatingDisplay();
        }
    }
    
    // Called when timer updates
    private void OnTimeUpdated(float elapsedTime)
    {
        // Calculate star rating based on current time
        UpdateStarRating();
        
        // Update time display
        UpdateTimeDisplay();
    }
    
    // Called when game officially starts (from WardenInteraction)
    public void OnGameStarted()
    {
        isGameStarted = true;
        gameStartTime = Time.time;
        Debug.Log("ScoreManager: Game started notification received");
        OnGameStartedEvent?.Invoke(); // Use the renamed UnityEvent
    }

    // ---------------- PHASE 1: Allergen Hunt ----------------
    public void AddAllergenFound()
    {
        allergensFound++;
        totalScore += pointsPerAllergen;
        
        // Check for all allergens bonus
        if (allergensFound >= 9 && !allAllergensBonusApplied)
        {
            totalScore += allAllergensBonus;
            allAllergensBonusApplied = true;
            Debug.Log($"All allergens collected! +{allAllergensBonus} bonus");
        }
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        OnAllergensFoundChanged?.Invoke(allergensFound);
        OnScoreUpdated?.Invoke(totalScore, allergensFound, totalWagonHits);
        
        Debug.Log($"Allergen collected! Total: {allergensFound}/9, Score: {totalScore}");
    }

    // ---------------- PHASE 2: Wagon Phase ----------------
    public void WagonHitAllergen()
    {
        totalWagonHits++;
        totalScore -= wagonHitPenalty;
        totalScore = Mathf.Max(0, totalScore);
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        OnWagonHitsChanged?.Invoke(totalWagonHits);
        OnScoreUpdated?.Invoke(totalScore, allergensFound, totalWagonHits);
        
        Debug.Log($"Wagon hit allergen! Hits: {totalWagonHits}, Penalty: -{wagonHitPenalty}, Score: {totalScore}");
    }
    
    // Check for no wagon hits bonus (call this at game end)
    private void CheckNoWagonHitsBonus()
    {
        if (totalWagonHits == 0 && !noWagonHitsBonusApplied)
        {
            totalScore += noWagonHitsBonus;
            noWagonHitsBonusApplied = true;
            Debug.Log($"No wagon hits! +{noWagonHitsBonus} bonus");
        }
    }

    // ---------------- PHASE 3: Platform Phase - Combo System ----------------
    public void HitHealthyFood()
    {
        comboMultiplier = Mathf.Clamp(comboMultiplier + 1, 1, maxCombo);
        
        // Track max combo achieved
        if (comboMultiplier > maxComboAchieved)
        {
            maxComboAchieved = comboMultiplier;
            OnMaxComboChanged?.Invoke(maxComboAchieved);
        }
        
        int gained = healthyFoodBasePoints * comboMultiplier;
        totalScore += gained;
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        OnMultiplierChanged?.Invoke(comboMultiplier);
        OnScoreUpdated?.Invoke(totalScore, allergensFound, totalWagonHits);
        
        Debug.Log($"Healthy food hit! Combo: x{comboMultiplier}, Gained: {gained}, Score: {totalScore}");
    }

    public void HitAllergenInPhase3()
    {
        comboMultiplier = 1;
        
        UpdateAllUI();
        OnMultiplierChanged?.Invoke(comboMultiplier);
        
        Debug.Log("Combo reset! Hit allergen in platform phase");
    }
    
    // Check for max combo bonus (call this at game end)
    private void CheckMaxComboBonus()
    {
        if (maxComboAchieved >= maxCombo && !maxComboBonusApplied)
        {
            totalScore += maxComboBonus;
            maxComboBonusApplied = true;
            Debug.Log($"Max combo achieved! +{maxComboBonus} bonus");
        }
        else if (maxComboAchieved >= maxCombo / 2 && !maxComboBonusApplied)
        {
            // Half combo bonus
            totalScore += maxComboBonus / 2;
            maxComboBonusApplied = true;
            Debug.Log($"Good combo achieved! +{maxComboBonus / 2} bonus");
        }
    }

    // ---------------- TIME BONUS ----------------
    public void CalculateTimeBonus(float completionTime)
    {
        if (timeBonusApplied) return;
        
        // Get time bonus based on completion time
        if (completionTime <= threeStarTime)
        {
            timeBonus = threeStarTimeBonus;
            starRating = 3;
        }
        else if (completionTime <= twoStarTime)
        {
            timeBonus = twoStarTimeBonus;
            starRating = 2;
        }
        else if (completionTime <= oneStarTime)
        {
            timeBonus = oneStarTimeBonus;
            starRating = 1;
        }
        else
        {
            timeBonus = noTimeBonus;
            starRating = 0;
        }

        totalScore += Mathf.RoundToInt(timeBonus);
        timeBonusApplied = true;
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        
        Debug.Log($"Time bonus calculated: +{timeBonus} (Time: {completionTime:F1}s, Stars: {starRating})");
    }
    
    // Calculate score based on elapsed time (for real-time display)
    public void CalculateTimeBasedScore(float elapsedTime)
    {
        // Calculate base time bonus (this is for display only, final bonus calculated at end)
        float timeRemainingPercentage = 1f - (elapsedTime / maxGameTime);
        int displayTimeBonus = Mathf.RoundToInt(timeRemainingPercentage * 1000);
        
        // Update UI with projected time bonus
        if (timeText != null && !timeBonusApplied)
        {
            timeText.text = $"Time: {FormatTime(elapsedTime)} | Bonus: ~{displayTimeBonus}";
        }
    }
    
    // Calculate final score with all bonuses
    public int CalculateFinalScore(float completionTime, int remainingHearts)
    {
        // Apply all bonuses
        CheckNoWagonHitsBonus();
        CheckMaxComboBonus();
        CalculateTimeBonus(completionTime);
        
        // Heart bonus
        int heartBonus = (remainingHearts - 1) * 100;
        totalScore += heartBonus;
        Debug.Log($"Heart bonus: +{heartBonus} ({remainingHearts} hearts)");
        
        // Score multiplier bonus (10% of current score)
        int scoreMultiplierBonus = Mathf.FloorToInt(totalScore * 0.1f);
        totalScore += scoreMultiplierBonus;
        Debug.Log($"Score multiplier bonus: +{scoreMultiplierBonus} (10% of score)");
        
        return totalScore;
    }
    
    // Update star rating based on current time
    private void UpdateStarRating()
    {
        if (gameTimer == null) return;
        
        float elapsedTime = gameTimer.ElapsedTime;
        int newStarRating = gameTimer.CurrentStarRating;
        
        if (newStarRating != starRating)
        {
            starRating = newStarRating;
            Debug.Log($"Star rating updated: {starRating}/3 (Time: {elapsedTime:F1}s)");
        }
    }
    
    // Get current star rating
    public int GetCurrentStarRating()
    {
        if (gameTimer != null)
        {
            return gameTimer.CurrentStarRating;
        }
        return starRating;
    }

    // ---------------- UI UPDATES ----------------
    private void UpdateAllUI()
    {
        UpdateScoreText();
        UpdateMultiplierText();
        UpdateAllergenCountText();
        UpdateWagonHitsText();
        UpdateTimeDisplay();
        UpdateStarRatingDisplay();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {totalScore}";
        }
    }

    private void UpdateMultiplierText()
    {
        if (multiplierText != null)
        {
            if (comboMultiplier > 1)
            {
                multiplierText.text = $"x{comboMultiplier} COMBO!";
                multiplierText.color = Color.Lerp(Color.yellow, Color.red, (float)comboMultiplier / maxCombo);
            }
            else
            {
                multiplierText.text = "";
            }
        }
    }

    private void UpdateAllergenCountText()
    {
        if (allergenCountText != null)
        {
            allergenCountText.text = $"Allergens: {allergensFound}/9";
            allergenCountText.color = allergensFound >= 9 ? Color.green : Color.white;
        }
    }

    private void UpdateWagonHitsText()
    {
        if (wagonHitsText != null)
        {
            wagonHitsText.text = $"Wagon Hits: {totalWagonHits}";
            wagonHitsText.color = totalWagonHits == 0 ? Color.green : 
                                 totalWagonHits <= 2 ? Color.yellow : Color.red;
        }
    }
    
    private void UpdateTimeDisplay()
    {
        if (timeText != null && gameTimer != null)
        {
            float elapsedTime = gameTimer.ElapsedTime;
            timeText.text = $"Time: {FormatTime(elapsedTime)}";
            
            // Color code based on star rating time
            if (elapsedTime <= threeStarTime)
            {
                timeText.color = Color.green;
            }
            else if (elapsedTime <= twoStarTime)
            {
                timeText.color = Color.yellow;
            }
            else if (elapsedTime <= oneStarTime)
            {
                timeText.color = Color.red;
            }
            else
            {
                timeText.color = Color.gray;
            }
        }
    }
    
    private void UpdateStarRatingDisplay()
    {
        if (starRatingText != null)
        {
            int currentStars = GetCurrentStarRating();
            string stars = "";
            for (int i = 0; i < 3; i++)
            {
                stars += i < currentStars ? "★" : "☆";
            }
            starRatingText.text = $"Stars: {stars}";
            
            // Color based on star count
            starRatingText.color = currentStars == 3 ? Color.yellow :
                                   currentStars == 2 ? Color.white :
                                   currentStars == 1 ? Color.gray : Color.red;
        }
    }
    
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    // ---------------- PUBLIC METHODS ----------------
    
    // ADD THIS METHOD - Used by BigRockInteraction for penalty
    public void AddScore(int amount)
    {
        totalScore += amount;
        totalScore = Mathf.Max(0, totalScore); // Ensure score doesn't go negative
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        OnScoreUpdated?.Invoke(totalScore, allergensFound, totalWagonHits);
        
        Debug.Log($"Score adjusted by {amount}. New score: {totalScore}");
    }
    
    public void ResetScore()
    {
        allergensFound = 0;
        totalWagonHits = 0;
        comboMultiplier = 1;
        maxComboAchieved = 1;
        starRating = 0;
        totalScore = 0;
        timeBonusApplied = false;
        allAllergensBonusApplied = false;
        noWagonHitsBonusApplied = false;
        maxComboBonusApplied = false;
        isGameStarted = false;
        
        UpdateAllUI();
        OnMultiplierChanged?.Invoke(comboMultiplier);
        OnScoreChanged?.Invoke(totalScore);
        OnAllergensFoundChanged?.Invoke(allergensFound);
        OnWagonHitsChanged?.Invoke(totalWagonHits);
        OnMaxComboChanged?.Invoke(maxComboAchieved);
        
        Debug.Log("Score reset to initial state");
    }

    public int GetFinalScore()
    {
        return totalScore;
    }

    public int GetMaxComboAchieved()
    {
        return maxComboAchieved;
    }
    
    public int GetStarRating()
    {
        return starRating;
    }
    
    public string GetScoreBreakdown()
    {
        return $"Total Score: {totalScore}\n" +
               $"Allergens: {allergensFound}/9 (+{allergensFound * pointsPerAllergen})\n" +
               $"Wagon Hits: {totalWagonHits} (-{totalWagonHits * wagonHitPenalty})\n" +
               $"Max Combo: x{maxComboAchieved}\n" +
               $"Time Bonus: +{timeBonus}\n" +
               $"Star Rating: {starRating}/3";
    }
    
    public ScoreData GetScoreData()
    {
        return new ScoreData
        {
            totalScore = totalScore,
            allergensFound = allergensFound,
            totalWagonHits = totalWagonHits,
            maxComboAchieved = maxComboAchieved,
            starRating = starRating,
            timeBonus = timeBonus
        };
    }
    
    [System.Serializable]
    public struct ScoreData
    {
        public int totalScore;
        public int allergensFound;
        public int totalWagonHits;
        public int maxComboAchieved;
        public int starRating;
        public float timeBonus;
        
        public override string ToString()
        {
            return $"Score: {totalScore}, Allergens: {allergensFound}/9, Wagon Hits: {totalWagonHits}, Max Combo: x{maxComboAchieved}, Stars: {starRating}/3, Time Bonus: {timeBonus}";
        }
    }

    // Helper method for AllerthriaGameManager
    public void UpdateAllUIManually()
    {
        UpdateAllUI();
    }
    
    // Debug method to test scoring
    [ContextMenu("Test Add Allergen")]
    public void TestAddAllergen()
    {
        AddAllergenFound();
    }
    
    [ContextMenu("Test Wagon Hit")]
    public void TestWagonHit()
    {
        WagonHitAllergen();
    }
    
    [ContextMenu("Test Healthy Food Hit")]
    public void TestHealthyFoodHit()
    {
        HitHealthyFood();
    }
    
    [ContextMenu("Test Add Score +50")]
    public void TestAddScore()
    {
        AddScore(50);
    }
    
    [ContextMenu("Test Add Score -50")]
    public void TestSubtractScore()
    {
        AddScore(-50);
    }
    
    [ContextMenu("Check Score State")]
    public void CheckScoreState()
    {
        Debug.Log(GetScoreBreakdown());
    }
    
    [ContextMenu("Calculate Test Final Score")]
    public void CalculateTestFinalScore()
    {
        float testTime = 550f; // 9:10
        int testHearts = 4;
        int finalScore = CalculateFinalScore(testTime, testHearts);
        Debug.Log($"Test Final Score: {finalScore}");
        Debug.Log(GetScoreBreakdown());
    }
}