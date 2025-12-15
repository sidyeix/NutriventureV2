using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class K2_QA1system : MonoBehaviour
{
    [Header("Trigger Settings")]
    public bool canTriggerMultipleTimes = false;
    public float cooldownTime = 5f;
    
    [Header("UI References")]
    public GameObject assessmentCanvas;
    public CanvasGroup canvasGroup; // For fade in/out animations
    
    [Header("Button Template Components - ASSIGN THESE")]
    public GameObject buttonTemplate; // The existing 1BTN GameObject
    public Transform buttonsParent; // 1QA_Buttons transform
    
    [Header("Confirm Button Component - ASSIGN THIS")]
    public Button confirmButton; // ConfirmBTN button component
    public TextMeshProUGUI confirmButtonText; // Text component inside ConfirmBTN
    
    [Header("Close Button Component - ASSIGN THIS")]
    public Button closeButton; // New button to close the panel
    public TextMeshProUGUI closeButtonText; // Text component inside Close button
    
    [Header("1BTN Child References - DRAG FROM YOUR 1BTN")]
    public RawImage templateContBG; // ContBG RawImage from 1BTN
    public TextMeshProUGUI templateFoodName; // FoodName TextMeshPro from 1BTN
    public RawImage templateFoodIcon; // FoodIcon RawImage from 1BTN
    public Button templateButton; // Button component from 1BTN
    
    [Header("UI Assets")]
    public Texture unselectedTexture;
    public Texture selectedTexture;
    
    [Header("Product Data")]
    public ProductInformationManager productManager;
    public ProductData productDatabase;
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip errorSound;
    public AudioClip successSound;
    public AudioClip uiShowSound;
    public AudioClip uiCloseSound; // New: Sound for closing UI
    
    [Header("Selection Settings")]
    public int maxSelections = 5; // Limit to select only 5 products
    
    [Header("Spawn Points - Assign 5 Transform positions")]
    public Transform[] spawnPoints = new Transform[5]; // 5 spawn points for products
    
    [Header("Animation Settings")]
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 5f; // Reduced intensity
    public float uiFadeInDuration = 0.5f;
    public float buttonStaggerDelay = 0.1f; // Delay between each button appearing
    public float enterAnimationDuration = 1f; // Total duration for button entrance animation
    
    [Header("Heart System")]
    public SugariaPlayerStat playerHealth; // Reference to player's health system
    public int errorHeartCost = 1; // Hearts lost per error
    
    [Header("Particle System")]
    public ParticleSystem triggerParticleSystem; // Particle system to disable on success
    
    [Header("Cursor Settings")]
    public bool keepCursorVisibleAfterSuccess = true; // NEW: Keep cursor visible after success
    
    [Header("Freeze Settings")]
    public bool freezeGameplay = false; // NEW: Option to freeze gameplay during assessment
    public bool freezePlayerControls = true; // NEW: Option to just freeze player controls
    
    [Header("Close Button Settings")]
    public bool enableCloseButton = true; // Whether close button is enabled
    public float closeCooldown = 3f; // Cooldown after closing before can trigger again
    
    // Runtime variables
    private List<ProductData.ProductInfo> collectedProducts = new List<ProductData.ProductInfo>();
    private List<string> selectedProductIDs = new List<string>();
    private Dictionary<string, AssessmentButton> productButtons = new Dictionary<string, AssessmentButton>();
    private bool hasTriggered = false;
    private bool isInCooldown = false;
    private bool isUIActive = false;
    private bool isProcessing = false; // To prevent multiple confirm clicks
    
    // Track spawned products and completion state
    private List<GameObject> spawnedProducts = new List<GameObject>();
    
    // Store original button positions for resetting after shake
    private Dictionary<GameObject, Vector3> originalButtonPositions = new Dictionary<GameObject, Vector3>();
    
    // Audio source for playing sounds while game is paused
    private AudioSource audioSource;
    
    // NEW: Track if we're in success state
    private bool isSuccessState = false;
    
    // Store original time scale
    private float originalTimeScale = 1f;
    
    // NEW: Track if panel was closed via close button
    private bool wasClosedByButton = false;
    
    void Start()
    {
        // Create an audio source for playing sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        if (assessmentCanvas != null)
        {
            assessmentCanvas.SetActive(false);
            
            // Get or add CanvasGroup for fade effects
            if (canvasGroup == null)
                canvasGroup = assessmentCanvas.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
                canvasGroup = assessmentCanvas.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0f; // Start invisible
        }
        
        // Setup confirm button if assigned
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
            confirmButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("Confirm Button is not assigned in Inspector!");
        }
        
        // Setup close button if assigned
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
            Debug.Log("Close button listener added");
        }
        else
        {
            Debug.LogWarning("Close Button is not assigned in Inspector!");
        }
        
        if (productManager == null)
            productManager = FindObjectOfType<ProductInformationManager>();
        
        if (productDatabase == null)
            productDatabase = Resources.Load<ProductData>("ProductData");
        
        // Find player health if not assigned
        if (playerHealth == null)
            playerHealth = FindObjectOfType<SugariaPlayerStat>();
        
        if (playerHealth != null)
        {
            Debug.Log("Player health system found");
        }
        else
        {
            Debug.LogWarning("Player health system not found! Error heart cost will not work.");
        }
        
        // Hide the template button initially
        if (buttonTemplate != null)
            buttonTemplate.SetActive(false);
        
        // Validate all components are assigned
        ValidateTemplateComponents();
        
        // Validate spawn points
        ValidateSpawnPoints();
    }
    
    private void ValidateTemplateComponents()
    {
        if (templateContBG == null) Debug.LogError("templateContBG is not assigned!");
        if (templateFoodName == null) Debug.LogError("templateFoodName is not assigned!");
        if (templateFoodIcon == null) Debug.LogError("templateFoodIcon is not assigned!");
        if (templateButton == null) Debug.LogError("templateButton is not assigned!");
        if (confirmButton == null) Debug.LogError("confirmButton is not assigned!");
        if (confirmButtonText == null) Debug.LogError("confirmButtonText is not assigned!");
        if (closeButton == null) Debug.LogError("closeButton is not assigned!");
        if (closeButtonText == null) Debug.LogWarning("closeButtonText is not assigned!");
    }
    
    private void ValidateSpawnPoints()
    {
        if (spawnPoints.Length != 5)
        {
            Debug.LogError($"Expected 5 spawn points, but found {spawnPoints.Length}!");
        }
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"Spawn point {i} is not assigned!");
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered && !isInCooldown && !isUIActive)
        {
            TriggerAssessment();
        }
    }
    
    void Update()
    {
        // Handle escape key to close UI (for testing/PC)
        if (isUIActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnCloseButtonClick();
        }
    }
    
    private void TriggerAssessment()
    {
        UpdateCollectedProductsList();
        
        if (collectedProducts.Count == 0)
        {
            Debug.Log("Cannot start assessment: No products collected yet");
            return;
        }
        
        StartCoroutine(ShowAssessmentUIWithAnimation());
        
        if (!canTriggerMultipleTimes)
        {
            hasTriggered = true;
        }
    }
    
    private IEnumerator ShowAssessmentUIWithAnimation()
    {
        isUIActive = true;
        isProcessing = false;
        isSuccessState = false; // Reset success state
        wasClosedByButton = false; // Reset close button state
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
        
        if (assessmentCanvas != null)
        {
            assessmentCanvas.SetActive(true);
            
            // Play UI show sound BEFORE potentially pausing game
            if (uiShowSound != null)
            {
                audioSource.PlayOneShot(uiShowSound);
            }
            
            // Fade in the canvas
            yield return StartCoroutine(FadeCanvas(0f, 1f, uiFadeInDuration));
        }
        
        CreateProductButtons();
        
        // Reset assessment state
        ResetAssessmentState();
        
        UpdateButtonInteractivity();
        
        // Animate buttons appearing
        yield return StartCoroutine(AnimateButtonsAppearing());
        
        // NEW: Only freeze time if enabled
        if (freezeGameplay)
        {
            Time.timeScale = 0f;
            Debug.Log("Gameplay frozen (Time.timeScale = 0)");
        }
        
        // NEW: Only disable player controls if enabled
        if (freezePlayerControls)
        {
            DisablePlayerControls();
            Debug.Log("Player controls disabled");
        }
        
        // NEW: Always show cursor when UI is active
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log($"Showing QA1 Assessment with {collectedProducts.Count} products (Max selections: {maxSelections})");
        Debug.Log($"Freeze Settings: Gameplay={freezeGameplay}, PlayerControls={freezePlayerControls}");
    }
    
    private IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        
        canvasGroup.alpha = endAlpha;
    }
    
    private IEnumerator AnimateButtonsAppearing()
    {
        // Store all button transforms
        List<Transform> buttonTransforms = new List<Transform>();
        foreach (var kvp in productButtons)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
                buttonTransforms.Add(kvp.Value.gameObject.transform);
                
                // Start with buttons scaled down and invisible
                var buttonRect = kvp.Value.gameObject.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localScale = Vector3.zero;
                }
            }
        }
        
        // Stagger the button animations
        for (int i = 0; i < buttonTransforms.Count; i++)
        {
            if (buttonTransforms[i] != null)
            {
                StartCoroutine(AnimateButtonAppear(buttonTransforms[i], i * buttonStaggerDelay));
            }
        }
        
        // Wait for all animations to complete
        yield return new WaitForSecondsRealtime(buttonTransforms.Count * buttonStaggerDelay + 0.3f);
    }
    
    private IEnumerator AnimateButtonAppear(Transform buttonTransform, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        var rectTransform = buttonTransform.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Bounce effect using easeOutBack
            float scale = EaseOutBack(t);
            rectTransform.localScale = new Vector3(scale, scale, scale);
            
            yield return null;
        }
        
        rectTransform.localScale = Vector3.one;
    }
    
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    private void UpdateCollectedProductsList()
    {
        collectedProducts.Clear();
        
        if (productManager == null || productDatabase == null)
            return;
        
        var collectedIDs = productManager.GetCollectedProductIDs();
        foreach (string productID in collectedIDs)
        {
            var productInfo = productDatabase.GetProductInfo(productID);
            if (productInfo != null)
            {
                collectedProducts.Add(productInfo);
            }
        }
    }
    
    private void CreateProductButtons()
    {
        // Clear existing dynamic buttons but keep the template
        foreach (Transform child in buttonsParent)
        {
            if (child.gameObject != buttonTemplate) // Don't destroy the template
                Destroy(child.gameObject);
        }
        productButtons.Clear();
        originalButtonPositions.Clear();
        
        // Check if we have all required components
        if (!ValidateAllComponents())
        {
            Debug.LogError("Missing required components!");
            return;
        }
        
        // Create buttons for each collected product by duplicating the template
        for (int i = 0; i < collectedProducts.Count; i++)
        {
            var product = collectedProducts[i];
            
            // Duplicate the template
            GameObject buttonObj = Instantiate(buttonTemplate, buttonsParent);
            buttonObj.name = $"Btn_{product.productID}";
            buttonObj.SetActive(true); // Make it visible
            
            // Find the child objects in the duplicated button
            Transform contBGTransform = FindChildByName(buttonObj.transform, "ContBG");
            Transform foodNameTransform = FindChildByName(buttonObj.transform, "FoodName");
            Transform foodIconTransform = FindChildByName(buttonObj.transform, "FoodIcon");
            
            if (contBGTransform == null || foodNameTransform == null || foodIconTransform == null)
            {
                Debug.LogError($"Duplicated button for {product.productID} is missing child objects!");
                Destroy(buttonObj);
                continue;
            }
            
            // Get components from the duplicated button
            RawImage contBGRawImage = contBGTransform.GetComponent<RawImage>();
            TextMeshProUGUI foodNameText = foodNameTransform.GetComponent<TextMeshProUGUI>();
            RawImage foodIconRawImage = foodIconTransform.GetComponent<RawImage>();
            Button buttonComp = buttonObj.GetComponent<Button>();
            
            // CRITICAL: Copy the EXACT properties from template components
            CopyComponentProperties(contBGRawImage, templateContBG);
            CopyComponentProperties(foodNameText, templateFoodName);
            CopyComponentProperties(foodIconRawImage, templateFoodIcon);
            CopyComponentProperties(buttonComp, templateButton);
            
            // Copy RectTransform properties to preserve size and position
            CopyRectTransform(contBGTransform.GetComponent<RectTransform>(), templateContBG.rectTransform);
            CopyRectTransform(foodNameTransform.GetComponent<RectTransform>(), templateFoodName.rectTransform);
            CopyRectTransform(foodIconTransform.GetComponent<RectTransform>(), templateFoodIcon.rectTransform);
            CopyRectTransform(buttonObj.GetComponent<RectTransform>(), buttonTemplate.GetComponent<RectTransform>());
            
            // Store original position
            originalButtonPositions[buttonObj] = buttonObj.transform.localPosition;
            
            // Add or get the AssessmentButton component
            AssessmentButton assessmentButton = buttonObj.GetComponent<AssessmentButton>();
            if (assessmentButton == null)
                assessmentButton = buttonObj.AddComponent<AssessmentButton>();
            
            // Initialize the button with product data
            assessmentButton.Initialize(
                product,
                contBGRawImage,
                foodNameText,
                foodIconRawImage,
                buttonComp,
                unselectedTexture,
                selectedTexture,
                OnProductButtonClick,
                templateFoodIcon
            );
            
            productButtons[product.productID] = assessmentButton;
            
            // Initially hide the button (it will be animated in later)
            buttonObj.transform.localScale = Vector3.zero;
        }
    }
    
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
        }
        return null;
    }
    
    private void CopyComponentProperties(Component target, Component source)
    {
        if (target == null || source == null) return;
        
        // Copy common properties based on component type
        if (target is RawImage targetImage && source is RawImage sourceImage)
        {
            targetImage.texture = sourceImage.texture;
            targetImage.color = sourceImage.color;
            targetImage.material = sourceImage.material;
            targetImage.raycastTarget = sourceImage.raycastTarget;
            targetImage.maskable = sourceImage.maskable;
        }
        else if (target is TextMeshProUGUI targetText && source is TextMeshProUGUI sourceText)
        {
            targetText.text = sourceText.text;
            targetText.color = sourceText.color;
            targetText.fontSize = sourceText.fontSize;
            targetText.fontStyle = sourceText.fontStyle;
            targetText.alignment = sourceText.alignment;
            targetText.enableAutoSizing = sourceText.enableAutoSizing;
        }
        else if (target is Button targetButton && source is Button sourceButton)
        {
            targetButton.interactable = sourceButton.interactable;
            targetButton.colors = sourceButton.colors;
            targetButton.spriteState = sourceButton.spriteState;
            targetButton.animationTriggers = sourceButton.animationTriggers;
            targetButton.navigation = sourceButton.navigation;
            targetButton.transition = sourceButton.transition;
        }
    }
    
    private void CopyRectTransform(RectTransform target, RectTransform source)
    {
        if (target == null || source == null) return;
        
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }
    
    private bool ValidateAllComponents()
    {
        bool isValid = true;
        
        if (buttonTemplate == null)
        {
            Debug.LogError("Button Template GameObject is not assigned!");
            isValid = false;
        }
        
        if (templateContBG == null)
        {
            Debug.LogError("templateContBG component is not assigned!");
            isValid = false;
        }
        
        if (templateFoodName == null)
        {
            Debug.LogError("templateFoodName component is not assigned!");
            isValid = false;
        }
        
        if (templateFoodIcon == null)
        {
            Debug.LogError("templateFoodIcon component is not assigned!");
            isValid = false;
        }
        
        if (templateButton == null)
        {
            Debug.LogError("templateButton component is not assigned!");
            isValid = false;
        }
        
        if (confirmButton == null)
        {
            Debug.LogError("confirmButton component is not assigned!");
            isValid = false;
        }
        
        if (confirmButtonText == null)
        {
            Debug.LogError("confirmButtonText component is not assigned!");
            isValid = false;
        }
        
        if (closeButton == null)
        {
            Debug.LogError("closeButton component is not assigned!");
            isValid = false;
        }
        
        return isValid;
    }
    
    private void OnProductButtonClick(string productID, bool isSelected)
    {
        // Play button click sound using the audio source (works whether paused or not)
        if (buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        if (isSelected)
        {
            // Check if we've reached the max selection limit
            if (selectedProductIDs.Count >= maxSelections)
            {
                // Can't select more, show feedback and don't add
                Debug.Log($"Maximum selection limit reached ({maxSelections}). Cannot select more products.");
                
                // Deselect the button that was just clicked
                if (productButtons.ContainsKey(productID))
                {
                    productButtons[productID].SetSelected(false);
                }
                return;
            }
            
            if (!selectedProductIDs.Contains(productID))
            {
                selectedProductIDs.Add(productID);
                Debug.Log($"Selected: {productID} ({selectedProductIDs.Count}/{maxSelections})");
            }
        }
        else
        {
            if (selectedProductIDs.Contains(productID))
            {
                selectedProductIDs.Remove(productID);
                Debug.Log($"Deselected: {productID} ({selectedProductIDs.Count}/{maxSelections})");
            }
        }
        
        UpdateConfirmButtonState();
        UpdateButtonInteractivity();
    }
    
    private void UpdateButtonInteractivity()
    {
        // Disable unselected buttons if max selections reached
        bool maxReached = selectedProductIDs.Count >= maxSelections;
        
        foreach (var kvp in productButtons)
        {
            string productID = kvp.Key;
            AssessmentButton button = kvp.Value;
            
            if (button != null)
            {
                // Get the Button component
                Button btnComponent = button.GetButtonComponent();
                if (btnComponent != null)
                {
                    // Disable button if:
                    // 1. Max selections reached
                    // 2. This button is NOT selected
                    btnComponent.interactable = !(maxReached && !button.IsSelected());
                }
            }
        }
    }
    
    private void UpdateConfirmButtonState()
    {
        if (confirmButton != null)
        {
            // Disable button when not exactly 5 products are selected
            confirmButton.interactable = selectedProductIDs.Count == maxSelections;
            
            // Just update the text content, don't search for component
            if (confirmButtonText != null)
            {
                if (selectedProductIDs.Count == maxSelections)
                {
                    confirmButtonText.text = $"CONFIRM ({selectedProductIDs.Count}/{maxSelections})";
                }
                else if (selectedProductIDs.Count > 0)
                {
                    confirmButtonText.text = $"SELECT MORE ({selectedProductIDs.Count}/{maxSelections})";
                }
                else
                {
                    confirmButtonText.text = $"SELECT PRODUCTS (Max: {maxSelections})";
                }
            }
        }
    }
    
    // NEW: Close button click handler
    private void OnCloseButtonClick()
    {
        if (!isUIActive || isProcessing || isSuccessState) return;
        
        // Play close sound
        if (uiCloseSound != null)
        {
            audioSource.PlayOneShot(uiCloseSound);
        }
        
        // Mark as closed by button
        wasClosedByButton = true;
        
        // Close the UI
        HideAssessmentUI();
    }
    
    private void OnConfirmButtonClick()
    {
        if (isProcessing || isSuccessState) return; // Prevent multiple clicks
        
        isProcessing = true;
        
        // Already validated in UpdateConfirmButtonState, but double-check
        if (selectedProductIDs.Count != maxSelections)
        {
            StartCoroutine(ShowError("Please select exactly 5 products!"));
            // FIXED: Reset isProcessing flag immediately so player can try again
            StartCoroutine(ResetProcessingFlagAfterDelay(0.1f));
            return;
        }
        
        // Check if all 5 are added sugar products
        bool allAddedSugar = CheckIfAllAddedSugar();
        
        if (allAddedSugar)
        {
            // SUCCESS: All 5 are added sugar
            StartCoroutine(ProcessSuccess());
        }
        else
        {
            // ERROR: At least one is natural sugar
            StartCoroutine(ShowError("One or more selections contain natural sugar!"));
            // FIXED: Reset processing flag so player can make more errors
            StartCoroutine(ResetProcessingFlagAfterDelay(0.1f));
        }
    }
    
    // NEW: Helper method to reset processing flag after a short delay
    private IEnumerator ResetProcessingFlagAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        isProcessing = false;
    }
    
    private bool CheckIfAllAddedSugar()
    {
        int addedSugarCount = 0;
        
        foreach (string productID in selectedProductIDs)
        {
            var product = productDatabase.GetProductInfo(productID);
            if (product != null && product.productType == ProductData.ProductType.AddedSugar)
            {
                addedSugarCount++;
            }
        }
        
        return addedSugarCount == maxSelections; // All 5 must be added sugar
    }
    
    private IEnumerator ProcessSuccess()
    {
        isSuccessState = true; // Set success state
        
        // Play success sound using audio source (works whether paused or not)
        if (successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }
        
        // Update confirm button text
        if (confirmButtonText != null)
        {
            confirmButtonText.text = "SUCCESS!";
            confirmButtonText.color = Color.green;
        }
        
        // Disable all buttons
        SetAllButtonsInteractable(false);
        
        // Also disable confirm button and close button
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
        
        if (closeButton != null)
        {
            closeButton.interactable = false;
        }
        
        // Wait a moment for user to see success message
        yield return new WaitForSecondsRealtime(1.0f);
        
        // Spawn the products (this happens in real-time, not paused)
        SpawnSelectedProducts();
        
        // Disable particle system if assigned
        DisableParticleSystem();
        
        // Fade out UI
        yield return StartCoroutine(FadeCanvas(1f, 0f, 0.5f));
        
        // Hide canvas
        assessmentCanvas.SetActive(false);
        
        // Resume game
        ResumeGameAfterSuccess();
        
        // Reset processing flag
        isProcessing = false;
    }
    
    private IEnumerator ShowError(string message)
    {
        // Deduct hearts for error - THIS NOW HAPPENS EVERY TIME
        DeductHeartsForError();
        
        // Play error sound using audio source (works whether paused or not)
        if (errorSound != null)
        {
            audioSource.PlayOneShot(errorSound);
        }
        
        // Store original text and color
        string originalText = confirmButtonText != null ? confirmButtonText.text : "";
        Color originalColor = confirmButtonText != null ? confirmButtonText.color : Color.white;
        
        // Show error message
        if (confirmButtonText != null)
        {
            confirmButtonText.text = message;
            confirmButtonText.color = Color.red;
        }
        
        // Disable confirm button temporarily during shake
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
        
        // Disable close button temporarily during shake
        if (closeButton != null)
        {
            closeButton.interactable = false;
        }
        
        // Shake all selected buttons
        yield return StartCoroutine(ShakeSelectedButtons());
        
        // Re-enable confirm button (only if we have exactly 5 selections)
        if (confirmButton != null)
        {
            confirmButton.interactable = selectedProductIDs.Count == maxSelections;
        }
        
        // Re-enable close button
        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
        
        // Reset confirm button text
        if (confirmButtonText != null)
        {
            confirmButtonText.text = originalText;
            confirmButtonText.color = originalColor;
        }
        
        // FIXED: Reset processing flag is now handled by ResetProcessingFlagAfterDelay
    }
    
    private void DeductHeartsForError()
    {
        if (playerHealth != null)
        {
            Debug.Log($"Deducting {errorHeartCost} heart(s) for incorrect selection");
            playerHealth.TakeDamage(errorHeartCost);
        }
        else
        {
            Debug.LogWarning("Player health system not found! Could not deduct hearts for error.");
        }
    }
    
    private void DisableParticleSystem()
    {
        if (triggerParticleSystem != null)
        {
            triggerParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Debug.Log("Particle system disabled on success");
        }
    }
    
    private IEnumerator ShakeSelectedButtons()
    {
        List<AssessmentButton> buttonsToShake = new List<AssessmentButton>();
        
        // Collect all selected buttons
        foreach (string productID in selectedProductIDs)
        {
            if (productButtons.ContainsKey(productID))
            {
                buttonsToShake.Add(productButtons[productID]);
            }
        }
        
        // Store original positions of RectTransforms
        Dictionary<RectTransform, Vector3> originalPositions = new Dictionary<RectTransform, Vector3>();
        foreach (var button in buttonsToShake)
        {
            if (button != null)
            {
                var rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    originalPositions[rectTransform] = rectTransform.anchoredPosition;
                }
            }
        }
        
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Calculate shake offset (using eased intensity)
            float progress = elapsed / shakeDuration;
            float shakeAmount = shakeIntensity * (1f - progress); // Fade out shake
            
            foreach (var button in buttonsToShake)
            {
                if (button != null)
                {
                    var rectTransform = button.GetComponent<RectTransform>();
                    if (rectTransform != null && originalPositions.ContainsKey(rectTransform))
                    {
                        // Add random offset to anchored position
                        Vector3 shakeOffset = new Vector3(
                            Random.Range(-shakeAmount, shakeAmount),
                            Random.Range(-shakeAmount, shakeAmount),
                            0
                        );
                        
                        rectTransform.anchoredPosition = originalPositions[rectTransform] + shakeOffset;
                    }
                }
            }
            
            yield return null;
        }
        
        // Reset positions
        foreach (var kvp in originalPositions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.anchoredPosition = kvp.Value;
            }
        }
    }
    
    private void SetAllButtonsInteractable(bool interactable)
    {
        foreach (var button in productButtons.Values)
        {
            Button btnComponent = button.GetButtonComponent();
            if (btnComponent != null)
            {
                btnComponent.interactable = interactable;
            }
        }
        
        if (confirmButton != null)
        {
            confirmButton.interactable = interactable;
        }
        
        if (closeButton != null)
        {
            closeButton.interactable = interactable;
        }
    }
    
        private void SpawnSelectedProducts()
    {
        Debug.Log($"Spawning {selectedProductIDs.Count} products at assigned spawn points");
        
        // Clear any previously spawned products (just in case)
        ClearSpawnedProducts();
        
        for (int i = 0; i < Mathf.Min(selectedProductIDs.Count, spawnPoints.Length); i++)
        {
            string productID = selectedProductIDs[i];
            var product = productDatabase.GetProductInfo(productID);
            
            if (product != null && product.productPrefab != null && spawnPoints[i] != null)
            {
                // Spawn the product at the assigned spawn point
                GameObject spawnedProduct = Instantiate(
                    product.productPrefab,
                    spawnPoints[i].position,
                    spawnPoints[i].rotation
                );
                
                // Tag for QA2 scanning - ADD THIS LINE
                spawnedProduct.tag = "QA2SpawnedProduct";
                
                // You might also want to keep the original tag for reference
                // Add a component to store the original product type
                QA2Scannable scannable = spawnedProduct.AddComponent<QA2Scannable>();
                scannable.productID = productID;
                scannable.productType = product.productType;
                
                // Add to spawned products list
                spawnedProducts.Add(spawnedProduct);
                
                Debug.Log($"Spawned {product.displayName} at spawn point {i} (Tagged for QA2)");
            }
            else
            {
                Debug.LogWarning($"Could not spawn product {productID} at spawn point {i}. " +
                            $"Product: {product != null}, Prefab: {product?.productPrefab != null}, " +
                            $"SpawnPoint: {spawnPoints[i] != null}");
            }
        }
        
        if (spawnedProducts.Count > 0)
        {
            Debug.Log($"Successfully spawned {spawnedProducts.Count} products for QA2 scanning");
        }
        else
        {
            Debug.LogError("Failed to spawn any products!");
        }
    }

    // Add this helper class at the end of your K2_QA1system script
    public class QA2Scannable : MonoBehaviour
    {
        public string productID;
        public ProductData.ProductType productType;
    }
    
    private void ClearSpawnedProducts()
    {
        foreach (GameObject product in spawnedProducts)
        {
            if (product != null)
            {
                Destroy(product);
            }
        }
        spawnedProducts.Clear();
    }
    
    private void HideAssessmentUI()
    {
        // Don't hide if we're in success state
        if (isSuccessState) return;
        
        isUIActive = false;
        isProcessing = false;
        
        // Start fade out animation
        if (assessmentCanvas != null && canvasGroup != null)
        {
            StartCoroutine(FadeAndHideUI());
        }
        else
        {
            if (assessmentCanvas != null)
                assessmentCanvas.SetActive(false);
            
            ResumeGame();
        }
    }
    
    private IEnumerator FadeAndHideUI()
    {
        // Fade out quickly
        yield return StartCoroutine(FadeCanvas(1f, 0f, 0.5f));
        
        // Hide canvas
        assessmentCanvas.SetActive(false);
        
        // Resume game
        ResumeGame();
    }
    
    private void ResumeGame()
    {
        // NEW: Only restore time scale if we froze it
        if (freezeGameplay)
        {
            Time.timeScale = originalTimeScale;
            Debug.Log($"Time scale restored to: {Time.timeScale}");
        }
        
        // NEW: Only enable player controls if we disabled them
        if (freezePlayerControls)
        {
            EnablePlayerControls();
            Debug.Log("Player controls re-enabled");
        }
        
        // NEW: Only hide cursor if not in success state or if keepCursorVisibleAfterSuccess is false
        if (!isSuccessState || !keepCursorVisibleAfterSuccess)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // Keep cursor visible after success
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // NEW: If closed by button, start the special cooldown
        if (wasClosedByButton && enableCloseButton)
        {
            StartCoroutine(StartCloseCooldown());
        }
        else if (canTriggerMultipleTimes && !isInCooldown)
        {
            StartCoroutine(StartCooldown());
        }
        
        Debug.Log("QA1 Assessment completed");
    }
    
    // NEW: Separate method for resuming after success
    private void ResumeGameAfterSuccess()
    {
        // NEW: Only restore time scale if we froze it
        if (freezeGameplay)
        {
            Time.timeScale = originalTimeScale;
            Debug.Log($"Time scale restored to: {Time.timeScale}");
        }
        
        // NEW: Only enable player controls if we disabled them
        if (freezePlayerControls)
        {
            EnablePlayerControls();
            Debug.Log("Player controls re-enabled");
        }
        
        // Handle cursor based on setting
        if (keepCursorVisibleAfterSuccess)
        {
            // Keep cursor visible after success
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        if (canTriggerMultipleTimes && !isInCooldown)
        {
            StartCoroutine(StartCooldown());
        }
        
        Debug.Log("QA1 Assessment SUCCESSFULLY completed");
    }
    
    private IEnumerator StartCooldown()
    {
        isInCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isInCooldown = false;
        Debug.Log("Cooldown finished");
    }
    
    // NEW: Special cooldown for close button
    private IEnumerator StartCloseCooldown()
    {
        isInCooldown = true;
        Debug.Log($"Close button cooldown started for {closeCooldown} seconds");
        yield return new WaitForSeconds(closeCooldown);
        isInCooldown = false;
        Debug.Log("Close button cooldown finished");
    }
    
    private void DisablePlayerControls()
    {
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = false;
        
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = false;
    }
    
    private void EnablePlayerControls()
    {
        MonoBehaviour movementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        if (movementScript != null)
            movementScript.enabled = true;
        
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = true;
    }
    
    private void ResetAllButtons()
    {
        foreach (var button in productButtons.Values)
        {
            button.SetSelected(false);
        }
        UpdateButtonInteractivity();
    }
    
    private void ResetAssessmentState()
    {
        selectedProductIDs.Clear();
        ResetAllButtons();
        UpdateConfirmButtonState();
        isProcessing = false; // Reset processing flag
        
        // Make sure confirm button is in correct state
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
        
        // Make sure close button is enabled
        if (closeButton != null && enableCloseButton)
        {
            closeButton.interactable = true;
        }
    }
    
    // Public method to close panel from external scripts
    public void ClosePanel()
    {
        OnCloseButtonClick();
    }
    
    // Enable/disable close button
    public void SetCloseButtonEnabled(bool enabled)
    {
        enableCloseButton = enabled;
        
        if (closeButton != null)
        {
            closeButton.interactable = enabled;
            closeButton.gameObject.SetActive(enabled);
        }
        
        Debug.Log($"Close button {(enabled ? "enabled" : "disabled")}");
    }
    
    // Set close cooldown time
    public void SetCloseCooldown(float cooldown)
    {
        closeCooldown = Mathf.Max(0.5f, cooldown); // Minimum 0.5 seconds
        Debug.Log($"Close button cooldown set to: {closeCooldown} seconds");
    }
    
    // AssessmentButton class
    public class AssessmentButton : MonoBehaviour
    {
        private RawImage contBG;
        private TextMeshProUGUI foodName;
        private RawImage foodIcon;
        private Button button;
        
        private ProductData.ProductInfo productInfo;
        private Texture unselectedTexture;
        private Texture selectedTexture;
        private System.Action<string, bool> onClickCallback;
        private bool isSelected = false;
        private RawImage templateFoodIcon; // Reference to template for preserving size
        
        public void Initialize(
            ProductData.ProductInfo product,
            RawImage contBGRawImage,
            TextMeshProUGUI foodNameText,
            RawImage foodIconRawImage,
            Button buttonComponent,
            Texture unselectedTex,
            Texture selectedTex,
            System.Action<string, bool> callback,
            RawImage templateFoodIconRef)
        {
            // Assign the components
            contBG = contBGRawImage;
            foodName = foodNameText;
            foodIcon = foodIconRawImage;
            button = buttonComponent;
            templateFoodIcon = templateFoodIconRef;
            
            productInfo = product;
            unselectedTexture = unselectedTex;
            selectedTexture = selectedTex;
            onClickCallback = callback;
            
            SetupUI();
            SetupButton();
        }
        
        private void SetupUI()
        {
            // Set product name
            if (foodName != null && productInfo != null)
            {
                foodName.text = productInfo.displayName;
            }
            
            // Set product icon - PRESERVE the original RawImage properties
            if (foodIcon != null && productInfo != null && productInfo.productIcon != null)
            {
                foodIcon.texture = productInfo.productIcon.texture;
                
                // Preserve the original size and position from template if available
                if (templateFoodIcon != null)
                {
                    foodIcon.rectTransform.sizeDelta = templateFoodIcon.rectTransform.sizeDelta;
                    foodIcon.rectTransform.anchoredPosition = templateFoodIcon.rectTransform.anchoredPosition;
                    foodIcon.uvRect = templateFoodIcon.uvRect;
                }
            }
            
            // Set initial background
            if (contBG != null)
            {
                contBG.texture = unselectedTexture;
            }
        }
        
        private void SetupButton()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClick);
            }
        }
        
        private void OnButtonClick()
        {
            isSelected = !isSelected;
            SetSelected(isSelected);
            
            if (onClickCallback != null && productInfo != null)
            {
                onClickCallback.Invoke(productInfo.productID, isSelected);
            }
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (contBG != null)
            {
                contBG.texture = selected ? selectedTexture : unselectedTexture;
            }
            
            if (foodName != null)
            {
                foodName.color = selected ? Color.green : Color.white;
                foodName.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
        
        public bool IsSelected()
        {
            return isSelected;
        }
        
        public Button GetButtonComponent()
        {
            return button;
        }
        
        public GameObject gameObject
        {
            get { return base.gameObject; }
        }
    }
    
    public void ForceShowAssessment()
    {
        TriggerAssessment();
    }
    
    public void ResetTrigger()
    {
        hasTriggered = false;
        isInCooldown = false;
        isSuccessState = false; // Reset success state
        wasClosedByButton = false; // Reset close button state
        
        // Re-enable particle system if it was disabled
        if (triggerParticleSystem != null)
        {
            triggerParticleSystem.Play();
        }
        
        Debug.Log("Assessment trigger reset");
    }
    
    public bool IsUIActive()
    {
        return isUIActive;
    }
    
    public int GetSelectedCount()
    {
        return selectedProductIDs.Count;
    }
    
    public int GetMaxSelections()
    {
        return maxSelections;
    }
    
    public List<string> GetSelectedProducts()
    {
        return new List<string>(selectedProductIDs);
    }
    
    public bool IsInCooldown()
    {
        return isInCooldown;
    }
    
    public float GetRemainingCooldown()
    {
        // Note: This is a simple implementation. For accurate timing, you'd need to track start time
        return isInCooldown ? closeCooldown : 0f;
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Auto Find All Components")]
    private void AutoFindAllComponents()
    {
        if (buttonTemplate != null)
        {
            // Find ContBG
            Transform contBGTransform = buttonTemplate.transform.Find("ContBG");
            if (contBGTransform != null)
            {
                templateContBG = contBGTransform.GetComponent<RawImage>();
                Debug.Log($"Found templateContBG: {templateContBG != null}");
            }
            
            // Find FoodName
            Transform foodNameTransform = buttonTemplate.transform.Find("FoodName");
            if (foodNameTransform != null)
            {
                templateFoodName = foodNameTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log($"Found templateFoodName: {templateFoodName != null}");
            }
            
            // Find FoodIcon
            Transform foodIconTransform = buttonTemplate.transform.Find("FoodIcon");
            if (foodIconTransform != null)
            {
                templateFoodIcon = foodIconTransform.GetComponent<RawImage>();
                Debug.Log($"Found templateFoodIcon: {templateFoodIcon != null}");
            }
            
            // Find Button component
            templateButton = buttonTemplate.GetComponent<Button>();
            Debug.Log($"Found templateButton: {templateButton != null}");
        }
        
        Debug.Log("=== AUTO-FIND COMPLETE ===");
        Debug.Log($"ContBG: {templateContBG != null}");
        Debug.Log($"FoodName: {templateFoodName != null}");
        Debug.Log($"FoodIcon: {templateFoodIcon != null}");
        Debug.Log($"Button: {templateButton != null}");
    }
    
    [ContextMenu("Test Show Assessment")]
    private void TestShowAssessment()
    {
        if (productManager != null && productDatabase != null)
        {
            productManager.collectedProductIDs = new List<string>();
            for (int i = 0; i < Mathf.Min(6, productDatabase.allProducts.Length); i++)
            {
                productManager.collectedProductIDs.Add(productDatabase.allProducts[i].productID);
            }
        }
        
        TriggerAssessment();
    }
    
    [ContextMenu("Test Success Scenario")]
    private void TestSuccessScenario()
    {
        // Simulate selecting 5 added sugar products
        if (productManager != null && productDatabase != null)
        {
            productManager.collectedProductIDs = new List<string>();
            int addedSugarCount = 0;
            
            // Find 5 added sugar products
            foreach (var product in productDatabase.allProducts)
            {
                if (product.productType == ProductData.ProductType.AddedSugar && addedSugarCount < 5)
                {
                    productManager.collectedProductIDs.Add(product.productID);
                    addedSugarCount++;
                }
            }
            
            // If we don't have 5 added sugar products, use whatever we have
            while (productManager.collectedProductIDs.Count < 5 && 
                   productManager.collectedProductIDs.Count < productDatabase.allProducts.Length)
            {
                foreach (var product in productDatabase.allProducts)
                {
                    if (!productManager.collectedProductIDs.Contains(product.productID) && 
                        productManager.collectedProductIDs.Count < 5)
                    {
                        productManager.collectedProductIDs.Add(product.productID);
                    }
                }
            }
        }
        
        TriggerAssessment();
    }
    
    [ContextMenu("Debug Spawn Points")]
    private void DebugSpawnPoints()
    {
        Debug.Log("=== SPAWN POINTS DEBUG ===");
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"Spawn Point {i}: {spawnPoints[i].name} at {spawnPoints[i].position}");
            }
            else
            {
                Debug.LogError($"Spawn Point {i}: NULL");
            }
        }
    }
    
    [ContextMenu("Test Close Button")]
    private void TestCloseButton()
    {
        if (isUIActive)
        {
            OnCloseButtonClick();
        }
        else
        {
            Debug.Log("UI is not active. Trigger assessment first.");
        }
    }
    #endif
}