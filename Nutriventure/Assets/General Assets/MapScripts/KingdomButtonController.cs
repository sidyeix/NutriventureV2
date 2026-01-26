using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KingdomButtonController : MonoBehaviour
{
    [Header("Kingdom Configuration")]
    [SerializeField] private string sceneToLoad;

    [Header("UI References")]
    [SerializeField] private Button kingdomButton;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    private void Start()
    {
        // Get button reference if not set
        if (kingdomButton == null)
            kingdomButton = GetComponent<Button>();

        // Add click listener
        if (kingdomButton != null)
        {
            kingdomButton.onClick.AddListener(OnKingdomButtonClick);
        }

        // Get AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Hide loading indicator initially
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }

    private void OnKingdomButtonClick()
    {
        // Play sound if available
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Show loading indicator
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
        }

        // Load the scene
        LoadKingdomScene();
    }

    public void LoadKingdomScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"Scene name not set for {gameObject.name}!");

            // Hide loading indicator if there was an error
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }
        }
    }
}