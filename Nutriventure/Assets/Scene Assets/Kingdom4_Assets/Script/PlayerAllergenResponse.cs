using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Optional auxiliary component that provides an independent UI layer for player
/// allergen-path responses. Can be wired directly to a <see cref="RockChallenge"/>
/// or used to display per-choice feedback independently of the core challenge flow.
/// </summary>
public class PlayerAllergenResponse : MonoBehaviour
{
    [Header("Rock Challenge (optional direct link)")]
    [Tooltip("If assigned, this component's buttons forward clicks to the linked RockChallenge.")]
    public RockChallenge rockChallenge;

    [Header("Path Selection Buttons")]
    public Button leftPathButton;
    public Button middlePathButton;
    public Button rightPathButton;

    [Header("Path Labels")]
    public TextMeshProUGUI leftPathLabel;
    public TextMeshProUGUI middlePathLabel;
    public TextMeshProUGUI rightPathLabel;

    [Header("Feedback")]
    [Tooltip("Text element used to display correct / wrong feedback to the player.")]
    public TextMeshProUGUI feedbackText;
    [Tooltip("How long (seconds) the feedback message stays on screen.")]
    public float feedbackDuration = 2f;

    private Coroutine feedbackCoroutine;

    void Awake()
    {
        if (leftPathButton   != null) leftPathButton.onClick.AddListener(OnLeftChosen);
        if (middlePathButton != null) middlePathButton.onClick.AddListener(OnMiddleChosen);
        if (rightPathButton  != null) rightPathButton.onClick.AddListener(OnRightChosen);

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        // Subscribe to result events from the linked RockChallenge so this component
        // can show its own feedback overlay independently.
        if (rockChallenge != null)
            rockChallenge.OnChoiceMade += OnChoiceMade;
    }

    void OnDestroy()
    {
        if (rockChallenge != null)
            rockChallenge.OnChoiceMade -= OnChoiceMade;
    }

    // ── Button handlers ───────────────────────────────────────────────────

    private void OnLeftChosen()   => LogChoice(0);
    private void OnMiddleChosen() => LogChoice(1);
    private void OnRightChosen()  => LogChoice(2);

    private void LogChoice(int pathIndex)
    {
        string[] names = { "Left", "Middle", "Right" };
        Debug.Log($"[PlayerAllergenResponse] Player chose path: {names[pathIndex]}");
    }

    // ── Result feedback ───────────────────────────────────────────────────

    private void OnChoiceMade(bool correct)
    {
        // feedbackText is optional; ShowFeedback can also be called externally
        ShowFeedback(correct, correct ? "safe path" : "dangerous path");
    }

    /// <summary>
    /// Displays a timed feedback message. Can be called from external scripts.
    /// </summary>
    /// <param name="correct">Whether the player's choice was safe.</param>
    /// <param name="allergenName">Name of the allergen on the chosen rock.</param>
    public void ShowFeedback(bool correct, string allergenName)
    {
        if (feedbackText == null) return;

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackText.text  = correct
            ? $"Correct! {allergenName} is safe."
            : $"Wrong! {allergenName} was dangerous!";
        feedbackText.color = correct ? Color.green : Color.red;
        feedbackText.gameObject.SetActive(true);

        feedbackCoroutine = StartCoroutine(HideFeedbackAfterDelay());
    }

    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Updates the three path label texts.</summary>
    public void SetPathLabels(string left, string middle, string right)
    {
        if (leftPathLabel   != null) leftPathLabel.text   = left;
        if (middlePathLabel != null) middlePathLabel.text = middle;
        if (rightPathLabel  != null) rightPathLabel.text  = right;
    }
}
