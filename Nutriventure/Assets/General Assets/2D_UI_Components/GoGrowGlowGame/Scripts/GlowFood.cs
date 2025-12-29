using UnityEngine;

public class GlowFood : MonoBehaviour
{
    [Header("Food Settings")]
    public int points = 100;
    public float glowEnergyGain = 22f;
    public Sprite foodSprite; // Add this field

    [Header("Optional Visual Effects")]
    public GameObject collectionEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectFood();
        }
    }

    private void CollectFood()
    {
        // Play optional visual effect
        if (collectionEffect != null)
        {
            collectionEffect.SetActive(true);
            collectionEffect.transform.SetParent(null);
            Destroy(collectionEffect, 3f);
        }

        // Notify GameManager
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.CollectGlowFood(gameObject);

            // Show food feedback UI with this food's sprite
            if (foodSprite != null)
            {
                GoGrowGlowGameManager.Instance.ShowFoodFeedback(foodSprite);
            }
        }

        // Destroy the food object
        Destroy(gameObject);
    }
}