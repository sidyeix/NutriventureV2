using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Cinemachine;
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

    [Header("Organ Sprites")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;

    [Header("Skills Panel")]
    public Transform skillsPanel;
    public GameObject skillButtonPrefab;

    [Header("Enerling Spawning")]
    public Transform enerlingSpawningPoint;
    public Transform aiSpawningPoint;

    [Header("Damage Feedback")]
    public GameObject damageFeedbackPrefab;
    public Transform playerFeedbackSpawnPoint;
    public Transform enemyFeedbackSpawnPoint;

    [Header("Animation Settings")]
    public float animationBufferTime = 0.1f;

    // Current battle enerling
    private IngredientDatabase.IngredientInfo battleEnerling;
    private GameObject spawnedEnerling;
    private Animator enerlingAnimator;

    // Defense tracking
    private int currentArmor = 0;
    private int activeDefense = 0; // Active defense shield for next attack
    private bool hasDefense = false; // Whether defense is active for next attack

    // Skill tracking
    private List<GameObject> skillButtons = new List<GameObject>();
    private bool isAnimating = false;
    private float animationEndTime = 0f;

    // Reference to selection manager
    private EnerlingSelectionManager selectionManager;

    // UI animation
    private Coroutine healthAnimationCoroutine;
    private Coroutine armorAnimationCoroutine;

    // Feedback queue for spawning with intervals
    private Queue<FeedbackInfo> feedbackQueue = new Queue<FeedbackInfo>();

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

    void Update()
    {
        // Process feedback queue
        if (feedbackQueue.Count > 0)
        {
            ProcessFeedbackQueue();
        }
    }

    void ProcessFeedbackQueue()
    {
        if (!IsInvoking("ProcessNextFeedback"))
        {
            Invoke("ProcessNextFeedback", 0.5f); // CHANGED: 0.3f to 0.5f
        }
    }

    void ProcessNextFeedback()
    {
        if (feedbackQueue.Count > 0)
        {
            var feedback = feedbackQueue.Dequeue();
            ShowDamageFeedback(feedback.amount, feedback.isHeal, feedback.spawnPoint, feedback.type, feedback.isOrganBonus, feedback.organName);
        }
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

        StartCoroutine(MonitorAnimationCompletion());
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

    void CreateSkillButtons()
    {
        foreach (GameObject button in skillButtons)
        {
            Destroy(button);
        }
        skillButtons.Clear();

        if (battleEnerling == null || skillButtonPrefab == null || skillsPanel == null) return;

        for (int i = 1; i <= 4; i++)
        {
            IngredientDatabase.SkillInfo skill = GetSkillByNumber(i);
            if (skill != null)
            {
                GameObject skillButton = Instantiate(skillButtonPrefab, skillsPanel);

                Transform skillNameTransform = skillButton.transform.Find("SkillName");
                if (skillNameTransform != null)
                {
                    TextMeshProUGUI skillNameText = skillNameTransform.GetComponent<TextMeshProUGUI>();
                    if (skillNameText != null)
                    {
                        skillNameText.text = skill.skillName;
                    }
                }

                Image parentImage = skillButton.GetComponent<Image>();
                if (parentImage != null && skill.skillSprite != null)
                {
                    parentImage.sprite = skill.skillSprite;
                    parentImage.preserveAspect = true;
                }

                Button button = skillButton.GetComponent<Button>();
                if (button != null)
                {
                    int skillNum = i;
                    button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));
                }
                else
                {
                    Debug.LogWarning("Skill button prefab has no Button component");
                }

                skillButtons.Add(skillButton);
            }
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

        string animationBool = GetAnimationBoolName(skillNumber);
        if (!string.IsNullOrEmpty(animationBool))
        {
            StartCoroutine(PlayAnimationAndApplyEffect(animationBool, skillNumber));
            Debug.Log($"Playing animation for skill {skillNumber}: {animationBool}");
        }
        else
        {
            Debug.LogWarning($"No animation bool found for skill {skillNumber}");
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

    IEnumerator PlayAnimationAndApplyEffect(string animationBool, int skillNumber)
    {
        isAnimating = true;

        enerlingAnimator.SetBool(animationBool, true);
        yield return new WaitForSeconds(0.1f);
        animationEndTime = Time.time + animationBufferTime;
        yield return new WaitForSeconds(0.5f);
        enerlingAnimator.SetBool(animationBool, false);

        ApplySkillEffect(skillNumber);

        if (battleEnerling != null)
        {
            battleEnerling.SetSkillCooldown(skillNumber);
        }

        if (turnSystem != null)
        {
            turnSystem.PlayerSkillChosen();
        }

        Debug.Log($"Skill {skillNumber} executed");
    }

    void ApplySkillEffect(int skillNumber)
    {
        if (battleEnerling == null) return;

        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);
        if (skill == null) return;

        int organCount = battleEnerling.OrganCount;
        int totalEffect = skill.CalculateTotalEffect(organCount);
        int organBonus = CalculateOrganBonus(battleEnerling.rarity, organCount);
        int organEffect = 0;

        // Calculate organ bonus based on skill type and enerling organs
        if (skill.type == IngredientDatabase.SkillInfo.SkillType.Heal && battleEnerling.beneficialOrgans.Count > 0)
        {
            organEffect = Mathf.RoundToInt(totalEffect * (organBonus / 100f));
        }
        else if (skill.type == IngredientDatabase.SkillInfo.SkillType.Damage && battleEnerling.targetOrgans.Count > 0)
        {
            organEffect = Mathf.RoundToInt(totalEffect * (organBonus / 100f));
        }

        // Apply effect based on skill type
        switch (skill.type)
        {
            case IngredientDatabase.SkillInfo.SkillType.Heal:
                StartCoroutine(ApplyHeal(totalEffect, organEffect, true));
                Debug.Log($"Healed for {totalEffect} HP (Base: {totalEffect - organEffect}, Organ Bonus: {organEffect})");
                break;

            case IngredientDatabase.SkillInfo.SkillType.Damage:
                if (aiEnerlingManager != null)
                {
                    string organName = GetOrganForBonus(battleEnerling);
                    StartCoroutine(aiEnerlingManager.TakeDamageWithFeedback(totalEffect, organEffect, enemyFeedbackSpawnPoint, organName));
                    Debug.Log($"Dealt {totalEffect} damage to AI (Base: {totalEffect - organEffect}, Organ Bonus: {organEffect})");
                }
                break;

            case IngredientDatabase.SkillInfo.SkillType.Defend:
                SetDefense(totalEffect);
                Debug.Log($"Defended with {totalEffect} armor for next attack");
                break;
        }
    }

    int CalculateOrganBonus(IngredientDatabase.Rarity rarity, int organCount)
    {
        // 5% per organ as per database logic
        return organCount * 5;
    }

    IEnumerator ApplyHeal(int baseHeal, int organBonus, bool isPlayer)
    {
        int totalHeal = baseHeal + organBonus;

        // Show base heal feedback
        if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
        {
            feedbackQueue.Enqueue(new FeedbackInfo(baseHeal, true, playerFeedbackSpawnPoint, "Heal", false, ""));
        }

        // Show organ bonus heal feedback if any
        if (organBonus > 0 && damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
        {
            string organName = GetOrganForBonus(battleEnerling);
            feedbackQueue.Enqueue(new FeedbackInfo(organBonus, true, playerFeedbackSpawnPoint, "Organ Heal", true, organName));
        }

        int targetHealth = Mathf.Min(battleEnerling.currentLife + totalHeal, battleEnerling.baseLife);

        if (healthAnimationCoroutine != null)
            StopCoroutine(healthAnimationCoroutine);

        healthAnimationCoroutine = StartCoroutine(SmoothHealthChange(battleEnerling.currentLife, targetHealth, 0.5f));
        battleEnerling.currentLife = targetHealth;

        yield return null;
    }

    public IEnumerator ApplyDamageToPlayer(int totalDamage, int organBonusDamage, string organName = "")
    {
        int remainingDamage = totalDamage;

        // Apply defense if active
        if (hasDefense && activeDefense > 0)
        {
            int defendedDamage = Mathf.Min(activeDefense, remainingDamage);
            remainingDamage -= defendedDamage;
            activeDefense -= defendedDamage;

            // Show defense feedback
            if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
            {
                feedbackQueue.Enqueue(new FeedbackInfo(defendedDamage, false, playerFeedbackSpawnPoint, "Defend", false, ""));
            }

            if (activeDefense <= 0)
            {
                hasDefense = false;
                activeDefense = 0;
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Damage goes to armor first
        if (currentArmor > 0 && remainingDamage > 0)
        {
            int armorDamage = Mathf.Min(currentArmor, remainingDamage);
            StartCoroutine(SmoothArmorChange(currentArmor, currentArmor - armorDamage, 0.3f));
            currentArmor -= armorDamage;
            remainingDamage -= armorDamage;

            // Show armor damage feedback
            if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
            {
                feedbackQueue.Enqueue(new FeedbackInfo(armorDamage, false, playerFeedbackSpawnPoint, "Armor", false, ""));
            }

            yield return new WaitForSeconds(0.3f);
        }

        // Show base damage feedback (if any)
        int baseDamage = totalDamage - organBonusDamage;
        if (baseDamage > 0 && remainingDamage > 0)
        {
            if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
            {
                feedbackQueue.Enqueue(new FeedbackInfo(baseDamage, false, playerFeedbackSpawnPoint, "Damage", false, ""));
            }
        }

        // Show organ bonus damage feedback (if any)
        if (organBonusDamage > 0 && remainingDamage > 0)
        {
            if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
            {
                feedbackQueue.Enqueue(new FeedbackInfo(organBonusDamage, false, playerFeedbackSpawnPoint, "Organ", true, organName));
            }
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

            // Check if player is defeated
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
        if (damageFeedbackPrefab != null && playerFeedbackSpawnPoint != null)
        {
            feedbackQueue.Enqueue(new FeedbackInfo(defenseAmount, false, playerFeedbackSpawnPoint, "Defend Active", false, ""));
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

    void ShowDamageFeedback(int amount, bool isHeal, Transform spawnPoint, string type, bool isOrganBonus, string organName = "")
    {
        if (damageFeedbackPrefab == null || spawnPoint == null) return;

        GameObject feedback = Instantiate(damageFeedbackPrefab, spawnPoint);
        feedback.transform.localPosition = Vector3.zero;

        // Add upward movement
        StartCoroutine(MoveFeedbackUpwards(feedback.transform));

        Transform damageTransform = feedback.transform.Find("Damage");
        if (damageTransform != null)
        {
            TextMeshProUGUI damageText = damageTransform.GetComponent<TextMeshProUGUI>();
            if (damageText != null)
            {
                damageText.text = isHeal ? $"+{amount}" : $"-{amount}";

                // Set color based on type
                if (isHeal)
                    damageText.color = Color.green;
                else if (type == "Defend" || type == "Defend Active")
                    damageText.color = Color.yellow;
                else if (isOrganBonus)
                    damageText.color = new Color(1f, 0.5f, 0f); // Orange for organ bonus
                else
                    damageText.color = Color.red;
            }
        }

        Transform organTransform = feedback.transform.Find("Organ");
        if (organTransform != null)
        {
            Image organImage = organTransform.GetComponent<Image>();
            TextMeshProUGUI organText = organTransform.GetComponent<TextMeshProUGUI>();

            if (isOrganBonus && !string.IsNullOrEmpty(organName))
            {
                // Show organ sprite for organ bonuses
                organImage.gameObject.SetActive(true);

                // Get the organ sprite
                Sprite organSprite = GetOrganSprite(organName);
                if (organImage != null && organSprite != null)
                {
                    organImage.sprite = organSprite;
                    organImage.preserveAspect = true;
                }

                // Hide the text component
                if (organText != null)
                {
                    organText.gameObject.SetActive(false);
                }
            }
            else if (type == "Damage" || type == "Heal")
            {
                // Hide organ image for base damage/heal
                organImage.gameObject.SetActive(false);

                // Show text for other types if needed
                if (organText != null)
                {
                    organText.text = type;
                    organText.gameObject.SetActive(type != "Damage" && type != "Heal");
                }
            }
            else
            {
                // For other types (Defend, Armor, etc.)
                organImage.gameObject.SetActive(false);

                if (organText != null)
                {
                    organText.text = type;
                    organText.gameObject.SetActive(true);
                }
            }
        }

        // Add fade out
        CanvasGroup canvasGroup = feedback.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = feedback.AddComponent<CanvasGroup>();

        StartCoroutine(FadeOutFeedback(canvasGroup, feedback));
    }

    Sprite GetOrganSprite(string organName)
    {
        if (string.IsNullOrEmpty(organName)) return null;

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
                return ingredientDatabase.GetOrganSprite(organName);
        }
    }

    string GetOrganForBonus(IngredientDatabase.IngredientInfo enerling)
    {
        List<string> organs = enerling.beneficialOrgans.Count > 0 ?
            enerling.beneficialOrgans : enerling.targetOrgans;

        if (organs.Count > 0)
            return organs[0]; // Return first organ
        return "";
    }

    IEnumerator MoveFeedbackUpwards(Transform feedbackTransform)
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = feedbackTransform.localPosition;
        Vector3 endPos = startPos + new Vector3(0, 0.33f, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            feedbackTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    IEnumerator FadeOutFeedback(CanvasGroup canvasGroup, GameObject feedback)
    {
        yield return new WaitForSeconds(1f);

        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        Destroy(feedback);
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

    IEnumerator MonitorAnimationCompletion()
    {
        while (true)
        {
            if (isAnimating && Time.time >= animationEndTime)
            {
                if (enerlingAnimator != null)
                {
                    AnimatorStateInfo stateInfo = enerlingAnimator.GetCurrentAnimatorStateInfo(0);

                    if (stateInfo.normalizedTime >= 1f ||
                        !stateInfo.IsName("Attack") &&
                        !stateInfo.IsName("Skill1") &&
                        !stateInfo.IsName("Skill2") &&
                        !stateInfo.IsName("Skill3"))
                    {
                        isAnimating = false;
                        Debug.Log("Animation completed, ready for next action");
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
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

        battleEnerling = null;
        currentArmor = 0;
        activeDefense = 0;
        hasDefense = false;
        isAnimating = false;
        feedbackQueue.Clear();
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