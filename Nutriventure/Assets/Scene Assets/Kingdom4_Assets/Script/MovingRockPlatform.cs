using UnityEngine;

public class MovingRockPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveDistance = 5f;
    public float moveSpeed = 2f;
    
    [Header("Player Sticky Settings")]
    public float stickForce = 15f;
    public float maxStickAngle = 30f;
    public bool useStickySurface = true;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingToTarget = true;
    
    // Store players on platform
    private System.Collections.Generic.List<PlayerStickInfo> playersOnPlatform = 
        new System.Collections.Generic.List<PlayerStickInfo>();
    
    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + (moveDirection.normalized * moveDistance);
        
        // Add physics material for better grip
        AddStickyPhysicsMaterial();
    }
    
    void FixedUpdate()
    {
        // Move platform
        Vector3 target = movingToTarget ? targetPosition : startPosition;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.fixedDeltaTime);
        
        // Reverse direction
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            movingToTarget = !movingToTarget;
        }
        
        // Apply sticky force to all players on platform
        foreach (var playerInfo in playersOnPlatform)
        {
            if (playerInfo.playerTransform != null && playerInfo.playerRigidbody != null)
            {
                ApplyStickyForce(playerInfo);
            }
        }
    }
    
    void ApplyStickyForce(PlayerStickInfo playerInfo)
    {
        if (!useStickySurface) return;
        
        Rigidbody rb = playerInfo.playerRigidbody;
        Vector3 playerPos = playerInfo.playerTransform.position;
        
        // Calculate point directly above platform under player
        Vector3 platformPoint = new Vector3(
            playerPos.x,
            transform.position.y + GetComponent<Collider>().bounds.extents.y,
            playerPos.z
        );
        
        // Calculate direction from player to platform point
        Vector3 stickDirection = (platformPoint - playerPos).normalized;
        
        // Only apply force if player is above platform and within stick angle
        float angleFromVertical = Vector3.Angle(Vector3.down, stickDirection);
        
        if (angleFromVertical <= maxStickAngle)
        {
            // Apply sticky force (stronger when moving away from center)
            float distanceFromCenter = Vector3.Distance(playerPos, platformPoint);
            float forceMultiplier = Mathf.Clamp01(distanceFromCenter * 2f);
            
            rb.AddForce(stickDirection * stickForce * forceMultiplier, ForceMode.Force);
            
            // Dampen horizontal movement to keep player from sliding off
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(-horizontalVelocity * 5f, ForceMode.Force);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                PlayerStickInfo newPlayer = new PlayerStickInfo
                {
                    playerTransform = collision.transform,
                    playerRigidbody = rb,
                    entryTime = Time.time
                };
                
                playersOnPlatform.Add(newPlayer);
                
                // Reduce player's velocity when landing on platform
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.5f, 0, rb.linearVelocity.z * 0.5f);
                
                Debug.Log("Player stuck to platform");
            }
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if player is trying to jump off
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.y > 5f)
            {
                // Player is jumping, remove from sticky list
                RemovePlayer(collision.transform);
            }
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            RemovePlayer(collision.transform);
        }
    }
    
    void RemovePlayer(Transform playerTransform)
    {
        for (int i = playersOnPlatform.Count - 1; i >= 0; i--)
        {
            if (playersOnPlatform[i].playerTransform == playerTransform)
            {
                playersOnPlatform.RemoveAt(i);
                Debug.Log("Player released from platform");
                break;
            }
        }
    }
    
    void AddStickyPhysicsMaterial()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            // Create new PhysicsMaterial (not PhysicMaterial)
            PhysicsMaterial stickyMat = new PhysicsMaterial("StickyPlatform")
            {
                dynamicFriction = 1f,
                staticFriction = 1f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounciness = 0f
            };
            
            col.material = stickyMat;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = playersOnPlatform.Count > 0 ? Color.green : Color.red;
        
        if (Application.isPlaying)
        {
            Gizmos.DrawLine(startPosition, targetPosition);
            Gizmos.DrawWireSphere(startPosition, 0.3f);
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
            
            // Show players stuck to platform
            foreach (var player in playersOnPlatform)
            {
                if (player.playerTransform != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, player.playerTransform.position);
                }
            }
        }
        else
        {
            Gizmos.DrawLine(transform.position, transform.position + (moveDirection.normalized * moveDistance));
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawWireSphere(transform.position + (moveDirection.normalized * moveDistance), 0.3f);
        }
    }
    
    private struct PlayerStickInfo
    {
        public Transform playerTransform;
        public Rigidbody playerRigidbody;
        public float entryTime;
    }
}