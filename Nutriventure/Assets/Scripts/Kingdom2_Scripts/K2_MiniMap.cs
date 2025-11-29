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
    
    [Header("Monster Icon Settings")]
    public Sprite monsterIconSprite; // Assign your monster icon here
    public float monsterIconSize = 15f;
    public Color monsterIconColor = Color.red;
    public bool showMonsterIcons = true;
    
    [Header("Layer Settings")]
    public LayerMask minimapLayers = -1; // Layers visible in minimap
    public string particleSystemTag = "ParticleSystem"; // Tag to identify particle systems to hide
    public int excludedLayer = 31; // Use an existing layer (31 is usually available)
    
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
    
    // Monster tracking
    private List<Transform> monsters = new List<Transform>();
    private List<RectTransform> monsterIcons = new List<RectTransform>();
    private GameObject monsterIconsContainer;

    void Start()
    {
        InitializeMinimap();
        SetupInputSystem();
        SetupMonsterIcons();
        SetupLayerFiltering();
    }

    void Update()
    {
        if (!isInitialized) return;
        
        UpdateMinimapCamera();
        UpdateMonsterIcons();
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
                Debug.LogError("Player not found! Please assign playerTransform in inspector.");
                return;
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
                    Debug.LogWarning("Could not find actual camera from Cinemachine virtual camera. Minimap will use fixed orientation.");
                }
                else
                {
                    Debug.Log("Using main camera as fallback: " + mainCamera.name);
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
        miniMapCamera.cullingMask = minimapLayers; // Apply layer filtering

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

    private void SetupMonsterIcons()
    {
        // Create container for monster icons
        if (miniMapBorder != null)
        {
            monsterIconsContainer = new GameObject("MonsterIcons");
            monsterIconsContainer.transform.SetParent(miniMapBorder);
            RectTransform containerRT = monsterIconsContainer.AddComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;
        }

        // Find all monsters in the scene
        FindAllMonsters();
    }

    private void FindAllMonsters()
    {
        // Clear existing lists
        monsters.Clear();
        foreach (var icon in monsterIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        monsterIcons.Clear();

        // Find all game objects with monster tags
        GameObject[] naturalMonsters = GameObject.FindGameObjectsWithTag("K2_Monster");        
        // Add all found monsters to the list
        foreach (GameObject monster in naturalMonsters)
        {
            if (monster != null && !monsters.Contains(monster.transform))
            {
                monsters.Add(monster.transform);
                CreateMonsterIcon(monster.transform);
            }
        }

        Debug.Log($"Found {monsters.Count} monsters in scene");
    }

    private void CreateMonsterIcon(Transform monsterTransform)
    {
        if (monsterIconsContainer == null || monsterIconSprite == null) return;

        // Create UI Image for monster icon
        GameObject iconGO = new GameObject($"MonsterIcon_{monsterTransform.name}");
        iconGO.transform.SetParent(monsterIconsContainer.transform);
        
        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.sprite = monsterIconSprite;
        iconImage.color = monsterIconColor;
        
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(monsterIconSize, monsterIconSize);
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        
        monsterIcons.Add(iconRT);
    }

    private void SetupLayerFiltering()
    {
        // Check if the excluded layer is valid
        if (excludedLayer < 0 || excludedLayer > 31)
        {
            Debug.LogWarning("Excluded layer is out of range. Using layer 31 instead.");
            excludedLayer = 31;
        }

        // Hide particle systems from minimap by disabling them for the minimap camera
        GameObject[] particleSystems = GameObject.FindGameObjectsWithTag(particleSystemTag);
        
        foreach (GameObject ps in particleSystems)
        {
            if (ps != null)
            {
                // Method 1: Move to excluded layer (if layer exists and is valid)
                try
                {
                    SetLayerRecursively(ps, excludedLayer);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not set layer for {ps.name}: {e.Message}");
                    // Fallback: Disable particle systems temporarily
                    ParticleSystem[] particles = ps.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem particle in particles)
                    {
                        particle.gameObject.SetActive(false);
                    }
                }
            }
        }

        Debug.Log($"Processed {particleSystems.Length} particle systems for minimap filtering");

        // Update minimap camera layer mask to exclude the particle layer
        if (miniMapCamera != null)
        {
            minimapLayers &= ~(1 << excludedLayer); // Remove excluded layer from visible layers
            miniMapCamera.cullingMask = minimapLayers;
            Debug.Log($"Minimap camera culling mask set to: {minimapLayers.value}");
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        // Validate layer range
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

    private void UpdateMonsterIcons()
    {
        if (!showMonsterIcons || monsterIcons.Count != monsters.Count) return;

        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] == null || monsterIcons[i] == null) continue;

            // Convert world position to minimap UI position using camera space
            Vector3 monsterWorldPos = monsters[i].position;
            
            // Convert world position to viewport position (0-1 range)
            Vector3 viewportPos = miniMapCamera.WorldToViewportPoint(monsterWorldPos);
            
            // Convert viewport position to UI coordinates
            RectTransform borderRT = miniMapBorder;
            Vector2 uiPos = new Vector2(
                (viewportPos.x - 0.5f) * borderRT.rect.width,
                (viewportPos.y - 0.5f) * borderRT.rect.height
            );
            
            // Only show icons that are within the minimap bounds
            bool isInBounds = viewportPos.x >= 0f && viewportPos.x <= 1f && 
                             viewportPos.y >= 0f && viewportPos.y <= 1f && 
                             viewportPos.z > 0f; // In front of camera
            
            monsterIcons[i].gameObject.SetActive(isInBounds);
            
            if (isInBounds)
            {
                // Update icon position
                monsterIcons[i].anchoredPosition = uiPos;
                
                // Update icon rotation based on monster's facing direction
                if (rotateWithCamera && mainCamera != null)
                {
                    float monsterRotation = monsters[i].eulerAngles.y;
                    float cameraRotation = mainCamera.transform.eulerAngles.y;
                    float relativeRotation = monsterRotation - cameraRotation;
                    monsterIcons[i].rotation = Quaternion.Euler(0f, 0f, -relativeRotation);
                }
                else
                {
                    monsterIcons[i].rotation = Quaternion.Euler(0f, 0f, -monsters[i].eulerAngles.y);
                }
            }
        }
    }

    // Public methods to manage monsters dynamically
    public void AddMonster(Transform monsterTransform)
    {
        if (monsterTransform != null && !monsters.Contains(monsterTransform))
        {
            monsters.Add(monsterTransform);
            CreateMonsterIcon(monsterTransform);
            Debug.Log($"Added monster to minimap: {monsterTransform.name}");
        }
    }

    public void RemoveMonster(Transform monsterTransform)
    {
        int index = monsters.IndexOf(monsterTransform);
        if (index >= 0)
        {
            monsters.RemoveAt(index);
            if (index < monsterIcons.Count && monsterIcons[index] != null)
            {
                Destroy(monsterIcons[index].gameObject);
                monsterIcons.RemoveAt(index);
            }
            Debug.Log($"Removed monster from minimap: {monsterTransform.name}");
        }
    }

    public void RefreshMonsters()
    {
        FindAllMonsters();
        Debug.Log("Refreshed monster list for minimap");
    }

    public void SetMonsterIconVisibility(bool visible)
    {
        showMonsterIcons = visible;
        if (monsterIconsContainer != null)
        {
            monsterIconsContainer.SetActive(visible && minimapEnabled);
        }
        Debug.Log("Monster icons visibility: " + visible);
    }

    public void SetMonsterIconColor(Color color)
    {
        monsterIconColor = color;
        foreach (var icon in monsterIcons)
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
        Debug.Log("Monster icon color updated");
    }

    // Alternative method to exclude objects without changing layers
    public void ExcludeObjectFromMinimap(GameObject obj)
    {
        if (obj != null)
        {
            // Simply disable the object for the minimap camera
            obj.SetActive(false);
        }
    }

    // Method to restore excluded objects
    public void IncludeObjectInMinimap(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    // Existing methods remain the same with minor updates for monster icons
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
        if (monsterIconsContainer != null) monsterIconsContainer.SetActive(minimapEnabled && showMonsterIcons);
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
        Debug.Log($"Monsters tracked: {monsters.Count}");
        Debug.Log($"Monster icons created: {monsterIcons.Count}");
        Debug.Log($"Show Monster Icons: {showMonsterIcons}");
        Debug.Log($"Minimap Layers: {minimapLayers.value}");
        Debug.Log($"Excluded Layer: {excludedLayer}");
        
        // Debug monster positions
        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null)
            {
                Vector3 viewportPos = miniMapCamera.WorldToViewportPoint(monsters[i].position);
                Debug.Log($"Monster {i}: World={monsters[i].position}, Viewport={viewportPos}");
            }
        }
    }

    [ContextMenu("Refresh Monsters")]
    public void DebugRefreshMonsters()
    {
        RefreshMonsters();
    }

    [ContextMenu("Fix Layer Settings")]
    public void FixLayerSettings()
    {
        // Reset to safe defaults
        excludedLayer = 31;
        minimapLayers = ~(1 << excludedLayer); // All layers except the excluded one
        
        if (miniMapCamera != null)
        {
            miniMapCamera.cullingMask = minimapLayers;
        }
        
        Debug.Log("Layer settings reset to safe defaults");
    }
}