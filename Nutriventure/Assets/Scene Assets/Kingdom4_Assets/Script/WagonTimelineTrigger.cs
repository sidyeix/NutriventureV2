using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.InputSystem;

public class WagonTimelineTrigger : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public GameObject player;
    private bool played = false;
    
    private StarterAssets.ThirdPersonController thirdPersonController;
    private PlayerInput playerInput;

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (player != null)
        {
            thirdPersonController = player.GetComponent<StarterAssets.ThirdPersonController>();
            playerInput = player.GetComponent<PlayerInput>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !played)
        {
            played = true;

            // Disable player movement
            DisablePlayerMovement();
            
            if (playableDirector != null)
            {
                playableDirector.Play();
                playableDirector.stopped += OnTimelineFinished;
            }
        }
    }
    
    private void DisablePlayerMovement()
    {
        // Disable the controller script
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = false;
        }
        
        // Disable player input
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
        
        // Stop any existing velocity
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    private void EnablePlayerMovement()
    {
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = true;
        }
        
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
    }
    
    private void OnTimelineFinished(PlayableDirector director)
    {
        EnablePlayerMovement();
        director.stopped -= OnTimelineFinished;
    }
    
    // Clean up event subscription if object is destroyed
    private void OnDestroy()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineFinished;
        }
    }
}