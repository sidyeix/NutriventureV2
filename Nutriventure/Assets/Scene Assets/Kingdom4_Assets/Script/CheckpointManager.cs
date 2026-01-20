using UnityEngine;
using System.Collections;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private Transform checkpointPlatform;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetCheckpoint(
        Vector3 position,
        Quaternion rotation,
        Transform platform)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
        checkpointPlatform = platform;
    }

    public void RespawnPlayer(GameObject player)
    {
        StartCoroutine(RespawnRoutine(player));
    }

    private IEnumerator RespawnRoutine(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        // Disable controller BEFORE moving
        if (controller != null)
            controller.enabled = false;

        player.transform.SetParent(null);
        player.transform.position = checkpointPosition;
        player.transform.rotation = checkpointRotation;

        // Wait one physics frame
        yield return new WaitForFixedUpdate();

        // Re-attach to platform
        if (checkpointPlatform != null)
            player.transform.SetParent(checkpointPlatform);

        // Re-enable controller AFTER parenting
        if (controller != null)
            controller.enabled = true;
    }
}
