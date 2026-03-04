using UnityEngine;
using UnityEngine.InputSystem;

public class ClickDebugger : MonoBehaviour
{
    private Camera cachedCamera;

    void Update()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) return;

        // Check for mouse click (null-safe for mobile)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = cachedCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"Clicked: {hit.collider.gameObject.name} at {Time.time}");
            }
            else
            {
                Debug.Log($"Missed click at {Mouse.current.position.ReadValue()}");
            }
        }

        // Check for touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
            Ray ray = cachedCamera.ScreenPointToRay(touchPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"Touched: {hit.collider.gameObject.name}");
            }
        }
    }
}