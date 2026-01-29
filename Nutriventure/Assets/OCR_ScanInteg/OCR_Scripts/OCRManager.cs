using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class OCRManager : MonoBehaviour
{
    [Header("UI References")]
    public RawImage imagePreview;
    public TMP_Text statusText;
    public TMP_Text resultsTextIngredient;
    public TMP_Text resultsTextCategory;
    public TMP_Text resultsTextScan;
    public GameObject resultsPanel;
    public TMP_Text warningText;
    public GameObject blurPanel;
    public TMP_Text noIngredientText;

    [Header("Manual Controls")]
    public Button captureButton;
    public Button galleryButton;
    public Button exitButton;
    public Button instructionsButton;
    public Button retryButton;

    [Header("Battle Transition")]
    public Button battleButton;
    public string battleSceneName = "BattleScene";

    [Header("Feedback Effects")]
    public GameObject fadePanel;
    public float fadeDuration = 0.3f;
    public AudioSource audioSource;
    public AudioClip scanSound;

    [Header("Scene Switching")]
    [SerializeField] private string sceneName = "MainMenu";

    [Header("Ingredient Data")]
    public IngredientDatabase ingredientDatabase;
    public ModelManager modelManager;

    [Header("Capture Cooldown")]
    public float captureCooldownDuration = 2f;

    private Texture2D currentImage;
    private bool isProcessing = false;
    private bool isCapturing = false;
    private IngredientData currentIngredientData;
    private Coroutine cooldownUpdateCoroutine;
    private string currentProductFingerprint;
    private bool isCaptureOnCooldown = false;
    private Coroutine captureCooldownCoroutine;
    
    public bool IsTutorialActive { get; set; } = false;

#if UNITY_ANDROID && !UNITY_EDITOR
    private WebCamTexture liveCameraTexture;
#endif

    // ==================== INITIALIZATION ====================
    void Start()
    {
        SetupUI();
        
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        if (captureButton != null)
            captureButton.onClick.AddListener(OnCaptureButtonClicked);
        
        if (galleryButton != null)
            galleryButton.onClick.AddListener(OnGalleryButtonClicked);
        
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            retryButton.gameObject.SetActive(false);
        }

        if (battleButton != null)
        {
            battleButton.onClick.AddListener(OnBattleButtonClicked);
            battleButton.gameObject.SetActive(false);
        }

        StartCooldownUpdates();
        UpdateStatus("Initializing...");

    #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(StartCameraPreview());
    #else
        UpdateStatus("Ready - Take photo or select from gallery");
    #endif
    }

    void SetupUI()
    {
        if (imagePreview != null)
        {
            imagePreview.texture = null;
            imagePreview.color = new Color(0.2f, 0.2f, 0.2f);
        }

        if (blurPanel != null) 
            blurPanel.SetActive(false);
        
        if (noIngredientText != null) 
            noIngredientText.gameObject.SetActive(false);
        
        if (resultsPanel != null)
        {
            // Ensure results panel is properly set up
            CanvasGroup panelGroup = resultsPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = resultsPanel.AddComponent<CanvasGroup>();
            }
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }
        
        UpdateButtonStates();
    }

    public void UpdateButtonStates()
    {
        bool resultsActive = resultsPanel != null && resultsPanel.activeSelf;
        bool canInteract = !isProcessing && !isCapturing && !resultsActive && !isCaptureOnCooldown;
       
        if (captureButton != null) 
            captureButton.interactable = canInteract;
        
        if (galleryButton != null) 
            galleryButton.interactable = !isProcessing && !resultsActive;
        
        if (instructionsButton != null) 
            instructionsButton.interactable = !isProcessing && !resultsActive;
        
        if (exitButton != null) 
            exitButton.interactable = !isProcessing && !isCapturing;
    }

    void UpdateStatus(string message)
    {
        if (statusText != null) 
            statusText.text = message;
    }

    // ==================== BATTLE BUTTON ====================
    private void OnBattleButtonClicked()
    {
        if (!string.IsNullOrEmpty(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }

    // ==================== CAPTURE COOLDOWN MANAGEMENT ====================
    private void StartCaptureCooldown()
    {
        if (captureCooldownCoroutine != null)
            StopCoroutine(captureCooldownCoroutine);
       
        captureCooldownCoroutine = StartCoroutine(CaptureCooldownCoroutine());
    }

    private IEnumerator CaptureCooldownCoroutine()
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

    // ==================== COOLDOWN MANAGEMENT ====================
    void StartCooldownUpdates()
    {
        if (cooldownUpdateCoroutine != null)
            StopCoroutine(cooldownUpdateCoroutine);
       
        cooldownUpdateCoroutine = StartCoroutine(CooldownUpdateCoroutine());
    }

    IEnumerator CooldownUpdateCoroutine()
    {
        while (true)
        {
            ProductManager.UpdateCooldowns();
           
            if (!string.IsNullOrEmpty(currentProductFingerprint))
                UpdateProductCooldownUI(currentProductFingerprint);
           
            yield return new WaitForSeconds(1f);
        }
    }

    void UpdateProductCooldownUI(string fingerprint)
    {
        TimeSpan cooldown = ProductManager.GetProductCooldown(fingerprint);
       
        if (cooldown.TotalSeconds > 0 && currentProductFingerprint == fingerprint)
        {
            string timeText = $"Product available in: {cooldown:hh\\:mm\\:ss}";
           
            if (noIngredientText != null && noIngredientText.gameObject.activeInHierarchy)
                noIngredientText.text = $"Product maxed out!\nAvailable again in:\n{cooldown:hh\\:mm\\:ss}";
           
            if (statusText != null) 
                statusText.text = timeText;
        }
        else if (cooldown.TotalSeconds <= 0 && currentProductFingerprint == fingerprint)
        {
            if (noIngredientText != null && noIngredientText.gameObject.activeInHierarchy)
            {
                noIngredientText.text = "Product ready to scan again!";
                if (retryButton != null) 
                    retryButton.gameObject.SetActive(false);
                if (blurPanel != null) 
                    blurPanel.SetActive(false);
            }
           
            if (statusText != null && resultsPanel != null && !resultsPanel.activeInHierarchy)
                statusText.text = "Ready - Take photo or select from gallery";
           
            currentProductFingerprint = null;
        }
    }

    // ==================== BUTTON HANDLERS ====================
    public void OnRetryButtonClicked()
    {
        if (blurPanel != null) 
            blurPanel.SetActive(false);
        
        if (noIngredientText != null) 
            noIngredientText.gameObject.SetActive(false);
        
        if (retryButton != null) 
            retryButton.gameObject.SetActive(false);

        currentProductFingerprint = null;
        UpdateStatus("Restarting camera...");
        UpdateButtonStates();

        if (currentImage != null)
        {
            Destroy(currentImage);
            currentImage = null;
        }

    #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RestartCameraPreview());
    #else
        UpdateStatus("Ready - Take photo or select from gallery");
        if (imagePreview != null)
            imagePreview.color = new Color(0.2f, 0.2f, 0.2f);
    #endif
    }

    public void OnCaptureButtonClicked()
    {
        if (IsTutorialActive)
        {
            Debug.Log("Capture blocked - Tutorial active");
            return;
        }
   
        if (isProcessing || isCapturing || (resultsPanel != null && resultsPanel.activeSelf) || isCaptureOnCooldown)
        {
            Debug.Log($"Capture blocked - Processing: {isProcessing}, Capturing: {isCapturing}, ResultsActive: {resultsPanel != null && resultsPanel.activeSelf}, OnCooldown: {isCaptureOnCooldown}");
            return;
        }
       
        isCapturing = true;
        isProcessing = true;
        UpdateButtonStates();
       
        StartCaptureCooldown();
       
        #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(TakePhotoCoroutine());
        #else
        MockImageCapture();
        #endif
    }

    public void OnGalleryButtonClicked()
    {
        if (IsTutorialActive)
        {
            Debug.Log("Gallery blocked - Tutorial active");
            return;
        }
   
        if (isProcessing || isCapturing || (resultsPanel != null && resultsPanel.activeSelf)) 
            return;
       
        #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(PickImageViaNativeGallery());
        #else
        MockGalleryPick();
        #endif
    }

    public void OnExitButtonClicked()
    {
        if (resultsPanel != null && resultsPanel.activeSelf)
        {
            ClosePanel();
        }
        else
        {
            CloseAndGoToScene();
        }
    }

    // ==================== CAMERA & GALLERY ====================
    #if UNITY_ANDROID && !UNITY_EDITOR

    IEnumerator StartCameraPreview()
    {
        UpdateStatus("Requesting camera permission...");
        
        yield return RequestCameraPermission();
        
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            UpdateStatus("No camera found");
            yield break;
        }

        string chosenName = devices[0].name;
        foreach (var d in devices)
            if (!d.isFrontFacing) { chosenName = d.name; break; }

        try
        {
            liveCameraTexture = new WebCamTexture(chosenName, 1280, 720);
            if (imagePreview != null)
            {
                imagePreview.texture = liveCameraTexture;
                imagePreview.color = Color.white;
            }
            liveCameraTexture.Play();
            UpdateStatus("Camera preview active - Tap Capture to scan");
        }
        catch (Exception e)
        {
            UpdateStatus("Camera preview error: " + e.Message);
        }
    }

    IEnumerator RequestCameraPermission()
    {
        if (!HasCameraPermission())
        {
            yield return RequestPermission("android.permission.CAMERA");
            
            if (!HasCameraPermission())
            {
                UpdateStatus("Camera permission denied");
                yield break;
            }
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
        catch (Exception)
        {
            return false;
        }
    }

    IEnumerator RequestPermission(string permission)
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                string[] permissions = new string[] { permission };
                currentActivity.Call("requestPermissions", permissions, 0);
            }
        }
        catch (Exception e)
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
       
        if (imagePreview != null)
        {
            imagePreview.texture = null;
            imagePreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
       
        yield return StartCoroutine(StartCameraPreview());
        UpdateStatus("Camera preview active - Tap Capture to scan");
    }

    IEnumerator TakePhotoCoroutine()
    {
        UpdateStatus("Preparing capture...");

        yield return RequestCameraPermission();
        
        if (!HasCameraPermission())
        {
            UpdateStatus("Camera permission required");
            ResetProcessingState();
            yield break;
        }

        WebCamTexture tempWebcam = null;
        bool usingLive = (liveCameraTexture != null && liveCameraTexture.isPlaying);

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

            try
            {
                tempWebcam = new WebCamTexture(cameraName, 1280, 720);
                if (imagePreview != null)
                {
                    imagePreview.texture = tempWebcam;
                    imagePreview.color = Color.white;
                }
                tempWebcam.Play();
            }
            catch (Exception e)
            {
                UpdateStatus("Camera error: " + e.Message);
                ResetProcessingState();
                if (tempWebcam != null && tempWebcam.isPlaying) 
                    tempWebcam.Stop();
                yield break;
            }
            yield return new WaitForSeconds(1.2f);
        }
        else yield return null;

        UpdateStatus("Capturing photo...");
        yield return new WaitForEndOfFrame();

        WebCamTexture source = usingLive ? liveCameraTexture : tempWebcam;
        Texture2D photo = null;

        try
        {
            photo = new Texture2D(source.width, source.height);
            photo.SetPixels(source.GetPixels());
            photo.Apply();
        }
        catch (Exception e)
        {
            UpdateStatus("Capture error: " + e.Message);
            if (tempWebcam != null && tempWebcam.isPlaying) 
                tempWebcam.Stop();
            ResetProcessingState();
            yield break;
        }

        if (currentImage != null) 
            Destroy(currentImage);
        
        currentImage = photo;
        
        if (imagePreview != null)
        {
            imagePreview.texture = currentImage;
            imagePreview.color = Color.white;
        }

        yield return StartCoroutine(ProcessCurrentImage());

        if (tempWebcam != null && tempWebcam.isPlaying)
        {
            tempWebcam.Stop();
            if (liveCameraTexture != null && liveCameraTexture.isPlaying)
            {
                if (imagePreview != null)
                {
                    imagePreview.texture = liveCameraTexture;
                    imagePreview.color = Color.white;
                }
            }
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

    #endif

    IEnumerator LoadImageFromPath(string imagePath)
    {
        bool success = false;
        try
        {
            byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);

            if (texture.LoadImage(imageData))
            {
                if (currentImage != null) 
                    Destroy(currentImage);
                
                currentImage = texture;
                
                if (imagePreview != null)
                {
                    imagePreview.texture = currentImage;
                    imagePreview.color = Color.white;
                }
                
                UpdateStatus("Image loaded - Processing...");
                success = true;
            }
            else 
                UpdateStatus("Failed to load image");
        }
        catch (Exception e) 
        { 
            UpdateStatus("Error loading image: " + e.Message); 
        }

        isProcessing = false;
        UpdateButtonStates();

        if (success) 
            yield return StartCoroutine(ProcessCurrentImage());
    }

    // ==================== OCR PROCESSING ====================
    IEnumerator ProcessCurrentImage()
    {
        if (currentImage == null)
        {
            UpdateStatus("No image to process");
            ResetProcessingState();
            yield break;
        }

        UpdateStatus("Processing image with OCR...");
        byte[] imageBytes = currentImage.EncodeToJPG(80);
        string base64Image = Convert.ToBase64String(imageBytes);

        bool ocrStarted = false;
        string ocrError = null;

        try
        {
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.nutriventure.mlkit.MLKitOcr"))
            {
                pluginClass.CallStatic("processManualImage", base64Image, gameObject.name, "OnOCRResult");
                ocrStarted = true;
            }
        }
        catch (Exception e) 
        { 
            ocrError = e.Message; 
        }

        if (!ocrStarted)
        {
            UpdateStatus("OCR Error: " + (ocrError ?? "Failed to start OCR"));
            ResetProcessingState();
        }
    }

    private void ResetProcessingState()
    {
        isProcessing = false;
        isCapturing = false;
        UpdateButtonStates();
    }

    // ==================== OCR RESULT HANDLING ====================
    public void OnOCRResult(string jsonResult)
    {
        Debug.Log("OCR Result: " + jsonResult);
        StartCoroutine(HandleOCRResultCoroutine(jsonResult));
    }

    IEnumerator HandleOCRResultCoroutine(string jsonResult)
    {
        ResetProcessingState();
       
        yield return null;

        IngredientData ingredientData = JsonParser.ParseIngredientResponse(jsonResult);
        
        if (ingredientData == null)
        {
            Debug.LogError("Failed to parse OCR result: " + jsonResult);
            ShowErrorUI("Failed to parse scan results");
            yield break;
        }
        
        currentIngredientData = ingredientData;
        currentProductFingerprint = ingredientData.fingerprint;

        if (!ingredientData.IsValid())
        {
            ShowErrorUI("No Ingredient/s detected... Please try again");
            yield break;
        }

        if (ingredientData.status != "success")
        {
            ShowErrorUI("Scan failed: " + ingredientData.status);
            yield break;
        }

        if (ingredientData.IsDuplicateProduct())
        {
            TimeSpan cooldown = ProductManager.GetProductCooldown(ingredientData.fingerprint);
            currentProductFingerprint = ingredientData.fingerprint;
            UpdateProductCooldownUI(ingredientData.fingerprint);

            if (blurPanel != null) 
                blurPanel.SetActive(true);
            
            if (noIngredientText != null)
            {
                noIngredientText.text = $"Product maxed out!\nAvailable again in:\n{cooldown:hh\\:mm\\:ss}";
                noIngredientText.gameObject.SetActive(true);
            }
            
            if (retryButton != null) 
                retryButton.gameObject.SetActive(true);
            
            yield break;
        }

        ProductManager.RecordProductScan(ingredientData.fingerprint, ingredientData.ingredient);
        ClearErrorUI();
        currentProductFingerprint = null;
        StartCoroutine(ScanSuccessSequence(ingredientData));
    }

    void ShowErrorUI(string message)
    {
        UpdateStatus(message);
        if (blurPanel != null) 
            blurPanel.SetActive(true);
        
        if (noIngredientText != null)
        {
            noIngredientText.text = message;
            noIngredientText.gameObject.SetActive(true);
        }
        
        if (retryButton != null) 
            retryButton.gameObject.SetActive(true);
    }

    void ClearErrorUI()
    {
        if (blurPanel != null) 
            blurPanel.SetActive(false);
        
        if (noIngredientText != null) 
            noIngredientText.gameObject.SetActive(false);
        
        if (retryButton != null) 
            retryButton.gameObject.SetActive(false);
    }

    IEnumerator ScanSuccessSequence(IngredientData ingredientData)
    {
        if (ingredientData == null)
        {
            Debug.LogError("ScanSuccessSequence: ingredientData is null!");
            ShowErrorUI("Failed to process ingredient data");
            yield break;
        }
    
        if (string.IsNullOrEmpty(ingredientData.ingredient))
        {
            Debug.LogError("ScanSuccessSequence: ingredient name is null or empty!");
            ShowErrorUI("No ingredient detected");
            yield break;
        }
    
        yield return StartCoroutine(FadeOut());
        ingredientSoundEffects();
        
        Debug.Log($"ScanSuccessSequence - Raw ingredient from JSON: '{ingredientData.ingredient}'");
        
        if (ingredientData.IsValid())
        {
            DisplayIngredient(ingredientData);
        }
        else
        {
            Debug.LogError($"ScanSuccessSequence: Invalid ingredient data. Status: {ingredientData.status}");
            ShowErrorUI("Invalid ingredient data received");
            yield break;
        }
        
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn());
    }

    // ==================== DISPLAY INGREDIENT ====================
    void DisplayIngredient(IngredientData ingredientData)
    {
        if (resultsPanel == null)
        {
            Debug.LogError("ResultsPanel is null!");
            return;
        }
        
        // Clear any error UI before showing results
        ClearErrorUI();
        
        // Ensure the results panel is interactable
        CanvasGroup panelGroup = resultsPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null)
        {
            panelGroup = resultsPanel.AddComponent<CanvasGroup>();
        }
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        
        resultsPanel.SetActive(true);
        
        if (galleryButton != null) 
            galleryButton.gameObject.SetActive(false);
        
        if (captureButton != null) 
            captureButton.gameObject.SetActive(false);
        
        if (instructionsButton != null) 
            instructionsButton.gameObject.SetActive(false);

        if (battleButton != null) 
            battleButton.gameObject.SetActive(true);

        if (ingredientData == null)
        {
            Debug.LogError("IngredientData is null!");
            return;
        }
        
        int productScanCount = ProductManager.GetProductScanCount(ingredientData.fingerprint);
        
        Debug.Log($"DisplayIngredient - Received ingredient: '{ingredientData.ingredient}'");
        
        string category = IngredientCategory.GetCategory(ingredientData.ingredient);
        Color categoryColor = IngredientCategory.GetCategoryColor(category);
        
        Debug.Log($"DisplayIngredient - Category determined: '{category}' for ingredient '{ingredientData.ingredient}'");
        
        if (resultsTextIngredient != null) 
            resultsTextIngredient.text = $"{ingredientData.ingredient}";
        
        if (resultsTextCategory != null) 
            resultsTextCategory.text = $"<color=#{ColorUtility.ToHtmlStringRGB(categoryColor)}>{category}</color>";
        
        TimeSpan cooldown = ProductManager.GetProductCooldown(ingredientData.fingerprint);
        
        if (resultsTextScan != null)
        {
            resultsTextScan.text = cooldown.TotalSeconds > 0 ? $"Product: {productScanCount}/3 scans"
                                                            : $"Product Scanned: {productScanCount}/3 times";
        }

        if (warningText != null)
        {
            if (ingredientData.totalDetected > 1)
            {
                warningText.text = $"{ingredientData.totalDetected} ingredients detected!\n" +
                                $"Scans remaining: {3 - productScanCount}";
                warningText.color = Color.yellow;
            }
            else
            {
                warningText.text = $"Single ingredient detected\n" +
                                $"Scans remaining: {3 - productScanCount}";
                warningText.color = Color.green;
            }
        }

        if (ingredientDatabase != null && modelManager != null)
        {
            var info = ingredientDatabase.GetIngredientInfo(ingredientData.ingredient);
            if (info != null && info.modelPrefab != null) 
                modelManager.DisplayModel(info.modelPrefab);
            else if (modelManager != null) 
                modelManager.DisplayModel(null);
        }
        else
        {
            Debug.LogWarning("IngredientDatabase or ModelManager is not assigned in the inspector!");
        }

        UpdateButtonStates();
    }

    // ==================== UI EFFECTS & CLEANUP ====================
    public void CloseAndGoToScene()
    {
        try
        {
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.nutriventure.mlkit.MLKitOcr"))
                pluginClass.CallStatic("cleanup");
        }
        catch (Exception e) 
        { 
            Debug.LogWarning("Cleanup error: " + e.Message); 
        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        if (liveCameraTexture != null && liveCameraTexture.isPlaying)
            liveCameraTexture.Stop();
        #endif

        SceneManager.LoadScene(sceneName);
    }

    public void ClosePanel()
    {
        if (resultsPanel != null)
        {
            // Make sure panel is interactable before closing
            CanvasGroup panelGroup = resultsPanel.GetComponent<CanvasGroup>();
            if (panelGroup != null)
            {
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
            }
            resultsPanel.SetActive(false);
        }
        
        if (galleryButton != null) 
            galleryButton.gameObject.SetActive(true);
        
        if (captureButton != null) 
            captureButton.gameObject.SetActive(true);
        
        if (instructionsButton != null) 
            instructionsButton.gameObject.SetActive(true);

        if (battleButton != null) 
            battleButton.gameObject.SetActive(false);

        // Clear any error UI
        ClearErrorUI();
        
        UpdateButtonStates();

        if (currentIngredientData != null && currentIngredientData.totalDetected > 1)
            UpdateStatus($"Remember: {currentIngredientData.totalDetected - 1} other ingredients can be found in this product! Try scanning again.");
        else
            UpdateStatus("Ready - Take photo or select from gallery");

        #if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RestartCameraPreviewAfterClose());
        #else
        if (imagePreview != null)
        {
            imagePreview.texture = null;
            imagePreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
        #endif
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator RestartCameraPreviewAfterClose()
    {
        yield return null;
        
        if (imagePreview != null)
        {
            imagePreview.texture = null;
            imagePreview.color = new Color(0.2f, 0.2f, 0.2f);
        }
        
        if (liveCameraTexture == null || !liveCameraTexture.isPlaying)
        {
            yield return StartCoroutine(RestartCameraPreview());
        }
        else
        {
            if (imagePreview != null)
            {
                imagePreview.texture = liveCameraTexture;
                imagePreview.color = Color.white;
            }
            UpdateStatus("Camera preview active - Tap Capture to scan");
        }
    }
    #endif

    IEnumerator FadeOut()
    {
        if (fadePanel == null) 
            yield break;
        
        fadePanel.SetActive(true);
        Image panelImage = fadePanel.GetComponent<Image>();
        if (panelImage == null) 
            yield break;

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

    IEnumerator FadeIn()
    {
        if (fadePanel == null) 
            yield break;
        
        Image panelImage = fadePanel.GetComponent<Image>();
        if (panelImage != null)
        {
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
    }

    public void ingredientSoundEffects()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (SystemInfo.supportsVibration) 
            Handheld.Vibrate();
        #endif
        
        if (audioSource != null && scanSound != null) 
            audioSource.PlayOneShot(scanSound);
    }

    // ==================== EDITOR MOCKS ====================
    void MockImageCapture()
    {
        UpdateStatus("Mock: Taking photo...");
        currentImage = new Texture2D(512, 512);
        if (imagePreview != null)
        {
            imagePreview.texture = currentImage;
            imagePreview.color = Color.white;
        }
        StartCoroutine(MockProcessImage());
    }

    void MockGalleryPick()
    {
        isProcessing = true;
        UpdateButtonStates();
        UpdateStatus("Mock: Selecting from gallery...");
        currentImage = new Texture2D(512, 512);
        if (imagePreview != null)
        {
            imagePreview.texture = currentImage;
            imagePreview.color = Color.white;
        }
        StartCoroutine(MockProcessImage());
    }

    IEnumerator MockProcessImage()
    {
        yield return new WaitForSeconds(1.5f);

        // Get random ingredient from database
        if (ingredientDatabase != null && ingredientDatabase.ingredients.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, ingredientDatabase.ingredients.Count);
            string randomIngredient = ingredientDatabase.ingredients[randomIndex].ingredientName;

            string mockJson = $"{{\"ingredient\":\"{randomIngredient}\",\"status\":\"success\"," +
                             "\"mode\":\"manual\",\"fingerprint\":\"mock123\",\"total_detected\":3}";

            Debug.Log($"Mock test with ingredient: {randomIngredient}");

            // Save to PersistentDataManager
            SaveScannedIngredient(randomIngredient);

            OnOCRResult(mockJson);
        }
        else
        {
            Debug.LogError("Ingredient database not found or empty!");
        }
    }

    // Add this method to OCRManager.cs:
    private void SaveScannedIngredient(string ingredientName)
    {
        // Make sure PersistentDataManager exists
        if (PersistentDataManager.Instance == null)
        {
            Debug.LogWarning("PersistentDataManager not found, creating one...");
            GameObject persistentDataObj = new GameObject("PersistentDataManager");
            persistentDataObj.AddComponent<PersistentDataManager>();
            DontDestroyOnLoad(persistentDataObj);
        }

        if (PersistentDataManager.Instance != null)
        {
            // Save as selected enerling (this will be the AI opponent)
            PersistentDataManager.Instance.SaveSelectedEnerling(ingredientName);
            Debug.Log($"Saved scanned enerling as AI opponent: {ingredientName}");
        }
    }

    void OnDestroy()
    {
        if (cooldownUpdateCoroutine != null) 
            StopCoroutine(cooldownUpdateCoroutine);
        
        if (captureCooldownCoroutine != null) 
            StopCoroutine(captureCooldownCoroutine);
        
        if (currentImage != null) 
            Destroy(currentImage);
       
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (liveCameraTexture != null && liveCameraTexture.isPlaying) 
            liveCameraTexture.Stop();
        #endif

        try
        {
            using (AndroidJavaClass pluginClass = new AndroidJavaClass("com.nutriventure.mlkit.MLKitOcr"))
                pluginClass.CallStatic("cleanup");
        }
        catch (Exception e) 
        { 
            Debug.LogWarning("Cleanup error: " + e.Message); 
        }
    }
}