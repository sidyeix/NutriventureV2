using UnityEngine;

public class RockCheckpointSpawner : MonoBehaviour
{
    public GameObject checkpointPrefab;
    public Transform[] checkpointPoints;

    void Start()
    {
        foreach (Transform point in checkpointPoints)
        {
            Instantiate(checkpointPrefab, point.position, point.rotation, transform);
        }
    }
}
