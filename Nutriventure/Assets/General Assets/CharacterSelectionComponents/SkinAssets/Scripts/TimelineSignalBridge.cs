using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelineSignalBridge : MonoBehaviour
{
    public PlayableDirector director;
    public CharacterVisualSwapper visualSwapper;
    public EnvironmentController environmentController;

    private int pendingSkinID = -1;

    // Called from SkinSelectionController
    public void PlayTimelineForSkin(TimelineAsset timeline, int skinID)
    {
        if (timeline == null)
        {
            Debug.Log("No timeline provided - applying skin immediately with main environment");
            ApplySkinImmediately(skinID);
            return;
        }

        pendingSkinID = skinID;

        // Switch to skin environment BEFORE playing timeline
        if (environmentController != null)
        {
            environmentController.SwitchToSkinEnvironment();
        }


        director.playableAsset = timeline;
        director.Play();
    }

    // Called by timeline signal - UPDATED
    public void ExecuteSkinSwap()
    {
        if (visualSwapper != null && pendingSkinID != -1)
        {
            // Use the new timeline-specific method
            //visualSwapper.ApplySkinViaTimeline(pendingSkinID);
        }

        // Reset pending values
        pendingSkinID = -1;
    }

    // Called by UI buttons (back/select/character)
    public void StopTimelineAndReturn()
    {
        // Stop timeline if playing
        if (director.state == PlayState.Playing)
        {
            director.Stop();
        }

        // ALWAYS switch back to main environment when timeline stops
        if (environmentController != null)
        {
            environmentController.SwitchToMainEnvironment();
        }

        // Reset character position
        if (visualSwapper != null)
        {
            //visualSwapper.ResetCharacterPosition();
        }

        // Reset
        pendingSkinID = -1;
    }

    // Helper for immediate skin application (for skins without timelines)
    private void ApplySkinImmediately(int skinID)
    {
        // Ensure we're in main environment first
        if (environmentController != null)
        {
            environmentController.SwitchToMainEnvironment();
        }

        // Reset character position
        if (visualSwapper != null)
        {
            //visualSwapper.ResetCharacterPosition();
        }

        // Then apply the skin
        if (visualSwapper != null)
        {
            visualSwapper.ApplySkinToCurrentCharacter(skinID);
        }
    }
}