using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using StarterAssets;

public class PushButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public ThirdPersonController playerController;
    public UICanvasControllerInput uiInput;
    public PushInteractionManager pushManager;

    [Header("Button Settings")]
    [Tooltip("Match this to Sprint button's pressed color for uniformity")]
    public Color normalColor = Color.white;
    public Color pushingColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color disabledColor = Color.gray;
    public float buttonHoldThreshold = 0.1f;

    private Button button;
    private Image buttonImage;
    private bool isHolding = false;
    private float holdTime = 0f;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // Find references if not assigned
        if (playerController == null)
            playerController = FindObjectOfType<ThirdPersonController>();

        if (uiInput == null)
            uiInput = FindObjectOfType<UICanvasControllerInput>();

        if (pushManager == null)
            pushManager = FindObjectOfType<PushInteractionManager>();

        UpdateButtonAppearance(false);
    }

    void Update()
    {
        if (playerController != null && button != null)
        {
            // Check if player can push (near object and facing it)
            bool canPush = playerController.CanPush() && !playerController.IsCrawling();
            button.interactable = canPush;

            // Update button appearance
            if (playerController.IsPushing())
            {
                buttonImage.color = pushingColor;
            }
            else if (!canPush)
            {
                buttonImage.color = disabledColor;
                isHolding = false; // Reset if can't push anymore
            }
            else
            {
                buttonImage.color = normalColor;
            }
        }

        // Handle hold time
        if (isHolding)
        {
            holdTime += Time.deltaTime;

            // Only activate pushing after holding for threshold time
            if (holdTime >= buttonHoldThreshold && uiInput != null)
            {
                uiInput.VirtualPushInput(true);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            isHolding = true;
            holdTime = 0f;
            Debug.Log("Push button pressed (starting hold)");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHolding)
        {
            isHolding = false;

            // Only send release if we were actually pushing
            if (holdTime >= buttonHoldThreshold && uiInput != null)
            {
                uiInput.VirtualPushInput(false);
                Debug.Log("Push button released (was pushing)");
            }
            else
            {
                Debug.Log("Push button released (tap, not hold)");
            }

            holdTime = 0f;
        }
    }

    void UpdateButtonAppearance(bool isPushing)
    {
        if (buttonImage != null)
        {
            buttonImage.color = isPushing ? pushingColor : normalColor;
        }
    }

    // For testing - also handle simple clicks
    public void OnPushButtonClick()
    {
        // This is for toggle mode if needed
        // For hold functionality, use the pointer handlers above
    }

    void OnDestroy()
    {
        // Ensure we stop pushing when button is destroyed
        if (uiInput != null)
        {
            uiInput.VirtualPushInput(false);
        }

        if (pushManager != null)
        {
            pushManager.StopAllPushing();
        }
    }
}