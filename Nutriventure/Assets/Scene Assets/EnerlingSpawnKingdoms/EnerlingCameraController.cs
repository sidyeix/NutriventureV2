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
    private List<Test_EnerlingController> otherEnerlings = new List<Test_EnerlingController>();
    
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
        
        // Get all other enerlings in scene
        FindAllOtherEnerlings();
        
        // Disable other enerlings
        DisableOtherEnerlings();
        
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
        
        Debug.Log($"Started viewing Enerling: {currentEnerling.gameObject.name}");
    }
    
    public void StopViewingEnerling()
    {
        if (!isViewingEnerling) return;
        
        isViewingEnerling = false;
        
        // Re-enable other enerlings
        EnableOtherEnerlings();
        
        // Re-enable specified components
        EnableComponents();
        
        // End interaction with current enerling
        if (currentEnerling != null)
        {
            currentEnerling.EndInteraction();
            
            // Switch back to player camera
            SwitchToPlayerCamera();
            
            Debug.Log($"Stopped viewing Enerling: {currentEnerling.gameObject.name}");
            currentEnerling = null;
        }
    }
    
    private void FindAllOtherEnerlings()
    {
        otherEnerlings.Clear();
        Test_EnerlingController[] allEnerlings = FindObjectsOfType<Test_EnerlingController>();
        
        foreach (var enerling in allEnerlings)
        {
            if (enerling != currentEnerling)
            {
                otherEnerlings.Add(enerling);
            }
        }
    }
    
    private void DisableOtherEnerlings()
    {
        foreach (var enerling in otherEnerlings)
        {
            if (enerling != null && enerling.gameObject != null)
            {
                // Store current state before disabling
                enerling.gameObject.SetActive(false);
            }
        }
    }
    
    private void EnableOtherEnerlings()
    {
        foreach (var enerling in otherEnerlings)
        {
            if (enerling != null && enerling.gameObject != null)
            {
                enerling.gameObject.SetActive(true);
            }
        }
        otherEnerlings.Clear();
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
}