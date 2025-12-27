using UnityEngine;

public class JunkFood : MonoBehaviour
{
    [Header("Food Settings")]
    public int pointsDeduction = 120;

    [Header("Optional Visual Effects")]
    public GameObject negativeEffect; // GameObject with ParticleSystem

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectJunkFood();
        }
    }

    private void CollectJunkFood()
    {
        // Play optional visual effect (if assigned on prefab)
        if (negativeEffect != null)
        {
            negativeEffect.SetActive(true);
            negativeEffect.transform.SetParent(null);
            Destroy(negativeEffect, 3f); // Destroy after 3 seconds
        }

        // Notify GameManager
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.CollectJunkFood(gameObject);
        }

        // Destroy the junk food object
        Destroy(gameObject);
    }
}