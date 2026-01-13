using UnityEngine;

public class K3_MonsterProximityAudio : MonoBehaviour
{
    [Header("Proximity Audio Settings")]
    [Tooltip("Audio clip to play when player is in proximity")]
    public AudioClip proximitySound;
    
    [Tooltip("Detection range for proximity audio")]
    public float detectionRange = 12f;
    
    [Tooltip("Minimum time between sounds")]
    public float minSoundInterval = 3f;
    
    [Tooltip("Maximum time between sounds")]
    public float maxSoundInterval = 8f;
    
    [Tooltip("Volume of proximity sound")]
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;
    
    [Tooltip("Should the sound volume fade with distance?")]
    public bool distanceBasedVolume = true;
    
    [Tooltip("Minimum volume at max distance (if distanceBasedVolume is true)")]
    [Range(0f, 1f)]
    public float minVolumeAtRange = 0.2f;
    
    [Header("Audio Source Settings")]
    [Tooltip("Audio source to use (if not assigned, will create one)")]
    public AudioSource audioSource;
    
    [Tooltip("Loop the proximity sound?")]
    public bool loopSound = false;
    
    [Tooltip("Spatial blend (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    public float spatialBlend = 1f;
    
    [Header("Debug")]
    [Tooltip("Show debug messages in console")]
    public bool showDebugMessages = true;
    
    [Tooltip("Draw detection range gizmo in editor")]
    public bool drawGizmos = true;
    
    [Tooltip("Color for range gizmo")]
    public Color gizmoColor = new Color(0.5f, 0f, 0.5f, 0.3f); // Purple
    
    private GameObject player;
    private float nextSoundTime;
    private bool isPlaying;
    
    void Start()
    {
        InitializeAudioSystem();
        FindPlayer();
        
        if (showDebugMessages)
        {
            Debug.Log($"Monster proximity audio initialized. Range: {detectionRange}m");
            Debug.Log($"Sound: {(proximitySound != null ? proximitySound.name : "Not assigned")}");
            Debug.Log($"Audio Source: {(audioSource != null ? "Ready" : "Missing")}");
        }
    }
    
    void Update()
    {
        if (player == null || proximitySound == null || audioSource == null)
            return;
        
        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        
        // Check if player is within range
        if (distanceToPlayer <= detectionRange)
        {
            HandleProximityAudio(distanceToPlayer);
        }
        else if (isPlaying)
        {
            // Player left range, stop audio
            StopProximityAudio();
        }
    }
    
    private void InitializeAudioSystem()
    {
        // Get or create AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                if (showDebugMessages) Debug.Log("Created AudioSource component");
            }
        }
        
        // Configure AudioSource
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = spatialBlend;
            audioSource.loop = loopSound;
            audioSource.volume = soundVolume;
            
            if (proximitySound != null)
            {
                audioSource.clip = proximitySound;
            }
        }
        
        // Set initial next sound time
        nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
    }
    
    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null && showDebugMessages)
        {
            Debug.LogWarning("No GameObject with 'Player' tag found!");
        }
        else if (showDebugMessages)
        {
            Debug.Log($"Player found: {player.name}");
        }
    }
    
    private void HandleProximityAudio(float distance)
    {
        if (loopSound)
        {
            HandleLoopingAudio(distance);
        }
        else
        {
            HandleTriggeredAudio(distance);
        }
    }
    
    private void HandleLoopingAudio(float distance)
    {
        // For looping audio, adjust volume based on distance
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
            isPlaying = true;
            if (showDebugMessages) Debug.Log("Started looping proximity audio");
        }
        
        // Adjust volume based on distance if enabled
        if (distanceBasedVolume)
        {
            float volumeMultiplier = 1f - Mathf.Clamp01(distance / detectionRange);
            float targetVolume = Mathf.Lerp(minVolumeAtRange, soundVolume, volumeMultiplier);
            audioSource.volume = targetVolume;
        }
    }
    
    private void HandleTriggeredAudio(float distance)
    {
        // For triggered audio, play at intervals
        if (Time.time >= nextSoundTime && !audioSource.isPlaying)
        {
            PlayProximitySound(distance);
            
            // Schedule next sound
            nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
        }
    }
    
    private void PlayProximitySound(float distance)
    {
        if (audioSource == null || proximitySound == null) return;
        
        // Calculate volume based on distance if enabled
        float finalVolume = soundVolume;
        if (distanceBasedVolume)
        {
            float volumeMultiplier = 1f - Mathf.Clamp01(distance / detectionRange);
            finalVolume = Mathf.Lerp(minVolumeAtRange, soundVolume, volumeMultiplier);
        }
        
        // Play the sound
        audioSource.PlayOneShot(proximitySound, finalVolume);
        isPlaying = true;
        
        if (showDebugMessages)
        {
            Debug.Log($"Playing proximity sound at {finalVolume:F2} volume");
            Debug.Log($"Distance to player: {distance:F1}m");
        }
    }
    
    private void StopProximityAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            isPlaying = false;
            
            if (showDebugMessages) Debug.Log("Stopped proximity audio (player out of range)");
        }
    }
    
    [ContextMenu("Test Proximity Sound")]
    public void TestProximitySound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        if (proximitySound == null || audioSource == null)
        {
            Debug.LogError("Cannot test: proximity sound or audio source not assigned!");
            return;
        }
        
        Debug.Log("=== TESTING PROXIMITY SOUND ===");
        audioSource.PlayOneShot(proximitySound, soundVolume);
        isPlaying = true;
    }
    
    [ContextMenu("Force Find Player")]
    public void ForceFindPlayer()
    {
        FindPlayer();
        if (player != null)
        {
            Debug.Log($"Player found: {player.name}");
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.Log($"Distance to player: {distance:F1}m");
            Debug.Log($"Within range ({detectionRange}m): {distance <= detectionRange}");
        }
    }
    
    [ContextMenu("Debug Audio Status")]
    public void DebugAudioStatus()
    {
        Debug.Log("=== PROXIMITY AUDIO STATUS ===");
        Debug.Log($"Player: {(player != null ? player.name : "Not found")}");
        Debug.Log($"Proximity Sound: {(proximitySound != null ? proximitySound.name : "Not assigned")}");
        Debug.Log($"Audio Source: {(audioSource != null ? "Ready" : "Missing")}");
        Debug.Log($"Detection Range: {detectionRange}m");
        Debug.Log($"Sound Interval: {minSoundInterval}-{maxSoundInterval}s");
        Debug.Log($"Loop Sound: {loopSound}");
        Debug.Log($"Distance Based Volume: {distanceBasedVolume}");
        Debug.Log($"Is Playing: {isPlaying}");
        Debug.Log($"Next Sound Time: {(nextSoundTime - Time.time):F1}s");
        
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            Debug.Log($"Distance to Player: {distance:F1}m");
            Debug.Log($"Player in Range: {distance <= detectionRange}");
        }
    }
    
    // Draw detection range in editor
    private void OnDrawGizmosSelected()
    {
        if (drawGizmos)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a * 0.5f);
            Gizmos.DrawSphere(transform.position, detectionRange);
        }
    }
}