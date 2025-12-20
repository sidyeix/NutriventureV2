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
    private bool playerInTrigger = false; // Track if player is in trigger

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
                int index = i; // Capture index for closure
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
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

    // This is called when any button is clicked
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

    void OnTriggerEnter(Collider other)
    {
        // Alternative trigger method using collider
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInTrigger = true;
            if (!canvasVisible)
            {
                ShowCanvas();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            playerInTrigger = false;
            if (canvasVisible)
            {
                HideCanvas();
            }
        }
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
}