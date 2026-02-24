using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;

public class TimelineChoiceManager : MonoBehaviour
{
    public static TimelineChoiceManager Instance;

    [Header("Timelines")]
    public PlayableDirector introTimeline;
    public PlayableDirector acceptTimeline;
    
    [Header("UI References")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    
    [Header("NPC Reference")]
    [SerializeField] private WardenInteraction wardenNPC;
    
    [Header("Camera Control")]
    [SerializeField] private GameObject playerCamera; // Assign your player camera
    [SerializeField] private GameObject timelineCamera; // Assign your timeline camera
    
    [Header("Button Visibility")]
    [SerializeField] private bool hideButtonsOnStart = true;

    private bool isPausedForChoice = false;
    private bool isAccepting = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        if (wardenNPC == null)
            wardenNPC = FindObjectOfType<WardenInteraction>();
        
        SetupButtons();
        
        if (acceptTimeline != null)
        {
            acceptTimeline.stopped += OnAcceptTimelineEnded;
            acceptTimeline.played += OnAcceptTimelineStarted;
        }
        
        if (hideButtonsOnStart)
            HideChoiceButtons();
    }

    private void SetupButtons()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(AcceptQuest);
        }
        
        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(RejectQuest);
        }
    }

    // Call this from Timeline Signal to show choice buttons
    public void ShowChoiceButtons()
    {
        if (introTimeline != null && introTimeline.state == PlayState.Playing)
        {
            // Pause the timeline
            introTimeline.Pause();
            isPausedForChoice = true;
            
            // Show buttons
            if (acceptButton != null) 
            {
                acceptButton.gameObject.SetActive(true);
                acceptButton.interactable = true;
            }
            
            if (rejectButton != null) 
            {
                rejectButton.gameObject.SetActive(true);
                rejectButton.interactable = true;
            }
            
            Debug.Log("Choice buttons shown - timeline paused");
        }
    }
    
    public void HideChoiceButtons()
    {
        if (acceptButton != null) 
            acceptButton.gameObject.SetActive(false);
        
        if (rejectButton != null) 
            rejectButton.gameObject.SetActive(false);
    }

    public void AcceptQuest()
    {
        if (isAccepting) return; // Prevent double acceptance
        
        isAccepting = true;
        Debug.Log("Accept button clicked!");
        
        // IMPORTANT: Properly clean up intro timeline
        if (introTimeline != null)
        {
            if (introTimeline.state == PlayState.Playing)
            {
                introTimeline.Stop();
            }
            
            // Force evaluate to clear any lingering tracks
            introTimeline.time = 0;
            introTimeline.Evaluate();
        }
        
        HideChoiceButtons();
        
        // Reset camera control briefly before playing accept timeline
        StartCoroutine(PlayAcceptTimeline());
    }
    
    private IEnumerator PlayAcceptTimeline()
    {
        // Small delay to ensure intro timeline is completely cleared
        yield return new WaitForSeconds(0.1f);
        
        // Play accept timeline if available
        if (acceptTimeline != null)
        {
            // Make sure accept timeline starts from beginning
            acceptTimeline.time = 0;
            acceptTimeline.Play();
            
            // Notify NPC after a short delay
            StartCoroutine(DelayedQuestAcceptance());
        }
        else
        {
            if (wardenNPC != null)
                wardenNPC.OnQuestAccepted();
            isAccepting = false;
        }
        
        isPausedForChoice = false;
    }
    
    private void OnAcceptTimelineStarted(PlayableDirector director)
    {
        Debug.Log("Accept timeline started - camera should now be controlled by accept timeline");
        
        // If you have specific camera setup, do it here
        if (timelineCamera != null && playerCamera != null)
        {
            // Ensure timeline camera is active and player camera is disabled
            // This depends on how your camera system works
        }
    }
    
    private IEnumerator DelayedQuestAcceptance()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (wardenNPC != null)
        {
            wardenNPC.OnQuestAccepted();
        }
        
        // Wait a bit more before resetting acceptance flag
        yield return new WaitForSeconds(0.5f);
        isAccepting = false;
    }
    
    public void RejectQuest()
    {
        Debug.Log("Reject button clicked!");
        
        HideChoiceButtons();
        
        // IMPORTANT: Resume the intro timeline
        if (introTimeline != null && isPausedForChoice)
        {
            introTimeline.Play();
            Debug.Log("Resuming intro timeline after rejection");
        }
        
        if (wardenNPC != null)
            wardenNPC.OnQuestRejected();
        
        isPausedForChoice = false;
    }

    private void OnAcceptTimelineEnded(PlayableDirector director)
    {
        Debug.Log("Accept timeline ended - returning control to player camera");
        
        // Ensure player camera regains control
        StartCoroutine(ReturnToPlayerCamera());
    }
    
    private IEnumerator ReturnToPlayerCamera()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Force camera to player control
        // This depends on your camera system. For Cinemachine:
        // if (cinemachineBrain != null) cinemachineBrain.enabled = true;
        // if (playerCamera != null) playerCamera.SetActive(true);
        // if (timelineCamera != null) timelineCamera.SetActive(false);
        
        Debug.Log("Camera control returned to player");
    }

    void OnDestroy()
    {
        if (acceptTimeline != null)
        {
            acceptTimeline.stopped -= OnAcceptTimelineEnded;
            acceptTimeline.played -= OnAcceptTimelineStarted;
        }
        
        if (Instance == this)
            Instance = null;
    }
}