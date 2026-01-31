using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class Kingdom4ScoreManager : MonoBehaviour
{
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnMultiplierChanged;

    public static Kingdom4ScoreManager Instance;

    [Header("Phase 1: Allergen Hunt")]
    [Tooltip("Points for collecting each allergen")]
    public int pointsPerAllergen = 200;

    [Header("Phase 2: Wagon Phase")]
    [Tooltip("Penalty for each allergen hit with the wagon")]
    public int wagonHitPenalty = 75;

    [Header("Phase 3: Platform Phase - Combo System")]
    [Tooltip("Base points for landing on healthy food")]
    public int healthyFoodBasePoints = 100;
    
    [Tooltip("Maximum combo multiplier")]
    public int maxCombo = 8;

    [Header("UI Display References")]
    [Tooltip("Drag your total score TextMeshPro UI here")]
    public TMP_Text scoreText;
    
    [Tooltip("Drag your combo multiplier TextMeshPro UI here")]
    public TMP_Text multiplierText;
    
    [Tooltip("Drag your allergen count TextMeshPro UI here")]
    public TMP_Text allergenCountText;
    
    [Tooltip("Drag your wagon hits TextMeshPro UI here")]
    public TMP_Text wagonHitsText;

    // Score tracking
    public int allergensFound = 0;
    public int totalWagonHits = 0;
    public int comboMultiplier = 1;
    
    private int totalScore = 0;
    public float timeBonus = 0f;
    private bool timeBonusApplied = false;

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
        UpdateAllUI();
    }

    // ---------------- PHASE 1: Allergen Hunt ----------------
    public void AddAllergenFound()
    {
        allergensFound++;
        totalScore += pointsPerAllergen;
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
    }

    // ---------------- PHASE 2: Wagon Phase ----------------
    public void WagonHitAllergen()
    {
        totalWagonHits++;
        totalScore -= wagonHitPenalty;
        totalScore = Mathf.Max(0, totalScore);
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
    }

    // ---------------- PHASE 3: Platform Phase - Combo System ----------------
    public void HitHealthyFood()
    {
        comboMultiplier = Mathf.Clamp(comboMultiplier + 1, 1, maxCombo);
        int gained = healthyFoodBasePoints * comboMultiplier;
        totalScore += gained;
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
        OnMultiplierChanged?.Invoke(comboMultiplier);
    }

    public void HitAllergenInPhase3()
    {
        comboMultiplier = 1;
        
        UpdateAllUI();
        OnMultiplierChanged?.Invoke(comboMultiplier);
    }

    // ---------------- TIME BONUS ----------------
    public void CalculateTimeBonus(float completionTime)
    {
        if (timeBonusApplied) return;

        if (completionTime <= 5 * 60)
            timeBonus = 800;
        else if (completionTime <= 7 * 60)
            timeBonus = 600;
        else if (completionTime <= 10 * 60)
            timeBonus = 400;
        else if (completionTime <= 15 * 60)
            timeBonus = 200;
        else
            timeBonus = 100;

        totalScore += Mathf.RoundToInt(timeBonus);
        timeBonusApplied = true;
        
        UpdateAllUI();
        OnScoreChanged?.Invoke(totalScore);
    }

    // ---------------- UI UPDATES ----------------
    private void UpdateAllUI()
    {
        UpdateScoreText();
        UpdateMultiplierText();
        UpdateAllergenCountText();
        UpdateWagonHitsText();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = totalScore.ToString();
        }
    }

    private void UpdateMultiplierText()
    {
        if (multiplierText != null)
        {
            multiplierText.text = comboMultiplier.ToString();
        }
    }

    private void UpdateAllergenCountText()
    {
        if (allergenCountText != null)
        {
            allergenCountText.text = allergensFound.ToString();
        }
    }

    private void UpdateWagonHitsText()
    {
        if (wagonHitsText != null)
        {
            wagonHitsText.text = totalWagonHits.ToString();
        }
    }

    // ---------------- PUBLIC METHODS ----------------
    public void ResetScore()
    {
        allergensFound = 0;
        totalWagonHits = 0;
        comboMultiplier = 1;
        totalScore = 0;
        timeBonusApplied = false;
        
        UpdateAllUI();
        OnMultiplierChanged?.Invoke(comboMultiplier);
        OnScoreChanged?.Invoke(totalScore);
    }

    public int GetFinalScore()
    {
        return totalScore;
    }

    // Helper method for AllerthriaGameManager
    public void UpdateAllUIManually()
    {
        UpdateAllUI();
    }
}