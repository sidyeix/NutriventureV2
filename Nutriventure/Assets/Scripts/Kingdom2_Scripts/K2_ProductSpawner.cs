using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ProductSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] spawnPoints; // Assign your 10 spawn point GameObjects here
    public GameObject[] productPrefabs; // Assign your 8 product prefabs here
    
    [Header("UI References")]
    public Button startButton; // Assign your Start Button here in Inspector
    
    [Header("Spawn Behavior")]
    public bool respawnOnDemand = true;
    public bool disableButtonAfterClick = true;
    
    private List<GameObject> spawnedProducts = new List<GameObject>();
    private bool isInitialized = false;
    
    void Start()
    {
        // Set up the button click listener
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log("Start button listener added");
        }
        else
        {
            Debug.LogError("Start Button not assigned in Inspector!");
        }
    }
    
    // This method is called when the start button is clicked
    private void OnStartButtonClicked()
    {
        if (!isInitialized)
        {
            InitializeGame();
        }
        else
        {
            Debug.LogWarning("Game already initialized!");
        }
    }
    
    public void InitializeGame()
    {
        SpawnProducts();
        isInitialized = true;
        
        // Handle button state after click
        if (disableButtonAfterClick && startButton != null)
        {
            startButton.interactable = false;
            // Optional: Hide the button completely
            // startButton.gameObject.SetActive(false);
        }
        
        Debug.Log("Game initialized with product spawning!");
    }
    
    [ContextMenu("Spawn Products")]
    public void SpawnProducts()
    {
        // Clear existing products if respawning
        ClearSpawnedProducts();
        
        // Validate setup
        if (!ValidateSpawnSetup())
            return;
        
        // Create a list of available spawn points
        List<GameObject> availableSpawnPoints = new List<GameObject>(spawnPoints);
        
        // Create a list of products to spawn
        List<GameObject> productsToSpawn = new List<GameObject>(productPrefabs);
        
        // Randomize the order of products
        ShuffleList(productsToSpawn);
        
        // Spawn products at random spawn points
        for (int i = 0; i < productsToSpawn.Count; i++)
        {
            if (availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning("Not enough spawn points for all products!");
                break;
            }
            
            // Pick a random spawn point
            int randomIndex = Random.Range(0, availableSpawnPoints.Count);
            GameObject spawnPoint = availableSpawnPoints[randomIndex];
            availableSpawnPoints.RemoveAt(randomIndex);
            
            // Spawn the product
            SpawnProduct(productsToSpawn[i], spawnPoint.transform.position, spawnPoint.transform.rotation);
        }
        
        Debug.Log($"Successfully spawned {spawnedProducts.Count} products at random locations. {availableSpawnPoints.Count} spawn points remain empty.");
    }
    
    [ContextMenu("Clear Products")]
    public void ClearSpawnedProducts()
    {
        foreach (GameObject product in spawnedProducts)
        {
            if (product != null)
            {
                DestroyImmediate(product);
            }
        }
        spawnedProducts.Clear();
        Debug.Log("Cleared all spawned products");
    }
    
    [ContextMenu("Respawn Products")]
    public void RespawnProducts()
    {
        if (respawnOnDemand)
        {
            SpawnProducts();
        }
        else
        {
            Debug.LogWarning("Respawn on demand is disabled. Enable it in the inspector or use SpawnProducts instead.");
        }
    }
    
    // Call this to reset the game completely and re-enable the start button
    public void ResetGame()
    {
        ClearSpawnedProducts();
        isInitialized = false;
        
        // Re-enable the start button if it was disabled
        if (startButton != null)
        {
            startButton.interactable = true;
            startButton.gameObject.SetActive(true);
        }
        
        Debug.Log("Game reset - start button re-enabled");
    }
    
    private void SpawnProduct(GameObject productPrefab, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedProduct = Instantiate(productPrefab, position, rotation);
        spawnedProducts.Add(spawnedProduct);
        
        // Optional: Name the spawned product for better organization in hierarchy
        spawnedProduct.name = $"{productPrefab.name}_Spawned";
        
        Debug.Log($"Spawned {productPrefab.name} at position {position}");
    }
    
    private bool ValidateSpawnSetup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned! Please assign spawn points in the inspector.");
            return false;
        }
        
        if (productPrefabs == null || productPrefabs.Length == 0)
        {
            Debug.LogError("No product prefabs assigned! Please assign product prefabs in the inspector.");
            return false;
        }
        
        if (spawnPoints.Length < productPrefabs.Length)
        {
            Debug.LogError($"Not enough spawn points! Have {spawnPoints.Length}, need at least {productPrefabs.Length}");
            return false;
        }
        
        // Check for null spawn points
        foreach (GameObject spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                Debug.LogError("One or more spawn points are null! Please check your spawn points assignment.");
                return false;
            }
        }
        
        // Check for null prefabs
        foreach (GameObject prefab in productPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogError("One or more product prefabs are null! Please check your prefabs assignment.");
                return false;
            }
        }
        
        return true;
    }
    
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    // Public methods for external control
    public int GetSpawnedProductCount()
    {
        return spawnedProducts.Count;
    }
    
    public int GetEmptySpawnPointCount()
    {
        return spawnPoints.Length - spawnedProducts.Count;
    }
    
    public bool AreAllProductsCollected()
    {
        return spawnedProducts.Count == 0;
    }
    
    public bool IsGameInitialized()
    {
        return isInitialized;
    }
    
    public void RemoveProduct(GameObject product)
    {
        if (spawnedProducts.Contains(product))
        {
            spawnedProducts.Remove(product);
            Destroy(product);
            Debug.Log($"Product {product.name} removed from spawn system");
        }
    }
    
    // Method to manually trigger spawning (for testing or other triggers)
    public void ForceSpawnProducts()
    {
        SpawnProducts();
    }
    
    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (GameObject spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.DrawWireSphere(spawnPoint.transform.position, 0.5f);
                    Gizmos.DrawIcon(spawnPoint.transform.position + Vector3.up, "d_Circle@2x");
                }
            }
        }
    }
    
    // Clean up
    private void OnDestroy()
    {
        // Remove the button listener to prevent memory leaks
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }
}