using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Add this line

public class PlayerHealthManager : MonoBehaviour
{
    public static PlayerHealthManager Instance;

    [Header("Health")]
    public int maxHearts = 5;
    public float currentHealth;

    [Header("Damage")]
    public float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("UI References")]
    public Transform heartsContainer; // Drag your existing container here
    public GameObject heartPrefab; // Drag your heart prefab here

    [Header("Heart Sprites")]
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite; // Optional
    public Sprite emptyHeartSprite;

    [Header("Game End Reference")]
    public Kingdom4GameEndManager gameEndManager; // Assign in Inspector

    private List<Image> hearts = new List<Image>();
    private Keyboard keyboard; // Cache keyboard reference

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentHealth = maxHearts;
        InitializeHealthUI();
        
        // Find GameEndManager if not assigned
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
            if (gameEndManager == null)
            {
                Debug.LogWarning("Kingdom4GameEndManager not found! Game over screen may not show properly.");
            }
        }
    }

    void Start()
    {
        // Get keyboard reference
        keyboard = Keyboard.current;
    }

    void InitializeHealthUI()
    {
        // Clear any existing hearts in the container (optional safety measure)
        ClearExistingHearts();
        
        // Create hearts from prefab
        CreateHearts();
        UpdateHearts();
    }

    void ClearExistingHearts()
    {
        if (heartsContainer == null)
        {
            Debug.LogError("Hearts container is not assigned!");
            return;
        }

        // Remove any existing heart GameObjects from previous runs
        foreach (Transform child in heartsContainer)
        {
            if (child.name.Contains("Heart"))
                Destroy(child.gameObject);
        }
    }

    void CreateHearts()
    {
        hearts.Clear();

        if (heartsContainer == null)
        {
            Debug.LogError("Hearts container is not assigned! Please drag your UI container to the inspector.");
            return;
        }

        if (heartPrefab == null)
        {
            Debug.LogError("Heart prefab is not assigned! Please drag your heart prefab to the inspector.");
            return;
        }

        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartGO = Instantiate(heartPrefab, heartsContainer);
            heartGO.name = "Heart_" + i;
            
            // Optional: Reset local position/scale if prefab has unusual values
            heartGO.transform.localPosition = Vector3.zero;
            heartGO.transform.localScale = Vector3.one;
            
            Image img = heartGO.GetComponent<Image>();
            if (img == null)
            {
                img = heartGO.AddComponent<Image>();
                Debug.LogWarning("Heart prefab didn't have Image component, added one.");
            }

            // Set initial sprite
            if (fullHeartSprite != null)
                img.sprite = fullHeartSprite;
            else
                Debug.LogWarning("Full heart sprite is not assigned!");

            hearts.Add(img);
        }
    }

    #region HEALTH LOGIC
    public void TakeDamage(float amount)
    {
        if (Time.time < lastDamageTime + damageCooldown) return;

        lastDamageTime = Time.time;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHearts();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHearts, currentHealth + amount);
        UpdateHearts();
    }

    void UpdateHearts()
    {
        float hp = currentHealth;

        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null) continue;
            
            if (hp >= 1)
            {
                hearts[i].sprite = fullHeartSprite;
                hp -= 1;
            }
            else if (hp >= 0.5f)
            {
                // Show half heart if sprite exists, otherwise show full or empty
                if (halfHeartSprite != null)
                    hearts[i].sprite = halfHeartSprite;
                else
                    hearts[i].sprite = fullHeartSprite; // or emptyHeartSprite based on your preference
                hp -= 0.5f;
            }
            else
            {
                hearts[i].sprite = emptyHeartSprite;
            }
        }
    }

    public void SetMaxHearts(int newMax)
    {
        maxHearts = newMax;
        currentHealth = Mathf.Min(currentHealth, maxHearts);
        
        // Clear old hearts
        foreach (Image heart in hearts)
        {
            if (heart != null && heart.gameObject != null)
                Destroy(heart.gameObject);
        }
        hearts.Clear();
        
        // Create new hearts
        CreateHearts();
        UpdateHearts();
    }

    public void SetHealth(float newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHearts);
        UpdateHearts();
    }

    void Die()
    {
        Debug.Log("Player Died - Showing Game Summary");
        
        // Call Kingdom4GameEndManager to show game over screen
        if (gameEndManager != null)
        {
            gameEndManager.HandleKingdom4GameOver();
        }
        else
        {
            Debug.LogError("Kingdom4GameEndManager is not assigned or found!");
            // Fallback - just show a debug message
            Debug.Log("GAME OVER - No lives remaining");
        }
    }
    #endregion

    #region FOR TESTING (Remove in production)
    void Update()
    {
        // Check if keyboard is available
        if (keyboard == null)
        {
            keyboard = Keyboard.current;
            return;
        }
        
        if (keyboard.digit1Key.wasPressedThisFrame)
            TakeDamage(0.5f);
        if (keyboard.digit2Key.wasPressedThisFrame)
            TakeDamage(1f);
        if (keyboard.digit3Key.wasPressedThisFrame)
            Heal(1f);
        if (keyboard.digit4Key.wasPressedThisFrame)
            SetMaxHearts(maxHearts + 1);
        if (keyboard.digit5Key.wasPressedThisFrame)
            SetMaxHearts(Mathf.Max(1, maxHearts - 1));
        if (keyboard.rKey.wasPressedThisFrame)
            SetHealth(maxHearts); // Reset health
        if (keyboard.gKey.wasPressedThisFrame)
            Die(); // Force game over for testing
    }
    #endregion
    
    #region PUBLIC METHODS FOR RESET
    public void ResetHealth()
    {
        currentHealth = maxHearts;
        UpdateHearts();
    }
    
    public void FullHeal()
    {
        currentHealth = maxHearts;
        UpdateHearts();
    }
    #endregion
}