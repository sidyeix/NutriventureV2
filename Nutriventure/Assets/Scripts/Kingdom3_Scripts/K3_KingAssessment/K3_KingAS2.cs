using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class K3_KingAS2 : MonoBehaviour
{
    [Header("Preservation System")]
    public PreservationUISettings preservationUISettings;

    public PreservativeType CurrentPreservativeType { get; private set; }
    public float CurrentSliderValue { get; private set; }

    /// <summary>
    /// Fired when the player lands in the correct zone with the correct preservative.
    /// </summary>
    public event System.Action OnCorrectPreservativeApplied;

    [Header("Arrow Movement Settings")]
    [SerializeField] private float arrowMinSpeed = 40f;
    [SerializeField] private float arrowMaxSpeed = 100f;

    [Header("Random Range Zone Position")]
    [SerializeField] private float randomRangeMin = 10f;
    [SerializeField] private float randomRangeMax = 50f;

    [Header("Preservative Icons")]
    [SerializeField] private Sprite ascorbicIcon;
    [SerializeField] private Sprite potassiumIcon;
    [SerializeField] private Sprite sodiumIcon;
    [SerializeField] private Sprite retryIcon;

    [Header("Button Scale Animation")]
    [SerializeField] private float buttonScaleDuration = 0.2f;
    [SerializeField] private float buttonScaleFactor = 1.2f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip sliderFillSound;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("Scoring System")]
    [SerializeField] private PreserviaScoringSystem scoringSystem;

    [Header("Food Database")]
    [SerializeField] private K3_FoodDatabase foodDatabase;

    [Header("Vibration Settings")]
    [SerializeField] private bool enableHapticFeedback = true;
    [SerializeField] private bool enableShakeAnimations = true;

    [Header("Collection System Reference")]
    [SerializeField] private K3_CollectPreservatives collectionSystem;
    [SerializeField] private PreservativesInformationManager infoManager;

    [Header("Warning Panel (inside KA_Panel)")]
    [Tooltip("The warning panel GameObject that pops up on wrong actions.")]
    [SerializeField] private GameObject warningPanel;
    [Tooltip("The TMP text inside the warning panel.")]
    [SerializeField] private TMP_Text warningText;
    [Tooltip("How long the warning panel stays visible before auto-hiding.")]
    [SerializeField] private float warningDisplayDuration = 2f;

    [Header("Health Penalty")]
    [Tooltip("Player health script — will auto-find if not assigned.")]
    [SerializeField] private PreserviaPlayerStat playerHealth;
    [Tooltip("How much health to deduct per wrong action.")]
    [SerializeField] private int penaltyDamage = 1;

    // UI references
    private Slider ascorbicAcidSlider;
    private Slider potassiumSorbateSlider;
    private Slider sodiumBenzoateSlider;
    private Button ascorbicAcidButton;
    private Button potassiumSorbateButton;
    private Button sodiumBenzoateButton;
    private TMP_Text ascorbicAcidValueText;
    private TMP_Text potassiumSorbateValueText;
    private TMP_Text sodiumBenzoateValueText;
    private TMP_Text preservationStatusText;
    private Image ascorbicAcidFillImage;
    private Image potassiumSorbateFillImage;
    private Image sodiumBenzoateFillImage;
    private Button confirmButton;

    // Arrow & Range Zone references
    private RectTransform ascorbicArrow;
    private RectTransform potassiumArrow;
    private RectTransform sodiumArrow;
    private Image ascorbicRangeZone;
    private Image potassiumRangeZone;
    private Image sodiumRangeZone;

    // Per-slider independent arrow state
    private bool isArrowMoving = false;
    private Dictionary<PreservativeType, float> perSliderValue = new Dictionary<PreservativeType, float>();
    private Dictionary<PreservativeType, bool> perSliderIncreasing = new Dictionary<PreservativeType, bool>();
    private Dictionary<PreservativeType, float> perSliderSpeed = new Dictionary<PreservativeType, float>();
    private Dictionary<PreservativeType, float> perSliderRangeMin = new Dictionary<PreservativeType, float>();
    private Dictionary<PreservativeType, float> perSliderRangeMax = new Dictionary<PreservativeType, float>();
    private Dictionary<PreservativeType, float> perSliderZoneHeight = new Dictionary<PreservativeType, float>();

    // Preservation state
    private bool isPreserving = false;
    private bool preservationComplete = false;
    private int currentFoodIndex = -1;

    // Collection tracking
    private bool hasCollectedAscorbicAcid = false;
    private bool hasCollectedPotassiumSorbate = false;
    private bool hasCollectedSodiumBenzoate = false;

    // Food state tracking
    private Dictionary<int, Dictionary<PreservativeType, bool>> foodButtonRetryModes = new Dictionary<int, Dictionary<PreservativeType, bool>>();
    private Dictionary<int, Dictionary<PreservativeType, float>> foodSliderValues = new Dictionary<int, Dictionary<PreservativeType, float>>();
    private Dictionary<int, bool> foodCompleted = new Dictionary<int, bool>();
    private Dictionary<int, List<PreservativeType>> foodPreservativesUsed = new Dictionary<int, List<PreservativeType>>();
    private Dictionary<int, Dictionary<PreservativeType, float>> foodPreservationValues = new Dictionary<int, Dictionary<PreservativeType, float>>();

    // Button scale tracking
    private Dictionary<PreservativeType, RectTransform> buttonTransforms = new Dictionary<PreservativeType, RectTransform>();

    // Warning panel coroutine
    private Coroutine warningCoroutine;
    private Dictionary<PreservativeType, Vector3> originalButtonScales = new Dictionary<PreservativeType, Vector3>();
    private Coroutine currentScaleCoroutine = null;

    [System.Serializable]
    public class PreservationUISettings
    {
        [Header("Sliders")]
        public Slider ascorbicAcidSlider;
        public Slider potassiumSorbateSlider;
        public Slider sodiumBenzoateSlider;

        [Header("Buttons")]
        public Button ascorbicAcidButton;
        public Button potassiumSorbateButton;
        public Button sodiumBenzoateButton;

        [Header("Value Displays")]
        public TMP_Text ascorbicAcidValueText;
        public TMP_Text potassiumSorbateValueText;
        public TMP_Text sodiumBenzoateValueText;

        [Header("Status Display")]
        public TMP_Text preservationStatusText;

        [Header("Slider Fill Images")]
        public Image ascorbicAcidFillImage;
        public Image potassiumSorbateFillImage;
        public Image sodiumBenzoateFillImage;

        [Header("Button Icon Images")]
        public Image ascorbicBTNimg;
        public Image potassiumBTNimg;
        public Image sodiumBTNimg;

        [Header("Moving Arrow Indicators (RectTransform with Image)")]
        [Tooltip("An Image/arrow that moves up and down along each slider")]
        public RectTransform ascorbicArrow;
        public RectTransform potassiumArrow;
        public RectTransform sodiumArrow;

        [Header("Target Range Zone Images")]
        [Tooltip("Image overlaid on slider to show the colored target range")]
        public Image ascorbicRangeZone;
        public Image potassiumRangeZone;
        public Image sodiumRangeZone;
    }

    private void Start()
    {
        InitializeUIReferences();
        SetupPreservativeButtons();
        InitializeFoodTracking();

        if (scoringSystem == null)
        {
            scoringSystem = FindObjectOfType<PreserviaScoringSystem>();
        }

        InitializeCollectionReferences();

        if (AudioHandler.Instance == null)
        {
            Debug.LogWarning("AudioHandler.Instance not found! Make sure AudioHandler is in the scene.");
        }

        HideAllArrowsAndZones();

        // Auto-find player health if not assigned
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PreserviaPlayerStat>();

        // Ensure warning panel starts hidden
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    private void InitializeCollectionReferences()
    {
        if (collectionSystem == null)
        {
            collectionSystem = FindObjectOfType<K3_CollectPreservatives>();
        }

        if (infoManager == null)
        {
            infoManager = FindObjectOfType<PreservativesInformationManager>();
        }
    }

    private void InitializeUIReferences()
    {
        if (preservationUISettings != null)
        {
            ascorbicAcidSlider = preservationUISettings.ascorbicAcidSlider;
            potassiumSorbateSlider = preservationUISettings.potassiumSorbateSlider;
            sodiumBenzoateSlider = preservationUISettings.sodiumBenzoateSlider;

            ascorbicAcidButton = preservationUISettings.ascorbicAcidButton;
            potassiumSorbateButton = preservationUISettings.potassiumSorbateButton;
            sodiumBenzoateButton = preservationUISettings.sodiumBenzoateButton;

            ascorbicAcidValueText = preservationUISettings.ascorbicAcidValueText;
            potassiumSorbateValueText = preservationUISettings.potassiumSorbateValueText;
            sodiumBenzoateValueText = preservationUISettings.sodiumBenzoateValueText;

            preservationStatusText = preservationUISettings.preservationStatusText;

            ascorbicAcidFillImage = preservationUISettings.ascorbicAcidFillImage;
            potassiumSorbateFillImage = preservationUISettings.potassiumSorbateFillImage;
            sodiumBenzoateFillImage = preservationUISettings.sodiumBenzoateFillImage;

            // Arrow and Range Zone references
            ascorbicArrow = preservationUISettings.ascorbicArrow;
            potassiumArrow = preservationUISettings.potassiumArrow;
            sodiumArrow = preservationUISettings.sodiumArrow;
            ascorbicRangeZone = preservationUISettings.ascorbicRangeZone;
            potassiumRangeZone = preservationUISettings.potassiumRangeZone;
            sodiumRangeZone = preservationUISettings.sodiumRangeZone;
        }

        InitializeAllSliders();
    }

    private void InitializeFoodTracking()
    {
        for (int i = 0; i < 8; i++)
        {
            foodCompleted[i] = false;
            foodPreservativesUsed[i] = new List<PreservativeType>();
            foodPreservationValues[i] = new Dictionary<PreservativeType, float>();

            foodButtonRetryModes[i] = new Dictionary<PreservativeType, bool>
            {
                { PreservativeType.AscorbicAcid, false },
                { PreservativeType.PotassiumSorbate, false },
                { PreservativeType.SodiumBenzoate, false }
            };

            foodSliderValues[i] = new Dictionary<PreservativeType, float>
            {
                { PreservativeType.AscorbicAcid, 0f },
                { PreservativeType.PotassiumSorbate, 0f },
                { PreservativeType.SodiumBenzoate, 0f }
            };
        }
    }

    private void InitializeAllSliders()
    {
        ConfigureSlider(ascorbicAcidSlider, 0f, 100f, false);
        ConfigureSlider(potassiumSorbateSlider, 0f, 100f, false);
        ConfigureSlider(sodiumBenzoateSlider, 0f, 100f, false);

        SetAllSlidersInteractable(false);
    }

    private void ConfigureSlider(Slider slider, float minValue, float maxValue, bool interactable)
    {
        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = 0f;
            slider.interactable = interactable;
        }
    }

    private void SetupPreservativeButtons()
    {
        if (ascorbicAcidButton != null)
        {
            buttonTransforms[PreservativeType.AscorbicAcid] = ascorbicAcidButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.AscorbicAcid] = buttonTransforms[PreservativeType.AscorbicAcid].localScale;
            ascorbicAcidButton.interactable = false;
            ascorbicAcidButton.onClick.AddListener(() => OnPreservativeButtonClicked(PreservativeType.AscorbicAcid));
        }

        if (potassiumSorbateButton != null)
        {
            buttonTransforms[PreservativeType.PotassiumSorbate] = potassiumSorbateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.PotassiumSorbate] = buttonTransforms[PreservativeType.PotassiumSorbate].localScale;
            potassiumSorbateButton.interactable = false;
            potassiumSorbateButton.onClick.AddListener(() => OnPreservativeButtonClicked(PreservativeType.PotassiumSorbate));
        }

        if (sodiumBenzoateButton != null)
        {
            buttonTransforms[PreservativeType.SodiumBenzoate] = sodiumBenzoateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.SodiumBenzoate] = buttonTransforms[PreservativeType.SodiumBenzoate].localScale;
            sodiumBenzoateButton.interactable = false;
            sodiumBenzoateButton.onClick.AddListener(() => OnPreservativeButtonClicked(PreservativeType.SodiumBenzoate));
        }
    }

    public void SetConfirmButton(Button confirmBtn)
    {
        confirmButton = confirmBtn;
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }

    // NEW METHOD: Update collection status
    public void UpdateCollectionStatus()
    {
        if (infoManager != null)
        {
            hasCollectedAscorbicAcid = infoManager.IsPreservativeCollected("0");
            hasCollectedPotassiumSorbate = infoManager.IsPreservativeCollected("1");
            hasCollectedSodiumBenzoate = infoManager.IsPreservativeCollected("2");
        }
        else if (collectionSystem != null)
        {
            hasCollectedAscorbicAcid = collectionSystem.HasCollectedPreservative("0");
            hasCollectedPotassiumSorbate = collectionSystem.HasCollectedPreservative("1");
            hasCollectedSodiumBenzoate = collectionSystem.HasCollectedPreservative("2");
        }

        // Update button interactability based on collection status
        UpdateButtonInteractability();
    }

    // NEW METHOD: Update button interactability based on collected preservatives
    private void UpdateButtonInteractability()
    {
        if (currentFoodIndex == -1) return;

        bool isCompleted = foodCompleted[currentFoodIndex];

        // Only enable buttons if preservative is collected AND food is not completed
        if (ascorbicAcidButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[currentFoodIndex].Contains(PreservativeType.AscorbicAcid);
            ascorbicAcidButton.interactable = hasCollectedAscorbicAcid && !isCompleted && !alreadyUsed;
        }

        if (potassiumSorbateButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[currentFoodIndex].Contains(PreservativeType.PotassiumSorbate);
            potassiumSorbateButton.interactable = hasCollectedPotassiumSorbate && !isCompleted && !alreadyUsed;
        }

        if (sodiumBenzoateButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[currentFoodIndex].Contains(PreservativeType.SodiumBenzoate);
            sodiumBenzoateButton.interactable = hasCollectedSodiumBenzoate && !isCompleted && !alreadyUsed;
        }
    }

    public void SetupForFood(int foodIndex, bool isCompleted, List<PreservativeType> usedPreservatives, Dictionary<PreservativeType, float> preservationValues)
    {
        currentFoodIndex = foodIndex;

        // Store used preservatives and values
        foreach (var type in usedPreservatives)
        {
            if (!foodPreservativesUsed[foodIndex].Contains(type))
            {
                foodPreservativesUsed[foodIndex].Add(type);
            }
        }

        foreach (var kvp in preservationValues)
        {
            foodPreservationValues[foodIndex][kvp.Key] = kvp.Value;
        }

        foodCompleted[foodIndex] = isCompleted;

        // Update collection status
        UpdateCollectionStatus();

        // Setup UI
        ResetPreservationState();
        RestoreFoodState(foodIndex);
        SetupPreservativeButtonsForFood(foodIndex);
        UpdateStatusText();

        // Auto-start the arrow moving when food is inspected (if not already completed)
        if (!isCompleted)
        {
            AutoStartArrow(foodIndex);
        }
    }

    /// <summary>
    /// Automatically starts the arrows moving on ALL collected preservative sliders.
    /// Each slider gets a random speed and random range zone position.
    /// </summary>
    private void AutoStartArrow(int foodIndex)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase != null ? foodDatabase.GetFoodProfile(foodIndex) : null;
        if (profile == null) return;

        // Randomize per-slider speed and range zone positions
        RandomizePerSliderState();

        // Make all slider fills transparent
        MakeAllFillsTransparent();

        // Hide all value texts
        HideAllValueTexts();

        // Show range zones for all collected preservatives
        ShowAllRangeZonesForFood(foodIndex, profile);

        isPreserving = true;
        isArrowMoving = true;

        if (confirmButton != null) confirmButton.interactable = false;

        preservationStatusText.text = "Arrows are moving! Click the correct preservative button when its arrow is in the colored zone!";
        preservationStatusText.color = Color.yellow;
    }

    private void RandomizePerSliderState()
    {
        PreservativeType[] types = { PreservativeType.AscorbicAcid, PreservativeType.PotassiumSorbate, PreservativeType.SodiumBenzoate };
        foreach (var type in types)
        {
            perSliderValue[type] = Random.Range(0f, 100f);
            perSliderIncreasing[type] = Random.value > 0.5f;
            perSliderSpeed[type] = Random.Range(arrowMinSpeed, arrowMaxSpeed);

            // Random range zone center position between randomRangeMin and randomRangeMax
            perSliderRangeMin[type] = Random.Range(randomRangeMin, randomRangeMax);

            // Random range zone height: 10-50 in slider value space (0.1-0.5 normalized)
            perSliderZoneHeight[type] = Random.Range(10f, 50f);
        }
    }

    private void MakeAllFillsTransparent()
    {
        Color transparent = new Color(0f, 0f, 0f, 0f);
        if (ascorbicAcidFillImage != null) ascorbicAcidFillImage.color = transparent;
        if (potassiumSorbateFillImage != null) potassiumSorbateFillImage.color = transparent;
        if (sodiumBenzoateFillImage != null) sodiumBenzoateFillImage.color = transparent;
    }

    private void HideAllValueTexts()
    {
        if (ascorbicAcidValueText != null) ascorbicAcidValueText.gameObject.SetActive(false);
        if (potassiumSorbateValueText != null) potassiumSorbateValueText.gameObject.SetActive(false);
        if (sodiumBenzoateValueText != null) sodiumBenzoateValueText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows range zones and arrows on ALL sliders for this food (for collected preservatives).
    /// </summary>
    private void ShowAllRangeZonesForFood(int foodIndex, K3_FoodDatabase.FoodProfile profile)
    {
        HideAllArrowsAndZones();

        // Show zone + arrow for each collected preservative that hasn't been used yet
        ShowRangeZoneIfAvailable(PreservativeType.AscorbicAcid, foodIndex, profile, hasCollectedAscorbicAcid);
        ShowRangeZoneIfAvailable(PreservativeType.PotassiumSorbate, foodIndex, profile, hasCollectedPotassiumSorbate);
        ShowRangeZoneIfAvailable(PreservativeType.SodiumBenzoate, foodIndex, profile, hasCollectedSodiumBenzoate);
    }

    private void ShowRangeZoneIfAvailable(PreservativeType type, int foodIndex, K3_FoodDatabase.FoodProfile profile, bool hasCollected)
    {
        if (!hasCollected) return;
        if (foodPreservativesUsed[foodIndex].Contains(type)) return;

        Image zone = GetRangeZoneForType(type);
        Slider slider = GetSliderForType(type);
        if (zone == null || slider == null) return;

        // Use random range position as center, random height per slider
        float rangeCenter = perSliderRangeMin[type];
        float zoneHeightValue = perSliderZoneHeight.ContainsKey(type) ? perSliderZoneHeight[type] : 30f;
        RectTransform zoneRect = zone.GetComponent<RectTransform>();
        RectTransform sliderRect = slider.GetComponent<RectTransform>();

        if (slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom)
        {
            float sliderHeight = sliderRect.rect.height;
            // Convert random height from 0-100 space to pixels
            float zonePixelHeight = (zoneHeightValue / 100f) * sliderHeight;
            // Keep existing width, set random height
            zoneRect.sizeDelta = new Vector2(zoneRect.sizeDelta.x, zonePixelHeight);

            float normalizedCenter = rangeCenter / 100f;
            float centerY = -sliderHeight / 2f + normalizedCenter * sliderHeight;
            float halfZone = zonePixelHeight / 2f;
            centerY = Mathf.Clamp(centerY, -sliderHeight / 2f + halfZone, sliderHeight / 2f - halfZone);
            zoneRect.anchoredPosition = new Vector2(zoneRect.anchoredPosition.x, centerY);

            // Compute actual range min/max in 0-100 space
            float bottomNorm = (centerY - halfZone + sliderHeight / 2f) / sliderHeight * 100f;
            float topNorm = (centerY + halfZone + sliderHeight / 2f) / sliderHeight * 100f;
            perSliderRangeMin[type] = Mathf.Max(0f, bottomNorm);
            perSliderRangeMax[type] = Mathf.Min(100f, topNorm);
        }
        else
        {
            float sliderWidth = sliderRect.rect.width;
            float zonePixelWidth = (zoneHeightValue / 100f) * sliderWidth;
            // Keep existing height, set random width
            zoneRect.sizeDelta = new Vector2(zonePixelWidth, zoneRect.sizeDelta.y);

            float normalizedCenter = rangeCenter / 100f;
            float centerX = -sliderWidth / 2f + normalizedCenter * sliderWidth;
            float halfZone = zonePixelWidth / 2f;
            centerX = Mathf.Clamp(centerX, -sliderWidth / 2f + halfZone, sliderWidth / 2f - halfZone);
            zoneRect.anchoredPosition = new Vector2(centerX, zoneRect.anchoredPosition.y);

            float leftNorm = (centerX - halfZone + sliderWidth / 2f) / sliderWidth * 100f;
            float rightNorm = (centerX + halfZone + sliderWidth / 2f) / sliderWidth * 100f;
            perSliderRangeMin[type] = Mathf.Max(0f, leftNorm);
            perSliderRangeMax[type] = Mathf.Min(100f, rightNorm);
        }

        // Don't change zone color or size — keep what's set in Inspector
        zone.gameObject.SetActive(true);

        // Show the arrow too
        ShowArrow(type, true);
    }

    public void SetButtonInteractable(PreservativeType type, bool interactable)
    {
        Button button = GetButtonForPreservative(type);
        if (button != null)
        {
            // Only allow interactable if preservative is collected
            bool hasPreservative = false;
            switch (type)
            {
                case PreservativeType.AscorbicAcid:
                    hasPreservative = hasCollectedAscorbicAcid;
                    break;
                case PreservativeType.PotassiumSorbate:
                    hasPreservative = hasCollectedPotassiumSorbate;
                    break;
                case PreservativeType.SodiumBenzoate:
                    hasPreservative = hasCollectedSodiumBenzoate;
                    break;
            }

            button.interactable = hasPreservative && interactable;
        }
    }

    public void SetAllButtonsInteractable(bool interactable)
    {
        SetAllPreservativeButtonsInteractable(interactable);
    }

    public void UpdateButtonStates(int foodIndex, List<PreservativeType> usedPreservatives)
    {
        if (currentFoodIndex != foodIndex) return;

        foreach (var type in usedPreservatives)
        {
            SetButtonInteractable(type, false);
            SetButtonIcon(type, false);
        }

        UpdateAllButtonIcons();
    }

    public void UpdateStatusText(string message, Color color)
    {
        if (preservationStatusText != null)
        {
            preservationStatusText.text = message;
            preservationStatusText.color = color;
        }
    }

    // ============================================================
    //  WARNING PANEL & LIFE PENALTY
    // ============================================================

    /// <summary>
    /// Shows the warning panel with a message and auto-hides it after warningDisplayDuration.
    /// </summary>
    private void ShowWarning(string message)
    {
        if (warningPanel == null || warningText == null) return;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningText.text = message;
        warningPanel.SetActive(true);

        warningCoroutine = StartCoroutine(AutoHideWarning());
    }

    private IEnumerator AutoHideWarning()
    {
        yield return new WaitForSeconds(warningDisplayDuration);
        HideWarning();
    }

    private void HideWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
        warningCoroutine = null;
    }

    /// <summary>
    /// Deducts one life from the player's health so the loss is
    /// immediately reflected in the health/life panel.
    /// </summary>
    private void ApplyLifePenalty()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PreserviaPlayerStat>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(penaltyDamage);
            Debug.Log($"K3_KingAS2: Life penalty applied ({penaltyDamage} damage). Health: {playerHealth.currentHealth}/{playerHealth.maxHealth}");
        }
    }

    /// <summary>
    /// Returns a human-readable name for the preservative type.
    /// </summary>
    private string GetPreservativeDisplayName(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return "Ascorbic Acid";
            case PreservativeType.PotassiumSorbate: return "Potassium Sorbate";
            case PreservativeType.SodiumBenzoate: return "Sodium Benzoate";
            default: return type.ToString();
        }
    }

    public void ResetForNextAttempt(PreservativeType type)
    {
        ResetPreservationStateForType(type);
        preservationComplete = false;
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
    }

    private void SetupPreservativeButtonsForFood(int foodIndex)
    {
        if (foodIndex == -1) return;

        bool isCompleted = foodCompleted[foodIndex];

        // Update collection status first
        UpdateCollectionStatus();

        if (ascorbicAcidButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid);
            bool canUse = hasCollectedAscorbicAcid && !isCompleted && !alreadyUsed;
            ascorbicAcidButton.interactable = canUse;
            SetButtonIcon(PreservativeType.AscorbicAcid, foodButtonRetryModes[foodIndex][PreservativeType.AscorbicAcid]);
        }

        if (potassiumSorbateButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.PotassiumSorbate);
            bool canUse = hasCollectedPotassiumSorbate && !isCompleted && !alreadyUsed;
            potassiumSorbateButton.interactable = canUse;
            SetButtonIcon(PreservativeType.PotassiumSorbate, foodButtonRetryModes[foodIndex][PreservativeType.PotassiumSorbate]);
        }

        if (sodiumBenzoateButton != null)
        {
            bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
            bool canUse = hasCollectedSodiumBenzoate && !isCompleted && !alreadyUsed;
            sodiumBenzoateButton.interactable = canUse;
            SetButtonIcon(PreservativeType.SodiumBenzoate, foodButtonRetryModes[foodIndex][PreservativeType.SodiumBenzoate]);
        }

        UpdateStatusText();
    }

    private void SetButtonIcon(PreservativeType type, bool isRetryMode)
    {
        if (preservationUISettings == null || currentFoodIndex == -1) return;

        bool shouldShowRetry = isRetryMode || foodButtonRetryModes[currentFoodIndex][type];

        switch (type)
        {
            case PreservativeType.AscorbicAcid:
                if (preservationUISettings.ascorbicBTNimg != null)
                {
                    preservationUISettings.ascorbicBTNimg.sprite = shouldShowRetry ? retryIcon : ascorbicIcon;
                    preservationUISettings.ascorbicBTNimg.preserveAspect = true;
                }
                break;

            case PreservativeType.PotassiumSorbate:
                if (preservationUISettings.potassiumBTNimg != null)
                {
                    preservationUISettings.potassiumBTNimg.sprite = shouldShowRetry ? retryIcon : potassiumIcon;
                    preservationUISettings.potassiumBTNimg.preserveAspect = true;
                }
                break;

            case PreservativeType.SodiumBenzoate:
                if (preservationUISettings.sodiumBTNimg != null)
                {
                    preservationUISettings.sodiumBTNimg.sprite = shouldShowRetry ? retryIcon : sodiumIcon;
                    preservationUISettings.sodiumBTNimg.preserveAspect = true;
                }
                break;
        }
    }

    public void UpdateStatusText()
    {
        if (currentFoodIndex == -1 || preservationStatusText == null) return;

        if (foodCompleted[currentFoodIndex])
        {
            preservationStatusText.text = $"Already preserved with {GetPreservativeList(currentFoodIndex)}";
            preservationStatusText.color = Color.green;
        }
        else if (foodPreservativesUsed[currentFoodIndex].Count > 0)
        {
            preservationStatusText.text = $"Partially preserved. {GetRemainingPreservativesText(currentFoodIndex)}";
            preservationStatusText.color = Color.yellow;
        }
        else
        {
            // Update collection status message
            string collectionMessage = GetCollectionStatusMessage();
            preservationStatusText.text = $"{collectionMessage}\nArrow will start moving when you inspect a food!";
            preservationStatusText.color = Color.black;
        }
    }

    private string GetCollectionStatusMessage()
    {
        List<string> missingPreservatives = new List<string>();

        if (!hasCollectedAscorbicAcid) missingPreservatives.Add("Ascorbic Acid");
        if (!hasCollectedPotassiumSorbate) missingPreservatives.Add("Potassium Sorbate");
        if (!hasCollectedSodiumBenzoate) missingPreservatives.Add("Sodium Benzoate");

        if (missingPreservatives.Count == 3)
            return "No preservatives collected! Find potions in the castle.";
        else if (missingPreservatives.Count > 0)
            return $"Missing: {string.Join(", ", missingPreservatives)}";
        else
            return "All preservatives collected!";
    }

    // ============================================================
    //  NEW CLICK-BASED ARROW MECHANIC
    // ============================================================

    /// <summary>
    /// Player clicks a preservative button to STOP the auto-moving arrow and evaluate.
    /// </summary>
    private void OnPreservativeButtonClicked(PreservativeType type)
    {
        if (currentFoodIndex == -1) return;
        if (foodCompleted[currentFoodIndex]) return;

        if (!IsPreservativeCollected(type))
        {
            string displayName = GetPreservativeDisplayName(type);
            preservationStatusText.text = $"{displayName} not collected! Find the potion first.";
            preservationStatusText.color = Color.red;
            ShowWarning($"{displayName} is not yet collected.");
            ApplyLifePenalty();
            return;
        }

        // If in retry mode → reset and restart the arrow
        if (foodButtonRetryModes[currentFoodIndex][type])
        {
            foodButtonRetryModes[currentFoodIndex][type] = false;
            SetButtonIcon(type, false);
            ResetPreservationStateForType(type);

            // Restart arrow movement
            K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
            if (profile != null)
            {
                AutoStartArrow(currentFoodIndex);
            }
            preservationStatusText.text = "Arrow restarted! Click when it's in the green zone!";
            preservationStatusText.color = Color.yellow;
            return;
        }

        // Arrow must be moving to stop it
        if (!isArrowMoving || !isPreserving) return;

        // === STOP the arrow and evaluate for this preservative ===
        PlaySound(buttonClickSound);
        ScaleButton(type, true);
        StartCoroutine(DelayedButtonScaleDown(type, 0.3f));

        CurrentPreservativeType = type;
        // Set CurrentSliderValue to this specific slider's current value
        CurrentSliderValue = perSliderValue.ContainsKey(type) ? perSliderValue[type] : 0f;
        isArrowMoving = false;

        CheckPreservationResult();
    }

    private void Update()
    {
        if (!isArrowMoving || !isPreserving) return;

        // Update each visible arrow independently at its own speed
        UpdateSingleArrow(PreservativeType.AscorbicAcid, ascorbicArrow, ascorbicAcidSlider);
        UpdateSingleArrow(PreservativeType.PotassiumSorbate, potassiumArrow, potassiumSorbateSlider);
        UpdateSingleArrow(PreservativeType.SodiumBenzoate, sodiumArrow, sodiumBenzoateSlider);
    }

    private void UpdateSingleArrow(PreservativeType type, RectTransform arrow, Slider slider)
    {
        if (arrow == null || !arrow.gameObject.activeSelf) return;
        if (!perSliderValue.ContainsKey(type)) return;

        float speed = perSliderSpeed[type];
        float value = perSliderValue[type];
        bool increasing = perSliderIncreasing[type];

        if (increasing)
        {
            value += speed * Time.deltaTime;
            if (value >= 100f)
            {
                value = 100f;
                increasing = false;
            }
        }
        else
        {
            value -= speed * Time.deltaTime;
            if (value <= 0f)
            {
                value = 0f;
                increasing = true;
            }
        }

        perSliderValue[type] = value;
        perSliderIncreasing[type] = increasing;

        // Update slider position (fill stays transparent)
        if (slider != null) slider.value = value;
        UpdateArrowPosition(type, value);
    }

    private void UpdateSliderUI(Slider slider, PreservativeType type)
    {
        if (slider != null)
        {
            slider.value = CurrentSliderValue;
            UpdateSliderColor(CurrentSliderValue, type);

            TMP_Text valueText = GetValueTextForPreservative(type);
            if (valueText != null) valueText.text = $"{CurrentSliderValue:F0}";
        }
    }

    // ============================================================
    //  ARROW & RANGE ZONE HELPERS
    // ============================================================

    private bool IsPreservativeCollected(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid:
                return hasCollectedAscorbicAcid;
            case PreservativeType.PotassiumSorbate:
                return hasCollectedPotassiumSorbate;
            case PreservativeType.SodiumBenzoate:
                return hasCollectedSodiumBenzoate;
            default:
                return false;
        }
    }

    private void ShowArrow(PreservativeType type, bool show)
    {
        RectTransform arrow = GetArrowForType(type);
        if (arrow != null) arrow.gameObject.SetActive(show);
    }

    private void HideAllArrowsAndZones()
    {
        if (ascorbicArrow != null) ascorbicArrow.gameObject.SetActive(false);
        if (potassiumArrow != null) potassiumArrow.gameObject.SetActive(false);
        if (sodiumArrow != null) sodiumArrow.gameObject.SetActive(false);

        if (ascorbicRangeZone != null) ascorbicRangeZone.gameObject.SetActive(false);
        if (potassiumRangeZone != null) potassiumRangeZone.gameObject.SetActive(false);
        if (sodiumRangeZone != null) sodiumRangeZone.gameObject.SetActive(false);
    }

    /// <summary>
    /// Positions the arrow indicator along the slider track based on value (0-100).
    /// Works for both vertical and horizontal sliders.
    /// </summary>
    private void UpdateArrowPosition(PreservativeType type, float value)
    {
        RectTransform arrow = GetArrowForType(type);
        Slider slider = GetSliderForType(type);
        if (arrow == null || slider == null) return;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        float normalized = value / 100f;

        if (slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom)
        {
            // Vertical slider: move arrow along Y axis
            float sliderHeight = sliderRect.rect.height;
            float bottomY = -sliderHeight / 2f;
            float topY = sliderHeight / 2f;
            float y = Mathf.Lerp(bottomY, topY, normalized);
            if (slider.direction == Slider.Direction.TopToBottom)
                y = Mathf.Lerp(topY, bottomY, normalized);
            arrow.anchoredPosition = new Vector2(arrow.anchoredPosition.x, y);
        }
        else
        {
            // Horizontal slider: move arrow along X axis
            float sliderWidth = sliderRect.rect.width;
            float leftX = -sliderWidth / 2f;
            float rightX = sliderWidth / 2f;
            float x = Mathf.Lerp(leftX, rightX, normalized);
            if (slider.direction == Slider.Direction.RightToLeft)
                x = Mathf.Lerp(rightX, leftX, normalized);
            arrow.anchoredPosition = new Vector2(x, arrow.anchoredPosition.y);
        }
    }

    /// <summary>
    /// Positions and sizes a range zone Image on a slider based on min/max values.
    /// </summary>
    private void PositionRangeZone(Image zone, Slider slider, float minVal, float maxVal)
    {
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        RectTransform zoneRect = zone.GetComponent<RectTransform>();

        float normalizedMin = minVal / 100f;
        float normalizedMax = maxVal / 100f;

        if (slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom)
        {
            float sliderHeight = sliderRect.rect.height;
            float bottomY = -sliderHeight / 2f;

            float zoneBottom = bottomY + normalizedMin * sliderHeight;
            float zoneTop = bottomY + normalizedMax * sliderHeight;
            float zoneHeight = zoneTop - zoneBottom;
            float zoneCenterY = (zoneBottom + zoneTop) / 2f;

            zoneRect.anchoredPosition = new Vector2(0f, zoneCenterY);
            zoneRect.sizeDelta = new Vector2(sliderRect.rect.width, zoneHeight);
        }
        else
        {
            float sliderWidth = sliderRect.rect.width;
            float leftX = -sliderWidth / 2f;

            float zoneLeft = leftX + normalizedMin * sliderWidth;
            float zoneRight = leftX + normalizedMax * sliderWidth;
            float zoneWidth = zoneRight - zoneLeft;
            float zoneCenterX = (zoneLeft + zoneRight) / 2f;

            zoneRect.anchoredPosition = new Vector2(zoneCenterX, 0f);
            zoneRect.sizeDelta = new Vector2(zoneWidth, sliderRect.rect.height);
        }
    }

    private void GetTargetRange(PreservativeType type, K3_FoodDatabase.FoodProfile profile, out float min, out float max)
    {
        // Special case for Fruit Juice (index 7)
        if (currentFoodIndex == 7)
        {
            if (type == PreservativeType.SodiumBenzoate) { min = 50f; max = 60f; return; }
            if (type == PreservativeType.AscorbicAcid) { min = 40f; max = 50f; return; }
        }
        min = profile.minSliderValue;
        max = profile.maxSliderValue;
    }

    private void SetOtherButtonsInteractable(PreservativeType activeType, bool interactable)
    {
        if (activeType != PreservativeType.AscorbicAcid && ascorbicAcidButton != null)
            ascorbicAcidButton.interactable = interactable && hasCollectedAscorbicAcid;
        if (activeType != PreservativeType.PotassiumSorbate && potassiumSorbateButton != null)
            potassiumSorbateButton.interactable = interactable && hasCollectedPotassiumSorbate;
        if (activeType != PreservativeType.SodiumBenzoate && sodiumBenzoateButton != null)
            sodiumBenzoateButton.interactable = interactable && hasCollectedSodiumBenzoate;
    }

    private RectTransform GetArrowForType(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicArrow;
            case PreservativeType.PotassiumSorbate: return potassiumArrow;
            case PreservativeType.SodiumBenzoate: return sodiumArrow;
            default: return null;
        }
    }

    private Image GetRangeZoneForType(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicRangeZone;
            case PreservativeType.PotassiumSorbate: return potassiumRangeZone;
            case PreservativeType.SodiumBenzoate: return sodiumRangeZone;
            default: return null;
        }
    }

    private Slider GetSliderForType(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicAcidSlider;
            case PreservativeType.PotassiumSorbate: return potassiumSorbateSlider;
            case PreservativeType.SodiumBenzoate: return sodiumBenzoateSlider;
            default: return null;
        }
    }

    private void UpdateSliderColor(float value, PreservativeType type)
    {
        // Fill is always transparent — no color changes needed
    }

    private void CheckPreservationResult()
    {
        if (currentFoodIndex == -1 || !isPreserving) return;

        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;

        // Check if the arrow value is within this slider's randomized range zone
        bool isInRange = false;
        if (perSliderRangeMin.ContainsKey(CurrentPreservativeType) && perSliderRangeMax.ContainsKey(CurrentPreservativeType))
        {
            float rangeMin = perSliderRangeMin[CurrentPreservativeType];
            float rangeMax = perSliderRangeMax[CurrentPreservativeType];
            isInRange = CurrentSliderValue >= rangeMin && CurrentSliderValue <= rangeMax;
        }

        if (isInRange)
        {
            bool isCorrectPreservative = IsCorrectPreservativeForFood(currentFoodIndex, CurrentPreservativeType);
            bool alreadyApplied = foodPreservativesUsed[currentFoodIndex].Contains(CurrentPreservativeType);

            if (isCorrectPreservative && !alreadyApplied)
            {
                string level = GetPreservationLevelDescription(CurrentSliderValue);
                preservationStatusText.text = $"Perfect! {CurrentPreservativeType} at {CurrentSliderValue:F0} is within target range!\n<color=#4CAF50>{level} preservation applied.</color>";
                preservationStatusText.color = Color.green;
                preservationComplete = true;

                PlaySound(successSound);
                StartCoroutine(SuccessFeedback());

                foodSliderValues[currentFoodIndex][CurrentPreservativeType] = CurrentSliderValue;

                // Notify K3_KingAssessment to auto-close
                OnCorrectPreservativeApplied?.Invoke();
            }
            else if (alreadyApplied)
            {
                string displayName = GetPreservativeDisplayName(CurrentPreservativeType);
                preservationStatusText.text = $"{displayName} already applied to this food!";
                preservationStatusText.color = Color.yellow;
                preservationComplete = false;

                if (confirmButton != null)
                {
                    confirmButton.interactable = false;
                }

                if (scoringSystem != null)
                {
                    scoringSystem.DeductPointsForMistake(currentFoodIndex, 300);
                }

                foodButtonRetryModes[currentFoodIndex][CurrentPreservativeType] = true;
                SetButtonIcon(CurrentPreservativeType, true);

                ShowWarning($"{displayName} was already applied to this food!");
                ApplyLifePenalty();

                PlaySound(failureSound);
                StartCoroutine(ShakeButton(GetButtonForPreservative(CurrentPreservativeType)));
                TriggerHapticFeedback();
            }
            else
            {
                string displayName = GetPreservativeDisplayName(CurrentPreservativeType);
                preservationStatusText.text = $"Wrong preservative! {displayName} is not needed for {profile.foodName}.";
                preservationStatusText.color = Color.red;
                preservationComplete = false;

                if (confirmButton != null)
                {
                    confirmButton.interactable = false;
                }
                if (scoringSystem != null)
                {
                    scoringSystem.DeductPointsForMistake(currentFoodIndex, 300);
                }

                foodButtonRetryModes[currentFoodIndex][CurrentPreservativeType] = true;
                SetButtonIcon(CurrentPreservativeType, true);

                ShowWarning($"Wrong ingredient! {displayName} is not the right preservative for {profile.foodName}.");
                ApplyLifePenalty();

                PlaySound(failureSound);
                StartCoroutine(FailureFeedback());
                StartCoroutine(ShakeButton(GetButtonForPreservative(CurrentPreservativeType)));
                TriggerHapticFeedback();
            }
        }
        else
        {
            // Arrow was NOT in the colored range zone
            preservationStatusText.text = $"Missed! Value {CurrentSliderValue:F0} is outside the target range. Try again!";
            preservationStatusText.color = Color.red;
            preservationComplete = false;

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
            if (scoringSystem != null)
            {
                scoringSystem.DeductPointsForMistake(currentFoodIndex, 300);
            }

            foodButtonRetryModes[currentFoodIndex][CurrentPreservativeType] = true;
            SetButtonIcon(CurrentPreservativeType, true);

            ShowWarning("Missed the target range! You lost a life.");
            ApplyLifePenalty();

            PlaySound(failureSound);
            StartCoroutine(FailureFeedback());
            StartCoroutine(ShakePanel());
            TriggerHapticFeedback();
        }

        isPreserving = false;
        isArrowMoving = false;
        SetAllSlidersInteractable(false);
        UpdateSliderColor(CurrentSliderValue, CurrentPreservativeType);

        // Hide all arrows and zones after evaluation
        HideAllArrowsAndZones();

        // Show the stopped arrow on the evaluated slider for visual feedback
        ShowArrow(CurrentPreservativeType, true);
        UpdateArrowPosition(CurrentPreservativeType, CurrentSliderValue);
    }

    private bool IsCorrectPreservativeForFood(int foodIndex, PreservativeType type)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;

        if (foodIndex == 7)
        {
            return type == PreservativeType.SodiumBenzoate || type == PreservativeType.AscorbicAcid;
        }

        return type == profile.preservativeType;
    }

    private bool IsFoodFullyPreserved(int foodIndex)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;

        if (foodIndex == 7)
        {
            bool hasSodiumBenzoate = foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
            bool hasAscorbicAcid = foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid);
            return hasSodiumBenzoate && hasAscorbicAcid;
        }

        return foodPreservativesUsed[foodIndex].Contains(profile.preservativeType);
    }

    private string GetPreservativeList(int foodIndex)
    {
        if (foodPreservativesUsed[foodIndex].Count == 0) return "nothing";

        List<string> preservativeNames = new List<string>();
        foreach (var type in foodPreservativesUsed[foodIndex])
        {
            preservativeNames.Add(type.ToString());
        }

        return string.Join(" & ", preservativeNames);
    }

    private string GetRemainingPreservativesText(int foodIndex)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return "";

        if (foodIndex == 7)
        {
            List<string> needed = new List<string>();

            if (!foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate))
                needed.Add("Sodium Benzoate");

            if (!foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid))
                needed.Add("Ascorbic Acid");

            if (needed.Count == 0) return "Fully preserved!";

            return $"Still need: {string.Join(" and ", needed)}";
        }

        if (!foodPreservativesUsed[foodIndex].Contains(profile.preservativeType))
        {
            return $"Still need: {profile.PreservativeDisplayName}";
        }

        return "Fully preserved!";
    }

    private void UpdateAllButtonIcons()
    {
        if (currentFoodIndex == -1) return;

        SetButtonIcon(PreservativeType.AscorbicAcid, foodButtonRetryModes[currentFoodIndex][PreservativeType.AscorbicAcid]);
        SetButtonIcon(PreservativeType.PotassiumSorbate, foodButtonRetryModes[currentFoodIndex][PreservativeType.PotassiumSorbate]);
        SetButtonIcon(PreservativeType.SodiumBenzoate, foodButtonRetryModes[currentFoodIndex][PreservativeType.SodiumBenzoate]);
    }

    private void RestoreFoodState(int foodIndex)
    {
        foreach (var preservativeType in foodPreservativesUsed[foodIndex])
        {
            float value = foodPreservationValues[foodIndex][preservativeType];

            switch (preservativeType)
            {
                case PreservativeType.AscorbicAcid:
                    if (ascorbicAcidSlider != null) ascorbicAcidSlider.value = value;
                    if (ascorbicAcidValueText != null) ascorbicAcidValueText.text = $"{value:F0}";
                    UpdateSliderColor(value, PreservativeType.AscorbicAcid);
                    break;

                case PreservativeType.PotassiumSorbate:
                    if (potassiumSorbateSlider != null) potassiumSorbateSlider.value = value;
                    if (potassiumSorbateValueText != null) potassiumSorbateValueText.text = $"{value:F0}";
                    UpdateSliderColor(value, PreservativeType.PotassiumSorbate);
                    break;

                case PreservativeType.SodiumBenzoate:
                    if (sodiumBenzoateSlider != null) sodiumBenzoateSlider.value = value;
                    if (sodiumBenzoateValueText != null) sodiumBenzoateValueText.text = $"{value:F0}";
                    UpdateSliderColor(value, PreservativeType.SodiumBenzoate);
                    break;
            }
        }
    }

    private void ScaleButton(PreservativeType type, bool scaleUp)
    {
        if (!buttonTransforms.ContainsKey(type) || !originalButtonScales.ContainsKey(type))
            return;

        if (currentScaleCoroutine != null)
            StopCoroutine(currentScaleCoroutine);

        currentScaleCoroutine = StartCoroutine(ScaleButtonCoroutine(type, scaleUp));
    }

    private IEnumerator ScaleButtonCoroutine(PreservativeType type, bool scaleUp)
    {
        RectTransform buttonTransform = buttonTransforms[type];
        Vector3 startScale = buttonTransform.localScale;
        Vector3 targetScale = scaleUp ?
            originalButtonScales[type] * buttonScaleFactor :
            originalButtonScales[type];

        float elapsedTime = 0f;

        while (elapsedTime < buttonScaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / buttonScaleDuration;
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        buttonTransform.localScale = targetScale;
        currentScaleCoroutine = null;
    }

    private IEnumerator DelayedButtonScaleDown(PreservativeType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        ScaleButton(type, false);
    }

    private IEnumerator ShakePanel()
    {
        if (!enableShakeAnimations) yield break;

        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        Vector3 originalPosition = panelRect.anchoredPosition;
        float shakeDuration = 0.5f;
        float shakeIntensity = 10f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            panelRect.anchoredPosition = originalPosition + new Vector3(x, y, 0);
            yield return null;
        }

        panelRect.anchoredPosition = originalPosition;
    }

    private IEnumerator ShakeButton(Button button)
    {
        if (!enableShakeAnimations || button == null) yield break;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null) yield break;

        Vector3 originalScale = buttonRect.localScale;
        float shakeDuration = 0.3f;
        float shakeIntensity = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float xShake = Random.Range(-shakeIntensity, shakeIntensity);
            float yShake = Random.Range(-shakeIntensity, shakeIntensity);
            buttonRect.localScale = originalScale + new Vector3(xShake, yShake, 0);
            yield return null;
        }

        buttonRect.localScale = originalScale;
    }

    private IEnumerator SuccessFeedback()
    {
        Slider slider = GetSliderForType(CurrentPreservativeType);
        if (slider != null)
        {
            Image fillImage = GetFillImageForType(CurrentPreservativeType);
            if (fillImage != null)
            {
                Color originalColor = fillImage.color;

                for (int i = 0; i < 3; i++)
                {
                    fillImage.color = Color.green;
                    yield return new WaitForSeconds(0.1f);
                    fillImage.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }

                UpdateSliderColor(CurrentSliderValue, CurrentPreservativeType);
            }
        }
    }

    private IEnumerator FailureFeedback()
    {
        Slider slider = GetSliderForType(CurrentPreservativeType);
        if (slider != null)
        {
            Image fillImage = GetFillImageForType(CurrentPreservativeType);
            if (fillImage != null)
            {
                Color originalColor = fillImage.color;

                for (int i = 0; i < 3; i++)
                {
                    fillImage.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    fillImage.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }

                if (CurrentSliderValue > 0)
                {
                    UpdateSliderColor(CurrentSliderValue, CurrentPreservativeType);
                }
            }
        }
    }

    private void TriggerHapticFeedback()
    {
        if (!enableHapticFeedback) return;

#if UNITY_IOS || UNITY_ANDROID
        if (SystemInfo.supportsVibration)
        {
            StartCoroutine(VibrateForSeconds(0.1f));
        }
#else
        Debug.Log("Haptic feedback triggered (PC/Editor)");
#endif
    }

    private void TriggerSuccessHapticFeedback()
    {
        if (!enableHapticFeedback) return;

#if UNITY_IOS || UNITY_ANDROID
        if (SystemInfo.supportsVibration)
        {
            StartCoroutine(VibrateForSeconds(0.05f));
        }
#endif
    }

    private IEnumerator VibrateForSeconds(float seconds)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
        yield return new WaitForSeconds(seconds);
#else
        yield return null;
#endif
    }

    private string GetPreservationLevelDescription(float value)
    {
        if (value <= 20) return "Minimal";
        if (value <= 50) return "Moderate";
        if (value <= 80) return "High";
        return "Very High";
    }

    private Image GetFillImageForType(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicAcidFillImage;
            case PreservativeType.PotassiumSorbate: return potassiumSorbateFillImage;
            case PreservativeType.SodiumBenzoate: return sodiumBenzoateFillImage;
            default: return null;
        }
    }

    private TMP_Text GetValueTextForPreservative(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicAcidValueText;
            case PreservativeType.PotassiumSorbate: return potassiumSorbateValueText;
            case PreservativeType.SodiumBenzoate: return sodiumBenzoateValueText;
            default: return null;
        }
    }

    private Button GetButtonForPreservative(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return ascorbicAcidButton;
            case PreservativeType.PotassiumSorbate: return potassiumSorbateButton;
            case PreservativeType.SodiumBenzoate: return sodiumBenzoateButton;
            default: return null;
        }
    }

    private void SetAllPreservativeButtonsInteractable(bool interactable)
    {
        if (ascorbicAcidButton != null)
            ascorbicAcidButton.interactable = hasCollectedAscorbicAcid && interactable;
        if (potassiumSorbateButton != null)
            potassiumSorbateButton.interactable = hasCollectedPotassiumSorbate && interactable;
        if (sodiumBenzoateButton != null)
            sodiumBenzoateButton.interactable = hasCollectedSodiumBenzoate && interactable;
    }

    private void SetAllSlidersInteractable(bool interactable)
    {
        if (ascorbicAcidSlider != null) ascorbicAcidSlider.interactable = interactable;
        if (potassiumSorbateSlider != null) potassiumSorbateSlider.interactable = interactable;
        if (sodiumBenzoateSlider != null) sodiumBenzoateSlider.interactable = interactable;
    }

    private void ResetPreservationState()
    {
        isPreserving = false;
        isArrowMoving = false;
        preservationComplete = false;
        CurrentSliderValue = 0f;

        ResetAllSliders();
        MakeAllFillsTransparent();
        SetAllSlidersInteractable(false);
        HideAllArrowsAndZones();

        foreach (var kvp in buttonTransforms)
        {
            if (kvp.Value != null && originalButtonScales.ContainsKey(kvp.Key))
            {
                kvp.Value.localScale = originalButtonScales[kvp.Key];
            }
        }
    }

    private void ResetPreservationStateForType(PreservativeType type)
    {
        isPreserving = false;
        isArrowMoving = false;
        preservationComplete = false;

        switch (type)
        {
            case PreservativeType.AscorbicAcid:
                if (ascorbicAcidSlider != null) ascorbicAcidSlider.value = 0;
                if (ascorbicAcidValueText != null) ascorbicAcidValueText.text = "0";
                UpdateSliderColor(0, PreservativeType.AscorbicAcid);
                break;

            case PreservativeType.PotassiumSorbate:
                if (potassiumSorbateSlider != null) potassiumSorbateSlider.value = 0;
                if (potassiumSorbateValueText != null) potassiumSorbateValueText.text = "0";
                UpdateSliderColor(0, PreservativeType.PotassiumSorbate);
                break;

            case PreservativeType.SodiumBenzoate:
                if (sodiumBenzoateSlider != null) sodiumBenzoateSlider.value = 0;
                if (sodiumBenzoateValueText != null) sodiumBenzoateValueText.text = "0";
                UpdateSliderColor(0, PreservativeType.SodiumBenzoate);
                break;
        }

        SetAllSlidersInteractable(false);
    }

    private void ResetAllSliders()
    {
        if (ascorbicAcidSlider != null)
        {
            ascorbicAcidSlider.value = 0;
            UpdateSliderColor(0, PreservativeType.AscorbicAcid);
        }
        if (potassiumSorbateSlider != null)
        {
            potassiumSorbateSlider.value = 0;
            UpdateSliderColor(0, PreservativeType.PotassiumSorbate);
        }
        if (sodiumBenzoateSlider != null)
        {
            sodiumBenzoateSlider.value = 0;
            UpdateSliderColor(0, PreservativeType.SodiumBenzoate);
        }

        if (ascorbicAcidValueText != null) ascorbicAcidValueText.text = "0";
        if (potassiumSorbateValueText != null) potassiumSorbateValueText.text = "0";
        if (sodiumBenzoateValueText != null) sodiumBenzoateValueText.text = "0";
    }

    // CHANGED: Using AudioHandler instead of local AudioSource
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(clip);
        }
        else if (clip != null && AudioHandler.Instance == null && Debug.isDebugBuild)
        {
            Debug.LogWarning($"AudioHandler.Instance is null! Cannot play sound: {clip.name}");
        }
    }

    public bool IsFoodPreserved(int foodIndex)
    {
        return foodCompleted.ContainsKey(foodIndex) && foodCompleted[foodIndex];
    }

    public List<PreservativeType> GetUsedPreservatives(int foodIndex)
    {
        return foodPreservativesUsed.ContainsKey(foodIndex) ? foodPreservativesUsed[foodIndex] : new List<PreservativeType>();
    }

    public void ResetFood(int foodIndex)
    {
        if (foodCompleted.ContainsKey(foodIndex))
        {
            foodCompleted[foodIndex] = false;
            foodPreservativesUsed[foodIndex].Clear();
            foodPreservationValues[foodIndex].Clear();

            foreach (var type in foodButtonRetryModes[foodIndex].Keys)
            {
                foodButtonRetryModes[foodIndex][type] = false;
            }

            foreach (var type in foodSliderValues[foodIndex].Keys)
            {
                foodSliderValues[foodIndex][type] = 0f;
            }
        }
    }
}