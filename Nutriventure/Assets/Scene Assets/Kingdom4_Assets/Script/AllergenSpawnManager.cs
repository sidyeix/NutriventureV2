using UnityEngine;
using System.Collections.Generic;

public class AllergenSpawnManager : MonoBehaviour
{
    [Header("Allergen Prefabs")]
    public List<GameObject> allergenPrefabs = new List<GameObject>(); // Drag your 9 allergen prefabs here
    
    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>(); // Your 20 empty GameObjects
    
    [Header("Spawn Settings")]
    public bool spawnAllAllergens = true; // Spawn one of each allergen type
    public int maxAllergensToSpawn = 9; // Maximum allergens to spawn
    public float minDistanceBetweenSpawns = 2f; // Minimum distance between allergens
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showGizmos = true;
    
    private List<GameObject> spawnedAllergens = new List<GameObject>();
    private Dictionary<string, GameObject> allergenPrefabMap = new Dictionary<string, GameObject>();
    
    void Start()
    {
        InitializeAllergenMap();
        SpawnAllergens();
    }
    
    void InitializeAllergenMap()
    {
        allergenPrefabMap.Clear();
        
        foreach (GameObject prefab in allergenPrefabs)
        {
            IngredientInteractable interactable = prefab.GetComponent<IngredientInteractable>();
            if (interactable != null && !string.IsNullOrEmpty(interactable.ingredientId))
            {
                string ingredientId = interactable.ingredientId.ToLower();
                
                if (!allergenPrefabMap.ContainsKey(ingredientId))
                {
                    allergenPrefabMap.Add(ingredientId, prefab);
                    
                    if (showDebugInfo)
                        Debug.Log($"Mapped prefab: {ingredientId} -> {prefab.name}");
                }
                else
                {
                    Debug.LogWarning($"Duplicate ingredientId found: {ingredientId} in {prefab.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Prefab {prefab.name} doesn't have IngredientInteractable component or missing ingredientId");
            }
        }
    }
    
    void SpawnAllergens()
    {
        if (allergenPrefabs.Count == 0)
        {
            Debug.LogError("No allergen prefabs assigned!");
            return;
        }
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }
        
        // Clear any existing allergens
        ClearAllAllergens();
        
        if (spawnAllAllergens)
        {
            SpawnOneOfEachAllergen();
        }
        else
        {
            SpawnRandomAllergens();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawned {spawnedAllergens.Count} allergens at random positions");
            Debug.Log($"Available spawn points: {spawnPoints.Count}");
        }
    }
    
    void SpawnOneOfEachAllergen()
    {
        if (spawnPoints.Count < allergenPrefabs.Count)
        {
            Debug.LogWarning($"Not enough spawn points ({spawnPoints.Count}) for all allergens ({allergenPrefabs.Count})");
        }
        
        // Create shuffled lists
        List<string> allergenIds = new List<string>(allergenPrefabMap.Keys);
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);
        
        ShuffleList(allergenIds);
        ShuffleList(availableSpawnPoints);
        
        int spawnedCount = 0;
        
        // Spawn one of each unique allergen
        for (int i = 0; i < allergenIds.Count && i < availableSpawnPoints.Count && spawnedCount < maxAllergensToSpawn; i++)
        {
            string allergenId = allergenIds[i];
            Transform spawnPoint = availableSpawnPoints[i];
            
            if (SpawnAllergenAtPoint(allergenId, spawnPoint))
            {
                spawnedCount++;
            }
        }
    }
    
    void SpawnRandomAllergens()
    {
        if (allergenPrefabs.Count == 0) return;
        
        List<Transform> availableSpawnPoints = new List<Transform>(spawnPoints);
        ShuffleList(availableSpawnPoints);
        
        int allergensToSpawn = Mathf.Min(maxAllergensToSpawn, availableSpawnPoints.Count);
        
        for (int i = 0; i < allergensToSpawn; i++)
        {
            // Pick random allergen
            GameObject randomPrefab = allergenPrefabs[Random.Range(0, allergenPrefabs.Count)];
            IngredientInteractable interactable = randomPrefab.GetComponent<IngredientInteractable>();
            
            if (interactable != null)
            {
                string allergenId = interactable.ingredientId.ToLower();
                Transform spawnPoint = availableSpawnPoints[i];
                
                SpawnAllergenAtPoint(allergenId, spawnPoint);
            }
        }
    }
    
    bool SpawnAllergenAtPoint(string allergenId, Transform spawnPoint)
    {
        if (!allergenPrefabMap.ContainsKey(allergenId))
        {
            Debug.LogWarning($"No prefab found for allergen: {allergenId}");
            return false;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point is null!");
            return false;
        }
        
        // Check if spawn point is too close to other allergens
        if (!IsValidSpawnPosition(spawnPoint.position))
            return false;
        
        GameObject prefab = allergenPrefabMap[allergenId];
        GameObject allergen = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        
        // Apply random rotation for variety
        allergen.transform.rotation = Quaternion.Euler(
            Random.Range(-10f, 10f), // Slight tilt
            Random.Range(0f, 360f),   // Full rotation
            Random.Range(-10f, 10f)   // Slight tilt
        );
        
        // Optional: Random scale variation
        float randomScale = Random.Range(0.8f, 1.2f);
        allergen.transform.localScale = Vector3.one * randomScale;
        
        // Set as child of spawn point for organization
        allergen.transform.SetParent(spawnPoint);
        
        spawnedAllergens.Add(allergen);
        
        if (showDebugInfo)
        {
            IngredientInteractable interactable = allergen.GetComponent<IngredientInteractable>();
            if (interactable != null)
            {
                Debug.Log($"Spawned {interactable.ingredientName} at {spawnPoint.name}");
            }
        }
        
        return true;
    }
    
    bool IsValidSpawnPosition(Vector3 position)
    {
        foreach (GameObject allergen in spawnedAllergens)
        {
            if (allergen != null && Vector3.Distance(position, allergen.transform.position) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }
        return true;
    }
    
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    void ClearAllAllergens()
    {
        foreach (GameObject allergen in spawnedAllergens)
        {
            if (allergen != null)
                Destroy(allergen);
        }
        spawnedAllergens.Clear();
    }
    
    // Call this when an allergen is collected
    public void OnAllergenCollected(GameObject allergen)
    {
        if (spawnedAllergens.Contains(allergen))
        {
            spawnedAllergens.Remove(allergen);
            
            if (showDebugInfo)
            {
                IngredientInteractable interactable = allergen.GetComponent<IngredientInteractable>();
                if (interactable != null)
                {
                    Debug.Log($"Allergen collected and removed: {interactable.ingredientName}");
                }
            }
        }
    }
    
    // Editor helper methods
    [ContextMenu("Collect All Child Spawn Points")]
    void CollectChildSpawnPoints()
    {
        spawnPoints.Clear();
        foreach (Transform child in transform)
        {
            if (child != null)
            {
                spawnPoints.Add(child);
            }
        }
        Debug.Log($"Collected {spawnPoints.Count} spawn points from children");
    }
    
    [ContextMenu("Spawn Allergens Now")]
    void SpawnNow()
    {
        SpawnAllergens();
    }
    
    [ContextMenu("Clear All Allergens")]
    void ClearNow()
    {
        ClearAllAllergens();
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw spawn points
        Gizmos.color = Color.green;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.3f);
                Gizmos.DrawIcon(point.position, "d_Transform Icon", true);
            }
        }
        
        // Draw spawned allergens
        Gizmos.color = Color.yellow;
        foreach (GameObject allergen in spawnedAllergens)
        {
            if (allergen != null)
            {
                Gizmos.DrawWireSphere(allergen.transform.position, 0.4f);
            }
        }
    }
}