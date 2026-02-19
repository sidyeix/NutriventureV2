using Cinemachine;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class KingVitronTimelineButton : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("Playable for first-time completion (key not collected yet)")]
    public PlayableAsset firstTimePlayable;
    [Tooltip("Playable for subsequent playthroughs (key already collected OR 0-1 stars)")]
    public PlayableAsset subsequentPlayable;

    [Header("Playable Director Reference")]
    public PlayableDirector playableDirector;

    [Header("Button Settings")]
    public Button kingButton;
    public float playDelay = 0.1f;

    [Header("Key Unlocked Canvas")]
    public KeyUnlockedCanvasController keyUnlockedCanvas;

    [Header("Star Rating Requirements")]
    [Tooltip("Minimum stars required to unlock the key on first completion")]
    public int minStarsToUnlockKey = 2; // Player needs at least 2 stars to unlock key

    [Header("Playable Director Objects")]
    public GameObject directorObject;
    public List<GameObject> otherDirectorObjects = new List<GameObject>();

    [Header("Game End Reference")]
    public GameEndManager gameEndManager;

    [Header("Camera Settings")]
    public bool switchToGameEndCamera = true;
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("Timeline Management")]
    public bool resetBeforeSwitch = true;
    public bool loopTimeline = false;
    public GameObject coroutineRunner;

    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;
    private bool isPlayingTimeline = false;
    private int starsEarned = 0;
    private bool isFirstTimeKeyUnlock = false; // Track if this is a first-time key unlock scenario

    void Start()
    {
        if (kingButton == null)
            kingButton = GetComponent<Button>();

        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();
            if (playableDirector == null)
                playableDirector = GetComponentInChildren<PlayableDirector>();
        }

        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<GameEndManager>();

        if (coroutineRunner == null)
            coroutineRunner = gameObject;

        if (directorObject == null && playableDirector != null)
            directorObject = playableDirector.gameObject;

        if (kingButton != null)
            kingButton.onClick.AddListener(OnKingButtonClick);
        else
            Debug.LogError("No Button component found on King button: " + gameObject.name);

        // Find key unlocked canvas if not assigned
        if (keyUnlockedCanvas == null)
            keyUnlockedCanvas = FindObjectOfType<KeyUnlockedCanvasController>(true);
    }

    private void OnKingButtonClick()
    {
        // Get stars earned from game end manager
        if (gameEndManager != null)
        {
            starsEarned = gameEndManager.GetStarsEarned();
            Debug.Log($"King Vitron: Stars earned this run: {starsEarned}");
        }

        // Check game state
        bool isGameActive = GoGrowGlowGameManager.Instance != null && GoGrowGlowGameManager.Instance.IsGameActive();

        if (isGameActive)
        {
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();

            isGameStatePaused = true;
            Debug.Log($"King Vitron: Game state paused. Energy was paused: {wasEnergyPaused}, Timer was paused: {wasTimerPaused}");
        }

        PlayKingButtonSound();
        ControlKingTimelinesImmediate();
    }

    private void PlayKingButtonSound()
    {
        if (AudioHandler.Instance != null)
            AudioHandler.Instance.PlayButtonClick();
    }

    private void ControlKingTimelinesImmediate()
    {
        if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
            StopAllCoroutines();

        // Determine which timeline to play based on key status and stars
        DetermineTimelineToPlay();

        if (currentPlayableToPlay == null)
        {
            Debug.LogError("King Vitron: No playable asset to play!");
            ResumeGameState();
            return;
        }

        DisableOtherDirectorObjects();
        EnableDirectorObject();

        if (resetBeforeSwitch)
            ResetCurrentTimelineImmediate();
        else
            StopCurrentTimelineIfPlaying();

        if (playableDirector != null)
        {
            ResetKingTimelineImmediate();
            ConfigurePlayableDirector();

            playableDirector.stopped += OnTimelineStopped;

            isPlayingTimeline = true;

            if (playDelay > 0)
            {
                if (coroutineRunner != null && coroutineRunner.activeInHierarchy)
                    StartCoroutine(PlayKingTimelineWithDelay());
                else
                    Invoke("PlayKingTimelineImmediate", playDelay);
            }
            else
            {
                PlayKingTimelineImmediate();
            }
        }
        else
        {
            Debug.LogWarning("King Vitron: Cannot play timeline. PlayableDirector is null");
            ResumeGameState();
        }
    }

    private PlayableAsset currentPlayableToPlay;

    private void DetermineTimelineToPlay()
    {
        bool hasKey = false;

        // Check if player already has the Nutri Kingdom key from GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            hasKey = GameDataManager.Instance.CurrentGameData.HasNutriKingdomKey();
        }

        Debug.Log($"King Vitron: Has Nutri Kingdom Key (from GameData): {hasKey}, Stars Earned: {starsEarned}");

        // Determine which timeline to play based SOLELY on GameData and stars
        if (!hasKey && starsEarned >= minStarsToUnlockKey)
        {
            // First time: Key not collected AND player earned enough stars
            currentPlayableToPlay = firstTimePlayable;
            isFirstTimeKeyUnlock = true;
            Debug.Log($"King Vitron: FIRST TIME playable (key unlock eligible) - Stars: {starsEarned} ? {minStarsToUnlockKey}");
        }
        else
        {
            // Subsequent: Key already collected OR stars too low (0-1 stars)
            currentPlayableToPlay = subsequentPlayable;
            isFirstTimeKeyUnlock = false;
            Debug.Log($"King Vitron: SUBSEQUENT playable - HasKey: {hasKey}, Stars: {starsEarned} < {minStarsToUnlockKey}");
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != playableDirector) return;

        Debug.Log("King Vitron: Timeline stopped");

        playableDirector.stopped -= OnTimelineStopped;
        isPlayingTimeline = false;

        // Handle post-timeline actions
        HandlePostTimelineActions();

        if (isGameStatePaused && GoGrowGlowGameManager.Instance != null)
            ResumeGameState();
    }

    private void HandlePostTimelineActions()
    {
        Debug.Log("King Vitron: Handling post-timeline actions...");

        // If this was a first-time key unlock timeline
        if (isFirstTimeKeyUnlock)
        {
            Debug.Log("King Vitron: First-time timeline completed. Will show key unlocked canvas after home button.");
            // The key is NOT collected yet - it will be collected after the canvas
        }

        // Show game end screen
        if (gameEndManager != null)
        {
            if (switchToGameEndCamera)
                SwitchToGameEndCamera();

            TeleportPlayerToResultPoint();
            ShowGameEndScreen();
        }
    }

    // This method should be called from GameEndManager when home button is clicked
    public void CheckAndShowKeyUnlockedCanvas()
    {
        // Only show if this was a first-time key unlock AND player hasn't collected key yet
        if (isFirstTimeKeyUnlock && keyUnlockedCanvas != null)
        {
            bool hasKey = false;
            if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
            {
                hasKey = GameDataManager.Instance.CurrentGameData.HasNutriKingdomKey();
            }

            // Don't show if key already collected (safety check)
            if (!hasKey)
            {
                Debug.Log("King Vitron: Showing key unlocked canvas");
                keyUnlockedCanvas.ShowKeyUnlockedCanvas(OnKeyUnlockedContinue);
            }
            else
            {
                Debug.Log("King Vitron: Key already collected, not showing canvas");
            }
        }
        else
        {
            Debug.Log($"King Vitron: Not showing key unlocked canvas - isFirstTimeKeyUnlock: {isFirstTimeKeyUnlock}");
        }
    }

    private void OnKeyUnlockedContinue()
    {
        Debug.Log("King Vitron: Key unlocked canvas continue clicked - collecting key now");

        // Collect the Nutri Kingdom key in GameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.CollectNutriKingdomKey();
            GameDataManager.Instance.SaveGameData();
            Debug.Log("King Vitron: Nutri Kingdom Key collected and saved to GameData!");

            // Reset the flag so we don't show canvas again
            isFirstTimeKeyUnlock = false;
        }
    }

    private void SwitchToGameEndCamera()
    {
        if (playerFollowCamera != null)
        {
            playerFollowCamera.Priority = 0;
            playerFollowCamera.gameObject.SetActive(false);
            Debug.Log("King Vitron: Disabled player follow camera");
        }
    }

    private void TeleportPlayerToResultPoint()
    {
        ThirdPersonController playerController = FindObjectOfType<ThirdPersonController>();

        if (playerController != null && gameEndManager != null)
        {
            // Use the public property or method instead of reflection if available
            Transform resultSpawnPoint = gameEndManager.GetResultSpawnPoint();
            if (resultSpawnPoint != null)
            {
                playerController.transform.position = resultSpawnPoint.position;
                playerController.transform.rotation = resultSpawnPoint.rotation;
                Debug.Log("King Vitron: Player teleported to result spawn point");
            }
        }
    }

    private void ShowGameEndScreen()
    {
        if (gameEndManager != null)
        {
            if (GoGrowGlowGameManager.Instance != null)
            {
                float completionTime = GoGrowGlowGameManager.Instance.GetGameTimer();
                int playerPoints = GoGrowGlowGameManager.Instance.GetCurrentScore();
                int remainingHearts = Mathf.CeilToInt(GoGrowGlowGameManager.Instance.GetCurrentLifeAmount());

                Debug.Log($"King Vitron: Game end data - Time: {completionTime}, Points: {playerPoints}, Hearts: {remainingHearts}");

                gameEndManager.HandleLevelComplete();
            }
            else
            {
                gameEndManager.TriggerLevelComplete();
            }
        }
    }

    private void ResumeGameState()
    {
        if (GoGrowGlowGameManager.Instance != null)
        {
            if (!wasTimerPaused)
                GoGrowGlowGameManager.Instance.ResumeGameTimer();

            if (!wasEnergyPaused)
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();

            Debug.Log($"King Vitron: Game state resumed. Timer resumed: {!wasTimerPaused}, Energy resumed: {!wasEnergyPaused}");
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
                Debug.Log($"King Vitron: Disabled other director object: {obj.name}");
            }
        }
    }

    private void EnableDirectorObject()
    {
        if (directorObject != null)
        {
            directorObject.SetActive(true);
            Debug.Log($"King Vitron: Enabled director object: {directorObject.name}");
        }
    }

    private void ResetCurrentTimelineImmediate()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int resetCount = 0;

        foreach (var director in allDirectors)
        {
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                director.Stop();
                director.time = 0;
                director.Evaluate();
                resetCount++;
                Debug.Log($"King Vitron: Immediately reset timeline to 0: {director.name}");
            }
        }

        if (resetCount > 0)
            Debug.Log($"King Vitron: Immediately reset {resetCount} timeline(s) to beginning");
    }

    private void StopCurrentTimelineIfPlaying()
    {
        PlayableDirector[] allDirectors = FindObjectsOfType<PlayableDirector>();
        int stoppedCount = 0;

        foreach (var director in allDirectors)
        {
            if (director != playableDirector && (director.state == PlayState.Playing || director.state == PlayState.Paused))
            {
                director.Stop();
                director.time = 0;
                director.Evaluate();
                stoppedCount++;
                Debug.Log($"King Vitron: Stopped and reset timeline: {director.name}");
            }
        }

        if (stoppedCount > 0)
            Debug.Log($"King Vitron: Stopped and reset {stoppedCount} timeline(s)");
    }

    private void ConfigurePlayableDirector()
    {
        if (playableDirector != null && currentPlayableToPlay != null)
        {
            playableDirector.playableAsset = currentPlayableToPlay;

            if (loopTimeline)
                playableDirector.extrapolationMode = DirectorWrapMode.Loop;
            else
                playableDirector.extrapolationMode = DirectorWrapMode.None;

            Debug.Log($"King Vitron: Configured PlayableDirector with asset: {currentPlayableToPlay.name}, Wrap Mode: {playableDirector.extrapolationMode}");
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
            playableDirector.time = 0;
            playableDirector.Evaluate();
            playableDirector.Play();

            Debug.Log($"King Vitron: Playing timeline from beginning: {currentPlayableToPlay.name}");
            Debug.Log($"King Vitron: Timeline duration: {currentPlayableToPlay.duration:F2} seconds");
            Debug.Log($"King Vitron: Playing {(currentPlayableToPlay == firstTimePlayable ? "FIRST-TIME" : "SUBSEQUENT")} timeline");
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

    // Public method to manually trigger key unlock check (to be called from GameEndManager)
    public void CheckKeyUnlockAfterHomeButton()
    {
        CheckAndShowKeyUnlockedCanvas();
    }

    // Method to get stars from GameEndManager
    public void SetStarsEarned(int stars)
    {
        starsEarned = stars;
    }
}