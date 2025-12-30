using UnityEngine;
using TMPro;
using System.Collections;

public class CanvasCoordinator : MonoBehaviour
{
    public static CanvasCoordinator Instance;
    
    [Header("Canvas References")]
    public Canvas dialogueCanvas;
    public TextMeshProUGUI dialogueText;
    
    [Header("Canvas State")]
    private CanvasGroup canvasGroup;
    private Coroutine currentDisplay;
    private string currentOwner = "none";
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (dialogueCanvas != null)
        {
            canvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialogueCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
    }
    
    // Method for NPCGuardController to show messages
    public void ShowNPCNarration(string message, float displayTime)
    {
        StartCoroutine(ShowMessageSequence("npc", message, displayTime, false));
    }
    
    // Method for BookUIManager to show messages
    public void ShowBookNarration(string message, AudioClip soundClip = null)
    {
        StartCoroutine(ShowMessageSequence("book", message, 6f, true, soundClip));
    }
    
    private IEnumerator ShowMessageSequence(string owner, string message, float displayTime, bool hasAudio, AudioClip soundClip = null)
    {
        // If another owner is using the canvas, wait a moment
        if (currentOwner != "none" && currentOwner != owner)
        {
            Debug.Log($"Canvas in use by {currentOwner}, waiting...");
            yield return new WaitForSeconds(1f);
        }
        
        currentOwner = owner;
        
        // Stop any existing display
        if (currentDisplay != null)
        {
            StopCoroutine(currentDisplay);
        }
        
        currentDisplay = StartCoroutine(DisplayMessage(message, displayTime, hasAudio, soundClip));
    }
    
    private IEnumerator DisplayMessage(string message, float displayTime, bool hasAudio, AudioClip soundClip = null)
    {
        if (dialogueCanvas == null || dialogueText == null)
        {
            Debug.LogError("Canvas or Text not assigned!");
            yield break;
        }
        
        // Activate canvas
        if (!dialogueCanvas.gameObject.activeSelf)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }
        
        // Set text
        dialogueText.text = message;
        
        // Fade in
        float timer = 0f;
        while (timer < 0.5f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // Play audio if provided
        if (hasAudio && soundClip != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.PlayOneShot(soundClip);
            yield return new WaitForSeconds(Mathf.Max(soundClip.length, displayTime));
        }
        else
        {
            yield return new WaitForSeconds(displayTime);
        }
        
        // Fade out
        timer = 0f;
        while (timer < 0.5f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Clear text
        dialogueText.text = "";
        
        // Release ownership
        currentOwner = "none";
        currentDisplay = null;
    }
    
    public void ForceHideCanvas()
    {
        if (currentDisplay != null)
        {
            StopCoroutine(currentDisplay);
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        
        currentOwner = "none";
        currentDisplay = null;
    }
}