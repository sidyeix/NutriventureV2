using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FoodFeedbackUI : MonoBehaviour
{
    [Header("UI References")]
    public Image foodIconImage;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float slideDistance = 100f;
    public float slideDuration = 0.5f;
    public float displayDuration = 2f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Position Settings")]
    public RectTransform uiRectTransform;
    public Vector2 hiddenPosition;
    public Vector2 shownPosition;

    // State tracking
    private bool isShowing = false;
    private bool isAnimating = false;
    private Coroutine currentAnimationCoroutine;

    private void Start()
    {
        InitializeUI();
        HideImmediate();
    }

    private void InitializeUI()
    {
        // Get references if not set
        if (uiRectTransform == null)
            uiRectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (foodIconImage == null)
            foodIconImage = GetComponent<Image>();

        // Store original position
        shownPosition = uiRectTransform.anchoredPosition;

        // Calculate hidden position (below the shown position)
        hiddenPosition = shownPosition - new Vector2(0, slideDistance);

        // Make sure the GameObject is ACTIVE (but hidden)
        gameObject.SetActive(true);
    }

    public void ShowFoodFeedback(Sprite foodSprite)
    {
        // If already showing or animating, ignore new feedback
        if (isShowing || isAnimating)
        {
            Debug.Log("Food feedback already showing, ignoring new food");
            return;
        }

        // Set the food sprite
        if (foodSprite != null && foodIconImage != null)
        {
            foodIconImage.sprite = foodSprite;
        }

        // Start the show animation
        StartCoroutine(ShowAnimationCoroutine());
    }

    private IEnumerator ShowAnimationCoroutine()
    {
        // Ensure GameObject is active
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        isShowing = true;
        isAnimating = true;

        // Reset to hidden position
        uiRectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        // Slide up and fade in
        float elapsedTime = 0f;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slideDuration;

            // Apply animation curves
            float slideT = slideCurve.Evaluate(t);
            float fadeT = fadeCurve.Evaluate(t);

            // Slide from hidden to shown position
            uiRectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, slideT);

            // Fade in
            canvasGroup.alpha = fadeT;

            yield return null;
        }

        // Ensure final position and alpha
        uiRectTransform.anchoredPosition = shownPosition;
        canvasGroup.alpha = 1f;

        isAnimating = false;

        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Start hide animation
        StartCoroutine(HideAnimationCoroutine());
    }

    private IEnumerator HideAnimationCoroutine()
    {
        isAnimating = true;

        // Slide down and fade out
        float elapsedTime = 0f;
        Vector2 startPosition = uiRectTransform.anchoredPosition;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slideDuration;

            // Apply animation curves (reverse)
            float slideT = slideCurve.Evaluate(1 - t);
            float fadeT = fadeCurve.Evaluate(1 - t);

            // Slide from shown to hidden position
            uiRectTransform.anchoredPosition = Vector2.Lerp(startPosition, hiddenPosition, 1 - slideT);

            // Fade out
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, 1 - fadeT);

            yield return null;
        }

        // Ensure hidden state
        uiRectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        isShowing = false;
        isAnimating = false;
    }

    public void HideImmediate()
    {
        // Stop any ongoing coroutines
        StopAllCoroutines();

        // Ensure GameObject is active
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Set to hidden state immediately
        uiRectTransform.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        isShowing = false;
        isAnimating = false;
    }

    // Check if feedback is currently showing
    public bool IsShowing() => isShowing;

    // Check if currently animating
    public bool IsAnimating() => isAnimating;

    // Force stop all animations
    public void StopAllAnimations()
    {
        StopAllCoroutines();
        isShowing = false;
        isAnimating = false;
    }

    // For debugging
    private void OnEnable()
    {
        // When enabled, make sure we're in a clean state
        if (isShowing || isAnimating)
        {
            Debug.LogWarning("FoodFeedbackUI was enabled while showing/animating. Resetting.");
            HideImmediate();
        }
    }

    private void OnDisable()
    {
        // When disabled, stop all coroutines to prevent errors
        StopAllCoroutines();
    }

    // Force show with specific sprite (for testing in editor)
    public void TestShow(Sprite testSprite)
    {
        ShowFoodFeedback(testSprite);
    }

    // Editor helper to preview positions
    private void OnValidate()
    {
        if (Application.isPlaying) return;

        // In editor, preview the hidden position
        if (uiRectTransform == null)
            uiRectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Store original position as shown position
        if (uiRectTransform != null)
        {
            shownPosition = uiRectTransform.anchoredPosition;
            hiddenPosition = shownPosition - new Vector2(0, slideDistance);
        }
    }
}