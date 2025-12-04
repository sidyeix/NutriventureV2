using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class K2_SubtitlePlayableAsset : PlayableAsset
{
    public string subtitleText;
    public float typingSpeed = 0.03f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<SubtitlePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.subtitleText = subtitleText;
        behaviour.typingSpeed = typingSpeed;

        return playable;
    }
}
