using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using System.Collections;

public class SugariaScoringSystem : MonoBehaviour
{
    // Singleton pattern
    public static SugariaScoringSystem Instance { get; private set; }

    [Header("Scoring Configuration")]
    [Tooltip("Base points for collecting a regular product")]
    public int baseProductPoints = 100;
    
    [Tooltip("Base points for completing QA1 successfully")]
    public int qa1CompletionPoints = 500;
    
    [Tooltip("Points for each correct QA2 answer")]
    public int qa2CorrectAnswerPoints = 200;
    
    [Tooltip("Bonus for completing all QA2 products")]
    public int qa2FullCompletionBonus = 1000;

    [Header("Progressive Multiplier Settings")]
    [Tooltip("Multiplier increase per product collected")]
    public float multiplierIncrement = 0.25f;
    
    [Tooltip("Maximum achievable multiplier")]
    public float maxMultiplier = 3.0f;
    
    [Tooltip("Starting multiplier value")]
    public float startingMultiplier = 1.0f;

    [Header("Bonus Settings")]
    [Tooltip("Bonus for product collection combos")]
    public int comboBonus = 50;
    
    [Tooltip("Time window for combos (seconds)")]
    public float comboTimeWindow = 10f;
    
    [Tooltip("Maximum combo multiplier")]
    public int maxComboMultiplier = 5;
    
    [Tooltip("Natural sugar product bonus")]
    public int naturalSugarBonus = 50;
    
    [Tooltip("Added sugar product bonus")]
    public int addedSugarBonus = 25;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnProductScored;
    public UnityEvent<int> OnQA1Scored;
    public UnityEvent<int> OnQA2Scored;
    public UnityEvent<float> OnMultiplierChanged;
    public UnityEvent<string> OnBonusEarned;
    public UnityEvent OnMultiplierReset;
    public UnityEvent OnMultiplierIncreased;

    [Header("Score Display")]
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public UnityEngine.UI.Text heartsText;
    public UnityEngine.UI.Text timeText;

    [Header("Score Popup System")]
    public GameObject scorePopupPrefab; // Prefab for "+X" popup
    public Transform scorePopupParent; // Parent transform for popups (usually Canvas)
    public Vector2 popupSpawnOffset = new Vector2(50, 50); // Offset from score display
    public float popupLifetime = 1.5f; // How long popup stays visible
    public float popupFadeDuration = 0.3f; // Fade out duration
    public float popupFloatSpeed = 50f; // How fast popup floats upward
    public bool enableScorePopups = true; // Toggle score popups on/off
    public float popupSpacing = 30f; // Vertical spacing between multiple popups
    
    [Header("Popup Colors")]
    public Color productScoreColor = Color.green;
    public Color qa1ScoreColor = Color.yellow;
    public Color qa2ScoreColor = Color.cyan;
    public Color bonusScoreColor = new Color(1f, 0.5f, 0f); // Orange
    public Color comboScoreColor = new Color(0.5f, 0f, 1f); // Purple

    // Score tracking
    private int _currentScore = 0;
    private int _productScore = 0;
    private int _qa1Score = 0;
    private int _qa2Score = 0;
    private int _bonusScore = 0;
    
    // Progressive multiplier system
    private float _progressiveMultiplier = 1.0f;
    private int _multiplierStreak = 0;
    private int _lastHealthValue = 0;
    private bool _isMultiplierLocked = false; // Prevent multiplier from decreasing during animations
    
    // Session stats
    private int _productsCollected = 0;
    private int _qa1CorrectSelections = 0;
    private int _qa2CorrectAnswers = 0;
    private int _comboCount = 0;
    private float _lastCollectionTime = 0f;
    private float _sessionStartTime = 0f;
    
    // References (found dynamically)
    private SugariaPlayerStat _playerHealth;
    private GameplayProgression _gameplayProgression;
    private ProductInformationManager _productManager;
    private K2_QA1system _qa1System;
    private K2_QA2system _qa2System;
    
    // QA1 tracking
    private bool _wasQA1Active = false;
    private int _lastQA1SelectedCount = 0;
    private bool _qa1CompletionScored = false; // Track if we already scored this QA1 session
    
    // State tracking
    private bool _isMonitoring = false;
    
    // Queue for score popups to prevent too many at once
    private Queue<ScorePopupData> _scorePopupQueue = new Queue<ScorePopupData>();
    private bool _isProcessingPopup = false;
    
    // Track active popups for positioning
    private List<GameObject> _activePopups = new List<GameObject>();
    
    // Add ProductDatabase reference for product type checking
    private ProductData _productDatabase;

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
        
        Debug.Log("Sugaria Scoring System initialized");
    }

    void InitializeSystem()
    {
        _sessionStartTime = Time.time;
        _progressiveMultiplier = startingMultiplier;
        
        // Find all necessary components dynamically
        FindAllReferences();
        
        // Subscribe to QA1 completion event if available
        SubscribeToQA1Events();
        
        // Initialize multiplier display
        UpdateMultiplierDisplay();
        UpdateScoreDisplay();
        
        // Store initial health value
        if (_playerHealth != null)
        {
            _lastHealthValue = _playerHealth.currentHealth;
        }
        
        // Validate score popup parent
        if (scorePopupParent == null && scorePopupPrefab != null)
        {
            // Try to find Canvas automatically
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                scorePopupParent = canvas.transform;
                Debug.Log($"Auto-assigned Canvas as score popup parent: {canvas.name}");
            }
        }
    }

    void FindAllReferences()
    {
        _playerHealth = FindObjectOfType<SugariaPlayerStat>();
        _gameplayProgression = FindObjectOfType<GameplayProgression>();
        _productManager = FindObjectOfType<ProductInformationManager>();
        _qa1System = FindObjectOfType<K2_QA1system>();
        _qa2System = FindObjectOfType<K2_QA2system>();
        
        if (_playerHealth != null) Debug.Log("Found player health system");
        if (_gameplayProgression != null) Debug.Log("Found gameplay progression");
        if (_productManager != null) Debug.Log("Found product manager");
        if (_qa1System != null) Debug.Log("Found QA1 system");
        if (_qa2System != null) Debug.Log("Found QA2 system");
    }
    
    // NEW: Subscribe to QA1 events
    void SubscribeToQA1Events()
    {
        if (_qa1System == null) return;
        
        // Try to subscribe to completion events using reflection
        var qa1Type = _qa1System.GetType();
        
        // Method 1: Check for UnityEvent
        var onCompletedField = qa1Type.GetField("OnQA1Completed");
        if (onCompletedField != null)
        {
            try
            {
                UnityEvent<int, bool> qa1Event = onCompletedField.GetValue(_qa1System) as UnityEvent<int, bool>;
                if (qa1Event != null)
                {
                    qa1Event.AddListener(HandleQA1Completed);
                    Debug.Log("Subscribed to QA1 UnityEvent");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not subscribe to QA1 UnityEvent: {e.Message}");
            }
        }
        
        // Method 2: Check for C# event
        var eventInfo = qa1Type.GetEvent("OnQA1Completed");
        if (eventInfo != null)
        {
            try
            {
                // This requires knowing the delegate type
                Debug.Log("Found QA1 C# event, but need delegate type to subscribe");
            }
            catch { }
        }
        
        // Method 3: Check for public method we can call
        var completeMethod = qa1Type.GetMethod("RegisterCompletionHandler");
        if (completeMethod != null)
        {
            try
            {
                completeMethod.Invoke(_qa1System, new object[] { new Action<int, bool>(HandleQA1Completed) });
                Debug.Log("Registered QA1 completion handler");
            }
            catch { }
        }
    }
    
    // NEW: Handle QA1 completion event
    void HandleQA1Completed(int selectedCount, bool allAddedSugar)
    {
        if (!_qa1CompletionScored)
        {
            AwardQA1Points(selectedCount, allAddedSugar);
            _qa1CompletionScored = true;
            Debug.Log($"QA1 completed via event! Selected: {selectedCount}, All Added Sugar: {allAddedSugar}");
        }
        else
        {
            Debug.LogWarning("QA1 already scored this session!");
        }
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
        
        // Monitor product collection
        MonitorProductCollection();
        
        // Monitor QA1 completion (backup method)
        MonitorQA1Completion();
        
        // Monitor QA2 answers
        MonitorQA2Answers();
        
        // Monitor health changes for multiplier resets
        MonitorHealthChanges();
        
        // Update display
        UpdateScoreDisplay();
        
        // Process score popup queue
        ProcessPopupQueue();
    }

    void MonitorProductCollection()
    {
        if (_productManager == null) return;
        
        // Get current collected count
        int currentCount = _productManager.GetCollectedCount();
        
        // Check if count increased since last check
        if (currentCount > _productsCollected)
        {
            int newProducts = currentCount - _productsCollected;
            
            // Award points for each new product
            for (int i = 0; i < newProducts; i++)
            {
                // We don't know the exact product type, so use generic
                AwardProductPoints("Product", "Regular");
            }
            
            _productsCollected = currentCount;
        }
    }

    void MonitorQA1Completion()
    {
        if (_qa1System == null) return;
        
        bool isQA1Active = _qa1System.IsUIActive();
        int currentSelected = _qa1System.GetSelectedCount();
        int maxSelections = _qa1System.GetMaxSelections();
        
        // NEW: Direct completion detection (not dependent on UI closing)
        if (isQA1Active && currentSelected >= maxSelections && !_qa1CompletionScored)
        {
            // QA1 is completed (max selections reached)
            bool allAddedSugar = CheckIfQA1AllAddedSugar();
            
            AwardQA1Points(currentSelected, allAddedSugar);
            _qa1CompletionScored = true;
            
            Debug.Log($"QA1 completed via direct detection! Selected: {currentSelected}/{maxSelections}, All Added Sugar: {allAddedSugar}");
        }
        
        // Backup: Still check for UI transition (legacy support)
        if (_wasQA1Active && !isQA1Active)
        {
            if (!_qa1CompletionScored && currentSelected >= maxSelections)
            {
                bool allAddedSugar = CheckIfQA1AllAddedSugar();
                AwardQA1Points(currentSelected, allAddedSugar);
                _qa1CompletionScored = true;
                Debug.Log($"QA1 completed via UI transition backup");
            }
        }
        
        // Reset scoring flag when QA1 becomes active (new attempt)
        if (!_wasQA1Active && isQA1Active)
        {
            _qa1CompletionScored = false;
            Debug.Log("QA1 started - resetting scoring flag");
        }
        
        // Update tracking variables
        _wasQA1Active = isQA1Active;
        _lastQA1SelectedCount = currentSelected;
    }
    
    // Method to check if all selected QA1 products are added sugar
    private bool CheckIfQA1AllAddedSugar()
    {
        if (_qa1System == null || _productDatabase == null) return false;
        
        try
        {
            // Get the selected product IDs from QA1
            var selectedProducts = _qa1System.GetSelectedProducts();
            
            if (selectedProducts == null || selectedProducts.Count == 0) return false;
            
            // Check each selected product
            int addedSugarCount = 0;
            foreach (string productID in selectedProducts)
            {
                var productInfo = GetProductInfo(productID);
                if (productInfo != null && productInfo.productType == ProductData.ProductType.AddedSugar)
                {
                    addedSugarCount++;
                }
            }
            
            return addedSugarCount == selectedProducts.Count;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error checking QA1 product types: {e.Message}");
            return false; // Default to not perfect
        }
    }
    
    // Helper method to get product info
    private ProductData.ProductInfo GetProductInfo(string productID)
    {
        if (_productDatabase != null)
        {
            return _productDatabase.GetProductInfo(productID);
        }
        return null;
    }

    void MonitorQA2Answers()
    {
        if (_qa2System == null) return;
        
        // Get current correctly answered count
        int currentCorrect = _qa2System.GetCorrectlyAnsweredCount();
        
        // Check if count increased
        if (currentCorrect > _qa2CorrectAnswers)
        {
            int newCorrect = currentCorrect - _qa2CorrectAnswers;
            
            // Award points for each new correct answer
            for (int i = 0; i < newCorrect; i++)
            {
                AwardQA2Points("QA2_Product");
            }
            
            _qa2CorrectAnswers = currentCorrect;
            
            // Check for full completion bonus (8 products total)
            if (currentCorrect >= 8)
            {
                AwardFullCompletionBonus();
            }
        }
    }

    void MonitorHealthChanges()
    {
        if (_playerHealth == null) return;
        
        int currentHealth = _playerHealth.currentHealth;
        
        // Check if player lost health (took damage)
        if (currentHealth < _lastHealthValue)
        {
            // Player took damage - reset multiplier
            ResetMultiplier();
            Debug.Log($"Health decreased from {_lastHealthValue} to {currentHealth}. Multiplier reset.");
        }
        
        _lastHealthValue = currentHealth;
    }
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
    public void AwardProductPoints(string productID, string productType)
    {
        // Calculate base points
        int basePoints = baseProductPoints;
        
        // Check for combo
        float timeSinceLast = Time.time - _lastCollectionTime;
        if (timeSinceLast <= comboTimeWindow)
        {
            _comboCount = Mathf.Min(_comboCount + 1, maxComboMultiplier);
        }
        else
        {
            _comboCount = 1;
        }
        
        _lastCollectionTime = Time.time;
        
        // Calculate combo bonus
        int comboPoints = comboBonus * (_comboCount - 1);
        
        // Type bonus
        int typeBonus = 0;
        if (productType == "NaturalSugar")
        {
            typeBonus = naturalSugarBonus;
        }
        else if (productType == "AddedSugar")
        {
            typeBonus = addedSugarBonus;
        }
        
        // Calculate raw score
        int rawScore = basePoints + comboPoints + typeBonus;
        
        // Apply PROGRESSIVE multiplier (not health-based)
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Update scores
        _productScore += finalScore;
        _currentScore += finalScore;
        
        // INCREASE MULTIPLIER for next collection
        IncreaseMultiplier();
        
        // Trigger events
        OnProductScored?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup - ONLY SHOW FINAL SCORE, NOT SEPARATE BONUSES
        if (enableScorePopups && finalScore > 0)
        {
            ShowScorePopup(finalScore, productScoreColor, "Product");
        }
        
        // Show combo message if applicable
        if (_comboCount > 1)
        {
            OnBonusEarned?.Invoke($"Combo x{_comboCount}! +{comboPoints}");
        }
        
        Debug.Log($"Product scored: {productID} | " +
                 $"Raw: {rawScore} × {multiplier:F2} = {finalScore} points | " +
                 $"New Multiplier: {GetCurrentMultiplier():F1}x");
    }

    public void AwardQA1Points(int correctlySelected, bool perfectSelection)
    {
        Debug.Log($"AwardQA1Points called! Selected: {correctlySelected}, Perfect: {perfectSelection}");
        
        // Base points
        int basePoints = qa1CompletionPoints;
        
        // Accuracy bonus (perfect = all 5 are added sugar)
        int accuracyBonus = perfectSelection ? 250 : 100;
        
        // Time bonus (if QA1 was completed quickly)
        float completionTime = _gameplayProgression != null ? _gameplayProgression.GetCurrentTime() : 0f;
        int timeBonus = CalculateTimeBonus(completionTime);
        
        // Calculate total
        int rawScore = basePoints + accuracyBonus + timeBonus;
        
        // Apply progressive multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Update scores
        _qa1Score += finalScore;
        _currentScore += finalScore;
        _qa1CorrectSelections = correctlySelected;
        
        // Trigger events
        OnQA1Scored?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup - ONLY SHOW FINAL SCORE, NOT SEPARATE BONUSES
        if (enableScorePopups && finalScore > 0)
        {
            ShowScorePopup(finalScore, qa1ScoreColor, "QA1");
        }
        
        Debug.Log($"QA1 scored: {correctlySelected}/5 | " +
                 $"Raw: {rawScore} (Base:{basePoints}, Acc:{accuracyBonus}, Time:{timeBonus}) " +
                 $"× {multiplier:F2} = {finalScore} points");
    }

    public void AwardQA2Points(string productID)
    {
        // Base points
        int basePoints = qa2CorrectAnswerPoints;
        
        // Speed bonus (if we tracked answer time)
        // Note: We don't have answer time tracking in current setup
        
        // Calculate total
        int rawScore = basePoints;
        
        // Apply progressive multiplier
        float multiplier = GetCurrentMultiplier();
        int finalScore = Mathf.RoundToInt(rawScore * multiplier);
        
        // Update scores
        _qa2Score += finalScore;
        _currentScore += finalScore;
        
        // Trigger events
        OnQA2Scored?.Invoke(finalScore);
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalScore > 0)
        {
            ShowScorePopup(finalScore, qa2ScoreColor, "QA2");
        }
        
        Debug.Log($"QA2 scored: {productID} | " +
                 $"Raw: {rawScore} × {multiplier:F2} = {finalScore} points");
    }

    void AwardFullCompletionBonus()
    {
        int bonus = qa2FullCompletionBonus;
        float multiplier = GetCurrentMultiplier();
        int finalBonus = Mathf.RoundToInt(bonus * multiplier);
        
        _bonusScore += finalBonus;
        _currentScore += finalBonus;
        
        OnBonusEarned?.Invoke($"All Products Completed! +{finalBonus}");
        OnScoreChanged?.Invoke(_currentScore);
        
        // Show score popup
        if (enableScorePopups && finalBonus > 0)
        {
            ShowScorePopup(finalBonus, bonusScoreColor, "Completion Bonus");
        }
        
        Debug.Log($"Full completion bonus: {bonus} × {multiplier:F2} = {finalBonus} points");
    }
    
    int CalculateTimeBonus(float completionTime)
    {
        // Bonus for quick QA1 completion
        if (completionTime <= 60f) return 100;   // Within 1 minute
        if (completionTime <= 120f) return 50;   // Within 2 minutes
        if (completionTime <= 180f) return 25;   // Within 3 minutes
        return 0;
    }
    
    // NEW: Public method for QA1 system to call directly
    public void ScoreQA1Completion(int selectedCount, bool allAddedSugar)
    {
        if (!_qa1CompletionScored)
        {
            AwardQA1Points(selectedCount, allAddedSugar);
            _qa1CompletionScored = true;
            Debug.Log($"QA1 scored via direct call! Selected: {selectedCount}, All Added Sugar: {allAddedSugar}");
        }
    }
    #endregion

    #region UI Management
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
            
            // Optional: Add a quick color flash when score changes
            StartCoroutine(FlashScoreText());
        }
        
        UpdateMultiplierDisplay();
        
        if (heartsText != null && _playerHealth != null)
        {
            heartsText.text = $"Hearts: {_playerHealth.currentHealth}/{_playerHealth.maxHealth}";
            heartsText.color = GetHeartColor(_playerHealth.currentHealth, _playerHealth.maxHealth);
        }
        
        if (timeText != null && _gameplayProgression != null)
        {
            timeText.text = $"Time: {_gameplayProgression.GetFormattedTime()}";
        }
    }
    
    IEnumerator FlashScoreText()
    {
        Color originalColor = scoreText.color;
        scoreText.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        scoreText.color = originalColor;
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
    
    Color GetHeartColor(int current, int max)
    {
        float percent = (float)current / max;
        
        if (percent >= 0.7f) return Color.green;
        if (percent >= 0.4f) return Color.yellow;
        return Color.red;
    }
    #endregion

    #region Score Popup System
    // NEW: Class to store popup data
    private class ScorePopupData
    {
        public int scoreAmount;
        public Color color;
        public string label;
        public Vector3? worldPosition;
    }
    
    // NEW: Show score popup
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
    
    // NEW: Process popup queue
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
                
                // Keep consistent scale (no scaling based on score)
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
    
    // Easing function for smooth animation
    private float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }
    
    // NEW: Public method to show score popup at world position
    public void ShowScorePopupAtPosition(int score, Vector3 worldPosition, ScorePopupType type = ScorePopupType.Product)
    {
        Color color = GetPopupColorForType(type);
        string label = GetPopupLabelForType(type);
        
        ShowScorePopup(score, color, label, worldPosition);
    }
    
    public enum ScorePopupType
    {
        Product,
        QA1,
        QA2,
        Bonus,
        Combo
    }
    
    private Color GetPopupColorForType(ScorePopupType type)
    {
        switch (type)
        {
            case ScorePopupType.Product: return productScoreColor;
            case ScorePopupType.QA1: return qa1ScoreColor;
            case ScorePopupType.QA2: return qa2ScoreColor;
            case ScorePopupType.Bonus: return bonusScoreColor;
            case ScorePopupType.Combo: return comboScoreColor;
            default: return Color.white;
        }
    }
    
    private string GetPopupLabelForType(ScorePopupType type)
    {
        switch (type)
        {
            case ScorePopupType.Product: return "";
            case ScorePopupType.QA1: return "QA1";
            case ScorePopupType.QA2: return "QA2";
            case ScorePopupType.Bonus: return "Bonus";
            case ScorePopupType.Combo: return "Combo";
            default: return "";
        }
    }
    
    // NEW: Method to show popup with custom settings
    public void ShowCustomScorePopup(int score, Vector3 position, Color color, string text = "")
    {
        if (!string.IsNullOrEmpty(text))
        {
            ShowScorePopup(score, color, text, position);
        }
        else
        {
            ShowScorePopup(score, color, "", position);
        }
    }
    #endregion

    #region Public API
    // Manual triggers for integration (optional)
    public void ManualProductCollected(string productID, string productType)
    {
        AwardProductPoints(productID, productType);
    }
    
    public void ManualQA1Completed(int correctlySelected, bool allAddedSugar)
    {
        AwardQA1Points(correctlySelected, allAddedSugar);
    }
    
    public void ManualQA2Answered(string productID, bool isCorrect)
    {
        if (!isCorrect) return;
        AwardQA2Points(productID);
    }
    
    public void ManualHealthDecreased()
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
    public int GetProductsCollected() => _productsCollected;
    public int GetQA1Correct() => _qa1CorrectSelections;
    public int GetQA2Correct() => _qa2CorrectAnswers;
    
    // NEW: Getter for QA1 scoring status
    public bool GetQA1Scored() => _qa1CompletionScored;
    
    // NEW: Direct scoring method for QA1 system
    public void DirectScoreQA1(int selectedCount, bool allAddedSugar)
    {
        ScoreQA1Completion(selectedCount, allAddedSugar);
    }
    #endregion

    #region Session Management
    public void ResetSessionStats()
    {
        _currentScore = 0;
        _productScore = 0;
        _qa1Score = 0;
        _qa2Score = 0;
        _bonusScore = 0;
        
        _productsCollected = 0;
        _qa1CorrectSelections = 0;
        _qa2CorrectAnswers = 0;
        _comboCount = 0;
        _multiplierStreak = 0;
        _lastCollectionTime = 0f;
        _sessionStartTime = Time.time;
        
        _progressiveMultiplier = startingMultiplier;
        _qa1CompletionScored = false; // Reset QA1 scoring flag
        
        if (_playerHealth != null)
        {
            _lastHealthValue = _playerHealth.currentHealth;
        }
        
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
        
        Debug.Log("=== SCORING SESSION SUMMARY ===");
        Debug.Log($"Final Score: {_currentScore}");
        Debug.Log($"Session Duration: {FormatTime(sessionDuration)}");
        Debug.Log($"Final Multiplier: {finalMultiplier:F1}x (Max Streak: {_multiplierStreak})");
        Debug.Log($"--- Breakdown ---");
        Debug.Log($"Products: {_productScore} ({_productsCollected}/8 collected)");
        Debug.Log($"QA1: {_qa1Score} ({_qa1CorrectSelections}/5 correct)");
        Debug.Log($"QA2: {_qa2Score} ({_qa2CorrectAnswers}/8 correct)");
        Debug.Log($"Bonuses: {_bonusScore}");
        Debug.Log($"Max Combo: x{_comboCount}");
        Debug.Log($"QA1 Scored This Session: {_qa1CompletionScored}");
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
    [ContextMenu("Test Product Collection")]
    public void DebugTestProductCollection()
    {
        AwardProductPoints("TEST_BANANA", "NaturalSugar");
    }
    
    [ContextMenu("Test QA1 Completion")]
    public void DebugTestQA1Completion()
    {
        AwardQA1Points(5, true);
    }
    
    [ContextMenu("Test QA2 Answer")]
    public void DebugTestQA2Answer()
    {
        AwardQA2Points("TEST_SODA");
    }
    
    [ContextMenu("Test Multiplier Increase")]
    public void DebugTestMultiplierIncrease()
    {
        IncreaseMultiplier();
    }
    
    [ContextMenu("Test Multiplier Reset")]
    public void DebugTestMultiplierReset()
    {
        ResetMultiplier();
    }
    
    [ContextMenu("Debug Scoring State")]
    public void DebugScoringState()
    {
        Debug.Log("=== SCORING SYSTEM STATE ===");
        Debug.Log($"Current Score: {_currentScore}");
        Debug.Log($"Multiplier: {GetCurrentMultiplier():F1}x (Streak: {_multiplierStreak})");
        Debug.Log($"Products: {_productsCollected}/8 collected");
        Debug.Log($"QA1: {_qa1CorrectSelections}/5 (Scored: {_qa1CompletionScored})");
        Debug.Log($"QA2: {_qa2CorrectAnswers}/8");
        Debug.Log($"Combo: x{_comboCount}");
        Debug.Log($"QA1 System Found: {_qa1System != null}");
        
        if (_qa1System != null)
        {
            Debug.Log($"QA1 Active: {_qa1System.IsUIActive()}");
            Debug.Log($"QA1 Selected: {_qa1System.GetSelectedCount()}/{_qa1System.GetMaxSelections()}");
        }
        
        if (_playerHealth != null)
        {
            Debug.Log($"Health: {_playerHealth.currentHealth}/{_playerHealth.maxHealth}");
        }
    }
    
    [ContextMenu("Reset System")]
    public void DebugResetSystem()
    {
        ResetSessionStats();
    }
    
    [ContextMenu("Force QA1 Score")]
    public void DebugForceQA1Score()
    {
        // This simulates a QA1 completion with 5 selections, all added sugar
        AwardQA1Points(5, true);
    }
    
    [ContextMenu("Test Direct QA1 Scoring")]
    public void DebugTestDirectQA1Scoring()
    {
        ScoreQA1Completion(5, true);
    }
    
    [ContextMenu("Test Score Popup")]
    public void DebugTestScorePopup()
    {
        if (enableScorePopups)
        {
            ShowScorePopup(200, qa1ScoreColor, "Test");
            Debug.Log("Test score popup created");
        }
        else
        {
            Debug.Log("Score popups are disabled. Enable them in the Inspector.");
        }
    }
    
    [ContextMenu("Test Multiple Popups")]
    public void DebugTestMultiplePopups()
    {
        ShowScorePopup(100, productScoreColor, "Product");
        ShowScorePopup(250, qa1ScoreColor, "QA1");
        ShowScorePopup(200, qa2ScoreColor, "QA2");
        ShowScorePopup(500, bonusScoreColor, "Bonus");
        Debug.Log("Multiple test popups queued");
    }
    #endregion

    void OnDestroy()
    {
        StopMonitoring();
    }
    
    void OnEnable()
    {
        // Load product database for type checking
        if (_productDatabase == null)
        {
            _productDatabase = Resources.Load<ProductData>("ProductData");
            if (_productDatabase != null)
            {
                Debug.Log("Product database loaded for QA1 type checking");
            }
        }
    }
    
    // NEW: Unsubscribe from events
    void OnDisable()
    {
        // Note: For UnityEvents, we don't need to unsubscribe since they're component-specific
        // If using C# events, unsubscribe here
    }
}