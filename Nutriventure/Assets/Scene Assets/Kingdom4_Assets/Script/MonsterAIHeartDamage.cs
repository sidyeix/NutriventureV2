using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterAIHeartDamage : MonoBehaviour
{
    private PlayerHealth playerHealth;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float centerRange = 15f;
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float attackTriggerDistance = 1.5f;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;

    [Header("Animation Parameters")]
    [SerializeField] private string idleParam = "isIdle";
    [SerializeField] private string movingParam = "isMoving";
    [SerializeField] private string attackingParam = "isAttacking";
    [SerializeField] private string playerEnterParam = "PlayerEnter";

    [Header("References")]
    [SerializeField] private Transform monsterBody;
    [SerializeField] private GameObject sleepingEffect;
    [SerializeField] private GameObject warningEffect;
    [SerializeField] private Collider attackTriggerCollider;
    [SerializeField] private SphereCollider detectionCollider;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkingSound;
    [SerializeField] private AudioClip idleSound;
    [SerializeField] private AudioClip attackingSound;
    [SerializeField] private AudioClip playerEnterSound;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip playerHurtSound;

    [Header("Timing")]
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float attackAnimationDelay = 0.5f;

    // State
    private Transform playerTransform;
    private Vector3 centerPosition;
    private Animator monsterAnimator; // First animator (for idle/moving/attacking)
    private bool isPlayerInDetectionRange = false;
    private bool isPlayerInCenterRange = true;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isReturningToCenter = false;
    private bool isWarningPhase = false;
    private Coroutine detectionCoroutine;
    private Coroutine warningCoroutine;
    private Coroutine attackCoroutine;

    // Cache
    private GoGrowGlowGameManager gameManager;

    // Audio state - Simplified
    private AudioClip currentLoopAudio;

    private void Start()
    {


        // Get the main animator from TurtleShell
        if (monsterBody != null)
        {
            monsterAnimator = monsterBody.GetComponent<Animator>();
        }
        else
        {
            monsterAnimator = GetComponentInChildren<Animator>();
        }

        // Find player
GameObject player = GameObject.FindGameObjectWithTag("Player");
if (player != null)
{
    playerTransform = player.transform;
    playerHealth = player.GetComponent<PlayerHealth>();

    if (playerHealth == null)
        Debug.LogError("PlayerHealth NOT found on Player!");
}
else
{
    Debug.LogError("Player GameObject with tag 'Player' not found!");
}


        // Store center position
        centerPosition = transform.position;

        // Cache references
        gameManager = GoGrowGlowGameManager.Instance;

        // Initialize audio source with fixed volume
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Ensure audio source has proper settings
        audioSource.loop = false; // We'll control looping manually
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D audio

        // Initialize state
        SetIdleState(true);

        // Start detection
        if (playerTransform != null)
        {
            detectionCoroutine = StartCoroutine(DetectionRoutine());
        }

        // Initialize effects - Sleeping effect should be enabled by default
        if (sleepingEffect != null)
            sleepingEffect.SetActive(true);
        if (warningEffect != null)
            warningEffect.SetActive(false);
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = false;
    }

    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (playerTransform == null) continue;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            float distanceToCenter = Vector3.Distance(transform.position, centerPosition);

            // Check center range
            isPlayerInCenterRange = Vector3.Distance(playerTransform.position, centerPosition) <= centerRange;

            // Check if player entered detection range
            if (distanceToPlayer <= detectionRange && !isPlayerInDetectionRange && !isWarningPhase && !isAttacking)
            {
                PlayerEnteredDetectionRange();
            }
            // Check if player exited detection range
            else if (distanceToPlayer > detectionRange && isPlayerInDetectionRange && !isAttacking && !isWarningPhase)
            {
                PlayerExitedDetectionRange();
            }

            // Check if player exited center range and we're not already returning
            if (!isPlayerInCenterRange && !isReturningToCenter && !isAttacking && !isWarningPhase)
            {
                PlayerExitedCenterRange();
            }

            // If returning to center and reached it
            if (isReturningToCenter && distanceToCenter <= stoppingDistance)
            {
                ReachedCenter();
            }
        }
    }

    private void PlayerEnteredDetectionRange()
    {
        if (isWarningPhase || isAttacking || isReturningToCenter) return;

        isPlayerInDetectionRange = true;
        isWarningPhase = true;

        // Disable sleeping effect when player enters detection range
        if (sleepingEffect != null)
        {
            sleepingEffect.SetActive(false);
        }

        // Trigger PlayerEnter animation parameter
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(playerEnterParam, true);
        }

        // Show warning effect and play warning sound
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(ShowWarningAndMove());
    }

    private IEnumerator ShowWarningAndMove()
    {
        // Show warning effect
        if (warningEffect != null)
        {
            warningEffect.SetActive(true);
        }

        // Play one-shot warning sound
        if (warningSound != null)
        {
            PlayOneShotSound(warningSound);
        }

        yield return new WaitForSeconds(warningDuration);

        // Hide warning effect
        if (warningEffect != null)
        {
            warningEffect.SetActive(false);
        }

        // Reset PlayerEnter parameter
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(playerEnterParam, false);
        }

        isWarningPhase = false;

        // Start moving towards player if still in range
        if (isPlayerInDetectionRange && !isAttacking && !isReturningToCenter)
        {
            SetMovingState(true);
        }
    }

    private void PlayerExitedDetectionRange()
    {
        isPlayerInDetectionRange = false;

        // If player is still in center range, stop chasing but don't return yet
        if (isPlayerInCenterRange)
        {
            SetMovingState(false);
            SetIdleState(true);

            // Only re-enable sleeping effect if monster is at center and idle
            // This will be handled in ReachedCenter() method
        }
    }

    private void PlayerExitedCenterRange()
    {
        if (isAttacking || isWarningPhase) return;

        isReturningToCenter = true;
        SetMovingState(true);

        // Stop attack animation if was attacking
        if (isAttacking)
        {
            SetAttackingState(false);
            isAttacking = false;
        }
    }

    private void ReachedCenter()
    {
        isReturningToCenter = false;
        SetMovingState(false);
        SetIdleState(true);

        // Enable sleeping effect when monster returns to center and is idle
        if (sleepingEffect != null && !isPlayerInDetectionRange)
        {
            sleepingEffect.SetActive(true);
        }

        // Reset detection flag
        isPlayerInDetectionRange = false;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool playerInCenterRange = Vector3.Distance(playerTransform.position, centerPosition) <= centerRange;

        // If moving towards player and not in warning phase
        if (monsterAnimator != null && monsterAnimator.GetBool(movingParam) && !isReturningToCenter && !isWarningPhase)
        {
            // Face and move towards player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            if (playerInCenterRange)
            {
                // If close enough to attack
                if (distanceToPlayer <= attackTriggerDistance && canAttack && !isAttacking)
                {
                    // Stop moving and attack
                    SetMovingState(false);
                    StartAttack();
                }
                else if (distanceToPlayer > attackTriggerDistance)
                {
                    // Keep moving towards player
                    transform.position += directionToPlayer * movementSpeed * Time.deltaTime;
                }
            }
            else
            {
                // Player left center range, start returning
                PlayerExitedCenterRange();
            }
        }
        // If returning to center
        else if (monsterAnimator != null && monsterAnimator.GetBool(movingParam) && isReturningToCenter)
        {
            Vector3 directionToCenter = (centerPosition - transform.position).normalized;
            directionToCenter.y = 0;

            if (directionToCenter != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCenter);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            float distanceToCenter = Vector3.Distance(transform.position, centerPosition);

            if (distanceToCenter > stoppingDistance)
            {
                transform.position += directionToCenter * movementSpeed * Time.deltaTime;
            }
            else
            {
                // Reached center
                ReachedCenter();
            }
        }

        // Additional check: If monster is at center and idle, ensure sleeping effect is enabled
        CheckAndUpdateSleepingEffect();
    }

    // NEW METHOD: Check and update sleeping effect status
    private void CheckAndUpdateSleepingEffect()
    {
        if (sleepingEffect == null) return;

        float distanceToCenter = Vector3.Distance(transform.position, centerPosition);
        bool isAtCenter = distanceToCenter <= stoppingDistance;
        bool isIdle = monsterAnimator != null && monsterAnimator.GetBool(idleParam);

        // Enable sleeping effect if:
        // 1. Monster is at center position
        // 2. Monster is in idle state
        // 3. Player is NOT in detection range
        // 4. Monster is NOT in warning phase
        // 5. Monster is NOT attacking
        // 6. Monster is NOT returning to center
        bool shouldEnableSleeping = isAtCenter &&
                                   isIdle &&
                                   !isPlayerInDetectionRange &&
                                   !isWarningPhase &&
                                   !isAttacking &&
                                   !isReturningToCenter;

        if (sleepingEffect.activeSelf != shouldEnableSleeping)
        {
            sleepingEffect.SetActive(shouldEnableSleeping);
            if (shouldEnableSleeping)
                Debug.Log("Sleeping effect enabled: Monster is at center and idle");
            else
                Debug.Log("Sleeping effect disabled");
        }
    }

    private void StartAttack()
    {
        if (!canAttack || isAttacking || isReturningToCenter || isWarningPhase) return;

        SetAttackingState(true);
        isAttacking = true;

        // Ensure sleeping effect is disabled when attacking
        if (sleepingEffect != null && sleepingEffect.activeSelf)
        {
            sleepingEffect.SetActive(false);
        }

        // Start attack sequence
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        // Wait for attack animation to reach hitting point
        yield return new WaitForSeconds(attackAnimationDelay);

        // Apply damage to player
        ApplyDamageToPlayer();

        // Enable attack collider for knockback
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = true;

        // Keep collider enabled briefly for knockback
        yield return new WaitForSeconds(0.2f);

        // Disable attack collider
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = false;

        // Wait for attack animation to finish
        yield return new WaitForSeconds(0.3f);

        // After attack, start cooldown
        SetAttackingState(false);
        isAttacking = false;
        StartCoroutine(AttackCooldownRoutine());
    }

    private IEnumerator AttackCooldownRoutine()
    {
        canAttack = false;

        // Go to idle during cooldown
        SetIdleState(true);

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;

        // After cooldown, check what to do next
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool playerInCenterRange = Vector3.Distance(playerTransform.position, centerPosition) <= centerRange;

            if (distanceToPlayer <= attackTriggerDistance && playerInCenterRange)
            {
                // Player is still in attack range, attack again
                StartAttack();
            }
            else if (distanceToPlayer <= detectionRange && playerInCenterRange)
            {
                // Player is in detection range but not attack range, move towards them
                SetMovingState(true);
            }
            else
            {
                // Player left range, start returning to center
                PlayerExitedDetectionRange();
                PlayerExitedCenterRange();
            }
        }
    }

    // Trigger for attack collider (only for knockback now)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isAttacking)
        {
            // Only apply knockback here
            ApplyKnockbackToPlayer();
        }
    }

    private void ApplyDamageToPlayer()
{
    if (playerHealth == null) return;

    // Deal 1 heart damage (or change value)
    playerHealth.TakeDamage(1);

    Debug.Log("Monster damaged player's HEART!");

    if (playerHurtSound != null && playerTransform != null)
    {
        AudioSource.PlayClipAtPoint(playerHurtSound, playerTransform.position);
    }
}


    private void ApplyKnockbackToPlayer()
    {
        if (playerTransform != null && gameManager != null)
        {
            Vector3 knockbackDirection = (playerTransform.position - transform.position).normalized;
            knockbackDirection.y = 0.2f;
            knockbackDirection.Normalize();

            gameManager.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
            Debug.Log("Applied knockback to player!");
        }
    }

    // SIMPLIFIED AUDIO MANAGEMENT WITH FIXED VOLUME

    // Play a one-shot sound at fixed volume
    private void PlayOneShotSound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        // Stop any currently playing sound
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.PlayOneShot(clip);
    }

    // Play looping sound at fixed volume
    private void PlayLoopingSound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        // Only change if different clip
        if (currentLoopAudio != clip || !audioSource.isPlaying)
        {
            currentLoopAudio = clip;
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // Stop current sound
    private void StopCurrentSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            currentLoopAudio = null;
        }
    }

    // Animation state setters with SIMPLIFIED audio management
    private void SetIdleState(bool state)
    {
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(idleParam, state);
            monsterAnimator.SetBool(movingParam, !state);
            monsterAnimator.SetBool(attackingParam, false);

            // Audio management - fixed volume
            if (state && idleSound != null)
            {
                PlayLoopingSound(idleSound);
            }
            else if (!state && currentLoopAudio == idleSound)
            {
                StopCurrentSound();
            }
        }
    }

    private void SetMovingState(bool state)
    {
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(movingParam, state);
            monsterAnimator.SetBool(idleParam, !state);
            monsterAnimator.SetBool(attackingParam, false);

            // Ensure sleeping effect is disabled when moving
            if (state && sleepingEffect != null && sleepingEffect.activeSelf)
            {
                sleepingEffect.SetActive(false);
            }

            // Audio management - fixed volume
            if (state && walkingSound != null)
            {
                PlayLoopingSound(walkingSound);
            }
            else if (!state && currentLoopAudio == walkingSound)
            {
                StopCurrentSound();
            }
        }
    }

    private void SetAttackingState(bool state)
    {
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(attackingParam, state);
            monsterAnimator.SetBool(movingParam, !state);
            monsterAnimator.SetBool(idleParam, false);

            // Ensure sleeping effect is disabled when attacking
            if (state && sleepingEffect != null && sleepingEffect.activeSelf)
            {
                sleepingEffect.SetActive(false);
            }

            // Audio management - fixed volume
            if (state && attackingSound != null)
            {
                PlayLoopingSound(attackingSound);
            }
            else if (!state && currentLoopAudio == attackingSound)
            {
                StopCurrentSound();
            }
        }
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack trigger distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackTriggerDistance);

        // Draw center range
        Gizmos.color = Color.blue;
        if (Application.isPlaying)
        {
            Gizmos.DrawWireSphere(centerPosition, centerRange);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, centerPosition);
            Gizmos.DrawWireSphere(centerPosition, 0.5f);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, centerRange);
        }
    }

    private void OnDestroy()
    {
        if (detectionCoroutine != null)
            StopCoroutine(detectionCoroutine);
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);
    }
}