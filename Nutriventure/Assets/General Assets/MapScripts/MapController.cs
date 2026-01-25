using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("Map Canvas")]
    [SerializeField] private GameObject mapCanvas;

    [Header("Map Buttons")]
    [SerializeField] private Button openMapButton;    // Icon button to open map
    [SerializeField] private Button closeMapButton;   // Close button on the map itself

    [Header("Optional Settings")]
    [SerializeField] private bool startWithMapOpen = false;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        // Initialize map state
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(startWithMapOpen);
        }

        // Setup open map button
        if (openMapButton != null)
        {
            openMapButton.onClick.AddListener(OpenMap);
        }
        else
        {
            Debug.LogWarning("Open Map Button not assigned in MapController!");
        }

        // Setup close map button (usually on the map canvas itself)
        if (closeMapButton != null)
        {
            closeMapButton.onClick.AddListener(CloseMap);
        }
        else
        {
            Debug.LogWarning("Close Map Button not assigned in MapController!");
        }

        // Get AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (openSound != null || closeSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    // Open the map
    public void OpenMap()
    {
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(true);
            PlaySound(openSound);

            // Optional: Disable the open button while map is open
            if (openMapButton != null)
            {
                openMapButton.interactable = false;
            }
        }
    }

    // Close the map
    public void CloseMap()
    {
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
            PlaySound(closeSound);

            // Re-enable the open button when map is closed
            if (openMapButton != null)
            {
                openMapButton.interactable = true;
            }
        }
    }

    // Toggle map open/close (optional method if you want both functions)
    public void ToggleMap()
    {
        if (mapCanvas != null)
        {
            bool isActive = mapCanvas.activeSelf;
            mapCanvas.SetActive(!isActive);

            PlaySound(isActive ? closeSound : openSound);

            // Update open button interactable state
            if (openMapButton != null)
            {
                openMapButton.interactable = isActive;
            }
        }
    }

    // Play sound if available
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}