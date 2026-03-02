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
    public PlayableAsset firstTimePlayable;
    public PlayableAsset subsequentPlayable;

    [Header("Playable Director Reference")]
    public PlayableDirector playableDirector;

    [Header("Button Settings")]
    public Button kingButton;
    public float playDelay = 0.1f;

    [Header("Playable Director Objects")]
    public GameObject directorObject;
    public List<GameObject> otherDirectorObjects = new List<GameObject>();

    [Header("Game End Reference")]
    public GameEndManager gameEndManager;

    [Header("Camera Settings")]
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("Timeline Management")]
    public bool resetBeforeSwitch = true;
    public bool loopTimeline = false;

    private bool wasEnergyPaused = false;
    private bool wasTimerPaused = false;
    private bool isGameStatePaused = false;
    private bool isPlayingTimeline = false;

    private PlayableAsset currentPlayableToPlay;

    void Start()
    {
        if (kingButton == null)
            kingButton = GetComponent<Button>();

        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();

        if (gameEndManager == null)
            gameEndManager = FindObjectOfType<GameEndManager>();

        if (kingButton != null)
            kingButton.onClick.AddListener(OnKingButtonClick);
    }

    private void OnKingButtonClick()
    {
        PauseGameState();
        DetermineTimelineToPlay();
        PlayTimeline();
    }

    // ===============================
    // ONLY CHECKS KEY (NO COLLECTING)
    // ===============================
    private void DetermineTimelineToPlay()
    {
        bool hasSugariaKey = GameDataManager.Instance.HasSugariaKey();

        Debug.Log("=== KING VITRON DECISION ===");
        Debug.Log("Has Sugaria Key: " + hasSugariaKey);

        if (!hasSugariaKey)
        {
            currentPlayableToPlay = firstTimePlayable;
            Debug.Log("Playing FIRST-TIME timeline");
        }
        else
        {
            currentPlayableToPlay = subsequentPlayable;
            Debug.Log("Playing SUBSEQUENT timeline");
        }
    }

    private void PlayTimeline()
    {
        if (currentPlayableToPlay == null || playableDirector == null)
        {
            Debug.LogError("Playable asset or director missing!");
            ResumeGameState();
            return;
        }

        DisableOtherDirectorObjects();

        if (directorObject != null)
            directorObject.SetActive(true);

        playableDirector.Stop();
        playableDirector.time = 0;
        playableDirector.playableAsset = currentPlayableToPlay;

        playableDirector.extrapolationMode =
            loopTimeline ? DirectorWrapMode.Loop : DirectorWrapMode.None;

        playableDirector.stopped += OnTimelineStopped;

        StartCoroutine(PlayWithDelay());
    }

    private IEnumerator PlayWithDelay()
    {
        yield return new WaitForSeconds(playDelay);

        playableDirector.Play();
        isPlayingTimeline = true;
    }

    private int lastStarsEarned = 0;

    public void SetStarsEarned(int stars)
    {
        lastStarsEarned = stars;
        Debug.Log("KingVitron received stars: " + stars);
    }

    public void CheckKeyUnlockAfterHomeButton()
    {
        // Intentionally empty - key saving is now handled by GameEndManager
        Debug.Log("KingVitron CheckKeyUnlockAfterHomeButton called (no action taken)");
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != playableDirector) return;

        playableDirector.stopped -= OnTimelineStopped;
        isPlayingTimeline = false;

        ResumeGameState();

        Debug.Log("Timeline finished. Player can now roam freely.");
    }

    private void DisableOtherDirectorObjects()
    {
        foreach (GameObject obj in otherDirectorObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private void PauseGameState()
    {
        if (GoGrowGlowGameManager.Instance != null &&
            GoGrowGlowGameManager.Instance.IsGameActive())
        {
            wasEnergyPaused = GoGrowGlowGameManager.Instance.IsEnergyDecreasePaused();
            wasTimerPaused = GoGrowGlowGameManager.Instance.IsGameTimerPaused();

            GoGrowGlowGameManager.Instance.PauseEnergyDecrease();
            GoGrowGlowGameManager.Instance.PauseGameTimer();

            isGameStatePaused = true;
        }
    }

    private void ResumeGameState()
    {
        if (!isGameStatePaused) return;

        if (GoGrowGlowGameManager.Instance != null)
        {
            if (!wasTimerPaused)
                GoGrowGlowGameManager.Instance.ResumeGameTimer();

            if (!wasEnergyPaused)
                GoGrowGlowGameManager.Instance.ResumeEnergyDecrease();
        }

        isGameStatePaused = false;
    }
}