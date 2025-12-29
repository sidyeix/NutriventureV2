using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleRespawnTrigger : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint; // Drag and drop your spawn point GameObject here

    [Header("Warning Canvas")]
    [SerializeField] private CanvasGroup warningCanvas; // Drag and drop your warning UI CanvasGroup here
    [SerializeField] private float respawnDelay = 0.5f; // Time before respawning after trigger
    [SerializeField] private float warningDuration = 2f; // How long to show the warning
    [SerializeField] private float fadeDuration = 0.5f; // Fade in/out time

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool resetVelocity = true;

    private void Start()
    {
        // Hide warning canvas on start
        if (warningCanvas != null)
        {
            warningCanvas.alpha = 0f;
            warningCanvas.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            ShowWarningAndRespawn(other.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            ShowWarningAndRespawn(other.gameObject);
        }
    }

    private void ShowWarningAndRespawn(GameObject player)
    {
        // Show warning first
        StartCoroutine(ShowWarningAndThenRespawn(player));
    }

    private IEnumerator ShowWarningAndThenRespawn(GameObject player)
    {
        // Show warning
        if (warningCanvas != null)
        {
            // Fade in
            warningCanvas.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                warningCanvas.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            warningCanvas.alpha = 1f;

            // Wait for respawn delay (0.5 seconds)
            yield return new WaitForSeconds(respawnDelay);

            // Now respawn the player immediately (warning stays visible)
            RespawnPlayer(player);

            // Wait remaining time for warning to stay visible
            float remainingWarningTime = warningDuration - respawnDelay;
            if (remainingWarningTime > 0)
            {
                yield return new WaitForSeconds(remainingWarningTime);
            }

            // Fade out
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
            // If no warning canvas, just wait and respawn
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer(player);
        }
    }

    private void RespawnPlayer(GameObject player)
    {
        if (respawnPoint == null)
        {
            Debug.LogError("SimpleRespawnTrigger: No respawn point assigned!");
            return;
        }

        // Reset velocity if needed
        if (resetVelocity)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero; // Changed back to .velocity (linearVelocity is for Rigidbody in Unity 6+)
                rb.angularVelocity = Vector3.zero;
            }

            Rigidbody2D rb2D = player.GetComponent<Rigidbody2D>();
            if (rb2D != null)
            {
                rb2D.linearVelocity = Vector2.zero; // Changed back to .velocity
                rb2D.angularVelocity = 0f;
            }
        }

        // Move player to respawn point
        player.transform.position = respawnPoint.position;
        player.transform.rotation = respawnPoint.rotation;

        Debug.Log($"Player respawned to: {respawnPoint.position}");
    }

    // Draw gizmos for visualization
    private void OnDrawGizmos()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }

        if (respawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, respawnPoint.position);
            Gizmos.DrawWireSphere(respawnPoint.position, 0.5f);
        }
    }
}