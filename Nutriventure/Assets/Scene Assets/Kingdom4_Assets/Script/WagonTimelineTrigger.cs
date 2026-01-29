using UnityEngine;
using UnityEngine.Playables;

public class WagonTimelineTrigger : MonoBehaviour
{
    public PlayableDirector playableDirector;
    private bool played = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !played)
        {
            played = true;

            if (playableDirector != null)
                playableDirector.Play();
        }
    }
}
