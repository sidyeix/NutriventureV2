using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class BookInteractable : Interactable
{
    public static BookInteractable Instance { get; private set; }
private bool hasRequestedTouch = false;

    public bool IsClaimed { get; private set; } = false;
    
    [Header("First Ingredient Unlock")]
    [SerializeField] private GameObject firstIngredient;

    [Header("Book Settings")]
    public string bookId = "BookOfAllergens";
    public string bookName = "Book of Allergens";
    public Sprite bookIcon;

    [Header("Timeline")]
    public PlayableDirector pickupTimeline;

    [Header("Visual Feedback")]
    public GameObject bookHighlight; // Visual feedback when player is in range
    public GameObject pickupPrompt; // Optional: "Press E to pickup" text/image
    
    [Header("Mobile Settings")]
    [SerializeField] private float maxPickupDistance = 10f; // How far away player can be to pickup
    [SerializeField] private LayerMask raycastLayers = ~0; // Layers to raycast against
    public bool requireLineOfSight = false; // Does player need direct line of sight?
    
    [Header("Screen Detection")]
    [SerializeField] private bool showOnScreenIndicator = true; // Show when book is on screen
    [SerializeField] private GameObject screenIndicatorPrefab; // Prefab to show when book is on screen
    private GameObject screenIndicatorInstance;
    
    [Header("Debug/Testing")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color pickupRangeColor = new Color(0, 1, 0, 0.2f);
    public bool simulateMobileInEditor = true; // Toggle to simulate mobile input in editor

    [Header("Collected Ingredients")]
    public List<IngredientData> collectedIngredients = new List<IngredientData>();

    [System.Serializable]
    public class IngredientData
    {
        public string ingredientId;
        public string ingredientName;
        [TextArea(3, 5)]
        public string ingredientDescription;
        public Sprite ingredientIcon;
    }

    private BookUIManager bookManager;
    private Transform playerTransform;
    private Camera mainCamera;
    private bool isVisibleOnScreen = false;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // Configure collider (not necessarily as trigger anymore)
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            // We can keep it as non-trigger since we're using screen detection
            collider.isTrigger = false;
        }
    }

    void Start()
    {
        bookManager = FindAnyObjectByType<BookUIManager>();
        if (bookManager == null)
        {
            Debug.LogError("BookUIManager not found!");
        }


        mainCamera = Camera.main;
        
        // Find player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (firstIngredient != null)
        {
            firstIngredient.SetActive(false); // 🔒 locked at start
        }
        
        // Initialize visual feedback
        if (bookHighlight != null) bookHighlight.SetActive(false);
        if (pickupPrompt != null) pickupPrompt.SetActive(false);
        
        // Create screen indicator if needed
        if (showOnScreenIndicator && screenIndicatorPrefab != null)
        {
            screenIndicatorInstance = Instantiate(screenIndicatorPrefab, transform.position, Quaternion.identity);
            screenIndicatorInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (IsClaimed || mainCamera == null) return;
        
        // Check if book is visible on screen
        CheckScreenVisibility();
        
        // Update screen indicator
        UpdateScreenIndicator();
        
        // For testing in Unity Editor: simulate mobile with mouse click
        bool isSimulatingMobile = simulateMobileInEditor && Application.isEditor;
        
        // Check for PC input
        if (isVisibleOnScreen && IsPlayerInRange())
        {
            // Check for keyboard input (PC)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Pickup();
                return;
            }
        }

        // Check for mobile touch input
        CheckMobileTouchInput(isSimulatingMobile);

       bool canInteract = isVisibleOnScreen && IsPlayerInRange();

if (canInteract && !hasRequestedTouch)
{
    LookUIRaycastController.Instance?.RequestWorldTouch(this);
    hasRequestedTouch = true;
}
else if (!canInteract && hasRequestedTouch)
{
    LookUIRaycastController.Instance?.ReleaseWorldTouch(this);
    hasRequestedTouch = false;
}


    }

   private void HandleTouchInput(Vector2 screenPosition)
{
    if (IsClaimed || mainCamera == null) return;

    // 1️⃣ DIRECT TAP (raycast)
    Ray ray = mainCamera.ScreenPointToRay(screenPosition);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, 1000f, raycastLayers))
    {
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            if (!IsPlayerInRange())
                return;

            Pickup();
            return; // ⛔ stop here
        }
    }

    // 2️⃣ FALLBACK: tap near book (mobile-friendly)
    Vector3 bookScreenPos = mainCamera.WorldToScreenPoint(transform.position);
    float screenDist = Vector2.Distance(screenPosition, bookScreenPos);

    if (screenDist < 120f && IsPlayerInRange())
    {
        Pickup();
    }
}




    private void UpdateScreenIndicator()
    {
        if (screenIndicatorInstance == null || !showOnScreenIndicator) return;
        
        if (isVisibleOnScreen && IsPlayerInRange() && !IsClaimed)
        {
            // Book is on screen and player can collect it
            screenIndicatorInstance.SetActive(true);
            
            // Position indicator near the book (could be a UI element in screen space)
            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            
            // Convert to world position slightly above the book
            Vector3 worldPos = transform.position + Vector3.up * 2f;
            screenIndicatorInstance.transform.position = worldPos;
            
            // Face the camera
            screenIndicatorInstance.transform.LookAt(mainCamera.transform);
            screenIndicatorInstance.transform.Rotate(0, 180, 0);
        }
        else
        {
            screenIndicatorInstance.SetActive(false);
        }
    }

    private bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > maxPickupDistance) return false;
        
        // Check line of sight if required
        if (requireLineOfSight)
        {
            Vector3 direction = (transform.position - playerTransform.position).normalized;
            Ray ray = new Ray(playerTransform.position, direction);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxPickupDistance, raycastLayers))
            {
                // Check if we hit this book or something else
                if (hit.collider.gameObject != gameObject)
                {
                    return false; // Something is blocking the view
                }
            }
        }
        
        return true;
    }

    private void CheckMobileTouchInput(bool simulateMobile = false)
{
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        return;

    // REAL MOBILE TOUCH
    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
    {
        Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        HandleTouchInput(touchPosition);
    }
}


    private void CheckScreenVisibility()
{
    Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
    isVisibleOnScreen = vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
}


    private System.Collections.IEnumerator FlashHighlight()
    {
        if (bookHighlight != null)
        {
            bookHighlight.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            bookHighlight.SetActive(false);
        }
    }

    // For testing from Unity Editor - Add this to easily test pickup
    [ContextMenu("Test Pickup")]
    public void TestPickup()
    {
        if (!IsClaimed)
        {
            Debug.Log("🧪 Testing Pickup from Context Menu");
            Pickup();
        }
        else
        {
            Debug.Log("Book already claimed!");
        }
    }

    // For testing from Unity Editor - Reset book state
    [ContextMenu("Reset Book State")]
    public void ResetBookState()
    {
        IsClaimed = false;
        
        // Re-enable collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // Reset visual feedback
        if (bookHighlight != null) bookHighlight.SetActive(false);
        if (pickupPrompt != null) pickupPrompt.SetActive(false);
        if (screenIndicatorInstance != null) screenIndicatorInstance.SetActive(false);
        
        Debug.Log("Book state reset - ready for pickup");
    }

    public override void Pickup()
    {
        if (IsClaimed) return; // prevent double pickup
        
        if (firstIngredient != null)
        {
            firstIngredient.SetActive(true); // 🔓 unlocked
            Debug.Log("🥇 First ingredient activated!");
        }

        // Notify game manager
        if (!AllerthriaGameManager.Instance.hasScroll)
        {
            AllerthriaGameManager.Instance.CollectScroll();
        }

        IsClaimed = true;

        if (bookManager != null)
        {
            bookManager.SetMainBook(this);
            bookManager.AddBookToUI(bookId, bookName, bookIcon);
        }

        Debug.Log($"📖 Book collected: {bookName}");

        // ✅ Spawn allergens AFTER scroll is claimed
        AllergenSpawnManager spawner = FindAnyObjectByType<AllergenSpawnManager>();
        if (spawner != null)
        {
            spawner.SpawnNow(); // 🔥 THIS WAS MISSING
            Debug.Log("🌱 Allergens spawned because scroll was claimed!");
        }
        else
        {
            Debug.LogWarning("⚠️ AllergenSpawnManager not found!");
        }

        // 🎬 Play timeline
        if (pickupTimeline != null)
        {
            pickupTimeline.Play();
        }
        
        // Hide visual feedback after pickup
        if (bookHighlight != null)
        {
            bookHighlight.SetActive(false);
        }
        if (pickupPrompt != null)
        {
            pickupPrompt.SetActive(false);
        }
        
        // Disable screen indicator
        if (screenIndicatorInstance != null)
        {
            screenIndicatorInstance.SetActive(false);
        }
        
        // Disable the collider after pickup
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (LookUIRaycastController.Instance != null)
{
   LookUIRaycastController.Instance?.ReleaseWorldTouch(this);
hasRequestedTouch = false;


}

    }

    // Rest of your methods remain the same...
    public void AddIngredient(string ingredientId, string name, string description, Sprite icon)
    {
        if (IsIngredientCollected(ingredientId)) return;

        IngredientData newIngredient = new IngredientData
        {
            ingredientId = ingredientId,
            ingredientName = name,
            ingredientDescription = description,
            ingredientIcon = icon
        };

        collectedIngredients.Add(newIngredient);

        Debug.Log($"➕ Added ingredient: {name} (Total: {collectedIngredients.Count}/9)");

        if (bookManager != null)
        {
            bookManager.OnIngredientCollected(ingredientId);
        }
    }

    public bool IsIngredientCollected(string ingredientId)
    {
        foreach (var ingredient in collectedIngredients)
        {
            if (ingredient.ingredientId == ingredientId)
                return true;
        }
        return false;
    }

    public List<IngredientData> GetCollectedIngredients()
    {
        return collectedIngredients;
    }

    public int GetCollectedCount()
    {
        return collectedIngredients.Count;
    }

    // Visualize the pickup range in the editor
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw pickup range sphere
        Gizmos.color = pickupRangeColor;
        Gizmos.DrawSphere(transform.position, maxPickupDistance);
        
        // Draw line to player if in range
        if (Application.isPlaying && playerTransform != null && IsPlayerInRange())
        {
            Gizmos.color = isVisibleOnScreen ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            
            // Draw camera frustum lines
            if (mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Vector3[] frustumCorners = new Vector3[4];
                mainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
                
                for (int i = 0; i < 4; i++)
                {
                    Vector3 worldCorner = mainCamera.transform.TransformVector(frustumCorners[i]);
                    Gizmos.DrawLine(mainCamera.transform.position, mainCamera.transform.position + worldCorner * maxPickupDistance);
                }
            }
        }
    }

    [ContextMenu("Add Test Ingredient")]
    public void AddTestIngredient()
    {
        string testId = $"allergen_{collectedIngredients.Count + 1}";
        string testName = $"Allergy Source {collectedIngredients.Count + 1}";
        string testDesc = "A substance that causes allergic reactions in Allerthria.";

        AddIngredient(testId, testName, testDesc, null);
    }

    [ContextMenu("Clear All Ingredients")]
    public void ClearAllIngredients()
    {
        collectedIngredients.Clear();
        Debug.Log("🧹 All ingredients cleared");

        if (bookManager != null)
        {
            bookManager.UpdateBookUI();
        }
    }

    [ContextMenu("Fill All 9 Ingredients")]
    public void FillAllNineIngredients()
    {
        ClearAllIngredients();

        for (int i = 0; i < 9; i++)
        {
            AddTestIngredient();
        }
    }

    [ContextMenu("Check Progress")]
    public void CheckProgress()
    {
        int count = GetCollectedCount();
        Debug.Log($"📊 Progress: {count}/9");
    }
}