using UnityEngine;

public class K4_HeartCollectible : MonoBehaviour
{
  [SerializeField] private float healAmount = 1f;
  [SerializeField] private AudioClip collectSound;

  private bool collected;

  private void OnTriggerEnter(Collider other)
  {
    if (collected) return;
    if (!other.CompareTag("Player")) return;

    if (AllergenGameManager.Instance == null) return;

    // Only collect if the player is not already at max hearts
    if (AllergenGameManager.Instance.currentHealth >= AllergenGameManager.Instance.maxHearts)
      return;

    collected = true;

    AllergenGameManager.Instance.Heal(healAmount);

    if (collectSound != null)
      AudioSource.PlayClipAtPoint(collectSound, transform.position);

    Destroy(gameObject);
  }
}
