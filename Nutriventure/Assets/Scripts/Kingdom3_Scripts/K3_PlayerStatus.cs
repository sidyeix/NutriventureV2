using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System.Collections;

public class K3_PlayerStatus : MonoBehaviour
{
    [Header("Damage & Respawn")]
    [Tooltip("UI Image to flash red when player takes damage")]
    public Image damagePanel;
    
    [Tooltip("Respawn point where player will respawn")]
    public Transform respawnPoint;
    
    [Tooltip("Sound to play when player respawns")]
    public AudioClip resurrectionSound;
    
    [Tooltip("Duration of the red flash effect")]
    public float flashDuration = 0.5f;
    
    [Header("Plane Detection")]
    [Tooltip("Assign the specific Plane GameObject that causes player death")]
    public GameObject deathPlane;
    
    private AudioSource audioSource;
    private ThirdPersonController playerController;
    private CharacterController characterController;
    private bool isRespawning = false;
    private float checkInterval = 0.1f; // Check every 0.1 seconds
    private float nextCheckTime = 0f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        playerController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Fallback detection: Check player's Y position if death plane is at a specific height
        // This is a backup method in case collision detection fails
        if (deathPlane != null && Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            // If player is below death plane (assuming death plane is at y=0 or similar)
            if (transform.position.y < deathPlane.transform.position.y)
            {
                Debug.Log("Player fell below death plane!");
                HandlePlayerFall();
            }
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // This is the main method that should work with CharacterController
        Debug.Log($"Controller collided with: {hit.gameObject.name}");
        
        if (deathPlane != null && hit.gameObject == deathPlane)
        {
            Debug.Log("Death plane hit via controller!");
            HandlePlayerFall();
        }
    }

    private void HandlePlayerFall()
    {
        if (isRespawning) return;
        
        Debug.Log("HandlePlayerFall called!");
        isRespawning = true;
        
        // Enable and flash red damage panel
        StartCoroutine(FlashRed());
        
        // Disable player controller temporarily
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable character controller to prevent physics issues
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Respawn after flash
        Invoke(nameof(RespawnPlayer), flashDuration);
    }

    private IEnumerator FlashRed()
    {
        if (damagePanel == null)
        {
            Debug.LogError("Damage panel is not assigned!");
            yield break;
        }
        
        Debug.Log("Starting flash red coroutine");
        
        // Enable the damage panel GameObject
        damagePanel.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        
        // Fade in red
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 0.7f, elapsedTime / (flashDuration / 2));
            damagePanel.color = new Color(1, 0, 0, alpha);
            yield return null;
        }
        
        // Fade out red
        elapsedTime = 0f;
        while (elapsedTime < flashDuration / 2)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0.7f, 0, elapsedTime / (flashDuration / 2));
            damagePanel.color = new Color(1, 0, 0, alpha);
            yield return null;
        }
        
        // Ensure panel is transparent and disable it
        damagePanel.color = new Color(1, 0, 0, 0);
        damagePanel.gameObject.SetActive(false);
        
        Debug.Log("Flash red coroutine completed");
    }

    private void RespawnPlayer()
    {
        Debug.Log("RespawnPlayer called");
        
        // Move player to respawn point
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
            Debug.Log($"Player respawned at: {respawnPoint.position}");
        }
        else
        {
            Debug.LogError("Respawn point is not assigned!");
        }
        
        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }
        
        // Play resurrection sound
        if (resurrectionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resurrectionSound);
        }
        else
        {
            Debug.LogWarning("Resurrection sound or audio source missing!");
        }
        
        // Re-enable player controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        isRespawning = false;
        Debug.Log("Respawn complete");
    }
}