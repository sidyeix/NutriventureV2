using UnityEngine;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject talkButtonUI; // The actual GameObject
    [SerializeField] private CanvasGroup talkButtonCanvas;
    [SerializeField] private RectTransform talkButtonRect;
    [SerializeField] private Button talkButton;
    
    [Header("Timeline")]
    [SerializeField] private PlayableDirector timelineDirector;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float slideDistance = 50f;
    private bool isTimelinePlaying = false;

    private bool isPlayerInRange = false;
    private bool isUIVisible = false;
    private Vector2 originalPosition;
    private Coroutine animationCoroutine;
    
    private void Start()
    {
        // Make sure collider is trigger
        GetComponent<Collider>().isTrigger = true;
        
        // Deactivate UI completely at start
        if (talkButtonUI != null)
        {
            talkButtonUI.SetActive(false);
        }
        
        // Store original position for animation
        if (talkButtonRect != null)
        {
            originalPosition = talkButtonRect.anchoredPosition;
        }
        
        // Setup button click
        if (talkButton != null)
        {
            talkButton.onClick.AddListener(StartTimeline);
        }
    }
    
    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player") && !isTimelinePlaying)
    {
        isPlayerInRange = true;
        ShowTalkButton();
    }
}

    
    private void OnTriggerExit(Collider other)
{
    if (other.CompareTag("Player"))
    {
        isPlayerInRange = false;
        HideTalkButton();
    }
}

    
    private void ShowTalkButton()
    {
        if (isUIVisible) return;
        
        // Activate the UI GameObject first
        if (talkButtonUI != null)
        {
            talkButtonUI.SetActive(true);
        }
        
        // Set initial state (invisible and moved down)
        if (talkButtonCanvas != null)
        {
            talkButtonCanvas.alpha = 0f;
            talkButtonCanvas.blocksRaycasts = false;
            talkButtonCanvas.interactable = false;
        }
        
        if (talkButtonRect != null)
        {
            talkButtonRect.anchoredPosition = originalPosition + Vector2.down * slideDistance;
        }
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateButton(true));
    }
    
    private void HideTalkButton()
    {
        if (!isUIVisible) return;
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateButton(false));
    }
    
    private IEnumerator AnimateButton(bool show)
    {
        isUIVisible = show;
        
        // Enable/disable button interaction
        if (talkButtonCanvas != null)
        {
            talkButtonCanvas.blocksRaycasts = show;
            talkButtonCanvas.interactable = show;
        }
        
        float startAlpha = talkButtonCanvas != null ? talkButtonCanvas.alpha : 0f;
        float targetAlpha = show ? 1f : 0f;
        
        Vector2 startPos = talkButtonRect != null ? talkButtonRect.anchoredPosition : Vector2.zero;
        Vector2 targetPos = show ? originalPosition : 
            originalPosition + Vector2.down * slideDistance;
        
        float time = 0f;
        
        while (time < animationDuration)
        {
            time += Time.deltaTime;
            float t = time / animationDuration;
            
            // Fade
            if (talkButtonCanvas != null)
                talkButtonCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            // Slide
            if (talkButtonRect != null)
                talkButtonRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            
            yield return null;
        }
        
        // Set final values
        if (talkButtonCanvas != null)
            talkButtonCanvas.alpha = targetAlpha;
        
        if (talkButtonRect != null)
            talkButtonRect.anchoredPosition = targetPos;
        
        // Deactivate GameObject when hidden
        if (!show && talkButtonUI != null)
        {
            talkButtonUI.SetActive(false);
        }
        
        animationCoroutine = null;
    }
    
    private void StartTimeline()
{
    if (timelineDirector != null)
    {
        isTimelinePlaying = true;

        // Force-hide UI immediately
        HideTalkButton();

        timelineDirector.Play();
        StartCoroutine(WaitForTimelineEnd());
    }
    else
    {
        Debug.LogError("No Timeline Director assigned!");
    }
}

    
    private IEnumerator WaitForTimelineEnd()
{
    yield return null;

    while (timelineDirector.state == PlayState.Playing)
    {
        yield return null;
    }

    isTimelinePlaying = false;

    // Only show again if player is still nearby
    if (isPlayerInRange)
    {
        ShowTalkButton();
    }
}

}