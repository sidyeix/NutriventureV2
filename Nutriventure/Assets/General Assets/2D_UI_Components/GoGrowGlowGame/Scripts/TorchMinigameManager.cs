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
        Debug.Log("=== TORCH MINIGAME MANAGER START ===");

        InitializeTracker();

        if (showTrackerOnStart)
        {
            ShowTrackerPanel();
        }

        Debug.Log($"Total torches to track: {totalTorches}");

        // Validate Timeline Settings
        if (playableDirector == null)
        {
            Debug.LogWarning("Playable Director not assigned. Timeline won't play on completion.");
        }
        if (timelineToPlay == null && playTimelineOnCompletion)
        {
            Debug.LogWarning("Timeline asset not assigned. Please assign a timeline in the Inspector if you want it to play on completion.");
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

            Debug.Log("Tracker panel initialized - starting hidden on LEFT side");
        }
        else
        {
            Debug.LogError("Tracker Panel is not assigned!");
        }

        // Initialize text
        UpdateTrackerText();

        // Set up trigger
        if (trackerTrigger != null && !trackerTrigger.isTrigger)
        {
            trackerTrigger.isTrigger = true;
            Debug.Log("Tracker trigger set to isTrigger = true");
        }
    }

    // Called by individual torches to register themselves
    public void RegisterTorch(TorchMinigame torch)
    {
        if (!allTorches.Contains(torch))
        {
            allTorches.Add(torch);
            Debug.Log($"Registered torch: {torch.GetTorchID()}");

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
        Debug.Log($"Torch lit! Total: {litTorchesCount}/{totalTorches}");

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
            Debug.LogWarning("PlusOne prefab or spawn point not assigned!");
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
            Debug.Log("Player passed through tracker trigger");

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
            Debug.Log("Player left tracker trigger area - hiding panel");
            HideTrackerPanel();
        }
    }

    public void ShowTrackerPanel()
    {
        if (isTrackerVisible || trackerPanel == null) return;

        Debug.Log("Showing tracker panel (sliding from LEFT)");

        isTrackerVisible = true;
        trackerPanel.SetActive(true);

        if (panelSlideCoroutine != null)
            StopCoroutine(panelSlideCoroutine);

        panelSlideCoroutine = StartCoroutine(SlidePanel(true));
    }

    public void HideTrackerPanel()
    {
        if (!isTrackerVisible || trackerPanel == null) return;

        Debug.Log("Hiding tracker panel (sliding to LEFT)");

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
            yield return new WaitForSeconds(panelShowDelay);
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
        yield return new WaitForSeconds(panelSlideDuration + 0.1f);
        trackerPanel.SetActive(false);
        isTrackerVisible = false;
    }

    private void AllTorchesLit()
    {
        if (hasCompleted) return; // Prevent multiple triggers

        Debug.Log("=== ALL TORCHES ARE LIT! ===");

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
        Debug.Log($"Waiting {timelineDelay} seconds before playing timeline...");
        yield return new WaitForSeconds(timelineDelay);

        Debug.Log("Playing timeline...");

        if (playableDirector != null && timelineToPlay != null)
        {
            // Assign the timeline to the Playable Director and play it
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Play();

            Debug.Log($"Playing timeline: {timelineToPlay.name}");
        }
        else if (playableDirector == null)
        {
            Debug.LogWarning("Playable Director not assigned. Cannot play timeline.");
        }
        else if (timelineToPlay == null)
        {
            Debug.LogWarning("Timeline asset not assigned. Please assign a timeline in the Inspector.");
        }

        // Optional: Hide tracker panel when timeline starts
        HideTrackerPanel();
    }

    // Manual method to play timeline
    public void PlayTimeline()
    {
        if (playableDirector != null && timelineToPlay != null)
        {
            Debug.Log($"Manually playing timeline: {timelineToPlay.name}");
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Play();
        }
        else
        {
            Debug.LogWarning("Cannot play timeline: Playable Director or Timeline asset not assigned.");
        }
    }

    // Method to stop timeline
    public void StopTimeline()
    {
        if (playableDirector != null && playableDirector.state == PlayState.Playing)
        {
            Debug.Log("Stopping timeline");
            playableDirector.Stop();
        }
    }

    // Method to restart timeline
    public void RestartTimeline()
    {
        if (playableDirector != null && timelineToPlay != null)
        {
            Debug.Log("Restarting timeline");
            playableDirector.playableAsset = timelineToPlay;
            playableDirector.Stop();
            playableDirector.time = 0;
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
        yield return new WaitForSeconds(delay);
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
        Debug.Log("Tracker trigger reset");
    }

    // Reset minigame completion state
    public void ResetMinigame()
    {
        hasCompleted = false;
        litTorchesCount = 0;
        UpdateTrackerText();
        Debug.Log("Minigame reset");
    }

    // Force update tracker (useful for debugging)
    public void ForceUpdateTracker()
    {
        UpdateTrackerText();
        Debug.Log($"Force updated tracker: {litTorchesCount}/{totalTorches}");
    }

    // Set Playable Director at runtime
    public void SetPlayableDirector(PlayableDirector director)
    {
        playableDirector = director;
        Debug.Log($"Playable Director set to: {(director != null ? director.name : "null")}");
    }

    // Set Timeline asset at runtime
    public void SetTimelineToPlay(PlayableAsset timelineAsset)
    {
        timelineToPlay = timelineAsset;
        Debug.Log($"Timeline asset set to: {(timelineAsset != null ? timelineAsset.name : "null")}");
    }

    // Set timeline delay at runtime
    public void SetTimelineDelay(float delay)
    {
        timelineDelay = Mathf.Max(0f, delay);
        Debug.Log($"Timeline delay set to: {timelineDelay} seconds");
    }

    // Enable/disable timeline playback
    public void SetPlayTimelineOnCompletion(bool enabled)
    {
        playTimelineOnCompletion = enabled;
        Debug.Log($"Timeline playback on completion: {enabled}");
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
        Debug.Log($"Restored torch states: {litTorchesCount}/{totalTorches} lit");

        // Check if already completed
        if (litTorchesCount >= totalTorches)
        {
            hasCompleted = true;
        }
    }
}