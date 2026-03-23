using UnityEngine;
using Cinemachine;
using System.Collections;

public class KartCameraSystem : MonoBehaviour
{
    private const int KartCameraActivePriority = 50;
    private const int KartCameraInactivePriority = 0;

    [Header("Camera References")]
    public CinemachineVirtualCamera kartFollowCamera;

    [Header("Kart Reference")]
    public KartController kartController;

    [Header("Trigger Reference")]
    [SerializeField] private KartTrigger kartTrigger;

    [SerializeField] private CinemachineVirtualCamera playerFollowCamera;
    private GameObject mainCamera;
    private bool isFindingCameras = false;

    private void Start()
    {
        // Find main camera by tag
        FindMainCamera();

        // Find player follow camera automatically
        FindPlayerFollowCamera();

        if (kartTrigger == null)
        {
            kartTrigger = FindAnyObjectByType<KartTrigger>();
        }

        // Keep both camera objects active; use priority only for switching.
        if (kartFollowCamera != null)
        {
            kartFollowCamera.Priority = KartCameraInactivePriority;
        }
    }

    private void Update()
    {
        // Retry finding cameras via coroutine instead of per-frame Find calls
        if ((playerFollowCamera == null || mainCamera == null) && !isFindingCameras)
        {
            StartCoroutine(RetryFindCameras());
        }

        bool isDriving = kartController != null && kartController.enabled;
        bool isInKartCountdown = kartTrigger != null && kartTrigger.IsKartCameraMode;
        bool useKartCamera = isDriving || isInKartCountdown;

        if (kartFollowCamera != null)
        {
            kartFollowCamera.Priority = useKartCamera ? KartCameraActivePriority : KartCameraInactivePriority;
        }

        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = useKartCamera ? KartCameraInactivePriority : KartCameraActivePriority;
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
        if (playerFollowCamera != null)
        {
            return;
        }

        // Prefer common player camera names first.
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

        // Fallback: choose a non-kart virtual camera with highest current priority.
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>();
        int bestPriority = int.MinValue;
        for (int i = 0; i < cameras.Length; i++)
        {
            CinemachineVirtualCamera candidate = cameras[i];
            if (candidate == null || candidate == kartFollowCamera)
                continue;

            if (candidate.Priority > bestPriority)
            {
                bestPriority = candidate.Priority;
                playerFollowCamera = candidate;
            }
        }

        if (playerFollowCamera != null)
        {
            Debug.Log($"✅ Player follow camera fallback selected: {playerFollowCamera.name}");
            return;
        }

        Debug.LogWarning("⚠️ Could not automatically find player follow camera!");
    }

    private IEnumerator RetryFindCameras()
    {
        isFindingCameras = true;
        yield return new WaitForSeconds(1f);

        if (playerFollowCamera == null)
            FindPlayerFollowCamera();
        if (mainCamera == null)
            FindMainCamera();

        isFindingCameras = false;
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