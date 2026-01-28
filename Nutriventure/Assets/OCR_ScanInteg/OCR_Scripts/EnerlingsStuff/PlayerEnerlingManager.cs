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
    public Transform skillsPanel; // This should be the same skillsPanel from BattleEnerlingManager

    [Header("Skill Button Prefabs")]
    public GameObject skillButtonPrefab; // This should be the original prefab from BattleEnerlingManager

    [Header("Skill Frame Sprites by Rarity")]
    public Sprite commonSkillFrame;
    public Sprite rareSkillFrame;
    public Sprite ultraRareSkillFrame;

    [Header("Cooldown UI")]
    public GameObject cooldownOverlayPrefab;

    // References
    private BattleEnerlingManager battleManager;
    private IngredientDatabase.IngredientInfo playerEnerling;
    private TurnSystem turnSystem;

    // Skill tracking
    private Dictionary<int, GameObject> skillButtons = new Dictionary<int, GameObject>();
    private Dictionary<int, Slider> cooldownSliders = new Dictionary<int, Slider>();
    private Dictionary<int, int> currentCooldowns = new Dictionary<int, int>();

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
                        // Frame will be updated in UpdateSkillFrames()
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
                        cooldownSlider.maxValue = skill.cooldownTurns;
                        cooldownSlider.value = 0;
                        cooldownSlider.gameObject.SetActive(false); // Hidden initially

                        // FIXED: Don't change the color of the cooldown slider fill
                        // Keep it as is (the original color from the prefab)
                    }
                }

                // Add button listener
                Button button = skillButton.GetComponent<Button>();
                if (button != null)
                {
                    int skillNum = i;
                    button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));
                    UpdateButtonAppearance(button, true);
                }

                skillButtons[i] = skillButton;
                currentCooldowns[i] = 0;

                Debug.Log($"Created skill button for Skill {i}: {skill.skillName}");
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
                // White (#FFFFFF) when enabled
                buttonImage.color = Color.white;
            }
            else
            {
                // Gray (#746F6F) when disabled
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

            // Show cooldown overlay
            if (cooldownSliders.ContainsKey(skillNumber))
            {
                cooldownSliders[skillNumber].gameObject.SetActive(true);
                cooldownSliders[skillNumber].maxValue = skill.cooldownTurns;
                cooldownSliders[skillNumber].value = skill.cooldownTurns; // Start full

                // FIXED: Don't animate the slider value, keep it static
                // The color changing should only happen to the button
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

    // Call this at the end of each turn
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
                    var skill = GetSkillByNumber(skillNum);
                    if (skill != null && skill.cooldownTurns > 0)
                    {
                        // Set slider value based on remaining cooldown
                        float remainingValue = currentCooldowns[skillNum];
                        cooldownSliders[skillNum].value = remainingValue;
                    }
                }

                // Check if cooldown just ended
                if (currentCooldowns[skillNum] == 0 && skillButtons.ContainsKey(skillNum))
                {
                    Button button = skillButtons[skillNum].GetComponent<Button>();
                    button.interactable = true;
                    UpdateButtonAppearance(button, true);

                    if (cooldownSliders.ContainsKey(skillNum))
                    {
                        // Hide the slider immediately (no fade)
                        cooldownSliders[skillNum].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    // Enable/disable all skill buttons
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

    // Clean up
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

        isInitialized = false;
        playerEnerling = null;
    }

    // Get current cooldown for a skill
    public int GetSkillCooldown(int skillNumber)
    {
        return currentCooldowns.ContainsKey(skillNumber) ? currentCooldowns[skillNumber] : 0;
    }

    // Check if any skill is available
    public bool HasAvailableSkills()
    {
        foreach (var cooldown in currentCooldowns.Values)
        {
            if (cooldown <= 0) return true;
        }
        return false;
    }
}