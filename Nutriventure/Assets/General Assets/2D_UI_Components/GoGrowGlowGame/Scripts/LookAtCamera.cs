using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform _cameraTransform;

    void Start()
    {
        // Get the main camera's transform
        _cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Make the sprite face the camera
        transform.LookAt(_cameraTransform);
    }
}