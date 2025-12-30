using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;
    
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private GameObject player;
    private bool hasCheckpoint = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        FindPlayer();
        // Set initial checkpoint at player start position
        if (player != null)
        {
            checkpointPosition = player.transform.position;
            checkpointRotation = player.transform.rotation;
            hasCheckpoint = true;
        }
    }
    
    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
        hasCheckpoint = true;
        Debug.Log("Checkpoint saved at: " + position);
    }
    
    public void RespawnPlayer()
    {
        if (player == null) FindPlayer();
        if (player == null) return;
        
        if (hasCheckpoint)
        {
            // Teleport player
            player.transform.position = checkpointPosition;
            player.transform.rotation = checkpointRotation;
            
            // Reset physics
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log("Player respawned at checkpoint");
        }
        else
        {
            // Fallback to start
            player.transform.position = Vector3.zero;
        }
    }
}