using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnhancedCanvasTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Transform player;

    [Header("Trigger Settings")]
    [SerializeField] private float activationRadius = 3f;
    [SerializeField] private LayerMask playerLayer = 1; // Default layer
    [SerializeField] private string playerTag = "Player"; // ADDED: Explicit player tag
    [SerializeField] private bool useBothTagAndLayer = true; // ADDED: Require both tag AND layer

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float slideDistance = 150f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float buttonSpacing = 0.5f;

    [Header("Button Click Settings")]
    [SerializeField] private bool hideOnButtonClick = true;

    private Coroutine animationCoroutine;
    private Button[] buttons;
    private Vector3[] originalPositions;
    private bool canvasVisible = false;
    private bool playerInTrigger = false;

    void Start()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInChildren<Canvas>();

        InitializeButtons();
        targetCanvas.gameObject.SetActive(false);
    }

    void InitializeButtons()
    {
        buttons = targetCanvas.GetComponentsInChildren<Button>(true);
        originalPositions = new Vector3[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                originalPositions[i] = buttons[i].transform.localPosition;
                SetButtonAlpha(buttons[i], 0f);
                buttons[i].transform.localPosition -= new Vector3(slideDistance, 0, 0);
                buttons[i].interactable = false;

                // Add click listener to each button
                int index = i;
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }
    }

    void Update()
    {
        // FIXED: Only proceed if we have a valid player reference
        if (player == null)
        {
            // Try to find player by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                return; // No player found yet
            }
        }

        // FIXED: Check if the object we're tracking is STILL the player
        // This prevents other objects with "Player" tag from being triggered
        if (!player.CompareTag(playerTag))
        {
            Debug.LogWarning($"EnhancedCanvasTrigger: Tracked object no longer has tag '{playerTag}'. Reacquiring...");
            player = null;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= activationRadius && !playerInTrigger)
        {
            playerInTrigger = true;
            if (!canvasVisible)
            {
                ShowCanvas();
            }
        }
        else if (distance > activationRadius && playerInTrigger)
        {
            playerInTrigger = false;
            if (canvasVisible)
            {
                HideCanvas();
            }
        }
    }

    void ShowCanvas()
    {
        canvasVisible = true;
        targetCanvas.gameObject.SetActive(true);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateButtons(true));
    }

    void HideCanvas()
    {
        canvasVisible = false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateButtons(false));
    }

    void OnButtonClicked(int buttonIndex)
    {
        if (!hideOnButtonClick || !canvasVisible) return;

        // Hide the canvas immediately when button is clicked
        HideCanvas();
    }

    IEnumerator AnimateButtons(bool show)
    {
        if (show)
        {
            // Animate buttons in sequentially
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    StartCoroutine(AnimateButton(buttons[i], originalPositions[i], show));
                    yield return new WaitForSeconds(buttonSpacing);
                }
            }
        }
        else
        {
            // Animate buttons out simultaneously
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].interactable = false;
                    StartCoroutine(AnimateButton(buttons[i],
                        originalPositions[i] - new Vector3(slideDistance, 0, 0), show));
                }
            }

            yield return new WaitForSeconds(fadeDuration);
            targetCanvas.gameObject.SetActive(false);
        }
    }

    IEnumerator AnimateButton(Button button, Vector3 targetPosition, bool showIn)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = button.transform.localPosition;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            // Use animation curves
            float slideT = slideCurve.Evaluate(t);
            float fadeT = fadeCurve.Evaluate(t);

            // Position animation
            button.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, slideT);

            // Alpha animation
            float alpha = showIn ? fadeT : (1 - fadeT);
            SetButtonAlpha(button, alpha);

            yield return null;
        }

        // Final state
        button.transform.localPosition = targetPosition;
        SetButtonAlpha(button, showIn ? 1f : 0f);

        if (showIn)
            button.interactable = true;
    }

    void SetButtonAlpha(Button button, float alpha)
    {
        // Set alpha for all graphic components
        Graphic[] graphics = button.GetComponentsInChildren<Graphic>();
        foreach (var graphic in graphics)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    // FIXED: Enhanced trigger enter - checks BOTH tag AND layer
    void OnTriggerEnter(Collider other)
    {
        // Check 1: Is it on the correct layer?
        bool isCorrectLayer = ((1 << other.gameObject.layer) & playerLayer) != 0;

        // Check 2: Does it have the correct tag?
        bool isCorrectTag = other.CompareTag(playerTag);

        // Decide whether to trigger based on settings
        bool shouldTrigger = useBothTagAndLayer ? (isCorrectLayer && isCorrectTag) : (isCorrectLayer || isCorrectTag);

        if (shouldTrigger)
        {
            // Update player reference if this is the actual player
            if (other.CompareTag(playerTag))
            {
                player = other.transform;
            }

            playerInTrigger = true;
            if (!canvasVisible)
            {
                ShowCanvas();
            }

            Debug.Log($"EnhancedCanvasTrigger: Player entered trigger - Layer OK: {isCorrectLayer}, Tag OK: {isCorrectTag}, Triggered: {shouldTrigger}");
        }
    }

    // FIXED: Enhanced trigger exit - checks BOTH tag AND layer
    void OnTriggerExit(Collider other)
    {
        // Check 1: Is it on the correct layer?
        bool isCorrectLayer = ((1 << other.gameObject.layer) & playerLayer) != 0;

        // Check 2: Does it have the correct tag?
        bool isCorrectTag = other.CompareTag(playerTag);

        // Decide whether to trigger based on settings
        bool shouldTrigger = useBothTagAndLayer ? (isCorrectLayer && isCorrectTag) : (isCorrectLayer || isCorrectTag);

        if (shouldTrigger)
        {
            playerInTrigger = false;
            if (canvasVisible)
            {
                HideCanvas();
            }

            Debug.Log($"EnhancedCanvasTrigger: Player exited trigger - Layer OK: {isCorrectLayer}, Tag OK: {isCorrectTag}, Triggered: {shouldTrigger}");
        }
    }

    // ADDED: Clean up player reference when disabled
    void OnDisable()
    {
        if (canvasVisible)
        {
            HideCanvas();
        }
        playerInTrigger = false;
    }

    // Clean up event listeners when destroyed
    void OnDestroy()
    {
        if (buttons != null)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }
    }

    // ADDED: Visualize trigger area in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        // Draw trigger collider if it exists
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(transform.position + box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Vector3 top = transform.position + capsule.center + Vector3.up * (capsule.height * 0.5f - capsule.radius);
                Vector3 bottom = transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius);
                Gizmos.DrawWireSphere(top, capsule.radius);
                Gizmos.DrawWireSphere(bottom, capsule.radius);
            }
        }
    }
}