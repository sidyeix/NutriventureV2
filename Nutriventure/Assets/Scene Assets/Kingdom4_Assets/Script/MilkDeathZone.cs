using UnityEngine;

public class MilkDeathZone : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 1f; // Changed to float to match PlayerHealthManager

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 🛡️ Check shield (same logic used by ItemCollectible)
        if (ItemCollectible.IsShieldActive())
        {
            Debug.Log("🛡️ Shield protected player from milk damage!");
            return;
        }

        // ❤️ Apply damage using AllergenGameManager
        if (AllergenGameManager.Instance != null)
        {
            float before = AllergenGameManager.Instance.currentHealth;
            AllergenGameManager.Instance.TakeDamage(damageAmount);
            Debug.Log($"🥛 Milk damage! Health: {before} → {AllergenGameManager.Instance.currentHealth}");
        }
        else
        {
            Debug.LogWarning("AllergenGameManager not found!");
        }

        // 🔥 Force detach from moving platform
        other.transform.SetParent(null);

        // 🔄 Respawn at last checkpoint
        CheckpointManager.Instance.RespawnPlayer(other.gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 0, 0.3f);

        if (TryGetComponent(out BoxCollider box))
        {
            Gizmos.DrawCube(transform.position + box.center, box.size);
        }
        else if (TryGetComponent(out SphereCollider sphere))
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
        }
    }
}