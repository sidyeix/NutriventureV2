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

        // Initialize all spawn points
        AddSpawnPointsToArray(goFoodSpawnPoints);
        AddSpawnPointsToArray(growFoodSpawnPoints);
        AddSpawnPointsToArray(glowFoodSpawnPoints);
        AddSpawnPointsToArray(junkFoodSpawnPoints);
    }

    private void AddSpawnPointsToArray(Transform[] spawnPoints)
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null && !allSpawnPoints.Contains(spawnPoint))
            {
                allSpawnPoints.Add(spawnPoint);
                spawnPointOccupied[spawnPoint] = false;
                respawnTimers[spawnPoint] = 0f;
            }
        }
    }

    public void StartSpawning()
    {
        isSpawningEnabled = true;
        SpawnInitialFood();
        Debug.Log("Food Spawning Started!");
    }

    public void StopSpawning()
    {
        isSpawningEnabled = false;
        ClearAllFood();
        Debug.Log("Food Spawning Stopped!");
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
        foreach (Transform spawnPoint in spawnPoints)
        {
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
            Debug.LogWarning($"No food prefabs assigned for spawn point: {spawnPoint.name}!");
            return;
        }

        // Select random prefab from the array
        int randomIndex = Random.Range(0, prefabs.Length);
        GameObject foodPrefab = prefabs[randomIndex];

        if (foodPrefab != null)
        {
            // Spawn the food and make spawn point its parent
            GameObject food = Instantiate(foodPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            activeFood.Add(food);

            // Mark spawn point as occupied
            spawnPointOccupied[spawnPoint] = true;

            // Add a script to notify when food is collected
            FoodCollectionNotifier notifier = food.AddComponent<FoodCollectionNotifier>();
            notifier.Initialize(this, spawnPoint);

            Debug.Log($"Spawned food at {spawnPoint.name}: {foodPrefab.name}");
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
        // Create a list of spawn points that need respawning
        List<Transform> spawnPointsToRespawn = new List<Transform>();

        // First pass: update timers and collect points that need respawning
        foreach (Transform spawnPoint in allSpawnPoints)
        {
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
        foreach (Transform spawnPoint in spawnPointsToRespawn)
        {
            // Determine which prefab array to use based on spawn point
            GameObject[] prefabs = GetPrefabsForSpawnPoint(spawnPoint);

            // Respawn food at this point
            SpawnFoodAtPoint(spawnPoint, prefabs);
        }
    }

    private GameObject[] GetPrefabsForSpawnPoint(Transform spawnPoint)
    {
        // Check which array this spawn point belongs to
        if (System.Array.Exists(goFoodSpawnPoints, point => point == spawnPoint))
            return goFoodPrefabs;
        else if (System.Array.Exists(growFoodSpawnPoints, point => point == spawnPoint))
            return growFoodPrefabs;
        else if (System.Array.Exists(glowFoodSpawnPoints, point => point == spawnPoint))
            return glowFoodPrefabs;
        else if (System.Array.Exists(junkFoodSpawnPoints, point => point == spawnPoint))
            return junkFoodPrefabs;

        Debug.LogWarning($"Spawn point {spawnPoint.name} not found in any spawn point array!");
        return new GameObject[0];
    }

    public void ClearAllFood()
    {
        // Destroy all active food
        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] != null)
            {
                Destroy(activeFood[i]);
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

        Debug.Log("All food cleared!");
    }

    public void RespawnAllFood()
    {
        if (!isSpawningEnabled) return;

        ClearAllFood();
        SpawnInitialFood();
        Debug.Log("All food respawned!");
    }

    public bool IsSpawningEnabled()
    {
        return isSpawningEnabled;
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

    private void OnDestroy()
    {
        if (spawner != null && spawnPoint != null)
        {
            spawner.OnFoodCollected(spawnPoint);
        }
    }
}