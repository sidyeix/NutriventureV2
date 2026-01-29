using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AIEnerlingManager : MonoBehaviour
{
    [Header("AI Settings")]
    public float minDecisionTime = 1f;
    public float maxDecisionTime = 3f;

    [Header("Spawning")]
    public Transform aiSpawningPoint;

    [Header("UI References - AI Battlefield Info")]
    public TextMeshProUGUI aiEnerlingName;
    public Slider aiHealthSlider;
    public TextMeshProUGUI aiHealthText;
    public Slider aiArmorSlider;
    public TextMeshProUGUI aiArmorText;
    public Image aiFrame;
    public Image aiRarityTag;
    public Image aiEnerlingImage;
    public TextMeshProUGUI aiAbilityText;
    public Transform aiOrganPanel;
    public GameObject aiOrganImagePrefab;
    public Image aiNameStatsBG;

    [Header("AI NameStats BG Sprites by Rarity")]
    public Sprite aiCommonNameStatsBG;
    public Sprite aiRareNameStatsBG;
    public Sprite aiUltraRareNameStatsBG;

    // AI state
    private IngredientDatabase.IngredientInfo aiEnerling;
    private GameObject spawnedAIEnerling;
    private Animator aiAnimator;

    // Defend tracking
    private int currentAIArmor = 0;
    private int activeAIDefend = 0;
    private bool hasAIDefend = false;

    // Skill tracking
    private Dictionary<int, int> skillCooldowns = new Dictionary<int, int>();
    private List<int> availableSkills = new List<int>();

    // Organ cooldown tracking
    private int aiOrganCooldownTimer = 0;
    private int aiMaxOrganCooldown = 4; // Updated: Common=4, Rare=3, UltraRare=2
    private bool aiOrganCooldownReady = false;

    // References
    private BattleEnerlingManager battleManager;
    private TurnSystem turnSystem;
    private IngredientDatabase ingredientDatabase;

    // Organ bonus tracking
    private int aiOrganBonusDamage = 0;
    private int aiOrganBonusHeal = 0;
    private bool aiHasOrganDamageBonus = false;
    private bool aiHasOrganHealBonus = false;
    private List<string> aiOrganBonusNames = new List<string>();

    void Start()
    {
        battleManager = FindObjectOfType<BattleEnerlingManager>();
        turnSystem = FindObjectOfType<TurnSystem>();

        for (int i = 1; i <= 4; i++)
        {
            skillCooldowns[i] = 0;
        }
    }

    // Call this from TurnSystem when organ bonus is ready
    public void SetOrganDamageBonus(int bonusAmount, List<string> organs)
    {
        aiOrganBonusDamage = bonusAmount;
        aiOrganBonusNames = new List<string>(organs);
        aiHasOrganDamageBonus = true;
        Debug.Log($"AI organ damage bonus set: {bonusAmount} from {organs.Count} organs");
    }

    // Call this from TurnSystem when organ heal bonus is ready
    public void SetOrganHealBonus(int bonusAmount, List<string> organs)
    {
        aiOrganBonusHeal = bonusAmount;
        aiOrganBonusNames = new List<string>(organs);
        aiHasOrganHealBonus = true;
        Debug.Log($"AI organ heal bonus set: {bonusAmount} from {organs.Count} organs");
    }

    // Apply organ bonus to damage - FIXED to return BattleStructs.DamageBreakdown
    public BattleStructs.DamageBreakdown ApplyOrganDamageBonus(int baseDamage)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (aiHasOrganDamageBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI applying organ damage bonus: {aiOrganBonusDamage} from {aiOrganBonusNames.Count} organs");

            // Calculate individual organ bonus: 5% of base damage per organ (minimum 1)
            foreach (string organ in aiOrganBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseDamage * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"AI Organ {organ}: {organBonus} damage (5% of {baseDamage})");
            }

            // Reset bonus
            aiHasOrganDamageBonus = false;
            aiOrganBonusDamage = 0;
            aiOrganBonusNames.Clear();
        }

        return new BattleStructs.DamageBreakdown(baseDamage, organBonuses);
    }

    // Apply organ bonus to heal - FIXED to return BattleStructs.HealBreakdown
    public BattleStructs.HealBreakdown ApplyOrganHealBonus(int baseHeal)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (aiHasOrganHealBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI applying organ heal bonus: {aiOrganBonusHeal} from {aiOrganBonusNames.Count} organs");

            // Calculate individual organ bonus: 5% of base heal per organ (minimum 1)
            foreach (string organ in aiOrganBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseHeal * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"AI Organ {organ}: {organBonus} heal (5% of {baseHeal})");
            }

            // Reset bonus
            aiHasOrganHealBonus = false;
            aiOrganBonusHeal = 0;
            aiOrganBonusNames.Clear();
        }

        return new BattleStructs.HealBreakdown(baseHeal, organBonuses);
    }

    public void InitializeAIEnerling(string enerlingName, IngredientDatabase database) 
    {
        ingredientDatabase = database;

        Debug.Log($"Initializing AI Enerling in BATTLE SCENE: {enerlingName}");

        // Get the actual enerling data from database
        aiEnerling = CreateAICopy(enerlingName, database);

        if (aiEnerling == null)
        {
            Debug.LogError("Failed to create AI enerling copy");
            return;
        }

        currentAIArmor = CalculateArmorValue(aiEnerling);
        activeAIDefend = 0;
        hasAIDefend = false;

        InitializeAIOrganCooldown();

        // Use the aiSpawningPoint that's already assigned in the battle scene
        SpawnAIEnerling(); // This uses aiSpawningPoint directly

        for (int i = 1; i <= 4; i++)
        {
            skillCooldowns[i] = 0;
        }

        UpdateAvailableSkills();
        UpdateAIUI();

        Debug.Log($"AI Enerling initialized in battle scene: {aiEnerling.ingredientName}");
        Debug.Log($"- Using spawn point: {aiSpawningPoint?.name}");
    }

    IngredientDatabase.IngredientInfo CreateAICopy(string enerlingName, IngredientDatabase database)
    {
        var original = database.GetIngredientInfo(enerlingName);
        if (original == null) return null;

        return new IngredientDatabase.IngredientInfo
        {
            ingredientName = original.ingredientName,
            rarity = original.rarity,
            kingdom = original.kingdom,
            isUnlocked = original.isUnlocked,
            enerlingSprite = original.enerlingSprite,
            modelPrefab = original.modelPrefab,
            animatorController = original.animatorController,
            baseLife = original.baseLife,
            currentLife = original.baseLife,
            armorPercent = original.armorPercent,
            baseDamage = original.baseDamage,
            immuneToOrganDamage = original.immuneToOrganDamage,
            beneficialOrgans = new List<string>(original.beneficialOrgans),
            targetOrgans = new List<string>(original.targetOrgans),
            skill1 = original.skill1,
            skill2 = original.skill2,
            skill3 = original.skill3,
            skill4 = original.skill4,
            enerlingDescription = original.enerlingDescription
        };
    }

    void InitializeAIOrganCooldown()
    {
        if (aiEnerling == null) return;

        // Set cooldown based on rarity - UPDATED VALUES
        switch (aiEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                aiMaxOrganCooldown = 4;  // Changed from 5 to 4
                break;
            case IngredientDatabase.Rarity.Rare:
                aiMaxOrganCooldown = 3;  // Changed from 4 to 3
                break;
            case IngredientDatabase.Rarity.UltraRare:
                aiMaxOrganCooldown = 2;  // Changed from 3 to 2
                break;
        }

        aiOrganCooldownTimer = 0; // Start at 0
        aiOrganCooldownReady = false; // Not ready until we reach max cooldown

        Debug.Log($"AI Organ cooldown initialized: Timer={aiOrganCooldownTimer}/{aiMaxOrganCooldown} for {aiEnerling.rarity}");
    }

    void SpawnAIEnerling()
    {
        // First, check if we have valid data
        if (aiEnerling == null)
        {
            Debug.LogError("Cannot spawn AI enerling: aiEnerling is null!");
            return;
        }

        // Debug: Check what we have
        Debug.Log($"AI Spawning - Name: {aiEnerling.ingredientName}, Prefab: {aiEnerling.modelPrefab}");

        // Get the actual model prefab from the database if it's null
        if (aiEnerling.modelPrefab == null)
        {
            Debug.LogWarning($"AI enerling modelPrefab is null for: {aiEnerling.ingredientName}. Attempting to retrieve from database...");

            if (ingredientDatabase != null)
            {
                var original = ingredientDatabase.GetIngredientInfo(aiEnerling.ingredientName);
                if (original != null && original.modelPrefab != null)
                {
                    aiEnerling.modelPrefab = original.modelPrefab;
                    Debug.Log($"Retrieved model prefab from database for: {aiEnerling.ingredientName}");
                }
                else
                {
                    Debug.LogError($"No model prefab found in database for: {aiEnerling.ingredientName}");
                    return;
                }
            }
            else
            {
                Debug.LogError("IngredientDatabase is null, cannot retrieve model prefab!");
                return;
            }
        }

        // Ensure we have a spawning point
        if (aiSpawningPoint == null)
        {
            Debug.LogError("AI spawning point is null! Attempting to find it...");

            // Try to find it in the scene
            aiSpawningPoint = GameObject.Find("AISpawningPoint")?.transform;

            if (aiSpawningPoint == null)
            {
                // Look for any object with "AI" or "Enemy" in the name
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("AI") || obj.name.Contains("Enemy") || obj.name.Contains("Spawn"))
                    {
                        aiSpawningPoint = obj.transform;
                        Debug.Log($"Found potential spawn point: {obj.name}");
                        break;
                    }
                }

                // If still not found, create one
                if (aiSpawningPoint == null)
                {
                    GameObject spawnObj = new GameObject("AI_Enemy_Spawn_Point");
                    aiSpawningPoint = spawnObj.transform;
                    aiSpawningPoint.position = new Vector3(3, 0, 0); // Position it to the right
                    aiSpawningPoint.rotation = Quaternion.identity;
                    Debug.LogWarning("Created default AI spawn point at position (3, 0, 0)");
                }
            }
        }

        Debug.Log($"AI will spawn at: {aiSpawningPoint.name}, Position: {aiSpawningPoint.position}");

        // Clean up any existing spawned opponent
        if (spawnedAIEnerling != null)
        {
            Debug.Log($"Destroying existing AI enerling: {spawnedAIEnerling.name}");
            Destroy(spawnedAIEnerling);
        }

        // Spawn the AI enerling
        try
        {
            Debug.Log($"Instantiating AI enerling prefab: {aiEnerling.modelPrefab.name}");

            spawnedAIEnerling = Instantiate(aiEnerling.modelPrefab, aiSpawningPoint.position, aiSpawningPoint.rotation);

            // Make it a child of the spawning point for organization
            spawnedAIEnerling.transform.SetParent(aiSpawningPoint);

            // Reset local position and rotation to (0,0,0) relative to parent
            spawnedAIEnerling.transform.localPosition = Vector3.zero;
            spawnedAIEnerling.transform.localRotation = Quaternion.identity;
            spawnedAIEnerling.transform.localScale = Vector3.one;

            Debug.Log($"Successfully spawned AI enerling: {aiEnerling.ingredientName}");
            Debug.Log($"- Parent: {aiSpawningPoint.name}");
            Debug.Log($"- Local Position: {spawnedAIEnerling.transform.localPosition}");
            Debug.Log($"- World Position: {spawnedAIEnerling.transform.position}");

            // Add a debug component to make it visible
            spawnedAIEnerling.name = $"AI_{aiEnerling.ingredientName}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn AI enerling: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            return;
        }

        // Set up animator
        aiAnimator = spawnedAIEnerling.GetComponent<Animator>();
        if (aiAnimator != null)
        {
            if (aiEnerling.animatorController != null)
            {
                aiAnimator.runtimeAnimatorController = aiEnerling.animatorController;
                Debug.Log($"AI animator controller set: {aiEnerling.animatorController.name}");
            }
            else
            {
                Debug.LogWarning($"No animator controller assigned for AI enerling: {aiEnerling.ingredientName}");
            }
        }
        else
        {
            Debug.LogWarning($"Spawned AI enerling '{aiEnerling.ingredientName}' has no Animator component");
        }
    }

    public void UpdateAIUI()
    {
        if (aiEnerling == null) return;

        if (aiEnerlingName != null)
            aiEnerlingName.text = aiEnerling.ingredientName;

        if (aiHealthSlider != null)
        {
            aiHealthSlider.maxValue = aiEnerling.baseLife;
            aiHealthSlider.value = aiEnerling.currentLife;
        }

        if (aiHealthText != null)
        {
            aiHealthText.text = $"{aiEnerling.currentLife}/{aiEnerling.baseLife}";
            UpdateAIHealthTextColor();
        }

        if (aiArmorSlider != null)
        {
            aiArmorSlider.maxValue = CalculateArmorValue(aiEnerling);
            aiArmorSlider.value = currentAIArmor;
        }

        if (aiArmorText != null)
        {
            aiArmorText.text = $"{currentAIArmor}";
            UpdateAIArmorTextColor();
        }

        if (aiFrame != null)
        {
            Sprite frameSprite = GetFrameSprite(aiEnerling.rarity);
            if (frameSprite != null)
                aiFrame.sprite = frameSprite;
        }

        if (aiRarityTag != null)
        {
            Sprite raritySprite = GetRarityIcon(aiEnerling.rarity);
            if (raritySprite != null)
                aiRarityTag.sprite = raritySprite;
        }

        if (aiEnerlingImage != null && aiEnerling.enerlingSprite != null)
        {
            aiEnerlingImage.sprite = aiEnerling.enerlingSprite;
            aiEnerlingImage.preserveAspect = true;
        }

        if (aiAbilityText != null)
        {
            aiAbilityText.text = GetAbilityText(aiEnerling);
        }

        if (aiNameStatsBG != null)
        {
            UpdateAINameStatsBackground();
        }

        UpdateAIOrganPanel();
    }

    int CalculateArmorValue(IngredientDatabase.IngredientInfo enerling)
    {
        float armorDecimal = enerling.armorPercent / 100f;
        return Mathf.RoundToInt(enerling.baseLife * armorDecimal);
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

    Sprite GetFrameSprite(IngredientDatabase.Rarity rarity)
    {
        if (ingredientDatabase != null)
            return ingredientDatabase.GetFrameSprite(rarity);
        else if (battleManager != null && battleManager.ingredientDatabase != null)
            return battleManager.ingredientDatabase.GetFrameSprite(rarity);
        return null;
    }

    Sprite GetRarityIcon(IngredientDatabase.Rarity rarity)
    {
        if (ingredientDatabase != null)
            return ingredientDatabase.GetRarityIcon(rarity);
        else if (battleManager != null && battleManager.ingredientDatabase != null)
            return battleManager.ingredientDatabase.GetRarityIcon(rarity);
        return null;
    }

    void UpdateAINameStatsBackground()
    {
        if (aiNameStatsBG == null || aiEnerling == null) return;

        switch (aiEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                if (aiCommonNameStatsBG != null)
                    aiNameStatsBG.sprite = aiCommonNameStatsBG;
                break;
            case IngredientDatabase.Rarity.Rare:
                if (aiRareNameStatsBG != null)
                    aiNameStatsBG.sprite = aiRareNameStatsBG;
                break;
            case IngredientDatabase.Rarity.UltraRare:
                if (aiUltraRareNameStatsBG != null)
                    aiNameStatsBG.sprite = aiUltraRareNameStatsBG;
                break;
        }
    }

    void UpdateAIOrganPanel()
    {
        if (aiOrganPanel != null)
        {
            foreach (Transform child in aiOrganPanel)
            {
                Destroy(child.gameObject);
            }
        }

        if (aiEnerling == null || aiOrganImagePrefab == null || aiOrganPanel == null) return;

        List<string> organs = aiEnerling.beneficialOrgans.Count > 0 ?
            aiEnerling.beneficialOrgans : aiEnerling.targetOrgans;

        foreach (string organ in organs)
        {
            GameObject organImage = Instantiate(aiOrganImagePrefab, aiOrganPanel);
            Image image = organImage.GetComponent<Image>();

            Sprite organSprite = GetOrganSprite(organ);
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

    Sprite GetOrganSprite(string organName)
    {
        if (ingredientDatabase != null)
            return ingredientDatabase.GetOrganSprite(organName);
        else if (battleManager != null && battleManager.ingredientDatabase != null)
            return battleManager.ingredientDatabase.GetOrganSprite(organName);
        return null;
    }

    public void StartAITurn()
    {
        StartCoroutine(AIDecisionRoutine());
    }

    IEnumerator AIDecisionRoutine()
    {
        Debug.Log("AI starting decision process...");

        // Wait a moment before starting AI decision
        yield return new WaitForSeconds(0.5f);

        // Notify turn system that animation is starting
        if (turnSystem != null)
        {
            turnSystem.OnSkillAnimationStart();
        }

        float decisionTime = Random.Range(minDecisionTime, maxDecisionTime);
        Debug.Log($"AI thinking for {decisionTime} seconds...");
        yield return new WaitForSeconds(decisionTime);

        int chosenSkill = ChooseSkill();
        if (chosenSkill > 0)
        {
            var skill = GetSkillByNumber(chosenSkill);
            if (skill != null)
            {
                skillCooldowns[chosenSkill] = skill.cooldownTurns;
                UpdateAvailableSkills();
            }

            Debug.Log($"AI chose skill {chosenSkill}");

            // 1. Play animation FIRST
            yield return StartCoroutine(PlayAISkillAnimation(chosenSkill));

            // 2. Apply skill effect AFTER animation completes AND WAIT FOR IT
            yield return StartCoroutine(UseSkill(chosenSkill));

            // 3. Animation completed, notify turn system
            if (turnSystem != null)
            {
                turnSystem.OnSkillAnimationEnd();
            }
        }
        else
        {
            Debug.Log("AI has no available skills");

            // Still notify turn system
            if (turnSystem != null)
            {
                turnSystem.OnSkillAnimationEnd();
            }
        }
    }

    IEnumerator PlayAISkillAnimation(int skillNumber)
    {
        string animationBool = GetAnimationBoolName(skillNumber);
        if (aiAnimator != null && !string.IsNullOrEmpty(animationBool))
        {
            aiAnimator.SetBool(animationBool, true);
            Debug.Log($"AI playing animation: {animationBool}");

            // Wait for animation to complete using the generic wait coroutine
            yield return StartCoroutine(WaitForCurrentStateToFinish(aiAnimator));

            aiAnimator.SetBool(animationBool, false);

            // Wait a bit more to ensure animation resets
            yield return new WaitForSeconds(0.2f);

            // ADD THIS LINE - Wait for animation to be visually complete
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Debug.LogWarning($"No animator or animation bool found for AI skill {skillNumber}");
            yield return new WaitForSeconds(1f); // Fallback wait time
        }

        // DO NOT call UseSkill here anymore - it's called in AIDecisionRoutine
    }

    // Generic animation waiting coroutine
    IEnumerator WaitForCurrentStateToFinish(Animator animator, int layer = 0)
    {
        if (animator == null) yield break;

        // Wait until we are fully inside a state (not transitioning)
        while (animator.IsInTransition(layer))
            yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);

        // Wait until this state finishes playing
        while (state.normalizedTime < 1f || animator.IsInTransition(layer))
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(layer);
        }
    }

    int ChooseSkill()
    {
        UpdateAvailableSkills();

        if (availableSkills.Count == 0) return 0;

        float healthPercentage = (float)aiEnerling.currentLife / aiEnerling.baseLife;
        if (healthPercentage < 0.5f)
        {
            foreach (int skillNum in availableSkills)
            {
                var skill = GetSkillByNumber(skillNum);
                if (skill != null && skill.type == IngredientDatabase.SkillInfo.SkillType.Heal)
                {
                    return skillNum;
                }
            }
        }

        int strongestSkill = 0;
        int highestDamage = 0;

        foreach (int skillNum in availableSkills)
        {
            var skill = GetSkillByNumber(skillNum);
            if (skill != null && skill.type == IngredientDatabase.SkillInfo.SkillType.Damage)
            {
                int damage = CalculateAISkillEffect(skillNum);
                if (damage > highestDamage)
                {
                    highestDamage = damage;
                    strongestSkill = skillNum;
                }
            }
        }

        if (strongestSkill > 0) return strongestSkill;

        return availableSkills[Random.Range(0, availableSkills.Count)];
    }

    int CalculateAISkillEffect(int skillNumber)
    {
        var skill = GetSkillByNumber(skillNumber);
        if (skill == null) return 0;

        // Get base value without organ bonus
        return skill.GetValue();
    }

    IEnumerator UseSkill(int skillNumber)
    {
        int effect = CalculateAISkillEffect(skillNumber);
        var skill = GetSkillByNumber(skillNumber);

        if (skill != null)
        {
            bool aiHasBeneficialOrgans = aiEnerling.beneficialOrgans.Count > 0;
            bool aiHasTargetOrgans = aiEnerling.targetOrgans.Count > 0;

            switch (skill.type)
            {
                case IngredientDatabase.SkillInfo.SkillType.Heal:
                    // Apply organ heal bonus if available
                    BattleStructs.HealBreakdown healBreakdown = ApplyOrganHealBonus(effect);

                    Debug.Log($"AI healing: Base={healBreakdown.baseHeal}, Total={healBreakdown.totalHeal}, OrganBonuses={healBreakdown.organBonuses?.Count ?? 0}");

                    // Show TOTAL heal with breakdown
                    if (FeedbackManager.Instance != null)
                    {
                        FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                            FeedbackManager.Instance.aiFeedbackSpawnPoint,
                            healBreakdown.baseHeal,
                            healBreakdown.organBonuses,
                            false,
                            "AI Heal"
                        );
                    }

                    // Apply total heal AND WAIT FOR IT
                    yield return StartCoroutine(ApplyAIHeal(healBreakdown.totalHeal, 0));

                    // NOTE: REMOVED the beneficial organ healing from here
                    // It will now happen automatically at the start of each turn
                    break;

                case IngredientDatabase.SkillInfo.SkillType.Damage:
                    if (battleManager != null)
                    {
                        // Check if player has immuneToOrganDamage
                        bool playerImmune = false;
                        if (battleManager.GetBattleEnerling() != null)
                        {
                            playerImmune = battleManager.GetBattleEnerling().immuneToOrganDamage;
                        }

                        // Apply organ damage bonus if available
                        BattleStructs.DamageBreakdown damageBreakdown;

                        if (playerImmune && aiHasTargetOrgans)
                        {
                            Debug.Log($"Player is immune to organ damage. Only base damage will be applied.");
                            damageBreakdown = new BattleStructs.DamageBreakdown(effect, new List<FeedbackManager.OrganBonus>());
                        }
                        else
                        {
                            damageBreakdown = ApplyOrganDamageBonus(effect);
                        }

                        Debug.Log($"AI attacking: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}, OrganBonuses={damageBreakdown.organBonuses?.Count ?? 0}");

                        // Apply total damage AND WAIT FOR IT
                        yield return StartCoroutine(battleManager.ApplyDamageToPlayer(
                            damageBreakdown,
                            FeedbackManager.Instance != null ? FeedbackManager.Instance.playerFeedbackSpawnPoint : null
                        ));

                        // Target organ damage (triggers when cooldown is ready AND player is not immune)
                        if (aiHasTargetOrgans && aiOrganCooldownReady && !playerImmune)
                        {
                            Debug.Log($"AI Organ Damage Triggered! {aiEnerling.targetOrgans.Count} target organs");

                            // For EACH organ, calculate bonus
                            int organBonusPerOrgan = Mathf.RoundToInt(effect * 0.05f);
                            if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                            List<FeedbackManager.OrganBonus> cooldownBonuses = new List<FeedbackManager.OrganBonus>();
                            foreach (string organ in aiEnerling.targetOrgans)
                            {
                                cooldownBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonusPerOrgan));
                            }

                            // Create damage breakdown for cooldown bonuses
                            BattleStructs.DamageBreakdown cooldownDamage = new BattleStructs.DamageBreakdown(0, cooldownBonuses);

                            // Apply organ cooldown damage AND WAIT FOR IT
                            yield return StartCoroutine(battleManager.ApplyDamageToPlayer(
                                cooldownDamage,
                                FeedbackManager.Instance != null ? FeedbackManager.Instance.playerFeedbackSpawnPoint : null
                            ));

                            // Reset organ cooldown
                            aiOrganCooldownTimer = aiMaxOrganCooldown;
                            aiOrganCooldownReady = false;
                        }
                        else if (aiHasTargetOrgans && !playerImmune)
                        {
                            UpdateAIOrganCooldown();
                        }
                        else if (playerImmune)
                        {
                            Debug.Log("Player immune to organ damage - AI organ cooldown not activated");
                        }
                    }
                    break;

                case IngredientDatabase.SkillInfo.SkillType.Defend:
                    SetAIDefend(effect);  // Changed from SetAIDefense
                    // Wait a moment for defend feedback
                    yield return new WaitForSeconds(0.5f);
                    break;
            }
        }
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

    IngredientDatabase.SkillInfo GetSkillByNumber(int skillNumber)
    {
        if (aiEnerling == null) return null;

        switch (skillNumber)
        {
            case 1: return aiEnerling.skill1;
            case 2: return aiEnerling.skill2;
            case 3: return aiEnerling.skill3;
            case 4: return aiEnerling.skill4;
            default: return null;
        }
    }

    void UpdateAvailableSkills()
    {
        availableSkills.Clear();

        for (int i = 1; i <= 4; i++)
        {
            if (skillCooldowns[i] <= 0)
            {
                var skill = GetSkillByNumber(i);
                if (skill != null)
                {
                    availableSkills.Add(i);
                }
            }
        }
    }

    public IEnumerator TakeDamageWithFeedback(BattleStructs.DamageBreakdown damageBreakdown, Transform feedbackSpawnPoint)
    {
        Debug.Log($"AI receiving damage: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}, OrganBonuses={damageBreakdown.organBonuses?.Count ?? 0}");

        int totalDamage = damageBreakdown.totalDamage;
        int remainingDamage = totalDamage;

        // Apply AI defend if active - DEFEND BLOCKS THE NEXT ATTACK ONLY
        if (hasAIDefend && activeAIDefend > 0)
        {
            Debug.Log($"AI has defend: {activeAIDefend} against {totalDamage} damage");
            
            // Calculate how much damage defend can block
            int damageBlocked = Mathf.Min(activeAIDefend, remainingDamage);
            int damageThatGoesThrough = remainingDamage - damageBlocked;
            
            // Reduce defend by the damage blocked
            activeAIDefend -= damageBlocked;
            remainingDamage = damageThatGoesThrough;
            
            Debug.Log($"AI Defend blocked {damageBlocked} damage. Remaining defend: {activeAIDefend}, Damage that goes through: {damageThatGoesThrough}");

            // Show defend feedback
            if (FeedbackManager.Instance != null && damageBlocked > 0)
            {
                FeedbackManager.Instance.ShowDefend(
                    FeedbackManager.Instance.aiFeedbackSpawnPoint,
                    damageBlocked,
                    false,
                    "AI Defend"
                );
            }

            // Check if defend is used up
            if (activeAIDefend <= 0)
            {
                hasAIDefend = false;
                activeAIDefend = 0;
                Debug.Log("AI defend used up");
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Calculate armor damage (if defend didn't block all damage)
        int armorDamage = 0;
        if (currentAIArmor > 0 && remainingDamage > 0)
        {
            armorDamage = Mathf.Min(currentAIArmor, remainingDamage);
            currentAIArmor -= armorDamage;
            remainingDamage -= armorDamage;
        }

        // Show ALL damage feedback with breakdown
        if (FeedbackManager.Instance != null && totalDamage > 0)
        {
            FeedbackManager.Instance.ShowTotalDamageWithOrganBreakdown(
                FeedbackManager.Instance.aiFeedbackSpawnPoint,
                damageBreakdown.baseDamage,
                damageBreakdown.organBonuses,
                false,
                "Player Attack"
            );
        }

        // Apply armor change
        if (armorDamage > 0)
        {
            StartCoroutine(SmoothAIArmorChange(currentAIArmor + armorDamage, currentAIArmor, 0.3f));
            yield return new WaitForSeconds(0.3f);
        }

        // Remaining damage goes to health
        if (remainingDamage > 0)
        {
            int targetHealth = Mathf.Max(0, aiEnerling.currentLife - remainingDamage);

            StartCoroutine(PulseAIHealthSliderRed());

            StartCoroutine(SmoothAIHealthChange(aiEnerling.currentLife, targetHealth, 0.5f));
            aiEnerling.currentLife = targetHealth;

            if (aiEnerling.currentLife <= 0)
            {
                Debug.Log("AI defeated!");
            }
        }

        yield return null;
    }

    public IEnumerator ApplyAIHeal(int totalHeal, int organBonus)
    {
        // Show heal feedback - handled in UseSkill method

        int targetHealth = Mathf.Min(aiEnerling.currentLife + totalHeal, aiEnerling.baseLife);
        yield return StartCoroutine(SmoothAIHealthChange(aiEnerling.currentLife, targetHealth, 0.5f));
        aiEnerling.currentLife = targetHealth;
    }

    void SetAIDefend(int defendAmount)
    {
        activeAIDefend = defendAmount;
        hasAIDefend = true;

        // Show defend activation feedback
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.ShowDefend(
                FeedbackManager.Instance.aiFeedbackSpawnPoint,
                defendAmount,
                true,
                "AI Defend"
            );
        }

        Debug.Log($"AI Defend set to {defendAmount} for the next enemy attack");
    }

    public void ClearAIDefend()
    {
        if (hasAIDefend)
        {
            hasAIDefend = false;
            activeAIDefend = 0;
        }
    }

    void UpdateAIOrganCooldown()
    {
        // Increase cooldown timer each turn
        if (aiOrganCooldownTimer < aiMaxOrganCooldown)
        {
            aiOrganCooldownTimer++;
            Debug.Log($"AI Organ Cooldown: {aiOrganCooldownTimer}/{aiMaxOrganCooldown}");

            // Check if cooldown is now ready
            if (aiOrganCooldownTimer >= aiMaxOrganCooldown)
            {
                aiOrganCooldownReady = true;
                Debug.Log("AI Organ cooldown ready!");
            }
        }
    }

    public void ProcessEndTurn()
    {
        UpdateAIOrganCooldown();
        ClearAIDefend();
    }

    IEnumerator SmoothAIHealthChange(float startValue, float endValue, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, endValue, t);

            if (aiHealthSlider != null)
                aiHealthSlider.value = currentValue;

            if (aiHealthText != null)
            {
                aiHealthText.text = $"{(int)currentValue}/{aiEnerling.baseLife}";
                UpdateAIHealthTextColor();
            }

            yield return null;
        }

        if (aiHealthSlider != null)
            aiHealthSlider.value = endValue;

        if (aiHealthText != null)
        {
            aiHealthText.text = $"{(int)endValue}/{aiEnerling.baseLife}";
            UpdateAIHealthTextColor();
        }
    }

    IEnumerator SmoothAIArmorChange(float startValue, float endValue, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentValue = Mathf.Lerp(startValue, endValue, t);

            if (aiArmorSlider != null)
                aiArmorSlider.value = currentValue;

            if (aiArmorText != null)
            {
                aiArmorText.text = $"{(int)currentValue}";
                UpdateAIArmorTextColor();
            }

            yield return null;
        }

        if (aiArmorSlider != null)
            aiArmorSlider.value = endValue;

        if (aiArmorText != null)
        {
            aiArmorText.text = $"{(int)endValue}";
            UpdateAIArmorTextColor();
        }
    }

    IEnumerator PulseAIHealthSliderRed()
    {
        Image fillImage = aiHealthSlider?.fillRect?.GetComponent<Image>();
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

    void UpdateAIHealthTextColor()
    {
        if (aiHealthText == null || aiEnerling == null) return;

        float healthPercentage = (float)aiEnerling.currentLife / aiEnerling.baseLife;

        if (healthPercentage <= 0.33f)
            aiHealthText.color = Color.red;
        else if (healthPercentage <= 0.66f)
            aiHealthText.color = new Color(1f, 0.5f, 0f);
        else
            aiHealthText.color = Color.white;
    }

    void UpdateAIArmorTextColor()
    {
        if (aiArmorText == null || aiEnerling == null) return;

        int maxArmor = CalculateArmorValue(aiEnerling);
        float armorPercentage = maxArmor > 0 ? (float)currentAIArmor / maxArmor : 1f;

        if (armorPercentage <= 0.33f)
            aiArmorText.color = Color.red;
        else if (armorPercentage <= 0.66f)
            aiArmorText.color = new Color(1f, 0.5f, 0f);
        else
            aiArmorText.color = Color.white;
    }

    public void EndTurn()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (skillCooldowns[i] > 0)
            {
                skillCooldowns[i]--;
            }
        }
        UpdateAvailableSkills();

        ProcessOrganEffects();
    }

    void ProcessOrganEffects()
    {
        if (aiEnerling == null) return;

        int organCount = aiEnerling.OrganCount;
        if (organCount == 0) return;

        int bonusPercent = CalculateOrganBonusPercent(aiEnerling.rarity, organCount);
        int currentRound = turnSystem != null ? turnSystem.GetCurrentRound() : 1;
        int organCooldown = GetOrganCooldown(aiEnerling.rarity);

        if (organCooldown > 0 && currentRound % organCooldown == 0)
        {
            if (aiEnerling.beneficialOrgans.Count > 0)
            {
                int healAmount = Mathf.RoundToInt(aiEnerling.baseLife * (bonusPercent / 100f));
                StartCoroutine(ApplyAIHeal(healAmount, 0));
                Debug.Log($"AI Organ bonus: Healed {healAmount} HP");
            }
            else if (aiEnerling.targetOrgans.Count > 0)
            {
                Debug.Log($"AI Organ bonus: +{bonusPercent}% damage on next attack");
            }
        }
    }

    int CalculateOrganBonusPercent(IngredientDatabase.Rarity rarity, int organCount)
    {
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

    public void Cleanup()
    {
        StopAllCoroutines();

        if (spawnedAIEnerling != null)
        {
            Destroy(spawnedAIEnerling);
            spawnedAIEnerling = null;
        }

        skillCooldowns.Clear();
        availableSkills.Clear();

        if (aiOrganPanel != null)
        {
            foreach (Transform child in aiOrganPanel)
            {
                Destroy(child.gameObject);
            }
        }

        aiEnerling = null;
        ingredientDatabase = null;
        currentAIArmor = 0;
        activeAIDefend = 0;
        hasAIDefend = false;

        // Reset AI organ cooldown
        aiOrganCooldownTimer = 0;
        aiOrganCooldownReady = false;

        // Reset organ bonuses
        aiOrganBonusDamage = 0;
        aiOrganBonusHeal = 0;
        aiHasOrganDamageBonus = false;
        aiHasOrganHealBonus = false;
        aiOrganBonusNames.Clear();
    }

    public void CheckAndApplyOrganHeal()
    {
        if (aiEnerling == null || aiEnerling.beneficialOrgans.Count == 0) return;

        // Check if organ cooldown is ready
        if (aiOrganCooldownReady)
        {
            // Calculate heal amount: 5% of base life per organ
            int healPerOrgan = Mathf.RoundToInt(aiEnerling.baseLife * 0.05f);
            int totalHeal = healPerOrgan * aiEnerling.beneficialOrgans.Count; 

            // Create organ bonuses for feedback
            List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();
            foreach (string organ in aiEnerling.beneficialOrgans)
            {
                organBonuses.Add(new FeedbackManager.OrganBonus(organ, healPerOrgan));
            }

            // Show organ heal feedback
            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                    FeedbackManager.Instance.aiFeedbackSpawnPoint,
                    0, // No base heal, only organ bonus
                    organBonuses,
                    false,
                    "Beneficial Organ Heal"
                );
            }

            // Apply the heal
            StartCoroutine(ApplyAIHeal(totalHeal, 0));

            // Reset organ cooldown
            aiOrganCooldownTimer = aiMaxOrganCooldown;
            aiOrganCooldownReady = false;

            Debug.Log($"AI Beneficial Organ Heal Applied: {totalHeal} HP ({aiEnerling.beneficialOrgans.Count} organs x {healPerOrgan} HP each)");
        }
        else
        {
            Debug.Log($"AI Organ Cooldown: {aiOrganCooldownTimer} turns remaining");
        }
    }

    public IngredientDatabase.IngredientInfo GetAIEnerling()
    {
        return aiEnerling;
    }
}