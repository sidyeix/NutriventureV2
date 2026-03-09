using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Linq;

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
    public TextMeshProUGUI baseAttackText;
    public TextMeshProUGUI organsLabelText;
    public Transform addedAbilityPanel;
    public GameObject organImagePrefab;
    public TextMeshProUGUI addedAbilityText;

    [Header("UI References - Armor")]
    public TextMeshProUGUI armorText;
    public Slider armorSlider;

    [Header("UI References - Info Panel Frame")]
    public Image infoPanelFrame;

    [Header("UI References - Skills Panel")]
    public Transform skillsUIPanel;
    public GameObject skillButtonPrefab;
    public TextMeshProUGUI skillDescriptionText;

    [Header("UI References - Selection")]
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;

    [Header("Timeline System")]
    public PlayableDirector timelineDirector;
    public PlayableAsset selectionTimelineAsset;

    [Header("Spawning References")]
    public Transform playerSpawnPoint;
    public Transform opponentSpawnPoint;

    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = new Color(0.52f, 0.52f, 0.52f);
    public Color disabledButtonColor = Color.gray;

    [Header("Organ Sprites Mapping")]
    public Sprite heartSprite;
    public Sprite liverSprite;
    public Sprite kidneySprite;
    public Sprite pancreasSprite;
    public Sprite brainSprite;

    [Header("Skill Button Settings")]
    public Sprite defaultSkillIcon;

    [Header("Health Regen UI")]
    [Tooltip("Sprite to use for slider fill when enerling is regenerating health")]
    public Sprite healingFillSprite;
    [Tooltip("Default sprite for slider fill (normal state)")]
    public Sprite normalFillSprite;

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

    // Timeline and spawning state
    private bool isTimelinePlaying = false;
    private GameObject spawnedPlayerEnerling;
    private GameObject spawnedOpponentEnerling;

    // Regen text oscillation state
    private Coroutine regenTextCoroutine;
    private bool isShowingRegenText = false;
    private float regenTickTimer = 0f;
    private const float REGEN_TICK_INTERVAL = 1f;

    void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    void Update()
    {
        // Tick enerling health regen once per second
        if (PersistentDataManager.Instance != null)
        {
            regenTickTimer += Time.deltaTime;
            if (regenTickTimer >= REGEN_TICK_INTERVAL)
            {
                regenTickTimer = 0f;
                PersistentDataManager.Instance.ProcessAllEnerlingHealthRegen();

                // Update slider value live if selected enerling is regenerating
                if (!string.IsNullOrEmpty(selectedEnerlingName) && ingredientDatabase != null)
                {
                    var enerling = ingredientDatabase.GetIngredientInfo(selectedEnerlingName);
                    if (enerling != null && lifeSlider != null)
                    {
                        lifeSlider.value = enerling.currentLife;

                        // If fully healed, reset UI to normal
                        if (enerling.currentLife >= enerling.baseLife)
                        {
                            StopRegenTextLoop();
                            SetSliderFillSprite(false);
                            if (currentLifeText != null)
                            {
                                currentLifeText.text = enerling.LifeText;
                                currentLifeText.color = enerling.LifeTextColor;
                            }
                        }
                    }
                }
            }
        }
    }

    IEnumerator InitializeAfterDelay()
    {
        yield return null;

        // STOP OTHER PLAYABLE DIRECTORS FIRST
        StopOtherPlayableDirectors();

        // Ensure all damaged enerlings have regen running
        if (PersistentDataManager.Instance != null)
            PersistentDataManager.Instance.EnsureAllDamagedEnerlingsRegenerating();

        InitializeDatabase();
        SetupFilterButtons();
        SetupKingdomButtons();
        DisplayAllUnlockedEnerlings();
        UpdateSelectButton();
        LoadSelectedEnerling();
    }

    // SIMPLE FIX: Stop other PlayableDirectors
    void StopOtherPlayableDirectors()
    {
        Debug.Log("=== STOPPING OTHER PLAYABLE DIRECTORS ===");

        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>(true);
        Debug.Log($"Found {allDirectors.Length} PlayableDirectors in scene");

        foreach (PlayableDirector director in allDirectors)
        {
            // Skip our own director
            if (director == timelineDirector) continue;

            Debug.Log($"Stopping PlayableDirector: {director.name}");
            director.Stop();
            director.time = 0;
            director.Evaluate();
        }
    }

    void InitializeDatabase()
    {
        if (ingredientDatabase == null)
        {
            Debug.LogError("IngredientDatabase not assigned!");
            return;
        }

        if (PersistentDataManager.Instance == null)
        {
            Debug.LogError("PersistentDataManager not found!");
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

        if (PersistentDataManager.Instance.ingredientDatabase == null)
        {
            PersistentDataManager.Instance.ingredientDatabase = ingredientDatabase;
            Debug.Log("Assigned ingredientDatabase to PersistentDataManager");
        }

        Debug.Log($"Database initialized with {ingredientDatabase.ingredients.Count} total ingredients");

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
        UpdateFilterButtonColors();
    }

    void SetupKingdomButtons()
    {
        allKingdomButton.onClick.AddListener(() => SetKingdomFilter(false, IngredientDatabase.KingdomOrigin.NutriKingdom));
        nutriKingdomButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.NutriKingdom));
        alerthiaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Alerthia));
        suragriaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Sugaria));
        preserviaButton.onClick.AddListener(() => SetKingdomFilter(true, IngredientDatabase.KingdomOrigin.Preservia));
        UpdateKingdomButtonColors();
    }

    void SetRarityFilter(bool useFilter, IngredientDatabase.Rarity rarity)
    {
        useRarityFilter = useFilter;
        currentRarityFilter = rarity;
        UpdateFilterButtonColors();
        RefreshEnerlingDisplay();
        AutoSelectFirstAfterFilter();
    }

    void SetKingdomFilter(bool useFilter, IngredientDatabase.KingdomOrigin kingdom)
    {
        useKingdomFilter = useFilter;
        currentKingdomFilter = kingdom;
        UpdateKingdomButtonColors();
        RefreshEnerlingDisplay();
        AutoSelectFirstAfterFilter();
    }

    void AutoSelectFirstAfterFilter()
    {
        bool currentSelectionInFilter = false;
        foreach (var enerling in currentFilteredEnerlings)
        {
            if (enerling.ingredientName == selectedEnerlingName)
            {
                currentSelectionInFilter = true;
                break;
            }
        }

        if (!currentSelectionInFilter && currentFilteredEnerlings.Count > 0)
        {
            string firstEnerlingName = currentFilteredEnerlings[0].ingredientName;
            Debug.Log($"Auto-selecting first enerling after filter: {firstEnerlingName}");
            OnEnerlingButtonClicked(firstEnerlingName);
        }
        else if (currentFilteredEnerlings.Count == 0)
        {
            selectedEnerlingName = "";
            enerlingInfoPanel.SetActive(false);
            UpdateSelectButton();
            Debug.LogWarning("No enerlings in current filter!");
        }
    }

    void UpdateFilterButtonColors()
    {
        allFilterButton.image.color = normalButtonColor;
        commonFilterButton.image.color = normalButtonColor;
        rareFilterButton.image.color = normalButtonColor;
        ultraRareFilterButton.image.color = normalButtonColor;

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
        allKingdomButton.image.color = normalButtonColor;
        nutriKingdomButton.image.color = normalButtonColor;
        alerthiaButton.image.color = normalButtonColor;
        suragriaButton.image.color = normalButtonColor;
        preserviaButton.image.color = normalButtonColor;

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
                case IngredientDatabase.KingdomOrigin.Sugaria:
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
        var unlockedEnerlings = ingredientDatabase.GetUnlockedIngredients();
        currentFilteredEnerlings = unlockedEnerlings;

        Debug.Log($"DisplayAllUnlockedEnerlings: Found {unlockedEnerlings.Count} unlocked enerlings");

        if (unlockedEnerlings.Count == 0)
        {
            Debug.LogWarning("No enerlings unlocked yet!");
            enerlingInfoPanel.SetActive(false);
            UpdateSelectButton();
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
            GameObject rowPrefab = (rowIndex % 2 == 0) ? oddRowPrefab : evenRowPrefab;
            GameObject row = Instantiate(rowPrefab, contentParent);
            currentRows.Add(row);

            int maxButtons = (rowIndex % 2 == 0) ? 4 : 3;

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
        EnerlingButtonController buttonController = buttonObj.GetComponent<EnerlingButtonController>();

        if (buttonController != null)
        {
            buttonController.Initialize(
                enerling.ingredientName,
                enerling.enerlingSprite,
                enerling.rarity,
                ingredientDatabase
            );

            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnEnerlingButtonClicked(enerling.ingredientName));
            }
        }
        else
        {
            Debug.LogWarning("Enerling button prefab has no EnerlingButtonController component! Using fallback...");
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("Enerling button prefab has no Button component!");
                return;
            }

            Image enerlingImage = buttonObj.GetComponent<Image>();
            if (enerlingImage != null && enerling.enerlingSprite != null)
            {
                enerlingImage.sprite = enerling.enerlingSprite;
                enerlingImage.preserveAspect = true;
            }

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

            Transform nameTransform = buttonObj.transform.Find("NameText");
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = enerling.ingredientName;
                    nameText.gameObject.SetActive(false);
                }
            }

            button.onClick.AddListener(() => OnEnerlingButtonClicked(enerling.ingredientName));
        }

        enerlingButtons[enerling.ingredientName] = buttonObj;

        if (enerling.ingredientName == selectedEnerlingName)
        {
            HighlightButton(buttonObj, true);
        }
    }

    public void OnEnerlingButtonClicked(string enerlingName)
    {
        if (string.IsNullOrEmpty(enerlingName)) return;

        selectedEnerlingName = enerlingName;

        foreach (var kvp in enerlingButtons)
        {
            HighlightButton(kvp.Value, kvp.Key == enerlingName);
        }

        DisplayEnerlingInfo(enerlingName);
        UpdateSelectButton();
        Debug.Log($"Selected enerling: {enerlingName}");
    }

    void HighlightButton(GameObject buttonObj, bool highlight)
    {
        EnerlingButtonController buttonController = buttonObj.GetComponent<EnerlingButtonController>();
        if (buttonController != null)
        {
            buttonController.SetHighlight(highlight);
        }
        else
        {
            Image image = buttonObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = highlight ? selectedButtonColor : Color.white;
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

        enerlingInfoPanel.SetActive(true);
        enerlingNameText.text = enerling.ingredientName;
        rarityIconImage.sprite = ingredientDatabase.GetRarityIcon(enerling.rarity);
        lifeSlider.maxValue = enerling.baseLife;
        lifeSlider.value = enerling.currentLife;

        // Check if this enerling is regenerating health
        bool isRegenerating = PersistentDataManager.Instance != null
            && PersistentDataManager.Instance.IsEnerlingRegenerating(enerling.ingredientName);

        Debug.Log($"[RegenUI] DisplayEnerlingInfo: {enerling.ingredientName} life={enerling.currentLife}/{enerling.baseLife} regen={isRegenerating}");

        if (isRegenerating)
        {
            SetSliderFillSprite(true);
            StartRegenTextLoop(enerling);
        }
        else
        {
            SetSliderFillSprite(false);
            StopRegenTextLoop();
            currentLifeText.text = enerling.LifeText;
            currentLifeText.color = enerling.LifeTextColor;
        }

        int armorValue = CalculateArmorValue(enerling);
        armorText.text = $"{armorValue}/{armorValue}";
        armorSlider.maxValue = armorValue;
        armorSlider.value = armorValue;

        if (infoPanelFrame != null)
        {
            Sprite frameSprite = ingredientDatabase.GetFrameSprite(enerling.rarity);
            if (frameSprite != null)
            {
                infoPanelFrame.sprite = frameSprite;
            }
        }

        enerlingDescriptionText.text = enerling.enerlingDescription;
        enerlingIconImage.sprite = enerling.enerlingSprite;

        if (enerling.skill1 != null)
        {
            baseAttackText.text = enerling.skill1.baseValue.ToString();
        }
        else
        {
            baseAttackText.text = "0";
        }

        organsLabelText.text = enerling.OrgansLabel;

        foreach (Transform child in addedAbilityPanel)
        {
            Destroy(child.gameObject);
        }

        List<string> allOrgans = new List<string>();

        if (enerling.beneficialOrgans != null)
        {
            foreach (string organ in enerling.beneficialOrgans)
            {
                if (!string.IsNullOrEmpty(organ))
                    allOrgans.Add(organ);
            }
        }

        if (enerling.targetOrgans != null)
        {
            foreach (string organ in enerling.targetOrgans)
            {
                if (!string.IsNullOrEmpty(organ))
                    allOrgans.Add(organ);
            }
        }

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
                Sprite organSprite = ingredientDatabase.GetOrganSprite(organ);
                if (organSprite != null)
                {
                    image.sprite = organSprite;
                    image.preserveAspect = true;
                }
                else
                {
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

            TextMeshProUGUI organText = organImage.GetComponentInChildren<TextMeshProUGUI>();
            if (organText != null)
            {
                organText.text = organ;
            }
        }

        string addedAbility = CalculateAddedAbilityText(enerling);
        addedAbilityText.text = addedAbility;
        DisplaySkills(enerling);
    }

    int CalculateArmorValue(IngredientDatabase.IngredientInfo enerling)
    {
        float armorDecimal = enerling.armorPercent / 100f;
        return Mathf.RoundToInt(enerling.baseLife * armorDecimal);
    }

    string CalculateAddedAbilityText(IngredientDatabase.IngredientInfo enerling)
    {
        int beneficialCount = enerling.beneficialOrgans?.Count ?? 0;
        int targetCount = enerling.targetOrgans?.Count ?? 0;
        int totalOrgans = beneficialCount + targetCount;

        if (totalOrgans == 0)
            return "No additional abilities";

        float bonusPercentage = CalculateTotalBonusPercentage(totalOrgans);
        int cooldownTurns = GetOrganCooldown(enerling.rarity);

        if (beneficialCount > 0)
        {
            int healAmount = Mathf.RoundToInt(enerling.baseLife * (bonusPercentage / 100f));
            return $"Has {beneficialCount} beneficial organ(s): +{healAmount} health every {cooldownTurns} turns";
        }
        else if (targetCount > 0)
        {
            int damageBonus = Mathf.RoundToInt(enerling.baseDamage * (bonusPercentage / 100f));
            return $"Has {targetCount} target organ(s): +{damageBonus} damage every {cooldownTurns} turns";
        }

        return "No additional abilities";
    }

    float CalculateTotalBonusPercentage(int organCount)
    {
        return organCount * 5f;
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
        foreach (Transform child in skillsUIPanel)
        {
            Destroy(child.gameObject);
        }

        if (skillButtonPrefab != null)
        {
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

            if (skillButtons.Count > 0)
            {
                HighlightSkillButton(skillButtons[0], true);
            }
        }
        else
        {
            Debug.LogError("SkillButtonPrefab is not assigned in the inspector!");
        }

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
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<Button>();
        }

        buttonObj.name = $"SkillButton_{skillNumber}_{skill.skillName}";

        Transform nameTransform = buttonObj.transform.Find("SkillName");
        if (nameTransform != null)
        {
            TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = skill.skillName;
                nameText.raycastTarget = false;
            }
        }
        else
        {
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
                skillIconImage.sprite = defaultSkillIcon;
            }
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        button.onClick.RemoveAllListeners();
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
                image.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                Outline outline = buttonObj.GetComponent<Outline>();
                if (outline == null) outline = buttonObj.AddComponent<Outline>();
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(2, 2);
            }
            else
            {
                image.color = Color.white;
                Outline outline = buttonObj.GetComponent<Outline>();
                if (outline != null) Destroy(outline);
            }
        }
    }

    void OnSkillButtonClicked(IngredientDatabase.SkillInfo skill, GameObject buttonObj = null)
    {
        if (skill != null)
        {
            skillDescriptionText.text = skill.skillDescription;

            if (buttonObj != null)
            {
                foreach (Transform child in skillsUIPanel)
                {
                    HighlightSkillButton(child.gameObject, false);
                }
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

    // ==================== UPDATED: MAIN SELECTION FLOW ====================

    public void OnSelectButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedEnerlingName)) return;
        if (isTimelinePlaying) return;

        // Save to persistent data
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.SaveSelectedEnerling(selectedEnerlingName);

            var enerling = ingredientDatabase.GetIngredientInfo(selectedEnerlingName);
            if (enerling != null)
            {
                PersistentDataManager.Instance.SaveEnerlingCurrentLife(selectedEnerlingName, enerling.currentLife);
            }

            Debug.Log($"Enerling {selectedEnerlingName} selected and saved!");
        }

        UpdateSelectButton();

        // Disable select button to prevent multiple clicks
        selectButton.interactable = false;

        // === UPDATED: Spawn enerlings BEFORE timeline ===
        SpawnBothEnerlings();

        // Hide the selection UI
        HideSelectionUI();

        // Play timeline BEFORE starting battle
        StartCoroutine(PlayTimelineAndStartBattle());
    }

    // ==================== UPDATED: Timeline playback ====================
    IEnumerator PlayTimelineAndStartBattle()
    {
        isTimelinePlaying = true;
        Debug.Log("=== TIMELINE START ===");

        if (timelineDirector != null && selectionTimelineAsset != null)
        {
            Debug.Log($"Playing timeline: {selectionTimelineAsset.name}");
            timelineDirector.playableAsset = selectionTimelineAsset;
            timelineDirector.time = 0;
            timelineDirector.Play();

            while (timelineDirector.state == PlayState.Playing)
            {
                yield return null;
            }
            Debug.Log("=== TIMELINE COMPLETE ===");
        }
        else
        {
            Debug.LogWarning("No timeline director found. Proceeding directly.");
            yield return new WaitForSeconds(1f);
        }

        // === AFTER TIMELINE: Start actual battle with existing enerlings ===
        Debug.Log("Starting actual battle with spawned enerlings...");

        // Pass the spawned enerlings to BattleEnerlingManager
        BattleEnerlingManager battleManager = FindObjectOfType<BattleEnerlingManager>();
        if (battleManager != null)
        {
            battleManager.StartBattleWithExistingEnerlings(
                selectedEnerlingName,
                spawnedPlayerEnerling,
                spawnedOpponentEnerling
            );
        }
        else
        {
            Debug.LogError("BattleEnerlingManager not found! Using fallback...");
            // Fallback: find it
            battleManager = FindObjectOfType<BattleEnerlingManager>();
            if (battleManager != null)
            {
                battleManager.StartBattleWithExistingEnerlings(
                    selectedEnerlingName,
                    spawnedPlayerEnerling,
                    spawnedOpponentEnerling
                );
            }
            else
            {
                Debug.LogError("BattleEnerlingManager not found at all!");
                // Last resort: use old method (will cause glitch)
                battleManager = FindObjectOfType<BattleEnerlingManager>();
                if (battleManager != null)
                {
                    battleManager.OnSelectButtonClickedFromSelection();
                }
            }
        }

        // Clear our references (battle manager now owns them)
        spawnedPlayerEnerling = null;
        spawnedOpponentEnerling = null;

        isTimelinePlaying = false;
    }

    // ==================== METHODS FOR TIMELINE & SPAWNING ====================

    private void HideSelectionUI()
    {
        // Get the root canvas that contains this manager
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
            Debug.Log("Selection UI hidden via parent canvas");
            return;
        }

        // Fallback to BattlePlayManager reference
        BattlePlayManager battleManager = FindObjectOfType<BattlePlayManager>();
        if (battleManager != null && battleManager.enerlingPickingCanvas != null)
        {
            battleManager.enerlingPickingCanvas.SetActive(false);
            Debug.Log("EnerlingPickingCanvas hidden via BattlePlayManager");
            return;
        }

        Debug.LogWarning("Could not find selection UI to hide!");
    }

    private void SpawnBothEnerlings()
    {
        Debug.Log("Spawning both enerlings for timeline...");

        // 1. Spawn player enerling
        SpawnPlayerEnerling();

        // 2. Spawn opponent enerling
        SpawnOpponentEnerling();
    }

    private void SpawnPlayerEnerling()
    {
        if (string.IsNullOrEmpty(selectedEnerlingName)) return;

        var playerEnerlingData = ingredientDatabase.GetIngredientInfo(selectedEnerlingName);
        if (playerEnerlingData == null || playerEnerlingData.modelPrefab == null)
        {
            Debug.LogError($"Cannot spawn player enerling: {selectedEnerlingName} data or prefab is null");
            return;
        }

        if (playerSpawnPoint == null)
        {
            Debug.LogError("Player spawn point not assigned!");
            return;
        }

        // Clean up existing player enerling
        if (spawnedPlayerEnerling != null)
        {
            Destroy(spawnedPlayerEnerling);
        }

        // Spawn player enerling
        spawnedPlayerEnerling = Instantiate(playerEnerlingData.modelPrefab, playerSpawnPoint);
        spawnedPlayerEnerling.transform.localPosition = Vector3.zero;
        spawnedPlayerEnerling.transform.localRotation = Quaternion.identity;
        spawnedPlayerEnerling.transform.localScale = Vector3.one;

        Debug.Log($"Spawned player enerling: {selectedEnerlingName} at {playerSpawnPoint.name}");
    }

    private void SpawnOpponentEnerling()
    {
        // Get opponent enerling name from PersistentDataManager
        string opponentName = "";
        if (PersistentDataManager.Instance != null)
        {
            opponentName = PersistentDataManager.Instance.GetOpponentEnerlingName();
        }

        if (string.IsNullOrEmpty(opponentName))
        {
            opponentName = GetRandomOpponentName();
            Debug.LogWarning($"No opponent found, using random: {opponentName}");
        }

        var opponentEnerlingData = ingredientDatabase.GetIngredientInfo(opponentName);
        if (opponentEnerlingData == null || opponentEnerlingData.modelPrefab == null)
        {
            Debug.LogError($"Cannot spawn opponent enerling: {opponentName} data or prefab is null");
            return;
        }

        if (opponentSpawnPoint == null)
        {
            Debug.LogError("Opponent spawn point not assigned!");
            return;
        }

        // Clean up existing opponent enerling
        if (spawnedOpponentEnerling != null)
        {
            Destroy(spawnedOpponentEnerling);
        }

        // Spawn opponent enerling
        spawnedOpponentEnerling = Instantiate(opponentEnerlingData.modelPrefab, opponentSpawnPoint);
        spawnedOpponentEnerling.transform.localPosition = Vector3.zero;
        spawnedOpponentEnerling.transform.localRotation = Quaternion.identity;
        spawnedOpponentEnerling.transform.localScale = Vector3.one;

        Debug.Log($"Spawned opponent enerling: {opponentName} at {opponentSpawnPoint.name}");
    }

    private string GetRandomOpponentName()
    {
        if (ingredientDatabase == null) return "DefaultEnerling";

        List<IngredientDatabase.IngredientInfo> possibleOpponents = new List<IngredientDatabase.IngredientInfo>();

        foreach (var enerling in ingredientDatabase.GetUnlockedIngredients())
        {
            if (enerling.ingredientName != selectedEnerlingName)
            {
                possibleOpponents.Add(enerling);
            }
        }

        if (possibleOpponents.Count == 0)
        {
            foreach (var enerling in ingredientDatabase.ingredients)
            {
                if (enerling.ingredientName != selectedEnerlingName)
                {
                    possibleOpponents.Add(enerling);
                }
            }
        }

        if (possibleOpponents.Count > 0)
        {
            return possibleOpponents[Random.Range(0, possibleOpponents.Count)].ingredientName;
        }

        return "DefaultEnerling";
    }

    void LoadSelectedEnerling()
    {
        if (PersistentDataManager.Instance != null)
        {
            string savedEnerling = PersistentDataManager.Instance.GetSelectedEnerlingName();
            if (!string.IsNullOrEmpty(savedEnerling))
            {
                var enerling = ingredientDatabase.GetIngredientInfo(savedEnerling);
                if (enerling != null && enerling.isUnlocked)
                {
                    selectedEnerlingName = savedEnerling;
                    DisplayEnerlingInfo(savedEnerling);

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
        StartCoroutine(SelectFirstButtonAfterDelay());
    }

    IEnumerator SelectFirstButtonAfterDelay()
    {
        yield return null;

        if (enerlingButtons.Count > 0)
        {
            foreach (var kvp in enerlingButtons)
            {
                string firstEnerlingName = kvp.Key;
                Debug.Log($"Auto-selecting first button (first in first row): {firstEnerlingName}");
                OnEnerlingButtonClicked(firstEnerlingName);
                break;
            }
        }
    }

    void ClearCurrentDisplay()
    {
        foreach (GameObject row in currentRows)
        {
            Destroy(row);
        }
        currentRows.Clear();
        enerlingButtons.Clear();
    }

    public void RefreshDisplay()
    {
        RefreshEnerlingDisplay();
        UpdateSelectButton();
    }

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

        if (!string.IsNullOrEmpty(selectedEnerlingName))
        {
            DisplayEnerlingInfo(selectedEnerlingName);
        }
    }

    // ==================== HEALTH REGEN UI ====================

    void SetSliderFillSprite(bool healing)
    {
        if (lifeSlider == null) return;

        if (lifeSlider.fillRect == null)
        {
            Debug.LogWarning("[RegenUI] lifeSlider.fillRect is null — assign Fill Rect on the Slider component.");
            return;
        }

        Image fillImage = lifeSlider.fillRect.GetComponent<Image>();
        if (fillImage == null)
        {
            Debug.LogWarning("[RegenUI] No Image component on lifeSlider.fillRect.");
            return;
        }

        if (healing)
        {
            if (healingFillSprite == null)
            {
                Debug.LogWarning("[RegenUI] healingFillSprite is not assigned in Inspector!");
                return;
            }
            fillImage.sprite = healingFillSprite;
            fillImage.type = Image.Type.Sliced;
        }
        else
        {
            if (normalFillSprite == null)
            {
                Debug.LogWarning("[RegenUI] normalFillSprite is not assigned in Inspector!");
                return;
            }
            fillImage.sprite = normalFillSprite;
            fillImage.type = Image.Type.Sliced;
        }
    }

    void StartRegenTextLoop(IngredientDatabase.IngredientInfo enerling)
    {
        StopRegenTextLoop();
        regenTextCoroutine = StartCoroutine(RegenTextLoopCoroutine(enerling));
    }

    void StopRegenTextLoop()
    {
        if (regenTextCoroutine != null)
        {
            StopCoroutine(regenTextCoroutine);
            regenTextCoroutine = null;
        }
        isShowingRegenText = false;
    }

    IEnumerator RegenTextLoopCoroutine(IngredientDatabase.IngredientInfo enerling)
    {
        while (enerling != null && enerling.currentLife < enerling.baseLife)
        {
            // Show "Regenerating..." for 3 seconds
            if (currentLifeText != null)
            {
                currentLifeText.text = "Regenerating...";
                currentLifeText.color = new Color(0.3f, 1f, 0.3f); // Green
            }
            isShowingRegenText = true;
            yield return new WaitForSeconds(3f);

            // Show actual life text for 5 seconds
            if (currentLifeText != null)
            {
                currentLifeText.text = enerling.LifeText;
                currentLifeText.color = enerling.LifeTextColor;
            }
            isShowingRegenText = false;
            yield return new WaitForSeconds(5f);
        }

        // Regen complete — show final life
        if (currentLifeText != null)
        {
            currentLifeText.text = enerling.LifeText;
            currentLifeText.color = enerling.LifeTextColor;
        }
        SetSliderFillSprite(false);
        regenTextCoroutine = null;
    }
}