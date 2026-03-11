using UnityEngine;
using System.Collections;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 1f;
    public bool respawnPlayer = true;
    public bool destroyOnContact = false;

    [Header("Damage Type")]
    public bool reduceEnergyInstead = false; // Check this to reduce energy instead of life
    public float energyReductionAmount = 20f; // How much energy to reduce

    [Header("Damage Panel Settings")]
    [SerializeField] private float panelDisplayTime = 1f;
    [SerializeField] private bool useCustomDisplayTime = false;
    [SerializeField] private GameObject damagePanel; // Drag your UI damage panel here

    [Header("Knockback Settings")]
    public bool applyKnockback = true;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Visual Feedback")]
    public GameObject damageEffect;

    [Header("Audio")]
    public AudioClip damageSound;

    void Start()
    {
        // Hide panel at start if assigned
        if (damagePanel != null)
        {
            damagePanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
#if UNITY_EDITOR
            Debug.Log("=== PLAYER TOUCHED DAMAGE OBJECT ===");
#endif
            ApplyDamage(other.transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
#if UNITY_EDITOR
            Debug.Log("=== PLAYER COLLIDED WITH DAMAGE OBJECT ===");
#endif
            ApplyDamage(collision.transform);
        }
    }

    private void ApplyDamage(Transform playerTransform = null)
    {
        // 1. Show damage panel if assigned
        if (damagePanel != null)
        {
            StartCoroutine(ShowDamagePanel());
        }

        // 2. Check if GameManager exists
        if (GoGrowGlowGameManager.Instance == null)
        {
#if UNITY_EDITOR
            Debug.LogError("GameManager is NULL!");
#endif
            return;
        }

        if (GoGrowGlowGameManager.Instance.IsRespawning())
        {
            return;
        }

        // 3. Calculate knockback direction
        Vector3 knockbackDirection = Vector3.zero;
        if (playerTransform != null && applyKnockback)
        {
            knockbackDirection = (playerTransform.position - transform.position).normalized;
            knockbackDirection.y = 0.2f;
            knockbackDirection.Normalize();
        }

        // 4. Apply knockback if enabled
        if (applyKnockback && knockbackForce > 0 && playerTransform != null)
        {
            GoGrowGlowGameManager.Instance.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }

        // 5. Apply damage through GameManager
        if (reduceEnergyInstead)
        {
            // Reduce energy instead of life
            GoGrowGlowGameManager.Instance.RemoveEnergy(energyReductionAmount);
#if UNITY_EDITOR
            Debug.Log($"Energy reduced by: {energyReductionAmount}");
#endif
        }
        else
        {
            // Apply life damage
            if (respawnPlayer && damageAmount >= 1f)
            {
                GoGrowGlowGameManager.Instance.LoseLife();
            }
            else
            {
                GoGrowGlowGameManager.Instance.LoseLifeAmount(damageAmount, false);
            }
#if UNITY_EDITOR
            Debug.Log($"Life damage applied: {damageAmount}");
#endif
        }

        // 6. Visual effect
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }

        // 7. Sound
        if (damageSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);
        }

        // 8. Destroy if needed
        if (destroyOnContact)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShowDamagePanel()
    {
        // Show the panel
        damagePanel.SetActive(true);

        // Wait for display time
        float displayTime = useCustomDisplayTime ? panelDisplayTime : 1f;
        yield return CoroutineYieldCache.WaitForSeconds(displayTime);

        // Hide the panel
        damagePanel.SetActive(false);
    }

    // Public method to test the panel
    public void TestPanel()
    {
        if (damagePanel != null)
        {
            StartCoroutine(ShowDamagePanel());
#if UNITY_EDITOR
            Debug.Log($"Testing damage panel for {panelDisplayTime} seconds");
#endif
        }
    }
}