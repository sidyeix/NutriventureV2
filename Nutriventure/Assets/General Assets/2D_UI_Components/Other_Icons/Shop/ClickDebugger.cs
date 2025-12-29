using UnityEngine;
using UnityEngine.InputSystem;

public class ClickDebugger : MonoBehaviour
{
    void Update()
    {
        // Check for mouse click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
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
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"Touched: {hit.collider.gameObject.name}");
            }
        }
    }
}