using UnityEngine;

/// <summary>
/// Thin wrapper that delegates all health calls to AllergenGameManager.
/// Kept so existing scripts (MonsterAIHeartDamage, MilkDeathZone, ItemCollectible, etc.)
/// continue to work via PlayerHealthManager.Instance without changes.
/// You no longer need this on the Player object — place it on any persistent object,
/// or let AllergenGameManager handle everything directly.
/// </summary>
public class PlayerHealthManager : MonoBehaviour
{
    public static PlayerHealthManager Instance;

    public int maxHearts
    {
        get => AllergenGameManager.Instance != null ? AllergenGameManager.Instance.maxHearts : 5;
    }

    public float currentHealth
    {
        get => AllergenGameManager.Instance != null ? AllergenGameManager.Instance.currentHealth : 0f;
        set
        {
            if (AllergenGameManager.Instance != null)
                AllergenGameManager.Instance.currentHealth = value;
        }
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TakeDamage(float amount)
    {
        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.TakeDamage(amount);
    }

    public void Heal(float amount)
    {
        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.Heal(amount);
    }

    public void ResetHealth()
    {
        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.ResetHealth();
    }

    public void FullHeal()
    {
        ResetHealth();
    }

    public int GetCurrentHealth()
    {
        return Mathf.CeilToInt(currentHealth);
    }
}