using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AIEnerlingManager : MonoBehaviour
{
    [Header("AI Settings")]
    public float minDecisionTime = 0f;
    public float maxDecisionTime = 5f;

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

    [Header("Damage Feedback")]
    public GameObject damageFeedbackPrefab;
    public Transform aiFeedbackSpawnPoint;

    [Header("Organ Sprites")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;

    // AI state
    private IngredientDatabase.IngredientInfo aiEnerling;
    private GameObject spawnedAIEnerling;
    private Animator aiAnimator;

    // Defense tracking
    private int currentAIArmor = 0;
    private int activeAIDefense = 0;
    private bool hasAIDefense = false;

    // Skill tracking
    private Dictionary<int, int> skillCooldowns = new Dictionary<int, int>();
    private List<int> availableSkills = new List<int>();

    // Organ cooldown tracking
    private int aiOrganCooldownTimer = 0;
    private int aiMaxOrganCooldown = 5;
    private bool aiOrganCooldownReady = false;

    // References
    private BattleEnerlingManager battleManager;
    private TurnSystem turnSystem;
    private IngredientDatabase ingredientDatabase;

    // Feedback queue
    private Queue<FeedbackInfo> aiFeedbackQueue = new Queue<FeedbackInfo>();

    [Header("Shield Icon")]
    public Sprite shieldSprite;

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

    void Update()
    {
        // Process feedback queue with 0.5 second interval
        if (aiFeedbackQueue.Count > 0 && !IsInvoking("ProcessAIFeedback"))
        {
            Invoke("ProcessAIFeedback", 0.5f);
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

    // Apply organ bonus to damage
    public int ApplyOrganDamageBonus(int baseDamage)
    {
        if (aiHasOrganDamageBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI Applying organ damage bonus: {aiOrganBonusDamage} from {aiOrganBonusNames.Count} organs");

            // Calculate individual organ bonus: 5% of base damage per organ
            int organBonusPerOrgan = Mathf.RoundToInt(baseDamage * 0.05f);
            if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

            // Apply and show feedback for each organ
            foreach (string organ in aiOrganBonusNames)
            {
                // Show organ feedback
                ShowAIDamageFeedback(
                    organBonusPerOrgan,
                    false, // isHeal = false for damage
                    aiFeedbackSpawnPoint,
                    "Organ",
                    true,  // isOrganBonus = true
                    organ  // organName
                );

                // Apply damage for this organ (this will be handled in battle manager)
                if (battleManager != null)
                {
                    battleManager.StartCoroutine(battleManager.ApplyDamageToPlayer(
                        organBonusPerOrgan,
                        0,
                        organ
                    ));
                }
            }

            // Calculate total organ damage for return value
            int totalOrganDamage = organBonusPerOrgan * aiOrganBonusNames.Count;

            // Reset bonus
            aiHasOrganDamageBonus = false;
            aiOrganBonusDamage = 0;
            aiOrganBonusNames.Clear();

            return totalOrganDamage;
        }
        return 0;
    }

    // Apply organ bonus to heal
    public int ApplyOrganHealBonus(int baseHeal)
    {
        if (aiHasOrganHealBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI Applying organ heal bonus: {aiOrganBonusHeal} from {aiOrganBonusNames.Count} organs");

            // For EACH organ, show separate feedback
            int organCount = aiOrganBonusNames.Count;
            int bonusPerOrgan = Mathf.RoundToInt(aiOrganBonusHeal / (float)organCount);

            foreach (string organ in aiOrganBonusNames)
            {
                ShowAIDamageFeedback(
                    bonusPerOrgan,
                    true, // isHeal = true for healing
                    aiFeedbackSpawnPoint, // Show at AI position
                    "Organ",
                    true,  // isOrganBonus = true
                    organ  // organName
                );
            }

            // Reset bonus
            aiHasOrganHealBonus = false;
            aiOrganBonusHeal = 0;
            aiOrganBonusNames.Clear();

            return aiOrganBonusHeal;
        }
        return 0;
    }

    void ProcessAIFeedback()
    {
        if (aiFeedbackQueue.Count > 0)
        {
            var feedback = aiFeedbackQueue.Dequeue();
            ShowAIDamageFeedback(feedback.amount, feedback.isHeal, feedback.spawnPoint, feedback.type, feedback.isOrganBonus, feedback.organName);

            // Schedule next feedback after 0.5 seconds
            if (aiFeedbackQueue.Count > 0)
            {
                Invoke("ProcessAIFeedback", 0.5f);
            }
        }
    }

    public void InitializeAIEnerling(string enerlingName, IngredientDatabase database, Transform spawningPoint = null)
    {
        ingredientDatabase = database;

        if (spawningPoint != null)
        {
            aiSpawningPoint = spawningPoint;
        }

        aiEnerling = CreateAICopy(enerlingName, database);

        if (aiEnerling == null)
        {
            Debug.LogError("Failed to create AI enerling copy");
            return;
        }

        currentAIArmor = CalculateArmorValue(aiEnerling);
        activeAIDefense = 0;
        hasAIDefense = false;

        InitializeAIOrganCooldown();
        SpawnAIEnerling();

        for (int i = 1; i <= 4; i++)
        {
            skillCooldowns[i] = 0;
        }

        UpdateAvailableSkills();
        UpdateAIUI();

        Debug.Log($"AI Enerling initialized: {aiEnerling.ingredientName}");
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

        // Set cooldown based on rarity
        switch (aiEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                aiMaxOrganCooldown = 5;
                break;
            case IngredientDatabase.Rarity.Rare:
                aiMaxOrganCooldown = 4;
                break;
            case IngredientDatabase.Rarity.UltraRare:
                aiMaxOrganCooldown = 3;
                break;
        }

        aiOrganCooldownTimer = 0;
        aiOrganCooldownReady = false;

        Debug.Log($"AI Organ cooldown initialized: {aiMaxOrganCooldown} turns for {aiEnerling.rarity}");
    }

    void SpawnAIEnerling()
    {
        if (aiEnerling == null || aiEnerling.modelPrefab == null) return;

        if (spawnedAIEnerling != null)
        {
            Destroy(spawnedAIEnerling);
        }

        if (aiSpawningPoint == null) return;

        spawnedAIEnerling = Instantiate(aiEnerling.modelPrefab, aiSpawningPoint);
        spawnedAIEnerling.transform.localPosition = Vector3.zero;
        spawnedAIEnerling.transform.localRotation = Quaternion.identity;

        aiAnimator = spawnedAIEnerling.GetComponent<Animator>();
        if (aiAnimator != null && aiEnerling.animatorController != null)
        {
            aiAnimator.runtimeAnimatorController = aiEnerling.animatorController;
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
        if (turnSystem != null)
        {
            turnSystem.OnSkillAnimationStart(1f);
        }

        float decisionTime = Random.Range(minDecisionTime, maxDecisionTime);
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

            UseSkill(chosenSkill);
        }

        if (turnSystem != null)
        {
            turnSystem.OnSkillAnimationEnd();
        }

        yield return new WaitForSeconds(1f);

        if (turnSystem != null)
        {
            turnSystem.EndAITurn();
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

    void UseSkill(int skillNumber)
    {
        string animationBool = GetAnimationBoolName(skillNumber);
        if (aiAnimator != null && !string.IsNullOrEmpty(animationBool))
        {
            StartCoroutine(PlayAIAnimation(animationBool, skillNumber));
        }

        int effect = CalculateAISkillEffect(skillNumber);
        var skill = GetSkillByNumber(skillNumber);

        if (skill != null)
        {
            bool hasBeneficialOrgans = aiEnerling.beneficialOrgans.Count > 0;
            bool hasTargetOrgans = aiEnerling.targetOrgans.Count > 0;

            switch (skill.type)
            {
                case IngredientDatabase.SkillInfo.SkillType.Heal:
                    // Apply organ heal bonus if available
                    int organHealBonus = ApplyOrganHealBonus(effect);

                    // Get base effect WITHOUT organ bonus
                    int baseHeal = skill.GetValue();

                    // Show base heal feedback
                    ShowAIDamageFeedback(baseHeal, true, aiFeedbackSpawnPoint, "Heal", false, "");

                    // Apply total heal
                    StartCoroutine(ApplyAIHeal(baseHeal + organHealBonus, 0));

                    // Beneficial organ healing (triggers when cooldown is ready)
                    if (hasBeneficialOrgans && aiOrganCooldownReady)
                    {
                        Debug.Log($"AI Organ Heal Triggered! {aiEnerling.beneficialOrgans.Count} beneficial organs");

                        // For EACH organ, calculate and show feedback
                        int organCount = aiEnerling.beneficialOrgans.Count;
                        int organBonusPerOrgan = Mathf.RoundToInt(baseHeal * 0.05f);
                        if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                        foreach (string organ in aiEnerling.beneficialOrgans)
                        {
                            // Show organ feedback
                            ShowAIDamageFeedback(
                                organBonusPerOrgan,
                                true, // isHeal = true for healing
                                aiFeedbackSpawnPoint,
                                "Organ",
                                true, // isOrganBonus = true
                                organ // organName
                            );

                            // Apply heal for this organ
                            StartCoroutine(ApplyAIHeal(organBonusPerOrgan, 0));
                        }

                        // Reset organ cooldown
                        aiOrganCooldownTimer = aiMaxOrganCooldown;
                        aiOrganCooldownReady = false;
                    }
                    else if (hasBeneficialOrgans)
                    {
                        UpdateAIOrganCooldown();
                    }
                    break;

                case IngredientDatabase.SkillInfo.SkillType.Damage:
                    if (battleManager != null)
                    {
                        // Apply organ damage bonus if available
                        int organDamageBonus = ApplyOrganDamageBonus(effect);

                        // Get base effect WITHOUT organ bonus
                        int baseDamage = skill.GetValue();

                        // Show base damage feedback
                        ShowAIDamageFeedback(baseDamage, false, aiFeedbackSpawnPoint, "Damage", false, "");

                        // Apply total damage
                        battleManager.StartCoroutine(battleManager.ApplyDamageToPlayer(baseDamage + organDamageBonus, 0, ""));

                        // Target organ damage (triggers when cooldown is ready)
                        if (hasTargetOrgans && aiOrganCooldownReady)
                        {
                            Debug.Log($"AI Organ Damage Triggered! {aiEnerling.targetOrgans.Count} target organs");

                            // For EACH organ, calculate and show feedback
                            int organCount = aiEnerling.targetOrgans.Count;
                            int organBonusPerOrgan = Mathf.RoundToInt(baseDamage * 0.05f);
                            if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                            foreach (string organ in aiEnerling.targetOrgans)
                            {
                                // Show organ feedback
                                ShowAIDamageFeedback(
                                    organBonusPerOrgan,
                                    false, // isHeal = false for damage
                                    aiFeedbackSpawnPoint,
                                    "Organ",
                                    true, // isOrganBonus = true
                                    organ // organName
                                );

                                // Apply organ damage
                                battleManager.StartCoroutine(battleManager.ApplyDamageToPlayer(
                                    organBonusPerOrgan,
                                    0,
                                    organ
                                ));
                            }

                            // Reset organ cooldown
                            aiOrganCooldownTimer = aiMaxOrganCooldown;
                            aiOrganCooldownReady = false;
                        }
                        else if (hasTargetOrgans)
                        {
                            UpdateAIOrganCooldown();
                        }
                    }
                    break;

                case IngredientDatabase.SkillInfo.SkillType.Defend:
                    SetAIDefense(effect);
                    break;
            }
        }
    }

    float CalculateOrganBonusPercentage(int organCount)
    {
        // 5% per organ
        return organCount * 5f;
    }

    IEnumerator PlayAIAnimation(string animationBool, int skillNumber)
    {
        if (aiAnimator == null) yield break;

        aiAnimator.SetBool(animationBool, true);
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(0.5f);
        aiAnimator.SetBool(animationBool, false);
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

    public IEnumerator TakeDamageWithFeedback(int totalDamage, int organBonusDamage, Transform feedbackSpawnPoint, string organName = "")
    {
        Debug.Log($"AI TakeDamageWithFeedback: Total={totalDamage}, OrganBonus={organBonusDamage}, OrganName={organName}");

        int remainingDamage = totalDamage;

        // Apply AI defense if active
        if (hasAIDefense && activeAIDefense > 0)
        {
            int defendedDamage = Mathf.Min(activeAIDefense, remainingDamage);
            remainingDamage -= defendedDamage;
            activeAIDefense -= defendedDamage;

            // Show defense feedback
            aiFeedbackQueue.Enqueue(new FeedbackInfo(defendedDamage, false, aiFeedbackSpawnPoint, "Defend", false, ""));

            if (activeAIDefense <= 0)
            {
                hasAIDefense = false;
                activeAIDefense = 0;
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Damage goes to armor first
        if (currentAIArmor > 0 && remainingDamage > 0)
        {
            int armorDamage = Mathf.Min(currentAIArmor, remainingDamage);
            StartCoroutine(SmoothAIArmorChange(currentAIArmor, currentAIArmor - armorDamage, 0.3f));
            currentAIArmor -= armorDamage;
            remainingDamage -= armorDamage;

            // Show armor damage feedback
            aiFeedbackQueue.Enqueue(new FeedbackInfo(armorDamage, false, aiFeedbackSpawnPoint, "Armor", false, ""));

            yield return new WaitForSeconds(0.3f);
        }

        // Show base damage feedback
        int baseDamage = totalDamage - organBonusDamage;
        if (baseDamage > 0 && remainingDamage > 0)
        {
            aiFeedbackQueue.Enqueue(new FeedbackInfo(baseDamage, false, aiFeedbackSpawnPoint, "Damage", false, ""));
        }

        // Show organ bonus damage feedback
        if (organBonusDamage > 0 && remainingDamage > 0)
        {
            aiFeedbackQueue.Enqueue(new FeedbackInfo(organBonusDamage, false, aiFeedbackSpawnPoint, "Organ", true, organName));
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

    public IEnumerator ApplyAIHeal(int baseHeal, int organBonus)
    {
        int totalHeal = baseHeal + organBonus;

        // Show heal feedback
        aiFeedbackQueue.Enqueue(new FeedbackInfo(totalHeal, true, aiFeedbackSpawnPoint, "Heal", false, ""));

        int targetHealth = Mathf.Min(aiEnerling.currentLife + totalHeal, aiEnerling.baseLife);
        yield return StartCoroutine(SmoothAIHealthChange(aiEnerling.currentLife, targetHealth, 0.5f));
        aiEnerling.currentLife = targetHealth;
    }

    IEnumerator SpawnAIOrganFeedbacks(List<string> organs, bool isHeal, int totalAmount)
    {
        if (organs == null || organs.Count == 0) yield break;

        // Calculate individual amount per organ
        int individualAmount = Mathf.RoundToInt(totalAmount / (float)organs.Count);
        if (individualAmount < 1) individualAmount = 1;

        foreach (string organ in organs)
        {
            // Queue organ feedback
            aiFeedbackQueue.Enqueue(new FeedbackInfo(
                individualAmount,
                isHeal,
                aiFeedbackSpawnPoint,
                "Organ",
                true,
                organ
            ));

            yield return new WaitForSeconds(0.2f);
        }
    }

    void SetAIDefense(int defenseAmount)
    {
        activeAIDefense = defenseAmount;
        hasAIDefense = true;

        // Show defense activation feedback
        aiFeedbackQueue.Enqueue(new FeedbackInfo(defenseAmount, false, aiFeedbackSpawnPoint, "Defend Active", false, ""));
    }

    public void ClearAIDefense()
    {
        if (hasAIDefense)
        {
            hasAIDefense = false;
            activeAIDefense = 0;
        }
    }

    void UpdateAIOrganCooldown()
    {
        if (aiOrganCooldownTimer > 0)
        {
            aiOrganCooldownTimer--;
            if (aiOrganCooldownTimer <= 0)
            {
                aiOrganCooldownReady = true;
                Debug.Log("AI Organ cooldown ready!");
            }
        }
    }

    public void ProcessEndTurn()
    {
        UpdateAIOrganCooldown();
    }

    void ShowAIDamageFeedback(int amount, bool isHeal, Transform spawnPoint, string type, bool isOrganBonus, string organName = "")
    {
        if (damageFeedbackPrefab == null || spawnPoint == null) return;

        // Create a random position within the specified bounds
        // Y = -0.23 to 0.40, X = 0 to 1.21, Z = 0 to 0.78
        float randomX = Random.Range(0f, 1.21f);
        float randomY = Random.Range(-0.23f, 0.40f);
        float randomZ = Random.Range(0f, 0.78f);

        Vector3 randomPosition = new Vector3(randomX, randomY, randomZ);

        GameObject feedback = Instantiate(damageFeedbackPrefab, spawnPoint);
        feedback.transform.localPosition = randomPosition;

        Debug.Log($"AI SHOWING FEEDBACK: Amount={amount}, IsHeal={isHeal}, Type={type}, IsOrganBonus={isOrganBonus}, OrganName={organName}");

        // Set damage text
        Transform damageTransform = feedback.transform.Find("Damage");
        if (damageTransform != null)
        {
            TextMeshProUGUI damageText = damageTransform.GetComponent<TextMeshProUGUI>();
            if (damageText != null)
            {
                // For Defend type, show only the number
                if (type == "Defend" || type == "Defend Active")
                {
                    damageText.text = $"{amount}";
                    damageText.color = Color.yellow;
                }
                else
                {
                    damageText.text = isHeal ? $"+{amount}" : $"-{amount}";

                    if (isHeal)
                        damageText.color = Color.green;
                    else if (isOrganBonus)
                        damageText.color = new Color(1f, 0.5f, 0f);
                    else
                        damageText.color = Color.red;
                }
            }
        }

        // Set organ sprite
        Transform organTransform = feedback.transform.Find("Organ");
        if (organTransform != null)
        {
            Image organImage = organTransform.GetComponent<Image>();
            TextMeshProUGUI organText = organTransform.GetComponent<TextMeshProUGUI>();

            // Hide text component
            if (organText != null)
                organText.gameObject.SetActive(false);

            // Show organ sprite for organ bonuses
            if (isOrganBonus && !string.IsNullOrEmpty(organName))
            {
                if (organImage != null)
                {
                    Sprite organSprite = GetOrganSpriteFromName(organName);
                    if (organSprite != null)
                    {
                        organImage.sprite = organSprite;
                        organImage.preserveAspect = true;
                        organImage.gameObject.SetActive(true);
                    }
                }
            }
            // Show shield for defend skills
            else if (type == "Defend" || type == "Defend Active")
            {
                if (organImage != null && shieldSprite != null)
                {
                    organImage.sprite = shieldSprite;
                    organImage.preserveAspect = true;
                    organImage.gameObject.SetActive(true);
                }
            }
            // Hide for everything else
            else
            {
                if (organImage != null)
                    organImage.gameObject.SetActive(false);
            }
        }

        // Animate
        StartCoroutine(MoveAIFeedbackUpwards(feedback.transform));

        CanvasGroup canvasGroup = feedback.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = feedback.AddComponent<CanvasGroup>();

        StartCoroutine(FadeOutAIFeedback(canvasGroup, feedback));
    }

    Sprite GetOrganSpriteFromName(string organName)
    {
        if (string.IsNullOrEmpty(organName)) return null;

        // Try to get from local sprites first
        switch (organName.ToLower())
        {
            case "heart":
                return heartSprite;
            case "liver":
                return liverSprite;
            case "kidney":
            case "kidneys":
                return kidneySprite;
            case "pancreas":
                return pancreasSprite;
            case "brain":
                return brainSprite;
            default:
                // Try to get from database
                if (ingredientDatabase != null)
                    return ingredientDatabase.GetOrganSprite(organName);
                else if (battleManager != null && battleManager.ingredientDatabase != null)
                    return battleManager.ingredientDatabase.GetOrganSprite(organName);
                return null;
        }
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

    IEnumerator MoveAIFeedbackUpwards(Transform feedbackTransform)
    {
        if (feedbackTransform == null) yield break;

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = feedbackTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 0.33f, 0);

        while (elapsed < duration && feedbackTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            feedbackTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    IEnumerator FadeOutAIFeedback(CanvasGroup canvasGroup, GameObject feedback)
    {
        if (canvasGroup == null || feedback == null) yield break;

        yield return new WaitForSeconds(1f);

        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration && canvasGroup != null && feedback != null)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        if (feedback != null)
            Destroy(feedback);
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
        activeAIDefense = 0;
        hasAIDefense = false;
        aiFeedbackQueue.Clear();

        // Reset AI organ cooldown
        aiOrganCooldownTimer = 0;
        aiOrganCooldownReady = false;
    }

    public IngredientDatabase.IngredientInfo GetAIEnerling()
    {
        return aiEnerling;
    }

    // Helper struct for feedback queue
    private struct FeedbackInfo
    {
        public int amount;
        public bool isHeal;
        public Transform spawnPoint;
        public string type;
        public bool isOrganBonus;
        public string organName;

        public FeedbackInfo(int amount, bool isHeal, Transform spawnPoint, string type, bool isOrganBonus, string organName)
        {
            this.amount = amount;
            this.isHeal = isHeal;
            this.spawnPoint = spawnPoint;
            this.type = type;
            this.isOrganBonus = isOrganBonus;
            this.organName = organName;
        }
    }
}