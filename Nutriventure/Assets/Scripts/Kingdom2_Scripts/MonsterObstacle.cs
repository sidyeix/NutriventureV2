using UnityEngine;

public class MonsterObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float patrolDistance = 15f;
    public float rotationSpeed = 180f;
    
    [Header("Collision Settings")]
    public int damageAmount = 1;
    public float collisionCooldown = 2f;
    
    [Header("Collider Settings")]
    public float detectionRadius = 0.008f;
    public float blockingRadius = 0.008f;
    
    [Header("Monster Sound Settings")]
    public float monsterSoundRange = 8f;
    public float monsterSoundInterval = 3f;
    
    [Header("Audio")]
    public AudioClip collisionSound;
    public AudioClip monsterSound;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingToEnd = true;
    private Animator animator;
    private float lastCollisionTime;
    private float lastMonsterSoundTime;
    private GameObject player;
    private bool isCollidingWithPlayer = false;
    private CapsuleCollider triggerCollider;
    private CapsuleCollider blockingCollider;
    
    void Start()
    {
        // Store initial position and calculate patrol points
        startPosition = transform.position;
        targetPosition = startPosition + transform.forward * patrolDistance;
        
        // Get animator component
        animator = GetComponent<Animator>();
        
        // Start walking animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found! Make sure your player has the 'Player' tag.");
        }
        else
        {
            Debug.Log($"Found player: {player.name}");
        }
        
        SetupColliders();
        
        // Initialize sound timer
        lastMonsterSoundTime = Time.time;
        
        Debug.Log("MonsterObstacle initialized successfully");
    }
    
    void Update()
    {
        MoveMonster();
        CheckPatrolEnd();
        CheckMonsterSound();
    }
    
    private void SetupColliders()
    {
        // Remove any existing colliders first
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            DestroyImmediate(col);
        }

        // 1. Create TRIGGER collider for detection (small)
        triggerCollider = gameObject.AddComponent<CapsuleCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = detectionRadius;
        triggerCollider.height = 0.06f;
        triggerCollider.center = Vector3.zero;
        triggerCollider.direction = 1; // Y-Axis
        
        // 2. Create PHYSICAL collider for blocking (larger)
        blockingCollider = gameObject.AddComponent<CapsuleCollider>();
        blockingCollider.isTrigger = false;
        blockingCollider.radius = blockingRadius;
        blockingCollider.height = 1.5f;
        blockingCollider.center = Vector3.up * 0.75f;
        blockingCollider.direction = 1; // Y-Axis
        
        Debug.Log($"Monster colliders setup:");
        Debug.Log($"- Trigger: Radius {triggerCollider.radius}, Height {triggerCollider.height}");
        Debug.Log($"- Blocker: Radius {blockingCollider.radius}, Height {blockingCollider.height}");
    }
    
    private void MoveMonster()
    {
        // Calculate movement direction
        Vector3 target = movingToEnd ? targetPosition : startPosition;
        Vector3 direction = (target - transform.position).normalized;
        
        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        
        // Rotate towards movement direction (smooth rotation)
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private void CheckPatrolEnd()
    {
        // Check if reached target position
        float distanceToTarget = Vector3.Distance(transform.position, movingToEnd ? targetPosition : startPosition);
        
        if (distanceToTarget < 0.1f)
        {
            // Switch direction
            movingToEnd = !movingToEnd;
            Debug.Log($"Monster changing direction. Now moving to: {(movingToEnd ? "Target" : "Start")}");
        }
    }
    
    private void CheckMonsterSound()
    {
        // Check if it's time to play monster sound and player is nearby
        if (player != null && monsterSound != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            
            if (distanceToPlayer <= monsterSoundRange && Time.time >= lastMonsterSoundTime + monsterSoundInterval)
            {
                PlayMonsterSound();
                lastMonsterSoundTime = Time.time;
            }
        }
    }
    
    private void PlayMonsterSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(monsterSound);
            Debug.Log("Monster sound played");
        }
    }
    
    // TRIGGER for detection only
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
            HandlePlayerCollision(other.gameObject);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
            HandlePlayerCollision(other.gameObject);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCollidingWithPlayer = false;
            Debug.Log("Player exited monster trigger area");
        }
    }
    
    private void HandlePlayerCollision(GameObject playerObject)
    {
        // Check if collided with player and cooldown has passed
        if (playerObject.CompareTag("Player") && Time.time >= lastCollisionTime + collisionCooldown)
        {
            lastCollisionTime = Time.time;
            
            Debug.Log("Player collided with monster - applying damage!");
            
            // Apply damage to player
            ApplyDamageToPlayer();
            
            // Play collision sound
            PlayCollisionSound();
            
            // Trigger vibration
            TriggerVibration();
            
            // Trigger visual effects via Game Manager
            TriggerVisualEffects();
        }
    }
    
    private void ApplyDamageToPlayer()
    {
        // Try to find player health component
        SugariaPlayerStat playerHealth = FindAnyObjectByType<SugariaPlayerStat>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            Debug.Log($"Player took {damageAmount} damage!");
        }
        else
        {
            Debug.LogWarning("SugariaPlayerStat component not found!");
            
            // Fallback: try to find any player health component
            SugariaPlayerStat fallbackHealth = FindAnyObjectByType<SugariaPlayerStat>();
            if (fallbackHealth != null)
            {
                fallbackHealth.TakeDamage(damageAmount);
                Debug.Log($"Player took {damageAmount} damage (fallback method)!");
            }
        }
    }
    
    private void PlayCollisionSound()
    {
        if (collisionSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(collisionSound);
            Debug.Log("Collision sound played");
        }
    }
    
    private void TriggerVibration()
    {
        // Mobile vibration
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            Debug.Log("Vibration triggered on mobile device");
            #endif
        }
        #if UNITY_EDITOR
        else
        {
            Debug.Log("Vibration simulated in editor");
        }
        #endif
    }
    
    private void TriggerVisualEffects()
    {
        if (SugariaVFX.Instance != null)
        {
            SugariaVFX.Instance.TriggerDamageEffects();
            Debug.Log("Visual effects triggered");
        }
        else
        {
            Debug.LogWarning("VisualEffectsManager instance not found! Make sure VisualEffectsManager is in the scene.");
        }
    }
    
    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        // Draw patrol path
        Gizmos.color = Color.red;
        
        Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
        Vector3 currentEnd = Application.isPlaying ? targetPosition : transform.position + transform.forward * patrolDistance;
        
        Gizmos.DrawLine(currentStart, currentEnd);
        Gizmos.DrawWireSphere(currentStart, 0.3f);
        Gizmos.DrawWireSphere(currentEnd, 0.3f);
        
        // Draw movement direction indicator
        Gizmos.color = Color.yellow;
        Vector3 direction = (currentEnd - currentStart).normalized;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // Draw monster sound range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, monsterSoundRange);
        
        // Draw trigger collider (green - small)
        if (triggerCollider != null)
        {
            Gizmos.color = Color.green;
            Vector3 triggerCenter = transform.TransformPoint(triggerCollider.center);
            Gizmos.DrawWireSphere(triggerCenter, triggerCollider.radius);
        }
        
        // Draw blocking collider (red - larger)
        if (blockingCollider != null)
        {
            Gizmos.color = Color.red;
            Vector3 blockCenter = transform.TransformPoint(blockingCollider.center);
            Gizmos.DrawWireSphere(blockCenter, blockingCollider.radius);
        }
    }
    
    // Public methods for external control
    public void SetPatrolDistance(float newDistance)
    {
        patrolDistance = newDistance;
        targetPosition = startPosition + transform.forward * patrolDistance;
        Debug.Log($"Monster patrol distance set to: {newDistance}");
    }
    
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        Debug.Log($"Monster move speed set to: {newSpeed}");
    }
    
    public void StopPatrol()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
        enabled = false;
        Debug.Log("Monster patrol stopped");
    }
    
    public void StartPatrol()
    {
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
        enabled = true;
        Debug.Log("Monster patrol started");
    }
    
    public void SetDamage(int newDamage)
    {
        damageAmount = newDamage;
        Debug.Log($"Monster damage set to: {newDamage}");
    }
    
    // Method to manually trigger attack (for external events)
    public void ManualAttack()
    {
        if (player != null)
        {
            HandlePlayerCollision(player);
        }
    }
    
    // Get monster state information
    public bool IsPatrolling()
    {
        return enabled;
    }
    
    public bool IsCollidingWithPlayer()
    {
        return isCollidingWithPlayer;
    }
    
    public float GetTimeUntilNextAttack()
    {
        float timeSinceLastAttack = Time.time - lastCollisionTime;
        return Mathf.Max(0f, collisionCooldown - timeSinceLastAttack);
    }
    
    // Clean up when destroyed
    private void OnDestroy()
    {
        Debug.Log("Monster destroyed");
    }
}