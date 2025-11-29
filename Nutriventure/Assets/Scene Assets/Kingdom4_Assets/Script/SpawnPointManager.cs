using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject peanutPrefab;
    public int peanutsToSpawn = 10;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private List<Transform> spawnPoints = new List<Transform>();
    
    void Start()
    {
        FindSpawnPoints();
        SpawnPeanuts();
    }
    
    void FindSpawnPoints()
    {
        spawnPoints.Clear();
        
        foreach (Transform child in transform)
        {
            spawnPoints.Add(child);
        }
        
        Debug.Log($"Found {spawnPoints.Count} spawn points");
    }
    
    void SpawnPeanuts()
    {
        if (spawnPoints.Count == 0 || peanutPrefab == null) return;
        
        // Create a shuffled list of spawn points
        List<Transform> shuffledPoints = new List<Transform>(spawnPoints);
        ShuffleList(shuffledPoints);
        
        // Spawn only at unique points (limited by available points)
        int pointsToUse = Mathf.Min(peanutsToSpawn, shuffledPoints.Count);
        
        for (int i = 0; i < pointsToUse; i++)
        {
            SpawnPeanutAtPoint(shuffledPoints[i]);
        }
        
        Debug.Log($"Spawned {pointsToUse} peanuts at unique locations");
    }
    
    void SpawnPeanutAtPoint(Transform spawnPoint)
    {
        GameObject peanut = Instantiate(peanutPrefab, spawnPoint.position, Quaternion.identity);
        peanut.transform.SetParent(spawnPoint);
        peanut.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }
    
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform child in transform)
            {
                Gizmos.DrawWireSphere(child.position, 0.5f);
            }
        }
    }
}