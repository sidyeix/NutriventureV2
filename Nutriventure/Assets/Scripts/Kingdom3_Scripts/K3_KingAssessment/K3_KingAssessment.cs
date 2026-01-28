using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class K3_KingAssessment : MonoBehaviour
{
    // Inspector-assigned references
    [Header("Player Interaction")]
    [SerializeField] private GameObject player;
    [SerializeField] private float interactionRange = 3f;
    
    [Header("UI Elements")]
    [SerializeField] private Button inspectButton;
    [SerializeField] private GameObject KAPanel;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button confirmButton;
    
    [Header("Food Profile UI Elements")]
    [SerializeField] private TMP_Text foodNameText;
    [SerializeField] private TMP_Text foodTypeText;
    [SerializeField] private TMP_Text shelfLifeText;
    [SerializeField] private TMP_Text threatsText;
    [SerializeField] private TMP_Text contentsText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image foodIconImage;
    
    [Header("Preservative Display Elements")]
    [SerializeField] private TMP_Text requiredPreservativeText;
    [SerializeField] private TMP_Text targetRangesText;
    [SerializeField] private TMP_Text collectedPreservativeText;
    
    [Header("Database")]
    [SerializeField] private K3_FoodDatabase foodDatabase;

    [Header("Vibration Settings")]
    [SerializeField] private bool enableHapticFeedback = true;
    [SerializeField] private bool enableShakeAnimations = true;
    [SerializeField] private float panelCloseDelay = 2f;
    
    [Header("Collection System")]
    [SerializeField] private K3_CollectPreservatives collectionSystem;
    [SerializeField] private PreservativesInformationManager infoManager;
    
    [Header("Preservation System")]
    public K3_KingAS2 preservationSystem; // Reference to the new preservation system
    
    [Header("Objects to Disable")]
    [SerializeField] private GameObject[] objectsToDisable;
    
    [Header("Food Objects")]
    [SerializeField] private GameObject[] KAFoods;
    
    [Header("Food Cameras")]
    [SerializeField] private CinemachineVirtualCamera[] foodCameras;
    
    [Header("Food Particles")]
    [SerializeField] private GameObject[] foodParticles;
    
    [Header("Food Preserved Particles")]
    [SerializeField] private GameObject[] FoodPreservedPS;
    
    [Header("Main Camera")]
    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    
    [Header("Scoring System")]
    [SerializeField] private PreserviaScoringSystem scoringSystem;
    
    [Header("Collectibles UI")]
    [SerializeField] private GameObject collectiblesParent; // The "Collectibles" game object
    [SerializeField] private TMP_Text collectedPotionCountText; // "Collected Preservative: x/3"
    [SerializeField] private TMP_Text preservedFoodCountText; // "Preserved Food: x/8"
    
    private bool isPlayerNear = false;
    private int currentFoodIndex = -1;
    
    // Collection tracking
    private int collectedPotionsCount = 0;
    private int preservedFoodsCount = 0;
    
    // Food completion tracking
    private Dictionary<int, bool> foodCompleted = new Dictionary<int, bool>();
    private Dictionary<int, List<PreservativeType>> foodPreservativesUsed = new Dictionary<int, List<PreservativeType>>();
    private Dictionary<int, Dictionary<PreservativeType, float>> foodPreservationValues = new Dictionary<int, Dictionary<PreservativeType, float>>();
    
    // For polling preservative collection
    private int lastCollectedCount = 0;
    
    private void Start()
    {
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
        
        // Setup confirm button - connect to preservation system
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            confirmButton.interactable = false;
        }
        
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
        
        // Try to get scoring system if not assigned
        if (scoringSystem == null)
        {
            scoringSystem = FindObjectOfType<PreserviaScoringSystem>();
        }
        
        // Setup preservation system reference
        if (preservationSystem == null)
        {
            preservationSystem = FindObjectOfType<K3_KingAS2>();
            if (preservationSystem == null)
            {
                Debug.LogError("Preservation system (K3_KingAS2) not found! Please assign it in the inspector.");
            }
        }
        else
        {
            // Connect the confirm button to preservation system
            preservationSystem.SetConfirmButton(confirmButton);
        }
        
        // Initialize collectibles UI
        InitializeCollectiblesUI();
        
        // Check initial collection status
        UpdateInitialCollectionStatus();
        
        // Disable all food cameras initially
        DisableAllFoodCameras();
        
        // Initialize food completion tracking
        for (int i = 0; i < KAFoods.Length; i++)
        {
            foodCompleted[i] = false;
            foodPreservativesUsed[i] = new List<PreservativeType>();
            foodPreservationValues[i] = new Dictionary<PreservativeType, float>();
        }
        
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
    
    private void InitializeCollectiblesUI()
    {
        // Ensure collectibles UI is disabled at start
        if (collectiblesParent != null)
        {
            collectiblesParent.SetActive(false);
        }
        
        // Initialize the count texts
        UpdateCollectedPotionsCount();
        UpdatePreservedFoodsCount();
    }
    
    private void UpdateInitialCollectionStatus()
    {
        // Get initial collection count
        collectedPotionsCount = GetCurrentPreservativeCount();
        lastCollectedCount = collectedPotionsCount;
        
        // Activate collectibles UI if already collected some potions
        if (collectedPotionsCount > 0 && collectiblesParent != null)
        {
            collectiblesParent.SetActive(true);
            Debug.Log($"Collectibles UI activated - {collectedPotionsCount} preservatives already collected!");
        }
    }
    
    private int GetCurrentPreservativeCount()
    {
        int count = 0;
        if (HasCollectedPreservative("0")) count++;
        if (HasCollectedPreservative("1")) count++;
        if (HasCollectedPreservative("2")) count++;
        return count;
    }
    
    private void Update()
    {
        CheckPlayerProximity();
        
        // Poll for preservative collection changes
        CheckForPreservativeCollectionChanges();
    }
    
    private void CheckForPreservativeCollectionChanges()
    {
        int currentCount = GetCurrentPreservativeCount();
        
        if (currentCount != lastCollectedCount)
        {
            // Collection changed
            collectedPotionsCount = currentCount;
            
            // Activate collectibles UI when first potion is collected
            if (collectedPotionsCount == 1 && collectiblesParent != null && !collectiblesParent.activeSelf)
            {
                collectiblesParent.SetActive(true);
                Debug.Log("Collectibles UI activated - first preservative collected!");
            }
            
            UpdateCollectedPotionsCount();
            lastCollectedCount = currentCount;
            
            Debug.Log($"Preservative collection changed: {collectedPotionsCount}/3");
        }
    }
    
    private void UpdateCollectedPotionsCount()
    {
        if (collectedPotionCountText != null)
        {
            // Update from actual count
            collectedPotionsCount = GetCurrentPreservativeCount();
            collectedPotionCountText.text = $"Collected Potion: {collectedPotionsCount}/3";
        }
    }
    
    private void UpdatePreservedFoodsCount()
    {
        if (preservedFoodCountText != null)
        {
            // Count preserved foods
            int count = 0;
            foreach (var kvp in foodCompleted)
            {
                if (kvp.Value) count++;
            }
            
            preservedFoodsCount = count;
            preservedFoodCountText.text = $"Preserved Food: {preservedFoodsCount}/8";
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
        ClosePreservationPanel();
    }
    
    private void OnConfirmButtonClicked()
    {
        if (currentFoodIndex == -1 || preservationSystem == null) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(currentFoodIndex);
        if (profile == null) return;
        
        // Get current state from preservation system
        PreservativeType currentPreservativeType = preservationSystem.CurrentPreservativeType;
        float currentSliderValue = preservationSystem.CurrentSliderValue;
        
        // Check if this is a correct preservative for this food
        bool isCorrectPreservative = IsCorrectPreservativeForFood(currentFoodIndex, currentPreservativeType);
        
        if (!isCorrectPreservative)
        {
            preservationSystem.UpdateStatusText(
                $"{currentPreservativeType} is not the correct preservative for {profile.foodName}",
                Color.red
            );
            return;
        }
        
        // Store the successful preservation for this food
        if (!foodPreservativesUsed[currentFoodIndex].Contains(currentPreservativeType))
        {
            foodPreservativesUsed[currentFoodIndex].Add(currentPreservativeType);
            foodPreservationValues[currentFoodIndex][currentPreservativeType] = currentSliderValue;
        }
        
        // AWARD SCORING POINTS for successful preservation
        if (scoringSystem != null)
        {
            float targetMin = GetTargetMinForFood(currentFoodIndex, currentPreservativeType);
            float targetMax = GetTargetMaxForFood(currentFoodIndex, currentPreservativeType);
            bool isPerfect = IsValuePerfect(currentSliderValue, targetMin, targetMax);
            
            // Use the correct scoring method (6 parameters)
            scoringSystem.ManualFoodPreserved(
                profile.foodName,
                currentSliderValue,
                targetMin,
                targetMax,
                isCorrectPreservative,
                isPerfect
            );
            
            Debug.Log($"Scoring awarded for {profile.foodName} with {currentPreservativeType} at {currentSliderValue:F0}");
        }
        
        // Check if food is fully preserved (all required preservatives applied)
        bool isFullyPreserved = IsFoodFullyPreserved(currentFoodIndex);
        
        if (isFullyPreserved)
        {
            foodCompleted[currentFoodIndex] = true;
            
            // Update preserved foods count when food is fully preserved
            UpdatePreservedFoodsCount();
            
            // Update status through preservation system
            preservationSystem.UpdateStatusText(
                $"Successfully preserved with {GetPreservativeList(currentFoodIndex)}!",
                Color.green
            );
            
            // Disable all preservative buttons through preservation system
            preservationSystem.SetAllButtonsInteractable(false);
            confirmButton.interactable = false;
            
            // Switch particle systems
            SwitchToPreservedParticles(currentFoodIndex);
            
            // Close the panel
            ClosePreservationPanel();
            
            CheckAllFoodsCompleted();
        }
        else
        {
            // Food needs more preservatives
            preservationSystem.UpdateStatusText(
                $"{currentPreservativeType} applied! {GetRemainingPreservativesText(currentFoodIndex)}",
                Color.green
            );
            
            // Reset for next attempt
            preservationSystem.ResetForNextAttempt(currentPreservativeType);
            
            // Update button states through preservation system
            preservationSystem.UpdateButtonStates(currentFoodIndex, foodPreservativesUsed[currentFoodIndex]);
            
            // Wait a moment then close to show feedback
            StartCoroutine(ClosePanelAfterFeedback());
        }
    }

    private IEnumerator ClosePanelAfterFeedback()
    {
        yield return new WaitForSeconds(0.2f); // Brief pause to see the feedback
        ClosePreservationPanel();
    }
    
    private float GetTargetMinForFood(int foodIndex, PreservativeType type)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return 0f;
        
        if (foodIndex == 7) // Fruit Juice
        {
            if (type == PreservativeType.SodiumBenzoate) return 50f;
            if (type == PreservativeType.AscorbicAcid) return 40f;
        }
        
        return profile.minSliderValue;
    }
    
    private float GetTargetMaxForFood(int foodIndex, PreservativeType type)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return 0f;
        
        if (foodIndex == 7) // Fruit Juice
        {
            if (type == PreservativeType.SodiumBenzoate) return 60f;
            if (type == PreservativeType.AscorbicAcid) return 50f;
        }
        
        return profile.maxSliderValue;
    }
    
    private bool IsValuePerfect(float value, float targetMin, float targetMax)
    {
        float targetCenter = (targetMin + targetMax) / 2f;
        float rangeWidth = targetMax - targetMin;
        float distanceFromCenter = Mathf.Abs(value - targetCenter);
        float accuracyPercent = Mathf.Clamp01(1f - (distanceFromCenter / (rangeWidth / 2f))) * 100f;
        
        return accuracyPercent >= 90f || (value >= targetMin && value <= targetMax);
    }
    
    private bool IsCorrectPreservativeForFood(int foodIndex, PreservativeType type)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;
        
        // For Fruit Juice (index 7), only Sodium Benzoate is required
        if (foodIndex == 7)
        {
            return type == PreservativeType.SodiumBenzoate;
        }
        
        // For other foods, check the single required preservative
        return type == profile.preservativeType;
    }
    
    private bool IsFoodFullyPreserved(int foodIndex)
    {
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return false;
        
        // For Fruit Juice (index 7), only need Sodium Benzoate
        if (foodIndex == 7)
        {
            return foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
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
        
        if (foodIndex == 7)
        {
            if (!foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate))
            {
                return $"Still need: Sodium Benzoate";
            }
            return "Fully preserved!";
        }
        
        if (!foodPreservativesUsed[foodIndex].Contains(profile.preservativeType))
        {
            return $"Still need: {profile.PreservativeDisplayName}";
        }
        
        return "Fully preserved!";
    }

    private void SwitchToPreservedParticles(int foodIndex)
    {
        if (foodIndex >= 0 && foodIndex < foodParticles.Length && foodParticles[foodIndex] != null)
        {
            foodParticles[foodIndex].SetActive(false);
        }
        
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
        if (foodTypeText != null) foodTypeText.text = $"<b>Type:</b> {profile.foodType}";
        if (shelfLifeText != null) shelfLifeText.text = $"<b>Shelf Life:</b> {profile.shelfLife}";
            
        UpdateSeparatedPreservativeDisplay(foodIndex, profile);
        
        if (threatsText != null) threatsText.text = $"<b>Threats:</b> {profile.threats}";
        if (contentsText != null) contentsText.text = $"<b>Contents:</b> {profile.contents}";
        if (hintText != null) hintText.text = profile.hint;
        if (foodIconImage != null && profile.foodIcon != null) foodIconImage.sprite = profile.foodIcon;
    }
    
    private void UpdateSeparatedPreservativeDisplay(int foodIndex, K3_FoodDatabase.FoodProfile profile)
    {
        if (requiredPreservativeText != null)
        {
            if (foodIndex == 7)
            {
                // UPDATE: Fruit juice only requires Sodium Benzoate
                requiredPreservativeText.text = "<b>Required:</b> Sodium Benzoate";
            }
            else
            {
                requiredPreservativeText.text = $"<b>Required:</b> {profile.PreservativeDisplayName}";
            }
        }
        
        if (targetRangesText != null)
        {
            if (foodIndex == 7)
            {
                // UPDATE: Only show Sodium Benzoate range
                targetRangesText.text = $"<b>Sodium Benzoate Range:</b> 50-60";
            }
            else
            {
                targetRangesText.text = $"<b>Target Range:</b> {profile.minSliderValue}-{profile.maxSliderValue}";
            }
        }
        
        if (collectedPreservativeText != null)
        {
            UpdateCollectedPreservativeText();
        }
    }
        
    private void UpdateCollectedPreservativeText()
    {
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        string collectedText = "<b>Collected Potion:</b>\n";
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
        if (foodDatabase == null || preservationSystem == null) return;
        
        K3_FoodDatabase.FoodProfile profile = foodDatabase.GetFoodProfile(foodIndex);
        if (profile == null) return;
        
        // Update collection count before setup
        UpdateCollectedPotionsCount();
        
        // Setup the preservation system for this food
        preservationSystem.SetupForFood(
            foodIndex, 
            foodCompleted[foodIndex], 
            foodPreservativesUsed[foodIndex], 
            foodPreservationValues[foodIndex]
        );
        
        // Check which preservatives have been collected
        bool hasAscorbicAcid = HasCollectedPreservative("0");
        bool hasPotassiumSorbate = HasCollectedPreservative("1");
        bool hasSodiumBenzoate = HasCollectedPreservative("2");
        
        UpdateCollectedPreservativeText();
        
        bool isCompleted = foodCompleted[foodIndex];
        
        // Setup button interactability - UPDATED FOR SINGLE PRESERVATIVE
        if (foodIndex == 7)
        {
            // Fruit Juice only needs Sodium Benzoate
            if (hasSodiumBenzoate)
            {
                bool alreadyUsed = foodPreservativesUsed[foodIndex].Contains(PreservativeType.SodiumBenzoate);
                preservationSystem.SetButtonInteractable(PreservativeType.SodiumBenzoate, !isCompleted && !alreadyUsed);
            }
            
            // Disable the other buttons for Fruit Juice
            preservationSystem.SetButtonInteractable(PreservativeType.AscorbicAcid, false);
            preservationSystem.SetButtonInteractable(PreservativeType.PotassiumSorbate, false);
        }
        else
        {
            preservationSystem.SetButtonInteractable(PreservativeType.AscorbicAcid, hasAscorbicAcid && !isCompleted);
            preservationSystem.SetButtonInteractable(PreservativeType.PotassiumSorbate, hasPotassiumSorbate && !isCompleted);
            preservationSystem.SetButtonInteractable(PreservativeType.SodiumBenzoate, hasSodiumBenzoate && !isCompleted);
        }
        
        // Reset confirmation button state
        confirmButton.interactable = false;
    }
    
    private bool HasCollectedPreservative(string preservativeID)
    {
        if (infoManager != null)
        {
            return infoManager.IsPreservativeCollected(preservativeID);
        }
        
        if (collectionSystem != null)
        {
            return collectionSystem.HasCollectedPreservative(preservativeID);
        }
        
        return PlayerPrefs.GetInt($"Preservative_{preservativeID}_Collected", 0) == 1;
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
            
            if (scoringSystem != null)
            {
                scoringSystem.AwardFullCompletionBonus();
            }
            
            // Update preserved foods count one final time
            UpdatePreservedFoodsCount();
        }
    }
    
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
    
    public bool IsFoodPreserved(int foodIndex)
    {
        return foodCompleted.ContainsKey(foodIndex) && foodCompleted[foodIndex];
    }
    
    public List<PreservativeType> GetUsedPreservatives(int foodIndex)
    {
        return foodPreservativesUsed.ContainsKey(foodIndex) ? foodPreservativesUsed[foodIndex] : new List<PreservativeType>();
    }
    
    public float GetPreservationValue(int foodIndex, PreservativeType type)
    {
        if (foodPreservationValues.ContainsKey(foodIndex) && foodPreservationValues[foodIndex].ContainsKey(type))
        {
            return foodPreservationValues[foodIndex][type];
        }
        return 0f;
    }
    
    public void ForceUpdateCollectiblesUI()
    {
        UpdateCollectedPotionsCount();
        UpdatePreservedFoodsCount();
    }
    
    [ContextMenu("Debug Collection Status")]
    public void DebugCollectionStatus()
    {
        Debug.Log($"=== COLLECTION STATUS ===");
        Debug.Log($"Ascorbic Acid (ID 0) Collected: {HasCollectedPreservative("0")}");
        Debug.Log($"Potassium Sorbate (ID 1) Collected: {HasCollectedPreservative("1")}");
        Debug.Log($"Sodium Benzoate (ID 2) Collected: {HasCollectedPreservative("2")}");
        Debug.Log($"Collected Potions Count: {collectedPotionsCount}/3");
        Debug.Log($"Preserved Foods Count: {preservedFoodsCount}/8");
        Debug.Log($"Collectibles UI Active: {collectiblesParent != null && collectiblesParent.activeSelf}");
    }
    
    [ContextMenu("Debug Food Completion Status")]
    public void DebugFoodCompletionStatus()
    {
        Debug.Log($"=== FOOD COMPLETION STATUS ===");
        for (int i = 0; i < KAFoods.Length; i++)
        {
            bool completed = foodCompleted.ContainsKey(i) && foodCompleted[i];
            string preservatives = foodPreservativesUsed.ContainsKey(i) ? 
                GetPreservativeList(i) : "none";
            
            Debug.Log($"Food {i}: {(completed ? "PRESERVED" : "Not preserved")} - Preservatives: {preservatives}");
        }
        Debug.Log($"Total preserved: {preservedFoodsCount}/8");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (KAFoods == null) return;
        
        Gizmos.color = Color.yellow;
        foreach (GameObject food in KAFoods)
            if (food != null) Gizmos.DrawWireSphere(food.transform.position, interactionRange);
    }

    public int GetPreservedFoodCount()
    {
        int count = 0;
        foreach (var kvp in foodCompleted)
        {
            if (kvp.Value) count++;
        }
        return count;
    }
}