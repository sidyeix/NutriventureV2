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
    [SerializeField] private TMP_Text preservativeText;
    [SerializeField] private TMP_Text threatsText;
    [SerializeField] private TMP_Text contentsText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image foodIconImage;
    
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
    
    [Header("Main Camera")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    
    [Header("Preservation Settings")]
    [SerializeField] private float baseSliderSpeed = 30f;
    [SerializeField] private float maxSliderSpeed = 120f;
    [SerializeField] private float speedIncreaseRate = 0.5f;
    [SerializeField] private float minRequiredAccuracy = 10f;
    
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
    
    // Food completion tracking
    private Dictionary<int, bool> foodCompleted = new Dictionary<int, bool>();
    private Dictionary<int, PreservativeType> foodPreservativeUsed = new Dictionary<int, PreservativeType>();
    private Dictionary<int, float> foodPreservationValue = new Dictionary<int, float>();
    
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
    private TMP_Text targetRangeText;
    private TMP_Text currentValueText;
    private TMP_Text preservationStatusText;
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
        public TMP_Text targetRangeText;
        public TMP_Text currentValueText;
        public TMP_Text preservationStatusText;
        
        [Header("Slider Fill Images")]
        public Image ascorbicAcidFillImage;
        public Image potassiumSorbateFillImage;
        public Image sodiumBenzoateFillImage;
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
        if (collectionSystem != null)
        {
            collectionSystem = FindObjectOfType<K3_CollectPreservatives>();
        }
        
        // Try to get info manager if not assigned
        if (infoManager != null)
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
        }
        
        // Initialize all sliders
        InitializeAllSliders();
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
            
            targetRangeText = preservationUISettings.targetRangeText;
            currentValueText = preservationUISettings.currentValueText;
            preservationStatusText = preservationUISettings.preservationStatusText;
            
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
    
    private void OnButtonPressed(PreservativeType type, Slider slider)
    {
        if (isPreserving) return;
        if (currentFoodIndex == -1) return;
        if (foodCompleted[currentFoodIndex]) return;
        if (!HasCollectedPreservative(GetPreservativeID(type))) return;

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

        HighlightSelectedButton(type);

        preservationStatusText.text = $"Holding {type}… release when in target range!";
        preservationStatusText.color = Color.yellow;
        
        UpdateSliderUI(); // Initialize UI with 0 value
    }
    
    private void OnButtonReleased()
    {
        if (!isPreserving || !isButtonHeld) return;

        isButtonHeld = false;
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
            
            // BRIGHTER COLORS as requested
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
        if (currentFoodIndex == -1 || !preservationComplete) return;
        
        // Play button click sound
        PlaySound(buttonClickSound);
        
        foodCompleted[currentFoodIndex] = true;
        foodPreservativeUsed[currentFoodIndex] = currentPreservativeType;
        foodPreservationValue[currentFoodIndex] = currentSliderValue;
        
        preservationStatusText.text = $"✓ Preserved with {currentPreservativeType} ({currentSliderValue:F0})";
        preservationStatusText.color = Color.green;
        
        SetAllPreservativeButtonsInteractable(false);
        confirmButton.interactable = false;
        
        // Disable all sliders after completion
        SetAllSlidersInteractable(false);
        
        CheckAllFoodsCompleted();
    }
    
    private void ClosePreservationPanel()
    {
        if (KAPanel != null)
        {
            KAPanel.SetActive(false);
            SwitchToPlayerCamera();
            EnableFoodParticle(currentFoodIndex);
            
            foreach (GameObject obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
            
            ResetPreservationState();
            
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
        if (preservativeText != null) preservativeText.text = $"Preservative: {profile.PreservativeDisplayName}";
        if (threatsText != null) threatsText.text = $"Threats: {profile.threats}";
        if (contentsText != null) contentsText.text = $"Contents: {profile.contents}";
        if (hintText != null) hintText.text = profile.hint;
        if (foodIconImage != null && profile.foodIcon != null) foodIconImage.sprite = profile.foodIcon;
        if (targetRangeText != null) targetRangeText.text = $"Target: {profile.minSliderValue}-{profile.maxSliderValue}";
    }
    
    private void SetupPreservationSystem(int foodIndex)
    {
        if (foodDatabase == null) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return;
        
        // Reset sliders
        ResetAllSliders();
        
        // Check which preservatives have been collected
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        // Update preservative text
        UpdatePreservativeText(hasAscorbicAcid, hasPotassiumSorbate, hasSodiumBenzoate);
        
        // Disable all sliders by default
        SetAllSlidersInteractable(false);
        
        // Setup buttons based on collection status - BUTTONS LOCKED WHEN NOT COLLECTED
        if (ascorbicAcidButton != null)
        {
            ascorbicAcidButton.interactable = hasAscorbicAcid;
            // Use brighter colors for disabled state
            ascorbicAcidButton.GetComponent<Image>().color = hasAscorbicAcid ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
        }
        
        if (potassiumSorbateButton != null)
        {
            potassiumSorbateButton.interactable = hasPotassiumSorbate;
            potassiumSorbateButton.GetComponent<Image>().color = hasPotassiumSorbate ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
        }
        
        if (sodiumBenzoateButton != null)
        {
            sodiumBenzoateButton.interactable = hasSodiumBenzoate;
            sodiumBenzoateButton.GetComponent<Image>().color = hasSodiumBenzoate ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f);
        }
        
        // If food is already completed, show completion status
        if (foodCompleted[foodIndex])
        {
            preservationStatusText.text = $"✓ Already preserved with {foodPreservativeUsed[foodIndex]} ({foodPreservationValue[foodIndex]:F0})";
            preservationStatusText.color = Color.green;
            SetAllPreservativeButtonsInteractable(false);
        }
        else
        {
            preservationStatusText.text = "Hold a collected preservative button to start preservation";
            preservationStatusText.color = Color.white;
        }
        
        // Reset confirmation button
        confirmButton.interactable = false;
        preservationComplete = false;
    }
    
    private void UpdatePreservativeText(bool hasAscorbic, bool hasPotassium, bool hasSodium)
    {
        string availableText = "\n<color=#FFD700>Available Preservatives:</color>\n";
        bool anyAvailable = false;
        
        if (hasAscorbic) 
        {
            availableText += "• <color=#FF6B6B>Ascorbic Acid (Anti-Oxidant)</color>\n";
            anyAvailable = true;
        }
        if (hasPotassium) 
        {
            availableText += "• <color=#4CAF50>Potassium Sorbate (Anti-Microbial)</color>\n";
            anyAvailable = true;
        }
        if (hasSodium) 
        {
            availableText += "• <color=#2196F3>Sodium Benzoate (Anti-Microbial)</color>\n";
            anyAvailable = true;
        }
        
        if (!anyAvailable)
        {
            preservativeText.text += "\n<color=red>No preservatives collected yet! Find potions in the castle.</color>";
        }
        else
        {
            preservativeText.text += availableText;
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
            
            // Update current value text
            if (currentValueText != null) currentValueText.text = $"Current: {currentSliderValue:F0}";
            
            // Visual feedback based on target range
            K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
            if (profile != null)
            {
                bool inRange = profile.IsValueInRange(currentSliderValue);
                currentValueText.color = inRange ? Color.green : Color.red;
            }
        }
    }
    
    private void CheckPreservationResult()
    {
        if (currentFoodIndex == -1 || !isPreserving) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;
        
        bool isInRange = profile.IsValueInRange(currentSliderValue);
        bool isCloseEnough = Mathf.Abs(currentSliderValue - ((profile.minSliderValue + profile.maxSliderValue) / 2)) <= minRequiredAccuracy;
        
        if (isInRange || isCloseEnough)
        {
            // Success!
            string level = GetPreservationLevelDescription(currentSliderValue);
            preservationStatusText.text = $"✓ Perfect! {currentPreservativeType} at {currentSliderValue:F0} is within target range!\n<color=#4CAF50>{level} preservation applied.</color>";
            preservationStatusText.color = Color.green;
            preservationComplete = true;
            confirmButton.interactable = true;
            
            PlaySound(successSound);
            StartCoroutine(SuccessFeedback());
        }
        else
        {
            // Failed
            preservationStatusText.text = $"✗ {currentSliderValue:F0} is not in target range ({profile.minSliderValue}-{profile.maxSliderValue}). Try again!";
            preservationStatusText.color = Color.red;
            preservationComplete = false;
            confirmButton.interactable = false;
            
            PlaySound(failureSound);
            StartCoroutine(FailureFeedback());
        }
        
        isPreserving = false;
        SetAllSlidersInteractable(false);
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
                
                // Restore gradient color
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
                
                // Restore gradient color
                UpdateSliderColor(currentSliderValue, currentPreservativeType);
            }
        }
    }
    
    private void HighlightSelectedButton(PreservativeType type)
    {
        ResetButtonColors();
        
        Button selectedButton = GetButtonForPreservative(type);
        if (selectedButton != null && selectedButton.interactable)
        {
            selectedButton.GetComponent<Image>().color = Color.yellow;
        }
    }
    
    private void ResetButtonColors()
    {
        if (ascorbicAcidButton != null && ascorbicAcidButton.interactable)
            ascorbicAcidButton.GetComponent<Image>().color = Color.white;
        
        if (potassiumSorbateButton != null && potassiumSorbateButton.interactable)
            potassiumSorbateButton.GetComponent<Image>().color = Color.white;
        
        if (sodiumBenzoateButton != null && sodiumBenzoateButton.interactable)
            sodiumBenzoateButton.GetComponent<Image>().color = Color.white;
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
        
        if (currentValueText != null) currentValueText.text = "Current: 0";
        currentValueText.color = Color.white;
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
        
        ResetButtonColors();
        ResetAllSliders();
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
        if (foodIndex >= 0 && foodIndex < foodParticles.Length && foodParticles[foodIndex] != null)
            foodParticles[foodIndex].SetActive(true);
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