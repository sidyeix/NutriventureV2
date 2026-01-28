using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerHealthManager : MonoBehaviour
{
    public static PlayerHealthManager Instance { get; private set; }

    [Header("Health Settings")]
    public int maxHearts = 5;
    public float currentHealth;
    
    [Header("UI References")]
    public Transform heartsContainer;
    public GameObject heartPrefab;
    public Sprite fullHeartSprite;
    public Sprite halfHeartSprite;
    public Sprite emptyHeartSprite;
    
    [Header("Damage Settings")]
    public float damageCooldown = 1f;
    public float invulnerabilityTime = 1f;
    public Image damageOverlay;
    public float overlayFadeTime = 0.5f;
    
    [Header("Audio")]
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip deathSound;
    
    [Header("Effects")]
    public ParticleSystem damageParticles;
    public float damageFlashDuration = 0.3f;
    
    private List<Image> heartImages = new List<Image>();
    private float lastDamageTime;
    private bool isInvulnerable = false;
    private Coroutine damageCoroutine;
    private Renderer playerRenderer;
    private Color originalPlayerColor;
    
     // Add game over/phase reset logic
    private void Die()
    {
        Debug.Log("Player Died!");
        
        // Check current phase to handle death appropriately
        if (AllerthriaGameManager.Instance != null)
        {
            switch (AllerthriaGameManager.Instance.currentPhase)
            {
                case AllerthriaGameManager.GamePhase.AllergenHunt:
                    // Respawn at checkpoint
                    break;
                case AllerthriaGameManager.GamePhase.WagonPhase:
                    // Restart wagon phase
                    break;
                case AllerthriaGameManager.GamePhase.PlatformPhase:
                    // Restart platform phase
                    break;
            }
        }
    }

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
        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
            originalPlayerColor = playerRenderer.material.color;
            
        InitializeHearts();
        currentHealth = maxHearts;
        UpdateHeartsUI();
        
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
        }
    }
    
    private void InitializeHearts()
    {
        if (heartsContainer == null || heartPrefab == null) return;
        
        heartImages.Clear();
        
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);
        
        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            Image heartImage = heart.GetComponent<Image>();
            if (heartImage != null)
            {
                heartImages.Add(heartImage);
                heartImage.sprite = fullHeartSprite;
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isInvulnerable || Time.time < lastDamageTime + damageCooldown) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        lastDamageTime = Time.time;
        
        UpdateHeartsUI();
        PlayDamageEffects();
        
        if (damageCoroutine != null) StopCoroutine(damageCoroutine);
        damageCoroutine = StartCoroutine(DamageSequence());
        
        if (currentHealth <= 0)
            Die();
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHearts);
        
        UpdateHeartsUI();
        
        if (AudioHandler.Instance != null && healSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(healSound);
    }
    
    private void UpdateHeartsUI()
    {
        if (heartImages.Count == 0) return;
        
        float remainingHealth = currentHealth;
        
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] == null) continue;
            
            if (remainingHealth >= 1f)
            {
                heartImages[i].sprite = fullHeartSprite;
                remainingHealth -= 1f;
            }
            else if (remainingHealth >= 0.5f)
            {
                heartImages[i].sprite = halfHeartSprite;
                remainingHealth -= 0.5f;
            }
            else
            {
                heartImages[i].sprite = emptyHeartSprite;
            }
        }
    }
    
    private IEnumerator DamageSequence()
    {
        isInvulnerable = true;
        
        // Show damage overlay
        if (damageOverlay != null)
        {
            float timer = 0f;
            while (timer < overlayFadeTime)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.3f, timer / overlayFadeTime);
                damageOverlay.color = new Color(1f, 0f, 0f, alpha);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.1f);
            
            timer = 0f;
            while (timer < overlayFadeTime)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0.3f, 0f, timer / overlayFadeTime);
                damageOverlay.color = new Color(1f, 0f, 0f, alpha);
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(invulnerabilityTime - overlayFadeTime * 2 - 0.1f);
        
        isInvulnerable = false;
    }
    
    private void PlayDamageEffects()
    {
        // Play sound
        if (AudioHandler.Instance != null && damageSound != null)
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);
        
        // Play particles
        if (damageParticles != null)
            damageParticles.Play();
        
        // Flash player
        if (playerRenderer != null)
            StartCoroutine(FlashPlayer());
    }
    
    private IEnumerator FlashPlayer()
    {
        if (playerRenderer == null) yield break;
        
        playerRenderer.material.color = Color.red;
        yield return new WaitForSeconds(damageFlashDuration);
        playerRenderer.material.color = originalPlayerColor;
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHearts;
        UpdateHeartsUI();
        isInvulnerable = false;
        
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
        
        if (damageOverlay != null)
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
    }
    
    public void SetMaxHearts(int newMax)
    {
        maxHearts = Mathf.Max(1, newMax);
        InitializeHearts();
        currentHealth = Mathf.Min(currentHealth, maxHearts);
        UpdateHeartsUI();
    }
    
    public bool IsFullHealth() => currentHealth >= maxHearts;
    public float GetMissingHealth() => maxHearts - currentHealth;
    public bool CanTakeDamage() => Time.time >= lastDamageTime + damageCooldown && !isInvulnerable;
}