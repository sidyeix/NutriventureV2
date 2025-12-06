using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class K4_MiniMap : MonoBehaviour, IPointerClickHandler
{
    [Header("Minimap Settings")]
    public bool minimapEnabled = true;
    public float cameraHeight = 50f;
    public float cameraSize = 30f;
    public bool rotateWithPlayer = true;
    
    [Header("UI References")]
    public RawImage miniMapRenderImage;
    public RectTransform miniMapBorder;
    
    [Header("Visual Settings")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    public Color borderColor = new Color(1f, 1f, 1f, 0.8f);
    
    [Header("Player Settings")]
    public string playerTag = "Player";
    
    [Header("Mobile Controls")]
    public bool toggleOnTap = true;
    
    [Header("Input Settings")]
    public Key toggleKey = Key.M; // Using Input System Key instead of KeyCode
    
    // Private variables
    private Camera miniMapCamera;
    private RenderTexture miniMapRenderTexture;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isInitialized = false;
    private Keyboard keyboard; // Input System keyboard reference

    void Start()
    {
        InitializeMinimap();
    }

    void Update()
    {
        if (!isInitialized || !minimapEnabled) return;
        
        UpdateMinimapCamera();
        HandleInput();
    }

    private void InitializeMinimap()
    {
        // Find player automatically
        FindPlayer();
        if (playerTransform == null)
        {
            Debug.LogError("K4_MiniMap: Player not found! Make sure your player has the tag: " + playerTag);
            return;
        }

        // Find main camera automatically
        FindMainCamera();

        // Create minimap camera
        CreateMinimapCamera();

        // Create render texture
        CreateRenderTexture();

        // Set up UI
        SetupUI();

        // Initialize Input System
        keyboard = Keyboard.current;

        isInitialized = true;
        Debug.Log("K4_MiniMap: Initialized successfully!");
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log("K4_MiniMap: Found player: " + playerObj.name);
        }
    }

    private void FindMainCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // Try to find any active camera using the new method
            mainCamera = FindAnyObjectByType<Camera>();
        }
        
        if (mainCamera != null)
        {
            Debug.Log("K4_MiniMap: Found main camera: " + mainCamera.name);
        }
        else
        {
            Debug.LogWarning("K4_MiniMap: No main camera found. Rotation will be fixed.");
        }
    }

    private void CreateMinimapCamera()
    {
        GameObject cameraGO = new GameObject("K4_MiniMapCamera");
        cameraGO.transform.SetParent(transform);
        
        miniMapCamera = cameraGO.AddComponent<Camera>();
        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = cameraSize;
        miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
        miniMapCamera.backgroundColor = backgroundColor;
        
        // Set up culling mask to show most layers except UI
        miniMapCamera.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
        
        Debug.Log("K4_MiniMap: Created minimap camera");
    }

    private void CreateRenderTexture()
    {
        miniMapRenderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        miniMapRenderTexture.Create();
        miniMapCamera.targetTexture = miniMapRenderTexture;
        
        Debug.Log("K4_MiniMap: Created render texture");
    }

    private void SetupUI()
    {
        // Set up render image
        if (miniMapRenderImage != null)
        {
            miniMapRenderImage.texture = miniMapRenderTexture;
        }
        else
        {
            Debug.LogWarning("K4_MiniMap: MiniMapRenderImage not assigned!");
        }

        // Set up border
        if (miniMapBorder != null)
        {
            Image borderImage = miniMapBorder.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = borderColor;
            }
        }

        UpdateMinimapVisibility();
    }

    private void UpdateMinimapCamera()
    {
        if (playerTransform == null) return;

        // Update camera position (follow player)
        Vector3 newPosition = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + cameraHeight,
            playerTransform.position.z
        );
        miniMapCamera.transform.position = newPosition;

        // Update camera rotation
        if (rotateWithPlayer && mainCamera != null)
        {
            float cameraRotation = mainCamera.transform.eulerAngles.y;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, cameraRotation, 0f);
        }
        else
        {
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void HandleInput()
    {
        // Use Input System for keyboard input
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            ToggleMinimap();
        }
    }

    // Mobile tap support
    public void OnPointerClick(PointerEventData eventData)
    {
        if (toggleOnTap)
        {
            ToggleMinimap();
        }
    }

    public void ToggleMinimap()
    {
        minimapEnabled = !minimapEnabled;
        UpdateMinimapVisibility();
        Debug.Log("K4_MiniMap: " + (minimapEnabled ? "Enabled" : "Disabled"));
    }

    private void UpdateMinimapVisibility()
    {
        if (miniMapRenderImage != null) 
            miniMapRenderImage.gameObject.SetActive(minimapEnabled);
        if (miniMapBorder != null) 
            miniMapBorder.gameObject.SetActive(minimapEnabled);
        if (miniMapCamera != null)
            miniMapCamera.gameObject.SetActive(minimapEnabled);
    }

    // Public methods for external control
    public void EnableMinimap() => SetMinimapEnabled(true);
    public void DisableMinimap() => SetMinimapEnabled(false);
    
    public void SetMinimapEnabled(bool enabled)
    {
        minimapEnabled = enabled;
        UpdateMinimapVisibility();
    }

    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        Debug.Log("K4_MiniMap: Camera height set to " + height);
    }

    public void SetCameraSize(float size)
    {
        cameraSize = size;
        if (miniMapCamera != null)
        {
            miniMapCamera.orthographicSize = size;
        }
        Debug.Log("K4_MiniMap: Camera size set to " + size);
    }

    // Clean up
    private void OnDestroy()
    {
        if (miniMapRenderTexture != null)
        {
            miniMapRenderTexture.Release();
            Destroy(miniMapRenderTexture);
        }
    }

    [ContextMenu("Debug Minimap Info")]
    public void DebugMinimapInfo()
    {
        Debug.Log("=== K4_MiniMap Debug Info ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Enabled: {minimapEnabled}");
        Debug.Log($"Player Found: {playerTransform != null}");
        Debug.Log($"Main Camera Found: {mainCamera != null}");
        Debug.Log($"MiniMap Camera: {miniMapCamera != null}");
        Debug.Log($"Render Texture: {miniMapRenderTexture != null}");
        Debug.Log($"Keyboard Available: {keyboard != null}");
        
        if (playerTransform != null)
        {
            Debug.Log($"Player Position: {playerTransform.position}");
        }
    }

    [ContextMenu("Reinitialize Minimap")]
    public void Reinitialize()
    {
        // Clean up old objects
        if (miniMapCamera != null) Destroy(miniMapCamera.gameObject);
        if (miniMapRenderTexture != null)
        {
            miniMapRenderTexture.Release();
            Destroy(miniMapRenderTexture);
        }

        // Reinitialize
        isInitialized = false;
        InitializeMinimap();
    }
}