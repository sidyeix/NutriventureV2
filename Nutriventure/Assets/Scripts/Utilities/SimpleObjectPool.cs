using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lightweight object pool to replace Instantiate/Destroy cycles on mobile.
/// Usage: SimpleObjectPool.Get(prefab, position, rotation) / SimpleObjectPool.Return(instance)
/// </summary>
public static class SimpleObjectPool
{
  private static Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
  private static Dictionary<int, int> instanceToPrefabId = new Dictionary<int, int>();

  /// <summary>
  /// Get an object from the pool or create a new one.
  /// </summary>
  public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
  {
    int prefabId = prefab.GetInstanceID();

    if (!pools.ContainsKey(prefabId))
      pools[prefabId] = new Queue<GameObject>();

    GameObject obj;
    Queue<GameObject> pool = pools[prefabId];

    // Try to get from pool, skip destroyed objects
    while (pool.Count > 0)
    {
      obj = pool.Dequeue();
      if (obj != null)
      {
        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
      }
    }

    // No pooled object available — instantiate new
    obj = Object.Instantiate(prefab, position, rotation, parent);
    instanceToPrefabId[obj.GetInstanceID()] = prefabId;
    return obj;
  }

  /// <summary>
  /// Return object to pool instead of destroying it.
  /// </summary>
  public static void Return(GameObject obj)
  {
    if (obj == null) return;

    int instanceId = obj.GetInstanceID();
    if (instanceToPrefabId.ContainsKey(instanceId))
    {
      int prefabId = instanceToPrefabId[instanceId];
      obj.SetActive(false);
      obj.transform.SetParent(null);

      if (!pools.ContainsKey(prefabId))
        pools[prefabId] = new Queue<GameObject>();

      pools[prefabId].Enqueue(obj);
    }
    else
    {
      // Object wasn't created via pool — just destroy it
      Object.Destroy(obj);
    }
  }

  /// <summary>
  /// Destroy all pooled objects and clear the pool. Call on scene unload.
  /// </summary>
  public static void ClearAll()
  {
    foreach (var kvp in pools)
    {
      while (kvp.Value.Count > 0)
      {
        GameObject obj = kvp.Value.Dequeue();
        if (obj != null)
          Object.Destroy(obj);
      }
    }
    pools.Clear();
    instanceToPrefabId.Clear();
  }
}
