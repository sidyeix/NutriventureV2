using UnityEngine;

public class PeanutHazard : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 2f;
    
    [Header("Disappear Settings")]
    public bool disappearOnContact = true;
    public float disappearDelay = 0.5f;
    
    private float lastDamageTime;
    private bool hasDamaged = false;
    
    void Start()
    {
        // Start any idle animations if you have them
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Idle");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other.gameObject);
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryDamagePlayer(other.gameObject);
        }
    }
    
    void TryDamagePlayer(GameObject player)
    {
        if (Time.time - lastDamageTime >= damageCooldown && !hasDamaged)
        {
            DamagePlayer(player);
            lastDamageTime = Time.time;
        }
    }
    
    void DamagePlayer(GameObject player)
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
            
            // Visual feedback
            StartCoroutine(PlayTouchEffects());
            
            // Make ingredient disappear
            if (disappearOnContact)
            {
                hasDamaged = true;
                StartCoroutine(DisappearAfterDelay());
            }
        }
    }
    
    System.Collections.IEnumerator PlayTouchEffects()
    {
        // Flash the ingredient when damaging player
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            renderer.material.color = originalColor;
        }
    }
    
    System.Collections.IEnumerator DisappearAfterDelay()
    {
        yield return new WaitForSeconds(disappearDelay);
        
        // Play disappear animation if available
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Disappear");
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        }
        
        // Disable or destroy the object
        gameObject.SetActive(false);
        // Or use: Destroy(gameObject);
    }
    
    // Optional: Reset when player respawns
    public void ResetHazard()
    {
        hasDamaged = false;
        gameObject.SetActive(true);
    }
}