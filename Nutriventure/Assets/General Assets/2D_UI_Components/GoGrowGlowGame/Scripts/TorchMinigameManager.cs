using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables; // Add this namespace

public class TorchMinigameManager : MonoBehaviour
{
    public static TorchMinigameManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject trackerPanel; // The panel that slides in
    [SerializeField] private TMP_Text trackerText; // "0/8 Torches" text
    [SerializeField] private Transform plusOneSpawnPoint; // Where to spawn "+1" text
    [SerializeField] private GameObject plusOnePrefab; // "+1" text prefab

    [Header("Animation Settings")]
    [SerializeField] private float panelSlideDuration = 0.8f;
    [SerializeField] private float panelSlideDistance = 400f;
    [SerializeField] private float panelShowDelay = 0.2f; // Reduced delay for smoother entry
    [SerializeField] private float plusOneDuration = 1.5f;
    [SerializeField] private float plusOneFadeDuration = 0.5f;
    [SerializeField] private float plusOneFloatHeight = 50f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip panelSlideInSound;
    [SerializeField] private AudioClip panelSlideOutSound;
    [SerializeField] private AudioClip plusOneSound;
    [SerializeField] private AudioClip completeSound;
    [SerializeField] private float panelSlideSoundDelay = 0.1f; // Delay before playing sound

    [Header("Tracking Settings")]
    [SerializeField] private int totalTorches = 8;
    [SerializeField] private string trackerFormat = "{0}/{1} Torches";

    [Header("Trigger Settings")]
    [SerializeField] private BoxCollider trackerTrigger; // Trigger in 3D world
    [SerializeField] private bool showTrackerOnStart = false;
    [SerializeField] private bool hideOnExit = false;

    [Header("Timeline Settings")]
    [SerializeField] private PlayableDirector playableDirector; // Reference to Playable Director
    [SerializeField] private PlayableAsset timelineToPlay; // The Timeline asset you assign in Inspector
    [SerializeField] private bool playTimelineOnCompletion = true;
    [SerializeField] private float timelineDelay = 2f; // Delay before playing timeline

    private List<TorchMinigame> allTorches = new List<TorchMinigame>();
    private int litTorchesCount = 0;
    private bool isTrackerVisible = false;
    private bool hasBeenTriggered = false;
    private Vector3 trackerPanelHiddenPosition;
    private Vector3 trackerPanelVisiblePosition;
    private Coroutine panelSlideCoroutine;
    private AudioSource audioSource;
    private bool hasCompleted = false; // Track if minigame is already complete

    // Game state tracking for pausing
    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        #if UNITY_EDITOR
        Debug.Log("=== TORCH MINIGAME MANAGER START ===");
        #endif

        InitializeTracker();

        if (showTrackerOnStart)
        {
            ShowTrackerPanel();
        }

        #if UNITY_EDITOR
        Debug.Log($"Total torches to track: {totalTorches}");
        #endif

        // Validate Timeline Settings
        if (playableDirector == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Playable Director not assigned. Timeline won't play on completion.");
            #endif
        }
        if (timelineToPlay == null && playTimelineOnCompletion)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Timeline asset not assigned. Please assign a timeline in the Inspector if you want it to play on completion.");
            #endif
        }
    }

    private void InitializeTracker()
    {
        // Set up panel positions - NOW FROM LEFT SIDE
        if (trackerPanel != null)
        {
            // Panel starts OFF-SCREEN to the LEFT
            trackerPanelHiddenPosition = trackerPanel.transform.localPosition - new Vector3(panelSlideDistance, 0, 0);
            trackerPanelVisiblePosition = trackerPanel.transform.localPosition;

            // Start hidden (off-screen to the left)
            trackerPanel.transform.localPosition = trackerPanelHiddenPosition;
            trackerPanel.SetActive(false);

            #if UNITY_EDITOR
            Debug.Log("Tracker panel initialized - starting hidden on LEFT side");
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogError("Tracker Panel is not assigned!");
            #endif
        }

        // Initialize text
        UpdateTrackerText();

        // Set up trigger
        if (trackerTrigger != null && !trackerTrigger.isTrigger)
        {
            trackerTrigger.isTrigger = true;
            #if UNITY_EDITOR
            Debug.Log("Tracker trigger set to isTrigger = true");
            #endif
        }
    }

    // Called by individual torches to register themselves
    public void RegisterTorch(TorchMinigame torch)
    {
        if (!allTorches.Contains(torch))
        {
            allTorches.Add(torch);
            #if UNITY_EDITOR
            Debug.Log($"Registered torch: {torch.GetTorchID()}");
            #endif

            // If torch is already lit (from save), update count
            if (torch.IsLit())
            {
                litTorchesCount++;
                UpdateTrackerText();
            }
        }
    }

    // Called when a torch is successfully lit
    public void TorchLit(TorchMinigame torch)
    {
        if (!torch.IsLit() || hasCompleted) return;

        litTorchesCount++;
        #if UNITY_EDITOR
        Debug.Log($"Torch lit! Total: {litTorchesCount}/{totalTorches}");
        #endif

        // Update UI
        UpdateTrackerText();

        // Show +1 effect
        ShowPlusOneEffect();

        // Play sound
        PlaySound(plusOneSound);

        // Check if all torches are lit
        if (litTorchesCount >= totalTorches)
        {
            AllTorchesLit();
        }
    }

    private void UpdateTrackerText()
    {
        if (trackerText != null)
        {
            trackerText.text = string.Format(trackerFormat, litTorchesCount, totalTorches);
        }
    }

    private void ShowPlusOneEffect()
    {
        if (plusOnePrefab == null || plusOneSpawnPoint == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("PlusOne prefab or spawn point not assigned!");
            #endif
            return;
        }

        GameObject plusOneObj = Instantiate(plusOnePrefab, plusOneSpawnPoint.position, Quaternion.identity, plusOneSpawnPoint);

        // Ensure it's visible above other UI
        Canvas canvas = plusOneObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = plusOneObj.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100; // High sorting order
        }

        StartCoroutine(AnimatePlusOne(plusOneObj));
    }

    private IEnumerator AnimatePlusOne(GameObject plusOneObj)
    {
        TMP_Text textComponent = plusOneObj.GetComponent<TMP_Text>();
        if (textComponent == null) yield break;

        Color originalColor = textComponent.color;
        Vector3 originalPosition = plusOneObj.transform.localPosition;
        float elapsedTime = 0f;

        // Float up and fade out
        while (elapsedTime < plusOneDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / plusOneDuration;

            // Float up with easing
            float yOffset = Mathf.Lerp(0, plusOneFloatHeight, Mathf.Sin(progress * Mathf.PI * 0.5f));
            plusOneObj.transform.localPosition = originalPosition + new Vector3(0, yOffset, 0);

            // Fade out in the last part
            if (progress > (1 - (plusOneFadeDuration / plusOneDuration)))
            {
                float fadeProgress = (progress - (1 - (plusOneFadeDuration / plusOneDuration))) / (plusOneFadeDuration / plusOneDuration);
                textComponent.color = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0), fadeProgress);
            }

            // Slight scale effect
            float scale = 1 + Mathf.Sin(progress * Mathf.PI) * 0.1f;
            plusOneObj.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(plusOneObj);
    }

    // Trigger to show tracker panel (one-time pass-through)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            #if UNITY_EDITOR
            Debug.Log("Player passed through tracker trigger");
            #endif

            // Only trigger once unless we want re-triggering
            if (!hasBeenTriggered || hideOnExit)
            {
                ShowTrackerPanel();
                hasBeenTriggered = true;
            }
        }
    }

    // Optional exit trigger to hide panel
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && hideOnExit)
        {
            #if UNITY_EDITOR
            Debug.Log("Player left tracker trigger area - hiding panel");
            #endif
            HideTrackerPanel();
        }
    }

    public void ShowTrackerPanel()
    {
        if (isTrackerVisible || trackerPanel == null) return;

        #if UNITY_EDITOR
        Debug.Log("Showing tracker panel (sliding from LEFT)");
        #endif

        isTrackerVisible = true;
        trackerPanel.SetActive(true);

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(true));
    }

    public void HideTrackerPanel()
    {
        if (!isTrackerVisible || trackerPanel == null) return;

        #if UNITY_EDITOR
        Debug.Log("Hiding tracker panel (sliding to LEFT)");
        #endif

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(false));

        // Start coroutine to disable panel after slide
        StartCoroutine(DisablePanelAfterSlide());
    }

    private IEnumerator SlidePanel(bool slideIn)
    {
        if (trackerPanel == null) yield break;

        Vector3 startPos = trackerPanel.transform.localPosition;
        Vector3 targetPos = slideIn ? trackerPanelVisiblePosition : trackerPanelHiddenPosition;
        float elapsedTime = 0f;

        // Play sound with slight delay for better timing
        if (slideIn && panelSlideInSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideInSound, panelSlideSoundDelay));
        }
        else if (!slideIn && panelSlideOutSound != null)
        {
            StartCoroutine(PlaySoundDelayed(panelSlideOutSound, panelSlideSoundDelay));
        }

        // Add slight delay when showing (for anticipation)
        if (slideIn)
        {
            yield return CoroutineYieldCache.WaitForSeconds(panelShowDelay);
        }

        while (elapsedTime < panelSlideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panelSlideDuration;

            // Use easing for smoother animation
            if (slideIn)
            {
                // Ease out for slide in (starts fast, ends slow)
                t = 1 - Mathf.Pow(1 - t, 3); // Cubic ease out
            }
            else
            {
                // Ease in for slide out (starts slow, ends fast)
                t = Mathf.Pow(t, 3); // Cubic ease in
            }

            trackerPanel.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        trackerPanel.transform.localPosition = targetPos;
        panelSlideCoroutine = null;
    }

    private IEnumerator DisablePanelAfterSlide()
    {
        yield return CoroutineYieldCache.WaitForSeconds(panelSlideDuration + 0.1f);
        trackerPanel.SetActive(false);
        isTrackerVisible = false;
    }

    private void AllTorchesLit()
    {
        if (hasCompleted) return; // Prevent multiple triggers

        #if UNITY_EDITOR
        Debug.Log("=== ALL TORCHES ARE LIT! ===");
        #endif

        hasCompleted = true;

        // Play complete sound
        PlaySound(completeSound);

        // Update UI
        if (trackerText != null)
        {
            trackerText.text = "COMPLETE!";
            StartCoroutine(FlashCompleteText());
        }

        // Optional: Trigger game event
        if (GoGrowGlowGameManager.Instance != null)
        {
            GoGrowGlowGameManager.Instance.AddPoints(500); // Bonus points
        }

        // Play timeline if enabled
        if (playTimelineOnCompletion)
        {
            StartCoroutine(PlayTimelineAfterDelay());
        }
    }

    // Coroutine to play timeline after delay
    private IEnumerator PlayTimelineAfterDelay()
    {
        #if UNITY_EDITOR
        Debug.Log($"Waiting {timelineDelay} seconds before playing timeline...");
        #endif
        yield return CoroutineYieldCache.WaitForSeconds(timelineDelay);

        #if UNITY_EDITOR
        Debug.Log("Playing timeline...");
        #endif

        // Check if game is active
        bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

        if (isGameActive)
        {
            // Save current state before pausing
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

            // Pause both energy decrease and game timer
            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();

            isGameStatePaused = true;

            #if UNITY_EDITOR
            Debug.Log($"Torch Minigame: Game state paused. Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
            #endif
        }

        if (playableDirector != null && timelineToPlay != null)
        {
            // Subscribe to timeline stopped event to resume game state
            playableDirector.stopped += OnTimelineStopped;

            // Assign the timeline to the Playable Director and play it
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Play();

            #if UNITY_EDITOR
            Debug.Log($"Playing timeline: {timelineToPlay.name}");
            #endif
        }
        else if (playableDirector == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Playable Director not assigned. Cannot play timeline.");
            #endif
        }
        else if (timelineToPlay == null)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Timeline asset not assigned. Please assign a timeline in the Inspector.");
            #endif
        }

        // Optional: Hide tracker panel when timeline starts
        HideTrackerPanel();
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Only handle our own director
        if (director != playableDirector) return;

        #if UNITY_EDITOR
        Debug.Log($"Torch Minigame: Timeline stopped. Director: {director.name}");
        #endif

        // Unsubscribe from the event
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }

        // Resume game state if we paused it
        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }
    }

    private void ResumeGameState()
    {
        if (GoGrowGlowGameManager.Instance != null)
        {
            // Resume timer if it wasn't paused before
            if (!wasTimerPaused)
            {
                GoGrowGlowGameManager.Instance.ResumeGameTimer();
            }

            // Resume energy if it wasn't paused before
            if (!wasEnergyPaused)
            {
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();
            }

            #if UNITY_EDITOR
            Debug.Log($"Torch Minigame: Game state resumed. Timer resumed: {!wasTimerPaused}, Energy resumed: {!wasEnergyPaused}");
            #endif
        }

        isGameStatePaused = false;
    }

    // Manual method to play timeline
    public void PlayTimeline()
    {
        // Check if game is active
        bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

        if (isGameActive)
        {
            // Save current state before pausing
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

            // Pause both energy decrease and game timer
            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();

            isGameStatePaused = true;

            #if UNITY_EDITOR
            Debug.Log($"Torch Minigame: Game state paused (manual). Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
            #endif
        }

        if (playableDirector != null && timelineToPlay != null)
        {
            // Subscribe to timeline stopped event to resume game state
            playableDirector.stopped += OnTimelineStopped;

            #if UNITY_EDITOR
            Debug.Log($"Manually playing timeline: {timelineToPlay.name}");
            #endif
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Play();
        }
        else
        {
            #if UNITY_EDITOR
            Debug.LogWarning("Cannot play timeline: Playable Director or Timeline asset not assigned.");
            #endif
        }
    }

    // Method to stop timeline
    public void StopTimeline()
    {
        if (playableDirector != null && playableDirector.state == PlayState.Playing)
        {
            #if UNITY_EDITOR
            Debug.Log("Stopping timeline");
            #endif
            playableDirector.Stop();

            // Resume game state if it was paused
            if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
            {
                ResumeGameState();
            }
        }
    }

    // Method to restart timeline
    public void RestartTimeline()
    {
        if (playableDirector != null && timelineToPlay != null)
        {
            #if UNITY_EDITOR
            Debug.Log("Restarting timeline");
            #endif
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Stop();
            playableDirector.time = 0;

            // Check if game is active
            bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

            if (isGameActive)
            {
                // Save current state before pausing
                wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
                wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

                // Pause both energy decrease and game timer
                GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
                GoGrowGlowGameManager.Instance.PauseGameTimer();

                isGameStatePaused = true;

                #if UNITY_EDITOR
                Debug.Log($"Torch Minigame: Game state paused (restart). Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
                #endif
            }

            // Subscribe to timeline stopped event
            playableDirector.stopped += OnTimelineStopped;

            playableDirector.Play();
        }
    }

    private IEnumerator FlashCompleteText()
    {
        if (trackerText == null) yield break;

        Color originalColor = trackerText.color;
        float flashDuration = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.PingPong(elapsedTime * 3f, 1f); // Faster ping pong
            trackerText.color = Color.Lerp(originalColor, Color.yellow, t);

            // Add slight scale effect
            float scale = 1 + Mathf.Sin(elapsedTime * 5f) * 0.05f;
            trackerText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        trackerText.color = originalColor;
        trackerText.transform.localScale = Vector3.one;
    }

    // Audio helper methods
    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return CoroutineYieldCache.WaitForSeconds(delay);
        PlaySound(clip);
    }

    // Manual toggle method
    public void ToggleTrackerPanel()
    {
        if (isTrackerVisible)
        {
            HideTrackerPanel();
        }
        else
        {
            ShowTrackerPanel();
        }
    }

    // Reset trigger (if you want to allow re-triggering)
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
        #if UNITY_EDITOR
        Debug.Log("Tracker trigger reset");
        #endif
    }

    // Reset minigame completion state
    public void ResetMinigame()
    {
        hasCompleted = false;
        litTorchesCount = 0;
        UpdateTrackerText();
        #if UNITY_EDITOR
        Debug.Log("Minigame reset");
        #endif
    }

    // Force update tracker (useful for debugging)
    public void ForceUpdateTracker()
    {
        UpdateTrackerText();
        #if UNITY_EDITOR
        Debug.Log($"Force updated tracker: {litTorchesCount}/{totalTorches}");
        #endif
    }

    // Set Playable Director at runtime
    public void SetPlayableDirector(PlayableDirector director)
    {
        playableDirector = director;
        #if UNITY_EDITOR
        Debug.Log($"Playable Director set to: {(director != null ? director.name : "null")}");
        #endif
    }

    // Set Timeline asset at runtime
    public void SetTimelineToPlay(PlayableAsset timelineAsset)
    {
        timelineToPlay = timelineAsset;
        #if UNITY_EDITOR
        Debug.Log($"Timeline asset set to: {(timelineAsset != null ? timelineAsset.name : "null")}");
        #endif
    }

    // Set timeline delay at runtime
    public void SetTimelineDelay(float delay)
    {
        timelineDelay = Mathf.Max(0f, delay);
        #if UNITY_EDITOR
        Debug.Log($"Timeline delay set to: {timelineDelay} seconds");
        #endif
    }

    // Enable/disable timeline playback
    public void SetPlayTimelineOnCompletion(bool enabled)
    {
        playTimelineOnCompletion = enabled;
        #if UNITY_EDITOR
        Debug.Log($"Timeline playback on completion: {enabled}");
        #endif
    }

    // Public getters
    public int GetLitTorchesCount() => litTorchesCount;
    public int GetTotalTorches() => totalTorches;
    public bool AreAllTorchesLit() => litTorchesCount >= totalTorches;
    public bool IsTrackerVisible() => isTrackerVisible;
    public bool HasCompleted() => hasCompleted;
    public PlayableDirector GetPlayableDirector() => playableDirector;
    public PlayableAsset GetTimelineToPlay() => timelineToPlay;
    public bool IsTimelinePlaying() => playableDirector != null && playableDirector.state == PlayState.Playing;

    // For saving/loading game state
    public void SetLitTorchesCount(int count)
    {
        litTorchesCount = Mathf.Clamp(count, 0, totalTorches);
        UpdateTrackerText();
    }

    public List<string> GetLitTorchIDs()
    {
        List<string> litIDs = new List<string>();
        foreach (TorchMinigame torch in allTorches)
        {
            if (torch.IsLit())
            {
                litIDs.Add(torch.GetTorchID());
            }
        }
        return litIDs;
    }

    public void RestoreTorchStates(List<string> litTorchIDs)
    {
        foreach (TorchMinigame torch in allTorches)
        {
            torch.SetLit(litTorchIDs.Contains(torch.GetTorchID()));
        }

        // Recalculate count
        litTorchesCount = 0;
        foreach (TorchMinigame torch in allTorches)
        {
            if (torch.IsLit()) litTorchesCount++;
        }

        UpdateTrackerText();
        #if UNITY_EDITOR
        Debug.Log($"Restored torch states: {litTorchesCount}/{totalTorches} lit");
        #endif

        // Check if already completed
        if (litTorchesCount >= totalTorches)
        {
            hasCompleted = true;
        }
    }

    public void ResetAllTorches()
    {
        #if UNITY_EDITOR
        Debug.Log("=== RESETTING ALL TORCHES ===");
        #endif

        // Reset manager state
        hasCompleted = false;
        litTorchesCount = 0;
        UpdateTrackerText();

        // Reset individual torches
        foreach (TorchMinigame torch in allTorches)
        {
            if (torch != null)
            {
                torch.ResetTorch();
            }
        }

        // Reset trigger
        ResetTrigger();

        // Hide tracker panel
        HideTrackerPanel();

        #if UNITY_EDITOR
        Debug.Log($"All torches reset. Total torches: {allTorches.Count}");
        #endif
    }

    public void CompleteMinigameReset()
    {
        ResetAllTorches();

        // Also reset any timeline state
        if (playableDirector != null && playableDirector.state == PlayState.Playing)
        {
            playableDirector.Stop();
            #if UNITY_EDITOR
            Debug.Log("Stopped playing timeline");
            #endif
        }

        // Make sure game state is resumed
        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }

        #if UNITY_EDITOR
        Debug.Log("Minigame completely reset to initial state");
        #endif
    }


    private void OnDestroy()
    {
        // Unsubscribe from timeline events
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }

        // Make sure we resume if we're destroyed while timeline is playing
        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }
    }
}
