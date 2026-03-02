using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video; // Add this for VideoPlayer

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

    [Header("Video Player Reference")]
    public VideoPlayer endingVideoPlayer; // Reference to the VideoPlayer in EndingCutsceneCanvas

    private IngredientDatabase.IngredientInfo aiEnerling;
    private GameObject spawnedAIEnerling;
    private Animator aiAnimator;

    private int currentAIArmor = 0;
    private int activeAIDefend = 0;
    private bool hasAIDefend = false;

    private Dictionary<int, int> skillCooldowns = new Dictionary<int, int>();
    private List<int> availableSkills = new List<int>();

    private int aiOrganCooldownTimer = 0;
    private int aiMaxOrganCooldown = 4;
    private bool aiOrganCooldownReady = false;

    private BattleEnerlingManager battleManager;
    private TurnSystem turnSystem;
    private IngredientDatabase ingredientDatabase;

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

        // If video player not assigned, try to find it
        if (endingVideoPlayer == null)
        {
            endingVideoPlayer = FindObjectOfType<VideoPlayer>();
            if (endingVideoPlayer != null)
                Debug.Log("Found VideoPlayer automatically");
        }
    }

    public void SetOrganDamageBonus(int bonusAmount, List<string> organs)
    {
        aiOrganBonusDamage = bonusAmount;
        aiOrganBonusNames = new List<string>(organs);
        aiHasOrganDamageBonus = true;
        Debug.Log($"AI organ damage bonus set: {bonusAmount} from {organs.Count} organs");
    }

    public void SetOrganHealBonus(int bonusAmount, List<string> organs)
    {
        aiOrganBonusHeal = bonusAmount;
        aiOrganBonusNames = new List<string>(organs);
        aiHasOrganHealBonus = true;
        Debug.Log($"AI organ heal bonus set: {bonusAmount} from {organs.Count} organs");
    }

    public BattleStructs.DamageBreakdown ApplyOrganDamageBonus(int baseDamage)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (aiHasOrganDamageBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI applying organ damage bonus: {aiOrganBonusDamage} from {aiOrganBonusNames.Count} organs");

            foreach (string organ in aiOrganBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseDamage * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"AI Organ {organ}: {organBonus} damage (5% of {baseDamage})");
            }

            aiHasOrganDamageBonus = false;
            aiOrganBonusDamage = 0;
            aiOrganBonusNames.Clear();
        }

        return new BattleStructs.DamageBreakdown(baseDamage, organBonuses);
    }

    public BattleStructs.HealBreakdown ApplyOrganHealBonus(int baseHeal)
    {
        List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();

        if (aiHasOrganHealBonus && aiOrganBonusNames.Count > 0)
        {
            Debug.Log($"AI applying organ heal bonus: {aiOrganBonusHeal} from {aiOrganBonusNames.Count} organs");

            foreach (string organ in aiOrganBonusNames)
            {
                int organBonus = Mathf.RoundToInt(baseHeal * 0.05f);
                if (organBonus < 1) organBonus = 1;

                organBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonus));
                Debug.Log($"AI Organ {organ}: {organBonus} heal (5% of {baseHeal})");
            }

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
        InitializeAISkillCooldowns();
        SpawnAIEnerling();

        for (int i = 1; i <= 4; i++)
        {
            skillCooldowns[i] = 0;
        }

        UpdateAvailableSkills();
        UpdateAIUI();

        // Preload the ending cutscene video
        PreloadEndingCutscene();

        Debug.Log($"AI Enerling initialized in battle scene: {aiEnerling.ingredientName}");
    }

    void InitializeAISkillCooldowns()
    {
        if (aiEnerling == null) return;

        for (int i = 1; i <= 4; i++)
        {
            var skill = GetSkillByNumber(i);
            if (skill != null && skill.cooldownTurns > 0)
            {
                skillCooldowns[i] = skill.cooldownTurns;
                Debug.Log($"AI Skill {i} starts on cooldown: {skill.cooldownTurns} turns");
            }
            else
            {
                skillCooldowns[i] = 0;
            }
        }
    }

    // FIXED: Added endingCutscene to the copy
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
            enerlingDescription = original.enerlingDescription,
            endingCutscene = original.endingCutscene // CRITICAL FIX: Copy the ending cutscene video
        };
    }

    // NEW METHOD: Preload the ending cutscene video using VideoPlayer
    public void PreloadEndingCutscene()
    {
        if (aiEnerling == null)
        {
            Debug.Log("AI Enerling is null, cannot preload video");
            return;
        }

        if (aiEnerling.endingCutscene == null)
        {
            Debug.Log($"No ending cutscene to preload for {aiEnerling.ingredientName}");
            return;
        }

        if (endingVideoPlayer == null)
        {
            Debug.LogError("endingVideoPlayer is null! Cannot preload video. Make sure to assign it in the inspector.");
            return;
        }

        Debug.Log($"Preloading ending cutscene for {aiEnerling.ingredientName}: {aiEnerling.endingCutscene.name}");

        // Set the clip and prepare it
        endingVideoPlayer.clip = aiEnerling.endingCutscene;
        endingVideoPlayer.Prepare();
    }

    void InitializeAIOrganCooldown()
    {
        if (aiEnerling == null) return;

        switch (aiEnerling.rarity)
        {
            case IngredientDatabase.Rarity.Common:
                aiMaxOrganCooldown = 4;
                break;
            case IngredientDatabase.Rarity.Rare:
                aiMaxOrganCooldown = 3;
                break;
            case IngredientDatabase.Rarity.UltraRare:
                aiMaxOrganCooldown = 2;
                break;
        }

        aiOrganCooldownTimer = 0;
        aiOrganCooldownReady = false;

        Debug.Log($"AI Organ cooldown initialized: Timer={aiOrganCooldownTimer}/{aiMaxOrganCooldown} for {aiEnerling.rarity}");
    }

    void SpawnAIEnerling()
    {
        if (aiEnerling == null)
        {
            Debug.LogError("Cannot spawn AI enerling: aiEnerling is null!");
            return;
        }

        Debug.Log($"AI Spawning - Name: {aiEnerling.ingredientName}");

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

        if (aiSpawningPoint == null)
        {
            Debug.LogError("AI spawning point is null! Attempting to find it...");

            aiSpawningPoint = GameObject.Find("AISpawningPoint")?.transform;

            if (aiSpawningPoint == null)
            {
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

                if (aiSpawningPoint == null)
                {
                    GameObject spawnObj = new GameObject("AI_Enemy_Spawn_Point");
                    aiSpawningPoint = spawnObj.transform;
                    aiSpawningPoint.position = new Vector3(3, 0, 0);
                    aiSpawningPoint.rotation = Quaternion.identity;
                    Debug.LogWarning("Created default AI spawn point at position (3, 0, 0)");
                }
            }
        }

        Debug.Log($"AI will spawn at: {aiSpawningPoint.name}, Position: {aiSpawningPoint.position}");

        if (spawnedAIEnerling != null)
        {
            Debug.Log($"Destroying existing AI enerling: {spawnedAIEnerling.name}");
            Destroy(spawnedAIEnerling);
        }

        try
        {
            Debug.Log($"Instantiating AI enerling prefab: {aiEnerling.modelPrefab.name}");

            spawnedAIEnerling = Instantiate(aiEnerling.modelPrefab, aiSpawningPoint.position, aiSpawningPoint.rotation);
            spawnedAIEnerling.transform.SetParent(aiSpawningPoint);
            spawnedAIEnerling.transform.localPosition = Vector3.zero;
            spawnedAIEnerling.transform.localRotation = Quaternion.identity;
            spawnedAIEnerling.transform.localScale = Vector3.one;

            Debug.Log($"Successfully spawned AI enerling: {aiEnerling.ingredientName}");
            Debug.Log($"- Parent: {aiSpawningPoint.name}");

            spawnedAIEnerling.name = $"AI_{aiEnerling.ingredientName}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to spawn AI enerling: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
            return;
        }

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

        yield return new WaitForSeconds(0.5f);

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

            yield return StartCoroutine(PlayAISkillAnimation(chosenSkill));

            yield return StartCoroutine(UseSkill(chosenSkill));

            if (turnSystem != null)
            {
                turnSystem.OnSkillAnimationEnd();
            }
        }
        else
        {
            Debug.Log("AI has no available skills");

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

            yield return StartCoroutine(WaitForCurrentStateToFinish(aiAnimator));

            aiAnimator.SetBool(animationBool, false);

            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Debug.LogWarning($"No animator or animation bool found for AI skill {skillNumber}");
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator WaitForCurrentStateToFinish(Animator animator, int layer = 0)
    {
        if (animator == null) yield break;

        while (animator.IsInTransition(layer))
            yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);

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

        if (aiEnerling.rarity == IngredientDatabase.Rarity.Common)
        {
            foreach (int skillNum in availableSkills)
            {
                var skill = GetSkillByNumber(skillNum);
                if (skill != null && skill.type == IngredientDatabase.SkillInfo.SkillType.Damage)
                {
                    return skillNum;
                }
            }

            if (healthPercentage < 0.3f)
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
        }
        else
        {
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

        int baseDamage = skill.GetValue();

        if (battleManager != null && battleManager.GetBattleEnerling() != null)
        {
            var playerEnerling = battleManager.GetBattleEnerling();
            float multiplier = GetRarityDamageMultiplier(aiEnerling.rarity, playerEnerling.rarity);
            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        return baseDamage;
    }

    private float GetRarityDamageMultiplier(IngredientDatabase.Rarity attackerRarity, IngredientDatabase.Rarity defenderRarity)
    {
        if (attackerRarity == IngredientDatabase.Rarity.Common && defenderRarity == IngredientDatabase.Rarity.Common)
        {
            return 0.6f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.Rare && defenderRarity == IngredientDatabase.Rarity.Rare)
        {
            return 0.85f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.UltraRare && defenderRarity == IngredientDatabase.Rarity.UltraRare)
        {
            return 1.0f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.UltraRare && defenderRarity == IngredientDatabase.Rarity.Common)
        {
            return 1.2f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.UltraRare && defenderRarity == IngredientDatabase.Rarity.Rare)
        {
            return 1.15f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.Rare && defenderRarity == IngredientDatabase.Rarity.Common)
        {
            return 1.1f;
        }

        if (attackerRarity == IngredientDatabase.Rarity.Common && defenderRarity == IngredientDatabase.Rarity.UltraRare)
        {
            return 0.8f;
        }

        return 1.0f;
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
                    BattleStructs.HealBreakdown healBreakdown = ApplyOrganHealBonus(effect);

                    Debug.Log($"AI healing: Base={healBreakdown.baseHeal}, Total={healBreakdown.totalHeal}");

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

                    yield return StartCoroutine(ApplyAIHeal(healBreakdown.totalHeal, 0));
                    break;

                case IngredientDatabase.SkillInfo.SkillType.Damage:
                    if (battleManager != null)
                    {
                        bool playerImmune = false;
                        if (battleManager.GetBattleEnerling() != null)
                        {
                            playerImmune = battleManager.GetBattleEnerling().immuneToOrganDamage;
                        }

                        BattleStructs.DamageBreakdown damageBreakdown;

                        if (playerImmune)
                        {
                            Debug.Log($"Player is immune to organ damage. Only base damage will be applied.");
                            damageBreakdown = new BattleStructs.DamageBreakdown(effect, new List<FeedbackManager.OrganBonus>());
                        }
                        else
                        {
                            damageBreakdown = ApplyOrganDamageBonus(effect);
                        }

                        Debug.Log($"AI attacking: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}");

                        yield return StartCoroutine(battleManager.ApplyDamageToPlayer(
                            damageBreakdown,
                            FeedbackManager.Instance != null ? FeedbackManager.Instance.playerFeedbackSpawnPoint : null
                        ));

                        if (aiHasTargetOrgans && aiOrganCooldownReady && !playerImmune)
                        {
                            Debug.Log($"AI Organ Damage Triggered! {aiEnerling.targetOrgans.Count} target organs");

                            int organBonusPerOrgan = Mathf.RoundToInt(effect * 0.05f);
                            if (organBonusPerOrgan < 1) organBonusPerOrgan = 1;

                            List<FeedbackManager.OrganBonus> cooldownBonuses = new List<FeedbackManager.OrganBonus>();
                            foreach (string organ in aiEnerling.targetOrgans)
                            {
                                cooldownBonuses.Add(new FeedbackManager.OrganBonus(organ, organBonusPerOrgan));
                            }

                            BattleStructs.DamageBreakdown cooldownDamage = new BattleStructs.DamageBreakdown(0, cooldownBonuses);

                            yield return StartCoroutine(battleManager.ApplyDamageToPlayer(
                                cooldownDamage,
                                FeedbackManager.Instance != null ? FeedbackManager.Instance.playerFeedbackSpawnPoint : null
                            ));

                            aiOrganCooldownTimer = 0;
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
                    SetAIDefend(effect);
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

        Debug.Log($"AI available skills: {availableSkills.Count}");
    }

    public IEnumerator TakeDamageWithFeedback(BattleStructs.DamageBreakdown damageBreakdown, Transform feedbackSpawnPoint)
    {
        Debug.Log($"AI receiving damage: Base={damageBreakdown.baseDamage}, Total={damageBreakdown.totalDamage}");

        int totalDamage = damageBreakdown.totalDamage;
        int remainingDamage = totalDamage;

        if (hasAIDefend && activeAIDefend > 0)
        {
            Debug.Log($"AI has defend: {activeAIDefend} against {totalDamage} damage");

            int damageBlocked = Mathf.Min(activeAIDefend, remainingDamage);
            int damageThatGoesThrough = remainingDamage - damageBlocked;

            activeAIDefend -= damageBlocked;
            remainingDamage = damageThatGoesThrough;

            Debug.Log($"AI Defend blocked {damageBlocked} damage. Remaining defend: {activeAIDefend}, Damage that goes through: {damageThatGoesThrough}");

            if (FeedbackManager.Instance != null)
            {
                if (damageBlocked > 0)
                {
                    FeedbackManager.Instance.ShowDefend(
                        FeedbackManager.Instance.aiFeedbackSpawnPoint,
                        damageBlocked,
                        false,
                        "AI Defend Block"
                    );
                }
            }

            hasAIDefend = false;
            activeAIDefend = 0;
            Debug.Log("AI defend used up");
        }

        int armorDamage = 0;
        if (currentAIArmor > 0 && remainingDamage > 0)
        {
            armorDamage = Mathf.Min(currentAIArmor, remainingDamage);
            currentAIArmor -= armorDamage;
            remainingDamage -= armorDamage;

            Debug.Log($"AI Armor blocked {armorDamage} damage. Remaining armor: {currentAIArmor}");
        }

        if (FeedbackManager.Instance != null && remainingDamage > 0)
        {
            FeedbackManager.Instance.ShowTotalDamageWithOrganBreakdown(
                FeedbackManager.Instance.aiFeedbackSpawnPoint,
                damageBreakdown.baseDamage,
                damageBreakdown.organBonuses,
                false,
                "Player Attack"
            );
        }

        if (armorDamage > 0)
        {
            StartCoroutine(SmoothAIArmorChange(currentAIArmor + armorDamage, currentAIArmor, 0.3f));
            yield return new WaitForSeconds(0.3f);
        }

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
        int targetHealth = Mathf.Min(aiEnerling.currentLife + totalHeal, aiEnerling.baseLife);
        yield return StartCoroutine(SmoothAIHealthChange(aiEnerling.currentLife, targetHealth, 0.5f));
        aiEnerling.currentLife = targetHealth;
    }

    void SetAIDefend(int defendAmount)
    {
        activeAIDefend = defendAmount;
        hasAIDefend = true;

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.ShowDefend(
                FeedbackManager.Instance.aiFeedbackSpawnPoint,
                defendAmount,
                true,
                "AI Defend"
            );
        }

        Debug.Log($"AI Defend set to {defendAmount}. Will block next player attack.");
    }

    public void ClearAIDefend()
    {
        if (hasAIDefend)
        {
            Debug.Log($"AI Defend cleared (was {activeAIDefend})");
            hasAIDefend = false;
            activeAIDefend = 0;
        }
    }

    void UpdateAIOrganCooldown()
    {
        if (aiOrganCooldownTimer < aiMaxOrganCooldown)
        {
            aiOrganCooldownTimer++;
            Debug.Log($"AI Organ Cooldown: {aiOrganCooldownTimer}/{aiMaxOrganCooldown}");

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

    public void CheckAndApplyOrganHeal()
    {
        if (aiEnerling == null) return;

        Debug.Log($"AI CheckAndApplyOrganHeal: Cooldown Ready={aiOrganCooldownReady}, Timer={aiOrganCooldownTimer}/{aiMaxOrganCooldown}");
        Debug.Log($"AI has {aiEnerling.beneficialOrgans?.Count ?? 0} beneficial organs, {aiEnerling.targetOrgans?.Count ?? 0} target organs");

        if (aiOrganCooldownReady && aiEnerling.beneficialOrgans.Count > 0)
        {
            int healPerOrgan = Mathf.RoundToInt(aiEnerling.baseLife * 0.05f);
            int totalHeal = healPerOrgan * aiEnerling.beneficialOrgans.Count;

            List<FeedbackManager.OrganBonus> organBonuses = new List<FeedbackManager.OrganBonus>();
            foreach (string organ in aiEnerling.beneficialOrgans)
            {
                organBonuses.Add(new FeedbackManager.OrganBonus(organ, healPerOrgan));
            }

            if (FeedbackManager.Instance != null)
            {
                FeedbackManager.Instance.ShowTotalHealWithOrganBreakdown(
                    FeedbackManager.Instance.aiFeedbackSpawnPoint,
                    0,
                    organBonuses,
                    false,
                    "Beneficial Organ Heal"
                );
            }

            StartCoroutine(ApplyAIHeal(totalHeal, 0));

            aiOrganCooldownTimer = 0;
            aiOrganCooldownReady = false;

            Debug.Log($"AI Beneficial Organ Heal Applied: {totalHeal} HP ({aiEnerling.beneficialOrgans.Count} organs)");
        }
        else
        {
            if (aiOrganCooldownReady && aiEnerling.beneficialOrgans.Count == 0)
            {
                Debug.Log("AI organ cooldown ready but no beneficial organs to heal");
            }
            else
            {
                Debug.Log($"AI Organ Cooldown: {aiOrganCooldownTimer}/{aiMaxOrganCooldown} turns remaining ({(aiOrganCooldownReady ? "READY" : "NOT READY")})");
            }
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

    public void InitializeWithExistingAIEnerling(string enerlingName, IngredientDatabase database, GameObject existingEnerling)
    {
        ingredientDatabase = database;
        Debug.Log($"AIEnerlingManager: Initializing with existing AI enerling: {enerlingName}");

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

        if (existingEnerling != null)
        {
            spawnedAIEnerling = existingEnerling;

            if (aiSpawningPoint != null)
            {
                spawnedAIEnerling.transform.SetParent(aiSpawningPoint);
                spawnedAIEnerling.transform.localPosition = Vector3.zero;
                spawnedAIEnerling.transform.localRotation = Quaternion.identity;
                spawnedAIEnerling.transform.localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning("AI spawn point not assigned, keeping current transform");
            }

            aiAnimator = spawnedAIEnerling.GetComponent<Animator>();
            if (aiAnimator != null && aiEnerling.animatorController != null)
            {
                aiAnimator.runtimeAnimatorController = aiEnerling.animatorController;
            }

            Debug.Log($"Using existing AI enerling: {aiEnerling.ingredientName}");
        }
        else
        {
            SpawnAIEnerling();
        }

        for (int i = 1; i <= 4; i++)
        {
            skillCooldowns[i] = 0;
        }

        UpdateAvailableSkills();
        UpdateAIUI();

        // Preload the ending cutscene video
        PreloadEndingCutscene();

        Debug.Log($"AI Enerling initialized in battle scene: {aiEnerling.ingredientName}");
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
        ProcessEndTurn();
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

    public bool IsAIDefeated()
    {
        if (aiEnerling == null) return false;
        return aiEnerling.currentLife <= 0;
    }

    public Animator GetAIAnimator()
    {
        if (spawnedAIEnerling != null)
            return spawnedAIEnerling.GetComponent<Animator>();
        return null;
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

        aiOrganCooldownTimer = 0;
        aiOrganCooldownReady = false;

        aiOrganBonusDamage = 0;
        aiOrganBonusHeal = 0;
        aiHasOrganDamageBonus = false;
        aiHasOrganHealBonus = false;
        aiOrganBonusNames.Clear();
    }

    public IngredientDatabase.IngredientInfo GetAIEnerling()
    {
        return aiEnerling;
    }
}