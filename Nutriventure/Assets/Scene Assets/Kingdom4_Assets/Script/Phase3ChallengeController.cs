using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Orchestrates the full Phase 3 allergen challenge sequence across 5 big rocks.
/// Tracks per-rock results, awards points via <see cref="Kingdom4ScoreManager"/>,
/// and notifies <see cref="AllerthriaGameManager"/> when all rocks are complete.
/// </summary>
public class Phase3ChallengeController : MonoBehaviour
{
    public static Phase3ChallengeController Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Total number of big-rock challenges in Phase 3.")]
    public int totalRocks = 5;

    [Header("Scoring")]
    [Tooltip("Points awarded for each correct (safe) path choice.")]
    public int pointsPerCorrectChoice = 150;
    [Tooltip("Bonus points awarded if the player answers all 5 rocks correctly.")]
    public int bonusAllCorrect = 500;

    [Header("NPC References")]
    [Tooltip("Assign each AllergenNPC (one per big rock) in order here.")]
    public List<AllergenNPC> allergenNPCs = new List<AllergenNPC>();

    [Header("Summary UI")]
    [Tooltip("Panel shown after all 5 rocks are completed.")]
    public GameObject summaryPanel;
    [Tooltip("Text element that displays the per-rock results and final score.")]
    public TextMeshProUGUI summaryText;
    [Tooltip("Button the player presses to leave the summary and continue.")]
    public Button continueButton;

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Fired after each rock challenge. (rockIndex, wasCorrect)</summary>
    public event Action<int, bool> OnRockChallengeResult;
    /// <summary>Fired when all 5 rocks are complete. (correctCount, totalRocks)</summary>
    public event Action<int, int> OnPhase3Complete;

    // ── State ─────────────────────────────────────────────────────────────
    private int correctCount  = 0;
    private int completedCount = 0;
    private bool[] results;

    // ── Unity ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        results = new bool[totalRocks];

        if (summaryPanel   != null) summaryPanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="AllergenNPC"/> after the player completes a single rock challenge.
    /// </summary>
    /// <param name="rockIndex">Zero-based index of the rock (0–4).</param>
    /// <param name="correct">True if the player chose a safe path.</param>
    public void OnChallengeCompleted(int rockIndex, bool correct)
    {
        if (rockIndex < 0 || rockIndex >= totalRocks)
        {
            Debug.LogWarning($"[Phase3ChallengeController] Invalid rockIndex: {rockIndex}");
            return;
        }

        results[rockIndex] = correct;
        completedCount++;

        if (correct)
        {
            correctCount++;
            AwardPoints(pointsPerCorrectChoice);
            Debug.Log($"[Phase3] Rock {rockIndex + 1}: Correct! (+{pointsPerCorrectChoice} pts)");
        }
        else
        {
            Debug.Log($"[Phase3] Rock {rockIndex + 1}: Wrong path chosen.");
        }

        OnRockChallengeResult?.Invoke(rockIndex, correct);

        if (completedCount >= totalRocks)
            CompletePhase3();
    }

    /// <summary>Resets all state so the challenge can be played again.</summary>
    public void ResetChallenge()
    {
        correctCount   = 0;
        completedCount = 0;
        results        = new bool[totalRocks];

        if (summaryPanel != null) summaryPanel.SetActive(false);

        foreach (AllergenNPC npc in allergenNPCs)
        {
            if (npc != null) npc.ResetNPC();
        }

        Debug.Log("[Phase3ChallengeController] Challenge reset.");
    }

    public int GetCorrectCount()   => correctCount;
    public int GetCompletedCount() => completedCount;
    public bool IsComplete()       => completedCount >= totalRocks;

    // ── Internal ──────────────────────────────────────────────────────────

    private void AwardPoints(int points)
    {
        if (Kingdom4ScoreManager.Instance != null)
            Kingdom4ScoreManager.Instance.AddPhase3AllergenChallengePoints(points);
    }

    private void CompletePhase3()
    {
        // Award all-correct bonus
        if (correctCount >= totalRocks)
        {
            AwardPoints(bonusAllCorrect);
            Debug.Log($"[Phase3] All correct! Bonus +{bonusAllCorrect} pts.");
        }

        ShowSummary();

        OnPhase3Complete?.Invoke(correctCount, totalRocks);

        Debug.Log($"[Phase3ChallengeController] Complete – {correctCount}/{totalRocks} correct.");

        // Notify the game manager so it can advance to the next game phase
        if (AllerthriaGameManager.Instance != null)
            AllerthriaGameManager.Instance.CompleteAllergenChallenge();
    }

    private void ShowSummary()
    {
        if (summaryPanel == null) return;

        summaryPanel.SetActive(true);

        if (summaryText != null)
        {
            string resultsStr = string.Empty;
            for (int i = 0; i < totalRocks; i++)
            {
                string status = (i < completedCount)
                    ? (results[i] ? "✓ Safe" : "✗ Wrong")
                    : "–";
                resultsStr += $"Rock {i + 1}: {status}\n";
            }

            int earnedPoints = correctCount * pointsPerCorrectChoice
                             + (correctCount >= totalRocks ? bonusAllCorrect : 0);

            summaryText.text =
                $"Allergen Challenge Complete!\n\n" +
                $"{resultsStr}\n" +
                $"Score: {correctCount}/{totalRocks}  (+{earnedPoints} pts)";
        }
    }

    private void OnContinueClicked()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }
}
