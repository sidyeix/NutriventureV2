using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using System.Collections;

public class PreserviaScoringSystem : MonoBehaviour
{
    // Singleton pattern
    public static PreserviaScoringSystem Instance { get; private set; }

    private int _totalNegativeScore = 0;
    private Dictionary<int, bool> _foodMistakeMade = new Dictionary<int, bool>();

    [Header("Scoring Configuration")]
    [Tooltip("Base points for collecting a preservative potion")]
    public int basePreservativePoints = 100;
    
    [Tooltip("Base points for completing GEM interaction")]
    public int gemCompletionPoints = 500;
    
    [Tooltip("Points for each correct food preservation")]
    public int foodPreservationPoints = 300;
    
    [Tooltip("Bonus for completing all foods")]
    public int fullCompletionBonus = 1500;
    
    [Tooltip("Bonus for perfect slider placement (within optimal range)")]
    public int perfectPlacementBonus = 200;

    [Header("Progressive Multiplier Settings")]
    [Tooltip("Multiplier increase per successful preservative application")]
    public float multiplierIncrement = 0.25f;
    
    [Tooltip("Maximum achievable multiplier")]
    public float maxMultiplier = 3.0f;
    
    [Tooltip("Starting multiplier value")]
    public float startingMultiplier = 1.0f;

    [Header("Bonus Settings")]
    [Tooltip("Bonus for consecutive preservative collections")]
    public int comboBonus = 50;
    
    [Tooltip("Time window for combos (seconds)")]
    public float comboTimeWindow = 15f;
    
    [Tooltip("Maximum combo multiplier")]
    public int maxComboMultiplier = 5;
    
    [Tooltip("Anti-Oxidant GEM bonus")]
    public int antiOxidantBonus = 75;
    
    [Tooltip("Anti-Microbe GEM bonus")]
    public int antiMicrobeBonus = 75;
    
    [Tooltip("Bonus for using correct preservative type")]
    public int correctTypeBonus = 100;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnPreservativeCollected;
    public UnityEvent<int> OnGEMCompleted;
    public UnityEvent<int> OnFoodPreserved;
    public UnityEvent<float> OnMultiplierChanged;
    public UnityEvent<string> OnBonusEarned;
    public UnityEvent OnMultiplierReset;
    public UnityEvent OnMultiplierIncreased;

    [Header("Score Display")]
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public TMP_Text preservativesText;
    public TMP_Text foodsPreservedText;
    public TMP_Text gemsCompletedText;

    [Header("Score Popup System")]
    public GameObject scorePopupPrefab;
    public Transform scorePopupParent;
    public Vector2 popupSpawnOffset = new Vector2(50, 50);
    public float popupLifetime = 1.5f;
    public float popupFadeDuration = 0.3f;
    public float popupFloatSpeed = 50f;
    public bool enableScorePopups = true;
    public float popupSpacing = 30f;
    
    [Header("Popup Colors")]
    public Color preservativeScoreColor = new Color(0.2f, 0.8f, 0.2f); // Green
    public Color gemScoreColor = new Color(1f, 0.8f, 0f); // Gold
    public Color foodScoreColor = new Color(0.2f, 0.6f, 1f); // Blue
    public Color bonusScoreColor = new Color(1f, 0.5f, 0f); // Orange
    public Color comboScoreColor = new Color(0.8f, 0.2f, 0.8f); // Purple
    public Color perfectScoreColor = new Color(1f, 1f, 0f); // Yellow

    // Score tracking
    private int _currentScore = 0;
    private int _preservativeScore = 0;
    private int _gemScore = 0;
    private int _foodScore = 0;
    private int _bonusScore = 0;
    
    // Progressive multiplier system
    private float _progressiveMultiplier = 1.0f;
    private int _multiplierStreak = 0;
    private bool _isMultiplierLocked = false;
    
    // Session stats
    private int _preservativesCollected = 0;
    private int _gemsCompleted = 0;
    private int _foodsPreserved = 0;
    private int _comboCount = 0;
    private float _lastCollectionTime = 0f;
    private float _sessionStartTime = 0f;
    private int _perfectPreservations = 0;
    
    // References (found dynamically)
    private K3_CollectPreservatives _collectionSystem;
    private K3_Phase1Functions _gemSystem;
    private K3_KingAssessment _assessmentSystem;
    private GameplayProgression _gameplayProgression;
    
    // State tracking
    private bool _isMonitoring = false;
    
    // GEM tracking
    private bool _oxidantGEMScored = false;
    private bool _microbeGEMScored = false;
    
    // Food tracking
    private Dictionary<int, bool> _foodPreservedThisSession = new Dictionary<int, bool>();
    
    // Queue for score popups
    private Queue<ScorePopupData> _scorePopupQueue = new Queue<ScorePopupData>();
    private bool _isProcessingPopup = false;
    
    // Track active popups for positioning
    private List<GameObject> _activePopups = new List<GameObject>();
    
    // Preservative type tracking
    private Dictionary<string, int> _preservativeTypeCount = new Dictionary<string, int>();

    #region Initialization
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeSystem();
        StartMonitoring();
        
        Debug.Log("Preservia Scoring System initialized");
    }

    void InitializeSystem()
    {
        _sessionStartTime = Time.time;
        _progressiveMultiplier = startingMultiplier;

            // Initialize mistake tracking for 8 foods
        for (int i = 0; i < 8; i++)
        {
            _foodMistakeMade[i] = false;
        }
        
        // Find all necessary components dynamically
        FindAllReferences();
        
        // Subscribe to events
        SubscribeToEvents();
        
        // Initialize displays
        UpdateMultiplierDisplay();
        UpdateScoreDisplay();
        
        // Initialize food tracking
        InitializeFoodTracking();
        
        // Validate score popup parent
        if (scorePopupParent == null && scorePopupPrefab != null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                scorePopupParent = canvas.transform;
                Debug.Log($"Auto-assigned Canvas as score popup parent: {canvas.name}");
            }
        }
    }
    
    void InitializeFoodTracking()
    {
        // Initialize tracking for up to 8 foods
        for (int i = 0; i < 8; i++)
        {
            _foodPreservedThisSession[i] = false;
        }
    }

    void FindAllReferences()
    {
        _collectionSystem = FindObjectOfType<K3_CollectPreservatives>();
        _gemSystem = FindObjectOfType<K3_Phase1Functions>();
        _assessmentSystem = FindObjectOfType<K3_KingAssessment>();
        _gameplayProgression = FindObjectOfType<GameplayProgression>();
        
        if (_collectionSystem != null) Debug.Log("Found collection system");
        if (_gemSystem != null) Debug.Log("Found GEM system");
        if (_assessmentSystem != null) Debug.Log("Found assessment system");
        if (_gameplayProgression != null) Debug.Log("Found gameplay progression");
    }
    
    void SubscribeToEvents()
    {
        // Subscribe to preservative collection events
        if (_collectionSystem != null)
        {
            _collectionSystem.OnPotionCollected += HandlePreservativeCollected;
            Debug.Log("Subscribed to preservative collection events");
        }
    }
    
    void HandlePreservativeCollected(GameObject potion, string preservativeID)
    {
        AwardPreservativePoints(preservativeID);
    }
    #endregion

    #region Monitoring System
    void StartMonitoring()
    {
        _isMonitoring = true;
        
        // Start checking for scoring events
        InvokeRepeating("CheckForScoringEvents", 0.5f, 0.5f);
        
        Debug.Log("Scoring monitoring started");
    }

    void StopMonitoring()
    {
        _isMonitoring = false;
        CancelInvoke("CheckForScoringEvents");
    }

    void CheckForScoringEvents()
    {
        if (!_isMonitoring) return;
        
        // Monitor GEM completion
        MonitorGEMCompletion();
        
        // Monitor food preservation
        MonitorFoodPreservation();
        
        // Update display
        UpdateScoreDisplay();
        
        // Process score popup queue
        ProcessPopupQueue();
    }

    void MonitorGEMCompletion()
    {
        if (_gemSystem == null) return;
        
        try
        {
            // Use reflection to check if panels are active
            var gemType = _gemSystem.GetType();
            
            // Check Antioxidant panel
            var antioxidantPanelField = gemType.GetField("antioxidantInfo");
            if (antioxidantPanelField != null)
            {
                GameObject antioxidantPanel = antioxidantPanelField.GetValue(_gemSystem) as GameObject;
                if (antioxidantPanel != null && antioxidantPanel.activeSelf && !_oxidantGEMScored)
                {
                    // Panel is open for the first time - award points
                    AwardGEMPoints("Anti-Oxidant GEM");
                    _oxidantGEMScored = true;
                    _gemsCompleted++;
                    Debug.Log("Scored Anti-Oxidant GEM (panel opened for first time)");
                }
            }
            
            // Check Antimicrobe panel
            var antimicrobePanelField = gemType.GetField("antimicrobeInfo");
            if (antimicrobePanelField != null)
            {
                GameObject antimicrobePanel = antimicrobePanelField.GetValue(_gemSystem) as GameObject;
                if (antimicrobePanel != null && antimicrobePanel.activeSelf && !_microbeGEMScored)
                {
                    // Panel is open for the first time - award points
                    AwardGEMPoints("Anti-Microbe GEM");
                    _microbeGEMScored = true;
                    _gemsCompleted++;
                    Debug.Log("Scored Anti-Microbe GEM (panel opened for first time)");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error monitoring GEM completion: {e.Message}");
        }
    }

    void MonitorFoodPreservation()
    {
        if (_assessmentSystem == null) return;
        
        try
        {
            // Use reflection to check food completion dictionary
            var assessmentType = _assessmentSystem.GetType();
            var foodCompletedField = assessmentType.GetField("foodCompleted");
            
            if (foodCompletedField != null)
            {
                Dictionary<int, bool> foodCompleted = foodCompletedField.GetValue(_assessmentSystem) as Dictionary<int, bool>;
                if (foodCompleted != null)
                {
                    // Check each food
                    foreach (var kvp in foodCompleted)
                    {
                        int foodIndex = kvp.Key;
                        bool isCompleted = kvp.Value;
                        
                        // If food is completed AND hasn't been scored this session
                        if (isCompleted && !_foodPreservedThisSession[foodIndex])
                        {
                            // Get food name from database if possible
                            string foodName = GetFoodName(foodIndex);
                            
                            // Award points for food preservation
                            AwardFoodPreservationPoints(foodName, false, true);
                            _foodPreservedThisSession[foodIndex] = true;
                            _foodsPreserved++;
                            
                            Debug.Log($"Scored food preservation: {foodName} (Index: {foodIndex})");
                            
                            // Check for full completion bonus
                            int completedCount = foodCompleted.Count(kvp => kvp.Value);
                            if (completedCount >= 8 && !_bonusAwarded)
                            {
                                AwardFullCompletionBonus();
                                _bonusAwarded = true;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error monitoring food preservation: {e.Message}");
        }
    }
    
    // Helper method to get food name
    private string GetFoodName(int foodIndex)
    {
        if (_assessmentSystem != null)
        {
            try
            {
                var assessmentType = _assessmentSystem.GetType();
                var databaseField = assessmentType.GetField("foodDatabase");
                
                if (databaseField != null)
                {
                    K3_FoodDatabase foodDatabase = databaseField.GetValue(_assessmentSystem) as K3_FoodDatabase;
                    if (foodDatabase != null)
                    {
                        var profile = foodDatabase.GetFoodProfile(foodIndex);
                        if (profile != null)
                        {
                            return profile.foodName;
                        }
                    }
                }
            }
            catch { }
        }
        
        return $"Food_{foodIndex}";
    }
    
    private bool _bonusAwarded = false;
    #endregion

    #region Progressive Multiplier System
    void IncreaseMultiplier()
    {
        if (_isMultiplierLocked) return;
        
        // Increase streak
        _multiplierStreak++;
        
        // Calculate new multiplier
        float newMultiplier = _progressiveMultiplier + multiplierIncrement;
        
        // Cap at maximum
        if (newMultiplier > maxMultiplier)
        {
            newMultiplier = maxMultiplier;
            OnBonusEarned?.Invoke($"Max Multiplier Reached! {maxMultiplier:F1}x");
        }
        
        // Apply new multiplier
        _progressiveMultiplier = newMultiplier;
        
        // Trigger events
        OnMultiplierChanged?.Invoke(_progressiveMultiplier);
        OnMultiplierIncreased?.Invoke();
        
        Debug.Log($"Multiplier increased to {_progressiveMultiplier:F1}x (Streak: {_multiplierStreak})");
        
        UpdateMultiplierDisplay();
    }

    void ResetMultiplier()
    {
        if (_isMultiplierLocked) return;
        
        // Store old multiplier for feedback
        float oldMultiplier = _progressiveMultiplier;
        
        // Reset to starting value
        _progressiveMultiplier = startingMultiplier;
        _multiplierStreak = 0;
        
        // Trigger events
        OnMultiplierChanged?.Invoke(_progressiveMultiplier);
        OnMultiplierReset?.Invoke();
        
        Debug.Log($"Multiplier reset from {oldMultiplier:F1}x to {_progressiveMultiplier:F1}x");
        
        UpdateMultiplierDisplay();
    }

    public void LockMultiplier(bool locked)
    {
        _isMultiplierLocked = locked;
    }

    float GetCurrentMultiplier()
    {
        return _progressiveMultiplier;
    }

    int GetCurrentStreak()
    {
        return _multiplierStreak;
    }
    #endregion

    #region Scoring Methods
    public void AwardPreservativePoints(string preservativeID)
    {
        // CHECK FOR COMBO
        float timeSinceLast = Time.time - _lastCollectionTime;
        
        // If this is the first product OR time window expired, start new combo
        if (_lastCollectionTime == 0f || timeSinceLast > comboTimeWindow)
        {
            _comboCount = 1;
            Debug.Log($"Starting new combo (first preservative or combo expired: {timeSinceLast:F1}s > {comboTimeWindow}s)");
        }
        else if (timeSinceLast <= comboTimeWindow)
        {
            // Continue combo
            _comboCount = Mathf.Min(_comboCount + 1, maxComboMultiplier);
            Debug.Log($"Continuing combo: {_comboCount}x (time since last: {timeSinceLast:F1}s)");
        }
        
        _lastCollectionTime = Time.time;
        
        // Calculate combo bonus
        int comboPoints = 0;
        if (_comboCount > 1)
        {
            comboPoints = comboBonus * (_comboCount - 1);
            Debug.Log($"Combo bonus: {comboPoints} (Combo x{_comboCount}, Bonus per level: {comboBonus})");
        }
        
        // Type bonus based on preservative ID
        int typeBonus = 0;
        string preservativeType = GetPreservativeTypeFromID(preservativeID);
        
        switch (preservativeType)
        {
            case "AscorbicAcid":
                typeBonus = antiOxidantBonus;
                Debug.Log($"Anti-Oxidant bonus: +{typeBonus}");
                break;
            case "PotassiumSorbate":
            case "SodiumBenzoate":
                typeBonus = antiMicrobeBonus;
                Debug.Log($"Anti-Microbe bonus: +{typeBonus}");
                break;
            default:
                Debug.Log($"Regular preservative type: {preservativeType}");
                break;
        }
        
        // Track preservative type count
        if (!_preservativeTypeCount.ContainsKey(preservativeType))
        {
            _preservativeTypeCount[preservativeType] = 0;
        }
        _preservativeTypeCount[preservativeType]++;
        
        // Calculate raw score
        int rawScore = basePreservativePoints + comboPoints + typeBonus;
        
        // Apply PROGRESSIVE multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Update scores
        _preservativeScore += finalScore;
        _currentScore += finalScore;
        _preservativesCollected++;
        
        // INCREASE MULTIPLIER for next collection
        IncreaseMultiplier();
        
        // Trigger events
        OnPreservativeCollected?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalScore > 0)
        {
            ShowScorePopup(finalScore, preservativeScoreColor, "Preservative");
            
            // Show combo popup separately if we have combo
            if (_comboCount > 1 && comboPoints > 0)
            {
                ShowComboPopup(_comboCount, comboPoints);
            }
        }
        
        // Show combo message if applicable
        if (_comboCount > 1)
        {
            string comboMessage = $"Combo x{_comboCount}! +{comboPoints}";
            OnBonusEarned?.Invoke(comboMessage);
            Debug.Log($"Bonus earned: {comboMessage}");
        }
        
        // Debug breakdown
        Debug.Log($"Preservative collected: {preservativeID} | " +
                 $"Base: {basePreservativePoints} + Combo: {comboPoints} + Type: {typeBonus} = Raw: {rawScore} " +
                 $"× Multiplier: {multiplier:F2} = Final: {finalScore} points | " +
                 $"Combo: x{_comboCount} | New Multiplier: {GetCurrentMultiplier():F1}x");
        
        // Update UI
        UpdateScoreDisplay();
    }

    public void AwardGEMPoints(string gemType)
    {
        // Base points
        int basePoints = gemCompletionPoints;
        
        // Type bonus
        int typeBonus = 0;
        if (gemType.Contains("Oxidant") || gemType.Contains("Antioxidant"))
        {
            typeBonus = antiOxidantBonus;
        }
        else if (gemType.Contains("Microbe") || gemType.Contains("Antimicrobe"))
        {
            typeBonus = antiMicrobeBonus;
        }
        
        // Calculate total
        int rawScore = basePoints + typeBonus;
        
        // Apply progressive multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Update scores
        _gemScore += finalScore;
        _currentScore += finalScore;
        
        // Trigger events
        OnGEMCompleted?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalScore > 0)
        {
            ShowScorePopup(finalScore, gemScoreColor, "GEM");
        }
        
        Debug.Log($"GEM completed: {gemType} | " +
                 $"Raw: {rawScore} (Base:{basePoints}, Type:{typeBonus}) " +
                 $"× {multiplier:F2} = {finalScore} points");
        
        // Update UI
        UpdateScoreDisplay();
    }

    public void AwardFoodPreservationPoints(string foodName, bool isPerfect = false, bool correctType = true)
    {
        // Base points
        int basePoints = foodPreservationPoints;
        
        // Accuracy bonus (perfect placement)
        int accuracyBonus = isPerfect ? perfectPlacementBonus : 50;
        
        // Correct type bonus
        int typeBonus = correctType ? correctTypeBonus : 0;
        
        // Time bonus (if we tracked completion time)
        int timeBonus = 0; // Could be implemented based on completion time
        
        // Calculate total
        int rawScore = basePoints + accuracyBonus + typeBonus + timeBonus;
        
        // Apply progressive multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Track perfect preservations
        if (isPerfect)
        {
            _perfectPreservations++;
        }
        
        // Update scores
        _foodScore += finalScore;
        _currentScore += finalScore;
        
        // Trigger events
        OnFoodPreserved?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalScore > 0)
        {
            Color popupColor = isPerfect ? perfectScoreColor : foodScoreColor;
            string label = isPerfect ? "Perfect!" : "Food";
            ShowScorePopup(finalScore, popupColor, label);
        }
        
        // INCREASE MULTIPLIER for successful preservation
        IncreaseMultiplier();
        
        Debug.Log($"Food preserved: {foodName} | " +
                 $"Raw: {rawScore} (Base:{basePoints}, Acc:{accuracyBonus}, Type:{typeBonus}, Time:{timeBonus}) " +
                 $"× {multiplier:F2} = {finalScore} points | " +
                 $"Perfect: {isPerfect}, Correct Type: {correctType}");
        
        // Update UI
        UpdateScoreDisplay();
    }

    public void AwardFullCompletionBonus()
    {
        int bonus = fullCompletionBonus;
        float multiplier = GetCurrentMultiplier();
        int finalBonus = Mathf.RoundToInt(bonus * multiplier);
        
        _bonusScore += finalBonus;
        _currentScore += finalBonus;
        
        OnBonusEarned?.Invoke($"All Foods Preserved! +{finalBonus}");
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalBonus > 0)
        {
            ShowScorePopup(finalBonus, bonusScoreColor, "Completion Bonus");
        }
        
        Debug.Log($"Full completion bonus: {bonus} × {multiplier:F2} = {finalBonus} points");
        
        // Update UI
        UpdateScoreDisplay();
    }

        public void DeductPointsForMistake(int foodIndex, int points = 300)
    {
        if (_foodMistakeMade.ContainsKey(foodIndex) && _foodMistakeMade[foodIndex])
        {
            Debug.Log($"Mistake already recorded for food {foodIndex}");
            return;
        }
        
        // Deduct the points
        _currentScore -= points;
        _totalNegativeScore += points;
        
        // Mark this food as having a mistake
        _foodMistakeMade[foodIndex] = true;
        
        // Trigger events
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show negative score popup
        if (enableScorePopups && scorePopupPrefab != null)
        {
            ShowScorePopup(-points, Color.red, "Mistake");
        }
        
        Debug.Log($"Deducted {points} points for mistake on food {foodIndex}. Total negative: {_totalNegativeScore}");
        
        // Check if we need to deduct a heart (every 500 negative points)
        CheckForHeartDeduction();
        
        // Update UI
        UpdateScoreDisplay();
    }

    // Add this method in PreserviaScoringSystem.cs
    private void CheckForHeartDeduction()
    {
        if (_totalNegativeScore >= 500)
        {
            // Calculate how many hearts to deduct
            int heartsToDeduct = Mathf.FloorToInt(_totalNegativeScore / 500f);
            
            // Find the health system
            PreserviaPlayerStat healthSystem = FindObjectOfType<PreserviaPlayerStat>();
            if (healthSystem != null)
            {
                // Deduct hearts
                for (int i = 0; i < heartsToDeduct; i++)
                {
                    healthSystem.TakeDamage(1);
                    Debug.Log($"Deducted 1 heart! Health: {healthSystem.currentHealth}/{healthSystem.maxHealth}");
                }
                
                // Reduce negative score by the amount used
                _totalNegativeScore -= heartsToDeduct * 500;
                
                // Show warning message
                OnBonusEarned?.Invoke($"Lost {heartsToDeduct} heart(s)!");
            }
            else
            {
                Debug.LogWarning("PreserviaPlayerStat not found! Cannot deduct hearts.");
            }
        }
    }
    
    string GetPreservativeTypeFromID(string preservativeID)
    {
        // Map preservative IDs to types based on your game's implementation
        switch (preservativeID)
        {
            case "0": return "AscorbicAcid";
            case "1": return "PotassiumSorbate";
            case "2": return "SodiumBenzoate";
            default: return "Unknown";
        }
    }
    
    // FIXED: Updated method signature to match what's called in K3_KingAssessment
    public void ManualFoodPreserved(string foodName, float sliderValue, float targetMin, float targetMax, bool correctType, bool isPerfect = false)
    {
        // Base points
        int basePoints = foodPreservationPoints;
        
        // Accuracy bonus (perfect placement)
        int accuracyBonus = isPerfect ? perfectPlacementBonus : 50;
        
        // Correct type bonus
        int typeBonus = correctType ? correctTypeBonus : 0;
        
        // Time bonus (if we tracked completion time)
        int timeBonus = 0;
        
        // Calculate total
        int rawScore = basePoints + accuracyBonus + typeBonus + timeBonus;
        
        // Apply progressive multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Track perfect preservations
        if (isPerfect)
        {
            _perfectPreservations++;
        }
        
        // Update scores
        _foodScore += finalScore;
        _currentScore += finalScore;
        _foodsPreserved++;
        
        // Trigger events
        OnFoodPreserved?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalScore > 0)
        {
            Color popupColor = isPerfect ? perfectScoreColor : foodScoreColor;
            string label = isPerfect ? "Perfect!" : "Food";
            ShowScorePopup(finalScore, popupColor, label);
        }
        
        // INCREASE MULTIPLIER for successful preservation
        IncreaseMultiplier();
        
        Debug.Log($"Food preserved: {foodName} | " +
                 $"Raw: {rawScore} (Base:{basePoints}, Acc:{accuracyBonus}, Type:{typeBonus}, Time:{timeBonus}) " +
                 $"× {multiplier:F2} = {finalScore} points | " +
                 $"Perfect: {isPerfect}, Correct Type: {correctType}");
        
        UpdateScoreDisplay();
    }
    
    // FIXED: Direct scoring method that works even when text is disabled
    public void ScoreFoodPreservationDirectly(string foodName, float sliderValue, float targetMin, float targetMax, bool correctType)
    {
        // Calculate accuracy
        bool isInRange = sliderValue >= targetMin && sliderValue <= targetMax;
        float targetCenter = (targetMin + targetMax) / 2f;
        float distanceFromCenter = Mathf.Abs(sliderValue - targetCenter);
        float rangeWidth = targetMax - targetMin;
        float accuracyPercent = Mathf.Clamp01(1f - (distanceFromCenter / (rangeWidth / 2f))) * 100f;
        
        bool isPerfect = accuracyPercent >= 90f || isInRange;
        
        // Award points using the updated method
        ManualFoodPreserved(foodName, sliderValue, targetMin, targetMax, correctType, isPerfect);
    }
    #endregion

    #region UI Management
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
        }
        
        UpdateMultiplierDisplay();
        
        if (preservativesText != null)
        {
            preservativesText.text = $"Preservatives: {_preservativesCollected}";
        }
        
        if (foodsPreservedText != null)
        {
            foodsPreservedText.text = $"Foods Preserved: {_foodsPreserved}/8";
        }
        
        if (gemsCompletedText != null)
        {
            gemsCompletedText.text = $"GEMs: {_gemsCompleted}/2";
        }
    }
    
    void UpdateMultiplierDisplay()
    {
        if (multiplierText != null)
        {
            float multiplier = GetCurrentMultiplier();
            multiplierText.text = $"Multiplier: {multiplier:F1}x";
            multiplierText.color = GetMultiplierColor(multiplier);
        }
    }
    
    Color GetMultiplierColor(float multiplier)
    {
        if (multiplier >= 2.5f) return new Color(1f, 0.5f, 0f); // Orange
        if (multiplier >= 2.0f) return Color.yellow;
        if (multiplier >= 1.5f) return Color.green;
        if (multiplier >= 1.0f) return Color.white;
        return Color.gray;
    }
    #endregion

    #region Score Popup System
    // Class to store popup data
    private class ScorePopupData
    {
        public int scoreAmount;
        public Color color;
        public string label;
        public Vector3? worldPosition;
    }
    
    // Show score popup
    private void ShowScorePopup(int score, Color color, string label = "", Vector3? worldPosition = null)
    {
        if (!enableScorePopups || scorePopupPrefab == null || scorePopupParent == null)
            return;
        
        // Add to queue
        _scorePopupQueue.Enqueue(new ScorePopupData
        {
            scoreAmount = score,
            color = color,
            label = label,
            worldPosition = worldPosition
        });
    }
    
    // Process popup queue
    private void ProcessPopupQueue()
    {
        if (_scorePopupQueue.Count > 0 && !_isProcessingPopup)
        {
            StartCoroutine(ProcessPopupCoroutine());
        }
    }
    
    private IEnumerator ProcessPopupCoroutine()
    {
        _isProcessingPopup = true;
        
        while (_scorePopupQueue.Count > 0)
        {
            ScorePopupData popupData = _scorePopupQueue.Dequeue();
            CreateScorePopup(popupData);
            
            // Small delay between popups to prevent overlap
            yield return new WaitForSeconds(0.05f);
        }
        
        _isProcessingPopup = false;
    }
    
    private void CreateScorePopup(ScorePopupData data)
    {
        try
        {
            // Instantiate the popup
            GameObject popupObj = Instantiate(scorePopupPrefab, scorePopupParent);
            popupObj.name = $"ScorePopup_{data.scoreAmount}";
            
            // Get the TextMeshPro component
            TMP_Text popupText = popupObj.GetComponent<TMP_Text>();
            if (popupText == null)
            {
                popupText = popupObj.GetComponentInChildren<TMP_Text>();
            }
            
            if (popupText != null)
            {
                // Format the text
                string formattedText = $"+{data.scoreAmount}";
                if (!string.IsNullOrEmpty(data.label))
                {
                    formattedText += $" {data.label}";
                }
                
                popupText.text = formattedText;
                popupText.color = data.color;
                
                // Keep consistent scale
                popupText.transform.localScale = Vector3.one;
                
                // Calculate position
                Vector3 popupPosition;
                
                if (data.worldPosition.HasValue)
                {
                    // Convert world position to screen position
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(data.worldPosition.Value);
                    popupPosition = screenPos;
                }
                else
                {
                    // Position near the score display with stacking
                    if (scoreText != null)
                    {
                        Vector3 scorePos = scoreText.transform.position;
                        
                        // Calculate vertical offset based on active popups
                        float verticalOffset = _activePopups.Count * popupSpacing;
                        
                        popupPosition = new Vector3(
                            scorePos.x + popupSpawnOffset.x,
                            scorePos.y + popupSpawnOffset.y + verticalOffset,
                            scorePos.z
                        );
                    }
                    else
                    {
                        popupPosition = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                    }
                }
                
                popupObj.transform.position = popupPosition;
                
                // Add to active popups list
                _activePopups.Add(popupObj);
                
                // Start the popup animation
                StartCoroutine(AnimateScorePopup(popupObj, popupText, popupPosition));
            }
            else
            {
                Debug.LogWarning("Score popup prefab doesn't have a TMP_Text component!");
                Destroy(popupObj);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating score popup: {e.Message}");
        }
    }
    
    private IEnumerator AnimateScorePopup(GameObject popupObj, TMP_Text popupText, Vector3 startPosition)
    {
        float elapsedTime = 0f;
        Color startColor = popupText.color;
        
        while (elapsedTime < popupLifetime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / popupLifetime;
            
            // Float upward with easing
            float floatAmount = Mathf.Lerp(0, popupFloatSpeed, EaseOutQuad(normalizedTime));
            popupObj.transform.position = startPosition + Vector3.up * floatAmount;
            
            // Fade out near the end
            if (normalizedTime > (1f - (popupFadeDuration / popupLifetime)))
            {
                float fadeProgress = (normalizedTime - (1f - (popupFadeDuration / popupLifetime))) * 
                    (popupLifetime / popupFadeDuration);
                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(startColor.a, 0f, fadeProgress);
                popupText.color = fadedColor;
            }
            
            yield return null;
        }
        
        // Remove from active popups and destroy
        _activePopups.Remove(popupObj);
        Destroy(popupObj);
    }
    
    private void ShowComboPopup(int comboLevel, int comboBonusPoints)
    {
        if (!enableScorePopups || scorePopupPrefab == null || scorePopupParent == null)
            return;
        
        // Create combo popup with different style
        GameObject comboPopup = Instantiate(scorePopupPrefab, scorePopupParent);
        comboPopup.name = $"ComboPopup_x{comboLevel}";
        
        TMP_Text comboText = comboPopup.GetComponent<TMP_Text>();
        if (comboText == null)
        {
            comboText = comboPopup.GetComponentInChildren<TMP_Text>();
        }
        
        if (comboText != null)
        {
            comboText.text = $"COMBOx{comboLevel}! +{comboBonusPoints}";
            comboText.color = comboScoreColor;
            comboText.enableAutoSizing = false;
            comboText.fontSize = 60f;
            
            // Position near but offset from regular score popup
            Vector3 position = new Vector3(
                Screen.width / 2 + 50,
                Screen.height * 0.3f + (_activePopups.Count * popupSpacing * 2),
                0
            );
            
            comboPopup.transform.position = position;
            
            // Special animation for combo popup
            StartCoroutine(AnimateComboPopup(comboPopup, comboText, position));
        }
        else
        {
            Destroy(comboPopup);
        }
    }
    
    private IEnumerator AnimateComboPopup(GameObject popupObj, TMP_Text popupText, Vector3 startPosition)
    {
        float elapsedTime = 0f;
        Color startColor = popupText.color;
        Vector3 startScale = popupText.transform.localScale;
        
        // First, grow animation
        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / 0.2f;
            
            // Pulse effect
            float pulse = Mathf.Sin(t * Mathf.PI) * 0.3f + 1f;
            popupText.transform.localScale = startScale * pulse;
            
            yield return null;
        }
        
        // Reset scale
        popupText.transform.localScale = startScale;
        
        // Then float upward like regular popup
        elapsedTime = 0f;
        
        while (elapsedTime < popupLifetime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / popupLifetime;
            
            // Float upward
            float floatAmount = Mathf.Lerp(0, popupFloatSpeed * 1.5f, EaseOutQuad(normalizedTime));
            popupObj.transform.position = startPosition + Vector3.up * floatAmount;
            
            // Fade out near the end
            if (normalizedTime > (1f - (popupFadeDuration / popupLifetime)))
            {
                float fadeProgress = (normalizedTime - (1f - (popupFadeDuration / popupLifetime))) * 
                    (popupLifetime / popupFadeDuration);
                Color fadedColor = startColor;
                fadedColor.a = Mathf.Lerp(startColor.a, 0f, fadeProgress);
                popupText.color = fadedColor;
            }
            
            yield return null;
        }
        
        Destroy(popupObj);
    }
    
    // Easing function for smooth animation
    private float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }
    #endregion

    #region Public API
    // Manual triggers for integration
    public void ManualPreservativeCollected(string preservativeID)
    {
        AwardPreservativePoints(preservativeID);
    }
    
    public void ManualGEMCompleted(string gemType)
    {
        if (gemType.Contains("Oxidant") && !_oxidantGEMScored)
        {
            AwardGEMPoints(gemType);
            _oxidantGEMScored = true;
            _gemsCompleted++;
        }
        else if (gemType.Contains("Microbe") && !_microbeGEMScored)
        {
            AwardGEMPoints(gemType);
            _microbeGEMScored = true;
            _gemsCompleted++;
        }
    }
    
    // Backward compatibility method (6 parameters)
    public void ManualFoodPreserved(string foodName, float sliderValue, float targetMin, float targetMax, bool correctType)
    {
        ManualFoodPreserved(foodName, sliderValue, targetMin, targetMax, correctType, false);
    }
    
    public void ManualResetMultiplier()
    {
        ResetMultiplier();
    }
    
    public void ManualGameStarted()
    {
        ResetSessionStats();
        StartMonitoring();
    }
    
    // Getters for other systems
    public int GetCurrentScore() => _currentScore;
    public float GetCurrentMultiplierValue() => GetCurrentMultiplier();
    public int GetCurrentStreakCount() => GetCurrentStreak();
    public int GetPreservativesCollected() => _preservativesCollected;
    public int GetGemsCompleted() => _gemsCompleted;
    public int GetFoodsPreserved() => _foodsPreserved;
    public int GetPerfectPreservations() => _perfectPreservations;
    
    // Check if specific GEM has been scored
    public bool IsGEMScored(string gemType)
    {
        if (gemType.Contains("Oxidant")) return _oxidantGEMScored;
        if (gemType.Contains("Microbe")) return _microbeGEMScored;
        return false;
    }
    
    // Check if specific food has been preserved this session
    public bool IsFoodPreservedThisSession(int foodIndex)
    {
        return _foodPreservedThisSession.ContainsKey(foodIndex) && _foodPreservedThisSession[foodIndex];
    }
    #endregion

    #region Session Management
    public void ResetSessionStats()
    {
        _currentScore = 0;
        _preservativeScore = 0;
        _gemScore = 0;
        _foodScore = 0;
        _bonusScore = 0;
        
        _preservativesCollected = 0;
        _gemsCompleted = 0;
        _foodsPreserved = 0;
        _perfectPreservations = 0;
        _comboCount = 0;
        _multiplierStreak = 0;
        _lastCollectionTime = 0f;
        _sessionStartTime = Time.time;

        _totalNegativeScore = 0;
        ResetMistakes();
        
        // Reset GEM tracking
        _oxidantGEMScored = false;
        _microbeGEMScored = false;
        
        // Reset food tracking
        foreach (var key in _foodPreservedThisSession.Keys.ToList())
        {
            _foodPreservedThisSession[key] = false;
        }
        
        _preservativeTypeCount.Clear();
        _bonusAwarded = false;
        
        _progressiveMultiplier = startingMultiplier;
        
        UpdateScoreDisplay();
        
        Debug.Log("Scoring session reset");
    }
    
    public void EndSession()
    {
        StopMonitoring();
        PrintSessionSummary();
    }
    
    void PrintSessionSummary()
    {
        float sessionDuration = Time.time - _sessionStartTime;
        float finalMultiplier = GetCurrentMultiplier();
        
        Debug.Log("=== PRESERVIA SCORING SESSION SUMMARY ===");
        Debug.Log($"Final Score: {_currentScore}");
        Debug.Log($"Session Duration: {FormatTime(sessionDuration)}");
        Debug.Log($"Final Multiplier: {finalMultiplier:F1}x (Max Streak: {_multiplierStreak})");
        Debug.Log($"--- Breakdown ---");
        Debug.Log($"Preservatives: {_preservativeScore} ({_preservativesCollected} collected)");
        Debug.Log($"GEMs: {_gemScore} ({_gemsCompleted}/2 completed)");
        Debug.Log($"Foods: {_foodScore} ({_foodsPreserved}/8 preserved)");
        Debug.Log($"Perfect Preservations: {_perfectPreservations}");
        Debug.Log($"Bonuses: {_bonusScore}");
        Debug.Log($"Max Combo: x{_comboCount}");
        Debug.Log($"--- GEM Status ---");
        Debug.Log($"  Anti-Oxidant GEM: {(_oxidantGEMScored ? "Scored" : "Not scored")}");
        Debug.Log($"  Anti-Microbe GEM: {(_microbeGEMScored ? "Scored" : "Not scored")}");
        Debug.Log($"--- Preservative Types ---");
        foreach (var kvp in _preservativeTypeCount)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
        Debug.Log("==================");
    }
    
    string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
    #endregion

    #region Debug & Testing
    [ContextMenu("Test Preservative Collection")]
    public void DebugTestPreservativeCollection()
    {
        AwardPreservativePoints("0"); // Ascorbic Acid
    }
    
    [ContextMenu("Test GEM Completion")]
    public void DebugTestGEMCompletion()
    {
        ManualGEMCompleted("Anti-Oxidant_GEM");
    }
    
    [ContextMenu("Test Food Preservation")]
    public void DebugTestFoodPreservation()
    {
        ScoreFoodPreservationDirectly("TEST_FOOD", 75f, 70f, 80f, true);
    }
    
    [ContextMenu("Debug Scoring State")]
    public void DebugScoringState()
    {
        Debug.Log("=== SCORING SYSTEM STATE ===");
        Debug.Log($"Current Score: {_currentScore}");
        Debug.Log($"Multiplier: {GetCurrentMultiplier():F1}x (Streak: {_multiplierStreak})");
        Debug.Log($"Preservatives: {_preservativesCollected} collected");
        Debug.Log($"GEMs: {_gemsCompleted}/2 completed");
        Debug.Log($"  - Anti-Oxidant: {(_oxidantGEMScored ? "Scored" : "Not scored")}");
        Debug.Log($"  - Anti-Microbe: {(_microbeGEMScored ? "Scored" : "Not scored")}");
        Debug.Log($"Foods: {_foodsPreserved}/8 preserved");
        Debug.Log($"Perfect Preservations: {_perfectPreservations}");
        Debug.Log($"Combo: x{_comboCount}");
        
        if (_collectionSystem != null)
        {
            Debug.Log($"Collection System Found: {_collectionSystem.gameObject.name}");
        }
        
        if (_gemSystem != null)
        {
            Debug.Log($"GEM System Found: {_gemSystem.gameObject.name}");
        }
        
        if (_assessmentSystem != null)
        {
            Debug.Log($"Assessment System Found: {_assessmentSystem.gameObject.name}");
        }
    }
    #endregion

    void OnDestroy()
    {
        StopMonitoring();
        
        // Unsubscribe from events
        if (_collectionSystem != null)
        {
            _collectionSystem.OnPotionCollected -= HandlePreservativeCollected;
        }
    }

    // In PreserviaScoringSystem.cs, add to Public API region:
    public void DeductHealthForNegativeScore()
    {
        CheckForHeartDeduction();
    }

    public int GetNegativeScore()
    {
        return _totalNegativeScore;
    }

    public void ResetMistakes()
    {
        for (int i = 0; i < 8; i++)
        {
            _foodMistakeMade[i] = false;
        }
        _totalNegativeScore = 0;
    }
}