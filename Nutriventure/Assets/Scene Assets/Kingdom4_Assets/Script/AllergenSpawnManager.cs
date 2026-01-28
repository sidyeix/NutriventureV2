using UnityEngine;
using System.Collections.Generic;

public class AllergenSpawnManager : MonoBehaviour
{
    [Header("Allergen Prefabs (Big Nine Only)")]
    public List<GameObject> allergenPrefabs = new List<GameObject>();

    [Header("Spawn Points")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Spawn Settings")]
    public bool spawnOneOfEachAllergen = true;
    public int maxAllergensToSpawn = 9;
    public float minDistanceBetweenSpawns = 2f;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showGizmos = true;
    private bool hasSpawned = false;


    // ================= INTERNAL =================
    private readonly List<GameObject> spawnedAllergens = new List<GameObject>();
    private readonly Dictionary<string, GameObject> allergenPrefabMap =
        new Dictionary<string, GameObject>();

    // ================= UNITY =================
    void Start()
{
    InitializeAllergenMap(); // ✅ ALWAYS DO THIS

    if (BookInteractable.Instance == null || !BookInteractable.Instance.IsClaimed)
    {
        if (showDebugInfo)
            Debug.Log("🚫 Allergens not spawned: Scroll not claimed yet.");
        return;
    }

    SpawnAllergens();
}




    // ================= INITIALIZATION =================
    private void InitializeAllergenMap()
    {
        allergenPrefabMap.Clear();

        foreach (GameObject prefab in allergenPrefabs)
        {
            if (prefab == null) continue;

            IngredientInteractable interactable =
                prefab.GetComponent<IngredientInteractable>();

            if (interactable == null || string.IsNullOrEmpty(interactable.ingredientId))
            {
                Debug.LogWarning(
                    $"Prefab '{prefab.name}' is missing IngredientInteractable or ingredientId");
                continue;
            }

            string id = interactable.ingredientId.ToLowerInvariant();

            if (!allergenPrefabMap.ContainsKey(id))
            {
                allergenPrefabMap.Add(id, prefab);

                if (showDebugInfo)
                    Debug.Log($"Mapped allergen prefab: {id} → {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"Duplicate allergen ID detected: {id}");
            }
        }
    }

    // ================= SPAWNING =================
    private void SpawnAllergens()
{
    if (hasSpawned) return;

    if (spawnPoints.Count == 0 || allergenPrefabMap.Count == 0)
    {
        Debug.LogError("Cannot spawn allergens: missing prefabs or spawn points.");
        return;
    }

    if (spawnedAllergens.Count > 0)
        ClearAllAllergens();

    if (spawnOneOfEachAllergen)
        SpawnOneOfEach();
    else
        SpawnRandom();

    hasSpawned = true;

    if (showDebugInfo)
        Debug.Log($"Spawned {spawnedAllergens.Count} allergens.");
}

    private void SpawnOneOfEach()
    {
        List<string> allergenIds = new List<string>(allergenPrefabMap.Keys);
        List<Transform> points = new List<Transform>(spawnPoints);

        Shuffle(allergenIds);
        Shuffle(points);

        int spawnLimit = Mathf.Min(maxAllergensToSpawn, points.Count, allergenIds.Count);

        for (int i = 0; i < spawnLimit; i++)
        {
            TrySpawn(allergenIds[i], points[i]);
        }
    }
    

    private void SpawnRandom()
    {
        List<Transform> points = new List<Transform>(spawnPoints);
        Shuffle(points);

        int spawnLimit = Mathf.Min(maxAllergensToSpawn, points.Count);

        for (int i = 0; i < spawnLimit; i++)
        {
            string randomId = GetRandomAllergenId();
            TrySpawn(randomId, points[i]);
        }
    }

    private bool TrySpawn(string allergenId, Transform point)
    {
        if (!allergenPrefabMap.ContainsKey(allergenId)) return false;
        if (!IsValidSpawnPosition(point.position)) return false;

        GameObject allergen =
            Instantiate(allergenPrefabMap[allergenId], point.position, Quaternion.identity);

        allergen.transform.SetParent(point);
        spawnedAllergens.Add(allergen);

        if (showDebugInfo)
            Debug.Log($"Spawned allergen [{allergenId}] at {point.name}");

        return true;
    }

    // ================= VALIDATION =================
    private bool IsValidSpawnPosition(Vector3 position)
    {
        foreach (GameObject a in spawnedAllergens)
        {
            if (a != null &&
                Vector3.Distance(position, a.transform.position) < minDistanceBetweenSpawns)
                return false;
        }
        return true;
    }

    private string GetRandomAllergenId()
    {
        List<string> keys = new List<string>(allergenPrefabMap.Keys);
        return keys[Random.Range(0, keys.Count)];
    }

    // ================= COLLECTION =================
    public void OnAllergenCollected(GameObject allergen)
{
    if (spawnedAllergens.Remove(allergen))
    {
        IngredientInteractable i = allergen.GetComponent<IngredientInteractable>();

        if (showDebugInfo)
            Debug.Log($"Collected allergen: {i?.ingredientId}");

        // ✅ SCORING HOOK (PHASE 1)
        if (Kingdom4ScoreManager.Instance != null)
        {
            Kingdom4ScoreManager.Instance.AddAllergenFound();
        }
    }
}


    // ================= UTILITIES =================
private void ClearAllAllergens()
{
    foreach (GameObject a in spawnedAllergens)
        if (a != null)
            Destroy(a);

    spawnedAllergens.Clear();
    hasSpawned = false;
}


    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // ================= EDITOR HELPERS =================
    [ContextMenu("Collect Child Spawn Points")]
    private void CollectChildSpawnPoints()
    {
        spawnPoints.Clear();
        foreach (Transform child in transform)
            spawnPoints.Add(child);

        Debug.Log($"Collected {spawnPoints.Count} spawn points.");
    }

    [ContextMenu("Spawn Allergens Now")]
public void SpawnNow()
{
    if (BookInteractable.Instance == null || !BookInteractable.Instance.IsClaimed)
    {
        if (showDebugInfo)
            Debug.Log("🚫 SpawnNow blocked: Scroll not claimed.");
        return;
    }

    SpawnAllergens();
}


    [ContextMenu("Clear Allergens")]
    private void ClearNow() => ClearAllAllergens();

    // ================= GIZMOS =================
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        foreach (Transform t in spawnPoints)
            if (t != null)
                Gizmos.DrawWireSphere(t.position, 0.3f);

        Gizmos.color = Color.yellow;
        foreach (GameObject a in spawnedAllergens)
            if (a != null)
                Gizmos.DrawWireSphere(a.transform.position, 0.4f);
    }
}
