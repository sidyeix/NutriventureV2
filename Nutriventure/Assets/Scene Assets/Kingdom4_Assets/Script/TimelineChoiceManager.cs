using UnityEngine;
using UnityEngine.Playables;

public class TimelineChoiceManager : MonoBehaviour
{
    public static TimelineChoiceManager Instance;

    [Header("Timelines")]
    public PlayableDirector introTimeline;
    public PlayableDirector acceptTimeline;

    void Awake()
    {
        Instance = this;
    }

    public void AcceptQuest()
    {
        StopAllTimelines();
        acceptTimeline.Play();

        Debug.Log("Accept cutscene started");
    }

    void StopAllTimelines()
    {
        if (introTimeline != null) introTimeline.Stop();
        if (acceptTimeline != null) acceptTimeline.Stop();
    }
}
