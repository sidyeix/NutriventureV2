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
            InitializeNewSession();
        }
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
        
        // Save any data if needed
        // Reset everything
        InitializeNewSession();
    }
    
    public void RestartGame()
    {
        Debug.Log("=== RESTARTING GAME ===");
        EndCurrentSession();
        // Optionally reload scene
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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