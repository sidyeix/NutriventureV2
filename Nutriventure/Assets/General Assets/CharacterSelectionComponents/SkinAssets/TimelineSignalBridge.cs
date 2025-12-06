using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineSignalBridge : MonoBehaviour
{
    public PlayableDirector director;
    public CharacterVisualSwapper visualSwapper;

    private int pendingSkinID = -1;

    // Called when skin button is clicked
    public void PlayTimelineForSkin(TimelineAsset timeline, int skinID)
    {
        if (timeline == null)
        {
            Debug.Log("No timeline for this skin, swapping immediately");
            if (visualSwapper != null)
                visualSwapper.ApplySkinToCurrentCharacter(skinID);
            return;
        }

        // Store the skin ID
        pendingSkinID = skinID;

        // Play the timeline
        director.playableAsset = timeline;
        director.Play();
    }

    // This is called by Timeline Signal Emitter
    public void ExecuteSkinSwap()
    {
        Debug.Log($"Timeline signal: Swap to skin {pendingSkinID}");

        if (visualSwapper != null && pendingSkinID != -1)
        {
            visualSwapper.ApplySkinToCurrentCharacter(pendingSkinID);
        }
    }
}