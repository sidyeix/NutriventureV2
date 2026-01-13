using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PreserviaPlayerStat : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;
    
    [Header("UI References")]
    public Image heartIconPrefab; // Assign your heart image prefab here
    public Transform heartsContainer; // Assign a parent object for hearts
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;
    
    [Header("Damage Settings")]
    public float damageCooldown = 1f; // Prevent rapid damage spam
    
    [Header("Damage Panel")]
    [Tooltip("The red overlay image that appears when taking damage")]
    public Image damagePanel;
    [Tooltip("How long the damage panel stays visible")]
    public float damagePanelDuration = 0.3f;
    [Tooltip("Color of the damage panel")]
    public Color damageColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
    
    private Image[] heartIcons;
    private float lastDamageTime;
    private Color originalPanelColor;
    private Coroutine damagePanelCoroutine;
    
    void Start()
    {
        InitializeHearts();
        currentHealth = maxHealth;
        UpdateHealthUI();
        InitializeDamagePanel();
        
        Debug.Log($"Player health system initialized with {maxHealth} hearts");
    }
    
    private void InitializeHearts()
    {
        // Clear existing hearts if any
        if (heartsContainer != null && heartsContainer.childCount > 0)
        {
            foreach (Transform child in heartsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        // Create heart icons
        heartIcons = new Image[maxHealth];
        
        for (int i = 0; i < maxHealth; i++)
        {
            if (heartIconPrefab != null && heartsContainer != null)
            {
                Image heart = Instantiate(heartIconPrefab, heartsContainer);
                heartIcons[i] = heart;
                heart.gameObject.name = $"Heart_{i + 1}";
                
                // FORCE ACTIVATE THE HEART OBJECT AND COMPONENTS
                heart.gameObject.SetActive(true); // Ensure GameObject is active
                heart.enabled = true; // Ensure Image component is enabled
                heart.raycastTarget = false; // Make sure it doesn't block UI interactions
                
                // Force layout rebuild
                LayoutRebuilder.ForceRebuildLayoutImmediate(heartsContainer as RectTransform);
            }
        }
        
        // Log warnings if references are missing
        if (heartIconPrefab == null)
        {
            Debug.LogError("Heart Icon Prefab not assigned in Inspector!");
        }
        if (heartsContainer == null)
        {
            Debug.LogError("Hearts Container not assigned in Inspector!");
        }
        else
        {
            Debug.Log($"Hearts container found with {heartsContainer.childCount} children after initialization");
        }
    }
    
    private void InitializeDamagePanel()
    {
        if (damagePanel != null)
        {
            // Store original color (should be transparent or white with 0 alpha)
            originalPanelColor = damagePanel.color;
            
            // Make sure damage panel is initially disabled
            damagePanel.gameObject.SetActive(false);
            
            Debug.Log("Damage panel initialized and disabled");
        }
        else
        {
            Debug.LogWarning("Damage Panel not assigned! No visual feedback will appear when taking damage.");
        }
    }
    
    public void TakeDamage(int damage)
    {
        // Check cooldown to prevent rapid damage
        if (Time.time < lastDamageTime + damageCooldown)
        {
            Debug.Log("Damage cooldown active, ignoring damage");
            return;
        }
        
        lastDamageTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        UpdateHealthUI();
        
        // Show damage panel effect
        ShowDamagePanel();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void ShowDamagePanel()
    {
        if (damagePanel == null) return;
        
        // Stop any existing damage panel coroutine
        if (damagePanelCoroutine != null)
        {
            StopCoroutine(damagePanelCoroutine);
        }
        
        // Start new damage panel effect
        damagePanelCoroutine = StartCoroutine(DamagePanelEffect());
    }
    
    private IEnumerator DamagePanelEffect()
    {
        // Activate the damage panel
        damagePanel.gameObject.SetActive(true);
        
        // Set to damage color
        damagePanel.color = damageColor;
        
        Debug.Log("Damage panel activated");
        
        // Wait for the duration
        yield return new WaitForSeconds(damagePanelDuration);
        
        // Deactivate the damage panel
        damagePanel.gameObject.SetActive(false);
        
        // Reset to original color (optional)
        damagePanel.color = originalPanelColor;
        
        Debug.Log("Damage panel deactivated");
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        Debug.Log($"Player healed {amount}! Health: {currentHealth}/{maxHealth}");
        
        UpdateHealthUI();
    }
    
    private void UpdateHealthUI()
    {
        if (heartIcons == null || heartIcons.Length == 0) 
        {
            Debug.LogWarning("Heart icons array is null or empty");
            return;
        }
        
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null)
            {
                // DOUBLE CHECK: Ensure heart is active and enabled
                if (!heartIcons[i].gameObject.activeInHierarchy)
                {
                    heartIcons[i].gameObject.SetActive(true);
                }
                if (!heartIcons[i].enabled)
                {
                    heartIcons[i].enabled = true;
                }
                
                // Show full heart if current health is greater than this heart index
                if (i < currentHealth)
                {
                    heartIcons[i].sprite = fullHeartSprite;
                    heartIcons[i].color = Color.white;
                }
                else
                {
                    heartIcons[i].sprite = emptyHeartSprite;
                    heartIcons[i].color = new Color(1f, 1f, 1f, 0.5f);
                }
            }
            else
            {
                Debug.LogWarning($"Heart icon at index {i} is null!");
            }
        }
        
        // Force UI update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(heartsContainer as RectTransform);
        
        Debug.Log($"Health UI updated: {currentHealth}/{maxHealth} hearts");
    }
    
    private void Die()
    {
        Debug.Log("Player died!");
        // Add your death logic here (respawn, game over, etc.)
    }
    
    // Method to change max health
    public void SetMaxHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        InitializeHearts();
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    // Check if player is at full health
    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }
    
    // Get missing health amount
    public int GetMissingHealth()
    {
        return maxHealth - currentHealth;
    }
    
    // Reset health to max
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        Debug.Log("Player health reset to full");
    }
    
    // Check if can take damage (cooldown)
    public bool CanTakeDamage()
    {
        return Time.time >= lastDamageTime + damageCooldown;
    }
    
    // Force refresh the hearts (call this if hearts disappear)
    [ContextMenu("Force Refresh Hearts")]
    public void ForceRefreshHearts()
    {
        Debug.Log("Forcing heart refresh...");
        InitializeHearts();
        UpdateHealthUI();
    }
    
    // Test damage panel effect without taking damage
    [ContextMenu("Test Damage Panel")]
    public void TestDamagePanel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Test only works in Play Mode!");
            return;
        }
        
        Debug.Log("=== TESTING DAMAGE PANEL ===");
        ShowDamagePanel();
    }
    
    // Context menu for testing in editor
    [ContextMenu("Test Take Damage")]
    private void TestTakeDamage()
    {
        TakeDamage(1);
    }
    
    [ContextMenu("Test Heal")]
    private void TestHeal()
    {
        Heal(1);
    }
    
    [ContextMenu("Reset Health")]
    private void TestResetHealth()
    {
        ResetHealth();
    }
    
    // Debug info
    [ContextMenu("Debug Health Info")]
    private void DebugHealthInfo()
    {
        Debug.Log($"Health: {currentHealth}/{maxHealth}");
        Debug.Log($"Hearts Container: {heartsContainer}");
        Debug.Log($"Heart Prefab: {heartIconPrefab}");
        Debug.Log($"Heart Icons Array: {(heartIcons != null ? heartIcons.Length : 0)} elements");
        Debug.Log($"Damage Panel: {(damagePanel != null ? damagePanel.name : "Not assigned")}");
        Debug.Log($"Damage Panel Active: {(damagePanel != null ? damagePanel.gameObject.activeInHierarchy : false)}");
        
        if (heartsContainer != null)
        {
            Debug.Log($"Container Children: {heartsContainer.childCount}");
            for (int i = 0; i < heartsContainer.childCount; i++)
            {
                Transform child = heartsContainer.GetChild(i);
                Image img = child.GetComponent<Image>();
                Debug.Log($"Child {i}: {child.name} - Active: {child.gameObject.activeInHierarchy} - Image Enabled: {(img != null ? img.enabled : false)}");
            }
        }
        
        if (heartIcons != null)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] != null)
                {
                    Debug.Log($"Heart {i}: Active={heartIcons[i].gameObject.activeInHierarchy}, Enabled={heartIcons[i].enabled}, Sprite={heartIcons[i].sprite?.name}");
                }
            }
        }
    }
}