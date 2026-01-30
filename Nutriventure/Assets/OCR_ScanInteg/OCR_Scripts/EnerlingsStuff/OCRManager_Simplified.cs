using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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

    [Header("Database Reference")]
    public IngredientDatabase ingredientDatabase;

    [Header("Scene Transition")]
    public string nextSceneName = "BattlePlay";
    public float sceneTransitionDelay = 1f;
    public float fadeDuration = 0.5f;

    [Header("Mock Settings")]
    public bool useMockScan = true; // Set to false when building for Android

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scanSound;

    // State
    private bool isProcessing = false;
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

        UpdateStatus("Ready to scan ingredient");
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

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator StartCameraPreview()
    {
        UpdateStatus("Initializing camera...");
        
        // Request camera permission
        yield return StartCoroutine(RequestCameraPermission());
        
        // Get available cameras
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            UpdateStatus("No camera found");
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
            UpdateStatus("Camera ready - Tap Capture to scan");
        }
        catch (System.Exception e)
        {
            UpdateStatus("Camera error: " + e.Message);
            useMockScan = true; // Fallback to mock
        }
    }
    
    IEnumerator RequestCameraPermission()
    {
        // Android permission request logic
        // (Keep your existing permission code here)
        yield return null;
    }
#endif

    // ==================== BUTTON HANDLERS ====================

    public void OnCaptureButtonClicked()
    {
        if (isProcessing) return;

        isProcessing = true;
        UpdateStatus("Scanning...");

        // Play scan sound
        if (audioSource != null && scanSound != null)
            audioSource.PlayOneShot(scanSound);

        if (useMockScan || Application.isEditor)
        {
            // Mock scan - randomly select enerling
            StartCoroutine(MockScanCoroutine());
        }
        else
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(TakePhotoCoroutine());
#endif
        }
    }

    public void OnGalleryButtonClicked()
    {
        if (isProcessing) return;

        isProcessing = true;
        UpdateStatus("Selecting from gallery...");

        // For now, use mock for gallery too
        StartCoroutine(MockScanCoroutine());
    }

    public void OnRetryButtonClicked()
    {
        ResetScanState();
        UpdateStatus("Ready to scan again");
    }

    public void OnInstructionsButtonClicked()
    {
        // Show instructions
        UpdateStatus("Point camera at ingredient label and tap Capture");
    }

    // ==================== SCANNING LOGIC ====================

    IEnumerator MockScanCoroutine()
    {
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
            isProcessing = false;
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

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator TakePhotoCoroutine()
    {
        // Your existing camera capture code here
        // This should eventually call ProcessRealOCR()
        yield return null;
    }
    
    void ProcessRealOCR(Texture2D photo)
    {
        // This is where real OCR would happen
        // For now, we'll use mock
        StartCoroutine(MockScanCoroutine());
    }
#endif

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

    void ResetScanState()
    {
        isProcessing = false;
        selectedEnerlingName = "";
        ClearError();

#if UNITY_ANDROID && !UNITY_EDITOR
        // Restart camera if using real camera
        if (!useMockScan && liveCameraTexture != null)
        {
            if (!liveCameraTexture.isPlaying)
                liveCameraTexture.Play();
        }
#endif
    }

    // ==================== UI UTILITIES ====================

    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

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
        if (liveCameraTexture != null && liveCameraTexture.isPlaying)
            liveCameraTexture.Stop();
#endif

        if (currentImage != null)
            Destroy(currentImage);
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