using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public Transform spawnPoint;
    public Transform movingPlatform;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CheckpointManager.Instance.SetCheckpoint(
            spawnPoint.position,
            spawnPoint.rotation,
            movingPlatform
        );
    }
}
