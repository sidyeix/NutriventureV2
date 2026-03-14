using UnityEngine;
using System.Collections.Generic;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn Points - Separate for Each Type")]
    public Transform[] goFoodSpawnPoints;     // Only spawn Go foods
    public Transform[] growFoodSpawnPoints;   // Only spawn Grow foods
    public Transform[] glowFoodSpawnPoints;   // Only spawn Glow foods
    public Transform[] junkFoodSpawnPoints;   // Only spawn Junk foods

    [Header("Food Prefabs")]
    public GameObject[] goFoodPrefabs;       // Go food prefabs
    public GameObject[] growFoodPrefabs;     // Grow food prefabs
    public GameObject[] glowFoodPrefabs;     // Glow food prefabs
    public GameObject[] junkFoodPrefabs;     // Junk food prefabs

    [Header("Game Settings")]
    public bool spawnAtStart = false;        // We'll spawn manually
    public bool respawnWhenCollected = true;
    public float respawnDelay = 5f;          // Time before respawning food

    private List<GameObject> activeFood = new List<GameObject>();
    private Dictionary<Transform, bool> spawnPointOccupied = new Dictionary<Transform, bool>();
    private Dictionary<Transform, float> respawnTimers = new Dictionary<Transform, float>();
    private bool isSpawningEnabled = false;
    private List<Transform> allSpawnPoints = new List<Transform>();
    private List<Transform> spawnPointsToRespawn = new List<Transform>();
    private Dictionary<Transform, GameObject[]> spawnPointToPrefabs = new Dictionary<Transform, GameObject[]>();

    // Store original active state of food objects
    private Dictionary<GameObject, bool> originalFoodStates = new Dictionary<GameObject, bool>();

    private void Start()
    {
        InitializeSpawnSystem();
    }

    private void Update()
    {
        if (!isSpawningEnabled) return;

        if (respawnWhenCollected)
        {
            UpdateRespawnTimers();
        }
    }

    private void InitializeSpawnSystem()
    {
        allSpawnPoints.Clear();
        spawnPointToPrefabs.Clear();

        // Initialize all spawn points and cache their prefab mappings
        AddSpawnPointsToArray(goFoodSpawnPoints, goFoodPrefabs);
        AddSpawnPointsToArray(growFoodSpawnPoints, growFoodPrefabs);
        AddSpawnPointsToArray(glowFoodSpawnPoints, glowFoodPrefabs);
        AddSpawnPointsToArray(junkFoodSpawnPoints, junkFoodPrefabs);
    }

    private void AddSpawnPointsToArray(Transform[] spawnPoints, GameObject[] prefabs)
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint != null && !allSpawnPoints.Contains(spawnPoint))
            {
                allSpawnPoints.Add(spawnPoint);
                spawnPointOccupied[spawnPoint] = false;
                respawnTimers[spawnPoint] = 0f;
                spawnPointToPrefabs[spawnPoint] = prefabs;
            }
        }
    }

    public void StartSpawning()
    {
        isSpawningEnabled = true;
        SpawnInitialFood();
#if UNITY_EDITOR
        Debug.Log("Food Spawning Started!");
#endif
    }

    public void StopSpawning()
    {
        isSpawningEnabled = false;
        ClearAllFood();
#if UNITY_EDITOR
        Debug.Log("Food Spawning Stopped!");
#endif
    }

    public void SpawnInitialFood()
    {
        ClearAllFood();

        // Spawn Go foods
        SpawnFoodAtPoints(goFoodSpawnPoints, goFoodPrefabs);

        // Spawn Grow foods
        SpawnFoodAtPoints(growFoodSpawnPoints, growFoodPrefabs);

        // Spawn Glow foods
        SpawnFoodAtPoints(glowFoodSpawnPoints, glowFoodPrefabs);

        // Spawn Junk foods
        SpawnFoodAtPoints(junkFoodSpawnPoints, junkFoodPrefabs);
    }

    private void SpawnFoodAtPoints(Transform[] spawnPoints, GameObject[] prefabs)
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint != null && !spawnPointOccupied[spawnPoint])
            {
                SpawnFoodAtPoint(spawnPoint, prefabs);
            }
        }
    }

    private void SpawnFoodAtPoint(Transform spawnPoint, GameObject[] prefabs)
    {
        if (prefabs.Length == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"No food prefabs assigned for spawn point: {spawnPoint.name}!");
#endif
            return;
        }

        // Select random prefab from the array
        int randomIndex = Random.Range(0, prefabs.Length);
        GameObject foodPrefab = prefabs[randomIndex];

        if (foodPrefab != null)
        {
            // Use object pool instead of Instantiate
            GameObject food = SimpleObjectPool.Get(foodPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            activeFood.Add(food);

            // Store original active state
            originalFoodStates[food] = true;

            // Mark spawn point as occupied
            spawnPointOccupied[spawnPoint] = true;

            // Reuse or add FoodCollectionNotifier
            FoodCollectionNotifier notifier = food.GetComponent<FoodCollectionNotifier>();
            if (notifier == null)
                notifier = food.AddComponent<FoodCollectionNotifier>();
            notifier.Initialize(this, spawnPoint);

#if UNITY_EDITOR
            Debug.Log($"Spawned food at {spawnPoint.name}: {foodPrefab.name}");
#endif
        }
    }

    public void OnFoodCollected(Transform spawnPoint)
    {
        if (spawnPoint == null || !spawnPointOccupied.ContainsKey(spawnPoint))
            return;

        // Mark spawn point as available
        spawnPointOccupied[spawnPoint] = false;

        if (respawnWhenCollected && isSpawningEnabled)
        {
            // Start respawn timer
            respawnTimers[spawnPoint] = respawnDelay;
        }
    }

    private void UpdateRespawnTimers()
    {
        // Reuse pre-allocated list to avoid GC allocation every frame
        spawnPointsToRespawn.Clear();

        // First pass: update timers and collect points that need respawning
        for (int i = 0; i < allSpawnPoints.Count; i++)
        {
            Transform spawnPoint = allSpawnPoints[i];
            if (respawnTimers.ContainsKey(spawnPoint) && respawnTimers[spawnPoint] > 0f)
            {
                respawnTimers[spawnPoint] -= Time.deltaTime;

                if (respawnTimers[spawnPoint] <= 0f && !spawnPointOccupied[spawnPoint])
                {
                    spawnPointsToRespawn.Add(spawnPoint);
                }
            }
        }

        // Second pass: respawn collected points
        for (int i = 0; i < spawnPointsToRespawn.Count; i++)
        {
            Transform spawnPoint = spawnPointsToRespawn[i];
            // Use cached prefab mapping instead of searching arrays
            GameObject[] prefabs;
            if (spawnPointToPrefabs.TryGetValue(spawnPoint, out prefabs))
            {
                SpawnFoodAtPoint(spawnPoint, prefabs);
            }
        }
    }

    private GameObject[] GetPrefabsForSpawnPoint(Transform spawnPoint)
    {
        // Use cached mapping first
        GameObject[] prefabs;
        if (spawnPointToPrefabs.TryGetValue(spawnPoint, out prefabs))
            return prefabs;

#if UNITY_EDITOR
        Debug.LogWarning($"Spawn point {spawnPoint.name} not found in prefab cache!");
#endif
        return System.Array.Empty<GameObject>();
    }

    public void ClearAllFood()
    {
        // Clear original states dictionary
        originalFoodStates.Clear();

        // Return all active food to pool instead of destroying
        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] != null)
            {
                SimpleObjectPool.Return(activeFood[i]);
            }
        }
        activeFood.Clear();

        // Reset all spawn points using the list instead of dictionary keys
        foreach (Transform spawnPoint in allSpawnPoints)
        {
            if (spawnPointOccupied.ContainsKey(spawnPoint))
            {
                spawnPointOccupied[spawnPoint] = false;
            }
            if (respawnTimers.ContainsKey(spawnPoint))
            {
                respawnTimers[spawnPoint] = 0f;
            }
        }

#if UNITY_EDITOR
        Debug.Log("All food cleared!");
#endif
    }

    // NEW METHOD: Hide all spawned food without destroying them
    public void HideAllFood()
    {
        for (int i = 0; i < activeFood.Count; i++)
        {
            if (activeFood[i] != null)
            {
                // Store current active state before hiding
                if (!originalFoodStates.ContainsKey(activeFood[i]))
                {
                    originalFoodStates[activeFood[i]] = activeFood[i].activeSelf;
                }

                activeFood[i].SetActive(false);
            }
        }

        // Also pause respawn timers
        PauseAllRespawnTimers();

#if UNITY_EDITOR
        Debug.Log($"Hid {activeFood.Count} food objects");
#endif
    }

    // NEW METHOD: Show all previously hidden food
    public void ShowAllFood()
    {
        for (int i = 0; i < activeFood.Count; i++)
        {
            if (activeFood[i] != null)
            {
                // Restore to original state or default to active
                if (originalFoodStates.ContainsKey(activeFood[i]))
                {
                    activeFood[i].SetActive(originalFoodStates[activeFood[i]]);
                }
                else
                {
                    activeFood[i].SetActive(true);
                }
            }
        }

        // Resume respawn timers
        ResumeAllRespawnTimers();

#if UNITY_EDITOR
        Debug.Log($"Showed {activeFood.Count} food objects");
#endif
    }

    // NEW METHOD: Pause all respawn timers
    private void PauseAllRespawnTimers()
    {
        foreach (Transform spawnPoint in allSpawnPoints)
        {
            if (respawnTimers.ContainsKey(spawnPoint))
            {
                // Set timers to a negative value to pause them
                if (respawnTimers[spawnPoint] > 0f)
                {
                    respawnTimers[spawnPoint] = -respawnTimers[spawnPoint];
                }
            }
        }
    }

    // NEW METHOD: Resume all paused respawn timers
    private void ResumeAllRespawnTimers()
    {
        foreach (Transform spawnPoint in allSpawnPoints)
        {
            if (respawnTimers.ContainsKey(spawnPoint))
            {
                // If timer is negative (paused), convert back to positive
                if (respawnTimers[spawnPoint] < 0f)
                {
                    respawnTimers[spawnPoint] = -respawnTimers[spawnPoint];
                }
            }
        }
    }

    // NEW METHOD: Temporarily disable food spawning without clearing food
    public void PauseSpawning()
    {
        isSpawningEnabled = false;
        PauseAllRespawnTimers();
#if UNITY_EDITOR
        Debug.Log("Food spawning paused");
#endif
    }

    // NEW METHOD: Resume food spawning
    public void ResumeSpawning()
    {
        isSpawningEnabled = true;
        ResumeAllRespawnTimers();
#if UNITY_EDITOR
        Debug.Log("Food spawning resumed");
#endif
    }

    public void RespawnAllFood()
    {
        if (!isSpawningEnabled) return;

        ClearAllFood();
        SpawnInitialFood();
#if UNITY_EDITOR
        Debug.Log("All food respawned!");
#endif
    }

    public bool IsSpawningEnabled()
    {
        return isSpawningEnabled;
    }

    // NEW: Getter for active food count
    public int GetActiveFoodCount()
    {
        return activeFood.Count;
    }

    // NEW: Check if any food is currently active
    public bool HasActiveFood()
    {
        return activeFood.Count > 0;
    }
}

// Helper class to notify spawner when food is collected
public class FoodCollectionNotifier : MonoBehaviour
{
    private FoodSpawner spawner;
    private Transform spawnPoint;

    public void Initialize(FoodSpawner spawner, Transform spawnPoint)
    {
        this.spawner = spawner;
        this.spawnPoint = spawnPoint;
    }

    /// <summary>
    /// Call this when food is collected to return it to the pool.
    /// </summary>
    public void Collect()
    {
        if (spawner != null && spawnPoint != null)
        {
            spawner.OnFoodCollected(spawnPoint);
        }
        SimpleObjectPool.Return(gameObject);
    }

    private void OnDestroy()
    {
        // Fallback: if truly destroyed (scene unload etc), still notify spawner
        if (spawner != null && spawnPoint != null)
        {
            spawner.OnFoodCollected(spawnPoint);
        }
    }
}
