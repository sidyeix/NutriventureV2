using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GlobalMapManager : MonoBehaviour
{
    [System.Serializable]
    public class KingdomButton
    {
        public string kingdomName; // "sugaria", "preservia", "nutri", "allerthia", "ocr"
        public Button button;
        public Image buttonImage;
        public Color lockedColor = Color.black;
        public Color unlockedColor = Color.white;
        public CanvasGroup loadingIndicator; // For loading animation
        public AudioClip clickSound; // Optional per-button click sound
        
        [Header("Scene Configuration")]
        public string sceneToLoad; // Scene name for this kingdom
    }

    [Header("Kingdom Buttons")]
    [SerializeField] private List<KingdomButton> kingdomButtons = new List<KingdomButton>();

    [Header("OCR Scanner Special Settings")]
    [SerializeField] private GameObject ocrScannerFeature;

    [Header("Global Loading State")]
    [SerializeField] private GameObject globalLoadingIndicator;
    [SerializeField] private float maxWaitTime = 5f;

    [Header("Global Audio")]
    [SerializeField] private AudioSource globalAudioSource;
    [SerializeField] private AudioClip defaultClickSound;

    private GameDataManager gameDataManager;
    private Dictionary<string, KingdomButton> buttonDictionary = new Dictionary<string, KingdomButton>();
    private bool isInitialized = false;

    private void Awake()
    {
        // Build dictionary and initialize buttons
        foreach (var kingdom in kingdomButtons)
        {
            if (!buttonDictionary.ContainsKey(kingdom.kingdomName.ToLower()))
            {
                buttonDictionary.Add(kingdom.kingdomName.ToLower(), kingdom);
            }

            // Initialize button click listener
            if (kingdom.button != null)
            {
                kingdom.button.onClick.RemoveAllListeners();
                kingdom.button.onClick.AddListener(() => OnKingdomButtonClick(kingdom));
            }

            // Initialize loading indicator
            if (kingdom.loadingIndicator != null)
            {
                kingdom.loadingIndicator.alpha = 0;
                kingdom.loadingIndicator.blocksRaycasts = false;
                kingdom.loadingIndicator.interactable = false;
            }
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeWithGameDataManager());
    }

    private void OnEnable()
    {
        if (isInitialized && gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            UpdateAllKingdomButtons();
        }
        else if (!isInitialized)
        {
            StartCoroutine(InitializeWithGameDataManager());
        }
    }

    private IEnumerator InitializeWithGameDataManager()
    {
        if (globalLoadingIndicator != null)
            globalLoadingIndicator.SetActive(true);

        float elapsedTime = 0f;
        
        while (elapsedTime < maxWaitTime)
        {
            if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
            {
                gameDataManager = GameDataManager.Instance;
                isInitialized = true;
                UpdateAllKingdomButtons();
                
                if (globalLoadingIndicator != null)
                    globalLoadingIndicator.SetActive(false);
                
                yield break;
            }
            
            yield return new WaitForSeconds(0.2f);
            elapsedTime += 0.2f;
        }

        Debug.LogWarning("GlobalMapManager: Could not find GameDataManager. Using default state.");
        SetDefaultKingdomState();
        
        if (globalLoadingIndicator != null)
            globalLoadingIndicator.SetActive(false);
    }

    private void SetDefaultKingdomState()
    {
        // Nutri Kingdom unlocked by default, others locked
        UpdateKingdomButtonState("nutri", true);
        UpdateKingdomButtonState("sugaria", false);
        UpdateKingdomButtonState("preservia", false);
        UpdateKingdomButtonState("allerthia", false);
        UpdateKingdomButtonState("ocr", false);
        
        if (ocrScannerFeature != null)
            ocrScannerFeature.SetActive(false);
    }

    public void UpdateAllKingdomButtons()
    {
        if (gameDataManager == null || gameDataManager.CurrentGameData == null)
        {
            SetDefaultKingdomState();
            return;
        }

        GameData data = gameDataManager.CurrentGameData;

        UpdateKingdomButtonState("sugaria", data.HasSugariaKey());
        UpdateKingdomButtonState("preservia", data.HasPreserviaKey());
        UpdateKingdomButtonState("nutri", data.HasNutriKingdomKey());
        UpdateKingdomButtonState("allerthia", data.HasAllerthiaKey());
        UpdateKingdomButtonState("ocr", data.HasOCRScannerKey());

        if (ocrScannerFeature != null)
            ocrScannerFeature.SetActive(data.HasOCRScannerKey());
    }

    private void UpdateKingdomButtonState(string kingdomName, bool isUnlocked)
    {
        if (!buttonDictionary.TryGetValue(kingdomName.ToLower(), out KingdomButton kingdom))
            return;

        // Set button interactability
        if (kingdom.button != null)
        {
            kingdom.button.interactable = isUnlocked;
        }

        // Set button image color
        if (kingdom.buttonImage != null)
        {
            kingdom.buttonImage.color = isUnlocked ? kingdom.unlockedColor : kingdom.lockedColor;
        }
    }

    // ==================== BUTTON CLICK HANDLING ====================

    private void OnKingdomButtonClick(KingdomButton kingdom)
    {
        // Check if kingdom is unlocked
        if (kingdom.button != null && !kingdom.button.interactable)
        {
            Debug.Log($"Kingdom {kingdom.kingdomName} is locked!");
            return;
        }

        // Check if current scene is the same as the target scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == kingdom.sceneToLoad)
        {
            Debug.Log($"Already in {kingdom.kingdomName} kingdom!");
            return;
        }

        // Play click sound
        PlayClickSound(kingdom);

        // Show loading indicator
        ShowLoading(kingdom);

        // Disable button to prevent double clicks
        if (kingdom.button != null)
            kingdom.button.interactable = false;

        // Start scene loading
        StartCoroutine(LoadKingdomSceneAsync(kingdom));
    }

    private void PlayClickSound(KingdomButton kingdom)
    {
        AudioClip soundToPlay = kingdom.clickSound != null ? kingdom.clickSound : defaultClickSound;
        
        if (soundToPlay != null)
        {
            if (globalAudioSource != null)
            {
                globalAudioSource.PlayOneShot(soundToPlay);
            }
            else
            {
                // Create temporary audio source if none exists
                AudioSource tempSource = gameObject.AddComponent<AudioSource>();
                tempSource.PlayOneShot(soundToPlay);
                Destroy(tempSource, soundToPlay.length);
            }
        }
    }

    private void ShowLoading(KingdomButton kingdom)
    {
        if (kingdom.loadingIndicator == null) return;

        kingdom.loadingIndicator.alpha = 1;
        kingdom.loadingIndicator.blocksRaycasts = true;
        kingdom.loadingIndicator.interactable = true;
    }

    private void HideLoading(KingdomButton kingdom)
    {
        if (kingdom.loadingIndicator == null) return;

        kingdom.loadingIndicator.alpha = 0;
        kingdom.loadingIndicator.blocksRaycasts = false;
        kingdom.loadingIndicator.interactable = false;
    }

    private IEnumerator LoadKingdomSceneAsync(KingdomButton kingdom)
    {
        if (string.IsNullOrEmpty(kingdom.sceneToLoad))
        {
            Debug.LogError($"Scene name not set for {kingdom.kingdomName} kingdom!");
            HideLoading(kingdom);
            
            // Re-enable button
            if (kingdom.button != null)
                kingdom.button.interactable = true;
            
            yield break;
        }

        AsyncOperation loadOp = SceneManager.LoadSceneAsync(kingdom.sceneToLoad);
        loadOp.allowSceneActivation = false;

        // Small delay so loading animation is visible
        yield return new WaitForSeconds(0.2f);

        while (loadOp.progress < 0.9f)
        {
            yield return null;
        }

        loadOp.allowSceneActivation = true;
    }

    // ==================== PUBLIC METHODS ====================

    public void RefreshMap()
    {
        if (isInitialized)
        {
            UpdateAllKingdomButtons();
        }
        else
        {
            StartCoroutine(InitializeWithGameDataManager());
        }
    }

    // ==================== DEBUG METHODS ====================

    [ContextMenu("Debug/Unlock Sugaria Key")]
    private void DebugUnlockSugariaKey()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.CollectSugariaKey();
            gameDataManager.SaveGameData();
            UpdateAllKingdomButtons();
        }
    }

    [ContextMenu("Debug/Unlock Preservia Key")]
    private void DebugUnlockPreserviaKey()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.CollectPreserviaKey();
            gameDataManager.SaveGameData();
            UpdateAllKingdomButtons();
        }
    }

    [ContextMenu("Debug/Unlock Allerthia Key")]
    private void DebugUnlockAllerthiaKey()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.CollectAllerthiaKey();
            gameDataManager.SaveGameData();
            UpdateAllKingdomButtons();
        }
    }

    [ContextMenu("Debug/Unlock OCR Scanner Key")]
    private void DebugUnlockOCRScannerKey()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            gameDataManager.CurrentGameData.CollectOCRScannerKey();
            gameDataManager.SaveGameData();
            UpdateAllKingdomButtons();
        }
    }

    [ContextMenu("Debug/Reset All Keys (except Nutri)")]
    private void DebugResetAllKeys()
    {
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            var data = gameDataManager.CurrentGameData;
            data.ResetSugariaKey();
            data.ResetPreserviaKey();
            data.ResetAllerthiaKey();
            data.ResetOCRScannerKey();
            gameDataManager.SaveGameData();
            UpdateAllKingdomButtons();
        }
    }

    [ContextMenu("Debug/Print Current State")]
    private void DebugPrintCurrentState()
    {
        Debug.Log("=== GLOBAL MAP STATE ===");
        
        if (gameDataManager != null && gameDataManager.CurrentGameData != null)
        {
            var data = gameDataManager.CurrentGameData;
            Debug.Log($"Sugaria: {(data.HasSugariaKey() ? "UNLOCKED" : "LOCKED")}");
            Debug.Log($"Preservia: {(data.HasPreserviaKey() ? "UNLOCKED" : "LOCKED")}");
            Debug.Log($"Nutri: {(data.HasNutriKingdomKey() ? "UNLOCKED" : "LOCKED")}");
            Debug.Log($"Allerthia: {(data.HasAllerthiaKey() ? "UNLOCKED" : "LOCKED")}");
            Debug.Log($"OCR: {(data.HasOCRScannerKey() ? "UNLOCKED" : "LOCKED")}");
        }
        else
        {
            Debug.Log("Using default state: Nutri UNLOCKED, others LOCKED");
        }

        Debug.Log("\nButton States:");
        foreach (var kingdom in kingdomButtons)
        {
            string sceneCheck = SceneManager.GetActiveScene().name == kingdom.sceneToLoad ? " (CURRENT SCENE)" : "";
            Debug.Log($"{kingdom.kingdomName}: Interactable={kingdom.button?.interactable}, Scene={kingdom.sceneToLoad}{sceneCheck}");
        }
    }
}