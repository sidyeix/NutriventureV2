using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

public class CrawlButtonToggle : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonController playerController;

    private Button button;
    private bool isCrawling = false;

    void Start()
    {
        // Get the button component
        button = GetComponent<Button>();

        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<ThirdPersonController>();
        }

        // Set up button click
        if (button != null && playerController != null)
        {
            button.onClick.AddListener(ToggleCrawl);
            Debug.Log("Crawl button setup complete");
        }
        else
        {
            Debug.LogError("Crawl button setup failed - missing components");
        }
    }

    void Update()
    {
        // Update button state based on player's actual crawl state
        if (playerController != null)
        {
            bool currentCrawlState = playerController.IsCrawling();
            if (currentCrawlState != isCrawling)
            {
                isCrawling = currentCrawlState;
                UpdateButtonAppearance();
            }
        }
    }

    void ToggleCrawl()
    {
        if (playerController != null)
        {
            playerController.ToggleCrawl();
            isCrawling = playerController.IsCrawling();
            UpdateButtonAppearance();
            Debug.Log("Crawl toggled via UI. New state: " + isCrawling);
        }
    }

    void UpdateButtonAppearance()
    {
        // Optional: Change button color or text based on crawl state
        if (button != null)
        {
            var colors = button.colors;
            colors.normalColor = isCrawling ? Color.red : Color.white;
            colors.selectedColor = isCrawling ? Color.red : Color.white;
            button.colors = colors;
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleCrawl);
        }
    }
}