using UnityEngine;
using Cinemachine;

public class EnerlingCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera playerFollowCamera;
    public CinemachineVirtualCamera enerlingViewCamera;
    
    [Header("Camera Settings")]
    public float cameraSwitchTime = 0.5f;
    
    [Header("First Person View Settings")]
    public Vector3 firstPersonOffset = new Vector3(0, 1.7f, 0); // Player eye level
    public float minCameraDistance = 2f;
    public float maxCameraDistance = 5f;
    public float defaultCameraDistance = 3f;
    
    private Test_EnerlingController currentEnerling;
    private bool isViewingEnerling = false;
    private GameObject player;
    
    void Start()
    {
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Ensure cameras are properly set up
        if (playerFollowCamera != null && enerlingViewCamera != null)
        {
            // Start with player camera
            SwitchToPlayerCamera();
        }
        else
        {
            Debug.LogError("Virtual cameras not assigned!");
        }
    }
    
    public void StartViewingEnerling(Test_EnerlingController enerling)
    {
        if (enerling == null || isViewingEnerling || player == null) return;
        
        currentEnerling = enerling;
        isViewingEnerling = true;
        
        // Position camera at player's location (first-person view)
        if (enerlingViewCamera != null && currentEnerling != null)
        {
            // Camera is positioned at player's location with offset
            Vector3 cameraPosition = player.transform.position + firstPersonOffset;
            enerlingViewCamera.transform.position = cameraPosition;
            
            // Camera looks at the Enerling
            enerlingViewCamera.LookAt = currentEnerling.transform;
            
            // Switch to enerling camera
            SwitchToEnerlingCamera();
        }
        
        Debug.Log($"Started viewing Enerling: {currentEnerling.gameObject.name} from player position");
    }
    
    public void StopViewingEnerling()
    {
        if (!isViewingEnerling) return;
        
        isViewingEnerling = false;
        
        // Switch back to player camera
        SwitchToPlayerCamera();
        
        if (currentEnerling != null)
        {
            Debug.Log($"Stopped viewing Enerling: {currentEnerling.gameObject.name}");
            currentEnerling = null;
        }
    }
    
    private void SwitchToPlayerCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 10;
        }
        
        if (enerlingViewCamera != null)
        {
            enerlingViewCamera.Priority = 0;
        }
    }
    
    private void SwitchToEnerlingCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
        }
        
        if (enerlingViewCamera != null)
        {
            enerlingViewCamera.Priority = 10;
        }
    }
    
    public bool IsViewingEnerling()
    {
        return isViewingEnerling;
    }
    
    // Method to adjust camera distance (can be called from UI if needed)
    public void SetCameraDistance(float distance)
    {
        if (enerlingViewCamera != null)
        {
            distance = Mathf.Clamp(distance, minCameraDistance, maxCameraDistance);
            
            // Adjust FOV or other settings if needed
            // enerlingViewCamera.m_Lens.FieldOfView = Mathf.Lerp(40f, 60f, distance / maxCameraDistance);
        }
    }
}