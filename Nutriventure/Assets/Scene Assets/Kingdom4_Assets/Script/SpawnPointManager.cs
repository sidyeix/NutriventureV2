using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    [System.Serializable]
    public class RowDefinition
    {
        public int rowNumber;
        public string rowName;
        public Transform leftPoint;
        public Transform middlePoint;
        public Transform rightPoint;
        
        [Header("Safety Pattern")]
        [Tooltip("L=Left Safe, M=Middle Safe, R=Right Safe, X=Unsafe\nExamples: LMR=all safe, LMX=Left+Middle safe, LXX=Only Left safe")]
        public string safetyPattern = "LMR"; // Default to all safe
        
        public bool IsColumnSafe(int columnIndex)
        {
            if (string.IsNullOrEmpty(safetyPattern))
                return false;
            
            safetyPattern = safetyPattern.ToUpper();
            
            return columnIndex switch
            {
                0 => safetyPattern.Contains("L"), // Left
                1 => safetyPattern.Contains("M"), // Middle
                2 => safetyPattern.Contains("R"), // Right
                _ => false
            };
        }
        
        public Transform GetPoint(int columnIndex)
        {
            return columnIndex switch
            {
                0 => leftPoint,
                1 => middlePoint,
                2 => rightPoint,
                _ => null
            };
        }
        
        public void SetPoint(int columnIndex, Transform point)
        {
            switch (columnIndex)
            {
                case 0: leftPoint = point; break;
                case 1: middlePoint = point; break;
                case 2: rightPoint = point; break;
            }
        }
        
        public int GetSafeColumnCount()
        {
            int count = 0;
            if (IsColumnSafe(0)) count++;
            if (IsColumnSafe(1)) count++;
            if (IsColumnSafe(2)) count++;
            return count;
        }
        
        public int GetUnsafeColumnCount()
        {
            return 3 - GetSafeColumnCount();
        }
    }
    
    [Header("Row Configuration")]
    public List<RowDefinition> rows = new List<RowDefinition>();
    public int totalRows = 25;
    
    [Header("Safety Pattern Settings")]
    public bool useRandomPatterns = true;
    [Tooltip("Possible safety patterns (L=Left, M=Middle, R=Right, X=Unsafe)")]
    public List<string> possiblePatterns = new List<string>
    {
        "LMR", // 3 safe, 0 harmful
        "LMX", // 2 safe, 1 harmful
        "LXR", // 2 safe, 1 harmful
        "XMR", // 2 safe, 1 harmful
        "LXX", // 1 safe, 2 harmful
        "XMX", // 1 safe, 2 harmful
        "XXR"  // 1 safe, 2 harmful
    };
    [Tooltip("Weight for each pattern (higher = more likely)")]
    public List<int> patternWeights = new List<int> { 1, 3, 3, 3, 5, 5, 5 };
    
    [Header("Item Data")]
    public SpawnableItemData coinData;
    public SpawnableItemData peanutData;
    public SpawnableItemData milkData;
    public SpawnableItemData eggData;
    public SpawnableItemData fishData;
    public SpawnableItemData shellfishData;
    public SpawnableItemData treeNutData;
    public SpawnableItemData wheatData;
    public SpawnableItemData soybeanData;
    public SpawnableItemData sesameData;
    public SpawnableItemData shieldData;
    public SpawnableItemData heartData;
    
    [Header("Spawn Rules")]
    [Range(0f, 1f)] public float powerupSpawnChance = 0.3f; // Increased for testing
    public int coinsPerSafePoint = 3;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool drawGizmos = true;
    public bool spawnOnStart = true;
    
    private Dictionary<Transform, List<GameObject>> spawnedItems = new Dictionary<Transform, List<GameObject>>();
    
    void Start()
    {
        InitializeRows();
        
        if (useRandomPatterns)
        {
            GenerateRandomSafetyPatterns();
        }
        
        if (spawnOnStart)
        {
            SpawnAllItems();
        }
        
        LogPatternSummary();
    }
    
    void InitializeRows()
    {
        // Ensure we have exactly totalRows
        if (rows.Count != totalRows)
        {
            rows.Clear();
            
            for (int i = 0; i < totalRows; i++)
            {
                rows.Add(new RowDefinition
                {
                    rowNumber = i + 1,
                    rowName = $"Row {i + 1}",
                    safetyPattern = "LMR" // Default to all safe
                });
            }
        }
    }
    
    void GenerateRandomSafetyPatterns()
    {
        if (possiblePatterns.Count == 0)
        {
            Debug.LogError("No possible patterns defined!");
            return;
        }
        
        // Ensure weights list matches patterns list
        while (patternWeights.Count < possiblePatterns.Count)
        {
            patternWeights.Add(1); // Default weight
        }
        
        for (int i = 0; i < rows.Count; i++)
        {
            // Weighted random selection
            string randomPattern = GetWeightedRandomPattern();
            rows[i].safetyPattern = randomPattern;
            
            if (showDebugInfo)
            {
                int safeCount = rows[i].GetSafeColumnCount();
                int unsafeCount = rows[i].GetUnsafeColumnCount();
                Debug.Log($"Row {i + 1}: Pattern={randomPattern} ({safeCount} safe, {unsafeCount} harmful)");
            }
        }
    }
    
    string GetWeightedRandomPattern()
    {
        // Calculate total weight
        int totalWeight = 0;
        for (int i = 0; i < patternWeights.Count && i < possiblePatterns.Count; i++)
        {
            totalWeight += patternWeights[i];
        }
        
        // Random selection with weights
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        for (int i = 0; i < possiblePatterns.Count; i++)
        {
            currentWeight += patternWeights[i];
            if (randomValue < currentWeight)
            {
                return possiblePatterns[i];
            }
        }
        
        return possiblePatterns[0]; // Fallback
    }
    
    void SpawnAllItems()
    {
        ClearAllItems();
        
        foreach (var row in rows)
        {
            SpawnRowItems(row);
        }
        
        Debug.Log($"Spawned {GetTotalSpawnedItems()} items across {rows.Count} rows");
    }
    
    void SpawnRowItems(RowDefinition row)
{
    for (int col = 0; col < 3; col++)
    {
        Transform point = row.GetPoint(col);
        if (point == null) continue;

        if (row.IsColumnSafe(col))
            SpawnSafeItem(point, row, col);
        else
            SpawnHarmfulItem(point, row, col);
    }
}

    
    void SpawnSafeItem(Transform point, RowDefinition row, int columnIndex)

    {
        // Decide whether to spawn coins OR powerup at this point
        float randomValue = Random.value;
        
        if (randomValue < powerupSpawnChance)
        {
            // Spawn powerup instead of coins
            SpawnableItemData powerupData = Random.value > 0.5f ? shieldData : heartData;
            if (powerupData != null && powerupData.prefab != null)
            {
                Vector3 powerupPos = point.position + Vector3.up * 0.5f;
                SpawnItemAtPosition(powerupPos, powerupData, point, row, columnIndex);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Spawned {powerupData.itemType} at {point.name}");
                }
            }
        }
        else
        {
            // Spawn coins
            if (coinData != null && coinData.prefab != null)
            {
                for (int i = 0; i < coinsPerSafePoint; i++)
                {
                    Vector3 offset = Vector3.zero;
                    if (coinsPerSafePoint > 1)
                    {
                        float angle = (i * 360f / coinsPerSafePoint) * Mathf.Deg2Rad;
                        offset = new Vector3(Mathf.Cos(angle) * 0.3f, 0, Mathf.Sin(angle) * 0.3f);
                    }
                    
                    SpawnItemAtPosition(point.position + offset, coinData, point, row, columnIndex);
                }
            }
        }
    }
    
    void SpawnHarmfulItem(Transform point, RowDefinition row, int columnIndex)
    {
        SpawnableItemData allergenData = GetRandomAllergen();
        if (allergenData != null && allergenData.prefab != null)
        {
            SpawnItemAtPosition(point.position, allergenData, point, row, columnIndex);
        }
    }
    
    SpawnableItemData GetRandomAllergen()
    {
        // Create list of allergens
        List<SpawnableItemData> allergens = new List<SpawnableItemData>();
        
        if (peanutData != null) allergens.Add(peanutData);
        if (milkData != null) allergens.Add(milkData);
        if (eggData != null) allergens.Add(eggData);
        if (fishData != null) allergens.Add(fishData);
        if (shellfishData != null) allergens.Add(shellfishData);
        if (treeNutData != null) allergens.Add(treeNutData);
        if (wheatData != null) allergens.Add(wheatData);
        if (soybeanData != null) allergens.Add(soybeanData);
        if (sesameData != null) allergens.Add(sesameData);
        
        if (allergens.Count == 0)
        {
            Debug.LogWarning("No allergen data assigned!");
            return null;
        }
        
        // Random selection
        return allergens[Random.Range(0, allergens.Count)];
    }
    
    void SpawnItemAtPosition(
    Vector3 position,
    SpawnableItemData itemData,
    Transform parent,
    RowDefinition row,
    int columnIndex
)
    {
        if (itemData == null || itemData.prefab == null) return;
        
        GameObject spawnedItem = Instantiate(itemData.prefab, position, Quaternion.identity);
        spawnedItem.transform.SetParent(parent);
        spawnedItem.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        
FloatingAnimation floatAnim = spawnedItem.GetComponent<FloatingAnimation>();
if (floatAnim == null)
    floatAnim = spawnedItem.AddComponent<FloatingAnimation>();

// FORCE animation values (overwrite prefab data)
floatAnim.speed = 2f;
floatAnim.amplitude = 0.25f;

// ROW PATTERN
bool isOddRow = row.rowNumber % 2 == 1;

// STRICT COLUMN PATTERN
if (isOddRow)
{
    floatAnim.phaseOffset = columnIndex switch
    {
        0 => Mathf.PI / 2f,    // L ↑↓
        1 => -Mathf.PI / 2f,   // M ↓↑
        2 => Mathf.PI / 2f,    // R ↑↓
        _ => 0f
    };
}
else
{
    floatAnim.phaseOffset = columnIndex switch
    {
        0 => -Mathf.PI / 2f,   // L ↓↑
        1 => Mathf.PI / 2f,    // M ↑↓
        2 => -Mathf.PI / 2f,   // R ↓↑
        _ => 0f
    };
}



        
        // Store reference
        if (!spawnedItems.ContainsKey(parent))
        {
            spawnedItems[parent] = new List<GameObject>();
        }
        spawnedItems[parent].Add(spawnedItem);
    }
    
    void ClearAllItems()
    {
        foreach (var itemList in spawnedItems.Values)
        {
            foreach (var item in itemList)
            {
                if (item != null) Destroy(item);
            }
        }
        spawnedItems.Clear();
    }
    
    int GetTotalSpawnedItems()
    {
        int total = 0;
        foreach (var itemList in spawnedItems.Values)
        {
            total += itemList.Count;
        }
        return total;
    }
    
    void LogPatternSummary()
    {
        if (!showDebugInfo) return;
        
        int totalSafeColumns = 0;
        int totalHarmfulColumns = 0;
        Dictionary<string, int> patternCounts = new Dictionary<string, int>();
        
        foreach (var row in rows)
        {
            totalSafeColumns += row.GetSafeColumnCount();
            totalHarmfulColumns += row.GetUnsafeColumnCount();
            
            string pattern = row.safetyPattern;
            if (patternCounts.ContainsKey(pattern))
                patternCounts[pattern]++;
            else
                patternCounts[pattern] = 1;
        }
        
        Debug.Log("=== Pattern Summary ===");
        Debug.Log($"Total rows: {rows.Count}");
        Debug.Log($"Total safe columns: {totalSafeColumns}");
        Debug.Log($"Total harmful columns: {totalHarmfulColumns}");
        Debug.Log($"Average safe per row: {(float)totalSafeColumns / rows.Count:F2}");
        Debug.Log($"Average harmful per row: {(float)totalHarmfulColumns / rows.Count:F2}");
        
        Debug.Log("Pattern distribution:");
        foreach (var kvp in patternCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} rows");
        }
    }
    
    // Public API
    public void RegeneratePatterns()
    {
        GenerateRandomSafetyPatterns();
        SpawnAllItems();
        LogPatternSummary();
    }
    
    public void SetRowPattern(int rowNumber, string pattern)
    {
        if (rowNumber < 1 || rowNumber > rows.Count)
        {
            Debug.LogError($"Invalid row number: {rowNumber}");
            return;
        }
        
        rows[rowNumber - 1].safetyPattern = pattern;
        
        // Respawn items for this row
        ClearRowItems(rows[rowNumber - 1]);
        SpawnRowItems(rows[rowNumber - 1]);
    }
    
    void ClearRowItems(RowDefinition row)
    {
        for (int col = 0; col < 3; col++)
        {
            Transform point = row.GetPoint(col);
            if (point != null && spawnedItems.ContainsKey(point))
            {
                foreach (var item in spawnedItems[point])
                {
                    if (item != null) Destroy(item);
                }
                spawnedItems.Remove(point);
            }
        }
    }
    
    public string GetRowPattern(int rowNumber)
    {
        if (rowNumber < 1 || rowNumber > rows.Count) return "";
        return rows[rowNumber - 1].safetyPattern;
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            
            // Draw each point in the row
            for (int col = 0; col < 3; col++)
            {
                Transform point = row.GetPoint(col);
                if (point == null) continue;
                
                // Color based on safety
                bool isSafe = row.IsColumnSafe(col);
                Gizmos.color = isSafe ? Color.green : Color.red;
                Gizmos.DrawWireSphere(point.position, 0.5f);
                
                // Draw label
                Vector3 labelPos = point.position + Vector3.up * 0.3f;
                string label = $"Row {i + 1}\n";
                label += col == 0 ? "L" : col == 1 ? "M" : "R";
                label += isSafe ? " (Safe)" : " (Harmful)";
                UnityEditor.Handles.Label(labelPos, label);
            }
            
            // Draw row number in the middle
            if (row.middlePoint != null)
            {
                UnityEditor.Handles.Label(row.middlePoint.position + Vector3.up * 2f, 
                    $"Row {i + 1}: {row.safetyPattern}", 
                    new GUIStyle() { 
                        normal = new GUIStyleState() { 
                            textColor = Color.yellow,
                            background = Texture2D.whiteTexture
                        },
                        fontSize = 12
                    });
            }
        }
    }
    #endif
}