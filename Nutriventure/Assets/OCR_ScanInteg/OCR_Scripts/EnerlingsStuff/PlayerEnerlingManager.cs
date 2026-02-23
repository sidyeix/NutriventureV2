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

    [Header("Skill Button Text References")]
    private List<Button> skillButtons = new List<Button>();
    private List<TextMeshProUGUI> skillCooldownTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> skillNameTexts = new List<TextMeshProUGUI>();

    [Header("Cooldown UI Settings")]
    public Color cooldownTextColor = Color.red;
    public Color readyTextColor = Color.white;
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color enabledButtonColor = Color.white;

    [Header("Cooldown Slider References")]
    public Slider skill1CooldownSlider;
    public Slider skill2CooldownSlider;
    public Slider skill3CooldownSlider;
    public Slider skill4CooldownSlider;

    [Header("Current Enerling")]
    public IngredientDatabase.IngredientInfo playerEnerling;

    private BattleEnerlingManager battleManager;
    private TurnSystem turnSystem;

    void Start()
    {
        battleManager = FindObjectOfType<BattleEnerlingManager>();
        turnSystem = FindObjectOfType<TurnSystem>();

        // Initialize lists
        for (int i = 0; i < 4; i++)
        {
            skillButtons.Add(null);
            skillCooldownTexts.Add(null);
            skillNameTexts.Add(null);
        }
    }

    public void InitializePlayerEnerling(string enerlingName)
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase not assigned!");
            return;
        }

        playerEnerling = ingredientDatabase.CreateBattleCopy(enerlingName);
        if (playerEnerling == null)
        {
            Debug.LogError($"Failed to create player enerling: {enerlingName}");
            return;
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

        skillButtons.Clear();
        skillCooldownTexts.Clear();
        skillNameTexts.Clear();

        // Reset slider references
        skill1CooldownSlider = null;
        skill2CooldownSlider = null;
        skill3CooldownSlider = null;
        skill4CooldownSlider = null;

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

        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Skill button prefab has no Button component!");
            return;
        }

        // Get skill info
        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);

        // Find UI elements by name (based on your prefab structure)
        TextMeshProUGUI damageValueText = FindChildComponent<TextMeshProUGUI>(buttonObj, "DamageValue");
        Image skillTypeImage = FindChildComponent<Image>(buttonObj, "SkillType");
        TextMeshProUGUI skillNameText = FindChildComponent<TextMeshProUGUI>(buttonObj, "SkillName");
        Slider skillCooldownSlider = FindChildComponent<Slider>(buttonObj, "SkillCooldownSlider");

        // Set skill name
        if (skillNameText != null && skill != null)
        {
            skillNameText.text = skill.skillName;
            skillNameTexts.Add(skillNameText);
        }
        else
        {
            skillNameTexts.Add(null);
        }

        // Set base value
        if (damageValueText != null && skill != null)
        {
            damageValueText.text = skill.GetValue().ToString();
        }

        // Set skill type image
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

        // Initialize cooldown slider
        if (skillCooldownSlider != null && skill != null)
        {
            if (skill.cooldownTurns > 0)
            {
                skillCooldownSlider.maxValue = skill.cooldownTurns;
                skillCooldownSlider.value = 0; // Start at 0 (ready)
                skillCooldownSlider.gameObject.SetActive(false); // Hidden when ready
            }
            else
            {
                skillCooldownSlider.gameObject.SetActive(false);
            }

            // Store reference to cooldown slider
            switch (skillNumber)
            {
                case 1: skill1CooldownSlider = skillCooldownSlider; break;
                case 2: skill2CooldownSlider = skillCooldownSlider; break;
                case 3: skill3CooldownSlider = skillCooldownSlider; break;
                case 4: skill4CooldownSlider = skillCooldownSlider; break;
            }
        }

        // Set button image (skill icon)
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null && skill != null && skill.skillSprite != null)
        {
            buttonImage.sprite = skill.skillSprite;
        }

        // Create cooldown text object
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
        cooldownText.gameObject.SetActive(false);

        skillCooldownTexts.Add(cooldownText);
        skillButtons.Add(button);

        // Add click listener
        int skillNum = skillNumber; // Capture for closure
        button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));

        Debug.Log($"Created Skill {skillNumber} button with cooldown: {skill?.cooldownTurns ?? 0}");
    }

    // Helper method to find child components
    private T FindChildComponent<T>(GameObject parent, string childName) where T : Component
    {
        Transform childTransform = parent.transform.Find(childName);
        if (childTransform != null)
        {
            return childTransform.GetComponent<T>();
        }
        return null;
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

        bool isReady = playerEnerling.IsSkillReady(skillNumber);
        Button button = skillButtons[buttonIndex];
        TextMeshProUGUI cooldownText = skillCooldownTexts[buttonIndex];

        // Get skill cooldown
        int cooldown = GetSkillCooldownValue(skillNumber);

        // Get the cooldown slider
        Slider cooldownSlider = GetCooldownSliderByNumber(skillNumber);

        // Get skill info for max cooldown
        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);

        // Update cooldown slider
        if (cooldownSlider != null && skill != null)
        {
            if (cooldown > 0)
            {
                // Skill is on cooldown
                cooldownSlider.maxValue = skill.cooldownTurns;
                // Slider shows progress from 0 to max
                // When cooldown = max (just used): slider value = 0
                // When cooldown = 1 (almost ready): slider value = max-1
                // When cooldown = 0 (ready): slider hidden
                int remainingCooldown = cooldown;
                int currentSliderValue = skill.cooldownTurns - remainingCooldown;
                cooldownSlider.value = currentSliderValue;
                cooldownSlider.gameObject.SetActive(true);

                Debug.Log($"Skill {skillNumber}: Cooldown={cooldown}, Max={skill.cooldownTurns}, SliderValue={currentSliderValue}");
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
        Image buttonImage = button.GetComponent<Image>();
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

    // Helper method to get cooldown value
    private int GetSkillCooldownValue(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return playerEnerling.skill1Cooldown;
            case 2: return playerEnerling.skill2Cooldown;
            case 3: return playerEnerling.skill3Cooldown;
            case 4: return playerEnerling.skill4Cooldown;
            default: return 0;
        }
    }

    private Slider GetCooldownSliderByNumber(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return skill1CooldownSlider;
            case 2: return skill2CooldownSlider;
            case 3: return skill3CooldownSlider;
            case 4: return skill4CooldownSlider;
            default: return null;
        }
    }

    IEnumerator FlashButtonCooldown(int skillNumber)
    {
        int buttonIndex = skillNumber - 1;
        if (buttonIndex < 0 || buttonIndex >= skillButtons.Count || skillButtons[buttonIndex] == null)
            yield break;

        Button button = skillButtons[buttonIndex];
        Image buttonImage = button.GetComponent<Image>();
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

        // Restore original state
        UpdateSkillButton(skillNumber);
    }

    public void SetButtonsInteractable(bool interactable)
    {
        // Only enable buttons that are not on cooldown
        if (playerEnerling != null)
        {
            for (int i = 1; i <= 4; i++)
            {
                int buttonIndex = i - 1;
                if (buttonIndex >= 0 && buttonIndex < skillButtons.Count && skillButtons[buttonIndex] != null)
                {
                    bool skillReady = playerEnerling.IsSkillReady(i);
                    skillButtons[buttonIndex].interactable = interactable && skillReady;

                    // Update visual state
                    UpdateSkillButton(i);
                }
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
            // Reduce cooldowns
            playerEnerling.ReduceCooldowns();
            Debug.Log("Reduced player skill cooldowns");

            // Update all skill buttons
            UpdateAllSkillButtons();
        }
    }

    void Update()
    {
        // Update UI periodically
        if (Time.frameCount % 30 == 0) // Update every 30 frames
        {
            UpdateAllSkillButtons();
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

        skillButtons.Clear();
        skillCooldownTexts.Clear();
        skillNameTexts.Clear();

        playerEnerling = null;

        // Reset cooldown sliders
        skill1CooldownSlider = null;
        skill2CooldownSlider = null;
        skill3CooldownSlider = null;
        skill4CooldownSlider = null;
    }
}