using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Kingdom_GameSettings : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject GameSettingsPanel;
    [SerializeField] private GameObject MainComponents;
    [SerializeField] private Button SettingsBTN;
    [SerializeField] private Button ResumeBTN;
    [SerializeField] private Button RestartBTN;
    [SerializeField] private TMP_Text PauseCDtxt;
    
    [Header("Audio Settings")]
    [SerializeField] private Slider backgroundMusicSlider;
    [SerializeField] private Slider soundEffectsSlider;
    
    [Header("Audio Sources")]
    [SerializeField] private List<AudioSource> backgroundMusicSources;
    [SerializeField] private List<AudioSource> soundEffectsSources;
    
    [Header("Countdown Settings")]
    [SerializeField] private AudioClip countdownTickSound;
    
    private AudioSource audioSource;
    private bool isPaused = false;
    private float originalTimeScale;
    private string currentSceneName;
    private Coroutine countdownCoroutine;
    
    // PlayerPrefs keys
    private const string BG_MUSIC_VOLUME_KEY = "BGMusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    void Start()
    {
        // Get current scene name
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // Setup AudioSource for countdown sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        
        // Setup button listeners
        SetupButtonListeners();
        
        // Initialize audio settings
        InitializeAudioSettings();
        
        // Ensure all UI elements are in correct initial state
        if (PauseCDtxt != null)
        {
            PauseCDtxt.gameObject.SetActive(false);
        }
        
        if (MainComponents != null)
        {
            MainComponents.SetActive(true);
        }
        
        if (GameSettingsPanel != null)
        {
            GameSettingsPanel.SetActive(false);
        }
        
        if (SettingsBTN != null)
        {
            SettingsBTN.gameObject.SetActive(true);
        }
    }
    
    void SetupButtonListeners()
    {
        // Settings Button
        if (SettingsBTN != null)
        {
            SettingsBTN.onClick.RemoveAllListeners();
            SettingsBTN.onClick.AddListener(OpenSettingsPanel);
        }
        
        // Resume Button
        if (ResumeBTN != null)
        {
            ResumeBTN.onClick.RemoveAllListeners();
            ResumeBTN.onClick.AddListener(ResumeGame);
        }
        
        // Restart Button
        if (RestartBTN != null)
        {
            RestartBTN.onClick.RemoveAllListeners();
            RestartBTN.onClick.AddListener(RestartGame);
        }
        
        // Audio Sliders
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.onValueChanged.RemoveAllListeners();
            backgroundMusicSlider.onValueChanged.AddListener(OnBackgroundMusicChanged);
        }
        
        if (soundEffectsSlider != null)
        {
            soundEffectsSlider.onValueChanged.RemoveAllListeners();
            soundEffectsSlider.onValueChanged.AddListener(OnSoundEffectsChanged);
        }
    }
    
    void InitializeAudioSettings()
    {
        // Load saved volume preferences
        float bgVolume = PlayerPrefs.GetFloat(BG_MUSIC_VOLUME_KEY, 0.7f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.7f);
        
        // Set slider values
        if (backgroundMusicSlider != null)
            backgroundMusicSlider.value = bgVolume;
        
        if (soundEffectsSlider != null)
            soundEffectsSlider.value = sfxVolume;
        
        // Apply initial volume settings
        ApplyAudioSettings();
    }
    
    void OpenSettingsPanel()
    {
        if (GameSettingsPanel != null && !GameSettingsPanel.activeSelf)
        {
            // Pause the game
            PauseGame();
            
            // Show settings panel
            GameSettingsPanel.SetActive(true);
            
            if (MainComponents != null)
            {
                MainComponents.SetActive(true);
            }
            
            // Hide settings button while panel is open
            if (SettingsBTN != null)
                SettingsBTN.gameObject.SetActive(false);
        }
    }
    
    void PauseGame()
    {
        if (!isPaused)
        {
            isPaused = true;
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }
    
    void ResumeGame()
    {
        if (MainComponents != null)
        {
            MainComponents.SetActive(false);
        }
        
        // Start countdown before resuming
        StartCountdownCoroutine();
    }
    
    void StartCountdownCoroutine()
    {
        // Stop any existing countdown coroutine
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        
        countdownCoroutine = StartCoroutine(CountdownBeforeResume());
    }
    
    IEnumerator CountdownBeforeResume()
    {
        // Stop any existing audio from playing
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Show countdown text
        if (PauseCDtxt != null)
        {
            PauseCDtxt.gameObject.SetActive(true);
            
            // Countdown from 3
            PauseCDtxt.text = "3";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);
            
            PauseCDtxt.text = "2";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);
            
            PauseCDtxt.text = "1";
            PlaySound(countdownTickSound);
            yield return new WaitForSecondsRealtime(1f);
            
            PauseCDtxt.text = "GAME!";
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Stop any playing audio immediately
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Hide countdown text
            PauseCDtxt.gameObject.SetActive(false);
        }
        
        // Hide the GameSettingsPanel AFTER countdown completes
        if (GameSettingsPanel != null)
        {
            GameSettingsPanel.SetActive(false);
        }
        
        // Re-enable Main_Components after countdown
        if (MainComponents != null)
        {
            MainComponents.SetActive(true);
        }
        
        // Resume game
        Time.timeScale = originalTimeScale;
        isPaused = false;
        
        // Show settings button again
        if (SettingsBTN != null)
            SettingsBTN.gameObject.SetActive(true);
    }
    
    void RestartGame()
    {
        // Ensure all UI is in correct state before restart
        if (GameSettingsPanel != null)
            GameSettingsPanel.SetActive(false);
        
        if (PauseCDtxt != null)
            PauseCDtxt.gameObject.SetActive(false);
        
        if (MainComponents != null)
            MainComponents.SetActive(true);
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Resume time immediately before restarting
        Time.timeScale = 1f;
        
        // Reload current scene
        SceneManager.LoadScene(currentSceneName);
    }
    
    void OnBackgroundMusicChanged(float value)
    {
        // Save preference
        PlayerPrefs.SetFloat(BG_MUSIC_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // Apply to all background music sources
        foreach (AudioSource source in backgroundMusicSources)
        {
            if (source != null)
                source.volume = value;
        }
    }
    
    void OnSoundEffectsChanged(float value)
    {
        // Save preference
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // Apply to all registered sound effect sources
        foreach (AudioSource source in soundEffectsSources)
        {
            if (source != null)
                source.volume = value;
        }
        
        // Update volume for our own audio source
        if (audioSource != null)
        {
            audioSource.volume = value;
        }
    }
    
    void ApplyAudioSettings()
    {
        // Apply volume to all registered audio sources
        float bgVolume = backgroundMusicSlider != null ? backgroundMusicSlider.value : 0.7f;
        float sfxVolume = soundEffectsSlider != null ? soundEffectsSlider.value : 0.7f;
        
        foreach (AudioSource source in backgroundMusicSources)
        {
            if (source != null)
                source.volume = bgVolume;
        }
        
        foreach (AudioSource source in soundEffectsSources)
        {
            if (source != null)
                source.volume = sfxVolume;
        }
        
        // Update our own audio source
        if (audioSource != null)
        {
            audioSource.volume = sfxVolume;
        }
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            float sfxVolume = soundEffectsSlider != null ? soundEffectsSlider.value : 0.7f;
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    // Public methods to manually add audio sources
    public void AddBackgroundMusicSource(AudioSource source)
    {
        if (source != null && !backgroundMusicSources.Contains(source))
        {
            backgroundMusicSources.Add(source);
            // Apply current volume setting
            if (backgroundMusicSlider != null)
                source.volume = backgroundMusicSlider.value;
        }
    }
    
    public void AddSoundEffectSource(AudioSource source)
    {
        if (source != null && !soundEffectsSources.Contains(source))
        {
            soundEffectsSources.Add(source);
            // Apply current volume setting
            float sfxVolume = soundEffectsSlider != null ? soundEffectsSlider.value : 0.7f;
            source.volume = sfxVolume;
        }
    }
    
    // Clean up when disabled
    void OnDisable()
    {
        // Save all preferences
        PlayerPrefs.Save();
        
        // Stop any playing audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}