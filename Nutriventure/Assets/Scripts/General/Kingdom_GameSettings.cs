using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    
    [Header("Dynamic Audio Control")]
    [SerializeField] private bool useGlobalAudioInterceptor = true;
    [SerializeField] private List<string> targetScriptNames = new List<string>() 
    { 
        "K2_CollectKey", 
        "NutriHeartCollector"
    };
    
    private AudioSource audioSource;
    private bool isPaused = false;
    private float originalTimeScale;
    private string currentSceneName;
    private float currentSFXVolume = 0.7f;
    private Coroutine countdownCoroutine;
    
    // PlayerPrefs keys
    private const string BG_MUSIC_VOLUME_KEY = "BGMusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    
    // Audio interceptor system
    private Dictionary<AudioSource, float> originalAudioSourceVolumes = new Dictionary<AudioSource, float>();
    private List<AudioSourceInterceptor> audioInterceptors = new List<AudioSourceInterceptor>();
    
    // Component to intercept AudioSource calls
    private class AudioSourceInterceptor : MonoBehaviour
    {
        private Kingdom_GameSettings settingsManager;
        private AudioSource interceptedSource;
        private float originalVolume;
        
        public void Initialize(AudioSource source, Kingdom_GameSettings manager)
        {
            interceptedSource = source;
            settingsManager = manager;
            originalVolume = source.volume;
        }
        
        public void UpdateVolume(float newVolume)
        {
            if (interceptedSource != null)
            {
                interceptedSource.volume = originalVolume * newVolume;
            }
        }
    }
    
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
            MainComponents.SetActive(true); // Main components should be enabled initially
        }
        
        if (GameSettingsPanel != null)
        {
            GameSettingsPanel.SetActive(false); // Panel should be disabled initially
        }
        
        if (SettingsBTN != null)
        {
            SettingsBTN.gameObject.SetActive(true); // Settings button should be enabled initially
        }
        
        // Initialize audio interception system
        if (useGlobalAudioInterceptor)
        {
            SetupAudioInterception();
        }
    }
    
    void SetupAudioInterception()
    {
        // Find all AudioSources in target scripts
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>(true);
        
        foreach (MonoBehaviour script in allScripts)
        {
            string scriptName = script.GetType().Name;
            
            // Check if this script is in our target list
            if (!targetScriptNames.Contains(scriptName) && !targetScriptNames.Contains("All"))
                continue;
            
            // Get all AudioSource components on or attached to this GameObject
            AudioSource[] sources = script.GetComponents<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source != null && !originalAudioSourceVolumes.ContainsKey(source))
                {
                    originalAudioSourceVolumes[source] = source.volume;
                    
                    // Create an interceptor for this AudioSource
                    AudioSourceInterceptor interceptor = source.gameObject.AddComponent<AudioSourceInterceptor>();
                    interceptor.Initialize(source, this);
                    audioInterceptors.Add(interceptor);
                    
                    // Apply current SFX volume
                    source.volume = originalAudioSourceVolumes[source] * currentSFXVolume;
                    
                    Debug.Log($"Intercepted AudioSource on {scriptName}: {source.clip?.name ?? "No clip"}");
                }
            }
            
            // Also check for AudioSource fields in the script
            FieldInfo[] fields = script.GetType().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(AudioSource))
                {
                    AudioSource fieldSource = field.GetValue(script) as AudioSource;
                    if (fieldSource != null && !originalAudioSourceVolumes.ContainsKey(fieldSource))
                    {
                        originalAudioSourceVolumes[fieldSource] = fieldSource.volume;
                        
                        // Create an interceptor for this AudioSource
                        AudioSourceInterceptor interceptor = fieldSource.gameObject.AddComponent<AudioSourceInterceptor>();
                        interceptor.Initialize(fieldSource, this);
                        audioInterceptors.Add(interceptor);
                        
                        // Apply current SFX volume
                        fieldSource.volume = originalAudioSourceVolumes[fieldSource] * currentSFXVolume;
                        
                        Debug.Log($"Intercepted AudioSource field {field.Name} on {scriptName}: {fieldSource.clip?.name ?? "No clip"}");
                    }
                }
            }
        }
        
        Debug.Log($"Audio Interception: Found and controlling {audioInterceptors.Count} AudioSources");
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
        currentSFXVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.7f);
        
        // Set slider values
        if (backgroundMusicSlider != null)
            backgroundMusicSlider.value = bgVolume;
        
        if (soundEffectsSlider != null)
            soundEffectsSlider.value = currentSFXVolume;
        
        // Apply initial volume settings
        ApplyAudioSettings();
    }
    
    void OpenSettingsPanel()
    {
        if (GameSettingsPanel != null && !GameSettingsPanel.activeSelf)
        {
            // Pause the game
            PauseGame();
            
            // Show settings panel AND Main_Components (as per your flow)
            GameSettingsPanel.SetActive(true);
            
            if (MainComponents != null)
            {
                MainComponents.SetActive(true); // Ensure Main_Components is enabled
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
        // According to your flow: Disable Main Components, keep GameSettings Panel enabled, enable PauseCDtxt
        if (MainComponents != null)
        {
            MainComponents.SetActive(false); // Disable Main_Components
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
        currentSFXVolume = value;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
        PlayerPrefs.Save();
        
        // Apply to all registered sound effect sources
        foreach (AudioSource source in soundEffectsSources)
        {
            if (source != null)
                source.volume = value;
        }
        
        // Update all intercepted AudioSources
        UpdateInterceptedAudioSources();
        
        // Update volume for our own audio source
        if (audioSource != null)
        {
            audioSource.volume = value;
        }
    }
    
    void UpdateInterceptedAudioSources()
    {
        // Update all intercepted AudioSources
        foreach (var interceptor in audioInterceptors)
        {
            if (interceptor != null)
            {
                interceptor.UpdateVolume(currentSFXVolume);
            }
        }
        
        // Also update any AudioSources we tracked
        foreach (var kvp in originalAudioSourceVolumes)
        {
            if (kvp.Key != null)
            {
                kvp.Key.volume = kvp.Value * currentSFXVolume;
            }
        }
    }
    
    void ApplyAudioSettings()
    {
        // Apply volume to all registered audio sources
        float bgVolume = backgroundMusicSlider != null ? backgroundMusicSlider.value : 0.7f;
        currentSFXVolume = soundEffectsSlider != null ? soundEffectsSlider.value : 0.7f;
        
        foreach (AudioSource source in backgroundMusicSources)
        {
            if (source != null)
                source.volume = bgVolume;
        }
        
        foreach (AudioSource source in soundEffectsSources)
        {
            if (source != null)
                source.volume = currentSFXVolume;
        }
        
        // Apply to intercepted AudioSources
        UpdateInterceptedAudioSources();
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            // Apply current SFX volume
            audioSource.PlayOneShot(clip, currentSFXVolume);
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
            source.volume = currentSFXVolume;
        }
    }
    
    // Method to manually intercept an AudioSource
    public void InterceptAudioSource(AudioSource source, MonoBehaviour ownerScript = null)
    {
        if (source == null || originalAudioSourceVolumes.ContainsKey(source)) return;
        
        originalAudioSourceVolumes[source] = source.volume;
        
        // Create interceptor
        AudioSourceInterceptor interceptor = source.gameObject.AddComponent<AudioSourceInterceptor>();
        interceptor.Initialize(source, this);
        audioInterceptors.Add(interceptor);
        
        // Apply current volume
        source.volume = originalAudioSourceVolumes[source] * currentSFXVolume;
        
        Debug.Log($"Manually intercepted AudioSource: {source.clip?.name ?? "No clip"}");
    }
    
    // Method to intercept all AudioSources on a GameObject
    public void InterceptAllAudioOnGameObject(GameObject target)
    {
        AudioSource[] sources = target.GetComponents<AudioSource>();
        foreach (AudioSource source in sources)
        {
            InterceptAudioSource(source);
        }
        
        // Also check in children
        sources = target.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource source in sources)
        {
            InterceptAudioSource(source);
        }
    }
    
    // For handling Android back button
    void Update()
    {
        // Handle Android back button
        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    if (GameSettingsPanel != null && GameSettingsPanel.activeSelf)
        //    {
        //        ResumeGame(); // This now follows your desired flow
        //    }
        //    else
        //    {
        //        OpenSettingsPanel();
        //    }
        //}
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