using UnityEngine;

public class TransformFollower : MonoBehaviour
{
    public enum FollowType
    {
        WorldPosition,
        LocalPosition,
        Smooth
    }

    [Header("TARGET")]
    public Transform target;
    public FollowType followType = FollowType.WorldPosition;

    [Header("OFFSET")]
    public Vector3 positionOffset = Vector3.zero;
    public float verticalOffset = 1f;
    public bool useWorldUp = true;

    [Header("ROTATION")]
    public bool matchRotation = false;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("SMOOTHING (if using Smooth type)")]
    public float smoothSpeed = 5f;
    public float rotationSmoothSpeed = 5f;

    [Header("FLOATING ANIMATION")]
    public bool enableFloating = true;
    public float floatHeight = 0.2f;
    public float floatSpeed = 1.5f;
    public Vector3 floatRotationSpeed = new Vector3(0, 30, 0);

    private Vector3 currentVelocity;
    private float randomOffset;
    private Vector3 basePosition;

Vector3 GetTargetTopCenter()
{
    Renderer rend = target.GetComponentInChildren<Renderer>();
    if (rend != null)
    {
        Vector3 center = rend.bounds.center;
        center.y = rend.bounds.max.y;
        return center;
    }

    return target.position;
}

    void Start()
    {
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
        
        if (target == null)
        {
            Debug.LogWarning($"{name} has no target to follow!");
            enabled = false;
            return;
        }

        UpdateBasePosition();
    }

    void Update()
    {
        if (target == null) return;

        UpdateBasePosition();

        // Handle rotation
        if (matchRotation)
        {
            Quaternion targetRot = useWorldUp ? 
                Quaternion.Euler(rotationOffset) : 
                target.rotation * Quaternion.Euler(rotationOffset);
            
            if (followType == FollowType.Smooth)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = targetRot;
            }
        }
        else if (floatRotationSpeed != Vector3.zero)
        {
            transform.Rotate(floatRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void LateUpdate()
    {
        // For absolute precision, use LateUpdate
        if (followType == FollowType.WorldPosition && target != null)
        {
            UpdateBasePosition();
            if (enableFloating)
            {
                float floatY = Mathf.Sin((Time.time + randomOffset) * floatSpeed) * floatHeight;
                transform.position = basePosition + Vector3.up * floatY;
            }
            else
            {
                transform.position = basePosition;
            }
        }
    }

    void UpdateBasePosition()
{
    if (target == null) return;

    Vector3 topCenter = GetTargetTopCenter();
    basePosition = topCenter + positionOffset + Vector3.up * verticalOffset;
}


    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position + Vector3.up * verticalOffset, 0.1f);
        }
    }
}