using UnityEngine;

public class LevelCompleteTrigger : MonoBehaviour
{
    [SerializeField] private GameEndManager gameEndManager;

    [Header("🔑 Key Settings")]
    [SerializeField] private bool isKeyTrigger = false;
    [SerializeField] private string keyName = "Sugaria";

    private bool hasTriggered = false;

    void Start()
    {
        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<GameEndManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (!other.CompareTag("Player")) return;

        if (isKeyTrigger)
        {
            TryCollectKey();
        }
        else
        {
            TriggerLevelComplete();
        }

        hasTriggered = true;
        GetComponent<Collider>().enabled = false;
    }

    private void TriggerLevelComplete()
    {
        if (gameEndManager != null)
        {
            Debug.Log("Level Complete Trigger Activated!");
            gameEndManager.TriggerLevelComplete();
        }
    }

    private void TryCollectKey()
    {
        if (gameEndManager == null) return;
        if (GameDataManager.Instance == null) return;

        // Check if key already owned
        bool hasKey = false;
        switch (keyName.ToLower())
        {
            case "sugaria":
                hasKey = GameDataManager.Instance.HasSugariaKey();
                break;
            case "preservia":
                hasKey = GameDataManager.Instance.HasPreserviaKey();
                break;
            case "allerthia":
                hasKey = GameDataManager.Instance.HasAllerthiaKey();
                break;
            case "ocr":
                hasKey = GameDataManager.Instance.HasOCRScannerKey();
                break;
        }

        if (hasKey)
        {
            Debug.Log($"{keyName} Key already owned.");
            // Still show game summary
            gameEndManager.ShowGameEndScreen(true);
            return;
        }

        // Wait for GameEndManager to calculate stars
        // The key collection will be handled after the game summary
        Debug.Log($"Key trigger activated for {keyName} - waiting for star calculation");
        
        // Destroy the key object so it can't be collected again
        Destroy(gameObject);

        // Show game summary - GameEndManager will handle the key unlock logic
        gameEndManager.ShowGameEndScreen(true);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isKeyTrigger ? Color.yellow : Color.green;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position + col.center, col.size);
        }
    }
}