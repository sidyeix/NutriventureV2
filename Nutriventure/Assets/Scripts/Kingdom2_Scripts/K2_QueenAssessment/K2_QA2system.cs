using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class K2_QA2system : MonoBehaviour
{
    [Header("Dependencies")]
    public K2_QA1system qa1System; // Reference to the first assessment system
    public ProductData productDatabase; // Product database for nutrition labels
    public Transform playerTransform; // Reference to player transform
    
    [Header("Scan Settings")]
    public float scanRange = 5f; // Increased range for testing
    public string spawnProductTag = "QA2SpawnedProduct"; // Tag for spawned products
    
    [Header("UI References")]
    public Button scanButton; // QA2_ScanBTN
    public GameObject assessmentCanvas; // Canvas for nutrition label display
    public CanvasGroup canvasGroup; // For fade effects
    
    [Header("Panel Components")]
    public GameObject panel; // Panel inside canvas
    public RawImage nutritionLabelImage; // RawImage for nutrition label
    public Button confirmButton; // Confirm button on panel
    public Button closeButton; // Close button on panel
    public TextMeshProUGUI productNameText; // Optional: Show product name on panel
    
    [Header("Input Field Components")]
    public TMP_InputField sugarInputField; // Input field for sugar amount
    public GameObject inputFieldContainer; // Container for the input field (optional)
    public TextMeshProUGUI validationText; // Text to show validation messages
    public GameObject inputFieldGroup; // Group containing input field and label
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;
    public Color defaultColor = Color.white;
    public float validationTextDuration = 2f;
    
    [Header("Heart Damage Settings")]
    public SugariaPlayerStat playerHealth; // Reference to player health system
    public int wrongAnswerDamage = 1; // Hearts to lose on wrong answer
    public GameObject damageEffectPrefab; // Optional: Visual effect when losing heart
    public Transform damageEffectSpawnPoint; // Where to spawn damage effect
    public AudioClip damageSound; // Sound when taking damage
    public AudioClip wrongAnswerSound; // Sound for wrong input
    
    [Header("Audio")]
    public AudioClip scanSound;
    public AudioClip panelOpenSound;
    public AudioClip panelCloseSound;
    public AudioClip completionSound; // NEW: Sound when product is completed (particle activates + animator removed)
    public AudioClip particleActivationSound; // NEW: Sound when particle system activates
    public AudioClip correctAnswerSound; // Sound for correct input
    
    [Header("Animation Settings")]
    public float uiFadeInDuration = 0.3f;
    public float uiFadeOutDuration = 0.3f;
    public float wrongAnswerShakeDuration = 0.5f;
    public float wrongAnswerShakeIntensity = 10f;
    public float correctAnswerPulseDuration = 0.5f;
    public float correctAnswerPulseScale = 1.2f;
    
    [Header("Player Controls")]
    public bool freezePlayerControls = true; // Freeze player when panel is open
    public bool freezeGameplay = false; // Freeze time when panel is open
    
    [Header("Particle Systems")]
    public ParticleSystem[] spawnPointParticles; // Array of particle systems at spawn points (assign in inspector)
    
    [Header("Audio Settings")]
    public float completionSoundVolume = 0.7f;
    public float particleSoundVolume = 0.5f;
    public float soundDelay = 0.1f; // Delay before playing completion sound
    
    
    [Header("Debug")]
    public bool debugMode = true;
    public bool alwaysShowScanButton = false; // For testing
    public bool skipValidationForTesting = false; // Skip validation for testing
    
    // Runtime variables
    private GameObject currentNearbyProduct = null;
    private bool isScanButtonVisible = false;
    private bool isPanelActive = false;
    private AudioSource audioSource;
    private float originalTimeScale = 1f;
    
    // Track scanned products
    private List<string> scannedProductIDs = new List<string>();
    private List<string> correctlyAnsweredIDs = new List<string>(); // Track which products were answered correctly
    
    // Input validation
    private float targetSugarAmount = 0f;
    private Coroutine validationTextCoroutine;
    private string currentProductID = "";
    private Vector3 originalPanelPosition;
    
    // References to player components
    private MonoBehaviour playerMovementScript;
    private UnityEngine.InputSystem.PlayerInput playerInput;
    
    // Store particle system positions for reference
    private Vector3[] particlePositions;
    
    void Start()
    {
        InitializeComponents();
        SetupUI();
        SetupAudio();
        
        // Try to find dependencies if not assigned
        FindDependencies();
        
        // Initialize particle systems (disable them all at start)
        InitializeParticleSystems();
        
        // Store particle system positions for reference
        CacheParticlePositions();
        
        // Store original position for animation
        if (panel != null) originalPanelPosition = panel.transform.localPosition;
        
        // Disable scan button by default
        if (scanButton != null)
        {
            scanButton.gameObject.SetActive(false);
        }
        
        // Disable canvas by default
        if (assessmentCanvas != null)
        {
            assessmentCanvas.SetActive(false);
        }
        
        // Disable validation text by default
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
        
        LogDebug("QA2 System initialized.");
        LogDebug($"Looking for products with tag: {spawnProductTag}");
        LogDebug($"QA1 System found: {qa1System != null}");
        LogDebug($"Player Transform found: {playerTransform != null}");
        LogDebug($"Player Health System found: {playerHealth != null}");
    }
    
    private void InitializeComponents()
    {
        // Create or get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Find player components
        playerMovementScript = FindObjectOfType<StarterAssets.ThirdPersonController>();
        playerInput = FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        
        // Find player health if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<SugariaPlayerStat>();
            if (playerHealth != null)
            {
                LogDebug("Found player health system automatically.");
            }
            else
            {
                LogDebug("Player health system not found.");
            }
        }
        
        // Find player transform if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                LogDebug("Found player transform automatically.");
            }
            else
            {
                Debug.LogError("Player not found! Make sure player has 'Player' tag.");
            }
        }
    }
    
    private void FindDependencies()
    {
        if (qa1System == null)
        {
            qa1System = FindObjectOfType<K2_QA1system>();
            if (qa1System != null)
            {
                LogDebug("Found QA1 system automatically.");
            }
            else
            {
                LogDebug("QA1 System not found! Make sure QA1 is completed before QA2.");
            }
        }
        
        if (productDatabase == null)
        {
            productDatabase = Resources.Load<ProductData>("ProductData");
            if (productDatabase != null)
            {
                LogDebug("Found ProductData in Resources.");
            }
            else
            {
                Debug.LogError("ProductData not found in Resources!");
            }
        }
    }
    
    private void SetupUI()
    {
        // Setup scan button
        if (scanButton != null)
        {
            scanButton.onClick.AddListener(OnScanButtonClicked);
            LogDebug("Scan button listener added.");
        }
        else
        {
            Debug.LogError("Scan Button (QA2_ScanBTN) is not assigned!");
        }
        
        // Setup panel buttons
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ValidateAndConfirm);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        
        // Setup input field - NO real-time validation
        if (sugarInputField != null)
        {
            // REMOVED: onValueChanged listener for real-time validation
            sugarInputField.onSubmit.AddListener(OnInputSubmitted);
            sugarInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            sugarInputField.characterLimit = 6; // Allow up to 999.99
            
            // Set up validation
            sugarInputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
            sugarInputField.keyboardType = TouchScreenKeyboardType.NumberPad;
            
            // Clear initial text
            sugarInputField.text = "";
            
            LogDebug("Sugar input field initialized (no real-time validation).");
        }
        else
        {
            Debug.LogWarning("Sugar input field not assigned! Input validation will not work.");
        }
        
        // Initialize validation text
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
        
        // Get or add CanvasGroup for fade effects
        if (assessmentCanvas != null && canvasGroup == null)
        {
            canvasGroup = assessmentCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = assessmentCanvas.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
        
        // Hide input field group initially
        if (inputFieldGroup != null)
        {
            inputFieldGroup.SetActive(false);
        }
    }
    
    private void SetupAudio()
    {
        if (audioSource == null) return;
        
        audioSource.spatialBlend = 0f; // 2D sound for UI
        audioSource.playOnAwake = false;
    }
    
    private void InitializeParticleSystems()
    {
        if (spawnPointParticles != null)
        {
            foreach (ParticleSystem ps in spawnPointParticles)
            {
                if (ps != null)
                {
                    ps.Stop(); // Ensure particle system is stopped
                    ps.gameObject.SetActive(false); // Disable the GameObject
                    LogDebug($"Initialized particle system at {ps.transform.position}");
                }
            }
            LogDebug($"Initialized {spawnPointParticles.Length} particle systems.");
        }
        else
        {
            LogDebug("No particle systems assigned. Skipping particle initialization.");
        }
    }
    
    private void CacheParticlePositions()
    {
        if (spawnPointParticles != null && spawnPointParticles.Length > 0)
        {
            particlePositions = new Vector3[spawnPointParticles.Length];
            for (int i = 0; i < spawnPointParticles.Length; i++)
            {
                if (spawnPointParticles[i] != null)
                {
                    particlePositions[i] = spawnPointParticles[i].transform.position;
                    LogDebug($"Cached particle position {i}: {particlePositions[i]}");
                }
            }
        }
        else
        {
            particlePositions = new Vector3[0];
            LogDebug("No particle positions cached.");
        }
    }
    
    void Update()
    {
        // For testing: allow always showing scan button
        if (alwaysShowScanButton && !isScanButtonVisible && !isPanelActive)
        {
            TestFindAnyProduct();
            return;
        }
        
        // Only check for products if QA1 is completed
        if (!IsQA1Completed())
        {
            if (isScanButtonVisible)
            {
                HideScanButton();
            }
            return;
        }
        
        // Check for nearby products when not scanning
        if (!isPanelActive)
        {
            CheckForNearbyProducts();
        }
        
        // Handle ESC key to close panel
        if (isPanelActive && UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnCloseButtonClicked();
        }
        
        // Handle Enter key to confirm
        if (isPanelActive && UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ValidateAndConfirm();
        }
        
        // Handle Tab key to focus input field
        if (isPanelActive && UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame &&
            sugarInputField != null && sugarInputField.gameObject.activeInHierarchy)
        {
            sugarInputField.Select();
            sugarInputField.ActivateInputField();
        }
    }
    
    private bool IsQA1Completed()
    {
        // Check if QA1 system exists and has spawned products
        if (qa1System == null) return false;
        
        // Check if QA1 is in success state
        // For now, check if it has selected products
        return qa1System.GetSelectedProducts().Count >= qa1System.GetMaxSelections();
    }
    
    private void CheckForNearbyProducts()
    {
        if (playerTransform == null)
        {
            LogDebug("Player transform is null, cannot check for nearby products.");
            return;
        }
        
        GameObject closestProduct = null;
        float closestDistance = float.MaxValue;
        
        try
        {
            // Find all products with the spawn tag
            GameObject[] products = GameObject.FindGameObjectsWithTag(spawnProductTag);
            
            if (debugMode && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
            {
                LogDebug($"Found {products.Length} products with tag '{spawnProductTag}'");
            }
            
            foreach (GameObject product in products)
            {
                if (product == null) continue;
                
                // Check if this product has already been scanned and correctly answered
                string productID = GetProductIDFromObject(product);
                if (correctlyAnsweredIDs.Contains(productID))
                {
                    continue; // Skip products that were already correctly answered
                }
                
                // FIXED: Use player position instead of this GameObject's position
                float distance = Vector3.Distance(playerTransform.position, product.transform.position);
                
                if (debugMode && Time.frameCount % 60 == 0)
                {
                    LogDebug($"Product: {product.name}, Distance to player: {distance:F1}, Scan Range: {scanRange}");
                }
                
                if (distance < scanRange && distance < closestDistance)
                {
                    closestProduct = product;
                    closestDistance = distance;
                }
            }
        }
        catch (UnityException e)
        {
            LogDebug($"Tag '{spawnProductTag}' error: {e.Message}");
            // Tag doesn't exist, try alternative approach
            closestProduct = FindProductByComponent();
        }
        
        // Update current nearby product
        if (closestProduct != null && closestDistance <= scanRange)
        {
            if (currentNearbyProduct != closestProduct)
            {
                currentNearbyProduct = closestProduct;
                ShowScanButton();
                LogDebug($"Near product: {currentNearbyProduct.name}, Distance to player: {closestDistance:F1}");
            }
        }
        else
        {
            if (currentNearbyProduct != null)
            {
                LogDebug("Moved away from product.");
                currentNearbyProduct = null;
                HideScanButton();
            }
        }
    }
    
    private GameObject FindProductByComponent()
    {
        // Alternative: Find products by component instead of tag
        K2_QA1system.QA2Scannable[] scannableProducts = FindObjectsOfType<K2_QA1system.QA2Scannable>();
        LogDebug($"Found {scannableProducts.Length} products by component.");
        
        GameObject closestProduct = null;
        float closestDistance = float.MaxValue;
        
        foreach (var scannable in scannableProducts)
        {
            if (scannable == null || scannable.gameObject == null) continue;
            
            GameObject product = scannable.gameObject;
            string productID = scannable.productID;
            
            // Skip products that were already correctly answered
            if (correctlyAnsweredIDs.Contains(productID))
            {
                continue;
            }
            
            // FIXED: Use player position
            float distance = Vector3.Distance(playerTransform.position, product.transform.position);
            if (distance < scanRange && distance < closestDistance)
            {
                closestProduct = product;
                closestDistance = distance;
            }
        }
        
        return closestProduct;
    }
    
    private void ShowScanButton()
    {
        if (!isScanButtonVisible && scanButton != null && !isPanelActive)
        {
            scanButton.gameObject.SetActive(true);
            isScanButtonVisible = true;
            LogDebug($"Scan button shown for {currentNearbyProduct?.name}");
        }
    }
    
    private void HideScanButton()
    {
        if (isScanButtonVisible && scanButton != null)
        {
            scanButton.gameObject.SetActive(false);
            isScanButtonVisible = false;
            LogDebug("Scan button hidden");
        }
    }
    
    private void OnScanButtonClicked()
    {
        if (currentNearbyProduct == null || isPanelActive) return;
        
        LogDebug($"Scan button clicked for {currentNearbyProduct.name}");
        
        // Play scan sound
        if (scanSound != null)
        {
            audioSource.PlayOneShot(scanSound);
        }
        
        // Get product ID from the spawned product
        string productID = GetProductIDFromObject(currentNearbyProduct);
        
        if (string.IsNullOrEmpty(productID))
        {
            Debug.LogWarning("Could not extract product ID from spawned product!");
            return;
        }
        
        // Show the nutrition label panel
        ShowNutritionLabelPanel(productID);
    }
    
    private string GetProductIDFromObject(GameObject product)
    {
        if (product == null) return "";
        
        // First try to get from QA2Scannable component
        K2_QA1system.QA2Scannable scannable = product.GetComponent<K2_QA1system.QA2Scannable>();
        if (scannable != null && !string.IsNullOrEmpty(scannable.productID))
        {
            return scannable.productID;
        }
        
        // Fallback: Extract from name
        string productName = product.name;
        
        // Remove common suffixes
        if (productName.Contains("(Clone)"))
            productName = productName.Replace("(Clone)", "").Trim();
        
        // Try to extract from the name
        string[] nameParts = productName.Split('_');
        string cleanName = nameParts[0].ToUpper();
        
        // Remove any numbers from the end
        while (cleanName.Length > 0 && char.IsDigit(cleanName[cleanName.Length - 1]))
        {
            cleanName = cleanName.Substring(0, cleanName.Length - 1);
        }
        
        return cleanName;
    }
    
    private void ShowNutritionLabelPanel(string productID)
    {
        isPanelActive = true;
        currentProductID = productID; // Store the current product ID
        
        // Hide scan button
        HideScanButton();
        
        if (productDatabase == null)
        {
            Debug.LogError("ProductData is not assigned!");
            return;
        }
        
        ProductData.ProductInfo productInfo = productDatabase.GetProductInfo(productID);
        if (productInfo == null)
        {
            Debug.LogError($"Product info not found for ID: {productID}");
            
            // Try alternative ID formats
            string alternativeID = productID.Replace("_", "");
            productInfo = productDatabase.GetProductInfo(alternativeID);
            
            if (productInfo == null)
            {
                Debug.LogError($"Could not find product info for '{productID}' or '{alternativeID}'");
                isPanelActive = false;
                return;
            }
        }
        
        LogDebug($"Showing nutrition label for: {productInfo.displayName}");
        
        // Store target sugar amount for validation
        targetSugarAmount = productInfo.sugarContentAmount;
        
        // Set product name if text component is available
        if (productNameText != null)
        {
            productNameText.text = productInfo.displayName;
        }
        
        // Set nutrition label image
        if (nutritionLabelImage != null)
        {
            if (productInfo.nutritionLabelImage != null)
            {
                nutritionLabelImage.texture = productInfo.nutritionLabelImage.texture;
                nutritionLabelImage.gameObject.SetActive(true);
                LogDebug($"Set nutrition label image: {productInfo.nutritionLabelImage.name}");
            }
            else
            {
                Debug.LogWarning($"Nutrition label image not found for product: {productInfo.displayName}");
                nutritionLabelImage.gameObject.SetActive(false);
            }
        }
        
        // Reset input field
        if (sugarInputField != null)
        {
            sugarInputField.text = "";
            sugarInputField.interactable = true;
            
            // Show input field if this product hasn't been correctly answered yet
            if (inputFieldGroup != null)
            {
                bool showInputField = !correctlyAnsweredIDs.Contains(productID);
                inputFieldGroup.SetActive(showInputField);
                
                if (showInputField)
                {
                    // Select the input field
                    sugarInputField.Select();
                    sugarInputField.ActivateInputField();
                }
            }
        }
        
        // Hide validation text
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
        
        // REMOVED: No button state update on typing
        // Confirm button is always interactable
        
        // Store original time scale
        originalTimeScale = Time.timeScale;
        
        // Freeze gameplay if enabled
        if (freezeGameplay)
        {
            Time.timeScale = 0f;
            LogDebug("Gameplay frozen");
        }
        
        // Freeze player controls if enabled
        if (freezePlayerControls)
        {
            DisablePlayerControls();
            LogDebug("Player controls disabled");
        }
        
        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Play panel open sound
        if (panelOpenSound != null)
        {
            audioSource.PlayOneShot(panelOpenSound);
        }
        
        // Show and fade in canvas
        if (assessmentCanvas != null)
        {
            assessmentCanvas.SetActive(true);
            StartCoroutine(FadeCanvas(0f, 1f, uiFadeInDuration));
        }
    }
    
    // REMOVED: OnInputValueChanged method - no real-time validation
    
    private void OnInputSubmitted(string value)
    {
        // When user presses Enter in the input field, validate
        ValidateAndConfirm();
    }
    
    // REMOVED: UpdateConfirmButtonState method - no button highlighting
    
    private void ValidateAndConfirm()
    {
        if (sugarInputField == null || string.IsNullOrEmpty(sugarInputField.text))
        {
            ShowValidationText("Please enter a value!", defaultColor);
            return;
        }
        
        bool validationPassed = false;
        
        if (skipValidationForTesting)
        {
            // Bypass validation for testing
            validationPassed = true;
            LogDebug("Skipping validation for testing");
        }
        else if (float.TryParse(sugarInputField.text, out float inputValue))
        {
            // Check if the value matches the target sugar amount
            validationPassed = Mathf.Abs(inputValue - targetSugarAmount) < 0.01f;
            LogDebug($"Validation: Input={inputValue}, Target={targetSugarAmount}, Passed={validationPassed}");
        }
        else
        {
            ShowValidationText("Please enter a valid number!", defaultColor);
            return;
        }
        
        if (validationPassed)
        {
            OnCorrectAnswer();
        }
        else
        {
            OnWrongAnswer();
        }
    }
    
    private void OnCorrectAnswer()
    {
        LogDebug("Correct answer!");
        
        // Play correct answer sound
        if (correctAnswerSound != null)
        {
            audioSource.PlayOneShot(correctAnswerSound);
        }
        
        // Show success message
        ShowValidationText($"Correct! {targetSugarAmount}g of sugar", correctColor, 2f);
        
        // Add to correctly answered list
        if (!correctlyAnsweredIDs.Contains(currentProductID))
        {
            correctlyAnsweredIDs.Add(currentProductID);
        }
        
        // Disable input field
        if (sugarInputField != null)
        {
            sugarInputField.interactable = false;
        }
        
        // Start success animation
        StartCoroutine(CorrectAnswerAnimation());
        
        // Wait a moment then proceed
        StartCoroutine(DelayedConfirm(1f));
    }
    
    private void OnWrongAnswer()
    {
        LogDebug("Wrong answer!");
        
        // Play wrong answer sound
        if (wrongAnswerSound != null)
        {
            audioSource.PlayOneShot(wrongAnswerSound);
        }
        
        // Show error message
        ShowValidationText($"Incorrect. Try again!", incorrectColor);
        
        // Apply damage to player health
        ApplyDamageToPlayer();
        
        // Start wrong answer animation
        StartCoroutine(WrongAnswerAnimation());
        
        // Clear input field but keep it focused
        if (sugarInputField != null)
        {
            sugarInputField.text = "";
            sugarInputField.Select();
            sugarInputField.ActivateInputField();
        }
        
        // Do NOT close the panel - let user try again
    }
    
    private void ApplyDamageToPlayer()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(wrongAnswerDamage);
            LogDebug($"Player took {wrongAnswerDamage} damage for wrong answer");
            
            // Show damage effect
            if (damageEffectPrefab != null)
            {
                Transform spawnPoint = damageEffectSpawnPoint != null ? damageEffectSpawnPoint : transform;
                GameObject effect = Instantiate(damageEffectPrefab, spawnPoint.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Play damage sound
            if (damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
        }
        else
        {
            LogDebug("Player health system not found - damage not applied");
        }
    }
    
    private IEnumerator CorrectAnswerAnimation()
    {
        if (confirmButton != null)
        {
            Vector3 originalScale = confirmButton.transform.localScale;
            float elapsed = 0f;
            
            while (elapsed < correctAnswerPulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / correctAnswerPulseDuration;
                
                // Pulse animation
                float scale = Mathf.Lerp(1f, correctAnswerPulseScale, 
                    Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f);
                confirmButton.transform.localScale = originalScale * scale;
                
                yield return null;
            }
            
            confirmButton.transform.localScale = originalScale;
        }
    }
    
    private IEnumerator WrongAnswerAnimation()
    {
        if (panel != null)
        {
            Vector3 originalPos = panel.transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < wrongAnswerShakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / wrongAnswerShakeDuration;
                
                // Shake animation
                float shakeAmount = Mathf.Lerp(wrongAnswerShakeIntensity, 0f, t);
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-shakeAmount, shakeAmount),
                    Random.Range(-shakeAmount, shakeAmount),
                    0f
                );
                panel.transform.localPosition = originalPos + shakeOffset;
                
                yield return null;
            }
            
            panel.transform.localPosition = originalPos;
        }
    }
    
    private IEnumerator DelayedConfirm(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        // Now actually confirm
        ProcessProductConfirmation();
    }
    
    private void ProcessProductConfirmation()
    {
        // Add product to scanned list if not already there
        if (currentNearbyProduct != null)
        {
            string productID = GetProductIDFromObject(currentNearbyProduct);
            if (!string.IsNullOrEmpty(productID) && !scannedProductIDs.Contains(productID))
            {
                scannedProductIDs.Add(productID);
                LogDebug($"Scanned product: {productID} (Total: {scannedProductIDs.Count})");
                
                // NEW: Remove Animator and disable ProductParticles
                CleanupProductComponents(currentNearbyProduct);
                
                // Visual feedback that product was scanned
                MarkProductAsScanned(currentNearbyProduct);
                
                // Activate particle system for this spawn point
                ActivateParticleSystemForProduct(currentNearbyProduct);
                
                // NEW: Play completion sound with delay
                StartCoroutine(PlayCompletionSounds());
            }
        }
        
        // Close panel
        ClosePanel();
    }
    
    private void ShowValidationText(string message, Color color, float duration = -1f)
    {
        if (validationText == null) return;
        
        // Stop existing coroutine if any
        if (validationTextCoroutine != null)
        {
            StopCoroutine(validationTextCoroutine);
        }
        
        // Set text and color
        validationText.text = message;
        validationText.color = color;
        validationText.gameObject.SetActive(true);
        
        // Start hide coroutine if duration is specified
        if (duration > 0)
        {
            validationTextCoroutine = StartCoroutine(HideValidationTextAfterDelay(duration));
        }
    }
    
    private IEnumerator HideValidationTextAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        if (validationText != null)
        {
            validationText.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
    {
        if (canvasGroup == null) yield break;
        
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
    
    public void OnCloseButtonClicked()
    {
        LogDebug("Close button clicked");
        
        // Check if we should warn about unsaved input
        if (sugarInputField != null && !string.IsNullOrEmpty(sugarInputField.text) && 
            !correctlyAnsweredIDs.Contains(currentProductID))
        {
            // Optional: Show confirmation dialog
            // For now, just close
        }
        
        ClosePanel();
    }
    
    private void ClosePanel()
    {
        if (!isPanelActive) return;
        
        // Play panel close sound
        if (panelCloseSound != null)
        {
            audioSource.PlayOneShot(panelCloseSound);
        }
        
        // Fade out and hide canvas
        StartCoroutine(FadeAndHideUI());
    }
    
    private IEnumerator FadeAndHideUI()
    {
        // Fade out
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvas(1f, 0f, uiFadeOutDuration));
        }
        
        // Hide canvas
        if (assessmentCanvas != null)
        {
            assessmentCanvas.SetActive(false);
        }
        
        // Resume gameplay
        ResumeGameplay();
        
        // Reset panel state
        isPanelActive = false;
        currentProductID = "";
        
        // Show scan button again if still near product and not correctly answered
        if (currentNearbyProduct != null)
        {
            string productID = GetProductIDFromObject(currentNearbyProduct);
            float distance = Vector3.Distance(playerTransform.position, currentNearbyProduct.transform.position);
            
            if (distance <= scanRange && !correctlyAnsweredIDs.Contains(productID))
            {
                ShowScanButton();
            }
        }
        
        LogDebug("Nutrition label panel closed");
    }
    
    private void ResumeGameplay()
    {
        // Restore time scale if it was frozen
        if (freezeGameplay)
        {
            Time.timeScale = originalTimeScale;
            LogDebug("Gameplay resumed");
        }
        
        // Re-enable player controls if they were disabled
        if (freezePlayerControls)
        {
            EnablePlayerControls();
            LogDebug("Player controls enabled");
        }
        
    }
    
    private void DisablePlayerControls()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;
        
        if (playerInput != null)
            playerInput.enabled = false;
    }
    
    private void EnablePlayerControls()
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;
        
        if (playerInput != null)
            playerInput.enabled = true;
    }
    
    private IEnumerator PlayCompletionSounds()
    {
        // Wait a moment before playing sounds
        yield return new WaitForSeconds(soundDelay);
        
        // Play completion sound (for animator removal and general completion)
        if (completionSound != null)
        {
            audioSource.PlayOneShot(completionSound, completionSoundVolume);
            LogDebug($"Played completion sound: {completionSound.name}");
        }
        
        // Play particle activation sound
        if (particleActivationSound != null)
        {
            // Play particle sound slightly after completion sound for layered effect
            yield return new WaitForSeconds(0.05f);
            audioSource.PlayOneShot(particleActivationSound, particleSoundVolume);
            LogDebug($"Played particle activation sound: {particleActivationSound.name}");
        }
    }
    
    private void CleanupProductComponents(GameObject product)
    {
        if (product == null)
        {
            LogDebug("Cannot clean up null product.");
            return;
        }
        
        LogDebug($"Cleaning up components for product: {product.name}");
        
        // 1. Remove Animator component if it exists
        Animator animator = product.GetComponent<Animator>();
        if (animator != null)
        {
            Destroy(animator);
            LogDebug($"Removed Animator component from {product.name}");
        }
        else
        {
            LogDebug($"No Animator component found on {product.name}");
        }
        
        // 2. Find and disable "ProductParticles" GameObject
        // First try direct child
        Transform productParticles = FindChildByName(product.transform, "ProductParticles");
        
        // If not found, search through all children and grandchildren
        if (productParticles == null)
        {
            productParticles = FindChildRecursive(product.transform, "ProductParticles");
        }
        
        if (productParticles != null)
        {
            productParticles.gameObject.SetActive(false);
            LogDebug($"Disabled ProductParticles GameObject: {productParticles.name}");
            
            // Also disable any ParticleSystem components on the ProductParticles GameObject
            ParticleSystem[] particleSystems = productParticles.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                ps.Stop();
                ps.gameObject.SetActive(false);
                LogDebug($"Stopped and disabled particle system: {ps.name}");
            }
        }
        else
        {
            LogDebug($"ProductParticles GameObject not found in {product.name} or its children");
            
            // Alternative: Look for any particle systems on the product
            ParticleSystem[] allParticleSystems = product.GetComponentsInChildren<ParticleSystem>();
            if (allParticleSystems.Length > 0)
            {
                foreach (ParticleSystem ps in allParticleSystems)
                {
                    ps.Stop();
                    ps.gameObject.SetActive(false);
                    LogDebug($"Stopped and disabled particle system: {ps.name}");
                }
            }
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
    
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
    
    private void MarkProductAsScanned(GameObject product)
    {
        // Add a visual indicator that the product has been scanned
        Renderer renderer = product.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material originalMaterial = renderer.material;
            Color originalColor = originalMaterial.color;
            Color scannedColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
            
            Material scannedMaterial = new Material(originalMaterial);
            scannedMaterial.color = scannedColor;
            renderer.material = scannedMaterial;
            
            LogDebug($"Marked {product.name} as scanned (transparent)");
        }
    }
    
    private void ActivateParticleSystemForProduct(GameObject product)
    {
        if (spawnPointParticles == null || spawnPointParticles.Length == 0)
        {
            LogDebug("No particle systems assigned. Skipping particle activation.");
            return;
        }
        
        // Find which particle system is closest to this product
        int particleIndex = FindClosestParticleSystemIndex(product.transform.position);
        
        if (particleIndex >= 0 && particleIndex < spawnPointParticles.Length)
        {
            ParticleSystem ps = spawnPointParticles[particleIndex];
            if (ps != null)
            {
                // Enable and play the particle system
                ps.gameObject.SetActive(true);
                ps.Play();
                
                LogDebug($"Activated particle system at index {particleIndex} for product {product.name}");
                Debug.Log($"✅ Particle system activated at spawn point {particleIndex + 1}!");
            }
            else
            {
                LogDebug($"No particle system assigned at index {particleIndex}");
            }
        }
        else
        {
            LogDebug($"Invalid particle index {particleIndex} for product {product.name}");
        }
    }
    
    private int FindClosestParticleSystemIndex(Vector3 productPosition)
    {
        if (particlePositions == null || particlePositions.Length == 0)
        {
            LogDebug("No particle positions cached.");
            return -1;
        }
        
        // Find the closest particle system position to this product
        float closestDistance = float.MaxValue;
        int closestIndex = -1;
        
        for (int i = 0; i < particlePositions.Length; i++)
        {
            float distance = Vector3.Distance(productPosition, particlePositions[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        LogDebug($"Product at {productPosition} is closest to particle system {closestIndex} (distance: {closestDistance:F2})");
        return closestIndex;
    }
    
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[QA2] {message}");
        }
    }
    
    private void TestFindAnyProduct()
    {
        // For debugging: find any product in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Banana") || obj.name.Contains("Cookie") || 
                obj.name.Contains("Soda") || obj.name.Contains("Product"))
            {
                currentNearbyProduct = obj;
                ShowScanButton();
                LogDebug($"Test mode: Found {obj.name}");
                return;
            }
        }
    }
    
    // Public methods for external access
    public bool IsPanelActive()
    {
        return isPanelActive;
    }
    
    public List<string> GetScannedProducts()
    {
        return new List<string>(scannedProductIDs);
    }
    
    public List<string> GetCorrectlyAnsweredProducts()
    {
        return new List<string>(correctlyAnsweredIDs);
    }
    
    public int GetScannedCount()
    {
        return scannedProductIDs.Count;
    }
    
    public int GetCorrectlyAnsweredCount()
    {
        return correctlyAnsweredIDs.Count;
    }
    
    public bool IsScanningAvailable()
    {
        return IsQA1Completed() && !isPanelActive;
    }
    
    public GameObject GetCurrentNearbyProduct()
    {
        return currentNearbyProduct;
    }
    
    // Visualize scan range in editor - FIXED: Show around player, not this GameObject
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, scanRange);
            
            // Draw line to current nearby product if any
            if (currentNearbyProduct != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(playerTransform.position, currentNearbyProduct.transform.position);
            }
        }
        
        // Visualize particle system positions
        if (particlePositions != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Vector3 pos in particlePositions)
            {
                Gizmos.DrawWireSphere(pos, 0.5f);
            }
        }
    }
    
    // Debug methods
    [ContextMenu("Debug QA2 Status")]
    public void DebugQA2Status()
    {
        debugMode = true;
        Debug.Log("=== QA2 SYSTEM DEBUG ===");
        Debug.Log($"QA1 System Found: {qa1System != null}");
        Debug.Log($"QA1 Completed: {IsQA1Completed()}");
        Debug.Log($"Player Transform: {playerTransform != null}");
        Debug.Log($"Player Position: {(playerTransform != null ? playerTransform.position.ToString() : "No Player")}");
        Debug.Log($"Current Nearby Product: {(currentNearbyProduct != null ? currentNearbyProduct.name : "None")}");
        Debug.Log($"Scan Button Visible: {isScanButtonVisible}");
        Debug.Log($"Panel Active: {isPanelActive}");
        Debug.Log($"Scanned Products: {scannedProductIDs.Count} - {string.Join(", ", scannedProductIDs)}");
        Debug.Log($"Correctly Answered: {correctlyAnsweredIDs.Count} - {string.Join(", ", correctlyAnsweredIDs)}");
        Debug.Log($"Scan Range: {scanRange}");
        Debug.Log($"Spawn Product Tag: {spawnProductTag}");
        Debug.Log($"Particle Systems: {spawnPointParticles?.Length ?? 0}");
        Debug.Log($"Cached Particle Positions: {particlePositions?.Length ?? 0}");
        
        // Check for products with the tag
        try
        {
            GameObject[] products = GameObject.FindGameObjectsWithTag(spawnProductTag);
            Debug.Log($"Products with tag '{spawnProductTag}': {products.Length}");
            foreach (var product in products)
            {
                float distance = playerTransform != null ? 
                    Vector3.Distance(playerTransform.position, product.transform.position) : 0f;
                Debug.Log($"  - {product.name} at {product.transform.position} (Distance: {distance:F1})");
                
                // Also show distance to nearest particle system
                if (particlePositions != null && particlePositions.Length > 0)
                {
                    int closestParticle = FindClosestParticleSystemIndex(product.transform.position);
                    if (closestParticle >= 0)
                    {
                        float particleDistance = Vector3.Distance(product.transform.position, particlePositions[closestParticle]);
                        Debug.Log($"    Closest to particle {closestParticle}: {particleDistance:F1} units");
                    }
                }
            }
        }
        catch (UnityException)
        {
            Debug.LogError($"Tag '{spawnProductTag}' is not defined!");
        }
    }
    
    [ContextMenu("Force Show Scan Button")]
    public void ForceShowScanButton()
    {
        ShowScanButton();
    }
    
    [ContextMenu("Force Hide Scan Button")]
    public void ForceHideScanButton()
    {
        HideScanButton();
    }
    
    [ContextMenu("Test Show Panel")]
    public void TestShowPanel()
    {
        if (currentNearbyProduct != null)
        {
            string productID = GetProductIDFromObject(currentNearbyProduct);
            ShowNutritionLabelPanel(productID);
        }
        else
        {
            Debug.LogWarning("No nearby product to scan!");
            CreateTestProduct();
        }
    }
    
    [ContextMenu("Test Cleanup Product")]
    public void TestCleanupProduct()
    {
        if (currentNearbyProduct != null)
        {
            CleanupProductComponents(currentNearbyProduct);
            Debug.Log($"Test cleanup performed on {currentNearbyProduct.name}");
        }
        else
        {
            Debug.LogWarning("No current nearby product to clean up!");
        }
    }
    
    [ContextMenu("Test Completion Sounds")]
    public void TestCompletionSounds()
    {
        StartCoroutine(PlayCompletionSounds());
        Debug.Log("Testing completion sounds...");
    }
    
    [ContextMenu("Test Activate All Particles")]
    public void TestActivateAllParticles()
    {
        if (spawnPointParticles != null)
        {
            for (int i = 0; i < spawnPointParticles.Length; i++)
            {
                if (spawnPointParticles[i] != null)
                {
                    spawnPointParticles[i].gameObject.SetActive(true);
                    spawnPointParticles[i].Play();
                    Debug.Log($"Test: Activated particle system {i}");
                }
            }
        }
    }
    
    [ContextMenu("Test Deactivate All Particles")]
    public void TestDeactivateAllParticles()
    {
        if (spawnPointParticles != null)
        {
            for (int i = 0; i < spawnPointParticles.Length; i++)
            {
                if (spawnPointParticles[i] != null)
                {
                    spawnPointParticles[i].Stop();
                    spawnPointParticles[i].gameObject.SetActive(false);
                }
            }
            Debug.Log("Test: Deactivated all particle systems");
        }
    }
    
    [ContextMenu("Test Correct Answer")]
    public void TestCorrectAnswer()
    {
        if (isPanelActive && sugarInputField != null)
        {
            sugarInputField.text = targetSugarAmount.ToString();
            ValidateAndConfirm();
        }
    }
    
    [ContextMenu("Test Wrong Answer")]
    public void TestWrongAnswer()
    {
        if (isPanelActive && sugarInputField != null)
        {
            sugarInputField.text = (targetSugarAmount + 5).ToString();
            ValidateAndConfirm();
        }
    }
    
    private void CreateTestProduct()
    {
        if (playerTransform == null)
        {
            Debug.LogError("Cannot create test product: Player transform is null!");
            return;
        }
        
        // Create a test product for debugging
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.name = "TEST_BANANA_Clone";
        testCube.tag = spawnProductTag;
        testCube.transform.position = playerTransform.position + playerTransform.forward * 2f;
        
        // Add an Animator component for testing
        Animator testAnimator = testCube.AddComponent<Animator>();
        LogDebug("Added Animator for testing");
        
        // Create a child GameObject with ProductParticles
        GameObject productParticles = new GameObject("ProductParticles");
        productParticles.transform.parent = testCube.transform;
        productParticles.transform.localPosition = Vector3.zero;
        
        // Add a particle system to the ProductParticles
        ParticleSystem ps = productParticles.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = Color.yellow;
        
        // Add the scannable component
        K2_QA1system.QA2Scannable scannable = testCube.AddComponent<K2_QA1system.QA2Scannable>();
        scannable.productID = "BANANA";
        
        currentNearbyProduct = testCube;
        ShowScanButton();
        
        Debug.Log("Created test product with Animator and ProductParticles for testing");
    }
    
    [ContextMenu("Enable Debug Mode")]
    public void EnableDebugMode()
    {
        debugMode = true;
        Debug.Log("QA2 Debug mode enabled");
    }
    
    [ContextMenu("Disable Debug Mode")]
    public void DisableDebugMode()
    {
        debugMode = false;
        Debug.Log("QA2 Debug mode disabled");
    }
    
    [ContextMenu("Clear Scanned Products")]
    public void ClearScannedProducts()
    {
        scannedProductIDs.Clear();
        correctlyAnsweredIDs.Clear();
        Debug.Log("Cleared scanned and correctly answered products lists");
    }
    
    [ContextMenu("Recache Particle Positions")]
    public void RecacheParticlePositions()
    {
        CacheParticlePositions();
        Debug.Log("Recached particle positions");
    }
    
    [ContextMenu("Find Player Health System")]
    public void FindPlayerHealth()
    {
        playerHealth = FindObjectOfType<SugariaPlayerStat>();
        if (playerHealth != null)
        {
            Debug.Log($"Found player health system: {playerHealth.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("Player health system not found in scene!");
        }
    }
}