using UnityEngine;
using System.Collections;

public class GoldBallController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float flightDuration = 1.0f;
    public float scaleDuration = 0.3f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Arc Settings")]
    public float arcHeight = 2.0f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 startScale;
    private bool isMoving = false;

    public void Initialize(Vector3 startPos, Vector3 targetPos)
    {
        startPosition = startPos;
        targetPosition = targetPos;
        startScale = transform.localScale;

        // Position at start
        transform.position = startPos;
        transform.localScale = Vector3.zero;

        // Start animation
        StartCoroutine(FlyToTarget());
    }

    IEnumerator FlyToTarget()
    {
        isMoving = true;

        // Scale up quickly
        float scaleTime = 0f;
        while (scaleTime < scaleDuration)
        {
            scaleTime += Time.deltaTime;
            float progress = scaleTime / scaleDuration;
            transform.localScale = startScale * scaleCurve.Evaluate(progress);
            yield return null;
        }

        transform.localScale = startScale;

        // Fly to target with arc
        float flightTime = 0f;
        while (flightTime < flightDuration)
        {
            flightTime += Time.deltaTime;
            float progress = flightTime / flightDuration;

            // Calculate position with arc
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition,
                movementCurve.Evaluate(progress));

            // Add arc height
            float height = heightCurve.Evaluate(progress) * arcHeight;
            currentPos.y += height;

            transform.position = currentPos;

            yield return null;
        }

        // Ensure final position
        transform.position = targetPosition;

        // Trigger pop effect
        OnReachTarget();
    }

    void OnReachTarget()
    {
        // Trigger pop particle effect
        ParticleSystem popEffect = GetComponent<ParticleSystem>();
        if (popEffect != null)
        {
            popEffect.Play();
        }

        // Disable trail
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
        }

        // Scale down and destroy
        StartCoroutine(ScaleDownAndDestroy());
    }

    IEnumerator ScaleDownAndDestroy()
    {
        float scaleTime = 0f;
        Vector3 originalScale = transform.localScale;

        while (scaleTime < scaleDuration)
        {
            scaleTime += Time.deltaTime;
            float progress = scaleTime / scaleDuration;
            transform.localScale = originalScale * (1 - scaleCurve.Evaluate(progress));
            yield return null;
        }

        // Wait for particles to finish
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null && ps.isPlaying)
        {
            yield return new WaitForSeconds(ps.main.duration);
        }

        Destroy(gameObject);
    }
}