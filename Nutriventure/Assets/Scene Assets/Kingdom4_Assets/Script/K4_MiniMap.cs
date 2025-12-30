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
    public float borderThickness = 2f;
    
    [Header("Player Settings")]
    public string playerTag = "Player";
    public Sprite playerArrowSprite;
    public Color playerArrowColor = Color.blue;
    public float playerArrowSize = 20f;
    public bool alwaysShowPlayerArrow = true;
    
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

    // Circular mask components
    private Material circleMaskMaterial;
    private Image circularMaskImage;
    private GameObject maskObject;
    
    // Player arrow
    private RectTransform playerArrow;
    private Image playerArrowImage;
    private GameObject playerArrowObject;

    void Start()
    {
        InitializeMinimap();
    }

    void Update()
    {
        if (!isInitialized || !minimapEnabled) return;
        
        UpdateMinimapCamera();
        HandleInput();
        UpdatePlayerArrow();
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

        // Set up UI with circular mask
        SetupCircularUI();

        // Set up player arrow
        SetupPlayerArrow();

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

    private void SetupCircularUI()
    {
        // Create or setup circular mask
        SetupCircularMask();

        // Set up render image inside the mask
        if (miniMapRenderImage != null)
        {
            miniMapRenderImage.texture = miniMapRenderTexture;
            
            // Make the RawImage follow the mask's shape
            miniMapRenderImage.maskable = true;
            
            // Parent the render image to the mask if it exists
            if (maskObject != null)
            {
                miniMapRenderImage.transform.SetParent(maskObject.transform, false);
                miniMapRenderImage.rectTransform.anchorMin = Vector2.zero;
                miniMapRenderImage.rectTransform.anchorMax = Vector2.one;
                miniMapRenderImage.rectTransform.offsetMin = Vector2.zero;
                miniMapRenderImage.rectTransform.offsetMax = Vector2.zero;
            }
        }
        else
        {
            Debug.LogWarning("K4_MiniMap: MiniMapRenderImage not assigned!");
        }

        // Set up circular border
        if (miniMapBorder != null)
        {
            Image borderImage = miniMapBorder.GetComponent<Image>();
            if (borderImage != null)
            {
                borderImage.color = borderColor;
                borderImage.pixelsPerUnitMultiplier = 0.5f; // Makes the border smoother
            }
            
            // Make border circular
            miniMapBorder.sizeDelta = new Vector2(miniMapBorder.sizeDelta.x + borderThickness * 2, 
                                                miniMapBorder.sizeDelta.y + borderThickness * 2);
        }

        UpdateMinimapVisibility();
    }

    private void SetupCircularMask()
    {
        // Create a mask object if we have a render image
        if (miniMapRenderImage != null)
        {
            // Create a mask object
            maskObject = new GameObject("CircularMask");
            maskObject.transform.SetParent(miniMapRenderImage.transform.parent, false);
            maskObject.transform.SetSiblingIndex(miniMapRenderImage.transform.GetSiblingIndex());
            
            // Add Image component for masking
            circularMaskImage = maskObject.AddComponent<Image>();
            
            // Create a circle sprite dynamically
            circularMaskImage.sprite = CreateCircleSprite(256, 256);
            
            // Add Mask component
            Mask mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // Position and size the mask
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.anchorMin = miniMapRenderImage.rectTransform.anchorMin;
            maskRect.anchorMax = miniMapRenderImage.rectTransform.anchorMax;
            maskRect.pivot = miniMapRenderImage.rectTransform.pivot;
            maskRect.sizeDelta = miniMapRenderImage.rectTransform.sizeDelta;
            maskRect.anchoredPosition = miniMapRenderImage.rectTransform.anchoredPosition;
        }
    }

    private void SetupPlayerArrow()
    {
        if (maskObject == null) return;
        
        // Create player arrow object
        playerArrowObject = new GameObject("PlayerArrow");
        playerArrowObject.transform.SetParent(maskObject.transform, false);
        playerArrowObject.transform.SetAsLastSibling(); // Make sure it's on top
        
        // Add Image component for the arrow
        playerArrowImage = playerArrowObject.AddComponent<Image>();
        
        // Set arrow sprite (use default if none provided)
        if (playerArrowSprite != null)
        {
            playerArrowImage.sprite = playerArrowSprite;
        }
        else
        {
            // Create a simple triangle arrow sprite if none is provided
            playerArrowImage.sprite = CreateDefaultArrowSprite();
        }
        
        playerArrowImage.color = playerArrowColor;
        
        // Set up RectTransform
        playerArrow = playerArrowObject.GetComponent<RectTransform>();
        playerArrow.anchorMin = new Vector2(0.5f, 0.5f);
        playerArrow.anchorMax = new Vector2(0.5f, 0.5f);
        playerArrow.pivot = new Vector2(0.5f, 0.5f);
        playerArrow.sizeDelta = new Vector2(playerArrowSize, playerArrowSize);
        playerArrow.anchoredPosition = Vector2.zero;
    }

    private Sprite CreateDefaultArrowSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        
        // Clear texture
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }
        
        // Create a simple triangle arrow pointing up
        Vector2 center = new Vector2(size / 2, size / 2);
        
        // Arrow points (triangle pointing up)
        Vector2[] arrowPoints = new Vector2[]
        {
            new Vector2(center.x, center.y + size * 0.4f), // Top
            new Vector2(center.x - size * 0.3f, center.y - size * 0.2f), // Bottom left
            new Vector2(center.x + size * 0.3f, center.y - size * 0.2f)  // Bottom right
        };
        
        // Draw the triangle
        FillTriangle(texture, arrowPoints[0], arrowPoints[1], arrowPoints[2], Color.white);
        
        // Draw a small circle in the middle for better visibility
        DrawCircle(texture, center, size * 0.1f, Color.white);
        
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void FillTriangle(Texture2D texture, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        Vector2 min = new Vector2(Mathf.Min(p1.x, p2.x, p3.x), Mathf.Min(p1.y, p2.y, p3.y));
        Vector2 max = new Vector2(Mathf.Max(p1.x, p2.x, p3.x), Mathf.Max(p1.y, p2.y, p3.y));
        
        for (int y = (int)min.y; y <= max.y; y++)
        {
            for (int x = (int)min.x; x <= max.x; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    Vector2 p = new Vector2(x, y);
                    if (IsPointInTriangle(p, p1, p2, p3))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
        float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
        float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
        return s >= 0 && t >= 0 && (s + t) <= 1;
    }

    private void DrawCircle(Texture2D texture, Vector2 center, float radius, Color color)
    {
        for (int y = (int)(center.y - radius); y <= center.y + radius; y++)
        {
            for (int x = (int)(center.x - radius); x <= center.x + radius; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }

    private Sprite CreateCircleSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        
        Vector2 center = new Vector2(width / 2, height / 2);
        float radius = Mathf.Min(width, height) / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color color = (distance <= radius) ? Color.white : Color.clear;
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        return sprite;
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

    private void UpdatePlayerArrow()
    {
        if (playerArrow == null || playerTransform == null) return;

        // Always show player arrow at center of minimap
        playerArrow.anchoredPosition = Vector2.zero;
        
        // Rotate arrow based on player/camera rotation
        if (rotateWithPlayer && mainCamera != null)
        {
            // If minimap rotates with player, arrow should always point "up" (forward)
            playerArrow.rotation = Quaternion.identity;
        }
        else
        {
            // If minimap is fixed north-up, arrow should show player's facing direction
            if (mainCamera != null)
            {
                float cameraRotation = mainCamera.transform.eulerAngles.y;
                playerArrow.rotation = Quaternion.Euler(0f, 0f, -cameraRotation);
            }
        }
        
        // Show/hide arrow based on settings
        playerArrowObject.SetActive(minimapEnabled && alwaysShowPlayerArrow);
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
        if (maskObject != null)
            maskObject.SetActive(minimapEnabled);
        if (playerArrowObject != null)
            playerArrowObject.SetActive(minimapEnabled && alwaysShowPlayerArrow);
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

    // Player arrow control methods
    public void SetPlayerArrowColor(Color color)
    {
        playerArrowColor = color;
        if (playerArrowImage != null)
        {
            playerArrowImage.color = color;
        }
    }

    public void SetPlayerArrowSize(float size)
    {
        playerArrowSize = size;
        if (playerArrow != null)
        {
            playerArrow.sizeDelta = new Vector2(size, size);
        }
    }

    public void SetPlayerArrowSprite(Sprite sprite)
    {
        playerArrowSprite = sprite;
        if (playerArrowImage != null && sprite != null)
        {
            playerArrowImage.sprite = sprite;
        }
    }

    public void SetPlayerArrowVisibility(bool visible)
    {
        alwaysShowPlayerArrow = visible;
        if (playerArrowObject != null)
        {
            playerArrowObject.SetActive(minimapEnabled && visible);
        }
    }

    // Clean up
    private void OnDestroy()
    {
        if (miniMapRenderTexture != null)
        {
            miniMapRenderTexture.Release();
            Destroy(miniMapRenderTexture);
        }
        
        if (circleMaskMaterial != null)
        {
            Destroy(circleMaskMaterial);
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
        Debug.Log($"Circular Mask: {maskObject != null}");
        Debug.Log($"Player Arrow: {playerArrow != null}");
        
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
        if (maskObject != null) Destroy(maskObject);
        if (playerArrowObject != null) Destroy(playerArrowObject);
        if (circleMaskMaterial != null) Destroy(circleMaskMaterial);

        // Reinitialize
        isInitialized = false;
        InitializeMinimap();
    }

    // Helper method to update the circular mask size
    public void UpdateMaskSize(Vector2 newSize)
    {
        if (maskObject != null)
        {
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.sizeDelta = newSize;
            
            // Update border size if it exists
            if (miniMapBorder != null)
            {
                miniMapBorder.sizeDelta = new Vector2(newSize.x + borderThickness * 2, 
                                                    newSize.y + borderThickness * 2);
            }
        }
    }
}