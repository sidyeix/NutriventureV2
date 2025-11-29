// DebugChestTester.cs - Updated for Input System
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugChestTester : MonoBehaviour
{
    private void OnMouseEnter()
    {
        Debug.Log("? MOUSE ENTERED chest: " + gameObject.name);
    }

    private void OnMouseExit()
    {
        Debug.Log("? MOUSE EXITED chest: " + gameObject.name);
    }

    private void OnMouseOver()
    {
        // This fires every frame while mouse is over the object
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("?? MOUSE CLICK DETECTED via OnMouseOver on: " + gameObject.name);
        }
    }

    private void OnMouseDown()
    {
        Debug.Log("?? MOUSE DOWN on chest: " + gameObject.name);
    }

    private void Update()
    {
        // Manual raycast test - press Space to test (using Input System)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TestRaycast();
        }
    }

    private void TestRaycast()
    {
        if (Camera.main == null)
        {
            Debug.LogError("? Main camera not found!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        Debug.Log("?? Testing raycast from mouse position: " + Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log("?? Raycast HIT: " + hit.collider.gameObject.name + " at distance: " + hit.distance);
            if (hit.collider.gameObject == gameObject)
            {
                Debug.Log("? SUCCESS: Chest was hit by raycast!");
            }
        }
        else
        {
            Debug.Log("? Raycast missed everything");
        }
    }
}