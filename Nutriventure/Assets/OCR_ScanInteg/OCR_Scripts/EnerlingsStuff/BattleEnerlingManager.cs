using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleEnerlingManager : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("Canvas References")]
    public GameObject selectionCanvas;
    public GameObject battlefieldCanvas;

    [Header("New Managers")]
    public PlayerEnerlingManager playerEnerlingManager;
    public AIEnerlingManager aiEnerlingManager;
    public TurnSystem turnSystem;

    [Header("UI References - Player Battlefield Info")]
    public TextMeshProUGUI battlefieldEnerlingName;
    public Slider battlefieldHealthSlider;
    public TextMeshProUGUI healthText;
    public Slider battlefieldArmorSlider;
    public TextMeshProUGUI armorText;
    public Image battlefieldFrame;
    public Image rarityTag;
    public Image enerlingImage;
    public TextMeshProUGUI abilityText;
    public Transform organPanel;
    public GameObject organImagePrefab;
    public Image nameStatsBG;

    [Header("NameStats BG Sprites by Rarity")]
    public Sprite commonNameStatsBG;
    public Sprite rareNameStatsBG;
    public Sprite ultraRareNameStatsBG;

    [Header("Enerling Spawning")]
    public Transform enerlingSpawningPoint;
    public Transform aiSpawningPoint;

    // Current battle enerling
    private IngredientDatabase.IngredientInfo battleEnerling;
    private GameObject spawnedEnerling;
    private Animator enerlingAnimator;

    // Defend tracking
    private int currentArmor = 0;
    private int activeDefend = 0;
    private bool hasDefend = false;

    // Skill tracking
    private bool isAnimating = false;

    // Organ cooldown tracking
    private int organCooldownTimer = 0;
    private int maxOrganCooldown = 4; // Updated: Common=4, Rare=3, UltraRare=2
    private bool organCooldownReady = false;

    // Reference to selection manager
    private EnerlingSelectionManager selectionManager;

    // UI animation
    private Coroutine healthAnimationCoroutine;
    private Coroutine armorAnimationCoroutine;

    private int organBonusDamage = 0;
    private int organBonusHeal = 0;
    private bool hasOrganDamageBonus = false;
    private bool hasOrganHealBonus = false;
    private List<string> organBonusNames = new List<string>();

    void Awake()
    {
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<EnerlingSelectionManager>();
        }

        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(false);
        }

        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(true);
        }
    }

    // ==================== NEW METHOD: Start battle with existing enerlings ====================
    public void StartBattleWithExistingEnerlings(string playerEnerlingName, GameObject existingPlayerEnerling, GameObject existingOpponentEnerling)
    {
        Debug.Log($"=== BATTLEENERLINGMANAGER: Starting battle with existing enerlings ===");

        // 1. Switch to battlefield WITHOUT spawning new enerling
        SwitchToBattlefieldWithExistingEnerling(playerEnerlingName, existingPlayerEnerling);

        // 2. Initialize AI with existing enerling
        InitializeAIOpponentWithExisting(existingOpponentEnerling);

        // 3. Start the battle systems
        StartBattle();

        Debug.Log($"=== BATTLE STARTED WITH EXISTING ENERLINGS ===");
    }

    // ==================== NEW METHOD: Switch to battlefield with existing enerling ====================
    public void SwitchToBattlefieldWithExistingEnerling(string playerEnerlingName, GameObject existingPlayerEnerling)
    {
        Debug.Log($"Switching to battlefield with existing enerling: {playerEnerlingName}");

        if (selectionCanvas != null)
            selectionCanvas.SetActive(false);

        if (battlefieldCanvas != null)
            battlefieldCanvas.SetActive(true);

        // Load data
        LoadBattleEnerlingByName(playerEnerlingName);
        InitializeBattleState();
        InitializeOrganCooldown();
        UpdateBattlefieldUI();

        // Use existing enerling instead of spawning
        if (existingPlayerEnerling != null)
        {
            spawnedEnerling = existingPlayerEnerling;

            // Reparent to our spawn point
            if (enerlingSpawningPoint != null)
            {
                spawnedEnerling.transform.SetParent(enerlingSpawningPoint);
                spawnedEnerling.transform.localPosition = Vector3.zero;
                spawnedEnerling.transform.localRotation = Quaternion.identity;
                spawnedEnerling.transform.localScale = Vector3.one;
            }

            // Setup animator
            enerlingAnimator = spawnedEnerling.GetComponent<Animator>();
            if (enerlingAnimator != null && battleEnerling != null && battleEnerling.animatorController != null)
            {
                enerlingAnimator.runtimeAnimatorController = battleEnerling.animatorController;
            }

            Debug.Log($"Using existing player enerling: {playerEnerlingName}");
        }
        else
        {
            // Fallback to spawning new one
            SpawnEnerling();
        }

        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.InitializePlayerEnerling(playerEnerlingName);
        }

        Debug.Log($"Player battle enerling initialized: {playerEnerlingName}");
    }

    // ==================== NEW METHOD: Initialize AI with existing enerling ====================
    private void InitializeAIOpponentWithExisting(GameObject existingOpponentEnerling)
    {
        // Get opponent enerling name
        string opponentName = PersistentDataManager.Instance?.GetOpponentEnerlingName();
        if (string.IsNullOrEmpty(opponentName))
        {
            opponentName = GetRandomOpponent();
            PersistentDataManager.Instance?.SaveOpponentEnerling(opponentName);
        }

        Debug.Log($"Initializing AI opponent with existing enerling: {opponentName}");

        if (aiEnerlingManager != null && ingredientDatabase != null)
        {
            // Initialize AI with existing enerling
            aiEnerlingManager.InitializeWithExistingAIEnerling(opponentName, ingredientDatabase, existingOpponentEnerling);
            Debug.Log($"AI opponent initialized with existing enerling: {opponentName}");
        }
        else
        {
            Debug.LogError("Cannot initialize AI: aiEnerlingManager or ingredientDatabase is null!");
        }
    }

    // Call this from TurnSystem when organ bonus is ready
    public void SetOrganDamageBonus(int bonusAmount, List<string> organs)
    {
        organBonusDamage = bonusAmount;
        organBonusNames = new List<string>(organs);
        hasOrganDamageBonus = true;
        Debug.Log($"Player organ damage bonus set: {bonusAmount} from {organs.Count} organs");
    }

    // Call this from TurnSystem when organ heal bonus is ready
    public void SetOrganHealBonus(int bonusAmount, List<string> organs)
    {
        organBonusHeal = bonusAmount;
        organBonusNames = new List<string>(organs);
        hasOrganHealBonus = true;
        Debug.Log($"Player organ heal bonus set: {bonusAmount} from {organs.Count} organs");
    }

    // Apply organ bonus to damage - PUBLIC method using BattleStructs
    public BattleStructs.DamageBreakdown ApplyOrganDamageBonus(int baseDamage)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (hasOrganDamageBonus && organBonusNames.Count > 0)
        {
            Debug.Log($"Player applying organ damage bonus: {organBonusDamage} from {organBonusNames.Count} organs");

            // Calculate individual organ bonus: 5% of base damage per organ (minimum 1)
            foreach (string organ in organBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseDamage * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"Organ {organ}: {organBonus} damage (5% of {baseDamage})");
            }

            // Reset bonus
            hasOrganDamageBonus = false;
            organBonusDamage = 0;
            organBonusNames.Clear();
        }

        return new BattleStructs.DamageBreakdown(baseDamage, organBonuses);
    }

    // Apply organ bonus to heal - PUBLIC method using BattleStructs
    public BattleStructs.HealBreakdown ApplyOrganHealBonus(int baseHeal)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (hasOrganHealBonus && organBonusNames.Count > 0)
        {
            Debug.Log($"Player applying organ heal bonus: {organBonusHeal} from {organBonusNames.Count} organs");

            // Calculate individual organ bonus: 5% of base heal per organ (minimum 1)
            foreach (string organ in organBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseHeal * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"Organ {organ}: {organBonus} heal (5% of {baseHeal})");
            }

            // Reset bonus
            hasOrganHealBonus = false;
            organBonusHeal = 0;
            organBonusNames.Clear();
        }

        return new BattleStructs.HealBreakdown(baseHeal, organBonuses);
    }

    public void CheckAndApplyOrganHeal()
    {
        if (battleEnerling == null || battleEnerling.beneficialOrgans.Count == 0) return;

        Debug.Log($"Player CheckAndApplyOrganHeal: Cooldown Ready={organCooldownReady}, Timer={organCooldownTimer}/{maxOrganCooldown}");

        // Check if organ cooldown is ready
        if (organCooldownReady)
        {
            // Calculate heal amount: 5% of base life per organ
            int healPerOrgan = Mathf.RoundToInt(battleEnerling.baseLife * 0.05f);
            int totalHeal = healPerOrgan * battleEnerling.beneficialOrgans.Count;

            Debug.Log($"Player Organ Heal: BaseLife={battleEnerling.baseLife}, HealPerOrgan={healPerOrgan}, TotalHeal={totalHeal}, OrganCount={battleEnerling.beneficialOrgans.Count}");

            // Create organ bonuses for feedback
            List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();
            foreach (string organ in battleEnerling.beneficialOrgans)
            {
                organBonuses.Add(new FeedbackManager.OrganBonus(organ, healPerOrgan));
            }

            // Show organ heal feedback
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                    FeedbackManager.Instance.playerFeedbackSpawnPoint,
                    0, // No base heal, only organ bonus
                    organBonuses,
                    true,
                    "Beneficial Organ Heal"
                );
            }

            // Apply the heal
            StartCoroutine(ApplyHeal(totalHeal, 0));

            // Reset organ cooldown to 0
            organCooldownTimer = 0;
            organCooldownReady = false;

            Debug.Log($"Player Beneficial Organ Heal Applied: {totalHeal} HP. Cooldown reset to 0.");
        }
        else
        {
            Debug.Log($"Player Organ Cooldown: {organCooldownTimer}/{maxOrganCooldown} turns ({(organCooldownReady ? "READY" : "NOT READY")})");
        }
    }

    public void OnSelectButtonClickedFromSelection()
    {
        if (selectionManager != null)
        {
            string selectedName = PersistentDataManager.Instance?.GetSelectedEnerlingName();

            if (!string.IsNullOrEmpty(selectedName))
            {
                // Save player's selection first
                PersistentDataManager.Instance.SaveSelectedEnerling(selectedName);

                // FIX: Initialize player AND AI BEFORE starting battle
                Debug.Log("=== BATTLE INITIALIZATION ===");

                // 1. Switch to battlefield (initializes player)
                SwitchToBattlefield(selectedName);

                // 2. Initialize AI opponent
                InitializeAIOpponent();

                // 3. Start the battle through TurnSystem
                StartBattle();

                Debug.Log("=== BATTLE STARTED ===");
            }
            else
            {
                Debug.LogError("No enerling selected! Cannot switch to battlefield.");
            }
        }
        else
        {
            Debug.LogError("EnerlingSelectionManager not found!");
        }
    }

    private void InitializeAIOpponent()
    {
        // Get the opponent enerling name from PersistentDataManager
        string opponentName = PersistentDataManager.Instance?.GetOpponentEnerlingName();

        if (string.IsNullOrEmpty(opponentName))
        {
            // If no opponent saved, get a random opponent
            opponentName = GetRandomOpponent();
            Debug.LogWarning($"No opponent saved, using random: {opponentName}");

            // Save it for consistency
            PersistentDataManager.Instance?.SaveOpponentEnerling(opponentName);
        }

        Debug.Log($"Initializing AI opponent: {opponentName}");

        // Initialize AI enerling - use the AIEnerlingManager's own spawning point
        if (aiEnerlingManager != null && ingredientDatabase != null)
        {
            // Get the spawn point FROM AIEnerlingManager itself (just for logging)
            Transform aiSpawnPoint = aiEnerlingManager.aiSpawningPoint;

            if (aiSpawnPoint == null)
            {
                Debug.LogWarning("AIEnerlingManager.aiSpawningPoint is null!");
                // No need to create one - AIEnerlingManager will handle it in SpawnAIEnerling()
            }
            else
            {
                Debug.Log($"AI will spawn at: {aiSpawnPoint.name} at {aiSpawnPoint.position}");
            }

            // Initialize with ONLY 2 parameters now!
            aiEnerlingManager.InitializeAIEnerling(opponentName, ingredientDatabase);
            // REMOVED: , aiSpawnPoint - the 3rd parameter

            Debug.Log($"AI opponent initialized: {opponentName} in battle scene");
        }
        else
        {
            Debug.LogError("Cannot initialize AI: aiEnerlingManager or ingredientDatabase is null!");
            if (aiEnerlingManager == null) Debug.LogError("aiEnerlingManager is null!");
            if (ingredientDatabase == null) Debug.LogError("ingredientDatabase is null!");
        }
    }

    private string GetRandomOpponent()
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase is null!");
            return "DefaultEnerling";
        }

        // Get all unlocked enerlings from database
        var unlocked = ingredientDatabase.GetUnlockedIngredients();

        // Get player's selected enerling to avoid fighting the same one
        string playerEnerling = PersistentDataManager.Instance?.GetSelectedEnerlingName();

        // Filter out player's enerling if it's in the unlocked list
        List<IngredientDatabase.IngredientInfo> possibleOpponents = new List<IngredientDatabase.IngredientInfo>();

        foreach (var enerling in unlocked)
        {
            if (enerling.ingredientName != playerEnerling)
            {
                possibleOpponents.Add(enerling);
            }
        }

        // If no other unlocked enerlings, use any from database (excluding player's)
        if (possibleOpponents.Count == 0)
        {
            foreach (var enerling in ingredientDatabase.ingredients)
            {
                if (enerling.ingredientName != playerEnerling)
                {
                    possibleOpponents.Add(enerling);
                }
            }
        }

        // Select random opponent
        if (possibleOpponents.Count > 0)
        {
            return possibleOpponents[Random.Range(0, possibleOpponents.Count)].ingredientName;
        }

        // Fallback
        Debug.LogWarning("No opponents found, using default");
        return "DefaultEnerling";
    }

    private void StartBattle()
    {
        Debug.Log("=== Starting Battle ===");

        // Make sure we have references
        if (battleEnerling == null)
        {
            Debug.LogError("Player battleEnerling is null!");
            return;
        }

        if (aiEnerlingManager == null)
        {
            Debug.LogError("AIEnerlingManager is null!");
            return;
        }

        var aiEnerling = aiEnerlingManager.GetAIEnerling();
        if (aiEnerling == null)
        {
            Debug.LogError("AI enerling is null!");
            return;
        }

        Debug.Log($"Battle starting: {battleEnerling.ingredientName} vs {aiEnerling.ingredientName}");

        // Initialize turn system
        if (turnSystem != null)
        {
            // MAKE SURE references are set
            turnSystem.InitializeBattle(this, aiEnerlingManager);

            // Start the battle
            turnSystem.StartBattle();
            Debug.Log("Turn system started");
        }
        else
        {
            Debug.LogError("TurnSystem is null!");
        }

        // Enable player controls (TurnSystem will disable/enable as needed)
        if (playerEnerlingManager != null)
        {
            // Don't enable immediately - let TurnSystem handle it
            playerEnerlingManager.SetButtonsInteractable(false);
            Debug.Log("Player controls initialized (waiting for TurnSystem)");
        }

        Debug.Log("Battle started successfully!");
    }


    public void SwitchToBattlefield(string selectedEnerlingName)
    {
        Debug.Log($"Switching to battlefield with enerling: {selectedEnerlingName}");

        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(false);
        }

        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(true);
        }

        // Initialize player enerling
        LoadBattleEnerlingByName(selectedEnerlingName);
        InitializeBattleState();
        InitializeOrganCooldown();
        UpdateBattlefieldUI();
        SpawnEnerling();

        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.InitializePlayerEnerling(selectedEnerlingName);
        }

        Debug.Log($"Player battle enerling initialized: {selectedEnerlingName}");
    }

    public void SwitchToSelection()
    {
        Debug.Log("Switching back to selection screen");
        CleanupBattlefield();

        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(false);
        }

        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(true);
        }
    }

    public void InitializeBattlefieldWithEnerling(string playerEnerlingName)
    {
        Debug.Log($"Switching to battlefield with player enerling: {playerEnerlingName}");

        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(false);
        }

        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(true);
        }

        // Initialize player enerling ONLY
        LoadBattleEnerlingByName(playerEnerlingName);
        InitializeBattleState();
        InitializeOrganCooldown();
        UpdateBattlefieldUI();
        SpawnEnerling();

        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.InitializePlayerEnerling(playerEnerlingName);
        }

        // Note: AI initialization is now handled by BattlePlayManager
        // when we click "Fight" button after the cutscene

        Debug.Log($"Player battle enerling initialized: {playerEnerlingName}");
    }

    void LoadBattleEnerlingByName(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName))
        {
            Debug.LogError("Cannot load battle enerling: name is empty!");
            return;
        }

        battleEnerling = ingredientDatabase.CreateBattleCopy(enerlingName);
        if (battleEnerling == null)
        {
            Debug.LogError($"Failed to create battle copy for {enerlingName}");
            return;
        }

        if (PersistentDataManager.Instance != null)
        {
            int savedLife = PersistentDataManager.Instance.GetEnerlingCurrentLife(enerlingName);
            if (savedLife > 0)
            {
                battleEnerling.currentLife = savedLife;
            }
        }

        currentArmor = CalculateArmorValue(battleEnerling);
        activeDefend = 0;
        hasDefend = false;

        // Initialize skill cooldowns based on database values
        InitializeSkillCooldowns();

        Debug.Log($"Battle enerling loaded: {battleEnerling.ingredientName} (Life: {battleEnerling.currentLife}/{battleEnerling.baseLife}, Armor: {currentArmor})");
    }

    void InitializeSkillCooldowns()
    {
        if (battleEnerling == null) return;

        // Set initial cooldowns for skills that have cooldown
        // Skills should start with their cooldown value (not available at round 1 if cooldown > 0)
        for (int i = 1; i <= 4; i++)
        {
            var skill = GetSkillByNumber(i);
            if (skill != null && skill.cooldownTurns > 0)
            {
                // Set the skill to be on cooldown at battle start
                switch (i)
                {
                    case 1:
                        battleEnerling.skill1Cooldown = skill.cooldownTurns;
                        break;
                    case 2:
                        battleEnerling.skill2Cooldown = skill.cooldownTurns;
                        break;
                    case 3:
                        battleEnerling.skill3Cooldown = skill.cooldownTurns;
                        break;
                    case 4:
                        battleEnerling.skill4Cooldown = skill.cooldownTurns;
                        break;
                }
                Debug.Log($"Skill {i} starts on cooldown: {skill.cooldownTurns} turns");
            }
        }
    }

    void InitializeBattleState()
    {
        if (battleEnerling != null)
        {
            battleEnerling.ResetBattleState();

            if (PersistentDataManager.Instance != null)
            {
                int savedLife = PersistentDataManager.Instance.GetEnerlingCurrentLife(battleEnerling.ingredientName);
                if (savedLife > 0)
                {
                    battleEnerling.currentLife = savedLife;
                }
            }
        }
    }

    void InitializeOrganCooldown()
    {
        if (battleEnerling == null) return;

        // Set cooldown based on rarity - UPDATED VALUES
        switch (battleEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                maxOrganCooldown = 4;  // Changed from 5 to 4
                break;
            case IngredientDatabase.Rarity.Rare:
                maxOrganCooldown = 3;  // Changed from 4 to 3
                break;
            case IngredientDatabase.Rarity.UltraRare:
                maxOrganCooldown = 2;  // Changed from 3 to 2
                break;
        }

        organCooldownTimer = 0; // Start at 0
        organCooldownReady = false; // Not ready until we reach max cooldown

        Debug.Log($"Organ cooldown initialized: Timer={organCooldownTimer}/{maxOrganCooldown} for {battleEnerling.rarity}");
    }

    void UpdateBattlefieldUI()
    {
        if (battleEnerling == null) return;

        if (battlefieldEnerlingName != null)
            battlefieldEnerlingName.text = battleEnerling.ingredientName;

        if (battlefieldHealthSlider != null)
        {
            battlefieldHealthSlider.maxValue = battleEnerling.baseLife;
            battlefieldHealthSlider.value = battleEnerling.currentLife;
        }

        if (healthText != null)
        {
            healthText.text = $"{battleEnerling.currentLife}/{battleEnerling.baseLife}";
            UpdateHealthTextColor();
        }

        if (battlefieldArmorSlider != null)
        {
            battlefieldArmorSlider.maxValue = CalculateArmorValue(battleEnerling);
            battlefieldArmorSlider.value = currentArmor;
        }

        if (armorText != null)
        {
            armorText.text = $"{currentArmor}";
            UpdateArmorTextColor();
        }

        if (battlefieldFrame != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(battleEnerling.rarity);
            if (frameSprite != null)
                battlefieldFrame.sprite = frameSprite;
        }

        if (rarityTag != null)
        {
            Sprite raritySprite = ingredientDatabase.GetRarityIcon(battleEnerling.rarity);
            if (raritySprite != null)
                rarityTag.sprite = raritySprite;
        }

        if (enerlingImage != null && battleEnerling.enerlingSprite != null)
        {
            enerlingImage.sprite = battleEnerling.enerlingSprite;
            enerlingImage.preserveAspect = true;
        }

        if (abilityText != null)
        {
            abilityText.text = GetAbilityText(battleEnerling);
        }

        if (nameStatsBG != null)
        {
            UpdateNameStatsBackground();
        }

        UpdateOrganPanel();
    }

    int CalculateArmorValue(IngredientDatabase.IngredientInfo enerling)
    {
        float armorDecimal = enerling.armorPercent / 100f;
        int armorValue = Mathf.RoundToInt(enerling.baseLife * armorDecimal);
        return armorValue;
    }

    string GetAbilityText(IngredientDatabase.IngredientInfo enerling)
    {
        if (enerling.beneficialOrgans.Count > 0)
            return "Beneficial Organ";
        else if (enerling.targetOrgans.Count > 0)
            return "Target Organ";
        else
            return "No Special Ability";
    }

    void UpdateNameStatsBackground()
    {
        if (nameStatsBG == null || battleEnerling == null) return;

        switch (battleEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                if (commonNameStatsBG != null)
                    nameStatsBG.sprite = commonNameStatsBG;
                break;
            case IngredientDatabase.Rarity.Rare:
                if (rareNameStatsBG != null)
                    nameStatsBG.sprite = rareNameStatsBG;
                break;
            case IngredientDatabase.Rarity.UltraRare:
                if (ultraRareNameStatsBG != null)
                    nameStatsBG.sprite = ultraRareNameStatsBG;
                break;
        }
    }

    void UpdateOrganPanel()
    {
        foreach (Transform child in organPanel)
        {
            Destroy(child.gameObject);
        }

        if (battleEnerling == null || organImagePrefab == null) return;

        List<string> organs = battleEnerling.beneficialOrgans.Count > 0 ?
            battleEnerling.beneficialOrgans : battleEnerling.targetOrgans;

        foreach (string organ in organs)
        {
            GameObject organImage = Instantiate(organImagePrefab, organPanel);
            Image image = organImage.GetComponent<Image>();

            Sprite organSprite = ingredientDatabase.GetOrganSprite(organ);
            if (organSprite != null && image != null)
            {
                image.sprite = organSprite;
                image.preserveAspect = true;
            }

            TextMeshProUGUI organText = organImage.GetComponentInChildren<TextMeshProUGUI>();
            if (organText != null)
            {
                organText.text = organ;
            }
        }
    }

    void SpawnEnerling()
    {
        if (battleEnerling == null || battleEnerling.modelPrefab == null)
        {
            Debug.LogError("Cannot spawn enerling: no battle enerling data or prefab");
            return;
        }

        if (spawnedEnerling != null)
        {
            Destroy(spawnedEnerling);
        }

        spawnedEnerling = Instantiate(battleEnerling.modelPrefab, enerlingSpawningPoint);
        spawnedEnerling.transform.localPosition = Vector3.zero;
        spawnedEnerling.transform.localRotation = Quaternion.identity;

        enerlingAnimator = spawnedEnerling.GetComponent<Animator>();
        if (enerlingAnimator == null)
        {
            Debug.LogWarning("Spawned enerling has no Animator component");
        }

        if (battleEnerling.animatorController != null && enerlingAnimator != null)
        {
            enerlingAnimator.runtimeAnimatorController = battleEnerling.animatorController;
        }

        Debug.Log($"Spawned enerling: {battleEnerling.ingredientName}");
    }

    public void OnSkillButtonClicked(int skillNumber)
    {
        if (isAnimating)
        {
            Debug.Log("Animation in progress, please wait...");
            return;
        }

        // Check if it's player's turn
        if (turnSystem != null && !turnSystem.IsPlayerTurn())
        {
            Debug.Log("Not player's turn!");
            return;
        }

        // Check if turn system is animating
        if (turnSystem != null && turnSystem.IsAnimating())
        {
            Debug.Log("System is busy, please wait...");
            return;
        }

        if (enerlingAnimator == null)
        {
            Debug.LogWarning("No animator found for skill animation");
            return;
        }

        if (battleEnerling != null && !battleEnerling.IsSkillReady(skillNumber))
        {
            Debug.Log($"Skill {skillNumber} is on cooldown!");
            return;
        }

        // Notify turn system
        if (turnSystem != null)
        {
            turnSystem.PlayerSkillChosen();
        }

        StartCoroutine(PlaySkillAnimationAndEffect(skillNumber));
    }


    string GetAnimationBoolName(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return "isAttack";
            case 2: return "isSkill1";
            case 3: return "isSkill2";
            case 4: return "isSkill3";
            default: return "";
        }
    }

    IEnumerator PlaySkillAnimationAndEffect(int skillNumber)
    {
        isAnimating = true;

        // Notify turn system that animation is starting
        if (turnSystem != null)
        {
            turnSystem.OnSkillAnimationStart();
        }

        string animationBool = GetAnimationBoolName(skillNumber);
        if (!string.IsNullOrEmpty(animationBool))
        {
            enerlingAnimator.SetBool(animationBool, true);
            Debug.Log($"Playing animation for skill {skillNumber}: {animationBool}");

            // Wait for animation to complete
            yield return StartCoroutine(WaitForAnimationToComplete(animationBool));

            enerlingAnimator.SetBool(animationBool, false);
        }
        else
        {
            Debug.LogWarning($"No animation bool found for skill {skillNumber}");
        }

        // Apply skill effect AFTER animation completes
        ApplySkillEffect(skillNumber);

        if (battleEnerling != null)
        {
            // Set the skill cooldown (this method already exists)
            battleEnerling.SetSkillCooldown(skillNumber);
        }

        // Update player skill buttons UI
        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.UpdateSkillButton(skillNumber);
        }

        // Notify turn system that animation is complete
        if (turnSystem != null)
        {
            turnSystem.OnSkillAnimationEnd();
        }

        isAnimating = false;
        Debug.Log($"Skill {skillNumber} executed");
    }

    IEnumerator WaitForAnimationToComplete(string animationBool)
    {
        // Wait until animation starts
        yield return new WaitForSeconds(0.1f);

        // Wait for current state to finish using FeedbackManager's utility
        if (FeedbackManager.Instance != null && enerlingAnimator != null)
        {
            yield return StartCoroutine(FeedbackManager.Instance.WaitForCurrentStateToFinish(enerlingAnimator));
        }
        else
        {
            // Fallback: wait a fixed time
            yield return new WaitForSeconds(1f);
        }
    }

    void ApplySkillEffect(int skillNumber)
    {
        if (battleEnerling == null) return;

        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);
        if (skill == null) return;

        // Get BASE effect WITHOUT organ bonus
        int skillValue = skill.GetValue();

        // Check for organ effects
        bool playerHasBeneficialOrgans = battleEnerling.beneficialOrgans.Count > 0;
        bool playerHasTargetOrgans = battleEnerling.targetOrgans.Count > 0;

        switch (skill.type)
        {
            case IngredientDatabase.SkillInfo.SkillType.Heal:
                // Apply organ heal bonus if available
                BattleStructs.HealBreakdown healBreakdown = ApplyOrganHealBonus(skillValue);

                Debug.Log($"Player healing: Base={healBreakdown.baseHeal}, Total={healBreakdown.totalHeal}, OrganBonuses={healBreakdown.organBonuses?.Count ?? 0}");

                // Show TOTAL heal with breakdown
                if (FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                        FeedbackManager.Instance.playerFeedbackSpawnPoint,
                        healBreakdown.baseHeal,
                        healBreakdown.organBonuses,
                        true,
                        "Player Heal"
                    );
                }

                // Apply total heal to PLAYER
                StartCoroutine(ApplyHeal(healBreakdown.totalHeal, 0));
                break;

            case IngredientDatabase.SkillInfo.SkillType.Damage:
                if (aiEnerlingManager != null)
                {
                    // Check if opponent is immune to organ damage
                    bool opponentImmune = false;
                    if (aiEnerlingManager.GetAIEnerling() != null)
                    {
                        opponentImmune = aiEnerlingManager.GetAIEnerling().immuneToOrganDamage;
                    }

                    // Check if we should apply organ damage
                    bool canApplyOrganDamage = !opponentImmune || !playerHasTargetOrgans;

                    // Apply organ damage bonus if available
                    BattleStructs.DamageBreakdown damageBreakdown;

                    if (opponentImmune && playerHasTargetOrgans)
                    {
                        Debug.Log($"Opponent is immune to organ damage. Only base damage will be applied.");
                        damageBreakdown = new BattleStructs.DamageBreakdown(skillValue, new List<FeedbackManager.OrganBonus>());
                    }
                    else
                    {
                        damageBreakdown = ApplyOrganDamageBonus(skillValue);
                    }

                    Debug.Log($"Player attacking: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}, OrganBonuses={damageBreakdown.organBonuses?.Count ?? 0}");

                    // Apply total damage
                    StartCoroutine(aiEnerlingManager.TakeDamageWithFeedback(
                        damageBreakdown,
                        FeedbackManager.Instance != null ? FeedbackManager.Instance.aiFeedbackSpawnPoint : null
                    ));

                    // Target organ damage (only triggers when cooldown is ready AND opponent is not immune)
                    if (playerHasTargetOrgans && organCooldownReady && !opponentImmune)
                    {
                        Debug.Log($"Player Organ Damage Triggered! {battleEnerling.targetOrgans.Count} target organs");

                        // For EACH organ, calculate bonus
                        int organBonusPerOrgan = Mathf.RoundToInt(skillValue * 0.05f);
                        if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                        List<FeedbackManager.OrganBonus> cooldownBonuses = new List<FeedbackManager.OrganBonus>();
                        foreach (string organ in battleEnerling.targetOrgans)
                        {
                            cooldownBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonusPerOrgan));
                        }

                        // Create damage breakdown for cooldown bonuses
                        BattleStructs.DamageBreakdown cooldownDamage = new BattleStructs.DamageBreakdown(0, cooldownBonuses);

                        // Apply organ cooldown damage
                        StartCoroutine(aiEnerlingManager.TakeDamageWithFeedback(
                            cooldownDamage,
                            FeedbackManager.Instance != null ? FeedbackManager.Instance.aiFeedbackSpawnPoint : null
                        ));

                        // Reset organ cooldown
                        organCooldownTimer = maxOrganCooldown;
                        organCooldownReady = false;
                        Debug.Log($"Player Organ Cooldown Reset: {organCooldownTimer} turns remaining");
                    }
                    else if (playerHasTargetOrgans && !opponentImmune)
                    {
                        UpdateOrganCooldown();
                    }
                    else if (opponentImmune)
                    {
                        Debug.Log("Opponent immune to organ damage - organ cooldown not activated");
                    }
                }
                break;

            case IngredientDatabase.SkillInfo.SkillType.Defend:
                SetDefend(skillValue);
                break;
        }
    }

    IngredientDatabase.SkillInfo GetSkillByNumber(int skillNumber)
    {
        if (battleEnerling == null) return null;

        switch (skillNumber)
        {
            case 1: return battleEnerling.skill1;
            case 2: return battleEnerling.skill2;
            case 3: return battleEnerling.skill3;
            case 4: return battleEnerling.skill4;
            default: return null;
        }
    }

    public IEnumerator ApplyHeal(int totalHeal, int organBonus)
    {
        // Show heal feedback - handled in ApplySkillEffect

        int targetHealth = Mathf.Min(battleEnerling.currentLife + totalHeal, battleEnerling.baseLife);

        if (healthAnimationCoroutine != null)
            StopCoroutine(healthAnimationCoroutine);

        healthAnimationCoroutine = StartCoroutine(SmoothHealthChange(battleEnerling.currentLife, targetHealth, 0.5f));
        battleEnerling.currentLife = targetHealth;

        yield return null;
    }

    public IEnumerator ApplyDamageToPlayer(BattleStructs.DamageBreakdown damageBreakdown, Transform feedbackSpawnPoint)
    {
        Debug.Log($"Player receiving damage: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}");

        int totalDamage = damageBreakdown.totalDamage;
        int remainingDamage = totalDamage;
        int damageBlockedByDefend = 0;

        // Apply defend if active - DEFEND BLOCKS THE NEXT ATTACK ONLY
        if (hasDefend && activeDefend > 0)
        {
            Debug.Log($"Player has defend: {activeDefend} against {totalDamage} damage");

            // Calculate how much damage defend can block
            damageBlockedByDefend = Mathf.Min(activeDefend, remainingDamage);
            int damageThatGoesThrough = remainingDamage - damageBlockedByDefend;

            // Reduce defend by the damage blocked
            activeDefend -= damageBlockedByDefend;
            remainingDamage = damageThatGoesThrough;

            Debug.Log($"Defend blocked {damageBlockedByDefend} damage. Remaining defend: {activeDefend}, Damage that goes through: {damageThatGoesThrough}");

            // Show defend feedback for blocked damage
            if (FeedbackManager.Instance != null)
            {
                if (damageBlockedByDefend > 0)
                {
                    FeedbackManager.Instance.ShowDefend(
                        feedbackSpawnPoint,
                        damageBlockedByDefend,
                        false, // Not activation, this is block effect
                        "Player Defend Block"
                    );
                }

                // If defend blocked all damage, show special feedback
                if (damageBlockedByDefend >= totalDamage)
                {
                    FeedbackManager.Instance.ShowDefend(
                        feedbackSpawnPoint,
                        totalDamage,
                        false,
                        "Player Defend Complete Block"
                    );
                }
            }

            // Check if defend is used up
            if (activeDefend <= 0)
            {
                hasDefend = false;
                activeDefend = 0;
                Debug.Log("Player defend used up");
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Calculate armor damage (if defend didn't block all damage)
        int armorDamage = 0;
        if (currentArmor > 0 && remainingDamage > 0)
        {
            armorDamage = Mathf.Min(currentArmor, remainingDamage);
            currentArmor -= armorDamage;
            remainingDamage -= armorDamage;

            Debug.Log($"Armor blocked {armorDamage} damage. Remaining armor: {currentArmor}");
        }

        // Show remaining damage that goes through to health
        if (FeedbackManager.Instance != null && remainingDamage > 0)
        {
            // Create a new damage breakdown for the remaining damage
            List<FeedbackManager.OrganBonus> effectiveOrganBonuses = CalculateEffectiveOrganBonuses(
                damageBreakdown,
                damageBlockedByDefend,
                totalDamage
            );

            // Calculate effective base damage (after defend blocked some)
            int effectiveBaseDamage = Mathf.Max(0, damageBreakdown.baseDamage - damageBlockedByDefend);

            FeedbackManager.Instance.ShowTotalDamageWithOrganBreakdown(
                feedbackSpawnPoint,
                effectiveBaseDamage,
                effectiveOrganBonuses,
                true,
                "AI Attack"
            );
        }

        // Apply armor change
        if (armorDamage > 0)
        {
            StartCoroutine(SmoothArmorChange(currentArmor + armorDamage, currentArmor, 0.3f));
            yield return new WaitForSeconds(0.3f);
        }

        // Remaining damage goes to health
        if (remainingDamage > 0)
        {
            int targetHealth = Mathf.Max(0, battleEnerling.currentLife - remainingDamage);

            StartCoroutine(PulseHealthSliderRed());

            if (healthAnimationCoroutine != null)
                StopCoroutine(healthAnimationCoroutine);

            healthAnimationCoroutine = StartCoroutine(SmoothHealthChange(battleEnerling.currentLife, targetHealth, 0.5f));
            battleEnerling.currentLife = targetHealth;

            if (battleEnerling.currentLife <= 0)
            {
                Debug.Log("Player defeated!");
            }
        }

        yield return null;
    }

    // Helper method to calculate organ bonuses after defend
    private List<FeedbackManager.OrganBonus> CalculateEffectiveOrganBonuses(
        BattleStructs.DamageBreakdown originalBreakdown,
        int damageBlocked,
        int totalOriginalDamage)
    {
        List<FeedbackManager.OrganBonus> effectiveBonuses = new List<FeedbackManager.OrganBonus>();

        if (originalBreakdown.organBonuses == null || originalBreakdown.organBonuses.Count == 0)
            return effectiveBonuses;

        // If defend blocked all damage, no organ bonuses get through
        if (damageBlocked >= totalOriginalDamage)
            return effectiveBonuses;

        // Calculate what percentage of damage got through
        float penetrationRatio = 1f - ((float)damageBlocked / totalOriginalDamage);

        foreach (var bonus in originalBreakdown.organBonuses)
        {
            int effectiveBonus = Mathf.RoundToInt(bonus.bonusAmount * penetrationRatio);
            if (effectiveBonus > 0)
            {
                effectiveBonuses.Add(new FeedbackManager.OrganBonus(bonus.organName, effectiveBonus));
            }
        }

        return effectiveBonuses;
    }

    void SetDefend(int defendAmount)
    {
        activeDefend = defendAmount;
        hasDefend = true;

        // Show defend activation feedback
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.ShowDefend(
                FeedbackManager.Instance.playerFeedbackSpawnPoint,
                defendAmount,
                true,
                "Player Defend"
            );
        }

        Debug.Log($"Defend set to {defendAmount} for next opponent's attack");
    }

    public void ClearDefend()
    {
        if (hasDefend)
        {
            Debug.Log($"Defend cleared (was {activeDefend})");
            hasDefend = false;
            activeDefend = 0;
        }
    }

    void UpdateOrganCooldown()
    {
        // Increase cooldown timer each turn
        if (organCooldownTimer < maxOrganCooldown)
        {
            organCooldownTimer++;
            Debug.Log($"Player Organ Cooldown: {organCooldownTimer}/{maxOrganCooldown}");

            // Check if cooldown is now ready
            if (organCooldownTimer >= maxOrganCooldown)
            {
                organCooldownReady = true;
                Debug.Log("Player Organ cooldown ready!");
            }
        }
    }

    public void ProcessEndTurn()
    {
        UpdateOrganCooldown();
        ClearDefend();
    }

    IEnumerator SmoothHealthChange(float startValue, float endValue, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, endValue, t);

            if (battlefieldHealthSlider != null)
                battlefieldHealthSlider.value = currentValue;

            if (healthText != null)
            {
                healthText.text = $"{(int)currentValue}/{battleEnerling.baseLife}";
                UpdateHealthTextColor();
            }

            yield return null;
        }

        if (battlefieldHealthSlider != null)
            battlefieldHealthSlider.value = endValue;

        if (healthText != null)
        {
            healthText.text = $"{(int)endValue}/{battleEnerling.baseLife}";
            UpdateHealthTextColor();
        }
    }

    IEnumerator SmoothArmorChange(float startValue, float endValue, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, endValue, t);

            if (battlefieldArmorSlider != null)
                battlefieldArmorSlider.value = currentValue;

            if (armorText != null)
            {
                armorText.text = $"{(int)currentValue}";
                UpdateArmorTextColor();
            }

            yield return null;
        }

        if (battlefieldArmorSlider != null)
            battlefieldArmorSlider.value = endValue;

        if (armorText != null)
        {
            armorText.text = $"{(int)endValue}";
            UpdateArmorTextColor();
        }
    }

    IEnumerator PulseHealthSliderRed()
    {
        Image fillImage = battlefieldHealthSlider?.fillRect?.GetComponent<Image>();
        if (fillImage == null) yield break;

        Color originalColor = fillImage.color;
        Color redColor = Color.red;

        float pulseDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            if (t < 0.5f)
                fillImage.color = Color.Lerp(originalColor, redColor, t * 2);
            else
                fillImage.color = Color.Lerp(redColor, originalColor, (t - 0.5f) * 2);

            yield return null;
        }

        fillImage.color = originalColor;
    }

    void UpdateHealthTextColor()
    {
        if (healthText == null || battleEnerling == null) return;

        float healthPercentage = (float)battleEnerling.currentLife / battleEnerling.baseLife;

        if (healthPercentage <= 0.33f)
            healthText.color = Color.red;
        else if (healthPercentage <= 0.66f)
            healthText.color = new Color(1f, 0.5f, 0f);
        else
            healthText.color = Color.white;
    }

    void UpdateArmorTextColor()
    {
        if (armorText == null || battleEnerling == null) return;

        int maxArmor = CalculateArmorValue(battleEnerling);
        float armorPercentage = maxArmor > 0 ? (float)currentArmor / maxArmor : 1f;

        if (armorPercentage <= 0.33f)
            armorText.color = Color.red;
        else if (armorPercentage <= 0.66f)
            armorText.color = new Color(1f, 0.5f, 0f);
        else
            armorText.color = Color.white;
    }

    public void UpdateBattleUI()
    {
        if (battleEnerling == null) return;

        if (battlefieldHealthSlider != null)
        {
            battlefieldHealthSlider.value = battleEnerling.currentLife;
        }

        if (healthText != null)
        {
            healthText.text = $"{battleEnerling.currentLife}/{battleEnerling.baseLife}";
            UpdateHealthTextColor();
        }

        if (armorText != null)
        {
            armorText.text = $"{currentArmor}";
            UpdateArmorTextColor();
        }
    }

    public void CleanupBattlefield()
    {
        StopAllCoroutines();

        if (healthAnimationCoroutine != null)
            StopCoroutine(healthAnimationCoroutine);
        if (armorAnimationCoroutine != null)
            StopCoroutine(armorAnimationCoroutine);

        CleanupSpawnedEnerling();

        if (organPanel != null)
        {
            foreach (Transform child in organPanel)
            {
                Destroy(child.gameObject);
            }
        }

        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.Cleanup();
        }

        if (aiEnerlingManager != null)
        {
            aiEnerlingManager.Cleanup();
        }

        if (turnSystem != null)
        {
            turnSystem.Cleanup();
        }

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.Cleanup();
        }

        battleEnerling = null;
        currentArmor = 0;
        activeDefend = 0;
        hasDefend = false;
        isAnimating = false;
    }

    // ==================== Updated: Initialize with existing enerling (for backward compatibility) ====================
    public void InitializeWithExistingEnerling(string enerlingName, GameObject existingEnerling)
    {
        Debug.Log($"BattleEnerlingManager: Initializing with existing enerling: {enerlingName}");

        // This is now just an alias for the new method
        SwitchToBattlefieldWithExistingEnerling(enerlingName, existingEnerling);
    }

    public IngredientDatabase.IngredientInfo GetBattleEnerling()
    {
        return battleEnerling;
    }

    public void CleanupSpawnedEnerling()
    {
        if (spawnedEnerling != null)
        {
            Destroy(spawnedEnerling);
            spawnedEnerling = null;
            enerlingAnimator = null;
        }
    }

    void OnDestroy()
    {
        SaveBattleState();
        CleanupBattlefield();
    }

    void SaveBattleState()
    {
        if (battleEnerling != null && PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.SaveEnerlingCurrentLife(
                battleEnerling.ingredientName,
                battleEnerling.currentLife
            );

            var original = ingredientDatabase.GetIngredientInfo(battleEnerling.ingredientName);
            if (original != null)
            {
                original.currentLife = battleEnerling.currentLife;
            }
        }
    }
}