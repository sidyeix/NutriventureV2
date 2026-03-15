using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GlobalMapManager : MonoBehaviour
{
    [Header("Loading Panel")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Buttons")]
    [SerializeField] private Button kingdom1Button;
    [SerializeField] private Button kingdom2Button;
    [SerializeField] private Button kingdom3Button;
    [SerializeField] private Button kingdom4Button;

    [Header("Scene Names")]
    [SerializeField] private string kingdom1Scene = "3_kingdom1";
    [SerializeField] private string kingdom2Scene = "4_kingdom2";
    [SerializeField] private string kingdom3Scene = "5_kingdom3";
    [SerializeField] private string kingdom4Scene = "6_kingdom4";

    [Header("OCR Scanner Object")]
    [SerializeField] private GameObject ocrScannerObject;

    [Header("Book of Enerling")]
    [SerializeField] private GameObject bookOfEnerlingObject;

    public void Start()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        SetupButtons();
        UpdateKingdomButtons();

        UpdateOCRScannerObjectVisibility();
        UpdateBookOfEnerlingVisibility();
    }

    public void UpdateOCRScannerObjectVisibility()
    {
        if (ocrScannerObject == null)
        {
            Debug.LogWarning("OCR Scanner Object not assigned!");
            return;
        }

        // Scanning is always available regardless of kingdom progress
        ocrScannerObject.SetActive(true);
    }

    public void UpdateBookOfEnerlingVisibility()
    {
        if (bookOfEnerlingObject == null)
            return;

        // Book of Enerling is always active during playtime
        bookOfEnerlingObject.SetActive(true);
    }

    public void OnEnable()
    {
        // Listen for ALL key collection events
        KeyCollectionEvents.OnKeyCollected += OnKeyCollected;
        UpdateAllUI();
    }

    public void OnDisable()
    {
        KeyCollectionEvents.OnKeyCollected -= OnKeyCollected;
    }

    public void OnKeyCollected(string keyName)
    {
        Debug.Log($"GlobalMapManager received key collection event: {keyName}");

        // Update ALL UI elements when ANY key is collected
        UpdateAllUI();
    }

    public void UpdateAllUI()
    {
        UpdateKingdomButtons();
        UpdateOCRScannerObjectVisibility();
        UpdateBookOfEnerlingVisibility();
    }

    void SetupButtons()
    {
        // Clear existing listeners to prevent duplicates
        kingdom1Button.onClick.RemoveAllListeners();
        kingdom2Button.onClick.RemoveAllListeners();
        kingdom3Button.onClick.RemoveAllListeners();
        kingdom4Button.onClick.RemoveAllListeners();

        kingdom1Button.onClick.AddListener(() => TryLoad(kingdom1Scene, true));
        kingdom2Button.onClick.AddListener(() => TryLoad(kingdom2Scene, IsKingdomUnlocked(2)));
        kingdom3Button.onClick.AddListener(() => TryLoad(kingdom3Scene, IsKingdomUnlocked(3)));
        kingdom4Button.onClick.AddListener(() => TryLoad(kingdom4Scene, IsKingdomUnlocked(4)));
    }

    public void UpdateKingdomButtons()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return;

        kingdom1Button.interactable = true;
        kingdom2Button.interactable = IsKingdomUnlocked(2);
        kingdom3Button.interactable = IsKingdomUnlocked(3);
        kingdom4Button.interactable = IsKingdomUnlocked(4);

        Debug.Log($"Kingdom Buttons Updated - K1: true, K2 (Sugaria): {kingdom2Button.interactable}, K3 (Preservia): {kingdom3Button.interactable}, K4 (Allerthia): {kingdom4Button.interactable}");
    }

    /// <summary>
    /// Checks if a kingdom is unlocked based on collected keys in GameData.
    /// Kingdom 1 = always unlocked, 2 = Sugaria key, 3 = Preservia key, 4 = Allerthia key.
    /// </summary>
    private bool IsKingdomUnlocked(int kingdomNumber)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentGameData == null)
            return false;

        var gd = GameDataManager.Instance.CurrentGameData;
        switch (kingdomNumber)
        {
            case 1: return true;
            case 2: return gd.HasSugariaKey();
            case 3: return gd.HasPreserviaKey();
            case 4: return gd.HasAllerthiaKey();
            default: return false;
        }
    }

    public void TryLoad(string sceneName, bool canLoad)
    {
        if (!canLoad)
        {
            Debug.Log("Kingdom Locked!");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        loadingPanel.SetActive(true);

        if (loadingText != null)
            loadingText.text = "Loading...";

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(sceneName);
    }
}