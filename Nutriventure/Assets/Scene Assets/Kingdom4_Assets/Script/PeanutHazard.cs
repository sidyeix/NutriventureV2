using UnityEngine;

public class PeanutHazard : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 1;
    public float damageCooldown = 2f;
    
    private float lastDamageTime;
    
    void Start()
    {
        // Start any idle animations if you have them
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("Idle");
        }
    }
    
    // void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         TryDamagePlayer(other.gameObject);
    //     }
    // }
    
    // void OnTriggerStay(Collider other)
    // {
    //     if (other.CompareTag("Player"))
    //     {
    //         TryDamagePlayer(other.gameObject);
    //     }
    // }
    
    // void TryDamagePlayer(GameObject player)
    // {
    //     if (Time.time - lastDamageTime >= damageCooldown)
    //     {
    //         DamagePlayer(player);
    //         lastDamageTime = Time.time;
    //     }
    // }
    
    // void DamagePlayer(GameObject player)
    // {
    //     PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
    //     if (playerHealth != null)
    //     {
    //         playerHealth.TakeDamage(damageAmount);
            
    //         // Visual feedback
    //         StartCoroutine(PlayTouchEffects());
    //     }
    // }
    
    System.Collections.IEnumerator PlayTouchEffects()
    {
        // Flash the peanut when damaging player
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.material.color;
            renderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            renderer.material.color = originalColor;
        }
    }
}