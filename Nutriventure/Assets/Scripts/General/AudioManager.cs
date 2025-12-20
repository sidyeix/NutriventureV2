using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Background Music")]
    public AudioSource backgroundMusic;

    [Header("Playback Settings")]
    public bool startWithMusic = true; // Set to true to have music on at start
    public float startTime = 0f; // Time in seconds to start playing from (e.g., 3.5)
    public bool loop = true; // Whether to loop the music

    private bool isMusicMuted = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Initialize music state
        if (backgroundMusic != null)
        {
            // Set loop setting
            backgroundMusic.loop = loop;

            if (startWithMusic)
            {
                UnmuteMusic();
                PlayMusicFromTime(startTime);
            }
            else
            {
                MuteMusic();
            }
        }
        else
        {
            Debug.LogWarning("Background music AudioSource not assigned!");
        }
    }

    // Play music from specific time
    public void PlayMusicFromTime(float timeInSeconds)
    {
        if (backgroundMusic != null && backgroundMusic.clip != null)
        {
            // Ensure time is within clip bounds
            timeInSeconds = Mathf.Clamp(timeInSeconds, 0f, backgroundMusic.clip.length - 0.1f);

            // Set the time and play
            backgroundMusic.time = timeInSeconds;

            if (!backgroundMusic.isPlaying && !isMusicMuted)
            {
                backgroundMusic.Play();
            }

            Debug.Log($"Playing music from {timeInSeconds:F2}s");
        }
    }

    // Reset to start time and play
    public void RestartMusic()
    {
        PlayMusicFromTime(startTime);
    }

    // Toggle music on/off
    public void ToggleMusic()
    {
        if (isMusicMuted)
        {
            UnmuteMusic();
        }
        else
        {
            MuteMusic();
        }
    }

    // Mute the background music
    public void MuteMusic()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.mute = true;
            isMusicMuted = true;
            Debug.Log("Background music muted");
        }
    }

    // Unmute the background music
    public void UnmuteMusic()
    {
        if (backgroundMusic != null)
        {
            backgroundMusic.mute = false;
            isMusicMuted = false;
            Debug.Log("Background music unmuted");

            // Make sure music is playing from the right time
            if (!backgroundMusic.isPlaying)
            {
                PlayMusicFromTime(backgroundMusic.time);
            }
        }
    }

    // Check if music is currently muted
    public bool IsMusicMuted()
    {
        return isMusicMuted;
    }

    // Set a new music clip and play it from start time
    public void ChangeMusic(AudioClip newMusicClip, bool autoPlay = true)
    {
        if (backgroundMusic != null && newMusicClip != null)
        {
            backgroundMusic.clip = newMusicClip;
            backgroundMusic.loop = loop;

            if (autoPlay && !isMusicMuted)
            {
                PlayMusicFromTime(startTime);
            }
        }
    }

    // Get current playback time
    public float GetCurrentTime()
    {
        if (backgroundMusic != null && backgroundMusic.isPlaying)
        {
            return backgroundMusic.time;
        }
        return 0f;
    }

    // Get total clip length
    public float GetClipLength()
    {
        if (backgroundMusic != null && backgroundMusic.clip != null)
        {
            return backgroundMusic.clip.length;
        }
        return 0f;
    }
}