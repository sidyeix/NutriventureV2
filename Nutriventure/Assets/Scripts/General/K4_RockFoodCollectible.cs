using UnityEngine;

public class K4_RockFoodCollectible : MonoBehaviour
{
  [SerializeField] private bool isSafeFood = true;
  [SerializeField] private int safePoints = 100;
  [SerializeField] private int unsafePoints = 50;
  [SerializeField] private AudioClip collectSound;

  private bool collected;

  private void OnTriggerEnter(Collider other)
  {
    if (collected) return;
    if (!other.CompareTag("Player")) return;

    collected = true;

    if (isSafeFood)
    {
      if (AllergenGameManager.Instance != null)
        AllergenGameManager.Instance.AddPoints(safePoints);
    }
    else
    {
      if (AllergenGameManager.Instance != null)
        AllergenGameManager.Instance.DeductPoints(unsafePoints);

      if (AllergenGameManager.Instance != null)
        AllergenGameManager.Instance.TakeDamage(1f);
    }

    if (collectSound != null)
      AudioSource.PlayClipAtPoint(collectSound, transform.position);

    Destroy(gameObject);
  }
}
