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

    // State
    private bool isProcessing = false;
    private bool isCaptureOnCooldown = false;
    private Coroutine captureCooldownCoroutine;
    private string selectedEnerlingName = "";
    private Texture2D currentImage;
    private bool waitingForPluginResponse = false;
    private float maxProcessingTime = 10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private WebCamTexture liveCameraTexture;
#endif

    void Start()
    {
        SetupUI();
        InitializeCameraPreview();

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
        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

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
        
        if (ingredientData == null)
        {
            Debug.LogError("Failed to parse OCR result");
            ShowError("Failed to process scan results");
            yield break;
        }
        
        if (!ingredientData.IsValid())
        {
            ShowError("No ingredient detected");
            yield break;
        }

        if (ingredientData.status != "success")
        {
            ShowError("Scan failed: " + ingredientData.status);
            yield break;
        }

        // Check for duplicate product
        if (ingredientData.IsDuplicateProduct())
        {
            TimeSpan cooldown = ProductManager.GetProductCooldown(ingredientData.fingerprint);
            string timeString = FormatTimeSpan(cooldown);
            ShowError($"Product already scanned 3 times today.\nTry again in {timeString}.");
            yield break;
        }

        // Check global cooldown
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
        // Record in systems
        CooldownSystem.RecordScan(ingredientData.ingredient);
        ProductManager.RecordProductScan(ingredientData.fingerprint, ingredientData.ingredient);
        
        // Get the selected ingredient name
        selectedEnerlingName = ingredientData.ingredient;
        
        // Show scanning animation/effect
        StartCoroutine(ShowScanSuccessEffect());
        
        // Save to persistent data
        SaveScannedEnerling(selectedEnerlingName);
        
        // Update status
        string category = IngredientCategory.GetCategory(selectedEnerlingName);
        UpdateStatus($"Found: {selectedEnerlingName} ({category})");
        
        // Transition to next scene
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

            // Create mock ingredient data
            IngredientData mockData = new IngredientData
            {
                ingredient = selectedEnerlingName,
                status = "success",
                fingerprint = System.Guid.NewGuid().ToString().Substring(0, 8),
                total_detected = UnityEngine.Random.Range(1, 4),
                mode = "mock",
                all_ingredients = new string[] { selectedEnerlingName }
            };

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
        // Show final success message
        UpdateStatus($"Ingredient scanned! Preparing battle...");

        // Wait a moment
        yield return new WaitForSeconds(1f);

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
        ClearError();
        UpdateButtonStates();
        
        if (cameraPreview != null && currentImage != null)
        {
            cameraPreview.texture = null;
            cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
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
}