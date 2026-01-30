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

    private TurnState currentTurn = TurnState.Player;
    private float currentTurnTime = 0f;
    private bool isTurnActive = false;
    private bool isAnimating = false;
    private bool isWaitingForAnimation = false;
    private int currentRound = 1;
    private float gameTimer = 0f;
    private bool isGameTimerRunning = false;

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

    public void InitializeBattle(BattleEnerlingManager playerBattleManager, AIEnerlingManager aiBattleManager)
    {
        Debug.Log("=== TurnSystem.InitializeBattle() called ===");

        this.battleManager = playerBattleManager;
        this.aiManager = aiBattleManager;

        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerEnerlingManager>();
        }

        Debug.Log($"TurnSystem initialized: PlayerBattleManager={battleManager != null}, AIBattleManager={aiManager != null}");

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

    public void StartBattle()
    {
        Debug.Log("=== TurnSystem.StartBattle() called ===");

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

        yield return new WaitForSeconds(0.5f);

        if (turnText != null)
        {
            turnText.text = "BATTLE START!";
            turnText.color = Color.red;
        }

        yield return new WaitForSeconds(1f);

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

        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(true);
            playerManager.UpdateAllSkillButtons(); // Update skill buttons
        }
        else
        {
            Debug.LogWarning("PlayerEnerlingManager not found for enabling controls");
            playerManager = FindObjectOfType<PlayerEnerlingManager>();
            if (playerManager != null)
            {
                playerManager.SetButtonsInteractable(true);
                playerManager.UpdateAllSkillButtons();
            }
        }

        // Apply beneficial organ heal for PLAYER at start of player turn
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

        if (playerManager != null)
        {
            playerManager.SetButtonsInteractable(false);
        }

        // Apply beneficial organ heal for AI at start of AI turn
        ApplyBeneficialOrganHeal(false);

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

        if (isPlayer)
        {
            // Apply beneficial organ heal for PLAYER
            if (battleManager != null)
            {
                var playerEnerling = battleManager.GetBattleEnerling();
                if (playerEnerling != null && playerEnerling.beneficialOrgans.Count > 0)
                {
                    Debug.Log($"Player has {playerEnerling.beneficialOrgans.Count} beneficial organs - applying heal");
                    battleManager.CheckAndApplyOrganHeal(); // This will heal the player
                }
                else if (playerEnerling != null)
                {
                    Debug.Log($"Player has no beneficial organs ({playerEnerling.beneficialOrgans.Count}) - no heal applied");
                }
            }
            else
            {
                Debug.LogWarning("battleManager is null in ApplyBeneficialOrganHeal");
            }
        }
        else
        {
            // Apply beneficial organ heal for AI
            if (aiManager != null)
            {
                var aiEnerling = aiManager.GetAIEnerling();
                if (aiEnerling != null && aiEnerling.beneficialOrgans.Count > 0)
                {
                    Debug.Log($"AI has {aiEnerling.beneficialOrgans.Count} beneficial organs - applying heal");
                    aiManager.CheckAndApplyOrganHeal(); // This will heal the AI
                }
                else if (aiEnerling != null)
                {
                    Debug.Log($"AI has no beneficial organs ({aiEnerling.beneficialOrgans.Count}) - no heal applied");
                }
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