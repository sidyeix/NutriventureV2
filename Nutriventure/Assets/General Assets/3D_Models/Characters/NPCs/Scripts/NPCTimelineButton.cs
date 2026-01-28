using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.Collections;
using UnityEngine.Timeline;
using System.Collections.Generic;

public class NPCTimelineButton : MonoBehaviour
{
    [Header("NPC Timeline Playable")]
    [Tooltip("Drag the Playable Asset (timeline) to play")]
    public PlayableAsset npcPlayable;

    [Header("Playable Director Reference")]
    [Tooltip("Drag the Playable Director component that will play the timeline")]
    public PlayableDirector playableDirector;

    [Header("Button Settings")]
    [Tooltip("Optional: If not assigned, will try to get Button component on this GameObject")]
    public Button npcButton;

    [Tooltip("Delay before playing NPC timeline (seconds)")]
    public float npcPlayDelay = 0.1f;

    [Header("NPC Specific Settings")]
    [Tooltip("Optional: NPC name for debugging")]
    public string npcName = "NPC";

    [Tooltip("Should the NPC timeline loop?")]
    public bool loopNPCTimeline = false;

    [Header("Playable Director Objects")]
    [Tooltip("The GameObject containing the PlayableDirector (will be enabled when timeline plays)")]
    public GameObject directorObject;

    [Tooltip("List of other PlayableDirector GameObjects to disable")]
    public List<GameObject> otherDirectorObjects = new List<GameObject>();

    [Header("Timeline Stopping")]
    [Tooltip("Check to automatically stop any currently playing timeline")]
    public bool stopCurrentTimeline = true;

    [Tooltip("Optional: Specific timeline to stop (if empty, will stop all)")]
    public PlayableDirector specificTimelineToStop;

    [Tooltip("Reset timeline to beginning when stopped?")]
    public bool resetOnStop = true;

    [Header("Timeline Management")]
    [Tooltip("Should we immediately reset the current timeline to 0 before switching?")]
    public bool resetBeforeSwitch = true;

    [Tooltip("Parent object to run coroutines if button is inactive")]
    public GameObject coroutineRunner;

    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;

    void Start()
    {
        // Get button component if not assigned
        if (npcButton == null)
        {
            npcButton = GetComponent<Button>();
        }

        // Get PlayableDirector if not assigned
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();

            // If still null, try to find in children
            if (playableDirector == null)
            {
                playableDirector = GetComponentInChildren<PlayableDirector>();
            }
        }

        // Set coroutine runner to this gameobject by default
        if (coroutineRunner == null)
        {
            coroutineRunner = gameObject;
        }

        // If directorObject is not assigned but we have playableDirector, use its GameObject
        if (directorObject == null && playableDirector != null)
        {
            directorObject = playableDirector.gameObject;
        }

        // Add click listener
        if (npcButton != null)
        {
            npcButton.onClick.AddListener(OnNPCButtonClick);
        }
        else
        {
            Debug.LogError("No Button component found on NPC button: " + gameObject.name);
        }

        // Validate setup
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (playableDirector == null)
        {
            Debug.LogError($"NPC '{npcName}': No PlayableDirector assigned or found!");
        }

        if (npcPlayable != null)
        {
            Debug.Log($"NPC '{npcName}': Using timeline: {npcPlayable.name}");
        }
        else
        {
            Debug.LogWarning($"NPC '{npcName}': No PlayableAsset assigned!");
        }

        if (directorObject == null)
        {
            Debug.LogWarning($"NPC '{npcName}': No directorObject assigned. Will not be able to enable/disable GameObject.");
        }
    }

    private void OnNPCButtonClick()
    {
        // Check if game is active from Game Manager
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

            Debug.Log($"NPC '{npcName}': Game state paused. Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
        }

        // Play button click sound
        PlayNPCButtonSound();

        // Handle NPC timeline control IMMEDIATELY
        ControlNPCTimelinesImmediate();
    }

    private void PlayNPCButtonSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
            Debug.Log($"NPC '{npcName}' button click sound played");
        }
        else
        {
            Debug.LogWarning("AudioHandler.Instance is null. Cannot play NPC button click sound.");
        }
    }

    private void ControlNPCTimelinesImmediate()
    {
        // Stop any existing coroutine
        if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
        {
            StopAllCoroutines();
        }

        // Step 1: Disable other director objects
        DisableOtherDirectorObjects();

        // Step 2: Enable our director object
        EnableDirectorObject();

        // Step 3: Stop any currently playing timeline and reset it IMMEDIATELY
        if (resetBeforeSwitch)
        {
            ResetCurrentTimelineImmediate();
        }
        else
        {
            StopCurrentTimelineIfPlaying();
        }

        // Step 4: Play the NPC playable using the playable director
        if (playableDirector != null && npcPlayable != null)
        {
            // Ensure timeline is properly reset before playing
            ResetNPCTimelineImmediate();

            // Configure the playable director WITH NONE WRAP MODE
            ConfigurePlayableDirector();

            // Subscribe to timeline stopped event to resume game state
            playableDirector.stopped += OnTimelineStopped;

            if (npcPlayDelay > 0)
            {
                if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
                {
                    StartCoroutine(PlayNPCTimelineWithDelay());
                }
                else
                {
                    // Use Invoke if coroutine runner is inactive
                    Invoke("PlayNPCTimelineImmediate", npcPlayDelay);
                }
            }
            else
            {
                PlayNPCTimelineImmediate();
            }
        }
        else
        {
            Debug.LogWarning($"NPC '{npcName}': Cannot play timeline. PlayableDirector: {playableDirector != null}, PlayableAsset: {npcPlayable != null}");
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Only handle our own director
        if (director != playableDirector) return;

        Debug.Log($"NPC '{npcName}': Timeline stopped. Director: {director.name}");

        // Unsubscribe from the event
        playableDirector.stopped -= OnTimelineStopped;

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

            Debug.Log($"NPC '{npcName}': Game state resumed. Timer resumed: {!wasTimerPaused}, Energy resumed: {!wasEnergyPaused}");
        }

        isGameStatePaused = false;
    }

    private void DisableOtherDirectorObjects()
    {
        if (otherDirectorObjects.Count == 0)
        {
            // Optional: Find all other PlayableDirector GameObjects automatically
            FindAndDisableOtherDirectors();
            return;
        }

        foreach (GameObject obj in otherDirectorObjects)
        {
            if (obj != null && obj != directorObject)
            {
                obj.SetActive(false);
                Debug.Log($"NPC '{npcName}': Disabled other director object: {obj.name}");
            }
        }
    }

    private void FindAndDisableOtherDirectors()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();

        foreach (PlayableDirector director in allDirectors)
        {
            if (director != playableDirector && director.gameObject != directorObject)
            {
                director.gameObject.SetActive(false);
                Debug.Log($"NPC '{npcName}': Disabled automatically found director: {director.gameObject.name}");
            }
        }
    }

    private void EnableDirectorObject()
    {
        if (directorObject != null)
        {
            directorObject.SetActive(true);
            Debug.Log($"NPC '{npcName}': Enabled director object: {directorObject.name}");
        }
    }

    private void ResetCurrentTimelineImmediate()
    {
        if (!stopCurrentTimeline) return;

        // Option 1: Reset specific timeline
        if (specificTimelineToStop != null)
        {
            if (specificTimelineToStop.state == PlayState.Playing || specificTimelineToStop.state == PlayState.Paused)
            {
                // COMPLETELY stop and reset the timeline
                specificTimelineToStop.Stop();
                specificTimelineToStop.time = 0;
                specificTimelineToStop.Evaluate();
                Debug.Log($"NPC '{npcName}': Immediately reset specific timeline to 0: {specificTimelineToStop.name}");
            }
        }
        // Option 2: Reset all playable directors in the scene
        else
        {
            ResetAllPlayingTimelinesImmediate();
        }
    }

    private void ResetAllPlayingTimelinesImmediate()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int resetCount = 0;

        foreach (var director in allDirectors)
        {
            // Don't reset our own director yet (we'll start it fresh)
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                // COMPLETELY stop and reset
                director.Stop();
                director.time = 0;
                director.Evaluate();
                resetCount++;
                Debug.Log($"NPC '{npcName}': Immediately reset timeline to 0: {director.name}");
            }
        }

        if (resetCount > 0)
        {
            Debug.Log($"NPC '{npcName}': Immediately reset {resetCount} timeline(s) to beginning");
        }
    }

    private void StopCurrentTimelineIfPlaying()
    {
        if (!stopCurrentTimeline) return;

        // Option 1: Stop specific timeline
        if (specificTimelineToStop != null)
        {
            if (specificTimelineToStop.state == PlayState.Playing || specificTimelineToStop.state == PlayState.Paused)
            {
                StopAndResetTimeline(specificTimelineToStop);
                Debug.Log($"NPC '{npcName}': Stopped and reset specific timeline: {specificTimelineToStop.name}");
            }
        }
        // Option 2: Stop all playable directors in the scene
        else
        {
            StopAllPlayingTimelines();
        }
    }

    private void StopAndResetTimeline(PlayableDirector director)
    {
        if (director == null) return;

        // Stop the timeline
        director.Stop();

        // Reset time to beginning
        director.time = 0;

        // Evaluate to apply the reset
        director.Evaluate();

        // Force a frame update to ensure reset is applied
        if (resetOnStop)
        {
            // This ensures all tracks are properly reset
            director.RebuildGraph();
        }
    }

    private void StopAllPlayingTimelines()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int stoppedCount = 0;

        foreach (var director in allDirectors)
        {
            // Don't stop our own director yet (we'll start it fresh)
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                StopAndResetTimeline(director);
                stoppedCount++;
                Debug.Log($"NPC '{npcName}': Stopped and reset timeline: {director.name}");
            }
        }

        if (stoppedCount > 0)
        {
            Debug.Log($"NPC '{npcName}': Stopped and reset {stoppedCount} timeline(s)");
        }
    }

    private void ConfigurePlayableDirector()
    {
        if (playableDirector != null && npcPlayable != null)
        {
            // Set the playable asset
            playableDirector.playableAsset = npcPlayable;

            // KEY CHANGE: Use None instead of Hold!
            // This makes the timeline stop completely at the end
            if (loopNPCTimeline)
            {
                playableDirector.extrapolationMode = DirectorWrapMode.Loop;
            }
            else
            {
                // Use NONE instead of HOLD - timeline will stop completely at the end
                playableDirector.extrapolationMode = DirectorWrapMode.None;
            }

            Debug.Log($"NPC '{npcName}': Configured PlayableDirector with asset: {npcPlayable.name}, Wrap Mode: {playableDirector.extrapolationMode}");
        }
    }

    private IEnumerator PlayNPCTimelineWithDelay()
    {
        yield return new WaitForSeconds(npcPlayDelay);
        PlayNPCTimelineImmediate();
    }

    private void PlayNPCTimelineImmediate()
    {
        if (playableDirector != null && npcPlayable != null)
        {
            // Make sure we're starting from the beginning
            playableDirector.time = 0;
            playableDirector.Evaluate();

            // Play the timeline
            playableDirector.Play();

            Debug.Log($"NPC '{npcName}': Playing timeline from beginning: {npcPlayable.name} using director: {playableDirector.name}");

            // Log timeline duration
            Debug.Log($"NPC '{npcName}': Timeline duration: {npcPlayable.duration:F2} seconds, Starting at time: {playableDirector.time:F2}, Wrap Mode: {playableDirector.extrapolationMode}");
        }
    }

    private void ResetNPCTimelineImmediate()
    {
        if (playableDirector != null)
        {
            playableDirector.Stop();
            playableDirector.time = 0;
            playableDirector.Evaluate();

            // Optional: Rebuild graph for complete reset
            if (resetOnStop)
            {
                playableDirector.RebuildGraph();
            }
        }
    }

    // Public methods for manual control
    public void SetNPCPlayable(PlayableAsset playable)
    {
        npcPlayable = playable;
    }

    public void SetPlayableDirector(PlayableDirector director)
    {
        playableDirector = director;

        // Update directorObject reference
        if (playableDirector != null && directorObject == null)
        {
            directorObject = playableDirector.gameObject;
        }
    }

    public void SetDirectorObject(GameObject obj)
    {
        directorObject = obj;
    }

    public void AddOtherDirectorObject(GameObject obj)
    {
        if (obj != null && !otherDirectorObjects.Contains(obj))
        {
            otherDirectorObjects.Add(obj);
        }
    }

    public void RemoveOtherDirectorObject(GameObject obj)
    {
        if (otherDirectorObjects.Contains(obj))
        {
            otherDirectorObjects.Remove(obj);
        }
    }

    public void ClearOtherDirectorObjects()
    {
        otherDirectorObjects.Clear();
    }

    public void SetNPCName(string name)
    {
        npcName = name;
    }

    public void PlayNPCImmediate()
    {
        // Check if game is active
        bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

        if (isGameActive)
        {
            // Save and pause game state
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();
            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();
            isGameStatePaused = true;
        }

        // Use immediate methods
        DisableOtherDirectorObjects();
        EnableDirectorObject();

        if (resetBeforeSwitch)
        {
            ResetCurrentTimelineImmediate();
        }
        else
        {
            StopCurrentTimelineIfPlaying();
        }

        if (playableDirector != null && npcPlayable != null)
        {
            ResetNPCTimelineImmediate();
            ConfigurePlayableDirector();

            // Subscribe to stopped event
            playableDirector.stopped += OnTimelineStopped;

            PlayNPCTimelineImmediate();
        }
    }

    // Method to manually trigger NPC button click
    public void SimulateNPCButtonClick()
    {
        OnNPCButtonClick();
    }

    // Method to check if NPC timeline is currently playing
    public bool IsNPCTimelinePlaying()
    {
        return playableDirector != null &&
               playableDirector.playableAsset == npcPlayable &&
               playableDirector.state == PlayState.Playing;
    }

    // Method to get current play state
    public PlayState GetNPCPlayState()
    {
        return playableDirector != null ? playableDirector.state : PlayState.Paused;
    }

    // Method to stop NPC timeline
    public void StopNPCTimeline()
    {
        if (playableDirector != null && (playableDirector.state == PlayState.Playing || playableDirector.state == PlayState.Paused))
        {
            StopAndResetTimeline(playableDirector);

            // Resume game state if it was paused
            if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
            {
                ResumeGameState();
            }
        }
    }

    // Method to reset NPC timeline to beginning
    public void ResetNPCTimeline()
    {
        if (playableDirector != null)
        {
            playableDirector.Stop();
            playableDirector.time = 0;
            playableDirector.Evaluate();

            // Optional: Rebuild graph for complete reset
            if (resetOnStop)
            {
                playableDirector.RebuildGraph();
            }
        }
    }

    // Method to restart the current NPC timeline from beginning
    public void RestartNPCTimeline()
    {
        if (playableDirector != null && npcPlayable != null)
        {
            StopNPCTimeline();
            PlayNPCTimelineImmediate();
        }
    }

    // Method to check if timeline is at the beginning
    public bool IsAtBeginning()
    {
        return playableDirector != null && playableDirector.time <= 0.01f;
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (npcButton != null)
        {
            npcButton.onClick.RemoveListener(OnNPCButtonClick);
        }

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

        // Clean up any pending invokes
        CancelInvoke();
    }
}