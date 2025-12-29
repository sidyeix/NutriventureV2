using UnityEngine;

public class PlayerPlatformStick : MonoBehaviour
{
    private Transform currentPlatform;
    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only stick when landing on top
        if (hit.moveDirection.y < -0.5f && hit.collider.CompareTag("MovingPlatform"))
        {
            if (currentPlatform != hit.collider.transform)
            {
                currentPlatform = hit.collider.transform;
                transform.SetParent(currentPlatform);
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
    }
}
