using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))] // Add this line
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

    [Header("Tap Settings")]
    public float maxTapDistance = 3f; // How close player needs to be
    public GameObject bookHighlight; // Visual feedback
    
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
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Ensure we have a collider for tapping
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    void Update()
    {
        // Check if player is close enough to tap
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = distance <= maxTapDistance;
            
            // Show/hide visual feedback
            if (bookHighlight != null)
            {
                bookHighlight.SetActive(isPlayerInRange && !IsClaimed);
            }
        }
    }

    // This makes the book tappable (works with mobile touch!)
    private void OnMouseDown()
    {
        if (IsClaimed) return; // Already collected
        
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= maxTapDistance)
            {
                Pickup();
            }
            else
            {
                Debug.Log("Move closer to pick up the book!");
                // Optional: Show floating text or sound
            }
        }
        else
        {
            Pickup(); // Fallback if no player
        }
    }

    public override void Pickup()
    {
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
        
        if (IsClaimed) return; // prevent double pickup

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
        
        // Optional: Hide the book model after pickup
        // GetComponent<Renderer>().enabled = false;
        // GetComponent<Collider>().enabled = false;
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

    // Visualize tap range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxTapDistance);
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