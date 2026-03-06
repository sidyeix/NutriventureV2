using UnityEngine;
using VolumetricLines;
using System.Collections;

[RequireComponent(typeof(VolumetricLineBehavior))]
public class VolumetricLineCollisionController : MonoBehaviour
{
    [Header("Collision Settings")]
    public string pushableTag = "Pushable";
    public LayerMask pushableCollisionLayers = -1;
    public float collisionThickness = 0.1f;

    [Header("Damage Settings")]
    public bool enableDamage = true;
    public float damageAmount = 1f;
    public bool respawnPlayer = true;
    public bool reduceEnergyInstead = false;
    public float energyReductionAmount = 20f;

    [Header("Damage Panel Settings")]
    [SerializeField] private float panelDisplayTime = 1f;
    [SerializeField] private bool useCustomDisplayTime = false;
    [SerializeField] private GameObject damagePanel;

    [Header("Knockback Settings")]
    public bool applyKnockback = true;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Visual Settings")]
    public Color normalColor = Color.green;
    public Color blockedColor = Color.red;
    public float colorChangeSpeed = 5f;

    [Header("Line Effects")]
    public float blockedLineWidthMultiplier = 1.2f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;

    [Header("Damage Visual Feedback")]
    public GameObject damageEffect;
    public AudioClip damageSound;

    [Header("Collider Settings")]
    public bool useCapsuleCollider = true;
    public float colliderRadius = 0.1f;

    private VolumetricLineBehavior lineBehavior;
    private CapsuleCollider damageCollider;

    private Vector3 originalStartPos;
    private Vector3 originalEndPos;
    private float originalLineWidth;

    private bool isBlocked;
    private Vector3 hitPoint;
    private float pulseTimer;
    private Coroutine panelCoroutine;
    private float currentBeamLength;
    private Vector3 currentBeamStart; // Track current beam start position
    private Vector3 currentBeamEnd;   // Track current beam end position

    // Performance: pre-allocated array for SphereCastNonAlloc (avoids GC every frame)
    private RaycastHit[] _hitBuffer = new RaycastHit[16];

    void Start()
    {
        lineBehavior = GetComponent<VolumetricLineBehavior>();

        originalStartPos = lineBehavior.StartPos;
        originalEndPos = lineBehavior.EndPos;
        originalLineWidth = lineBehavior.LineWidth;

        lineBehavior.LineColor = normalColor;

        // Create damage collider for players
        CreateDamageCollider();

        // Hide panel at start if assigned
        if (damagePanel != null)
        {
            damagePanel.SetActive(false);
        }
    }

    void Update()
    {
        CheckCollisionAndMoveStart();
        UpdateLineVisuals();
        UpdateDamageCollider();
        UpdateCurrentBeamPositions(); // Update current beam positions
    }

    void CreateDamageCollider()
    {
        if (!enableDamage) return;

        // Create a capsule collider for player damage detection
        damageCollider = gameObject.AddComponent<CapsuleCollider>();
        damageCollider.isTrigger = true; // IMPORTANT: Make it a trigger for player detection
        damageCollider.radius = colliderRadius;
        damageCollider.height = 1f; // Will be updated dynamically
        damageCollider.direction = 2; // Z-axis aligned initially

        // Set the collider to NOT interact with pushable objects
        // We'll handle pushable objects separately with SphereCast
        damageCollider.enabled = true;

        // Add Rigidbody for proper trigger detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void UpdateDamageCollider()
    {
        if (!enableDamage || damageCollider == null) return;

        // Get current beam positions
        Vector3 currentStart = transform.TransformPoint(lineBehavior.StartPos);
        Vector3 currentEnd = transform.TransformPoint(lineBehavior.EndPos);

        // Calculate current beam length
        currentBeamLength = Vector3.Distance(currentStart, currentEnd);

        if (currentBeamLength < 0.01f)
        {
            // Beam is too short, disable collider
            damageCollider.enabled = false;
            return;
        }

        damageCollider.enabled = true;

        // Calculate center point of the beam
        Vector3 beamCenter = (currentStart + currentEnd) * 0.5f;

        // Update capsule collider to match the beam
        damageCollider.center = transform.InverseTransformPoint(beamCenter);
        damageCollider.height = currentBeamLength;

        // Calculate beam direction in local space
        Vector3 beamDirection = (currentEnd - currentStart).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(beamDirection);

        // Determine capsule direction based on the largest component
        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.y) &&
            Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
        {
            damageCollider.direction = 0; // X-axis
        }
        else if (Mathf.Abs(localDirection.y) > Mathf.Abs(localDirection.z))
        {
            damageCollider.direction = 1; // Y-axis
        }
        else
        {
            damageCollider.direction = 2; // Z-axis
        }
    }

    void UpdateCurrentBeamPositions()
    {
        // Store current beam positions for damage checking
        currentBeamStart = transform.TransformPoint(lineBehavior.StartPos);
        currentBeamEnd = transform.TransformPoint(lineBehavior.EndPos);
    }

    void CheckCollisionAndMoveStart()
    {
        Vector3 worldStart = transform.TransformPoint(originalStartPos);
        Vector3 worldEnd = transform.TransformPoint(originalEndPos);

        Vector3 direction = (worldEnd - worldStart).normalized;
        float maxDistance = Vector3.Distance(worldStart, worldEnd);

        // Use SphereCastNonAlloc to avoid GC allocation every frame
        int hitCount = Physics.SphereCastNonAlloc(
            worldStart,
            collisionThickness,
            direction,
            _hitBuffer,
            maxDistance,
            pushableCollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit closestHit = default;
        float closestDistance = float.MaxValue;
        bool foundPushable = false;

        for (int i = 0; i < hitCount; i++)
        {            RaycastHit hit = _hitBuffer[i];            // Check if it's a pushable object (not the player)
            if (!hit.collider.CompareTag(pushableTag) || hit.collider.CompareTag("Player"))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundPushable = true;
            }
        }

        if (foundPushable)
        {
            isBlocked = true;
            hitPoint = closestHit.point;

            // Offset so the volumetric glow doesn't clip the object
            float offset = collisionThickness * 0.5f;
            Vector3 adjustedWorldStart = closestHit.point + direction * offset;
            Vector3 localStart = transform.InverseTransformPoint(adjustedWorldStart);

            // Move START forward, keep END fixed
            lineBehavior.StartPos = localStart;
            lineBehavior.EndPos = originalEndPos;
        }
        else
        {
            isBlocked = false;
            // Restore original positions
            lineBehavior.StartPos = originalStartPos;
            lineBehavior.EndPos = originalEndPos;
        }
    }

    void UpdateLineVisuals()
    {
        if (isBlocked)
            pulseTimer += Time.deltaTime * pulseSpeed;
        else
            pulseTimer = 0f;

        float pulse = isBlocked
            ? 1f + Mathf.Sin(pulseTimer) * pulseAmount
            : 1f;

        Color targetColor = isBlocked ? blockedColor : normalColor;
        lineBehavior.LineColor = Color.Lerp(
            lineBehavior.LineColor,
            targetColor,
            Time.deltaTime * colorChangeSpeed
        );

        float targetWidth = isBlocked
            ? originalLineWidth * blockedLineWidthMultiplier * pulse
            : originalLineWidth;

        lineBehavior.LineWidth = Mathf.Lerp(
            lineBehavior.LineWidth,
            targetWidth,
            Time.deltaTime * colorChangeSpeed
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableDamage) return;

        // Only apply damage to players
        if (other.CompareTag("Player"))
        {
#if UNITY_EDITOR
            Debug.Log("=== PLAYER TOUCHED LASER BEAM ===");
#endif

            // Check if the player is touching the ACTIVE portion of the beam
            if (IsPlayerTouchingActiveBeam(other.transform.position))
            {
                ApplyDamage(other.transform);
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log("Player is touching blocked portion of beam, no damage");
            }
#endif
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enableDamage) return;

        // Player collision (backup method)
        if (collision.gameObject.CompareTag("Player"))
        {
#if UNITY_EDITOR
            Debug.Log("=== PLAYER COLLIDED WITH LASER BEAM ===");
#endif

            // Check if the player is touching the ACTIVE portion of the beam
            if (IsPlayerTouchingActiveBeam(collision.transform.position))
            {
                ApplyDamage(collision.transform);
            }
#if UNITY_EDITOR
            else
            {
                Debug.Log("Player is touching blocked portion of beam, no damage");
            }
#endif
        }
    }

    // NEW METHOD: Check if player is touching the active (unblocked) portion of the beam
    private bool IsPlayerTouchingActiveBeam(Vector3 playerPosition)
    {
        // Get the current beam positions (already updated in UpdateCurrentBeamPositions)
        Vector3 beamStart = currentBeamStart;
        Vector3 beamEnd = currentBeamEnd;

        // Calculate the closest point on the CURRENT beam to the player
        Vector3 closestPoint = GetClosestPointOnLineSegment(beamStart, beamEnd, playerPosition);

        // Calculate distance from player to the beam
        float distanceToBeam = Vector3.Distance(playerPosition, closestPoint);

        // Player is "touching" the beam if within the collision thickness
        if (distanceToBeam > collisionThickness * 2f)
        {
            return false;
        }

        // If beam is NOT blocked, all parts are active
        if (!isBlocked)
        {
            return true;
        }

        // If beam IS blocked, check if player is BEHIND the obstruction
        float distanceToObstruction = Vector3.Distance(hitPoint, beamStart);
        float distanceFromStartToPlayer = Vector3.Distance(beamStart, closestPoint);

        // Player is in active portion if they're BEYOND the obstruction point
        return distanceFromStartToPlayer > distanceToObstruction - collisionThickness;
    }

    private void ApplyDamage(Transform playerTransform = null)
    {
        // Skip if beam is too short (effectively blocked)
        if (currentBeamLength < 0.3f)
        {
            return;
        }

        // 1. Show damage panel if assigned
        if (damagePanel != null)
        {
            if (panelCoroutine != null)
                StopCoroutine(panelCoroutine);
            panelCoroutine = StartCoroutine(ShowDamagePanel());
        }

        // 2. Check if GameManager exists
        if (GoGrowGlowGameManager.Instance == null)
        {
            return;
        }

        if (GoGrowGlowGameManager.Instance.IsRespawning())
        {
            return;
        }

        // 3. Calculate knockback direction (away from the beam)
        Vector3 knockbackDirection = Vector3.zero;
        if (playerTransform != null && applyKnockback)
        {
            // Get the closest point on the beam to the player
            Vector3 beamStart = currentBeamStart;
            Vector3 beamEnd = currentBeamEnd;
            Vector3 closestPointOnBeam = GetClosestPointOnLineSegment(
                beamStart, beamEnd, playerTransform.position);

            knockbackDirection = (playerTransform.position - closestPointOnBeam).normalized;
            knockbackDirection.y = 0.2f; // Add slight upward force
            knockbackDirection.Normalize();
        }

        // 4. Apply knockback if enabled
        if (applyKnockback && knockbackForce > 0 && playerTransform != null)
        {
            GoGrowGlowGameManager.Instance.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }

        // 5. Apply damage through GameManager
        if (reduceEnergyInstead)
        {
            // Reduce energy instead of life
            GoGrowGlowGameManager.Instance.RemoveEnergy(energyReductionAmount);
        }
        else
        {
            // Apply life damage
            if (respawnPlayer && damageAmount >= 1f)
            {
                GoGrowGlowGameManager.Instance.LoseLife();
            }
            else
            {
                GoGrowGlowGameManager.Instance.LoseLifeAmount(damageAmount, false);
            }
        }

        // 6. Visual effect at the point of contact
        if (damageEffect != null && playerTransform != null)
        {
            GameObject effect = Instantiate(damageEffect, playerTransform.position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }

        // 7. Sound
        if (damageSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);
        }
    }

    private Vector3 GetClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;
        if (lineLength < 0.001f) return lineStart; // Avoid division by zero

        Vector3 normalizedDirection = lineDirection.normalized;

        float projection = Vector3.Dot(point - lineStart, normalizedDirection);
        projection = Mathf.Clamp(projection, 0f, lineLength);

        return lineStart + normalizedDirection * projection;
    }

    private IEnumerator ShowDamagePanel()
    {
        // Show the panel
        damagePanel.SetActive(true);

        // Wait for display time
        float displayTime = useCustomDisplayTime ? panelDisplayTime : 1f;
        yield return CoroutineYieldCache.WaitForSeconds(displayTime);

        // Hide the panel
        damagePanel.SetActive(false);
        panelCoroutine = null;
    }

    // Optional public helpers
    public bool IsBlocked() => isBlocked;
    public Vector3 GetHitPoint() => hitPoint;
    public bool IsDamageEnabled() => enableDamage;

    // Method to enable/disable damage at runtime
    public void SetDamageEnabled(bool enabled)
    {
        enableDamage = enabled;
        if (damageCollider != null) damageCollider.enabled = enabled;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || damageCollider == null || !damageCollider.enabled) return;

        // Draw the capsule collider for visualization
        Gizmos.color = Color.yellow;
        Vector3 center = transform.TransformPoint(damageCollider.center);

        // Draw wireframe capsule
        float height = damageCollider.height;
        float radius = damageCollider.radius;

        // This is a simplified visualization - drawing a proper capsule is complex
        Gizmos.DrawWireSphere(center, radius);

        // Draw line showing the beam direction
        Vector3 beamStart = transform.TransformPoint(lineBehavior.StartPos);
        Vector3 beamEnd = transform.TransformPoint(lineBehavior.EndPos);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(beamStart, beamEnd);

        // Draw obstruction point if blocked
        if (isBlocked)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(hitPoint, 0.1f);
            Gizmos.DrawLine(hitPoint, beamStart);
        }
    }
}