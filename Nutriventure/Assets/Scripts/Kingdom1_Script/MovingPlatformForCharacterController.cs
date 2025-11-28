using UnityEngine;

public class MovingPlatformForCharacterController : MonoBehaviour
{
    private Vector3 lastPlatformPosition;
    private Vector3 platformVelocity;

    void Start()
    {
        lastPlatformPosition = transform.position;
        // Add the MovingPlatform tag automatically
        gameObject.tag = "MovingPlatform";
    }

    void Update()
    {
        // Calculate platform movement this frame
        Vector3 currentPlatformPosition = transform.position;
        platformVelocity = (currentPlatformPosition - lastPlatformPosition) / Time.deltaTime;
        lastPlatformPosition = currentPlatformPosition;
    }

    public Vector3 GetPlatformVelocity()
    {
        return platformVelocity;
    }
}