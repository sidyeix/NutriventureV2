using UnityEngine;

public class RestrictedArea : MonoBehaviour
{
    [Header("Restricted Area Settings")]
    public bool respawnPlayer = true;
    public GameObject enterEffect;
    public AudioClip enterSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GoGrowGlowGameManager.Instance != null)
            {
                if (respawnPlayer)
                {
                    GoGrowGlowGameManager.Instance.LoseLife();
                }
                else
                {
                    GoGrowGlowGameManager.Instance.LoseLifeAmount(1f, false);
                }
            }

            // Play effects
            if (enterEffect != null)
            {
                GameObject effect = Instantiate(enterEffect, transform.position, Quaternion.identity);
                effect.SetActive(true);
                Destroy(effect, 3f);
            }

            if (enterSound != null && AudioHandler.Instance != null)
            {
                AudioHandler.Instance.soundEffectsSource.PlayOneShot(enterSound);
            }
        }
    }
}