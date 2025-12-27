using UnityEngine;

public class DamageObject : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 1f;           // Can be 0.5, 1, 1.5, etc.
    public bool respawnPlayer = true;         // Should player respawn at checkpoint?
    public bool destroyOnContact = false;     // Should this object be destroyed?

    [Header("Effects")]
    public GameObject damageEffect;           // Optional effect on hit
    public AudioClip damageSound;             // Optional sound

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyDamage();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        // Apply damage through GameManager
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.LoseLifeAmount(damageAmount, respawnPlayer);
        }

        // Play effects
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            effect.SetActive(true);
            Destroy(effect, 3f);
        }

        if (damageSound != null && AudioHandler.Instance != null)
        {
            AudioHandler.Instance.soundEffectsSource.PlayOneShot(damageSound);
        }

        // Destroy object if configured
        if (destroyOnContact)
        {
            Destroy(gameObject);
        }
    }
}