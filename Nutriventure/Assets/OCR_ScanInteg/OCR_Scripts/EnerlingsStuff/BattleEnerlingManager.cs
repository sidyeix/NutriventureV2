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
    public GameObject selectionCanvas; // The canvas with EnerlingSelectionManager
    public GameObject battlefieldCanvas; // The canvas with battlefield UI

    [Header("Camera Reference")]
    public CinemachineVirtualCamera battleFocusCamera;

    [Header("UI References - Battlefield Info")]
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

    [Header("Skills Panel")]
    public Transform skillsPanel;
    public GameObject skillButtonPrefab;

    [Header("Enerling Spawning")]
    public Transform enerlingSpawningPoint;

    [Header("Animation Settings")]
    public float animationBufferTime = 0.1f; // Time to wait before resetting animation bools

    [Header("Battle Initialization")]
    public bool initializeOnAwake = false; // Set to false, we'll initialize via button click
    public bool setCameraPriorityOnInit = true;
    public int cameraPriority = 20;

    // Current battle enerling
    private IngredientDatabase.IngredientInfo battleEnerling;
    private GameObject spawnedEnerling;
    private Animator enerlingAnimator;

    // Skill button references
    private List<GameObject> skillButtons = new List<GameObject>();
    private bool isAnimating = false;
    private float animationEndTime = 0f;

    // Reference to selection manager
    private EnerlingSelectionManager selectionManager;

    void Awake()
    {
        // Find the selection manager if not assigned
        if (selectionManager == null)
        {
            selectionManager = FindObjectOfType<EnerlingSelectionManager>();
        }

        if (initializeOnAwake)
        {
            InitializeBattlefield();
        }
        else
        {
            // Start with camera disabled and battlefield hidden
            if (battleFocusCamera != null)
            {
                battleFocusCamera.Priority = 0;
            }

            // Ensure battlefield canvas is hidden initially
            if (battlefieldCanvas != null)
            {
                battlefieldCanvas.SetActive(false);
            }

            // Ensure selection canvas is shown initially
            if (selectionCanvas != null)
            {
                selectionCanvas.SetActive(true);
            }
        }
    }

    // Call this method from the Select Button in EnerlingSelectionManager
    public void OnSelectButtonClickedFromSelection()
    {
        // Get the currently selected enerling from the selection manager
        if (selectionManager != null)
        {
            // Use reflection or a public method to get the selected enerling name
            // For now, we'll get it from PersistentDataManager since selection manager saves it there
            string selectedName = PersistentDataManager.Instance?.GetSelectedEnerlingName();

            if (!string.IsNullOrEmpty(selectedName))
            {
                // Switch to battlefield
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

    // Switch from selection to battlefield
    public void SwitchToBattlefield(string selectedEnerlingName)
    {
        Debug.Log($"Switching to battlefield with enerling: {selectedEnerlingName}");

        // 1. Hide selection canvas
        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(false);
        }

        // 2. Show battlefield canvas
        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(true);
        }

        // 3. Initialize battlefield with the selected enerling
        InitializeBattlefieldWithEnerling(selectedEnerlingName);
    }

    // Switch from battlefield back to selection
    public void SwitchToSelection()
    {
        Debug.Log("Switching back to selection screen");

        // 1. Clean up battlefield
        CleanupBattlefield();

        // 2. Hide battlefield canvas
        if (battlefieldCanvas != null)
        {
            battlefieldCanvas.SetActive(false);
        }

        // 3. Show selection canvas
        if (selectionCanvas != null)
        {
            selectionCanvas.SetActive(true);
        }

        // 4. Reset camera priority
        ResetCameraPriority();
    }

    // Initialize battlefield with specific enerling
    public void InitializeBattlefieldWithEnerling(string enerlingName)
    {
        // Set camera priority
        if (setCameraPriorityOnInit && battleFocusCamera != null)
        {
            battleFocusCamera.Priority = cameraPriority;
            Debug.Log($"Battle focus camera priority set to {cameraPriority}");
        }

        // Load the specific battle enerling
        LoadBattleEnerlingByName(enerlingName);

        // Initialize battle state
        InitializeBattleState();

        // Update battlefield UI
        UpdateBattlefieldUI();

        // Spawn the enerling prefab
        SpawnEnerling();

        // Create skill buttons
        CreateSkillButtons();

        // Setup animation completion detection
        StartCoroutine(MonitorAnimationCompletion());
    }

    // Main initialization method (for backward compatibility)
    public void InitializeBattlefield()
    {
        // Get selected enerling from PersistentData
        string selectedName = PersistentDataManager.Instance?.GetSelectedEnerlingName();

        if (string.IsNullOrEmpty(selectedName))
        {
            Debug.LogError("No enerling selected in PersistentData!");
            return;
        }

        InitializeBattlefieldWithEnerling(selectedName);
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

        // Load saved current life if available
        if (PersistentDataManager.Instance != null)
        {
            int savedLife = PersistentDataManager.Instance.GetEnerlingCurrentLife(enerlingName);
            if (savedLife > 0)
            {
                battleEnerling.currentLife = savedLife;
            }
        }

        Debug.Log($"Battle enerling loaded: {battleEnerling.ingredientName} (Life: {battleEnerling.currentLife}/{battleEnerling.baseLife})");
    }

    void InitializeBattleState()
    {
        if (battleEnerling != null)
        {
            // Reset skill cooldowns for new battle
            battleEnerling.ResetBattleState();

            // But keep the current life from saved state
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

        // Enerling name
        if (battlefieldEnerlingName != null)
            battlefieldEnerlingName.text = battleEnerling.ingredientName;

        // Health slider and text
        if (battlefieldHealthSlider != null)
        {
            battlefieldHealthSlider.maxValue = battleEnerling.baseLife;
            battlefieldHealthSlider.value = battleEnerling.currentLife;
        }

        if (healthText != null)
            healthText.text = battleEnerling.LifeText;

        // Armor slider and text
        if (battlefieldArmorSlider != null)
        {
            int armorValue = CalculateArmorValue(battleEnerling);
            battlefieldArmorSlider.maxValue = armorValue;
            battlefieldArmorSlider.value = armorValue; // Always at max at battle start
        }

        if (armorText != null)
        {
            int armorValue = CalculateArmorValue(battleEnerling);
            armorText.text = $"{armorValue}/{armorValue}";
        }

        // Frame based on rarity
        if (battlefieldFrame != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(battleEnerling.rarity);
            if (frameSprite != null)
                battlefieldFrame.sprite = frameSprite;
        }

        // Rarity tag
        if (rarityTag != null)
        {
            Sprite raritySprite = ingredientDatabase.GetRarityIcon(battleEnerling.rarity);
            if (raritySprite != null)
                rarityTag.sprite = raritySprite;
        }

        // Enerling image
        if (enerlingImage != null && battleEnerling.enerlingSprite != null)
        {
            enerlingImage.sprite = battleEnerling.enerlingSprite;
            enerlingImage.preserveAspect = true;
        }

        // Ability text
        if (abilityText != null)
        {
            abilityText.text = GetAbilityText(battleEnerling);
        }

        // NameStats BG based on rarity
        if (nameStatsBG != null)
        {
            UpdateNameStatsBackground();
        }

        // Create organ images
        UpdateOrganPanel();
    }

    int CalculateArmorValue(IngredientDatabase.IngredientInfo enerling)
    {
        // Calculate armor value based on armor percentage and base life
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
        // Clear previous organ images
        foreach (Transform child in organPanel)
        {
            Destroy(child.gameObject);
        }

        if (battleEnerling == null || organImagePrefab == null) return;

        // Get organs list (beneficial or target)
        List<string> organs = battleEnerling.beneficialOrgans.Count > 0 ?
            battleEnerling.beneficialOrgans : battleEnerling.targetOrgans;

        // Create organ images
        foreach (string organ in organs)
        {
            GameObject organImage = Instantiate(organImagePrefab, organPanel);
            Image image = organImage.GetComponent<Image>();

            // Set sprite based on organ name
            Sprite organSprite = ingredientDatabase.GetOrganSprite(organ);
            if (organSprite != null && image != null)
            {
                image.sprite = organSprite;
                image.preserveAspect = true;
            }

            // Add organ name as text if needed
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

        // Clear any existing spawned enerling
        if (spawnedEnerling != null)
        {
            Destroy(spawnedEnerling);
        }

        // Instantiate as child of spawning point
        spawnedEnerling = Instantiate(battleEnerling.modelPrefab, enerlingSpawningPoint);
        spawnedEnerling.transform.localPosition = Vector3.zero;
        spawnedEnerling.transform.localRotation = Quaternion.identity;

        // Get animator
        enerlingAnimator = spawnedEnerling.GetComponent<Animator>();
        if (enerlingAnimator == null)
        {
            Debug.LogWarning("Spawned enerling has no Animator component");
        }

        // Set animator controller if specified
        if (battleEnerling.animatorController != null && enerlingAnimator != null)
        {
            enerlingAnimator.runtimeAnimatorController = battleEnerling.animatorController;
        }

        Debug.Log($"Spawned enerling: {battleEnerling.ingredientName}");
    }

    void CreateSkillButtons()
    {
        // Clear existing skill buttons
        foreach (GameObject button in skillButtons)
        {
            Destroy(button);
        }
        skillButtons.Clear();

        if (battleEnerling == null || skillButtonPrefab == null || skillsPanel == null) return;

        // Create buttons for all 4 skills
        for (int i = 1; i <= 4; i++)
        {
            IngredientDatabase.SkillInfo skill = GetSkillByNumber(i);
            if (skill != null)
            {
                GameObject skillButton = CreateSkillButton(skill, i);
                if (skillButton != null)
                {
                    skillButtons.Add(skillButton);
                }
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

    GameObject CreateSkillButton(IngredientDatabase.SkillInfo skill, int skillNumber)
    {
        if (skill == null) return null;

        GameObject buttonObj = Instantiate(skillButtonPrefab, skillsPanel);

        // Set skill sprite on the parent image
        Image parentImage = buttonObj.GetComponent<Image>();
        if (parentImage != null && skill.skillSprite != null)
        {
            parentImage.sprite = skill.skillSprite;
            parentImage.preserveAspect = true;
        }

        // Set skill name text
        Transform skillNameTransform = buttonObj.transform.Find("SkillName");
        if (skillNameTransform != null)
        {
            TextMeshProUGUI skillNameText = skillNameTransform.GetComponent<TextMeshProUGUI>();
            if (skillNameText != null)
            {
                skillNameText.text = skill.skillName;
            }
        }

        // Add button click listener
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            int skillNum = skillNumber; // Local copy for closure
            button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));
        }
        else
        {
            Debug.LogWarning("Skill button prefab has no Button component");
        }

        return buttonObj;
    }

    void OnSkillButtonClicked(int skillNumber)
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

        // Check if skill is ready (cooldown)
        if (battleEnerling != null && !battleEnerling.IsSkillReady(skillNumber))
        {
            Debug.Log($"Skill {skillNumber} is on cooldown!");
            return;
        }

        // Set animation based on skill number
        string animationBool = GetAnimationBoolName(skillNumber);
        if (!string.IsNullOrEmpty(animationBool))
        {
            StartCoroutine(PlayAnimation(animationBool, skillNumber));
            Debug.Log($"Playing animation for skill {skillNumber}: {animationBool}");

            // Apply skill effect (damage, healing, etc.)
            ApplySkillEffect(skillNumber);
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

    void ApplySkillEffect(int skillNumber)
    {
        if (battleEnerling == null) return;

        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);
        if (skill == null) return;

        // Calculate skill effect with organ bonuses
        int organCount = battleEnerling.OrganCount;
        int totalEffect = skill.CalculateTotalEffect(organCount);

        // Apply effect based on skill type
        switch (skill.type)
        {
            case IngredientDatabase.SkillInfo.SkillType.Heal:
                // Heal self or allies (for now, just heal self)
                battleEnerling.Heal(totalEffect);
                Debug.Log($"Healed for {totalEffect} HP");
                break;

            case IngredientDatabase.SkillInfo.SkillType.Damage:
                // Damage enemy (for now, just log it)
                Debug.Log($"Dealt {totalEffect} damage to enemy");
                break;

            case IngredientDatabase.SkillInfo.SkillType.Defend:
                // Defend/armor up (for now, just log it)
                Debug.Log($"Defended with {totalEffect} armor");
                break;
        }

        // Update UI to reflect changes
        UpdateBattleUI();
    }

    IEnumerator PlayAnimation(string animationBool, int skillNumber)
    {
        isAnimating = true;

        // Set the animation bool to true
        enerlingAnimator.SetBool(animationBool, true);

        // Wait for animation to start
        yield return new WaitForSeconds(0.1f);

        // Record when animation should end
        animationEndTime = Time.time + animationBufferTime;

        // Wait a bit before resetting to avoid immediate reset
        yield return new WaitForSeconds(0.5f);

        // Reset the bool to false
        enerlingAnimator.SetBool(animationBool, false);

        // Set skill cooldown
        if (battleEnerling != null)
        {
            battleEnerling.SetSkillCooldown(skillNumber);
            Debug.Log($"Skill {skillNumber} cooldown set to {GetSkillCooldownTurns(skillNumber)} turns");
        }

        // Animation completion will be handled by MonitorAnimationCompletion
    }

    int GetSkillCooldownTurns(int skillNumber)
    {
        if (battleEnerling == null) return 0;

        var skill = GetSkillByNumber(skillNumber);
        return skill != null ? skill.cooldownTurns : 0;
    }

    IEnumerator MonitorAnimationCompletion()
    {
        while (true)
        {
            if (isAnimating && Time.time >= animationEndTime)
            {
                // Check if animator is in idle state (or any non-skill state)
                if (enerlingAnimator != null)
                {
                    AnimatorStateInfo stateInfo = enerlingAnimator.GetCurrentAnimatorStateInfo(0);

                    // Check if animation is complete (normalizedTime >= 1)
                    // or if we're back to a base layer state
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

    // Update UI with current battle state (call this when life changes during battle)
    public void UpdateBattleUI()
    {
        if (battleEnerling == null) return;

        // Update health slider and text
        if (battlefieldHealthSlider != null)
        {
            battlefieldHealthSlider.value = battleEnerling.currentLife;
        }

        if (healthText != null)
            healthText.text = battleEnerling.LifeText;
    }

    // Clean up battlefield (call when switching back to selection)
    void CleanupBattlefield()
    {
        // Stop animation monitoring
        StopAllCoroutines();

        // Clean up spawned enerling
        CleanupSpawnedEnerling();

        // Clear skill buttons
        foreach (GameObject button in skillButtons)
        {
            Destroy(button);
        }
        skillButtons.Clear();

        // Clear organ panel
        if (organPanel != null)
        {
            foreach (Transform child in organPanel)
            {
                Destroy(child.gameObject);
            }
        }

        // Reset battle state
        battleEnerling = null;
        isAnimating = false;
    }

    // Get the battle enerling
    public IngredientDatabase.IngredientInfo GetBattleEnerling()
    {
        return battleEnerling;
    }

    // Save battle state (call this when battle ends or scene changes)
    void SaveBattleState()
    {
        if (battleEnerling != null && PersistentDataManager.Instance != null)
        {
            // 1. Save to PersistentData
            PersistentDataManager.Instance.SaveEnerlingCurrentLife(
                battleEnerling.ingredientName,
                battleEnerling.currentLife
            );

            // 2. ALSO update the original in the database
            var original = ingredientDatabase.GetIngredientInfo(battleEnerling.ingredientName);
            if (original != null)
            {
                original.currentLife = battleEnerling.currentLife;
                Debug.Log($"Updated database entry for {battleEnerling.ingredientName}: {original.currentLife} life");
            }
        }
    }

    // Update enerling after battle
    public void UpdateAfterBattle(int damageTaken, int healingReceived)
    {
        if (battleEnerling != null)
        {
            battleEnerling.TakeDamage(damageTaken);
            battleEnerling.Heal(healingReceived);

            SaveBattleState();

            // Update UI if needed
            UpdateBattleUI();
        }
    }

    // Reset camera priority (call when leaving battle)
    public void ResetCameraPriority()
    {
        if (battleFocusCamera != null)
        {
            battleFocusCamera.Priority = 0;
            Debug.Log("Battle focus camera priority reset to 0");
        }
    }

    // Clean up spawned enerling
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
        ResetCameraPriority();
        CleanupBattlefield();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveBattleState();
        }
    }

    // For testing - manually trigger initialization
    [ContextMenu("Test Switch to Battlefield")]
    public void TestSwitchToBattlefield()
    {
        // Get any unlocked enerling for testing
        var unlocked = ingredientDatabase.GetUnlockedIngredients();
        if (unlocked != null && unlocked.Count > 0)
        {
            string testEnerling = unlocked[0].ingredientName;
            Debug.Log($"Testing switch to battlefield with: {testEnerling}");
            SwitchToBattlefield(testEnerling);
        }
    }

    // For testing - switch back to selection
    [ContextMenu("Test Switch to Selection")]
    public void TestSwitchToSelection()
    {
        SwitchToSelection();
    }

    // For testing - trigger skill animation
    [ContextMenu("Test Skill 1 Animation")]
    public void TestSkill1Animation()
    {
        OnSkillButtonClicked(1);
    }
}