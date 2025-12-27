using UnityEngine;

public class GoFood : MonoBehaviour
{
    [Header("Food Settings")]
    public int points = 100;

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
            GoGrowGlowGameManager.Instance.CollectGoFood(gameObject);
        }

        // Destroy the food object
        Destroy(gameObject);
    }
}