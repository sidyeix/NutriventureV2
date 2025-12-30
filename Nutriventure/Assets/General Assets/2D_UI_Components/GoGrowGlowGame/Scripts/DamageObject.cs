using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 1f;
    public bool respawnPlayer = true;
    public bool destroyOnContact = false;

    [Header("Knockback Settings")]
    public bool applyKnockback = true;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Visual Feedback")]
    public GameObject damageEffect;

    [Header("Audio")]
    public AudioClip damageSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("=== PLAYER TOUCHED DAMAGE OBJECT ===");
            ApplyDamage(other.transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("=== PLAYER COLLIDED WITH DAMAGE OBJECT ===");
            ApplyDamage(collision.transform);
        }
    }

    private void ApplyDamage(Transform playerTransform = null)
    {
        // 1. FIRST - SHOW THE DAMAGE PANEL
        ShowDamagePanel();

        // 2. Check if GameManager exists
        if (GoGrowGlowGameManager.Instance == null)
        {
            Debug.LogError("GameManager is NULL!");
            return;
        }

        if (GoGrowGlowGameManager.Instance.IsRespawning())
        {
            Debug.Log("Player is respawning, ignoring damage");
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
        if (respawnPlayer && damageAmount >= 1f)
        {
            GoGrowGlowGameManager.Instance.LoseLife();
        }
        else
        {
            GoGrowGlowGameManager.Instance.LoseLifeAmount(damageAmount, false);
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

        Debug.Log($"Damage applied: {damageAmount}");
    }

    private void ShowDamagePanel()
    {
        if (DamagePanelController.Instance != null)
        {
            DamagePanelController.Instance.ShowDamagePanel();
        }
        else
        {
            Debug.LogError("DamagePanelController not found! Make sure the script is attached to your damage panel UI.");

            // Try to find it anyway as a fallback
            DamagePanelController panelController = FindObjectOfType<DamagePanelController>();
            if (panelController != null)
            {
                panelController.ShowDamagePanel();
            }
            else
            {
                Debug.LogError("Could not find DamagePanelController in the scene!");
            }
        }
    }
}