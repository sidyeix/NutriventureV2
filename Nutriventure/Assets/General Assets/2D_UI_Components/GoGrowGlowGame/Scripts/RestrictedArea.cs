using UnityEngine;

public class RestrictedArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GoGrowGlowGameManager.Instance != null)
            {
                GoGrowGlowGameManager.Instance.LoseLife();
            }
        }
    }
}