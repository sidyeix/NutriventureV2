using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SugariaVFX : MonoBehaviour
{
    public static SugariaVFX Instance { get; private set; }
    
    [Header("Screen Flash Settings")]
    public Image damageFlashImage;
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.3f);
    public float flashDuration = 0.3f;
    
    [Header("Screen Shake Settings")]
    public float screenShakeIntensity = 0.3f;
    public float screenShakeDuration = 0.5f;
    
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Coroutine flashCoroutine;
    private Coroutine shakeCoroutine;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.localPosition;
        }
        
        // Initialize flash image to transparent
        if (damageFlashImage != null)
        {
            damageFlashImage.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, 0f);
        }
        else
        {
            Debug.LogWarning("Damage Flash Image not assigned in VisualEffectsManager!");
        }
    }
    
    // ========== PUBLIC METHODS ==========
    
    public void TriggerDamageEffects()
    {
        FlashScreen();
        ShakeScreen();
    }
    
    public void FlashScreen()
    {
        if (damageFlashImage == null) 
        {
            Debug.LogWarning("Flash image is not assigned!");
            return;
        }
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashRoutine());
    }
    
    public void ShakeScreen()
    {
        if (mainCamera == null) return;
        
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeRoutine());
    }
    
    public void FlashScreenCustom(Color color, float duration)
    {
        if (damageFlashImage == null) return;
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        
        flashCoroutine = StartCoroutine(FlashRoutineCustom(color, duration));
    }
    
    // ========== COROUTINES ==========
    
    private IEnumerator FlashRoutine()
    {
        damageFlashImage.color = damageFlashColor;
        
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(damageFlashColor.a, 0f, elapsed / flashDuration);
            damageFlashImage.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, alpha);
            yield return null;
        }
        
        damageFlashImage.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, 0f);
        flashCoroutine = null;
    }
    
    private IEnumerator FlashRoutineCustom(Color color, float duration)
    {
        damageFlashImage.color = color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
            damageFlashImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        
        damageFlashImage.color = new Color(color.r, color.g, color.b, 0f);
        flashCoroutine = null;
    }
    
    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        
        while (elapsed < screenShakeDuration)
        {
            elapsed += Time.deltaTime;
            
            float currentIntensity = screenShakeIntensity * (1f - (elapsed / screenShakeDuration));
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * currentIntensity;
            
            mainCamera.transform.localPosition = originalCameraPosition + randomOffset;
            
            yield return null;
        }
        
        mainCamera.transform.localPosition = originalCameraPosition;
        shakeCoroutine = null;
    }
    
    // Clean up
    private void OnDestroy()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        
        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = originalCameraPosition;
        }
    }
}