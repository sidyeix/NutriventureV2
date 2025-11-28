using UnityEngine;

public class PlatformSticker : MonoBehaviour
{
    private CharacterController _controller;
    private MovingPlatformForCharacterController _currentPlatform;
    private Vector3 _lastPlatformPosition;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Only apply platform movement if we're grounded on the platform
        if (_currentPlatform != null && IsGroundedOnPlatform())
        {
            Vector3 platformMovement = _currentPlatform.GetPlatformVelocity() * Time.deltaTime;

            // This MOVES the player with the platform while preserving their own control
            _controller.Move(platformMovement);
        }
        else
        {
            _currentPlatform = null; // Not on platform or not grounded
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only stick to platform when landing on it from above
        if (hit.moveDirection.y < -0.9f && hit.collider.CompareTag("MovingPlatform"))
        {
            _currentPlatform = hit.collider.GetComponent<MovingPlatformForCharacterController>();
            if (_currentPlatform != null)
            {
                _lastPlatformPosition = _currentPlatform.transform.position;
            }
        }
    }

    private bool IsGroundedOnPlatform()
    {
        // Use the same ground check logic as your Starter Assets controller
        bool grounded = Physics.CheckSphere(
            transform.position + Vector3.up * -0.14f,
            0.28f,
            LayerMask.GetMask("Default")
        );

        // Additional check: make sure we're still colliding with our platform
        return grounded && _currentPlatform != null;
    }

    // This ensures we detach when jumping
    public void OnJump()
    {
        _currentPlatform = null;
    }
}