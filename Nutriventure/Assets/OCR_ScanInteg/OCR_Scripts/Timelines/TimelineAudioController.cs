using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections;
using System.Linq;

public class TimelineAudioController : MonoBehaviour
{
    [Header("Timeline Reference")]
    public PlayableDirector playableDirector;

    [Header("Settings")]
    public bool stopAudioOnAwake = true;
    public bool stopAudioOnStart = true;
    public bool muteAudioTracks = true;

    void Awake()
    {
        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();

        if (stopAudioOnAwake)
        {
            StopTimelineAudioImmediately();
        }
    }

    void Start()
    {
        if (stopAudioOnStart)
        {
            StopTimelineAudioImmediately();
        }

        // Optional: Delay to ensure everything is initialized
        StartCoroutine(EnsureAudioStopped());
    }

    void OnEnable()
    {
        // Stop audio when object becomes active
        StopTimelineAudioImmediately();
    }

    public void StopTimelineAudioImmediately()
    {
        if (playableDirector == null)
        {
            Debug.LogWarning("PlayableDirector is null!");
            return;
        }

        Debug.Log($"Stopping audio for timeline: {playableDirector.gameObject.name}");

        // 1. Stop the PlayableDirector
        playableDirector.Stop();

        // 2. Set time to 0
        playableDirector.time = 0;

        // 3. Evaluate the timeline to apply the stop
        playableDirector.Evaluate();

        // 4. Manually stop all audio from timeline tracks
        StopAllTimelineAudioTracks();

        // 5. Disable auto-rebasing (this can cause audio to play)
        playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
    }

    private void StopAllTimelineAudioTracks()
    {
        if (playableDirector.playableAsset == null)
            return;

        TimelineAsset timeline = playableDirector.playableAsset as TimelineAsset;
        if (timeline == null)
            return;

        // Get all audio tracks
        var audioTracks = timeline.GetOutputTracks()
            .Where(track => track is AudioTrack)
            .Cast<AudioTrack>();

        foreach (AudioTrack audioTrack in audioTracks)
        {
            // Get the binding for this track
            AudioSource audioSource = playableDirector.GetGenericBinding(audioTrack) as AudioSource;

            if (audioSource != null)
            {
                // Stop and reset the audio source
                audioSource.Stop();
                audioSource.time = 0;

                // Mute if enabled
                if (muteAudioTracks)
                {
                    audioSource.mute = true;
                }

                Debug.Log($"Stopped AudioSource: {audioSource.gameObject.name} from track: {audioTrack.name}");
            }

            // Also disable the track itself
            audioTrack.muted = true;
        }
    }

    private IEnumerator EnsureAudioStopped()
    {
        // Wait for one frame to ensure everything is initialized
        yield return null;

        // Double-check audio is stopped
        StopTimelineAudioImmediately();

        // Additional safety: Find all AudioSources in scene and stop them
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);
        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.time = 0;
            }
        }
    }

    // Call this when you actually want to play the timeline
    public void PlayTimelineWithAudio()
    {
        if (playableDirector == null)
            return;

        // Re-enable auto-rebasing
        playableDirector.timeUpdateMode = DirectorUpdateMode.GameTime;

        // Unmute audio tracks if needed
        if (playableDirector.playableAsset is TimelineAsset timeline)
        {
            var audioTracks = timeline.GetOutputTracks()
                .Where(track => track is AudioTrack)
                .Cast<AudioTrack>();

            foreach (AudioTrack audioTrack in audioTracks)
            {
                audioTrack.muted = false;

                AudioSource audioSource = playableDirector.GetGenericBinding(audioTrack) as AudioSource;
                if (audioSource != null)
                {
                    audioSource.mute = false;
                }
            }
        }

        // Play the timeline
        playableDirector.Play();
    }

    // Editor helper to quickly stop audio
#if UNITY_EDITOR
    [ContextMenu("Stop Timeline Audio Now")]
    private void EditorStopAudio()
    {
        StopTimelineAudioImmediately();
    }
#endif
}