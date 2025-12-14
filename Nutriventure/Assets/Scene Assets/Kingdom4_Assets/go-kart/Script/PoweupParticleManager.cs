using UnityEngine;
using System.Collections;

public class PowerupParticleManager : MonoBehaviour
{
    [Header("Particle Prefabs")]
    public GameObject shieldParticlePrefab;
    public GameObject heartParticlePrefab;
    
    [Header("Attachment Settings")]
    public Vector3 shieldOffset = new Vector3(0, 1.5f, 0);
    public Vector3 heartOffset = new Vector3(0, 1f, 0);
    public float heartParticleDuration = 2f;
    
    [Header("References")]
    public Transform kartTransform;
    
    private GameObject currentShieldParticles;
    private GameObject currentHeartParticles;
    private Coroutine heartParticleCoroutine;
    
    void Start()
    {
        if (kartTransform == null)
        {
            kartTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (kartTransform == null)
            {
                kartTransform = FindAnyObjectByType<KartCollisionHandler>()?.transform;
            }
        }
    }
    
    public void AttachShieldParticles()
    {
        if (shieldParticlePrefab == null)
        {
            Debug.LogWarning("Shield particle prefab not assigned!");
            return;
        }
        
        // Remove existing shield particles
        RemoveShieldParticles();
        
        // Create new shield particles
        currentShieldParticles = Instantiate(shieldParticlePrefab);
        
        // Attach to kart
        if (kartTransform != null)
        {
            currentShieldParticles.transform.SetParent(kartTransform);
            currentShieldParticles.transform.localPosition = shieldOffset;
            currentShieldParticles.transform.localRotation = Quaternion.identity;
            
            // Add particle follower for smooth movement
            ParticleFollower follower = currentShieldParticles.AddComponent<ParticleFollower>();
            follower.target = kartTransform;
            follower.offset = shieldOffset;
            follower.followSpeed = 20f;
            follower.rotateWithTarget = false;
            
            Debug.Log($"🛡️ Shield particles attached to kart");
        }
        else
        {
            Debug.LogError("No kart transform found for shield particles!");
            Destroy(currentShieldParticles);
        }
    }
    
    public void AttachHeartParticles()
    {
        if (heartParticlePrefab == null)
        {
            Debug.LogWarning("Heart particle prefab not assigned!");
            return;
        }
        
        // Remove existing heart particles
        RemoveHeartParticles();
        
        // Create new heart particles
        currentHeartParticles = Instantiate(heartParticlePrefab);
        
        // Attach to kart
        if (kartTransform != null)
        {
            currentHeartParticles.transform.SetParent(kartTransform);
            currentHeartParticles.transform.localPosition = heartOffset;
            currentHeartParticles.transform.localRotation = Quaternion.identity;
            
            // Add particle follower for smooth movement
            ParticleFollower follower = currentHeartParticles.AddComponent<ParticleFollower>();
            follower.target = kartTransform;
            follower.offset = heartOffset;
            follower.followSpeed = 15f;
            follower.rotateWithTarget = false;
            
            Debug.Log($"❤️ Heart particles attached to kart");
            
            // Schedule removal
            heartParticleCoroutine = StartCoroutine(RemoveHeartParticlesAfterDelay(heartParticleDuration));
        }
        else
        {
            Debug.LogError("No kart transform found for heart particles!");
            Destroy(currentHeartParticles);
        }
    }
    
    public void RemoveShieldParticles()
    {
        if (currentShieldParticles != null)
        {
            // Stop particles gracefully
            ParticleSystem ps = currentShieldParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(currentShieldParticles, ps.main.duration);
            }
            else
            {
                Destroy(currentShieldParticles);
            }
            
            currentShieldParticles = null;
            Debug.Log("🛡️ Shield particles removed");
        }
    }
    
    public void RemoveHeartParticles()
    {
        if (heartParticleCoroutine != null)
        {
            StopCoroutine(heartParticleCoroutine);
            heartParticleCoroutine = null;
        }
        
        if (currentHeartParticles != null)
        {
            // Stop particles gracefully
            ParticleSystem ps = currentHeartParticles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(currentHeartParticles, ps.main.duration);
            }
            else
            {
                Destroy(currentHeartParticles);
            }
            
            currentHeartParticles = null;
            Debug.Log("❤️ Heart particles removed");
        }
    }
    
    IEnumerator RemoveHeartParticlesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveHeartParticles();
    }
    
    // Call this when shield expires
    public void OnShieldExpired()
    {
        RemoveShieldParticles();
    }
    
    // Clean up on destroy
    void OnDestroy()
    {
        RemoveShieldParticles();
        RemoveHeartParticles();
    }
}