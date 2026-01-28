using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CollectibleKey : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyId = "castle_key";
    public bool isCollectible = false;
    
    [Header("Debug Settings")]
    public bool debugMode = true;
    
    [Header("Timeline Integration")]
    public bool activatedByTimeline = true;
    
    [Header("Input System Settings")]
    public bool useInputSystem = true;
    public float interactionRange = 5f;
    
    [Header("Visual Settings")]
    public float rotationSpeed = 90f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;
    
    [Header("Collectible Visual Effects")]
    public Material inactiveMaterial;
    public Material collectibleMaterial;
    public GameObject glowEffect;
    public ParticleSystem collectibleParticles;
    public float glowIntensity = 2f;
    public float pulseSpeed = 1.5f;
    
    [Header("Collection Effects")]
    public GameObject collectEffect;
    public AudioClip collectSound;
    public float effectDuration = 2f;
    
    [Header("Audio")]
    public AudioClip becomeCollectibleSound;
    
    [Header("UI/Text")]
    public GameObject collectPrompt;
    public string collectText = "Press E to Collect!";
    
    [Header("Game Summary")]
    public Kingdom4GameEndManager gameEndManager;
    public float summaryDelay = 1f;
    
    [Header("Events")]
    public UnityEvent onKeyCollected;
    public UnityEvent onKeyBecameCollectible;

    private PlayerInput playerInput;
    private InputAction interactAction;
    
    private List<Renderer> allKeyRenderers = new List<Renderer>();
    private List<Material[]> originalMaterials = new List<Material[]>();
    
    private Vector3 startPosition;
    private bool isCollected = false;
    private AudioSource audioSource;
    private TextMesh promptText;
    private bool isInitialized = false;
    
    private GameObject player;
    private bool playerInRange = false;
    
    void Awake()
    {
        if (activatedByTimeline)
        {
            return;
        }
    }
    
    void OnEnable()
    {
        if (!isInitialized)
        {
            InitializeKey();
        }
    }
    
    void OnDisable()
    {
        // Clean up Input System subscription
        if (interactAction != null)
        {
            interactAction.performed -= OnCollectActionPerformed;
        }
    }
    
    void InitializeKey()
    {
        startPosition = transform.position;
        
        // Find ALL renderers in children
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        allKeyRenderers.AddRange(childRenderers);
        
        if (allKeyRenderers.Count == 0)
        {
            Debug.LogError("No Renderers found in Key GameObject or its children!");
            enabled = false;
            return;
        }
        
        if (debugMode) Debug.Log($"Found {allKeyRenderers.Count} renderer(s) in key");
        
        // Store original materials
        foreach (Renderer renderer in allKeyRenderers)
        {
            originalMaterials.Add(renderer.materials);
        }
        
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Initialize prompt
        if (collectPrompt != null)
        {
            promptText = collectPrompt.GetComponentInChildren<TextMesh>();
            if (promptText != null)
                promptText.text = "";
            collectPrompt.SetActive(false);
        }
        
        // Find game end manager
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
        }
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Setup Input System
        SetupInputSystem();
        
        // Load saved state
        if (PlayerPrefs.HasKey($"KeyCollected_{keyId}"))
        {
            isCollected = true;
            gameObject.SetActive(false);
            if (debugMode) Debug.Log("Key was already collected.");
            return;
        }
        
        // Ensure collider exists
        SetupCollider();
        
        // Start as NOT collectible
        isCollectible = false;
        SetInactiveVisuals();
        
        isInitialized = true;
        
        if (debugMode) 
        {
            Debug.Log("=== PARENT KEY INITIALIZED ===");
            Debug.Log($"Interaction Range: {interactionRange}");
            Debug.Log($"Press E or click when close to collect!");
        }
    }
    
    void SetupInputSystem()
    {
        if (!useInputSystem || player == null) return;
        
        // Get PlayerInput component
        playerInput = player.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("PlayerInput component not found on Player!");
            return;
        }
        
        // Try to find the interact action - check common action names
        string[] possibleActionNames = { "Interact", "Pickup", "Use", "Action", "Fire", "Attack" };
        
        foreach (string actionName in possibleActionNames)
        {
            try
            {
                interactAction = playerInput.actions.FindAction(actionName);
                if (interactAction != null)
                {
                    if (debugMode) Debug.Log($"Found action: {actionName}");
                    break;
                }
            }
            catch
            {
                // Action not found, continue searching
                continue;
            }
        }
        
        // If no action found, try direct keyboard input
        if (interactAction == null)
        {
            if (debugMode) Debug.Log("No interact action found. Using direct keyboard input.");
            return;
        }
        
        // Subscribe to action
        interactAction.performed += OnCollectActionPerformed;
        interactAction.Enable();
        
        if (debugMode) Debug.Log($"Subscribed to Input System action: {interactAction.name}");
    }
    
    void SetupCollider()
    {
        Collider parentCollider = GetComponent<Collider>();
        if (parentCollider == null)
        {
            parentCollider = gameObject.AddComponent<BoxCollider>();
            
            Bounds totalBounds = CalculateTotalBounds();
            BoxCollider boxCollider = (BoxCollider)parentCollider;
            boxCollider.center = totalBounds.center - transform.position;
            boxCollider.size = totalBounds.size;
            
            if (debugMode) Debug.Log($"Created parent collider with size: {totalBounds.size}");
        }
        
        parentCollider.isTrigger = true;
    }
    
    void OnCollectActionPerformed(InputAction.CallbackContext context)
    {
        if (!playerInRange)
        {
            if (debugMode) Debug.Log("Pressed Interact but NOT in range");
            return;
        }

        if (!isCollectible || isCollected)
            return;

        if (debugMode) Debug.Log("Interact pressed — collecting key!");
        CollectKey();
    }
    
    Bounds CalculateTotalBounds()
    {
        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
        }
        
        if (renderers.Length == 0)
        {
            totalBounds = new Bounds(transform.position, new Vector3(1, 1, 1));
        }
        
        return totalBounds;
    }
    
    void Start()
    {
        if (!activatedByTimeline && !isInitialized)
        {
            InitializeKey();
        }
    }
    
    void Update()
    {
        if (!isCollected && isCollectible)
        {
            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Rotation animation
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
            
            // Update visuals
            UpdateCollectibleVisuals();
            
            // Check player distance
            CheckPlayerDistance();
            
            // Show prompt
            ShowPromptWhenNear();
            
            // Check for E key directly (fallback if Input System fails)
            CheckDirectKeyboardInput();
        }
    }
    
    void CheckDirectKeyboardInput()
    {
        // Direct keyboard check as fallback
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && playerInRange)
        {
            if (isCollectible && !isCollected)
            {
                if (debugMode) Debug.Log("E key pressed directly!");
                CollectKey();
            }
        }
    }
    
    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRange;

        if (debugMode && playerInRange != wasInRange)
        {
            Debug.Log(playerInRange
                ? $"Player entered interaction range ({distance:F1}m)"
                : $"Player left interaction range ({distance:F1}m)");
        }
    }
    
    void SetInactiveVisuals()
    {
        foreach (Renderer renderer in allKeyRenderers)
        {
            if (renderer != null)
            {
                if (inactiveMaterial != null)
                {
                    Material[] mats = new Material[renderer.materials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = inactiveMaterial;
                    }
                    renderer.materials = mats;
                }
                else
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.color = Color.gray;
                    }
                }
            }
        }
        
        if (glowEffect != null) glowEffect.SetActive(false);
        if (collectibleParticles != null && collectibleParticles.isPlaying) 
            collectibleParticles.Stop();
        if (collectPrompt != null) collectPrompt.SetActive(false);
    }
    
    void UpdateCollectibleVisuals()
    {
        if (collectibleMaterial != null)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 0.3f) + 0.7f;
            Color pulseColor = Color.yellow * pulse * glowIntensity;
            
            foreach (Renderer renderer in allKeyRenderers)
            {
                if (renderer != null && renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.SetColor("_EmissionColor", pulseColor);
                }
            }
        }
    }
    
    void ShowPromptWhenNear()
    {
        if (collectPrompt == null || Camera.main == null) return;
        
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        
        if (screenPos.z > 0 && playerInRange)
        {
            collectPrompt.SetActive(true);
            
            if (promptText != null)
            {
                promptText.text = collectText;
            }
        }
        else
        {
            collectPrompt.SetActive(false);
        }
    }
    
    void OnMouseDown()
    {
        if (isCollectible && !isCollected)
        {
            if (debugMode) Debug.Log("Clicked on parent key GameObject!");
            CollectKey();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (debugMode) Debug.Log("Player entered key interaction trigger");
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (collectPrompt != null) collectPrompt.SetActive(false);
            if (debugMode) Debug.Log("Player left key interaction trigger");
        }
    }
    
    public void CollectKey()
    {
        if (isCollected || !isCollectible) return;
        
        if (!playerInRange && useInputSystem)
        {
            if (debugMode) Debug.Log("Can't collect - player not in range!");
            return;
        }
        
        isCollected = true;
        
        // Notify Game Manager
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.ReceiveKey();
        }
        
        // Play effects
        PlayCollectionEffects();
        
        // Invoke events
        onKeyCollected?.Invoke();
        
        // Save state
        SaveKeyState();
        
        // Show game summary
        ShowGameSummary();
        
        // Hide the key
        StartCoroutine(HideKeyWithDelay());
        
        if (debugMode) Debug.Log($"Parent Key '{keyId}' collected!");
    }
    
    void PlayCollectionEffects()
    {
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        if (collectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
        
        if (collectPrompt != null)
            collectPrompt.SetActive(false);
    }
    
    System.Collections.IEnumerator HideKeyWithDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        foreach (Renderer renderer in allKeyRenderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
        
        Collider parentCollider = GetComponent<Collider>();
        if (parentCollider != null)
            parentCollider.enabled = false;
        
        if (glowEffect != null) glowEffect.SetActive(false);
        if (collectibleParticles != null) collectibleParticles.Stop();
        
        yield return new WaitForSeconds(effectDuration - 0.5f);
        
        Destroy(gameObject);
    }
    
    void ShowGameSummary()
    {
        if (gameEndManager != null)
        {
            StartCoroutine(ShowSummaryWithDelay());
        }
    }
    
    System.Collections.IEnumerator ShowSummaryWithDelay()
    {
        yield return new WaitForSeconds(summaryDelay);
        
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4Complete();
        }
    }
    
    void SaveKeyState()
    {
        PlayerPrefs.SetInt($"KeyCollected_{keyId}", 1);
        PlayerPrefs.Save();
    }
    
    public void MakeCollectible()
    {
        if (isCollected) return;
        
        isCollectible = true;
        
        if (debugMode) Debug.Log("★ PARENT KEY IS NOW COLLECTIBLE! ★");
        
        StartCoroutine(BecomeCollectibleSequence());
    }
    
    System.Collections.IEnumerator BecomeCollectibleSequence()
    {
        if (becomeCollectibleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(becomeCollectibleSound);
        }
        
        onKeyBecameCollectible?.Invoke();
        
        yield return StartCoroutine(PulseEffect());
        
        SetCollectibleVisuals();
    }
    
    void SetCollectibleVisuals()
    {
        foreach (Renderer renderer in allKeyRenderers)
        {
            if (renderer != null)
            {
                if (collectibleMaterial != null)
                {
                    Material[] mats = new Material[renderer.materials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = collectibleMaterial;
                    }
                    renderer.materials = mats;
                }
                else
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.color = Color.yellow;
                    }
                }
            }
        }
        
        if (glowEffect != null) glowEffect.SetActive(true);
        if (collectibleParticles != null) collectibleParticles.Play();
        
        StartCoroutine(ContinuousPulse());
    }
    
    System.Collections.IEnumerator PulseEffect()
    {
        if (allKeyRenderers.Count == 0) yield break;
        
        List<Color[]> originalColors = new List<Color[]>();
        foreach (Renderer renderer in allKeyRenderers)
        {
            if (renderer != null)
            {
                Color[] colors = new Color[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    colors[i] = renderer.materials[i].color;
                }
                originalColors.Add(colors);
            }
        }
        
        for (int pulseCount = 0; pulseCount < 5; pulseCount++)
        {
            foreach (Renderer renderer in allKeyRenderers)
            {
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.color = Color.yellow;
                    }
                }
            }
            if (glowEffect != null) glowEffect.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            
            for (int i = 0; i < allKeyRenderers.Count; i++)
            {
                if (allKeyRenderers[i] != null && i < originalColors.Count)
                {
                    for (int j = 0; j < allKeyRenderers[i].materials.Length; j++)
                    {
                        if (j < originalColors[i].Length)
                        {
                            allKeyRenderers[i].materials[j].color = originalColors[i][j];
                        }
                    }
                }
            }
            if (glowEffect != null) glowEffect.SetActive(false);
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    System.Collections.IEnumerator ContinuousPulse()
    {
        while (isCollectible && !isCollected)
        {
            if (glowEffect != null)
            {
                float intensity = Mathf.PingPong(Time.time * 2f, 0.5f) + 0.5f;
                Renderer glowRenderer = glowEffect.GetComponent<Renderer>();
                if (glowRenderer != null && glowRenderer.material.HasProperty("_Color"))
                {
                    Color glowColor = glowRenderer.material.color;
                    glowColor.a = intensity;
                    glowRenderer.material.color = glowColor;
                }
            }
            yield return null;
        }
    }
    
    [ContextMenu("Test Make Collectible")]
    public void TestMakeCollectible()
    {
        MakeCollectible();
    }
    
    [ContextMenu("Test Collect Key")]
    public void TestCollectKey()
    {
        if (!isCollectible)
        {
            Debug.Log("Making key collectible first...");
            MakeCollectible();
            StartCoroutine(CollectAfterDelay());
        }
        else
        {
            playerInRange = true;
            CollectKey();
        }
    }
    
    System.Collections.IEnumerator CollectAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        playerInRange = true;
        CollectKey();
    }
    
    [ContextMenu("Reset Key")]
    public void ResetKey()
    {
        isCollected = false;
        isCollectible = false;
        playerInRange = false;
        
        if (gameObject != null)
        {
            for (int i = 0; i < allKeyRenderers.Count; i++)
            {
                if (allKeyRenderers[i] != null && i < originalMaterials.Count)
                {
                    allKeyRenderers[i].enabled = true;
                    allKeyRenderers[i].materials = originalMaterials[i];
                }
            }
            
            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = true;
            
            if (glowEffect != null) glowEffect.SetActive(false);
            if (collectibleParticles != null) 
                collectibleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            if (collectPrompt != null) collectPrompt.SetActive(false);
        }
        
        PlayerPrefs.DeleteKey($"KeyCollected_{keyId}");
        
        Debug.Log("Parent Key reset to inactive state");
    }
    
    void OnGUI()
    {
        if (debugMode)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 150));
            GUILayout.Label($"Parent Key: {(isCollected ? "COLLECTED" : (isCollectible ? "COLLECTIBLE" : "INACTIVE"))}", style);
            GUILayout.Label($"Player in Range: {playerInRange}", style);
            GUILayout.Label($"Distance: {(player != null ? Vector3.Distance(transform.position, player.transform.position).ToString("F1") : "N/A")}m", style);
            
            if (isCollectible && !isCollected)
            {
                if (playerInRange)
                {
                    GUILayout.Label($"✓ Press E or click to collect!", style);
                }
                else
                {
                    GUILayout.Label($"✗ Get closer to collect ({interactionRange}m range)", style);
                }
            }
            GUILayout.EndArea();
        }
    }
}