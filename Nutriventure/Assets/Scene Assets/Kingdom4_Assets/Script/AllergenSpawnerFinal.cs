using UnityEngine;
using System.Collections.Generic;

public class AllergenSpawnerFinal : MonoBehaviour
{
    [System.Serializable]
    public class Row
    {
        public string rowName = "Row 1";
        public List<GameObject> rocks = new List<GameObject>();
        public float itemHeight = 1.5f;
    }

    [Header("YOUR ROWS")]
    public List<Row> rows = new List<Row>();

    [Header("ALLERGEN PREFABS")]
    public List<GameObject> allergenPrefabs = new List<GameObject>();

    [Header("SPAWN SETTINGS")]
    public int itemsPerRow = 3; // How many items to spawn per row

    // Store all spawned items
    private Dictionary<GameObject, GameObject> rockToItemMap = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, TransformFollower> itemFollowers = new Dictionary<GameObject, TransformFollower>();

    void Start()
    {
        RandomizeAllRows();
    }

    [ContextMenu("Randomize All Rows")]
    void RandomizeAllRows()
    {
        ClearAllItems();
        
        foreach (Row row in rows)
        {
            RandomizeRow(row);
        }
        
        Debug.Log($"Spawned items on {rockToItemMap.Count} rocks");
    }

    public void RandomizeRow(Row row)
    {
        if (row.rocks.Count == 0) return;

        // Clear existing items on these rocks first
        ClearItemsOnRocks(row.rocks);

        // Shuffle rocks
        List<GameObject> shuffled = new List<GameObject>(row.rocks);
        Shuffle(shuffled);

        // Spawn allergens on ALL rocks (up to itemsPerRow)
        int itemsToSpawn = Mathf.Min(itemsPerRow, shuffled.Count);
        
        for (int i = 0; i < itemsToSpawn; i++)
        {
            GameObject allergenPrefab = GetRandomAllergenPrefab();
            if (allergenPrefab != null)
            {
                SpawnItemOnRock(shuffled[i], allergenPrefab, row.itemHeight);
            }
        }
        
        Debug.Log($"Randomized row {row.rowName} with {itemsToSpawn} allergens");
    }

    GameObject GetRandomAllergenPrefab()
    {
        if (allergenPrefabs == null || allergenPrefabs.Count == 0)
        {
            Debug.LogWarning("No allergen prefabs assigned!");
            return null;
        }

        int index = Random.Range(0, allergenPrefabs.Count);
        return allergenPrefabs[index];
    }

    public GameObject GetAllergenPrefabByName(string allergenName)
    {
        foreach (var prefab in allergenPrefabs)
        {
            if (prefab != null && prefab.name.Equals(allergenName, System.StringComparison.OrdinalIgnoreCase))
                return prefab;
        }
        // Try partial match
        foreach (var prefab in allergenPrefabs)
        {
            if (prefab != null && prefab.name.IndexOf(allergenName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return prefab;
        }
        Debug.LogWarning($"No allergen prefab found for: {allergenName}. Using random.");
        return GetRandomAllergenPrefab();
    }

    public List<GameObject> GetSafeAllergenPrefabs(string excludeAllergen, int count)
    {
        List<GameObject> safeOptions = allergenPrefabs.FindAll(p =>
            p != null &&
            p.name.IndexOf(excludeAllergen, System.StringComparison.OrdinalIgnoreCase) < 0);
        if (safeOptions.Count == 0) return new List<GameObject>();
        Shuffle(safeOptions);
        return safeOptions.GetRange(0, Mathf.Min(count, safeOptions.Count));
    }

    /// <summary>
    /// Spawns the specified allergen on all rocks in the dangerous column.
    /// </summary>
    public void SpawnSpecificAllergenOnRocks(List<GameObject> rocks, string allergenName, float height)
    {
        ClearItemsOnRocks(rocks);
        GameObject prefab = GetAllergenPrefabByName(allergenName);
        foreach (var rock in rocks)
        {
            if (rock != null && prefab != null)
                SpawnItemOnRock(rock, prefab, height);
        }
    }

    /// <summary>
    /// Spawns safe (non-dangerous) allergens on two safe columns.
    /// Guarantees neither column gets the dangerous allergen.
    /// </summary>
    public void SpawnSafeAllergensOnRocks(List<GameObject> safeRocks1, List<GameObject> safeRocks2, string dangerousAllergen, float height)
    {
        ClearItemsOnRocks(safeRocks1);
        ClearItemsOnRocks(safeRocks2);

        List<GameObject> safePrefabs = GetSafeAllergenPrefabs(dangerousAllergen, 2);
        GameObject safe1Prefab = safePrefabs.Count > 0 ? safePrefabs[0] : GetRandomAllergenPrefab();
        GameObject safe2Prefab = safePrefabs.Count > 1 ? safePrefabs[1] : GetRandomAllergenPrefab();

        foreach (var rock in safeRocks1)
            if (rock != null) SpawnItemOnRock(rock, safe1Prefab, height);

        foreach (var rock in safeRocks2)
            if (rock != null) SpawnItemOnRock(rock, safe2Prefab, height);
    }

   void SpawnItemOnRock(GameObject rock, GameObject itemPrefab, float height)
{
    if (rock == null || itemPrefab == null) return;

    Vector3 spawnPos = rock.transform.position + Vector3.up * height;
    GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
    
    // Add TransformFollower component
    TransformFollower follower = item.AddComponent<TransformFollower>();
    follower.target = rock.transform;
    follower.enableFloating = true;
    follower.floatHeight = 0.2f;
    follower.floatSpeed = 1.4f;
    follower.verticalOffset = 1.0f;

    // ADD THIS: Tag the item for detection
    item.tag = "AllergenItem";
    
    // Make it a child of the rock for organization
    item.transform.SetParent(rock.transform);

    // Store references
    rockToItemMap[rock] = item;
    itemFollowers[item] = follower;

    string allergenName = itemPrefab.name.Replace("(Clone)", "").Trim();

    SmallRockTrigger trigger = rock.GetComponent<SmallRockTrigger>();
    if (trigger != null)
    {
        trigger.SetAllergen(allergenName);
    }

    item.name = $"{allergenName}_On_{rock.name}";
}

    public void ClearItemsOnRocks(List<GameObject> rocks)
    {
        foreach (GameObject rock in rocks)
        {
            if (rockToItemMap.ContainsKey(rock))
            {
                GameObject item = rockToItemMap[rock];
                if (item != null)
                {
                    Destroy(item);
                }
                rockToItemMap.Remove(rock);
            }
        }
    }

    public GameObject GetItemOnRock(GameObject rock)
    {
        if (rockToItemMap.ContainsKey(rock))
        {
            return rockToItemMap[rock];
        }
        return null;
    }

    public string GetRockItemType(GameObject rock)
    {
        if (rockToItemMap.ContainsKey(rock))
        {
            GameObject item = rockToItemMap[rock];
            if (item != null)
            {
                return item.name;
            }
        }
        return "Empty";
    }

    [ContextMenu("Clear All Items")]
    void ClearAllItems()
    {
        foreach (var item in rockToItemMap.Values)
        {
            if (item != null) Destroy(item);
        }
        
        rockToItemMap.Clear();
        itemFollowers.Clear();
        
        Debug.Log("Cleared all items");
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }

    #if UNITY_EDITOR
    [ContextMenu("Print Current Setup")]
    void PrintCurrentSetup()
    {
        string report = "\n🎮 CURRENT SPAWN SETUP:\n";
        report += "=========================\n";
        
        foreach (Row row in rows)
        {
            report += $"\n{row.rowName}:\n";
            Dictionary<string, int> allergenCounts = new Dictionary<string, int>();
            
            foreach (GameObject rock in row.rocks)
            {
                if (rockToItemMap.ContainsKey(rock))
                {
                    GameObject item = rockToItemMap[rock];
                    if (item != null)
                    {
                        string itemName = item.name.Replace("(Clone)", "").Trim();
                        if (allergenCounts.ContainsKey(itemName))
                            allergenCounts[itemName]++;
                        else
                            allergenCounts[itemName] = 1;
                    }
                }
            }
            
            foreach (var kvp in allergenCounts)
            {
                report += $"  • {kvp.Key}: {kvp.Value}\n";
            }
        }
        
        report += "\n=========================\n";
        Debug.Log(report);
    }
    #endif
}