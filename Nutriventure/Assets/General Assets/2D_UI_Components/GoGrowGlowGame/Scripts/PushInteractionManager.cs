using StarterAssets;
using UnityEngine;

public class PushInteractionManager : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonController playerController;

    [Header("Physics Settings")]
    public float pushForceMultiplier = 1f;
    public float minForceToMove = 0.1f;
    public float objectStopThreshold = 0.1f;

    private GameObject currentPushableObject;
    private Rigidbody currentObjectRb;
    private bool isPushingActive = false;
    private Vector3 lastObjectPosition;
    private float timeSinceLastMovement;

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<ThirdPersonController>();
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // Get the pushable object from the player controller
        GameObject pushableObject = playerController.GetCurrentPushableObject();

        if (pushableObject != currentPushableObject)
        {
            // Object changed
            currentPushableObject = pushableObject;
            if (currentPushableObject != null)
            {
                currentObjectRb = currentPushableObject.GetComponent<Rigidbody>();
                if (currentObjectRb != null)
                {
                    // When not pushing, make the object kinematic (won't move from collisions)
                    currentObjectRb.isKinematic = true;
                }
            }
            else
            {
                currentObjectRb = null;
            }
        }

        // Check if player is actively pushing
        bool shouldPush = playerController.IsPushing() && currentPushableObject != null;

        if (shouldPush != isPushingActive)
        {
            isPushingActive = shouldPush;

            if (currentObjectRb != null)
            {
                // Toggle kinematic state based on pushing
                currentObjectRb.isKinematic = !isPushingActive;

                if (isPushingActive)
                {
                    // Reset velocity when starting to push
                    currentObjectRb.linearVelocity = Vector3.zero;
                    currentObjectRb.angularVelocity = Vector3.zero;
                    lastObjectPosition = currentPushableObject.transform.position;
                    timeSinceLastMovement = 0f;
                }
            }
        }

        // Apply pushing force if active
        if (isPushingActive && currentObjectRb != null && !currentObjectRb.isKinematic)
        {
            // Check if player is moving forward
            if (playerController != null && Mathf.Abs(playerController.GetComponent<StarterAssetsInputs>().move.y) > 0.1f)
            {
                // Apply force in player's forward direction
                Vector3 pushDirection = playerController.transform.forward;
                float forwardInput = Mathf.Max(0, playerController.GetComponent<StarterAssetsInputs>().move.y);

                Vector3 force = pushDirection * playerController.PushForce * pushForceMultiplier * forwardInput * Time.deltaTime * 60f;
                currentObjectRb.AddForce(force, ForceMode.Force);

                // Track object movement
                if (Vector3.Distance(currentPushableObject.transform.position, lastObjectPosition) > objectStopThreshold)
                {
                    lastObjectPosition = currentPushableObject.transform.position;
                    timeSinceLastMovement = 0f;
                }
                else
                {
                    timeSinceLastMovement += Time.deltaTime;

                    // If object hasn't moved for a while, stop pushing
                    if (timeSinceLastMovement > 0.5f)
                    {
                        // Object is stuck, maybe reduce force or stop pushing
                    }
                }
            }
            else
            {
                // Player is not moving forward - dampen object movement
                currentObjectRb.linearVelocity *= 0.9f;
                currentObjectRb.angularVelocity *= 0.9f;
            }
        }
    }

    public void StopAllPushing()
    {
        if (currentObjectRb != null)
        {
            currentObjectRb.isKinematic = true;
            currentObjectRb.linearVelocity = Vector3.zero;
            currentObjectRb.angularVelocity = Vector3.zero;
        }
        isPushingActive = false;
        currentPushableObject = null;
        currentObjectRb = null;
    }

    void OnDestroy()
    {
        StopAllPushing();
    }
}