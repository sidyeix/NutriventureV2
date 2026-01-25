using UnityEngine;

public class GlowPartTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GlowPartManager glowPartManager;

    [Header("Settings")]
    [SerializeField] private bool disableAfterTrigger = true;
    [SerializeField] private bool showDebugGizmo = true;

    private bool hasBeenTriggered = false;

    void Start()
    {
        if (glowPartManager == null)
            glowPartManager = FindObjectOfType<GlowPartManager>();

        if (glowPartManager == null)
        {
            Debug.LogError($"GlowPartTrigger on {gameObject.name}: No GlowPartManager found!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasBeenTriggered && other.CompareTag("Player"))
        {
            Debug.Log("Player entered Glow Part trigger");

            hasBeenTriggered = true;

            // Start the glow part ONLY - NO MONSTER ACTIVATION
            if (glowPartManager != null)
            {
                glowPartManager.StartGlowPart();
            }
            else
            {
                Debug.LogError("GlowPartManager not found!");
            }

            // Disable trigger if configured
            if (disableAfterTrigger)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmo) return;

        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
        else if (GetComponent<SphereCollider>() != null)
        {
            SphereCollider col = GetComponent<SphereCollider>();
            Gizmos.DrawSphere(transform.position + col.center, col.radius);
        }
        else if (GetComponent<CapsuleCollider>() != null)
        {
            CapsuleCollider col = GetComponent<CapsuleCollider>();

            Vector3 top = transform.position + col.center + Vector3.up * (col.height * 0.5f - col.radius);
            Vector3 bottom = transform.position + col.center - Vector3.up * (col.height * 0.5f - col.radius);

            Gizmos.DrawWireSphere(top, col.radius);
            Gizmos.DrawWireSphere(bottom, col.radius);

            Gizmos.DrawLine(top + Vector3.right * col.radius, bottom + Vector3.right * col.radius);
            Gizmos.DrawLine(top - Vector3.right * col.radius, bottom - Vector3.right * col.radius);
            Gizmos.DrawLine(top + Vector3.forward * col.radius, bottom + Vector3.forward * col.radius);
            Gizmos.DrawLine(top - Vector3.forward * col.radius, bottom - Vector3.forward * col.radius);
        }
    }

    // NEW: Reset method for game restart
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        Debug.Log($"GlowPartTrigger {gameObject.name} reset");
    }
}