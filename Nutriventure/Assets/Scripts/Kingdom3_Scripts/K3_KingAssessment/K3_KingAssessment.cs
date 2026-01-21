using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class K3_KingAssessment : MonoBehaviour
{
    // Inspector-assigned references
    [Header("Player Interaction")]
    [SerializeField] private GameObject player;
    [SerializeField] private float interactionRange = 3f;
    
    [Header("UI Elements")]
    [SerializeField] private Button inspectButton;  // The "InspectBTN"
    [SerializeField] private GameObject KAPanel;    // The "KA_Panel"
    [SerializeField] private Button exitButton;     // The "ExitBTN" inside the panel
    [SerializeField] private Button confirmButton;  // Button to confirm preservation
    
    [Header("Food Profile UI Elements")]
    [SerializeField] private TMP_Text foodNameText;
    [SerializeField] private TMP_Text foodTypeText;
    [SerializeField] private TMP_Text shelfLifeText;
    [SerializeField] private TMP_Text threatsText;
    [SerializeField] private TMP_Text contentsText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image foodIconImage;
    
    [Header("Preservative Display Elements")]
    [SerializeField] private TMP_Text requiredPreservativeText;  // Separated: Required preservative
    [SerializeField] private TMP_Text targetRangesText;          // Separated: Target ranges
    [SerializeField] private TMP_Text collectedPreservativeText; // Separated: Collected preservative
    
    [Header("Database")]
    [SerializeField] private K3_FoodDatabase foodDatabase;
    
    [Header("Collection System")]
    [SerializeField] private K3_CollectPreservatives collectionSystem;
    [SerializeField] private PreservativesInformationManager infoManager;
    
    [Header("Preservation System")]
    public PreservationUISettings preservationUISettings;
    
    [Header("Objects to Disable")]
    [SerializeField] private GameObject[] objectsToDisable;
    
    [Header("Food Objects")]
    [SerializeField] private GameObject[] KAFoods;
    
    [Header("Food Cameras")]
    [SerializeField] private CinemachineVirtualCamera[] foodCameras;
    
    [Header("Food Particles")]
    [SerializeField] private GameObject[] foodParticles;
    
    [Header("Food Preserved Particles")]
    [SerializeField] private GameObject[] FoodPreservedPS; // New particle system for preserved foods
    
    [Header("Main Camera")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    
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
    
    [Header("Sound Effects (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sliderFillSound;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip buttonClickSound;
    
    private bool isPlayerNear = false;
    private int currentFoodIndex = -1;
    
    // Preservation state
    private bool isPreserving = false;
    private PreservativeType currentPreservativeType;
    private Slider currentActiveSlider;
    private float currentSliderValue = 0f;
    private bool isButtonHeld = false;
    private bool preservationComplete = false;
    private bool isIncreasing = true; // Direction: true = increasing, false = decreasing
    private float currentSpeed;
    private float holdDuration = 0f;
    
    // NEW: Per-food button state tracking
    private Dictionary<int, Dictionary<PreservativeType, bool>> foodButtonRetryModes = new Dictionary<int, Dictionary<PreservativeType, bool>>();
    private Dictionary<int, Dictionary<PreservativeType, float>> foodSliderValues = new Dictionary<int, Dictionary<PreservativeType, float>>();
    
    // Button scale tracking
    private Dictionary<PreservativeType, RectTransform> buttonTransforms = new Dictionary<PreservativeType, RectTransform>();
    private Dictionary<PreservativeType, Vector3> originalButtonScales = new Dictionary<PreservativeType, Vector3>();
    private Coroutine currentScaleCoroutine = null;
    
    // Food completion tracking
    private Dictionary<int, bool> foodCompleted = new Dictionary<int, bool>();
    private Dictionary<int, List<PreservativeType>> foodPreservativesUsed = new Dictionary<int, List<PreservativeType>>(); // CHANGED: List for multiple preservatives
    private Dictionary<int, Dictionary<PreservativeType, float>> foodPreservationValues = new Dictionary<int, Dictionary<PreservativeType, float>>(); // CHANGED: Dictionary for multiple values
    
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
    private TMP_Text preservationStatusText; // Only keep preservation status text
    private Image ascorbicAcidFillImage;
    private Image potassiumSorbateFillImage;
    private Image sodiumBenzoateFillImage;
    
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
        public TMP_Text preservationStatusText; // Only need preservation status text
        
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
        
        // Initialize button states
        if (inspectButton != null)
        {
            inspectButton.gameObject.SetActive(false);
            inspectButton.onClick.AddListener(OnInspectButtonClicked);
        }
        
        // Initialize panel state
        if (KAPanel != null)
        {
            KAPanel.SetActive(false);
        }
        
        // Setup exit button
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        
        // Setup confirm button
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButton.interactable = false;
        }
        
        // Setup preservative buttons
        SetupPreservativeButtons();
        
        // Try to get database if not assigned
        if (foodDatabase == null)
        {
            foodDatabase = K3_FoodDatabase.Instance;
        }
        
        // Try to get collection system if not assigned
        if (collectionSystem == null)
        {
            collectionSystem = FindObjectOfType<K3_CollectPreservatives>();
        }
        
        // Try to get info manager if not assigned
        if (infoManager == null)
        {
            infoManager = FindObjectOfType<PreservativesInformationManager>();
        }
        
        // Setup audio source if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Disable all food cameras initially
        DisableAllFoodCameras();
        
        // Initialize food completion tracking
        for (int i = 0; i < KAFoods.Length; i++)
        {
            foodCompleted[i] = false;
            foodPreservativesUsed[i] = new List<PreservativeType>();
            foodPreservationValues[i] = new Dictionary<PreservativeType, float>();
            
            // Initialize per-food button states
            foodButtonRetryModes[i] = new Dictionary<PreservativeType, bool>
            {
                { PreservativeType.AscorbicAcid, false },
                { PreservativeType.PotassiumSorbate, false },
                { PreservativeType.SodiumBenzoate, false }
            };
            
            // Initialize per-food slider values
            foodSliderValues[i] = new Dictionary<PreservativeType, float>
            {
                { PreservativeType.AscorbicAcid, 0f },
                { PreservativeType.PotassiumSorbate, 0f },
                { PreservativeType.SodiumBenzoate, 0f }
            };
        }
        
        // Initialize all sliders
        InitializeAllSliders();
        
        // Disable preserved particle systems initially
        if (FoodPreservedPS != null)
        {
            foreach (GameObject ps in FoodPreservedPS)
            {
                if (ps != null) ps.SetActive(false);
            }
        }
        
        // Enable all food particles initially (if not preserved)
        if (foodParticles != null)
        {
            for (int i = 0; i < foodParticles.Length; i++)
            {
                if (foodParticles[i] != null && !foodCompleted[i])
                {
                    foodParticles[i].SetActive(true);
                }
            }
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
            
            preservationStatusText = preservationUISettings.preservationStatusText; // Only need preservation status
            
            ascorbicAcidFillImage = preservationUISettings.ascorbicAcidFillImage;
            potassiumSorbateFillImage = preservationUISettings.potassiumSorbateFillImage;
            sodiumBenzoateFillImage = preservationUISettings.sodiumBenzoateFillImage;
        }
    }
    
    private void InitializeAllSliders()
    {
        // Set up all sliders with proper configuration
        ConfigureSlider(ascorbicAcidSlider, 0f, 100f, false);
        ConfigureSlider(potassiumSorbateSlider, 0f, 100f, false);
        ConfigureSlider(sodiumBenzoateSlider, 0f, 100f, false);
        
        // Lock all sliders initially
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
            
            // Ensure handle is always visible but non-interactable
            var handle = slider.handleRect;
            if (handle != null)
            {
                handle.gameObject.SetActive(true);
            }
        }
    }
    
    private void SetupPreservativeButtons()
    {
        // Store original scales and setup animations
        if (ascorbicAcidButton != null)
        {
            buttonTransforms[PreservativeType.AscorbicAcid] = ascorbicAcidButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.AscorbicAcid] = buttonTransforms[PreservativeType.AscorbicAcid].localScale;
        }
        
        if (potassiumSorbateButton != null)
        {
            buttonTransforms[PreservativeType.PotassiumSorbate] = potassiumSorbateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.PotassiumSorbate] = buttonTransforms[PreservativeType.PotassiumSorbate].localScale;
        }
        
        if (sodiumBenzoateButton != null)
        {
            buttonTransforms[PreservativeType.SodiumBenzoate] = sodiumBenzoateButton.GetComponent<RectTransform>();
            originalButtonScales[PreservativeType.SodiumBenzoate] = buttonTransforms[PreservativeType.SodiumBenzoate].localScale;
        }
        
        // Setup EventTriggers ONLY - NO onClick listeners
        SetupButtonHoldEvents(ascorbicAcidButton, PreservativeType.AscorbicAcid, ascorbicAcidSlider);
        SetupButtonHoldEvents(potassiumSorbateButton, PreservativeType.PotassiumSorbate, potassiumSorbateSlider);
        SetupButtonHoldEvents(sodiumBenzoateButton, PreservativeType.SodiumBenzoate, sodiumBenzoateSlider);
    }
    
    private void SetupButtonHoldEvents(Button button, PreservativeType type, Slider slider)
    {
        if (button == null) return;
        
        // Remove existing EventTrigger if any
        var existingTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (existingTrigger != null)
        {
            Destroy(existingTrigger);
        }
        
        // Add new EventTrigger
        var eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        
        // PointerDown event
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { OnButtonPressed(type, slider); });
        eventTrigger.triggers.Add(pointerDown);
        
        // PointerUp event
        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerUp);
        
        // Add PointerExit to handle finger/mouse leaving button
        var pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnButtonReleased(); });
        eventTrigger.triggers.Add(pointerExit);
    }
    
    private void SetButtonIcon(PreservativeType type, bool isRetryMode)
    {
        if (preservationUISettings == null || currentFoodIndex == -1) return;
        
        // Set the appropriate icon based on type and mode for CURRENT food
        bool shouldShowRetry = foodButtonRetryModes[currentFoodIndex][type];
        
        switch (type)
        {
            case PreservativeType.AscorbicAcid:
                if (preservationUISettings.ascorbicBTNimg != null)
                {
                    preservationUISettings.ascorbicBTNimg.sprite = shouldShowRetry ? retryIcon : ascorbicIcon;
                }
                break;
                
            case PreservativeType.PotassiumSorbate:
                if (preservationUISettings.potassiumBTNimg != null)
                {
                    preservationUISettings.potassiumBTNimg.sprite = shouldShowRetry ? retryIcon : potassiumIcon;
                }
                break;
                
            case PreservativeType.SodiumBenzoate:
                if (preservationUISettings.sodiumBTNimg != null)
                {
                    preservationUISettings.sodiumBTNimg.sprite = shouldShowRetry ? retryIcon : sodiumIcon;
                }
                break;
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
    
    private void OnButtonPressed(PreservativeType type, Slider slider)
    {
        if (currentFoodIndex == -1) return;
        if (foodCompleted[currentFoodIndex]) return;
        if (!HasCollectedPreservative(GetPreservativeID(type))) return;
        
        // Scale button up
        ScaleButton(type, true);
        
        // Check if button is in retry mode for THIS food
        if (foodButtonRetryModes[currentFoodIndex][type])
        {
            // Reset ONLY this button's state for THIS food
            foodButtonRetryModes[currentFoodIndex][type] = false;
            SetButtonIcon(type, false);
            
            // Reset preservation state for ONLY this type
            ResetPreservationStateForType(type);
            
            preservationStatusText.text = "Ready to try again. Hold the button to start.";
            preservationStatusText.color = Color.white;
            
            // Scale button back down after a moment
            StartCoroutine(DelayedButtonScaleDown(type, 0.5f));
            return;
        }
        
        if (isPreserving) return;

        // Play button click sound
        PlaySound(buttonClickSound);

        isPreserving = true;
        isButtonHeld = true;
        isIncreasing = true;
        holdDuration = 0f;
        currentSpeed = baseSliderSpeed;

        currentPreservativeType = type;
        currentActiveSlider = slider;
        currentSliderValue = 0f;

        // Disable all sliders except the active one
        SetAllSlidersInteractable(false);
        if (currentActiveSlider != null)
        {
            currentActiveSlider.interactable = true;
        }

        // Get current food profile for target range display
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile != null)
        {
            preservationStatusText.text = $"Holding {type}… Release when in target range!";
        }
        else
        {
            preservationStatusText.text = $"Holding {type}… release when in target range!";
        }
        preservationStatusText.color = Color.yellow;
        
        UpdateSliderUI(); // Initialize UI with 0 value
    }
    
    private IEnumerator DelayedButtonScaleDown(PreservativeType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        ScaleButton(type, false);
    }
    
    private void OnButtonReleased()
    {
        if (!isPreserving || !isButtonHeld) 
        {
            // Still scale down if button was pressed
            if (currentPreservativeType != PreservativeType.AscorbicAcid) // Default check
                ScaleButton(currentPreservativeType, false);
            return;
        }

        isButtonHeld = false;
        
        // Scale button down
        ScaleButton(currentPreservativeType, false);
        
        CheckPreservationResult();
    }
    
    private string GetPreservativeID(PreservativeType type)
    {
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return "0";
            case PreservativeType.PotassiumSorbate: return "1";
            case PreservativeType.SodiumBenzoate: return "2";
            default: return type.ToString();
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
                case PreservativeType.AscorbicAcid: // Brighter Red gradient
                    if (value <= 20) color = new Color(1f, 0.9f, 0.9f);     // Very Light Pink
                    else if (value <= 50) color = new Color(1f, 0.7f, 0.6f); // Light Coral
                    else if (value <= 80) color = new Color(1f, 0.4f, 0.3f); // Salmon
                    else color = new Color(1f, 0.3f, 0.2f); // Bright Red
                    break;
                    
                case PreservativeType.PotassiumSorbate: // Brighter Green gradient
                    if (value <= 20) color = new Color(0.9f, 1f, 0.9f);     // Very Light Green
                    else if (value <= 50) color = new Color(0.7f, 1f, 0.6f); // Light Lime
                    else if (value <= 80) color = new Color(0.4f, 0.9f, 0.3f); // Bright Green
                    else color = new Color(0.3f, 0.8f, 0.2f); // Vivid Green
                    break;
                    
                case PreservativeType.SodiumBenzoate: // Brighter Blue gradient
                    if (value <= 20) color = new Color(0.9f, 0.9f, 1f);     // Very Light Blue
                    else if (value <= 50) color = new Color(0.7f, 0.8f, 1f); // Light Sky Blue
                    else if (value <= 80) color = new Color(0.4f, 0.6f, 1f); // Bright Blue
                    else color = new Color(0.3f, 0.5f, 1f); // Vivid Blue
                    break;
            }
            
            fillImage.color = color;
        }
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
    
    private void Update()
    {
        CheckPlayerProximity();
        
        // Handle button hold for sliders - BOUNCING MECHANIC
        if (isPreserving && isButtonHeld)
        {
            holdDuration += Time.deltaTime;
            
            // Increase speed over time
            currentSpeed = Mathf.Lerp(baseSliderSpeed, maxSliderSpeed, holdDuration * speedIncreaseRate);
            
            // Move slider based on direction
            if (isIncreasing)
            {
                currentSliderValue += currentSpeed * Time.deltaTime;
                if (currentSliderValue >= 100f)
                {
                    currentSliderValue = 100f;
                    isIncreasing = false;
                    PlaySound(bounceSound);
                }
            }
            else
            {
                currentSliderValue -= currentSpeed * Time.deltaTime;
                if (currentSliderValue <= 0f)
                {
                    currentSliderValue = 0f;
                    isIncreasing = true;
                    PlaySound(bounceSound);
                }
            }
            
            UpdateSliderUI();
        }
    }
    
    private void CheckPlayerProximity()
    {
        isPlayerNear = false;
        currentFoodIndex = -1;
        
        if (player == null || KAFoods.Length == 0)
            return;
        
        for (int i = 0; i < KAFoods.Length; i++)
        {
            if (KAFoods[i] == null) continue;
            
            float distance = Vector3.Distance(player.transform.position, KAFoods[i].transform.position);
            
            if (distance <= interactionRange)
            {
                isPlayerNear = true;
                currentFoodIndex = i;
                break;
            }
        }
        
        // Update inspect button visibility based on proximity and panel state
        if (KAPanel != null && !KAPanel.activeSelf)
        {
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(isPlayerNear);
            }
        }
    }
    
    private void OnInspectButtonClicked()
    {
        if (currentFoodIndex == -1) return;
        
        // Play button click sound
        PlaySound(buttonClickSound);
        
        if (KAPanel != null)
        {
            KAPanel.SetActive(true);
            UpdateFoodPanelContent(currentFoodIndex);
            SwitchToFoodCamera(currentFoodIndex);
            
            // Disable food particles when opening panel
            DisableFoodParticle(currentFoodIndex);
            
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
            
            // Hide inspect button when panel opens
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(false);
            }
            
            SetupPreservationSystem(currentFoodIndex);
        }
    }
    
    private void OnExitButtonClicked()
    {
        // Play button click sound
        PlaySound(buttonClickSound);
        
        ClosePreservationPanel();
    }
    
    private void OnConfirmButtonClicked()
    {
        if (currentFoodIndex == -1) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;
        
        // Check if we have a successful preservation
        if (!preservationComplete)
        {
            // Check if we have a value in range
            bool isInRange = profile.IsValueInRange(currentSliderValue);
            bool isCloseEnough = Mathf.Abs(currentSliderValue - ((profile.minSliderValue + profile.maxSliderValue) / 2)) <= minRequiredAccuracy;
            
            if (!isInRange && !isCloseEnough)
            {
                preservationStatusText.text = $"No successful preservation to confirm. Try again!";
                preservationStatusText.color = Color.red;
                return;
            }
            
            // Check if this is a correct preservative for this food
            bool isCorrectPreservative = IsCorrectPreservativeForFood(currentFoodIndex, currentPreservativeType);
            
            if (!isCorrectPreservative)
            {
                preservationStatusText.text = $"✗ {currentPreservativeType} is not the correct preservative for {profile.foodName}";
                preservationStatusText.color = Color.red;
                
                // Switch button to retry mode for THIS food
                foodButtonRetryModes[currentFoodIndex][currentPreservativeType] = true;
                SetButtonIcon(currentPreservativeType, false);
                
                PlaySound(failureSound);
                return;
            }
        }
        
        // Play button click sound
        PlaySound(buttonClickSound);
        
        // Store the successful preservation for this food
        if (!foodPreservativesUsed[currentFoodIndex].Contains(currentPreservativeType))
        {
            foodPreservativesUsed[currentFoodIndex].Add(currentPreservativeType);
        }
        
        foodPreservationValues[currentFoodIndex][currentPreservativeType] = currentSliderValue;
        
        // Check if food is fully preserved (all required preservatives applied)
        bool isFullyPreserved = IsFoodFullyPreserved(currentFoodIndex);
        
        if (isFullyPreserved)
        {
            foodCompleted[currentFoodIndex] = true;
            preservationStatusText.text = $"Successfully preserved with {GetPreservativeList(currentFoodIndex)}!";
            preservationStatusText.color = Color.green;
            
            // Disable all preservative buttons
            SetAllPreservativeButtonsInteractable(false);
            confirmButton.interactable = false;
            
            // Disable all sliders after completion
            SetAllSlidersInteractable(false);
            
            // Switch particle systems
            SwitchToPreservedParticles(currentFoodIndex);
            
            CheckAllFoodsCompleted();
        }
        else
        {
            // Food needs more preservatives
            preservationStatusText.text = $"✓ {currentPreservativeType} applied! {GetRemainingPreservativesText(currentFoodIndex)}";
            preservationStatusText.color = Color.green;
            
            // Keep confirm button enabled for next preservative
            preservationComplete = false;
            confirmButton.interactable = false;
            
            // Reset current preservation state for next attempt
            ResetPreservationStateForType(currentPreservativeType);
            
            // Update button icons for current food
            UpdateAllButtonIcons();
        }
    }
    
    private bool IsCorrectPreservativeForFood(int foodIndex, PreservativeType type)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;
        
        // For Fruit Juice (index 7), both Sodium Benzoate AND Ascorbic Acid are required
        if (foodIndex == 7) // Fruit Juice
        {
            return type == PreservativeType.SodiumBenzoate || type == PreservativeType.AscorbicAcid;
        }
        
        // For other foods, check the single required preservative
        return type == profile.preservativeType;
    }
    
    private bool IsFoodFullyPreserved(int foodIndex)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;
        
        // For Fruit Juice (index 7), need BOTH Sodium Benzoate AND Ascorbic Acid
        if (foodIndex == 7) // Fruit Juice
        {
            bool hasSodiumBenzoate = foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
            bool hasAscorbicAcid = foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid);
            
            return hasSodiumBenzoate && hasAscorbicAcid;
        }
        
        // For other foods, just need the single required preservative
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
        
        if (foodIndex == 7) // Fruit Juice
        {
            List<string> needed = new List<string>();
            
            if (!foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate))
                needed.Add("Sodium Benzoate");
                
            if (!foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid))
                needed.Add("Ascorbic Acid");
                
            if (needed.Count == 0) return "Fully preserved!";
            
            return $"Still need: {string.Join(" and ", needed)}";
        }
        
        // For single-preservative foods
        if (!foodPreservativesUsed[foodIndex].Contains(profile.preservativeType))
        {
            return $"Still need: {profile.PreservativeDisplayName}";
        }
        
        return "Fully preserved!";
    }
    
    private void UpdateAllButtonIcons()
    {
        SetButtonIcon(PreservativeType.AscorbicAcid, false);
        SetButtonIcon(PreservativeType.PotassiumSorbate, false);
        SetButtonIcon(PreservativeType.SodiumBenzoate, false);
    }
    
    private void SwitchToPreservedParticles(int foodIndex)
    {
        // Disable the regular food particle
        if (foodIndex >= 0 && foodIndex < foodParticles.Length && foodParticles[foodIndex] != null)
        {
            foodParticles[foodIndex].SetActive(false);
        }
        
        // Enable the preserved particle system
        if (FoodPreservedPS != null && foodIndex >= 0 && foodIndex < FoodPreservedPS.Length && FoodPreservedPS[foodIndex] != null)
        {
            FoodPreservedPS[foodIndex].SetActive(true);
        }
    }
    
    private void ClosePreservationPanel()
    {
        if (KAPanel != null)
        {
            KAPanel.SetActive(false);
            SwitchToPlayerCamera();
            
            // Always enable food particle if food is not preserved
            if (currentFoodIndex >= 0 && !foodCompleted[currentFoodIndex])
            {
                EnableFoodParticle(currentFoodIndex);
            }
            
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            
            // Don't reset preservation state if food is completed - keep slider values
            if (!foodCompleted[currentFoodIndex])
            {
                ResetPreservationState();
            }
            
            // Restore inspect button if player is still near
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(isPlayerNear);
            }
        }
    }
    
    private void UpdateFoodPanelContent(int foodIndex)
    {
        if (foodDatabase == null)
        {
            Debug.LogError("Food Database not assigned!");
            return;
        }
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        
        if (profile == null)
        {
            Debug.LogError($"No food profile found at index {foodIndex}");
            return;
        }
        
        if (foodNameText != null) foodNameText.text = profile.foodName;
        if (foodTypeText != null) foodTypeText.text = $"Type: {profile.foodType}";
        if (shelfLifeText != null) shelfLifeText.text = $"Shelf Life: {profile.shelfLife}";
        
        // Update separated preservative display elements
        UpdateSeparatedPreservativeDisplay(foodIndex, profile);
        
        if (threatsText != null) threatsText.text = $"Threats: {profile.threats}";
        if (contentsText != null) contentsText.text = $"Contents: {profile.contents}";
        if (hintText != null) hintText.text = profile.hint;
        if (foodIconImage != null && profile.foodIcon != null) foodIconImage.sprite = profile.foodIcon;
    }
    
    private void UpdateSeparatedPreservativeDisplay(int foodIndex, K3_FoodDatabase.FoodProfile profile)
    {
        // Update Required Preservative Text
        if (requiredPreservativeText != null)
        {
            if (foodIndex == 7) // Fruit Juice
            {
                requiredPreservativeText.text = "Required: Sodium Benzoate AND Ascorbic Acid";
            }
            else
            {
                requiredPreservativeText.text = $"Required: {profile.PreservativeDisplayName}";
            }
        }
        
        // Update Target Ranges Text
        if (targetRangesText != null)
        {
            if (foodIndex == 7) // Fruit Juice
            {
                targetRangesText.text = $"Sodium Benzoate Range: 50-60\n" +
                                       $"Ascorbic Acid Range: 40-50";
            }
            else
            {
                targetRangesText.text = $"Target Range: {profile.minSliderValue}-{profile.maxSliderValue}";
            }
        }
        
        // Update Collected Preservative Text
        if (collectedPreservativeText != null)
        {
            UpdateCollectedPreservativeText();
        }
    }
    
    private void UpdateCollectedPreservativeText()
    {
        // Check which preservatives have been collected
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        string collectedText = "<color=#000000>Collected Preservatives:</color>\n";
        bool anyAvailable = false;
        
        if (hasAscorbicAcid) 
        {
            collectedText += "• <color=#FF6B6B>Ascorbic Acid (Anti-Oxidant)</color>\n";
            anyAvailable = true;
        }
        if (hasPotassiumSorbate) 
        {
            collectedText += "• <color=#4CAF50>Potassium Sorbate (Anti-Microbial)</color>\n";
            anyAvailable = true;
        }
        if (hasSodiumBenzoate) 
        {
            collectedText += "• <color=#2196F3>Sodium Benzoate (Anti-Microbial)</color>\n";
            anyAvailable = true;
        }
        
        if (!anyAvailable)
        {
            collectedText = "<color=red>No preservatives collected yet! Find potions in the castle.</color>";
        }
        
        if (collectedPreservativeText != null)
        {
            collectedPreservativeText.text = collectedText;
        }
    }
    
    private void SetupPreservationSystem(int foodIndex)
    {
        if (foodDatabase == null) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return;
        
        // Reset UI state
        ResetAllSliders();
        
        // Check which preservatives have been collected
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        // Update collected preservative text
        UpdateCollectedPreservativeText();
        
        // Disable all sliders by default
        SetAllSlidersInteractable(false);
        
        // Setup buttons based on collection status and food completion
        bool isCompleted = foodCompleted[foodIndex];
        
        // For Fruit Juice, both Sodium Benzoate and Ascorbic Acid should be interactable
        if (foodIndex == 7)
        {
            if (ascorbicAcidButton != null)
            {
                bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.AscorbicAcid);
                ascorbicAcidButton.interactable = hasAscorbicAcid && !isCompleted && !alreadyUsed;
            }
            
            if (potassiumSorbateButton != null)
            {
                potassiumSorbateButton.interactable = false; // Potassium not needed for Fruit Juice
            }
            
            if (sodiumBenzoateButton != null)
            {
                bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
                sodiumBenzoateButton.interactable = hasSodiumBenzoate && !isCompleted && !alreadyUsed;
            }
        }
        else
        {
            // For single-preservative foods
            if (ascorbicAcidButton != null)
            {
                ascorbicAcidButton.interactable = hasAscorbicAcid && !isCompleted;
            }
            
            if (potassiumSorbateButton != null)
            {
                potassiumSorbateButton.interactable = hasPotassiumSorbate && !isCompleted;
            }
            
            if (sodiumBenzoateButton != null)
            {
                sodiumBenzoateButton.interactable = hasSodiumBenzoate && !isCompleted;
            }
        }
        
        // Update button icons for current food
        UpdateAllButtonIcons();
        
        // Restore slider values for already applied preservatives
        RestoreFoodState(foodIndex);
        
        // If food is already completed, show completion status
        if (isCompleted)
        {
            preservationStatusText.text = $"✓ Already preserved with {GetPreservativeList(foodIndex)}";
            preservationStatusText.color = Color.green;
            SetAllPreservativeButtonsInteractable(false);
            confirmButton.interactable = false;
        }
        else if (foodPreservativesUsed[foodIndex].Count > 0)
        {
            // Partially preserved (for Fruit Juice)
            preservationStatusText.text = $"Partially preserved. {GetRemainingPreservativesText(foodIndex)}";
            preservationStatusText.color = Color.yellow;
            confirmButton.interactable = false;
        }
        else
        {
            preservationStatusText.text = $"Select and hold preservative button to preserve";
            preservationStatusText.color = Color.white;
        }
        
        // Reset confirmation button state
        preservationComplete = false;
        confirmButton.interactable = false;
    }
    
    private void RestoreFoodState(int foodIndex)
    {
        // Restore slider values for already applied preservatives
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
    
    private bool HasCollectedPreservative(string preservativeID)
    {
        // First check info manager
        if (infoManager != null)
        {
            return infoManager.IsPreservativeCollected(preservativeID);
        }
        
        // Then check collection system
        if (collectionSystem != null)
        {
            return collectionSystem.HasCollectedPreservative(preservativeID);
        }
        
        // Fallback to PlayerPrefs
        return PlayerPrefs.GetInt($"Preservative_{preservativeID}_Collected", 0) == 1;
    }
    
    private void UpdateSliderUI()
    {
        if (currentActiveSlider != null)
        {
            // Update the slider value smoothly
            currentActiveSlider.value = currentSliderValue;
            
            // Update slider color based on value
            UpdateSliderColor(currentSliderValue, currentPreservativeType);
            
            // Update value text
            TMP_Text valueText = GetValueTextForPreservative(currentPreservativeType);
            if (valueText != null) valueText.text = $"{currentSliderValue:F0}";
        }
    }
    
    private void CheckPreservationResult()
    {
        if (currentFoodIndex == -1 || !isPreserving) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;
        
        // Check if value is in correct range
        bool isInRange = false;
        bool isCloseEnough = false;
        
        if (currentFoodIndex == 7) // Fruit Juice
        {
            if (currentPreservativeType == PreservativeType.SodiumBenzoate)
            {
                isInRange = currentSliderValue >= 50 && currentSliderValue <= 60;
                isCloseEnough = Mathf.Abs(currentSliderValue - 55) <= minRequiredAccuracy; // 55 is midpoint of 50-60
            }
            else if (currentPreservativeType == PreservativeType.AscorbicAcid)
            {
                isInRange = currentSliderValue >= 40 && currentSliderValue <= 50;
                isCloseEnough = Mathf.Abs(currentSliderValue - 45) <= minRequiredAccuracy; // 45 is midpoint of 40-50
            }
        }
        else
        {
            isInRange = profile.IsValueInRange(currentSliderValue);
            isCloseEnough = Mathf.Abs(currentSliderValue - ((profile.minSliderValue + profile.maxSliderValue) / 2)) <= minRequiredAccuracy;
        }
        
        if (isInRange || isCloseEnough)
        {
            // Check if this is a correct preservative for this food
            bool isCorrectPreservative = IsCorrectPreservativeForFood(currentFoodIndex, currentPreservativeType);
            bool alreadyApplied = foodPreservativesUsed[currentFoodIndex].Contains(currentPreservativeType);
            
            if (isCorrectPreservative && !alreadyApplied)
            {
                // Success with correct preservative
                string level = GetPreservationLevelDescription(currentSliderValue);
                preservationStatusText.text = $"Perfect! {currentPreservativeType} at {currentSliderValue:F0} is within target range!\n<color=#4CAF50>{level} preservation applied. Click CONFIRM to apply.</color>";
                preservationStatusText.color = Color.green;
                preservationComplete = true;
                confirmButton.interactable = true;
                
                PlaySound(successSound);
                StartCoroutine(SuccessFeedback());
                
                // Store the slider value for this food
                foodSliderValues[currentFoodIndex][currentPreservativeType] = currentSliderValue;
            }
            else if (alreadyApplied)
            {
                // This preservative already applied
                preservationStatusText.text = $"✗ {currentPreservativeType} already applied to this food!";
                preservationStatusText.color = Color.yellow;
                preservationComplete = false;
                confirmButton.interactable = false;
                
                // Switch button to retry mode for THIS food
                foodButtonRetryModes[currentFoodIndex][currentPreservativeType] = true;
                SetButtonIcon(currentPreservativeType, false);
                
                PlaySound(failureSound);
            }
            else
            {
                // Wrong preservative
                preservationStatusText.text = $"✗ Wrong preservative! {currentPreservativeType} is not needed for {profile.foodName}.";
                preservationStatusText.color = Color.red;
                preservationComplete = false;
                confirmButton.interactable = false;
                
                // Switch button to retry mode for THIS food
                foodButtonRetryModes[currentFoodIndex][currentPreservativeType] = true;
                SetButtonIcon(currentPreservativeType, false);
                
                PlaySound(failureSound);
                StartCoroutine(FailureFeedback());
            }
        }
        else
        {
            // Failed to hit target range
            preservationStatusText.text = $"✗ {currentSliderValue:F0} is not in target range. Try again!";
            preservationStatusText.color = Color.red;
            preservationComplete = false;
            confirmButton.interactable = false;
            
            // Switch button to retry mode for THIS food
            foodButtonRetryModes[currentFoodIndex][currentPreservativeType] = true;
            SetButtonIcon(currentPreservativeType, false);
            
            PlaySound(failureSound);
            StartCoroutine(FailureFeedback());
        }
        
        isPreserving = false;
        SetAllSlidersInteractable(false);
        
        // Keep the color after releasing
        UpdateSliderColor(currentSliderValue, currentPreservativeType);
    }
    
    private string GetPreservationLevelDescription(float value)
    {
        if (value <= 20) return "Minimal";
        if (value <= 50) return "Moderate";
        if (value <= 80) return "High";
        return "Very High";
    }
    
    private IEnumerator SuccessFeedback()
    {
        if (currentActiveSlider != null)
        {
            Image fillImage = GetFillImageForType(currentPreservativeType);
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
                
                UpdateSliderColor(currentSliderValue, currentPreservativeType);
            }
        }
    }
    
    private IEnumerator FailureFeedback()
    {
        if (currentActiveSlider != null)
        {
            Image fillImage = GetFillImageForType(currentPreservativeType);
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
                
                if (currentSliderValue > 0)
                {
                    UpdateSliderColor(currentSliderValue, currentPreservativeType);
                }
            }
        }
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
    
    private void ResetPreservationState()
    {
        isPreserving = false;
        isButtonHeld = false;
        preservationComplete = false;
        currentSliderValue = 0f;
        isIncreasing = true;
        holdDuration = 0f;
        currentSpeed = baseSliderSpeed;
        
        ResetAllSliders();
        SetAllSlidersInteractable(false);
        
        // Reset button scales
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
        // Reset only the state for the specific preservative type
        isPreserving = false;
        isButtonHeld = false;
        preservationComplete = false;
        
        // Reset only the slider for this type
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
    
    private void SetAllPreservativeButtonsInteractable(bool interactable)
    {
        if (ascorbicAcidButton != null) ascorbicAcidButton.interactable = interactable;
        if (potassiumSorbateButton != null) potassiumSorbateButton.interactable = interactable;
        if (sodiumBenzoateButton != null) sodiumBenzoateButton.interactable = interactable;
    }
    
    private void SetAllSlidersInteractable(bool interactable)
    {
        if (ascorbicAcidSlider != null) ascorbicAcidSlider.interactable = interactable;
        if (potassiumSorbateSlider != null) potassiumSorbateSlider.interactable = interactable;
        if (sodiumBenzoateSlider != null) sodiumBenzoateSlider.interactable = interactable;
    }
    
    private void CheckAllFoodsCompleted()
    {
        bool allCompleted = true;
        foreach (var kvp in foodCompleted)
        {
            if (!kvp.Value)
            {
                allCompleted = false;
                break;
            }
        }
        
        if (allCompleted)
        {
            Debug.Log("=== ALL FOODS PRESERVED! ===");
            // You could trigger a completion event here
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
    
    // SOUND METHODS
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Camera Control Methods
    private void SwitchToFoodCamera(int foodIndex)
    {
        if (playerFollowCamera != null) playerFollowCamera.gameObject.SetActive(false);
        if (foodIndex >= 0 && foodIndex < foodCameras.Length && foodCameras[foodIndex] != null)
            foodCameras[foodIndex].gameObject.SetActive(true);
    }
    
    private void SwitchToPlayerCamera()
    {
        DisableAllFoodCameras();
        if (playerFollowCamera != null) playerFollowCamera.gameObject.SetActive(true);
    }
    
    private void DisableAllFoodCameras()
    {
        foreach (CinemachineVirtualCamera cam in foodCameras)
            if (cam != null) cam.gameObject.SetActive(false);
    }
    
    // Particle System Control Methods
    private void DisableFoodParticle(int foodIndex)
    {
        if (foodIndex >= 0 && foodIndex < foodParticles.Length && foodParticles[foodIndex] != null)
            foodParticles[foodIndex].SetActive(false);
    }
    
    private void EnableFoodParticle(int foodIndex)
    {
        if (foodIndex < 0 || foodIndex >= foodParticles.Length) return;
        
        GameObject particle = foodParticles[foodIndex];
        if (particle != null && !foodCompleted[foodIndex])
        {
            particle.SetActive(true);
        }
    }
    
    // Public method to force enable particles (call this from other scripts if needed)
    public void EnsureFoodParticlesEnabled()
    {
        for (int i = 0; i < foodParticles.Length; i++)
        {
            if (foodParticles[i] != null && !foodCompleted[i])
            {
                foodParticles[i].SetActive(true);
            }
        }
    }
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== COLLECTION STATUS ===");
        Debug.Log($"Ascorbic Acid (ID 0) Collected: {HasCollectedPreservative("0")}");
        Debug.Log($"Potassium Sorbate (ID 1) Collected: {HasCollectedPreservative("1")}");
        Debug.Log($"Sodium Benzoate (ID 2) Collected: {HasCollectedPreservative("2")}");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (KAFoods == null) return;
        
        Gizmos.color = Color.yellow;
        foreach (GameObject food in KAFoods)
            if (food != null) Gizmos.DrawWireSphere(food.transform.position, interactionRange);
    }
}