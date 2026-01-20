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
    [SerializeField] private float sliderIncreaseSpeed = 30f;
    [SerializeField] private float sliderDecreaseSpeed = 10f;
    [SerializeField] private float minRequiredAccuracy = 10f;
    
    private bool isPlayerNear = false;
    private int currentFoodIndex = -1;
    
    // Preservation state
    private bool isPreserving = false;
    private PreservativeType currentPreservativeType;
    private Slider currentActiveSlider;
    private float currentSliderValue = 0f;
    private bool isButtonHeld = false;
    private bool preservationComplete = false;
    
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
        if (collectionSystem == null)
        {
            collectionSystem = FindObjectOfType<K3_CollectPreservatives>();
        }
        
        // Try to get info manager if not assigned
        if (infoManager == null)
        {
            infoManager = FindObjectOfType<PreservativesInformationManager>();
        }
        
        // Disable all food cameras initially
        DisableAllFoodCameras();
        
        // Initialize food completion tracking
        for (int i = 0; i < KAFoods.Length; i++)
        {
            foodCompleted[i] = false;
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
            
            targetRangeText = preservationUISettings.targetRangeText;
            currentValueText = preservationUISettings.currentValueText;
            preservationStatusText = preservationUISettings.preservationStatusText;
            
            ascorbicAcidFillImage = preservationUISettings.ascorbicAcidFillImage;
            potassiumSorbateFillImage = preservationUISettings.potassiumSorbateFillImage;
            sodiumBenzoateFillImage = preservationUISettings.sodiumBenzoateFillImage;
        }
    }
    
    private void SetupPreservativeButtons()
    {
        // Clear existing listeners first
        if (ascorbicAcidButton != null)
        {
            ascorbicAcidButton.onClick.RemoveAllListeners();
            ascorbicAcidButton.onClick.AddListener(() => StartPreserving(PreservativeType.AscorbicAcid, ascorbicAcidSlider));
            SetupButtonHoldEvents(ascorbicAcidButton, PreservativeType.AscorbicAcid, ascorbicAcidSlider);
        }
        
        if (potassiumSorbateButton != null)
        {
            potassiumSorbateButton.onClick.RemoveAllListeners();
            potassiumSorbateButton.onClick.AddListener(() => StartPreserving(PreservativeType.PotassiumSorbate, potassiumSorbateSlider));
            SetupButtonHoldEvents(potassiumSorbateButton, PreservativeType.PotassiumSorbate, potassiumSorbateSlider);
        }
        
        if (sodiumBenzoateButton != null)
        {
            sodiumBenzoateButton.onClick.RemoveAllListeners();
            sodiumBenzoateButton.onClick.AddListener(() => StartPreserving(PreservativeType.SodiumBenzoate, sodiumBenzoateSlider));
            SetupButtonHoldEvents(sodiumBenzoateButton, PreservativeType.SodiumBenzoate, sodiumBenzoateSlider);
        }
    }
    
    private void SetupButtonHoldEvents(Button button, PreservativeType type, Slider slider)
    {
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
    }
    
    private void StartPreserving(PreservativeType type, Slider slider)
    {
        if (isPreserving || currentFoodIndex == -1 || foodCompleted[currentFoodIndex]) return;
        
        // Convert type to ID based on your system (0, 1, 2)
        string preservativeID = GetPreservativeID(type);
        
        // Check if this preservative is collected
        if (!HasCollectedPreservative(preservativeID))
        {
            preservationStatusText.text = $"<color=red>{type} not collected yet! Find the potion first.</color>";
            preservationStatusText.color = Color.red;
            return;
        }
        
        // Start preserving with selected type
        isPreserving = true;
        currentPreservativeType = type;
        currentActiveSlider = slider;
        currentSliderValue = 0f;
        
        // Highlight the selected button
        HighlightSelectedButton(type);
        
        // Update instruction text
        preservationStatusText.text = $"Hold {type} button to increase level. Release at target range!";
        preservationStatusText.color = Color.yellow;
        
        // Update slider color to initial value
        UpdateSliderColor(currentSliderValue, type);
        
        Debug.Log($"Started preserving with {type}. Hold button to increase slider.");
    }
    
    private void OnButtonPressed(PreservativeType type, Slider slider)
    {
        // Only respond if this is the currently selected preservative
        if (isPreserving && currentPreservativeType == type && currentActiveSlider == slider)
        {
            isButtonHeld = true;
            Debug.Log($"Button pressed for {type}. Slider increasing...");
        }
    }
    
    private void OnButtonReleased()
    {
        if (isPreserving && isButtonHeld)
        {
            isButtonHeld = false;
            Debug.Log($"Button released for {currentPreservativeType}. Checking result...");
            CheckPreservationResult();
        }
    }
    
    private string GetPreservativeID(PreservativeType type)
    {
        // Convert PreservativeType to the ID format used in your K3_PreservativeData
        switch (type)
        {
            case PreservativeType.AscorbicAcid: return "0";     // Ascorbic Acid
            case PreservativeType.PotassiumSorbate: return "1"; // Potassium Sorbate
            case PreservativeType.SodiumBenzoate: return "2";   // Sodium Benzoate
            default: return type.ToString();
        }
    }
    
    private void UpdateSliderColor(float value, PreservativeType type)
    {
        Image fillImage = GetFillImageForType(type);
        
        if (fillImage != null)
        {
            // Calculate color based on value ranges (1-20, 21-50, 51-80, 81-100)
            Color color = Color.white;
            
            switch (type)
            {
                case PreservativeType.AscorbicAcid: // Red gradient
                    if (value <= 20) color = new Color(1f, 0.8f, 0.8f);     // Light Red
                    else if (value <= 50) color = new Color(1f, 0.5f, 0.3f); // Orange-Red
                    else if (value <= 80) color = new Color(0.8f, 0.2f, 0.1f); // Dark Red
                    else color = new Color(0.6f, 0.1f, 0.05f); // Very Dark Red
                    break;
                    
                case PreservativeType.PotassiumSorbate: // Green gradient
                    if (value <= 20) color = new Color(0.8f, 1f, 0.8f);     // Light Green
                    else if (value <= 50) color = new Color(0.5f, 1f, 0.3f); // Lime Green
                    else if (value <= 80) color = new Color(0.2f, 0.7f, 0.1f); // Green
                    else color = new Color(0.1f, 0.4f, 0.05f); // Dark Green
                    break;
                    
                case PreservativeType.SodiumBenzoate: // Blue gradient
                    if (value <= 20) color = new Color(0.8f, 0.8f, 1f);     // Light Blue
                    else if (value <= 50) color = new Color(0.4f, 0.8f, 1f); // Cyan-Blue
                    else if (value <= 80) color = new Color(0.1f, 0.4f, 0.8f); // Blue
                    else color = new Color(0.05f, 0.2f, 0.6f); // Dark Blue
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
        
        if (isPlayerNear && currentFoodIndex != -1)
        {
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(true);
            }
        }
        else
        {
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(false);
            }
        }
        
        // Handle button hold for sliders
        if (isPreserving && isButtonHeld)
        {
            UpdateSliderValue();
        }
        else if (isPreserving && !isButtonHeld && currentSliderValue > 0)
        {
            currentSliderValue = Mathf.Max(0, currentSliderValue - sliderDecreaseSpeed * Time.deltaTime);
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
    }
    
    private void OnInspectButtonClicked()
    {
        if (currentFoodIndex == -1) return;
        
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
            
            if (inspectButton != null)
            {
                inspectButton.gameObject.SetActive(false);
            }
            
            SetupPreservationSystem(currentFoodIndex);
        }
    }
    
    private void OnExitButtonClicked()
    {
        ClosePreservationPanel();
    }
    
    private void OnConfirmButtonClicked()
    {
        if (currentFoodIndex == -1 || !preservationComplete) return;
        
        foodCompleted[currentFoodIndex] = true;
        foodPreservativeUsed[currentFoodIndex] = currentPreservativeType;
        foodPreservationValue[currentFoodIndex] = currentSliderValue;
        
        preservationStatusText.text = $"✓ Preserved with {currentPreservativeType} ({currentSliderValue:F0})";
        preservationStatusText.color = Color.green;
        
        SetAllPreservativeButtonsInteractable(false);
        confirmButton.interactable = false;
        
        CheckAllFoodsCompleted();
        
        Debug.Log($"Food {currentFoodIndex} preserved with {currentPreservativeType} at value {currentSliderValue:F0}");
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
        
        // Check which preservatives have been collected using CORRECT IDs (0, 1, 2)
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        Debug.Log($"Preservative Collection Status - Ascorbic Acid (0): {hasAscorbicAcid}, Potassium Sorbate (1): {hasPotassiumSorbate}, Sodium Benzoate (2): {hasSodiumBenzoate}");
        
        // Enable/disable buttons based on collection
        if (ascorbicAcidButton != null)
        {
            ascorbicAcidButton.interactable = hasAscorbicAcid;
            ascorbicAcidButton.GetComponent<Image>().color = hasAscorbicAcid ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        
        if (potassiumSorbateButton != null)
        {
            potassiumSorbateButton.interactable = hasPotassiumSorbate;
            potassiumSorbateButton.GetComponent<Image>().color = hasPotassiumSorbate ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        
        if (sodiumBenzoateButton != null)
        {
            sodiumBenzoateButton.interactable = hasSodiumBenzoate;
            sodiumBenzoateButton.GetComponent<Image>().color = hasSodiumBenzoate ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
        
        // Update preservative text
        UpdatePreservativeText(hasAscorbicAcid, hasPotassiumSorbate, hasSodiumBenzoate);
        
        // If food is already completed, show completion status
        if (foodCompleted[foodIndex])
        {
            preservationStatusText.text = $"✓ Already preserved with {foodPreservativeUsed[foodIndex]} ({foodPreservationValue[foodIndex]:F0})";
            preservationStatusText.color = Color.green;
            SetAllPreservativeButtonsInteractable(false);
        }
        else
        {
            preservationStatusText.text = "Select a collected preservative and hold its button";
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
            bool collected = infoManager.IsPreservativeCollected(preservativeID);
            Debug.Log($"InfoManager check for ID '{preservativeID}': {collected}");
            return collected;
        }
        
        // Then check collection system
        if (collectionSystem != null)
        {
            bool collected = collectionSystem.HasCollectedPreservative(preservativeID);
            Debug.Log($"CollectionSystem check for ID '{preservativeID}': {collected}");
            return collected;
        }
        
        // Fallback to PlayerPrefs
        bool fallbackCheck = PlayerPrefs.GetInt($"Preservative_{preservativeID}_Collected", 0) == 1;
        Debug.Log($"PlayerPrefs check for ID '{preservativeID}': {fallbackCheck}");
        return fallbackCheck;
    }
    
    private void UpdateSliderValue()
    {
        if (!isPreserving || currentActiveSlider == null) return;
        
        currentSliderValue = Mathf.Min(100, currentSliderValue + sliderIncreaseSpeed * Time.deltaTime);
        UpdateSliderUI();
        
        if (currentSliderValue >= 100)
        {
            isButtonHeld = false;
            CheckPreservationResult();
        }
    }
    
    private void UpdateSliderUI()
    {
        if (currentActiveSlider != null)
        {
            currentActiveSlider.value = currentSliderValue;
            
            // Update slider color
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
            
            StartCoroutine(SuccessFeedback());
        }
        else
        {
            // Failed
            preservationStatusText.text = $"✗ {currentSliderValue:F0} is not in target range ({profile.minSliderValue}-{profile.maxSliderValue}). Try again!";
            preservationStatusText.color = Color.red;
            preservationComplete = false;
            confirmButton.interactable = false;
            
            StartCoroutine(FailureFeedback());
        }
        
        isPreserving = false;
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
        
        ResetButtonColors();
        ResetAllSliders();
    }
    
    private void SetAllPreservativeButtonsInteractable(bool interactable)
    {
        if (ascorbicAcidButton != null) ascorbicAcidButton.interactable = interactable;
        if (potassiumSorbateButton != null) potassiumSorbateButton.interactable = interactable;
        if (sodiumBenzoateButton != null) sodiumBenzoateButton.interactable = interactable;
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
            // Trigger completion event
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
    
    // Debug method
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== COLLECTION STATUS ===");
        Debug.Log($"Ascorbic Acid (ID 0) Collected: {HasCollectedPreservative("0")}");
        Debug.Log($"Potassium Sorbate (ID 1) Collected: {HasCollectedPreservative("1")}");
        Debug.Log($"Sodium Benzoate (ID 2) Collected: {HasCollectedPreservative("2")}");
        
        if (infoManager != null)
        {
            Debug.Log($"Info Manager Found: Yes");
            Debug.Log($"Collected IDs: {string.Join(", ", infoManager.GetCollectedPreservativeIDs())}");
        }
        else
        {
            Debug.Log($"Info Manager Found: No");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (KAFoods == null) return;
        
        Gizmos.color = Color.yellow;
        foreach (GameObject food in KAFoods)
            if (food != null) Gizmos.DrawWireSphere(food.transform.position, interactionRange);
    }
}