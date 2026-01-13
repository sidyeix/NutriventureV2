using UnityEngine;
using UnityEngine.UI;

public class K3_DeathplaneFall : MonoBehaviour
{
    [Header("DEATH PLANE SETUP")]
    [Tooltip("Drag your DeathPlane GameObject here")]
    public GameObject deathPlaneObject;
    [Tooltip("Make sure this matches your player's tag")]
    public string playerTag = "Player";
    
    [Header("RESPAWN SETTINGS")]
    [Tooltip("Drag your respawn point GameObject here")]
    public GameObject respawnPointObject;
    [Tooltip("How much health to lose when falling")]
    public int healthDamage = 1;
    [Tooltip("Time before respawn happens")]
    public float respawnDelay = 0.5f;
    
    [Header("VISUAL FEEDBACK")]
    [Tooltip("Drag your DamagePanel UI Image here")]
    public Image damagePanel;
    [Tooltip("How long the damage panel stays visible")]
    public float damagePanelDuration = 1f;
    [Tooltip("Color when damaged")]
    public Color damageColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
    
    [Header("RESPAWN VISUAL EFFECTS")]
    [Tooltip("Particle system at respawn point (initially disabled)")]
    public ParticleSystem respawnParticles;
    [Tooltip("Duration for respawn particle effect")]
    public float particleDuration = 2f;
    [Tooltip("Particle outro animation duration")]
    public float particleOutroDuration = 0.5f;
    
    [Header("AUDIO FEEDBACK")]
    [Tooltip("Sound when player drowns (plays with damage panel)")]
    public AudioClip drownedSFX;
    [Tooltip("Volume for drowned SFX")]
    [Range(0f, 2f)]
    public float drownedVolume = 2f;
    [Tooltip("Sound when player respawns")]
    public AudioClip respawnSFX;
    [Tooltip("Volume for respawn SFX")]
    [Range(0f, 2f)]
    public float respawnVolume = 2;
    public float soundVolume = 1f;
    
    [Header("PLAYER REFERENCES")]
    [Tooltip("Drag your Player GameObject here")]
    public GameObject playerObject;
    [Tooltip("Player's health script - will auto-find if not assigned")]
    public PreserviaPlayerStat playerHealth;
    
    [Header("DEBUG OPTIONS")]
    public bool showDebugMessages = true;
    public bool drawDebugGizmos = true;
    public Color gizmoColor = Color.red;
    
    // Private variables
    private BoxCollider deathCollider;
    private AudioSource audioSource;
    private Color originalPanelColor;
    private bool isRespawning = false;
    private ParticleSystem activeRespawnParticles;
    
    void Start()
    {
        SetupDeathPlane();
        SetupAudio();
        SetupDamagePanel();
        FindPlayerComponents();
        SetupParticles();
        
        if (showDebugMessages)
        {
            Debug.Log("K3_DeathplaneFall initialized successfully!");
            Debug.Log($"Death Plane: {deathPlaneObject?.name ?? "Not assigned"}");
            Debug.Log($"Respawn Point: {respawnPointObject?.name ?? "Not assigned"}");
            Debug.Log($"Player: {playerObject?.name ?? "Not found"}");
        }
    }
    
    void SetupDeathPlane()
    {
        // Make sure we have a death plane object
        if (deathPlaneObject == null)
        {
            Debug.LogError("DEATH PLANE OBJECT NOT ASSIGNED! Please drag your DeathPlane GameObject to the inspector.");
            this.enabled = false;
            return;
        }
        
        // Get or add BoxCollider
        deathCollider = deathPlaneObject.GetComponent<BoxCollider>();
        if (deathCollider == null)
        {
            deathCollider = deathPlaneObject.AddComponent<BoxCollider>();
            if (showDebugMessages) Debug.Log("Added BoxCollider to DeathPlane");
        }
        
        // Ensure it's a trigger
        deathCollider.isTrigger = true;
        
        // Make sure the death plane object is active
        deathPlaneObject.SetActive(true);
        
        // Add this script to the death plane if it's not already there
        if (deathPlaneObject.GetComponent<K3_DeathplaneFall>() == null && deathPlaneObject != this.gameObject)
        {
            deathPlaneObject.AddComponent<K3_DeathplaneFall>();
            Debug.LogWarning("Added K3_DeathplaneFall script to DeathPlane object. You should remove this duplicate script.");
        }
    }
    
    void SetupAudio()
    {
        // Create audio source on this object
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
    }
    
    void SetupDamagePanel()
    {
        if (damagePanel != null)
        {
            originalPanelColor = damagePanel.color;
            damagePanel.gameObject.SetActive(false);
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("Damage panel not assigned. No visual feedback will appear.");
        }
    }
    
    void SetupParticles()
    {
        if (respawnParticles != null)
        {
            // Make sure particle system is initially disabled
            respawnParticles.gameObject.SetActive(false);
            
            if (showDebugMessages)
            {
                Debug.Log("Respawn particles initialized (initially disabled)");
            }
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("No respawn particle system assigned.");
        }
    }
    
    void FindPlayerComponents()
    {
        // Find player object if not assigned
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null && showDebugMessages)
            {
                Debug.LogError($"No GameObject found with tag '{playerTag}'. Make sure your player is tagged correctly.");
            }
        }
        
        // Find player health component
        if (playerHealth == null && playerObject != null)
        {
            playerHealth = playerObject.GetComponent<PreserviaPlayerStat>();
            if (playerHealth == null)
            {
                // Try to find it in children
                playerHealth = playerObject.GetComponentInChildren<PreserviaPlayerStat>();
            }
            
            if (playerHealth == null && showDebugMessages)
            {
                Debug.LogError("PreserviaPlayerStat not found on player! Make sure the health script is attached.");
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if we're already processing a respawn
        if (isRespawning) return;
        
        // Check if it's the player
        if (other.CompareTag(playerTag))
        {
            if (showDebugMessages) Debug.Log($"PLAYER FELL INTO DEATH PLANE: {other.name}");
            
            // Start the death sequence
            HandlePlayerDeath(other.gameObject);
        }
    }
    
    void HandlePlayerDeath(GameObject player)
    {
        isRespawning = true;
        
        // Step 1: Show damage panel and play drowned SFX
        ShowDamagePanel();
        
        // Step 2: Apply damage to player
        ApplyDamage();
        
        // Step 3: Wait and then respawn
        Invoke("RespawnPlayer", respawnDelay);
        
        if (showDebugMessages) Debug.Log("Death sequence started...");
    }
    
    void ShowDamagePanel()
    {
        if (damagePanel != null)
        {
            // Show the panel with damage color
            damagePanel.color = damageColor;
            damagePanel.gameObject.SetActive(true);
            
            // Play drowned SFX
            PlayDrownedSFX();
            
            // Hide it after duration
            Invoke("HideDamagePanel", damagePanelDuration);
            
            if (showDebugMessages) Debug.Log("Damage panel activated");
        }
        else
        {
            // Still play sound even if no panel
            PlayDrownedSFX();
        }
    }
    
    void HideDamagePanel()
    {
        if (damagePanel != null)
        {
            damagePanel.gameObject.SetActive(false);
            damagePanel.color = originalPanelColor;
            
            if (showDebugMessages) Debug.Log("Damage panel hidden");
        }
    }
    
    void PlayDrownedSFX()
    {
        if (drownedSFX != null && audioSource != null)
        {
            // Clamp volume between 0 and 2 for safety
            float clampedVolume = Mathf.Clamp(drownedVolume, 0f, 2f);
            audioSource.PlayOneShot(drownedSFX, clampedVolume);
            
            if (showDebugMessages) 
            {
                Debug.Log($"Played drowned SFX at volume: {clampedVolume}");
            }
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("No drowned SFX assigned or no audio source");
        }
    }
    
    void PlayRespawnSFX()
    {
        if (respawnSFX != null && audioSource != null)
        {
            // Clamp volume between 0 and 2 for safety
            float clampedVolume = Mathf.Clamp(respawnVolume, 0f, 2f);
            audioSource.PlayOneShot(respawnSFX, clampedVolume);
            
            if (showDebugMessages) 
            {
                Debug.Log($"Played respawn SFX at volume: {clampedVolume}");
            }
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("No respawn SFX assigned or no audio source");
        }
    }
    
    void ApplyDamage()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(healthDamage);
            
            if (showDebugMessages) 
            {
                Debug.Log($"Player took {healthDamage} damage. Health: {playerHealth.currentHealth}/{playerHealth.maxHealth}");
            }
        }
        else
        {
            if (showDebugMessages) Debug.LogError("Cannot apply damage - player health script not found!");
        }
    }
    
    void RespawnPlayer()
    {
        if (showDebugMessages) Debug.Log("Respawning player...");
        
        // Make sure we have a player
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject == null)
            {
                Debug.LogError("Player not found for respawn!");
                isRespawning = false;
                return;
            }
        }
        
        // Make sure we have a respawn point
        if (respawnPointObject == null)
        {
            Debug.LogError("No respawn point assigned!");
            isRespawning = false;
            return;
        }
        
        // Play respawn SFX
        PlayRespawnSFX();
        
        // Play respawn particle effect
        PlayRespawnParticles();
        
        // Teleport player to respawn point
        playerObject.transform.position = respawnPointObject.transform.position;
        
        // Reset rotation
        playerObject.transform.rotation = respawnPointObject.transform.rotation;
        
        // Reset respawn flag after particle outro
        Invoke("CompleteRespawn", particleDuration);
        
        if (showDebugMessages) 
        {
            Debug.Log($"Player respawned at: {respawnPointObject.transform.position}");
            Debug.Log("Respawn sequence in progress...");
        }
    }
    
    void PlayRespawnParticles()
    {
        if (respawnParticles != null && respawnPointObject != null)
        {
            // Create a copy of the particle system at respawn point
            activeRespawnParticles = Instantiate(respawnParticles, respawnPointObject.transform.position, Quaternion.identity);
            activeRespawnParticles.gameObject.SetActive(true);
            
            // Play the particle system
            activeRespawnParticles.Play();
            
            // Start outro animation after main duration
            Invoke("StartParticleOutro", particleDuration - particleOutroDuration);
            
            // Destroy after complete duration
            Destroy(activeRespawnParticles.gameObject, particleDuration + 0.1f);
            
            if (showDebugMessages) 
            {
                Debug.Log($"Respawn particles started for {particleDuration} seconds");
            }
        }
        else if (showDebugMessages)
        {
            Debug.LogWarning("Cannot play respawn particles - system not assigned or no respawn point");
        }
    }
    
    void StartParticleOutro()
    {
        if (activeRespawnParticles != null)
        {
            // Stop emitting new particles (start outro)
            var emission = activeRespawnParticles.emission;
            emission.enabled = false;
            
            if (showDebugMessages) 
            {
                Debug.Log("Particle outro animation started");
            }
        }
    }
    
    void CompleteRespawn()
    {
        isRespawning = false;
        
        if (showDebugMessages) 
        {
            Debug.Log("Respawn sequence complete!");
        }
    }
    
    // ===== PUBLIC METHODS FOR TESTING AND CONTROL =====
    
    [ContextMenu("Test Death Fall")]
    public void TestDeathFall()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        GameObject testPlayer = GameObject.FindGameObjectWithTag(playerTag);
        if (testPlayer != null)
        {
            Debug.Log("=== TESTING DEATH FALL ===");
            HandlePlayerDeath(testPlayer);
        }
        else
        {
            Debug.LogError("No player found to test with!");
        }
    }
    
    [ContextMenu("Test Respawn Only")]
    public void TestRespawnOnly()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING RESPAWN ONLY ===");
        RespawnPlayer();
    }
    
    [ContextMenu("Force Respawn")]
    public void ForceRespawn()
    {
        RespawnPlayer();
    }
    
    [ContextMenu("Test Particle Effect")]
    public void TestParticleEffect()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        if (respawnParticles != null && respawnPointObject != null)
        {
            Debug.Log("=== TESTING PARTICLE EFFECT ===");
            PlayRespawnParticles();
        }
        else
        {
            Debug.LogError("Cannot test particles - respawnParticles or respawnPointObject not assigned!");
        }
    }
    
    public void SetNewRespawnPoint(GameObject newRespawnPoint)
    {
        respawnPointObject = newRespawnPoint;
        
        if (showDebugMessages) 
        {
            Debug.Log($"Respawn point changed to: {newRespawnPoint.name}");
        }
    }
    
    // ===== DEBUG VISUALIZATION =====
    
    void OnDrawGizmos()
    {
        if (!drawDebugGizmos || deathCollider == null) return;
        
        Gizmos.color = gizmoColor;
        
        // Draw the death plane collider
        if (deathCollider.enabled)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = deathCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(deathCollider.center, deathCollider.size);
            Gizmos.matrix = oldMatrix;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (deathCollider != null)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = deathCollider.transform.localToWorldMatrix;
            Gizmos.DrawCube(deathCollider.center, deathCollider.size);
            Gizmos.matrix = oldMatrix;
        }
        
        // Draw line from death plane to respawn point
        if (deathPlaneObject != null && respawnPointObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(deathPlaneObject.transform.position, respawnPointObject.transform.position);
            
            // Draw respawn point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPointObject.transform.position, 0.5f);
        }
    }
    
    [ContextMenu("Debug System Status")]
    public void DebugSystemStatus()
    {
        Debug.Log("=== K3_DEATHPLANEFALL STATUS ===");
        Debug.Log($"Script Enabled: {this.enabled}");
        Debug.Log($"Is Respawning: {isRespawning}");
        Debug.Log($"");
        Debug.Log($"DEATH PLANE:");
        Debug.Log($"- Object: {deathPlaneObject?.name ?? "NULL"}");
        Debug.Log($"- Has Collider: {deathCollider != null}");
        if (deathCollider != null)
        {
            Debug.Log($"- Is Trigger: {deathCollider.isTrigger}");
            Debug.Log($"- Collider Enabled: {deathCollider.enabled}");
        }
        Debug.Log($"");
        Debug.Log($"RESPAWN POINT:");
        Debug.Log($"- Object: {respawnPointObject?.name ?? "NULL"}");
        if (respawnPointObject != null)
        {
            Debug.Log($"- Position: {respawnPointObject.transform.position}");
        }
        Debug.Log($"");
        Debug.Log($"PLAYER:");
        Debug.Log($"- Object: {playerObject?.name ?? "NULL"}");
        Debug.Log($"- Tag: {playerObject?.tag ?? "N/A"}");
        Debug.Log($"- Health Script: {playerHealth != null}");
        if (playerHealth != null)
        {
            Debug.Log($"- Current Health: {playerHealth.currentHealth}/{playerHealth.maxHealth}");
        }
        Debug.Log($"");
        Debug.Log($"VISUAL EFFECTS:");
        Debug.Log($"- Damage Panel: {damagePanel?.name ?? "NULL"}");
        Debug.Log($"- Panel Active: {damagePanel?.gameObject.activeInHierarchy ?? false}");
        Debug.Log($"- Respawn Particles: {respawnParticles?.name ?? "NULL"}");
        Debug.Log($"- Active Particles: {activeRespawnParticles != null}");
        Debug.Log($"");
        Debug.Log($"AUDIO:");
        Debug.Log($"- Audio Source: {audioSource != null}");
        Debug.Log($"- Drowned SFX: {drownedSFX?.name ?? "NULL"}");
        Debug.Log($"- Respawn SFX: {respawnSFX?.name ?? "NULL"}");
        Debug.Log($"================================");
    }
    
    [ContextMenu("Quick Setup Check")]
    public void QuickSetupCheck()
    {
        Debug.Log("=== QUICK SETUP CHECK ===");
        
        bool allGood = true;
        
        // Check death plane
        if (deathPlaneObject == null)
        {
            Debug.LogError("❌ Death Plane Object not assigned in inspector!");
            allGood = false;
        }
        else
        {
            Debug.Log("✅ Death Plane Object assigned");
            
            BoxCollider collider = deathPlaneObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                Debug.LogError("❌ Death Plane has no BoxCollider!");
                allGood = false;
            }
            else if (!collider.isTrigger)
            {
                Debug.LogError("❌ Death Plane BoxCollider is not set as Trigger!");
                allGood = false;
            }
            else
            {
                Debug.Log("✅ Death Plane collider is properly set up");
            }
        }
        
        // Check respawn point
        if (respawnPointObject == null)
        {
            Debug.LogError("❌ Respawn Point not assigned in inspector!");
            allGood = false;
        }
        else
        {
            Debug.Log("✅ Respawn Point assigned");
        }
        
        // Check player
        if (playerObject == null)
        {
            Debug.LogWarning("⚠️ Player Object not assigned. Will try to find by tag...");
            GameObject foundPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (foundPlayer == null)
            {
                Debug.LogError($"❌ No GameObject found with tag '{playerTag}'!");
                allGood = false;
            }
            else
            {
                Debug.Log($"✅ Found player: {foundPlayer.name}");
            }
        }
        else
        {
            Debug.Log("✅ Player Object assigned");
        }
        
        // Check SFX
        if (drownedSFX == null)
        {
            Debug.LogWarning("⚠️ Drowned SFX not assigned");
        }
        else
        {
            Debug.Log("✅ Drowned SFX assigned");
        }
        
        if (respawnSFX == null)
        {
            Debug.LogWarning("⚠️ Respawn SFX not assigned");
        }
        else
        {
            Debug.Log("✅ Respawn SFX assigned");
        }
        
        // Check particles
        if (respawnParticles == null)
        {
            Debug.LogWarning("⚠️ Respawn Particles not assigned");
        }
        else
        {
            Debug.Log("✅ Respawn Particles assigned");
        }
        
        if (allGood)
        {
            Debug.Log("✅ All checks passed! System should work correctly.");
        }
        else
        {
            Debug.LogError("❌ Some issues found. Please fix them before testing.");
        }
    }
    
    // Clean up when destroyed
    void OnDestroy()
    {
        CancelInvoke();
        
        if (activeRespawnParticles != null)
        {
            Destroy(activeRespawnParticles.gameObject);
        }
    }
}