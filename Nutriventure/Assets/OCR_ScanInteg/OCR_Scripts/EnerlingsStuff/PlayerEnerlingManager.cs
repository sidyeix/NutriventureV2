using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerEnerlingManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playerEnerlingStats;
    public GameObject skillBG;
    public Transform skillsPanel;

    [Header("Skill Button Prefabs")]
    public GameObject skillButtonPrefab;

    [Header("Skill Frame Sprites by Rarity")]
    public Sprite commonSkillFrame;
    public Sprite rareSkillFrame;
    public Sprite ultraRareSkillFrame;

    // References
    private BattleEnerlingManager battleManager;
    private IngredientDatabase.IngredientInfo playerEnerling;
    private TurnSystem turnSystem;

    // Skill tracking
    private Dictionary<int, GameObject> skillButtons = new Dictionary<int, GameObject>();
    private Dictionary<int, Slider> cooldownSliders = new Dictionary<int, Slider>();
    private Dictionary<int, int> currentCooldowns = new Dictionary<int, int>();
    private Dictionary<int, int> maxCooldowns = new Dictionary<int, int>();

    // Animation
    private Animator skillBGAnimator;
    private bool isInitialized = false;

    void Start()
    {
        InitializeReferences();
    }

    void InitializeReferences()
    {
        battleManager = FindObjectOfType<BattleEnerlingManager>();
        turnSystem = FindObjectOfType<TurnSystem>();

        if (skillBG != null)
        {
            skillBGAnimator = skillBG.GetComponent<Animator>();
            skillBG.SetActive(false);
        }

        if (playerEnerlingStats != null)
        {
            playerEnerlingStats.SetActive(false);
        }
    }

    public void InitializePlayerEnerling(string enerlingName)
    {
        if (battleManager == null)
        {
            InitializeReferences();
        }

        if (battleManager == null) return;

        playerEnerling = battleManager.GetBattleEnerling();
        if (playerEnerling == null) return;

        // Show UI with animation
        if (skillBG != null)
        {
            skillBG.SetActive(true);
            if (skillBGAnimator != null)
            {
                skillBGAnimator.SetTrigger("Show");
            }
        }

        if (playerEnerlingStats != null)
        {
            playerEnerlingStats.SetActive(true);
        }

        // Create skill buttons
        CreateSkillButtons();
        UpdateSkillFrames();

        isInitialized = true;
        Debug.Log($"Player Enerling Manager initialized for: {enerlingName}");
    }

    void CreateSkillButtons()
    {
        // Clear existing buttons
        foreach (var button in skillButtons.Values)
        {
            Destroy(button);
        }
        skillButtons.Clear();
        cooldownSliders.Clear();
        currentCooldowns.Clear();
        maxCooldowns.Clear();

        if (playerEnerling == null || skillButtonPrefab == null || skillsPanel == null) return;

        // Create buttons for all 4 skills
        for (int i = 1; i <= 4; i++)
        {
            IngredientDatabase.SkillInfo skill = GetSkillByNumber(i);
            if (skill != null)
            {
                // Use the original skill button prefab
                GameObject skillButton = Instantiate(skillButtonPrefab, skillsPanel);

                // Set skill name
                Transform skillNameTransform = skillButton.transform.Find("SkillName");
                if (skillNameTransform != null)
                {
                    TextMeshProUGUI skillNameText = skillNameTransform.GetComponent<TextMeshProUGUI>();
                    if (skillNameText != null)
                    {
                        skillNameText.text = skill.skillName;
                    }
                }

                // Set skill sprite on the button's main Image
                Image buttonImage = skillButton.GetComponent<Image>();
                if (buttonImage != null && skill.skillSprite != null)
                {
                    buttonImage.sprite = skill.skillSprite;
                    buttonImage.preserveAspect = true;
                }

                // Find the SkillFrame child and update it based on rarity
                Transform skillFrameTransform = skillButton.transform.Find("SkillFrame");
                if (skillFrameTransform != null)
                {
                    Image frameImage = skillFrameTransform.GetComponent<Image>();
                    if (frameImage != null)
                    {
                        frameImage.enabled = true;
                    }
                }

                // Find and setup the cooldown slider that's already in the prefab
                Transform cooldownSliderTransform = skillButton.transform.Find("SkillCooldownSlider");
                if (cooldownSliderTransform != null)
                {
                    Slider cooldownSlider = cooldownSliderTransform.GetComponent<Slider>();
                    if (cooldownSlider != null)
                    {
                        cooldownSliders[i] = cooldownSlider;
                        maxCooldowns[i] = skill.cooldownTurns;
                        currentCooldowns[i] = skill.cooldownTurns; // Start with full cooldown

                        // Set up slider - value starts at 0 (empty) and fills to max as cooldown decreases
                        cooldownSlider.maxValue = skill.cooldownTurns;
                        cooldownSlider.minValue = 0;

                        if (skill.cooldownTurns > 0)
                        {
                            // Start with value at max (full cooldown)
                            cooldownSlider.value = 0; // Start at 0, will increase as cooldown decreases
                            cooldownSlider.gameObject.SetActive(true);

                            // Set cooldown text
                            Transform cooldownTextTransform = skillButton.transform.Find("CooldownText");
                            if (cooldownTextTransform != null)
                            {
                                TextMeshProUGUI cooldownText = cooldownTextTransform.GetComponent<TextMeshProUGUI>();
                                if (cooldownText != null)
                                {
                                    cooldownText.text = skill.cooldownTurns.ToString();
                                }
                            }
                        }
                        else
                        {
                            // No cooldown, hide slider
                            cooldownSlider.gameObject.SetActive(false);
                        }
                    }
                }

                // Add button listener
                Button button = skillButton.GetComponent<Button>();
                if (button != null)
                {
                    int skillNum = i;
                    button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));

                    // Set initial button state based on cooldown
                    bool isReady = currentCooldowns[i] <= 0;
                    button.interactable = isReady;
                    UpdateButtonAppearance(button, isReady);
                }

                skillButtons[i] = skillButton;

                Debug.Log($"Created skill button for Skill {i}: {skill.skillName}, Cooldown: {skill.cooldownTurns}, Ready: {currentCooldowns[i] <= 0}");
            }
        }

        // Update skill frames after creating all buttons
        UpdateSkillFrames();
    }

    void UpdateSkillFrames()
    {
        if (playerEnerling == null) return;

        Sprite frameSprite = GetSkillFrameByRarity(playerEnerling.rarity);

        foreach (var kvp in skillButtons)
        {
            Transform skillFrameTransform = kvp.Value.transform.Find("SkillFrame");
            if (skillFrameTransform != null)
            {
                Image frameImage = skillFrameTransform.GetComponent<Image>();
                if (frameImage != null && frameSprite != null)
                {
                    frameImage.sprite = frameSprite;
                    frameImage.enabled = true;
                }
            }
        }
    }

    Sprite GetSkillFrameByRarity(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common:
                return commonSkillFrame;
            case IngredientDatabase.Rarity.Rare:
                return rareSkillFrame;
            case IngredientDatabase.Rarity.UltraRare:
                return ultraRareSkillFrame;
            default:
                return commonSkillFrame;
        }
    }

    void UpdateButtonAppearance(Button button, bool isEnabled)
    {
        if (button == null) return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (isEnabled)
            {
                buttonImage.color = Color.white;
            }
            else
            {
                buttonImage.color = new Color(0.455f, 0.435f, 0.435f, 1f);
            }
        }
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
        if (!isInitialized) return;

        if (!IsSkillReady(skillNumber))
        {
            Debug.Log($"Skill {skillNumber} is on cooldown!");
            return;
        }

        // Set cooldown
        var skill = GetSkillByNumber(skillNumber);
        if (skill != null && skill.cooldownTurns > 0)
        {
            currentCooldowns[skillNumber] = skill.cooldownTurns;

            // Update cooldown slider - show it
            if (cooldownSliders.ContainsKey(skillNumber))
            {
                cooldownSliders[skillNumber].gameObject.SetActive(true);
                cooldownSliders[skillNumber].value = 0; // Start at 0 (full cooldown)
            }

            // Update cooldown text
            if (skillButtons.ContainsKey(skillNumber))
            {
                Transform cooldownTextTransform = skillButtons[skillNumber].transform.Find("CooldownText");
                if (cooldownTextTransform != null)
                {
                    TextMeshProUGUI cooldownText = cooldownTextTransform.GetComponent<TextMeshProUGUI>();
                    if (cooldownText != null)
                    {
                        cooldownText.text = skill.cooldownTurns.ToString();
                    }
                }
            }

            // Disable button and change appearance
            if (skillButtons.ContainsKey(skillNumber))
            {
                Button button = skillButtons[skillNumber].GetComponent<Button>();
                button.interactable = false;
                UpdateButtonAppearance(button, false);
            }
        }

        // Trigger skill through battle manager
        if (battleManager != null)
        {
            battleManager.OnSkillButtonClicked(skillNumber);
        }

        // Notify turn system
        if (turnSystem != null)
        {
            turnSystem.PlayerSkillChosen();
        }
    }

    bool IsSkillReady(int skillNumber)
    {
        return currentCooldowns.ContainsKey(skillNumber) && currentCooldowns[skillNumber] <= 0;
    }

    public void EndTurn()
    {
        if (!isInitialized) return;

        // Reduce cooldowns by 1 for all skills
        foreach (var skillNum in new List<int>(currentCooldowns.Keys))
        {
            if (currentCooldowns[skillNum] > 0)
            {
                currentCooldowns[skillNum]--;

                // Update slider if active
                if (cooldownSliders.ContainsKey(skillNum) && cooldownSliders[skillNum].gameObject.activeSelf)
                {
                    // Calculate remaining turns as percentage of max cooldown
                    float remainingTurns = currentCooldowns[skillNum];
                    float maxTurns = maxCooldowns[skillNum];

                    // Slider value increases from 0 to max as cooldown decreases
                    // Formula: (maxTurns - remainingTurns) / maxTurns * maxTurns
                    float sliderValue = (maxTurns - remainingTurns) / maxTurns * maxTurns;
                    cooldownSliders[skillNum].value = sliderValue;
                }

                // Update cooldown text
                if (skillButtons.ContainsKey(skillNum))
                {
                    Transform cooldownTextTransform = skillButtons[skillNum].transform.Find("CooldownText");
                    if (cooldownTextTransform != null)
                    {
                        TextMeshProUGUI cooldownText = cooldownTextTransform.GetComponent<TextMeshProUGUI>();
                        if (cooldownText != null)
                        {
                            cooldownText.text = currentCooldowns[skillNum] > 0 ? currentCooldowns[skillNum].ToString() : "";
                        }
                    }
                }

                // Check if cooldown just ended
                if (currentCooldowns[skillNum] == 0 && skillButtons.ContainsKey(skillNum))
                {
                    Button button = skillButtons[skillNum].GetComponent<Button>();
                    button.interactable = true;
                    UpdateButtonAppearance(button, true);

                    // Hide cooldown slider when cooldown is complete
                    if (cooldownSliders.ContainsKey(skillNum))
                    {
                        cooldownSliders[skillNum].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void SetButtonsInteractable(bool interactable)
    {
        foreach (var kvp in skillButtons)
        {
            Button button = kvp.Value.GetComponent<Button>();
            if (button != null)
            {
                bool isReady = IsSkillReady(kvp.Key);
                button.interactable = interactable && isReady;
                UpdateButtonAppearance(button, button.interactable);
            }
        }
    }

    public void Cleanup()
    {
        StopAllCoroutines();

        if (skillBG != null)
        {
            skillBG.SetActive(false);
        }

        if (playerEnerlingStats != null)
        {
            playerEnerlingStats.SetActive(false);
        }

        foreach (var button in skillButtons.Values)
        {
            Destroy(button);
        }
        skillButtons.Clear();
        cooldownSliders.Clear();
        currentCooldowns.Clear();
        maxCooldowns.Clear();

        isInitialized = false;
        playerEnerling = null;
    }

    public int GetSkillCooldown(int skillNumber)
    {
        return currentCooldowns.ContainsKey(skillNumber) ? currentCooldowns[skillNumber] : 0;
    }

    public bool HasAvailableSkills()
    {
        foreach (var cooldown in currentCooldowns.Values)
        {
            if (cooldown <= 0) return true;
        }
        return false;
    }
}