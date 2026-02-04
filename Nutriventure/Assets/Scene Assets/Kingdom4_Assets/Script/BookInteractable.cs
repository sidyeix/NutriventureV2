using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class BookInteractable : Interactable
{
    public static BookInteractable Instance { get; private set; }

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
    private bool isPlayerInRange = false;

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
        
        // Configure collider as trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    void Start()
    {
        bookManager = FindAnyObjectByType<BookUIManager>();
        if (bookManager == null)
        {
            Debug.LogError("BookUIManager not found!");
        }

        if (firstIngredient != null)
        {
            firstIngredient.SetActive(false); // 🔒 locked at start
        }
        
        // Initialize visual feedback
        if (bookHighlight != null) bookHighlight.SetActive(false);
        if (pickupPrompt != null) pickupPrompt.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered trigger
        if (other.CompareTag("Player") && !IsClaimed)
        {
            isPlayerInRange = true;
            
            // Show visual feedback
            if (bookHighlight != null) bookHighlight.SetActive(true);
            if (pickupPrompt != null) pickupPrompt.SetActive(true);
            
            Debug.Log("Player is near the book");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if player exited trigger
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            
            // Hide visual feedback
            if (bookHighlight != null) bookHighlight.SetActive(false);
            if (pickupPrompt != null) pickupPrompt.SetActive(false);
            
            Debug.Log("Player moved away from the book");
        }
    }

    void Update()
    {
        // Check for input when player is in range and book isn't claimed
        if (isPlayerInRange && !IsClaimed)
        {
            // Check for keyboard input (PC)
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Pickup();
            }
            
            // Check for touch input (Mobile) - might want a UI button instead
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Pickup();
            }
        }
    }

    // Alternative: Simple automatic pickup when player enters trigger
    public void AutoPickupOnEnter()
    {
        if (isPlayerInRange && !IsClaimed)
        {
            Pickup();
        }
    }

    // Optional: Call this from a UI button for mobile
    public void PickupFromUIButton()
    {
        if (isPlayerInRange && !IsClaimed)
        {
            Pickup();
        }
        else if (!isPlayerInRange)
        {
            Debug.Log("Move closer to pick up the book!");
            // Optional: Show floating text or sound
        }
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
        
        // Disable the trigger after pickup
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // Optional: Hide the book model after pickup
        // GetComponent<Renderer>().enabled = false;
    }

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

    // Visualize the trigger area in the editor
    private void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = Color.blue;
            
            if (collider is BoxCollider boxCollider)
            {
                Gizmos.DrawWireCube(
                    transform.position + boxCollider.center,
                    boxCollider.size
                );
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Gizmos.DrawWireSphere(
                    transform.position + sphereCollider.center,
                    sphereCollider.radius
                );
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                // Draw capsule outline
                Vector3 top = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
                Vector3 bottom = transform.position + capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius);
                
                Gizmos.DrawWireSphere(top, capsuleCollider.radius);
                Gizmos.DrawWireSphere(bottom, capsuleCollider.radius);
                
                // Draw connecting lines
                Gizmos.DrawLine(top + Vector3.right * capsuleCollider.radius, bottom + Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.right * capsuleCollider.radius, bottom - Vector3.right * capsuleCollider.radius);
                Gizmos.DrawLine(top + Vector3.forward * capsuleCollider.radius, bottom + Vector3.forward * capsuleCollider.radius);
                Gizmos.DrawLine(top - Vector3.forward * capsuleCollider.radius, bottom - Vector3.forward * capsuleCollider.radius);
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