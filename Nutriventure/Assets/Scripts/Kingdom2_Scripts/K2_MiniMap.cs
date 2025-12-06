using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cinemachine;
using System.Collections.Generic;

public class K2_MiniMap : MonoBehaviour, IPointerClickHandler
{
    [Header("Minimap Settings")]
    public Camera miniMapCamera;
    public Transform playerTransform;
    public CinemachineVirtualCamera playerFollowCamera;
    public float cameraHeight = 50f;
    public float cameraSize = 30f;
    public bool rotateWithCamera = true;
    
    [Header("UI Elements")]
    public RawImage miniMapRenderImage;
    public RectTransform miniMapBorder;
    
    [Header("Icon Settings")]
    public bool enableIconSystem = true;
    public Sprite monsterIconSprite;
    public float monsterIconSize = 15f;
    public Color monsterIconColor = Color.red;
    public bool showMonsterIcons = true;
    
    [Header("Layer Settings")]
    public LayerMask minimapLayers = -1;
    public bool enableLayerFiltering = true;
    public string particleSystemTag = "ParticleSystem";
    public int excludedLayer = 31;
    
    [Header("Mobile Controls")]
    public bool toggleOnTap = true;
    public bool minimapEnabled = true;
    
    [Header("Minimap Visual Settings")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
    
    private RenderTexture miniMapRenderTexture;
    private bool isInitialized = false;
    private InputAction toggleAction;
    private Camera mainCamera;
    
    // Icon tracking
    private List<Transform> trackedObjects = new List<Transform>();
    private List<RectTransform> objectIcons = new List<RectTransform>();
    private GameObject iconsContainer;

    void Start()
    {
        InitializeMinimap();
        SetupInputSystem();
        
        if (enableIconSystem)
        {
            SetupIconSystem();
        }
        
        if (enableLayerFiltering)
        {
            SetupLayerFiltering();
        }
    }

    void Update()
    {
        if (!isInitialized) return;
        
        UpdateMinimapCamera();
        
        if (enableIconSystem && showMonsterIcons)
        {
            UpdateObjectIcons();
        }
    }

    private void InitializeMinimap()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("Player found automatically: " + player.name);
            }
            else
            {
                // Create a dummy player transform if none exists
                GameObject dummyPlayer = new GameObject("DummyPlayer");
                playerTransform = dummyPlayer.transform;
                Debug.LogWarning("No player found. Created dummy player transform for minimap.");
            }
        }

        // Find Cinemachine virtual camera and get the actual camera
        if (playerFollowCamera == null)
        {
            playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (playerFollowCamera != null)
            {
                Debug.Log("Cinemachine virtual camera found automatically: " + playerFollowCamera.name);
            }
        }

        // Get the actual Unity Camera from the Cinemachine virtual camera
        if (playerFollowCamera != null)
        {
            mainCamera = playerFollowCamera.GetComponentInChildren<Camera>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("Could not find main camera. Minimap will use fixed orientation.");
                }
                else
                {
                    Debug.Log("Using main camera: " + mainCamera.name);
                }
            }
            else
            {
                Debug.Log("Found camera from Cinemachine virtual camera: " + mainCamera.name);
            }
        }
        else
        {
            Debug.LogWarning("Cinemachine virtual camera not found! Minimap will use fixed north-up orientation.");
        }

        // Create minimap camera if not assigned
        if (miniMapCamera == null)
        {
            CreateMinimapCamera();
        }

        // Create render texture if not assigned
        if (miniMapRenderTexture == null)
        {
            CreateRenderTexture();
        }

        // Set up camera
        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = cameraSize;
        miniMapCamera.transform.position = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + cameraHeight,
            playerTransform.position.z
        );
        
        miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        miniMapCamera.targetTexture = miniMapRenderTexture;
        miniMapCamera.backgroundColor = backgroundColor;
        miniMapCamera.cullingMask = minimapLayers;

        // Set up UI image
        if (miniMapRenderImage != null)
        {
            miniMapRenderImage.texture = miniMapRenderTexture;
        }

        // Set up border color
        if (miniMapBorder != null)
        {
            Image borderImage = miniMapBorder.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = borderColor;
            }
        }

        isInitialized = true;
        Debug.Log("Minimap initialized successfully!");
        Debug.Log("Minimap mode: " + (rotateWithCamera ? "Camera-oriented" : "North-up fixed"));
    }

    private void SetupIconSystem()
    {
        // Create container for icons
        if (miniMapBorder != null)
        {
            iconsContainer = new GameObject("MinimapIcons");
            iconsContainer.transform.SetParent(miniMapBorder);
            RectTransform containerRT = iconsContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;
        }

        // Optional: Auto-find monsters if enabled
        if (showMonsterIcons)
        {
            FindAllTrackedObjects();
        }
    }

    private void FindAllTrackedObjects()
    {
        // Clear existing lists
        trackedObjects.Clear();
        foreach (var icon in objectIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        objectIcons.Clear();

        // Find objects with common enemy/monster tags
        string[] possibleTags = { "K2_Monster", "Monster", "Enemy", "K2_Enemy" };
        
        foreach (string tag in possibleTags)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                GameObject[] foundObjects = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject obj in foundObjects)
                {
                    if (obj != null && !trackedObjects.Contains(obj.transform))
                    {
                        trackedObjects.Add(obj.transform);
                        CreateObjectIcon(obj.transform);
                    }
                }
                
                if (foundObjects.Length > 0)
                {
                    Debug.Log($"Found {foundObjects.Length} objects with tag: {tag}");
                    break; // Use the first tag that finds objects
                }
            }
        }

        Debug.Log($"Total tracked objects: {trackedObjects.Count}");
    }

    private void CreateObjectIcon(Transform objectTransform)
    {
        if (iconsContainer == null || monsterIconSprite == null) return;

        // Create UI Image for object icon
        GameObject iconGO = new GameObject($"Icon_{objectTransform.name}");
        iconGO.transform.SetParent(iconsContainer.transform);
        
        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.sprite = monsterIconSprite;
        iconImage.color = monsterIconColor;
        
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(monsterIconSize, monsterIconSize);
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        
        objectIcons.Add(iconRT);
    }

    private void SetupLayerFiltering()
    {
        // Check if the excluded layer is valid
        if (excludedLayer < 0 || excludedLayer > 31)
        {
            Debug.LogWarning("Excluded layer is out of range. Using layer 31 instead.");
            excludedLayer = 31;
        }

        // Hide particle systems from minimap
        if (!string.IsNullOrEmpty(particleSystemTag))
        {
            GameObject[] particleSystems = GameObject.FindGameObjectsWithTag(particleSystemTag);
            
            foreach (GameObject ps in particleSystems)
            {
                if (ps != null)
                {
                    try
                    {
                        SetLayerRecursively(ps, excludedLayer);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Could not set layer for {ps.name}: {e.Message}");
                    }
                }
            }

            Debug.Log($"Processed {particleSystems.Length} particle systems for minimap filtering");
        }

        // Update minimap camera layer mask to exclude the particle layer
        if (miniMapCamera != null)
        {
            minimapLayers &= ~(1 << excludedLayer);
            miniMapCamera.cullingMask = minimapLayers;
            Debug.Log($"Minimap camera culling mask set to: {minimapLayers.value}");
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        if (layer < 0 || layer > 31)
        {
            Debug.LogWarning($"Invalid layer {layer} for object {obj.name}. Using layer 0 instead.");
            layer = 0;
        }
        
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    private void UpdateObjectIcons()
    {
        if (objectIcons.Count != trackedObjects.Count) return;

        for (int i = 0; i < trackedObjects.Count; i++)
        {
            if (trackedObjects[i] == null || objectIcons[i] == null) continue;

            // Convert world position to minimap UI position
            Vector3 objectWorldPos = trackedObjects[i].position;
            Vector3 viewportPos = miniMapCamera.WorldToViewportPoint(objectWorldPos);
            
            // Convert viewport position to UI coordinates
            RectTransform borderRT = miniMapBorder;
            Vector2 uiPos = new Vector2(
                (viewportPos.x - 0.5f) * borderRT.rect.width,
                (viewportPos.y - 0.5f) * borderRT.rect.height
            );
            
            // Only show icons that are within the minimap bounds
            bool isInBounds = viewportPos.x >= 0f && viewportPos.x <= 1f && 
                             viewportPos.y >= 0f && viewportPos.y <= 1f && 
                             viewportPos.z > 0f;
            
            objectIcons[i].gameObject.SetActive(isInBounds);
            
            if (isInBounds)
            {
                // Update icon position
                objectIcons[i].anchoredPosition = uiPos;
                
                // Update icon rotation
                if (rotateWithCamera && mainCamera != null)
                {
                    float objectRotation = trackedObjects[i].eulerAngles.y;
                    float cameraRotation = mainCamera.transform.eulerAngles.y;
                    float relativeRotation = objectRotation - cameraRotation;
                    objectIcons[i].rotation = Quaternion.Euler(0f, 0f, -relativeRotation);
                }
                else
                {
                    objectIcons[i].rotation = Quaternion.Euler(0f, 0f, -trackedObjects[i].eulerAngles.y);
                }
            }
        }
    }

    // Public API for dynamic object tracking
    public void AddTrackedObject(Transform objectTransform)
    {
        if (objectTransform != null && !trackedObjects.Contains(objectTransform))
        {
            trackedObjects.Add(objectTransform);
            CreateObjectIcon(objectTransform);
            Debug.Log($"Added object to minimap: {objectTransform.name}");
        }
    }

    public void RemoveTrackedObject(Transform objectTransform)
    {
        int index = trackedObjects.IndexOf(objectTransform);
        if (index >= 0)
        {
            trackedObjects.RemoveAt(index);
            if (index < objectIcons.Count && objectIcons[index] != null)
            {
                Destroy(objectIcons[index].gameObject);
                objectIcons.RemoveAt(index);
            }
            Debug.Log($"Removed object from minimap: {objectTransform.name}");
        }
    }

    public void ClearAllTrackedObjects()
    {
        trackedObjects.Clear();
        foreach (var icon in objectIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        objectIcons.Clear();
        Debug.Log("Cleared all tracked objects from minimap");
    }

    public void RefreshTrackedObjects()
    {
        if (enableIconSystem)
        {
            FindAllTrackedObjects();
        }
        Debug.Log("Refreshed tracked objects for minimap");
    }

    public void SetIconVisibility(bool visible)
    {
        showMonsterIcons = visible;
        if (iconsContainer != null)
        {
            iconsContainer.SetActive(visible && minimapEnabled);
        }
        Debug.Log("Icons visibility: " + visible);
    }

    public void SetIconColor(Color color)
    {
        monsterIconColor = color;
        foreach (var icon in objectIcons)
        {
            if (icon != null)
            {
                Image iconImage = icon.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.color = color;
                }
            }
        }
        Debug.Log("Icon color updated");
    }

    // Scene management
    public void OnSceneLoaded()
    {
        Debug.Log("Minimap refreshing for new scene...");
        
        // Refresh camera references
        if (playerFollowCamera == null)
        {
            playerFollowCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Refresh tracked objects if system is enabled
        if (enableIconSystem)
        {
            RefreshTrackedObjects();
        }
        
        Debug.Log("Minimap refreshed for new scene");
    }

    // Input and core functionality
    private void SetupInputSystem()
    {
        toggleAction = new InputAction("ToggleMinimap", InputActionType.Button);
        toggleAction.AddBinding("<Keyboard>/m");
        toggleAction.performed += ctx => ToggleMinimap();
        toggleAction.Enable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (toggleOnTap) ToggleMinimap();
    }

    private void CreateMinimapCamera()
    {
        GameObject cameraGO = new GameObject("MinimapCamera");
        cameraGO.transform.SetParent(transform);
        miniMapCamera = cameraGO.AddComponent<Camera>();
        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = cameraSize;
        miniMapCamera.cullingMask = minimapLayers;
        miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
        miniMapCamera.backgroundColor = backgroundColor;
    }

    private void CreateRenderTexture()
    {
        miniMapRenderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        miniMapRenderTexture.Create();
    }

    private void UpdateMinimapCamera()
    {
        if (playerTransform == null) return;

        Vector3 newPosition = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + cameraHeight,
            playerTransform.position.z
        );
        miniMapCamera.transform.position = newPosition;

        if (rotateWithCamera && mainCamera != null)
        {
            float cameraRotation = mainCamera.transform.eulerAngles.y;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, cameraRotation, 0f);
        }
        else
        {
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void ToggleMinimap()
    {
        minimapEnabled = !minimapEnabled;
        UpdateMinimapVisibility();
    }

    private void UpdateMinimapVisibility()
    {
        if (miniMapRenderImage != null) miniMapRenderImage.gameObject.SetActive(minimapEnabled);
        if (miniMapBorder != null) miniMapBorder.gameObject.SetActive(minimapEnabled);
        if (iconsContainer != null) iconsContainer.SetActive(minimapEnabled && showMonsterIcons);
    }

    // Clean up
    private void OnDestroy()
    {
        if (toggleAction != null)
        {
            toggleAction.Disable();
            toggleAction.Dispose();
        }
        if (miniMapRenderTexture != null)
        {
            miniMapRenderTexture.Release();
            Destroy(miniMapRenderTexture);
        }
    }

    [ContextMenu("Debug Minimap Info")]
    public void DebugMinimapInfo()
    {
        Debug.Log("=== Minimap Debug Info ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Minimap Enabled: {minimapEnabled}");
        Debug.Log($"Icon System: {enableIconSystem}");
        Debug.Log($"Tracked Objects: {trackedObjects.Count}");
        Debug.Log($"Layer Filtering: {enableLayerFiltering}");
        Debug.Log($"Rotation Mode: {(rotateWithCamera ? "Camera-oriented" : "North-up fixed")}");
    }

    [ContextMenu("Refresh Minimap")]
    public void DebugRefreshMinimap()
    {
        OnSceneLoaded();
    }
}