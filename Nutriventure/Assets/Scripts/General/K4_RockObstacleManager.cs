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
    SpawnFoods(safeFoodSpawnPoints, safeFoodPrefabs);
    SpawnHeart();
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

  private void SpawnHeart()
  {
    if (heartPrefab == null || safeFoodSpawnPoints == null || safeFoodSpawnPoints.Length == 0)
      return;

    Transform point = safeFoodSpawnPoints[Random.Range(0, safeFoodSpawnPoints.Length)];
    GameObject heart = Instantiate(heartPrefab, point.position, point.rotation, point);
    AddFloating(heart);
    spawnedObjects.Add(heart);
  }

  private void SpawnFoods(Transform[] spawnPoints, GameObject[] prefabs)
  {
    if (spawnPoints == null || spawnPoints.Length == 0 || prefabs == null || prefabs.Length == 0)
      return;

    foreach (Transform point in spawnPoints)
    {
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
