using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Transform spawnPoint; // Optional: Drag a child object here
    
    void Start()
    {
        if (spawnPoint == null)
        {
            // Create a spawn point slightly above the checkpoint
            GameObject spawnObj = new GameObject("SpawnPoint");
            spawnObj.transform.SetParent(transform);
            spawnObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            spawnPoint = spawnObj.transform;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(spawnPoint.position, spawnPoint.rotation);
                
                // Visual feedback
                GetComponent<Renderer>().material.color = Color.green;
                Destroy(gameObject, 0.1f); // Remove checkpoint after use (optional)
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}