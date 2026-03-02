using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerEnerlingManager : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("UI References - Player Skills Panel")]
    public Transform skillsUIPanel;
    public GameObject skillButtonPrefab;

    [Header("Skill Type Sprites")]
    public Sprite damageSkillSprite;
    public Sprite defendSkillSprite;
    public Sprite healSkillSprite;

    [Header("Cooldown UI Settings")]
    public Color cooldownTextColor = Color.red;
    public Color readyTextColor = Color.white;
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color enabledButtonColor = Color.white;

    // Dynamic lists to store references to spawned button components
    private List<Button> skillButtons = new List<Button>();
    private List<TextMeshProUGUI> skillCooldownTexts = new List<TextMeshProUGUI>();
    private List<Slider> skillCooldownSliders = new List<Slider>();
    private List<TextMeshProUGUI> skillNameTexts = new List<TextMeshProUGUI>();
    private List<Image> skillButtonImages = new List<Image>();
    private List<GameObject> skillButtonObjects = new List<GameObject>();

    [Header("Current Enerling")]
    public IngredientDatabase.IngredientInfo playerEnerling;

    private BattleEnerlingManager battleManager;
    private TurnSystem turnSystem;

    void Start()
    {
        battleManager = FindObjectOfType<BattleEnerlingManager>();
        turnSystem = FindObjectOfType<TurnSystem>();

        // Initialize lists with 4 slots (for skills 1-4)
        for (int i = 0; i < 4; i++)
        {
            skillButtons.Add(null);
            skillCooldownTexts.Add(null);
            skillCooldownSliders.Add(null);
            skillNameTexts.Add(null);
            skillButtonImages.Add(null);
            skillButtonObjects.Add(null);
        }
    }

    public void InitializePlayerEnerling(string enerlingName)
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase not assigned!");
            return;
        }

        // IMPORTANT: Get the reference from BattleEnerlingManager instead of creating a new copy
        if (battleManager != null)
        {
            playerEnerling = battleManager.GetBattleEnerling();
            if (playerEnerling == null)
            {
                Debug.LogError("BattleEnerlingManager returned null player enerling!");
                return;
            }
        }
        else
        {
            // Fallback to creating a copy if battleManager not found
            playerEnerling = ingredientDatabase.CreateBattleCopy(enerlingName);
            if (playerEnerling == null)
            {
                Debug.LogError($"Failed to create player enerling: {enerlingName}");
                return;
            }
        }

        Debug.Log($"Player enerling initialized: {playerEnerling.ingredientName}");

        CreateSkillButtons();
        UpdateAllSkillButtons();
    }

    void CreateSkillButtons()
    {
        // Clear existing buttons
        foreach (Transform child in skillsUIPanel)
        {
            Destroy(child.gameObject);
        }

        // Reset lists
        for (int i = 0; i < 4; i++)
        {
            skillButtons[i] = null;
            skillCooldownTexts[i] = null;
            skillCooldownSliders[i] = null;
            skillNameTexts[i] = null;
            skillButtonImages[i] = null;
            skillButtonObjects[i] = null;
        }

        // Create 4 skill buttons
        for (int i = 1; i <= 4; i++)
        {
            CreateSkillButton(i);
        }
    }

    void CreateSkillButton(int skillNumber)
    {
        if (skillButtonPrefab == null || playerEnerling == null)
        {
            Debug.LogError("Cannot create skill button: prefab or playerEnerling is null");
            return;
        }

        GameObject buttonObj = Instantiate(skillButtonPrefab, skillsUIPanel);
        buttonObj.name = $"Skill{skillNumber}Button";

        // Get button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Skill button prefab has no Button component!");
            return;
        }

        // Get skill info
        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);
        int buttonIndex = skillNumber - 1;

        // Store references
        skillButtonObjects[buttonIndex] = buttonObj;
        skillButtons[buttonIndex] = button;
        skillButtonImages[buttonIndex] = buttonObj.GetComponent<Image>();

        // Find UI elements by name based on your hierarchy
        // SkillName (child of the button)
        TextMeshProUGUI skillNameText = buttonObj.transform.Find("SkillName")?.GetComponent<TextMeshProUGUI>();
        if (skillNameText != null && skill != null)
        {
            skillNameText.text = skill.skillName;
            skillNameTexts[buttonIndex] = skillNameText;
        }

        // DamageValue (child of the button)
        TextMeshProUGUI damageValueText = buttonObj.transform.Find("DamageValue")?.GetComponent<TextMeshProUGUI>();
        if (damageValueText != null && skill != null)
        {
            damageValueText.text = skill.GetValue().ToString();
        }

        // SkillType (child of the button)
        Image skillTypeImage = buttonObj.transform.Find("SkillType")?.GetComponent<Image>();
        if (skillTypeImage != null && skill != null)
        {
            switch (skill.type)
            {
                case IngredientDatabase.SkillInfo.SkillType.Heal:
                    skillTypeImage.sprite = healSkillSprite;
                    break;
                case IngredientDatabase.SkillInfo.SkillType.Damage:
                    skillTypeImage.sprite = damageSkillSprite;
                    break;
                case IngredientDatabase.SkillInfo.SkillType.Defend:
                    skillTypeImage.sprite = defendSkillSprite;
                    break;
            }
            skillTypeImage.preserveAspect = true;
        }

        // SkillCooldownSlider (child of the button)
        Slider cooldownSlider = buttonObj.transform.Find("SkillCooldownSlider")?.GetComponent<Slider>();
        if (cooldownSlider != null && skill != null)
        {
            skillCooldownSliders[buttonIndex] = cooldownSlider;

            // Configure slider
            cooldownSlider.minValue = 0;
            cooldownSlider.maxValue = skill.cooldownTurns > 0 ? skill.cooldownTurns : 1;
            cooldownSlider.value = 0;
            cooldownSlider.gameObject.SetActive(false); // Hidden when ready
        }

        // CooldownText (if it exists as a separate child)
        TextMeshProUGUI cooldownText = buttonObj.transform.Find("CooldownText")?.GetComponent<TextMeshProUGUI>();
        if (cooldownText == null)
        {
            // If not in prefab, create it
            cooldownText = CreateCooldownText(buttonObj);
        }
        skillCooldownTexts[buttonIndex] = cooldownText;
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }

        // Set button image (skill icon)
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null && skill != null && skill.skillSprite != null)
        {
            buttonImage.sprite = skill.skillSprite;
        }

        // Add click listener
        int skillNum = skillNumber;
        button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));

        Debug.Log($"Created Skill {skillNumber} button with cooldown: {skill?.cooldownTurns ?? 0}");
    }

    TextMeshProUGUI CreateCooldownText(GameObject buttonObj)
    {
        GameObject cooldownObj = new GameObject("CooldownText");
        cooldownObj.transform.SetParent(buttonObj.transform);
        cooldownObj.transform.localPosition = Vector3.zero;
        cooldownObj.transform.localScale = Vector3.one;

        TextMeshProUGUI cooldownText = cooldownObj.AddComponent<TextMeshProUGUI>();
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontSize = 36;
        cooldownText.fontStyle = FontStyles.Bold;
        cooldownText.color = cooldownTextColor;
        cooldownText.raycastTarget = false;

        return cooldownText;
    }

    IngredientDatabase.SkillInfo GetSkillByNumber(int skillNumber)
    {
        if (playerEnerling == null) return null;

        switch (skillNumber)
        {
            case 1: return playerEnerling.skill1;
            case 2: return playerEnerling.skill2;
            case 3: return playerEnerling.skill3;
            case 4: return playerEnerling.skill4;
            default: return null;
        }
    }

    public void OnSkillButtonClicked(int skillNumber)
    {
        Debug.Log($"=== Skill {skillNumber} button clicked ===");

        if (playerEnerling == null)
        {
            Debug.LogError("Player enerling not initialized!");
            return;
        }

        // Check if skill is ready FIRST
        if (!playerEnerling.IsSkillReady(skillNumber))
        {
            int cooldown = GetSkillCooldownValue(skillNumber);
            Debug.Log($"Skill {skillNumber} is on cooldown! Cooldown={cooldown}");
            StartCoroutine(FlashButtonCooldown(skillNumber));
            return;
        }

        // Check if it's player's turn
        if (turnSystem != null && !turnSystem.IsPlayerTurn())
        {
            Debug.Log("Not player's turn!");
            return;
        }

        // Check if system is animating
        if (turnSystem != null && turnSystem.IsAnimating())
        {
            Debug.Log("System is busy, please wait...");
            return;
        }

        // Check if skill exists
        var skill = GetSkillByNumber(skillNumber);
        if (skill == null)
        {
            Debug.LogError($"Skill {skillNumber} not found!");
            return;
        }

        Debug.Log($"Skill {skillNumber} is ready to use. Cooldown turns: {skill.cooldownTurns}");

        // Disable all buttons while processing
        SetButtonsInteractable(false);

        // Notify BattleEnerlingManager (it will handle setting cooldown after animation)
        if (battleManager != null)
        {
            battleManager.OnSkillButtonClicked(skillNumber);
        }
        else
        {
            Debug.LogError("BattleEnerlingManager not found!");
        }
    }

    public void FlashButtonOnCooldown(int skillNumber)
    {
        StartCoroutine(FlashButtonCooldown(skillNumber));
    }

    public void UpdateAllSkillButtons()
    {
        for (int i = 1; i <= 4; i++)
        {
            UpdateSkillButton(i);
        }
    }

    public void UpdateSkillButton(int skillNumber)
    {
        if (playerEnerling == null) return;

        int buttonIndex = skillNumber - 1;
        if (buttonIndex < 0 || buttonIndex >= skillButtons.Count || skillButtons[buttonIndex] == null)
            return;

        // Get fresh cooldown value from playerEnerling
        int cooldown = GetSkillCooldownValue(skillNumber);
        bool isReady = (cooldown == 0);

        Button button = skillButtons[buttonIndex];
        TextMeshProUGUI cooldownText = skillCooldownTexts[buttonIndex];
        Slider cooldownSlider = skillCooldownSliders[buttonIndex];
        Image buttonImage = skillButtonImages[buttonIndex];

        // Get skill info for max cooldown
        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);

        // Update cooldown slider
        if (cooldownSlider != null && skill != null)
        {
            if (cooldown > 0)
            {
                // Skill is on cooldown - show slider
                // IMPORTANT: Set maxValue to the skill's cooldownTurns
                cooldownSlider.maxValue = skill.cooldownTurns;
                // Show the current cooldown value (not the inverse)
                cooldownSlider.value = cooldown;
                cooldownSlider.gameObject.SetActive(true);

                Debug.Log($"Skill {skillNumber}: Cooldown={cooldown}, Max={skill.cooldownTurns}, SliderValue={cooldown}");
            }
            else
            {
                // Skill is ready - hide slider
                cooldownSlider.gameObject.SetActive(false);
            }
        }

        // Update button interactability
        button.interactable = isReady;

        // Update button color
        if (buttonImage != null)
        {
            buttonImage.color = isReady ? enabledButtonColor : disabledButtonColor;
        }

        // Update cooldown text
        if (cooldownText != null)
        {
            if (cooldown > 0)
            {
                cooldownText.text = cooldown.ToString();
                cooldownText.color = cooldownTextColor;
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }

        Debug.Log($"Skill {skillNumber}: Ready={isReady}, Cooldown={cooldown}, ButtonInteractable={button.interactable}");
    }

    // Helper method to get cooldown value directly from playerEnerling
    private int GetSkillCooldownValue(int skillNumber)
    {
        if (playerEnerling == null) return 0;

        switch (skillNumber)
        {
            case 1: return playerEnerling.skill1Cooldown;
            case 2: return playerEnerling.skill2Cooldown;
            case 3: return playerEnerling.skill3Cooldown;
            case 4: return playerEnerling.skill4Cooldown;
            default: return 0;
        }
    }

    IEnumerator FlashButtonCooldown(int skillNumber)
    {
        int buttonIndex = skillNumber - 1;
        if (buttonIndex < 0 || buttonIndex >= skillButtons.Count || skillButtons[buttonIndex] == null)
            yield break;

        Button button = skillButtons[buttonIndex];
        Image buttonImage = skillButtonImages[buttonIndex];
        if (buttonImage == null) yield break;

        Color originalColor = buttonImage.color;
        Color flashColor = Color.red;

        // Flash red 3 times
        for (int i = 0; i < 3; i++)
        {
            buttonImage.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            buttonImage.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (playerEnerling == null) return;

        for (int i = 1; i <= 4; i++)
        {
            int buttonIndex = i - 1;
            if (buttonIndex >= 0 && buttonIndex < skillButtons.Count && skillButtons[buttonIndex] != null)
            {
                bool skillReady = playerEnerling.IsSkillReady(i);
                // Only set interactable if the skill is ready AND we want it interactable
                skillButtons[buttonIndex].interactable = interactable && skillReady;

                // Update visual state immediately
                UpdateSkillButton(i);
            }
        }
    }

    public bool AreButtonsInteractable()
    {
        if (skillButtons.Count == 0) return false;
        foreach (Button button in skillButtons)
        {
            if (button != null && button.interactable)
                return true;
        }
        return false;
    }

    public void EndTurn()
    {
        if (playerEnerling != null)
        {
            // Reduce cooldowns by 1 each turn
            playerEnerling.ReduceCooldowns();
            Debug.Log("Reduced player skill cooldowns. New values: " +
                $"S1:{playerEnerling.skill1Cooldown}, S2:{playerEnerling.skill2Cooldown}, " +
                $"S3:{playerEnerling.skill3Cooldown}, S4:{playerEnerling.skill4Cooldown}");

            // Update all skill buttons
            UpdateAllSkillButtons();
        }
    }

    void Update()
    {
        // Update UI more frequently during player turn to ensure cooldowns are accurate
        if (turnSystem != null && turnSystem.IsPlayerTurn() && playerEnerling != null)
        {
            if (Time.frameCount % 15 == 0)
            {
                UpdateAllSkillButtons();
            }
        }
    }

    public void Cleanup()
    {
        if (skillsUIPanel != null)
        {
            foreach (Transform child in skillsUIPanel)
            {
                Destroy(child.gameObject);
            }
        }

        // Clear all lists
        skillButtons.Clear();
        skillCooldownTexts.Clear();
        skillCooldownSliders.Clear();
        skillNameTexts.Clear();
        skillButtonImages.Clear();
        skillButtonObjects.Clear();

        playerEnerling = null;
    }
}