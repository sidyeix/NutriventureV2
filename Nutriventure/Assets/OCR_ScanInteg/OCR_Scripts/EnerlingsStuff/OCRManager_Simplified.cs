using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
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

        UpdateStatus("Ready to scan ingredient");
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
        bool canInteract = !isProcessing && !isCaptureOnCooldown;
        
        if (captureButton != null)
            captureButton.interactable = canInteract;
        
        if (galleryButton != null)
            galleryButton.interactable = !isProcessing;
        
        if (instructionsButton != null)
            instructionsButton.interactable = !isProcessing;
        
        if (exitButton != null)
            exitButton.interactable = !isProcessing;
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            Debug.Log("OCR Status: " + message);
        }
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
        // In Editor, show mock mode but don't try to use camera
        UpdateStatus("Unity Editor - Using mock scan mode");
        if (cameraPreview != null)
        {
            cameraPreview.color = Color.gray;
        }
#endif
    }

    // ==================== CAMERA FUNCTIONALITY ====================

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator StartCameraPreview()
    {
        Debug.Log("Starting camera preview...");
        UpdateStatus("Initializing camera...");
        
        // First, check if we already have permission
        if (!HasCameraPermission())
        {
            UpdateStatus("Requesting camera permission...");
            yield return RequestCameraPermission();
            
            if (!HasCameraPermission())
            {
                UpdateStatus("Camera permission denied. Please enable in device settings.");
                useMockScan = true;
                yield break;
            }
        }

        // Get available cameras
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log("Found " + (devices?.Length ?? 0) + " camera devices");
        
        if (devices == null || devices.Length == 0)
        {
            UpdateStatus("No camera found on this device");
            useMockScan = true;
            yield break;
        }

        // Log all cameras
        foreach (var device in devices)
        {
            Debug.Log("Camera: " + device.name + " (Front: " + device.isFrontFacing + ")");
        }

        // Choose back camera if available
        string cameraName = devices[0].name;
        bool foundBackCamera = false;
        
        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                foundBackCamera = true;
                Debug.Log("Selected back camera: " + cameraName);
                break;
            }
        }
        
        if (!foundBackCamera)
        {
            Debug.Log("No back camera found, using front camera: " + cameraName);
        }

        try
        {
            // Create and start webcam texture with lower resolution for better performance
            liveCameraTexture = new WebCamTexture(cameraName, 1920, 1080, 30);
            
            // Hook up the texture to the RawImage
            if (cameraPreview != null)
            {
                cameraPreview.texture = liveCameraTexture;
                cameraPreview.color = Color.white;
                // Make sure the RawImage uses the correct aspect ratio
                cameraPreview.uvRect = new Rect(0, 0, 1, 1);
            }
            
            // Start the camera
            liveCameraTexture.Play();
            
            // Wait a moment for the camera to start
            yield return new WaitForSeconds(0.5f);
            
            // Check if camera started successfully
            if (liveCameraTexture != null && liveCameraTexture.isPlaying)
            {
                Debug.Log("Camera started successfully. Width: " + liveCameraTexture.width + ", Height: " + liveCameraTexture.height);
                UpdateStatus("Camera ready! Point at ingredient label and tap Capture");
                
                // Adjust the RawImage aspect ratio to match camera
                StartCoroutine(AdjustCameraAspectRatio());
            }
            else
            {
                UpdateStatus("Camera failed to start");
                if (liveCameraTexture != null)
                {
                    liveCameraTexture.Stop();
                    liveCameraTexture = null;
                }
                useMockScan = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Camera initialization error: " + e.Message + "\n" + e.StackTrace);
            UpdateStatus("Camera error: " + e.Message);
            useMockScan = true;
        }
    }

    IEnumerator AdjustCameraAspectRatio()
    {
        if (liveCameraTexture == null || cameraPreview == null) yield break;
        
        // Wait for camera texture to initialize properly
        int maxWait = 50; // 50 frames max
        int currentWait = 0;
        
        while ((liveCameraTexture.width <= 100 || liveCameraTexture.height <= 100) && currentWait < maxWait)
        {
            currentWait++;
            yield return null;
        }
        
        if (liveCameraTexture.width > 100 && liveCameraTexture.height > 100)
        {
            float aspectRatio = (float)liveCameraTexture.width / (float)liveCameraTexture.height;
            Debug.Log("Camera aspect ratio: " + aspectRatio + " (" + liveCameraTexture.width + "x" + liveCameraTexture.height + ")");
            
            // Adjust the RawImage to maintain aspect ratio
            RectTransform rect = cameraPreview.GetComponent<RectTransform>();
            if (rect != null)
            {
                float width = rect.rect.height * aspectRatio;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
        }
    }

    bool HasCameraPermission()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
            using (AndroidJavaObject packageName = currentActivity.Call<AndroidJavaObject>("getPackageName"))
            {
                int permissionGranted = packageManager.Call<int>("checkPermission", 
                    "android.permission.CAMERA", packageName);
                bool hasPermission = (permissionGranted == 0);
                Debug.Log("Camera permission check: " + (hasPermission ? "Granted" : "Denied"));
                return hasPermission;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Permission check error: " + e.Message);
            return false;
        }
    }

    IEnumerator RequestCameraPermission()
    {
        Debug.Log("Requesting camera permission...");
        
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string[] permissions = new string[] { "android.permission.CAMERA" };
                currentActivity.Call("requestPermissions", permissions, 0);
                Debug.Log("Camera permission requested");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Permission request error: " + e.Message);
        }
        
        // Wait for permission dialog
        yield return new WaitForSeconds(1f);
    }

    IEnumerator RestartCameraPreview()
    {
        UpdateStatus("Restarting camera...");
        
        if (liveCameraTexture != null)
        {
            if (liveCameraTexture.isPlaying) 
            {
                liveCameraTexture.Stop();
            }
            liveCameraTexture = null;
        }

        yield return new WaitForSeconds(0.3f);
        
        if (cameraPreview != null)
        {
            cameraPreview.texture = null;
            cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
        
        yield return StartCoroutine(StartCameraPreview());
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

        if (liveCameraTexture == null || !liveCameraTexture.isPlaying)
        {
            UpdateStatus("Camera not ready. Please wait...");
            ResetProcessingState();
            yield break;
        }

        // Wait for camera to stabilize
        yield return new WaitForSeconds(0.5f);
        
        UpdateStatus("Capturing photo...");
        yield return new WaitForEndOfFrame();

        try
        {
            // Create texture from webcam
            Texture2D photo = new Texture2D(liveCameraTexture.width, liveCameraTexture.height);
            photo.SetPixels(liveCameraTexture.GetPixels());
            photo.Apply();

            // Store the captured image
            if (currentImage != null) 
                Destroy(currentImage);
            
            currentImage = photo;
            
            // Show the captured photo in preview
            if (cameraPreview != null)
            {
                cameraPreview.texture = currentImage;
                cameraPreview.color = Color.white;
            }

            UpdateStatus("Photo captured! Processing...");
            
            // Process the captured image
            StartCoroutine(ProcessRealScan());
        }
        catch (System.Exception e)
        {
            Debug.LogError("Photo capture error: " + e.Message + "\n" + e.StackTrace);
            UpdateStatus("Capture error: " + e.Message);
            ResetProcessingState();
        }
    }

    IEnumerator ProcessRealScan()
    {
        // Simulate OCR processing time
        yield return new WaitForSeconds(1.5f);

        // For now, use mock result
        StartCoroutine(MockScanCoroutine());
    }

    IEnumerator PickImageViaNativeGallery()
    {
        isProcessing = true;
        UpdateButtonStates();
        UpdateStatus("Opening gallery...");
        
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((string path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                UpdateStatus("No image selected");
                isProcessing = false;
                UpdateButtonStates();
                return;
            }
            
            StartCoroutine(LoadImageFromPath(path));
        }, "Select an image", "image/*");

        Debug.Log("Gallery permission: " + permission);
        yield return null;
    }

    IEnumerator LoadImageFromPath(string imagePath)
    {
        bool success = false;
        
        try
        {
            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);

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
            }
        }
        catch (System.Exception e) 
        { 
            Debug.LogError("Image load error: " + e.Message);
            UpdateStatus("Error loading image"); 
        }

        isProcessing = false;
        UpdateButtonStates();

        if (success) 
        {
            StartCoroutine(ProcessRealScan());
        }
        
        yield return null;
    }

#endif

    // ==================== BUTTON HANDLERS ====================

    public void OnCaptureButtonClicked()
    {
        if (isProcessing || isCaptureOnCooldown) 
        {
            Debug.Log("Capture blocked - Processing: " + isProcessing + ", OnCooldown: " + isCaptureOnCooldown);
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
            Debug.Log("Starting mock scan...");
            StartCoroutine(MockScanCoroutine());
        }
        else
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("Starting real camera capture...");
            StartCoroutine(TakePhotoCoroutine());
#else
            StartCoroutine(MockScanCoroutine());
#endif
        }
    }

    public void OnGalleryButtonClicked()
    {
        if (isProcessing) return;

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
        Debug.Log("Retry button clicked");
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

    // ==================== SCANNING LOGIC ====================

    IEnumerator MockScanCoroutine()
    {
        Debug.Log("MockScanCoroutine started");
        
        // Simulate scanning delay
        yield return new WaitForSeconds(1.5f);

        // Randomly select an enerling from database
        if (ingredientDatabase != null && ingredientDatabase.ingredients.Count > 0)
        {
            int randomIndex = Random.Range(0, ingredientDatabase.ingredients.Count);
            selectedEnerlingName = ingredientDatabase.ingredients[randomIndex].ingredientName;
            var selectedEnerling = ingredientDatabase.ingredients[randomIndex];

            Debug.Log($"Mock scan selected: {selectedEnerlingName} from {selectedEnerling.kingdom}");

            // Show scanning animation/effect
            yield return StartCoroutine(ShowScanSuccessEffect());

            // Save to persistent data
            SaveScannedEnerling(selectedEnerlingName);

            // Update status
            UpdateStatus($"Found: {selectedEnerlingName}");

            // Transition to next scene
            yield return StartCoroutine(TransitionToNextScene());
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
        Debug.Log("Showing error: " + message);
        
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
        UpdateButtonStates();
    }

    void ResetScanState()
    {
        Debug.Log("Resetting scan state");
        isProcessing = false;
        selectedEnerlingName = "";
        ClearError();
        UpdateButtonStates();
        
        if (cameraPreview != null)
        {
            // Restore live camera view if available
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!useMockScan && liveCameraTexture != null && liveCameraTexture.isPlaying)
            {
                cameraPreview.texture = liveCameraTexture;
                cameraPreview.color = Color.white;
            }
            else
#endif
            {
                cameraPreview.texture = null;
                cameraPreview.color = new Color(0.2f, 0.2f, 0.2f);
            }
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

    void OnDestroy()
    {
        // Cleanup
#if UNITY_ANDROID && !UNITY_EDITOR
        if (liveCameraTexture != null)
        {
            if (liveCameraTexture.isPlaying)
                liveCameraTexture.Stop();
            liveCameraTexture = null;
        }
#endif

        if (currentImage != null)
            Destroy(currentImage);

        if (captureCooldownCoroutine != null)
            StopCoroutine(captureCooldownCoroutine);
    }

    void OnApplicationPause(bool pauseStatus)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pauseStatus && liveCameraTexture != null && liveCameraTexture.isPlaying)
        {
            liveCameraTexture.Pause();
        }
        else if (!pauseStatus && liveCameraTexture != null && !liveCameraTexture.isPlaying)
        {
            liveCameraTexture.Play();
        }
#endif
    }

    // Debug method to check camera status
    public void CheckCameraStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (liveCameraTexture != null)
        {
            Debug.Log("Camera Status: " + (liveCameraTexture.isPlaying ? "Playing" : "Stopped"));
            Debug.Log("Camera Dimensions: " + liveCameraTexture.width + "x" + liveCameraTexture.height);
        }
        else
        {
            Debug.Log("Camera: Not initialized");
        }
#endif
    }
}