using UnityEngine;

public class SugariaPlayerStat : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public int currentHealth;
    
    [Header("UI Reference")]
    public UnityEngine.UI.Slider healthBar;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Player health: {currentHealth}/{maxHealth}");
        
        UpdateHealthUI();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        UpdateHealthUI();
    }
    
    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }
    
    private void Die()
    {
        Debug.Log("Player died!");
        // Add your death logic here (respawn, game over, etc.)
    }
}