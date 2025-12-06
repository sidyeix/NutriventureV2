using UnityEngine;
using System.Collections;

public class KartCollisionHandler : MonoBehaviour
{
    [Header("Fence Settings")]
    public string fenceTag = "Fence";
    
    [Header("Reset Settings")]
    public Transform[] roadWaypoints; // Waypoints marking the middle of the road
    public float resetDistance = 5f; // Max distance to reset when hitting fence
    public float resetHeight = 0.5f; // Height above ground when resetting
    public float resetSpeed = 10f; // Speed of reset animation
    public bool findNearestWaypoint = true; // Auto-find nearest waypoint
    
    [Header("Damage Settings")]
    public int damagePerCollision = 1;
    public float collisionCooldown = 2f; // Time between allowed collisions
    public float invulnerabilityAfterReset = 1f; // Invulnerability after reset
    
    [Header("Collision Effects")]
    public AudioClip collisionSound;
    public ParticleSystem collisionParticles;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.3f;
    
    [Header("References")]
    public PlayerHealth playerHealth; // Reference to PlayerHealth component
    public KartController kartController; // Reference to KartController
    
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool isResetting = false;
    private float lastCollisionTime = -10f;
    private Rigidbody kartRigidbody;
    private bool isInvulnerable = false;
    private Camera mainCamera;
    private Vector3 cameraOriginalPosition;
    
    void Start()
    {
        kartRigidbody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            cameraOriginalPosition = mainCamera.transform.localPosition;
        }
        
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("PlayerHealth not found! Make sure to assign it or add it to the player.");
            }
        }
        
        // Auto-find KartController if not assigned
        if (kartController == null)
        {
            kartController = GetComponent<KartController>();
        }
        
        // Store initial position as safe
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        
        // Find waypoints in scene if not assigned
        if ((roadWaypoints == null || roadWaypoints.Length == 0) && findNearestWaypoint)
        {
            FindRoadWaypoints();
        }
    }
    
    void FindRoadWaypoints()
    {
        // Find all waypoints in the scene
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        
        if (waypointObjects.Length > 0)
        {
            roadWaypoints = new Transform[waypointObjects.Length];
            for (int i = 0; i < waypointObjects.Length; i++)
            {
                roadWaypoints[i] = waypointObjects[i].transform;
            }
            Debug.Log($"Found {roadWaypoints.Length} waypoints for road reset");
        }
        else
        {
            Debug.LogWarning("No waypoints found with tag 'Waypoint'! Add waypoints to the road.");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        HandleFenceCollision(collision);
    }
    
    void OnCollisionStay(Collision collision)
    {
        // Also handle continuous collision with fences
        if (Time.time - lastCollisionTime > collisionCooldown && !isResetting)
        {
            HandleFenceCollision(collision);
        }
    }
    
    void HandleFenceCollision(Collision collision)
    {
        // Check if collided with fence (using MeshCollider)
        if (IsFenceCollision(collision))
        {
            // Check cooldown
            if (Time.time - lastCollisionTime < collisionCooldown) return;
            
            // Set collision time
            lastCollisionTime = Time.time;
            
            Debug.Log($"🚗 Kart collided with fence: {collision.gameObject.name}");
            
            // Apply damage to player
            ApplyDamage();
            
            // Store current safe position before reset
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
            
            // Start reset process
            StartCoroutine(ResetToRoad());
            
            // Play collision effects
            PlayCollisionEffects(collision.contacts[0].point);
        }
    }
    
    bool IsFenceCollision(Collision collision)
    {
        // Check by tag
        if (!string.IsNullOrEmpty(fenceTag) && collision.gameObject.CompareTag(fenceTag))
        {
            return true;
        }
        
        // Check if it has a MeshCollider (common for fences)
        MeshCollider meshCollider = collision.gameObject.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            // Additional check: name contains "fence" or "Fence"
            string objName = collision.gameObject.name.ToLower();
            if (objName.Contains("fence") || objName.Contains("barrier") || objName.Contains("wall"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    void ApplyDamage()
    {
        if (isInvulnerable || playerHealth == null) return;
        
        // Deduct health
        playerHealth.TakeDamage(damagePerCollision);
        
        // Show warning message
        Debug.Log($"❤️ Player lost {damagePerCollision} heart(s)!");
    }
    
    IEnumerator ResetToRoad()
    {
        if (isResetting) yield break;
        
        isResetting = true;
        isInvulnerable = true;
        
        // Disable kart controls temporarily
        bool wasControllable = false;
        if (kartController != null)
        {
            wasControllable = kartController.enabled;
            kartController.SetControllable(false);
        }
        
        // Stop kart movement
        if (kartRigidbody != null)
        {
            kartRigidbody.linearVelocity = Vector3.zero;
            kartRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Find nearest road point
        Vector3 targetPosition = GetNearestRoadPoint();
        Quaternion targetRotation = Quaternion.LookRotation(GetRoadDirection(targetPosition));
        
        // Add height offset
        targetPosition += Vector3.up * resetHeight;
        
        // Smooth reset animation
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * resetSpeed;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        // Ensure exact position
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        
        // Wait a moment before re-enabling controls
        yield return new WaitForSeconds(0.5f);
        
        // Re-enable kart controls if it was controllable before
        if (kartController != null && wasControllable)
        {
            kartController.SetControllable(true);
        }
        
        // Store new safe position
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        
        // Keep invulnerability for a short time after reset
        yield return new WaitForSeconds(invulnerabilityAfterReset);
        
        isInvulnerable = false;
        isResetting = false;
    }
    
    Vector3 GetNearestRoadPoint()
    {
        // If waypoints are available, find nearest
        if (roadWaypoints != null && roadWaypoints.Length > 0)
        {
            Vector3 nearestPoint = roadWaypoints[0].position;
            float nearestDistance = Vector3.Distance(transform.position, nearestPoint);
            
            for (int i = 1; i < roadWaypoints.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, roadWaypoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPoint = roadWaypoints[i].position;
                }
            }
            
            // Adjust to be on the road surface
            RaycastHit hit;
            if (Physics.Raycast(nearestPoint + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                nearestPoint = hit.point;
            }
            
            return nearestPoint;
        }
        
        // Fallback: Return a position slightly forward from current position
        return transform.position + transform.forward * 3f;
    }
    
    Vector3 GetRoadDirection(Vector3 roadPoint)
    {
        // Find the road direction based on waypoints
        if (roadWaypoints != null && roadWaypoints.Length > 1)
        {
            // Find nearest waypoint index
            int nearestIndex = 0;
            float nearestDistance = Vector3.Distance(roadPoint, roadWaypoints[0].position);
            
            for (int i = 1; i < roadWaypoints.Length; i++)
            {
                float distance = Vector3.Distance(roadPoint, roadWaypoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }
            
            // Get direction to next waypoint
            if (nearestIndex < roadWaypoints.Length - 1)
            {
                Vector3 direction = (roadWaypoints[nearestIndex + 1].position - roadWaypoints[nearestIndex].position).normalized;
                return direction;
            }
            else if (nearestIndex > 0)
            {
                Vector3 direction = (roadWaypoints[nearestIndex].position - roadWaypoints[nearestIndex - 1].position).normalized;
                return direction;
            }
        }
        
        // Fallback: Use current forward direction
        return transform.forward;
    }
    
    void PlayCollisionEffects(Vector3 collisionPoint)
    {
        // Play sound
        if (collisionSound != null)
        {
            AudioSource.PlayClipAtPoint(collisionSound, collisionPoint);
        }
        
        // Play particles at collision point
        if (collisionParticles != null)
        {
            ParticleSystem particles = Instantiate(collisionParticles, collisionPoint, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
        
        // Camera shake effect
        StartCoroutine(ShakeCamera());
    }
    
    IEnumerator ShakeCamera()
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / shakeDuration;
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
            
            // Map noise to [-1, 1]
            float x = Random.value * 2.0f - 1.0f;
            float y = Random.value * 2.0f - 1.0f;
            x *= shakeIntensity * damper;
            y *= shakeIntensity * damper;
            
            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalPos;
    }
    
    // Public method to manually trigger reset (for debugging or special cases)
    public void ManualResetToRoad()
    {
        if (!isResetting)
        {
            StartCoroutine(ResetToRoad());
        }
    }
    
    // Method to add a waypoint at runtime
    public void AddWaypoint(Transform waypoint)
    {
        if (roadWaypoints == null)
        {
            roadWaypoints = new Transform[] { waypoint };
        }
        else
        {
            System.Array.Resize(ref roadWaypoints, roadWaypoints.Length + 1);
            roadWaypoints[roadWaypoints.Length - 1] = waypoint;
        }
    }
    
    // Method to manually set invulnerability
    public void SetInvulnerable(bool invulnerable, float duration = 0f)
    {
        isInvulnerable = invulnerable;
        if (duration > 0)
        {
            StartCoroutine(ResetInvulnerability(duration));
        }
    }
    
    IEnumerator ResetInvulnerability(float duration)
    {
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }
    
    // Visual debugging in editor
    void OnDrawGizmosSelected()
    {
        if (enabled)
        {
            // Draw reset area
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, resetDistance);
            
            // Draw waypoints if assigned
            if (roadWaypoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < roadWaypoints.Length; i++)
                {
                    if (roadWaypoints[i] != null)
                    {
                        Gizmos.DrawSphere(roadWaypoints[i].position, 0.5f);
                        
                        // Draw connections between waypoints
                        if (i < roadWaypoints.Length - 1 && roadWaypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(roadWaypoints[i].position, roadWaypoints[i + 1].position);
                        }
                    }
                }
            }
            
            // Draw safe position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSafePosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastSafePosition);
            
            // Draw current forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
    }
    
    void OnDrawGizmos()
    {
        // Always draw a small indicator on the kart for visibility
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}