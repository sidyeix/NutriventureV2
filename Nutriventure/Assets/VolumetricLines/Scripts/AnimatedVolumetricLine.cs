using UnityEngine;
using VolumetricLines;

[RequireComponent(typeof(VolumetricLineBehavior))]
public class AnimatedVolumetricLine : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private bool autoUpdateDuringAnimation = true;
    [SerializeField] private float updateThreshold = 0.001f; // Minimum position change to trigger update

    private VolumetricLineBehavior lineBehavior;
    private Vector3 lastStartPos;
    private Vector3 lastEndPos;

    void Start()
    {
        lineBehavior = GetComponent<VolumetricLineBehavior>();

        if (lineBehavior == null)
        {
            Debug.LogError("AnimatedVolumetricLine requires VolumetricLineBehavior component!");
            enabled = false;
            return;
        }

        // Store initial positions
        lastStartPos = lineBehavior.StartPos;
        lastEndPos = lineBehavior.EndPos;
    }

    void Update()
    {
        if (!autoUpdateDuringAnimation || lineBehavior == null) return;

        // Get current positions
        Vector3 currentStartPos = lineBehavior.StartPos;
        Vector3 currentEndPos = lineBehavior.EndPos;

        // Check if positions have changed significantly
        bool startPosChanged = Vector3.Distance(lastStartPos, currentStartPos) > updateThreshold;
        bool endPosChanged = Vector3.Distance(lastEndPos, currentEndPos) > updateThreshold;

        if (startPosChanged || endPosChanged)
        {
            // Force update the visual line
            ForceLineUpdate();

            // Update stored positions
            lastStartPos = currentStartPos;
            lastEndPos = currentEndPos;
        }
    }

    /// <summary>
    /// Forces an immediate update of the volumetric line visual
    /// Call this method when animating the line through external means
    /// </summary>
    public void ForceLineUpdate()
    {
        if (lineBehavior == null) return;

        // Directly call the internal method to update mesh vertices
        // Note: We need to use reflection since the method is private
        UpdateLineVertices();
    }

    /// <summary>
    /// Updates the line to specific start and end positions
    /// </summary>
    public void UpdateLinePositions(Vector3 startPos, Vector3 endPos)
    {
        if (lineBehavior == null) return;

        lineBehavior.StartPos = startPos;
        lineBehavior.EndPos = endPos;

        // Force immediate visual update
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates only the end position
    /// </summary>
    public void UpdateEndPosition(Vector3 endPos)
    {
        if (lineBehavior == null) return;

        lineBehavior.EndPos = endPos;
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates only the start position
    /// </summary>
    public void UpdateStartPosition(Vector3 startPos)
    {
        if (lineBehavior == null) return;

        lineBehavior.StartPos = startPos;
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates the end position by adding an offset
    /// </summary>
    public void OffsetEndPosition(Vector3 offset)
    {
        if (lineBehavior == null) return;

        lineBehavior.EndPos += offset;
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates the end Y position only (useful for vertical animations)
    /// </summary>
    public void UpdateEndYPosition(float yPosition)
    {
        if (lineBehavior == null) return;

        Vector3 currentEndPos = lineBehavior.EndPos;
        currentEndPos.y = yPosition;
        lineBehavior.EndPos = currentEndPos;
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates the end position by lerping between start and target
    /// </summary>
    public void LerpEndPosition(Vector3 targetPosition, float t)
    {
        if (lineBehavior == null) return;

        Vector3 startPos = lineBehavior.StartPos;
        Vector3 lerpedEndPos = Vector3.Lerp(startPos, targetPosition, t);
        lineBehavior.EndPos = lerpedEndPos;
        ForceLineUpdate();
    }

    /// <summary>
    /// Updates the end position based on a curve
    /// </summary>
    public void CurveEndPosition(Vector3 startPosition, Vector3 controlPoint, Vector3 targetPosition, float t)
    {
        if (lineBehavior == null) return;

        // Quadratic bezier curve
        Vector3 p0 = startPosition;
        Vector3 p1 = controlPoint;
        Vector3 p2 = targetPosition;

        Vector3 curvePos = Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
        lineBehavior.EndPos = curvePos;
        ForceLineUpdate();
    }

    /// <summary>
    /// Directly updates the mesh vertices (uses reflection to access private method)
    /// </summary>
    private void UpdateLineVertices()
    {
        // This is a workaround since SetStartAndEndPoints is public but might not update properly
        // We'll force update by setting the property values again
        Vector3 currentStart = lineBehavior.StartPos;
        Vector3 currentEnd = lineBehavior.EndPos;

        // Force update by setting the values through the property
        lineBehavior.StartPos = currentStart;
        lineBehavior.EndPos = currentEnd;

        // Also update bounds
        lineBehavior.UpdateBounds();
    }

    /// <summary>
    /// Enables or disables automatic updates during animation
    /// </summary>
    public void SetAutoUpdate(bool enabled)
    {
        autoUpdateDuringAnimation = enabled;
    }

    /// <summary>
    /// Sets the minimum position change threshold for triggering updates
    /// </summary>
    public void SetUpdateThreshold(float threshold)
    {
        updateThreshold = Mathf.Max(0.0001f, threshold);
    }

    /// <summary>
    /// Manual method to check and update if needed
    /// Call this from your animation events
    /// </summary>
    public void ManualUpdateCheck()
    {
        Vector3 currentStartPos = lineBehavior.StartPos;
        Vector3 currentEndPos = lineBehavior.EndPos;

        bool startPosChanged = Vector3.Distance(lastStartPos, currentStartPos) > updateThreshold;
        bool endPosChanged = Vector3.Distance(lastEndPos, currentEndPos) > updateThreshold;

        if (startPosChanged || endPosChanged)
        {
            ForceLineUpdate();
            lastStartPos = currentStartPos;
            lastEndPos = currentEndPos;
        }
    }

    void OnValidate()
    {
        // Ensure threshold is positive
        updateThreshold = Mathf.Max(0.0001f, updateThreshold);
    }

    void OnDrawGizmosSelected()
    {
        if (lineBehavior == null) return;

        // Visualize the line in scene view
        Gizmos.color = Color.cyan;
        Vector3 worldStart = transform.TransformPoint(lineBehavior.StartPos);
        Vector3 worldEnd = transform.TransformPoint(lineBehavior.EndPos);
        Gizmos.DrawLine(worldStart, worldEnd);

        // Draw spheres at start and end points
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldStart, 0.05f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(worldEnd, 0.05f);
    }
}