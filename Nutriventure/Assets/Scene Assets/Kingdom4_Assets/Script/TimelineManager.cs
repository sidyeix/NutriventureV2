using UnityEngine;
using UnityEngine.Playables;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager Instance;

    [SerializeField] private PlayableDirector peanutTimeline;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayPeanutTimeline()
    {
        if (peanutTimeline != null)
        {
            peanutTimeline.Play();
        }
    }
}
