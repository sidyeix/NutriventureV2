using UnityEngine;
using System.Collections;

public class AudioHandler : MonoBehaviour
{
    public static AudioHandler Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource soundEffectsSource;

    [Header("Audio Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip buttonClickSound;

    [Header("Chest Sound Effects - Optional")]
    public AudioClip chestOpenSound;
    public AudioClip rewardPopSound;
    public AudioClip claimSound;
    public AudioClip goldBallFlySound;

    private float musicVolume = 1f;
    private float soundVolume = 1f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        // Load saved volume settings
        if (GameDataManager.Instance != null)
        {
            musicVolume = GameDataManager.Instance.CurrentGameData.musicVolume;
            soundVolume = GameDataManager.Instance.CurrentGameData.soundVolume;
        }

        ApplyVolumeSettings();

        // Play main menu music if not already playing
        if (musicSource.clip != mainMenuMusic || !musicSource.isPlaying)
        {
            PlayMainMenuMusic();
        }
    }

    public void PlayMainMenuMusic()
    {
        if (mainMenuMusic != null)
        {
            musicSource.clip = mainMenuMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound != null)
        {
            soundEffectsSource.PlayOneShot(buttonClickSound);
        }
    }

    public void PlayCharacterSelectionSound(AudioClip clip)
    {
        if (clip != null)
        {
            soundEffectsSource.PlayOneShot(clip);
        }
    }

    // ===== SAFE CHEST SOUND METHODS =====
    // These won't cause errors even if clips are not assigned

    public void PlayChestOpen()
    {
        if (chestOpenSound != null && soundEffectsSource != null)
        {
            soundEffectsSource.PlayOneShot(chestOpenSound);
        }
        // No error if chestOpenSound is null - just silent
    }

    public void PlayRewardPop()
    {
        if (rewardPopSound != null && soundEffectsSource != null)
        {
            soundEffectsSource.PlayOneShot(rewardPopSound);
        }
        // No error if rewardPopSound is null - just silent
    }

    public void PlayClaimSound()
    {
        if (claimSound != null && soundEffectsSource != null)
        {
            soundEffectsSource.PlayOneShot(claimSound);
        }
        // No error if claimSound is null - just silent
    }

    public void PlayGoldBallFly()
    {
        if (goldBallFlySound != null && soundEffectsSource != null)
        {
            soundEffectsSource.PlayOneShot(goldBallFlySound);
        }
        // No error if goldBallFlySound is null - just silent
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();

        // Save to GameData
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.musicVolume = musicVolume;
            GameDataManager.Instance.SaveGameData();
        }
    }

    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();

        // Save to GameData
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CurrentGameData.soundVolume = soundVolume;
            GameDataManager.Instance.SaveGameData();
        }
    }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (soundEffectsSource != null)
            soundEffectsSource.volume = soundVolume;
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSoundVolume()
    {
        return soundVolume;
    }

    // Stop all audio (for scene transitions)
    public void StopAllAudio()
    {
        if (musicSource != null)
            musicSource.Stop();
        if (soundEffectsSource != null)
            soundEffectsSource.Stop();
    }
}