using UnityEngine;

public class LevelCompleteTrigger : MonoBehaviour
{
    [SerializeField] private GameEndManager gameEndManager;

    void Start()
    {
        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<GameEndManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && gameEndManager != null)
        {
            Debug.Log("Level Complete Trigger Activated!");

            // Trigger level complete
            gameEndManager.TriggerLevelComplete();

            // Disable trigger
            GetComponent<Collider>().enabled = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (GetComponent<BoxCollider>() != null)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Gizmos.DrawWireCube(transform.position + col.center, col.size);
        }
    }
}