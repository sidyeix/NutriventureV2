using UnityEngine;
using UnityEngine.Playables;

public class SimpleTimelineTrigger : MonoBehaviour
{
    public PlayableDirector playableDirector;
    public bool playOnlyOnce = true;

    private bool hasPlayed = false;

    void Start()
    {
        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnlyOnce && hasPlayed) return;

        hasPlayed = true;
        playableDirector.Play();
    }
}
