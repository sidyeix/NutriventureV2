using UnityEngine;
using UnityEngine.SceneManagement;

public class K2_GameSessionManager : MonoBehaviour
{
    public static K2_GameSessionManager Instance { get; private set; }

    [Header("References")]
    public ProductInformationManager productInfoManager;
    public ProductSpawner productSpawner;

    [Header("Session Settings")]
    public bool resetOnSceneLoad = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeNewSession();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (resetOnSceneLoad)
        {
            // Only refresh references after scene reload.
            // Do NOT call InitializeNewSession here because it can
            // destroy products that the fresh ProductSpawner just spawned
            // in its own Start() (sceneLoaded may fire after Start).
            RefreshReferences();
        }
    }

    /// <summary>
    /// Re-acquires references to scene objects after a scene reload.
    /// The new scene's own Start() methods handle their own initialization.
    /// </summary>
    private void RefreshReferences()
    {
        productInfoManager = FindObjectOfType<ProductInformationManager>();
        productSpawner = FindObjectOfType<ProductSpawner>();
        Debug.Log("K2_GameSessionManager: References refreshed after scene load");
    }

    public void InitializeNewSession()
    {
        Debug.Log("=== INITIALIZING NEW GAME SESSION ===");

        // Reset product collection
        if (productInfoManager != null)
        {
            productInfoManager.ResetForNewSession();
        }
        else
        {
            productInfoManager = FindObjectOfType<ProductInformationManager>();
            if (productInfoManager != null)
                productInfoManager.ResetForNewSession();
        }

        // Reset product spawner
        if (productSpawner != null)
        {
            productSpawner.ResetGame();
        }
        else
        {
            productSpawner = FindObjectOfType<ProductSpawner>();
            if (productSpawner != null)
                productSpawner.ResetGame();
        }

        // Reset player health if exists
        SugariaPlayerStat playerHealth = FindObjectOfType<SugariaPlayerStat>();
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        Debug.Log("New game session initialized successfully!");
    }

    public void EndCurrentSession()
    {
        Debug.Log("=== ENDING CURRENT GAME SESSION ===");
    }

    /// <summary>
    /// Comprehensive game restart: resets all systems and respawns products.
    /// Called by restart buttons in both the summary panel and in-game settings.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("=== RESTARTING GAME ===");

        // Reset product collection tracking
        if (productInfoManager == null)
            productInfoManager = FindObjectOfType<ProductInformationManager>();
        if (productInfoManager != null)
            productInfoManager.ResetForNewSession();

        // Reset and respawn products
        if (productSpawner == null)
            productSpawner = FindObjectOfType<ProductSpawner>();
        if (productSpawner != null)
            productSpawner.ResetAndRespawn();

        // Reset player health
        SugariaPlayerStat playerHealth = FindObjectOfType<SugariaPlayerStat>();
        if (playerHealth != null)
            playerHealth.ResetHealth();

        // Reset QA1 assessment
        K2_QA1system qa1 = FindObjectOfType<K2_QA1system>();
        if (qa1 != null)
            qa1.ResetForNewGame();

        // Reset QA2 assessment
        K2_QA2system qa2 = FindObjectOfType<K2_QA2system>();
        if (qa2 != null)
            qa2.ClearScannedProducts();

        // Reset scoring
        SugariaScoringSystem scoring = FindObjectOfType<SugariaScoringSystem>();
        if (scoring != null)
            scoring.ResetSessionStats();

        Debug.Log("Game restarted - all systems reset and products respawned!");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [ContextMenu("Force New Session")]
    public void ForceNewSession()
    {
        InitializeNewSession();
    }
}