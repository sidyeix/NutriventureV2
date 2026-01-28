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

    [Header("Animation Settings")]
    public float animationBufferTime = 0.1f;

    // Current battle enerling
    private IngredientDatabase.IngredientInfo battleEnerling;
    private GameObject spawnedEnerling;
    private Animator enerlingAnimator;

    // Defense tracking
    private int currentArmor = 0;
    private int activeDefense = 0;
    private bool hasDefense = false;

    // Skill tracking
    private List<GameObject> skillButtons = new List<GameObject>();
    private bool isAnimating = false;
    private float animationEndTime = 0f;

    // Organ cooldown tracking
    private int organCooldownTimer = 0;
    private int maxOrganCooldown = 5;
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

    // Apply organ bonus to damage
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

    // Apply organ bonus to heal
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

    public void OnSelectButtonClickedFromSelection()
    {
        if (selectionManager != null)
        {
            string selectedName = PersistentDataManager.Instance?.GetSelectedEnerlingName();

            if (!string.IsNullOrEmpty(selectedName))
            {
                SwitchToBattlefield(selectedName);
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

        InitializeBattlefieldWithEnerling(selectedEnerlingName);
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

    public void InitializeBattlefieldWithEnerling(string enerlingName)
    {
        LoadBattleEnerlingByName(enerlingName);
        InitializeBattleState();
        InitializeOrganCooldown();
        UpdateBattlefieldUI();
        SpawnEnerling();

        if (playerEnerlingManager != null)
        {
            playerEnerlingManager.InitializePlayerEnerling(enerlingName);
        }

        if (aiEnerlingManager != null && ingredientDatabase != null)
        {
            var unlocked = ingredientDatabase.GetUnlockedIngredients();
            if (unlocked.Count > 0)
            {
                int randomIndex = Random.Range(0, unlocked.Count);
                string randomAIEnerling = unlocked[randomIndex].ingredientName;
                aiEnerlingManager.InitializeAIEnerling(randomAIEnerling, ingredientDatabase, aiSpawningPoint);
                aiEnerlingManager.UpdateAIUI();
            }
        }

        if (turnSystem != null)
        {
            turnSystem.StartBattle();
        }

        Debug.Log($"Battlefield initialized with {enerlingName}");
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
        activeDefense = 0;
        hasDefense = false;

        Debug.Log($"Battle enerling loaded: {battleEnerling.ingredientName} (Life: {battleEnerling.currentLife}/{battleEnerling.baseLife}, Armor: {currentArmor})");
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

        // Set cooldown based on rarity
        switch (battleEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                maxOrganCooldown = 5;
                break;
            case IngredientDatabase.Rarity.Rare:
                maxOrganCooldown = 4;
                break;
            case IngredientDatabase.Rarity.UltraRare:
                maxOrganCooldown = 3;
                break;
        }

        organCooldownTimer = 0;
        organCooldownReady = false;

        Debug.Log($"Organ cooldown initialized: {maxOrganCooldown} turns for {battleEnerling.rarity}");
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
            battleEnerling.SetSkillCooldown(skillNumber);
        }

        // Wait a moment for feedback to display
        yield return new WaitForSeconds(0.5f);

        // Notify turn system that skill is complete
        if (turnSystem != null)
        {
            turnSystem.PlayerSkillChosen();
        }

        isAnimating = false;
        Debug.Log($"Skill {skillNumber} executed");
    }

    IEnumerator WaitForAnimationToComplete(string animationBool)
    {
        // Wait until animation starts
        yield return new WaitForSeconds(0.1f);

        // Wait for current state to finish
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
        int baseEffect = skill.GetValue();

        // Check for organ effects
        bool hasBeneficialOrgans = battleEnerling.beneficialOrgans.Count > 0;
        bool hasTargetOrgans = battleEnerling.targetOrgans.Count > 0;

        switch (skill.type)
        {
            case IngredientDatabase.SkillInfo.SkillType.Heal:
                // Apply organ heal bonus if available
                BattleStructs.HealBreakdown healBreakdown = ApplyOrganHealBonus(baseEffect);

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

                // Apply total heal
                StartCoroutine(ApplyHeal(healBreakdown.totalHeal, 0));

                // Beneficial organ healing (triggers when cooldown is ready)
                if (hasBeneficialOrgans && organCooldownReady)
                {
                    Debug.Log($"Player Organ Heal Triggered! {battleEnerling.beneficialOrgans.Count} beneficial organs");

                    // For EACH organ, calculate and show feedback
                    int organCount = battleEnerling.beneficialOrgans.Count;
                    int organBonusPerOrgan = Mathf.RoundToInt(baseEffect * 0.05f);
                    if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                    List<FeedbackManager.OrganBonus> cooldownBonuses = new List<FeedbackManager.OrganBonus>();
                    foreach (string organ in battleEnerling.beneficialOrgans)
                    {
                        cooldownBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonusPerOrgan));
                    }

                    // Show organ cooldown bonuses
                    if (FeedbackManager.Instance != null && cooldownBonuses.Count > 0)
                    {
                        FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                            FeedbackManager.Instance.playerFeedbackSpawnPoint,
                            0,
                            cooldownBonuses,
                            true,
                            "Player Organ Cooldown"
                        );
                    }

                    // Apply total organ heal
                    int totalOrganHeal = organBonusPerOrgan * battleEnerling.beneficialOrgans.Count;
                    StartCoroutine(ApplyHeal(totalOrganHeal, 0));

                    // Reset organ cooldown
                    organCooldownTimer = maxOrganCooldown;
                    organCooldownReady = false;
                    Debug.Log($"Player Organ Cooldown Reset: {organCooldownTimer} turns remaining");
                }
                else if (hasBeneficialOrgans)
                {
                    UpdateOrganCooldown();
                }
                break;

            case IngredientDatabase.SkillInfo.SkillType.Damage:
                if (aiEnerlingManager != null)
                {
                    // Apply organ damage bonus if available
                    BattleStructs.DamageBreakdown damageBreakdown = ApplyOrganDamageBonus(baseEffect);

                    Debug.Log($"Player attacking: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}, OrganBonuses={damageBreakdown.organBonuses?.Count ?? 0}");

                    // Apply total damage
                    StartCoroutine(aiEnerlingManager.TakeDamageWithFeedback(
                        damageBreakdown,
                        FeedbackManager.Instance != null ? FeedbackManager.Instance.aiFeedbackSpawnPoint : null
                    ));

                    // Target organ damage (only triggers when cooldown is ready)
                    if (hasTargetOrgans && organCooldownReady)
                    {
                        Debug.Log($"Player Organ Damage Triggered! {battleEnerling.targetOrgans.Count} target organs");

                        // For EACH organ, calculate bonus
                        int organCount = battleEnerling.targetOrgans.Count;
                        int organBonusPerOrgan = Mathf.RoundToInt(baseEffect * 0.05f);
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
                    else if (hasTargetOrgans)
                    {
                        UpdateOrganCooldown();
                    }
                }
                break;

            case IngredientDatabase.SkillInfo.SkillType.Defend:
                SetDefense(baseEffect);
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
        Debug.Log($"Player receiving damage: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}, OrganBonuses={damageBreakdown.organBonuses?.Count ?? 0}");

        int totalDamage = damageBreakdown.totalDamage;
        int remainingDamage = totalDamage;

        // Apply defense if active
        if (hasDefense && activeDefense > 0)
        {
            int defendedDamage = Mathf.Min(activeDefense, remainingDamage);
            remainingDamage -= defendedDamage;
            activeDefense -= defendedDamage;

            // Show defense feedback
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.ShowDefend(
                    FeedbackManager.Instance.playerFeedbackSpawnPoint,
                    defendedDamage,
                    false,
                    "Player Defense"
                );
            }

            if (activeDefense <= 0)
            {
                hasDefense = false;
                activeDefense = 0;
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Calculate armor damage
        int armorDamage = 0;
        if (currentArmor > 0 && remainingDamage > 0)
        {
            armorDamage = Mathf.Min(currentArmor, remainingDamage);
            currentArmor -= armorDamage;
            remainingDamage -= armorDamage;
        }

        // Show ALL damage feedback with breakdown
        if (FeedbackManager.Instance != null && totalDamage > 0)
        {
            FeedbackManager.Instance.ShowTotalDamageWithOrganBreakdown(
                FeedbackManager.Instance.playerFeedbackSpawnPoint,
                damageBreakdown.baseDamage,
                damageBreakdown.organBonuses,
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

    void SetDefense(int defenseAmount)
    {
        activeDefense = defenseAmount;
        hasDefense = true;

        // Show defense activation feedback
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.ShowDefend(
                FeedbackManager.Instance.playerFeedbackSpawnPoint,
                defenseAmount,
                true,
                "Player Defense"
            );
        }

        Debug.Log($"Defense set to {defenseAmount} for next attack");
    }

    public void ClearDefense()
    {
        if (hasDefense)
        {
            Debug.Log($"Defense cleared (was {activeDefense})");
            hasDefense = false;
            activeDefense = 0;
        }
    }

    void UpdateOrganCooldown()
    {
        if (organCooldownTimer > 0)
        {
            organCooldownTimer--;
            if (organCooldownTimer <= 0)
            {
                organCooldownReady = true;
                Debug.Log("Organ cooldown ready!");
            }
        }
    }

    public void ProcessEndTurn()
    {
        UpdateOrganCooldown();
        ClearDefense();
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

    void CleanupBattlefield()
    {
        StopAllCoroutines();

        if (healthAnimationCoroutine != null)
            StopCoroutine(healthAnimationCoroutine);
        if (armorAnimationCoroutine != null)
            StopCoroutine(armorAnimationCoroutine);

        CleanupSpawnedEnerling();

        foreach (GameObject button in skillButtons)
        {
            Destroy(button);
        }
        skillButtons.Clear();

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
        activeDefense = 0;
        hasDefense = false;
        isAnimating = false;
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