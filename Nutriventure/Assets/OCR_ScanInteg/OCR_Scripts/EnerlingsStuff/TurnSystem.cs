using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TurnSystem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI playerTurnTimerText;
    public TextMeshProUGUI aiTurnTimerText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI gameTimerText;
    public GameObject playerTurnIndicator;
    public GameObject aiTurnIndicator;

    [Header("Turn Settings")]
    public float playerTurnDuration = 10f;
    public float aiTurnDuration = 10f;
    public float animationBufferTime = 0.5f;

    // State
    private TurnState currentTurn = TurnState.Player;
    private float currentTurnTime = 0f;
    private bool isTurnActive = false;
    private bool isAnimating = false;
    private bool isWaitingForAnimation = false;
    private int currentRound = 1;
    private float gameTimer = 0f;
    private bool isGameTimerRunning = false;

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
        InitializeReferences();
        InitializeUI();
    }

    void InitializeReferences()
    {
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleEnerlingManager>();
        
        if (aiManager == null)
            aiManager = FindObjectOfType<AIEnerlingManager>();
        
        if (playerManager == null)
            playerManager = FindObjectOfType<PlayerEnerlingManager>();
        
        Debug.Log($"TurnSystem references: BattleManager={battleManager != null}, AIManager={aiManager != null}, PlayerManager={playerManager != null}");
    }

    // NEW METHOD: Initialize battle with managers
    public void InitializeBattle(BattleEnerlingManager playerBattleManager, AIEnerlingManager aiBattleManager)
    {
        Debug.Log("=== TurnSystem.InitializeBattle() called ===");
        
        this.battleManager = playerBattleManager;
        this.aiManager = aiBattleManager;
        
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerEnerlingManager>();
        }
        
        Debug.Log($"TurnSystem initialized: PlayerBattleManager={battleManager != null}, AIBattleManager={aiManager != null}, PlayerManager={playerManager != null}");
        
        // Reset state
        currentRound = 1;
        gameTimer = 0f;
        isGameTimerRunning = false;
        isTurnActive = false;
        isAnimating = false;
        isWaitingForAnimation = false;
        
        InitializeUI();
    }

    void InitializeUI()
    {
        if (playerTurnTimerText != null)
            playerTurnTimerText.text = playerTurnDuration.ToString("F0");
        
        if (aiTurnTimerText != null)
            aiTurnTimerText.text = aiTurnDuration.ToString("F0");
        
        if (roundText != null)
            roundText.text = $"Round {currentRound}";
        
        if (gameTimerText != null)
            gameTimerText.text = "0:00";
        
        if (playerTurnIndicator != null)
            playerTurnIndicator.SetActive(false);
        
        if (aiTurnIndicator != null)
            aiTurnIndicator.SetActive(false);
        
        if (turnText != null)
        {
            turnText.text = "GET READY!";
            turnText.color = Color.yellow;
        }
    }

    void Update()
    {
        if (isGameTimerRunning)
        {
            gameTimer += Time.deltaTime;
            UpdateGameTimerUI();
        }

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

    void UpdateGameTimerUI()
    {
        if (gameTimerText == null) return;
        
        int minutes = Mathf.FloorToInt(gameTimer / 60f);
        int seconds = Mathf.FloorToInt(gameTimer % 60f);
        gameTimerText.text = $"{minutes}:{seconds:00}";
    }

    void UpdateTurnTimerUI()
    {
        if (currentTurn == TurnState.Player && playerTurnTimerText != null)
        {
            playerTurnTimerText.text = Mathf.Max(0, Mathf.Ceil(currentTurnTime)).ToString();
            playerTurnTimerText.color = currentTurnTime < 3f ? Color.red : Color.white;
        }
        else if (currentTurn == TurnState.AI && aiTurnTimerText != null)
        {
            aiTurnTimerText.text = Mathf.Max(0, Mathf.Ceil(currentTurnTime)).ToString();
            aiTurnTimerText.color = currentTurnTime < 3f ? Color.red : Color.white;
        }
    }

    // UPDATED METHOD: Public method to start battle
    public void StartBattle()
    {
        Debug.Log("=== TurnSystem.StartBattle() called ===");
        
        // Check references
        if (battleManager == null || aiManager == null)
        {
            Debug.LogError("Cannot start battle: Managers not initialized!");
            InitializeReferences();
            
            if (battleManager == null || aiManager == null)
            {
                Debug.LogError("Still missing references after re-initialization!");
                return;
            }
        }
        
        currentRound = 1;
        gameTimer = 0f;
        isGameTimerRunning = true;
        
        Debug.Log($"Starting battle: Player={battleManager.GetBattleEnerling()?.ingredientName}, AI={aiManager.GetAIEnerling()?.ingredientName}");
        
        StartCoroutine(StartBattleSequence());
    }

    IEnumerator StartBattleSequence()
    {
        Debug.Log("Starting battle sequence...");
        
        // Brief pause for dramatic effect
        yield return new WaitForSeconds(0.5f);
        
        if (turnText != null)
        {
            turnText.text = "BATTLE START!";
            turnText.color = Color.red;
        }
        
        yield return new WaitForSeconds(1f);
        
        // Start with player turn
        StartPlayerTurn();
    }

    void StartPlayerTurn()
    {
        Debug.Log("=== STARTING PLAYER TURN ===");
        
        currentTurn = TurnState.Player;
        currentTurnTime = playerTurnDuration;
        isTurnActive = true;
        isAnimating = false;
        isWaitingForAnimation = false;

        if (playerTurnTimerText != null)
            playerTurnTimerText.gameObject.SetActive(true);
        if (aiTurnTimerText != null)
            aiTurnTimerText.gameObject.SetActive(false);

        UpdateTurnUI();

        // Enable player input
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(true);
        }
        else
        {
            Debug.LogWarning("PlayerEnerlingManager not found for enabling controls");
            // Try to find it again
            playerManager = FindObjectOfType<PlayerEnerlingManager>();
            if (playerManager != null)
                playerManager.SetButtonsInteractable(true);
        }

        // Apply beneficial organ heal automatically at the start of player's turn
        ApplyBeneficialOrganHeal(true);

        Debug.Log($"=== PLAYER TURN STARTED - Round {currentRound} ===");
    }

    void StartAITurn()
    {
        Debug.Log("=== STARTING AI TURN ===");
        
        currentTurn = TurnState.AI;
        currentTurnTime = aiTurnDuration;
        isTurnActive = true;
        isAnimating = false;
        isWaitingForAnimation = false;

        if (playerTurnTimerText != null)
            playerTurnTimerText.gameObject.SetActive(false);
        if (aiTurnTimerText != null)
            aiTurnTimerText.gameObject.SetActive(true);

        UpdateTurnUI();

        // Disable player input during AI turn
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        // Apply beneficial organ heal automatically at the start of AI's turn
        ApplyBeneficialOrganHeal(false);

        // Start AI decision making
        if (aiManager != null)
        {
            Debug.Log("Starting AI turn decision...");
            aiManager.StartAITurn();
        }
        else
        {
            Debug.LogError("AI Manager not found for AI turn!");
        }

        Debug.Log($"=== AI TURN STARTED - Round {currentRound} ===");
    }

    void ApplyBeneficialOrganHeal(bool isPlayer)
    {
        Debug.Log($"ApplyBeneficialOrganHeal called for {(isPlayer ? "Player" : "AI")}");
        
        // This applies heal from beneficial organs automatically at the start of each turn
        if (isPlayer)
        {
            // For player
            if (battleManager != null)
            {
                Debug.Log("Calling battleManager.CheckAndApplyOrganHeal()");
                battleManager.CheckAndApplyOrganHeal();
            }
            else
            {
                Debug.LogWarning("battleManager is null in ApplyBeneficialOrganHeal");
            }
        }
        else
        {
            // For AI
            if (aiManager != null)
            {
                Debug.Log("Calling aiManager.CheckAndApplyOrganHeal()");
                aiManager.CheckAndApplyOrganHeal();
            }
            else
            {
                Debug.LogWarning("aiManager is null in ApplyBeneficialOrganHeal");
            }
        }
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

        if (roundText != null)
        {
            roundText.text = $"Round {currentRound}";
        }

        UpdateTurnTimerUI();
    }

    public void OnSkillAnimationStart()
    {
        isAnimating = true;
        isTurnActive = false;
        Debug.Log("Animation started - turn timer paused");
    }

    public void OnSkillAnimationEnd()
    {
        isAnimating = false;
        Debug.Log("Animation completed");

        StartCoroutine(WaitForFeedbackAndEndTurn());
    }

    IEnumerator WaitForFeedbackAndEndTurn()
    {
        isWaitingForAnimation = true;

        yield return new WaitForSeconds(animationBufferTime);

        if (FeedbackManager.Instance != null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        isWaitingForAnimation = false;
        EndCurrentTurn();
    }

    public void PlayerSkillChosen()
    {
        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        Debug.Log("Player skill chosen - processing...");
    }

    void EndCurrentTurn()
    {
        if (isTurnActive) return;

        Debug.Log($"=== {currentTurn} TURN ENDED ===");

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

            StartCoroutine(SwitchToAITurn());
        }
        else if (currentTurn == TurnState.AI)
        {
            if (aiManager != null)
            {
                aiManager.EndTurn();
            }

            StartCoroutine(SwitchToPlayerTurn());
        }
    }

    IEnumerator SwitchToAITurn()
    {
        Debug.Log("Switching to AI turn...");
        yield return new WaitForSeconds(1f);
        StartAITurn();
    }

    IEnumerator SwitchToPlayerTurn()
    {
        Debug.Log("Switching to Player turn...");
        yield return new WaitForSeconds(1f);
        currentRound++;
        StartPlayerTurn();
    }

    public void ForceEndTurn()
    {
        EndCurrentTurn();
    }

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
        return currentRound;
    }

    public void Cleanup()
    {
        StopAllCoroutines();
        isTurnActive = false;
        isAnimating = false;
        isWaitingForAnimation = false;
        isGameTimerRunning = false;
    }
}