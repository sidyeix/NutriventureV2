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

    [Header("Skill Button Text References")]
    private List<Button> skillButtons = new List<Button>();
    private List<TextMeshProUGUI> skillCooldownTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> skillNameTexts = new List<TextMeshProUGUI>();

    [Header("Cooldown UI Settings")]
    public Color cooldownTextColor = Color.red;
    public Color readyTextColor = Color.white;
    public Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color enabledButtonColor = Color.white;

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

        // Set button properties
        button.interactable = true;
        
        // Get skill info
        IngredientDatabase.SkillInfo skill = GetSkillByNumber(skillNumber);
        
        // Set button text (skill name)
        TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null && skill != null)
        {
            nameText.text = skill.skillName;
            skillNameTexts.Add(nameText);
        }
        else
        {
            skillNameTexts.Add(null);
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
        cooldownObj.transform.localScale = Vector3.one * 1.5f;
        
        TextMeshProUGUI cooldownText = cooldownObj.AddComponent<TextMeshProUGUI>();
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.fontSize = 24;
        cooldownText.color = cooldownTextColor;
        cooldownText.raycastTarget = false;
        cooldownText.gameObject.SetActive(false);
        
        skillCooldownTexts.Add(cooldownText);
        skillButtons.Add(button);

        // Add click listener
        int skillNum = skillNumber; // Capture for closure
        button.onClick.AddListener(() => OnSkillButtonClicked(skillNum));
        
        Debug.Log($"Created Skill {skillNumber} button");
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
        Debug.Log($"Skill {skillNumber} button clicked");
        
        if (playerEnerling == null)
        {
            Debug.LogError("Player enerling not initialized!");
            return;
        }

        // Check if skill is ready
        if (!playerEnerling.IsSkillReady(skillNumber))
        {
            Debug.Log($"Skill {skillNumber} is on cooldown! Cannot use.");
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

        // Disable all buttons while processing
        SetButtonsInteractable(false);

        // Notify BattleEnerlingManager
        if (battleManager != null)
        {
            battleManager.OnSkillButtonClicked(skillNumber);
        }
        else
        {
            Debug.LogError("BattleEnerlingManager not found!");
        }
    }

    public void UpdateAllSkillButtons()
    {
        for (int i = 1; i <= 4; i++)
        {
            UpdateSkillButton(i);
        }
    }

    void UpdateSkillButton(int skillNumber)
    {
        if (playerEnerling == null) return;
        
        int buttonIndex = skillNumber - 1;
        if (buttonIndex < 0 || buttonIndex >= skillButtons.Count || skillButtons[buttonIndex] == null)
            return;

        bool isReady = playerEnerling.IsSkillReady(skillNumber);
        int cooldown = GetSkillCooldown(skillNumber);
        Button button = skillButtons[buttonIndex];
        TextMeshProUGUI cooldownText = skillCooldownTexts[buttonIndex];

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

        // Debug log
        Debug.Log($"Skill {skillNumber}: Ready={isReady}, Cooldown={cooldown}, ButtonInteractable={button.interactable}");
    }

    int GetSkillCooldown(int skillNumber)
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
    }
}