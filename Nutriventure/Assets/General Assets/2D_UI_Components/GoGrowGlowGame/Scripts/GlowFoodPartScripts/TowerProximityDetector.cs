using UnityEngine;

public class TowerProximityDetector : MonoBehaviour
{
    [Header("Tower Reference")]
    [SerializeField] private GlowTower glowTower;

    [Header("Detection Settings")]
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private bool showDebug = true;

    private Transform playerTransform;
    private Coroutine detectionCoroutine;

    public System.Action<GlowTower> OnPlayerEnterRange;
    public System.Action<GlowTower> OnPlayerExitRange;

    private bool wasPlayerInRange = false;

    private void Start()
    {
        // Find player using GameObject tag instead of specific controller type
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }

        if (glowTower == null)
            glowTower = GetComponent<GlowTower>();

        if (playerTransform != null && glowTower != null)
        {
            detectionCoroutine = StartCoroutine(DetectionRoutine());
        }
        else
        {
            Debug.LogWarning($"TowerProximityDetector on {gameObject.name}: Player or GlowTower not found!");
        }
    }

    private System.Collections.IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (playerTransform == null || glowTower == null) continue;

            bool isInRange = Vector3.Distance(playerTransform.position, glowTower.GetCenterPointPosition()) <= glowTower.GetRange();

            if (isInRange && !wasPlayerInRange)
            {
                // Player entered range
                wasPlayerInRange = true;
                OnPlayerEnterRange?.Invoke(glowTower);
                Debug.Log($"Player entered range of {glowTower.gameObject.name}");
            }
            else if (!isInRange && wasPlayerInRange)
            {
                // Player exited range
                wasPlayerInRange = false;
                OnPlayerExitRange?.Invoke(glowTower);
                Debug.Log($"Player exited range of {glowTower.gameObject.name}");
            }
        }
    }

    private void OnDestroy()
    {
        if (detectionCoroutine != null)
            StopCoroutine(detectionCoroutine);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug || glowTower == null) return;

        Vector3 center = glowTower.GetCenterPointPosition();
        float range = glowTower.GetRange();

        // Draw range sphere
        Gizmos.color = wasPlayerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(center, range);

        // Draw connection to player
        if (playerTransform != null)
        {
            Gizmos.color = wasPlayerInRange ? Color.green : Color.red;
            Gizmos.DrawLine(center, playerTransform.position);
        }
    }
}