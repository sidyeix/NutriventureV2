using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterAI : MonoBehaviour
{
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

    [Header("Stealing Animation - Separate Animator")]
    [SerializeField] private Animator stealingAnimator;
    [SerializeField] private string stealingParam = "isStealing";

    [Header("References")]
    [SerializeField] private Transform monsterBody;
    [SerializeField] private GameObject sleepingEffect;
    [SerializeField] private GameObject warningEffect;
    [SerializeField] private Collider attackTriggerCollider;
    [SerializeField] private SphereCollider detectionCollider;

    [Header("Damage Panel")]
    [SerializeField] private GameObject damagePanel;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkingSound;
    [SerializeField] private AudioClip idleSound;
    [SerializeField] private AudioClip attackingSound;
    [SerializeField] private AudioClip playerEnterSound;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip playerHurtSound;
    [SerializeField] private AudioClip stealingSound;

    [Header("Timing")]
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float attackAnimationDelay = 0.5f;

    private Transform playerTransform;
    private Vector3 centerPosition;
    private Animator monsterAnimator;
    private bool isPlayerInDetectionRange = false;
    private bool isPlayerInCenterRange = true;
    private bool isAttacking = false;
    private bool canAttack = true;
    private bool isReturningToCenter = false;
    private bool isWarningPhase = false;
    private Coroutine detectionCoroutine;
    private Coroutine warningCoroutine;
    private Coroutine attackCoroutine;
    private GoGrowGlowGameManager gameManager;
    private AudioClip currentLoopAudio;
    private float detectionRangeSqr;
    private float centerRangeSqr;
    private float stoppingDistanceSqr;
    private float attackTriggerDistanceSqr;

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
#if UNITY_EDITOR
            Debug.Log($"MonsterAI: Found player at position {playerTransform.position}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("MonsterAI: No player found with tag 'Player'!");
#endif
        }

        // Store center position
        centerPosition = transform.position;

        // Cache squared range values for sqrMagnitude comparisons
        detectionRangeSqr = detectionRange * detectionRange;
        centerRangeSqr = centerRange * centerRange;
        stoppingDistanceSqr = stoppingDistance * stoppingDistance;
        attackTriggerDistanceSqr = attackTriggerDistance * attackTriggerDistance;

        // Cache references
        gameManager = GoGrowGlowGameManager.Instance;
        if (gameManager == null)
        {
#if UNITY_EDITOR
            Debug.LogError("MonsterAI: GoGrowGlowGameManager.Instance is null!");
#endif
        }

        // Initialize audio source with fixed volume
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Ensure audio source has proper settings
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Initialize damage panel
        if (damagePanel != null)
        {
            damagePanel.SetActive(false);
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("MonsterAI: Damage panel is NOT assigned in the Inspector!");
#endif
        }

        // Initialize state
        SetIdleState(true);

        // Start detection
        if (playerTransform != null)
        {
            detectionCoroutine = StartCoroutine(DetectionRoutine());
#if UNITY_EDITOR
            Debug.Log("MonsterAI: Started detection routine");
#endif
        }

        // Initialize effects - Sleeping effect should be enabled by default
        if (sleepingEffect != null)
            sleepingEffect.SetActive(true);
        if (warningEffect != null)
            warningEffect.SetActive(false);
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = false;

        // Debug warning if stealing animator is not assigned
        if (stealingAnimator == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("MonsterAI: Stealing Animator is not assigned in the Inspector! Stealing animations won't play.");
#endif
        }

        // Initialize detection collider
        if (detectionCollider != null)
        {
            detectionCollider.radius = detectionRange;
        }
    }

    private IEnumerator DetectionRoutine()
    {
        while (true)
        {
            yield return CoroutineYieldCache.WaitForSeconds(checkInterval);

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

#if UNITY_EDITOR
        Debug.Log($"MonsterAI: Player entered detection range at distance: {Vector3.Distance(transform.position, playerTransform.position)}");
#endif
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
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Starting warning phase");
#endif
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

        yield return CoroutineYieldCache.WaitForSeconds(warningDuration);

#if UNITY_EDITOR
        Debug.Log("MonsterAI: Warning phase ended, starting movement");
#endif
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
        if (isPlayerInDetectionRange && !isAttacking && !isReturningToCenter && playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= detectionRange)
            {
                SetMovingState(true);
#if UNITY_EDITOR
                Debug.Log("MonsterAI: Started moving towards player");
#endif
            }
        }
    }

    private void PlayerExitedDetectionRange()
    {
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Player exited detection range");
#endif
        isPlayerInDetectionRange = false;

        // If player is still in center range, stop chasing but don't return yet
        if (isPlayerInCenterRange)
        {
            SetMovingState(false);
            SetIdleState(true);

            // Only re-enable sleeping effect if monster is at center and idle
            // This will be handled in ReachedCenter() method
        }
        else
        {
            // Player left both detection AND center range
            PlayerExitedCenterRange();
        }
    }

    private void PlayerExitedCenterRange()
    {
        if (isAttacking || isWarningPhase) return;

#if UNITY_EDITOR
        Debug.Log("MonsterAI: Player exited center range, returning to center");
#endif
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
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Reached center position");
#endif
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
        if (playerTransform == null || gameManager == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        float distToPlayerSqr = toPlayer.x * toPlayer.x + toPlayer.y * toPlayer.y + toPlayer.z * toPlayer.z;
        Vector3 playerToCenter = playerTransform.position - centerPosition;
        bool playerInCenterRange = (playerToCenter.x * playerToCenter.x + playerToCenter.y * playerToCenter.y + playerToCenter.z * playerToCenter.z) <= centerRangeSqr;

        // If moving towards player and not in warning phase or returning to center
        if (monsterAnimator != null && monsterAnimator.GetBool(movingParam) && !isReturningToCenter && !isWarningPhase && isPlayerInDetectionRange)
        {
            // Face and move towards player
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Check if player is still in center range
            if (playerInCenterRange)
            {
                // Check distance for attack
                if (distToPlayerSqr <= attackTriggerDistanceSqr && canAttack && !isAttacking)
                {
#if UNITY_EDITOR
                    Debug.Log($"MonsterAI: Player in attack range! SqrDist: {distToPlayerSqr}");
#endif
                    // Stop moving and attack
                    SetMovingState(false);
                    StartAttack();
                }
                else if (distToPlayerSqr > attackTriggerDistanceSqr)
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

            Vector3 toCenter = centerPosition - transform.position;
            float distToCenterSqr = toCenter.x * toCenter.x + toCenter.y * toCenter.y + toCenter.z * toCenter.z;

            if (distToCenterSqr > stoppingDistanceSqr)
            {
                transform.position += directionToCenter * movementSpeed * Time.deltaTime;
            }
            else
            {
                // Reached center
                ReachedCenter();
            }
        }
        // If idle but player is in detection range and not warning phase
        else if (monsterAnimator != null && monsterAnimator.GetBool(idleParam) &&
                 isPlayerInDetectionRange && !isWarningPhase && !isReturningToCenter && playerInCenterRange)
        {
            // Check if we should start moving towards player
            if (distToPlayerSqr > attackTriggerDistanceSqr && distToPlayerSqr <= detectionRangeSqr)
            {
#if UNITY_EDITOR
                Debug.Log("MonsterAI: Idle but player in range, starting movement");
#endif
                SetMovingState(true);
            }
        }

        // Additional check: If monster is at center and idle, ensure sleeping effect is enabled
        CheckAndUpdateSleepingEffect();
    }

    private void CheckAndUpdateSleepingEffect()
    {
        if (sleepingEffect == null) return;

        Vector3 toCenterVec = centerPosition - transform.position;
        bool isAtCenter = (toCenterVec.x * toCenterVec.x + toCenterVec.y * toCenterVec.y + toCenterVec.z * toCenterVec.z) <= stoppingDistanceSqr;
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
        }
    }

    private void StartAttack()
    {
        if (!canAttack || isAttacking || isReturningToCenter || isWarningPhase)
        {
#if UNITY_EDITOR
            Debug.Log($"MonsterAI: Cannot attack - canAttack={canAttack}, isAttacking={isAttacking}, isReturningToCenter={isReturningToCenter}, isWarningPhase={isWarningPhase}");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log("MonsterAI: Starting attack sequence");
#endif
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
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Attack sequence started");
#endif
        // Wait for attack animation to reach hitting point
        yield return CoroutineYieldCache.WaitForSeconds(attackAnimationDelay);

        // Activate damage panel
        if (damagePanel != null)
        {
            damagePanel.SetActive(true);
#if UNITY_EDITOR
            Debug.Log("MonsterAI: DAMAGE PANEL ACTIVATED!");
#endif

            // Auto-hide after 1 second (damage panel animation duration)
            StartCoroutine(HideDamagePanelAfterDelay());
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("MonsterAI: Damage panel is null! Did you assign it in the Inspector?");
#endif
        }

        // Apply damage to player
        ApplyDamageToPlayer();

        // Enable attack collider for knockback
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = true;

        // Keep collider enabled briefly for knockback
        yield return CoroutineYieldCache.WaitForSeconds(0.2f);

        // Disable attack collider
        if (attackTriggerCollider != null)
            attackTriggerCollider.enabled = false;

        // Wait for attack animation to finish
        yield return CoroutineYieldCache.WaitForSeconds(0.3f);

#if UNITY_EDITOR
        Debug.Log("MonsterAI: Attack sequence finished, starting cooldown");
#endif
        // After attack, start cooldown
        SetAttackingState(false);
        isAttacking = false;
        StartCoroutine(AttackCooldownRoutine());
    }

    private IEnumerator HideDamagePanelAfterDelay()
    {
        yield return CoroutineYieldCache.WaitForSeconds(1f);

        if (damagePanel != null)
        {
            damagePanel.SetActive(false);
#if UNITY_EDITOR
            Debug.Log("MonsterAI: Damage panel deactivated.");
#endif
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Attack cooldown started");
#endif
        canAttack = false;

        // Set stealing animation to TRUE during cooldown (using separate animator)
        SetStealingState(true);

        // Go to idle during cooldown
        SetIdleState(true);

        yield return CoroutineYieldCache.WaitForSeconds(attackCooldown);

        // Set stealing animation to FALSE after cooldown
        SetStealingState(false);

        canAttack = true;
#if UNITY_EDITOR
        Debug.Log("MonsterAI: Attack cooldown finished");
#endif

        // After cooldown, check what to do next
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            bool playerInCenterRange = Vector3.Distance(playerTransform.position, centerPosition) <= centerRange;
            bool playerInDetectionRange = distanceToPlayer <= detectionRange;

#if UNITY_EDITOR
            Debug.Log($"MonsterAI: After cooldown - Distance to player: {distanceToPlayer}, Player in center: {playerInCenterRange}, Player in detection: {playerInDetectionRange}");
#endif

            if (distanceToPlayer <= attackTriggerDistance && playerInCenterRange)
            {
                // Player is still in attack range, attack again
#if UNITY_EDITOR
                Debug.Log("MonsterAI: Player still in attack range, attacking again");
#endif
                StartAttack();
            }
            else if (playerInDetectionRange && playerInCenterRange)
            {
                // Player is in detection range but not attack range, move towards them
#if UNITY_EDITOR
                Debug.Log("MonsterAI: Player in detection range, moving towards player");
#endif
                SetMovingState(true);
            }
            else
            {
                // Player left range, start returning to center
#if UNITY_EDITOR
                Debug.Log("MonsterAI: Player left range, returning to center");
#endif
                PlayerExitedDetectionRange();
                PlayerExitedCenterRange();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isAttacking)
        {
#if UNITY_EDITOR
            Debug.Log("MonsterAI: Attack trigger collider hit player");
#endif
            ApplyKnockbackToPlayer();
        }
    }

    private void ApplyDamageToPlayer()
    {
        if (gameManager == null)
        {
#if UNITY_EDITOR
            Debug.LogError("MonsterAI: GoGrowGlowGameManager.Instance is null!");
#endif
            return;
        }

        gameManager.RemoveEnergy(attackDamage);
#if UNITY_EDITOR
        Debug.Log($"MonsterAI: Applied {attackDamage} damage to player's energy!");
#endif

        // Play player hurt sound at player position
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
#if UNITY_EDITOR
            Debug.Log("MonsterAI: Applied knockback to player!");
#endif
        }
    }

    private void PlayOneShotSound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.PlayOneShot(clip);
    }

    private void PlayLoopingSound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;

        if (currentLoopAudio != clip || !audioSource.isPlaying)
        {
            currentLoopAudio = clip;
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopCurrentSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            currentLoopAudio = null;
        }
    }

    private void SetIdleState(bool state)
    {
        if (monsterAnimator != null)
        {
            monsterAnimator.SetBool(idleParam, state);
            monsterAnimator.SetBool(movingParam, !state);
            monsterAnimator.SetBool(attackingParam, false);

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

            if (state && sleepingEffect != null && sleepingEffect.activeSelf)
            {
                sleepingEffect.SetActive(false);
            }

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
            monsterAnimator.SetBool(movingParam, false);
            monsterAnimator.SetBool(idleParam, false);

            if (state && sleepingEffect != null && sleepingEffect.activeSelf)
            {
                sleepingEffect.SetActive(false);
            }

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

    private void SetStealingState(bool state)
    {
        if (stealingAnimator != null && !string.IsNullOrEmpty(stealingParam))
        {
            stealingAnimator.SetBool(stealingParam, state);

            if (state && stealingSound != null)
            {
                PlayOneShotSound(stealingSound);
            }

#if UNITY_EDITOR
            Debug.Log($"MonsterAI: Stealing animation set to: {state}");
#endif
        }
        else if (state)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"MonsterAI: Cannot set stealing animation: Animator={(stealingAnimator != null)}, Param={stealingParam}");
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackTriggerDistance);

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
