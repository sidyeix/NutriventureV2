using UnityEngine;
using System.Collections.Generic;

public class ProductSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] spawnPoints; // Assign your 10 spawn point GameObjects here
    public GameObject[] productPrefabs; // Assign your 8 product prefabs here

    [Header("Spawn Behavior")]
    public bool spawnOnStart = true; // Automatically spawn when the game starts
    public bool respawnOnDemand = true;
    public bool randomizeSpawnLocations = true;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    private List<GameObject> spawnedProducts = new List<GameObject>();
    private bool isInitialized = false;

    void Start()
    {
        if (spawnOnStart)
        {
            InitializeAndSpawn();
        }
        else
        {
            Debug.LogWarning("Auto-spawning disabled. Products will not spawn automatically.");
        }
    }

    // Automatically initialize and spawn products
    public void InitializeAndSpawn()
    {
        if (!isInitialized)
        {
            SpawnProducts();
            isInitialized = true;
            LogDebug("Game initialized with automatic product spawning!");
        }
        else
        {
            LogDebug("Products already spawned!");
        }
    }

    [ContextMenu("Spawn Products")]
    public void SpawnProducts()
    {
        // Clear existing products if respawning
        ClearSpawnedProducts();

        // Validate setup
        if (!ValidateSpawnSetup())
            return;

        // Create lists for spawn points and products
        List<GameObject> availableSpawnPoints = new List<GameObject>(spawnPoints);
        List<GameObject> productsToSpawn = new List<GameObject>(productPrefabs);

        // Randomize if enabled
        if (randomizeSpawnLocations)
        {
            ShuffleList(availableSpawnPoints);
            ShuffleList(productsToSpawn);
        }

        // Spawn products
        for (int i = 0; i < productsToSpawn.Count; i++)
        {
            if (availableSpawnPoints.Count == 0)
            {
                LogWarning("Not enough spawn points for all products!");
                break;
            }

            // Get spawn point (random or sequential based on setting)
            GameObject spawnPoint = randomizeSpawnLocations
                ? availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)]
                : availableSpawnPoints[i % availableSpawnPoints.Count];

            if (spawnPoint == null) continue;

            // Spawn the product
            SpawnProduct(productsToSpawn[i], spawnPoint.transform.position, spawnPoint.transform.rotation);

            // Remove used spawn point if randomizing
            if (randomizeSpawnLocations)
            {
                availableSpawnPoints.Remove(spawnPoint);
            }
        }

        LogDebug($"Successfully spawned {spawnedProducts.Count} products. Empty spawn points: {availableSpawnPoints.Count}");
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
        LogDebug("Cleared all spawned products");
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
            LogWarning("Respawn on demand is disabled. Enable it in the inspector or use SpawnProducts instead.");
        }
    }

    // Call this to reset the game completely
    public void ResetGame()
    {
        ClearSpawnedProducts();
        isInitialized = false;
        LogDebug("Game reset - ready for new spawning");
    }

    /// <summary>
    /// Resets all spawned products and immediately respawns them.
    /// Use this when restarting the game without reloading the scene.
    /// </summary>
    public void ResetAndRespawn()
    {
        ClearSpawnedProducts();
        isInitialized = false;
        SpawnProducts();
        isInitialized = true;
        LogDebug("Game reset and products respawned");
    }

    private void SpawnProduct(GameObject productPrefab, Vector3 position, Quaternion rotation)
    {
        if (productPrefab == null)
        {
            LogError($"Attempted to spawn null prefab at position {position}");
            return;
        }

        GameObject spawnedProduct = Instantiate(productPrefab, position, rotation);
        spawnedProducts.Add(spawnedProduct);

        // Optional: Name the spawned product for better organization in hierarchy
        spawnedProduct.name = $"{productPrefab.name}_Spawned";

        LogDebug($"Spawned {productPrefab.name} at position {position}");
    }

    private bool ValidateSpawnSetup()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            LogError("No spawn points assigned! Please assign spawn points in the inspector.");
            return false;
        }

        if (productPrefabs == null || productPrefabs.Length == 0)
        {
            LogError("No product prefabs assigned! Please assign product prefabs in the inspector.");
            return false;
        }

        // Check for null spawn points
        foreach (GameObject spawnPoint in spawnPoints)
        {
            if (spawnPoint == null)
            {
                LogError("One or more spawn points are null! Please check your spawn points assignment.");
                return false;
            }
        }

        // Check for null prefabs
        foreach (GameObject prefab in productPrefabs)
        {
            if (prefab == null)
            {
                LogError("One or more product prefabs are null! Please check your prefabs assignment.");
                return false;
            }
        }

        // Warn if not enough spawn points
        if (spawnPoints.Length < productPrefabs.Length)
        {
            LogWarning($"More products ({productPrefabs.Length}) than spawn points ({spawnPoints.Length}). Some products may not spawn.");
        }

        return true;
    }

    private void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1) return;

        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    #region Public Methods for External Control

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
            LogDebug($"Product {product.name} removed from spawn system");
        }
    }

    // Method to manually trigger spawning (for testing or other triggers)
    public void ForceSpawnProducts()
    {
        SpawnProducts();
    }

    #endregion

    #region Debug Logging Methods

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ProductSpawner] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[ProductSpawner] {message}", this);
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ProductSpawner] {message}", this);
    }

    #endregion

    #region Editor Visualization

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

    #endregion

    #region Context Menu Commands

    [ContextMenu("Check Setup")]
    public void CheckSetup()
    {
        ValidateSpawnSetup();
        LogDebug($"Spawn Points: {spawnPoints?.Length ?? 0}");
        LogDebug($"Product Prefabs: {productPrefabs?.Length ?? 0}");
        LogDebug($"Currently Spawned: {spawnedProducts.Count}");
        LogDebug($"Is Initialized: {isInitialized}");
    }

    [ContextMenu("Toggle Debug Logs")]
    public void ToggleDebugLogs()
    {
        enableDebugLogs = !enableDebugLogs;
        LogDebug($"Debug logs {(enableDebugLogs ? "ENABLED" : "DISABLED")}");
    }

    #endregion
}