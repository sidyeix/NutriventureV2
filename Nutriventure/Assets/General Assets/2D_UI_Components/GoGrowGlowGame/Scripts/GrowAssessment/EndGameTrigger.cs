using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [SerializeField] private StartingSequenceManager sequenceManager;

    void Start()
    {
        if (sequenceManager == null)
            sequenceManager = FindObjectOfType<StartingSequenceManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && sequenceManager != null)
        {
            Debug.Log("Player reached end game trigger");

            // This re-enables all controls and UI
            sequenceManager.EnableAllControlsAndUI();

            // Optional: Disable this trigger after use
            // GetComponent<Collider>().enabled = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Red transparent
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawCube(transform.position + col.center, col.size);
        }
    }
}