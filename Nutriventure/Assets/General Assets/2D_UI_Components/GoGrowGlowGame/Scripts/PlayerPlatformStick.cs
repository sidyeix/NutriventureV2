using UnityEngine;

public class PlayerPlatformStick : MonoBehaviour
{
    private Transform currentPlatform;
    private CharacterController controller;
    private Transform platformParent;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Create an empty GameObject to use as parent
        platformParent = new GameObject("PlatformParent").transform;
        platformParent.localScale = Vector3.one; // Always scale 1
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only stick when landing on top
        if (hit.moveDirection.y < -0.5f && hit.collider.CompareTag("MovingPlatform"))
        {
            if (currentPlatform != hit.collider.transform)
            {
                currentPlatform = hit.collider.transform;

                // Position the parent at the platform
                platformParent.position = currentPlatform.position;
                platformParent.rotation = currentPlatform.rotation;

                // Parent to our empty object (not directly to platform)
                transform.SetParent(platformParent);
            }
        }
    }

    private void Update()
    {
        // Detach when jumping or falling
        if (controller.velocity.y > 0.1f && currentPlatform != null)
        {
            transform.SetParent(null);
            currentPlatform = null;
        }

        // If we're on a platform, update our parent's position to follow it
        if (currentPlatform != null)
        {
            platformParent.position = currentPlatform.position;
            platformParent.rotation = currentPlatform.rotation;
        }
    }
}