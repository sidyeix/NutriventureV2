using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public Transform spawnPoint; // The exact spawn position (if null, uses this transform)
    public bool activateOnTouch = true; // Automatically activate when player touches
    public bool isStartCheckpoint = false; // Is this the starting checkpoint?

    [Header("Visual Feedback")]
    public GameObject inactiveVisual;
    public GameObject activeVisual;
    public ParticleSystem activationParticles;
    public AudioClip activationSound;

    [Header("Respawn Settings")]
    public float respawnFacingAngle = 0f; // Direction player faces when respawning

    private bool isActivated = false;

    private void Start()
    {
        // If no spawn point specified, use this transform
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        // Initialize visuals
        UpdateVisuals();

        // If this is the start checkpoint, activate it immediately
        if (isStartCheckpoint)
        {
            Activate();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activateOnTouch && other.CompareTag("Player") && !isActivated)
        {
            Activate();
        }
    }

    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;

        // Register with GameManager
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.SetCurrentCheckpoint(this);
        }

        // Play visual effects
        if (activationParticles != null)
        {
            activationParticles.Play();
        }

        // Play sound
        if (activationSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(activationSound);
        }

        UpdateVisuals();

#if UNITY_EDITOR
        Debug.Log($"Checkpoint activated: {gameObject.name}");
#endif
    }

    private void UpdateVisuals()
    {
        if (inactiveVisual != null)
        {
            inactiveVisual.SetActive(!isActivated);
        }

        if (activeVisual != null)
        {
            activeVisual.SetActive(isActivated);
        }
    }

    public Vector3 GetSpawnPosition()
    {
        return spawnPoint.position;
    }

    public Quaternion GetSpawnRotation()
    {
        return Quaternion.Euler(0f, respawnFacingAngle, 0f);
    }

    public bool IsActivated() => isActivated;

    // Reset for game restart
    public void ResetCheckpoint()
    {
        if (!isStartCheckpoint)
        {
            isActivated = false;
            UpdateVisuals();
        }
    }
}