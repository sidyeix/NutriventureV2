using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Placed on each of the 5 big rock areas in Phase 3.
/// When the player enters the trigger zone the NPC announces its allergen allergy,
/// then hands control to the associated <see cref="RockChallenge"/>.
/// </summary>
public class AllergenNPC : MonoBehaviour
{
    [Header("NPC Model")]
    [Tooltip("The NPC character GameObject to show/hide.")]
    public GameObject npcModel;

    [Header("Announcement UI")]
    [Tooltip("Root panel shown when the NPC announces its allergen.")]
    public GameObject announcementPanel;
    [Tooltip("Main announcement text (filled at runtime).")]
    public TextMeshProUGUI announcementText;
    [Tooltip("Highlighted allergen name text (filled at runtime).")]
    public TextMeshProUGUI allergenNameText;
    [Tooltip("Button the player presses to proceed to the path choice.")]
    public Button proceedButton;

    [Header("Rock Challenge")]
    [Tooltip("The RockChallenge component managing the three-path choice for this big rock.")]
    public RockChallenge rockChallenge;

    [Header("Phase Controller")]
    [Tooltip("The Phase3ChallengeController that tracks overall progress across all 5 rocks.")]
    public Phase3ChallengeController challengeController;

    [Header("Settings")]
    [Tooltip("Zero-based index of this rock (0–4) used for reporting results.")]
    public int challengeIndex = 0;

    private AllergenProductData.AllergenType selectedAllergen;
    private bool hasBeenTriggered = false;

    private Coroutine autoProceedCoroutine;
    private bool hasProceedStarted = false;

    void Awake()
    {
        if (announcementPanel != null) announcementPanel.SetActive(false);
        // Button is optional — auto-proceed handles progression
        if (proceedButton != null) proceedButton.onClick.AddListener(OnProceedClicked);

        if (rockChallenge != null)
            rockChallenge.OnChoiceMade += OnChallengeResult;
    }

    void OnDestroy()
    {
        if (rockChallenge != null)
            rockChallenge.OnChoiceMade -= OnChallengeResult;
    }

    // ── Trigger ───────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered || !other.CompareTag("Player")) return;

        hasBeenTriggered = true;
        TriggerChallenge();
    }

    // ── Challenge Flow ────────────────────────────────────────────────────

    private void TriggerChallenge()
    {
        // Randomly pick one of the 9 major allergens
        selectedAllergen = AllergenManager.GetRandomAllergen();

        // Prepare the three-path rock challenge with the chosen allergen
        if (rockChallenge != null)
            rockChallenge.SetupChallenge(selectedAllergen);

        ShowAnnouncement();
    }

    private void ShowAnnouncement()
    {
        if (announcementPanel == null)
        {
            // No announcement panel — jump straight to the challenge
            ProceedToChallenge();
            return;
        }

        string allergenName = AllergenManager.GetDisplayName(selectedAllergen);
        string description  = AllergenManager.GetDescription(selectedAllergen);

        if (announcementText != null)
            announcementText.text =
                $"I am allergic to {allergenName}!\n" +
                $"{description}\n" +
                $"Choose a safe path!";

        if (allergenNameText != null)
            allergenNameText.text = allergenName;

        announcementPanel.SetActive(true);

        // Auto-proceed after 3 seconds — no button click required
        hasProceedStarted = false;
        if (autoProceedCoroutine != null) StopCoroutine(autoProceedCoroutine);
        autoProceedCoroutine = StartCoroutine(AutoProceedAfterDelay(3f));
    }

    private IEnumerator AutoProceedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProceedToChallenge();
    }

    private void OnProceedClicked()
    {
        // Stop the auto-proceed coroutine if the player clicks early
        if (autoProceedCoroutine != null)
        {
            StopCoroutine(autoProceedCoroutine);
            autoProceedCoroutine = null;
        }
        ProceedToChallenge();
    }

    private void ProceedToChallenge()
    {
        if (hasProceedStarted) return;
        hasProceedStarted = true;

        if (announcementPanel != null) announcementPanel.SetActive(false);

        if (rockChallenge != null)
            rockChallenge.ShowChallenge();
    }

    private void OnChallengeResult(bool correct)
    {
        if (challengeController != null)
            challengeController.OnChallengeCompleted(challengeIndex, correct);
    }

    // ── Public ────────────────────────────────────────────────────────────

    /// <summary>Resets this NPC so it can be triggered again (e.g. after a game restart).</summary>
    public void ResetNPC()
    {
        hasBeenTriggered = false;
        hasProceedStarted = false;
        if (autoProceedCoroutine != null)
        {
            StopCoroutine(autoProceedCoroutine);
            autoProceedCoroutine = null;
        }
        if (announcementPanel != null) announcementPanel.SetActive(false);
        if (rockChallenge     != null) rockChallenge.HideChallenge();
    }

    public AllergenProductData.AllergenType GetSelectedAllergen() => selectedAllergen;
}
