using UnityEngine;
using UnityEngine.Playables;

public class SubtitlePlayableBehaviour : PlayableBehaviour
{
    public string subtitleText;
    public float typingSpeed;
    private K2_SubtitleController controller;

    public override void OnGraphStart(Playable playable)
    {
        // includeInactive:true ensures we find the controller even if its
        // canvas starts disabled and is enabled just before timeline playback
        controller = GameObject.FindObjectOfType<K2_SubtitleController>(true);
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        // Re-query in case the controller wasn't active during OnGraphStart
        if (controller == null)
            controller = GameObject.FindObjectOfType<K2_SubtitleController>(true);
        
        if (controller != null)
            controller.ShowSubtitle(subtitleText, typingSpeed);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (controller != null)
            controller.ClearSubtitle();
    }
}
