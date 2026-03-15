using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class K3_PreservativeSpawner : MonoBehaviour
{
    [Header("Preservative Database")]
    [Tooltip("Assign your K3_PreservativeData ScriptableObject here")]
    public K3_PreservativeData preservativeDatabase;

    [Header("Spawn Configuration")]
    [Tooltip("Assign spawn point GameObjects here")]
    public GameObject[] spawnPoints;

    [Tooltip("IDs of preservatives to spawn from the database")]
    public string[] preservativeIDsToSpawn;

    [Header("Spawn Behavior")]
    public bool spawnOnStart = true;
    public bool respawnOnDemand = true;
    public bool randomizeSpawnLocations = true;

    [Header("Spawn Options")]
    [Tooltip("If true, will spawn all preservatives from database")]
    public bool spawnAllFromDatabase = false;

    [Tooltip("If true, will spawn one of each preservative type")]
    public bool spawnOneOfEachType = true;

    [Header("Debug Settings")]
    public bool enableDebugLogs = true;

    private List<GameObject> spawnedPreservatives = new List<GameObject>();
    private bool isInitialized = false;

    void Start()
    {
        if (spawnOnStart)
        {
            InitializeAndSpawn();
        }
        else
        {
            LogDebug("Auto-spawning disabled. Preservatives will not spawn automatically.");
        }
    }

    public void InitializeAndSpawn()
    {
        if (!isInitialized)
        {
            SpawnPreservatives();
            isInitialized = true;
            LogDebug("Preservative spawner initialized!");
        }
        else
        {
            LogDebug("Preservatives already spawned!");
        }
    }

    [ContextMenu("Spawn Preservatives")]
    public void SpawnPreservatives()
    {
        ClearSpawnedPreservatives();

        if (!ValidateSetup())
            return;

        // Get the list of preservatives to spawn based on configuration
        List<K3_PreservativeData.PreservativeInfo> preservativesToSpawn = GetPreservativesToSpawn();

        if (preservativesToSpawn.Count == 0)
        {
            LogError("No valid preservatives to spawn!");
            return;
        }

        // Prepare spawn points
        List<GameObject> availableSpawnPoints = spawnPoints.Where(point => point != null).ToList();

        if (availableSpawnPoints.Count == 0)
        {
            LogError("No valid spawn points available!");
            return;
        }

        // Randomize if enabled
        if (randomizeSpawnLocations)
        {
            ShuffleList(availableSpawnPoints);
            ShuffleList(preservativesToSpawn);
        }

        // Spawn preservatives
        int spawnCount = 0;
        for (int i = 0; i < preservativesToSpawn.Count && i < availableSpawnPoints.Count; i++)
        {
            var preservativeInfo = preservativesToSpawn[i];
            GameObject spawnPoint = availableSpawnPoints[i];

            if (SpawnPreservative(preservativeInfo, spawnPoint.transform))
            {
                spawnCount++;
            }
        }

        LogDebug($"Successfully spawned {spawnCount} preservatives from database. {availableSpawnPoints.Count - spawnCount} spawn points remain empty.");
    }

    [ContextMenu("Clear Preservatives")]
    public void ClearSpawnedPreservatives()
    {
        foreach (GameObject preservative in spawnedPreservatives)
        {
            if (preservative != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(preservative);
#else
                Destroy(preservative);
#endif
            }
        }
        spawnedPreservatives.Clear();
        LogDebug("Cleared all spawned preservatives");
    }

    [ContextMenu("Respawn Preservatives")]
    public void RespawnPreservatives()
    {
        if (respawnOnDemand)
        {
            SpawnPreservatives();
        }
        else
        {
            LogWarning("Respawn on demand is disabled. Enable it in the inspector or use SpawnPreservatives instead.");
        }
    }

    public void ResetSpawner()
    {
        ClearSpawnedPreservatives();
        isInitialized = false;
        LogDebug("Spawner reset - ready for new spawning");
    }

    private bool SpawnPreservative(K3_PreservativeData.PreservativeInfo preservativeInfo, Transform spawnTransform)
    {
        if (preservativeInfo == null || preservativeInfo.preservativePrefab == null)
        {
            LogError($"Cannot spawn null preservative or prefab");
            return false;
        }

        if (spawnTransform == null)
        {
            LogError($"Cannot spawn at null transform");
            return false;
        }

        GameObject spawnedPreservative = Instantiate(
            preservativeInfo.preservativePrefab,
            spawnTransform.position,
            spawnTransform.rotation,
            transform
        );

        spawnedPreservatives.Add(spawnedPreservative);

        // Add a component to track which preservative this is
        var tracker = spawnedPreservative.AddComponent<PreservativeInstance>();
        tracker.preservativeID = preservativeInfo.preservativeID;
        tracker.displayName = preservativeInfo.displayName;

        // Name for better organization
        spawnedPreservative.name = $"{preservativeInfo.displayName}_{preservativeInfo.preservativeID}_Spawned";

        LogDebug($"Spawned {preservativeInfo.displayName} ({preservativeInfo.preservativeID}) at {spawnTransform.position}");
        return true;
    }

    private List<K3_PreservativeData.PreservativeInfo> GetPreservativesToSpawn()
    {
        List<K3_PreservativeData.PreservativeInfo> result = new List<K3_PreservativeData.PreservativeInfo>();

        if (preservativeDatabase == null)
        {
            LogError("No preservative database assigned!");
            return result;
        }

        if (spawnAllFromDatabase)
        {
            // Spawn all preservatives from database
            foreach (var preservative in preservativeDatabase.allPreservatives)
            {
                if (preservative.preservativePrefab != null)
                {
                    result.Add(preservative);
                }
            }
        }
        else if (preservativeIDsToSpawn != null && preservativeIDsToSpawn.Length > 0)
        {
            // Spawn specific IDs
            foreach (string id in preservativeIDsToSpawn)
            {
                var preservative = preservativeDatabase.GetPreservativeInfo(id);
                if (preservative != null && preservative.preservativePrefab != null)
                {
                    result.Add(preservative);
                }
                else
                {
                    LogWarning($"Preservative with ID '{id}' not found in database or has no prefab assigned!");
                }
            }
        }
        else if (spawnOneOfEachType)
        {
            // Spawn one of each type (no duplicates)
            var uniquePreservatives = new Dictionary<string, K3_PreservativeData.PreservativeInfo>();
            foreach (var preservative in preservativeDatabase.allPreservatives)
            {
                if (preservative.preservativePrefab != null && !uniquePreservatives.ContainsKey(preservative.preservativeID))
                {
                    uniquePreservatives.Add(preservative.preservativeID, preservative);
                    result.Add(preservative);
                }
            }
        }

        return result;
    }

    private bool ValidateSetup()
    {
        if (preservativeDatabase == null)
        {
            LogError("No preservative database assigned! Please assign a K3_PreservativeData ScriptableObject.");
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            LogError("No spawn points assigned! Please assign spawn points in the inspector.");
            return false;
        }

        // Check for null spawn points
        int nullSpawnPoints = spawnPoints.Count(point => point == null);
        if (nullSpawnPoints > 0)
        {
            LogWarning($"Found {nullSpawnPoints} null spawn points. They will be ignored.");
        }

        // Check if we have any valid spawn points
        int validSpawnPoints = spawnPoints.Count(point => point != null);
        if (validSpawnPoints == 0)
        {
            LogError("No valid spawn points found!");
            return false;
        }

        // Check database content
        if (preservativeDatabase.allPreservatives == null || preservativeDatabase.allPreservatives.Length == 0)
        {
            LogError("Preservative database is empty!");
            return false;
        }

        int validPreservatives = preservativeDatabase.allPreservatives.Count(p => p != null && p.preservativePrefab != null);
        if (validPreservatives == 0)
        {
            LogError("No valid preservatives with prefabs found in database!");
            return false;
        }

        LogDebug($"Database contains {validPreservatives} preservatives with valid prefabs");
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

    #region Editor Tools

    [ContextMenu("Check Setup")]
    public void CheckSetup()
    {
        ValidateSetup();
        LogDebug($"=== {gameObject.name} Setup Check ===");
        LogDebug($"Database: {(preservativeDatabase != null ? preservativeDatabase.name : "NULL")}");
        LogDebug($"Total in DB: {preservativeDatabase?.allPreservatives?.Length ?? 0}");
        LogDebug($"With Prefabs: {preservativeDatabase?.allPreservatives?.Count(p => p?.preservativePrefab != null) ?? 0}");
        LogDebug($"Spawn Points: {spawnPoints?.Length ?? 0}");
        LogDebug($"Valid Spawn Points: {spawnPoints?.Count(point => point != null) ?? 0}");
        LogDebug($"IDs to Spawn: {preservativeIDsToSpawn?.Length ?? 0}");
        LogDebug($"Currently Spawned: {spawnedPreservatives.Count}");
        LogDebug($"Is Initialized: {isInitialized}");
        LogDebug($"==================================");
    }

    [ContextMenu("List All Preservatives in Database")]
    public void ListAllPreservativesInDatabase()
    {
        if (preservativeDatabase == null)
        {
            LogError("No database assigned!");
            return;
        }

        LogDebug("=== Preservatives in Database ===");
        for (int i = 0; i < preservativeDatabase.allPreservatives.Length; i++)
        {
            var preservative = preservativeDatabase.allPreservatives[i];
            if (preservative == null)
            {
                LogDebug($"[{i}] NULL ENTRY");
            }
            else
            {
                string hasPrefab = preservative.preservativePrefab != null ? "✓" : "✗";
                LogDebug($"[{i}] {hasPrefab} {preservative.displayName} ({preservative.preservativeID})");
            }
        }
        LogDebug("===============================");
    }

    [ContextMenu("Auto-assign IDs from Database")]
    public void AutoAssignIDsFromDatabase()
    {
        if (preservativeDatabase == null)
        {
            LogError("No database assigned!");
            return;
        }

        List<string> ids = new List<string>();
        foreach (var preservative in preservativeDatabase.allPreservatives)
        {
            if (preservative != null && !string.IsNullOrEmpty(preservative.preservativeID))
            {
                ids.Add(preservative.preservativeID);
            }
        }

        preservativeIDsToSpawn = ids.ToArray();
        LogDebug($"Auto-assigned {ids.Count} IDs from database");
    }

    #endregion

    #region Public Methods

    public int GetSpawnedPreservativeCount()
    {
        return spawnedPreservatives.Count;
    }

    public int GetEmptySpawnPointCount()
    {
        int validSpawnPoints = spawnPoints.Count(point => point != null);
        return Mathf.Max(0, validSpawnPoints - spawnedPreservatives.Count);
    }

    public bool AreAllPreservativesCollected()
    {
        return spawnedPreservatives.Count == 0;
    }

    public bool IsSpawnerInitialized()
    {
        return isInitialized;
    }

    public void RemovePreservative(GameObject preservative)
    {
        if (spawnedPreservatives.Contains(preservative))
        {
            spawnedPreservatives.Remove(preservative);
            Destroy(preservative);
            LogDebug($"Preservative removed from spawn system");
        }
    }

    /// <summary>
    /// Removes spawned preservatives that match the given collected IDs.
    /// Called by K3_GameStateManager on resume to hide already-collected potions.
    /// </summary>
    public void RemoveCollectedPreservatives(List<string> collectedIDs)
    {
        if (collectedIDs == null || collectedIDs.Count == 0) return;

        var toRemove = new List<GameObject>();
        foreach (var spawned in spawnedPreservatives)
        {
            if (spawned == null) continue;
            var tracker = spawned.GetComponent<PreservativeInstance>();
            if (tracker != null && collectedIDs.Contains(tracker.preservativeID))
                toRemove.Add(spawned);
        }

        foreach (var obj in toRemove)
        {
            spawnedPreservatives.Remove(obj);
            Destroy(obj);
        }

        LogDebug($"Removed {toRemove.Count} already-collected preservatives from world.");
    }

    public K3_PreservativeData.PreservativeInfo GetPreservativeInfo(GameObject spawnedObject)
    {
        var tracker = spawnedObject.GetComponent<PreservativeInstance>();
        if (tracker != null && preservativeDatabase != null)
        {
            return preservativeDatabase.GetPreservativeInfo(tracker.preservativeID);
        }
        return null;
    }

    #endregion

    #region Debug Logging

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[K3_PreservativeSpawner] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[K3_PreservativeSpawner] {message}", this);
    }

    private void LogError(string message)
    {
        Debug.LogError($"[K3_PreservativeSpawner] {message}", this);
    }

    #endregion

    #region Gizmos

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
}

// Helper component to track which preservative this spawned instance is
public class PreservativeInstance : MonoBehaviour
{
    public string preservativeID;
    public string displayName;
}