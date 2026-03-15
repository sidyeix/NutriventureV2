using UnityEngine;
using UnityEngine.EventSystems;

public class UI_SwipeLook : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float sensitivity = 0.2f;
    public bool invertX = false;
    public bool invertY = false;

    [Header("References")]
    public StarterAssets.UICanvasControllerInput uiInput;

    private Vector2 previousPosition;
    private bool pressing = false;
    private float dpiScale = 1f;

    // Reference DPI (average mobile). Devices with higher DPI need less raw delta.
    private const float REFERENCE_DPI = 326f;

    void Start()
    {
        // Load saved sensitivity from GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            sensitivity = GameDataManager.Instance.CurrentGameData.lookSensitivity;
        }

        // Scale sensitivity by screen DPI so swipe feel is consistent across devices
        float screenDpi = Screen.dpi;
        if (screenDpi > 0)
        {
            dpiScale = REFERENCE_DPI / screenDpi;
        }
    }

    /// <summary>Called externally (e.g. from ProfileSettings slider) to update sensitivity at runtime.</summary>
    public void SetSensitivity(float value)
    {
        sensitivity = value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
        previousPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!pressing) return;

        Vector2 delta = eventData.position - previousPosition;
        previousPosition = eventData.position;

        // Apply sensitivity with DPI compensation
        delta *= sensitivity * dpiScale;

        // Apply inversion
        if (invertX) delta.x = -delta.x;
        if (invertY) delta.y = -delta.y;

        // Send to Starter Assets
        uiInput.VirtualLookInput(delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
        uiInput.VirtualLookInput(Vector2.zero);
    }
}
