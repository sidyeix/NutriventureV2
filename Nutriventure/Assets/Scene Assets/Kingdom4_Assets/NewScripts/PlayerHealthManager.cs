using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using StarterAssets;

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
    
    // ADDED: AudioSource component reference
    private AudioSource audioSource;
    
    private void Die()
    {
        Debug.Log("Player Died!");
        
        // Play death sound with fallback
        PlaySoundEffect(deathSound);
        
        // Disable player controls
        ThirdPersonController playerController = GetComponent<ThirdPersonController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Trigger game over in Kingdom4GameEndManager
        if (Kingdom4GameEndManager.Instance != null)
        {
            Kingdom4GameEndManager.Instance.HandleKingdom4GameOver();
        }
        else
        {
            // Fallback: Try to find it in the scene
            Kingdom4GameEndManager gameEndManager = FindObjectOfType<Kingdom4GameEndManager>();
            if (gameEndManager != null)
            {
                gameEndManager.HandleKingdom4GameOver();
            }
            else
            {
                Debug.LogWarning("Kingdom4GameEndManager not found! Game over screen won't show.");
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
            
        // ADDED: Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        
        // ADDED: Play damage sound immediately when taking damage
        PlayDamageSound();
        
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
        
        // ADDED: Use the new PlaySoundEffect method
        PlaySoundEffect(healSound);
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
        
        // Activate damage overlay if it exists
        if (damageOverlay != null)
        {
            damageOverlay.gameObject.SetActive(true);
            
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
            
            // Deactivate overlay after fade out
            damageOverlay.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(invulnerabilityTime - overlayFadeTime * 2 - 0.1f);
        
        isInvulnerable = false;
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHearts;
        UpdateHeartsUI();
        isInvulnerable = false;
        
        // Ensure damage overlay is deactivated
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
            damageOverlay.gameObject.SetActive(false);
        }
        
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }
    
    private void PlayDamageEffects()
    {
        // Play particles
        if (damageParticles != null)
            damageParticles.Play();
        
        // Flash player
        if (playerRenderer != null)
            StartCoroutine(FlashPlayer());
    }
    
    // ADDED: Separate method to play damage sound
    private void PlayDamageSound()
    {
        PlaySoundEffect(damageSound);
    }
    
    // ADDED: Generic method to play sound effects with multiple fallbacks
    private void PlaySoundEffect(AudioClip clip)
    {
        if (clip == null) return;
        
        // Try AudioHandler first
        if (AudioHandler.Instance != null && AudioHandler.Instance.soundEffectsSource != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(clip);
            return;
        }
        
        // Fallback to local AudioSource
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return;
        }
        
        // Last resort: Create a temporary AudioSource
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }
    
    private IEnumerator FlashPlayer()
    {
        if (playerRenderer == null) yield break;
        
        playerRenderer.material.color = Color.red;
        yield return new WaitForSeconds(damageFlashDuration);
        playerRenderer.material.color = originalPlayerColor;
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