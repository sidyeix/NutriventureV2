using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

    [Header("Damage Overlay")]
    public GameObject damageOverlayObject; // Assign your inactive GameObject from Hierarchy here
    public float overlayDuration = 0.5f; // How long the overlay stays visible
    public float overlayFadeTime = 0.2f; // Fade in/out time
    private Coroutine overlayCoroutine;

    [Header("Game End Reference")]
    public Kingdom4GameEndManager gameEndManager; // Assign in Inspector

    private List<Image> hearts = new List<Image>();

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
        
        // Ensure damage overlay is inactive at start
        if (damageOverlayObject != null)
        {
            damageOverlayObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Damage overlay object is not assigned! Please drag your inactive GameObject from Hierarchy.");
        }
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
        
        // Show damage overlay
        ShowDamageOverlay();
        
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

    #region DAMAGE OVERLAY SYSTEM
    void ShowDamageOverlay()
    {
        // Check if damage overlay object is assigned
        if (damageOverlayObject == null)
        {
            Debug.LogWarning("Damage overlay object is not assigned!");
            return;
        }
        
        // Stop any existing overlay coroutine
        if (overlayCoroutine != null)
        {
            StopCoroutine(overlayCoroutine);
        }
        
        // Start new overlay
        overlayCoroutine = StartCoroutine(DamageOverlayRoutine());
    }

    IEnumerator DamageOverlayRoutine()
    {
        // Activate the GameObject
        damageOverlayObject.SetActive(true);
        
        // Get CanvasGroup for fading (optional - add if your GameObject doesn't have it)
        CanvasGroup canvasGroup = damageOverlayObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damageOverlayObject.AddComponent<CanvasGroup>();
        }
        
        // Fade in (optional)
        float timer = 0f;
        while (timer < overlayFadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / overlayFadeTime);
            yield return null;
        }
        canvasGroup.alpha = 1;
        
        // Wait for duration
        yield return new WaitForSeconds(overlayDuration);
        
        // Fade out (optional)
        timer = 0f;
        while (timer < overlayFadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, timer / overlayFadeTime);
            yield return null;
        }
        canvasGroup.alpha = 0;
        
        // Deactivate the GameObject
        damageOverlayObject.SetActive(false);
        
        // Reset alpha to 1 for next time (if fading is used)
        canvasGroup.alpha = 1;
        
        overlayCoroutine = null;
    }

    // Simple version without fading (if you prefer)
    IEnumerator SimpleDamageOverlayRoutine()
    {
        // Activate the GameObject
        damageOverlayObject.SetActive(true);
        
        // Wait for duration
        yield return new WaitForSeconds(overlayDuration);
        
        // Deactivate the GameObject
        damageOverlayObject.SetActive(false);
        
        overlayCoroutine = null;
    }

    // Call this to manually turn off overlay (optional)
    public void HideDamageOverlay()
    {
        if (overlayCoroutine != null)
        {
            StopCoroutine(overlayCoroutine);
            overlayCoroutine = null;
        }
        
        if (damageOverlayObject != null)
        {
            damageOverlayObject.SetActive(false);
            
            // Reset alpha if using CanvasGroup
            CanvasGroup canvasGroup = damageOverlayObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1;
            }
        }
    }
    #endregion
    
    #region PUBLIC METHODS FOR RESET
    public void ResetHealth()
    {
        currentHealth = maxHearts;
        UpdateHearts();
        HideDamageOverlay(); // Ensure overlay is off when resetting
    }
    
    public void FullHeal()
    {
        currentHealth = maxHearts;
        UpdateHearts();
    }
    
    public int GetCurrentHealth()
    {
        return Mathf.CeilToInt(currentHealth);
    }
    #endregion
    
    // Clean up when object is destroyed
    void OnDestroy()
    {
        HideDamageOverlay();
    }
}