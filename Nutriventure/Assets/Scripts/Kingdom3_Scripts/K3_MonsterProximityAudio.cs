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

    // REMOVED: Audio Source Settings section - using AudioHandler instead

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
    private float detectionRangeSqr;
    private float proximityCheckTimer;
    private const float PROXIMITY_CHECK_INTERVAL = 0.25f;

    void Start()
    {
        // Cache squared range
        detectionRangeSqr = detectionRange * detectionRange;
        // REMOVED: InitializeAudioSystem() - no local AudioSource needed
        FindPlayer();

        if (showDebugMessages)
        {
            Debug.Log($"Monster proximity audio initialized. Range: {detectionRange}m");
            Debug.Log($"Sound: {(proximitySound != null ? proximitySound.name : "Not assigned")}");
        }

        // Check AudioHandler exists
        if (AudioHandler.Instance == null)
        {
            Debug.LogWarning("AudioHandler.Instance not found! Make sure AudioHandler is in the scene.");
        }
    }

    void Update()
    {
        if (player == null || proximitySound == null)
            return;

        // Throttle distance checks
        proximityCheckTimer += Time.deltaTime;
        if (proximityCheckTimer < PROXIMITY_CHECK_INTERVAL) return;
        proximityCheckTimer = 0f;

        // Use sqrMagnitude to avoid sqrt
        float sqrDistance = (transform.position - player.transform.position).sqrMagnitude;

        // Check if player is within range
        if (sqrDistance <= detectionRangeSqr)
        {
            // Convert back to distance only when needed for volume calculation
            float distance = Mathf.Sqrt(sqrDistance);
            HandleProximityAudio(distance);
        }
        else if (isPlaying)
        {
            // Player left range, stop audio
            isPlaying = false;
        }
    }

    // REMOVED: InitializeAudioSystem() method

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
        // For triggered audio, play at intervals
        if (Time.time >= nextSoundTime && !isPlaying && AudioHandler.Instance != null)
        {
            PlayProximitySound(distance);

            // Schedule next sound
            nextSoundTime = Time.time + Random.Range(minSoundInterval, maxSoundInterval);
        }
    }

    // CHANGED: Using AudioHandler instead of local AudioSource
    private void PlayProximitySound(float distance)
    {
        if (proximitySound == null || AudioHandler.Instance == null) return;

        // Calculate volume based on distance if enabled
        float finalVolume = soundVolume;
        if (distanceBasedVolume)
        {
            float volumeMultiplier = 1f - Mathf.Clamp01(distance / detectionRange);
            finalVolume = Mathf.Lerp(minVolumeAtRange, soundVolume, volumeMultiplier);
        }

        // Play the sound through AudioHandler
        AudioHandler.Instance.PlayCharacterSelectionSound(proximitySound);
        isPlaying = true;

        if (showDebugMessages)
        {
            Debug.Log($"Playing proximity sound at {finalVolume:F2} volume (through AudioHandler)");
            Debug.Log($"Distance to player: {distance:F1}m");
        }
    }

    // REMOVED: StopProximityAudio() method - no need to stop anything

    [ContextMenu("Test Proximity Sound")]
    public void TestProximitySound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }

        if (proximitySound == null || AudioHandler.Instance == null)
        {
            Debug.LogError("Cannot test: proximity sound not assigned or AudioHandler missing!");
            return;
        }

        Debug.Log("=== TESTING PROXIMITY SOUND ===");
        AudioHandler.Instance.PlayCharacterSelectionSound(proximitySound);
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
        Debug.Log($"AudioHandler.Instance: {(AudioHandler.Instance != null ? "Ready" : "Missing")}");
        Debug.Log($"Detection Range: {detectionRange}m");
        Debug.Log($"Sound Interval: {minSoundInterval}-{maxSoundInterval}s");
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