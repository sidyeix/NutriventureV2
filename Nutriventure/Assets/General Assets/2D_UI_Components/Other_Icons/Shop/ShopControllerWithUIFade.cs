using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class ShopControllerWithUIFade : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private CinemachineVirtualCamera shopCamera;

    [Header("UI References")]
    [SerializeField] private CanvasGroup shopCanvasGroup;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private CanvasGroup uiControllerCanvasGroup;

    [Header("Input Settings")]
    [SerializeField] private InputActionReference closeShopAction;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Coroutine fadeCoroutine;
    private bool isShopOpen = false;

    private void Start()
    {
        // Ensure shop camera is disabled initially
        if (shopCamera != null)
        {
            shopCamera.Priority = 0;
        }

        // Set up shop UI
        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.interactable = false;
            shopCanvasGroup.blocksRaycasts = false;
        }

        // Set up close button
        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(CloseShop);
        }

        // Ensure UI controller is active initially
        if (uiControllerCanvasGroup != null)
        {
            uiControllerCanvasGroup.alpha = 1f;
            uiControllerCanvasGroup.interactable = true;
            uiControllerCanvasGroup.blocksRaycasts = true;
        }
    }

    private void OnEnable()
    {
        if (closeShopAction != null)
        {
            closeShopAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (closeShopAction != null)
        {
            closeShopAction.action.Disable();
        }
    }

    // Called when shop button is clicked
    public void OpenShop()
    {
        if (isShopOpen) return;

        isShopOpen = true;

        if (SimpleStoreUI.Instance != null)
        {
            SimpleStoreUI.Instance.OnShopOpened();
            SimpleStoreUI.Instance.UpdateResourceDisplay();
        }

        // Fade out main UI controller
        if (uiControllerCanvasGroup != null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeUIController(1f, 0f));
        }

        // Activate shop camera
        if (shopCamera != null)
        {
            shopCamera.Priority = 30;
        }

        // Show shop UI with fade
        if (shopCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(shopCanvasGroup, 0f, 1f));
        }
    }

    // Called when closing the shop
    public void CloseShop()
    {
        if (!isShopOpen) return;

        isShopOpen = false;

        // Fade in main UI controller
        if (uiControllerCanvasGroup != null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeUIController(0f, 1f));
        }

        // Deactivate shop camera
        if (shopCamera != null)
        {
            shopCamera.Priority = 0;
        }

        // Hide shop UI with fade
        if (shopCanvasGroup != null)
        {
            StartCoroutine(FadeCanvasGroup(shopCanvasGroup, 1f, 0f));
        }
    }

    private void Update()
    {
        if (isShopOpen && closeShopAction != null && closeShopAction.action.triggered)
        {
            CloseShop();
        }
    }

    private IEnumerator FadeUIController(float startAlpha, float endAlpha)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            uiControllerCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        uiControllerCanvasGroup.alpha = endAlpha;

        if (endAlpha > 0.5f)
        {
            uiControllerCanvasGroup.interactable = true;
            uiControllerCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            uiControllerCanvasGroup.interactable = false;
            uiControllerCanvasGroup.blocksRaycasts = false;
        }

        fadeCoroutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha)
    {
        float time = 0f;

        if (endAlpha > startAlpha)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        group.alpha = endAlpha;

        if (endAlpha < startAlpha)
        {
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}