using UnityEngine;

public class TowerProximityDetector : MonoBehaviour
{
    [Header("Tower Reference")]
    [SerializeField] public GlowTower glowTower;

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
#if UNITY_EDITOR
            Debug.LogWarning($"TowerProximityDetector on {gameObject.name}: Player or GlowTower not found!");
#endif
        }
    }

    private System.Collections.IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return CoroutineYieldCache.WaitForSeconds(checkInterval);

            if (playerTransform == null || glowTower == null) continue;

            // Use horizontal (XZ) distance only — ignoring Y so tall towers still detect the player at ground level
            Vector3 diff = playerTransform.position - glowTower.GetCenterPointPosition();
            float rangeSqr = glowTower.GetRange() * glowTower.GetRange();
            bool isInRange = (diff.x * diff.x + diff.z * diff.z) <= rangeSqr;

            if (isInRange && !wasPlayerInRange)
            {
                // Player entered range
                wasPlayerInRange = true;
                OnPlayerEnterRange?.Invoke(glowTower);
#if UNITY_EDITOR
                Debug.Log($"Player entered range of {glowTower.gameObject.name}");
#endif
            }
            else if (!isInRange && wasPlayerInRange)
            {
                // Player exited range
                wasPlayerInRange = false;
                OnPlayerExitRange?.Invoke(glowTower);
#if UNITY_EDITOR
                Debug.Log($"Player exited range of {glowTower.gameObject.name}");
#endif
            }
        }
    }

    /// <summary>
    /// Force an immediate re-check of player proximity. Resets wasPlayerInRange so
    /// the enter event fires again if the player is currently within range.
    /// </summary>
    public void ForceRecheck()
    {
        wasPlayerInRange = false;
    }

    public void ResetDetector()
    {
#if UNITY_EDITOR
        Debug.Log($"Resetting TowerProximityDetector on {gameObject.name}");
#endif

        // Stop the detection coroutine
        if (detectionCoroutine != null)
        {
            StopCoroutine(detectionCoroutine);
            detectionCoroutine = null;
        }

        // Reset state
        wasPlayerInRange = false;

        // Find player again (in case it changed)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }

        // Restart detection if conditions are met
        if (playerTransform != null && glowTower != null)
        {
            detectionCoroutine = StartCoroutine(DetectionRoutine());
        }

#if UNITY_EDITOR
        Debug.Log($"TowerProximityDetector reset complete for {gameObject.name}");
#endif
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
