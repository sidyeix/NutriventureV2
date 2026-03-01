using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GlobalMapManager : MonoBehaviour
{
    public static GlobalMapManager Instance;

    [Header("🎯 MAP OBJECT REFERENCES (Auto-Found by Name)")]
    [SerializeField] private GameObject worldMapObject; // "WorldMap" - parent container
    [SerializeField] private GameObject nutriMapObject; // "NutriMap" - the actual map UI
    [SerializeField] private GameObject canvasObject; // "Canvas" - main canvas
    
    [Header("🗺️ KINGDOM AREAS (Auto-Found by Name)")]
    [SerializeField] private Button nutriAreaButton; // "Nutri_Area" - Kingdom 1
    [SerializeField] private Button sugariaAreaButton; // "Sugaria_Area" - Kingdom 2
    [SerializeField] private Button preserviaAreaButton; // "Preservia_Area" - Kingdom 3
    [SerializeField] private Button allerthriaAreaButton; // "Allerthria_Area" - Kingdom 4
    
    [Header("🔘 UI CONTROLS (Auto-Found by Name)")]
    [SerializeField] private Button closeButton; // "Btn_Close"
    [SerializeField] private GameObject loadingIndicator; // "LoadingIndicator"
    [SerializeField] private CanvasGroup loadingCanvasGroup; // "CanvasGroup" on LoadingIndicator
    [SerializeField] private GameObject loadingPanel; // "LoadingPanel"

    [Header("Scene Names")]
    [SerializeField] private string kingdom1Scene = "3_kingdom1";
    [SerializeField] private string kingdom2Scene = "4_kingdom 2";
    [SerializeField] private string kingdom3Scene = "5_kingdom3";
    [SerializeField] private string kingdom4Scene = "6_kingdom4";

    [Header("Key Status Text")]
    [SerializeField] private TextMeshProUGUI kingdom2KeyStatusText;
    [SerializeField] private TextMeshProUGUI kingdom3KeyStatusText;
    [SerializeField] private TextMeshProUGUI kingdom4KeyStatusText;
    [SerializeField] private TextMeshProUGUI ocrKeyStatusText;

    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadTime = 1.0f;
    [SerializeField] private float spinnerRotationSpeed = 300f;
    [SerializeField] private bool useFadeAnimation = true;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private string[] loadingTips;
    [SerializeField] private float tipChangeInterval = 2f;

    [Header("Map Access")]
    [SerializeField] private string mapOpenButtonName = "Btn_OpenMap";

    // Private variables
    private bool isLoading = false;
    private Coroutine tipCoroutine;
    private AsyncOperation currentLoadOperation;
    private bool isMapVisible = false;
    private Image loadingSpinner;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GlobalMapManager initialized as DontDestroyOnLoad");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindAllMapObjects();
        InitializeMap();
        SetupButtonListeners();
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        KeyCollectionEvents.OnKeyCollected += OnKeyCollected;
    }

    void Update()
    {
        if (isLoading && loadingSpinner != null)
        {
            loadingSpinner.transform.Rotate(0, 0, -spinnerRotationSpeed * Time.unscaledDeltaTime);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        KeyCollectionEvents.OnKeyCollected -= OnKeyCollected;
    }

    // ========== OBJECT FINDING METHODS ==========

    private void FindAllMapObjects()
    {
        Debug.Log("🔍 Finding all map objects by name...");

        worldMapObject = GameObject.Find("WorldMap");
        nutriMapObject = GameObject.Find("NutriMap");
        canvasObject = GameObject.Find("Canvas");

        // Find Kingdom Areas
        GameObject area = GameObject.Find("Nutri_Area");
        if (area != null) nutriAreaButton = area.GetComponent<Button>();
        
        area = GameObject.Find("Sugaria_Area");
        if (area != null) sugariaAreaButton = area.GetComponent<Button>();
        
        area = GameObject.Find("Preservia_Area");
        if (area != null) preserviaAreaButton = area.GetComponent<Button>();
        
        area = GameObject.Find("Allerthria_Area");
        if (area != null) allerthriaAreaButton = area.GetComponent<Button>();

        // Find UI Controls
        GameObject btn = GameObject.Find("Btn_Close");
        if (btn != null) closeButton = btn.GetComponent<Button>();

        loadingIndicator = GameObject.Find("LoadingIndicator");
        loadingPanel = GameObject.Find("LoadingPanel");

        if (loadingIndicator != null)
        {
            loadingCanvasGroup = loadingIndicator.GetComponent<CanvasGroup>();
            loadingSpinner = loadingIndicator.GetComponent<Image>();
        }

        Debug.Log($"Found: WorldMap={worldMapObject != null}, NutriMap={nutriMapObject != null}");
    }

    private void InitializeMap()
    {
        if (nutriMapObject != null)
        {
            nutriMapObject.SetActive(false);
            isMapVisible = false;
        }

        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);

        UpdateKingdomButtons();
    }

    private void SetupButtonListeners()
    {
        if (nutriAreaButton != null)
        {
            nutriAreaButton.onClick.RemoveAllListeners();
            nutriAreaButton.onClick.AddListener(LoadKingdom1);
        }

        if (sugariaAreaButton != null)
        {
            sugariaAreaButton.onClick.RemoveAllListeners();
            sugariaAreaButton.onClick.AddListener(LoadKingdom2);
        }

        if (preserviaAreaButton != null)
        {
            preserviaAreaButton.onClick.RemoveAllListeners();
            preserviaAreaButton.onClick.AddListener(LoadKingdom3);
        }

        if (allerthriaAreaButton != null)
        {
            allerthriaAreaButton.onClick.RemoveAllListeners();
            allerthriaAreaButton.onClick.AddListener(LoadKingdom4);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideMap);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        isLoading = false;
        
        if (tipCoroutine != null)
        {
            StopCoroutine(tipCoroutine);
            tipCoroutine = null;
        }

        FindMapOpenButtonInScene();
        HideMap();
        UpdateKingdomButtons();
    }

    private void FindMapOpenButtonInScene()
    {
        if (string.IsNullOrEmpty(mapOpenButtonName)) return;
        
        GameObject buttonObj = GameObject.Find(mapOpenButtonName);
        if (buttonObj != null)
        {
            Button openButton = buttonObj.GetComponent<Button>();
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(ShowMap);
                openButton.onClick.AddListener(ShowMap);
                Debug.Log($"Connected map open button: {mapOpenButtonName}");
            }
        }
    }

    // ========== KEY COLLECTION EVENT HANDLER ==========

    private void OnKeyCollected(string keyName)
    {
        Debug.Log($"🎯 Key collection event: {keyName}");
        UpdateKingdomButtons();
    }

    // ========== MAP VISIBILITY CONTROL ==========

    public void ShowMap()
    {
        if (nutriMapObject != null)
        {
            nutriMapObject.SetActive(true);
            isMapVisible = true;
            UpdateKingdomButtons();
            Debug.Log("Map shown");
        }
        else
        {
            Debug.LogWarning("NutriMap not found! Re-finding...");
            FindAllMapObjects();
            if (nutriMapObject != null)
            {
                nutriMapObject.SetActive(true);
                isMapVisible = true;
                UpdateKingdomButtons();
            }
        }
    }

    public void HideMap()
    {
        if (nutriMapObject != null)
        {
            nutriMapObject.SetActive(false);
            isMapVisible = false;
            Debug.Log("Map hidden");
        }
    }

    public void ToggleMap()
    {
        if (isMapVisible) HideMap();
        else ShowMap();
    }

    public void EnsureMapAccessible()
    {
        FindAllMapObjects();
        SetupButtonListeners();
        Debug.Log("Map accessibility ensured");
    }

    // ========== KINGDOM BUTTON UPDATE METHODS ==========

    public void UpdateKingdomButtons()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
        {
            Debug.LogWarning("GameDataManager not initialized");
            return;
        }

        GameData gameData = GameDataManager.Instance.CurrentGameData;

        SetButtonInteractable(nutriAreaButton, true, "Nutri_Area");

        bool sugariaUnlocked = gameData.HasSugariaKey();
        SetButtonInteractable(sugariaAreaButton, sugariaUnlocked, "Sugaria_Area");
        UpdateKeyStatusText(kingdom2KeyStatusText, sugariaUnlocked);

        bool preserviaUnlocked = gameData.HasPreserviaKey();
        SetButtonInteractable(preserviaAreaButton, preserviaUnlocked, "Preservia_Area");
        UpdateKeyStatusText(kingdom3KeyStatusText, preserviaUnlocked);

        bool allerthiaUnlocked = gameData.HasAllerthiaKey();
        SetButtonInteractable(allerthriaAreaButton, allerthiaUnlocked, "Allerthria_Area");
        UpdateKeyStatusText(kingdom4KeyStatusText, allerthiaUnlocked);

        bool ocrKeyCollected = gameData.HasOCRScannerKey();
        UpdateKeyStatusText(ocrKeyStatusText, ocrKeyCollected);
    }

    private void SetButtonInteractable(Button button, bool isInteractable, string buttonName)
    {
        if (button == null) return;
        button.interactable = isInteractable;
    }

    private void UpdateKeyStatusText(TextMeshProUGUI statusText, bool isUnlocked)
    {
        if (statusText == null) return;
        statusText.text = isUnlocked ? "KEY: UNLOCKED" : "KEY: LOCKED";
        statusText.color = isUnlocked ? Color.green : Color.red;
    }

    public void RefreshMap()
    {
        UpdateKingdomButtons();
    }

    // ========== SCENE LOADING METHODS ==========

    public void LoadKingdom1()
    {
        if (CanLoadKingdom(0) && !isLoading)
            StartCoroutine(ShowLoadingIndicator(kingdom1Scene));
    }

    public void LoadKingdom2()
    {
        if (CanLoadKingdom(1) && !isLoading)
            StartCoroutine(ShowLoadingIndicator(kingdom2Scene));
    }

    public void LoadKingdom3()
    {
        if (CanLoadKingdom(2) && !isLoading)
            StartCoroutine(ShowLoadingIndicator(kingdom3Scene));
    }

    public void LoadKingdom4()
    {
        if (CanLoadKingdom(3) && !isLoading)
            StartCoroutine(ShowLoadingIndicator(kingdom4Scene));
    }

    private bool CanLoadKingdom(int kingdomIndex)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return false;

        GameData gameData = GameDataManager.Instance.CurrentGameData;

        switch (kingdomIndex)
        {
            case 0: return true; // Kingdom 1 always unlocked
            case 1: return gameData.HasSugariaKey();
            case 2: return gameData.HasPreserviaKey();
            case 3: return gameData.HasAllerthiaKey();
            default: return false;
        }
    }

    // ========== LOADING INDICATOR METHODS ==========

    private IEnumerator ShowLoadingIndicator(string sceneName)
    {
        if (isLoading) yield break;
        isLoading = true;

        // Show loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
            
            if (useFadeAnimation && loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    loadingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }
                loadingCanvasGroup.alpha = 1f;
            }
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);

        // Start rotating tips
        if (tipText != null && loadingTips != null && loadingTips.Length > 0)
        {
            tipCoroutine = StartCoroutine(RotateLoadingTips());
        }

        // Load scene
        currentLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        currentLoadOperation.allowSceneActivation = false;

        while (currentLoadOperation.progress < 0.9f)
            yield return null;

        float loadStartTime = Time.unscaledTime;
        while (Time.unscaledTime - loadStartTime < minimumLoadTime)
            yield return null;

        currentLoadOperation.allowSceneActivation = true;
    }

    private IEnumerator RotateLoadingTips()
    {
        int currentTipIndex = Random.Range(0, loadingTips.Length);
        
        while (isLoading)
        {
            tipText.text = loadingTips[currentTipIndex];
            currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
            yield return new WaitForSecondsRealtime(tipChangeInterval);
        }
    }

    public void ForceHideLoadingIndicator()
    {
        StopAllCoroutines();
        if (tipCoroutine != null) StopCoroutine(tipCoroutine);
        
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
        
        isLoading = false;
    }

    // ========== DEBUG METHODS ==========

    [ContextMenu("Manual Refresh Map")]
    public void ManualRefreshMap() => UpdateKingdomButtons();

    [ContextMenu("Force Find All Objects")]
    public void ForceFindAllObjects()
    {
        FindAllMapObjects();
        SetupButtonListeners();
    }

    [ContextMenu("Test Show Map")]
    public void TestShowMap() => ShowMap();

    [ContextMenu("Test Hide Map")]
    public void TestHideMap() => HideMap();

    [ContextMenu("Test Simulate OCR Key Collection")]
    public void TestSimulateOCRKeyCollection() => 
        KeyCollectionEvents.TriggerKeyCollected("OCR");

    [ContextMenu("Test Simulate Sugaria Key Collection")]
    public void TestSimulateSugariaKeyCollection() => 
        KeyCollectionEvents.TriggerKeyCollected("Sugaria");

    [ContextMenu("Test Simulate Preservia Key Collection")]
    public void TestSimulatePreserviaKeyCollection() => 
        KeyCollectionEvents.TriggerKeyCollected("Preservia");

    [ContextMenu("Test Simulate Allerthia Key Collection")]
    public void TestSimulateAllerthiaKeyCollection() => 
        KeyCollectionEvents.TriggerKeyCollected("Allerthia");

    [ContextMenu("Debug Key States")]
    public void DebugKeyStates()
    {
        if (GameDataManager.Instance?.CurrentGameData == null) return;

        GameData gameData = GameDataManager.Instance.CurrentGameData;
        Debug.Log("=== KEY STATES ===");
        Debug.Log($"Sugaria Key: {(gameData.HasSugariaKey() ? "✓" : "✗")}");
        Debug.Log($"Preservia Key: {(gameData.HasPreserviaKey() ? "✓" : "✗")}");
        Debug.Log($"Allerthia Key: {(gameData.HasAllerthiaKey() ? "✓" : "✗")}");
        Debug.Log($"OCR Key: {(gameData.HasOCRScannerKey() ? "✓" : "✗")}");
    }
}