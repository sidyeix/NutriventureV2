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

        // Apply sensitivity
        delta *= sensitivity;

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
