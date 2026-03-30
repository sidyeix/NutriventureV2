using System.Collections.Generic;
using UnityEngine;

public class K4_RockObstacleManager : MonoBehaviour
{
  [Header("Safe Foods")]
  [SerializeField] private GameObject[] safeFoodPrefabs;
  [SerializeField] private Transform[] safeFoodSpawnPoints;

  [Header("Heart")]
  [SerializeField] private GameObject heartPrefab;

  [Header("Allergens")]
  [SerializeField] private GameObject[] allergenPrefabs;
  [SerializeField] private Transform[] allergenSpawnPoints;

  [Header("Floating Animation")]
  [SerializeField] private float floatAmplitude = 0.5f;
  [SerializeField] private float floatSpeed = 2f;

  private readonly List<GameObject> spawnedObjects = new List<GameObject>();

  /// <summary>
  /// Call this from AllergenGameManager when the game starts.
  /// </summary>
  public void SpawnAll()
  {
    ClearAll();

    // Pick a random safe-food point for the heart (if available)
    int heartPointIndex = -1;
    if (heartPrefab != null && safeFoodSpawnPoints != null && safeFoodSpawnPoints.Length > 0)
    {
      heartPointIndex = Random.Range(0, safeFoodSpawnPoints.Length);
      Transform heartPoint = safeFoodSpawnPoints[heartPointIndex];
      GameObject heart = Instantiate(heartPrefab, heartPoint.position, heartPoint.rotation, heartPoint);
      AddFloating(heart);
      spawnedObjects.Add(heart);
    }

    // Spawn safe foods on all points EXCEPT the heart's point
    SpawnFoods(safeFoodSpawnPoints, safeFoodPrefabs, heartPointIndex);
    SpawnFoods(allergenSpawnPoints, allergenPrefabs);
  }

  /// <summary>
  /// Destroys all spawned food/heart objects.
  /// </summary>
  public void ClearAll()
  {
    foreach (GameObject obj in spawnedObjects)
    {
      if (obj != null)
        Destroy(obj);
    }
    spawnedObjects.Clear();
  }

  private void SpawnFoods(Transform[] spawnPoints, GameObject[] prefabs, int skipIndex = -1)
  {
    if (spawnPoints == null || spawnPoints.Length == 0 || prefabs == null || prefabs.Length == 0)
      return;

    for (int i = 0; i < spawnPoints.Length; i++)
    {
      if (i == skipIndex) continue;

      Transform point = spawnPoints[i];
      GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
      GameObject food = Instantiate(prefab, point.position, point.rotation, point);
      AddFloating(food);
      spawnedObjects.Add(food);
    }
  }

  private void AddFloating(GameObject obj)
  {
    FloatingAnimation fa = obj.GetComponent<FloatingAnimation>();
    if (fa == null)
      fa = obj.AddComponent<FloatingAnimation>();

    fa.amplitude = floatAmplitude;
    fa.speed = floatSpeed;
    fa.phaseOffset = Random.Range(0f, Mathf.PI * 2f);
  }
}
