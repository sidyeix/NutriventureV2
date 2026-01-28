using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class KingVitronTimelineButton : MonoBehaviour
{
    [Header("Quest Settings")]
    [SerializeField] private string kingdomID = "general_quests";
    [SerializeField] private string questID = "0001";

    [Header("Timeline Settings")]
    [Tooltip("Playable for first-time completion (quest InProgress/NotStarted)")]
    public PlayableAsset firstTimePlayable;
    [Tooltip("Playable for subsequent playthroughs (quest Completed/Claimed)")]
    public PlayableAsset subsequentPlayable;

    [Header("Playable Director Reference")]
    [Tooltip("Drag the Playable Director component that will play the timeline")]
    public PlayableDirector playableDirector;

    [Header("Button Settings")]
    [Tooltip("Optional: If not assigned, will try to get Button component on this GameObject")]
    public Button kingButton;

    [Tooltip("Delay before playing timeline (seconds)")]
    public float playDelay = 0.1f;

    [Header("King Specific Settings")]
    [Tooltip("King name for debugging")]
    public string kingName = "King Vitron";

    [Header("Playable Director Objects")]
    [Tooltip("The GameObject containing the PlayableDirector")]
    public GameObject directorObject;

    [Tooltip("List of other PlayableDirector GameObjects to disable")]
    public List<GameObject> otherDirectorObjects = new List<GameObject>();

    [Header("Game End Reference")]
    [Tooltip("Reference to GameEndManager to trigger end screen after timeline")]
    public GameEndManager gameEndManager;

    [Header("Camera Settings")]
    [Tooltip("Should we switch to game end camera after timeline?")]
    public bool switchToGameEndCamera = true;

    [Tooltip("Player follow camera to disable")]
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("Timeline Management")]
    [Tooltip("Should we immediately reset the current timeline to 0 before switching?")]
    public bool resetBeforeSwitch = true;

    [Tooltip("Should the timeline loop?")]
    public bool loopTimeline = false;

    [Tooltip("Parent object to run coroutines if button is inactive")]
    public GameObject coroutineRunner;

    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;
    private bool isPlayingTimeline = false;
    private bool shouldShowGameEndAfterTimeline = false;
    private PlayableAsset currentPlayableToPlay;

    void Start()
    {
        // Get button component if not assigned
        if (kingButton == null)
        {
            kingButton = GetComponent<Button>();
        }

        // Get PlayableDirector if not assigned
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();

            if (playableDirector == null)
            {
                playableDirector = GetComponentInChildren<PlayableDirector>();
            }
        }

        // Get GameEndManager if not assigned
        if (gameEndManager == null)
        {
            gameEndManager = FindObjectOfType<GameEndManager>();
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
        if (kingButton != null)
        {
            kingButton.onClick.AddListener(OnKingButtonClick);
        }
        else
        {
            Debug.LogError("No Button component found on King button: " + gameObject.name);
        }

        // Validate setup
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (playableDirector == null)
        {
            Debug.LogError($"King '{kingName}': No PlayableDirector assigned or found!");
        }

        if (firstTimePlayable == null)
        {
            Debug.LogWarning($"King '{kingName}': No first-time PlayableAsset assigned!");
        }

        if (subsequentPlayable == null)
        {
            Debug.LogWarning($"King '{kingName}': No subsequent PlayableAsset assigned!");
        }

        if (gameEndManager == null)
        {
            Debug.LogWarning($"King '{kingName}': GameEndManager not assigned or found!");
        }

        // Check quest status to determine which playable to use
        DeterminePlayableBasedOnQuestStatus();
    }

    private void DeterminePlayableBasedOnQuestStatus()
    {
        QuestStatus questStatus = GetQuestStatus();
        Debug.Log($"King '{kingName}': Quest {questID} status is {questStatus}");

        // Determine which playable to use
        switch (questStatus)
        {
            case QuestStatus.NotStarted:
            case QuestStatus.InProgress:
                currentPlayableToPlay = firstTimePlayable;
                shouldShowGameEndAfterTimeline = false; // Don't show game end for first time
                Debug.Log($"King '{kingName}': Using FIRST-TIME playable ({firstTimePlayable?.name})");
                break;

            case QuestStatus.Completed:
            case QuestStatus.Claimed:
            case QuestStatus.Failed:
            case QuestStatus.Abandoned:
                currentPlayableToPlay = subsequentPlayable;
                shouldShowGameEndAfterTimeline = true; // Show game end for subsequent playthroughs
                Debug.Log($"King '{kingName}': Using SUBSEQUENT playable ({subsequentPlayable?.name})");
                break;

            default:
                currentPlayableToPlay = subsequentPlayable; // Default to subsequent
                shouldShowGameEndAfterTimeline = true;
                Debug.Log($"King '{kingName}': Using DEFAULT subsequent playable");
                break;
        }
    }

    private QuestStatus GetQuestStatus()
    {
        if (QuestManager.Instance != null)
        {
            Quest quest = QuestManager.Instance.GetQuest(questID);
            if (quest != null)
            {
                return quest.status;
            }
        }
        return QuestStatus.NotStarted; // Default if quest manager not found
    }

    private void OnKingButtonClick()
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

            Debug.Log($"King '{kingName}': Game state paused. Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
        }

        // Play button click sound
        PlayKingButtonSound();

        // Handle timeline control IMMEDIATELY
        ControlKingTimelinesImmediate();
    }

    private void PlayKingButtonSound()
    {
        if (AudioHandler.Instance != null)
        {
            AudioHandler.Instance.PlayButtonClick();
            Debug.Log($"King '{kingName}' button click sound played");
        }
        else
        {
            Debug.LogWarning("AudioHandler.Instance is null. Cannot play King button click sound.");
        }
    }

    private void ControlKingTimelinesImmediate()
    {
        // Stop any existing coroutine
        if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
        {
            StopAllCoroutines();
        }

        // Update quest status and determine which playable to use
        DeterminePlayableBasedOnQuestStatus();

        if (currentPlayableToPlay == null)
        {
            Debug.LogError($"King '{kingName}': No playable asset to play!");
            ResumeGameState(); // Resume game if no timeline to play
            return;
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

        // Step 4: Play the king playable using the playable director
        if (playableDirector != null)
        {
            // Ensure timeline is properly reset before playing
            ResetKingTimelineImmediate();

            // Configure the playable director WITH NONE WRAP MODE
            ConfigurePlayableDirector();

            // Subscribe to timeline stopped event
            playableDirector.stopped += OnTimelineStopped;

            isPlayingTimeline = true;

            if (playDelay > 0)
            {
                if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
                {
                    StartCoroutine(PlayKingTimelineWithDelay());
                }
                else
                {
                    Invoke("PlayKingTimelineImmediate", playDelay);
                }
            }
            else
            {
                PlayKingTimelineImmediate();
            }
        }
        else
        {
            Debug.LogWarning($"King '{kingName}': Cannot play timeline. PlayableDirector is null");
            ResumeGameState(); // Resume game if no director
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        // Only handle our own director
        if (director != playableDirector) return;

        Debug.Log($"King '{kingName}': Timeline stopped. Director: {director.name}");

        // Unsubscribe from the event
        playableDirector.stopped -= OnTimelineStopped;

        isPlayingTimeline = false;

        // Handle post-timeline actions
        HandlePostTimelineActions();

        // Resume game state if we paused it
        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
        {
            ResumeGameState();
        }
    }

    private void HandlePostTimelineActions()
    {
        Debug.Log($"King '{kingName}': Handling post-timeline actions...");

        // Check quest status again to determine what to do
        QuestStatus currentStatus = GetQuestStatus();

        // If this was a first-time playthrough and the timeline shows the key
        if ((currentStatus == QuestStatus.NotStarted || currentStatus == QuestStatus.InProgress) &&
            currentPlayableToPlay == firstTimePlayable)
        {
            // For first-time playthrough, we need to:
            // 1. Complete the quest task
            // 2. Show the key unlocked object
            // 3. Complete the quest

            Debug.Log($"King '{kingName}': First-time timeline completed. Completing quest tasks...");

            // Complete the quest task
            if (QuestManager.Instance != null)
            {
                string taskID = $"{questID}_task_1";
                QuestManager.Instance.CompleteTask(questID, taskID);
                Debug.Log($"Completed task: {taskID}");

                // Claim the quest
                QuestManager.Instance.ClaimQuest(questID);
                Debug.Log($"Claimed quest: {questID}");
            }

            // Update quest status after completion
            QuestStatus updatedStatus = GetQuestStatus();
            Debug.Log($"Quest status after completion: {updatedStatus}");

            // Don't show game end screen for first-time completion
            // The key unlocked object will be shown by GameEndManager
            return;
        }

        // For subsequent playthroughs (or if quest is already completed/claimed)
        if (shouldShowGameEndAfterTimeline && gameEndManager != null)
        {
            Debug.Log($"King '{kingName}': Showing game end screen after timeline...");

            // Switch to game end camera if requested
            if (switchToGameEndCamera)
            {
                SwitchToGameEndCamera();
            }

            // Teleport player to result spawn point
            TeleportPlayerToResultPoint();

            // Show game end screen
            ShowGameEndScreen();
        }
        else
        {
            Debug.Log($"King '{kingName}': Not showing game end screen (shouldShowGameEndAfterTimeline={shouldShowGameEndAfterTimeline})");
        }
    }

    private void SwitchToGameEndCamera()
    {
        if (playerFollowCamera != null)
        {
            // Disable player camera
            playerFollowCamera.Priority = 0;
            playerFollowCamera.gameObject.SetActive(false);
            Debug.Log($"King '{kingName}': Disabled player follow camera");
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        // Get the player controller
        ThirdPersonController playerController = FindObjectOfType<ThirdPersonController>();

        // Get result spawn point from GameEndManager
        if (playerController != null && gameEndManager != null)
        {
            // Use reflection to access the resultCharacterSpawnPoint
            System.Type gameEndManagerType = typeof(GameEndManager);
            var resultSpawnField = gameEndManagerType.GetField("resultCharacterSpawnPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (resultSpawnField != null)
            {
                Transform resultSpawnPoint = resultSpawnField.GetValue(gameEndManager) as Transform;
                if (resultSpawnPoint != null)
                {
                    playerController.transform.position = resultSpawnPoint.position;
                    playerController.transform.rotation = resultSpawnPoint.rotation;
                    Debug.Log($"King '{kingName}': Player teleported to result spawn point");
                }
            }
        }
    }

    private void ShowGameEndScreen()
    {
        if (gameEndManager != null)
        {
            // Get current game state
            if (GoGrowGlowGameManager.Instance != null)
            {
                float completionTime = GoGrowGlowGameManager.Instance.GetGameTimer();
                int playerPoints = GoGrowGlowGameManager.Instance.GetCurrentScore();
                int remainingHearts = Mathf.CeilToInt(GoGrowGlowGameManager.Instance.GetCurrentLifeAmount());

                Debug.Log($"King '{kingName}': Game end data - Time: {completionTime}, Points: {playerPoints}, Hearts: {remainingHearts}");

                // Always show win screen when talking to king after timeline
                gameEndManager.HandleLevelComplete();
            }
            else
            {
                // Fallback: trigger level complete directly
                gameEndManager.TriggerLevelComplete();
            }
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

            Debug.Log($"King '{kingName}': Game state resumed. Timer resumed: {!wasTimerPaused}, Energy resumed: {!wasEnergyPaused}");
        }

        isGameStatePaused = false;
    }

    private void DisableOtherDirectorObjects()
    {
        if (otherDirectorObjects.Count == 0) return;

        foreach (GameObject obj in otherDirectorObjects)
        {
            if (obj != null && obj != directorObject)
            {
                obj.SetActive(false);
                Debug.Log($"King '{kingName}': Disabled other director object: {obj.name}");
            }
        }
    }

    private void EnableDirectorObject()
    {
        if (directorObject != null)
        {
            directorObject.SetActive(true);
            Debug.Log($"King '{kingName}': Enabled director object: {directorObject.name}");
        }
    }

    private void ResetCurrentTimelineImmediate()
    {
        // Reset all playable directors in the scene
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int resetCount = 0;

        foreach (var director in allDirectors)
        {
            // Don't reset our own director yet (we'll start it fresh)
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                director.Stop();
                director.time = 0;
                director.Evaluate();
                resetCount++;
                Debug.Log($"King '{kingName}': Immediately reset timeline to 0: {director.name}");
            }
        }

        if (resetCount > 0)
        {
            Debug.Log($"King '{kingName}': Immediately reset {resetCount} timeline(s) to beginning");
        }
    }

    private void StopCurrentTimelineIfPlaying()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int stoppedCount = 0;

        foreach (var director in allDirectors)
        {
            // Don't stop our own director yet (we'll start it fresh)
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                director.Stop();
                director.time = 0;
                director.Evaluate();
                stoppedCount++;
                Debug.Log($"King '{kingName}': Stopped and reset timeline: {director.name}");
            }
        }

        if (stoppedCount > 0)
        {
            Debug.Log($"King '{kingName}': Stopped and reset {stoppedCount} timeline(s)");
        }
    }

    private void ConfigurePlayableDirector()
    {
        if (playableDirector != null && currentPlayableToPlay != null)
        {
            // Set the playable asset
            playableDirector.playableAsset = currentPlayableToPlay;

            // Use NONE wrap mode - timeline will stop completely at the end
            if (loopTimeline)
            {
                playableDirector.extrapolationMode = DirectorWrapMode.Loop;
            }
            else
            {
                playableDirector.extrapolationMode = DirectorWrapMode.None;
            }

            Debug.Log($"King '{kingName}': Configured PlayableDirector with asset: {currentPlayableToPlay.name}, Wrap Mode: {playableDirector.extrapolationMode}");
        }
    }

    private IEnumerator PlayKingTimelineWithDelay()
    {
        yield return new WaitForSeconds(playDelay);
        PlayKingTimelineImmediate();
    }

    private void PlayKingTimelineImmediate()
    {
        if (playableDirector != null && currentPlayableToPlay != null)
        {
            // Make sure we're starting from the beginning
            playableDirector.time = 0;
            playableDirector.Evaluate();

            // Play the timeline
            playableDirector.Play();

            Debug.Log($"King '{kingName}': Playing timeline from beginning: {currentPlayableToPlay.name} using director: {playableDirector.name}");
            Debug.Log($"King '{kingName}': Timeline duration: {currentPlayableToPlay.duration:F2} seconds");

            // Log which playable is being played
            QuestStatus questStatus = GetQuestStatus();
            Debug.Log($"King '{kingName}': Quest status: {questStatus}, Playing: {(currentPlayableToPlay == firstTimePlayable ? "First-time" : "Subsequent")} timeline");
        }
    }

    private void ResetKingTimelineImmediate()
    {
        if (playableDirector != null)
        {
            playableDirector.Stop();
            playableDirector.time = 0;
            playableDirector.Evaluate();
        }
    }

    // Public methods for manual control
    public void SetFirstTimePlayable(PlayableAsset playable)
    {
        firstTimePlayable = playable;
        DeterminePlayableBasedOnQuestStatus();
    }

    public void SetSubsequentPlayable(PlayableAsset playable)
    {
        subsequentPlayable = playable;
        DeterminePlayableBasedOnQuestStatus();
    }

    public void SetQuestID(string id)
    {
        questID = id;
        DeterminePlayableBasedOnQuestStatus();
    }

    public void SetKingdomID(string id)
    {
        kingdomID = id;
    }

    public void PlayKingTimelineImmediate(bool forceFirstTime = false, bool forceSubsequent = false)
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

        // Force specific playable if requested
        if (forceFirstTime)
        {
            currentPlayableToPlay = firstTimePlayable;
            shouldShowGameEndAfterTimeline = false;
        }
        else if (forceSubsequent)
        {
            currentPlayableToPlay = subsequentPlayable;
            shouldShowGameEndAfterTimeline = true;
        }
        else
        {
            DeterminePlayableBasedOnQuestStatus();
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

        if (playableDirector != null && currentPlayableToPlay != null)
        {
            ResetKingTimelineImmediate();
            ConfigurePlayableDirector();

            // Subscribe to stopped event
            playableDirector.stopped += OnTimelineStopped;

            PlayKingTimelineImmediate();
        }
    }

    // Method to manually trigger king button click
    public void SimulateKingButtonClick()
    {
        OnKingButtonClick();
    }

    // Method to check if king timeline is currently playing
    public bool IsKingTimelinePlaying()
    {
        return isPlayingTimeline &&
               playableDirector != null &&
               playableDirector.state == PlayState.Playing;
    }

    // Method to stop king timeline
    public void StopKingTimeline()
    {
        if (playableDirector != null && (playableDirector.state == PlayState.Playing || playableDirector.state == PlayState.Paused))
        {
            playableDirector.Stop();
            playableDirector.time = 0;
            playableDirector.Evaluate();

            // Resume game state if it was paused
            if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
            {
                ResumeGameState();
            }

            isPlayingTimeline = false;
        }
    }

    // Method to update quest status and re-determine playable
    public void UpdateQuestStatus()
    {
        DeterminePlayableBasedOnQuestStatus();
    }

    // Method to get current quest status
    public QuestStatus GetCurrentQuestStatus()
    {
        return GetQuestStatus();
    }

    // Method to check if this is first-time completion
    public bool IsFirstTimeCompletion()
    {
        QuestStatus status = GetQuestStatus();
        return (status == QuestStatus.NotStarted || status == QuestStatus.InProgress) && currentPlayableToPlay == firstTimePlayable;
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (kingButton != null)
        {
            kingButton.onClick.RemoveListener(OnKingButtonClick);
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