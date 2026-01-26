using UnityEngine;
using System.Collections;

public class ItemCollectible : MonoBehaviour
{
    public enum Kingdom4Phase
{
    Phase1_FindAllergens,
    Phase2_Wagon,
    Phase3_MovingRocks
}

[Header("Kingdom 4 Phase")]
public Kingdom4Phase currentPhase;



    [Header("Item Settings")]
    public SpawnableItemData itemData;
    
    [Header("Collection Settings")]
    public float collectionRadius = 1f;
    public AudioClip overrideCollectSound;
    public ParticleSystem overrideCollectParticles;
    
    [Header("Shield Settings")]
    public float shieldDuration = 5f; // Fixed: 5 seconds duration
    public Material shieldMaterial;
    public ParticleSystem shieldActivationParticles;
    public AudioClip shieldActivationSound;
    
    [Header("Allergen Damage Settings")]
    public int damageAmount = 1; // Hearts to deduct for allergens
    public ParticleSystem damageParticles;
    public AudioClip damageSound;
    
    [Header("Visual Feedback")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    
    [Header("Particle Attachment Settings")]
    public GameObject shieldPowerupParticles; // Assign your shield particle prefab
    public GameObject heartPowerupParticles;  // Assign your heart particle prefab
    public Vector3 particleOffset = new Vector3(0, 1f, 0); // Position above kart
    
    private SphereCollider triggerCollider;
    private bool isCollected = false;
    private GameObject playerObject;
    private Vector3 startPosition;
    private float floatOffset;
    
    // FIXED: Proper static shield tracking
    private static bool isShieldActiveGlobal = false;
    private static float shieldEndTime = 0f;
    private static Material originalKartMaterial;
    private static GameObject currentShieldParticles;
    private static GameObject currentHeartParticles;
    private static Coroutine shieldCheckCoroutine;
    
    // NEW: Heart particle duration
    private static float heartParticlesEndTime = 0f;
    private static Coroutine heartParticleCheckCoroutine;
    
    public void Initialize(SpawnableItemData data)
    {
        itemData = data;
        SetupCollider();
        ApplyVisualMaterial();
        SetupFloating();
        
        // Set damage amount based on item type
        if (itemData != null && itemData.category == SpawnableItemData.ItemCategory.NotSafe)
        {
            damageAmount = 1; // All allergens deduct 1 heart
        }
        
        // Find player once
        playerObject = GameObject.FindGameObjectWithTag("Player");
        
        Debug.Log($"Initialized {itemData.itemType} ({itemData.category}) at {transform.position}");
    }
    
    void Start()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemCollectible has no itemData assigned!");
        }
        else
        {
            SetupCollider();
            SetupFloating();
        }
    }
    
    void SetupCollider()
    {
        // Remove existing colliders if any
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            if (!(col is SphereCollider)) // Keep SphereCollider if it exists
                Destroy(col);
        }
        
        // Get or add SphereCollider
        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        triggerCollider.isTrigger = true;
        triggerCollider.radius = collectionRadius;
        
        // Add a Rigidbody for better collision detection
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Don't let physics move it
            rb.useGravity = false;
        }
    }
    
    void SetupFloating()
    {
        startPosition = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f); // Random offset for variety
        StartCoroutine(FloatAnimation());
    }
    
    IEnumerator FloatAnimation()
    {
        while (!isCollected)
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + floatOffset) * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            yield return null;
        }
    }
    
    void ApplyVisualMaterial()
    {
        if (itemData.material != null)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = itemData.material;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        
        Debug.Log($"🎯 ItemCollectible triggered by: {other.gameObject.name}");
        
        // Check if it's the player OR the kart
        bool isPlayer = other.CompareTag("Player");
        bool isKart = other.GetComponent<KartCollisionHandler>() != null || 
                      other.GetComponentInParent<KartCollisionHandler>() != null;
        
        if (isPlayer || isKart)
        {
            Debug.Log($"🎉 Collected! (Player: {isPlayer}, Kart: {isKart})");
            CollectItem();
        }
        else
        {
            Debug.Log($"❓ Not collected - not player or kart");
        }
    }
    
    public void CollectItem()
    {
        if (isCollected) 
        {
            Debug.LogWarning($"Already collected: {itemData.itemType}");
            return;
        }
        
        isCollected = true;
        
        StopAllCoroutines();
        
        // IMMEDIATELY disable collider to prevent multiple triggers
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        
        // IMMEDIATELY disable renderer so it disappears
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;
        
        // Play collection effects
        PlayCollectionEffects();
        
        // Handle different item types
        switch (itemData.category)
        {
            case SpawnableItemData.ItemCategory.SafePassable:
                HandleCoinCollection();
                break;
            case SpawnableItemData.ItemCategory.NotSafe:
                HandleAllergenCollection();
                break;
            case SpawnableItemData.ItemCategory.SafePowerup:
                HandlePowerupCollection();
                break;
        }
        
        // Destroy IMMEDIATELY after effects start
        Destroy(gameObject, 0.1f); // Very short delay for effects
    }
    
    void HandleCoinCollection()
{
    Debug.Log("🥗 Healthy food collected");

    if (currentPhase == Kingdom4Phase.Phase3_MovingRocks)
    {
        Kingdom4ScoreManager.Instance?.HitHealthyFood();
    }
}

    
    void HandleAllergenCollection()
{
    // ===== BELOW IS PHASE 2 & 3 ONLY =====
    if (isShieldActiveGlobal && Time.time < shieldEndTime)
    {
        PlayShieldBlockEffect();
        return;
    }
    else if (isShieldActiveGlobal && Time.time >= shieldEndTime)
    {
        DeactivateShield();
    }

    // ❌ Player made a mistake
    ApplyAllergenDamage();
    PlayAllergenDamageEffects();

    // ✅ PHASE 3: COMBO RESET
    if (currentPhase == Kingdom4Phase.Phase3_MovingRocks)
    {
        Kingdom4ScoreManager.Instance?.HitAllergenInPhase3();
    }
}

    
    void ApplyAllergenDamage()
{
    PlayerHealth health = playerObject?.GetComponent<PlayerHealth>() 
                          ?? FindAnyObjectByType<PlayerHealth>();

    if (health == null) return;

    int before = health.currentHearts;
    health.TakeDamage(damageAmount);

    if (health.currentHearts < before &&
        currentPhase == Kingdom4Phase.Phase2_Wagon)
    {
        Kingdom4ScoreManager.Instance?.WagonHitAllergen();
    }
}

    
    void PlayAllergenDamageEffects()
    {
        // Play damage sound
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }
        else if (itemData.collectSound != null)
        {
            AudioSource.PlayClipAtPoint(itemData.collectSound, transform.position);
        }
        
        // Play damage particles
        if (damageParticles != null)
        {
            ParticleSystem particles = Instantiate(damageParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
    }
    
    void PlayShieldBlockEffect()
    {
        // Play shield block sound
        AudioClip sound = shieldActivationSound ?? itemData.collectSound;
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, transform.position);
        }
        
        // Play shield block particles
        ParticleSystem particles = shieldActivationParticles ?? itemData.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }
    void Awake()
{
    // Default safe phase (can be overridden by spawners)
    currentPhase = Kingdom4Phase.Phase1_FindAllergens;
}

    
    void HandlePowerupCollection()
    {
        if (itemData.itemType == SpawnableItemData.ItemType.Shield)
        {
            Debug.Log("🛡️ Shield collected! Activating shield for 5 seconds...");
            
            // Attach shield particles to kart
            AttachShieldParticlesToKart();
            
            // Activate shield with 5-second duration
            ActivateShield();
        }
        else if (itemData.itemType == SpawnableItemData.ItemType.Heart)
        {
            Debug.Log("❤️ Heart collected! Healing player...");
            
            // Attach heart particles to kart
            AttachHeartParticlesToKart();
            
            PlayerHealth health = playerObject?.GetComponent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
            if (health != null)
            {
                int heartsBefore = health.currentHearts;
                health.Heal(1);
                Debug.Log($"Healed! Hearts: {heartsBefore} → {health.currentHearts}");
            }
            else
            {
                Debug.LogError("PlayerHealth component not found!");
            }
        }
        else
        {
            Debug.LogError($"Unknown powerup type: {itemData.itemType}");
        }
    }
    
    void AttachShieldParticlesToKart()
    {
        if (shieldPowerupParticles == null)
        {
            Debug.LogWarning("No shield particle prefab assigned!");
            return;
        }
        
        // Remove existing shield particles
        RemoveShieldParticles();
        
        // Find the kart
        GameObject kart = FindKartObject();
        if (kart == null)
        {
            Debug.LogError("No kart found to attach shield particles!");
            return;
        }
        
        // Create and attach shield particles
        currentShieldParticles = Instantiate(shieldPowerupParticles);
        
        // Attach to kart with smooth following
        AttachParticlesToKart(currentShieldParticles, kart, particleOffset);
        
        Debug.Log($"🛡️ Shield particles attached to {kart.name} for {shieldDuration} seconds");
    }
    
    void AttachHeartParticlesToKart()
    {
        if (heartPowerupParticles == null)
        {
            Debug.LogWarning("No heart particle prefab assigned!");
            return;
        }
        
        // Remove existing heart particles
        RemoveHeartParticles();
        
        // Find the kart
        GameObject kart = FindKartObject();
        if (kart == null)
        {
            Debug.LogError("No kart found to attach heart particles!");
            return;
        }
        
        // Create and attach heart particles
        currentHeartParticles = Instantiate(heartPowerupParticles);
        
        // Attach to kart with smooth following
        AttachParticlesToKart(currentHeartParticles, kart, particleOffset);
        
        Debug.Log($"❤️ Heart particles attached to {kart.name}");
        
        // Set heart particle duration to 5 seconds
        heartParticlesEndTime = Time.time + 5f;
        
        // Start heart particle expiration check
        StartHeartParticleExpirationCheck();
    }
    
    void StartHeartParticleExpirationCheck()
    {
        // Stop any existing heart particle check
        if (heartParticleCheckCoroutine != null)
        {
            StopCoroutine(heartParticleCheckCoroutine);
        }
        
        // Start new heart particle check on a MonoBehaviour that won't be destroyed
        GameObject heartParticleManager = new GameObject("HeartParticleTimerManager");
        DontDestroyOnLoad(heartParticleManager);
        HeartParticleTimerManager timerManager = heartParticleManager.AddComponent<HeartParticleTimerManager>();
        heartParticleCheckCoroutine = timerManager.StartCoroutine(CheckHeartParticleExpirationCoroutine());
    }
    
    IEnumerator CheckHeartParticleExpirationCoroutine()
    {
        Debug.Log("⏱️ Heart particle timer started...");
        
        while (Time.time < heartParticlesEndTime)
        {
            float remainingTime = heartParticlesEndTime - Time.time;
            int currentSecond = Mathf.FloorToInt(remainingTime);
            int previousSecond = Mathf.FloorToInt(remainingTime + Time.deltaTime);
            
            if (currentSecond != previousSecond && currentSecond > 0)
            {
                Debug.Log($"❤️ Heart particles time remaining: {remainingTime:F1}s");
            }
            yield return null;
        }
        
        // Heart particles expired
        Debug.Log("❤️ Heart particles EXPIRED after 5 seconds!");
        RemoveHeartParticles();
        heartParticlesEndTime = 0f;
        
        // Clean up the timer manager GameObject
        GameObject timerManager = GameObject.Find("HeartParticleTimerManager");
        if (timerManager != null)
        {
            Destroy(timerManager);
        }
    }
    
    GameObject FindKartObject()
    {
        // First try to find by tag
        GameObject kart = GameObject.FindGameObjectWithTag("Player");
        
        // If not found, try to find by component
        if (kart == null)
        {
            KartCollisionHandler kartHandler = FindAnyObjectByType<KartCollisionHandler>();
            if (kartHandler != null)
            {
                kart = kartHandler.gameObject;
            }
        }
        
        // If still not found, use cached player object
        if (kart == null && playerObject != null)
        {
            kart = playerObject;
        }
        
        return kart;
    }
    
    void AttachParticlesToKart(GameObject particles, GameObject kart, Vector3 offset)
    {
        // Add a follower script for smooth movement
        ParticleFollower follower = particles.AddComponent<ParticleFollower>();
        follower.target = kart.transform;
        follower.offset = offset;
        follower.followSpeed = 20f;
        follower.rotateWithTarget = false;
        
        // Optionally set parent for hierarchy organization
        particles.transform.SetParent(kart.transform);
        particles.transform.localPosition = offset;
        particles.transform.localRotation = Quaternion.identity;
    }
    
    static void RemoveShieldParticles()
    {
        if (currentShieldParticles != null)
        {
            Debug.Log("🛡️ Removing shield particles...");
            
            // Stop particles gracefully
            ParticleSystem ps = currentShieldParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                float destroyDelay = ps.main.duration;
                Destroy(currentShieldParticles, destroyDelay);
                Debug.Log($"✅ Shield particles will be destroyed in {destroyDelay}s");
            }
            else
            {
                Destroy(currentShieldParticles);
                Debug.Log($"✅ Shield particles destroyed immediately");
            }
            
            currentShieldParticles = null;
        }
        else
        {
            Debug.Log("ℹ️ No shield particles to remove");
        }
    }
    
    static void RemoveHeartParticles()
    {
        if (currentHeartParticles != null)
        {
            Debug.Log("❤️ Removing heart particles...");
            
            // Stop particles gracefully
            ParticleSystem ps = currentHeartParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                float destroyDelay = ps.main.duration;
                Destroy(currentHeartParticles, destroyDelay);
                Debug.Log($"✅ Heart particles will be destroyed in {destroyDelay}s");
            }
            else
            {
                Destroy(currentHeartParticles);
                Debug.Log($"✅ Heart particles destroyed immediately");
            }
            
            currentHeartParticles = null;
            
            // Clean up the timer manager GameObject
            GameObject timerManager = GameObject.Find("HeartParticleTimerManager");
            if (timerManager != null)
            {
                Destroy(timerManager);
            }
        }
        else
        {
            Debug.Log("ℹ️ No heart particles to remove");
        }
    }
    
    void ActivateShield()
    {
        // Set shield end time
        shieldEndTime = Time.time + shieldDuration;
        isShieldActiveGlobal = true;
        
        // Apply shield visual
        ApplyShieldVisual(true);
        
        // Start shield expiration check coroutine
        StartShieldExpirationCheck();
        
        PlayShieldActivationEffects();
        
        Debug.Log($"🛡️ Shield activated until {shieldEndTime} (Current time: {Time.time})");
    }
    
    void StartShieldExpirationCheck()
    {
        // Stop any existing shield check
        if (shieldCheckCoroutine != null)
        {
            StopCoroutine(shieldCheckCoroutine);
        }
        
        // Start new shield check on a MonoBehaviour that won't be destroyed
        GameObject shieldManager = new GameObject("ShieldTimerManager");
        DontDestroyOnLoad(shieldManager);
        ShieldTimerManager timerManager = shieldManager.AddComponent<ShieldTimerManager>();
        shieldCheckCoroutine = timerManager.StartCoroutine(CheckShieldExpirationCoroutine());
    }
    
    IEnumerator CheckShieldExpirationCoroutine()
    {
        Debug.Log("⏱️ Shield timer started...");
        
        float startTime = Time.time;
        
        while (Time.time < shieldEndTime && isShieldActiveGlobal)
        {
            // Log remaining time every second
            float remainingTime = shieldEndTime - Time.time;
            int currentSecond = Mathf.FloorToInt(remainingTime);
            int previousSecond = Mathf.FloorToInt(remainingTime + Time.deltaTime);
            
            if (currentSecond != previousSecond && currentSecond > 0)
            {
                Debug.Log($"🛡️ Shield time remaining: {remainingTime:F1}s");
            }
            yield return null;
        }
        
        // Shield expired
        if (isShieldActiveGlobal && Time.time >= shieldEndTime)
        {
            Debug.Log("🛡️ Shield EXPIRED after 5 seconds!");
            DeactivateShield();
        }
        
        // Clean up the timer manager GameObject
        GameObject timerManager = GameObject.Find("ShieldTimerManager");
        if (timerManager != null)
        {
            Destroy(timerManager);
        }
    }
    
    void ApplyShieldVisual(bool enable)
    {
        GameObject kart = FindKartObject();
        if (kart == null) return;
        
        Renderer kartRenderer = kart.GetComponent<Renderer>();
        if (kartRenderer != null)
        {
            if (enable)
            {
                // Store original material
                if (originalKartMaterial == null)
                {
                    originalKartMaterial = kartRenderer.material;
                }
                
                // Apply shield material
                if (shieldMaterial != null)
                {
                    kartRenderer.material = shieldMaterial;
                }
                else
                {
                    // Default shield effect - tint blue
                    kartRenderer.material.color = Color.cyan;
                }
            }
            else
            {
                // Restore original material
                if (originalKartMaterial != null)
                {
                    kartRenderer.material = originalKartMaterial;
                }
            }
        }
    }
    
    void PlayShieldActivationEffects()
    {
        if (shieldActivationSound != null)
        {
            AudioSource.PlayClipAtPoint(shieldActivationSound, transform.position);
        }
        else if (itemData.collectSound != null)
        {
            AudioSource.PlayClipAtPoint(itemData.collectSound, transform.position);
        }
        
        ParticleSystem particles = shieldActivationParticles ?? itemData.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }
    
    void PlayCollectionEffects()
    {
        // Play collection sound (for all items)
        AudioClip sound = overrideCollectSound ?? itemData.collectSound;
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, transform.position);
        }
        
        // Play collection particles (for all items)
        ParticleSystem particles = overrideCollectParticles ?? itemData.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }
    
    // Public static method to check shield status
    public static bool IsShieldActive()
    {
        return isShieldActiveGlobal && Time.time < shieldEndTime;
    }
    
    public static void DeactivateShield()
    {
        isShieldActiveGlobal = false;
        shieldEndTime = 0f;
        
        // Restore visual
        GameObject kart = GameObject.FindGameObjectWithTag("Player") ?? 
                         FindAnyObjectByType<KartCollisionHandler>()?.gameObject;
        if (kart != null && originalKartMaterial != null)
        {
            Renderer kartRenderer = kart.GetComponent<Renderer>();
            if (kartRenderer != null)
            {
                kartRenderer.material = originalKartMaterial;
                Debug.Log("✅ Shield visual removed from kart");
            }
        }
        
        // Remove shield particles
        RemoveShieldParticles();
        
        Debug.Log("🛡️ Shield deactivated!");
    }
    
    // Clean up on destroy
    void OnDestroy()
    {
        // No coroutines to stop since we're using separate GameObject
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (itemData != null)
        {
            // Color code based on category
            Gizmos.color = itemData.category switch
            {
                SpawnableItemData.ItemCategory.SafePassable => Color.yellow,
                SpawnableItemData.ItemCategory.NotSafe => Color.red,
                SpawnableItemData.ItemCategory.SafePowerup => Color.blue,
                _ => Color.white
            };
            
            float radius = triggerCollider != null ? triggerCollider.radius : collectionRadius;
            Gizmos.DrawWireSphere(transform.position, radius);
            
            // Draw icon based on type
            string icon = itemData.itemType switch
            {
                SpawnableItemData.ItemType.Coin => "💰",
                SpawnableItemData.ItemType.Shield => "🛡️",
                SpawnableItemData.ItemType.Heart => "❤️",
                _ => "●"
            };
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"{icon}\n{itemData.itemType}\n{itemData.category}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (itemData != null && itemData.category == SpawnableItemData.ItemCategory.SafePowerup)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
    #endif
}

// Helper class to run the shield timer coroutine
public class ShieldTimerManager : MonoBehaviour
{
    // Empty class just to have a MonoBehaviour to run coroutines
}

// NEW: Helper class to run the heart particle timer coroutine
public class HeartParticleTimerManager : MonoBehaviour
{
    // Empty class just to have a MonoBehaviour to run coroutines
}