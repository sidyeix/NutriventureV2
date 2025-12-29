using UnityEngine;

public class JunkFood : MonoBehaviour
{
    [Header("Food Settings")]
    public int pointsDeduction = 120;
    public Sprite foodSprite; // Add this field - same as GoFood

    [Header("Optional Visual Effects")]
    public GameObject negativeEffect;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectJunkFood();
        }
    }

    private void CollectJunkFood()
    {
        // Play optional visual effect
        if (negativeEffect != null)
        {
            negativeEffect.SetActive(true);
            negativeEffect.transform.SetParent(null);
            Destroy(negativeEffect, 3f);
        }

        // Notify GameManager
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.CollectJunkFood(gameObject);

            // Show food feedback UI with this food's sprite
            if (foodSprite != null)
            {
                GoGrowGlowGameManager.Instance.ShowFoodFeedback(foodSprite);
            }
        }

        // Destroy the junk food object
        Destroy(gameObject);
    }
}