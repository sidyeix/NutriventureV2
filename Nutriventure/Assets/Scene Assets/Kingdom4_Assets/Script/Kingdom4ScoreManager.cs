using UnityEngine;
using UnityEngine.Events;


public class Kingdom4ScoreManager : MonoBehaviour
{
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnMultiplierChanged;


    public static Kingdom4ScoreManager Instance;

    [Header("Phase 1")]
    public int allergensFound = 0;
    public int pointsPerAllergen = 100;

    [Header("Phase 2")]
    public int wagonHitPenalty = 50;
    public int totalWagonHits = 0;

    [Header("Phase 3 - Combo System")]
    public int healthyFoodBasePoints = 50;
    public int comboMultiplier = 1;
    public int maxCombo = 5;

    private int totalScore = 0;
    private float timeBonus = 0f;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ---------------- PHASE 1 ----------------
    public void AddAllergenFound()
{
    allergensFound++;
    totalScore += pointsPerAllergen;

    OnScoreChanged?.Invoke(totalScore);
}


    // ---------------- PHASE 2 ----------------
    public void WagonHitAllergen()
{
    totalWagonHits++;
    totalScore -= wagonHitPenalty;
    totalScore = Mathf.Max(0, totalScore);

    OnScoreChanged?.Invoke(totalScore);
}


    // ---------------- PHASE 3 ----------------
    public void HitHealthyFood()
{
    comboMultiplier = Mathf.Clamp(comboMultiplier + 1, 1, maxCombo);
    int gained = healthyFoodBasePoints * comboMultiplier;
    totalScore += gained;

    OnScoreChanged?.Invoke(totalScore);
    OnMultiplierChanged?.Invoke(comboMultiplier);
}


    public void HitAllergenInPhase3()
    {
        comboMultiplier = 1; // reset combo
        OnMultiplierChanged?.Invoke(comboMultiplier);
    }

    // ---------------- TIME BONUS ----------------
   private bool timeBonusApplied = false;

public void CalculateTimeBonus(float completionTime)
{
    if (timeBonusApplied) return;

    if (completionTime <= 10 * 60)
        timeBonus = 500;
    else if (completionTime <= 15 * 60)
        timeBonus = 300;
    else
        timeBonus = 100;

    totalScore += Mathf.RoundToInt(timeBonus);
    timeBonusApplied = true;

    OnScoreChanged?.Invoke(totalScore);
}


public void ResetScore()
{
    allergensFound = 0;
    totalWagonHits = 0;
    comboMultiplier = 1;
    totalScore = 0;
    timeBonusApplied = false;
    comboMultiplier = 1;
    OnMultiplierChanged?.Invoke(comboMultiplier);
}


    public int GetFinalScore()
{
    return totalScore;
}
}
