using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

/// <summary>
/// Trigger collider at the end of Kingdom 4.
/// First completion: plays a timeline cutscene, then shows the game end screen.
/// Subsequent completions: immediately shows the game end screen.
/// </summary>
public class K4_EndingTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Assign the Collider that the player must enter. Must have 'Is Trigger' enabled.")]
    [SerializeField] private Collider triggerCollider;

    [Header("References")]
    [Tooltip("Timeline to play on FIRST completion only. Leave empty to skip cutscene.")]
    [SerializeField] private PlayableDirector firstCompletionTimeline;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void Start()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[K4_EndingTrigger] Collider on '{triggerCollider.gameObject.name}' was NOT set as Trigger. Enabling isTrigger.");
            triggerCollider.isTrigger = true;
        }

        Debug.Log($"[K4_EndingTrigger] Ready on '{gameObject.name}'. " +
                  $"Collider: {(triggerCollider != null ? triggerCollider.gameObject.name : "NONE")}, " +
                  $"Timeline: {(firstCompletionTimeline != null ? firstCompletionTimeline.gameObject.name : "NONE")}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[K4_EndingTrigger] OnTriggerEnter by '{other.gameObject.name}' tag='{other.tag}' hasTriggered={hasTriggered}");

        if (hasTriggered) return;
        if (!IsPlayer(other)) return;

        hasTriggered = true;

        // Stop the game timer
        if (AllergenGameManager.Instance != null)
            AllergenGameManager.Instance.StopTimer();

        // Mark kingdom as completed and save
        MarkKingdom4Completed();

        bool isFirstCompletion = !IsKingdom4CompletedBefore();
        Debug.Log($"[K4_EndingTrigger] isFirstCompletion={isFirstCompletion}");

        if (isFirstCompletion && firstCompletionTimeline != null)
        {
            StartCoroutine(PlayTimelineThenEndGame());
        }
        else
        {
            Debug.Log("[K4_EndingTrigger] Skipping timeline, going straight to game end.");
            TriggerGameEnd();
        }
    }

    private IEnumerator PlayTimelineThenEndGame()
    {
        // Force-enable the timeline's GameObject in case something disabled it
        if (!firstCompletionTimeline.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[K4_EndingTrigger] Timeline GameObject was inactive! Enabling it.");
            firstCompletionTimeline.gameObject.SetActive(true);
        }

        // Make the timeline play in unscaled time so Time.timeScale = 0 won't freeze it
        firstCompletionTimeline.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        Debug.Log("[K4_EndingTrigger] Playing first completion timeline...");
        firstCompletionTimeline.Play();

        // Wait for timeline to finish using real time
        while (firstCompletionTimeline != null &&
               firstCompletionTimeline.state == PlayState.Playing)
        {
            yield return null;
        }

        Debug.Log("[K4_EndingTrigger] Timeline finished.");

        // Mark that the cutscene has been played
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentGameData != null)
        {
            GameDataManager.Instance.CurrentGameData.hasPlayedK4CompletionCutscene = true;
            GameDataManager.Instance.SaveGameData();
        }

        TriggerGameEnd();
    }

    /// <summary>
    /// Calls the game managers to show the end screen.
    /// Calls AllerthriaGameManager.CompleteGame() first (which internally calls 
    /// Kingdom4GameEndManager.HandleKingdom4Complete()). 
    /// Falls back to Kingdom4GameEndManager directly if AllerthriaGameManager is absent.
    /// </summary>
    private void TriggerGameEnd()
    {
        Debug.Log($"[K4_EndingTrigger] TriggerGameEnd — " +
                  $"AllerthriaGM={AllerthriaGameManager.Instance != null} (isGameComplete={AllerthriaGameManager.Instance?.isGameComplete}), " +
                  $"K4EndMgr={Kingdom4GameEndManager.Instance != null}");

        // Primary path: AllerthriaGameManager → CompleteGame → HandleKingdom4Complete
        if (AllerthriaGameManager.Instance != null && !AllerthriaGameManager.Instance.isGameComplete)
        {
            AllerthriaGameManager.Instance.CompleteGame();
            return;
        }

        // If isGameComplete was already set (e.g. by PlayTimelineOnTrigger or timer),
        // CompleteGame() would early-return, so call the end manager directly.
        if (Kingdom4GameEndManager.Instance != null)
        {
            Debug.Log("[K4_EndingTrigger] isGameComplete already true or AllerthriaGM absent. Calling HandleKingdom4Complete directly.");
            Kingdom4GameEndManager.Instance.HandleKingdom4Complete();
            return;
        }

        Debug.LogError("[K4_EndingTrigger] No game manager found! Cannot show end screen.");
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;

        if (other.transform.root.CompareTag(playerTag))
        {
            Debug.Log("[K4_EndingTrigger] Tag found on root, proceeding.");
            return true;
        }

        Debug.Log($"[K4_EndingTrigger] Tag mismatch. Expected '{playerTag}', got '{other.tag}'");
        return false;
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
            GameDataManager.Instance.CollectAllerthiaKey();
    }

    /// <summary>
    /// Call this to allow the trigger to fire again (e.g., after a restart).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
