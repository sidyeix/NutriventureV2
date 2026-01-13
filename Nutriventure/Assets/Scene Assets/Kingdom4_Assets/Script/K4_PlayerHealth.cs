using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    private SugariaPlayerStat sugariaUI;

    [Header("Health Settings")]
    public int maxHearts = 6;
    public int currentHearts;
    
    [Header("UI References")]
    public Transform heartsContainer;
    public GameObject heartPrefab;
    public Color activeHeartColor = Color.red;
    public Color lostHeartColor = Color.black;
    
    [Header("Damage Overlay Settings")]
    public Image damageOverlay; // Assign a full-screen UI Image
    public Color damageOverlayColor = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
    public float overlayFadeInTime = 0.1f;
    public float overlayStayTime = 0.2f;
    public float overlayFadeOutTime = 0.5f;
    
    [Header("Damage Effects")]
    public AudioClip damageSound;
    public ParticleSystem damageParticles;
    public float damageFlashDuration = 0.3f;
    
    private List<Image> heartImages = new List<Image>();
    private bool isInvulnerable = false;
    private float invulnerabilityTime = 1f;
    private Coroutine overlayCoroutine;
    
    void Start()
{
    sugariaUI = GetComponent<SugariaPlayerStat>();

    if (sugariaUI == null)
    {
        Debug.LogError("SugariaPlayerStat not found! UI references will be missing.");
        return;
    }

    // Use Sugaria's container and prefab
    heartsContainer = sugariaUI.heartsContainer;
    heartPrefab = sugariaUI.heartIconPrefab.gameObject;

    InitializeHearts();
    InitializeDamageOverlay();
}

    
   private void InitializeHearts()
{
    if (heartsContainer == null)
    {
        Debug.LogError("Hearts container is null!");
        return;
    }

    heartImages.Clear();

    foreach (Transform child in heartsContainer)
    {
        Image img = child.GetComponent<Image>();
        if (img != null)
        {
            heartImages.Add(img);
        }
    }

    // 🔥 THIS IS THE KEY FIX
    maxHearts = heartImages.Count;
    currentHearts = maxHearts;

    Debug.Log($"PlayerHealth synced to UI hearts: {maxHearts}");
}


    
    private void InitializeDamageOverlay()
{
    if (damageOverlay != null)
    {
        damageOverlay.gameObject.SetActive(true);
        damageOverlay.color = new Color(
            damageOverlayColor.r,
            damageOverlayColor.g,
            damageOverlayColor.b,
            0f
        );
    }
}

    
    public void TakeDamage(int damage)
{
    if (isInvulnerable) return;

    // 🔥 LET SUGARIA UPDATE THE UI
    sugariaUI.TakeDamage(damage);

    StartCoroutine(DamageSequence(damage));
}



    private void ValidateHeartImages()
{
    for (int i = heartImages.Count - 1; i >= 0; i--)
    {
        // Unity-destroyed object check
        if (heartImages[i] == null || heartImages[i].gameObject == null)
        {
            heartImages.RemoveAt(i);
        }
    }
}

    
    private IEnumerator DamageSequence(int damage)
{
    isInvulnerable = true;

    // Effects ONLY
    PlayDamageEffects();
    ShowDamageOverlay();
    StartCoroutine(FlashPlayer());

    yield return new WaitForSeconds(invulnerabilityTime);

    isInvulnerable = false;

    if (sugariaUI.currentHealth <= 0)
    {
        GameOver();
    }
}

    
    private void ShowDamageOverlay()
    {
        if (damageOverlay != null)
        {
            // Stop any existing overlay coroutine
            if (overlayCoroutine != null)
            {
                StopCoroutine(overlayCoroutine);
            }
            
            // Start new overlay effect
            overlayCoroutine = StartCoroutine(DamageOverlayEffect());
        }
    }
    
    private IEnumerator DamageOverlayEffect()
    {
        if (damageOverlay == null) yield break;
        
        // Fade in
        float timer = 0f;
        while (timer < overlayFadeInTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, damageOverlayColor.a, timer / overlayFadeInTime);
            damageOverlay.color = new Color(damageOverlayColor.r, damageOverlayColor.g, damageOverlayColor.b, alpha);
            yield return null;
        }
        
        // Stay visible
        yield return new WaitForSeconds(overlayStayTime);
        
        // Fade out
        timer = 0f;
        Color startColor = damageOverlay.color;
        while (timer < overlayFadeOutTime)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(damageOverlayColor.a, 0f, timer / overlayFadeOutTime);
            damageOverlay.color = new Color(damageOverlayColor.r, damageOverlayColor.g, damageOverlayColor.b, alpha);
            yield return null;
        }
        
        // Ensure completely transparent
        damageOverlay.color = new Color(damageOverlayColor.r, damageOverlayColor.g, damageOverlayColor.b, 0f);
    }
    
    private void UpdateHeartsUI()
{
    if (heartImages.Count == 0) return;

    currentHearts = Mathf.Clamp(currentHearts, 0, heartImages.Count);

    for (int i = 0; i < heartImages.Count; i++)
    {
        if (heartImages[i] == null) continue;

        heartImages[i].color = i < currentHearts
            ? activeHeartColor
            : lostHeartColor;
    }
}



    
    private void PlayDamageEffects()
    {
        // Play sound
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }
        
        // Play particles
        if (damageParticles != null)
        {
            damageParticles.Play();
        }
    }
    
    private IEnumerator FlashPlayer()
    {
        Renderer playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            Color originalColor = playerRenderer.material.color;
            playerRenderer.material.color = Color.red;
            yield return new WaitForSeconds(damageFlashDuration);
            playerRenderer.material.color = originalColor;
        }
    }
    
    private void GameOver()
    {
        Debug.Log("Game Over! All hearts lost.");
        
        // Optional: Show persistent red overlay on game over
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1f, 0f, 0f, 0.5f); // Darker red for game over
        }
        
        // Add your game over logic here
        // Example: Show game over screen, restart level, etc.
    }
    
    public void Heal(int healAmount)
{
    sugariaUI.Heal(healAmount);
}

    
    public void ResetHealth()
    {
        currentHearts = maxHearts;
        UpdateHeartsUI();
        isInvulnerable = false;
        
        // Reset overlay
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(damageOverlayColor.r, damageOverlayColor.g, damageOverlayColor.b, 0f);
        }
    }
    
    // For debugging
    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        TakeDamage(1);
    }
    
    [ContextMenu("Test Heal")]
    public void TestHeal()
    {
        Heal(1);
    }
    
    [ContextMenu("Test Overlay")]
    public void TestOverlay()
    {
        ShowDamageOverlay();
    }
}