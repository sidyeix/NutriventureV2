using UnityEngine;
using System.Collections;

public class AudioManagerBattleField : MonoBehaviour
{
    [Header("Battle Audio Sources")]
    public AudioSource battleMusicSource;
    public AudioSource audienceSFXSource;

    [Header("Battle Audio Clips")]
    public AudioClip battleBackgroundMusic;
    public AudioClip audienceSFX;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float battleMusicVolume = 0.7f;
    [Range(0f, 1f)]
    public float audienceSFXVolume = 0.5f;
    public bool playOnBattleStart = true;
    public float fadeDuration = 1f;

    // Singleton instance
    private static AudioManagerBattleField instance;
    public static AudioManagerBattleField Instance => instance;

    // State
    private bool isBattleActive = false;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeAudioSources();
    }

    void InitializeAudioSources()
    {
        // Ensure we have audio sources
        if (battleMusicSource == null)
        {
            battleMusicSource = gameObject.AddComponent<AudioSource>();
            battleMusicSource.name = "BattleMusicSource";
        }

        if (audienceSFXSource == null)
        {
            audienceSFXSource = gameObject.AddComponent<AudioSource>();
            audienceSFXSource.name = "AudienceSFXSource";
        }

        // Configure battle music source
        battleMusicSource.loop = true;
        battleMusicSource.volume = 0f; // Start at 0, will fade in
        battleMusicSource.spatialBlend = 0f; // 2D audio
        battleMusicSource.playOnAwake = false;

        // Configure audience SFX source
        audienceSFXSource.loop = true;
        audienceSFXSource.volume = 0f; // Start at 0, will fade in
        audienceSFXSource.spatialBlend = 0f; // 2D audio
        audienceSFXSource.playOnAwake = false;
    }

    public void StartBattleAudio()
    {
        if (isBattleActive) return;

        isBattleActive = true;

        // Set clips
        if (battleBackgroundMusic != null && battleMusicSource != null)
        {
            battleMusicSource.clip = battleBackgroundMusic;
        }

        if (audienceSFX != null && audienceSFXSource != null)
        {
            audienceSFXSource.clip = audienceSFX;
        }

        // Play and fade in when possible, otherwise start immediately.
        if (CanRunCoroutines())
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            fadeCoroutine = StartCoroutine(FadeInBattleAudio());
        }
        else
        {
            StartBattleAudioImmediate();
        }
    }

    IEnumerator FadeInBattleAudio()
    {
        // Start playing
        if (battleMusicSource != null && battleMusicSource.clip != null)
        {
            battleMusicSource.Play();
        }

        if (audienceSFXSource != null && audienceSFXSource.clip != null)
        {
            audienceSFXSource.Play();
        }

        // Fade in
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;

            if (battleMusicSource != null)
            {
                battleMusicSource.volume = Mathf.Lerp(0f, battleMusicVolume, t);
            }

            if (audienceSFXSource != null)
            {
                audienceSFXSource.volume = Mathf.Lerp(0f, audienceSFXVolume, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final volume
        if (battleMusicSource != null)
        {
            battleMusicSource.volume = battleMusicVolume;
        }

        if (audienceSFXSource != null)
        {
            audienceSFXSource.volume = audienceSFXVolume;
        }

        fadeCoroutine = null;
        Debug.Log("Battle audio started and faded in");
    }

    public void StopBattleAudio()
    {
        if (!isBattleActive) return;

        isBattleActive = false;

        // Fade out when possible; during teardown/inactive states stop immediately.
        if (CanRunCoroutines())
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            fadeCoroutine = StartCoroutine(FadeOutBattleAudio());
        }
        else
        {
            StopBattleAudioImmediate();
        }
    }

    IEnumerator FadeOutBattleAudio()
    {
        float startBattleVolume = battleMusicSource != null ? battleMusicSource.volume : 0f;
        float startAudienceVolume = audienceSFXSource != null ? audienceSFXSource.volume : 0f;

        // Fade out
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            float t = elapsedTime / fadeDuration;

            if (battleMusicSource != null)
            {
                battleMusicSource.volume = Mathf.Lerp(startBattleVolume, 0f, t);
            }

            if (audienceSFXSource != null)
            {
                audienceSFXSource.volume = Mathf.Lerp(startAudienceVolume, 0f, t);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final volume is 0
        if (battleMusicSource != null)
        {
            battleMusicSource.volume = 0f;
            battleMusicSource.Stop();
        }

        if (audienceSFXSource != null)
        {
            audienceSFXSource.volume = 0f;
            audienceSFXSource.Stop();
        }

        fadeCoroutine = null;
        Debug.Log("Battle audio stopped and faded out");
    }

    private bool CanRunCoroutines()
    {
        return gameObject.activeInHierarchy && isActiveAndEnabled;
    }

    private void StartBattleAudioImmediate()
    {
        if (battleMusicSource != null && battleMusicSource.clip != null)
        {
            battleMusicSource.Play();
            battleMusicSource.volume = battleMusicVolume;
        }

        if (audienceSFXSource != null && audienceSFXSource.clip != null)
        {
            audienceSFXSource.Play();
            audienceSFXSource.volume = audienceSFXVolume;
        }
    }

    private void StopBattleAudioImmediate()
    {
        if (battleMusicSource != null)
        {
            battleMusicSource.volume = 0f;
            battleMusicSource.Stop();
        }

        if (audienceSFXSource != null)
        {
            audienceSFXSource.volume = 0f;
            audienceSFXSource.Stop();
        }
    }

    public void PauseBattleAudio()
    {
        if (battleMusicSource != null && battleMusicSource.isPlaying)
        {
            battleMusicSource.Pause();
        }

        if (audienceSFXSource != null && audienceSFXSource.isPlaying)
        {
            audienceSFXSource.Pause();
        }

        Debug.Log("Battle audio paused");
    }

    public void ResumeBattleAudio()
    {
        if (battleMusicSource != null && !battleMusicSource.isPlaying && battleMusicSource.time > 0)
        {
            battleMusicSource.UnPause();
        }

        if (audienceSFXSource != null && !audienceSFXSource.isPlaying && audienceSFXSource.time > 0)
        {
            audienceSFXSource.UnPause();
        }

        Debug.Log("Battle audio resumed");
    }

    public void SetBattleMusicVolume(float volume)
    {
        battleMusicVolume = Mathf.Clamp01(volume);
        if (battleMusicSource != null && isBattleActive)
        {
            battleMusicSource.volume = battleMusicVolume;
        }
    }

    public void SetAudienceSFXVolume(float volume)
    {
        audienceSFXVolume = Mathf.Clamp01(volume);
        if (audienceSFXSource != null && isBattleActive)
        {
            audienceSFXSource.volume = audienceSFXVolume;
        }
    }

    public bool IsBattleAudioPlaying()
    {
        return isBattleActive;
    }

    void OnDestroy()
    {
        // Cleanup
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}