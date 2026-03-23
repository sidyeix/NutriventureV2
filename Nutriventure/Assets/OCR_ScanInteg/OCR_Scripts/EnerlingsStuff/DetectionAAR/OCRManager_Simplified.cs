using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.IO;
#endif

public class OCRManager_Simplified : MonoBehaviour
{
    [Header("UI References")]
    public RawImage cameraPreview;
    public TMP_Text statusText;
    public TMP_Text warningText;
    public GameObject fadePanel;
    public GameObject blurPanel;
    public TMP_Text noIngredientText;

    [Header("Buttons")]
    public Button captureButton;
    public Button galleryButton;
    public Button instructionsButton;
    public Button retryButton;
    public Button exitButton;

    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("Scene Transition")]
    public string nextSceneName = "BattlePlay";
    public string mainMenuScene = "MainMenu";
    public float sceneTransitionDelay = 1f;
    public float fadeDuration = 0.5f;

    [Header("Camera Settings")]
    public bool useMockScan = false; // Set to false when building for Android
    public float captureCooldownDuration = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scanSound;
    [Tooltip("Background music to play while in the scan scene")]
    public AudioClip backgroundMusic;

    [Header("Heart Panel (Life System)")]
    [Tooltip("Parent transform inside the heart panel to hold heart images (add HorizontalLayoutGroup)")]
    public Transform heartContainer;
    [Tooltip("Sprite for a full heart")]
    public Sprite fullHeartSprite;
    [Tooltip("Sprite for an empty heart")]
    public Sprite emptyHeartSprite;
    [Tooltip("Size of each heart image (width x height)")]
    public Vector2 heartSize = new Vector2(64f, 64f);

    [Header("Energy & Regen UI")]
    [Tooltip("Energy text with format '15/15'")]
    public TextMeshProUGUI energyText;
    [Tooltip("Text that shows remaining regen time for life (hidden when full)")]
    public TextMeshProUGUI lifeRegenTimerText;
    [Tooltip("Text that shows remaining regen time for energy (hidden when full)")]
    public TextMeshProUGUI energyRegenTimerText;

    // State
    private bool isProcessing = false;
    private bool isCaptureOnCooldown = false;
    private Coroutine captureCooldownCoroutine;
    private string selectedEnerlingName = "";
    private Texture2D currentImage;
    private bool waitingForPluginResponse = false;
    private float maxProcessingTime = 10f;
    private string currentProductFingerprint = ""; // Track current product being scanned
    private System.Collections.Generic.List<Image> heartImages = new System.Collections.Generic.List<Image>();

    private const string PREVIOUS_SCENE_KEY = "ScanOCR_PreviousScene";

#if UNITY_ANDROID && !UNITY_EDITOR
    private WebCamTexture liveCameraTexture;
#endif

    void Start()
    {
        // Process any offline regen before UI setup
        if (GameDataManager.Instance != null)
            GameDataManager.Instance.ProcessOCRBattleRegen();

        SetupUI();
        BuildHeartImages();
        RefreshLifeEnergyUI();
        InitializeCameraPreview();

        // Play background music
        if (backgroundMusic != null && AudioHandler.Instance != null)
            AudioHandler.Instance.PlayMusic(backgroundMusic);

        // Button listeners
        if (captureButton != null)
            captureButton.onClick.AddListener(OnCaptureButtonClicked);

        if (galleryButton != null)
            galleryButton.onClick.AddListener(OnGalleryButtonClicked);

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            retryButton.gameObject.SetActive(false);
        }

        if (instructionsButton != null)
            instructionsButton.onClick.AddListener(OnInstructionsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        UpdateStatus("Ready to scan ingredient list");
        UpdateButtonStates();

        // Clean up any expired products on start
        ProductManager.CleanupExpiredProducts();
    }

    private float regenTickTimer = 0f;
    private const float REGEN_TICK_INTERVAL = 1f;

    void Update()
    {
        if (GameDataManager.Instance == null) return;

        // Lazy-build hearts if they weren't created yet
        if (heartImages.Count == 0 && heartContainer != null)
            BuildHeartImages();

        // Tick regen once per second (avoids SaveGameData every frame)
        regenTickTimer += Time.deltaTime;
        if (regenTickTimer >= REGEN_TICK_INTERVAL)
        {
            regenTickTimer = 0f;
            GameDataManager.Instance.ProcessOCRBattleRegen();
        }

        RefreshLifeEnergyUI();
    }

    void SetupUI()
    {
        if (cameraPreview != null)
        {
            cameraPreview.texture = null;
            cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
        }

        if (fadePanel != null)
            fadePanel.SetActive(false);

        if (blurPanel != null)
            blurPanel.SetActive(false);

        if (noIngredientText != null)
            noIngredientText.gameObject.SetActive(false);

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    void UpdateButtonStates()
    {
        bool canInteract = !isProcessing && !isCaptureOnCooldown && !waitingForPluginResponse;

        if (captureButton != null)
            captureButton.interactable = canInteract;

        if (galleryButton != null)
            galleryButton.interactable = !isProcessing && !waitingForPluginResponse;

        if (instructionsButton != null)
            instructionsButton.interactable = !isProcessing && !waitingForPluginResponse;

        if (exitButton != null)
            exitButton.interactable = !isProcessing && !waitingForPluginResponse;
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void InitializeCameraPreview()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!useMockScan)
        {
            StartCoroutine(StartCameraPreview());
        }
        else
        {
            UpdateStatus("Using mock scan mode");
        }
#else
        UpdateStatus("Using mock scan mode (Unity Editor)");
#endif
    }

    // ==================== CAMERA FUNCTIONALITY ====================

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator StartCameraPreview()
    {
        UpdateStatus("Initializing camera...");
        
        // Request camera permission
        if (!HasCameraPermission())
        {
            yield return StartCoroutine(RequestCameraPermission());
            
            if (!HasCameraPermission())
            {
                UpdateStatus("Camera permission denied");
                useMockScan = true;
                yield break;
            }
        }

        // Get available cameras
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            UpdateStatus("No camera found");
            useMockScan = true;
            yield break;
        }

        // Choose back camera if available
        string cameraName = devices[0].name;
        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        // Start camera preview
        try
        {
            liveCameraTexture = new WebCamTexture(cameraName, 1280, 720);
            if (cameraPreview != null)
            {
                cameraPreview.texture = liveCameraTexture;
                cameraPreview.color = Color.white;
            }
            liveCameraTexture.Play();
            UpdateStatus("Camera ready - Point at ingredient list and tap Capture");
        }
        catch (System.Exception e)
        {
            UpdateStatus("Camera error: " + e.Message);
            useMockScan = true;
        }
    }

    bool HasCameraPermission()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
            using (var packageName = currentActivity.Call<AndroidJavaObject>("getPackageName"))
            {
                int permissionGranted = packageManager.Call<int>("checkPermission", 
                    "android.permission.CAMERA", packageName);
                return permissionGranted == 0;
            }
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    IEnumerator RequestCameraPermission()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string[] permissions = new string[] { "android.permission.CAMERA" };
                currentActivity.Call("requestPermissions", permissions, 0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Permission request error: " + e.Message);
        }
        
        yield return new WaitForSeconds(1f);
    }

    IEnumerator RestartCameraPreview()
    {
        if (liveCameraTexture != null)
        {
            if (liveCameraTexture.isPlaying) 
                liveCameraTexture.Stop();
            liveCameraTexture = null;
        }

        yield return new WaitForSeconds(0.3f);
        
        if (cameraPreview != null)
        {
            cameraPreview.texture = null;
            cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
        
        yield return StartCoroutine(StartCameraPreview());
        UpdateStatus("Camera ready - Point at ingredient list and tap Capture");
    }

    IEnumerator TakePhotoCoroutine()
    {
        UpdateStatus("Preparing capture...");

        if (!HasCameraPermission())
        {
            UpdateStatus("Camera permission required");
            ResetProcessingState();
            yield break;
        }

        bool usingLive = (liveCameraTexture != null && liveCameraTexture.isPlaying);
        WebCamTexture tempWebcam = null;

        if (!usingLive)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                UpdateStatus("No camera found");
                ResetProcessingState();
                yield break;
            }

            string cameraName = devices[0].name;
            foreach (var device in devices)
                if (!device.isFrontFacing) { cameraName = device.name; break; }

            bool tempCamStarted = false;

            try
            {
                tempWebcam = new WebCamTexture(cameraName, 1280, 720);
                if (cameraPreview != null)
                {
                    cameraPreview.texture = tempWebcam;
                    cameraPreview.color = Color.white;
                }
                tempWebcam.Play();
                tempCamStarted = true;
            }
            catch (System.Exception e)
            {
                UpdateStatus("Camera error: " + e.Message);
                ResetProcessingState();
                yield break;
            }

            if (tempCamStarted)
            {
                yield return new WaitForSeconds(1.2f);
            }
        }

        // Capture photo from webcam
        WebCamTexture source = usingLive ? liveCameraTexture : tempWebcam;
        yield return StartCoroutine(CapturePhotoFromWebcam(source));

        // Clean up temp webcam if we created one
        if (tempWebcam != null)
        {
            tempWebcam.Stop();
        }
    }

    IEnumerator CapturePhotoFromWebcam(WebCamTexture webcam)
    {
        UpdateStatus("Capturing photo...");
        yield return new WaitForEndOfFrame();

        Texture2D photo = null;
        bool captureSuccess = false;

        try
        {
            photo = new Texture2D(webcam.width, webcam.height);
            photo.SetPixels(webcam.GetPixels());
            photo.Apply();
            captureSuccess = true;
        }
        catch (System.Exception e)
        {
            UpdateStatus("Capture error: " + e.Message);
            ResetProcessingState();
            yield break;
        }

        if (captureSuccess && photo != null)
        {
            if (currentImage != null) 
                Destroy(currentImage);
            
            currentImage = photo;
            
            if (cameraPreview != null)
            {
                cameraPreview.texture = currentImage;
                cameraPreview.color = Color.white;
            }

            // Process the captured image
            yield return StartCoroutine(ProcessRealScan());
        }
    }

    IEnumerator PickImageViaNativeGallery()
    {
        isProcessing = true;
        UpdateButtonStates();
        UpdateStatus("Opening gallery...");
        
        NativeGallery.GetImageFromGallery((string path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                UpdateStatus("No image selected or not accessible.");
                isProcessing = false;
                UpdateButtonStates();
                return;
            }
            StartCoroutine(LoadImageFromPath(path));
        }, "Select an image", "image/*");

        yield return null;
    }

    IEnumerator LoadImageFromPath(string imagePath)
    {
        bool success = false;
        Texture2D texture = null;
        
        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            texture = new Texture2D(2, 2);

            if (texture.LoadImage(imageData))
            {
                if (currentImage != null) 
                    Destroy(currentImage);
                
                currentImage = texture;
                
                if (cameraPreview != null)
                {
                    cameraPreview.texture = currentImage;
                    cameraPreview.color = Color.white;
                }
                
                UpdateStatus("Image loaded - Processing...");
                success = true;
            }
            else 
            {
                UpdateStatus("Failed to load image");
                if (texture != null) Destroy(texture);
            }
        }
        catch (System.Exception e) 
        { 
            UpdateStatus("Error loading image: " + e.Message);
            if (texture != null) Destroy(texture);
        }

        isProcessing = false;
        UpdateButtonStates();

        if (success) 
        {
            yield return StartCoroutine(ProcessRealScan());
        }
        
        yield return null;
    }

#endif

    // ==================== BUTTON HANDLERS ====================

    public void OnCaptureButtonClicked()
    {
        if (isProcessing || isCaptureOnCooldown || waitingForPluginResponse) return;

        // Check global cooldown first
        if (!CooldownSystem.CanScanAnyIngredient())
        {
            TimeSpan remaining = CooldownSystem.GetGlobalCooldown();
            ShowError($"Please wait {remaining.Seconds} seconds before scanning again.");
            return;
        }

        // Check if player has energy before allowing scan
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetOCRBattleEnergy() <= 0)
        {
            ShowError("No energy remaining! Wait for regeneration.");
            return;
        }

        isProcessing = true;
        UpdateButtonStates();
        StartCaptureCooldown();
        UpdateStatus("Scanning...");

        // Play scan sound
        if (audioSource != null && scanSound != null)
            audioSource.PlayOneShot(scanSound);

        if (useMockScan || Application.isEditor)
        {
            // Mock scan
            StartCoroutine(MockScanCoroutine());
        }
        else
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(TakePhotoCoroutine());
#else
            StartCoroutine(MockScanCoroutine());
#endif
        }
    }

    public void OnGalleryButtonClicked()
    {
        if (isProcessing || waitingForPluginResponse) return;

        // Check global cooldown first
        if (!CooldownSystem.CanScanAnyIngredient())
        {
            TimeSpan remaining = CooldownSystem.GetGlobalCooldown();
            ShowError($"Please wait {remaining.Seconds} seconds before scanning again.");
            return;
        }

        // Check if player has energy before allowing scan
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetOCRBattleEnergy() <= 0)
        {
            ShowError("No energy remaining! Wait for regeneration.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!useMockScan)
        {
            StartCoroutine(PickImageViaNativeGallery());
        }
        else
        {
            StartCoroutine(MockScanCoroutine());
        }
#else
        StartCoroutine(MockScanCoroutine());
#endif
    }

    public void OnRetryButtonClicked()
    {
        ResetScanState();
        ClearError();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!useMockScan)
        {
            StartCoroutine(RestartCameraPreview());
        }
        else
        {
            UpdateStatus("Ready to scan again");
        }
#else
        UpdateStatus("Ready to scan again");
#endif
    }

    public void OnInstructionsButtonClicked()
    {
        UpdateStatus("Point camera at ingredient label and tap Capture");
    }

    public void OnExitButtonClicked()
    {
        string previousScene = PlayerPrefs.GetString(PREVIOUS_SCENE_KEY, "");
        if (!string.IsNullOrEmpty(previousScene))
        {
            PlayerPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
            PlayerPrefs.Save();
            SceneManager.LoadScene(previousScene);
        }
        else if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    // ==================== PRODUCT MANAGER INTEGRATION ====================

    // ==================== CAPTURE COOLDOWN ====================

    void StartCaptureCooldown()
    {
        if (captureCooldownCoroutine != null)
            StopCoroutine(captureCooldownCoroutine);

        captureCooldownCoroutine = StartCoroutine(CaptureCooldownCoroutine());
    }

    IEnumerator CaptureCooldownCoroutine()
    {
        isCaptureOnCooldown = true;
        UpdateButtonStates();

        if (captureButton != null)
        {
            Image buttonImage = captureButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color originalColor = buttonImage.color;
                buttonImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            }
        }

        float cooldownTimer = captureCooldownDuration;

        while (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;

            if (captureButton != null)
            {
                TMP_Text buttonText = captureButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"Wait {cooldownTimer:F1}s";
                }
            }

            yield return null;
        }

        if (captureButton != null)
        {
            Image buttonImage = captureButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color originalColor = buttonImage.color;
                buttonImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
            }

            TMP_Text buttonText = captureButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Capture";
            }
        }

        isCaptureOnCooldown = false;
        UpdateButtonStates();

        captureCooldownCoroutine = null;
    }

    // ==================== REAL OCR PROCESSING ====================

    IEnumerator ProcessRealScan()
    {
        if (currentImage == null)
        {
            UpdateStatus("No image to process");
            ResetProcessingState();
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        UpdateStatus("Processing with ML Kit...");
        
        // Convert image to Base64 for the plugin
        byte[] imageBytes = currentImage.EncodeToJPG(85);
        string base64Image = System.Convert.ToBase64String(imageBytes);
        
        bool pluginCallSuccessful = false;
        
        try
        {
            // Call the Android plugin
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.nutriventure.mlkit.MLKitOcr"))
            {
                // For camera scan (automatic mode - looks for "ingredients" text)
                pluginClass.CallStatic("recognizeTextFromBase64", 
                    base64Image, 
                    gameObject.name, 
                    "OnOCRResult");
            }
            
            pluginCallSuccessful = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Android plugin error: {e.Message}");
            ShowError("Plugin error - using mock scan");
            StartCoroutine(MockScanCoroutine());
            yield break; // Exit early
        }
        
        // Now handle the waiting logic OUTSIDE the try-catch block
        if (pluginCallSuccessful)
        {
            waitingForPluginResponse = true;
            
            // Wait for plugin response with timeout
            float timeoutTimer = 0f;
            while (waitingForPluginResponse && timeoutTimer < maxProcessingTime)
            {
                timeoutTimer += Time.deltaTime;
                UpdateStatus($"Processing... {Mathf.FloorToInt(maxProcessingTime - timeoutTimer)}s");
                yield return null; // ← Now this is outside try-catch!
            }
            
            if (waitingForPluginResponse)
            {
                // Timeout
                ShowError("Processing timeout - try again");
                ResetProcessingState();
            }
        }
#else
        // In Unity Editor, use mock scan
        yield return StartCoroutine(MockScanCoroutine());
#endif
    }

    // ==================== OCR RESULT HANDLING ====================

    public void OnOCRResult(string jsonResult)
    {
        Debug.Log($"Android plugin result: {jsonResult}");
        StartCoroutine(HandleOCRResultCoroutine(jsonResult));
    }

    IEnumerator HandleOCRResultCoroutine(string jsonResult)
    {
        waitingForPluginResponse = false;

        yield return null;

        // Parse the JSON result
        IngredientData ingredientData = JsonParser.ParseIngredientResponse(jsonResult);

        // Case 1: Complete parse failure — no text extracted at all (accidental scan)
        if (ingredientData == null)
        {
            Debug.LogError("Failed to parse OCR result");
            ShowNoTextScanned();
            yield break;
        }

        // Case 2: Plugin returned an error / no text found (accidental scan)
        if (ingredientData.mode == "error" || ingredientData.status.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("OCR returned error/no text — no energy deducted");
            ShowNoTextScanned();
            yield break;
        }

        // Case 3: Text extracted but no ingredient matched the database
        if (!ingredientData.IsValid())
        {
            ShowNoIngredientFound();
            yield break;
        }

        // Case 4: Text extracted but scan failed for other reasons
        if (ingredientData.status != "success")
        {
            ShowNoIngredientFound();
            yield break;
        }

        // Store current product fingerprint for later use
        currentProductFingerprint = ingredientData.fingerprint;

        // ===== PRODUCT MANAGER INTEGRATION =====
        // Check if this product has been scanned too many times
        if (!ProductManager.CanScanProduct(ingredientData.fingerprint))
        {
            TimeSpan cooldown = ProductManager.GetProductCooldown(ingredientData.fingerprint);
            int remainingScans = ProductManager.GetRemainingScans(ingredientData.fingerprint);
            string timeString = FormatTimeSpan(cooldown);

            string errorMessage;
            if (remainingScans == 0)
            {
                errorMessage = $"This product has reached its maximum scans (3/3).\nTry again in {timeString}.";
            }
            else
            {
                errorMessage = $"Product limit reached. {remainingScans} scans remaining after cooldown.\nTry again in {timeString}.";
            }

            ShowError(errorMessage);

            yield break;
        }

        // Check global cooldown (already checked in button handlers, but double-check)
        if (!CooldownSystem.CanScanAnyIngredient())
        {
            TimeSpan remaining = CooldownSystem.GetGlobalCooldown();
            ShowError($"Please wait {remaining.Seconds} seconds before scanning again.");
            yield break;
        }

        // SUCCESS! Process the scanned ingredient
        ProcessSuccessfulScan(ingredientData);
    }

    void ProcessSuccessfulScan(IngredientData ingredientData)
    {
        // CHECK: Player must have at least one unlocked enerling to proceed to battle
        bool hasUnlockedEnerling = false;
        if (PersistentDataManager.Instance != null)
        {
            hasUnlockedEnerling = PersistentDataManager.Instance.GetTotalUnlockedCount() > 0;
        }
        else if (ingredientDatabase != null)
        {
            hasUnlockedEnerling = ingredientDatabase.GetUnlockedIngredients().Count > 0;
        }

        if (!hasUnlockedEnerling)
        {
            Debug.Log("[Scan] Player has no unlocked enerlings — cannot proceed to battle.");
            ShowNoUnlockedEnerlingWarning();
            return;
        }

        // Deduct energy on every successful scan (single deduction point)
        if (GameDataManager.Instance != null)
        {
            int before = GameDataManager.Instance.GetOCRBattleEnergy();
            bool success = GameDataManager.Instance.UseOCRBattleEnergy();
            int after = GameDataManager.Instance.GetOCRBattleEnergy();
            Debug.Log($"[Scan] UseOCRBattleEnergy returned {success}. Energy: {before} → {after}");
        }
        else
        {
            Debug.LogError("[Scan] GameDataManager.Instance is NULL — cannot deduct energy!");
        }
        RefreshLifeEnergyUI();

        // Record in systems
        CooldownSystem.RecordScan(ingredientData.ingredient);
        ProductManager.RecordProductScan(ingredientData.fingerprint, ingredientData.ingredient);

        // Get the selected ingredient name
        selectedEnerlingName = ingredientData.ingredient;

        // Show scanning animation/effect
        StartCoroutine(ShowScanSuccessEffect());

        // Save to persistent data
        SaveScannedEnerling(selectedEnerlingName);

        // Build scan info for display
        string category = IngredientCategory.GetCategory(selectedEnerlingName);
        int scanCount = ProductManager.GetProductScanCount(ingredientData.fingerprint);
        int remaining = ProductManager.GetRemainingScans(ingredientData.fingerprint);

        UpdateStatus($"Found: {selectedEnerlingName} ({category})");

        // Show scan count info on warningText
        string scanInfo;
        if (remaining > 0)
        {
            scanInfo = $"Scan {scanCount}/3 completed!\nYou can scan this ingredient list {remaining} more time{(remaining > 1 ? "s" : "")}.";
        }
        else
        {
            scanInfo = $"Scan limit reached! (3/3)\nThis ingredient list will reset after 24 hours.";
        }
        ShowWarning(scanInfo);

        // Transition to next scene (with delay so player reads the info)
        StartCoroutine(TransitionToNextScene());
    }

    // ==================== MOCK SCAN SYSTEM ====================

    IEnumerator MockScanCoroutine()
    {
        // Simulate scanning delay
        UpdateStatus("Analyzing image...");
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("Detecting text...");
        yield return new WaitForSeconds(0.5f);

        UpdateStatus("Matching ingredients...");
        yield return new WaitForSeconds(0.5f);

        // Randomly select an enerling from database
        if (ingredientDatabase != null && ingredientDatabase.ingredients.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, ingredientDatabase.ingredients.Count);
            selectedEnerlingName = ingredientDatabase.ingredients[randomIndex].ingredientName;
            var selectedEnerling = ingredientDatabase.ingredients[randomIndex];

            Debug.Log($"Mock scan selected: {selectedEnerlingName} from {selectedEnerling.kingdom}");

            // Create mock ingredient data with consistent fingerprint for testing
            string mockFingerprint = "MOCK_" + selectedEnerlingName.GetHashCode().ToString();
            currentProductFingerprint = mockFingerprint;

            // ===== MOCK SCAN: Skip product scan limit (only enforced on mobile builds) =====

            // Check global cooldown
            if (!CooldownSystem.CanScanAnyIngredient())
            {
                TimeSpan remaining = CooldownSystem.GetGlobalCooldown();
                ShowError($"Please wait {remaining.Seconds} seconds before scanning again.");
                ResetProcessingState();
                yield break;
            }

            IngredientData mockData = new IngredientData
            {
                ingredient = selectedEnerlingName,
                status = "success",
                fingerprint = mockFingerprint,
                total_detected = UnityEngine.Random.Range(1, 4),
                mode = "mock",
                all_ingredients = new string[] { selectedEnerlingName }
            };

            // Update scan count display for mock
            UpdateScanCountDisplay(mockFingerprint);

            // Process as if real scan
            ProcessSuccessfulScan(mockData);
        }
        else
        {
            UpdateStatus("Database not loaded");
            ShowError("No ingredients in database");
            ResetProcessingState();
        }
    }

    IEnumerator ShowScanSuccessEffect()
    {
        if (fadePanel != null)
        {
            yield return StartCoroutine(FadeIn());
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(FadeOut());
        }
    }

    void SaveScannedEnerling(string enerlingName)
    {
        if (PersistentDataManager.Instance != null)
        {
            // Save as opponent enerling (this is what we fight against)
            PersistentDataManager.Instance.SaveOpponentEnerling(enerlingName);

            // Also save it as selected for consistency
            PersistentDataManager.Instance.SaveSelectedEnerling(enerlingName);

            Debug.Log($"Saved scanned opponent enerling: {enerlingName}");
        }
        else
        {
            Debug.LogWarning("PersistentDataManager not found. Using PlayerPrefs directly.");

            // Create a temporary save using PlayerPrefs directly
            PlayerPrefs.SetString("OpponentEnerling", enerlingName);
            PlayerPrefs.SetString("SelectedEnerling", enerlingName);
            PlayerPrefs.Save();
        }
    }

    IEnumerator TransitionToNextScene()
    {
        // Let the player read the scan info shown on warningText
        yield return new WaitForSeconds(2.5f);

        // Show transition message on statusText
        UpdateStatus($"Preparing battle...");

        // Brief pause before fade
        yield return new WaitForSeconds(0.5f);

        // Fade out
        if (fadePanel != null)
        {
            yield return StartCoroutine(FadeIn());
            yield return new WaitForSeconds(sceneTransitionDelay * 0.5f);
        }

        // Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("Next scene name not set!");
            ResetScanState();
        }
    }

    // ==================== ERROR HANDLING ====================

    void ShowError(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
        }

        if (blurPanel != null)
            blurPanel.SetActive(true);

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        if (noIngredientText != null)
        {
            noIngredientText.text = message;
            noIngredientText.gameObject.SetActive(true);
        }

        ResetProcessingState();
    }

    void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
        }
    }

    void ShowNoTextScanned()
    {
        // No text extracted at all — accidental scan, no energy deducted
        if (noIngredientText != null)
        {
            noIngredientText.text = "No ingredients was detected from the scan.\nMake sure the camera is pointed at an ingredient list and try again.";
            noIngredientText.gameObject.SetActive(true);
        }
        ShowWarning("No text scanned — no energy was used.");

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        ResetProcessingState();
    }

    void ShowNoIngredientFound()
    {
        // Text was extracted but no ingredient matched the database — energy was deducted
        if (noIngredientText != null)
        {
            noIngredientText.text = "No ingredient found from our database.\nTry scanning a different product with a visible ingredient list.";
            noIngredientText.gameObject.SetActive(true);
        }
        ShowWarning("No ingredient detected — 1 energy was used.");

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        ResetProcessingState();
    }

    void ShowNoUnlockedEnerlingWarning()
    {
        // Scan was valid, but player has no unlocked enerlings to fight with — no energy deducted
        if (noIngredientText != null)
        {
            noIngredientText.text = "You don't have any Enerlings yet!\nCatch your first Enerling through the story before you can battle.";
            noIngredientText.gameObject.SetActive(true);
        }
        ShowWarning("No Enerlings unlocked — scan was not used.");

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);

        ResetProcessingState();
    }

    void ClearError()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (blurPanel != null)
            blurPanel.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (noIngredientText != null)
            noIngredientText.gameObject.SetActive(false);
    }

    void ResetProcessingState()
    {
        isProcessing = false;
        waitingForPluginResponse = false;
        UpdateButtonStates();
    }

    void ResetScanState()
    {
        isProcessing = false;
        waitingForPluginResponse = false;
        selectedEnerlingName = "";
        currentProductFingerprint = "";
        ClearError();
        UpdateButtonStates();

        if (cameraPreview != null && currentImage != null)
        {
            cameraPreview.texture = null;
            cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
        }

        // Clean up expired products
        ProductManager.CleanupExpiredProducts();
    }

    // ==================== UI UTILITIES ====================

    IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        fadePanel.SetActive(true);
        Image panelImage = fadePanel.GetComponent<Image>();
        if (panelImage == null) yield break;

        float elapsedTime = 0f;
        Color c = panelImage.color;
        c.a = 0;
        panelImage.color = c;

        while (elapsedTime < fadeDuration)
        {
            c.a = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            panelImage.color = c;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        c.a = 1;
        panelImage.color = c;
    }

    IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        Image panelImage = fadePanel.GetComponent<Image>();
        if (panelImage == null) yield break;

        float elapsedTime = 0f;
        Color c = panelImage.color;
        c.a = 1;

        while (elapsedTime < fadeDuration)
        {
            c.a = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            panelImage.color = c;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        c.a = 0;
        panelImage.color = c;
        fadePanel.SetActive(false);
    }

    // Helper method to format time for display
    string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        else if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        else
            return $"{timeSpan.Seconds}s";
    }

    void OnDestroy()
    {
        // Cleanup
#if UNITY_ANDROID && !UNITY_EDITOR
        if (liveCameraTexture != null && liveCameraTexture.isPlaying)
            liveCameraTexture.Stop();
#endif

        if (currentImage != null)
            Destroy(currentImage);

        if (captureCooldownCoroutine != null)
            StopCoroutine(captureCooldownCoroutine);

        // Cleanup Android plugin
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.nutriventure.mlkit.MLKitOcr"))
            {
                pluginClass.CallStatic("cleanup");
            }
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Plugin cleanup error: " + e.Message);
        }
    }

    // Optional: Method to switch between mock and real scanning
    public void SetScanMode(bool useMock)
    {
        useMockScan = useMock;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!useMockScan && liveCameraTexture == null)
        {
            StartCoroutine(StartCameraPreview());
        }
#endif
    }

    // ========================================================================
    //  SCAN COUNT DISPLAY (uses warningText)
    // ========================================================================

    void UpdateScanCountDisplay(string fingerprint)
    {
        if (warningText == null) return;
        if (string.IsNullOrEmpty(fingerprint)) return;

        int scanCount = ProductManager.GetProductScanCount(fingerprint);
        int remaining = ProductManager.GetRemainingScans(fingerprint);

        if (scanCount == 0)
        {
            ShowWarning("First time scanning this ingredient list!\nYou have 3 scans available.");
        }
        else if (remaining > 0)
        {
            ShowWarning($"Scan {scanCount}/3 completed!\nYou can scan this ingredient list {remaining} more time{(remaining > 1 ? "s" : "")}.");
        }
        else
        {
            TimeSpan cooldown = ProductManager.GetProductCooldown(fingerprint);
            string timeStr = FormatTimeSpan(cooldown);
            ShowWarning($"Scan limit reached! (3/3)\nThis ingredient list resets in {timeStr}.");
        }
    }

    // ========================================================================
    //  HEART (LIFE) UI
    // ========================================================================

    void BuildHeartImages()
    {
        if (heartContainer == null) return;

        heartImages.Clear();

        // Remove any existing children
        for (int i = heartContainer.childCount - 1; i >= 0; i--)
            Destroy(heartContainer.GetChild(i).gameObject);

        int maxLives = GameDataManager.Instance != null ? GameDataManager.Instance.GetOCRBattleMaxLives() : 5;

        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartGO.transform.SetParent(heartContainer, false);

            RectTransform rt = heartGO.GetComponent<RectTransform>();
            rt.sizeDelta = heartSize;

            Image img = heartGO.GetComponent<Image>();
            img.sprite = fullHeartSprite;
            img.preserveAspect = true;

            heartImages.Add(img);
        }
    }

    void UpdateHeartUI()
    {
        if (heartImages == null || heartImages.Count == 0) return;
        if (GameDataManager.Instance == null) return;

        int currentLives = GameDataManager.Instance.GetOCRBattleLives();

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;
            heartImages[i].sprite = (i < currentLives) ? fullHeartSprite : emptyHeartSprite;
        }
    }

    // ========================================================================
    //  ENERGY & REGEN UI (uses statusText)
    // ========================================================================

    void RefreshLifeEnergyUI()
    {
        if (GameDataManager.Instance == null) return;

        UpdateHeartUI();

        int curEnergy = GameDataManager.Instance.GetOCRBattleEnergy();
        int maxEnergy = GameDataManager.Instance.GetOCRBattleMaxEnergy();
        int curLives = GameDataManager.Instance.GetOCRBattleLives();
        int maxLives = GameDataManager.Instance.GetOCRBattleMaxLives();

        bool lifeFull = curLives >= maxLives;
        bool energyFull = curEnergy >= maxEnergy;

        // Update dedicated energy text if available
        if (energyText != null)
        {
            energyText.text = $"{curEnergy}/{maxEnergy}";
        }

        // Update regen timer texts
        if (lifeRegenTimerText != null)
        {
            if (lifeFull)
            {
                lifeRegenTimerText.gameObject.SetActive(false);
            }
            else
            {
                lifeRegenTimerText.gameObject.SetActive(true);
                float lifeRemain = GameDataManager.Instance.GetOCRLifeRegenRemainingSeconds();
                lifeRegenTimerText.text = $"Regen in: {FormatRegenTime(lifeRemain)}";
            }
        }

        if (energyRegenTimerText != null)
        {
            if (energyFull)
            {
                energyRegenTimerText.gameObject.SetActive(false);
            }
            else
            {
                energyRegenTimerText.gameObject.SetActive(true);
                float energyRemain = GameDataManager.Instance.GetOCREnergyRegenRemainingSeconds();
                energyRegenTimerText.text = $"Regen in: {FormatRegenTime(energyRemain)}";
            }
        }

        // Fallback: show compact info in statusText if no dedicated energy UI field
        // Only update statusText when idle (not during active scan messages)
        if (energyText == null && !isProcessing && !waitingForPluginResponse)
        {
            string info = $"Energy: {curEnergy}/{maxEnergy}";
            if (!lifeFull)
            {
                float lifeRemain = GameDataManager.Instance.GetOCRLifeRegenRemainingSeconds();
                info += $"  |  Heart regen: {FormatRegenTime(lifeRemain)}";
            }
            if (!energyFull)
            {
                float energyRemain = GameDataManager.Instance.GetOCREnergyRegenRemainingSeconds();
                info += $"  |  Energy regen: {FormatRegenTime(energyRemain)}";
            }
            UpdateStatus(info);
        }
    }

    static string FormatRegenTime(float totalSeconds)
    {
        if (totalSeconds <= 0f) return "00:00";
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}