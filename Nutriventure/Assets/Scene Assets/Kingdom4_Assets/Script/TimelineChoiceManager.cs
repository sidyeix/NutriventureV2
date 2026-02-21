// TimelineChoiceManager.cs (SIMPLIFIED)
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

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

    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        if (wardenNPC == null)
        {
            wardenNPC = FindObjectOfType<WardenInteraction>();
        }
        
        // Setup accept button
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(AcceptQuest);
        }
        
        // Setup reject button
        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(RejectQuest);
        }
    }

    public void AcceptQuest()
    {
        Debug.Log("Accept button clicked!");
        
        StopAllTimelines();
        
        if (acceptTimeline != null)
        {
            acceptTimeline.Play();
        }
        
        if (wardenNPC != null)
        {
            wardenNPC.OnQuestAccepted();
        }
    }
    
    public void RejectQuest()
    {
        Debug.Log("Reject button clicked!");
        
        StopAllTimelines();
        
        if (wardenNPC != null)
        {
            wardenNPC.OnQuestRejected();
        }
    }

    void StopAllTimelines()
    {
        if (introTimeline != null && introTimeline.state == PlayState.Playing) 
            introTimeline.Stop();
        if (acceptTimeline != null && acceptTimeline.state == PlayState.Playing) 
            acceptTimeline.Stop();
    }
}