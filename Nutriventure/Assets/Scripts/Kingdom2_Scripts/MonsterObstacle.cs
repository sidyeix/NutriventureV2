using UnityEngine;
using System.Collections;

public class MonsterObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float patrolDistance = 15f;
    public float rotationSpeed = 180f;
    
    [Header("Hunting Settings")]
    public float huntSpeedMultiplier = 1.5f;
    public float huntRotationSpeed = 360f;
    public float returnToPatrolDelay = 2f;
    public float stuckDetectionTime = 3f;
    
    [Header("Collision Settings")]
    public int damageAmount = 1;
    public float collisionCooldown = 2f;
    
    [Header("Attack Animation Settings")]
    public float attackAnimationDuration = 1.0f;
    public float damageTriggerTime = 0.5f; // When during animation to apply damage
    public bool pauseMovementDuringAttack = true;
    
    [Header("Collider Settings")]
    public float detectionRadius = 0.008f;
    public float blockingRadius = 0.008f;
    
    [Header("Monster Sound Settings")]
    public float monsterSoundRange = 8f;
    public float monsterSoundInterval = 3f;
    
    [Header("Audio")]
    public AudioClip collisionSound;
    public AudioClip monsterSound;
    public AudioClip huntSound;
    public AudioClip attackSound;
    
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
    
    // Hunting state variables
    private bool isHunting = false;
    private float lastPlayerDetectionTime;
    private Vector3 lastKnownPlayerPosition;
    private MonsterState currentState = MonsterState.Patrolling;
    
    // Stuck detection variables
    private Vector3 lastPosition;
    private float lastMovementTime;
    private bool isStuck = false;
    private float stuckTimer = 0f;
    
    // Attack animation variables
    private bool isAttacking = false;
    private Coroutine attackCoroutine;
    private bool damageApplied = false;
    
    // Animation parameters - ONLY IsAttacking is needed
    private readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    
    // Make the enum public to fix the accessibility error
    public enum MonsterState
    {
        Patrolling,
        Hunting,
        ReturningToPatrol,
        Attacking
    }
    
    void Start()
    {
        // Store initial position and calculate patrol points
        startPosition = transform.position;
        targetPosition = startPosition + transform.forward * patrolDistance;
        
        // Get animator component
        animator = GetComponent<Animator>();
        
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
        
        // Initialize stuck detection
        lastPosition = transform.position;
        lastMovementTime = Time.time;
        
        Debug.Log("MonsterObstacle initialized successfully");
    }
    
    void Update()
    {
        // Don't update movement if currently attacking
        if (currentState != MonsterState.Attacking)
        {
            CheckPlayerDetection();
            CheckIfStuck();
            
            switch (currentState)
            {
                case MonsterState.Patrolling:
                    MoveMonster();
                    CheckPatrolEnd();
                    break;
                    
                case MonsterState.Hunting:
                    HuntPlayer();
                    break;
                    
                case MonsterState.ReturningToPatrol:
                    ReturnToPatrol();
                    break;
            }
            
            CheckMonsterSound();
            UpdateMovementDetection();
        }
    }
    
    private void UpdateMovementDetection()
    {
        // Check if monster is actually moving
        if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
        {
            lastMovementTime = Time.time;
            lastPosition = transform.position;
            isStuck = false;
            stuckTimer = 0f; // Reset stuck timer when moving
        }
        else if (currentState == MonsterState.Hunting)
        {
            // Increment stuck timer when not moving while hunting
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= stuckDetectionTime)
            {
                isStuck = true;
                Debug.Log("Monster is stuck! Returning to patrol.");
                ReturnToPatrolState();
            }
        }
    }
    
    private void CheckIfStuck()
    {
        // Additional stuck detection using raycasts for obstacles
        if (currentState == MonsterState.Hunting && !isStuck && player != null)
        {
            // Check for obstacles in front
            RaycastHit hit;
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            
            if (Physics.Raycast(transform.position, directionToPlayer, out hit, 2f))
            {
                if (!hit.collider.CompareTag("Player") && hit.collider != blockingCollider)
                {
                    // There's an obstacle between monster and player
                    Debug.Log($"Obstacle detected: {hit.collider.name}");
                    
                    // Check if we've been stuck for the required time
                    if (stuckTimer >= stuckDetectionTime)
                    {
                        isStuck = true;
                        Debug.Log("Monster blocked by obstacle! Returning to patrol.");
                        ReturnToPatrolState();
                    }
                }
            }
        }
    }
    
    private void CheckPlayerDetection()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Use monsterSoundRange as hunting radius for consistency
        if (distanceToPlayer <= monsterSoundRange)
        {
            // Player detected in hunting radius
            if (currentState != MonsterState.Hunting && !isStuck && currentState != MonsterState.Attacking)
            {
                StartHunting();
            }
            lastPlayerDetectionTime = Time.time;
            lastKnownPlayerPosition = player.transform.position;
        }
        else if (currentState == MonsterState.Hunting)
        {
            // Player left hunting radius - return to patrol immediately
            ReturnToPatrolState();
        }
    }
    
    private void StartHunting()
    {
        currentState = MonsterState.Hunting;
        isHunting = true;
        isStuck = false;
        stuckTimer = 0f; // Reset stuck timer when starting to hunt
        
        // Update animations - only set IsAttacking to false
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, false);
        }
        
        // Play hunt sound
        if (huntSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(huntSound);
        }
        
        Debug.Log("Monster started hunting player!");
    }
    
    private void HuntPlayer()
    {
        if (player == null) return;
        
        // Calculate direction to player
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep movement horizontal
        
        // Use physics-based movement to respect collisions
        Vector3 movement = directionToPlayer * (moveSpeed * huntSpeedMultiplier * Time.deltaTime);
        
        // Simple obstacle avoidance
        if (!CheckForObstacles(directionToPlayer))
        {
            // Move towards player if no immediate obstacles
            transform.position += movement;
        }
        else
        {
            // Try to find alternative path
            Vector3 alternativeDirection = FindAlternativeDirection(directionToPlayer);
            if (alternativeDirection != Vector3.zero)
            {
                transform.position += alternativeDirection * (moveSpeed * huntSpeedMultiplier * Time.deltaTime);
            }
        }
        
        // Rotate towards player quickly
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                huntRotationSpeed * Time.deltaTime
            );
        }
    }
    
    private bool CheckForObstacles(Vector3 direction)
    {
        RaycastHit hit;
        float checkDistance = 1.5f;
        
        // Check for obstacles in the movement direction
        if (Physics.Raycast(transform.position, direction, out hit, checkDistance))
        {
            if (!hit.collider.CompareTag("Player") && hit.collider != blockingCollider)
            {
                return true; // Obstacle detected
            }
        }
        
        return false; // No obstacles
    }
    
    private Vector3 FindAlternativeDirection(Vector3 originalDirection)
    {
        // Try directions to the left and right of the original direction
        Vector3[] testDirections = {
            Quaternion.Euler(0, 30, 0) * originalDirection,
            Quaternion.Euler(0, -30, 0) * originalDirection,
            Quaternion.Euler(0, 45, 0) * originalDirection,
            Quaternion.Euler(0, -45, 0) * originalDirection
        };
        
        foreach (Vector3 testDir in testDirections)
        {
            if (!CheckForObstacles(testDir))
            {
                return testDir.normalized;
            }
        }
        
        return Vector3.zero; // No alternative path found
    }
    
    private void ReturnToPatrolState()
    {
        if (currentState == MonsterState.Hunting || currentState == MonsterState.Attacking)
        {
            currentState = MonsterState.ReturningToPatrol;
            isHunting = false;
            isStuck = false;
            stuckTimer = 0f; // Reset stuck timer
            
            // Update animations - only set IsAttacking to false
            if (animator != null)
            {
                animator.SetBool(IsAttackingHash, false);
            }
            
            Debug.Log("Monster returning to patrol route");
        }
    }
    
    private void ReturnToPatrol()
    {
        // Find the closest patrol point to return to
        float distanceToStart = Vector3.Distance(transform.position, startPosition);
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        Vector3 returnTarget = distanceToStart < distanceToTarget ? startPosition : targetPosition;
        movingToEnd = returnTarget == targetPosition;
        
        Vector3 direction = (returnTarget - transform.position).normalized;
        
        // Move towards patrol point
        transform.position = Vector3.MoveTowards(transform.position, returnTarget, moveSpeed * Time.deltaTime);
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Check if reached patrol route
        if (Vector3.Distance(transform.position, returnTarget) < 0.1f)
        {
            currentState = MonsterState.Patrolling;
            Debug.Log("Monster returned to patrol route");
        }
    }
    
    private void MoveMonster()
    {
        // Calculate movement direction for patrolling
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
            // Don't automatically handle collision in stay to prevent spam
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
            
            Debug.Log("Player collided with monster - starting attack sequence!");
            
            // Trigger attack animation FIRST
            TriggerAttackAnimation();
        }
    }
    
    private void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            // Stop any existing attack coroutine
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
            }
            
            // Start attack sequence
            attackCoroutine = StartCoroutine(AttackSequence());
        }
    }
    
    private IEnumerator AttackSequence()
    {
        // Set attacking state
        currentState = MonsterState.Attacking;
        isAttacking = true;
        damageApplied = false; // Reset damage flag
        
        // Update animator - trigger attack animation FIRST
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, true);
        }
        
        // Play attack sound
        if (attackSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayCharacterSelectionSound(attackSound);
        }
        
        Debug.Log("Monster attacking player - animation started!");
        
        // Wait for the damage trigger time (when attack connects in animation)
        yield return new WaitForSeconds(damageTriggerTime);
        
        // NOW apply damage and effects (at the peak of the attack animation)
        if (!damageApplied)
        {
            ApplyDamageAndEffects();
            damageApplied = true;
        }
        
        // Wait for the remaining animation duration
        yield return new WaitForSeconds(attackAnimationDuration - damageTriggerTime);
        
        // Reset attack state
        isAttacking = false;
        damageApplied = false;
        
        // Return to appropriate state based on player position
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, false);
            
            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= monsterSoundRange)
            {
                // Player still in range - resume hunting
                currentState = MonsterState.Hunting;
                Debug.Log("Attack complete - resuming hunt");
            }
            else
            {
                // Player out of range - return to patrol
                ReturnToPatrolState();
            }
        }
        
        attackCoroutine = null;
    }
    
    private void ApplyDamageAndEffects()
    {
        Debug.Log("Attack connecting - applying damage and effects!");
        
        // Apply damage to player
        ApplyDamageToPlayer();
        
        // Play collision sound
        PlayCollisionSound();
        
        // Trigger vibration
        TriggerVibration();
        
        // Trigger visual effects via Game Manager
        TriggerVisualEffects();
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
        
        // Draw monster sound range (now also used as hunting radius)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, monsterSoundRange);
        
        // Draw hunting state indicator
        Gizmos.color = currentState == MonsterState.Hunting ? Color.red : 
                      currentState == MonsterState.ReturningToPatrol ? Color.yellow : 
                      currentState == MonsterState.Attacking ? Color.magenta : new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, monsterSoundRange * 1.1f);
        
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
        
        // Draw obstacle detection rays when hunting
        if (currentState == MonsterState.Hunting && player != null)
        {
            Gizmos.color = Color.white;
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            Gizmos.DrawRay(transform.position, directionToPlayer * 2f);
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
    
    public void SetDamageTriggerTime(float newTime)
    {
        damageTriggerTime = Mathf.Clamp(newTime, 0.1f, attackAnimationDuration - 0.1f);
        Debug.Log($"Damage trigger time set to: {damageTriggerTime}");
    }
    
    public void StopPatrol()
    {
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, false);
        }
        
        // Stop any attack coroutine
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        enabled = false;
        Debug.Log("Monster patrol stopped");
    }
    
    public void StartPatrol()
    {
        if (animator != null)
        {
            animator.SetBool(IsAttackingHash, false);
        }
        enabled = true;
        currentState = MonsterState.Patrolling;
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
            TriggerAttackAnimation();
        }
    }
    
    // Get monster state information
    public bool IsPatrolling()
    {
        return currentState == MonsterState.Patrolling;
    }
    
    public bool IsHunting()
    {
        return currentState == MonsterState.Hunting;
    }
    
    public bool IsReturningToPatrol()
    {
        return currentState == MonsterState.ReturningToPatrol;
    }
    
    public bool IsAttacking()
    {
        return currentState == MonsterState.Attacking;
    }
    
    public bool IsCollidingWithPlayer()
    {
        return isCollidingWithPlayer;
    }
    
    public bool IsStuck()
    {
        return isStuck;
    }
    
    public float GetTimeUntilNextAttack()
    {
        float timeSinceLastAttack = Time.time - lastCollisionTime;
        return Mathf.Max(0f, collisionCooldown - timeSinceLastAttack);
    }
    
    // Now this works because MonsterState is public
    public MonsterState GetCurrentState()
    {
        return currentState;
    }
    
    // Clean up when destroyed
    private void OnDestroy()
    {
        // Stop any running coroutines
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        
        Debug.Log("Monster destroyed");
    }
}