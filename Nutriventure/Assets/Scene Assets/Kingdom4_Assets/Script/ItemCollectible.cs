using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ItemCollectible : MonoBehaviour
{
    public enum Kingdom4Phase
    {
        Phase1_FindAllergens,
        Phase2_Wagon,
        Phase3_MovingRocks
    }

    [Header("Kingdom 4 Phase")]
    public Kingdom4Phase currentPhase = Kingdom4Phase.Phase1_FindAllergens;

    [Header("Item Settings")]
    public SpawnableItemData itemData;

    [Header("Collection Settings")]
    public float collectionRadius = 1f;
    public AudioClip overrideCollectSound;
    public ParticleSystem overrideCollectParticles;

    [Header("Shield Settings")]
    public float shieldDuration = 5f;
    public Material shieldMaterial;
    public ParticleSystem shieldActivationParticles;
    public AudioClip shieldActivationSound;

    [Header("Allergen Damage Settings")]
    public int damageAmount = 1;
    public ParticleSystem damageParticles;
    public AudioClip damageSound;

    [Header("Visual Feedback")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;

    [Header("Particle Attachment Settings")]
    public GameObject shieldPowerupParticles;
    public GameObject heartPowerupParticles;
    public Vector3 particleOffset = new Vector3(0, 1f, 0);

    [Header("HEALTHY FOOD AUDIO FEEDBACK")]
    [Tooltip("List of audio clips to randomly play when collecting ANY healthy food")]
    public List<AudioClip> healthyFoodAudioClips = new List<AudioClip>();

    // Volume control for different feedback types
    [Range(0f, 1f)] public float healthyFoodVolume = 0.8f;
    [Range(0f, 1f)] public float allergenVolume = 1f;
    [Range(0f, 1f)] public float powerupVolume = 1f;

    // Settings for random audio behavior
    [Header("Audio Randomization Settings")]
    [Tooltip("If true, will never play the same sound twice in a row")]
    public bool preventRepeatSounds = true;
    [Tooltip("Minimum pitch variation")]
    [Range(0.5f, 1.5f)] public float minPitch = 0.9f;
    [Tooltip("Maximum pitch variation")]
    [Range(0.5f, 1.5f)] public float maxPitch = 1.1f;

    private SphereCollider triggerCollider;
    private bool isCollected = false;
    private GameObject playerObject;
    private Vector3 startPosition;
    private float floatOffset;

    // Shield tracking
    private static bool isShieldActiveGlobal = false;
    private static float shieldEndTime = 0f;
    private static Material originalKartMaterial;
    private static GameObject currentShieldParticles;
    private static GameObject currentHeartParticles;
    private static Coroutine shieldCheckCoroutine;

    // Heart particle duration
    private static float heartParticlesEndTime = 0f;
    private static Coroutine heartParticleCheckCoroutine;

    // Audio source for spatial audio
    private AudioSource audioSource;

    // Audio tracking
    private static AudioClip lastPlayedHealthySound;

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
            SetupAudioSource();
        }

        playerObject = GameObject.FindGameObjectWithTag("Player");
    }

    void SetupAudioSource()
    {
        // Add AudioSource if not present
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 20f;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    void SetupCollider()
    {
        Collider[] existingColliders = GetComponents<Collider>();
        foreach (Collider col in existingColliders)
        {
            if (!(col is SphereCollider))
                Destroy(col);
        }

        triggerCollider = GetComponent<SphereCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = collectionRadius;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void SetupFloating()
    {
        startPosition = transform.position;
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
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

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        bool isPlayer = other.CompareTag("Player");
        bool isKart = other.GetComponent<KartCollisionHandler>() != null ||
                      other.GetComponentInParent<KartCollisionHandler>() != null;

        if (isPlayer || isKart)
        {
            CollectItem();
        }
    }

    public void CollectItem()
    {
        if (isCollected) return;

        isCollected = true;

        StopAllCoroutines();

        // Disable visuals immediately
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        // Play collection effects (visual only, audio handled in specific handlers)
        PlayVisualCollectionEffects();

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

        // Destroy the GameObject immediately
        Destroy(gameObject);
    }

    void HandleCoinCollection()
    {
        Debug.Log("🥗 Healthy food collected");

        // Play random healthy food sound from the list
        PlayRandomHealthyFoodSound();

        if (currentPhase == Kingdom4Phase.Phase3_MovingRocks)
        {
            Kingdom4ScoreManager.Instance?.HitHealthyFood();
        }
    }

    void PlayRandomHealthyFoodSound()
    {
        if (healthyFoodAudioClips == null || healthyFoodAudioClips.Count == 0)
        {
            Debug.LogWarning("No healthy food audio clips assigned in the list!");
            return;
        }

        List<AudioClip> availableClips = new List<AudioClip>(healthyFoodAudioClips);
        availableClips.RemoveAll(clip => clip == null);

        if (availableClips.Count == 0)
        {
            Debug.LogWarning("All healthy food audio clips are null!");
            return;
        }

        if (preventRepeatSounds && availableClips.Count > 1)
        {
            if (lastPlayedHealthySound != null)
            {
                availableClips.Remove(lastPlayedHealthySound);
            }
        }

        int randomIndex = Random.Range(0, availableClips.Count);
        AudioClip selectedClip = availableClips[randomIndex];
        lastPlayedHealthySound = selectedClip;

        float pitch = Random.Range(minPitch, maxPitch);

        // Create a temporary GameObject to play the sound
        GameObject tempAudioObj = new GameObject("TempAudio_" + selectedClip.name);
        tempAudioObj.transform.position = transform.position;
        AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
        tempAudioSource.clip = selectedClip;
        tempAudioSource.volume = healthyFoodVolume;
        tempAudioSource.pitch = pitch;
        tempAudioSource.spatialBlend = 1f;
        tempAudioSource.minDistance = 1f;
        tempAudioSource.maxDistance = 20f;
        tempAudioSource.Play();

        Destroy(tempAudioObj, selectedClip.length + 0.1f);

        Debug.Log($"Playing healthy food sound: {selectedClip.name} (Pitch: {pitch:F2})");
    }

    void HandleAllergenCollection()
    {
        if (isShieldActiveGlobal && Time.time < shieldEndTime)
        {
            PlayShieldBlockEffect();
            return;
        }
        else if (isShieldActiveGlobal && Time.time >= shieldEndTime)
        {
            DeactivateShield();
        }

        ApplyAllergenDamage();
        PlayAllergenDamageEffects();

        if (currentPhase == Kingdom4Phase.Phase3_MovingRocks)
        {
            Kingdom4ScoreManager.Instance?.HitAllergenInPhase3();
        }
    }

    void ApplyAllergenDamage()
    {
        PlayerHealthManager healthManager = PlayerHealthManager.Instance;

        if (healthManager == null)
        {
            healthManager = playerObject?.GetComponent<PlayerHealthManager>();
            if (healthManager == null)
            {
                Debug.LogWarning("PlayerHealthManager not found!");
                return;
            }
        }

        float healthBefore = healthManager.currentHealth;
        healthManager.TakeDamage(damageAmount);

        Debug.Log($"Player took {damageAmount} damage! Health: {healthBefore} → {healthManager.currentHealth}");

        if (currentPhase == Kingdom4Phase.Phase2_Wagon)
        {
            Kingdom4ScoreManager.Instance?.WagonHitAllergen();
        }
    }

    void PlayAllergenDamageEffects()
    {
        AudioClip sound = damageSound ?? itemData?.collectSound;
        if (sound != null)
        {
            PlaySoundAtPosition(sound, transform.position, allergenVolume, 1.0f);
        }

        if (damageParticles != null)
        {
            ParticleSystem particles = Instantiate(damageParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
    }

    void PlayShieldBlockEffect()
    {
        AudioClip sound = shieldActivationSound ?? itemData?.collectSound;
        if (sound != null)
        {
            PlaySoundAtPosition(sound, transform.position, powerupVolume, 1.0f);
        }

        ParticleSystem particles = shieldActivationParticles ?? itemData?.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }

    void HandlePowerupCollection()
    {
        if (itemData.itemType == SpawnableItemData.ItemType.Shield)
        {
            Debug.Log("🛡️ Shield collected! Activating shield for 5 seconds...");
            AttachShieldParticlesToKart();
            ActivateShield();
        }
        else if (itemData.itemType == SpawnableItemData.ItemType.Heart)
        {
            Debug.Log("❤️ Heart collected! Healing player...");
            AttachHeartParticlesToKart();

            PlayerHealthManager healthManager = PlayerHealthManager.Instance;
            if (healthManager == null)
            {
                healthManager = playerObject?.GetComponent<PlayerHealthManager>();
            }

            if (healthManager != null)
            {
                float healthBefore = healthManager.currentHealth;
                healthManager.Heal(1);
                Debug.Log($"Healed! Health: {healthBefore} → {healthManager.currentHealth}");
            }
            else
            {
                Debug.LogError("PlayerHealthManager component not found!");
            }
        }
        else
        {
            Debug.LogError($"Unknown powerup type: {itemData.itemType}");
        }
    }

    void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        GameObject tempAudioObj = new GameObject("TempAudio_Damage");
        tempAudioObj.transform.position = position;
        AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
        tempAudioSource.clip = clip;
        tempAudioSource.volume = volume;
        tempAudioSource.pitch = pitch;
        tempAudioSource.spatialBlend = 1f;
        tempAudioSource.minDistance = 1f;
        tempAudioSource.maxDistance = 20f;
        tempAudioSource.Play();

        Destroy(tempAudioObj, clip.length + 0.1f);
    }

    void AttachShieldParticlesToKart()
    {
        if (shieldPowerupParticles == null) return;

        RemoveShieldParticles();

        GameObject kart = FindKartObject();
        if (kart == null) return;

        currentShieldParticles = Instantiate(shieldPowerupParticles);
        AttachParticlesToKart(currentShieldParticles, kart, particleOffset);
    }

    void AttachHeartParticlesToKart()
    {
        if (heartPowerupParticles == null) return;

        RemoveHeartParticles();

        GameObject kart = FindKartObject();
        if (kart == null) return;

        currentHeartParticles = Instantiate(heartPowerupParticles);
        AttachParticlesToKart(currentHeartParticles, kart, particleOffset);

        heartParticlesEndTime = Time.time + 5f;
        StartHeartParticleExpirationCheck();
    }

    void StartHeartParticleExpirationCheck()
    {
        if (heartParticleCheckCoroutine != null)
        {
            StopCoroutine(heartParticleCheckCoroutine);
        }

        GameObject heartParticleManager = new GameObject("HeartParticleTimerManager");
        DontDestroyOnLoad(heartParticleManager);
        HeartParticleTimerManager timerManager = heartParticleManager.AddComponent<HeartParticleTimerManager>();
        heartParticleCheckCoroutine = timerManager.StartCoroutine(CheckHeartParticleExpirationCoroutine());
    }

    IEnumerator CheckHeartParticleExpirationCoroutine()
    {
        while (Time.time < heartParticlesEndTime)
        {
            yield return null;
        }

        Debug.Log("❤️ Heart particles EXPIRED after 5 seconds!");
        RemoveHeartParticles();
        heartParticlesEndTime = 0f;

        GameObject timerManager = GameObject.Find("HeartParticleTimerManager");
        if (timerManager != null)
        {
            Destroy(timerManager);
        }
    }

    GameObject FindKartObject()
    {
        GameObject kart = GameObject.FindGameObjectWithTag("Player");

        if (kart == null)
        {
            KartCollisionHandler kartHandler = FindAnyObjectByType<KartCollisionHandler>();
            if (kartHandler != null)
            {
                kart = kartHandler.gameObject;
            }
        }

        if (kart == null && playerObject != null)
        {
            kart = playerObject;
        }

        return kart;
    }

    void AttachParticlesToKart(GameObject particles, GameObject kart, Vector3 offset)
    {
        ParticleFollower follower = particles.AddComponent<ParticleFollower>();
        follower.target = kart.transform;
        follower.offset = offset;
        follower.followSpeed = 20f;
        follower.rotateWithTarget = false;

        particles.transform.SetParent(kart.transform);
        particles.transform.localPosition = offset;
        particles.transform.localRotation = Quaternion.identity;
    }

    static void RemoveShieldParticles()
    {
        if (currentShieldParticles != null)
        {
            ParticleSystem ps = currentShieldParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                float destroyDelay = ps.main.duration;
                Destroy(currentShieldParticles, destroyDelay);
            }
            else
            {
                Destroy(currentShieldParticles);
            }

            currentShieldParticles = null;
        }
    }

    static void RemoveHeartParticles()
    {
        if (currentHeartParticles != null)
        {
            ParticleSystem ps = currentHeartParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                float destroyDelay = ps.main.duration;
                Destroy(currentHeartParticles, destroyDelay);
            }
            else
            {
                Destroy(currentHeartParticles);
            }

            currentHeartParticles = null;

            GameObject timerManager = GameObject.Find("HeartParticleTimerManager");
            if (timerManager != null)
            {
                Destroy(timerManager);
            }
        }
    }

    void ActivateShield()
    {
        shieldEndTime = Time.time + shieldDuration;
        isShieldActiveGlobal = true;

        ApplyShieldVisual(true);
        StartShieldExpirationCheck();
        PlayShieldActivationEffects();
    }

    void StartShieldExpirationCheck()
    {
        if (shieldCheckCoroutine != null)
        {
            StopCoroutine(shieldCheckCoroutine);
        }

        GameObject shieldManager = new GameObject("ShieldTimerManager");
        DontDestroyOnLoad(shieldManager);
        ShieldTimerManager timerManager = shieldManager.AddComponent<ShieldTimerManager>();
        shieldCheckCoroutine = timerManager.StartCoroutine(CheckShieldExpirationCoroutine());
    }

    IEnumerator CheckShieldExpirationCoroutine()
    {
        while (Time.time < shieldEndTime && isShieldActiveGlobal)
        {
            yield return null;
        }

        if (isShieldActiveGlobal && Time.time >= shieldEndTime)
        {
            Debug.Log("🛡️ Shield EXPIRED after 5 seconds!");
            DeactivateShield();
        }

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
                if (originalKartMaterial == null)
                {
                    originalKartMaterial = kartRenderer.material;
                }

                if (shieldMaterial != null)
                {
                    kartRenderer.material = shieldMaterial;
                }
                else
                {
                    kartRenderer.material.color = Color.cyan;
                }
            }
            else
            {
                if (originalKartMaterial != null)
                {
                    kartRenderer.material = originalKartMaterial;
                }
            }
        }
    }

    void PlayShieldActivationEffects()
    {
        AudioClip sound = shieldActivationSound ?? itemData?.collectSound;
        if (sound != null)
        {
            PlaySoundAtPosition(sound, transform.position, powerupVolume, 1.0f);
        }

        ParticleSystem particles = shieldActivationParticles ?? itemData?.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }

    void PlayVisualCollectionEffects()
    {
        ParticleSystem particles = overrideCollectParticles ?? itemData?.collectParticles;
        if (particles != null)
        {
            ParticleSystem instance = Instantiate(particles, transform.position, Quaternion.identity);
            instance.Play();
            Destroy(instance.gameObject, instance.main.duration);
        }
    }

    public static bool IsShieldActive()
    {
        return isShieldActiveGlobal && Time.time < shieldEndTime;
    }

    public static void DeactivateShield()
    {
        isShieldActiveGlobal = false;
        shieldEndTime = 0f;

        GameObject kart = GameObject.FindGameObjectWithTag("Player") ??
                         FindAnyObjectByType<KartCollisionHandler>()?.gameObject;
        if (kart != null && originalKartMaterial != null)
        {
            Renderer kartRenderer = kart.GetComponent<Renderer>();
            if (kartRenderer != null)
            {
                kartRenderer.material = originalKartMaterial;
            }
        }

        RemoveShieldParticles();
    }

    // Simple ParticleFollower class
    public class ParticleFollower : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        public float followSpeed = 10f;
        public bool rotateWithTarget = false;

        void Update()
        {
            if (target != null)
            {
                Vector3 targetPosition = target.position + offset;
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

                if (rotateWithTarget)
                {
                    transform.rotation = target.rotation;
                }
            }
        }
    }
}

// Helper classes
public class ShieldTimerManager : MonoBehaviour { }
public class HeartParticleTimerManager : MonoBehaviour { }