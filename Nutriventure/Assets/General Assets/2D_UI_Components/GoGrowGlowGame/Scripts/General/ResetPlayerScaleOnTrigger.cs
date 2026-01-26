using UnityEngine;

public class ResetPlayerScaleOnTrigger : MonoBehaviour
{
    public Transform playerArmature;
    public float transitionSpeed = 2f;

    private bool isScaling = false;
    private Vector3 startScale;
    private float scaleTimer = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (playerArmature != null && !isScaling)
        {
            startScale = playerArmature.localScale;
            scaleTimer = 0f;
            isScaling = true;
        }
    }

    private void Update()
    {
        if (isScaling)
        {
            scaleTimer += Time.deltaTime * transitionSpeed;
            float progress = Mathf.Clamp01(scaleTimer);

            playerArmature.localScale = Vector3.Lerp(startScale, Vector3.one, progress);

            if (progress >= 1f)
            {
                playerArmature.localScale = Vector3.one;
                isScaling = false;
            }
        }
    }
}