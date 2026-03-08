using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles a single allergen rock challenge where the player picks one of three paths.
/// One path hides the NPC's dangerous allergen; the other two are safe.
/// </summary>
public class RockChallenge : MonoBehaviour
{
    /// <summary>Fired when the player makes a choice. bool = true if the choice was safe.</summary>
    public event Action<bool> OnChoiceMade;

    [Header("Rock GameObjects (optional visual references)")]
    public GameObject leftRock;
    public GameObject middleRock;
    public GameObject rightRock;

    [Header("Challenge UI")]
    [Tooltip("Root panel that contains the three choice buttons")]
    public GameObject challengeUI;
    public Button leftButton;
    public Button middleButton;
    public Button rightButton;

    [Header("Path Labels")]
    [Tooltip("Label shown on the left choice button")]
    public TextMeshProUGUI leftLabel;
    [Tooltip("Label shown on the middle choice button")]
    public TextMeshProUGUI middleLabel;
    [Tooltip("Label shown on the right choice button")]
    public TextMeshProUGUI rightLabel;

    [Header("Result UI")]
    [Tooltip("Panel that reveals the result after the player chooses")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Settings")]
    [Tooltip("How long (seconds) the result panel is shown before hiding")]
    public float resultDisplayDuration = 2f;

    // 0 = left, 1 = middle, 2 = right
    private AllergenProductData.AllergenType[] rockAllergens = new AllergenProductData.AllergenType[3];
    private AllergenProductData.AllergenType dangerousAllergen;
    private bool isActive = false;

    void Awake()
    {
        if (challengeUI != null) challengeUI.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (leftButton != null)   leftButton.onClick.AddListener(() => OnRockChosen(0));
        if (middleButton != null) middleButton.onClick.AddListener(() => OnRockChosen(1));
        if (rightButton != null)  rightButton.onClick.AddListener(() => OnRockChosen(2));
    }

    /// <summary>
    /// Assigns allergens to the three rocks. Call this before <see cref="ShowChallenge"/>.
    /// Exactly one rock will contain the <paramref name="dangerous"/> allergen;
    /// the other two will contain randomly selected safe allergens.
    /// </summary>
    public void SetupChallenge(AllergenProductData.AllergenType dangerous)
    {
        dangerousAllergen = dangerous;

        List<AllergenProductData.AllergenType> safeAllergens = AllergenManager.GetSafeAllergens(dangerous, 2);

        // Randomly place the dangerous allergen in one of the three slots
        int dangerousIndex = UnityEngine.Random.Range(0, 3);
        int safeIndex = 0;
        for (int i = 0; i < 3; i++)
        {
            rockAllergens[i] = (i == dangerousIndex)
                ? dangerous
                : safeAllergens[safeIndex++];
        }

        // Reset button labels so allergen identities are hidden until reveal
        if (leftLabel   != null) leftLabel.text   = "Left";
        if (middleLabel != null) middleLabel.text = "Middle";
        if (rightLabel  != null) rightLabel.text  = "Right";

        SetButtonsInteractable(true);
    }

    /// <summary>Makes the choice UI visible and starts accepting player input.</summary>
    public void ShowChallenge()
    {
        isActive = true;
        if (challengeUI != null) challengeUI.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    /// <summary>Hides the challenge UI entirely.</summary>
    public void HideChallenge()
    {
        isActive = false;
        if (challengeUI != null) challengeUI.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void OnRockChosen(int index)
    {
        if (!isActive) return;

        isActive = false;
        SetButtonsInteractable(false);

        bool isCorrect = rockAllergens[index] != dangerousAllergen;
        ShowResult(isCorrect, rockAllergens[index]);

        StartCoroutine(FinishWithDelay(isCorrect));
    }

    private void ShowResult(bool correct, AllergenProductData.AllergenType chosenAllergen)
    {
        // Reveal allergen names on all path labels
        if (leftLabel   != null) leftLabel.text   = AllergenManager.GetDisplayName(rockAllergens[0]);
        if (middleLabel != null) middleLabel.text = AllergenManager.GetDisplayName(rockAllergens[1]);
        if (rightLabel  != null) rightLabel.text  = AllergenManager.GetDisplayName(rockAllergens[2]);

        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultText != null)
        {
            string allergenName = AllergenManager.GetDisplayName(chosenAllergen);
            resultText.text = correct
                ? $"Safe! This path had {allergenName}."
                : $"Danger! This path had {allergenName}!";
            resultText.color = correct ? Color.green : Color.red;
        }
    }

    private IEnumerator FinishWithDelay(bool correct)
    {
        yield return new WaitForSeconds(resultDisplayDuration);
        HideChallenge();
        SetButtonsInteractable(true);
        OnChoiceMade?.Invoke(correct);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (leftButton   != null) leftButton.interactable   = value;
        if (middleButton != null) middleButton.interactable = value;
        if (rightButton  != null) rightButton.interactable  = value;
    }
}
