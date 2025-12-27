using UnityEngine;
using System.Collections.Generic;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] foodSpawnPoints;
    public GameObject[] goFoodPrefabs;
    public GameObject[] junkFoodPrefabs;

    [Header("Spawn Rates")]
    [Range(0, 100)]
    public int goFoodSpawnChance = 70; // 70% chance for go food

    [Header("Game Settings")]
    public bool spawnAtStart = false; // CHANGE TO FALSE - We'll spawn manually
    public bool respawnWhenCollected = true;
    public float respawnDelay = 5f; // Time before respawning food

    private List<GameObject> activeFood = new List<GameObject>();
    private List<bool> spawnPointOccupied;
    private List<float> respawnTimers;
    private bool isSpawningEnabled = false;

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
        spawnPointOccupied = new List<bool>();
        respawnTimers = new List<float>();

        for (int i = 0; i < foodSpawnPoints.Length; i++)
        {
            spawnPointOccupied.Add(false);
            respawnTimers.Add(0f);
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

        for (int i = 0; i < foodSpawnPoints.Length; i++)
        {
            if (foodSpawnPoints[i] != null && !spawnPointOccupied[i])
            {
                SpawnFoodAtPoint(i);
            }
        }
    }

    private void SpawnFoodAtPoint(int spawnPointIndex)
    {
        if (spawnPointIndex >= foodSpawnPoints.Length || foodSpawnPoints[spawnPointIndex] == null)
            return;

        Transform spawnPoint = foodSpawnPoints[spawnPointIndex];

        // Randomly decide what type of food to spawn
        GameObject foodPrefab = GetRandomFoodPrefab();

        if (foodPrefab != null)
        {
            // Spawn the food and make spawn point its parent
            GameObject food = Instantiate(foodPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            activeFood.Add(food);

            // Mark spawn point as occupied
            spawnPointOccupied[spawnPointIndex] = true;

            // Add a script to notify when food is collected
            FoodCollectionNotifier notifier = food.AddComponent<FoodCollectionNotifier>();
            notifier.Initialize(this, spawnPointIndex);

            Debug.Log($"Spawned food at point {spawnPointIndex}: {foodPrefab.name}");
        }
    }

    private GameObject GetRandomFoodPrefab()
    {
        if (goFoodPrefabs.Length == 0 && junkFoodPrefabs.Length == 0)
        {
            Debug.LogError("No food prefabs assigned to FoodSpawner!");
            return null;
        }

        int randomValue = Random.Range(0, 100);

        if (randomValue < goFoodSpawnChance && goFoodPrefabs.Length > 0)
        {
            // Spawn go food
            int randomIndex = Random.Range(0, goFoodPrefabs.Length);
            return goFoodPrefabs[randomIndex];
        }
        else if (junkFoodPrefabs.Length > 0)
        {
            // Spawn junk food
            int randomIndex = Random.Range(0, junkFoodPrefabs.Length);
            return junkFoodPrefabs[randomIndex];
        }
        else if (goFoodPrefabs.Length > 0)
        {
            // Fallback to go food if junk food array is empty
            int randomIndex = Random.Range(0, goFoodPrefabs.Length);
            return goFoodPrefabs[randomIndex];
        }

        Debug.LogError("Failed to spawn any food!");
        return null;
    }

    public void OnFoodCollected(int spawnPointIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPointOccupied.Count)
            return;

        // Mark spawn point as available
        spawnPointOccupied[spawnPointIndex] = false;

        if (respawnWhenCollected && isSpawningEnabled)
        {
            // Start respawn timer
            respawnTimers[spawnPointIndex] = respawnDelay;
        }
    }

    private void UpdateRespawnTimers()
    {
        for (int i = 0; i < respawnTimers.Count; i++)
        {
            if (respawnTimers[i] > 0f)
            {
                respawnTimers[i] -= Time.deltaTime;

                if (respawnTimers[i] <= 0f && !spawnPointOccupied[i])
                {
                    // Respawn food at this point
                    SpawnFoodAtPoint(i);
                }
            }
        }
    }

    public void ClearAllFood()
    {
        foreach (GameObject food in activeFood)
        {
            if (food != null)
            {
                Destroy(food);
            }
        }
        activeFood.Clear();

        // Reset all spawn points
        for (int i = 0; i < spawnPointOccupied.Count; i++)
        {
            spawnPointOccupied[i] = false;
            respawnTimers[i] = 0f;
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

    // Helper method to check if spawning is enabled
    public bool IsSpawningEnabled()
    {
        return isSpawningEnabled;
    }
}

// Helper class to notify spawner when food is collected
public class FoodCollectionNotifier : MonoBehaviour
{
    private FoodSpawner spawner;
    private int spawnPointIndex = -1;

    public void Initialize(FoodSpawner spawner, int spawnPointIndex)
    {
        this.spawner = spawner;
        this.spawnPointIndex = spawnPointIndex;
    }

    private void OnDestroy()
    {
        if (spawner != null && spawnPointIndex >= 0)
        {
            spawner.OnFoodCollected(spawnPointIndex);
        }
    }
}