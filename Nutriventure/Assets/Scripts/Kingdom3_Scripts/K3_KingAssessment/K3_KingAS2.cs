using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class K3_KingAS2 : MonoBehaviour
{
    [Header("Preservation System")]
    public PreservationUISettings preservationUISettings;

    public PreservativeType CurrentPreservativeType { get; private set; }
    public float CurrentSliderValue { get; private set; }
    
    [Header("Preservation Settings")]
    [SerializeField] private float baseSliderSpeed = 30f;
    [SerializeField] private float maxSliderSpeed = 120f;
    [SerializeField] private float speedIncreaseRate = 0.5f;
    [SerializeField] private float minRequiredAccuracy = 10f;
    
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
    // REMOVED: [SerializeField] private AudioSource audioSource; - NO LOCAL AUDIO SOURCE
    
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
    
    // Preservation state
    private bool isPreserving = false;
    private bool isButtonHeld = false;
    private bool preservationComplete = false;
    private bool isIncreasing = true;
    private float currentSpeed;
    private float holdDuration = 0f;
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
        
        // REMOVED: AudioSource setup - NO LOCAL AUDIO SOURCE
        
        // Initialize collection system references
        InitializeCollectionReferences();
        
        // Check AudioHandler exists
        if (AudioHandler.Instance == null)
        {
            Debug.LogWarning("AudioHandler.Instance not found! Make sure AudioHandler is in the scene.");
        }
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
            
            // Initially disable all buttons until preservatives are collected
            ascorbicAcidButton.interactable = false;
        }
        
        if (potassiumSorbateButton != null)
        {
            buttonTransforms[PreservativeType.PotassiumSorbate] = potassiumSorbateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.PotassiumSorbate] = buttonTransforms[PreservativeType.PotassiumSorbate].localScale;
            potassiumSorbateButton.interactable = false;
        }
        
        if (sodiumBenzoateButton != null)
        {
            buttonTransforms[PreservativeType.SodiumBenzoate] = sodiumBenzoateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.SodiumBenzoate] = buttonTransforms[PreservativeType.SodiumBenzoate].localScale;
            sodiumBenzoateButton.interactable = false;
        }
        
        SetupButtonHoldEvents(ascorbicAcidButton, PreservativeType.AscorbicAcid, ascorbicAcidSlider);
        SetupButtonHoldEvents(potassiumSorbateButton, PreservativeType.PotassiumSorbate, potassiumSorbateSlider);
        SetupButtonHoldEvents(sodiumBenzoateButton, PreservativeType.SodiumBenzoate, sodiumBenzoateSlider);
    }
    
    private void SetupButtonHoldEvents(Button button, PreservativeType type, Slider slider)
    {
        if (button == null) return;
        
        var existingTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (existingTrigger != null)
        {
            Destroy(existingTrigger);
        }
        
        var eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnButtonPressed(type, slider); });
        eventTrigger.triggers.Add(pointerDown);
        
        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerUp);
        
        var pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerExit);
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
            preservationStatusText.text = $"{collectionMessage}\nSelect and hold preservative button to preserve";
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
    
    private void OnButtonPressed(PreservativeType type, Slider slider)
    {
        if (currentFoodIndex == -1) return;
        if (foodCompleted[currentFoodIndex]) return;
        
        // Check if preservative is collected
        if (!IsPreservativeCollected(type))
        {
            preservationStatusText.text = $"{type} not collected! Find the potion first.";
            preservationStatusText.color = Color.red;
            return;
        }
        
        ScaleButton(type, true);
        
        if (foodButtonRetryModes[currentFoodIndex][type])
        {
            foodButtonRetryModes[currentFoodIndex][type] = false;
            SetButtonIcon(type, false);
            ResetPreservationStateForType(type);
            preservationStatusText.text = "Ready to try again. Hold the button to start.";
            preservationStatusText.color = Color.black;
            StartCoroutine(DelayedButtonScaleDown(type, 0.5f));
            return;
        }
        
        if (isPreserving) return;
        
        // CHANGED: Play sound through AudioHandler
        PlaySound(buttonClickSound);
        
        isPreserving = true;
        isButtonHeld = true;
        isIncreasing = true;
        holdDuration = 0f;
        currentSpeed = baseSliderSpeed;
        
        CurrentPreservativeType = type;
        CurrentSliderValue = 0f;
        
        SetAllSlidersInteractable(false);
        if (slider != null)
        {
            slider.interactable = true;
        }
        
        preservationStatusText.text = $"Holding {type}… Release when in target range!";
        preservationStatusText.color = Color.yellow;
        
        UpdateSliderUI(slider, type);
    }
    
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
    
    private void OnButtonReleased()
    {
        if (!isPreserving || !isButtonHeld) 
        {
            if (CurrentPreservativeType != PreservativeType.AscorbicAcid)
                ScaleButton(CurrentPreservativeType, false);
            return;
        }
        
        isButtonHeld = false;
        ScaleButton(CurrentPreservativeType, false);
        
        CheckPreservationResult();
    }
    
    private void Update()
    {
        if (isPreserving && isButtonHeld)
        {
            holdDuration += Time.deltaTime;
            currentSpeed = Mathf.Lerp(baseSliderSpeed, maxSliderSpeed, holdDuration * speedIncreaseRate);
            
            if (isIncreasing)
            {
                CurrentSliderValue += currentSpeed * Time.deltaTime;
                if (CurrentSliderValue >= 100f)
                {
                    CurrentSliderValue = 100f;
                    isIncreasing = false;
                    PlaySound(bounceSound);
                }
            }
            else
            {
                CurrentSliderValue -= currentSpeed * Time.deltaTime;
                if (CurrentSliderValue <= 0f)
                {
                    CurrentSliderValue = 0f;
                    isIncreasing = true;
                    PlaySound(bounceSound);
                }
            }
            
            UpdateSliderUI(GetSliderForType(CurrentPreservativeType), CurrentPreservativeType);
        }
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
        Image fillImage = GetFillImageForType(type);
        
        if (fillImage != null)
        {
            Color color = Color.white;
            
            switch (type)
            {
                case PreservativeType.AscorbicAcid:
                    if (value <= 20) color = new Color(1f, 0.9f, 0.9f);
                    else if (value <= 50) color = new Color(1f, 0.7f, 0.6f);
                    else if (value <= 80) color = new Color(1f, 0.4f, 0.3f);
                    else color = new Color(1f, 0.3f, 0.2f);
                    break;
                    
                case PreservativeType.PotassiumSorbate:
                    if (value <= 20) color = new Color(0.9f, 1f, 0.9f);
                    else if (value <= 50) color = new Color(0.7f, 1f, 0.6f);
                    else if (value <= 80) color = new Color(0.4f, 0.9f, 0.3f);
                    else color = new Color(0.3f, 0.8f, 0.2f);
                    break;
                    
                case PreservativeType.SodiumBenzoate:
                    if (value <= 20) color = new Color(0.9f, 0.9f, 1f);
                    else if (value <= 50) color = new Color(0.7f, 0.8f, 1f);
                    else if (value <= 80) color = new Color(0.4f, 0.6f, 1f);
                    else color = new Color(0.3f, 0.5f, 1f);
                    break;
            }
            
            fillImage.color = color;
        }
    }
    
    private void CheckPreservationResult()
    {
        if (currentFoodIndex == -1 || !isPreserving) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;
        
        bool isInRange = false;
        bool isCloseEnough = false;
        
        if (currentFoodIndex == 7)
        {
            if (CurrentPreservativeType == PreservativeType.SodiumBenzoate)
            {
                isInRange = CurrentSliderValue >= 50 && CurrentSliderValue <= 60;
                isCloseEnough = Mathf.Abs(CurrentSliderValue - 55) <= minRequiredAccuracy;
            }
            else if (CurrentPreservativeType == PreservativeType.AscorbicAcid)
            {
                isInRange = CurrentSliderValue >= 40 && CurrentSliderValue <= 50;
                isCloseEnough = Mathf.Abs(CurrentSliderValue - 45) <= minRequiredAccuracy;
            }
        }
        else
        {
            isInRange = profile.IsValueInRange(CurrentSliderValue);
            isCloseEnough = Mathf.Abs(CurrentSliderValue - ((profile.minSliderValue + profile.maxSliderValue) / 2)) <= minRequiredAccuracy;
        }
        
        bool isInIncorrectRange = false;
        if (!isInRange && !isCloseEnough)
        {
            if (currentFoodIndex == 7)
            {
                if (CurrentPreservativeType == PreservativeType.SodiumBenzoate)
                {
                    isInIncorrectRange = CurrentSliderValue < 30 || CurrentSliderValue > 80;
                }
                else if (CurrentPreservativeType == PreservativeType.AscorbicAcid)
                {
                    isInIncorrectRange = CurrentSliderValue < 20 || CurrentSliderValue > 70;
                }
            }
            else
            {
                float targetCenter = (profile.minSliderValue + profile.maxSliderValue) / 2f;
                float acceptableRange = (profile.maxSliderValue - profile.minSliderValue) * 1.5f;
                isInIncorrectRange = Mathf.Abs(CurrentSliderValue - targetCenter) > acceptableRange;
            }
        }
        
        if (isInRange || isCloseEnough)
        {
            bool isCorrectPreservative = IsCorrectPreservativeForFood(currentFoodIndex, CurrentPreservativeType);
            bool alreadyApplied = foodPreservativesUsed[currentFoodIndex].Contains(CurrentPreservativeType);
            
            if (isCorrectPreservative && !alreadyApplied)
            {
                string level = GetPreservationLevelDescription(CurrentSliderValue);
                preservationStatusText.text = $"Perfect! {CurrentPreservativeType} at {CurrentSliderValue:F0} is within target range!\n<color=#4CAF50>{level} preservation applied. Click CONFIRM to apply.</color>";
                preservationStatusText.color = Color.green;
                preservationComplete = true;
                
                if (confirmButton != null)
                {
                    confirmButton.interactable = true;
                }
                
                PlaySound(successSound);
                StartCoroutine(SuccessFeedback());
                
                foodSliderValues[currentFoodIndex][CurrentPreservativeType] = CurrentSliderValue;
            }
            else if (alreadyApplied)
            {
                preservationStatusText.text = $"{CurrentPreservativeType} already applied to this food!";
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
                PlaySound(failureSound);
                StartCoroutine(ShakeButton(GetButtonForPreservative(CurrentPreservativeType)));
                TriggerHapticFeedback();
            }
            else
            {
                preservationStatusText.text = $"Wrong preservative! {CurrentPreservativeType} is not needed for {profile.foodName}.";
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
                
                PlaySound(failureSound);
                StartCoroutine(FailureFeedback());
                StartCoroutine(ShakeButton(GetButtonForPreservative(CurrentPreservativeType)));
                TriggerHapticFeedback();
            }
        }
        else
        {
            preservationStatusText.text = $"{CurrentSliderValue:F0} is not in target range. Try again!";
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
            
            if (isInIncorrectRange)
            {
                ResetPreservationStateForType(CurrentPreservativeType);
                preservationStatusText.text = "Too far off target! Resetting slider. Try again.";
            }
            
            PlaySound(failureSound);
            StartCoroutine(FailureFeedback());
            StartCoroutine(ShakePanel());
            TriggerHapticFeedback();
        }
        
        isPreserving = false;
        SetAllSlidersInteractable(false);
        UpdateSliderColor(CurrentSliderValue, CurrentPreservativeType);
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
        isButtonHeld = false;
        preservationComplete = false;
        CurrentSliderValue = 0f;
        isIncreasing = true;
        holdDuration = 0f;
        currentSpeed = baseSliderSpeed;
        
        ResetAllSliders();
        SetAllSlidersInteractable(false);
        
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
        isButtonHeld = false;
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