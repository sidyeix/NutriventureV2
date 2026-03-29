using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Kingdom 4 version of SimpleRespawnTrigger.
/// Same respawn + warning logic, but also deducts 1 life via AllergenGameManager.
/// </summary>
public class K4_SimpleRespawnTrigger : MonoBehaviour
{
  [Header("Respawn Settings")]
  [SerializeField] private Transform respawnPoint;

  [Header("Warning Canvas")]
  [SerializeField] private CanvasGroup warningCanvas;
  [SerializeField] private float respawnDelay = 0.5f;
  [SerializeField] private float warningDuration = 2f;
  [SerializeField] private float fadeDuration = 0.5f;

  [Header("Damage")]
  [SerializeField] private float damageAmount = 1f;

  [Header("Settings")]
  [SerializeField] private string playerTag = "Player";
  [SerializeField] private bool resetVelocity = true;

  private void Start()
  {
    if (warningCanvas != null)
    {
      warningCanvas.alpha = 0f;
      warningCanvas.gameObject.SetActive(false);
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag(playerTag))
      StartCoroutine(ShowWarningAndThenRespawn(other.gameObject));
  }

  private void OnTriggerEnter2D(Collider2D other)
  {
    if (other.CompareTag(playerTag))
      StartCoroutine(ShowWarningAndThenRespawn(other.gameObject));
  }

  private IEnumerator ShowWarningAndThenRespawn(GameObject player)
  {
    // Deduct life
    if (AllergenGameManager.Instance != null)
      AllergenGameManager.Instance.TakeDamage(damageAmount);

    if (warningCanvas != null)
    {
      warningCanvas.gameObject.SetActive(true);
      float timer = 0f;
      while (timer < fadeDuration)
      {
        timer += Time.deltaTime;
        warningCanvas.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
        yield return null;
      }
      warningCanvas.alpha = 1f;

      yield return new WaitForSeconds(respawnDelay);

      RespawnPlayer(player);

      float remainingWarningTime = warningDuration - respawnDelay;
      if (remainingWarningTime > 0)
        yield return new WaitForSeconds(remainingWarningTime);

      timer = 0f;
      while (timer < fadeDuration)
      {
        timer += Time.deltaTime;
        warningCanvas.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        yield return null;
      }
      warningCanvas.alpha = 0f;
      warningCanvas.gameObject.SetActive(false);
    }
    else
    {
      yield return new WaitForSeconds(respawnDelay);
      RespawnPlayer(player);
    }
  }

  private void RespawnPlayer(GameObject player)
  {
    if (respawnPoint == null)
    {
      Debug.LogError("K4_SimpleRespawnTrigger: No respawn point assigned!");
      return;
    }

    if (resetVelocity)
    {
      Rigidbody rb = player.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
      }

      Rigidbody2D rb2D = player.GetComponent<Rigidbody2D>();
      if (rb2D != null)
      {
        rb2D.linearVelocity = Vector2.zero;
        rb2D.angularVelocity = 0f;
      }
    }

    player.transform.position = respawnPoint.position;
    player.transform.rotation = respawnPoint.rotation;
  }
}
