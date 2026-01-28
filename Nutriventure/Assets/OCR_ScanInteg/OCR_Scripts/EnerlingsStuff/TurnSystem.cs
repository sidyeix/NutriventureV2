using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TurnSystem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public Slider turnTimerSlider;
    public TextMeshProUGUI turnTimerText;
    public GameObject playerTurnIndicator;
    public GameObject aiTurnIndicator;

    [Header("Turn Settings")]
    public float playerTurnDuration = 30f;
    public float aiTurnDuration = 30f;
    public float animationBufferTime = 0.5f;

    // State
    private TurnState currentTurn = TurnState.Player;
    private float currentTurnTime = 0f;
    private bool isTurnActive = false;
    private bool isAnimating = false;
    private bool isWaitingForAnimation = false;

    // References
    private BattleEnerlingManager battleManager;
    private AIEnerlingManager aiManager;
    private PlayerEnerlingManager playerManager;

    public enum TurnState
    {
        Player,
        AI
    }

    void Start()
    {
        battleManager = FindObjectOfType<BattleEnerlingManager>();
        aiManager = FindObjectOfType<AIEnerlingManager>();
        playerManager = FindObjectOfType<PlayerEnerlingManager>();
    }

    void Update()
    {
        if (isTurnActive && !isAnimating && !isWaitingForAnimation)
        {
            currentTurnTime -= Time.deltaTime;
            UpdateTurnTimerUI();

            if (currentTurnTime <= 0f)
            {
                EndCurrentTurn();
            }
        }
    }

    // Call this when battle starts
    public void StartBattle()
    {
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        currentTurn = TurnState.Player;
        currentTurnTime = playerTurnDuration;
        isTurnActive = true;
        isAnimating = false;
        isWaitingForAnimation = false;

        UpdateTurnUI();

        // Enable player input
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(true);
        }

        Debug.Log("=== PLAYER TURN STARTED ===");
    }

    void StartAITurn()
    {
        currentTurn = TurnState.AI;
        currentTurnTime = aiTurnDuration;
        isTurnActive = true;
        isAnimating = false;
        isWaitingForAnimation = false;

        UpdateTurnUI();

        // Disable player input during AI turn
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        // Start AI decision making
        if (aiManager != null)
        {
            aiManager.StartAITurn();
        }

        Debug.Log("=== AI TURN STARTED ===");
    }

    void UpdateTurnUI()
    {
        if (turnText != null)
        {
            turnText.text = currentTurn == TurnState.Player ? "PLAYER TURN" : "AI TURN";
            turnText.color = currentTurn == TurnState.Player ? Color.green : Color.red;
        }

        if (playerTurnIndicator != null)
        {
            playerTurnIndicator.SetActive(currentTurn == TurnState.Player);
        }

        if (aiTurnIndicator != null)
        {
            aiTurnIndicator.SetActive(currentTurn == TurnState.AI);
        }

        UpdateTurnTimerUI();
    }

    void UpdateTurnTimerUI()
    {
        if (turnTimerSlider != null)
        {
            turnTimerSlider.maxValue = currentTurn == TurnState.Player ? playerTurnDuration : aiTurnDuration;
            turnTimerSlider.value = currentTurnTime;
        }

        if (turnTimerText != null)
        {
            turnTimerText.text = Mathf.CeilToInt(currentTurnTime).ToString();
            turnTimerText.color = currentTurnTime < 10f ? Color.red : Color.white;
        }
    }

    // Called when skill animation starts
    public void OnSkillAnimationStart()
    {
        isAnimating = true;
        Debug.Log($"Animation started - turn timer paused");
    }

    // Called when skill animation ends
    public void OnSkillAnimationEnd()
    {
        isAnimating = false;
        Debug.Log("Animation completed");

        // Start a coroutine to wait for feedback before ending turn
        StartCoroutine(WaitForFeedbackAndEndTurn());
    }

    IEnumerator WaitForFeedbackAndEndTurn()
    {
        isWaitingForAnimation = true;

        // Wait for feedback to display
        yield return new WaitForSeconds(animationBufferTime);

        // Additional wait if FeedbackManager is processing
        if (FeedbackManager.Instance != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        isWaitingForAnimation = false;

        // End the current turn
        EndCurrentTurn();
    }

    // Called from BattleEnerlingManager when player finishes action
    public void PlayerSkillChosen()
    {
        // Disable player input immediately
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        Debug.Log("Player skill chosen - processing...");
    }

    void EndCurrentTurn()
    {
        if (!isTurnActive) return;

        isTurnActive = false;

        Debug.Log($"=== {currentTurn} TURN ENDED ===");

        // Process end turn effects
        if (currentTurn == TurnState.Player)
        {
            if (playerManager != null)
            {
                playerManager.EndTurn();
            }

            if (battleManager != null)
            {
                battleManager.ProcessEndTurn();
            }

            // Start AI turn after delay
            StartCoroutine(SwitchToAITurn());
        }
        else if (currentTurn == TurnState.AI)
        {
            if (aiManager != null)
            {
                aiManager.EndTurn();
            }

            if (aiManager != null)
            {
                aiManager.ProcessEndTurn();
            }

            // Start Player turn after delay
            StartCoroutine(SwitchToPlayerTurn());
        }
    }

    IEnumerator SwitchToAITurn()
    {
        yield return new WaitForSeconds(1f); // Brief pause between turns
        StartAITurn();
    }

    IEnumerator SwitchToPlayerTurn()
    {
        yield return new WaitForSeconds(1f); // Brief pause between turns
        StartPlayerTurn();
    }

    // Public method to manually end turn (for testing)
    public void ForceEndTurn()
    {
        EndCurrentTurn();
    }

    // Get current turn state
    public TurnState GetCurrentTurn()
    {
        return currentTurn;
    }

    public bool IsPlayerTurn()
    {
        return currentTurn == TurnState.Player;
    }

    public bool IsAITurn()
    {
        return currentTurn == TurnState.AI;
    }

    public bool IsAnimating()
    {
        return isAnimating || isWaitingForAnimation;
    }

    public int GetCurrentRound()
    {
        // Simple round counter - you might want to implement this properly
        return 1;
    }

    public void Cleanup()
    {
        StopAllCoroutines();
        isTurnActive = false;
        isAnimating = false;
        isWaitingForAnimation = false;
    }
}