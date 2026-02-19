using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class KeyUnlockedCanvasController : MonoBehaviour
{
    [Header("Canvas References")]
    public GameObject keyUnlockedCanvas;
    public CanvasGroup canvasGroup;
    public Button continueButton;

    [Header("Animation Settings")]
    public float fadeDuration = 0.5f;
    public float scaleDuration = 0.3f;

    [Header("Optional Audio")]
    public AudioClip keyUnlockedSound;
    public AudioClip buttonClickSound;

    private System.Action onContinueCallback;
    private bool isShowing = false;

    private void Awake()
    {
        if (keyUnlockedCanvas == null)
            keyUnlockedCanvas = gameObject;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null && keyUnlockedCanvas != null)
            canvasGroup = keyUnlockedCanvas.AddComponent<CanvasGroup>();

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        // Ensure canvas starts hidden
        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void ShowKeyUnlockedCanvas(System.Action onContinue)
    {
        if (isShowing) return;

        onContinueCallback = onContinue;

        // Play key unlocked sound
        if (AudioHandler.Instance != null && keyUnlockedSound != null)
            AudioHandler.Instance.PlayCharacterSelectionSound(keyUnlockedSound);

        // Show and animate canvas
        keyUnlockedCanvas.SetActive(true);
        StartCoroutine(AnimateCanvasIn());
    }

    private IEnumerator AnimateCanvasIn()
    {
        isShowing = true;

        // Reset scale for pop effect
        keyUnlockedCanvas.transform.localScale = Vector3.zero;

        // Fade in and scale up simultaneously
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // Scale with overshoot for pop effect
            if (elapsedTime < scaleDuration)
            {
                float scaleT = elapsedTime / scaleDuration;
                float scale = Mathf.Lerp(0f, 1.1f, scaleT);
                keyUnlockedCanvas.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Final scale to normal
        keyUnlockedCanvas.transform.localScale = Vector3.one;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateCanvasOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            keyUnlockedCanvas.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);

            yield return null;
        }

        keyUnlockedCanvas.SetActive(false);
        isShowing = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnContinueClicked()
    {
        // Play button click sound
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();
        else if (buttonClickSound != null && AudioHandler.Instance != null)
            AudioHandler.Instance.PlayCharacterSelectionSound(buttonClickSound);

        // Hide canvas
        StartCoroutine(AnimateCanvasOut());

        // Invoke callback
        onContinueCallback?.Invoke();
    }

    public bool IsShowing()
    {
        return isShowing;
    }

    public void ForceHide()
    {
        StopAllCoroutines();
        if (keyUnlockedCanvas != null)
            keyUnlockedCanvas.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        isShowing = false;
    }
}