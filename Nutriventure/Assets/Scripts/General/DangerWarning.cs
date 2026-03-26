using UnityEngine;
using TMPro;
using System.Collections;

public class DangerWarning : MonoBehaviour
{
    [Header("UI References")]
    public Canvas warningCanvas;
    public GameObject dangerPanel;
    public TextMeshProUGUI dangerText;
    public CanvasGroup panelCanvasGroup;

    [Header("Trigger")]
    public Collider triggerCollider;

    [Header("Warning Message")]
    [TextArea(2, 4)]
    public string warningMessage = "DANGER AHEAD!";

    [Header("Timing")]
    public float fadeInDuration = 0.3f;
    public float showDuration = 2f;
    public float blinkDuration = 2f;
    public float blinkSpeed = 1.5f;
    [Range(0f, 1f)]
    public float blinkMinAlpha = 0.3f;
    public float fadeOutDuration = 0.3f;

    [Header("Gizmo")]
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);

    private Coroutine warningCoroutine;
    private bool isShowing;

    private void Start()
    {
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 0f;

        if (dangerPanel != null)
            dangerPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ShowWarning();
    }

    public void ShowWarning()
    {
        if (isShowing) return;

        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);

        warningCoroutine = StartCoroutine(WarningSequence());
    }

    private IEnumerator WarningSequence()
    {
        isShowing = true;

        if (dangerText != null)
            dangerText.text = warningMessage;

        if (dangerPanel != null)
            dangerPanel.SetActive(true);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        // Show at full alpha
        yield return new WaitForSeconds(showDuration);

        // Slow blink
        elapsed = 0f;
        while (elapsed < blinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = (Mathf.Sin(elapsed * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            panelCanvasGroup.alpha = Mathf.Lerp(blinkMinAlpha, 1f, t);
            yield return null;
        }

        // Fade out
        panelCanvasGroup.alpha = 1f;
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;

        if (dangerPanel != null)
            dangerPanel.SetActive(false);

        isShowing = false;
    }

    private void OnDrawGizmos()
    {
        if (triggerCollider == null) return;

        Gizmos.color = gizmoColor;
        DrawColliderGizmo(triggerCollider, false);
    }

    private void OnDrawGizmosSelected()
    {
        if (triggerCollider == null) return;

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoColor.a + 0.3f);
        DrawColliderGizmo(triggerCollider, true);
    }

    private void DrawColliderGizmo(Collider col, bool wireframe)
    {
        Gizmos.matrix = col.transform.localToWorldMatrix;

        if (col is BoxCollider box)
        {
            if (wireframe)
                Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            if (wireframe)
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            Vector3 center = capsule.center;
            float radius = capsule.radius;
            if (wireframe)
                Gizmos.DrawWireSphere(center, radius);
            Gizmos.DrawSphere(center, radius);
        }
        else if (col is MeshCollider mesh && mesh.sharedMesh != null)
        {
            if (wireframe)
                Gizmos.DrawWireMesh(mesh.sharedMesh);
            Gizmos.DrawMesh(mesh.sharedMesh);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
