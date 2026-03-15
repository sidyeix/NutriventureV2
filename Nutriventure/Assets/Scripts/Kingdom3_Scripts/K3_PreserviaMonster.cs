using UnityEngine;

public class K3_PreserviaMonster : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage dealt to player per attack")]
    public int damageAmount = 1;
    
    [Tooltip("Cooldown between attacks (seconds)")]
    public float attackCooldown = 1.5f;
    
    [Tooltip("Force applied to push player away")]
    public float pushForce = 5f;
    
    [Header("Attack Collider")]
    [Tooltip("Box Collider on the monster's mouth (for biting attack)")]
    public BoxCollider attackCollider;
    
    [Header("Attack Particles")]
    [Tooltip("Particle system prefab to spawn at attack location")]
    public GameObject attackParticlePrefab;
    
    [Tooltip("Duration before destroying particle system")]
    public float particleDuration = 2f;
    
    [Tooltip("Offset from collider center for particle spawn")]
    public Vector3 particleOffset = Vector3.zero;
    
    [Header("Audio")]
    [Tooltip("Sound to play when attacking")]
    public AudioClip attackSound;
    
    [Tooltip("Volume for attack sound")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages in console")]
    public bool showDebugMessages = true;
    
    // REMOVED: private AudioSource audioSource; - NO LOCAL AUDIO SOURCE
    private float lastAttackTime;
    private bool canAttack = true;
    
    void Start()
    {
        InitializeMonster();
    }
    
    private void InitializeMonster()
    {
        // REMOVED: AudioSource setup - NO LOCAL AUDIO SOURCE
        
        // Validate attack collider
        if (attackCollider == null)
        {
            attackCollider = GetComponent<BoxCollider>();
            if (attackCollider == null)
            {
                Debug.LogWarning("No BoxCollider found on monster! Please add one to the mouth area.");
            }
            else
            {
                // Make sure it's set as trigger for attack detection
                attackCollider.isTrigger = true;
            }
        }
        else
        {
            // Ensure assigned collider is a trigger
            attackCollider.isTrigger = true;
        }
        
        // Validate particle prefab
        if (attackParticlePrefab != null)
        {
            ParticleSystem ps = attackParticlePrefab.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                Debug.LogWarning("Attack particle prefab doesn't have a ParticleSystem component!");
            }
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"Preservia Monster initialized");
            Debug.Log($"Attack collider: {(attackCollider != null ? attackCollider.name : "Not found")}");
            Debug.Log($"Particle prefab: {(attackParticlePrefab != null ? attackParticlePrefab.name : "Not assigned")}");
            Debug.Log($"Damage: {damageAmount}, Cooldown: {attackCooldown}s");
        }
        
        // Check AudioHandler exists
        if (AudioHandler.Instance == null)
        {
            Debug.LogWarning("AudioHandler.Instance not found! Make sure AudioHandler is in the scene.");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if can attack (cooldown)
        if (!canAttack || Time.time < lastAttackTime + attackCooldown)
            return;
        
        // Check if collided object is player
        if (other.CompareTag("Player"))
        {
            // Get collision point for particle spawn
            Vector3 collisionPoint = GetCollisionPoint(other);
            AttackPlayer(other.gameObject, collisionPoint);
        }
    }
    
    private Vector3 GetCollisionPoint(Collider other)
    {
        // Get the closest point on the player's collider to our attack collider
        Vector3 closestPoint = other.ClosestPoint(attackCollider.bounds.center);
        
        // Alternatively, use the attack collider's center
        // Vector3 closestPoint = attackCollider.transform.TransformPoint(attackCollider.center);
        
        return closestPoint;
    }
    
    private void AttackPlayer(GameObject player, Vector3 collisionPoint)
    {
        if (showDebugMessages) Debug.Log($"Monster attacking player: {player.name}");
        
        // Get player's health component
        PreserviaPlayerStat playerHealth = player.GetComponent<PreserviaPlayerStat>();
        
        if (playerHealth != null)
        {
            // Apply damage to player
            playerHealth.TakeDamage(damageAmount);
            
            if (showDebugMessages) Debug.Log($"Player took {damageAmount} damage!");
        }
        else
        {
            // Try to find health component in children
            playerHealth = player.GetComponentInChildren<PreserviaPlayerStat>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
            else
            {
                if (showDebugMessages) Debug.LogWarning("Could not find PreserviaPlayerStat on player!");
            }
        }
        
        // Push player away
        PushPlayer(player);
        
        // Spawn particles at collision point
        SpawnAttackParticles(collisionPoint);
        
        // CHANGED: Play attack sound through AudioHandler
        PlayAttackSound();
        
        // Update attack cooldown
        lastAttackTime = Time.time;
        
        // Optional: Temporarily disable attack collider during animation
        StartCoroutine(DisableAttackTemporarily());
    }
    
    private void SpawnAttackParticles(Vector3 spawnPosition)
    {
        if (attackParticlePrefab == null)
        {
            if (showDebugMessages) Debug.LogWarning("No particle prefab assigned for attack!");
            return;
        }
        
        // Calculate final spawn position with offset
        Vector3 finalPosition = spawnPosition + particleOffset;
        
        // Spawn the particle system
        GameObject particleInstance = Instantiate(attackParticlePrefab, finalPosition, Quaternion.identity);
        
        // Make sure it's active
        particleInstance.SetActive(true);
        
        // Play the particle system
        ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            
            if (showDebugMessages) 
                Debug.Log($"Spawned attack particles at: {finalPosition}");
        }
        else
        {
            Debug.LogWarning("Spawned particle prefab doesn't have ParticleSystem component!");
        }
        
        // Destroy after duration
        Destroy(particleInstance, particleDuration);
    }
    
    // CHANGED: Using AudioHandler instead of local AudioSource
    private void PlayAttackSound()
    {
        if (attackSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(attackSound);
            
            if (showDebugMessages) Debug.Log("Attack sound played through AudioHandler");
        }
        else if (showDebugMessages)
        {
            if (attackSound == null) Debug.LogWarning("No attack sound assigned!");
            if (AudioHandler.Instance == null) Debug.LogWarning("AudioHandler.Instance is null!");
        }
    }
    
    private void PushPlayer(GameObject player)
    {
        // Calculate push direction (away from monster)
        Vector3 pushDirection = player.transform.position - transform.position;
        pushDirection.y = 0.5f; // Add slight upward force
        pushDirection.Normalize();
        
        // Apply force if player has Rigidbody
        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            
            if (showDebugMessages) Debug.Log($"Pushed player with force: {pushForce}");
        }
        else
        {
            // Alternative: Simple positional push
            player.transform.position += pushDirection * 0.5f;
        }
    }
    
    private System.Collections.IEnumerator DisableAttackTemporarily()
    {
        // Disable attack during animation
        canAttack = false;
        
        // Wait for attack animation duration (adjust as needed)
        yield return new WaitForSeconds(attackCooldown * 0.5f);
        
        // Re-enable attack
        canAttack = true;
    }
    
    [ContextMenu("Test Attack (No Player)")]
    public void TestAttackWithoutPlayer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING ATTACK PARTICLES (No Player) ===");
        
        // Spawn particles at attack collider position
        if (attackCollider != null && attackParticlePrefab != null)
        {
            Vector3 spawnPosition = attackCollider.transform.TransformPoint(attackCollider.center);
            SpawnAttackParticles(spawnPosition);
            
            // CHANGED: Test sound through AudioHandler
            PlayAttackSound();
            Debug.Log("Test attack particles spawned");
        }
        else
        {
            Debug.LogError("Cannot test: Attack collider or particle prefab not assigned!");
        }
    }
    
    [ContextMenu("Test Attack With Player")]
    public void TestAttackWithPlayer()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        // Find player in scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && attackCollider != null)
        {
            Debug.Log("=== TESTING MONSTER ATTACK WITH PLAYER ===");
            
            // Get a test collision point
            Vector3 testPoint = attackCollider.transform.TransformPoint(attackCollider.center);
            AttackPlayer(player, testPoint);
        }
        else
        {
            Debug.LogError("No GameObject with 'Player' tag found or no attack collider!");
        }
    }
    
    [ContextMenu("Debug Monster Status")]
    public void DebugMonsterStatus()
    {
        Debug.Log("=== MONSTER STATUS ===");
        Debug.Log($"Can Attack: {canAttack}");
        Debug.Log($"Time Since Last Attack: {Time.time - lastAttackTime:F2}s");
        Debug.Log($"Attack Cooldown: {attackCooldown}s");
        Debug.Log($"Damage Amount: {damageAmount}");
        Debug.Log($"Push Force: {pushForce}");
        Debug.Log($"Attack Collider: {(attackCollider != null ? "Assigned" : "Not assigned")}");
        Debug.Log($"Particle Prefab: {(attackParticlePrefab != null ? attackParticlePrefab.name : "Not assigned")}");
        Debug.Log($"Particle Duration: {particleDuration}s");
        Debug.Log($"Particle Offset: {particleOffset}");
        Debug.Log($"AudioHandler.Instance: {(AudioHandler.Instance != null ? "Ready" : "Missing")}");
        Debug.Log($"Attack Sound: {(attackSound != null ? "Assigned" : "Not assigned")}");
    }
    
    // Draw gizmos to visualize attack collider and particle spawn point in editor
    private void OnDrawGizmosSelected()
    {
        if (attackCollider != null && attackCollider.enabled)
        {
            // Draw attack collider
            Gizmos.color = Color.red;
            Gizmos.matrix = attackCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(attackCollider.center, attackCollider.size);
            
            // Draw particle spawn point
            Gizmos.color = Color.yellow;
            Vector3 spawnPoint = attackCollider.transform.TransformPoint(attackCollider.center + particleOffset);
            Gizmos.DrawSphere(spawnPoint, 0.1f);
            Gizmos.DrawWireSphere(spawnPoint, 0.2f);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (attackCollider != null && attackCollider.enabled)
        {
            // Draw semi-transparent collider
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = attackCollider.transform.localToWorldMatrix;
            Gizmos.DrawCube(attackCollider.center, attackCollider.size);
            
            // Draw particle spawn point indicator
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Vector3 spawnPoint = attackCollider.transform.TransformPoint(attackCollider.center + particleOffset);
            Gizmos.DrawSphere(spawnPoint, 0.05f);
        }
    }
}