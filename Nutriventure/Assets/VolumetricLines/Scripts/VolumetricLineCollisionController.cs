using UnityEngine;
using VolumetricLines;

[RequireComponent(typeof(VolumetricLineBehavior))]
public class VolumetricLineCollisionController : MonoBehaviour
{
    [Header("Collision Settings")]
    public string pushableTag = "Pushable";
    public LayerMask collisionLayers = -1;
    public float collisionThickness = 0.1f;

    [Header("Visual Settings")]
    public Color normalColor = Color.green;
    public Color blockedColor = Color.red;
    public float colorChangeSpeed = 5f;

    [Header("Line Effects")]
    public float blockedLineWidthMultiplier = 1.2f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.1f;

    private VolumetricLineBehavior lineBehavior;

    private Vector3 originalStartPos;
    private Vector3 originalEndPos;
    private float originalLineWidth;

    private bool isBlocked;
    private Vector3 hitPoint;
    private float pulseTimer;

    void Start()
    {
        lineBehavior = GetComponent<VolumetricLineBehavior>();

        originalStartPos = lineBehavior.StartPos;
        originalEndPos = lineBehavior.EndPos;
        originalLineWidth = lineBehavior.LineWidth;

        lineBehavior.LineColor = normalColor;
    }

    void Update()
    {
        CheckCollisionAndMoveStart();
        UpdateLineVisuals();
    }

    void CheckCollisionAndMoveStart()
    {
        Vector3 worldStart = transform.TransformPoint(originalStartPos);
        Vector3 worldEnd = transform.TransformPoint(originalEndPos);

        Vector3 direction = (worldEnd - worldStart).normalized;
        float maxDistance = Vector3.Distance(worldStart, worldEnd);

        RaycastHit[] hits = Physics.SphereCastAll(
            worldStart,
            collisionThickness,
            direction,
            maxDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit closestHit = default;
        float closestDistance = float.MaxValue;
        bool foundPushable = false;

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.CompareTag(pushableTag))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundPushable = true;
            }
        }

        if (foundPushable)
        {
            isBlocked = true;
            hitPoint = closestHit.point;

            // Offset so the volumetric glow doesn't clip the object
            float offset = collisionThickness * 0.5f;

            Vector3 adjustedWorldStart =
                closestHit.point + direction * offset;

            Vector3 localStart =
                transform.InverseTransformPoint(adjustedWorldStart);

            // ?? Move START forward, keep END fixed
            lineBehavior.StartPos = localStart;
            lineBehavior.EndPos = originalEndPos;
        }
        else
        {
            isBlocked = false;

            // Restore original positions
            lineBehavior.StartPos = originalStartPos;
            lineBehavior.EndPos = originalEndPos;
        }
    }

    void UpdateLineVisuals()
    {
        if (isBlocked)
            pulseTimer += Time.deltaTime * pulseSpeed;
        else
            pulseTimer = 0f;

        float pulse = isBlocked
            ? 1f + Mathf.Sin(pulseTimer) * pulseAmount
            : 1f;

        Color targetColor = isBlocked ? blockedColor : normalColor;
        lineBehavior.LineColor = Color.Lerp(
            lineBehavior.LineColor,
            targetColor,
            Time.deltaTime * colorChangeSpeed
        );

        float targetWidth = isBlocked
            ? originalLineWidth * blockedLineWidthMultiplier * pulse
            : originalLineWidth;

        lineBehavior.LineWidth = Mathf.Lerp(
            lineBehavior.LineWidth,
            targetWidth,
            Time.deltaTime * colorChangeSpeed
        );
    }

    // Optional public helpers
    public bool IsBlocked() => isBlocked;
    public Vector3 GetHitPoint() => hitPoint;
}
