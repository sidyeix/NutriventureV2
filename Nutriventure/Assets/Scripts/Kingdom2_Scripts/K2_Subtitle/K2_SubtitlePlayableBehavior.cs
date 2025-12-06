using UnityEngine;
using UnityEngine.Playables;

public class SubtitlePlayableBehaviour : PlayableBehaviour
{
    public string subtitleText;
    public float typingSpeed;
    private K2_SubtitleController controller;

    public override void OnGraphStart(Playable playable)
    {
        controller = GameObject.FindObjectOfType<K2_SubtitleController>();
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (controller != null)
            controller.ShowSubtitle(subtitleText, typingSpeed);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (controller != null)
            controller.ClearSubtitle();
    }
}
