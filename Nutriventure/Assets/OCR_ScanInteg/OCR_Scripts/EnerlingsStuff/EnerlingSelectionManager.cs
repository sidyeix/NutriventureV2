using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class EnerlingSelectionManager : MonoBehaviour
{
    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("UI References - Content & Rows")]
    public Transform contentParent;
    public GameObject oddRowPrefab;
    public GameObject evenRowPrefab;
    public GameObject enerlingButtonPrefab;

    [Header("UI References - Filters")]
    public Button allFilterButton;
    public Button commonFilterButton;
    public Button rareFilterButton;
    public Button ultraRareFilterButton;

    public Button allKingdomButton;
    public Button nutriKingdomButton;
    public Button alerthiaButton;
    public Button suragriaButton;
    public Button preserviaButton;

    [Header("UI References - Enerling Info Panel")]
    public GameObject enerlingInfoPanel;
    public TextMeshProUGUI enerlingNameText;
    public Image rarityIconImage;
    public TextMeshProUGUI currentLifeText;
    public Slider lifeSlider;
    public TextMeshProUGUI enerlingDescriptionText;
    public Image enerlingIconImage;
    public TextMeshProUGUI baseAttackText; // Will show just the number
    public TextMeshProUGUI organsLabelText;
    public Transform addedAbilityPanel; // Parent for organ images
    public GameObject organImagePrefab;
    public TextMeshProUGUI addedAbilityText;

    [Header("UI References - Armor")]
    public TextMeshProUGUI armorText;
    public Slider armorSlider;

    [Header("UI References - Info Panel Frame")]
    public Image infoPanelFrame; // Frame in the enerling side information

    [Header("UI References - Skills Panel")]
    public Transform skillsUIPanel; // Parent for skill buttons
    public GameObject skillButtonPrefab; // Make sure this has Button component!
    public TextMeshProUGUI skillDescriptionText;

    [Header("UI References - Selection")]
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;

    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = new Color(0.52f, 0.52f, 0.52f); // #858585 in RGB (0-1)
    public Color disabledButtonColor = Color.gray;

    [Header("Organ Sprites Mapping")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;

    [Header("Skill Button Settings")]
    public Sprite defaultSkillIcon; // Fallback icon if skill has no sprite

    // Current filters
    private IngredientDatabase.Rarity currentRarityFilter = IngredientDatabase.Rarity.Common;
    private IngredientDatabase.KingdomOrigin currentKingdomFilter = IngredientDatabase.KingdomOrigin.NutriKingdom;
    private bool useRarityFilter = false;
    private bool useKingdomFilter = false;

    // Current selection
    private string selectedEnerlingName = "";
    private Dictionary<string, GameObject> enerlingButtons = new Dictionary<string, GameObject>();
    private List<GameObject> currentRows = new List<GameObject>();

    // Store current filtered list for auto-selection
    private List<IngredientDatabase.IngredientInfo> currentFilteredEnerlings = new List<IngredientDatabase.IngredientInfo>();

    void Start()
    {
        // Wait for PersistentDataManager to initialize
        StartCoroutine(InitializeAfterDelay());
    }

    IEnumerator InitializeAfterDelay()
    {
        yield return null; // Wait one frame for PersistentDataManager to initialize

        InitializeDatabase();
        SetupFilterButtons();
        SetupKingdomButtons();
        DisplayAllUnlockedEnerlings();
        UpdateSelectButton();
        LoadSelectedEnerling();
    }

    void InitializeDatabase()
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase not assigned!");
            return;
        }

        // CRITICAL: Wait for PersistentDataManager to be ready
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("PersistentDataManager not found! Make sure it's in the scene.");

            // Create a temporary unlock for testing if no PersistentDataManager
            if (ingredientDatabase.ingredients.Count > 0)
            {
                Debug.LogWarning("No PersistentDataManager - unlocking first 3 for testing");
                for (int i = 0; i < Mathf.Min(3, ingredientDatabase.ingredients.Count); i++)
                {
                    ingredientDatabase.ingredients[i].isUnlocked = true;
                }
            }
            return;
        }

        // Make sure the database reference is set in PersistentDataManager
        if (PersistentDataManager.Instance.ingredientDatabase == null)
        {
            PersistentDataManager.Instance.ingredientDatabase = ingredientDatabase;
            Debug.Log("Assigned ingredientDatabase to PersistentDataManager");
        }

        // Check what's in the database
        Debug.Log($"Database initialized with {ingredientDatabase.ingredients.Count} total ingredients");

        // Count unlocked ingredients
        int unlockedCount = 0;
        foreach (var ingredient in ingredientDatabase.ingredients)
        {
            if (ingredient.isUnlocked)
            {
                unlockedCount++;
                Debug.Log($"Found unlocked: {ingredient.ingredientName}");
            }
        }
        Debug.Log($"Total unlocked in database: {unlockedCount}");

        // If still no unlocks, unlock first 3 for testing
        if (unlockedCount == 0 && ingredientDatabase.ingredients.Count > 0)
        {
            Debug.Log("No enerlings unlocked. Unlocking first 3 for testing...");
            for (int i = 0; i < Mathf.Min(3, ingredientDatabase.ingredients.Count); i++)
            {
                if (PersistentDataManager.Instance != null)
                {
                    PersistentDataManager.Instance.UnlockEnerling(ingredientDatabase.ingredients[i].ingredientName);
                }
                else
                {
                    ingredientDatabase.ingredients[i].isUnlocked = true;
                }
            }
        }
    }

    void SetupFilterButtons()
    {
        allFilterButton.onClick.AddListener(() => SetRarityFilter(false, IngredientDatabase.Rarity.Common));
        commonFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.Common));
        rareFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.Rare));
        ultraRareFilterButton.onClick.AddListener(() => SetRarityFilter(true, IngredientDatabase.Rarity.UltraRare));

        // Set initial state
        UpdateFilterButtonColors();
    }

    void SetupKingdomButtons()
    {
        allKingdomButton.onClick.AddListener(() => SetKingdomFilter(false, IngredientDatabase.KingdomOrigin.NutriKingdom));
        nutriKingdomButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.NutriKingdom));
        alerthiaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Alerthia));
        suragriaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Suragria));
        preserviaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Preservia));

        // Set initial state
        UpdateKingdomButtonColors();
    }

    void SetRarityFilter(bool useFilter, IngredientDatabase.Rarity rarity)
    {
        useRarityFilter = useFilter;
        currentRarityFilter = rarity;
        UpdateFilterButtonColors();
        RefreshEnerlingDisplay();

        // After filtering, auto-select first enerling if current selection is not in filtered list
        AutoSelectFirstAfterFilter();
    }

    void SetKingdomFilter(bool useFilter, IngredientDatabase.KingdomOrigin kingdom)
    {
        useKingdomFilter = useFilter;
        currentKingdomFilter = kingdom;
        UpdateKingdomButtonColors();
        RefreshEnerlingDisplay();

        // After filtering, auto-select first enerling if current selection is not in filtered list
        AutoSelectFirstAfterFilter();
    }

    void AutoSelectFirstAfterFilter()
    {
        // Check if currently selected enerling is in the filtered list
        bool currentSelectionInFilter = false;
        foreach (var enerling in currentFilteredEnerlings)
        {
            if (enerling.ingredientName == selectedEnerlingName)
            {
                currentSelectionInFilter = true;
                break;
            }
        }

        // If current selection is not in filtered list OR no selection exists, select first enerling
        if (!currentSelectionInFilter && currentFilteredEnerlings.Count > 0)
        {
            string firstEnerlingName = currentFilteredEnerlings[0].ingredientName;
            Debug.Log($"Auto-selecting first enerling after filter: {firstEnerlingName}");
            OnEnerlingButtonClicked(firstEnerlingName);
        }
        else if (currentFilteredEnerlings.Count == 0)
        {
            // No enerlings in filter, clear selection
            selectedEnerlingName = "";
            enerlingInfoPanel.SetActive(false);
            UpdateSelectButton();
            Debug.LogWarning("No enerlings in current filter!");
        }
    }

    void UpdateFilterButtonColors()
    {
        // Reset all buttons
        allFilterButton.image.color = normalButtonColor;
        commonFilterButton.image.color = normalButtonColor;
        rareFilterButton.image.color = normalButtonColor;
        ultraRareFilterButton.image.color = normalButtonColor;

        // Highlight selected
        if (!useRarityFilter)
        {
            allFilterButton.image.color = selectedButtonColor;
        }
        else
        {
            switch (currentRarityFilter)
            {
                case IngredientDatabase.Rarity.Common:
                    commonFilterButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.Rarity.Rare:
                    rareFilterButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.Rarity.UltraRare:
                    ultraRareFilterButton.image.color = selectedButtonColor;
                    break;
            }
        }
    }

    void UpdateKingdomButtonColors()
    {
        // Reset all buttons
        allKingdomButton.image.color = normalButtonColor;
        nutriKingdomButton.image.color = normalButtonColor;
        alerthiaButton.image.color = normalButtonColor;
        suragriaButton.image.color = normalButtonColor;
        preserviaButton.image.color = normalButtonColor;

        // Highlight selected
        if (!useKingdomFilter)
        {
            allKingdomButton.image.color = selectedButtonColor;
        }
        else
        {
            switch (currentKingdomFilter)
            {
                case IngredientDatabase.KingdomOrigin.NutriKingdom:
                    nutriKingdomButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Alerthia:
                    alerthiaButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Suragria:
                    suragriaButton.image.color = selectedButtonColor;
                    break;
                case IngredientDatabase.KingdomOrigin.Preservia:
                    preserviaButton.image.color = selectedButtonColor;
                    break;
            }
        }
    }

    void RefreshEnerlingDisplay()
    {
        ClearCurrentDisplay();

        // Get filtered enerlings and store them
        currentFilteredEnerlings = ingredientDatabase.GetIngredientsByFilter(
            currentRarityFilter,
            currentKingdomFilter,
            useRarityFilter,
            useKingdomFilter
        );

        DisplayEnerlings(currentFilteredEnerlings);
    }

    void DisplayAllUnlockedEnerlings()
    {
        ClearCurrentDisplay();

        // Get unlocked enerlings from database
        var unlockedEnerlings = ingredientDatabase.GetUnlockedIngredients();
        currentFilteredEnerlings = unlockedEnerlings; // Store for auto-selection

        Debug.Log($"DisplayAllUnlockedEnerlings: Found {unlockedEnerlings.Count} unlocked enerlings");

        if (unlockedEnerlings.Count == 0)
        {
            // If nothing is unlocked, show a message in UI
            Debug.LogWarning("No enerlings unlocked yet!");
            enerlingInfoPanel.SetActive(false);
            UpdateSelectButton();

            // For debugging: List all ingredients and their unlocked status
            Debug.Log("All ingredients status:");
            foreach (var ingredient in ingredientDatabase.ingredients)
            {
                Debug.Log($"- {ingredient.ingredientName}: unlocked={ingredient.isUnlocked}");
            }

            return;
        }

        DisplayEnerlings(unlockedEnerlings);
    }

    void DisplayEnerlings(List<IngredientDatabase.IngredientInfo> enerlings)
    {
        if (enerlings == null || enerlings.Count == 0)
        {
            Debug.Log("No enerlings to display");
            enerlingInfoPanel.SetActive(false);
            return;
        }

        Debug.Log($"Displaying {enerlings.Count} enerlings");

        // Sort by rarity (Common -> Rare -> UltraRare), then by name
        enerlings.Sort((a, b) =>
        {
            int rarityCompare = a.rarity.CompareTo(b.rarity);
            if (rarityCompare != 0) return rarityCompare;
            return a.ingredientName.CompareTo(b.ingredientName);
        });

        int enerlingIndex = 0;
        int rowIndex = 0;

        while (enerlingIndex < enerlings.Count)
        {
            // Create row (odd or even)
            GameObject rowPrefab = (rowIndex % 2 == 0) ? oddRowPrefab : evenRowPrefab;
            GameObject row = Instantiate(rowPrefab, contentParent);
            currentRows.Add(row);

            // Get max buttons per row (4 for odd, 3 for even)
            int maxButtons = (rowIndex % 2 == 0) ? 4 : 3;

            // Fill row with buttons
            for (int i = 0; i < maxButtons && enerlingIndex < enerlings.Count; i++)
            {
                var enerling = enerlings[enerlingIndex];
                CreateEnerlingButton(enerling, row.transform);
                enerlingIndex++;
            }

            rowIndex++;
        }
    }

    void CreateEnerlingButton(IngredientDatabase.IngredientInfo enerling, Transform parent)
    {
        if (enerling == null || string.IsNullOrEmpty(enerling.ingredientName))
        {
            Debug.LogWarning("Trying to create button for null enerling");
            return;
        }

        GameObject buttonObj = Instantiate(enerlingButtonPrefab, parent);

        // Get the EnerlingButtonController component
        EnerlingButtonController buttonController = buttonObj.GetComponent<EnerlingButtonController>();

        if (buttonController != null)
        {
            // Initialize the button controller with enerling data AND the database
            buttonController.Initialize(
                enerling.ingredientName,
                enerling.enerlingSprite,
                enerling.rarity,
                ingredientDatabase // Pass the database reference
            );

            // Get the button component for adding click listener
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnEnerlingButtonClicked(enerling.ingredientName));
            }
        }
        else
        {
            Debug.LogWarning("Enerling button prefab has no EnerlingButtonController component! Using fallback...");

            // Fallback to old method
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("Enerling button prefab has no Button component!");
                return;
            }

            // Set the enerling sprite on the main button Image
            Image enerlingImage = buttonObj.GetComponent<Image>();
            if (enerlingImage != null && enerling.enerlingSprite != null)
            {
                enerlingImage.sprite = enerling.enerlingSprite;
                enerlingImage.preserveAspect = true;
            }

            // Set the frame sprite
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(enerling.rarity);
            Transform frameTransform = buttonObj.transform.Find("Frame");
            if (frameTransform != null)
            {
                Image frameImage = frameTransform.GetComponent<Image>();
                if (frameImage != null && frameSprite != null)
                {
                    frameImage.sprite = frameSprite;
                }
            }

            // Find and set the name text
            Transform nameTransform = buttonObj.transform.Find("NameText");
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = enerling.ingredientName;
                    // Initially hide the name text
                    nameText.gameObject.SetActive(false);
                }
            }

            button.onClick.AddListener(() => OnEnerlingButtonClicked(enerling.ingredientName));
        }

        // Store reference
        enerlingButtons[enerling.ingredientName] = buttonObj;

        // Highlight if this is the currently selected enerling
        if (enerling.ingredientName == selectedEnerlingName)
        {
            HighlightButton(buttonObj, true);
        }
    }

    public void OnEnerlingButtonClicked(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        selectedEnerlingName = enerlingName;

        // Update all button highlights
        foreach (var kvp in enerlingButtons)
        {
            HighlightButton(kvp.Value, kvp.Key == enerlingName);
        }

        // Display enerling info
        DisplayEnerlingInfo(enerlingName);

        // Update select button
        UpdateSelectButton();

        Debug.Log($"Selected enerling: {enerlingName}");
    }

    void HighlightButton(GameObject buttonObj, bool highlight)
    {
        // Try to use EnerlingButtonController first
        EnerlingButtonController buttonController = buttonObj.GetComponent<EnerlingButtonController>();
        if (buttonController != null)
        {
            buttonController.SetHighlight(highlight);
        }
        else
        {
            // Fallback to old method
            // Change button color to #858585 (RGB: 133, 133, 133 -> 0.52, 0.52, 0.52)
            Image image = buttonObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = highlight ? selectedButtonColor : Color.white;

                // Also add/remove outline for better visual feedback
                Outline outline = buttonObj.GetComponent<Outline>();
                if (highlight)
                {
                    if (outline == null)
                    {
                        outline = buttonObj.AddComponent<Outline>();
                    }
                    outline.effectColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                    outline.effectDistance = new Vector2(2, 2);
                }
                else
                {
                    if (outline != null)
                    {
                        Destroy(outline);
                    }
                }
            }

            // Also handle name text activation for fallback
            Transform nameTransform = buttonObj.transform.Find("NameText");
            if (nameTransform != null)
            {
                nameTransform.gameObject.SetActive(highlight);
            }
        }
    }

    void DisplayEnerlingInfo(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        var enerling = ingredientDatabase.GetIngredientInfo(enerlingName);
        if (enerling == null)
        {
            Debug.LogError($"Could not find enerling: {enerlingName}");
            return;
        }

        // Show info panel
        enerlingInfoPanel.SetActive(true);

        // Update enerling name text with the selected enerling's name
        enerlingNameText.text = enerling.ingredientName;

        // Rarity icon
        rarityIconImage.sprite = ingredientDatabase.GetRarityIcon(enerling.rarity);

        // Life info
        currentLifeText.text = enerling.LifeText;
        currentLifeText.color = enerling.LifeTextColor;
        lifeSlider.maxValue = enerling.baseLife;
        lifeSlider.value = enerling.currentLife;

        // Armor info - Convert percentage to actual value
        int armorValue = CalculateArmorValue(enerling);
        armorText.text = $"{armorValue}/{armorValue}"; // Always shows max armor value
        armorSlider.maxValue = armorValue;
        armorSlider.value = armorValue; // Always at max

        // Update info panel frame based on rarity
        if (infoPanelFrame != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(enerling.rarity);
            if (frameSprite != null)
            {
                infoPanelFrame.sprite = frameSprite;
                Debug.Log($"Updated info panel frame to {enerling.rarity} frame");
            }
        }

        // Description
        enerlingDescriptionText.text = enerling.enerlingDescription;

        // Icon
        enerlingIconImage.sprite = enerling.enerlingSprite;

        // Base attack - Show only the number (no "Base Attack:" text)
        if (enerling.skill1 != null)
        {
            baseAttackText.text = enerling.skill1.baseValue.ToString();
        }
        else
        {
            baseAttackText.text = "0";
        }

        // Organs label
        organsLabelText.text = enerling.OrgansLabel;

        // Clear previous organ images
        foreach (Transform child in addedAbilityPanel)
        {
            Destroy(child.gameObject);
        }

        // Combine all organs (beneficial and target)
        List<string> allOrgans = new List<string>();

        // Add beneficial organs
        if (enerling.beneficialOrgans != null)
        {
            foreach (string organ in enerling.beneficialOrgans)
            {
                if (!string.IsNullOrEmpty(organ))
                    allOrgans.Add(organ);
            }
        }

        // Add target organs
        if (enerling.targetOrgans != null)
        {
            foreach (string organ in enerling.targetOrgans)
            {
                if (!string.IsNullOrEmpty(organ))
                    allOrgans.Add(organ);
            }
        }

        // Add organ images to addedAbilityPanel
        foreach (string organ in allOrgans)
        {
            if (organImagePrefab == null)
            {
                Debug.LogError("OrganImagePrefab is not assigned!");
                continue;
            }

            GameObject organImage = Instantiate(organImagePrefab, addedAbilityPanel);
            Image image = organImage.GetComponent<Image>();

            if (image != null)
            {
                // Get the organ sprite from database
                Sprite organSprite = ingredientDatabase.GetOrganSprite(organ);
                if (organSprite != null)
                {
                    image.sprite = organSprite;
                    image.preserveAspect = true;
                }
                else
                {
                    // Fallback to inspector sprites
                    switch (organ.ToLower())
                    {
                        case "heart":
                            image.sprite = heartSprite;
                            break;
                        case "liver":
                            image.sprite = liverSprite;
                            break;
                        case "kidney":
                        case "kidneys":
                            image.sprite = kidneySprite;
                            break;
                        case "pancreas":
                            image.sprite = pancreasSprite;
                            break;
                        case "brain":
                            image.sprite = brainSprite;
                            break;
                    }
                    image.preserveAspect = true;
                }
            }

            // Add tooltip or text if needed
            TextMeshProUGUI organText = organImage.GetComponentInChildren<TextMeshProUGUI>();
            if (organText != null)
            {
                organText.text = organ;
            }
        }

        // Calculate and display added ability text
        string addedAbility = CalculateAddedAbilityText(enerling);
        addedAbilityText.text = addedAbility;

        // Display skills
        DisplaySkills(enerling);
    }

    int CalculateArmorValue(IngredientDatabase.IngredientInfo enerling)
    {
        // Calculate armor value based on armor percentage and base life
        float armorDecimal = enerling.armorPercent / 100f;
        int armorValue = Mathf.RoundToInt(enerling.baseLife * armorDecimal);
        return armorValue;
    }

    string CalculateAddedAbilityText(IngredientDatabase.IngredientInfo enerling)
    {
        int beneficialCount = enerling.beneficialOrgans?.Count ?? 0;
        int targetCount = enerling.targetOrgans?.Count ?? 0;
        int totalOrgans = beneficialCount + targetCount;

        if (totalOrgans == 0)
            return "No additional abilities";

        // Calculate total bonus based on distribution logic
        float bonusPercentage = CalculateTotalBonusPercentage(totalOrgans);

        // Get cooldown based on rarity
        int cooldownTurns = GetOrganCooldown(enerling.rarity);

        if (beneficialCount > 0)
        {
            // Calculate healing amount (percentage of base life)
            int healAmount = Mathf.RoundToInt(enerling.baseLife * (bonusPercentage / 100f));
            return $"Has {beneficialCount} beneficial organ(s): +{healAmount} health every {cooldownTurns} turns";
        }
        else if (targetCount > 0)
        {
            // Calculate damage bonus (percentage of base damage)
            int damageBonus = Mathf.RoundToInt(enerling.baseDamage * (bonusPercentage / 100f));
            return $"Has {targetCount} target organ(s): +{damageBonus} damage every {cooldownTurns} turns";
        }

        return "No additional abilities";
    }

    float CalculateTotalBonusPercentage(int organCount)
    {
        // Distribution logic:
        // 1 organ = 5%
        // 2 organs = 10% (5% each)
        // 3 organs = 15% (5% each)
        // 4 organs = 20% (5% each)
        // 5 organs = 25% (5% each)

        return organCount * 5f; // 5% per organ
    }

    int GetOrganCooldown(IngredientDatabase.Rarity rarity)
    {
        switch (rarity)
        {
            case IngredientDatabase.Rarity.Common: return 5;
            case IngredientDatabase.Rarity.Rare: return 4;
            case IngredientDatabase.Rarity.UltraRare: return 3;
            default: return 5;
        }
    }

    void DisplaySkills(IngredientDatabase.IngredientInfo enerling)
    {
        // Clear previous skill buttons
        foreach (Transform child in skillsUIPanel)
        {
            Destroy(child.gameObject);
        }

        // Create skill buttons - with null checks
        if (skillButtonPrefab != null)
        {
            // Store skill buttons for highlighting
            List<GameObject> skillButtons = new List<GameObject>();

            if (enerling.skill1 != null)
            {
                GameObject skill1Button = CreateSkillButton(enerling.skill1, 1);
                if (skill1Button != null) skillButtons.Add(skill1Button);
            }
            if (enerling.skill2 != null)
            {
                GameObject skill2Button = CreateSkillButton(enerling.skill2, 2);
                if (skill2Button != null) skillButtons.Add(skill2Button);
            }
            if (enerling.skill3 != null)
            {
                GameObject skill3Button = CreateSkillButton(enerling.skill3, 3);
                if (skill3Button != null) skillButtons.Add(skill3Button);
            }
            if (enerling.skill4 != null)
            {
                GameObject skill4Button = CreateSkillButton(enerling.skill4, 4);
                if (skill4Button != null) skillButtons.Add(skill4Button);
            }

            // Highlight first skill button
            if (skillButtons.Count > 0)
            {
                HighlightSkillButton(skillButtons[0], true);
            }
        }
        else
        {
            Debug.LogError("SkillButtonPrefab is not assigned in the inspector!");
        }

        // Set default skill description (first skill)
        if (enerling.skill1 != null)
        {
            skillDescriptionText.text = enerling.skill1.skillDescription;
        }
        else
        {
            skillDescriptionText.text = "No skills available";
        }
    }

    GameObject CreateSkillButton(IngredientDatabase.SkillInfo skill, int skillNumber)
    {
        if (skill == null || skillButtonPrefab == null)
        {
            Debug.LogWarning($"Cannot create skill button for Skill {skillNumber}: skill or prefab is null");
            return null;
        }

        GameObject buttonObj = Instantiate(skillButtonPrefab, skillsUIPanel);

        // Try to get Button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"Skill button prefab has no Button component! Please add a Button component to {skillButtonPrefab.name}");

            // Try to add Button component automatically
            button = buttonObj.AddComponent<Button>();
            if (button != null)
            {
                Debug.Log($"Added Button component to {buttonObj.name}");
            }
            else
            {
                return null; // Cannot continue without a Button component
            }
        }

        // Set button name for debugging
        buttonObj.name = $"SkillButton_{skillNumber}_{skill.skillName}";

        // Find and set the skill name text
        Transform nameTransform = buttonObj.transform.Find("SkillName");
        if (nameTransform != null)
        {
            TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = skill.skillName;
                nameText.raycastTarget = false; // Don't block clicks
            }
            else
            {
                Debug.LogWarning($"SkillName child found but has no TextMeshProUGUI component");
            }
        }
        else
        {
            Debug.LogWarning($"No 'SkillName' child found in skill button prefab. Looking for alternatives...");

            // Try alternative names
            nameTransform = buttonObj.transform.Find("Text");
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = skill.skillName;
                    nameText.raycastTarget = false;
                }
            }
        }

        // Set the skill icon on the button's main Image component
        Image skillIconImage = buttonObj.GetComponent<Image>();
        if (skillIconImage != null)
        {
            if (skill.skillSprite != null)
            {
                skillIconImage.sprite = skill.skillSprite;
                skillIconImage.preserveAspect = true;
            }
            else if (defaultSkillIcon != null)
            {
                // Use default icon if no skill sprite
                skillIconImage.sprite = defaultSkillIcon;
            }
        }

        // Set up button colors for visual feedback
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        // Add click listener
        button.onClick.RemoveAllListeners(); // Clear any existing listeners
        button.onClick.AddListener(() =>
        {
            Debug.Log($"Skill button clicked: {skill.skillName}");
            OnSkillButtonClicked(skill, buttonObj);
        });

        Debug.Log($"Created skill button for {skill.skillName}");
        return buttonObj;
    }

    void HighlightSkillButton(GameObject buttonObj, bool highlight)
    {
        if (buttonObj == null) return;

        Image image = buttonObj.GetComponent<Image>();
        if (image != null)
        {
            if (highlight)
            {
                image.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray for selected skill

                // Add outline
                Outline outline = buttonObj.GetComponent<Outline>();
                if (outline == null) outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(2, 2);
            }
            else
            {
                image.color = Color.white;

                // Remove outline
                Outline outline = buttonObj.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }
        }
    }

    void OnSkillButtonClicked(IngredientDatabase.SkillInfo skill, GameObject buttonObj = null)
    {
        if (skill != null)
        {
            // Update skill description
            skillDescriptionText.text = skill.skillDescription;

            // Highlight the clicked skill button
            if (buttonObj != null)
            {
                // Unhighlight all other skill buttons
                foreach (Transform child in skillsUIPanel)
                {
                    HighlightSkillButton(child.gameObject, false);
                }

                // Highlight clicked button
                HighlightSkillButton(buttonObj, true);
            }
        }
        else
        {
            skillDescriptionText.text = "Skill information not available";
        }
    }

    void UpdateSelectButton()
    {
        if (string.IsNullOrEmpty(selectedEnerlingName))
        {
            selectButton.interactable = false;
            selectButtonText.text = "Select Enerling";
        }
        else
        {
            selectButton.interactable = true;

            // Check if this is already the selected enerling
            if (PersistentDataManager.Instance != null &&
                PersistentDataManager.Instance.GetSelectedEnerlingName() == selectedEnerlingName)
            {
                selectButtonText.text = "Selected";
                selectButton.interactable = false;
            }
            else
            {
                selectButtonText.text = "Select";
            }
        }
    }

    public void OnSelectButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedEnerlingName)) return;

        // Save to persistent data
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.SaveSelectedEnerling(selectedEnerlingName);

            // Also save current life state
            var enerling = ingredientDatabase.GetIngredientInfo(selectedEnerlingName);
            if (enerling != null)
            {
                PersistentDataManager.Instance.SaveEnerlingCurrentLife(selectedEnerlingName, enerling.currentLife);
            }

            Debug.Log($"Enerling {selectedEnerlingName} selected and saved!");
        }

        UpdateSelectButton();

        // NEW: Switch to battlefield
        BattleEnerlingManager battleManager = FindObjectOfType<BattleEnerlingManager>();
        if (battleManager != null)
        {
            battleManager.OnSelectButtonClickedFromSelection();
        }
        else
        {
            Debug.LogError("BattleEnerlingManager not found in scene!");
        }
    }

    void LoadSelectedEnerling()
    {
        if (PersistentDataManager.Instance != null)
        {
            string savedEnerling = PersistentDataManager.Instance.GetSelectedEnerlingName();
            if (!string.IsNullOrEmpty(savedEnerling))
            {
                // Check if this enerling exists and is unlocked
                var enerling = ingredientDatabase.GetIngredientInfo(savedEnerling);
                if (enerling != null && enerling.isUnlocked)
                {
                    selectedEnerlingName = savedEnerling;
                    DisplayEnerlingInfo(savedEnerling);

                    // Highlight the button
                    if (enerlingButtons.ContainsKey(savedEnerling))
                    {
                        HighlightButton(enerlingButtons[savedEnerling], true);
                    }

                    Debug.Log($"Loaded previously selected enerling: {savedEnerling}");
                    return;
                }
            }
        }

        Debug.Log("No saved enerling selection found or enerling is not unlocked");

        // Start coroutine to select first button after all buttons are created
        StartCoroutine(SelectFirstButtonAfterDelay());
    }

    IEnumerator SelectFirstButtonAfterDelay()
    {
        yield return null; // Wait one frame

        // Now all buttons should be created
        if (enerlingButtons.Count > 0)
        {
            // Get the first button (first key in dictionary)
            foreach (var kvp in enerlingButtons)
            {
                string firstEnerlingName = kvp.Key;
                Debug.Log($"Auto-selecting first button (first in first row): {firstEnerlingName}");
                OnEnerlingButtonClicked(firstEnerlingName);
                break; // Only select first one
            }
        }
    }

    IEnumerator SelectFirstEnerlingCoroutine()
    {
        yield return null; // Wait one frame for buttons to be created

        // Check if we have any buttons
        if (enerlingButtons.Count > 0)
        {
            // Find the first button that was created
            // The buttons are created in order, so first key in dictionary is first button
            foreach (var kvp in enerlingButtons)
            {
                string firstEnerlingName = kvp.Key;
                Debug.Log($"Auto-selecting first button in first row: {firstEnerlingName}");
                OnEnerlingButtonClicked(firstEnerlingName);
                break; // Only select the first one
            }
        }
    }

    void ClearCurrentDisplay()
    {
        // Clear all rows
        foreach (GameObject row in currentRows)
        {
            Destroy(row);
        }
        currentRows.Clear();

        // Clear button references
        enerlingButtons.Clear();
    }

    // Method to refresh display (call this when unlocking new enerlings)
    public void RefreshDisplay()
    {
        RefreshEnerlingDisplay();
        UpdateSelectButton();
    }

    // Method to unlock an enerling (for testing or from scanning)
    public void UnlockEnerling(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.UnlockEnerling(enerlingName);
        }
        else
        {
            ingredientDatabase.UnlockIngredient(enerlingName);
        }

        RefreshDisplay();
    }

    // Method to heal all enerlings (for testing)
    public void HealAllEnerlings()
    {
        foreach (var enerling in ingredientDatabase.ingredients)
        {
            if (enerling.isUnlocked)
            {
                enerling.currentLife = enerling.baseLife;
                if (PersistentDataManager.Instance != null)
                {
                    PersistentDataManager.Instance.SaveEnerlingCurrentLife(enerling.ingredientName, enerling.currentLife);
                }
            }
        }

        // Refresh display if we're showing the currently selected enerling
        if (!string.IsNullOrEmpty(selectedEnerlingName))
        {
            DisplayEnerlingInfo(selectedEnerlingName);
        }
    }

    [ContextMenu("Test Auto-Select First")]
    public void TestAutoSelectFirst()
    {
        if (currentFilteredEnerlings.Count > 0)
        {
            string firstEnerlingName = currentFilteredEnerlings[0].ingredientName;
            Debug.Log($"Test: Auto-selecting first enerling: {firstEnerlingName}");
            OnEnerlingButtonClicked(firstEnerlingName);
        }
    }

    [ContextMenu("Debug Skill Button Setup")]
    public void DebugSkillButtonSetup()
    {
        Debug.Log("=== DEBUG: Skill Button Setup ===");
        Debug.Log($"Skill Button Prefab: {skillButtonPrefab}");

        if (skillButtonPrefab != null)
        {
            Debug.Log($"Prefab has Button component: {skillButtonPrefab.GetComponent<Button>() != null}");
            Debug.Log($"Prefab has Image component: {skillButtonPrefab.GetComponent<Image>() != null}");

            // Check for SkillName child
            Transform skillNameTransform = skillButtonPrefab.transform.Find("SkillName");
            if (skillNameTransform != null)
            {
                Debug.Log($"Found SkillName child: {skillNameTransform.name}");
                Debug.Log($"Has TextMeshProUGUI: {skillNameTransform.GetComponent<TextMeshProUGUI>() != null}");
            }
            else
            {
                Debug.LogWarning("No SkillName child found in prefab. Looking for alternatives...");

                // List all children
                foreach (Transform child in skillButtonPrefab.transform)
                {
                    Debug.Log($"  Child: {child.name}");
                    TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        Debug.Log($"    - Has TextMeshProUGUI component");
                    }
                }
            }
        }
        Debug.Log("=== END DEBUG ===");
    }
}