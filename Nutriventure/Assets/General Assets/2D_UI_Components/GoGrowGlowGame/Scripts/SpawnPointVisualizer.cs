using UnityEngine;

public class SpawnPointVisualizer : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private float gizmoRadius = 1f;
    [SerializeField] private bool showDirection = true;

    private void OnDrawGizmos()
    {
        // Set the gizmo color
        Gizmos.color = gizmoColor;

        // Draw a wire sphere at the spawn point position
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // Draw a line showing the forward direction (optional)
        if (showDirection)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * gizmoRadius * 2);

            // Add an arrow tip
            Vector3 arrowTip = transform.position + transform.forward * gizmoRadius * 2;
            Gizmos.DrawLine(arrowTip, arrowTip - transform.forward * 0.3f + transform.right * 0.3f);
            Gizmos.DrawLine(arrowTip, arrowTip - transform.forward * 0.3f - transform.right * 0.3f);
        }

        // Draw the object's name above the spawn point
        GUIStyle style = new GUIStyle();
        style.normal.textColor = gizmoColor;
        style.alignment = TextAnchor.MiddleCenter;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoRadius + 0.5f),
            gameObject.name, style);
#endif
    }
}