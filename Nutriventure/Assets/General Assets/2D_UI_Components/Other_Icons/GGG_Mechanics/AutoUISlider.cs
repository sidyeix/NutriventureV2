using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class AutoUISlider : MonoBehaviour
{
    [Header("References")]
    public RectTransform viewport;
    public RectTransform content;
    public Button nextButton;
    public Button prevButton;

    [Header("Prefabs")]
    public GameObject imagePanelPrefab;  // For image slides
    public GameObject videoPanelPrefab;  // For video slides
    public GameObject dotPrefab;
    public Transform dotsParent;

    [Header("Video Setup")]
    public Material greenScreenMaterial;
    public RenderTexture renderTexture;

    [Header("Data")]
    public List<SlideContent> slides = new List<SlideContent>();

    [Header("Settings")]
    public float slideDuration = 0.3f;
    public Color activeDotColor = Color.white;
    public Color inactiveDotColor = Color.gray;

    private List<RectTransform> panels = new();
    private List<CanvasGroup> panelGroups = new();
    private List<Image> dots = new();
    private List<VideoPlayer> panelVideoPlayers = new();

    private int currentIndex = 0;
    private float panelWidth;
    private bool isSliding = false;

    void Start()
    {
        // Load last slide index from PlayerPrefs (optional)
        if (PlayerPrefs.HasKey("LastSlideIndex"))
        {
            currentIndex = PlayerPrefs.GetInt("LastSlideIndex", 0);
            currentIndex = Mathf.Clamp(currentIndex, 0, slides.Count - 1);
        }

        BuildSlider();
        UpdateUI();
        InitializeAllVideos();
    }

    void BuildSlider()
    {
        if (viewport == null || content == null)
        {
            Debug.LogError("AutoUISlider: Viewport or Content is not assigned!");
            return;
        }

        panelWidth = viewport.rect.width;

        // Clear existing
        foreach (Transform child in content)
            Destroy(child.gameObject);

        if (dotsParent != null)
        {
            foreach (Transform child in dotsParent)
                Destroy(child.gameObject);
        }

        panels.Clear();
        panelGroups.Clear();
        dots.Clear();
        panelVideoPlayers.Clear();

        for (int i = 0; i < slides.Count; i++)
        {
            GameObject panelGO = null;
            SlideContent slide = slides[i];

            // Choose the correct prefab based on slide type
            if (slide.contentType == SlideContent.ContentType.Video && slide.videoClip != null && videoPanelPrefab != null)
            {
                // Use video panel prefab
                panelGO = Instantiate(videoPanelPrefab, content);
            }
            else if (imagePanelPrefab != null)
            {
                // Use image panel prefab
                panelGO = Instantiate(imagePanelPrefab, content);
            }

            if (panelGO == null)
            {
                Debug.LogWarning($"AutoUISlider: Could not create panel for slide {i}. Check prefab assignments.");
                continue;
            }

            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            if (panelRT == null)
            {
                Debug.LogWarning($"AutoUISlider: Panel {i} has no RectTransform.");
                continue;
            }

            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(0, 1);
            panelRT.pivot = new Vector2(0, 0.5f);
            panelRT.sizeDelta = new Vector2(panelWidth, 0);
            panelRT.anchoredPosition = new Vector2(i * panelWidth, 0);

            // Setup based on slide type
            if (slide.contentType == SlideContent.ContentType.Video && slide.videoClip != null)
            {
                SetupVideoPanel(panelGO, slide);
            }
            else
            {
                SetupImagePanel(panelGO, slide);
            }

            CanvasGroup cg = panelGO.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = panelGO.AddComponent<CanvasGroup>();

            cg.alpha = (i == currentIndex) ? 1f : 0f; // Show current slide based on saved index

            panels.Add(panelRT);
            panelGroups.Add(cg);

            // Create dot
            if (dotPrefab != null && dotsParent != null)
            {
                GameObject dotGO = Instantiate(dotPrefab, dotsParent);
                Image dotImg = dotGO.GetComponent<Image>();
                if (dotImg != null)
                    dots.Add(dotImg);
            }
        }

        // Resize content
        content.sizeDelta = new Vector2(panelWidth * slides.Count, content.sizeDelta.y);
        content.anchoredPosition = new Vector2(-currentIndex * panelWidth, 0); // Position at current slide
    }

    void SetupImagePanel(GameObject panelGO, SlideContent slide)
    {
        // Set up image
        Image panelImage = panelGO.GetComponentInChildren<Image>();
        if (panelImage != null)
        {
            if (slide.image != null)
            {
                panelImage.sprite = slide.image;
            }
            panelImage.maskable = true;
            panelImage.preserveAspect = true;
        }

        // No video player for image panels
        panelVideoPlayers.Add(null);
    }

    void SetupVideoPanel(GameObject panelGO, SlideContent slide)
    {
        // Get the existing VideoPlayer from prefab
        VideoPlayer videoPlayer = panelGO.GetComponentInChildren<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPanelPrefab should have a VideoPlayer component!");
            panelVideoPlayers.Add(null);
            return;
        }

        // Configure the existing VideoPlayer
        videoPlayer.playOnAwake = false; // IMPORTANT: Disable auto-play
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = slide.loopVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        if (renderTexture != null)
            videoPlayer.targetTexture = renderTexture;

        if (slide.videoClip != null)
            videoPlayer.clip = slide.videoClip;

        videoPlayer.Stop();
        panelVideoPlayers.Add(videoPlayer);

        // Get the existing RawImage (VideoDisplay) from prefab
        RawImage rawImage = panelGO.GetComponentInChildren<RawImage>();
        if (rawImage == null)
        {
            Debug.LogError("VideoPanelPrefab should have a RawImage component (VideoDisplay)!");
            return;
        }

        // ONLY configure the RawImage material and texture - DON'T TOUCH RectTransform!
        if (renderTexture != null)
            rawImage.texture = renderTexture;

        if (greenScreenMaterial != null)
            rawImage.material = greenScreenMaterial;

        rawImage.color = Color.white;
        rawImage.maskable = true;
        rawImage.raycastTarget = false;

        // Disable any Image components in the panel (except the RawImage)
        Image[] allImages = panelGO.GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            if (img.gameObject != rawImage.gameObject)
            {
                img.enabled = false;
            }
        }

        // Also check for any CanvasRenderer components on the VideoDisplay GameObject
        // and ensure they're enabled
        CanvasRenderer canvasRenderer = rawImage.GetComponent<CanvasRenderer>();
        if (canvasRenderer != null)
        {
            canvasRenderer.cull = false;
        }
    }

    void InitializeAllVideos()
    {
        // Prepare all videos immediately so they're ready to play
        for (int i = 0; i < panelVideoPlayers.Count; i++)
        {
            if (panelVideoPlayers[i] != null)
            {
                panelVideoPlayers[i].Prepare();
            }
        }

        // Play current video if it exists
        if (currentIndex < panelVideoPlayers.Count && panelVideoPlayers[currentIndex] != null)
        {
            StartCoroutine(PlayVideoWhenReady(currentIndex));
        }
    }

    IEnumerator PlayVideoWhenReady(int index)
    {
        if (index < 0 || index >= panelVideoPlayers.Count) yield break;

        VideoPlayer vp = panelVideoPlayers[index];
        if (vp == null) yield break;

        // If not prepared, prepare it
        if (!vp.isPrepared)
        {
            vp.Prepare();
            yield return new WaitUntil(() => vp.isPrepared);
        }

        // Wait a frame to ensure everything is ready
        yield return null;

        // Play the video
        vp.Play();
    }

    void StopAllVideos()
    {
        for (int i = 0; i < panelVideoPlayers.Count; i++)
        {
            if (panelVideoPlayers[i] != null && panelVideoPlayers[i].isPlaying)
            {
                panelVideoPlayers[i].Pause();
                panelVideoPlayers[i].frame = 0; // Reset to beginning
            }
        }
    }

    public void Next()
    {
        if (isSliding || currentIndex >= panels.Count - 1)
            return;

        PlayClickSound();
        StopAllVideos();
        currentIndex++;
        StartCoroutine(Slide());
    }

    public void Previous()
    {
        if (isSliding || currentIndex <= 0)
            return;

        PlayClickSound();
        StopAllVideos();
        currentIndex--;
        StartCoroutine(Slide());
    }

    IEnumerator Slide()
    {
        isSliding = true;

        Vector2 startPos = content.anchoredPosition;
        Vector2 targetPos = new Vector2(-currentIndex * panelWidth, 0);

        int previousIndex = Mathf.Clamp(
            currentIndex + (targetPos.x > startPos.x ? -1 : 1),
            0,
            panels.Count - 1
        );

        // Stop previous video completely
        if (previousIndex < panelVideoPlayers.Count && panelVideoPlayers[previousIndex] != null)
        {
            panelVideoPlayers[previousIndex].Pause();
            panelVideoPlayers[previousIndex].frame = 0;
        }

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;

            // Slide movement
            content.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

            // Fade transition
            panelGroups[currentIndex].alpha = Mathf.Lerp(0f, 1f, t);
            panelGroups[previousIndex].alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        content.anchoredPosition = targetPos;

        // Final alpha cleanup
        for (int i = 0; i < panelGroups.Count; i++)
            panelGroups[i].alpha = (i == currentIndex) ? 1f : 0f;

        // Play video for current slide
        if (currentIndex < panelVideoPlayers.Count && panelVideoPlayers[currentIndex] != null)
        {
            StartCoroutine(PlayVideoWhenReady(currentIndex));
        }

        isSliding = false;
        UpdateUI();

        // Save current slide index
        SaveCurrentSlide();
    }

    void UpdateUI()
    {
        if (prevButton != null)
            prevButton.interactable = currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentIndex < panels.Count - 1;

        for (int i = 0; i < dots.Count; i++)
            dots[i].color = (i == currentIndex) ? activeDotColor : inactiveDotColor;
    }

    void PlayClickSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
        }
    }

    public void JumpToSlide(int slideIndex)
    {
        if (isSliding) return;

        // Clamp the index to valid range
        slideIndex = Mathf.Clamp(slideIndex, 0, slides.Count - 1);

        // If already on this slide, do nothing
        if (slideIndex == currentIndex) return;

        // Stop all videos
        StopAllVideos();

        // Set the current index
        currentIndex = slideIndex;

        // Immediately position content to show the target slide
        content.anchoredPosition = new Vector2(-currentIndex * panelWidth, 0);

        // Update all alphas (only target slide visible)
        for (int i = 0; i < panelGroups.Count; i++)
        {
            panelGroups[i].alpha = (i == currentIndex) ? 1f : 0f;
        }

        // Play video for current slide if it exists
        if (currentIndex < panelVideoPlayers.Count && panelVideoPlayers[currentIndex] != null)
        {
            StartCoroutine(PlayVideoWhenReady(currentIndex));
        }

        // Update UI (dots, buttons)
        UpdateUI();

        // Save current slide index
        SaveCurrentSlide();
    }

    // Public method to get current slide index
    public int GetCurrentSlideIndex()
    {
        return currentIndex;
    }

    // Public method to get total slide count
    public int GetSlideCount()
    {
        return slides.Count;
    }

    // Save current slide index to PlayerPrefs
    void SaveCurrentSlide()
    {
        PlayerPrefs.SetInt("LastSlideIndex", currentIndex);
        PlayerPrefs.Save();
    }

    // Clean up videos when the slider is disabled
    void OnDisable()
    {
        StopAllVideos();
        SaveCurrentSlide(); // Save when closing
    }

    void OnDestroy()
    {
        StopAllVideos();
        SaveCurrentSlide(); // Save when destroyed
    }
}