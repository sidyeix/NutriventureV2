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
            Debug.Log("=== PLAYER TOUCHED DAMAGE ===");
            ApplyDamage(other.transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("=== PLAYER COLLIDED WITH DAMAGE ===");
            ApplyDamage(collision.transform);
        }
    }

    private void ApplyDamage(Transform playerTransform = null)
    {
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

        // 1. TRIGGER DAMAGE ANIMATION (THIS WILL SHOW PANEL)
        GoGrowGlowGameManager.Instance.TriggerDamageAnimation("isDamaged", 1f);

        Debug.Log("Damage animation triggered - panel should show!");

        // 2. Calculate knockback direction
        Vector3 knockbackDirection = Vector3.zero;
        if (playerTransform != null && applyKnockback)
        {
            knockbackDirection = (playerTransform.position - transform.position).normalized;
            knockbackDirection.y = 0.2f;
            knockbackDirection.Normalize();
        }

        // 3. Apply knockback if enabled
        if (applyKnockback && knockbackForce > 0 && playerTransform != null)
        {
            GoGrowGlowGameManager.Instance.ApplyKnockback(knockbackDirection, knockbackForce, knockbackDuration);
        }

        // 4. Apply damage through GameManager
        if (respawnPlayer && damageAmount >= 1f)
        {
            GoGrowGlowGameManager.Instance.LoseLife();
        }
        else
        {
            GoGrowGlowGameManager.Instance.LoseLifeAmount(damageAmount, false);
        }

        // 5. Visual effect
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }

        // 6. Sound
        if (damageSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);
        }

        // 7. Destroy if needed
        if (destroyOnContact)
        {
            Destroy(gameObject);
        }

        Debug.Log($"Damage applied: {damageAmount}");
    }
}