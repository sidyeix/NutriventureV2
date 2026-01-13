using UnityEngine;

public class K3_PlayerPlatformStick : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Layer mask for moving platforms")]
    public LayerMask platformLayer = 1 << 0; // Default layer
    
    [Tooltip("Vertical offset for platform detection")]
    public float verticalOffset = 0.1f;
    
    [Tooltip("Raycast distance for ground check")]
    public float groundCheckDistance = 0.2f;
    
    [Tooltip("Smooth movement speed")]
    public float movementSmoothness = 10f;
    
    [Header("Debug")]
    public bool showDebugMessages = true;
    public bool drawGizmos = true;
    
    private CharacterController controller;
    private Transform currentPlatform;
    private Vector3 platformOffset;
    private bool isOnPlatform = false;
    private Vector3 lastPlatformPosition;
    private Vector3 targetMovePosition;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    
    private void Update()
    {
        // Check if we're on a platform
        CheckPlatformStatus();
        
        // If on platform, move with it
        if (isOnPlatform && currentPlatform != null)
        {
            MoveWithPlatform();
        }
        
        // Handle jumping off platform
        if (isOnPlatform && !IsGroundedOnPlatform())
        {
            DetachFromPlatform();
        }
    }
    
    private void CheckPlatformStatus()
    {
        // If not already on a platform, check if we should attach
        if (!isOnPlatform && controller.isGrounded)
        {
            CheckForPlatformUnderneath();
        }
    }
    
    private void CheckForPlatformUnderneath()
    {
        // Raycast down to detect platform
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * verticalOffset;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance))
        {
            // Check if it's a moving platform
            if (hit.collider.CompareTag("MovingPlatform"))
            {
                AttachToPlatform(hit.collider.transform, hit.point);
            }
        }
    }
    
    private bool IsGroundedOnPlatform()
    {
        if (currentPlatform == null) return false;
        
        // Raycast down to check if we're still above the platform
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * verticalOffset;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance))
        {
            return hit.collider.CompareTag("MovingPlatform");
        }
        
        return false;
    }
    
    private void AttachToPlatform(Transform platform, Vector3 contactPoint)
    {
        currentPlatform = platform;
        isOnPlatform = true;
        
        // Calculate offset from platform's position
        platformOffset = transform.position - currentPlatform.position;
        lastPlatformPosition = currentPlatform.position;
        
        // Set target position
        targetMovePosition = transform.position;
        
        if (showDebugMessages)
            Debug.Log($"Attached to platform: {platform.name}, Offset: {platformOffset}");
    }
    
    private void MoveWithPlatform()
    {
        if (currentPlatform == null) return;
        
        // Calculate platform movement
        Vector3 platformMovement = currentPlatform.position - lastPlatformPosition;
        
        // Only move if platform has actually moved
        if (platformMovement.magnitude > 0.001f)
        {
            // Update target position
            targetMovePosition += platformMovement;
            
            // Smoothly move towards target position
            Vector3 moveDelta = targetMovePosition - transform.position;
            
            // Use CharacterController.Move for proper collision
            if (moveDelta.magnitude > 0.001f)
            {
                // Move only horizontally (maintain vertical position from gravity)
                Vector3 horizontalMove = new Vector3(moveDelta.x, 0, moveDelta.z);
                
                // Apply movement
                controller.Move(horizontalMove);
                
                if (showDebugMessages && horizontalMove.magnitude > 0.01f)
                    Debug.Log($"Moving with platform: {horizontalMove}");
            }
        }
        
        // Update last platform position
        lastPlatformPosition = currentPlatform.position;
        
        // Update platform offset for reference
        platformOffset = transform.position - currentPlatform.position;
    }
    
    private void DetachFromPlatform()
    {
        if (isOnPlatform)
        {
            if (showDebugMessages)
                Debug.Log($"Detached from platform");
            
            isOnPlatform = false;
            currentPlatform = null;
        }
    }
    
    // Force detach (can be called from other scripts)
    public void ForceDetach()
    {
        DetachFromPlatform();
    }
    
    // Check if currently on a platform
    public bool IsOnPlatform()
    {
        return isOnPlatform;
    }
    
    // Get current platform (if any)
    public Transform GetCurrentPlatform()
    {
        return currentPlatform;
    }
    
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        
        // Draw ground check ray
        Gizmos.color = Color.cyan;
        Vector3 rayStart = transform.position + Vector3.up * verticalOffset;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundCheckDistance);
        
        // Draw platform connection
        if (isOnPlatform && currentPlatform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentPlatform.position);
            Gizmos.DrawWireSphere(currentPlatform.position, 0.3f);
        }
    }
}