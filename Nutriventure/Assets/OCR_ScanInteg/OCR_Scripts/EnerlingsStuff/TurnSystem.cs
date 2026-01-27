using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TurnSystem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI playerTimerText;
    public TextMeshProUGUI aiTimerText;
    public TextMeshProUGUI roundText;
    public GameObject yourTurnIndicator;
    public GameObject aiTurnIndicator;
    public TextMeshProUGUI gameTimerText; // Total game timer

    [Header("Turn Settings")]
    public float turnDuration = 10f;
    public bool playerStartsFirst = true;
    public float aiTimerShowThreshold = 5f; // Show AI timer when below 5 seconds
    public float skillAnimationWaitTime = 1f; // Time to wait for skill animation

    // References
    private PlayerEnerlingManager playerManager;
    private AIEnerlingManager aiManager;
    private BattleEnerlingManager battleManager;

    // Turn state
    private bool isPlayerTurn = true;
    private float currentTurnTime = 0f;
    private float currentGameTime = 0f;
    private int currentRound = 1;
    private bool turnActive = false;
    private bool skillAnimationInProgress = false;
    private bool skillChosenThisTurn = false;
    private Coroutine turnTimerCoroutine;
    private Coroutine gameTimerCoroutine;

    void Start()
    {
        FindManagers();

        // Initialize UI
        if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
        if (aiTurnIndicator != null) aiTurnIndicator.SetActive(false);
        if (playerTimerText != null) playerTimerText.gameObject.SetActive(false);
        if (aiTimerText != null) aiTimerText.gameObject.SetActive(false);

        if (!playerStartsFirst)
        {
            isPlayerTurn = false;
        }

        UpdateTurnUI();
    }

    void FindManagers()
    {
        playerManager = FindObjectOfType<PlayerEnerlingManager>();
        aiManager = FindObjectOfType<AIEnerlingManager>();
        battleManager = FindObjectOfType<BattleEnerlingManager>();
    }

    public void StartBattle()
    {
        currentRound = 1;
        currentGameTime = 0f;
        UpdateRoundUI();
        UpdateGameTimerUI();

        // Start game timer
        if (gameTimerCoroutine != null) StopCoroutine(gameTimerCoroutine);
        gameTimerCoroutine = StartCoroutine(GameTimerRoutine());

        if (isPlayerTurn)
        {
            StartPlayerTurn();
        }
        else
        {
            StartAITurn();
        }
    }

    void StartPlayerTurn()
    {
        Debug.Log($"Round {currentRound}: Player's Turn");

        turnActive = true;
        skillChosenThisTurn = false;
        currentTurnTime = turnDuration;

        // Update UI
        UpdateTurnUI();
        if (yourTurnIndicator != null) yourTurnIndicator.SetActive(true);
        if (aiTurnIndicator != null) aiTurnIndicator.SetActive(false);

        // Show/Hide timers
        if (playerTimerText != null)
        {
            playerTimerText.gameObject.SetActive(true);
            playerTimerText.text = Mathf.CeilToInt(currentTurnTime).ToString();
            playerTimerText.color = Color.white;
        }
        if (aiTimerText != null) aiTimerText.gameObject.SetActive(false);

        // Enable player skill buttons
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(true);
        }

        // Start turn timer (but wait for animation if needed)
        if (turnTimerCoroutine != null) StopCoroutine(turnTimerCoroutine);

        // If we're waiting for an animation, don't start timer yet
        if (skillAnimationInProgress)
        {
            // Timer will start when animation completes
        }
        else
        {
            turnTimerCoroutine = StartCoroutine(PlayerTurnTimer());
        }
    }

    void StartAITurn()
    {
        Debug.Log($"Round {currentRound}: AI's Turn");

        turnActive = true;
        currentTurnTime = turnDuration;

        // Update UI
        UpdateTurnUI();
        if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
        if (aiTurnIndicator != null) aiTurnIndicator.SetActive(true);

        // Show/Hide timers
        if (playerTimerText != null) playerTimerText.gameObject.SetActive(false);
        if (aiTimerText != null)
        {
            aiTimerText.gameObject.SetActive(false); // Hidden initially
            aiTimerText.text = Mathf.CeilToInt(currentTurnTime).ToString();
            aiTimerText.color = Color.white;
        }

        // Disable player controls
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        // Start turn timer
        if (turnTimerCoroutine != null) StopCoroutine(turnTimerCoroutine);

        // If we're waiting for an animation, don't start timer yet
        if (skillAnimationInProgress)
        {
            // Timer will start when animation completes
        }
        else
        {
            turnTimerCoroutine = StartCoroutine(AITurnTimer());
        }
    }

    IEnumerator PlayerTurnTimer()
    {
        // Wait for any ongoing animation
        while (skillAnimationInProgress)
        {
            yield return null;
        }

        // Start the timer
        while (currentTurnTime > 0 && turnActive && !skillChosenThisTurn)
        {
            currentTurnTime -= Time.deltaTime;
            UpdatePlayerTimerUI();

            yield return null;
        }

        // If time's up and no skill was chosen, end turn
        if (turnActive && currentTurnTime <= 0 && !skillChosenThisTurn)
        {
            Debug.Log("Player turn time's up!");
            EndPlayerTurn();
        }
        // If skill was chosen, wait for animation then end turn
        else if (turnActive && skillChosenThisTurn)
        {
            // Wait for animation to complete
            skillAnimationInProgress = true;
            yield return new WaitForSeconds(skillAnimationWaitTime);
            skillAnimationInProgress = false;

            // End turn
            EndPlayerTurn();
        }
    }

    IEnumerator AITurnTimer()
    {
        // Wait for any ongoing animation
        while (skillAnimationInProgress)
        {
            yield return null;
        }

        // Start AI decision after a short delay
        yield return new WaitForSeconds(0.5f);

        if (aiManager != null)
        {
            aiManager.StartAITurn();
        }

        // Start the timer
        while (currentTurnTime > 0 && turnActive)
        {
            currentTurnTime -= Time.deltaTime;
            UpdateAITimerUI();

            yield return null;
        }

        // Time's up - end turn
        if (turnActive)
        {
            EndAITurn();
        }
    }

    IEnumerator GameTimerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            currentGameTime += 1f;
            UpdateGameTimerUI();
        }
    }

    public void PlayerSkillChosen()
    {
        skillChosenThisTurn = true;

        // Start animation wait
        StartCoroutine(WaitForSkillAnimation());
    }

    IEnumerator WaitForSkillAnimation()
    {
        skillAnimationInProgress = true;

        // Wait for animation to complete
        yield return new WaitForSeconds(skillAnimationWaitTime);

        skillAnimationInProgress = false;
        skillChosenThisTurn = false;

        // If it's still player's turn and timer is running, end turn
        if (isPlayerTurn && turnActive)
        {
            EndPlayerTurn();
        }
    }

    public void EndPlayerTurn()
    {
        if (!turnActive) return;

        Debug.Log("Player turn ended");
        turnActive = false;

        // Stop turn timer
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        // Apply end-of-turn effects
        if (playerManager != null)
        {
            playerManager.EndTurn();
        }

        // Process organ effects
        ProcessOrganEffects();

        // Switch to AI turn
        isPlayerTurn = false;
        StartAITurn();
    }

    public void EndAITurn()
    {
        if (!turnActive) return;

        Debug.Log("AI turn ended");
        turnActive = false;

        // Stop turn timer
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        // Apply end-of-turn effects
        if (aiManager != null)
        {
            aiManager.EndTurn();
        }

        // Process organ effects for AI
        ProcessAIOrganEffects();

        // Next round
        currentRound++;
        UpdateRoundUI();

        // Switch to player turn
        isPlayerTurn = true;
        StartPlayerTurn();
    }

    void ProcessOrganEffects()
    {
        // Get player enerling from battle manager
        var playerEnerling = battleManager?.GetBattleEnerling();
        if (playerEnerling == null) return;

        int organCount = playerEnerling.OrganCount;
        if (organCount == 0) return;

        // Calculate bonus based on rarity and organ count
        int bonusPercent = CalculateOrganBonus(playerEnerling.rarity, organCount);

        // Check cooldown for organ bonus (every few turns based on rarity)
        int organCooldown = GetOrganCooldown(playerEnerling.rarity);

        if (currentRound % organCooldown == 0)
        {
            if (playerEnerling.beneficialOrgans.Count > 0)
            {
                // Apply healing bonus: 10 + (bonusPercent * baseLife / 100)
                int healAmount = 10 + Mathf.RoundToInt(playerEnerling.baseLife * (bonusPercent / 100f));
                if (battleManager != null)
                {
                    // Set organ heal bonus for next heal skill
                    battleManager.SetOrganHealBonus(healAmount, playerEnerling.beneficialOrgans);
                }
                Debug.Log($"Player Organ bonus: +{bonusPercent}% healing on next heal (amount: {healAmount})");
            }
            else if (playerEnerling.targetOrgans.Count > 0)
            {
                // Calculate damage bonus: 10 + (bonusPercent * baseDamage / 100)
                int damageBonus = 10 + Mathf.RoundToInt(playerEnerling.baseDamage * (bonusPercent / 100f));
                if (battleManager != null)
                {
                    // Set organ damage bonus for next damage skill
                    battleManager.SetOrganDamageBonus(damageBonus, playerEnerling.targetOrgans);
                }
                Debug.Log($"Player Organ bonus: +{bonusPercent}% damage on next attack (amount: {damageBonus})");
            }
        }
    }

    void ProcessAIOrganEffects()
    {
        // Get AI enerling from AI manager
        if (aiManager == null) return;

        var aiEnerling = aiManager.GetAIEnerling();
        if (aiEnerling == null) return;

        int organCount = aiEnerling.OrganCount;
        if (organCount == 0) return;

        // Calculate bonus based on rarity and organ count
        int bonusPercent = CalculateOrganBonus(aiEnerling.rarity, organCount);

        // Check cooldown for organ bonus (every few turns based on rarity)
        int organCooldown = GetOrganCooldown(aiEnerling.rarity);

        if (currentRound % organCooldown == 0)
        {
            if (aiEnerling.beneficialOrgans.Count > 0)
            {
                // Apply healing bonus to AI
                int healAmount = Mathf.RoundToInt(aiEnerling.baseLife * (bonusPercent / 100f));
                if (aiManager != null)
                {
                    aiManager.SetOrganHealBonus(healAmount, aiEnerling.beneficialOrgans);
                }
                Debug.Log($"AI Organ bonus: +{bonusPercent}% healing on next heal (amount: {healAmount})");
            }
            else if (aiEnerling.targetOrgans.Count > 0)
            {
                // Calculate damage bonus based on base damage
                int damageBonus = Mathf.RoundToInt(aiEnerling.baseDamage * (bonusPercent / 100f));
                if (aiManager != null)
                {
                    aiManager.SetOrganDamageBonus(damageBonus, aiEnerling.targetOrgans);
                }
                Debug.Log($"AI Organ bonus: +{bonusPercent}% damage on next attack (amount: {damageBonus})");
            }
        }
    }

    int CalculateOrganBonus(IngredientDatabase.Rarity rarity, int organCount)
    {
        // 5% per organ
        return organCount * 5;
    }

    int GetOrganCooldown(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common: return 4;
            case IngredientDatabase.Rarity.Rare: return 3;
            case IngredientDatabase.Rarity.UltraRare: return 2;
            default: return 4;
        }
    }

    void UpdatePlayerTimerUI()
    {
        if (playerTimerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTurnTime);
            playerTimerText.text = seconds.ToString();

            // Change color when time is running out
            if (currentTurnTime <= 3f)
            {
                playerTimerText.color = Color.red;
            }
            else
            {
                playerTimerText.color = Color.white;
            }
        }
    }

    void UpdateAITimerUI()
    {
        if (aiTimerText != null)
        {
            int seconds = Mathf.CeilToInt(currentTurnTime);
            aiTimerText.text = seconds.ToString();

            // Show timer only when below threshold
            if (currentTurnTime <= aiTimerShowThreshold)
            {
                aiTimerText.gameObject.SetActive(true);

                // Change color when time is running out
                if (currentTurnTime <= 3f)
                {
                    aiTimerText.color = Color.red;
                }
                else
                {
                    aiTimerText.color = Color.white;
                }
            }
            else
            {
                aiTimerText.gameObject.SetActive(false);
            }
        }
    }

    void UpdateGameTimerUI()
    {
        if (gameTimerText != null)
        {
            int minutes = Mathf.FloorToInt(currentGameTime / 60f);
            int seconds = Mathf.FloorToInt(currentGameTime % 60f);
            gameTimerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void UpdateTurnUI()
    {
        if (turnText != null)
        {
            turnText.text = isPlayerTurn ? "YOUR TURN" : "AI TURN";
        }
    }

    void UpdateRoundUI()
    {
        if (roundText != null)
        {
            roundText.text = $"ROUND {currentRound}";
        }
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    public void Cleanup()
    {
        // Stop all coroutines
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
        }

        StopAllCoroutines();

        turnActive = false;
        skillAnimationInProgress = false;
        skillChosenThisTurn = false;

        // Hide UI indicators
        if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
        if (aiTurnIndicator != null) aiTurnIndicator.SetActive(false);
        if (playerTimerText != null) playerTimerText.gameObject.SetActive(false);
        if (aiTimerText != null) aiTimerText.gameObject.SetActive(false);
    }

    // Helper methods for external access
    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }

    public bool IsTurnActive()
    {
        return turnActive;
    }

    public bool IsSkillAnimationInProgress()
    {
        return skillAnimationInProgress;
    }

    public float GetCurrentTurnTime()
    {
        return currentTurnTime;
    }

    public float GetTotalGameTime()
    {
        return currentGameTime;
    }

    // Call this when a skill animation starts
    public void OnSkillAnimationStart(float animationDuration = 1f)
    {
        skillAnimationInProgress = true;
        skillAnimationWaitTime = animationDuration;
    }

    // Call this when a skill animation ends
    public void OnSkillAnimationEnd()
    {
        skillAnimationInProgress = false;
    }
}