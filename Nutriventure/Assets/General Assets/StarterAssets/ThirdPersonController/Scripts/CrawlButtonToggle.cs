using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using StarterAssets;

public class CrawlButtonToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public ThirdPersonController playerController;

    [Header("Color Settings")]
    [Tooltip("Color when crouch is active (match this to Sprint button's pressed color for uniformity)")]
    public Color activeColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    public Color normalColor = Color.white;

    private Button button;
    private Image buttonImage;
    private bool isCrawling = false;

    void Start()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        if (playerController == null)
        {
            playerController = FindObjectOfType<ThirdPersonController>();
        }

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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonImage != null)
            buttonImage.color = activeColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        UpdateButtonAppearance();
    }

    void UpdateButtonAppearance()
    {
        if (buttonImage != null)
        {
            buttonImage.color = isCrawling ? activeColor : normalColor;
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