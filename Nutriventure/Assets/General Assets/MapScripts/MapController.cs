using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("Map Canvas")]
    [SerializeField] private GameObject mapCanvas;

    [Header("Player UI")]
    [SerializeField] private GameObject playerUI;  // Reference to player UI canvas/elements

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
            
            // If starting with map closed, make sure open button is interactable
            if (!startWithMapOpen && openMapButton != null)
            {
                openMapButton.interactable = true;
            }
        }

        // Initialize player UI state (opposite of map)
        if (playerUI != null)
        {
            playerUI.SetActive(!startWithMapOpen);
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

            // Hide player UI when map opens
            if (playerUI != null)
            {
                playerUI.SetActive(false);
            }

            // Disable the open button while map is open
            if (openMapButton != null)
            {
                openMapButton.interactable = false;
            }
            
            Debug.Log("Map Opened - Player UI Hidden");
        }
    }

    // Close the map
    public void CloseMap()
    {
        if (mapCanvas != null)
        {
            mapCanvas.SetActive(false);
            PlaySound(closeSound);

            // Show player UI when map closes
            if (playerUI != null)
            {
                playerUI.SetActive(true);
            }

            // Re-enable the open button when map is closed
            if (openMapButton != null)
            {
                openMapButton.interactable = true;
            }
            
            Debug.Log("Map Closed - Player UI Visible");
        }
    }

    // Hide map (alias for CloseMap)
    public void HideMap()
    {
        CloseMap();
    }

    // Show map (alias for OpenMap)
    public void ShowMap()
    {
        OpenMap();
    }

    // Toggle map open/close
    public void ToggleMap()
    {
        if (mapCanvas != null)
        {
            bool isActive = mapCanvas.activeSelf;
            mapCanvas.SetActive(!isActive);
            PlaySound(isActive ? closeSound : openSound);

            // Toggle player UI opposite of map
            if (playerUI != null)
            {
                playerUI.SetActive(isActive); // If map was active (true), now hide player UI
            }

            // Update open button interactable state
            if (openMapButton != null)
            {
                openMapButton.interactable = isActive;
            }
            
            Debug.Log($"Map Toggled: {(isActive ? "Closed" : "Opened")} - Player UI {(isActive ? "Visible" : "Hidden")}");
        }
    }

    // Check if map is currently visible
    public bool IsMapVisible()
    {
        return mapCanvas != null && mapCanvas.activeSelf;
    }

    // Set map visibility directly
    public void SetMapVisibility(bool isVisible)
    {
        if (mapCanvas != null && mapCanvas.activeSelf != isVisible)
        {
            mapCanvas.SetActive(isVisible);
            PlaySound(isVisible ? openSound : closeSound);
            
            // Set player UI opposite of map visibility
            if (playerUI != null)
            {
                playerUI.SetActive(!isVisible);
            }
            
            if (openMapButton != null)
            {
                openMapButton.interactable = !isVisible;
            }
            
            Debug.Log($"Map Visibility Set to: {(isVisible ? "Visible" : "Hidden")} - Player UI {(isVisible ? "Hidden" : "Visible")}");
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

    // Optional: Clean up listeners when object is destroyed
    private void OnDestroy()
    {
        if (openMapButton != null)
        {
            openMapButton.onClick.RemoveListener(OpenMap);
        }
        
        if (closeMapButton != null)
        {
            closeMapButton.onClick.RemoveListener(CloseMap);
        }
    }
}