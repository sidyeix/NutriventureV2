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
        // This will automatically hide all other skin environments first
        if (environmentController != null)
        {
            environmentController.SwitchToSkinEnvironment(); // Uses default (first environment)
        }

        director.playableAsset = timeline;
        director.Play();
    }

    // Called by timeline signal
    public void ExecuteSkinSwap()
    {
        if (visualSwapper != null && pendingSkinID != -1)
        {
            visualSwapper.ApplySkinToCurrentCharacter(pendingSkinID);
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
        // This will automatically hide all skin environments
        if (environmentController != null)
        {
            environmentController.SwitchToMainEnvironment();
        }

        // Reset
        pendingSkinID = -1;
    }

    // Helper for immediate skin application (for skins without timelines)
    private void ApplySkinImmediately(int skinID)
    {
        // Ensure we're in main environment first
        // This will automatically hide all skin environments
        if (environmentController != null)
        {
            environmentController.SwitchToMainEnvironment();
        }

        // Then apply the skin
        if (visualSwapper != null)
        {
            visualSwapper.ApplySkinToCurrentCharacter(skinID);
        }
    }
}