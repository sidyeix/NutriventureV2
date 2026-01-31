using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class EnerlingCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera playerFollowCamera;
    
    [Header("To Disable Components")]
    [Tooltip("List of GameObjects to disable when interacting with Enerlings")]
    public List<GameObject> toDisableComponents = new List<GameObject>();
    
    private Test_EnerlingController currentEnerling;
    private bool isViewingEnerling = false;
    
    void Start()
    {
        // Ensure player camera is properly set up
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 10;
        }
    }
    
    public void StartViewingEnerling(Test_EnerlingController enerling)
    {
        if (enerling == null || isViewingEnerling) return;
        
        currentEnerling = enerling;
        isViewingEnerling = true;
        
        // Pause ALL other enerlings (stop their movement completely)
        Test_EnerlingController.PauseAllEnerlings();
        
        // Disable specified components
        DisableComponents();
        
        // Get the specific virtual camera from this enerling
        CinemachineVirtualCamera enerlingVirtualCamera = currentEnerling.GetVirtualCamera();
        
        if (enerlingVirtualCamera != null)
        {
            // Switch to enerling camera by setting priority
            SwitchToEnerlingCamera(enerlingVirtualCamera);
            
            // Start interaction with this enerling
            currentEnerling.StartInteraction(enerlingVirtualCamera);
        }
        else
        {
            // If no virtual camera, just start interaction
            currentEnerling.StartInteraction();
        }
        
        Debug.Log($"Started viewing Enerling: {currentEnerling.gameObject.name}");
    }
    
    public void StopViewingEnerling()
    {
        if (!isViewingEnerling) return;
        
        isViewingEnerling = false;
        
        // End interaction with current enerling FIRST
        if (currentEnerling != null)
        {
            currentEnerling.EndInteraction();
            
            // Switch back to player camera
            SwitchToPlayerCamera();
            
            Debug.Log($"Stopped viewing Enerling: {currentEnerling.gameObject.name}");
        }
        
        // Then resume movement for ALL enerlings (including the current one)
        Test_EnerlingController.ResumeAllEnerlings();
        
        // Re-enable specified components
        EnableComponents();
        
        currentEnerling = null;
    }
    
    private void DisableComponents()
    {
        foreach (GameObject component in toDisableComponents)
        {
            if (component != null)
            {
                component.SetActive(false);
            }
        }
    }
    
    private void EnableComponents()
    {
        foreach (GameObject component in toDisableComponents)
        {
            if (component != null)
            {
                component.SetActive(true);
            }
        }
    }
    
    private void SwitchToPlayerCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 10;
        }
    }
    
    private void SwitchToEnerlingCamera(CinemachineVirtualCamera enerlingCamera)
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
        }
        
        if (enerlingCamera != null)
        {
            enerlingCamera.Priority = 20;
        }
    }
    
    public bool IsViewingEnerling()
    {
        return isViewingEnerling;
    }
    
    // Method to add a GameObject to the disable list dynamically
    public void AddComponentToDisableList(GameObject component)
    {
        if (component != null && !toDisableComponents.Contains(component))
        {
            toDisableComponents.Add(component);
            Debug.Log($"Added {component.name} to disable list");
        }
    }
    
    // Method to remove a GameObject from the disable list
    public void RemoveComponentFromDisableList(GameObject component)
    {
        if (toDisableComponents.Contains(component))
        {
            toDisableComponents.Remove(component);
            Debug.Log($"Removed {component.name} from disable list");
        }
    }
}