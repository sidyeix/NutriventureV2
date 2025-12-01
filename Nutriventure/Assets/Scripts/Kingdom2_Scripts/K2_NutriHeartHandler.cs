using UnityEngine;

public class NutriHeartCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    public string heartTag = "NutriHeart";
    public int healthRestoreAmount = 1;
    public float collectionRange = 2f;
    public LayerMask heartLayerMask = -1;
    
    [Header("Particle System")]
    public ParticleSystem collectionParticleSystem;
    public bool attachParticlesToPlayer = true;
    public float particleDuration = 2f;
    
    [Header("Audio")]
    public AudioClip collectionSound;
    public float soundVolume = 0.7f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;
    
    private SugariaPlayerStat playerHealth;
    private AudioSource audioSource;
    private Transform playerTransform;
    private bool isInitialized = false;

    void Start()
    {
        InitializeCollector();
    }

    private void InitializeCollector()
    {
        playerTransform = transform;
        playerHealth = GetComponent<SugariaPlayerStat>();
        
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<SugariaPlayerStat>();
            if (playerHealth == null)
            {
                Debug.LogError("SugariaPlayerStat not found on player or parent! Make sure the health script is on the same GameObject or parent.");
                return;
            }
        }

        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log("NutriHeart collector initialized successfully!");
            Debug.Log($"Player Health Found: {playerHealth != null}");
            Debug.Log($"Audio Source Ready: {audioSource != null}");
            Debug.Log($"Particle System Assigned: {collectionParticleSystem != null}");
        }
    }

    void Update()
    {
        if (!isInitialized) return;
        
        // Auto-collect hearts when in range
        AutoCollectHeartsInRange();
    }

    private void AutoCollectHeartsInRange()
    {
        // Find all hearts in range
        Collider[] heartsInRange = Physics.OverlapSphere(playerTransform.position, collectionRange, heartLayerMask);
        
        if (showDebugLogs && heartsInRange.Length > 0)
        {
            Debug.Log($"Found {heartsInRange.Length} colliders in range");
        }

        foreach (Collider heartCollider in heartsInRange)
        {
            if (heartCollider != null && heartCollider.CompareTag(heartTag))
            {
                if (showDebugLogs) Debug.Log($"Found heart in range: {heartCollider.name}");
                CollectHeart(heartCollider.gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isInitialized) return;
        
        if (showDebugLogs) Debug.Log($"Trigger entered with: {other.name} (Tag: {other.tag})");
        
        if (other.CompareTag(heartTag))
        {
            if (showDebugLogs) Debug.Log($"Trigger collection with: {other.name}");
            CollectHeart(other.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized) return;
        
        if (showDebugLogs) Debug.Log($"Collision with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        
        if (collision.gameObject.CompareTag(heartTag))
        {
            if (showDebugLogs) Debug.Log($"Collision collection with: {collision.gameObject.name}");
            CollectHeart(collision.gameObject);
        }
    }

    public void CollectHeart(GameObject heartObject)
    {
        if (heartObject == null) 
        {
            if (showDebugLogs) Debug.LogWarning("Tried to collect null heart object!");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"=== COLLECTING HEART: {heartObject.name} ===");

        // Check if we can heal (not at full health)
        if (playerHealth != null)
        {
            if (playerHealth.currentHealth >= playerHealth.maxHealth)
            {
                if (showDebugLogs) Debug.Log("Player already at full health! Cannot collect heart.");
                return;
            }

            int oldHealth = playerHealth.currentHealth;
            playerHealth.Heal(healthRestoreAmount);
            
            if (showDebugLogs) 
            {
                Debug.Log($"Health restored: {oldHealth} -> {playerHealth.currentHealth}");
                Debug.Log($"Healed {healthRestoreAmount} health points!");
            }
        }
        else
        {
            if (showDebugLogs) Debug.LogError("No player health component found during collection!");
            return;
        }
        
        // Play particle effect
        PlayCollectionParticles();
        
        // Play sound
        PlayCollectionSound();
        
        // Destroy the heart
        if (showDebugLogs) Debug.Log($"Destroying heart object: {heartObject.name}");
        Destroy(heartObject);
        
        if (showDebugLogs) Debug.Log($"=== HEART COLLECTION COMPLETE ===");
    }

    private void PlayCollectionParticles()
    {
        if (collectionParticleSystem != null)
        {
            // Get the position for particles (player's position)
            Vector3 particlePosition = playerTransform.position;
            
            // Create the particle system
            ParticleSystem particles = Instantiate(collectionParticleSystem, particlePosition, Quaternion.identity);
            
            if (attachParticlesToPlayer)
            {
                particles.transform.SetParent(playerTransform);
            }
            
            // Ensure it plays
            particles.Play();
            
            // Destroy after duration
            Destroy(particles.gameObject, particleDuration);
            
            if (showDebugLogs) Debug.Log("Collection particles instantiated and playing");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("No collection particle system assigned!");
        }
    }

    private void PlayCollectionSound()
    {
        if (collectionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectionSound, soundVolume);
            if (showDebugLogs) Debug.Log("Collection sound played");
        }
        else
        {
            if (showDebugLogs) 
            {
                if (collectionSound == null) Debug.LogWarning("No collection sound assigned!");
                if (audioSource == null) Debug.LogWarning("No audio source available!");
            }
        }
    }

    // Manual collection method (can be called from other scripts)
    public void ManualCollectHeart(GameObject heartObject)
    {
        if (!isInitialized) 
        {
            Debug.LogWarning("Collector not initialized!");
            return;
        }
        CollectHeart(heartObject);
    }

    // Set collection range dynamically
    public void SetCollectionRange(float newRange)
    {
        collectionRange = Mathf.Max(0.5f, newRange);
        if (showDebugLogs) Debug.Log($"Collection range set to: {collectionRange}");
    }

    // Set health restore amount
    public void SetHealthRestoreAmount(int amount)
    {
        healthRestoreAmount = Mathf.Max(1, amount);
        if (showDebugLogs) Debug.Log($"Health restore amount set to: {healthRestoreAmount}");
    }

    // Visualize collection range in editor
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
        
        // Draw a small indicator at the player's position
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.3f);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Always show a small wire sphere for the collection range
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }

    [ContextMenu("Test Heart Collection")]
    public void TestHeartCollection()
    {
        if (!isInitialized) 
        {
            Debug.LogError("Collector not initialized! Cannot test.");
            return;
        }

        Debug.Log("=== TESTING HEART COLLECTION ===");
        
        // Create a test heart with proper collider
        GameObject testHeart = new GameObject("TestHeart");
        testHeart.tag = heartTag;
        testHeart.transform.position = transform.position + Vector3.forward * 1f;
        
        // Add a collider (make it a trigger)
        SphereCollider collider = testHeart.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.5f;
        
        // Add a visible component
        MeshFilter meshFilter = testHeart.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = testHeart.AddComponent<MeshRenderer>();
        
        Debug.Log("Created test heart, attempting collection...");
        CollectHeart(testHeart);
    }

    [ContextMenu("Debug Collector Status")]
    public void DebugCollectorStatus()
    {
        Debug.Log("=== COLLECTOR STATUS ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Player Transform: {playerTransform}");
        Debug.Log($"Player Health: {playerHealth}");
        Debug.Log($"Audio Source: {audioSource}");
        Debug.Log($"Particle System: {collectionParticleSystem}");
        Debug.Log($"Collection Range: {collectionRange}");
        Debug.Log($"Heart Tag: {heartTag}");
        
        if (playerHealth != null)
        {
            Debug.Log($"Current Health: {playerHealth.currentHealth}/{playerHealth.maxHealth}");
        }
        
        // Check for hearts in scene
        GameObject[] hearts = GameObject.FindGameObjectsWithTag(heartTag);
        Debug.Log($"Hearts in scene: {hearts.Length}");
        foreach (GameObject heart in hearts)
        {
            Collider heartCollider = heart.GetComponent<Collider>();
            Debug.Log($"- {heart.name}: Collider={heartCollider != null}, IsTrigger={heartCollider?.isTrigger}, Position={heart.transform.position}");
        }
    }
}