using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

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
    public PlayableDirector pickupTimeline;   // 👈 ADD THIS

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
}


public override void Pickup()
{
    if (firstIngredient != null)
{
    firstIngredient.SetActive(true); // 🔓 unlocked
    Debug.Log("🥇 First ingredient activated!");
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
