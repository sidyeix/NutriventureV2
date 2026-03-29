using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Trigger collider at the end of Kingdom 4.
/// First completion: plays a timeline cutscene, then shows the game end screen.
/// Subsequent completions: immediately shows the game end screen.
/// </summary>
public class K4_EndingTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector firstCompletionTimeline;
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;

        // Stop the game timer
        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.StopTimer();

        bool isFirstCompletion = !IsKingdom4CompletedBefore();

        // Mark kingdom as completed and save
        MarkKingdom4Completed();

        if (isFirstCompletion && firstCompletionTimeline != null)
        {
            firstCompletionTimeline.stopped += OnFirstCompletionTimelineStopped;
            firstCompletionTimeline.Play();
        }
        else
        {
            ShowGameEndScreen();
        }
    }

    private void OnFirstCompletionTimelineStopped(PlayableDirector director)
    {
        director.stopped -= OnFirstCompletionTimelineStopped;

        // Mark that the cutscene has been played
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.hasPlayedK4CompletionCutscene = true;
            GameDataManager.Instance.SaveGameData();
        }

        ShowGameEndScreen();
    }

    private void ShowGameEndScreen()
    {
        if (AllerthriaGameManager.Instance != null)
        {
            AllerthriaGameManager.Instance.CompleteGame();
        }
        else if (Kingdom4GameEndManager.Instance != null)
        {
            Kingdom4GameEndManager.Instance.HandleKingdom4Complete();
        }
    }

    private bool IsKingdom4CompletedBefore()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
            return GameDataManager.Instance.CurrentGameData.hasPlayedK4CompletionCutscene;

        return false;
    }

    private void MarkKingdom4Completed()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.CollectAllerthiaKey();
        }
    }

    /// <summary>
    /// Call this to allow the trigger to fire again (e.g., after a restart).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
