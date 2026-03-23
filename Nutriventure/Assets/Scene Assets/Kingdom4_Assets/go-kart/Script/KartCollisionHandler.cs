using UnityEngine;
using System.Collections;

public class KartCollisionHandler : MonoBehaviour
{
    [Header("Fence Settings")]
    public string fenceTag = "Fence";
    
    [Header("Allergen Settings")]
    public string allergenTag = "Allergen"; // Optional: tag allergens with this
    
    [Header("Reset Settings")]
    public Transform[] roadWaypoints;
    public float resetDistance = 5f;
    public float resetHeight = 0.5f;
    public float resetSpeed = 10f;
    public bool findNearestWaypoint = true;

    [Header("Bounds Safety Reset")]
    public bool enableOutOfBoundsReset = true;
    public float positionLimitXZ = 50f;
    public float outOfBoundsDuration = 3f;

    [Header("Restricted Zone Reset")]
    public bool enableRestrictedZoneReset = true;
    public string restrictedZoneTag = "RestrictedZone";
    public bool allowNameContainsRestricted = true;
    
    [Header("Damage Settings")]
    public int damagePerCollision = 1;
    public float collisionCooldown = 2f;
    public float invulnerabilityAfterReset = 1f;
    public float allergenCooldown = 0.3f; // Separate cooldown for allergens
    
    [Header("Collision Effects")]
    public AudioClip collisionSound;
    public ParticleSystem collisionParticles;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.3f;
    
    [Header("Allergen Effects")]
    public AudioClip allergenSound;
    public ParticleSystem allergenParticles;
    
    [Header("Shield Settings")]
    public AudioClip shieldBlockSound;
    public ParticleSystem shieldBlockParticles;
    public float shieldBlockShakeIntensity = 0.3f;
    public float shieldBlockShakeDuration = 0.2f;
    
    [Header("References")]
    public PlayerHealthManager playerHealthManager; // Changed from PlayerHealth
    public KartController kartController;
    
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool isResetting = false;
    private float lastFenceCollisionTime = -10f;
    private float lastAllergenCollisionTime = -10f;
    private Rigidbody kartRigidbody;
    private bool isInvulnerable = false;
    private Camera mainCamera;
    private Vector3 cameraOriginalPosition;
    private float outOfBoundsTimer = 0f;
    
    void Start()
    {
        kartRigidbody = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        
        if (mainCamera != null)
        {
            cameraOriginalPosition = mainCamera.transform.localPosition;
        }
        
        // MODIFIED: Connect to PlayerHealthManager
        if (playerHealthManager == null)
        {
            // Try to find PlayerHealthManager (singleton pattern)
            if (PlayerHealthManager.Instance != null)
            {
                playerHealthManager = PlayerHealthManager.Instance;
                Debug.Log("Connected to PlayerHealthManager");
            }
            else
            {
                // Fallback: search for it in the scene
                PlayerHealthManager foundManager = FindObjectOfType<PlayerHealthManager>();
                if (foundManager != null)
                {
                    playerHealthManager = foundManager;
                    Debug.Log("Found PlayerHealthManager in scene");
                }
                else
                {
                    Debug.LogWarning("PlayerHealthManager not found! Damage won't work.");
                }
            }
        }
        
        if (kartController == null)
        {
            kartController = GetComponent<KartController>();
        }
        
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        
        if ((roadWaypoints == null || roadWaypoints.Length == 0) && findNearestWaypoint)
        {
            FindRoadWaypoints();
            // ADDED: Ensure waypoints have correct rotation
            EnsureWaypointRotations();
        }
    }

    void Update()
    {
        HandleOutOfBoundsReset();
    }
    
    void FindRoadWaypoints()
    {
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        
        if (waypointObjects.Length > 0)
        {
            roadWaypoints = new Transform[waypointObjects.Length];
            for (int i = 0; i < waypointObjects.Length; i++)
            {
                roadWaypoints[i] = waypointObjects[i].transform;
            }
            Debug.Log($"Found {roadWaypoints.Length} waypoints for road reset");
            
            // ADDED: Fix rotations after finding waypoints
            EnsureWaypointRotations();
        }
        else
        {
            Debug.LogWarning("No waypoints found with tag 'Waypoint'!");
        }
    }
    
    void EnsureWaypointRotations()
    {
        if (roadWaypoints == null || roadWaypoints.Length == 0) return;
        
        foreach (Transform waypoint in roadWaypoints)
        {
            if (waypoint != null)
            {
                // Set rotation to Y 180 (facing forward in Unity's coordinate system)
                Vector3 newRotation = waypoint.eulerAngles;
                newRotation.y = 180f;
                waypoint.rotation = Quaternion.Euler(newRotation);
                
                Debug.Log($"Set waypoint {waypoint.name} rotation to Y: 180");
            }
        }
    }
    
    [ContextMenu("Fix All Waypoint Rotations")]
    public void FixAllWaypointRotations()
    {
        // Refresh waypoints if needed
        if (roadWaypoints == null || roadWaypoints.Length == 0)
        {
            FindRoadWaypoints();
        }
        
        // Ensure rotations are correct
        EnsureWaypointRotations();
        
        Debug.Log($"Fixed rotations for {roadWaypoints.Length} waypoints");
    }
    
    public void InitializeWaypointsWithRotation(Transform[] newWaypoints)
    {
        roadWaypoints = newWaypoints;
        EnsureWaypointRotations();
        
        Debug.Log($"Initialized {roadWaypoints.Length} waypoints with Y rotation 180");
    }
    
    public void SetWaypointsAndFixRotation(Transform[] waypoints)
    {
        roadWaypoints = waypoints;
        FixAllWaypointRotations();
    }
    
    // Handle regular collisions (for fences)
    void OnCollisionEnter(Collision collision)
    {
        HandleFenceCollision(collision);
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastFenceCollisionTime > collisionCooldown && !isResetting)
        {
            HandleFenceCollision(collision);
        }
    }
    
    // Handle trigger collisions
    void OnTriggerEnter(Collider other)
    {
        // Add DEBUG LOG to see what's triggering
        Debug.Log($"🔵 Kart OnTriggerEnter: {other.gameObject.name}");

        if (enableRestrictedZoneReset && IsRestrictedZone(other.gameObject))
        {
            Debug.Log($"🚫 Entered restricted zone: {other.gameObject.name}. Resetting kart.");
            if (!isResetting)
            {
                StartCoroutine(ResetToRoad());
            }
            return;
        }
        
        // Check if it's a powerup FIRST
        ItemCollectible item = other.GetComponent<ItemCollectible>();
        if (item != null && item.itemData != null)
        {
            if (item.itemData.category == SpawnableItemData.ItemCategory.SafePowerup)
            {
                Debug.Log($"🎯 POWERUP detected! Type: {item.itemData.itemType}");
                Debug.Log($"   Skipping allergen handling for powerup");
                return; // Don't handle powerups at all
            }
        }
        
        // Only handle allergens (not powerups)
        HandleAllergenTrigger(other);
    }

    void HandleOutOfBoundsReset()
    {
        if (!enableOutOfBoundsReset || isResetting)
        {
            outOfBoundsTimer = 0f;
            return;
        }

        if (kartController != null && !kartController.enabled)
        {
            outOfBoundsTimer = 0f;
            return;
        }

        bool outOfBounds = Mathf.Abs(transform.position.x) >= positionLimitXZ || Mathf.Abs(transform.position.z) >= positionLimitXZ;

        if (!outOfBounds)
        {
            outOfBoundsTimer = 0f;
            return;
        }

        outOfBoundsTimer += Time.deltaTime;
        if (outOfBoundsTimer >= outOfBoundsDuration)
        {
            Debug.Log($"🧭 Out-of-bounds for {outOfBoundsDuration:F1}s (|X|/|Z| >= {positionLimitXZ}). Resetting kart.");
            outOfBoundsTimer = 0f;
            StartCoroutine(ResetToRoad());
        }
    }

    bool IsRestrictedZone(GameObject obj)
    {
        if (obj == null)
            return false;

        if (!string.IsNullOrEmpty(restrictedZoneTag) && obj.CompareTag(restrictedZoneTag))
            return true;

        if (allowNameContainsRestricted && obj.name.ToLower().Contains("restricted"))
            return true;

        return false;
    }
    
    void HandleFenceCollision(Collision collision)
    {
        if (IsFenceCollision(collision))
        {
            if (Time.time - lastFenceCollisionTime < collisionCooldown) return;
            
            lastFenceCollisionTime = Time.time;
            
            Debug.Log($"🚗 Kart collided with fence: {collision.gameObject.name}");
            
            if (IsShieldActive())
            {
                PlayShieldBlockEffect(collision.contacts[0].point);
                return;
            }
            
            ApplyDamage();
            
            lastSafePosition = transform.position;
            lastSafeRotation = transform.rotation;
            
            StartCoroutine(ResetToRoad());
            PlayCollisionEffects(collision.contacts[0].point);
        }
    }
    
    void HandleAllergenTrigger(Collider other)
    {
        // FIRST, check if it's a powerup - if so, DO NOTHING
        ItemCollectible itemCollectible = other.GetComponent<ItemCollectible>();
        if (itemCollectible != null && itemCollectible.itemData != null)
        {
            if (itemCollectible.itemData.category == SpawnableItemData.ItemCategory.SafePowerup)
            {
                Debug.Log($"🎯 Powerup detected by kart: {itemCollectible.itemData.itemType}");
                Debug.Log($"   Letting ItemCollectible handle collection...");
                return; // Let ItemCollectible handle powerups
            }
        }
        
        // Now check if it's an allergen
        if (IsAllergen(other.gameObject))
        {
            if (Time.time - lastAllergenCollisionTime < allergenCooldown) return;
            
            lastAllergenCollisionTime = Time.time;
            
            Debug.Log($"⚠️ Kart hit allergen: {other.gameObject.name}");
            
            if (IsShieldActive())
            {
                Debug.Log($"🛡️ Shield protected from allergen!");
                PlayShieldBlockEffect(other.transform.position);
                
                // Destroy the allergen
                if (other.gameObject != null)
                {
                    Destroy(other.gameObject);
                }
                return;
            }
            
            // Apply damage for allergen
            ApplyDamage();
            
            // Play allergen-specific effects
            PlayAllergenEffects(other.transform.position);
            
            // Destroy the allergen
            if (other.gameObject != null)
            {
                Destroy(other.gameObject);
            }
        }
    }
    
    bool IsFenceCollision(Collision collision)
    {
        if (!string.IsNullOrEmpty(fenceTag) && collision.gameObject.CompareTag(fenceTag))
        {
            return true;
        }
        
        MeshCollider meshCollider = collision.gameObject.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            string objName = collision.gameObject.name.ToLower();
            if (objName.Contains("fence") || objName.Contains("barrier") || objName.Contains("wall"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool IsAllergen(GameObject obj)
    {
        // Check by tag
        if (!string.IsNullOrEmpty(allergenTag) && obj.CompareTag(allergenTag))
        {
            return true;
        }
        
        // Check by name
        string objName = obj.name.ToLower();
        string[] allergenKeywords = { 
            "peanut", "milk", "egg", "fish", "shellfish", 
            "treenut", "wheat", "soybean", "sesame", "allergen" 
        };
        
        foreach (string keyword in allergenKeywords)
        {
            if (objName.Contains(keyword))
            {
                return true;
            }
        }
        
        // Check by ItemCollectible component - ONLY if it's NOT a powerup
        ItemCollectible item = obj.GetComponent<ItemCollectible>();
        if (item != null && item.itemData != null)
        {
            // Check if it's an allergen (NotSafe category)
            if (item.itemData.category == SpawnableItemData.ItemCategory.NotSafe)
            {
                return true;
            }
            // If it's a powerup, return false
            else if (item.itemData.category == SpawnableItemData.ItemCategory.SafePowerup)
            {
                return false;
            }
        }
        
        return false;
    }
    
    bool IsShieldActive()
    {
        return ItemCollectible.IsShieldActive();
    }
    
    void ApplyDamage()
    {
        if (isInvulnerable) return;
        
        // Use PlayerHealthManager
        if (playerHealthManager != null)
        {
            playerHealthManager.TakeDamage(damagePerCollision);
            Debug.Log($"❤️ Player lost {damagePerCollision} heart(s)! (via PlayerHealthManager)");
        }
        else
        {
            Debug.LogWarning("No PlayerHealthManager connected!");
        }
    }
    
    IEnumerator ResetToRoad()
    {
        if (isResetting) yield break;
        
        isResetting = true;
        isInvulnerable = true;
        
        bool wasControllable = false;
        if (kartController != null)
        {
            wasControllable = kartController.enabled;
            kartController.SetControllable(false);
        }
        
        if (kartRigidbody != null)
        {
            kartRigidbody.linearVelocity = Vector3.zero;
            kartRigidbody.angularVelocity = Vector3.zero;
        }
        
        Vector3 targetPosition = GetNearestRoadPoint();
        
        // MODIFIED: Always use Y rotation 180
        Vector3 targetDirection = GetRoadDirection(targetPosition);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        
        // Ensure Y rotation is exactly 180
        Vector3 targetEuler = targetRotation.eulerAngles;
        targetEuler.y = 180f;
        targetRotation = Quaternion.Euler(targetEuler);
        
        targetPosition += Vector3.up * resetHeight;
        
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
        
        // Final adjustment to ensure exact Y 180 rotation
        Vector3 finalEuler = targetRotation.eulerAngles;
        finalEuler.y = 180f;
        transform.rotation = Quaternion.Euler(finalEuler);
        transform.position = targetPosition;
        
        yield return new WaitForSeconds(0.5f);
        
        if (kartController != null && wasControllable)
        {
            kartController.SetControllable(true);
        }
        
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        
        yield return new WaitForSeconds(invulnerabilityAfterReset);
        
        isInvulnerable = false;
        isResetting = false;
    }
    
    Vector3 GetNearestRoadPoint()
    {
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
            
            RaycastHit hit;
            if (Physics.Raycast(nearestPoint + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                nearestPoint = hit.point;
            }
            
            return nearestPoint;
        }
        
        return transform.position + transform.forward * 3f;
    }
    
    Vector3 GetRoadDirection(Vector3 roadPoint)
    {
        if (roadWaypoints != null && roadWaypoints.Length > 1)
        {
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
            
            // Calculate direction based on waypoints
            Vector3 direction = Vector3.forward; // Default
            
            if (nearestIndex < roadWaypoints.Length - 1)
            {
                direction = (roadWaypoints[nearestIndex + 1].position - roadWaypoints[nearestIndex].position).normalized;
            }
            else if (nearestIndex > 0)
            {
                direction = (roadWaypoints[nearestIndex].position - roadWaypoints[nearestIndex - 1].position).normalized;
            }
            
            // Create rotation with Y always at 180
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 eulerAngles = rotation.eulerAngles;
            eulerAngles.y = 180f; // Force Y rotation to 180
            return Quaternion.Euler(eulerAngles) * Vector3.forward;
        }
        
        // Fallback: return direction with Y rotation 180
        return Quaternion.Euler(0, 180, 0) * Vector3.forward;
    }
    
    void PlayCollisionEffects(Vector3 collisionPoint)
    {
        if (collisionSound != null)
        {
            AudioSource.PlayClipAtPoint(collisionSound, collisionPoint);
        }
        
        if (collisionParticles != null)
        {
            ParticleSystem particles = Instantiate(collisionParticles, collisionPoint, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
        
        StartCoroutine(ShakeCamera(shakeIntensity, shakeDuration));
    }
    
    void PlayAllergenEffects(Vector3 position)
    {
        // Use allergen-specific sound if available, otherwise use collision sound
        AudioClip sound = allergenSound != null ? allergenSound : collisionSound;
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, position);
        }
        
        // Use allergen-specific particles if available
        ParticleSystem particles = allergenParticles != null ? allergenParticles : collisionParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
        
        // Gentler shake for allergens
        StartCoroutine(ShakeCamera(shakeIntensity * 0.5f, shakeDuration * 0.5f));
    }
    
    void PlayShieldBlockEffect(Vector3 collisionPoint)
    {
        Debug.Log($"🛡️ Shield protected!");
        
        if (shieldBlockSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldBlockSound, collisionPoint);
        }
        
        if (shieldBlockParticles != null)
        {
            ParticleSystem particles = Instantiate(shieldBlockParticles, collisionPoint, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
        
        StartCoroutine(ShakeCamera(shieldBlockShakeIntensity, shieldBlockShakeDuration));
        StartCoroutine(FlashShieldEffect());
    }
    
    IEnumerator ShakeCamera(float intensity, float duration)
    {
        if (mainCamera == null) yield break;
        
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percentComplete = elapsed / duration;
            float damper = 1.0f - Mathf.Clamp(4.0f * percentComplete - 3.0f, 0.0f, 1.0f);
            
            float x = Random.value * 2.0f - 1.0f;
            float y = Random.value * 2.0f - 1.0f;
            x *= intensity * damper;
            y *= intensity * damper;
            
            mainCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalPos;
    }
    
    IEnumerator FlashShieldEffect()
    {
        Renderer playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            Color originalColor = playerRenderer.material.color;
            playerRenderer.material.color = Color.cyan;
            yield return new WaitForSeconds(0.1f);
            playerRenderer.material.color = originalColor;
        }
    }
    
    // Keep all your existing public methods...
    public void ManualResetToRoad()
    {
        if (!isResetting)
        {
            StartCoroutine(ResetToRoad());
        }
    }
    
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
        
        // Fix the new waypoint's rotation
        Vector3 newRotation = waypoint.eulerAngles;
        newRotation.y = 180f;
        waypoint.rotation = Quaternion.Euler(newRotation);
    }
    
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
    
    public bool CheckShieldActive()
    {
        return IsShieldActive();
    }
    
    public bool GetInvulnerabilityState()
    {
        return isInvulnerable;
    }
    
    public bool GetResettingState()
    {
        return isResetting;
    }
    
    public Vector3 GetLastSafePosition()
    {
        return lastSafePosition;
    }
    
    public void TestShieldEffect()
    {
        PlayShieldBlockEffect(transform.position);
    }
    
    [ContextMenu("Test Allergen Damage")]
    void TestAllergenDamage()
    {
        Debug.Log("Testing allergen damage...");
        ApplyDamage();
    }
    
    void OnDrawGizmosSelected()
    {
        if (enabled)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, resetDistance);
            
            if (roadWaypoints != null)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < roadWaypoints.Length; i++)
                {
                    if (roadWaypoints[i] != null)
                    {
                        Gizmos.DrawSphere(roadWaypoints[i].position, 0.5f);
                        
                        // Draw rotation indicator (Y 180)
                        Vector3 forward = roadWaypoints[i].rotation * Vector3.forward;
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(roadWaypoints[i].position, forward * 2f);
                        
                        Gizmos.color = Color.green;
                        if (i < roadWaypoints.Length - 1 && roadWaypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(roadWaypoints[i].position, roadWaypoints[i + 1].position);
                        }
                    }
                }
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastSafePosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastSafePosition);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 3f);
        }
    }
    
    void OnDrawGizmos()
    {
        if (enabled)
        {
            Gizmos.color = IsShieldActive() ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            
            if (IsShieldActive())
            {
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Gizmos.DrawSphere(transform.position, 0.6f);
            }
        }
    }
}