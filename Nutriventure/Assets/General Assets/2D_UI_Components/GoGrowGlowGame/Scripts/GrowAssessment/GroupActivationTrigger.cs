using UnityEngine;

public class GroupActivationTrigger : MonoBehaviour
{
    [SerializeField] private ObjectGroupManager groupManager;

    void Start()
    {
        if (groupManager == null)
        {
            // Try to find in parent
            groupManager = GetComponentInParent<ObjectGroupManager>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && groupManager != null)
        {
            #if UNITY_EDITOR
            Debug.Log($"Player entered group: {groupManager.gameObject.name}");
            #endif

            // Activate the group FIRST
            groupManager.OnPlayerEnterGroup();

            // Wait one frame for objects to be properly activated, then set isEntry
            StartCoroutine(SetEntryAnimationAfterActivation());

            // Optional: Disable trigger after activation
            // GetComponent<Collider>().enabled = false;
        }
    }

    private System.Collections.IEnumerator SetEntryAnimationAfterActivation()
    {
        // Wait one frame to ensure objects are fully activated
        yield return null;

        // NEW: Set isEntry to true on the ObjectGroupManager's animator
        groupManager.SetGroupEntryAnimation(true);
        #if UNITY_EDITOR
        Debug.Log($"Set isEntry = true on ObjectGroupManager: {groupManager.gameObject.name}");
        #endif
    }

    void OnDrawGizmos()
    {
        if (groupManager != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange transparent
            if (GetComponent<BoxCollider>() != null)
            {
                BoxCollider col = GetComponent<BoxCollider>();
                Gizmos.DrawCube(transform.position + col.center, col.size);
            }

            // Draw line to group starting point
            Transform startingPoint = groupManager.GetGroupStartingPoint();
            if (startingPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, startingPoint.position);
            }
        }
    }
}
