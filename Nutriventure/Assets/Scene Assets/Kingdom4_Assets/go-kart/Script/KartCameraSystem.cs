using UnityEngine;
using Cinemachine;

public class KartCameraSystem : MonoBehaviour
{
    [Header("Camera References")]
    public CinemachineVirtualCamera kartFollowCamera;
    
    [Header("Kart Reference")]
    public KartController kartController;
    
    private CinemachineVirtualCamera playerFollowCamera;
    private GameObject mainCamera;

    private void Start()
    {
        // Find main camera by tag
        FindMainCamera();
        
        // Find player follow camera automatically
        FindPlayerFollowCamera();
        
        // Ensure kart camera is disabled initially
        if (kartFollowCamera != null)
            kartFollowCamera.gameObject.SetActive(false);
            
        // Ensure player camera is enabled initially
        if (playerFollowCamera != null)
            playerFollowCamera.gameObject.SetActive(true);
    }
    
    private void Update()
    {
        // Try to find cameras if they're null
        if (playerFollowCamera == null)
        {
            FindPlayerFollowCamera();
        }
        
        if (mainCamera == null)
        {
            FindMainCamera();
        }

        if (kartController == null) return;
        
        bool isDriving = kartController.enabled;
        
        // Switch cameras based on driving state
        if (kartFollowCamera != null && kartFollowCamera.gameObject.activeSelf != isDriving)
        {
            kartFollowCamera.gameObject.SetActive(isDriving);
        }
        
        if (playerFollowCamera != null && playerFollowCamera.gameObject.activeSelf == isDriving)
        {
            playerFollowCamera.gameObject.SetActive(!isDriving);
        }
    }
    
    private void FindMainCamera()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamera != null)
        {
            Debug.Log("✅ Main camera found by tag!");
        }
        else
        {
            Debug.LogWarning("⚠️ No GameObject with 'MainCamera' tag found!");
        }
    }
    
    private void FindPlayerFollowCamera()
    {
        // Use the new FindAnyObjectByType method
        CinemachineVirtualCamera cam = FindAnyObjectByType<CinemachineVirtualCamera>();
        
        if (cam != null && cam != kartFollowCamera)
        {
            playerFollowCamera = cam;
            Debug.Log("✅ Player follow camera found automatically!");
            return;
        }
        
        // Method 2: If still not found, try to find by common naming patterns
        GameObject playerCamObj = GameObject.Find("PlayerFollowCamera") 
                                ?? GameObject.Find("ThirdPersonCamera")
                                ?? GameObject.Find("Player Camera");
        
        if (playerCamObj != null)
        {
            playerFollowCamera = playerCamObj.GetComponent<CinemachineVirtualCamera>();
            if (playerFollowCamera != null)
            {
                Debug.Log("✅ Player follow camera found by name!");
                return;
            }
        }
        
        Debug.LogWarning("⚠️ Could not automatically find player follow camera!");
    }
    
    public void SetKartCameraTarget(Transform target)
    {
        if (kartFollowCamera != null)
        {
            kartFollowCamera.Follow = target;
            kartFollowCamera.LookAt = target;
        }
    }
    
    // Optional: Manual assignment if auto-find fails
    public void SetPlayerFollowCamera(CinemachineVirtualCamera playerCam)
    {
        playerFollowCamera = playerCam;
        if (playerFollowCamera != null)
        {
            Debug.Log("✅ Player follow camera assigned manually!");
        }
    }
}