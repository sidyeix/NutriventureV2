using UnityEngine;

public class StartSequenceTrigger : MonoBehaviour
{
    [SerializeField] private StartingSequenceManager sequenceManager;

    void Start()
    {
        // Auto-find if not assigned
        if (sequenceManager == null)
            sequenceManager = FindObjectOfType<StartingSequenceManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered start trigger");

            if (sequenceManager != null)
            {
                sequenceManager.StartSequence();
            }
            else
            {
                Debug.LogError("StartingSequenceManager not assigned!");
            }

            // Optional: Disable trigger after use
            // GetComponent<Collider>().enabled = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Green transparent
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
    }
}