using UnityEngine;

public class CanvasFaceCamera : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogWarning("No main camera found. Looking for any camera.");
            Camera cam = FindObjectOfType<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform != null)
        {
            // Rotate to face the camera
            transform.rotation = cameraTransform.rotation;
        }
    }
}