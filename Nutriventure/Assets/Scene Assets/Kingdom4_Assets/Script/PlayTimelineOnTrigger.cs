using UnityEngine;
using UnityEngine.Playables;

public class PlayTimelineOnTrigger : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        playableDirector.Play();
        hasPlayed = true;
    }
}
