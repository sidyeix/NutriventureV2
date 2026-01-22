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

    [Header("ITEM PREFABS")]
    [Header("ALLERGEN PREFABS (UNSAFE)")]
    public List<GameObject> allergenPrefabs = new List<GameObject>();

    [Header("HEALTHY FOODS (SAFE)")]
    public GameObject bananaPrefab;
    public GameObject applePrefab;
    public GameObject avocadoPrefab;
    public GameObject kiwiPrefab;

    [Header("SPAWN SETTINGS")]
    public int safePerRow = 1;          // Healthy foods per row
    public int allergensPerRow = 2;     // Allergen items per row
    [Range(0, 1)] public float healthyFoodChance = 1.0f; // Always healthy food (since no coins)

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

    void RandomizeRow(Row row)
    {
        if (row.rocks.Count < 3) return;

        // Shuffle rocks
        List<GameObject> shuffled = new List<GameObject>(row.rocks);
        Shuffle(shuffled);

        int index = 0;

        // ✅ Spawn SAFE items (healthy foods only)
        for (int i = 0; i < safePerRow && index < shuffled.Count; i++)
        {
            GameObject healthyFoodPrefab = GetRandomHealthyFoodPrefab();
            if (healthyFoodPrefab != null)
            {
                SpawnItemOnRock(shuffled[index], healthyFoodPrefab, row.itemHeight);
                index++;
            }
        }

        // ✅ Spawn ALLERGEN items (random from list)
        for (int i = 0; i < allergensPerRow && index < shuffled.Count; i++)
        {
            GameObject allergenPrefab = GetRandomAllergenPrefab();
            if (allergenPrefab != null)
            {
                SpawnItemOnRock(shuffled[index], allergenPrefab, row.itemHeight);
                index++;
            }
        }
    }

    GameObject GetRandomHealthyFoodPrefab()
    {
        List<GameObject> availableHealthyFoods = new List<GameObject>();
        
        if (bananaPrefab != null) availableHealthyFoods.Add(bananaPrefab);
        if (applePrefab != null) availableHealthyFoods.Add(applePrefab);
        if (avocadoPrefab != null) availableHealthyFoods.Add(avocadoPrefab);
        if (kiwiPrefab != null) availableHealthyFoods.Add(kiwiPrefab);
        
        if (availableHealthyFoods.Count > 0)
        {
            return availableHealthyFoods[Random.Range(0, availableHealthyFoods.Count)];
        }
        
        Debug.LogWarning("No healthy food prefabs assigned!");
        return null;
    }

    void SpawnItemOnRock(GameObject rock, GameObject itemPrefab, float height)
    {
        if (rock == null || itemPrefab == null) return;

        // Create item at rock's position
        Vector3 spawnPos = rock.transform.position + Vector3.up * height;
        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        // **CRITICAL: Add the TransformFollower component**
        TransformFollower follower = item.AddComponent<TransformFollower>();
        follower.target = rock.transform;
        follower.enableFloating = true;
        follower.floatHeight = 0.2f;
        follower.floatSpeed = 1.4f;
        follower.verticalOffset = 1.0f;

        // Store references
        rockToItemMap[rock] = item;
        itemFollowers[item] = follower;

        // Setup ItemCollectible if needed
        ItemCollectible collectible = item.GetComponent<ItemCollectible>();
        if (collectible == null)
        {
            collectible = item.AddComponent<ItemCollectible>();
        }

        // Name for clarity
        item.name = $"{itemPrefab.name}_On_{rock.name}";
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

    void Update()
    {
        // Optional: Debug visualization
        foreach (var kvp in rockToItemMap)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                Debug.DrawLine(kvp.Key.transform.position, kvp.Value.transform.position, Color.green);
            }
        }
    }

    // Helper method to get info about what's on a rock
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

    // Get all items of a specific type
    public List<GameObject> GetAllItemsOfType(string containsName)
    {
        List<GameObject> items = new List<GameObject>();
        foreach (var item in rockToItemMap.Values)
        {
            if (item != null && item.name.Contains(containsName))
            {
                items.Add(item);
            }
        }
        return items;
    }

    #if UNITY_EDITOR
    [ContextMenu("Create Default 4 Rows")]
    void CreateDefaultRows()
    {
        rows.Clear();
        for (int i = 0; i < 4; i++)
        {
            rows.Add(new Row { rowName = $"Row {i + 1}", itemHeight = 1.5f });
        }
        Debug.Log("Created 4 default rows");
    }

    [ContextMenu("Print Current Setup")]
    void PrintCurrentSetup()
    {
        string report = "\n🎮 CURRENT SPAWN SETUP:\n";
        report += "=========================\n";
        
        foreach (Row row in rows)
        {
            report += $"\n{row.rowName}:\n";
            int safeCount = 0;
            int allergenCount = 0;
            int bananaCount = 0;
            int appleCount = 0;
            int avocadoCount = 0;
            int kiwiCount = 0;
            
            foreach (GameObject rock in row.rocks)
            {
                if (rockToItemMap.ContainsKey(rock))
                {
                    GameObject item = rockToItemMap[rock];
                    if (item != null)
                    {
                        string itemName = item.name.ToLower();
                        
                        // Check for allergens
                        bool isAllergen = false;
                        foreach (GameObject allergenPrefab in allergenPrefabs)
                        {
                            if (allergenPrefab != null && itemName.Contains(allergenPrefab.name.ToLower()))
                            {
                                allergenCount++;
                                isAllergen = true;
                                break;
                            }
                        }
                        
                        // Check for healthy foods
                        if (!isAllergen)
                        {
                            safeCount++;
                            if (itemName.Contains("banana")) bananaCount++;
                            else if (itemName.Contains("apple")) appleCount++;
                            else if (itemName.Contains("avocado")) avocadoCount++;
                            else if (itemName.Contains("kiwi")) kiwiCount++;
                        }
                    }
                }
            }
            
            report += $"  Safe Items: {safeCount}\n";
            report += $"    • Banana: {bananaCount}\n";
            report += $"    • Apple: {appleCount}\n";
            report += $"    • Avocado: {avocadoCount}\n";
            report += $"    • Kiwi: {kiwiCount}\n";
            report += $"  Allergens: {allergenCount}\n";
            report += $"  Total: {safeCount + allergenCount} items\n";
        }
        
        report += "\n=========================\n";
        Debug.Log(report);
    }
    #endif
}